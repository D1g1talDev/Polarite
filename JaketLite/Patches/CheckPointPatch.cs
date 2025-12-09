using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using Steamworks;

using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(CheckPoint))]
    internal class CheckPointPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void PostfixStart(CheckPoint __instance)
        {
            if(!SceneObjectCache.Contains(__instance.gameObject))
            {
                SceneObjectCache.Add(__instance.gameObject);
            }
        }

        [HarmonyPatch(nameof(CheckPoint.ActivateCheckPoint))]
        [HarmonyPrefix]
        static void Prefix(CheckPoint __instance)
        {
            if (NetworkManager.InLobby && !NetworkManager.Sandbox && SceneHelper.CurrentScene != "Level 4-S")
            {
                __instance.roomsToInherit.Clear();
            }
        }

        [HarmonyPatch(nameof(CheckPoint.OnRespawn))]
        [HarmonyPostfix]
        static void Postfix(CheckPoint __instance)
        {
            if (NetworkManager.InLobby && !NetworkManager.Sandbox && SceneHelper.CurrentScene != "Level 4-S")
            {
                __instance.onRestart.Invoke();
                __instance.toActivate.SetActive(true);
                NewMovement m = MonoSingleton<NewMovement>.Instance;
                PlatformerMovement p = MonoSingleton<PlatformerMovement>.Instance;
                if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS)
                {
                    m.transform.position = __instance.transform.position + Vector3.up * 1.25f;
                    float num = __instance.transform.rotation.eulerAngles.y + 0.01f + __instance.additionalSpawnRotation;
                    if (m != null && m.transform.parent && m.transform.parent.gameObject.CompareTag("Moving"))
                    {
                        num -= m.transform.parent.rotation.eulerAngles.y;
                    }
                    m.cc.ResetCamera(num);
                    m.Respawn();
                }
                else
                {
                    p.transform.position = __instance.transform.position + Vector3.up * 1.25f;
                    float num2 = __instance.transform.rotation.eulerAngles.y + 0.01f + __instance.additionalSpawnRotation;
                    if (p != null && p.transform.parent && p.transform.parent.gameObject.CompareTag("Moving"))
                    {
                        num2 -= p.transform.parent.rotation.eulerAngles.y;
                    }
                    p.ResetCamera(num2);
                    p.Respawn();
                }
            }
        }

        [HarmonyPatch(nameof(CheckPoint.OnRespawn))]
        [HarmonyPrefix]
        static void Prefix2(CheckPoint __instance)
        {
            if (NetworkManager.InLobby && !NetworkManager.Sandbox && SceneHelper.CurrentScene != "Level 4-S")
            {
                __instance.newRooms.Clear();
            }
        }
        [HarmonyPatch(nameof(CheckPoint.ActivateCheckPoint))]
        [HarmonyPrefix]
        static void Postfix2(CheckPoint __instance)
        {
            if (NetworkManager.InLobby && !__instance.activated)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.Checkpoint, w.GetBytes());
                NetworkManager.ShoutCheckpoint(NetworkManager.GetNameOfId(NetworkManager.Id));
            }
        }
    }
}
