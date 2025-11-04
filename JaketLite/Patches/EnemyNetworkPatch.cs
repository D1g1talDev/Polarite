using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(EnemyIdentifier))]
    internal class EnemyNetworkPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Spawn(EnemyIdentifier __instance)
        {
            if (__instance.GetComponent<NetworkEnemy>() == null && __instance.gameObject.scene.name != null && NetworkManager.InLobby)
            {
                NetworkEnemy.Create(__instance.GetComponent<NetworkEnemySync>().id, __instance);
            }
        }
        [HarmonyPatch(nameof(EnemyIdentifier.DeliverDamage))]
        [HarmonyPostfix]
        static void Damage(EnemyIdentifier __instance, ref float multiplier, ref Vector3 force, ref GameObject target, ref Vector3 hitPoint)
        {
            if(force == Vector3.zero)
            {
                return;
            }
            NetworkEnemy netE = __instance.GetComponent<NetworkEnemy>();
            if (netE != null)
            {
                netE.BroadcastDamage(multiplier, __instance.hitter, target == __instance.weakPoint, hitPoint);
            }
        }
    }
}
