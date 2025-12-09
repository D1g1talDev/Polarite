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

            // respawn host aswell
            ItePlugin.Ghost(false);
            NetworkEnemy.Flush();
        }
        public static void Load(ArenaPattern pat, int w)
        {
            NetworkEnemy.Flush();
            current = pat;
            wave = w;
            EndlessGrid.instance.NextWave();
            EndlessGrid.Instance.waveNumberText.transform.parent.parent.gameObject.SetActive(true);
            Collider col = EndlessGrid.instance.GetComponent<Collider>();
            if (col.enabled)
            {
                col.enabled = false;
                GameObject.Find("Everything").transform.Find("Timer").gameObject.SetActive(true);
                return;
            }
            CrowdReactions.instance.React(CrowdReactions.instance.cheerLong);
            NewMovement i = MonoSingleton<NewMovement>.instance;
            WeaponCharges.instance.MaxCharges();
            i.ResetHardDamage();
            i.exploded = false;
            i.GetHealth(9999, true);
            i.FullStamina();
            if (NetworkPlayer.selfIsGhost)
            {
                ItePlugin.Ghost(false);
            }
        }
        public static bool LastPlayerAlive()
        {
            int alive = 0;
            foreach (NetworkPlayer p in NetworkManager.players.Values)
            {
                if (!p.isGhost)
                {
                    alive++;
                }
            }
            return alive <= 1;
        }
        public static int PlayersAlive()
        {
            int alive = 0;
            foreach (NetworkPlayer p in NetworkManager.players.Values)
            {
                if (!p.isGhost)
                {
                    alive++;
                }
            }
            return alive;
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
                pattern = current;
                __instance.currentWave = wave;
            }
            return true;
        }
        [HarmonyPatch(nameof(EndlessGrid.SpawnOnGrid))]
        [HarmonyPrefix]
        static bool NoDupes(ref bool enemy)
        {
            if (NetworkManager.ClientAndConnected && enemy)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void UpdateLeft(EndlessGrid __instance, ref ActivateNextWave ___anw)
        {
            if (NetworkManager.ClientAndConnected)
            {
                __instance.currentWave = wave;
                ___anw.deadEnemies = -5;
            }
        }
        public static void DoubleCheckForSoftlock()
        {
            if(!NetworkManager.HostAndConnected)
            {
                return;
            }
            int alive;
            if(NetworkPlayer.selfIsGhost)
            {
                alive = PlayersAlive() - 1;
            }
            else
            {
                alive = PlayersAlive();
            }
            if (alive < 1)
            {
                ItePlugin.GameOver();
            }
        }
    }
    [HarmonyPatch(typeof(FinalCyberRank))]
    internal class CyberDeathSync
    {
        [HarmonyPatch(nameof(FinalCyberRank.GameOver))]
        [HarmonyPrefix]
        static bool GameOverPrefix()
        {
            if(!NetworkManager.InLobby)
            {
                return true;
            }
            if (CyberSync.LastPlayerAlive() && NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.CyberGameOver, w.GetBytes());
                return true;
            }
            else
            {
                ItePlugin.Ghost(true);
                return !NetworkManager.InLobby;
            }
        }
    }
}
