using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Polarite.Multiplayer;
using Sandbox;
using Random = UnityEngine.Random;
using Steamworks;

namespace Polarite.Multiplayer
{
    public class NetworkEnemy : MonoBehaviour
    {
        public string ID;
        public EnemyIdentifier Enemy;
        public bool IsAlive = true;
        public float HP;
        public ulong Owner = 0;

        private static readonly Dictionary<string, NetworkEnemy> allEnemies = new Dictionary<string, NetworkEnemy>();
        private static Coroutine globalTargetUpdater;

        private float lastSyncTime;
        private Vector3 lastPos;
        private Quaternion lastRot;
        public Vector3 targetPos;
        public Quaternion targetRot;

        private static readonly WaitForSeconds targetUpdateDelay = new WaitForSeconds(1f);

        public static NetworkEnemy Create(string id, EnemyIdentifier eid, ulong owner)
        {
            var netE = eid.gameObject.AddComponent<NetworkEnemy>();
            netE.ID = id;
            netE.Enemy = eid;
            netE.IsAlive = true;
            netE.Owner = owner;
            allEnemies[id] = netE;

            if (globalTargetUpdater == null && NetworkManager.Instance != null)
                globalTargetUpdater = NetworkManager.Instance.StartCoroutine(GlobalTargetUpdater());

            return netE;
        }

        public static NetworkEnemy Find(string id)
        {
            allEnemies.TryGetValue(id, out var result);
            return result;
        }

        private void Start()
        {
            if (Enemy == null)
            {
                return;
            }
            if(Owner == 0)
            {
                Owner = NetworkManager.Instance.CurrentLobby.Owner.Id.Value;
            }
            DestroyOnCheckpointRestart destroyComp = Enemy.GetComponent<DestroyOnCheckpointRestart>();
            if (destroyComp != null)
                Destroy(destroyComp);

            lastPos = Enemy.transform.position;
            lastRot = Enemy.transform.rotation;
            targetPos = Enemy.transform.position;
            targetRot = Enemy.transform.rotation;
            // ensure it exists for everyone
            if(!SceneObjectCache.Contains(gameObject))
            {
                SceneObjectCache.Add(gameObject);
            }
            NetworkManager.Instance.BroadcastPacket(new NetPacket
            {
                type = "ownership",
                name = ID,
                parameters = new string[] { Owner.ToString() }
            });
            if(Enemy.isBoss && NetworkManager.InLobby && NetworkManager.Instance.CurrentLobby.MemberCount > 1)
            {
                float mult = 1f + (Mathf.Max(0, NetworkManager.Instance.CurrentLobby.MemberCount - 1) * 1.5f);
                Enemy.health *= mult;
            }
        }
        private void OnEnable()
        {
            SetHealth();
        }

        private void OnDestroy()
        {
            allEnemies.Remove(ID);
            SceneObjectCache.Remove(gameObject);
        }

        private void Update()
        {
            if (Enemy == null || !IsAlive) return;

            Enemy.ignorePlayer = true;
            if(Enemy.dead && IsAlive)
            {
                BroadcastDeath();
                IsAlive = false;
            }
            if (SteamClient.SteamId.Value == Owner)
            {
                TryBroadcastState();
            }
            else
            {
                Enemy.transform.position = Vector3.Lerp(Enemy.transform.position, targetPos, Time.unscaledDeltaTime * 10f);
                Enemy.transform.rotation = Quaternion.Slerp(Enemy.transform.rotation, targetRot, Time.unscaledDeltaTime * 10f);
            }
        }
        public void TakeOwnership(ulong newOwner)
        {
            Owner = newOwner;
            NetworkManager.Instance.BroadcastPacket(new NetPacket
            {
                type = "ownership",
                name = ID,
                parameters = new string[] {newOwner.ToString()}
            });
        }
        public void TakeOwnerP2P(ulong newOwner) => Owner = newOwner;

        private static IEnumerator GlobalTargetUpdater()
        {
            while (true)
            {
                foreach (var kvp in allEnemies)
                {
                    NetworkEnemy enemy = kvp.Value;
                    if (enemy != null && enemy.IsAlive)
                        enemy.UpdateTarget();
                }
                yield return targetUpdateDelay;
            }
        }

        private void UpdateTarget()
        {
            if (Enemy == null || !IsAlive) return;
            if (Enemy.attackEnemies || Enemy.prioritizeEnemiesUnlessAttacked) return;
            Enemy.target = GetClosestTarget();
        }

        private EnemyTarget GetClosestTarget()
        {
            Transform[] players = GetAllPlayers();
            Transform closest = null;
            float closestDist = float.MaxValue;

            Vector3 pos = Enemy.transform.position;
            foreach (var player in players)
            {
                float dist = (pos - player.position).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = player;
                }
            }

            return (closest != null) ? new EnemyTarget(closest) : (NetworkManager.ClientAndConnected) ? new EnemyTarget(NetworkManager.players[NetworkManager.Instance.CurrentLobby.Owner.Id.Value.ToString()].transform) : new EnemyTarget(MonoSingleton<NewMovement>.Instance.transform);
        }

        private Transform[] GetAllPlayers()
        {
            List<Transform> players = new List<Transform>();
            foreach (var player in NetworkManager.players)
            {
                if (player.Value != null)
                    players.Add(player.Value.transform);
            }

            NewMovement localPlayer = MonoSingleton<NewMovement>.Instance;
            if (localPlayer != null)
                players.Add(localPlayer.transform);

            return players.ToArray();
        }

        private void TryBroadcastState()
        {
            if (Time.time - lastSyncTime < 0.1f) return;
            Vector3 pos = Enemy.transform.position;
            Quaternion rot = Enemy.transform.rotation;

            if (Vector3.SqrMagnitude(pos - lastPos) < 0.0025f && Quaternion.Angle(rot, lastRot) < 2f)
                return;

            lastSyncTime = Time.time;
            lastPos = pos;
            lastRot = rot;

            NetPacket packet = new NetPacket
            {
                type = "enemystate",
                name = ID,
                parameters = new string[]
                {
                    pos.x.ToString("F3"), pos.y.ToString("F3"), pos.z.ToString("F3"),
                    rot.x.ToString("F3"), rot.y.ToString("F3"), rot.z.ToString("F3"), rot.w.ToString("F3"),
                    Enemy.health.ToString("F3")
                }
            };

            NetworkManager.Instance.BroadcastPacket(packet);
        }

        public void ApplyState(string[] parameters)
        {
            if (Enemy == null) return;

            Vector3 pos = new Vector3(
                float.Parse(parameters[0]),
                float.Parse(parameters[1]),
                float.Parse(parameters[2])
            );

            Quaternion rot = new Quaternion(
                float.Parse(parameters[3]),
                float.Parse(parameters[4]),
                float.Parse(parameters[5]),
                float.Parse(parameters[6])
            );

            targetPos = pos;
            targetRot = rot;
            HP = float.Parse(parameters[7]);
        }
        public void SetHealth()
        {
            if (Enemy == null || !IsAlive) return;
            Machine mach = Enemy.machine;
            Zombie zom = Enemy.zombie;
            SpiderBody spi = Enemy.spider;
            Statue stat = Enemy.statue;
            Drone drone = Enemy.drone;
            if (mach != null)
            {
                mach.health = HP;
            }
            if (zom != null)
            {
                zom.health = HP;
            }
            if (spi != null)
            {
                spi.health = HP;
            }
            if (stat != null)
            {
                stat.health = HP;
            }
            if (drone != null)
            {
                drone.health = HP;
            }
            Enemy.health = HP;
        }

        public void BroadcastDamage(float damage, string hitter, bool weakpoint, Vector3 point)
        {
            if (!IsAlive) return;

            NetPacket packet = new NetPacket()
            {
                type = "enemydmg",
                name = ID,
                parameters = new string[]
                {
                    damage.ToString("F1"),
                    hitter,
                    weakpoint.ToString(),
                    point.x.ToString(), point.y.ToString(), point.z.ToString()
                }
            };

            NetworkManager.Instance.BroadcastPacket(packet);
        }
        public void BroadcastDeath()
        {
            if (!IsAlive) return;

            NetPacket packet = new NetPacket()
            {
                type = "deathenemy",
                name = ID
            };
            NetworkManager.Instance.BroadcastPacket(packet);
        }

        public void ApplyDamage(string[] parameters)
        {
            if (Enemy == null || !IsAlive) return;

            if (float.TryParse(parameters[0], out float damage))
            {
                string hitter = parameters[1];
                Enemy.hitter = hitter;
                bool weakpoint = bool.Parse(parameters[2]);
                Vector3 part = new Vector3(float.Parse(parameters[3]), float.Parse(parameters[4]), float.Parse(parameters[5]));
                EnemyIdentifierIdentifier[] eidIds = Enemy.GetComponents<EnemyIdentifierIdentifier>();
                Enemy.DeliverDamage((weakpoint) ? Enemy.weakPoint : Enemy.gameObject, Vector3.zero, part, damage, false);
            }
        }

        public void HandleDeath()
        {
            if (Enemy == null || !IsAlive) return;

            IsAlive = false;
            Enemy.SimpleDamage(int.MaxValue);
        }
    }
}
