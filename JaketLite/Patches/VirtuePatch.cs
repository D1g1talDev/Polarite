using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(VirtueInsignia))]
    internal class VirtuePatch
    {
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPostfix]
        static void Postfix(VirtueInsignia __instance)
        {
            DeadPatch.Death("was smited");
        }
    }
}
