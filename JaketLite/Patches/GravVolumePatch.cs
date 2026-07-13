using Gravity;
using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GravityVolume))]
    internal class GravVolumePatch
    {
        [HarmonyPatch(nameof(GravityVolume.OnTriggerEnter))]
        [HarmonyPrefix]
        static void Prefix(GravityVolume __instance, ref Collider other)
        {
            if(other.gameObject == MonoSingleton<NewMovement>.Instance.gameObject && NetworkManager.InLobby && __instance.playerRequests <= 0)
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.CalculateGravityVector());
                NetworkManager.Instance.BroadcastPacket(PacketType.GravVol, w.GetBytes());
                if(NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.PortalRotate(__instance.CalculateGravityVector());
                }
            }
        }
    }
}
