using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(CutsceneSkip))]
    internal class SkipPatch
    {
        [HarmonyPatch(nameof(CutsceneSkip.Update))]
        [HarmonyPostfix]
        static void Never(CutsceneSkip __instance)
        {
            MonoSingleton<CutsceneSkipText>.Instance.Hide();
            __instance.waitingForInput = false;
        }
    }
}
