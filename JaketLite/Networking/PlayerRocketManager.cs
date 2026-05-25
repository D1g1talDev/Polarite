using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using UnityEngine;

namespace Polarite
{
    public class PlayerRocketManager : MonoBehaviour
    {
        public ulong owner;
        public List<PlayerRocket> rockets = new List<PlayerRocket>();
        public bool rocketsFrozen;
        public AudioSource oilLoop, tickLoop;
        public AudioClip oilStart, oilEnd;

        public bool spray = false;
        public bool noiseCheck = false;

        public float sprayNoiseCooldown = 0f;

        void Start()
        {
            oilStart = ItePlugin.mainBundle.LoadAsset<AudioClip>("RocketSprayStart");
            oilEnd = ItePlugin.mainBundle.LoadAsset<AudioClip>("RocketSprayStop");
            oilLoop = CreateSourceLoop("Spray", ItePlugin.mainBundle.LoadAsset<AudioClip>("RocketSprayLoop"));
            tickLoop = CreateSourceLoop("Tick", ItePlugin.mainBundle.LoadAsset<AudioClip>("ClockTickTock"));
        }
        public AudioSource CreateSourceLoop(string name, AudioClip clip)
        {
            GameObject go = new GameObject(name);
            AudioSource aud = go.AddComponent<AudioSource>();
            aud.transform.SetParent(transform, false);
            aud.transform.position = new Vector3(transform.position.x, transform.position.y + 1.5f);
            aud.spatialBlend = 1f;
            aud.volume = 0.5f;
            aud.minDistance = 30f;
            aud.maxDistance = 100f;
            aud.clip = clip;
            aud.loop = true;
            aud.Play();
            aud.mute = true;
            return aud;
        }

        void Update()
        {
            for(int i = rockets.Count - 1; i >= 0; i--)
            {
                if(rockets[i] == null)
                {
                    rockets.RemoveAt(i);
                }
                else
                {
                    rockets[i].frozen = rocketsFrozen;
                }
            }
            sprayNoiseCooldown -= Time.unscaledDeltaTime;
            if(sprayNoiseCooldown <= 0f)
            {
                spray = false;
                sprayNoiseCooldown = 0f;
            }
            else
            {
                spray = true;
            }
            if(spray && !noiseCheck)
            {
                SprayNoiseStart();
                noiseCheck = true;
            }
            if(!spray && noiseCheck)
            {
                SprayNoiseStop();
                noiseCheck = false;
            }
            oilLoop.mute = !spray;
            tickLoop.mute = !rocketsFrozen;
        }
        public void SprayNoiseStart()
        {
            ItePlugin.SpawnSound(oilStart, Random.Range(0.975f, 1.05f), null, 0.5f, oilLoop.transform.position);
        }
        public void SprayNoiseStop()
        {
            ItePlugin.SpawnSound(oilEnd, Random.Range(0.975f, 1.05f), null, 0.5f, oilLoop.transform.position);
        }
        public static void Freeze(ulong player, bool value)
        {
            PlayerRocketManager rm = Get(player);
            rm.rocketsFrozen = value;
        }
        public static PlayerRocketManager Get(ulong player)
        {
            foreach(var p in NetworkManager.players.Values)
            {
                if(p.TryGetComponent<PlayerRocketManager>(out var rm))
                {
                    if(rm.owner == player)
                    {
                        return rm;
                    }
                }
            }
            return null;
        }
    }
}
