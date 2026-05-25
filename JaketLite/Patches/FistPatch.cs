using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(FistControl))]
    internal class FistPatch
    {
        [HarmonyPatch(nameof(FistControl.Start))]
        [HarmonyPostfix]
        static void Postfix()
        {
            if(NetworkManager.InLobby)
            {
                ItePlugin.ArmCheck(SwapWeaponsPatch.AltWeapon(GunControl.Instance.currentWeapon));
            }
        }
    }
}
