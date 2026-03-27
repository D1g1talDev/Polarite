using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using Steamworks;

using UnityEngine;

namespace Polarite.Networking
{
    public static class Net
    {
        public static NetworkList List { get; set; }
        public static bool Paused { get; set; }
        public static bool IsOwner(INetworkObject obj)
        {
            return obj.Owner == NetworkManager.Id;
        }
        public static bool Dev(ulong id)
        {
            return id == 76561198893363168 || id == 76561199078878250;
        }
        public static void Setup()
        {
            List?.Clear();
            List = new NetworkList();
        }
        public static void End()
        {
            List?.Clear();
            List = null;
        }

        public static INetworkObject Get(string path, ulong sender, Vector3 fallbackPos)
        {
            if(Paused)
            {
                return null;
            }
            GameObject found = SceneObjectCache.Find(path);
            if (found != null)
            {
                INetworkObject netObj = found.GetComponent<INetworkObject>();
                if (netObj != null)
                    return netObj;
            }

            foreach (var obj in List.Objects)
            {
                if (obj.ID == path)
                    return obj;
            }

            return Recover(GetSimpleId(path), path, sender, fallbackPos);
        }


        public static INetworkObject Recover(string simpleId, string id, ulong sender, Vector3 pos)
        {
            if(NetworkManager.Id == sender)
            {
                return null;
            }
            INetworkObject obj = null;
            if (Enum.TryParse<EnemyType>(simpleId, true, out EnemyType type))
            {
                EnemyIdentifier eid = SceneObjectCache.TrySpawnEnemy(id, type, pos, Quaternion.identity, sender);
                NetworkEnemy enemy = NetworkEnemy.Create(id, eid, sender);
                obj = enemy.GetComponent<INetworkObject>();
                if (obj != null && !List.Contains(obj))
                {
                    List.Add(obj);
                }
            }
            ItePlugin.LogDebug($"[OBJECT {simpleId}] Recovering object for sender {sender}.");
            return obj;
        }


        public static string GetSimpleId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            int index = id.IndexOf(':');
            return index == -1 ? id : id.Substring(0, index);
        }

        public static void Pause()
        {
            Paused = true;
        }
        public static void Unpause()
        {
            Paused = false;
        }

        public static void Tick()
        {
            List.Tick();
        }
    }
}
