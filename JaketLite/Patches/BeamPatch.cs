using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(RevolverBeam))]
    internal class BeamPatch
    {
        [HarmonyPatch(nameof(RevolverBeam.ExecuteHits))]
        [HarmonyPostfix]
        static void ExecuteHits(RevolverBeam __instance)
        {
            if (!NetworkManager.InLobby)
            {
                return;
            }
            if (__instance.beamType != BeamType.Enemy)
            {
                return;
            }
            DeadPatch.Death("was shot by ", (ulong)__instance.ignoreEnemyType);
        }
    }
}
