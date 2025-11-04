using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(DoorController))]
    internal class DoorPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void OpenIfPlayerNear(DoorController __instance)
        {
            if(NetworkManager.InLobby)
            {
                Vector3 doorPos = __instance.transform.position;
                bool found = false;
                foreach (var p in NetworkManager.players.Values)
                {
                    if (Vector3.SqrMagnitude(doorPos - p.transform.position) <= 75f)
                    {
                        found = true;
                        break;
                    }
                }
                __instance.enemyIn = found;
            }
        }
    }
    [HarmonyPatch(typeof(FinalDoor))]
    internal class FinalDoorPatch
    {
        [HarmonyPatch(nameof(FinalDoor.Open))]
        [HarmonyPrefix]
        static void Postfix(FinalDoor __instance)
        {
            if(NetworkManager.InLobby && !__instance.aboutToOpen)
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "finalopen",
                    name = SceneObjectCache.GetScenePath(__instance.gameObject)
                });
            }
        }
    }
    [HarmonyPatch(typeof(Door))]
    internal class DoorStuff
    {
        [HarmonyPatch(nameof(Door.Lock))]
        [HarmonyPrefix]
        static bool Prefix()
        {
            return false;
        }
        [HarmonyPatch(nameof(Door.Optimize))]
        [HarmonyPrefix]
        static bool Prefix2()
        {
            return false;
        }
    }
}
