using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite
{
    public struct Bullet
    {
        public GameObject bullet;
        public ulong owner;
    }
    public static class BannedModsDetector
    {
        public static float rate = 0.1f;
        public static List<Bullet> bullets = new List<Bullet>();
        public static List<Bullet> shotgunBullets = new List<Bullet>();
        public static List<ulong> culprits = new List<ulong>();

        public static void AddBullet(GameObject bulletObj, ulong owner, bool shotgun = false)
        {
            rate = 0.1f;
            Bullet bullet = new Bullet { bullet = bulletObj, owner = owner };
            if(shotgun)
                shotgunBullets.Add(bullet);
            else
                bullets.Add(bullet);
            if(!culprits.Contains(owner)) culprits.Add(owner);
        }
        public static void Tick()
        {
            rate -= Time.deltaTime;
            if (rate <= 0f)
            {
                rate = 0f;
                bullets.Clear();
                culprits.Clear();
                return;
            }
            if (NetworkManager.IsHostSocket && NetworkManager.Instance.currentType == LobbyType.Public)
            {
                foreach (var culprit in culprits)
                {
                    Check(bullets.ToArray(), false, culprit);
                    Check(shotgunBullets.ToArray(), true, culprit);
                }
            }
        }
        public static void Check(Bullet[] list, bool shotgun, ulong culprit)
        {
            List<Bullet> bulletsFromPerson = new List<Bullet>();
            foreach(var bul in list)
            {
                if(bul.owner == culprit) bulletsFromPerson.Add(bul);
            }
            if(bulletsFromPerson.Count > (shotgun ? 450 : 45))
            {
                NetworkManager.Instance.ForceKick(culprit, "Potentially using banned mod: FullestAuto/UltraCoins in public lobby");
            }
        }
    }
}
