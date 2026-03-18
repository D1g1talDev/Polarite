using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Revolver))]
    internal class RevolverSyncPatches
    {
        [HarmonyPatch(nameof(Revolver.Shoot))]
        [HarmonyPrefix]
        static void Postfix(Revolver __instance, ref int shotType)
        {
            BulletType type = (__instance.altVersion) ? (shotType == 1) ? BulletType.Slab : (__instance.gunVariation == 2) ? BulletType.SlabSharp : BulletType.SlabSuper : (shotType == 1) ? BulletType.Revolver : (__instance.gunVariation == 2) ? BulletType.RevolverSharp : BulletType.RevolverSuper;
            GunSync.Sync(type, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero);
        }
        [HarmonyPatch(nameof(Revolver.ThrowCoin))]
        [HarmonyPrefix]
        static void CoinPostfix(Revolver __instance)
        {
            GunSync.Sync(BulletType.Coin, GunSync.Pos, GunSync.Rot, GunSync.Dir, PlayerTracker.Instance.GetPlayerVelocity(true));
        }
    }
    [HarmonyPatch(typeof(Shotgun))]
    internal class ShotgunSyncPatches
    {
        [HarmonyPatch(nameof(Shotgun.Shoot))]
        [HarmonyPrefix]
        static void Postfix(Shotgun __instance)
        {
            GunSync.Sync(BulletType.ShotgunMain, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero, __instance.primaryCharge, 0, __instance.variation);
        }
        [HarmonyPatch(nameof(Shotgun.ShootSinks))]
        [HarmonyPrefix]
        static void SinksPostfix(Shotgun __instance)
        {
            GunSync.Sync(BulletType.ShotgunCore, GunSync.Pos, GunSync.Rot, __instance.grenadeVector, Vector3.zero, 0, __instance.grenadeForce);
        }
    }
    [HarmonyPatch(typeof(ShotgunHammer))]
    internal class ShotgunHammerSyncPatches
    {
        [HarmonyPatch(nameof(ShotgunHammer.ThrowNade))]
        [HarmonyPrefix]
        static void Postfix(ShotgunHammer __instance)
        {
            GunSync.Sync(BulletType.JackCore, MonoSingleton<CameraController>.Instance.GetDefaultPos() + GunSync.Dir * 2f - MonoSingleton<CameraController>.Instance.transform.up * 0.5f, GunSync.Rot, GunSync.Dir, (MonoSingleton<NewMovement>.Instance.ridingRocket ? MonoSingleton<NewMovement>.Instance.ridingRocket.rb.velocity : MonoSingleton<NewMovement>.Instance.rb.velocity));
        }
        [HarmonyPatch(nameof(ShotgunHammer.HitNade))]
        [HarmonyPrefix]
        static void HitPostfix(ShotgunHammer __instance)
        {
            GunSync.Sync(BulletType.GrenadeBeam, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero);
        }
        [HarmonyPatch(nameof(ShotgunHammer.ImpactEffects))]
        [HarmonyPrefix]
        static void ImpactPrefix(ShotgunHammer __instance)
        {
            BulletType type;
            if (__instance.primaryCharge > 0)
            {
                type = BulletType.JackhammerPump;
            }
            else
            {
                switch(__instance.tier)
                {
                    case 0:
                        type = BulletType.JackhammerLight;
                        break;
                    case 1:
                        type = BulletType.JackhammerMedium;
                        break;
                    case 2:
                        type = BulletType.JackhammerHeavy;
                        break;
                    default:
                        type = BulletType.JackhammerLight;
                        break;
                }
            }
            if(__instance.forceWeakHit && type != BulletType.JackhammerPump)
            {
                type = BulletType.JackhammerLight;
            }
            GunSync.Sync(type, MonoSingleton<CameraController>.Instance.GetDefaultPos() + GunSync.Dir * 2.5f, GunSync.Rot, GunSync.Dir, Vector3.zero, __instance.primaryCharge);
        }
    }
    [HarmonyPatch(typeof(Nailgun))]
    internal class NailgunSyncPatches
    {
        [HarmonyPatch(nameof(Nailgun.Shoot))]
        [HarmonyPrefix]
        static void Postfix(Nailgun __instance)
        {
            BulletType type = (__instance.altVersion) ? (__instance.variation == 1) ? BulletType.Saw : BulletType.SawSad : (__instance.variation == 1) ? BulletType.Nail : BulletType.NailSad;
            GunSync.Sync(type, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero, 0, __instance.currentSpread);
        }
        [HarmonyPatch(nameof(Nailgun.BurstFire))]
        [HarmonyPrefix]
        static void BurstPostfix(Nailgun __instance)
        {
            GunSync.Sync(BulletType.NailHeated, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero, 0, __instance.currentSpread);
        }
        [HarmonyPatch(nameof(Nailgun.SuperSaw))]
        [HarmonyPrefix]
        static void SuperPostfix(Nailgun __instance)
        {
            GunSync.Sync(BulletType.SawHeated, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero, 0, __instance.heatUp);
        }
        [HarmonyPatch(nameof(Nailgun.ShootMagnet))]
        [HarmonyPrefix]
        static void MagnetPostfix(Nailgun __instance)
        {
            GunSync.Sync(BulletType.Magnet, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero);
        }
    }
    [HarmonyPatch(typeof(Railcannon))]
    internal class RailcannonSyncPatches
    {
        [HarmonyPatch(nameof(Railcannon.Shoot))]
        [HarmonyPrefix]
        static void Postfix(Railcannon __instance)
        {
            BulletType type = (__instance.variation == 0) ? BulletType.RailBlue : (__instance.variation == 1) ? BulletType.RailGreen : BulletType.RailRed;
            GunSync.Sync(type, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero, 0);
        }
    }
    [HarmonyPatch(typeof(Grenade))]
    internal class RocketPatch
    {
        [HarmonyPatch("get_frozen")]
        [HarmonyPrefix]
        static bool Prefix(Grenade __instance, ref bool __result)
        {
            if(!NetworkManager.InLobby)
            {
                return true;
            }
            if(__instance.TryGetComponent<PlayerRocket>(out var rocket))
            {
                if(rocket == null)
                {
                    return true;
                }
                if(NetworkManager.Id == rocket.owner)
                {
                    __result = MonoSingleton<WeaponCharges>.Instance.rocketFrozen;
                    return false;
                }
                __result = rocket.frozen;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(RocketLauncher))]
    internal class RocketLauncherSyncPatches
    {
        [HarmonyPatch(nameof(RocketLauncher.Shoot))]
        [HarmonyPrefix]
        static void Postfix(RocketLauncher __instance)
        {
            GunSync.Sync(BulletType.Rocket, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero);
        }
        [HarmonyPatch(nameof(RocketLauncher.FreezeRockets))]
        [HarmonyPostfix]
        static void FreezePostfix(RocketLauncher __instance)
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteBool(MonoSingleton<WeaponCharges>.Instance.rocketFrozen);
                NetworkManager.Instance.BroadcastPacket(PacketType.RocketFreeze, w.GetBytes());
            }
        }

        [HarmonyPatch(nameof(RocketLauncher.UnfreezeRockets))]
        [HarmonyPostfix]
        static void UnfreezePostfix(RocketLauncher __instance)
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteBool(MonoSingleton<WeaponCharges>.Instance.rocketFrozen);
                NetworkManager.Instance.BroadcastPacket(PacketType.RocketFreeze, w.GetBytes());
            }
        }

        [HarmonyPatch(nameof(RocketLauncher.ShootCannonball))]
        [HarmonyPrefix]
        static void CannonballPostfix(RocketLauncher __instance)
        {
            GunSync.Sync(BulletType.Cannonball, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero, 0, __instance.cbCharge);
        }
        [HarmonyPatch(nameof(RocketLauncher.ShootNapalm))]
        [HarmonyPrefix]
        static void NapalmPostfix(RocketLauncher __instance)
        {
            GunSync.Sync(BulletType.Oil, GunSync.Pos, GunSync.Rot, GunSync.Dir, Vector3.zero);
        }
    }
    [HarmonyPatch(typeof(Punch))]
    internal class MyHands
    {
        [HarmonyPatch(nameof(Punch.BlastCheck))]
        [HarmonyPrefix]
        static void Postfix(Punch __instance)
        {
            if(NetworkManager.InLobby && __instance.heldAction.IsPressed())
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.transform.position);
                NetworkManager.Instance.BroadcastPacket(PacketType.Blast, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(Punch.ActiveFrame))]
        [HarmonyPrefix]
        static void ActivePrefix(Punch __instance, ref bool firstFrame)
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.cc.GetDefaultPos());
                w.WriteVector3(__instance.cc.GetDefaultPos() + __instance.cc.transform.forward);
                w.WriteBool(__instance.parriedSomething);
                w.WriteBool(__instance.hitSomething);
                w.WriteInt((int)__instance.type);
                w.WriteBool(firstFrame);
                NetworkManager.Instance.BroadcastPacket(PacketType.Feedback, w.GetBytes());
            }
        }
    }
    [HarmonyPatch(typeof(Projectile))]
    internal class ProjBoostPatch
    {
        [HarmonyPatch(nameof(Projectile.CreateExplosionEffect))]
        [HarmonyPrefix]
        static void Prefix(Projectile __instance)
        {
            if(NetworkManager.InLobby && (__instance.parried || __instance.boosted))
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(__instance.transform.position);
                NetworkManager.Instance.BroadcastPacket(PacketType.ProjExplode, w.GetBytes());
            }
        }
    }
}
