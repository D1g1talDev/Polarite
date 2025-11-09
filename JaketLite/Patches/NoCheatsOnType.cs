using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(CheatsManager))]
    internal class NoCheatsOnType
    {
        [HarmonyPatch(nameof(CheatsManager.ToggleCheat))]
        [HarmonyPrefix]
        static bool StopCheatsIfTyping()
        {
            return !ChatUI.isTyping;
        }
    }
}
