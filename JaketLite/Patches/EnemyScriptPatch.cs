using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILL.Cheats;
using ULTRAKILL.Enemy;
using ULTRAKILL.Portal;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(EnemyScript))]
    internal class EnemyScriptPatch
    {
        // should hopefully force the targets to be the players?
        [HarmonyPatch(nameof(EnemyScript.CheckTarget))]
        [HarmonyPrefix]
        static bool Prefix(TargetDataRef __0, EnemyIdentifier __1, bool __result)
        {
            ITarget target = __0.target;
            if (NetworkPlayer.IsPlayer(__1.target.targetTransform.gameObject) && NetworkPlayer.IsPlayer(target.GameObject) && NetworkManager.InLobby)
            {
                __result = true;
                return false;
            }
            if (__1.target.isPlayer)
            {
                if (__1.ignorePlayer)
                {
                    __result = false;
                    return false;
                }

                if (!target.isPlayer)
                {
                    __result = false;
                    return false;
                }

                __result = true;
                return true;
            }

            if (__1.target.isEnemy)
            {
                if (!target.isEnemy)
                {
                    __result = false;
                    return false;
                }

                if (__1.target.enemyIdentifier != null)
                {
                    __result = (object)__1.target.enemyIdentifier == target.EID;
                    return __result;
                }

                if (__1.enemyClass == target.EID.enemyClass)
                {
                    __result = false;
                    return false;
                }

                if (!__1.attackEnemies)
                {
                    __result = false;
                    return false;
                }

                __result = EnemiesHateEnemies.Active;
                return EnemiesHateEnemies.Active;
            }
            __result = false;
            return false;
        }
    }
}
