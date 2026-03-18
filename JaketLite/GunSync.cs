using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using UnityEngine;
using UnityEngine.AddressableAssets;

using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using System.Runtime.Remoting.Channels;

public enum BulletType
{
    Revolver,
    RevolverSuper,
    Coin,
    RevolverSharp,
    Slab,
    SlabSuper,
    SlabSharp,
    ShotgunMain,
    ShotgunCore,
    Nail,
    NailSad,
    NailHeated,
    Saw,
    SawSad,
    SawHeated,
    RailBlue,
    RailGreen,
    RailRed,
    Rocket,
    Cannonball,
    Oil,
    Magnet,
    GrenadeBeam,
    JackCore,
    JackhammerPump,
    JackhammerLight,
    JackhammerMedium,
    JackhammerHeavy
}

namespace Polarite
{
    public static class GunSync
    {
        public static Dictionary<BulletType, string> Bullets = new Dictionary<BulletType, string>
        {
            { BulletType.Revolver, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam.prefab" },
            { BulletType.RevolverSuper, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam Super.prefab" },
            { BulletType.Coin, "Assets/Prefabs/Attacks and Projectiles/Coin.prefab" },
            { BulletType.RevolverSharp, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam Sharp.prefab"},
            { BulletType.Slab, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam Alternative.prefab"},
            { BulletType.SlabSuper, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam Super Alternative.prefab"},
            { BulletType.SlabSharp, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Revolver Beam Sharp Alternative.prefab"},
            { BulletType.ShotgunMain, "Assets/Prefabs/Attacks and Projectiles/Shotgun Projectile.prefab" },
            { BulletType.ShotgunCore, "Assets/Prefabs/Attacks and Projectiles/Grenade.prefab" },
            { BulletType.Nail, "Assets/Prefabs/Attacks and Projectiles/Nails/NailFodder.prefab" },
            { BulletType.NailSad, "Assets/Prefabs/Attacks and Projectiles/Nails/Nail.prefab" },
            { BulletType.NailHeated, "Assets/Prefabs/Attacks and Projectiles/Nails/NailHeated.prefab" },
            { BulletType.Saw, "Assets/Prefabs/Attacks and Projectiles/NailAltFodder.prefab" },
            { BulletType.SawSad, "Assets/Prefabs/Attacks and Projectiles/NailAlt.prefab" },
            { BulletType.SawHeated, "Assets/Prefabs/Attacks and Projectiles/NailAltHeated.prefab" },
            { BulletType.Magnet, "Assets/Prefabs/Attacks and Projectiles/Harpoon.prefab" },
            { BulletType.RailBlue, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Railcannon Beam.prefab" },
            { BulletType.RailGreen, "Assets/Prefabs/Attacks and Projectiles/Harpoon Malicious.prefab" },
            { BulletType.RailRed, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Railcannon Beam Malicious.prefab" },
            { BulletType.Rocket, "Assets/Prefabs/Attacks and Projectiles/Rocket.prefab" },
            { BulletType.Cannonball, "Assets/Prefabs/Attacks and Projectiles/Cannonball.prefab" },
            { BulletType.Oil, "Assets/Prefabs/Attacks and Projectiles/GasolineProjectile.prefab" },
            { BulletType.GrenadeBeam, "Assets/Prefabs/Attacks and Projectiles/Hitscan Beams/Grenade Beam.prefab" },
            { BulletType.JackCore, "Assets/Prefabs/Attacks and Projectiles/Grenade.prefab" },
            { BulletType.JackhammerPump, "Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Hammer Weak.prefab" },
            { BulletType.JackhammerLight, "Assets/Particles/HammerHitLight.prefab" },
            { BulletType.JackhammerMedium, "Assets/Particles/HammerHitMedium.prefab" },
            { BulletType.JackhammerHeavy, "Assets/Particles/HammerHitHeavy.prefab" }
        };
        public static Vector3 Pos => MonoSingleton<CameraController>.Instance.transform.position + MonoSingleton<CameraController>.Instance.transform.forward;
        public static Quaternion Rot => MonoSingleton<CameraController>.Instance.transform.rotation;
        public static Vector3 Dir => MonoSingleton<CameraController>.Instance.transform.forward;

        public static void Shockwave(Vector3 pos)
        {
            GameObject shockwave = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/PhysicalShockwavePlayer.prefab").WaitForCompletion(), pos, Quaternion.identity);
            PhysicalShockwave ps = shockwave.GetComponent<PhysicalShockwave>();
            ps.enemy = true;
            ps.damage = 2;
            ps.noDamageToEnemy = false;
            ps.hasHurtPlayer = false;
            ps.force = 1000f;
        }
        public static void Blast(Vector3 pos)
        {
            GameObject blast = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Wave.prefab").WaitForCompletion(), pos, Quaternion.identity);
            Explosion exp = blast.GetComponentInChildren<Explosion>();
            exp.damage = 5;
            exp.friendlyFire = false;
            exp.harmless = false;
            exp.hasHitPlayer = false;
            exp.canHit = AffectedSubjects.PlayerOnly;
        }
        public static void ProjBoost(Vector3 pos)
        {
            GameObject blast = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion.prefab").WaitForCompletion(), pos, Quaternion.identity);
            Explosion exp = blast.GetComponentInChildren<Explosion>();
            exp.damage = 10;
            exp.friendlyFire = false;
            exp.harmless = false;
            exp.hasHitPlayer = false;
            exp.canHit = AffectedSubjects.PlayerOnly;
        }
        public static void Sync(BulletType type, Vector3 pos, Quaternion rot, Vector3 dir, Vector3 vel, int pump = 0, float pow = 0f, int var = 0)
        {
            if(!NetworkManager.InLobby)
            {
                return;
            }
            PacketWriter writer = new PacketWriter();
            writer.WriteInt((int)type);
            writer.WriteVector3(pos);
            writer.WriteQuaternion(rot);
            writer.WriteInt(pump);
            writer.WriteVector3(dir);
            writer.WriteVector3(vel);
            writer.WriteFloat(pow);
            writer.WriteInt(var);
            NetworkManager.Instance.BroadcastPacket(PacketType.Bullet, writer.GetBytes());
        }
        public static void ReceiveBullet(BinaryPacketReader reader, ulong sender)
        {
            BulletType type = (BulletType)reader.ReadInt();
            Vector3 pos = reader.ReadVector3();
            Quaternion rot = reader.ReadQuaternion();
            int pump = reader.ReadInt();
            GameObject bulletPrefab;
            Vector3 dir = reader.ReadVector3();
            Vector3 vel = reader.ReadVector3();
            float pow = reader.ReadFloat();
            int var = reader.ReadInt();
            if (Bullets.TryGetValue(type, out string path))
            {
                bulletPrefab = Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();
            }
            else
            {
                ItePlugin.LogDebug($"[GUNSYNC] Unknown bullet type: {type}");
                return;
            }
            int shotgunPells = 12;
            bool explodeInstead = false;
            if (var == 1)
            {
                switch (pump)
                {
                    case 0:
                        shotgunPells = 10;
                        break;
                    case 1:
                        shotgunPells = 16;
                        break;
                    case 2:
                        shotgunPells = 24;
                        break;
                    case 3:
                        shotgunPells = 0;
                        explodeInstead = true;
                        break;
                }
            }
            switch(type)
            {
                case BulletType.Coin:
                    {
                        GameObject coin = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Coin c = coin.GetComponent<Coin>();
                        c.GetComponent<Rigidbody>().AddForce(dir * 20f + Vector3.up * 15f + vel, ForceMode.VelocityChange);
                        c.power = 0;
                        return;
                    }
                case BulletType.ShotgunMain:
                    {
                        if(explodeInstead)
                        {
                            GameObject exp = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Big.prefab").WaitForCompletion(), pos, Quaternion.identity);
                            Explosion explosion = exp.GetComponentInChildren<Explosion>();
                            explosion.enemy = true;
                            explosion.friendlyFire = false;
                            explosion.damage = 30;
                            explosion.canHit = AffectedSubjects.PlayerOnly;
                            return;
                        }
                        for (int i = 0; i < shotgunPells; i++)
                        {
                            GameObject pellet = GameObject.Instantiate(bulletPrefab, pos, rot);
                            Projectile proj = pellet.GetComponent<Projectile>();
                            proj.damage = 0;
                            proj.friendly = true;
                            if (var == 1)
                            {
                                switch (pump)
                                {
                                    case 0:
                                        pellet.transform.Rotate(Random.Range((0f - 10) / 1.5f, 10 / 1.5f), Random.Range((0f - 10) / 1.5f, 10 / 1.5f), Random.Range((0f - 10) / 1.5f, 10 / 1.5f));
                                        break;
                                    case 1:
                                        pellet.transform.Rotate(Random.Range(0f - 10, 10), Random.Range(0f - 10, 10), Random.Range(0f - 10, 10));
                                        break;
                                    case 2:
                                        pellet.transform.Rotate(Random.Range((0f - 10) * 2f, 10 * 2f), Random.Range((0f - 10) * 2f, 10 * 2f), Random.Range((0f - 10) * 2f, 10 * 2f));
                                        break;
                                }
                            }
                            else
                            {
                                pellet.transform.Rotate(Random.Range(0f - 10, 10), Random.Range(0f - 10, 10), Random.Range(0f - 10, 10));
                            }
                            CleanProj(pellet, sender);
                        }
                        return;
                    }
                case BulletType.ShotgunCore:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            if (vel != Vector3.zero)
                            {
                                rb.AddForce(vel, ForceMode.VelocityChange);
                            }
                            else
                            {
                                rb.AddForce(dir * (pow + 10f), ForceMode.VelocityChange);
                            }
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.JackCore:
                    {
                        GameObject obj = GameObject.Instantiate(bulletPrefab, pos, Random.rotation);
                        if (obj.TryGetComponent<Rigidbody>(out var component))
                        {
                            component.AddForce(dir * 3f + Vector3.up * 7.5f + vel, ForceMode.VelocityChange);
                        }
                        CleanProj(obj, sender);
                        return;
                    }
                case BulletType.NailSad:
                case BulletType.SawSad:
                case BulletType.Saw:
                case BulletType.Nail:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            projectile.transform.Rotate(Random.Range((0f - pow) / 3f, pow / 3f), Random.Range((0f - pow) / 3f, pow / 3f), Random.Range((0f - pow) / 3f, pow / 3f));
                            if (projectile.TryGetComponent<Rigidbody>(out var component2))
                            {
                                component2.velocity = projectile.transform.forward * 200f;
                            }
                        }
                        if (projectile.TryGetComponent<Nail>(out var n))
                        {
                            if (n.sawblade)
                            {
                                n.ForceCheckSawbladeRicochet();
                            }
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.SawHeated:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.velocity = rb.transform.forward * 200f;
                        }
                        if (projectile.TryGetComponent<Nail>(out var component2))
                        {
                            component2.multiHitAmount = Mathf.RoundToInt(pow * 3f);
                            component2.ForceCheckSawbladeRicochet();
                            component2.damage = 0;
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.NailHeated:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        projectile.transform.forward *= -1f;
                        if (rb != null)
                        {
                            projectile.transform.Rotate(Random.Range((0f - pow) / 3f, pow / 3f), Random.Range((0f - pow) / 3f, pow / 3f), Random.Range((0f - pow) / 3f, pow / 3f));
                            rb.AddForce(projectile.transform.forward * -100f, ForceMode.VelocityChange);
                        }
                        if(projectile.TryGetComponent<Nail>(out var n))
                        {
                            if(n.sawblade)
                            {
                                n.ForceCheckSawbladeRicochet();
                            }
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.Magnet:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.AddForce(rb.transform.forward * 100f, ForceMode.VelocityChange);
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.RailGreen:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.AddForce(rb.transform.forward * 250f, ForceMode.VelocityChange);
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.Cannonball:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.velocity = rb.transform.forward * Mathf.Max(15f, pow * 150f);
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.Oil:
                    {
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.transform.Rotate(new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)));
                            rb.velocity = rb.transform.forward * 150f;
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.JackhammerPump:
                    {
                        if(pump == 3)
                        {
                            GameObject boom = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Super.prefab").WaitForCompletion(), pos, Quaternion.identity);
                            CleanProj(boom, sender);
                            return;
                        }
                        GameObject hammer = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Explosion[] componentsInChildren = hammer.GetComponentsInChildren<Explosion>();
                        foreach (Explosion explosion in componentsInChildren)
                        {
                            explosion.hitterWeapon = "hammer";
                            if (pump == 2)
                            {
                                explosion.maxSize *= 2f;
                            }
                            explosion.canHit = AffectedSubjects.PlayerOnly;
                            explosion.damage -= 10;
                        }
                        return;
                    }
                case BulletType.JackhammerLight:
                    {
                        GameObject light = GameObject.Instantiate(bulletPrefab, pos, rot);
                        return;
                    }
                case BulletType.JackhammerMedium:
                    {
                        GameObject medium = GameObject.Instantiate(bulletPrefab, pos, rot);
                        return;
                    }
                case BulletType.JackhammerHeavy:
                    {
                        GameObject heavy = GameObject.Instantiate(bulletPrefab, pos, rot);
                        return;
                    }
            }
            GameObject bullet = GameObject.Instantiate(bulletPrefab, pos, rot);
            CleanProj(bullet, sender);
        }
        public static void CleanProj(GameObject bullet, ulong sender)
        {
            if (bullet.TryGetComponent<Grenade>(out var gre))
            {
                if (gre.rocket)
                {
                    PlayerRocketManager rm = PlayerRocketManager.Get(sender);
                    PlayerRocket rock = gre.gameObject.AddComponent<PlayerRocket>();
                    rock.owner = sender;
                    if(rm != null)
                    {
                        if(!rm.rockets.Contains(rock))
                        {
                            rm.rockets.Add(rock);
                        }
                    }
                }
                Explosion norm = gre.explosion.GetComponentInChildren<Explosion>();
                Explosion big = gre.superExplosion.GetComponentInChildren<Explosion>();
                Explosion harm = gre.harmlessExplosion.GetComponentInChildren<Explosion>();
                if (norm != null)
                {
                    norm.canHit = AffectedSubjects.PlayerOnly;
                }
                if (big != null)
                {
                    big.canHit = AffectedSubjects.PlayerOnly;
                }
                if (harm != null)
                {
                    harm.canHit = AffectedSubjects.PlayerOnly;
                }
            }
            if (bullet.TryGetComponent<RevolverBeam>(out var rev))
            {
                rev.damage = 0;
                Explosion potExp = rev.hitParticle.GetComponentInChildren<Explosion>();
                if (potExp != null)
                {
                    potExp.enemy = true;
                    potExp.friendlyFire = false;
                    potExp.hasHitPlayer = false;
                    potExp.canHit = AffectedSubjects.PlayerOnly;
                }
            }
            if (bullet.TryGetComponent<Projectile>(out var proje))
            {
                proje.unparryable = true;
                proje.friendly = true;
                proje.damage = 0;
            }
            if(bullet.TryGetComponent<Nail>(out var n))
            {
                n.damage = 0;
            }
            if(bullet.TryGetComponent<Cannonball>(out var c))
            {
                c.damage = 0;
                Explosion potExp = c.interruptionExplosion.GetComponentInChildren<Explosion>();
                if (potExp != null)
                {
                    potExp.enemy = true;
                    potExp.friendlyFire = false;
                    potExp.hasHitPlayer = false;
                    potExp.canHit = AffectedSubjects.PlayerOnly;
                }
            }
        }

        public static void PunchflectionNetwork(Vector3 pos, Vector3 forward, Coin coin)
        {
            bool flag = false;
            bool flag2 = false;
            GameObject gameObject = null;
            float num = float.PositiveInfinity;
            Vector3 position = coin.transform.position;
            GameObject gameObject2 = GameObject.Instantiate(coin.gameObject, coin.transform.position, Quaternion.identity);
            gameObject2.SetActive(value: false);
            Vector3 position2 = coin.transform.position;
            coin.scol.enabled = false;
            GameObject[] array = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject gameObject3 in array)
            {
                Vector3 position3 = gameObject3.transform.position;
                if (gameObject3.TryGetComponent<EnemyIdentifier>(out var component))
                {
                    if ((bool)component.weakPoint)
                    {
                        position3 = component.weakPoint.transform.position;
                    }
                    else if ((bool)component.overrideCenter)
                    {
                        position3 = component.overrideCenter.position;
                    }
                }

                float sqrMagnitude = (position3 - position).sqrMagnitude;
                Debug.Log($"Distance for {gameObject3.name}: {sqrMagnitude}");
                if (!(sqrMagnitude < num))
                {
                    continue;
                }

                component = gameObject3.GetComponent<EnemyIdentifier>();
                if (component != null && !component.dead)
                {
                    Transform transform = ((!(component.weakPoint != null) || !component.weakPoint.activeInHierarchy) ? component.GetComponentInChildren<EnemyIdentifierIdentifier>().transform : component.weakPoint.transform);
                    if (!Physics.Raycast(coin.transform.position, transform.position - coin.transform.position, out var _, Vector3.Distance(coin.transform.position, transform.position) - 0.5f, LayerMaskDefaults.Get(LMD.Environment)))
                    {
                        gameObject = gameObject3;
                        num = sqrMagnitude;
                    }
                    else
                    {
                        component = null;
                    }
                }
                else
                {
                    component = null;
                }
            }

            if (gameObject != null)
            {
                if (coin.eid == null)
                {
                    coin.eid = gameObject.GetComponent<EnemyIdentifier>();
                }

                LineRenderer component2 = coin.SpawnBeam().GetComponent<LineRenderer>();
                AudioSource[] components = component2.GetComponents<AudioSource>();
                if (coin.hitPoint == Vector3.zero)
                {
                    component2.SetPosition(0, coin.transform.position);
                }
                else
                {
                    component2.SetPosition(0, coin.hitPoint);
                }

                _ = Vector3.zero;
                Vector3 vector = ((!(coin.eid.weakPoint != null) || !coin.eid.weakPoint.activeInHierarchy) ? coin.eid.GetComponentInChildren<EnemyIdentifierIdentifier>().transform.position : coin.eid.weakPoint.transform.position);
                if (coin.eid.blessed)
                {
                    flag2 = true;
                }

                component2.SetPosition(1, vector);
                position2 = vector;
                if (!coin.eid.puppet && !coin.eid.blessed)
                {
                    coin.shud.AddPoints(50, "ultrakill.fistfullofdollar", coin.sourceWeapon, coin.eid);
                }

                if (coin.eid.weakPoint != null && coin.eid.weakPoint.activeInHierarchy && coin.eid.weakPoint.GetComponent<EnemyIdentifierIdentifier>() != null)
                {
                    coin.eid.hitter = "coin";
                    if (!coin.eid.hitterWeapons.Contains("coin"))
                    {
                        coin.eid.hitterWeapons.Add("coin");
                    }

                    coin.eid.DeliverDamage(coin.eid.weakPoint, (coin.eid.weakPoint.transform.position - coin.transform.position).normalized * 10000f, vector, coin.power, tryForExplode: false, 1f, coin.sourceWeapon);
                }
                else if (coin.eid.weakPoint != null && coin.eid.weakPoint.activeInHierarchy)
                {
                    Breakable componentInChildren = coin.eid.weakPoint.GetComponentInChildren<Breakable>();
                    if (componentInChildren.precisionOnly)
                    {
                        coin.shud.AddPoints(100, "ultrakill.interruption", coin.sourceWeapon, coin.eid);
                        MonoSingleton<TimeController>.Instance.ParryFlash();
                        if ((bool)componentInChildren.interruptEnemy && !componentInChildren.interruptEnemy.blessed)
                        {
                            componentInChildren.interruptEnemy.Explode(fromExplosion: true);
                        }
                    }

                    componentInChildren.Break();
                }
                else
                {
                    coin.eid.hitter = "coin";
                    coin.eid.DeliverDamage(coin.eid.GetComponentInChildren<EnemyIdentifierIdentifier>().gameObject, (coin.eid.GetComponentInChildren<EnemyIdentifierIdentifier>().transform.position - coin.transform.position).normalized * 10000f, coin.hitPoint, coin.power, tryForExplode: false, 1f, coin.sourceWeapon);
                }

                if (coin.power > 2f)
                {
                    AudioSource[] array2 = components;
                    foreach (AudioSource obj in array2)
                    {
                        obj.pitch = 1f + (coin.power - 2f) / 5f;
                        obj.Play();
                    }

                    coin.eid = null;
                }
            }
            else
            {
                flag = true;
                LineRenderer component3 = coin.SpawnBeam().GetComponent<LineRenderer>();
                if (coin.power > 2f)
                {
                    AudioSource[] array2 = component3.GetComponents<AudioSource>();
                    foreach (AudioSource obj2 in array2)
                    {
                        obj2.pitch = 1f + (coin.power - 2f) / 5f;
                        obj2.Play();
                    }
                }

                if (coin.hitPoint == Vector3.zero)
                {
                    component3.SetPosition(0, coin.transform.position);
                }
                else
                {
                    component3.SetPosition(0, coin.hitPoint);
                }

                if (Physics.Raycast(pos, forward, out var hitInfo2, float.PositiveInfinity, LayerMaskDefaults.Get(LMD.Environment)))
                {
                    component3.SetPosition(1, hitInfo2.point);
                    position2 = hitInfo2.point - forward;
                    if (SceneHelper.IsStaticEnvironment(hitInfo2))
                    {
                        MonoSingleton<SceneHelper>.Instance.CreateEnviroGibs(hitInfo2);
                    }
                }
                else
                {
                    component3.SetPosition(1, pos + forward * 1000f);
                    GameObject.Destroy(gameObject2);
                }
            }

            if ((bool)gameObject2)
            {
                gameObject2.transform.position = position2;
                gameObject2.SetActive(value: true);
                Coin component4 = gameObject2.GetComponent<Coin>();
                if ((bool)component4)
                {
                    component4.shot = false;
                    if (component4.power < 5f || (!flag && !flag2))
                    {
                        component4.power += 1f;
                    }

                    gameObject2.name = "NewCoin+" + (component4.power - 2f);
                }

                Rigidbody component5 = gameObject2.GetComponent<Rigidbody>();
                if ((bool)component5)
                {
                    component5.isKinematic = false;
                    component5.velocity = Vector3.zero;
                    component5.AddForce(Vector3.up * 25f, ForceMode.VelocityChange);
                }
            }

            coin.gameObject.SetActive(value: false);
            new GameObject().AddComponent<CoinCollector>().coin = coin.gameObject;
            coin.CancelInvoke("GetDeleted");
        }
        public static void DelayedPunchflectionNetwork(Vector3 pos, Vector3 forward, Coin coin)
        {
            if (coin.checkingSpeed && (!coin.shot || coin.shotByEnemy))
            {
                if (coin.shotByEnemy)
                {
                    coin.CancelInvoke("EnemyReflect");
                    coin.CancelInvoke("ShootAtPlayer");
                    coin.shotByEnemy = false;
                }

                coin.CancelInvoke("TripleTime");
                coin.CancelInvoke("TripleTimeEnd");
                coin.CancelInvoke("DoubleTime");
                coin.ricochets++;
                if (coin.currentCharge != null)
                {
                    GameObject.Destroy(coin.currentCharge);
                }

                coin.rb.isKinematic = true;
                coin.shot = true;
                PunchflectionNetwork(pos, forward, coin);
            }
        }
        public static void ActiveFrame(Vector3 pos, Vector3 fow, FistType type, bool parried, bool hitSomething, bool firstFrame = false)
        {
            if (type == FistType.Standard && !parried)
            {
                Collider[] array = Physics.OverlapSphere(pos, 0.01f, 16384, QueryTriggerInteraction.Collide);
                List<Transform> list = new List<Transform>();
                Collider[] array2 = array;
                foreach (Collider collider in array2)
                {
                    list.Add(collider.transform);
                    if (TryParryProjectile(pos, fow, (collider.attachedRigidbody != null) ? collider.attachedRigidbody.transform : collider.transform, firstFrame, parried, hitSomething))
                    {
                        break;
                    }
                }
                bool flag;
                RaycastHit localHit;
                flag = Physics.Raycast(pos, fow, out localHit, 4f, 16384);
                if (!flag)
                {
                    flag = Physics.BoxCast(pos, Vector3.one * 0.3f, fow, out localHit, Quaternion.LookRotation(fow), 4f, 16384);
                }

                if (!flag || list.Contains(localHit.transform) || !TryParryProjectile(pos, fow, localHit.transform, firstFrame, parried, hitSomething))
                {
                    hitSomething = true;
                }
            }

            bool flag3 = Physics.Raycast(pos, fow, out RaycastHit hit, 4f, LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
            if (!flag3)
            {
                flag3 = Physics.SphereCast(pos, 1f, fow, out hit, 4f, LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
            }

            if (flag3)
            {
                if (type == FistType.Standard && hit.collider.CompareTag("Coin"))
                {
                    Coin component3 = hit.collider.GetComponent<Coin>();
                    if ((bool)component3 && component3.doubled)
                    {
                        DelayedPunchflectionNetwork(pos, fow, component3);
                    }
                }

                if (hitSomething)
                {
                    return;
                }

                bool flag4 = false;
                if (Physics.Raycast(pos, hit.point - pos, out var hitInfo, 5f, LayerMaskDefaults.Get(LMD.Environment)) && Vector3.Distance(pos, hit.point) > Vector3.Distance(pos, hitInfo.point))
                {
                    flag4 = true;
                }

                if (!flag4)
                {
                    hitSomething = true;
                }
            }

            if (hitSomething)
            {
                return;
            }

            Collider[] array4 = Physics.OverlapSphere(pos, 0.1f, LayerMaskDefaults.Get(LMD.Enemies), QueryTriggerInteraction.Collide);
            if (array4 != null && array4.Length != 0)
            {
                hitSomething = true;
            }

            if (type == FistType.Standard && !hitSomething && !parried)
            {
                Collider[] array5 = Physics.OverlapSphere(pos + fow * 3f, 3f, 16384, QueryTriggerInteraction.Collide);
                bool flag5 = false;
                Collider[] array2 = array5;
                foreach (Collider collider3 in array2)
                {
                    Nail nail = ((!collider3.attachedRigidbody) ? collider3.GetComponent<Nail>() : collider3.attachedRigidbody.GetComponent<Nail>());
                    if (!(nail == null) && nail.sawblade && nail.punchable)
                    {
                        flag5 = true;
                        if (nail.stopped)
                        {
                            nail.stopped = false;
                            nail.rb.velocity = (fow - nail.transform.position).normalized * nail.originalVelocity.magnitude;
                        }
                        else
                        {
                            nail.rb.velocity = (fow - nail.transform.position).normalized * nail.rb.velocity.magnitude;
                        }

                        nail.punched = true;
                        if (nail.magnets.Count > 0)
                        {
                            nail.punchDistance = Vector3.Distance(nail.transform.position, nail.GetTargetMagnet().transform.position);
                        }
                    }
                }

                if (!flag5)
                {
                    array2 = Physics.OverlapSphere(pos + fow, 1f, 1, QueryTriggerInteraction.Collide);
                    foreach (Collider collider4 in array2)
                    {
                        float num = Vector3.Distance(pos + fow, collider4.transform.position);
                        if (num < 6f || num > 12f || Mathf.Abs((pos + fow).y - collider4.transform.position.y) > 3f || !collider4.TryGetComponent<Magnet>(out var component4) || component4.sawblades.Count <= 0)
                        {
                            continue;
                        }

                        float num2 = float.PositiveInfinity;
                        float num3 = 0f;
                        int num4 = -1;
                        for (int num5 = component4.sawblades.Count - 1; num5 >= 0; num5--)
                        {
                            if (component4.sawblades[num5] == null)
                            {
                                component4.sawblades.RemoveAt(num5);
                                if (flag5)
                                {
                                    num4--;
                                }
                            }
                            else
                            {
                                num3 = Vector3.Distance(component4.sawblades[num5].transform.position, pos);
                                if (component4.sawblades[num5] != null && (num4 < 0 || num2 < num3))
                                {
                                    num4 = num5;
                                    num2 = num3;
                                    flag5 = true;
                                }
                            }
                        }

                        if (!flag5 || !component4.sawblades[num4].TryGetComponent<Nail>(out var component5))
                        {
                            continue;
                        }

                        component5.transform.position = pos + fow;
                        if (component5.stopped)
                        {
                            component5.stopped = false;
                            component5.rb.velocity = (fow - component5.transform.position).normalized * component5.originalVelocity.magnitude;
                        }
                        else
                        {
                            component5.rb.velocity = (fow - component5.transform.position).normalized * component5.rb.velocity.magnitude;
                        }

                        component5.punched = true;
                        if (component5.magnets.Count > 0)
                        {
                            Magnet targetMagnet = component5.GetTargetMagnet();
                            if (Vector3.Distance(component5.transform.position + component5.rb.velocity.normalized, targetMagnet.transform.position) > Vector3.Distance(component5.transform.position, targetMagnet.transform.position))
                            {
                                component5.MagnetRelease(targetMagnet);
                            }
                            else
                            {
                                component5.punchDistance = Vector3.Distance(component5.transform.position, targetMagnet.transform.position);
                            }
                        }

                        break;
                    }
                }
            }

            hitSomething = true;
            if (hit.collider.gameObject.TryGetComponent<Bleeder>(out var component7))
            {
                if (type == FistType.Standard)
                {
                    component7.GetHit(hit.point, GoreType.Body);
                }
                else
                {
                    component7.GetHit(hit.point, GoreType.Head);
                }
            }

            if (type == FistType.Heavy)
            {
                Glass component8 = hit.collider.gameObject.GetComponent<Glass>();
                if (component8 != null && !component8.broken)
                {
                    component8.Shatter();
                }
            }
        }
        public static bool TryParryProjectile(Vector3 pos, Vector3 fow, Transform target, bool canProjectileBoost = false, bool parriedSomething = false, bool hitSomething = false)
        {
            if (target.TryGetComponent<ParryHelper>(out var component))
            {
                target = component.target;
            }

            if (target.TryGetComponent<Cannonball>(out var component3) && component3.launchable)
            {
                if (!component3.parry)
                {
                    MonoSingleton<TimeController>.Instance.ParryFlash();
                }

                Vector3 parryLookTarget = fow;
                if (Vector3.Distance(component3.transform.position, parryLookTarget) < 10f)
                {
                    if (Physics.Raycast(pos, fow, out var hitInfo, 5f, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment)))
                    {
                        component3.transform.position = hitInfo.point;
                    }
                    else
                    {
                        component3.transform.position = pos + fow * 5f;
                    }

                    component3.transform.forward = fow;
                }
                else
                {
                    component3.transform.LookAt(parryLookTarget);
                }

                component3.Launch();
                hitSomething = true;
                parriedSomething = true;
                return true;
            }

            if (target.TryGetComponent<ParryReceiver>(out var component4))
            {
                if (!component4.enabled)
                {
                    return false;
                }
                MonoSingleton<TimeController>.Instance.ParryFlash();

                component4.Parry();
                hitSomething = true;
                parriedSomething = true;
                return true;
            }

            if (target.TryGetComponent<MassSpear>(out var component6))
            {
                if (!component6.beenStopped || component6.hittingPlayer)
                {
                    component6.Deflected();
                    hitSomething = true;
                    parriedSomething = true;
                }
                else
                {
                    if (!component6.hitPlayer || hitSomething)
                    {
                        return false;
                    }
                    MonoSingleton<TimeController>.Instance.HitStop(0.1f);
                    component6.GetHurt(5f);
                    hitSomething = true;
                }

                return true;
            }

            if (target.TryGetComponent<Landmine>(out var component7))
            {
                component7.transform.LookAt(fow);
                component7.Parry();
                hitSomething = true;
                parriedSomething = true;
                return true;
            }
            return false;
        }
    }
}