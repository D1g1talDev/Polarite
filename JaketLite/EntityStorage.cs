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
        public static Dictionary<EnemyType, string> EnemyToAdd = new Dictionary<EnemyType, string>();

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
                                if (keyStr.Contains("Very Can"))
                                {
                                    continue;
                                }
                                if (keyStr.Contains("Big John"))
                                {
                                    eid.enemyType = EnemyType.BigJohnator;
                                }
                                if (keyStr.Contains("Mandalore"))
                                {
                                    continue;
                                }
                                if(keyStr.Contains("Green Arm"))
                                {
                                    continue;
                                }
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
                                if (keyStr.Contains("Vertex"))
                                {
                                    continue;
                                }
                                if (EnemyToAdd.ContainsKey(eid.enemyType))
                                {
                                    continue;
                                }
                                EnemyToAdd.Add(eid.enemyType, keyStr);
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
                                if (EnemyToAdd.ContainsKey(cEid.enemyType))
                                {
                                    continue;
                                }
                                EnemyToAdd.Add(cEid.enemyType, keyStr);
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
        public static EnemyIdentifier Spawn(EnemyType type, Vector3 pos, Quaternion rot, bool sandbox)
        {
            if (EnemyToAdd.ContainsKey(type))
            {
                GameObject enemy = Addressables.InstantiateAsync(EnemyToAdd[type], pos, rot).WaitForCompletion();
                EnemyIdentifier eid = enemy.GetComponentInChildren<EnemyIdentifier>(true);
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
        public static void TestSpawn(EnemyType type)
        {
            Spawn(type, NewMovement.Instance.transform.position, Quaternion.identity, NetworkManager.Sandbox);
        }
    }
}
