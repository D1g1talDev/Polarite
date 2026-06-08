using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(OnLevelStart))]
    internal class LevelStartPatch
    {
        [HarmonyPatch(nameof(OnLevelStart.StartLevel))]
        [HarmonyPrefix]
        static void LevelSPrefix(OnLevelStart __instance, ref bool startTimer, ref bool startMusic)
        {
            if(NetworkManager.InLobby && !__instance.activated)
            {
                PacketWriter w = new PacketWriter();
                w.WriteBool(startTimer);
                w.WriteBool(startMusic);
                NetworkManager.Instance.BroadcastPacket(PacketType.LevelStart, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(OnLevelStart.StartLevel))]
        [HarmonyPostfix]
        static void LevelSPostfix()
        {
            if(NetworkManager.InLobby)
            {
                if(NetworkManager.HostAndConnected)
                {
                    NetworkManager.Instance.CurrentLobby.SetData("levelStarted", "1");
                }
                if(CyberSync.Active)
                {
                    if (CyberSync.current == null && EndlessGrid.Instance.gameObject.activeSelf && CyberSync.LobbyHasPattern && int.TryParse(NetworkManager.Instance.CurrentLobby.GetData("cyberWave"), out int wav) && NetworkManager.ClientAndConnected)
                    {
                        ItePlugin.Instance.StartCoroutine(ItePlugin.DelayLoadServerPattern(wav));
                    }
                }
            }
        }
    }
}
