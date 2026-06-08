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
                NewMovement nm = __instance.nm;
                GameObject player = __instance.player;
                PlatformerMovement p = MonoSingleton<PlatformerMovement>.Instance;
                if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS)
                {
                    player.transform.position = __instance.transform.position + __instance.transform.up * 1.25f;
                    Rigidbody component = player.GetComponent<Rigidbody>();
                    component.velocity = Vector3.zero;
                    if (nm == null)
                    {
                        nm = MonoSingleton<NewMovement>.Instance;
                    }

                    CameraController cc = nm.cc;
                    nm.rb.SetCustomGravityMode(useCustomGravity: false);
                    cc.gravityRotation = Quaternion.identity;
                    cc.gravityVec = Physics.gravity.normalized;
                    cc.rotationOffset = Quaternion.identity;
                    cc.transitionRotationZ = 0f;
                    cc.transitionRotationZSmooth = 0f;
                    cc.tiltRotationZ = 0f;
                    cc.tiltRotationZSmooth = 0f;
                    float num = __instance.transform.rotation.eulerAngles.y + 0.01f + __instance.additionalSpawnRotation;
                    if ((bool)player && (bool)player.transform.parent && player.transform.parent.gameObject.CompareTag("Moving"))
                    {
                        num -= player.transform.parent.rotation.eulerAngles.y;
                    }

                    if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS)
                    {
                        cc.ResetCamera(num);
                    }
                    else
                    {
                        MonoSingleton<PlatformerMovement>.Instance.ResetCamera(num);
                    }

                    cc.ApplyRotations();
                    nm.rb.SetCustomGravity(__instance.gravity);
                    nm.rb.SetCustomGravityMode(useCustomGravity: true);
                    nm.gc.heavyFall = false;
                    cc.Transform(Matrix4x4.identity, __instance.gravity);
                    MonoSingleton<CameraController>.Instance.activated = true;
                    component.position = __instance.transform.position + __instance.transform.up * 1.25f;
                    if (!nm.enabled)
                    {
                        nm.enabled = true;
                    }

                    nm.Respawn();
                    nm.GetHealth(0, silent: true);
                    nm.cc.StopShake();
                    nm.ActivatePlayer();
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
                NetworkManager.ShoutCheckpoint(NetworkManager.GetNameOfId(NetworkManager.Id, true));
            }
        }
    }
}
