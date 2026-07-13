using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using Polarite.Multiplayer;
using System.Runtime.InteropServices;

namespace Polarite.Networking.Sockets
{
    public class Client : IConnectionManager
    {
        public void OnConnected(ConnectionInfo info)
        {
            NetworkManager.DisplayJoin("green", $"Connected to socket server");
            NetworkManager.Instance.JoinAnnounceClient();
            NetworkManager.LocPlayerCheck();
        }

        public void OnConnecting(ConnectionInfo info)
        {
            NetworkManager.DisplayJoin("yellow", $"Connecting to socket server...");
        }

        public void OnDisconnected(ConnectionInfo info)
        {
            NetworkManager.DisplayJoin("red", $"Disconnected from socket server: {info.EndReason}");
            NetworkManager.Instance.LeaveLobby(true);
        }

        public void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            if(!NetworkManager.IsConnectedSocket)
            {
                return;
            }
            byte[] buffer = new byte[size];
            Marshal.Copy(data, buffer, 0, size);

            NetworkManager.Instance.HandlePackClient(buffer);
        }
    }
}
