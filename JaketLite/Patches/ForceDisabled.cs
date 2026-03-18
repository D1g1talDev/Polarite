using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILL.Cheats;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(DisabledEnemiesChecker))]
    internal class ForceDisabled
    {
        [HarmonyPatch(nameof(DisabledEnemiesChecker.Update))]
        [HarmonyPrefix]
        static void DisableArenaTriggers(DisabledEnemiesChecker __instance)
        {
            if(!__instance.activated && NetworkManager.InLobby)
            {
                __instance.Invoke(nameof(__instance.Activate), __instance.delay);
            }
        }
    }
}
