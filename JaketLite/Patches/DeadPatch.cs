using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using Steamworks;

using UnityEngine;

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

        public static int DeadPlayers = 0;
        public static List<NetworkPlayer> DeadPs = new List<NetworkPlayer>();

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
        public static bool SpectateOnDeath;
        public static bool IsDeadInSpectate;

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        static void Postfix(DeathSequence __instance)
        {
            if (!NetworkManager.InLobby)
                return;

            string final = DeathMessage;
            if (DeathMessage.Contains("{0}"))
            {
                string enemy = Enum.GetName(typeof(EnemyType), (int)Arg);
                string player = NetworkManager.GetNameOfId(Arg);

                if (!string.IsNullOrEmpty(enemy))
                {
                    final = string.Format(DeathMessage, enemy);
                }
                else if (!string.IsNullOrEmpty(player))
                {
                    final = string.Format(DeathMessage, player);
                }
                else
                {
                    final = string.Format(DeathMessage, Arg);
                }
            }
            PacketWriter w = new PacketWriter();
            w.WriteString(final);
            NetworkManager.Instance.BroadcastPacket(PacketType.Die, w.GetBytes());

            NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(NetworkManager.Id) + " " + final);
            NetworkPlayer.ToggleEidForAll(false);
        }

        public static void Respawn(Vector3 pos, Quaternion rot)
        {
            NewMovement m = MonoSingleton<NewMovement>.Instance;
            if(m.hp > 0)
            {
                return;
            }
            ItePlugin.StopSpectating();
            m.transform.position = pos;
            m.transform.rotation = rot;
            m.cc.ResetCamera(m.transform.eulerAngles.y + 0.01f);
            m.Respawn();
            IsDeadInSpectate = false;
        }
    }
}
