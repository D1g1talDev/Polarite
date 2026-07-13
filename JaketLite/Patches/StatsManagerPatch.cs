using HarmonyLib;
using Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILL.Cheats;
using UnityEngine;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(StatsManager))]
    internal class StatsManagerPatch
    {
        [HarmonyPatch(nameof(StatsManager.Restart))]
        [HarmonyPrefix]
        static bool Prefix(StatsManager __instance)
        {
            if(!NetworkManager.InLobby) return true;
            if(CyberSync.Active) return true;
            MonoSingleton<MusicManager>.Instance.ArenaMusicEnd();
            if (!PreventTimerStart.Active)
            {
                __instance.timer = true;
            }
            // this should fix some bugs
            if (__instance.currentCheckPoint == null)
            {
                NewMovement nm = __instance.nm;
                GameObject player = __instance.player;
                PlatformerMovement p = MonoSingleton<PlatformerMovement>.Instance;
                Vector3 pos = PlayerActivator.lastActivatedPosition;
                if (MonoSingleton<PlayerTracker>.Instance.playerType == PlayerType.FPS)
                {
                    player.transform.position = pos;
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

                    cc.ApplyRotations();
                    nm.rb.SetCustomGravity(Physics.gravity);
                    nm.rb.SetCustomGravityMode(useCustomGravity: true);
                    nm.gc.heavyFall = false;
                    cc.Transform(Matrix4x4.identity, Physics.gravity);
                    MonoSingleton<CameraController>.Instance.activated = true;
                    component.position = pos;
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
                    p.transform.position = pos;
                    p.Respawn();
                }
                __instance.restarts++;
                OnLevelStart.Instance.onStart?.Invoke();
                ItePlugin.Flash(false, false);
                return false;
            }
            else
            {
                return true;
            }
        }
        [HarmonyPatch(nameof(StatsManager.StopTimer))]
        [HarmonyPrefix]
        static void FlagEnd(StatsManager __instance)
        {
            if(NetworkManager.InLobby && __instance.timer && !CyberSync.Active)
            {
                PacketWriter w = new PacketWriter();
                int mins = Mathf.FloorToInt(__instance.seconds / 60f);
                float seconds = mins > 0 ? __instance.seconds - 60f * mins : __instance.seconds;
                string time = mins + ":" + seconds.ToString("00.000");
                w.WriteString(time);
                NetworkManager.Instance.BroadcastPacket(PacketType.LevelFinished, w.GetBytes());
                ChatUI.Message($"<color=#FF5E36>{NetworkManager.GetNameOfId(NetworkManager.Id, true)} finished the level with the time: </color><color=#FFDD57>{time}</color>", 7f);
            }
        }
    }
}
