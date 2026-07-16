using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GroundCheck))]
    internal class ShockwavePatch
    {
        private static bool wasStillHolding, canShockwave;
        [HarmonyPatch(nameof(GroundCheck.UpdateState))]
        [HarmonyPrefix]
        static void Prefix(GroundCheck __instance)
        {
            if (NetworkManager.InLobby)
            {
                wasStillHolding = __instance.nmov.stillHolding;
                canShockwave = __instance.superJumpChance > 0f;
            }
        }
        [HarmonyPatch(nameof(GroundCheck.UpdateState))]
        [HarmonyPostfix]
        static void Postfix(GroundCheck __instance)
        {
            if(NetworkManager.InLobby && __instance.superJumpChance == 0f && wasStillHolding && canShockwave)
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.transform.position);
                w.WriteVector3(__instance.transform.forward);
                w.WriteVector3(__instance.transform.up);
                w.WriteFloat(__instance.nmov.slamForce * 2.25f);
                NetworkManager.Instance.BroadcastPacket(PacketType.SlamShockwave, w.GetBytes());
            }
        }
    }
}
