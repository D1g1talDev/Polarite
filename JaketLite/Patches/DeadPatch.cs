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
        public static string[] DeathMessages = new string[]
        {
            "died",
            "was friendly fired by {0}",
            "was shot by {0}",
            "was ran over by a tram",
            "walked into the danger zone", /* : */ "fell into danger",
            "exploded",
            "was slain by {0}",
            "was shot by {0}",
            "was smited"
        };

        public static void Death(string str, ulong arg = 0) {
            for (int i = 0; i < DeathMessages.Length; i++) {
                if (DeathMessages[i].Contains(str)) {
                    deathMessage = (byte)i;
                    Arg = arg;
                    return;
                }
            }
        }

        public static string DeathMessage => DeathMessages[deathMessage];
        public static byte deathMessage = 0;
        static ulong Arg = 0;

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        static void Postfix()
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteByte(deathMessage);
                w.WriteULong(Arg);
                NetworkManager.Instance.BroadcastPacket(PacketType.Die, w.GetBytes());
                NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(NetworkManager.Id) + " " + DeathMessage);
                NetworkPlayer.ToggleEidForAll(false);
            }
        }
    }
}
