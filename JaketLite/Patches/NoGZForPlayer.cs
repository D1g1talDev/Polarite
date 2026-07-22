using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GoreZone))]
    internal class NoGZForPlayer
    {
        [HarmonyPatch(nameof(GoreZone.ResolveGoreZone))]
        [HarmonyPrefix]
        static bool Prefix(ref Transform transform)
        {
            if(transform.GetComponent<NetworkPlayer>() != null)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch(nameof(GoreZone.DestroyNextFrame))]
        [HarmonyPrefix]
        static void Prefix2(GoreZone __instance)
        {
            NetworkPlayer p = __instance.toDestroy.Select(o => o?.GetComponent<NetworkPlayer>()).FirstOrDefault(c => c != null);
            __instance.toDestroy.Remove(p.gameObject);
        }
    }
}
