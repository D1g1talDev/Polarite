using Polarite.Debugging;
using Polarite.Multiplayer;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Networking.Sockets
{
    public class Server : ISocketManager
    {
        public void OnConnected(Connection connection, ConnectionInfo info)
        {
            // map connection to steamid and vice versa
            NetworkManager.AddMapping(connection, info.Identity.SteamId);

            NetworkManager.DisplayJoin("green", $"{NetworkManager.GetNameOfId(info.Identity.SteamId, true)} connected to the socket server");
            NetworkManager.Instance.JoinAnnounce(info);
        }

        public void OnConnecting(Connection connection, ConnectionInfo info)
        {
            NetworkManager.DisplayJoin("yellow", $"{NetworkManager.GetNameOfId(info.Identity.SteamId, true)} is connecting to the socket server...");
            if(NetworkManager.Instance.IsBanned(info.Identity))
            {
                NetworkManager.DisplayJoin("red", $"Rejected connection {NetworkManager.GetNameOfId(info.Identity.SteamId, true)} (banned)");
                connection.Close();
                return;
            }
            if (NetworkManager.Instance.currentType == LobbyType.FriendsOnly && !new Friend(info.Identity.SteamId).IsFriend)
            {
                NetworkManager.DisplayJoin("red", $"Rejected connection {NetworkManager.GetNameOfId(info.Identity.SteamId, true)} (not a friend)");
                connection.Close();
                return;
            }
            if (NetworkManager.Contains(connection))
            {
                NetworkManager.DisplayJoin("red", $"Rejected connection from {NetworkManager.GetNameOfId(info.Identity.SteamId, true)} (already connected)");
                connection.Close();
                return;
            }
            // passed all checks, allow connection
            connection.Accept();
        }

        public void OnDisconnected(Connection connection, ConnectionInfo info)
        {
            // clean up mappings
            NetworkManager.RemoveMapping(connection, info.Identity.SteamId);

            NetworkManager.DisplayJoin("red", $"{NetworkManager.GetNameOfId(info.Identity.SteamId, true)} disconnected from the socket server: {info.EndReason}");

            PacketWriter write = new PacketWriter();
            write.WriteULong(info.Identity.SteamId);
            NetworkManager.Instance.BroadcastPacket(PacketType.GlobalConnectionLeave, write.GetBytes());
        }

        public void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            byte[] buffer = new byte[size];
            Marshal.Copy(data, buffer, 0, size);

            NetworkManager.Instance.HandlePack(buffer, identity.SteamId);
        }
    }
}
