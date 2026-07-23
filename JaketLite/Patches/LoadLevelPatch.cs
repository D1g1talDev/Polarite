using HarmonyLib;
using Polarite.Multiplayer;
using Polarite.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(SceneHelper))]
    public class LoadLevelPatch
    {
        [HarmonyPatch(nameof(SceneHelper.LoadScene))]
        [HarmonyPrefix]
        static bool Prefix(ref string sceneName, ref GameObject ___loadingBlocker)
        {
            if(NetworkManager.InLobby)
            {
                ChatUI.Instance.ForceOff();
                Net.Pause();
                NetworkSkull.ClearHolders();
                NetworkManager.SceneLoading = true;
                SceneHelper.SetLoadingSubtext("<color=#91FFFF>+++ VIA POLARITE ---");
            }
            if (sceneName == "Main Menu" && NetworkManager.InLobby)
            {
                NetworkManager.Instance.LeaveLobby();
                ItePlugin.ignoreSpectate = true;
            }
            if(sceneName == "Level 7-4" && NetworkManager.InLobby)
            {
                SceneHelper.LoadScene("Level 8-1");
                return false;
            }
            if(sceneName == "Intermission1")
            {
                SceneHelper.LoadScene("Level 4-1");
                return false;
            }
            if(sceneName == "Intermission2")
            {
                SceneHelper.LoadScene("Level 7-1");
                return false;
            }
            if(NetworkManager.HostAndConnected)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(sceneName);
                w.WriteInt(PrefsManager.Instance.GetInt("difficulty"));
                NetworkManager.Instance.BroadcastPacket(PacketType.Level, w.GetBytes());
                NetworkManager.Instance.CurrentLobby.SetData("level", sceneName);
                NetworkManager.Instance.CurrentLobby.SetData("difficulty", PrefsManager.Instance.GetInt("difficulty").ToString());
                return true;
            }
            if(NetworkManager.ClientAndConnected && sceneName == "Endless" && CyberSync.Active)
            {
                ___loadingBlocker.SetActive(false);
                ChatUI.Message($"<color=orange>{NetworkManager.GetNameOfId(NetworkManager.GetHostID(), true)} hasn't finished looking at the results screen yet.</color>", 5f);
                return false;
            }
            if(NetworkManager.ClientAndConnected && (sceneName == "uk_construct" || sceneName == "Endless") && !ItePlugin.ignoreSpectate)
            {
                ___loadingBlocker.SetActive(false);
                ChatUI.Message($"<color=red>Only the host ({NetworkManager.GetNameOfId(NetworkManager.GetHostID(), true)}) can load into that level.</color>", 5f);
                return false;
            }
            if(NetworkManager.ClientAndConnected && sceneName != "Main Menu" && SceneHelper.CurrentScene != "Main Menu" && NetworkManager.players.Count > 1 && !ItePlugin.ignoreSpectate)
            {
                ItePlugin.SpectatePlayers(true);
                ___loadingBlocker.SetActive(false);
                ItePlugin.ignoreSpectate = true;
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(SceneHelper.RestartSceneAsync))]
        [HarmonyPrefix]
        static bool Prefix()
        {
            // also run the restart level prefix
            return RestartLevelPatch.Prefix();
        }
    }
    [HarmonyPatch(typeof(OptionsManager))]
    public class RestartLevelPatch
    {
        [HarmonyPatch(nameof(OptionsManager.RestartMission))]
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (NetworkManager.HostAndConnected)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Restart, w.GetBytes());
                return true;
            }
            if(CyberSync.Active && !ItePlugin.cameFromPacketRestart)
            {
                ChatUI.Message($"<color=orange>{NetworkManager.GetNameOfId(NetworkManager.GetHostID(), true)} hasn't finished looking at the results screen yet.</color>", 10f);
                return false;
            }
            if (NetworkManager.ClientAndConnected && !ItePlugin.cameFromPacketRestart && !CyberSync.Active)
            {
                return false;
            }
            return true;
        }
    }
}
