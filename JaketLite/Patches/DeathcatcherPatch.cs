using HarmonyLib;
using Polarite.Multiplayer;
using Polarite.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Deathcatcher))]
    internal class DeathcatcherPatch
    {
        [HarmonyPatch(nameof(Deathcatcher.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix(Deathcatcher __instance)
        {
            if(NetworkManager.InLobby && !__instance.gameObject.Owner())
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(Deathcatcher.RespawnPuppets))]
        [HarmonyPostfix]
        static void SyncEffect(Deathcatcher __instance)
        {
            if (NetworkManager.InLobby && __instance.gameObject.Owner())
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.DeathcatchRespawn, w.GetBytes());
            }
        }
    }
}
