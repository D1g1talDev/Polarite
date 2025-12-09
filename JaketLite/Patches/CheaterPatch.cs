using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using Steamworks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(CheatsController))]
    internal class CheaterPatch
    {
        [HarmonyPatch(nameof(CheatsController.ActivateCheats))]
        [HarmonyPrefix]
        static bool Prefix()
        {
            if(!SteamClient.IsValid)
            {
                return true;
            }
            if(NetworkManager.Instance.CurrentLobby.GetData("cheat") == "0" && NetworkManager.ClientAndConnected)
            {
                NetworkManager.DisplayError("The host disabled cheating!");
                return false;
            }
            if(SceneHelper.CurrentScene != "uk_construct" && NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(NetworkManager.GetNameOfId(NetworkManager.Id));
                NetworkManager.Instance.BroadcastPacket(PacketType.Cheater, w.GetBytes());
                NetworkManager.ShoutCheater(NetworkManager.GetNameOfId(NetworkManager.Id));
            }
            return true;
        }
    }
}
