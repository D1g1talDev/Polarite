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
        public static EnemyIdentifier Spawn(EnemyType type, Vector3 pos, Quaternion rot)
        {
            GameObject enemy = DefaultReferenceManager.Instance.GetEnemyPrefab(type);
            EnemyIdentifier eid = enemy.GetComponentInChildren<EnemyIdentifier>(true);
            if (eid.GetComponent<NetworkObject>() == null)
            {
                eid.gameObject.AddComponent<NetworkObject>();
            }
            return eid;
        }
        public static void TestSpawn(EnemyType type)
        {
            Spawn(type, NewMovement.Instance.transform.position, Quaternion.identity);
        }
    }
}
