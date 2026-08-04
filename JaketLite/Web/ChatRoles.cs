using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polarite.Multiplayer;
using Polarite.Networking;
using Polarite.Debugging;
using BepInEx.Logging;

namespace Polarite.Web
{
    public class ServerRole
    {
        public string name;
        public string hex;
        public List<string> owners;
        public ServerRole(string name, string hex, JArray owners)
        {
            this.name = name;
            this.hex = hex;

            List<string> items = owners.ToObject<List<string>>();
            this.owners = items;
        }
    }
    public static class ChatRoles
    {
        public static Dictionary<ulong, List<ServerRole>> playerRoles = new Dictionary<ulong, List<ServerRole>>();

        public static string Tags(ulong plr, bool onlyColors)
        {
            StringBuilder sb = new StringBuilder();
            foreach(var role in playerRoles[plr])
            {
                if (!onlyColors)
                {
                    sb.Append($"<color={role.hex}>[{(role.name == role.name.ToLower() ? role.name : role.name.ToUpper())}]");
                }
                else
                {
                    sb.Append($"<color={role.hex}>");
                }
            }
            return sb.ToString();
        }
        public static string Finisher(ulong plr)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var role in playerRoles[plr])
            {
                sb.Append($"</color>");
            }
            return sb.ToString();
        }
        public static List<ServerRole> Get(ulong plr)
        {
            if (!playerRoles.ContainsKey(plr)) Init(plr);
            return playerRoles[plr];
        }
        public static List<ServerRole> Get()
        {
            if (!playerRoles.ContainsKey(NetworkManager.Id)) Init(NetworkManager.Id);
            return playerRoles[NetworkManager.Id];
        }
        public static void Init(ulong plr)
        {
            if (!playerRoles.ContainsKey(plr)) playerRoles.Add(plr, new List<ServerRole>());
            XServers.Roles((r) =>
            {
                AddRoles(r, plr);
            });
        }
        public static void AddRoles(List<ServerRole> roles, ulong user)
        {
            List<ServerRole> newRoles = new List<ServerRole>();
            foreach(var role in roles)
            {
                if (role.owners.Contains(user.ToString()))
                {
                    newRoles.Add(role);
                }
            }
            playerRoles[user] = newRoles;
        }
        public static void OnChatRoleUpdate(string role, string user)
        {
            Init(ulong.Parse(user));
        }
    }
}
