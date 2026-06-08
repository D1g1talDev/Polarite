using Polarite.Debugging;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Polarite
{
    public class NetworkList
    {
        const float NetTick = 0.1f;
        const float BlacklistTick = 0.3f;
        public float netTimer { get; private set; }
        public float blackTimer { get; private set; }

        private readonly List<INetworkObject> netList = new List<INetworkObject>();
        private readonly List<string> blacklist = new List<string>();

        public IReadOnlyCollection<INetworkObject> Objects => netList.AsReadOnly();
        public IReadOnlyCollection<string> Blacklist => blacklist.AsReadOnly();

        public INetworkObject this[string id] => id != null ? netList.Find(obj => obj.ID == id) : null;
        public INetworkObject Add(INetworkObject obj)
        {
            if (obj == null)
                return null;

            if (netList.Contains(obj))
            {
                Logs.Debug($"Dupe ID detected: {obj.ID}", this);
                return netList.Find(o => o.ID == obj.ID);
            }

            netList.Add(obj);
            return obj;
        }

        public void Tick()
        {
            BlackListTick();
            netTimer += Time.deltaTime;
            if (netTimer < NetTick) return;
            netTimer = 0f;
            List<INetworkObject> trash = new List<INetworkObject>();

            foreach (var obj in Objects)
            {
                if(ValidObjectCheck(obj))
                {
                    obj.SendState(new PacketWriter(), PacketType.ObjectState);
                }
                else
                {
                    trash.Add(obj);
                }
            }
            Dump(trash);
        }
        public void BlackListTick()
        {
            blackTimer += Time.deltaTime;
            if (blackTimer < BlacklistTick) return;
            blackTimer = 0f;
            blacklist.Clear();
        }
        public void AddBlacklist(string id)
        {
            if (blacklist.Contains(id)) return;
            blackTimer = 0f;
            blacklist.Add(id);
        }
        public static bool ValidObjectCheck(INetworkObject obj)
        {
            if (obj.Base == null) return false;
            if (!obj.Base.Alive) return false;
            return true;
        }
        public void Dump(List<INetworkObject> objs)
        {
            foreach(var obj in objs)
            {
                if(obj.Cleanup && obj.Base != null)
                {
                    continue;
                }
                Remove(obj, true);
            }
        }


        public bool Remove(INetworkObject obj, bool includePath)
        {
            try
            {
                if (includePath && SceneObjectCache.ContainsIndex(obj.Index))
                {
                    SceneObjectCache.Remove(obj.Base.gameObject);
                }
                return netList.Remove(obj);
            }
            catch
            {
                return netList.Remove(obj);
            }
        }

        public bool Contains(INetworkObject obj)
        {
            return obj != null && netList.Contains(obj);
        }

        public int IndexOf(INetworkObject obj)
        {
            if(!netList.Contains(obj))
            {
                return -1;
            }
            return netList.IndexOf(obj);
        }


        public void Clear()
        {
            List<INetworkObject> snapshot = new List<INetworkObject>(netList);

            netList.Clear();
            blacklist.Clear();

            foreach (var obj in snapshot)
            {
                if (obj?.Base != null)
                    GameObject.Destroy(obj.Base.gameObject);
            }
        }
    }

}
