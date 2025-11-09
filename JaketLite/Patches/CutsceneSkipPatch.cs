using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch]
    internal class SkipVotePatch
    {
        public static class SkipVoteManager
        {
            public static Dictionary<ulong, bool> votes = new Dictionary<ulong, bool>();
            public static int neededToSkip = 0;
            public static int lastAcceptCount = 0;
            public static int lastTotal = 1;
            public static bool localVoteSet = false;
            public static bool localVoteValue = false;

            public static void HandleClientVote(ulong clientId, bool accept)
            {
                votes[clientId] = accept;
                UpdateAndBroadcast();
            }

            public static void UpdateAndBroadcast()
            {
                int acceptCount = 0;
                int total = 0;
                if (NetworkManager.Instance.CurrentLobby.Id != 0)
                    total = NetworkManager.Instance.CurrentLobby.MemberCount;
                else
                    total = 1;

                foreach (var m in NetworkManager.Instance.CurrentLobby.Members)
                {
                    if (votes.ContainsKey(m.Id.Value) && votes[m.Id.Value])
                        acceptCount++;
                }

                neededToSkip = total;

                PacketWriter w = new PacketWriter();
                w.WriteInt(acceptCount);
                w.WriteInt(neededToSkip);
                w.WriteInt(total);
                NetworkManager.Instance.BroadcastPacket(PacketType.SkipVoteUpdate, w.GetBytes());

                if (acceptCount >= neededToSkip)
                {
                    PacketWriter w2 = new PacketWriter();
                    NetworkManager.Instance.BroadcastPacket(PacketType.SkipExecute, w2.GetBytes());
                    ExecuteSkipLocal();
                }
            }

            public static void ApplyUpdateClientSide(int acceptCount, int needed, int total)
            {
                lastAcceptCount = acceptCount;
                lastTotal = total;
                neededToSkip = needed;

                int votesLeft = Math.Max(0, neededToSkip - lastAcceptCount);
                int displayAccept = lastAcceptCount;

                string text = $"Skip votes: {displayAccept}/{neededToSkip} — {votesLeft} votes left. Press <color=orange>F1</color> to accept, <color=orange>F2</color> to decline";

                var txtMono = MonoSingleton<CutsceneSkipText>.Instance;
                if (txtMono != null && txtMono.txt != null)
                {
                    txtMono.txt.text = text;
                    txtMono.Show();
                }
                else
                {
                    HudMessageReceiver.Instance.SendHudMessage(text);
                }
            }

            public static void ExecuteSkipLocal()
            {
                var skip = GameObject.FindObjectOfType<CutsceneSkip>();
                if (skip != null)
                {
                    try
                    {
                        skip.onSkip.Invoke("");
                        var txtMono = MonoSingleton<CutsceneSkipText>.Instance;
                        if (txtMono != null)
                            txtMono.Hide();
                        else
                            HudMessageReceiver.Instance.SendHudMessage("Cutscene skipped by lobby vote.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("Failed to execute cutscene skip: " + e);
                    }
                }
            }

            public static void SendVote(bool accept)
            {
                if (!NetworkManager.InLobby) return;

                int predictedAccept = lastAcceptCount;
                if (!localVoteSet)
                {
                    if (accept) predictedAccept += 1;
                }
                else
                {
                    if (localVoteValue && !accept) predictedAccept -= 1;
                    if (!localVoteValue && accept) predictedAccept += 1;
                }

                localVoteSet = true;
                localVoteValue = accept;

                int votesLeft = Math.Max(0, neededToSkip - predictedAccept);

                string voteText = accept
                    ? $"You voted to skip. ({predictedAccept}/{neededToSkip}) — {votesLeft} left to skip. Press <color=orange>F2</color> to change vote."
                    : $"You declined to skip. ({predictedAccept}/{neededToSkip}) — {votesLeft} left to skip. Press <color=orange>F1</color> to change vote.";

                var txtMono = MonoSingleton<CutsceneSkipText>.Instance;
                if (txtMono != null && txtMono.txt != null)
                {
                    txtMono.txt.text = voteText;
                    txtMono.Show();
                }
                else
                {
                    HudMessageReceiver.Instance.SendHudMessage(voteText);
                }

                PacketWriter w = new PacketWriter();
                w.WriteBool(accept);
                NetworkManager.Instance.SendToHost(PacketType.SkipVoteRequest, w.GetBytes());
            }
        }

        [HarmonyPatch(typeof(CutsceneSkip), "Start")]
        [HarmonyPrefix]
        static bool StartPrefix(CutsceneSkip __instance)
        {
            if (!NetworkManager.InLobby)
                return true;

            __instance.SendMessage("OnDisable");

            if (NetworkManager.HostAndConnected)
            {
                SkipVoteManager.votes.Clear();
                foreach (var m in NetworkManager.Instance.CurrentLobby.Members)
                    SkipVoteManager.votes[m.Id.Value] = false;

                SkipVoteManager.votes[NetworkManager.Id] = true;
                SkipVoteManager.UpdateAndBroadcast();
            }
            else
            {
                var txtMono = MonoSingleton<CutsceneSkipText>.Instance;
                string text = "Lobby: Voting to skip cutscene. Press <color=orange>F1</color> to accept, <color=orange>F2</color> to decline.";
                if (txtMono != null && txtMono.txt != null)
                {
                    txtMono.txt.text = text;
                    txtMono.Show();
                }
                else
                {
                    HudMessageReceiver.Instance.SendHudMessage(text);
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(CutsceneSkip), "LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdatePrefix(CutsceneSkip __instance)
        {
            if (!NetworkManager.InLobby)
                return true;

            if (Input.GetKeyDown(KeyCode.F1))
                SkipVoteManager.SendVote(true);
            else if (Input.GetKeyDown(KeyCode.F2))
                SkipVoteManager.SendVote(false);

            return false;
        }
    }
}
