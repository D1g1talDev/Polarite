using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Networking.Extensions
{
    public static class GameObjectExtensions
    {
        public static INetworkObject NetObject(this GameObject obj)
        {
            if(obj.TryGetComponent<INetworkObject>(out var netObj))
            {
                return netObj;
            }
            return null;
        }
        public static bool Owner(this GameObject obj)
        {
            return obj.NetObject().Owns;
        }
        public static ulong OwnerId(this GameObject obj) => obj.NetObject().Owner;
        public static bool IsNetwork(this GameObject obj) => obj.NetObject() != null;
    }
}
