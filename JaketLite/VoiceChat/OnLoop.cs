using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.VoiceChat
{
    public class OnLoop : MonoBehaviour
    {
        private AudioSource source;
        private float prev;
        public bool hasLooped;

        void Awake()
        {
            source = GetComponent<AudioSource>();
        }
        void Update()
        {
            if (source.time < prev)
            {
                hasLooped = true;
                return;
            }
            prev = source.time;
        }
        public void Reset()
        {
            hasLooped = false;
            prev = source.time;
        }
    }
}
