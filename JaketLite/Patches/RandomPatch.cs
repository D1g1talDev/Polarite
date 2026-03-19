using HarmonyLib;
using Polarite.Multiplayer;
using Randomness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(RandomBase<RandomGameObjectEntry>))]
    internal class RandomPatch
    {
        [HarmonyPatch(nameof(RandomBase<RandomGameObjectEntry>.WeightedPick))]
        [HarmonyPrefix]
        static bool Prefix(RandomBase<RandomGameObjectEntry> __instance, ref List<RandomGameObjectEntry> __0, ref RandomGameObjectEntry __result)
        {
            if(NetworkManager.InLobby && __0.Count > 0)
            {
                __result = __0[0];
                return false;
            }
            return true;
        }
    }
}
