using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Explosion))]
    internal class ExplodePatch
    {
        [HarmonyPatch("Collide")]
        [HarmonyPostfix]
        static void Postfix(Explosion __instance)
        {
            PlayerExplosionId expId = __instance.GetComponentInParent<PlayerExplosionId>();
            if (expId != null)
            {
                switch(expId.id)
                {
                    case "blast":
                        DeadPatch.Death("waltzed into the blast of a knuckleblaster from ", expId.player, true);
                        break;
                    case "parry":
                        DeadPatch.Death("ran into the explosion of a parried projectile from ", expId.player, true);
                        break;
                    case "default":
                        DeadPatch.Death("was exploded by ", expId.player, true);
                        break;
                }
            }
            else
            {
                if(__instance.enemy && __instance.originEnemy != null)
                {
                    DeadPatch.Death("was exploded by ", (ulong)__instance.originEnemy.enemyType);
                }
                else
                {
                    DeadPatch.Death("exploded");
                }
            }
        }
    }
}
