using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;

namespace Polarite.Patches
{
    // jackets

    [HarmonyPatch(typeof(EndlessGrid))]
    internal class CyberSync
    {
        public static int wave;

        public static ArenaPattern current;

        public static bool Active => SceneHelper.CurrentScene == "Endless";

        public static void Sync(ArenaPattern pat)
        {
            PacketWriter w = new PacketWriter();
            w.WriteInt(EndlessGrid.instance.currentWave);
            w.WriteString(pat.heights);
            w.WriteString(pat.prefabs);
            current = pat;
            wave = EndlessGrid.instance.currentWave;
            NetworkManager.Instance.BroadcastPacket(PacketType.CyberPattern, w.GetBytes());
        }
        public static void Load(ArenaPattern pat, int w)
        {
            current = pat;
            wave = w;
            EndlessGrid.instance.NextWave();
            EndlessGrid.instance.LoadPattern(pat);
            Collider col = EndlessGrid.instance.GetComponent<Collider>();
            if (col.enabled)
            {
                col.enabled = false;
                GameObject.Find("Everything").transform.Find("Timer").gameObject.SetActive(true);
                return;
            }
            CrowdReactions.instance.React(CrowdReactions.instance.cheerLong);
            if(MonoSingleton<NewMovement>.instance.hp > 0)
            {
                NewMovement i = MonoSingleton<NewMovement>.instance;
                WeaponCharges.instance.MaxCharges();
                i.ResetHardDamage();
                i.exploded = false;
                i.GetHealth(9999, true);
                i.FullStamina();
            }
            else
            {
                DeadPatch.Respawn(FindRespawnPosition(), Quaternion.identity);
            }
        }

        public static Vector3 FindRespawnPosition()
        {
            Vector3 basePos = EndlessGrid.instance.transform.position;

            var players = NetworkManager.players
                .Where(p => !DeadPatch.DeadPs.Contains(p.Value))
                .ToList();

            if (players.Count > 0)
                basePos = players[UnityEngine.Random.Range(0, players.Count)].Value.transform.position;

            Vector3 candidate = basePos + new Vector3(UnityEngine.Random.Range(-5f, 5f), 10f, UnityEngine.Random.Range(-5f, 5f));
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 50f, LayerMask.GetMask("Default", "Environment")))
            {
                return hit.point + Vector3.up * 1.5f;
            }

            return basePos + Vector3.up * 2f;
        }


        // patch stuff

        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        static bool OnlyHost()
        {
            return !NetworkManager.ClientAndConnected;
        }
        [HarmonyPatch(nameof(EndlessGrid.LoadPattern))]
        [HarmonyPrefix]
        static bool LoadPatternPrefix(EndlessGrid __instance, ref ArenaPattern pattern)
        {
            if (NetworkManager.InLobby && NetworkManager.HostAndConnected)
            {
                Sync(pattern);
            }
            else if (NetworkManager.InLobby)
            {
                current = pattern;
                __instance.currentWave = wave;
            }
            if(NetworkManager.HostAndConnected && MonoSingleton<NewMovement>.instance.dead)
            {
                DeadPatch.Respawn(FindRespawnPosition(), Quaternion.identity);
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(FinalCyberRank))]
    public class DeadPatchButCyber
    {
        [HarmonyPatch(nameof(FinalCyberRank.GameOver))]
        [HarmonyPrefix]
        static bool GameOvering()
        {
            if (!NetworkManager.InLobby)
                return true;

            int deadCount = DeadPatch.DeadPlayers;
            int total = NetworkManager.Instance.CurrentLobby.MemberCount;

            if (deadCount >= total)
            {
                return true;
            }
            foreach (var p in NetworkManager.players)
            {
                if (DeadPatch.DeadPs.Contains(p.Value))
                {
                    Vector3 respawnPos = CyberSync.FindRespawnPosition();
                    DeadPatch.Respawn(respawnPos, Quaternion.identity);
                }
            }
            return false;
        }
    }

}
