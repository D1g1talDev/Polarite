using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(RevolverBeam))]
    internal class BeamPatch
    {
        [HarmonyPatch(nameof(RevolverBeam.ExecuteHits))]
        [HarmonyPostfix]
        static void ExecuteHits(RevolverBeam __instance)
        {
            if(__instance.beamType != BeamType.Enemy)
            {
                return;
            }
            DeadPatch.DeathMessage = "was shot by " + __instance.ignoreEnemyType.ToString();
        }
    }
}
