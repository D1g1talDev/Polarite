using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;
using UnityEngine.UIElements.Experimental;

using static UnityEngine.GraphicsBuffer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(FleshPrison))]
    internal class PrisonPatch
    {
        [HarmonyPatch(nameof(FleshPrison.Start))]
        [HarmonyPostfix]
        static void Postfix(FleshPrison __instance)
        {
            if(__instance.altVersion && __instance.GetComponent<CustomP2Event>() == null)
            {
                CustomP2Event p2 = __instance.gameObject.AddComponent<CustomP2Event>();
                p2.eid = __instance.eid;
                p2.pri = __instance;
            }
        }

        [HarmonyPatch(nameof(FleshPrison.SpawnFleshDrones))]
        [HarmonyPrefix]
        static bool NoEyes(FleshPrison __instance)
        {
            if(NetworkManager.InLobby)
            {
                __instance.aud.Stop();
                __instance.shakingCamera = false;
                __instance.currentDrone = 0;
                __instance.fleshDroneCooldown = Mathf.Infinity;
                __instance.healing = false;
                __instance.noDrones = true;
                __instance.SpawnBlackHole();
                return false;
            }
            return true;
        }
    }
}
