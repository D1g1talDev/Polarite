using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox;

using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Polarite
{
    public static class EntityStorage
    {
        public static Dictionary<EnemyType, string> EnemyToAdd = new Dictionary<EnemyType, string>();

        public static void StoreAll()
        {
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    string keyStr = key.ToString();
                    if (keyStr.Contains("Prefabs/Enemies") && Addressables.LoadAssetAsync<GameObject>(keyStr).WaitForCompletion().TryGetComponent<EnemyIdentifier>(out var eid))
                    {
                        if(EnemyToAdd.ContainsKey(eid.enemyType))
                        {
                            continue;
                        }
                        if(eid.enemyType == EnemyType.Drone && keyStr.Contains("Flesh"))
                        {
                            continue;
                        }
                        if (eid.enemyType == EnemyType.Drone && keyStr.Contains("Skull"))
                        {
                            continue;
                        }
                        if (eid.enemyType == EnemyType.Drone && keyStr.Contains("Camera"))
                        {
                            continue;
                        }
                        EnemyToAdd.Add(eid.enemyType, keyStr);
                    }
                }
            }
        }
        public static EnemyIdentifier Spawn(EnemyType type, Vector3 pos, Quaternion rot, bool sandbox)
        {
            if (EnemyToAdd.ContainsKey(type))
            {
                GameObject enemy = Addressables.InstantiateAsync(EnemyToAdd[type], pos, rot).WaitForCompletion();
                EnemyIdentifier eid = enemy.GetComponent<EnemyIdentifier>();
                if (eid != null)
                {
                    if(sandbox)
                    {
                        eid.gameObject.AddComponent<SandboxEnemy>();
                    }
                }
                return eid;
            }
            return null;
        }
    }
}
