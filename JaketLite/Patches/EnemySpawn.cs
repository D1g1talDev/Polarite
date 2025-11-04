using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using StreamIntegration.Multiplayer;

using UnityEngine;

namespace StreamIntegration.Patches
{
    [HarmonyPatch(typeof(EnemyIdentifier))]
    internal class EnemySpawn
    {
        public static bool killActive;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void ChangeToRandomUser(ref string ___overrideFullName, EnemyIdentifier __instance)
        {
            List<string> names = new List<string>();
            names.AddRange(StreamMessageGetter.namesY);
            names.AddRange(StreamMessageGetter.namesT);
            if (names.Count > 0 && Plug.chatBosses.value)
            {
                string randName = names[Random.Range(0, names.Count)];
                if (!string.IsNullOrEmpty(randName))
                {
                    ___overrideFullName = randName;
                    if (__instance.GetComponent<BossHealthBar>() != null)
                    {
                        __instance.GetComponent<BossHealthBar>().ChangeName(randName);
                    }
                }
            }
        }
        [HarmonyPatch(nameof(EnemyIdentifier.Death), new System.Type[] { })]
        [HarmonyPostfix]
        static void PPFix(EnemyIdentifier __instance)
        {
            if (killActive && NetworkManager.InLobby)
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "EidDeath",
                    name = __instance.enemyType.ToString()
                });
            }
        }

    }
}
