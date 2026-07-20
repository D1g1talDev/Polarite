using HarmonyLib;
using Polarite.Multiplayer;
using Polarite.Networking.Skins;
using Polarite.SamTTS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SceneHelper;
using Random = UnityEngine.Random;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(NewMovement))]
    internal class HurtPatch
    {
        public static string[] HurtNoises = new string[]
        {
            "ow",
            "ouch"
        };

        [HarmonyPatch(nameof(NewMovement.GetHurt))]
        [HarmonyPrefix]
        static bool Prefix(NewMovement __instance, ref int damage, ref bool invincible)
        {
            bool wasIFrames = invincible && __instance.hurtInvincibility > 0f;
            if(ItePlugin.immuneToDeath)
            {
                ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("SlamFail"), Random.Range(0.95f, 1.15f), MonoSingleton<CameraController>.Instance.transform, 1f);
                return false;
            }
            if(NetworkManager.InLobby && damage > 0 && !wasIFrames && !__instance.boost)
            {
                PacketWriter w = new PacketWriter();
                w.WriteSam(SamPitch.configSam);
                NetworkManager.Instance.BroadcastPacket(PacketType.Hurt, w.GetBytes());
                if(ItePlugin.ttsHurtAndDeath.value && ItePlugin.canTTS.value)
                {
                    SamPitch.Set();
                    TextReader.SayString(HurtNoises[Random.Range(0, HurtNoises.Length)]);
                    SamPitch.Reset();
                }
            }
            return true;
        }
        [HarmonyPatch(nameof(NewMovement.Respawn))]
        [HarmonyPostfix]
        static void RespawnPostfix()
        {
            if(NetworkManager.InLobby)
            {
                DeadPatch.deathMessage = 0;
                DeadPatch.lastMessage = 0;
                DeadPatch.Arg = 0;
                DeadPatch.LastArg = 0;
                if(ItePlugin.currentScreaming != null)
                {
                    ItePlugin.Instance.StopCoroutine(ItePlugin.currentScreaming);
                    if(ItePlugin.screaming != null)
                    {
                        ItePlugin.Destroy(ItePlugin.screaming);
                    }
                }

                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Respawn, w.GetBytes());
                CameraController.Instance.cameraShaking = 0;
                foreach(var pince in GameObject.FindObjectsOfType<Pincer>(true))
                {
                    GameObject.Destroy(pince.gameObject);
                }
            }
        }

        [HarmonyPatch(nameof(NewMovement.Jump))]
        [HarmonyPostfix]
        static void JPatch(NewMovement __instance)
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteBool(__instance.quakeJump);
                NetworkManager.Instance.BroadcastPacket(PacketType.Jump, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(NewMovement.WallJump))]
        [HarmonyPostfix]
        static void WJPatch(NewMovement __instance)
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteBool(__instance.quakeJump);
                NetworkManager.Instance.BroadcastPacket(PacketType.Jump, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(NewMovement.DeactivateMovement))]
        [HarmonyPostfix]
        static void DisablePatch()
        {
            NetworkPlayer.ToggleColsForAll(false);
        }
        [HarmonyPatch(nameof(NewMovement.DeactivatePlayer))]
        [HarmonyPostfix]
        static void OtherDisablePatch()
        {
            NetworkPlayer.ToggleColsForAll(false);
        }
        [HarmonyPatch(nameof(NewMovement.ReactivateMovement))]
        [HarmonyPostfix]
        static void EnablePatch()
        {
            NetworkPlayer.ToggleColsForAll(true);
        }
        [HarmonyPatch(nameof(NewMovement.ActivatePlayer))]
        [HarmonyPostfix]
        static void OtherEnablePatch()
        {
            NetworkPlayer.ToggleColsForAll(true);
        }
        [HarmonyPatch(nameof(NewMovement.Update))]
        [HarmonyPostfix]
        static void UpdatePatch(NewMovement __instance)
        {
            if(NetworkPlayer.selfIsGhost)
            {
                __instance.stillHolding = false;
            }
        }
        [HarmonyPatch(nameof(NewMovement.Start))]
        [HarmonyPostfix]
        static void UpdateRig(NewMovement __instance)
        {
            if(NetworkManager.InLobby)
            {
                PlayerAnimations rig = __instance.gc.GetComponentInChildren<PlayerAnimations>();
                if (rig != null)
                {
                    SkinnedMeshRenderer r = rig.transform.Find("v1_mdl").GetComponent<SkinnedMeshRenderer>();
                    for (int i = 0; i < r.materials.Length; i++)
                    {
                        if (i == 0)
                        {
                            SkinManagerV2.CustomColor(r, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.V1_BASE_MASK, "Base" + NetworkManager.Id, i);
                        }
                        else
                        {
                            // turn the emissive flag off
                            r.materials[i].DisableKeyword("EMISSIVE");
                            SkinManagerV2.CustomColor(r, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.WingLight, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.V1_WING_MASK, "Wing" + NetworkManager.Id, i);
                        }
                    }
                }
            }
        }
        [HarmonyPatch(nameof(NewMovement.Parry))]
        [HarmonyPostfix]
        static void ParryPostfix()
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.PunchParry, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(NewMovement.LandingImpact))]
        [HarmonyPostfix]
        static void ImpactPostfix(NewMovement __instance)
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.gc.transform.position);
                w.WriteVector3(__instance.transform.position);
                w.WriteVector3(__instance.transform.up);
                w.WriteFloat(__instance.fallSpeed);
                NetworkManager.Instance.BroadcastPacket(PacketType.Slam, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(NewMovement.CreateSlideScrape))]
        [HarmonyPrefix]
        static void CSPrefix(NewMovement __instance, ref bool ignorePrevious)
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                HitSurfaceData surf = new HitSurfaceData();
                bool hit = false;
                if(!__instance.gc.onGround && __instance.wcGroup.TryGetActiveInstance(out WallCheck wc))
                {
                    Vector3 normal = (wc.poc - wc.transform.position).normalized;
                    if (MonoSingleton<SceneHelper>.Instance.TryGetSurfaceData(wc.transform.position, normal, 3f, out surf))
                    {
                        hit = true;
                    }
                }
                else if(MonoSingleton<SceneHelper>.Instance.TryGetSurfaceData(__instance.transform.position, __instance.rb.GetGravityDirection(), 3f, out surf))
                {
                    hit = true;
                }
                if (hit)
                {
                    if (!MonoSingleton<DefaultReferenceManager>.Instance.footstepSet.TryGetSlideParticle(surf.surfaceType, out var particle))
                    {
                        surf.surfaceType = SurfaceType.Generic;
                    }
                    if (!ignorePrevious && surf.surfaceType == __instance.currentSlideSurfaceType)
                    {
                        return;
                    }
                    w.WriteEnum(surf.surfaceType);
                    w.WriteVector3(__instance.dodgeDirection);
                    w.WriteColor(surf.particleColor);
                    NetworkManager.Instance.BroadcastPacket(PacketType.SlideScrape, w.GetBytes());
                }
                else
                {
                    if (ignorePrevious || __instance.currentSlideSurfaceType != SurfaceType.Generic)
                    {
                        return;
                    }
                    w.WriteEnum(SurfaceType.Generic);
                    w.WriteVector3(__instance.dodgeDirection);
                    w.WriteColor(surf.particleColor);
                    NetworkManager.Instance.BroadcastPacket(PacketType.SlideScrape, w.GetBytes());
                }
            }
        }
        [HarmonyPatch(nameof(NewMovement.CreateWallScrape))]
        [HarmonyPrefix]
        static void WSPrefix(NewMovement __instance, ref Vector3 position, ref bool ignorePrevious)
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                if(MonoSingleton<SceneHelper>.Instance.TryGetSurfaceData(__instance.transform.position, position - __instance.transform.position, 5f, out var surf))
                {
                    SurfaceType surfaceType = surf.surfaceType;
                    if (!MonoSingleton<DefaultReferenceManager>.Instance.footstepSet.TryGetWallScrapeParticle(surfaceType, out var particle))
                    {
                        surfaceType = SurfaceType.Generic;
                    }
                    if (ignorePrevious || __instance.currentScrapeSurfaceType != surfaceType)
                    {
                        w.WriteEnum(surfaceType);
                        w.WriteVector3(position);
                        w.WriteBool(false);
                    }
                    else
                    {
                        w.WriteEnum(surfaceType);
                        w.WriteVector3(position);
                        w.WriteBool(true);
                    }
                }
                else if (__instance.currentScrapeSurfaceType != SurfaceType.Generic || ignorePrevious)
                {
                    w.WriteEnum(SurfaceType.Generic);
                    w.WriteVector3(position);
                    w.WriteBool(false);
                }
                else
                {
                    w.WriteEnum(SurfaceType.Generic);
                    w.WriteVector3(position);
                    w.WriteBool(true);
                }
                NetworkManager.Instance.BroadcastPacket(PacketType.WallScrape, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(NewMovement.DetachSlideScrape))]
        [HarmonyPostfix]
        static void DSSPostfix()
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.DetachSlideScrape, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(NewMovement.DetachWallScrape))]
        [HarmonyPostfix]
        static void DWSPostfix()
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.DetachWallScrape, w.GetBytes());
            }
        }
    }
}
