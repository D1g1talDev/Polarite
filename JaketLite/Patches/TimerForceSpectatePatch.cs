using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(StatsManager))]
    internal class TimerForceSpectatePatch
    {
        [HarmonyPatch(nameof(StatsManager.StartTimer))]
        [HarmonyPostfix]
        static void Postfix()
        {
            if (NetworkManager.HostAndConnected)
            {
                NetworkManager.Instance.CurrentLobby.SetData("forceS", "1");
            }
        }
    }
}
