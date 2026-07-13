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
                bool found = false;
                foreach (var p in NetworkManager.players.Values)
                {
                    if (Vector3.SqrMagnitude(__instance.transform.position - p.transform.position) <= (SceneHelper.CurrentScene == "Level 8-1" ? 5f : 50f))
                    {
                        found = true;
                        break;
                    }
                }
                __instance.enemyIn = found;
                foreach(var room in __instance.dc.activatedRooms)
                {
                    if(__instance.open && !room.activeSelf)
                    {
                        room.SetActive(true);
                    }
                }
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
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.FinalOpen, w.GetBytes());
            }
        }
    }
    [HarmonyPatch(typeof(Door))]
    internal class DoorStuff
    {
        [HarmonyPatch(nameof(Door.Lock))]
        [HarmonyPrefix]
        static bool Prefix(Door __instance)
        {
            if(__instance.name == "Barrier")
            {
                return true;
            }
            return !NetworkManager.InLobby;
        }
    }
}
