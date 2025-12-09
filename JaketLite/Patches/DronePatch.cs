using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Drone))]
    internal class DronePatch
    {
        [HarmonyPatch(nameof(Drone.Hooked))]
        [HarmonyPostfix]
        static void Postfix(Drone __instance)
        {
            if (NetworkManager.InLobby && __instance.TryGetComponent<NetworkEnemy>(out var netE))
            {
                netE.TakeOwnership(NetworkManager.Id);
            }
        }
    }
}
