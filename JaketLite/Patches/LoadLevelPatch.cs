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
            }
            if(NetworkManager.InLobby && sceneName == "Endless")
            {
                NetworkManager.DisplayWarningChatMessage("CYBERGRIND SYNC IS NOW IN TESTING!!!!");
            }
            if(sceneName == "Main Menu" && NetworkManager.InLobby)
            {
                NetworkManager.Instance.LeaveLobby();
                ItePlugin.ignoreSpectate = true;
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
                NetworkManager.Instance.CurrentLobby.SetData("forceS", "0");
                SceneHelper.SetLoadingSubtext("<color=#91FFFF>/// VIA POLARITE ///");
                return true;
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
        [HarmonyPatch(nameof(SceneHelper.RestartScene))]
        [HarmonyPostfix]
        static void Postfix()
        {
            if(NetworkManager.HostAndConnected)
            {
                NetworkManager.Instance.CurrentLobby.SetData("forceS", "0");
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Restart, w.GetBytes());
            }
        }
    }
}
