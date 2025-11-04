using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using Steamworks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GroundCheckEnemy))]
    internal class GroundEnemyPatch
    {
        [HarmonyPatch(nameof(GroundCheckEnemy.ForceOff))]
        [HarmonyPostfix]
        static void Postfix(GroundCheckEnemy __instance)
        {
            if(NetworkManager.InLobby && __instance.transform.parent.TryGetComponent<NetworkEnemy>(out var netE))
            {
                netE.TakeOwnership(SteamClient.SteamId);
            }
        }
    }
}
