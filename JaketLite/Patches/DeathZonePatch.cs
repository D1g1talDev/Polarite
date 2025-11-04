using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(DeathZone))]
    internal class DeathZonePatch
    {
        [HarmonyPatch("GotHit")]
        [HarmonyPostfix]
        static void Postfix(DeathZone __instance)
        {
            if(__instance.deathType == "tram")
            {
                DeadPatch.DeathMessage = "was ran over by a tram";
            }
            else
            {
                DeadPatch.DeathMessage = (__instance.notInstakill) ? "walked into the danger zone" : "fell into danger";
            }
        }
    }
}
