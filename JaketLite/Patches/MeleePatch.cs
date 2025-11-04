using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(SwingCheck2))]
    internal class MeleePatch
    {
        [HarmonyPatch("CheckCollision")]
        [HarmonyPostfix]
        static void Postfix(SwingCheck2 __instance)
        {
            DeadPatch.DeathMessage = "was slain by " + __instance.type.ToString();
        }
    }
}
