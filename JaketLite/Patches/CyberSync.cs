using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(EndlessGrid))]
    public class CyberSync
    {
        public static int wave;

        public static ArenaPattern current;

        public static List<Deathcatcher> catchers = new List<Deathcatcher>();
        public static List<EndlessEvent> events = new List<EndlessEvent>();
        public static List<NetworkEnemy> enemies = new List<NetworkEnemy>();
        public static bool Active => SceneHelper.CurrentScene == "Endless";
        public static bool LobbyHasPattern
        {
            get
            {
                return !string.IsNullOrEmpty(NetworkManager.Instance.CurrentLobby.GetData("cyberHe")) && !string.IsNullOrEmpty(NetworkManager.Instance.CurrentLobby.GetData("cyberPr"));
            }
        }
        public static ArenaPattern LobbyPattern
        {
            get
            {
                return new ArenaPattern
                {
                    heights = NetworkManager.Instance.CurrentLobby.GetData("cyberHe"),
                    prefabs = NetworkManager.Instance.CurrentLobby.GetData("cyberPr")
                };
            }
        }

        public static void SyncPattern(ArenaPattern pat)
        {
            PacketWriter w = new PacketWriter();
            w.WriteInt(EndlessGrid.Instance.currentWave);
            w.WriteString(pat.heights);
            w.WriteString(pat.prefabs);

            NetworkManager.Instance.CurrentLobby.SetData("cyberHe", pat.heights);
            NetworkManager.Instance.CurrentLobby.SetData("cyberPr", pat.prefabs);
            NetworkManager.Instance.CurrentLobby.SetData("cyberWave", EndlessGrid.Instance.currentWave.ToString());

            current = pat;
            wave = EndlessGrid.Instance.currentWave;
            NetworkManager.Instance.BroadcastPacket(PacketType.CyberPattern, w.GetBytes());
            // respawn host aswell
            ItePlugin.Ghost(false);
            NetworkEnemy.Flush();
        }
        public static void LoadPattern(ArenaPattern pat, int wav)
        {
            NetworkEnemy.Flush();
            catchers.Clear();
            events.Clear();
            current = pat;
            wave = wav;
            Collider trigger = EndlessGrid.Instance.GetComponent<Collider>();
            if (trigger.enabled)
            {
                GameObject.Find("Everything").transform.Find("Timer").gameObject.SetActive(true);
                trigger.enabled = false;
                return;
            }
            EndlessGrid.Instance.NextWave();
            EndlessGrid.Instance.waveNumberText.transform.parent.parent.gameObject.SetActive(true);
            CrowdReactions.Instance.React(CrowdReactions.Instance.cheerLong);
            NewMovement i = MonoSingleton<NewMovement>.Instance;
            WeaponCharges.Instance.MaxCharges();
            i.exploded = false;
            i.ResetHardDamage();
            i.FullStamina();
            i.GetHealth(454545, true);
            if (NetworkPlayer.selfIsGhost)
            {
                ItePlugin.Ghost(false);
            }
        }
        public static void BasicLoad(ArenaPattern pat, int wav)
        {
            current = pat;
            wave = wav;
            Collider trigger = EndlessGrid.Instance.GetComponent<Collider>();
            if (trigger.enabled)
            {
                GameObject.Find("Everything").transform.Find("Timer").gameObject.SetActive(true);
                trigger.enabled = false;
                return;
            }
            EndlessGrid.Instance.NextWave();
            EndlessGrid.Instance.waveNumberText.transform.parent.parent.gameObject.SetActive(true);
        }

        // patch stuff
        [HarmonyPatch(nameof(EndlessGrid.Start))]
        [HarmonyPostfix]
        static void LoadPatternOnStart()
        {
            if(NetworkManager.ClientAndConnected && Active && LobbyHasPattern && int.TryParse(NetworkManager.Instance.CurrentLobby.GetData("cyberWave"), out int wave))
            {
                BasicLoad(LobbyPattern, wave);
            }
        }
        [HarmonyPatch(nameof(EndlessGrid.OnTriggerEnter))]
        [HarmonyPrefix]
        static bool OnlyHost()
        {
            return !NetworkManager.ClientAndConnected;
        }
        [HarmonyPatch(nameof(EndlessGrid.LoadPattern))]
        [HarmonyPrefix]
        static bool LoadPatternPrefix(EndlessGrid __instance, ref ArenaPattern pattern)
        {
            if (NetworkManager.HostAndConnected)
            {
                SyncPattern(pattern);
            }
            else if (NetworkManager.ClientAndConnected)
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
        [HarmonyPatch(nameof(EndlessGrid.Update))]
        [HarmonyPrefix]
        static void UpdateLeft(EndlessGrid __instance)
        {
            if (NetworkManager.ClientAndConnected)
            {
                if(current == null && __instance.gameObject.activeSelf && Active && LobbyHasPattern && int.TryParse(NetworkManager.Instance.CurrentLobby.GetData("cyberWave"), out int wav))
                {
                    BasicLoad(LobbyPattern, wav);
                }
                __instance.anw.deadEnemies = 0;
                __instance.currentWave = wave;
                __instance.enemiesLeftText.text = enemies.Count.ToString() ?? "";
            }
        }
        [HarmonyPatch(nameof(EndlessGrid.WaveProgressCheck))]
        [HarmonyPostfix]
        static void WaveCheck(EndlessGrid __instance)
        {
            if(NetworkManager.HostAndConnected)
            {
                float prog = (float)__instance.anw.deadEnemies / (float)(__instance.enemyAmount - __instance.totalDeathcatchers);
                PacketWriter w = new PacketWriter();
                w.WriteFloat(prog);
                NetworkManager.Instance.BroadcastPacket(PacketType.CyberProgress, w.GetBytes());
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
            if(!ItePlugin.canBecomeGhost)
            {
                return true;
            }
            ItePlugin.Ghost(true);
            return !NetworkManager.InLobby;
        }
    }
}
