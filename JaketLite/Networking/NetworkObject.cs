using Polarite.Multiplayer;

using UnityEngine;
using Polarite.Networking;
using UnityEngine.ProBuilder.MeshOperations;

namespace Polarite
{
    public abstract class NetworkObject : MonoBehaviour, INetworkObject
    {
        public string id;
        public string simpleId;
        public ulong owner = 0;
        public bool syncTransform = true;
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

                int index = id.IndexOf(':');
                return index == -1 ? id : id.Substring(0, index);
            }
        }

        public ulong Owner
        {
            get => owner;
            set => owner = value;
        }

        public Vector3 TargetPosition { get; set; }
        public Quaternion TargetRotation { get; set; }
        public Vector3 LastPosition { get; set; }
        public Quaternion LastRotation { get; set; }

        protected float interpolationTime = 0.1f;

        private float interpTimer = 0f;

        public virtual void Start()
        {
            /*
            if(!Net.List.Contains(this))
            {
                Net.List.Add(this);
            }
            LastPosition = transform.position;
            LastRotation = transform.rotation;
            TargetPosition = transform.position;
            TargetRotation = transform.rotation;

            Transfer(owner);
            */
        }
        public virtual void OnDestroy()
        {
            HandDestroy();
        }
        public virtual void HandDestroy()
        {
            /*
            if (Net.List.Contains(this))
            {
                Net.List.Remove(id);
            }
            if (Net.IsOwner(this))
            {
                PacketWriter w = new PacketWriter();
                w.WriteVector3(transform.position);
                w.WriteString(id);
                Send(w, PacketType.ObjectRemoved);
            }
            */
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
        public virtual void SendState(PacketWriter writer)
        {
            if (!Net.IsOwner(this))
                return;

            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (Vector3.SqrMagnitude(pos - LastPosition) < 0.0025f &&
                Quaternion.Angle(rot, LastRotation) < 2f)
                return;

            LastPosition = pos;
            LastRotation = rot;

            writer.WriteString(id);
            writer.WriteVector3(pos);
            writer.WriteQuaternion(rot);

            NetworkManager.Instance.BroadcastPacket(PacketType.ObjectState, writer.GetBytes());
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

        public virtual void State(Vector3 pos, BinaryPacketReader reader)
        {
            Quaternion rot = reader.ReadQuaternion();
            if(syncTransform)
            {
                LastPosition = TargetPosition;
                LastRotation = TargetRotation;
                TargetPosition = pos;
                TargetRotation = rot;
                interpTimer = 0f;
            }
        }
        public virtual void Update()
        {
            if (owner == 0)
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
