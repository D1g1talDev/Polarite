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
using Polarite.Debugging;
using UnityEngine.AI;

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

        public static readonly Dictionary<string, NetworkEnemy> allEnemies = new Dictionary<string, NetworkEnemy>();
        private static Coroutine globalTargetUpdater;

        private Vector3 lastPos;
        private Quaternion lastRot;

        public Vector3 targetPos;
        public Quaternion targetRot;

        private static readonly WaitForSeconds targetUpdateDelay = new WaitForSeconds(Random.Range(1f, 3f));

        public static NetworkEnemy Create(EnemyIdentifier eid, ulong owner, string id = "")
        {
            if (eid.GetComponent<NetworkPlayer>() != null)
                return null;

            var netE = eid.gameObject.GetOrAddComponent<NetworkEnemy>();
            netE.Enemy = eid;
            netE.IsAlive = true;
            netE.Owner = owner;
            if(!string.IsNullOrEmpty(id))
            {
                netE.ID = id;
            }
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

            base.Start();

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
                    BossBarManager.Instance.UpdateBossBar(bHB);
                }
            }
            if (Owns)
            {
                SyncSpawn();
            }
            UpdateTarget();
        }

        public override void OnDestroy()
        {
            allEnemies.Remove(ID);
            SceneObjectCache.Remove(gameObject);
            // small check to prevent softlocks
            if(Enemy.puppet)
            {
                base.OnDestroy();
                return;
            }
            if(Enemy.health > 0)
            {
                base.OnDestroy();
                return;
            }
        }

        public override void Update()
        {
            alive = IsAlive;
            if (Enemy.health <= 0 && IsAlive)
            {
                BroadcastDeath();
                IsAlive = false;
            }
            if (!IsAlive && Enemy.health > 0)
            {
                HandleDeath();
                return;
            }

            if (Enemy == null || !IsAlive) return;

            if (!allEnemies.ContainsKey(ID) && !Enemy.dead) allEnemies.Add(ID, this);
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
            HandlePositions();
        }
        public void HandlePositions()
        {
            if (owner == 0)
                return;
            if (!alive)
                return;

            if (syncTransform && !Net.IsOwner(this))
            {
                Transform head = (TryGetComponent<MaliciousFace>(out var malFace)) ? malFace.headModel : null;
                interpTimer += Time.deltaTime;
                float t = interpTimer / interpolationTime;
                NavMeshAgent nma = EE.nma;

                if (t >= 1f)
                {
                    SetPosAndRot(nma, transform, TargetPosition, TargetRotation);
                    if (head != null)
                    {
                        head.transform.position = TargetPosition;
                    }
                    return;
                }

                if (Vector3.Distance(transform.position, TargetPosition) >= 10f)
                {
                    SetPosAndRot(nma, transform, TargetPosition, TargetRotation);
                    if (head != null)
                    {
                        head.transform.position = TargetPosition;
                    }
                    return;
                }

                Vector3 pos = Vector3.Lerp(LastPosition, TargetPosition, t);
                Quaternion rot = Quaternion.Slerp(LastRotation, TargetRotation, t);

                if ((transform.position - pos).sqrMagnitude > 0.0001f)
                {
                    if (head != null)
                    {
                        head.transform.position = pos;
                        SetPosAndRot(nma, transform, pos, rot);
                    }
                    else
                    {
                        transform.SetPositionAndRotation(pos, rot);
                    }
                }
            }
        }
        public void SetPosAndRot(NavMeshAgent agent, Transform transform, Vector3 pos, Quaternion rot)
        {
            if(agent != null)
            {
                agent.Warp(pos);
            }
            else
            {
                transform.position = pos;
            }
            transform.rotation = rot;
        }

        public void SyncSpawn()
        {
            PacketWriter w = new PacketWriter();
            w.WriteULong(owner);
            w.WriteString(name);
            w.WriteString(id);
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
                if(allEnemies.Count >= 1)
                {
                    Logs.Debug($"Global target updater ticked with {allEnemies.Count} enemies.", name: "NetworkEnemy");
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
            Logs.Info($"Updated target for {Enemy.enemyType} to {(tar.targetTransform != null ? tar.targetTransform.name : "null")}", this);
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

            foreach (var player in players)
            {
                float dist = Vector3.Distance(player.transform.position, Enemy.transform.position);
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

            foreach (var player in players)
            {
                float dist = Vector3.Distance(player.transform.position, from);
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
            if (Helpers.Count > 1 && !Enemy.puppet)
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
            StartCoroutine(CleanupEnemy());
        }

        public void ApplyDamage(float damage, string hitter, bool weakpoint, Vector3 point, ulong sender)
        {
            if (Enemy == null) return;

            Enemy.hitter = hitter;
            Enemy.DeliverDamage((weakpoint) ? Enemy.weakPoint : Enemy.gameObject, Vector3.zero, point, damage, false, sourceWeapon: NetworkPlayer.Find(sender).gameObject);
            if(!Helpers.Contains(sender))
            {
                Helpers.Add(sender);
            }
        }

        public void HandleDeath()
        {
            if (Enemy == null) return;

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
            StartCoroutine(CleanupEnemy());
        }
        public IEnumerator CleanupEnemy()
        {
            isCleaningUp = true;
            // small delay to hopefully ignore recovery
            yield return new WaitForSecondsRealtime(0.5f);
            allEnemies.Remove(ID);
            if(Net.List.Contains(this))
            {
                Net.List.Remove(this, false);
            }
            SceneObjectCache.Remove(gameObject);
            isCleaningUp = false;
        }
        public override void SendState(PacketWriter writer, PacketType type)
        {
            if(IsAlive)
            {
                // write extra enemy states
                writer.WriteBool(Enemy.healthBuff);
                writer.WriteBool(Enemy.speedBuff);
                writer.WriteBool(Enemy.damageBuff);
                // write modifiers for... reasons
                writer.WriteFloat(Enemy.healthBuffModifier);
                writer.WriteFloat(Enemy.speedBuffModifier);
                writer.WriteFloat(Enemy.damageBuffModifier);
                // write sanded and puppet state
                writer.WriteBool(Enemy.sandified);
                writer.WriteBool(Enemy.puppet);
                // write idoled and attack enemies state
                writer.WriteBool(Enemy.blessed);
                writer.WriteBool(Enemy.attackEnemies);
                base.SendState(writer, PacketType.EnemyState);
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
                Destroy(e.gameObject);
            }
            allEnemies.Clear();
            Logs.Info("Flushed all enemies (down the toilet?)", name: "NetworkEnemy");
        }
    }
}
