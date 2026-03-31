// oh boy can't wait to change something because of fraud :D

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
using Polarite.Networking;
using ULTRAKILL.Enemy;
using static ULTRAKILL.Enemy.VisionQuery;
using ULTRAKILL.Portal;
using System.Threading;

namespace Polarite.Multiplayer
{
    public class NetworkEnemy : NetworkObject
    {
        public EnemyIdentifier Enemy;
        public Enemy EE => Enemy.GetComponent<Enemy>();
        public bool IsAlive = true;
        public bool IgnoreSpawnSync = false;
        public bool HelpedWithKill;
        public List<ulong> Helpers = new List<ulong>();

        private static readonly Dictionary<string, NetworkEnemy> allEnemies = new Dictionary<string, NetworkEnemy>();
        private static Coroutine globalTargetUpdater;

        private Vector3 lastPos;
        private Quaternion lastRot;

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

        public override void Start()
        {
            if (Enemy == null) return;
            if (Owner == 0) Owner = NetworkManager.Instance.CurrentLobby.Owner.Id.Value;
            simpleId = Enemy.enemyType.ToString();
            DestroyOnCheckpointRestart destroyComp = Enemy.GetComponent<DestroyOnCheckpointRestart>();
            if (destroyComp != null) Destroy(destroyComp);

            lastPos = Enemy.transform.position;
            lastRot = Enemy.transform.rotation;
            targetPos = Enemy.transform.position;
            targetRot = Enemy.transform.rotation;

            string path = SceneObjectCache.GetScenePath(gameObject);
            if (!SceneObjectCache.Contains(path))
            {
                SceneObjectCache.Add(path, gameObject);
            }

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
            if (Net.IsOwner(this))
            {
                SyncSpawn();
            }
            UpdateTarget();
            base.Start();
        }

        public override void OnDestroy()
        {
            allEnemies.Remove(ID);
            SceneObjectCache.Remove(gameObject);
            base.OnDestroy();
        }

        public override void Update()
        {
            if(!IsAlive && !Enemy.dead)
            {
                HandleDeath();
                return;
            }

            if (Enemy == null || !IsAlive) return;

            if (!allEnemies.ContainsKey(ID)) allEnemies.Add(ID, this);
            if (SceneHelper.CurrentScene == "Level 0-2" && Enemy.enemyType == EnemyType.Swordsmachine)
            {
                return;
            }
            if (BlindEnemies.Blind)
            {
                Enemy.target = null;
                return;
            }
            Enemy.ignorePlayer = true;

            if (Enemy.dead && IsAlive)
            {
                BroadcastDeath();
                IsAlive = false;
            }
            base.Update();
        }

        public void SyncSpawn()
        {
            PacketWriter w = new PacketWriter();
            w.WriteULong(owner);
            w.WriteString(name);
            w.WriteString(ID);
            w.WriteString(simpleId);
            w.WriteVector3(Enemy.transform.position);
            w.WriteQuaternion(Enemy.transform.rotation);
            w.WriteBool(Enemy.healthBuff);
            w.WriteBool(Enemy.speedBuff);
            w.WriteBool(Enemy.damageBuff);
            w.WriteBool(Enemy.puppet);
            w.WriteFloat(Enemy.healthBuffModifier);
            w.WriteFloat(Enemy.speedBuffModifier);
            w.WriteFloat(Enemy.damageBuffModifier);
            w.WriteBool(Enemy.isBoss);
            NetworkManager.Instance.BroadcastPacket(PacketType.EnemySpawn, w.GetBytes());
        }

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
            if (SceneHelper.CurrentScene == "Level 0-2" && Enemy.enemyType == EnemyType.Swordsmachine) return;
            EnemyTarget tar = GetClosestTarget();
            Enemy.target = tar;
            TargetTrackerStuff();
        }

        private void TargetTrackerStuff()
        {
            if (Enemy == null || !IsAlive) return;
            ITarget target = null;
            EnemyTarget target1 = GetClosestTarget();
            CancellationToken tok = new CancellationToken();
            if (target1.targetTransform.GetComponent<NewMovement>() != null)
            {
                target = target1.targetTransform.GetComponent<NewMovement>();
                tok = target1.targetTransform.GetComponent<NewMovement>().destroyCancellationToken;
            }
            else if (target1.targetTransform.GetComponent<NetworkPlayer>() != null)
            {
                target = target1.targetTransform.GetComponent<NetworkPlayer>();
                tok = target1.targetTransform.GetComponent<NetworkPlayer>().destroyCancellationToken;
            }
            TargetTracker tracker = PortalManagerV2.Instance.TargetTracker;
            if(tracker != null)
            {
                if(!tracker.targets.Contains(target) && !tracker.newTargets.Contains(target))
                {
                    tracker.RegisterTarget(target, tok);
                }
            }
        }
        public static TargetHandle CreateHandleFrom(Vector3 pos)
        {
            TargetHandle handle = new TargetHandle(GetITargetFrom(pos));
            return handle;
        }

        public EnemyTarget GetClosestTarget()
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
            return (closest != null) ? CreateTarget(closest) : (NetworkManager.ClientAndConnected) ? CreateTarget(NetworkManager.players[NetworkManager.Instance.CurrentLobby.Owner.Id.Value].transform) : CreateTarget(MonoSingleton<NewMovement>.Instance.transform);
        }
        private static EnemyTarget CreateTarget(Transform t)
        {
            EnemyTarget target = new EnemyTarget(t);
            target.isPlayer = false;
            target.enemyIdentifier = null;
            return target;
        }

        public static Transform[] GetAllPlayers()
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
        public static EnemyTarget GetClosestTargetFrom(Vector3 from)
        {
            Transform[] players = GetAllPlayers();
            Transform closest = null;
            float closestDist = float.MaxValue;

            Vector3 pos = from;
            foreach (var player in players)
            {
                float dist = (pos - player.position).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = player;
                }
            }
            return (closest != null) ? CreateTarget(closest) : (NetworkManager.ClientAndConnected) ? CreateTarget(NetworkManager.players[NetworkManager.Instance.CurrentLobby.Owner.Id.Value].transform) : CreateTarget(MonoSingleton<NewMovement>.Instance.transform);
        }
        public static ITarget GetITargetFrom(Vector3 pos)
        {
            ITarget target = null;
            EnemyTarget target1 = GetClosestTargetFrom(pos);
            if (target1.targetTransform.GetComponent<NewMovement>() != null)
            {
                target = target1.targetTransform.GetComponent<NewMovement>();
            }
            else if (target1.targetTransform.GetComponent<NetworkPlayer>() != null)
            {
                target = target1.targetTransform.GetComponent<NetworkPlayer>();
            }
            return target;
        }

        public void SetHealth(float hp)
        {
            if (Enemy == null || !IsAlive) return;
            Enemy.GetComponent<Enemy>().health = hp;
        }

        public void BroadcastDamage(float damage, string hitter, bool weakpoint, Vector3 point)
        {
            if (!IsAlive) return;

            PacketWriter w = new PacketWriter();
            w.WriteString(ID);
            w.WriteVector3(Enemy.transform.position);
            w.WriteFloat(damage);
            w.WriteString(hitter);
            w.WriteBool(weakpoint);
            w.WriteVector3(point);

            Send(w, PacketType.EnemyDmg);
            HelpedWithKill = true;
        }

        public void BroadcastDeath()
        {
            if (!IsAlive) return;

            PacketWriter w = new PacketWriter();
            w.WriteString(ID);
            w.WriteVector3(Enemy.transform.position);
            NetworkManager.Instance.BroadcastPacket(PacketType.DeathEnemy, w.GetBytes());
            if (Helpers.Count > 0)
            {
                if (HelpedWithKill)
                {
                    StyleHUD.Instance.AddPoints(Helpers.Count + 1, $"<color=#91FFFF>TEAMKILL</color> x{Helpers.Count + 1}");
                }
                else
                {
                    StyleHUD.Instance.AddPoints(Helpers.Count, $"<color=#FF3030>TEAMKILL</color> x{Helpers.Count}");
                }
            }
        }

        public void ApplyDamage(float damage, string hitter, bool weakpoint, Vector3 point, ulong sender)
        {
            if (Enemy == null || Enemy.dead) return;

            Enemy.hitter = hitter;
            Enemy.DeliverDamage((weakpoint) ? Enemy.weakPoint : Enemy.gameObject, Vector3.zero, point, damage, false, sourceWeapon: NetworkPlayer.Find(sender).gameObject);
            if(!Helpers.Contains(sender))
            {
                Helpers.Add(sender);
            }
        }

        public void HandleDeath()
        {
            if (Enemy == null || Enemy.dead) return;

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
            if(Helpers.Count > 0)
            {
                if (HelpedWithKill)
                {
                    StyleHUD.Instance.AddPoints(Helpers.Count + 1, $"<color=#91FFFF>TEAMKILL</color> x{Helpers.Count + 1}");
                }
                else
                {
                    StyleHUD.Instance.AddPoints(Helpers.Count, $"<color=#FF3030>TEAMKILL</color> x{Helpers.Count}");
                }
            }
        }
        // handle the responses
        public override void Respond(BinaryPacketReader reader, PacketType packet, ulong sender)
        {
            if(packet == PacketType.EnemyDmg)
            {
                float damage = reader.ReadFloat();
                string hitter = reader.ReadString();
                bool weakpoint = reader.ReadBool();
                Vector3 poi = reader.ReadVector3();
                ApplyDamage(damage, hitter, weakpoint, poi, sender);
            }
            if(packet == PacketType.DeathEnemy)
            {
                HandleDeath();
            }
            base.Respond(reader, packet, sender);
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
