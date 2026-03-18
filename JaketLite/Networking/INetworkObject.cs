using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using UnityEngine;

namespace Polarite
{
    public interface INetworkObject
    {
        string ID { get; set; }
        string SimpleID { get; }

        ulong Owner { get; set; }

        Vector3 TargetPosition { get; set; }
        Quaternion TargetRotation { get; set; }
        Vector3 LastPosition { get; set; }
        Quaternion LastRotation { get; set; }
        NetworkObject Base { get; }
        void Transfer(ulong newOwner);
        void HandDestroy();
        void SendState(PacketWriter writer);
        void TransferOwnerP2P(ulong newOwner);
        void Send(PacketWriter writer, PacketType packet, bool showDebug = true);
        void Respond(BinaryPacketReader reader, PacketType packet, ulong sender);
        void State(Vector3 pos, BinaryPacketReader reader);
        
    }
}
