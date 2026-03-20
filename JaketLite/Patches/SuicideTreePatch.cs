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
    internal class SuicideTreePatch
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
}
