using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using HarmonyLib;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Skull))]
    internal class SkullConvert
    {
        [HarmonyPatch(nameof(Skull.Awake))]
        [HarmonyPostfix]
        static void AwakePatch(Skull __instance)
        {
            /* not for now
            if(__instance.GetComponent<NetworkSkull>() == null && NetworkManager.InLobby)
            {
                __instance.gameObject.AddComponent<NetworkSkull>();
            }
            */
        }
    }
}
