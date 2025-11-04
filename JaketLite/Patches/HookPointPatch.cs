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
    [HarmonyPatch(typeof(HookPoint))]
    internal class HookPointPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void PostfixStart(HookPoint __instance)
        {
            if(!SceneObjectCache.Contains(__instance.gameObject))
            {
                SceneObjectCache.Add(__instance.gameObject);
            }
        }

        [HarmonyPatch(nameof(HookPoint.SwitchPulled))]
        [HarmonyPrefix]
        static void Postfix(HookPoint __instance)
        {
            if(NetworkManager.InLobby && __instance.timer <= 0f && __instance.type == hookPointType.Switch)
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "hookS",
                    name = SceneObjectCache.GetScenePath(__instance.gameObject)
                });
            }
        }
    }
}
