using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using Steamworks;

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
            if (__instance.GetComponent<INetworkObject>() == null && NetworkManager.InLobby)
            {
                NetworkEnemy.Create(__instance, NetworkManager.GetNearestPlayerID(__instance.transform.position));
            }
        }
        [HarmonyPatch(nameof(EnemyIdentifier.DeliverDamage))]
        [HarmonyPostfix]
        static void Damage(EnemyIdentifier __instance, ref float multiplier, ref GameObject sourceWeapon, ref GameObject target, ref Vector3 hitPoint)
        {
            if(__instance.hitter == "drill")
            {
                return;
            }
            if (__instance.hitter == "fire")
            {
                return;
            }
            /*
            if(__instance.TryGetComponent<NetworkPlayer>(out var netP) && multiplier > 0f)
            {
                netP.HandleFriendlyFire(NetworkManager.Id, Mathf.RoundToInt(multiplier));
                return;
            }
            */
            try
            {
                if (sourceWeapon.GetComponent<NetworkPlayer>() != null)
                {
                    return;
                }
            }
            catch
            {
                // ignore
            }
            NetworkEnemy netE = __instance.GetComponent<NetworkEnemy>();
            if (netE != null)
            {
                netE.BroadcastDamage(multiplier, __instance.hitter, target == __instance.weakPoint, hitPoint);
            }
        }
    }
}
