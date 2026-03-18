using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox;

using UnityEngine;
using UnityEngine.AddressableAssets;
using Polarite.Multiplayer;

namespace Polarite
{
    public static class EntityStorage
    {
        public static Dictionary<string, string> EnemyToAdd = new Dictionary<string, string>();

        public static void StoreAll()
        {
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    string keyStr = key.ToString();
                    if (keyStr.Contains("Prefabs/Enemies"))
                    {
                        GameObject obj = Addressables.LoadAssetAsync<GameObject>(keyStr).WaitForCompletion();
                        if(obj != null)
                        {
                            if (obj.TryGetComponent<EnemyIdentifier>(out var eid))
                            {
                                if (eid.enemyType == EnemyType.Drone && keyStr.Contains("Flesh"))
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
                                if(keyStr.Contains("Green Arm"))
                                {
                                    eid.overrideFullName = "V2(2)";
                                }
                                if (keyStr.Contains("Vertex"))
                                {
                                    continue;
                                }
                                if (EnemyToAdd.ContainsKey(eid.FullName))
                                {
                                    continue;
                                }
                                EnemyToAdd.Add(eid.FullName, keyStr);
                            }
                            else if (obj.GetComponentInChildren<EnemyIdentifier>(true) != null && NotFake(keyStr))
                            {
                                EnemyIdentifier cEid = obj.GetComponentInChildren<EnemyIdentifier>(true);
                                if (keyStr.Contains("Very Can"))
                                {
                                    continue;
                                }
                                if (keyStr.Contains("Big John"))
                                {
                                    cEid.enemyType = EnemyType.BigJohnator;
                                }
                                if (keyStr.Contains("Mandalore"))
                                {
                                    continue;
                                }
                                if (keyStr.Contains("Green Arm"))
                                {
                                    continue;
                                }
                                if (cEid.enemyType == EnemyType.Drone && keyStr.Contains("Flesh"))
                                {
                                    continue;
                                }
                                if (cEid.enemyType == EnemyType.Drone && keyStr.Contains("Skull"))
                                {
                                    continue;
                                }
                                if (cEid.enemyType == EnemyType.Drone && keyStr.Contains("Camera"))
                                {
                                    continue;
                                }
                                if (keyStr.Contains("Vertex"))
                                {
                                    continue;
                                }
                                if (keyStr.Contains("Green Arm"))
                                {
                                    cEid.overrideFullName = "V2(2)";
                                }
                                if (EnemyToAdd.ContainsKey(cEid.FullName))
                                {
                                    continue;
                                }
                                EnemyToAdd.Add(cEid.FullName, keyStr);
                            }
                        }
                    }
                }
            }
        }
        public static bool NotFake(string keyStr)
        {
            if (keyStr.Contains("CerberusStatue")) return false;
            if(keyStr.Contains("MannequinPoserWithEnemy")) return false;
            return true;
        }
        // no more sandbox enemies apparently
        public static EnemyIdentifier Spawn(string name, Vector3 pos, Quaternion rot)
        {
            if (EnemyToAdd.ContainsKey(name))
            {
                GameObject enemy = Addressables.InstantiateAsync(EnemyToAdd[name], pos, rot).WaitForCompletion();
                EnemyIdentifier eid = enemy.GetComponentInChildren<EnemyIdentifier>(true);
                if(eid.GetComponent<NetworkObject>() == null)
                {
                    eid.gameObject.AddComponent<NetworkObject>();
                }
                return eid;
            }
            return null;
        }
        public static void TestSpawn(EnemyType type)
        {
            Spawn(EnemyTypes.GetEnemyName(type), NewMovement.Instance.transform.position, Quaternion.identity);
        }
    }
}
