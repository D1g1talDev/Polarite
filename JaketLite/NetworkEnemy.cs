using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Polarite.Multiplayer;
using Sandbox;
using Random = UnityEngine.Random;
using Steamworks;
using ULTRAKILL.Cheats;
using System.Linq;

namespace Polarite.Multiplayer
{
    public class NetworkEnemy : MonoBehaviour
    {
        public string ID;
        public EnemyIdentifier Enemy;
        public bool IsAlive = true;
        public ulong Owner = 0;
        public bool IgnoreSpawnSync = false;

        private static readonly Dictionary<string, NetworkEnemy> allEnemies = new Dictionary<string, NetworkEnemy>();
        private static Coroutine globalTargetUpdater;

        private float lastSyncTime;
        private Vector3 lastPos, currentP;
        private Quaternion lastRot, currentR;

        private float lerpT = 0f;
        private const float lerpDuration = 0.1f;

        public Vector3 targetPos;
        public Quaternion targetRot;

        private static readonly WaitForSeconds targetUpdateDelay = new WaitForSeconds(Random.Range(1f, 3f));

        public static NetworkEnemy Create(string id, EnemyIdentifier eid, ulong owner)
        {
            if (eid.GetComponent<NetworkPlayer>() != null)
                return null;

            if (eid.TryGetComponent<NetworkEnemy>(out var net))
            {
                net.ID = id;
                return net;
            }

            var netE = eid.gameObject.AddComponent<NetworkEnemy>();
            netE.ID = id;
            netE.Enemy = eid;
            netE.IsAlive = true;
            netE.Owner = owner;
            allEnemies[id] = netE;
            SceneObjectCache.CoroutineRunner.ForceInvokeFrame();

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
            if (Enemy == null) return;

            DestroyOnCheckpointRestart destroyComp = Enemy.GetComponent<DestroyOnCheckpointRestart>();
            if (destroyComp != null) Destroy(destroyComp);

            lastPos = Enemy.transform.position;
            lastRot = Enemy.transform.rotation;
            currentP = lastPos;
            currentR = lastRot;
            targetPos = Enemy.transform.position;
            targetRot = Enemy.transform.rotation;

            string uniquePath = $"{ID}:{Enemy.enemyType}";
            if (!SceneObjectCache.Contains(uniquePath))
            {
                SceneObjectCache.Add(uniquePath, gameObject);
            }

            PacketWriter w = new PacketWriter();
            w.WriteString(ID);
            w.WriteULong(Owner);
            NetworkManager.Instance.BroadcastPacket(PacketType.Ownership, w.GetBytes());

            if (Enemy.isBoss && NetworkManager.InLobby && NetworkManager.Instance.CurrentLobby.MemberCount > 1 && NetworkManager.Instance.CurrentLobby.GetData("bh") == "1")
            {
                float playerScale = 1f + (Mathf.Max(0, NetworkManager.Instance.CurrentLobby.MemberCount - 1) * 1.5f);
                float configScale = float.Parse(NetworkManager.Instance.CurrentLobby.GetData("bhm"));

                float finalMult = playerScale * configScale;
                SetHealth(Enemy.health * finalMult);
                BossHealthBar bHB = GetComponent<BossHealthBar>();
                if (bHB != null)
                {
                    foreach (var layer in bHB.healthLayers)
                    {
                        layer.health *= finalMult;
                    }
                    bHB.enabled = false;
                    bHB.enabled = true;
                }
            }
            if (NetworkManager.Id == Owner)
            {
                SyncSpawn();
            }
        }

        private void OnDestroy()
        {
            allEnemies.Remove(ID);
            SceneObjectCache.Remove(gameObject);
        }

        private void Update()
        {
            if (Enemy == null || !IsAlive) return;

            if (!allEnemies.ContainsKey(ID)) allEnemies.Add(ID, this);
            if (SceneHelper.CurrentScene == "Level 0-2" && Enemy.enemyType == EnemyType.Swordsmachine)
            {
                if (globalTargetUpdater != null) StopCoroutine(globalTargetUpdater);
                return;
            }
            if (BlindEnemies.Blind)
            {
                Enemy.target = null;
                return;
            }
            if (Owner == 0) TakeOwnership(NetworkManager.Instance.CurrentLobby.Owner.Id.Value);
            Enemy.ignorePlayer = true;

            if (Enemy.dead && IsAlive)
            {
                BroadcastDeath();
                IsAlive = false;
            }

            if (NetworkManager.Id == Owner)
            {
                TryBroadcastState();
            }
            else
            {
                lerpT += Time.unscaledDeltaTime / lerpDuration;
                lerpT = Mathf.Clamp01(lerpT);

                Enemy.transform.position = Vector3.Lerp(currentP, targetPos, lerpT);
                Enemy.transform.rotation = Quaternion.Slerp(currentR, targetRot, lerpT);
            }
        }

        public void SyncSpawn()
        {
            PacketWriter w = new PacketWriter();
            w.WriteString(name);
            w.WriteString(ID);
            w.WriteString(Enum.GetName(typeof(EnemyType), Enemy.enemyType));
            w.WriteVector3(Enemy.transform.position);
            w.WriteQuaternion(Enemy.transform.rotation);
            w.WriteBool(Enemy.healthBuff);
            w.WriteBool(Enemy.speedBuff);
            w.WriteBool(Enemy.damageBuff);
            w.WriteFloat(Enemy.healthBuffModifier);
            w.WriteFloat(Enemy.speedBuffModifier);
            w.WriteFloat(Enemy.damageBuffModifier);
            w.WriteBool(Enemy.isBoss);
            NetworkManager.Instance.BroadcastPacket(PacketType.EnemySpawn, w.GetBytes());
        }

        public void TakeOwnership(ulong newOwner)
        {
            Owner = newOwner;
            PacketWriter w = new PacketWriter();
            w.WriteString(ID);
            w.WriteULong(newOwner);
            NetworkManager.Instance.BroadcastPacket(PacketType.Ownership, w.GetBytes());
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
                if (player.Value != null && player.Value != NetworkPlayer.LocalPlayer && !player.Value.isGhost)
                    players.Add(player.Value.transform);

            NewMovement localPlayer = MonoSingleton<NewMovement>.Instance;
            if (localPlayer != null && !NetworkPlayer.selfIsGhost)
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

            PacketWriter w = new PacketWriter();
            w.WriteString(ID);
            w.WriteVector3(pos);
            w.WriteQuaternion(rot);
            NetworkManager.Instance.BroadcastPacket(PacketType.EnemyState, w.GetBytes());
        }

        public void ApplyState(Vector3 pos, Quaternion rot)
        {
            if (Enemy == null) return;

            currentP = Enemy.transform.position;
            currentR = Enemy.transform.rotation;

            targetPos = pos;
            targetRot = rot;

            lerpT = 0f;
        }

        public void SetHealth(float hp)
        {
            if (Enemy == null || !IsAlive) return;
            Machine mach = Enemy.machine;
            Zombie zom = Enemy.zombie;
            SpiderBody spi = Enemy.spider;
            Statue stat = Enemy.statue;
            Drone drone = Enemy.drone;

            if (mach != null) { mach.health = hp; }
            if (zom != null) { zom.health = hp; }
            if (spi != null) { spi.health = hp; }
            if (stat != null) { stat.health = hp; }
            if (drone != null) { drone.health = hp; }

            Enemy.health = hp;
        }

        public void BroadcastDamage(float damage, string hitter, bool weakpoint, Vector3 point)
        {
            if (!IsAlive) return;

            PacketWriter w = new PacketWriter();
            w.WriteString(ID);
            w.WriteFloat(damage);
            w.WriteString(hitter);
            w.WriteBool(weakpoint);
            w.WriteVector3(point);

            NetworkManager.Instance.BroadcastPacket(PacketType.EnemyDmg, w.GetBytes());
        }

        public void BroadcastDeath()
        {
            if (!IsAlive) return;

            PacketWriter w = new PacketWriter();
            w.WriteString(ID);
            NetworkManager.Instance.BroadcastPacket(PacketType.DeathEnemy, w.GetBytes());
        }

        public void ApplyDamage(float damage, string hitter, bool weakpoint, Vector3 point, ulong sender)
        {
            if (Enemy == null || !IsAlive) return;

            Enemy.hitter = hitter;
            Enemy.DeliverDamage((weakpoint) ? Enemy.weakPoint : Enemy.gameObject, Vector3.zero, point, damage, false, sourceWeapon: NetworkPlayer.Find(sender).gameObject);
        }

        public void HandleDeath()
        {
            if (Enemy == null || !IsAlive) return;

            IsAlive = false;
            if(!Enemy.dead)
            {
                if (Enemy.idol != null)
                {
                    Enemy.idol.Death();
                }
                else
                {
                    ApplyDamage(9999, "polr.someonekilled", false, Enemy.transform.position, Owner);
                }
            }
        }
        public static void Flush()
        {
            foreach(var e in allEnemies.Values)
            {
                e.HandleDeath();
            }
            allEnemies.Clear();
        }
    }
}
