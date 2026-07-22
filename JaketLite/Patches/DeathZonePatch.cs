using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(DeathZone))]
    internal class DeathZonePatch
    {
        [HarmonyPatch("GotHit")]
        [HarmonyPostfix]
        static void Postfix(DeathZone __instance)
        {
            if (!NetworkManager.InLobby)
            {
                return;
            }
            if (__instance.deathType == "tram")
            {
                DeadPatch.Death("was ran over by a tram");
            }
            else
            {
                DeadPatch.Death(__instance.notInstakill ? "walked into the danger zone" : "fell into danger");
            }
        }
    }
}
