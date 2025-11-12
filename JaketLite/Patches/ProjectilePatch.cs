using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Projectile))]
    internal class ProjectilePatch
    {
        [HarmonyPatch("TimeToDie")]
        [HarmonyPostfix]
        static void TimeToDie(Projectile __instance)
        {
            DeadPatch.Death("was shot by ", (ulong)__instance.safeEnemyType);
        }
    }
}
