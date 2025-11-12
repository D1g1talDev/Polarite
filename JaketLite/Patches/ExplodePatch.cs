using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Explosion))]
    internal class ExplodePatch
    {
        [HarmonyPatch("Collide")]
        [HarmonyPostfix]
        static void Postfix()
        {
            DeadPatch.Death("exploded");
        }
    }
}
