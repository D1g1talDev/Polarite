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
using Polarite.Debugging;

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
    public class PlayerExplosionId : MonoBehaviour
    {
        public string id;
        public ulong player;
    }

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
        public static Dictionary<BulletType, string> BulletNoises = new Dictionary<BulletType, string>
        {
            { BulletType.Revolver, "RevNorm" },
            { BulletType.RevolverSuper, "RevSuper" },
            { BulletType.RevolverSharp, "RevRed"},
            { BulletType.Slab, "RevAlt"},
            { BulletType.SlabSuper, "RevAltSuper"},
            { BulletType.SlabSharp, "RevRed"},
            { BulletType.ShotgunMain, "ShotgunFire" },
            { BulletType.ShotgunCore, "ShotgunAltFire" },
            { BulletType.Nail, "NailgunFire" },
            { BulletType.NailSad, "NailgunFire" },
            { BulletType.NailHeated, "NailgunFire" },
            { BulletType.Saw, "NailgunAltFire" },
            { BulletType.SawSad, "NailgunAltFire" },
            { BulletType.SawHeated, "NailgunMagFire" },
            { BulletType.Magnet, "NailgunMagFire" },
            { BulletType.RailBlue, "RailcannonFire" },
            { BulletType.RailGreen, "RailcannonFire" },
            { BulletType.RailRed, "RailcannonMalFire" },
            { BulletType.Rocket, "RocketFire" },
            { BulletType.Cannonball, "RocketFire" },
            { BulletType.JackCore, "JackAltFire" },
        };
        public static Vector3 Pos => MonoSingleton<CameraController>.Instance.transform.position + MonoSingleton<CameraController>.Instance.transform.forward;
        public static Quaternion Rot
        {
            get
            {
                if(MonoSingleton<CameraFrustumTargeter>.Instance.CurrentTarget != null && MonoSingleton<CameraFrustumTargeter>.Instance.IsAutoAimed)
                {
                    return Quaternion.LookRotation(MonoSingleton<CameraFrustumTargeter>.Instance.CurrentTargetAimPosition - MonoSingleton<CameraController>.Instance.GetDefaultPos());
                }
                return MonoSingleton<CameraController>.Instance.transform.rotation;
            }
        }
        public static Vector3 Dir
        {
            get
            {
                return MonoSingleton<CameraController>.Instance.transform.forward;
            }
        }
        public static Vector3 DirWAutoAim
        {
            get
            {
                if (MonoSingleton<CameraFrustumTargeter>.Instance.CurrentTarget != null && MonoSingleton<CameraFrustumTargeter>.Instance.IsAutoAimed)
                {
                    return MonoSingleton<CameraFrustumTargeter>.Instance.GetAimDirectionFrom(MonoSingleton<CameraController>.Instance.GetDefaultPos());
                }
                return MonoSingleton<CameraController>.Instance.transform.forward;
            }
        }

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
        public static void Blast(Vector3 pos, ulong player)
        {
            GameObject blast = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Wave.prefab").WaitForCompletion(), pos, Quaternion.identity);
            Explosion exp = blast.GetComponentInChildren<Explosion>();
            exp.damage = (ItePlugin.canBeFriendlyFired.value) ? 5 : 0;
            exp.friendlyFire = false;
            exp.harmless = false;
            exp.hasHitPlayer = false;
            exp.canHit = AffectedSubjects.PlayerOnly;
            AddExpId(blast, "blast", player);
        }
        public static void ProjBoost(Vector3 pos, ulong player)
        {
            GameObject blast = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion.prefab").WaitForCompletion(), pos, Quaternion.identity);
            Explosion exp = blast.GetComponentInChildren<Explosion>();
            exp.damage = (ItePlugin.canBeFriendlyFired.value) ? 10 : 0;
            exp.friendlyFire = false;
            exp.harmless = false;
            exp.hasHitPlayer = false;
            exp.canHit = AffectedSubjects.PlayerOnly;
            AddExpId(blast, "parry", player);
        }
        public static void AddExpId(GameObject obj, string id, ulong player)
        {
            PlayerExplosionId expId = obj.AddComponent<PlayerExplosionId>();
            expId.id = id;
            expId.player = player;
        }
        public static void AnimShoot(ulong plr)
        {
            NetworkPlayer netPlr = NetworkPlayer.Find(plr);
            if(netPlr != null)
            {
                netPlr.ShootAnim();
            }
        }
        public static void Sync(BulletType type, Vector3 pos, Quaternion rot, Vector3 dir, Vector3 vel, int pump = 0, float pow = 0f, int var = 0)
        {
            if(!NetworkManager.InLobby)
            {
                return;
            }
            if(NetworkPlayer.LocalPlayer.testPlayer)
            {
                AnimShoot(NetworkManager.Id);
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
        public static void Read(BinaryPacketReader reader, ulong sender)
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
                Logs.Warn($"Unknown bullet type: {type}", name: "GunSync");
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
                        Rigidbody rb = coin.GetComponent<Rigidbody>();
                        rb.AddForce(dir * 20f + -Vector3.down * 15f + vel, ForceMode.VelocityChange);
                        c.power = 0;
                        return;
                    }
                case BulletType.ShotgunMain:
                    {
                        AnimShoot(sender);
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
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.ShotgunCore:
                    {
                        AnimShoot(sender);
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
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.JackCore:
                    {
                        AnimShoot(sender);
                        GameObject obj = GameObject.Instantiate(bulletPrefab, pos, Random.rotation);
                        if (obj.TryGetComponent<Rigidbody>(out var component))
                        {
                            component.AddForce(dir * 3f + Vector3.up * 7.5f + vel, ForceMode.VelocityChange);
                        }
                        CleanProj(obj, sender);
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.NailSad:
                case BulletType.SawSad:
                case BulletType.Saw:
                case BulletType.Nail:
                    {
                        AnimShoot(sender);
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
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.SawHeated:
                    {
                        AnimShoot(sender);
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
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.NailHeated:
                    {
                        AnimShoot(sender);
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
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.Magnet:
                    {
                        AnimShoot(sender);
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.AddForce(rb.transform.forward * 100f, ForceMode.VelocityChange);
                        }
                        CleanProj(projectile, sender);
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.RailGreen:
                    {
                        AnimShoot(sender);
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.AddForce(rb.transform.forward * 250f, ForceMode.VelocityChange);
                        }
                        CleanProj(projectile, sender);
                        PlayNoise(type, pos);
                        return;
                    }
                case BulletType.Cannonball:
                    {
                        AnimShoot(sender);
                        GameObject projectile = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Rigidbody rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.velocity = rb.transform.forward * Mathf.Max(15f, pow * 150f);
                        }
                        CleanProj(projectile, sender);
                        PlayNoise(type, pos);
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
                        PlayerRocketManager rm = PlayerRocketManager.Get(sender);
                        if(rm != null)
                        {
                            rm.sprayNoiseCooldown = 0.05f;
                        }
                        CleanProj(projectile, sender);
                        return;
                    }
                case BulletType.JackhammerPump:
                    {
                        AnimShoot(sender);
                        if (pump == 3)
                        {
                            GameObject boom = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Attacks and Projectiles/Explosions/Explosion Super.prefab").WaitForCompletion(), pos, Quaternion.identity);
                            CleanProj(boom, sender);
                            return;
                        }
                        GameObject hammer = GameObject.Instantiate(bulletPrefab, pos, rot);
                        Explosion[] componentsInChildren = hammer.GetComponentsInChildren<Explosion>();
                        foreach (Explosion explosion in componentsInChildren)
                        {
                            AddExpId(explosion.gameObject, "default", sender);
                            explosion.hitterWeapon = "hammer";
                            if (pump == 2)
                            {
                                explosion.maxSize *= 2f;
                            }
                            explosion.canHit = AffectedSubjects.PlayerOnly;
                            if(ItePlugin.canBeFriendlyFired.value)
                                explosion.damage -= 10;
                            else
                                explosion.damage = 0;
                        }
                        return;
                    }
                case BulletType.JackhammerLight:
                    {
                        AnimShoot(sender);
                        GameObject light = GameObject.Instantiate(bulletPrefab, pos, rot);
                        return;
                    }
                case BulletType.JackhammerMedium:
                    {
                        AnimShoot(sender);
                        GameObject medium = GameObject.Instantiate(bulletPrefab, pos, rot);
                        return;
                    }
                case BulletType.JackhammerHeavy:
                    {
                        AnimShoot(sender);
                        GameObject heavy = GameObject.Instantiate(bulletPrefab, pos, rot);
                        return;
                    }
            }
            GameObject bullet = GameObject.Instantiate(bulletPrefab, pos, rot);
            CleanProj(bullet, sender);
            PlayNoise(type, pos);
            AnimShoot(sender);
        }
        public static void PlayNoise(BulletType type, Vector3 pos)
        {
            AudioClip clip = ItePlugin.mainBundle.LoadAsset<AudioClip>(BulletNoises[type]);
            if(clip != null)
            {
                float pitch = (type == BulletType.Cannonball) ? 0.75f : (ShouldUseNormPitch(type)) ? 1f : Random.Range(0.975f, 1.05f);
                ItePlugin.SpawnSound(clip, pitch, null, 0.5f, pos);
            }
        }
        public static bool ShouldUseNormPitch(BulletType type) => (type == BulletType.RailBlue || type == BulletType.RailGreen || type == BulletType.RailRed);
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
                    AddExpId(norm.gameObject, "default", sender);
                    norm.canHit = AffectedSubjects.PlayerOnly;
                    if (!ItePlugin.canBeFriendlyFired.value)
                    {
                        norm.damage = 0;
                    }
                }
                if (big != null)
                {
                    AddExpId(big.gameObject, "default", sender);
                    big.canHit = AffectedSubjects.PlayerOnly;
                    if (!ItePlugin.canBeFriendlyFired.value)
                    {
                        big.damage = 0;
                    }
                }
                if (harm != null)
                {
                    harm.canHit = AffectedSubjects.PlayerOnly;
                    if (!ItePlugin.canBeFriendlyFired.value)
                    {
                        harm.damage = 0;
                    }
                }
            }
            if (bullet.TryGetComponent<RevolverBeam>(out var rev))
            {
                rev.damage = 0;
                Explosion potExp = rev.hitParticle.GetComponentInChildren<Explosion>();
                if (potExp != null)
                {
                    AddExpId(potExp.gameObject, "default", sender);
                    potExp.enemy = true;
                    potExp.friendlyFire = false;
                    potExp.hasHitPlayer = false;
                    potExp.canHit = AffectedSubjects.PlayerOnly;
                    if(!ItePlugin.canBeFriendlyFired.value)
                    {
                        potExp.damage = 0;
                    }
                }
            }
            if (bullet.TryGetComponent<Projectile>(out var proje))
            {
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
                    AddExpId(potExp.gameObject, "default", sender);
                    potExp.enemy = true;
                    potExp.friendlyFire = false;
                    potExp.hasHitPlayer = false;
                    potExp.canHit = AffectedSubjects.PlayerOnly;
                    if (!ItePlugin.canBeFriendlyFired.value)
                    {
                        potExp.damage = 0;
                    }
                }
            }
        }
    }
}