using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Projectile))]
    internal class ProjectilePatch
    {
        [HarmonyPatch("TimeToDie")]
        [HarmonyPostfix]
        static void TimeToDie(Projectile __instance)
        {
            if (!NetworkManager.InLobby)
            {
                return;
            }
            DeadPatch.Death("was shot by ", (ulong)__instance.safeEnemyType);
        }
    }
}
