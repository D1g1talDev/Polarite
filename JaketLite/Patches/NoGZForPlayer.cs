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
    }
}
