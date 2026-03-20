using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Deathcatcher))]
    internal class DeathcatcherPatch
    {
        [HarmonyPatch(nameof(Deathcatcher.Update))]
        [HarmonyPrefix]
        static void Update(Deathcatcher __instance)
        {
            if(NetworkManager.InLobby)
            {
                __instance.active = !NetworkManager.ClientAndConnected;
            } 
        }
    }
}
