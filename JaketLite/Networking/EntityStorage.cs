using Polarite.Multiplayer;
using Polarite.Networking.Extensions;
using Polarite.Patches;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;

namespace Polarite
{
    public static class EntityStorage
    {
        public static EnemyIdentifier Spawn(EnemyType type, Vector3 pos, Quaternion rot, ulong sender, string id)
        {
            GameObject enemy = null;
            // check for missing prefabs because some prefabs are missing from default reference manager, thanks ultrakill dev team
            if(type == EnemyType.MirrorReaper)
            {
                enemy = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/MirrorReaperCyberGrind.prefab").WaitForCompletion(), pos, rot);
            }
            else if(type == EnemyType.Power)
            {
                enemy = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/Power.prefab").WaitForCompletion(), pos, rot);
            }
            else if(type == EnemyType.Deathcatcher && CyberSync.Active)
            {
                enemy = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/DeathcatcherCaseEndless.prefab").WaitForCompletion(), pos, rot);
                if(NetworkManager.HostAndConnected)
                {
                    EndlessGrid.Instance.endlessEvents.Add(enemy.GetComponent<EndlessEvent>());
                    EndlessGrid.Instance.deathcatchers.Add(enemy.GetComponentInChildren<Deathcatcher>(true));
                }
                else
                {
                    CyberSync.events.Add(enemy.GetComponent<EndlessEvent>());
                    CyberSync.catchers.Add(enemy.GetComponentInChildren<Deathcatcher>(true));
                }
            }
            else
            {
                enemy = GameObject.Instantiate(DefaultReferenceManager.Instance.GetEnemyPrefab(type), pos, rot);
            }
            AddNetEnemy(enemy, sender, id);
            EnemyIdentifier eid = enemy.GetComponentInChildren<EnemyIdentifier>(true);
            return eid;
        }
        public static INetworkObject AddNetEnemy(GameObject enemy, ulong sender, string id)
        {
            if (enemy.NetObject() == null)
            {
                NetworkEnemy e = NetworkEnemy.Create(enemy.GetComponentInChildren<EnemyIdentifier>(true), sender, id);
                if(CyberSync.Active && !CyberSync.enemies.Contains(e))
                {
                    CyberSync.enemies.Add(e);
                }
                return e;
            }
            return null;
        }
        public static void TestSpawn(EnemyType type)
        {
            Spawn(type, NewMovement.Instance.transform.position, Quaternion.identity, NetworkManager.Id, "");
        }
    }
}
