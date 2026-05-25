using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(DualWieldPickup))]
    internal class DualWieldSandboxBan
    {
        [HarmonyPatch(nameof(DualWieldPickup.PickedUp))]
        [HarmonyPostfix]
        static void Postfix()
        {
            if(NetworkManager.InLobby && MonoSingleton<GunControl>.Instance.dualWieldCount > 3)
            {
                ItePlugin.ForceKillSelf("broke the law");
            }
        }
    }
}
