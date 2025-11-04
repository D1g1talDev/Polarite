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
            if(NetworkManager.Instance.CurrentLobby.GetData("cheat") == "0" && NetworkManager.ClientAndConnected)
            {
                NetworkManager.DisplayError("The host disabled cheating!");
                return false;
            }
            if(SceneHelper.CurrentScene != "uk_construct")
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "cheater",
                    name = NetworkManager.GetNameOfId(SteamClient.SteamId),
                });
                NetworkManager.ShoutCheater(NetworkManager.GetNameOfId(SteamClient.SteamId));
            }
            return true;
        }
    }
}
