using Polarite.Multiplayer;

using UnityEngine;
using Polarite.Networking;
using UnityEngine.ProBuilder.MeshOperations;
using Polarite.Debugging;
using System.Collections;

namespace Polarite
{
    public abstract class NetworkObject : MonoBehaviour, INetworkObject
    {
        public string id;
        public string simpleId;
        public int index;
        public ulong owner = 0;

        protected bool syncTransform = true;
        protected bool isCleaningUp = false;
        protected bool alive = true;

        public string ID
        {
            get => id;
            set => id = value;
        }
        public NetworkObject Base
        {
            get => this;
        }
        public string SimpleID
        {
            get
            {
                if (!string.IsNullOrEmpty(simpleId))
                    return simpleId;
                if (string.IsNullOrEmpty(id))
                    return string.Empty;

                return Net.GetSimpleId(id);
            }
        }
        public int Index
        {
            get => index;
        }
        public ulong Owner
        {
            get => owner;
            set => owner = value;
        }
        public bool Alive
        {
            get
            {
                if (Base == null) return false;
                return alive;
            }
        }
        public bool Cleanup
        {
            get
            {
                if(!NetworkList.ValidObjectCheck(Base)) return false;
                return isCleaningUp;
            }
        }
        public bool TransformSynced
        {
            get => syncTransform;
        }
        public bool Owns
        {
            get => Net.IsOwner(this);
        }
        public Vector3 TargetPosition { get; set; }
        public Quaternion TargetRotation { get; set; }
        public Vector3 LastPosition { get; set; }
        public Quaternion LastRotation { get; set; }

        protected float interpolationTime = 0.1f;
        protected float interpTimer = 0f;

        public virtual void Start()
        {
            if(!Net.List.Contains(this))
            {
                Net.List.Add(this);
                index = Net.List.IndexOf(this);
            }
            LastPosition = transform.position;
            LastRotation = transform.rotation;
            TargetPosition = transform.position;
            TargetRotation = transform.rotation;

            if(string.IsNullOrEmpty(ID))
            {
                ID = SceneObjectCache.GetScenePath(gameObject);
            }

            Transfer(owner);
        }
        public virtual void OnDestroy()
        {
            alive = false;
            Net.List.AddBlacklist(id);
            HandDestroy();
        }
        public virtual void HandDestroy()
        {
            if (Net.List.Contains(this))
            {
                Net.List.Remove(this, true);
            }
            if (Net.IsOwner(this))
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(transform.position);
                w.WriteString(id);
                Send(w, PacketType.ObjectRemoved);
            }
        }

        public virtual void Transfer(ulong newOwner)
        {
            owner = newOwner;
            PacketWriter w = new PacketWriter();
            w.WriteString(id);
            w.WriteULong(newOwner);
            w.WriteVector3(transform.position);
            NetworkManager.Instance.BroadcastPacket(PacketType.Ownership, w.GetBytes());
        }
        public virtual void TransferOwnerP2P(ulong newOwner)
        {
            owner = newOwner;
        }
        public virtual void SendState(PacketWriter writer, PacketType type)
        {
            if (!Net.IsOwner(this))
                return;
            if (!alive)
                return;

            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            LastPosition = pos;
            LastRotation = rot;

            writer.WriteString(id);
            writer.WriteVector3(pos);
            writer.WriteQuaternion(rot);

            NetworkManager.Instance.BroadcastPacket(type, writer.GetBytes(), sendtype: SendTypeConsts.ST_OBJSTATE);
        }


        public virtual void Send(PacketWriter writer, PacketType packet, bool showDebug = true)
        {
            if(showDebug && ItePlugin.debugSending)
            {
                ItePlugin.LogDebug($"<color=yellow>[OBJECT {SimpleID}] Sending {packet} to network.</color>");
            }
            NetworkManager.Instance.BroadcastPacket(packet, writer.GetBytes());
        }

        public virtual void Respond(BinaryPacketReader response, PacketType packet, ulong sender)
        {
            if(ItePlugin.debugSending)
            {
                ItePlugin.LogDebug($"<color=green>[OBJECT {SimpleID}] Responded to {packet}.</color>");
            }
        }

        public virtual void State(Vector3 pos, Quaternion rot, BinaryPacketReader reader)
        {
            if (!alive) return;

            if (syncTransform)
            {
                LastPosition = TargetPosition;
                LastRotation = TargetRotation;
                TargetPosition = pos;
                TargetRotation = rot;
                interpTimer = 0f;
            }
        }
        public void PrepDestroy()
        {
            Logs.Info($"Preparing to destroy: {simpleId}", name: "NetworkObject");
            StartCoroutine(DestroyCoro());
        }
        private IEnumerator DestroyCoro()
        {
            isCleaningUp = true;
            yield return new WaitForSeconds(0.5f);
            isCleaningUp = false;
            Destroy(gameObject);
        }
        public virtual void Update()
        {
            if (owner == 0)
                return;
            if (!alive)
                return;

            if (syncTransform && !Net.IsOwner(this))
            {
                interpTimer += Time.deltaTime;
                float t = interpTimer / interpolationTime;

                if (t >= 1f)
                {
                    transform.SetPositionAndRotation(TargetPosition, TargetRotation);
                    return;
                }

                if(Vector3.Distance(transform.position, TargetPosition) >= 10f)
                {
                    transform.SetPositionAndRotation(TargetPosition, TargetRotation);
                    return;
                }

                Vector3 pos = Vector3.Lerp(LastPosition, TargetPosition, t);
                Quaternion rot = Quaternion.Slerp(LastRotation, TargetRotation, t);

                if ((transform.position - pos).sqrMagnitude > 0.0001f)
                {
                    transform.SetPositionAndRotation(pos, rot);
                }
            }
        }

    }
}
