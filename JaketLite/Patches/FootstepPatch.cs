using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(PlayerFootsteps))]
    internal class FootstepPatch
    {
        [HarmonyPatch(nameof(PlayerFootsteps.Footstep))]
        [HarmonyPostfix]
        static void Postfix(PlayerFootsteps __instance, ref float volume)
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.transform.position);
                w.WriteFloat(volume);
                NetworkManager.Instance.BroadcastPacket(PacketType.Footstep, w.GetBytes(), sendtype: SendTypeConsts.ST_PLRSTATE);
            }
        }
    }
}
