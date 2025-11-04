using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(NewMovement))]
    internal class DashPatch
    {
        [HarmonyPatch("Dodge")]
        [HarmonyPostfix]
        public static void Postfix()
        {
            /*
            if(NetworkManager.InLobby && !MonoSingleton<InputManager>.Instance.InputSource.Dodge.WasPerformedThisFrame)
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "dash",
                    name = "d"
                });
            }
            */
        }
    }
}
