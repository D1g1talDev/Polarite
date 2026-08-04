using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(EnemyInfoPage))]
    internal class ShopEnemies
    {
        [HarmonyPatch(nameof(EnemyInfoPage.Start))]
        [HarmonyPrefix]
        static void Prefix(EnemyInfoPage __instance)
        {
            if(BestiaryEntryManager.data != null)
            {
                List<SpawnableObject> add = new List<SpawnableObject>();
                add.AddRange(__instance.objects.enemies);
                add.Insert(14, BestiaryEntryManager.data);
                __instance.objects.enemies = add.ToArray();
            }    
        }
    }
    [HarmonyPatch(typeof(SpawnMenu))]
    internal class SpawnerArmEnemies
    {
        [HarmonyPatch(nameof(SpawnMenu.Awake))]
        [HarmonyPrefix]
        static void Prefix(SpawnMenu __instance)
        {
            if (BestiaryEntryManager.data != null)
            {
                List<SpawnableObject> add = new List<SpawnableObject>();
                add.AddRange(__instance.objects.enemies);
                add.Insert(14, BestiaryEntryManager.data);
                __instance.objects.enemies = add.ToArray();
            }
        }
    }
}
