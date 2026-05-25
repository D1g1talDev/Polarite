using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polarite.Debugging;
using Polarite.Multiplayer;

using TMPro;

using UnityEngine;

namespace Polarite
{
    public class PlayerRocket : MonoBehaviour
    {
        public ulong owner;
        public bool frozen;
        public Grenade rocket;

        void Start()
        {
            rocket = GetComponent<Grenade>();
            Logs.Info($"Rocket spawned with owner {owner}", this);
        }

    }
}
