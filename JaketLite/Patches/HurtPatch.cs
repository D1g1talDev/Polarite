using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(NewMovement))]
    internal class HurtPatch
    {
        [HarmonyPatch(nameof(NewMovement.GetHurt))]
        [HarmonyPrefix]
        static bool Prefix(ref int damage, ref bool invincible)
        {
            if(ItePlugin.immuneToDeath)
            {
                ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("SlamFail"), Random.Range(0.95f, 1.15f), MonoSingleton<CameraController>.Instance.transform, 1f);
                return false;
            }
            if(NetworkManager.InLobby && damage > 0)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Hurt, w.GetBytes());
            }
            return true;
        }
        [HarmonyPatch(nameof(NewMovement.Respawn))]
        [HarmonyPostfix]
        static void RespawnPostfix()
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Respawn, w.GetBytes());
                CameraController.Instance.cameraShaking = 0;
            }
        }

        [HarmonyPatch(nameof(NewMovement.Jump))]
        [HarmonyPostfix]
        static void JPatch()
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Jump, w.GetBytes());
            }
        }
        [HarmonyPatch("WallJump")]
        [HarmonyPostfix]
        static void WJPatch()
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
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
    }
}
