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
    [HarmonyPatch(typeof(SteamController))]
    internal class SteamPatch
    {
        [HarmonyPatch(nameof(SteamController.FetchSceneActivity))]
        [HarmonyPostfix]
        static void Postfix()
        {
            if (!NetworkManager.HasRichPresence)
            {
                return;
            }
            StockMapInfo instance = StockMapInfo.Instance;
            if(instance != null && !string.IsNullOrEmpty(instance.assets.Deserialize().LargeText))
            {
                SteamFriends.SetRichPresence("level", instance.assets.Deserialize().LargeText + $" ({NetworkManager.Instance.CurrentLobby.MemberCount}/{NetworkManager.Instance.CurrentLobby.MaxMembers} In Polarite Lobby)");
            }
        }
    }
}
