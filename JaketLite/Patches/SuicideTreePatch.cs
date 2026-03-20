using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(EnemySpawnRadius))]
    internal class SuicideTreePatchSpawn
    {
        [HarmonyPatch(nameof(EnemySpawnRadius.SpawnEnemy))]
        [HarmonyPrefix]
        static bool Prefix()
        {
            if(NetworkManager.InLobby)
            {
                return !NetworkManager.ClientAndConnected;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(BloodFiller))]
    internal class SuicideTreePatchFill
    {
        [HarmonyPatch(nameof(BloodFiller.FullyFilled))]
        [HarmonyPrefix]
        static void SyncFill(BloodFiller __instance)
        {
            if (NetworkManager.InLobby && !__instance.fullyFilled)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.SuicideFill, w.GetBytes());
            }
        }
    }
}
