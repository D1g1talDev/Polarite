using System;
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
