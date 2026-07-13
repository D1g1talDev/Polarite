using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polarite.Debugging;
using Polarite.Multiplayer;
using Polarite.Networking.Extensions;
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
            return id == 76561198893363168 || id == 76561199078878250 || id == 76561198709095584 || id == 76561198728009961;
        }
        public static void Setup()
        {
            List?.Clear();
            List = new NetworkList();
            Logs.Info("Setup network list", name: "Net");
        }
        public static void End()
        {
            List?.Clear();
            List = null;
            Logs.Info("Cleared network list", name: "Net");
        }
        public static bool TryGet(string path, ulong sender, Vector3 fallbackPos, out INetworkObject net, bool noRecovery = false)
        {
            try
            {
                INetworkObject obj = Get(path, sender, fallbackPos, noRecovery);
                if (NetworkList.ValidObjectCheck(obj))
                {
                    net = obj;
                    return true;
                }
                else
                {
                    net = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                Logs.Error("TryGet: " + e.Message + " path: " + path, name: "Net");
                net = null;
                return false;
            }
        }

        public static INetworkObject Get(string path, ulong sender, Vector3 fallbackPos, bool noRecovery = false)
        {
            if (Paused)
            {
                return null;
            }
            GameObject found = SceneObjectCache.Find(path);
            if (found != null)
            {
                INetworkObject netObj = found.NetObject();
                if (netObj != null && NetworkList.ValidObjectCheck(netObj))
                    return netObj;
            }

            foreach (var obj in List.Objects)
            {
                if (obj.ID == path && NetworkList.ValidObjectCheck(obj))
                    return obj;
            }

            if (!noRecovery && !List.Blacklist.Contains(path))
            {
                return Recover(GetSimpleId(path), path, sender, fallbackPos);
            }
            else
            {
                Logs.DebugError("rip");
                return null;
            }
        }

        public static INetworkObject Recover(string simpleId, string id, ulong sender, Vector3 pos)
        {
            Logs.Debug("recovering " + simpleId);
            if(NetworkManager.Id == sender)
            {
                return null;
            }
            INetworkObject obj = null;
            if (Enum.TryParse<EnemyType>(simpleId, true, out EnemyType type))
            {
                EnemyIdentifier eid = SceneObjectCache.TrySpawnEnemy(id, type, pos, Quaternion.identity, sender);
                obj = eid.gameObject.NetObject();
                obj.Base.simpleId = simpleId;
                obj.Base.owner = sender;
                if (obj != null && !List.Contains(obj))
                {
                    List.Add(obj);
                }
            }
            Logs.Debug($"[OBJECT {simpleId}] Recovering object for sender {sender}.", name: "Net");
            return obj;
        }


        public static string GetSimpleId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            var parts = id.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                return parts[2];
            }
            return string.Empty;
        }

        public static void Pause()
        {
            Paused = true;
            Logs.Info("Network paused", name: "Net");
        }
        public static void Unpause()
        {
            Paused = false;
            Logs.Info("Network unpaused", name: "Net");
        }

        public static void Tick()
        {
            List.Tick();
        }
    }
}
