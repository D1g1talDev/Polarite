using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(NewMovement))]
    internal class DashPatch
    {
        [HarmonyPatch(nameof(NewMovement.TryDash))]
        [HarmonyPostfix]
        public static void Postfix(NewMovement __instance)
        {
            if(__instance.boostCharge > 100f)
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.inputDir);
                w.WriteVector3(__instance.transform.position);
                NetworkManager.Instance.BroadcastPacket(PacketType.Dash, w.GetBytes());
            }
        }
    }
}
