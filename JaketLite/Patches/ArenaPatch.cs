using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using HarmonyLib;
using UnityEngine;
using Steamworks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(ActivateArena))]
    internal class ArenaPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Postfix(ActivateArena __instance)
        {
            if (NetworkManager.InLobby)
            {
                __instance.doors = new Door[0];
            }
        }
        [HarmonyPatch("OnTriggerEnter")]
        [HarmonyPrefix]
        static bool ActivatePrefixA(ActivateArena __instance, ref Collider other)
        {
            if(other.GetComponent<NewMovement>() == null && NetworkManager.InLobby)
            {
                return false;
            }
            NetworkManager.Instance.BroadcastPacket(new NetPacket
            {
                type = "arena",
                name = SceneObjectCache.GetScenePath(__instance.gameObject),
            });
            return true;
        }
        [HarmonyPatch("OnEnable")]
        [HarmonyPrefix]
        static bool ActivatePrefixB(ActivateArena __instance)
        {
            if (!__instance.activateOnEnable && NetworkManager.InLobby)
            {
                return false;
            }
            NetworkManager.Instance.BroadcastPacket(new NetPacket
            {
                type = "arena",
                name = SceneObjectCache.GetScenePath(__instance.gameObject),
            });
            return true;
        }
    }
}
