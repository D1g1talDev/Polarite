using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILL.Enemy;
using Polarite.Multiplayer;
using ULTRAKILL.Portal;
using System.Reflection;
using Unity.Mathematics;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Vision))]
    internal class VisionPatch
    {
        [HarmonyPatch(nameof(Vision.TrySee))]
        [HarmonyPrefix]
        static bool Prefix(Vision __instance, VisionQuery __0, ref TargetDataRef __1, ref bool __result)
        {
            if(NetworkManager.InLobby && !__instance.filter.HasType(TargetType.GLASS))
            {
                __1 = PortalManagerV2.Instance.TargetTracker.GetDataReference(NetworkEnemy.CreateHandleFrom(__instance.sourcePos));
                __result = true;
                return false;
            }
            return true;
        }   
    }
}
