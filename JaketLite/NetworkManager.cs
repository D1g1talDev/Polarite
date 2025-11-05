using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;

using Mono.Cecil.Cil;

using Steamworks;
using Steamworks.Data;

using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SocialPlatforms;

using static UnityEngine.GraphicsBuffer;

using Lobby = Steamworks.Data.Lobby;
using Random = UnityEngine.Random;

namespace Polarite.Multiplayer
{
    [Serializable]
    public class NetPacket
    {
        public string type;
        public string name;
        public string[] parameters;
        public ulong senderId;
    }

    public enum LobbyType
    {
        Private,
        FriendsOnly,
        Public
    }

    public static class LobbyCodeUtil
    {
        private const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string ToBase36(ulong value)
        {
            if (value == 0) return "0";
            StringBuilder sb = new StringBuilder();
            while (value > 0)
            {
                int remainder = (int)(value % 36);
                sb.Insert(0, chars[remainder]);
                value /= 36;
            }
            return sb.ToString();
        }

        public static ulong FromBase36(string input)
        {
            ulong result = 0;
            foreach (char c in input.ToUpperInvariant())
            {
                int value = chars.IndexOf(c);
                if (value < 0) throw new ArgumentException("Invalid base36 character: " + c);
                result = result * 36 + (ulong)value;
            }
            return result;
        }
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public event Action<NetPacket> OnPacketReceived;
        public event Action<Friend, SteamId> OnPlayerJoined;
        public event Action<Friend, SteamId> OnPlayerLeft;
        public Lobby CurrentLobby;

        public static bool HostAndConnected;
        public static bool ClientAndConnected;
        public static bool InLobby;
        public static bool HasRichPresence;
        public static bool Sandbox;
        public static bool WasUsed;

        public bool autoUpdate = true;

        // rigs
        public static Dictionary<string, NetworkPlayer> players = new Dictionary<string, NetworkPlayer>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            try
            {
                SteamClient.Init(1229490, true);
            }
            catch (Exception e)
            {
                Debug.LogError("[Net] Failed to init SteamClient: " + e);
                return;
            }

            if (!SteamClient.IsValid)
            {
                Debug.LogError("[Net] SteamClient is not initialized.");
                return;
            }

            SteamMatchmaking.OnLobbyMemberJoined += HandleMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += HandleMemberLeft;
            SteamFriends.OnGameLobbyJoinRequested += HandleLobbyInvite;
            SteamFriends.OnGameRichPresenceJoinRequested += HandleLobbyRPJ;

            SteamNetworking.OnP2PSessionRequest += (SteamId id) =>
            {
                SteamNetworking.AcceptP2PSessionWithUser(id);
            };
            SteamNetworking.OnP2PConnectionFailed += (SteamId id, P2PSessionError error) =>
            {
                SteamNetworking.AcceptP2PSessionWithUser(id);
            };

            OnPacketReceived += (NetPacket packet) => PacketReader.ReadPacket(packet);
        }

        void OnApplicationQuit()
        {
            LeaveLobby();
            SteamClient.Shutdown();
        }

        public static string GetNameOfId(ulong id)
        {
            return TMPUtils.StripTMP(new Friend(id).Name);
        }
        public async Task CreateLobby(int maxPlayers = 4, LobbyType lobbyType = LobbyType.Public, string lobbyName = "My Lobby", Action<string> onJoin = null, bool canCheat = false)
        {
            if (!SteamClient.IsValid) return;
            if (InLobby) LeaveLobby();

            Lobby? lobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);

            if (lobby.HasValue)
            {
                CurrentLobby = lobby.Value;
                switch(lobbyType)
                {
                    case LobbyType.Public:
                        CurrentLobby.SetPublic();
                        break;
                    case LobbyType.Private:
                        CurrentLobby.SetPrivate();
                        break;
                    case LobbyType.FriendsOnly:
                        CurrentLobby.SetFriendsOnly();
                        break;
                    default:
                        CurrentLobby.SetPublic(); 
                        break;
                }
                HostAndConnected = true;
                InLobby = true;
                WasUsed = true;
                CurrentLobby.SetData("LobbyName", lobbyName);
                CurrentLobby.SetData("level", SceneHelper.CurrentScene);
                CurrentLobby.SetData("difficulty", PrefsManager.Instance.GetInt("difficulty").ToString());
                CurrentLobby.SetData("cheat", (canCheat) ? "1" : "0");
                onJoin?.Invoke(LobbyCodeUtil.ToBase36(CurrentLobby.Id));
                SetRichPresenceForLobby(CurrentLobby);
                CreateLocalPlayer();
                DisplaySystemChatMessage($"Successfully created lobby '{CurrentLobby.GetData("LobbyName")}'");
                if(lobbyType == LobbyType.Public)
                {
                    DisplayWarningChatMessage($"Your lobby is public, Anyone can join this lobby from the public lobbies tab.");
                }
                PlayerList.UpdatePList();
                ItePlugin.CleanLevelOfSoftlocks();
            }
        }

        public async Task JoinLobby(ulong lobbyId)
        {
            if (!SteamClient.IsValid) return;
            if (InLobby) LeaveLobby();

            Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
            if (lobby.HasValue)
            {
                if (lobby.Value.GetData("banned_" + SteamClient.SteamId.Value.ToString()) == "1")
                {
                    DisplayError("You were banned from this lobby.");
                    lobby.Value.Leave();
                    return;
                }
                if (lobby.Value.MemberCount > lobby.Value.MaxMembers)
                {
                    DisplayError("This lobby is full.");
                    lobby.Value.Leave();
                    return;
                }
                CurrentLobby = lobby.Value;
                ClientAndConnected = true;
                InLobby = true;
                WasUsed = true;
                SetRichPresenceForLobby(lobby.Value);
                CreateLocalPlayer();
                DisplaySystemChatMessage("Successfully joined lobby '" + CurrentLobby.GetData("LobbyName") + "'");
                EnsureP2PSessionWithAll();
                foreach (var member in lobby.Value.Members)
                {
                    if (!players.ContainsKey(member.Id.Value.ToString()))
                    {
                        NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                        players.Add(member.Id.Value.ToString(), newPlr);
                    }
                }
                LoadLevelAndDifficulty(lobby);
                BroadcastPacket(new NetPacket
                {
                    type = "skin",
                    name = ((int)ItePlugin.skin.value).ToString()
                });
                PlayerList.UpdatePList();
            }
            else
            {
                DisplayError("Failed to join lobby.");
            }
        }

        public bool AmIHost()
        {
            return CurrentLobby.Id != 0 &&
                   CurrentLobby.Owner.Id != 0 &&
                   CurrentLobby.Owner.Id == SteamClient.SteamId;
        }

        public async Task JoinLobbyByCode(string code)
        {
            if (!SteamClient.IsValid) return;
            if (InLobby) LeaveLobby();

            ulong lobbyId = LobbyCodeUtil.FromBase36(code);
            await JoinLobby(lobbyId);
        }

        public async void FetchPublicLobbies(Action<Lobby?> onFound)
        {
            var list = await SteamMatchmaking.LobbyList.RequestAsync();

            if (list == null || list.Length == 0)
            {
                return;
            }
            foreach (var lobby in list)
            {
                onFound.Invoke(lobby);
            }
        }

        public void LeaveLobby()
        {
            if (CurrentLobby.Id == 0)
                return;

            if(HostAndConnected)
            {
                BroadcastPacket(new NetPacket
                {
                    type = "hostleave"
                });
            }
            string lobbyName = CurrentLobby.GetData("LobbyName");
            HostAndConnected = false;
            ClientAndConnected = false;
            InLobby = false;
            SetRichPresenceForLobby(null);
            CurrentLobby.Leave();
            CurrentLobby = default;
            foreach (var plr in players.Values)
            {
                if (plr != null && plr != NetworkPlayer.LocalPlayer)
                {
                    Destroy(plr.gameObject);
                }
            }
            players.Clear();
            DisplaySystemChatMessage($"Successfully left lobby '{lobbyName}'");
            PlayerList.UpdatePList();
        }

        public string GetLobbyCode()
        {
            if (CurrentLobby.Id == 0) return null;
            return LobbyCodeUtil.ToBase36(CurrentLobby.Id);
        }

        public void ShowInviteOverlay()
        {
            if (CurrentLobby.Id != 0)
                SteamFriends.OpenGameInviteOverlay(CurrentLobby.Id);
        }

        public void KickPlayer(ulong targetId, bool ban = false)
        {
            if (CurrentLobby.Id == 0) return;

            if (!AmIHost())
            {
                DisplayError((!ban) ? "Only the host can kick!" : "Only the host can ban!");
                return;
            }
            NetPacket packet;
            if (ban)
            {
                CurrentLobby.SetData("banned_" + targetId, "1");
                packet = new NetPacket { type = "ban", name = "You were banned", senderId = SteamClient.SteamId };
            }
            else
            {
                packet = new NetPacket { type = "kick", name = "You were kicked", senderId = SteamClient.SteamId };
            }
            SendPacket(packet, targetId);
        }

        public void GetAllPlayersInLobby(Lobby? lobby, out SteamId[] ids, bool ignoreSelf = true)
        {
            List<SteamId> list = new List<SteamId>();
            foreach(var p in lobby.Value.Members)
            {
                if(ignoreSelf && p.Id == SteamClient.SteamId)
                {
                    continue;
                }
                list.Add(p.Id);
            }
            ids = list.ToArray();
        }

        public void SetRichPresenceForLobby(Lobby? lobby)
        {
            if (!SteamClient.IsValid)
                return;

            if (lobby.HasValue && lobby.Value.Id != 0)
            {
                HasRichPresence = true;
                SteamFriends.SetRichPresence("connect", lobby.Value.Id.ToString());
                SteamFriends.SetRichPresence("status", "In Lobby");
                SteamFriends.SetRichPresence("steam_display", "In Lobby");
                if(ItePlugin.HasDiscord)
                {
                    ItePlugin.discord.GetActivityManager().UpdateActivity(new Activity
                    {
                        Details = $"Playing in: {Instance.CurrentLobby.GetData("levelName")}, In Polarite Lobby ({CurrentLobby.MemberCount}/{CurrentLobby.MaxMembers})",
                        Instance = true
                    }, delegate { });
                }
            }
            else
            {
                HasRichPresence = false;
                SteamFriends.SetRichPresence("connect", null);
                SteamFriends.SetRichPresence("status", null);
                SteamFriends.SetRichPresence("steam_display", null);

                if(ItePlugin.HasDiscord)
                {
                    string levelName = StockMapInfo.Instance.levelName;
                    if (string.IsNullOrEmpty(levelName))
                    {
                        levelName = SceneHelper.CurrentScene;
                    }
                    ItePlugin.discord.GetActivityManager().UpdateActivity(new Activity
                    {
                        Details = $"Playing in: {levelName}, Not In Lobby",
                        Instance = true
                    }, delegate { });
                }
            }
        }
        private void HandleMemberJoined(Lobby lobby, Friend member)
        {
            if (!HostAndConnected)
            {
                return;
            }
            if (lobby.Id == CurrentLobby.Id)
            {
                OnPlayerJoined?.Invoke(member, member.Id);
                if(!players.ContainsKey(member.Id.Value.ToString()))
                {
                    NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                    players.Add(member.Id.Value.ToString(), newPlr);
                }
                DisplaySystemChatMessage(GetNameOfId(member.Id) + " has joined this lobby");
                NetPacket packet = new NetPacket
                {
                    type = "join",
                    name = member.Id.ToString()
                };
                foreach (var member1 in CurrentLobby.Members)
                {
                    if (member1.Id != SteamClient.SteamId && member1.Id != member.Id)
                        SendPacket(packet, member1.Id.Value);
                }
                EnsureP2PSessionWithAll();
                BroadcastPacket(new NetPacket
                {
                    type = "skin",
                    name = ((int)ItePlugin.skin.value).ToString()
                });
                PlayerList.UpdatePList();
            }
        }

        public void HandleMemberJoinedP2P(Friend member)
        {
            OnPlayerJoined?.Invoke(member, member.Id);
            if (!players.ContainsKey(member.Id.Value.ToString()))
            {
                NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                players.Add(member.Id.Value.ToString(), newPlr);
            }
            DisplaySystemChatMessage(GetNameOfId(member.Id) + " has joined this lobby");
            EnsureP2PSessionWithAll();
            BroadcastPacket(new NetPacket
            {
                type = "skin",
                name = ((int)ItePlugin.skin.value).ToString()
            });
            HostAndConnected = AmIHost();
            ClientAndConnected = !AmIHost();
            InLobby = CurrentLobby.Id != 0;
            PlayerList.UpdatePList();
        }

        public void EnsureP2PSessionWithAll()
        {
            if (CurrentLobby.Id == 0) return;
            foreach (var member in CurrentLobby.Members)
            {
                if (member.Id != SteamClient.SteamId)
                {
                    SteamNetworking.AcceptP2PSessionWithUser(member.Id);
                }
            }
        }

        private void HandleMemberLeft(Lobby lobby, Friend member)
        {
            if(!HostAndConnected)
            {
                return;
            }
            if (lobby.Id == CurrentLobby.Id)
            {
                OnPlayerLeft?.Invoke(member, member.Id);
                if (players.ContainsKey(member.Id.Value.ToString()))
                {
                    Destroy(players[member.Id.Value.ToString()].gameObject);
                    players.Remove(member.Id.Value.ToString());
                }
                DisplaySystemChatMessage(GetNameOfId(member.Id) + " has left this lobby");
                NetPacket packet = new NetPacket
                {
                    type = "left",
                    name = member.Id.ToString()
                };
                foreach (var member1 in CurrentLobby.Members)
                {
                    if (member1.Id != SteamClient.SteamId && member1.Id != member.Id)
                        SendPacket(packet, member1.Id.Value);
                }
                PlayerList.UpdatePList();
            }
        }

        public void HandleMemberLeftP2P(Friend member)
        {
            OnPlayerLeft?.Invoke(member, member.Id);
            if (players.ContainsKey(member.Id.Value.ToString()))
            {
                Destroy(players[member.Id.Value.ToString()].gameObject);
                players.Remove(member.Id.Value.ToString());
            }
            DisplaySystemChatMessage(GetNameOfId(member.Id) + " has left this lobby");
            HostAndConnected = AmIHost();
            ClientAndConnected = !AmIHost();
            InLobby = CurrentLobby.Id != 0;
            PlayerList.UpdatePList();
        }

        private void HandleLobbyInvite(Lobby lobby, SteamId id)
        {
            if (!SteamClient.IsValid) return;
            DisplaySystemChatMessage("Attempting to join " + GetNameOfId(id) + "'s game (via invite)");
            JoinLobby(lobby.Id).Forget();
        }

        private void HandleLobbyRPJ(Friend friend, string connect)
        {
            DisplaySystemChatMessage("Attempting to join " + GetNameOfId(friend.Id) + "'s game (via profile)");
            if (ulong.TryParse(connect, out var lobbyId))
            {
                JoinLobby(lobbyId).Forget();
            }
        }
        public void SendPacket(NetPacket packet, ulong targetId)
        {
            if (!SteamClient.IsValid) return;
            packet.senderId = SteamClient.SteamId;
            byte[] data = Encoding.UTF8.GetBytes(JsonUtility.ToJson(packet));
            SteamNetworking.SendP2PPacket(targetId, data);
        }

        public void BroadcastPacket(NetPacket packet)
        {
            packet.senderId = SteamClient.SteamId;

            if (CurrentLobby.Id == 0 || !SteamClient.IsValid) return;

            foreach (var member in CurrentLobby.Members)
            {
                if (member.Id != SteamClient.SteamId && member.Id != packet.senderId)
                    SendPacket(packet, member.Id.Value);
            }
        }

        public void SendToHost(NetPacket packet)
        {
            if (AmIHost()) 
                return;

            SendPacket(packet, CurrentLobby.Owner.Id.Value);
        }

        public void LoadLevelAndDifficulty(Lobby? lobby)
        {
            if(lobby.HasValue)
            {
                ItePlugin.ignoreSpectate = true;
                SceneHelper.LoadScene(lobby.Value.GetData("level"));
                PrefsManager.Instance.SetInt("difficulty", int.Parse(lobby.Value.GetData("difficulty")));
            }
        }

        void Update()
        {
            if (!SteamClient.IsValid) return;
            Sandbox = SceneHelper.CurrentScene == "uk_construct";

            foreach(var member in CurrentLobby.Members)
            {
                if(member.Id != SteamClient.SteamId && !players.ContainsKey(member.Id.Value.ToString()))
                {
                    NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                    players.Add(member.Id.Value.ToString(), newPlr);
                }
            }

            while (SteamNetworking.IsP2PPacketAvailable(out uint packetSize))
            {
                byte[] buffer = new byte[packetSize];
                SteamId id = default;
                if (SteamNetworking.ReadP2PPacket(buffer, ref packetSize, ref id))
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, (int)packetSize);
                    NetPacket packet = JsonUtility.FromJson<NetPacket>(json);

                    if (packet.senderId == SteamClient.SteamId)
                        continue;

                    OnPacketReceived?.Invoke(packet);
                }
            }
        }
        public void CreateTestPlayer()
        {
            if (!players.ContainsKey(SteamClient.SteamId.Value.ToString()))
            {
                NetworkPlayer newPlr = NetworkPlayer.Create(SteamClient.SteamId.Value, GetNameOfId(SteamClient.SteamId));
                players.Add(SteamClient.SteamId.Value.ToString(), newPlr);
                newPlr.testPlayer = true;
            }
        }
        public NetworkPlayer CreateLocalPlayer()
        {
            if(NetworkPlayer.LocalPlayer == null)
            {
                NetworkPlayer newPlr = NetworkPlayer.Create(SteamClient.SteamId.Value, GetNameOfId(SteamClient.SteamId));
                players.Add(SteamClient.SteamId.Value.ToString(), newPlr);
                return newPlr;
            }
            return null;
        }
        public NetworkPlayer CreateFakePlayer()
        {
            NetworkPlayer newPlr = NetworkPlayer.Create((ulong)players.Count + 1, GetNameOfId(SteamFriends.GetFriends().ToArray()[Random.Range(0, SteamFriends.GetFriends().ToArray().Length)].Id));
            players.Add((players.Count + 1).ToString(), newPlr);
            return newPlr;
        }
        public static void DisplaySystemChatMessage(string msg)
        {
            if(ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=yellow>[SYSTEM]: {msg}</color>", false, "");
                ChatUI.Instance.ShowUIForBit();
            }
        }
        public static void DisplayWarningChatMessage(string msg)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=orange>[WARNING]: {msg}</color>", false, "");
                ChatUI.Instance.ShowUIForBit();
            }
        }
        public static void DisplayGameChatMessage(string msg)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=grey>{msg}</color>", false, "");
                ChatUI.Instance.ShowUIForBit(7f);
            }
        }
        public static void ShoutCheckpoint(string whoCheckpointed)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=#e96bff>{whoCheckpointed} has reached a checkpoint.</color>", false, "");
                ChatUI.Instance.ShowUIForBit(5f);
            }
        }
        public static void ShoutCheater(string whoCheated)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=#2eff69>{whoCheated} activated cheats.</color>", false, "");
                ChatUI.Instance.ShowUIForBit(7f);
            }
        }
        public static void DisplayError(string errorMsg)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=red>[ERROR]: {errorMsg}</color>", false, "");
                ChatUI.Instance.ShowUIForBit(7f);
            }
        }
    }

    public static class TaskExtensions
    {
        public static void Forget(this Task task) { }
    }
    public static class TMPUtils
    {
        private static readonly Regex tmpTagRegex = new Regex("<.*?>", RegexOptions.Compiled);

        public static string StripTMP(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return tmpTagRegex.Replace(input, string.Empty);
        }
    }
}
