using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;
using Random = UnityEngine.Random;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(NewMovement))]
    internal class HurtPatch
    {
        [HarmonyPatch(nameof(NewMovement.GetHurt))]
        [HarmonyPostfix]
        static void Postfix(ref int damage, ref bool invincible)
        {
            if(NetworkManager.InLobby && damage > 0)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Hurt, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(NewMovement.Respawn))]
        [HarmonyPostfix]
        static void RespawnPostfix()
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Respawn, w.GetBytes());
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
