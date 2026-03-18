using System;
using System.Collections.Generic;

using UnityEngine;

namespace Polarite
{
    public class NetworkList
    {
        const float NetTick = 0.1f;
        private float timer;

        private readonly Dictionary<string, INetworkObject> byId = new Dictionary<string, INetworkObject>();

        public IReadOnlyCollection<INetworkObject> Objects => byId.Values;

        public INetworkObject this[string id] => id != null && byId.TryGetValue(id, out var obj) ? obj : null;

        public INetworkObject Add(INetworkObject obj)
        {
            if (obj == null || string.IsNullOrEmpty(obj.ID))
                return null;

            if (byId.ContainsKey(obj.ID))
            {
                ItePlugin.LogDebug($"[NET LIST] Dupe ID detected: {obj.ID}");
                return byId[obj.ID];
            }

            byId[obj.ID] = obj;
            return obj;
        }

        public void Tick()
        {
            timer += Time.deltaTime;
            if (timer < NetTick) return;
            timer = 0f;

            foreach (var obj in Objects)
                obj.SendState(new Multiplayer.PacketWriter());
        }


        public bool Remove(string id)
        {
            if (id == null)
                return false;

            return byId.Remove(id);
        }

        public bool Contains(string id)
        {
            return id != null && byId.ContainsKey(id);
        }

        public bool Contains(INetworkObject obj)
        {
            return obj != null && !string.IsNullOrEmpty(obj.ID) && byId.TryGetValue(obj.ID, out var existing) && ReferenceEquals(existing, obj);
        }


        public void Clear()
        {
            List<INetworkObject> snapshot = new List<INetworkObject>(byId.Values);

            byId.Clear();

            foreach (var obj in snapshot)
            {
                if (obj?.Base != null)
                    GameObject.Destroy(obj.Base.gameObject);
            }
        }
    }

}
