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
    [HarmonyPatch(typeof(GroundCheck))]
    internal class NoGhostSlamming
    {
        [HarmonyPatch(nameof(GroundCheck.OnTriggerEnter))]
        [HarmonyPrefix]
        static bool NoGhostSlam(GroundCheck __instance, Collider other)
        {
            if(NetworkPlayer.selfIsGhost)
            {
                if (__instance.ColliderIsCheckable(other) && !__instance.cols.Contains(other))
                {
                    __instance.cols.Add(other);
                    __instance.touchingGround = true;
                    if ((!other.attachedRigidbody && other.TryGetComponent<CustomGroundProperties>(out var component)) || ((bool)other.attachedRigidbody && other.attachedRigidbody.TryGetComponent<CustomGroundProperties>(out component)))
                    {
                        MonoSingleton<NewMovement>.Instance.groundProperties = component;
                    }
                    else
                    {
                        MonoSingleton<NewMovement>.Instance.groundProperties = null;
                    }

                    if (!__instance.slopeCheck && (other.gameObject.CompareTag("Moving") || other.gameObject.layer == 11 || other.gameObject.layer == 26) && other.attachedRigidbody != null && !__instance.pmov.IsObjectTracked(other.transform))
                    {
                        __instance.pmov.AttachPlayer(other.transform);
                    }
                }
                else if (!other.gameObject.CompareTag("Slippery") && other.gameObject.layer == 12)
                {
                    __instance.currentEnemyCol = other;
                    __instance.canJump = true;
                }

                if (__instance.heavyFall)
                {
                    if ((other.gameObject.layer == 10 || other.gameObject.layer == 11) && !Physics.Raycast(__instance.transform.position + Vector3.up, other.bounds.center - __instance.transform.position + Vector3.up, Vector3.Distance(__instance.transform.position + Vector3.up, other.bounds.center), LayerMaskDefaults.Get(LMD.Environment)))
                    {
                        EnemyIdentifierIdentifier component2 = other.gameObject.GetComponent<EnemyIdentifierIdentifier>();
                        if ((bool)component2 && (bool)component2.eid && !__instance.hitEnemies.Contains(component2.eid))
                        {
                            bool dead = component2.eid.dead;
                            __instance.hitEnemies.Add(component2.eid);
                            AudioSource.PlayClipAtPoint(ItePlugin.mainBundle.LoadAsset<AudioClip>("SlamFail"), other.transform.position);
                            if (!dead)
                            {
                                if (!GameStateManager.Instance.PlayerInputLocked && MonoSingleton<InputManager>.Instance.InputSource.Jump.IsPressed)
                                {
                                    __instance.Bounce(__instance.transform.position);
                                }
                                else if (__instance.bounceChance <= 0f)
                                {
                                    __instance.bouncePosition = __instance.transform.position;
                                    __instance.bounceChance = 0.15f;
                                }
                            }
                        }
                    }
                    else if (!other.gameObject.CompareTag("Slippery") && LayerMaskDefaults.IsMatchingLayer(other.gameObject.layer, LMD.Environment))
                    {
                        __instance.superJumpChance = 0.1f;
                    }
                }

                if (!__instance.slopeCheck && other.gameObject.layer == 4 && ((MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS && __instance.nmov.sliding && __instance.nmov.rb.velocity.y < 0f) || (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.Platformer && MonoSingleton<PlatformerMovement>.Instance.sliding && MonoSingleton<PlatformerMovement>.Instance.rb.velocity.y < 0f)))
                {
                    Vector3 a = other.ClosestPoint(__instance.transform.position);
                    if (!MonoSingleton<UnderwaterController>.Instance.inWater && ((Vector3.Distance(a, __instance.transform.position) < 0.1f && other.Raycast(new Ray(__instance.transform.position + Vector3.up * 1f, Vector3.down), out var _, 1.1f)) || !Physics.Raycast(__instance.transform.position, Vector3.down, Vector3.Distance(a, __instance.transform.position), LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies), QueryTriggerInteraction.Collide)))
                    {
                        __instance.BounceOnWater(other);
                    }
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(GroundCheck.FixedUpdate))]
        [HarmonyPrefix]
        static bool NoGhostSlamAgain(GroundCheck __instance)
        {
            if (NetworkPlayer.selfIsGhost)
            {
                if (__instance.heavyFall)
                {
                    Collider[] array = RaycastAssistant.TrueSphereCastAll(__instance.transform.position, 1.25f, Vector3.down, 3f, LayerMaskDefaults.Get(LMD.Enemies));
                    if (array != null)
                    {
                        Collider[] array2 = array;
                        foreach (Collider collider in array2)
                        {
                            if ((collider.gameObject.layer != 10 && collider.gameObject.layer != 11) || Physics.Raycast(__instance.transform.position + Vector3.up, collider.bounds.center - __instance.transform.position + Vector3.up, Vector3.Distance(__instance.transform.position + Vector3.up, collider.bounds.center), LayerMaskDefaults.Get(LMD.Environment)))
                            {
                                continue;
                            }

                            EnemyIdentifierIdentifier component = collider.gameObject.GetComponent<EnemyIdentifierIdentifier>();
                            if (!component || !component.eid || __instance.hitEnemies.Contains(component.eid))
                            {
                                continue;
                            }

                            bool dead = component.eid.dead;
                            __instance.hitEnemies.Add(component.eid);
                            AudioSource.PlayClipAtPoint(ItePlugin.mainBundle.LoadAsset<AudioClip>("SlamFail"), collider.transform.position);
                            if (!dead)
                            {
                                if (!GameStateManager.Instance.PlayerInputLocked && MonoSingleton<InputManager>.Instance.InputSource.Jump.IsPressed)
                                {
                                    __instance.Bounce(__instance.transform.position);
                                }
                                else if (__instance.bounceChance <= 0f)
                                {
                                    __instance.bouncePosition = __instance.transform.position;
                                    __instance.bounceChance = 0.15f;
                                }
                            }
                        }
                    }
                }

                if (!MonoSingleton<UnderwaterController>.Instance.inWater && !__instance.slopeCheck && !(MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity().y >= 0f) && (MonoSingleton<PlayerTracker>.Instance.playerType != 0 || MonoSingleton<NewMovement>.Instance.sliding) && (MonoSingleton<PlayerTracker>.Instance.playerType != PlayerType.Platformer || MonoSingleton<PlatformerMovement>.Instance.sliding) && Physics.Raycast(__instance.transform.position, Vector3.down, out var hitInfo, Mathf.Abs(MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity().y), __instance.waterMask, QueryTriggerInteraction.Collide) && hitInfo.transform.gameObject.layer == 4)
                {
                    __instance.BounceOnWater(hitInfo.collider);
                }
                return false;
            }
            return true;
        }
    }
}
