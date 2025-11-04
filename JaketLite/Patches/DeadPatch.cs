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
    [HarmonyPatch(typeof(DeathSequence))]
    internal class DeadPatch
    {
        public static string DeathMessage = "died";

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        static void Postfix()
        {
            if(NetworkManager.InLobby)
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "die",
                    name = DeathMessage
                });
                NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(SteamClient.SteamId) + " " + DeathMessage);
            }
        }
    }
}
