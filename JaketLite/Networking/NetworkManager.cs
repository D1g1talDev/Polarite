using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;

using Mono.Cecil.Cil;
using Polarite.Debugging;
using Polarite.Networking;
using Polarite.Networking.Extensions;
using Polarite.Networking.Sockets;
using Polarite.Patches;

using Steamworks;
using Steamworks.Data;

using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SocialPlatforms;

using static Polarite.Multiplayer.PacketReader;
using static UnityEngine.GraphicsBuffer;

using Lobby = Steamworks.Data.Lobby;
using Random = UnityEngine.Random;

namespace Polarite.Multiplayer
{
    public class CrackedMode
    {
        public string server_mode;
    }
    public static class SendTypeConsts
    {
        public const ushort ST_DEFAULT = 0;
        public const ushort ST_PLRSTATE = 1;
        public const ushort ST_OBJSTATE = 2;
        public const ushort ST_VOICE = 3;
    }

    public enum LobbyType
    {
        None,
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

        public event Action<Friend, SteamId> OnPlayerJoined;
        public event Action<Friend, SteamId> OnPlayerLeft;
        public Lobby CurrentLobby;
        public LobbyType currentType = LobbyType.None;
        public int currentTypeRaw;
        public string currentLobbyName;
        public int currentMaxPlayers;
        public int currentCheats;

        public static bool HostAndConnected, IsHostSocket;
        public static bool ClientAndConnected, IsClientSocket;
        public static bool InLobby;
        public static bool HasRichPresence;
        public static bool Sandbox;
        public static bool WasUsed;
        public static bool PrivateLobby;

        // this will be the steam id from now on
        public static ulong Id { get; private set; }

        public bool autoUpdate = true;

        public static bool SceneLoading;

        // rigs
        public static Dictionary<ulong, NetworkPlayer> players = new Dictionary<ulong, NetworkPlayer>();

        public static CrackedMode cMode;

        public static ConnectionManager ClientToHost;
        public static SocketManager ServerInstance;

        public Server Server = new Server();
        public Client Client = new Client();

        public static Dictionary<ulong, Connection> connections = new Dictionary<ulong, Connection>();

        // we should handle a packet only if we're connected to the socket
        public static bool IsConnectedSocket
        {
            get
            {
                return (HostAndConnected && ServerInstance != null) ||
                       (ClientAndConnected && ClientToHost != null && ClientToHost.Connected);
            }
        }

        void CrackCheck()
        {
            string path = "server_config.json";
            if (!File.Exists(path))
            {
                SaveDefault(path);
            }
            string text = File.ReadAllText(path);
            CrackedMode loaded = null;
            try
            {
                loaded = JsonUtility.FromJson<CrackedMode>(text);
            }
            catch { }
            if (loaded == null || string.IsNullOrEmpty(loaded.server_mode))
            {
                Debug.LogWarning("Invalid or missing server_config.json, regenerating...");
                SaveDefault(path);
                loaded = new CrackedMode();
            }
            cMode = loaded;
            cMode.server_mode = cMode.server_mode.ToLower();
            switch (cMode.server_mode)
            {
                case "steam":
                    Debug.Log("Steam servers are active.");
                    break;

                case "pirated":
                    Debug.Log("Pirated servers are active.");
                    break;

                default:
                    Debug.LogWarning("Unknown mode in config, resetting to steam.");
                    SaveDefault(path);
                    cMode.server_mode = "steam";
                    break;
            }
        }

        void SaveDefault(string path)
        {
            File.WriteAllText(path,
        @"{
  ""server_mode"": ""steam""
}");
        }
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            CrackCheck();
            Instance = this;
            DontDestroyOnLoad(gameObject);
            try
            {
                if(cMode.server_mode == "steam")
                {
                    try
                    {
                        SteamClient.Init(1229490, true);
                    }
                    catch(Exception)
                    {
                        SteamClient.Init(480, true);
                        cMode.server_mode = "pirated";
                    }
                }
                else if(cMode.server_mode == "pirated")
                {
                    SteamClient.Init(480, true);
                }
            }
            catch (Exception e)
            {
                Logs.Error("[Net] Failed to init SteamClient: " + e, this);
                return;
            }

            if (!SteamClient.IsValid)
            {
                Logs.Error("[Net] SteamClient is not initialized.", this);
                return;
            }

            SteamMatchmaking.OnLobbyMemberJoined += HandleMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += HandleMemberLeft;
            SteamFriends.OnGameLobbyJoinRequested += HandleLobbyInvite;
            SteamFriends.OnGameRichPresenceJoinRequested += HandleLobbyRPJ;

            if (VoiceChatManager.Instance == null)
            {
                GameObject vc = new GameObject("VoiceChatManager");
                vc.AddComponent<VoiceChatManager>();
                DontDestroyOnLoad(vc);
            }
            Id = SteamClient.SteamId.Value;
        }

        void OnApplicationQuit()
        {
            if(ServerInstance != null)
            {
                StopHostServer();
            }
            if(ClientToHost != null)
            {
                DisconnectFromHostServer();
            }
            SetRichPresenceForLobby(null);
            LeaveLobby();
            SteamClient.Shutdown();
        }

        public static string GetNameOfId(ulong id, bool colorName = false)
        {
            string colorHex = Net.Dev(id) ? "color=green" : id == GetHostID() ? "color=#00F2FF" : "";
            if (colorName && !string.IsNullOrEmpty(colorHex)) return $"<{colorHex}>{new Friend(id).Name.WithoutTMP()}</color>";
            return new Friend(id).Name.WithoutTMP();
        }
        public async Task CreateLobby(int maxPlayers = 10, LobbyType lobbyType = LobbyType.Public, string lobbyName = "My Lobby", Action<string> onJoin = null, bool canCheat = false)
        {
            if (!SteamClient.IsValid) return;
            if (InLobby) LeaveLobby();
            if (!ItePlugin.ReleaseBuild && lobbyType == LobbyType.Public)
            {
                DisplayError("You cannot make public lobbies on beta builds.");
                return;
            }
            if(maxPlayers <= 0)
            {
                DisplayError("...");
                return;
            }

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
                CurrentLobby.SetData("levelName", ItePlugin.GetLevelName());
                CurrentLobby.SetData("level", SceneHelper.CurrentScene);
                CurrentLobby.SetData("difficulty", PrefsManager.Instance.GetInt("difficulty").ToString());
                CurrentLobby.SetData("cheat", (canCheat) ? "1" : "0");
                CurrentLobby.SetData("bh", (ItePlugin.bossHpIncrease.value) ? "1" : "0");
                CurrentLobby.SetData("bhm", ItePlugin.bossHpMult.value.ToString());
                CurrentLobby.SetData("priv", lobbyType == LobbyType.Private ? "1" : "0");
                CurrentLobby.SetData("ver", ItePlugin.Version);
                PrivateLobby = lobbyType == LobbyType.Private;
                onJoin?.Invoke(LobbyCodeUtil.ToBase36(CurrentLobby.Id));
                SetRichPresenceForLobby(CurrentLobby);
                CreateLocalPlayer();
                DisplaySystemChatMessage($"Successfully created lobby '{CurrentLobby.GetData("LobbyName")}'");
                if(lobbyType == LobbyType.Public)
                {
                    DisplayWarningChatMessage($"Your lobby is public, Anyone can join this lobby from the public lobbies tab.");
                }
                PlayerList.UpdatePList();
                ItePlugin.Instance.CleanLevel();
                DisplaySkinTip();
                PauseMenuPatch.DisablePauseEffects();
                Net.Setup();
                ItePlugin.ArmCheck(SwapWeaponsPatch.AltWeapon(MonoSingleton<GunControl>.Instance.currentWeapon));
                ItePlugin.AnimationCheck();
                if(SceneHelper.CurrentScene == "Level 7-4")
                {
                    SceneHelper.LoadScene("Level 8-1");
                }
                currentType = lobbyType;
                currentTypeRaw = ItePlugin.Instance.LobbyTypeToRaw(lobbyType);
                currentLobbyName = lobbyName;
                currentMaxPlayers = maxPlayers;
                currentCheats = (canCheat) ? 1 : 0;

                // start host server
                ServerInstance = SteamNetworkingSockets.CreateRelaySocket<SocketManager>(4544);
                ServerInstance.Interface = Server;
                Logs.Info("Started host socket", this);
                IsHostSocket = true;
                LocPlayerCheck();
            }
        }

        public async Task JoinLobby(ulong lobbyId)
        {
            if (!SteamClient.IsValid) return;
            if (SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap") return;
            if (InLobby) LeaveLobby();

            Lobby? lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
            if (lobby.HasValue)
            {
                if (lobby.Value.GetData("ver") != ItePlugin.Version)
                {
                    DisplayError("This lobby is running a different version of the mod.");
                    lobby.Value.Leave();
                    return;
                }
                if (lobby.Value.GetData("banned_" + Id.ToString()) == "1")
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
                if (string.IsNullOrEmpty(lobby.Value.GetData("LobbyName")))
                {
                    DisplayError("This lobby is invalid and it seems the original host disconnected from it.");
                    lobby.Value.Leave();
                    return;
                }
                if (lobby.Value.MemberCount <= 0)
                {
                    DisplayError("There are zero people in this lobby.");
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
                foreach (var member in lobby.Value.Members)
                {
                    if (!players.ContainsKey(member.Id.Value))
                    {
                        NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                        players.Add(member.Id.Value, newPlr);
                    }
                }
                LoadLevelAndDifficulty(lobby);
                PlayerList.UpdatePList();
                DisplaySkinTip();
                PauseMenuPatch.DisablePauseEffects();
                Net.Setup();
                Net.Pause();
                PrivateLobby = lobby.Value.GetData("priv") == "1";
                ItePlugin.ArmCheck(SwapWeaponsPatch.AltWeapon(MonoSingleton<GunControl>.Instance.currentWeapon));

                // small delay
                await Task.Delay(1000);

                ClientToHost = SteamNetworkingSockets.ConnectRelay<ConnectionManager>(lobby.Value.Owner.Id, 4544);
                ClientToHost.Interface = Client;
                Logs.Info("Connecting to host socket... Id: " + lobby.Value.Owner.Id, this);
                IsClientSocket = true;

                while(!ClientToHost.Connected)
                {
                    Logs.Debug("Client state: " + ClientToHost.Connection.DetailedStatus(), this);
                    await Task.Delay(100);
                }
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
                   CurrentLobby.Owner.Id == NetworkManager.Id;
        }

        public async Task JoinLobbyByCode(string code)
        {
            if (!SteamClient.IsValid) return;
            if (SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap") return;
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

        public void LeaveLobby(bool bootToMenu = false)
        {
            if (CurrentLobby.Id == 0)
                return;

            if(IsHostSocket)
            {
                PacketWriter w = new PacketWriter();
                BroadcastPacket(PacketType.HostLeave, w.GetBytes());
                StopHostServer();
                currentType = LobbyType.None;
            }
            else
            {
                DisconnectFromHostServer();
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
            Voice.Clear();
            DisplaySystemChatMessage($"Successfully left lobby '{lobbyName}'");
            PlayerList.UpdatePList();
            Net.End();
            ItePlugin.CustomTogglePlayer(true);
            PrivateLobby = false;
            ItePlugin.ReverseArmCheck(SwapWeaponsPatch.AltWeapon(MonoSingleton<GunControl>.Instance.currentWeapon));
            ItePlugin.ReverseAnimationCheck();
            if(bootToMenu)
            {
                SceneHelper.LoadScene("Main Menu");
            }
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
            PacketWriter w = new PacketWriter();
            PacketType type;
            if (ban)
            {
                CurrentLobby.SetData("banned_" + targetId, "1");
                w.WriteString("You were banned");
                type = PacketType.Ban;
            }
            else
            {
                w.WriteString("You were kicked");
                type = PacketType.Kick;
            }
            SendPacket(type, w.GetBytes(), targetId);
        }
        public void ForceKick(ulong id, string reason)
        {
            if (!connections.TryGetValue(id, out var con)) return;
            con.Close();
            DisplaySystemChatMessage($"{GetNameOfId(id, true)} was auto-kicked from the lobby with the reason: {reason}");
        }

        public void GetAllPlayersInLobby(Lobby? lobby, out SteamId[] ids, bool ignoreSelf = true)
        {
            List<SteamId> list = new List<SteamId>();
            foreach(var p in lobby.Value.Members)
            {
                if(ignoreSelf && p.Id == NetworkManager.Id)
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
                // players should have privacy yknow?
                if(!PrivateLobby)
                {
                    SteamFriends.SetRichPresence("connect", lobby.Value.Id.ToString());
                }
                SteamFriends.SetRichPresence("status", "In Lobby");
                SteamFriends.SetRichPresence("steam_display", "In Lobby");
                if(ItePlugin.HasDiscord)
                {
                    DiscordController.Instance.enabled = false;
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
                    DiscordController.Instance.enabled = true;
                }
            }
        }

        // disconnecting
        public void DisconnectFromHostServer()
        {
            if(ClientToHost != null)
            {
                ClientToHost.Close();
                ClientToHost.Interface = null;
                ClientToHost = null;
                Logs.Info("Disconnected from host socket", this);
                IsClientSocket = false;
            }
        }
        public void StopHostServer()
        {
            if(!IsHostSocket)
            {
                return;
            }
            foreach (var client in ServerInstance.Connected)
            {
                client.Close();
            }
            if (ServerInstance != null)
            {
                ServerInstance.Close();
                ServerInstance.Interface = null;
                ServerInstance = null;
                IsHostSocket = false;
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
                if(!players.ContainsKey(member.Id.Value))
                {
                    NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                    players.Add(member.Id.Value, newPlr);
                }
                DisplaySystemChatMessage(GetNameOfId(member.Id, true) + " has joined this lobby");
                PacketWriter w = new PacketWriter();
                w.WriteULong(member.Id);
                foreach (var member1 in CurrentLobby.Members)
                {
                    if (member1.Id != NetworkManager.Id && member1.Id != member.Id)
                        SendPacket(PacketType.Join, w.GetBytes(), member1.Id);
                }
                // from raw id to Net.Dev
                if (Net.Dev(member.Id))
                {
                    ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("DevJoin"), Random.Range(0.95f, 1.15f), CameraController.Instance.transform, 1f);
                }
                PlayerList.UpdatePList();
            }
        }

        public void HandleMemberJoinedNet(Friend member)
        {
            OnPlayerJoined?.Invoke(member, member.Id);
            if (!players.ContainsKey(member.Id.Value))
            {
                NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                players.Add(member.Id.Value, newPlr);
            }
            DisplaySystemChatMessage(GetNameOfId(member.Id, true) + " has joined this lobby");
            HostAndConnected = AmIHost();
            ClientAndConnected = !AmIHost();
            InLobby = CurrentLobby.Id != 0;
            if(Net.Dev(member.Id))
            {
                ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("DevJoin"), Random.Range(0.95f, 1.15f), CameraController.Instance.transform, 1f);
            }
            PlayerList.UpdatePList();
        }
        public void ConnectionNet(ulong id)
        {
            if(Id == id)
            {
                return;
            }
            DisplayJoin("green", $"{GetNameOfId(id, true)} has connected this server");
            // share skin aswell
            PacketWriter write = new PacketWriter();
            write.WriteSkin(ItePlugin.currentSkin);
            SendPacket(PacketType.Skin, write.GetBytes(), id);
        }
        public void DisconnectionNet(ulong id)
        {
            if(Id == id)
            {
                return;
            }
            DisplayJoin("red", $"{GetNameOfId(id, true)} has disconnected from this server");
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
                if (players.ContainsKey(member.Id.Value))
                {
                    ItePlugin.LeaveEffect(players[member.Id.Value].transform.position);
                    Destroy(players[member.Id.Value].gameObject);
                    players.Remove(member.Id.Value);
                }
                DisplaySystemChatMessage(GetNameOfId(member.Id, true) + " has left this lobby");
                PacketWriter write = new PacketWriter();
                write.WriteULong(member.Id);
                foreach (var member1 in CurrentLobby.Members)
                {
                    if (member1.Id != NetworkManager.Id && member1.Id != member.Id)
                        SendPacket(PacketType.Left, write.GetBytes(), member1.Id);
                }
                PlayerList.UpdatePList();
                ItePlugin.DoubleCheckForSoftlock();
                if (Voice.idToSource.ContainsKey(member.Id))
                {
                    Voice.Remove(member.Id);
                }
            }
        }
        public static void LocPlayerCheck()
        {
            try
            {
                if (NetworkPlayer.LocalPlayer == null && NetworkPlayer.hadLocPlr)
                {
                    foreach (var plr in FindObjectsOfType<NetworkPlayer>(true))
                    {
                        if (plr.SteamId == Id)
                        {
                            NetworkPlayer.LocalPlayer = plr;
                        }
                    }
                }
                if(NetworkPlayer.LocalPlayer.updatePos != null)
                {
                    NetworkPlayer.LocalPlayer.StopCoroutine(NetworkPlayer.LocalPlayer.updatePos);
                    NetworkPlayer.LocalPlayer.updatePos = null;
                }
                NetworkPlayer.LocalPlayer.updatePos = NetworkPlayer.LocalPlayer.StartCoroutine(NetworkPlayer.LocalPlayer.UpdatePos());
            }
            catch { }
        }

        public void HandleMemberLeftNet(Friend member)
        {
            OnPlayerLeft?.Invoke(member, member.Id);
            if (players.ContainsKey(member.Id.Value))
            {
                ItePlugin.LeaveEffect(players[member.Id.Value].transform.position);
                Destroy(players[member.Id.Value].gameObject);
                players.Remove(member.Id.Value);
            }
            DisplaySystemChatMessage(GetNameOfId(member.Id, true) + " has left this lobby");
            HostAndConnected = AmIHost();
            ClientAndConnected = !AmIHost();
            InLobby = CurrentLobby.Id != 0;
            PlayerList.UpdatePList();
            ItePlugin.DoubleCheckForSoftlock();
            if (Voice.idToSource.ContainsKey(member.Id))
            {
                Voice.Remove(member.Id);
            }
        }

        private void HandleLobbyInvite(Lobby lobby, SteamId id)
        {
            if (SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap" || SceneHelper.CurrentScene == "Main Menu")
            {
                DisplayError("You must be in a level before joining someone.");
                return;
            }
            if (!SteamClient.IsValid) return;
            DisplaySystemChatMessage("Attempting to join " + GetNameOfId(id, true) + "'s game (via invite)");
            JoinLobby(lobby.Id).Forget();
        }

        private void HandleLobbyRPJ(Friend friend, string connect)
        {
            if(SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap" || SceneHelper.CurrentScene == "Main Menu")
            {
                DisplayError("You must be in a level before joining someone.");
                return;
            }
            if (!SteamClient.IsValid) return;
            DisplaySystemChatMessage("Attempting to join " + GetNameOfId(friend.Id, true) + "'s game (via profile)");
            if (ulong.TryParse(connect, out var lobbyId))
            {
                JoinLobby(lobbyId).Forget();
            }
        }
        public void BroadcastPacket(PacketType type, byte[] data, ulong owner = 0, ushort sendtype = 0)
        {
            if (CurrentLobby.Id == 0 || !SteamClient.IsValid) return;

            if(ClientAndConnected)
            {
                // funnel to host
                SendToHost(type, data, true, sendtype);
                return;
            }

            foreach (var member in CurrentLobby.Members)
            {
                if (member.Id != Id && member.Id != owner)
                {
                    SendPacket(type, data, member.Id.Value, owner, sendtype, true);
                }
            }
        }

        public void SendPacket(PacketType type, byte[] data, ulong targetId, ulong owner = 0, ushort sendtype = SendTypeConsts.ST_DEFAULT, bool broadcasting = false)
        {
            if(!IsConnectedSocket)
            {
                return;
            }
            if(owner == 0)
            {
                owner = Id;
            }

            PacketWriter w = new PacketWriter();
            w.WriteByte((byte)type);
            w.WriteULong(owner);
            w.WriteInt(data.Length);
            w.WriteUShort(sendtype);
            w.WriteBool(broadcasting);
            w.WriteULong(targetId);
            w.WriteBytes(data);

            byte[] bytes = w.GetBytes();
            if (ClientAndConnected)
            {
                ClientToHost.Connection.SendMessage(bytes, GetSendType(sendtype));
            }
            if(HostAndConnected)
            {
                if (connections.TryGetValue(targetId, out var con))
                {
                    con.SendMessage(bytes, GetSendType(sendtype));
                }
            }
        }

        public SendType GetSendType(ushort sendtype)
        {
            switch(sendtype)
            {
                case 0:
                    return SendType.Reliable;
                case 1:
                case 2:
                case 3:
                    return SendType.Unreliable;
            }
            return SendType.Reliable;
        }

        // send to host only
        public void SendToHost(PacketType type, byte[] payload, bool broadcasting, ushort sendtype = SendTypeConsts.ST_DEFAULT)
        {
            if (CurrentLobby.Owner.Id == NetworkManager.Id) return;
            SendPacket(type, payload, CurrentLobby.Owner.Id, sendtype: sendtype, broadcasting: broadcasting);
        }

        public void LoadLevelAndDifficulty(Lobby? lobby)
        {
            if(lobby.HasValue)
            {
                SceneLoading = true;
                ItePlugin.ignoreSpectate = true;
                SceneHelper.LoadScene(lobby.Value.GetData("level"));
                PrefsManager.Instance.SetInt("difficulty", int.Parse(lobby.Value.GetData("difficulty")));
            }
        }
        public void SceneLoad()
        {
            StartCoroutine(OnSceneLoad());
        }
        private IEnumerator OnSceneLoad()
        {
            yield return new WaitForSeconds(0.25f);
            SceneLoading = false;
            ItePlugin.cameFromPacketRestart = false;
            SceneObjectCache.Rebuild();
        }
        public void JoinAnnounce(ConnectionInfo info)
        {
            StartCoroutine(DelayJoinAnnounce(info));
        }
        private IEnumerator DelayJoinAnnounce(ConnectionInfo info)
        {
            yield return new WaitForSeconds(0.5f);
            PacketWriter w = new PacketWriter();
            w.WriteSkin(ItePlugin.currentSkin);
            BroadcastPacket(PacketType.Skin, w.GetBytes());

            PacketWriter w2 = new PacketWriter();
            w2.WriteULong(info.Identity.SteamId);
            BroadcastPacket(PacketType.GlobalConnectionJoin, w2.GetBytes());
        }
        public void JoinAnnounceClient()
        {
            StartCoroutine(DelayJoinAnnounceClient());
        }
        private IEnumerator DelayJoinAnnounceClient()
        {
            yield return new WaitForSeconds(0.45f);
            PacketWriter w = new PacketWriter();
            w.WriteSkin(ItePlugin.currentSkin);
            BroadcastPacket(PacketType.Skin, w.GetBytes());
        }
        public static ulong GetNearestPlayerID(Vector3 pos)
        {
            NetworkPlayer closest = null;
            float closestDist = float.MaxValue;
            foreach (var player in players.Values)
            {
                float dist = Vector3.Distance(player == NetworkPlayer.LocalPlayer ? MonoSingleton<NewMovement>.Instance.transform.position : player.transform.position, pos);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = player;
                }
            }
            return closest.SteamId;
        }

        void Update()
        {
            if (!SteamClient.IsValid) return;
            Sandbox = SceneHelper.CurrentScene == "uk_construct";
            if(InLobby)
            {
                bool isHost = AmIHost();
                HostAndConnected = isHost;
                ClientAndConnected = !isHost;
            }
            else
            {
                HostAndConnected = false;
                ClientAndConnected = false;
            }
            SteamClient.RunCallbacks();
            if (ServerInstance != null)
            {
                ServerInstance.Receive();
            }
            if(ClientToHost != null)
            {
                ClientToHost.Receive();
            }
        }
        public bool IsBanned(NetIdentity id)
        {
            if (id.SteamId == 0) return false;
            if (CurrentLobby.Id == 0) return false;

            return CurrentLobby.GetData("banned_" + id.SteamId) == "1";
        }
        public void ChangeLobbySettings(int maxPlayers, LobbyType newLobbyType, bool allowCheats, string newLobbyName)
        {
            PacketWriter w = new PacketWriter();
            if (currentType != newLobbyType)
            {
                SetLobbyType(newLobbyType);
                currentTypeRaw = ItePlugin.Instance.LobbyTypeToRaw(newLobbyType);
                w.WriteEnum<LobbyType>(currentType);
            }
            else
            {
                w.WriteEnum<LobbyType>(LobbyType.None);
            }
            if (CurrentLobby.GetData("LobbyName") != newLobbyName)
            {
                ChangeLobName(newLobbyName);
                currentLobbyName = newLobbyName;
                w.WriteString(newLobbyName);
            }
            else
            {
                w.WriteString("0");
            }
            if(CurrentLobby.MaxMembers != maxPlayers)
            {
                ChangeMaxMembers(maxPlayers);
                currentMaxPlayers = maxPlayers;
                w.WriteInt(maxPlayers);
            }
            else
            {
                w.WriteInt(0);
            }
            string canCheat = (allowCheats) ? "1" : "0";
            if (CurrentLobby.GetData("cheat") != canCheat)
            {
                ChangeCheats(allowCheats);
                currentCheats = (allowCheats) ? 1 : 0;
                w.WriteString(canCheat);
            }
            else
            {
                w.WriteString("null");
            }
            BroadcastPacket(PacketType.LobbySettings, w.GetBytes());
        }
        public void ChangeLobName(string newN)
        {
            if (string.IsNullOrEmpty(newN))
            {
                newN = $"{GetNameOfId(Id)}'s Lobby";
                ItePlugin.Instance.polrMM.lobbyName.text = newN;
            }
            CurrentLobby.SetData("LobbyName", newN);
            DisplaySystemChatMessage("Lobby name has been changed to: " + newN);
        }
        public void ChangeMaxMembers(int newM)
        {
            if(newM > 250)
            {
                newM = 250;
                ItePlugin.Instance.polrMM.maxP.text = "250";
            }
            int memberCountSnap = CurrentLobby.MemberCount;
            if(newM <= 0)
            {
                LeaveLobby(true);
            }
            if(memberCountSnap > newM)
            {
                for (int i = memberCountSnap; i > newM; i--)
                {
                    ForceKick(CurrentLobby.Members.Last().Id, "Exceeding max player count");
                }
            }
            CurrentLobby.MaxMembers = newM;
            DisplaySystemChatMessage("Lobby max player limit has been changed to: " + newM);
        }
        public void ChangeCheats(bool allow)
        {
            CurrentLobby.SetData("cheat", (allow) ? "1" : "0");
            DisplaySystemChatMessage("Allow cheats has been changed to: " + CurrentLobby.GetData("cheat"));
        }
        public void SetLobbyType(LobbyType newT)
        {
            if(newT == LobbyType.Public && !ItePlugin.ReleaseBuild)
            {
                DisplayError("You can't set this lobby to Public due to being on a beta build.");
                return;
            }
            switch(newT)
            {
                case LobbyType.Public:
                    CurrentLobby.SetPublic();
                    break;
                case LobbyType.FriendsOnly:
                    CurrentLobby.SetFriendsOnly();
                    break;
                case LobbyType.Private:
                    CurrentLobby.SetPrivate();
                    break;
            }
            DisplaySystemChatMessage("Lobby has been set to: " + newT.ToString());
            currentType = newT;
        }
        public static ulong GetHostID()
        {
            if (!InLobby) return 0;
            return Instance.CurrentLobby.Owner.Id;
        }
        public void HandlePack(byte[] buff, SteamId id)
        {
            int length = buff.Length;
            PacketType globalType = PacketType.None;

            try
            {
                if (IsBanned(id) && connections.TryGetValue(id, out var con))
                {
                    ForceKick(id, "Bypassing ban");
                    return;
                }
                BinaryPacketReader reader = new BinaryPacketReader(buff, length);

                PacketType type = (PacketType)reader.ReadByte();
                globalType = type;
                ulong sender = reader.ReadULong();
                int len = reader.ReadInt();
                ushort sendtype = reader.ReadUShort();
                bool broadcasting = reader.ReadBool();
                ulong target = reader.ReadULong();
                byte[] data = reader.ReadBytes();

                if (sender == Id)
                    return;
                if (sender != id)
                    return;

                if(ItePlugin.logPacketParsing.value)
                    Logs.Info($"Parsed packet from {GetNameOfId(id)} ({id.Value}), type: {type}, length: {len}, st: {sendtype}", name: "Server");

                // voice packets
                if(type == PacketType.Voice)
                {
                    BinaryPacketReader vcReader = new BinaryPacketReader(data, data.Length);
                    byte[] buffer = vcReader.ReadByteArray();
                    if (length > 0 && buffer[0] == 0x56)
                    {
                        VoiceChatManager.Instance?.OnDataReceived(buffer, buffer.Length, id);
                        return;
                    }
                }

                if (HostAndConnected)
                {
                    // send it to everyone or send it to the target the client wanted to send it to
                    if (broadcasting)
                        BroadcastPacket(type, data, sender, sendtype);
                    else
                        SendPacket(type, data, target, sender, sendtype);
                }

                Handle(type, data, length, sender);
            }
            catch (Exception ex)
            {
                Logs.Error($"[NET] Failed to parse packet on host with type {globalType}: " + ex, this);
            }
        }
        public void HandlePackClient(byte[] buff)
        {
            int length = buff.Length;
            BinaryPacketReader reader = new BinaryPacketReader(buff, length);
            PacketType type = (PacketType)reader.ReadByte();
            ulong sender = reader.ReadULong();
            int len = reader.ReadInt();
            ushort sendtype = reader.ReadUShort();
            bool broadcasting = reader.ReadBool();
            ulong target = reader.ReadULong();
            byte[] data = reader.ReadBytes();

            try
            {
                if (CurrentLobby.GetData("banned_" + Id) == "1")
                {
                    LeaveLobby();
                    return;
                }
                if (sender == Id)
                    return;

                if(ItePlugin.logPacketParsing.value)
                    Logs.Info($"Parsed packet from {GetNameOfId(sender)} ({sender}), type: {type}, length: {len}, st: {sendtype}", name: "Client");

                // voice packets
                if (type == PacketType.Voice)
                {
                    BinaryPacketReader vcReader = new BinaryPacketReader(data, data.Length);
                    byte[] buffer = vcReader.ReadByteArray();
                    if (length > 0 && buffer[0] == 0x56)
                    {
                        VoiceChatManager.Instance?.OnDataReceived(buffer, buffer.Length, sender);
                        return;
                    }
                }

                Handle(type, data, length, sender);
            }
            catch (Exception ex)
            {
                Logs.Error($"[NET] Failed to parse packet on client with type {type}: " + ex, this);
            }
        }
        public void CreateTestPlayer()
        {
            if (!players.ContainsKey(NetworkManager.Id))
            {
                NetworkPlayer newPlr = NetworkPlayer.Create(NetworkManager.Id, GetNameOfId(NetworkManager.Id));
                players.Add(NetworkManager.Id, newPlr);
                newPlr.testPlayer = true;
            }
        }
        public NetworkPlayer CreateLocalPlayer()
        {
            if(NetworkPlayer.LocalPlayer == null)
            {
                NetworkPlayer newPlr = NetworkPlayer.Create(NetworkManager.Id, GetNameOfId(NetworkManager.Id));
                players.Add(NetworkManager.Id, newPlr);
                return newPlr;
            }
            return null;
        }
        public NetworkPlayer CreateFakePlayer()
        {
            NetworkPlayer newPlr = NetworkPlayer.Create((ulong)players.Count + 1, GetNameOfId(SteamFriends.GetFriends().ToArray()[Random.Range(0, SteamFriends.GetFriends().ToArray().Length)].Id));
            players.Add((ulong)(players.Count + 1), newPlr);
            return newPlr;
        }
        public static void DisplaySystemChatMessage(string msg)
        {
            if(ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=yellow>[SYSTEM]: {msg}</color>", false, $"<color=yellow>[SYSTEM]: {msg}</color>", tts: false);
                ChatUI.Instance.ShowUIForBit();
            }
        }
        public static void DisplayWarningChatMessage(string msg)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=orange>[WARNING]: {msg}</color>", false, $"<color=orange>[WARNING]: {msg}</color>", tts: false);
                ChatUI.Instance.ShowUIForBit();
            }
        }
        public static void DisplayGameChatMessage(string msg)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=grey>{msg}</color>", false, $"<color=grey>{msg}</color>", tts: false);
                ChatUI.Instance.ShowUIForBit(7f);
            }
        }
        public static void ShoutCheckpoint(string whoCheckpointed)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=#e96bff>{whoCheckpointed} has reached a checkpoint.</color>", false, $"<color=#e96bff>{whoCheckpointed} has reached a checkpoint.</color>", tts: false);
                ChatUI.Instance.ShowUIForBit(5f);
            }
        }
        public static void ShoutCheater(string whoCheated)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=#2eff69>{whoCheated} activated cheats.</color>", false, $"<color=#2eff69>{whoCheated} activated cheats.</color>", tts: false);
                ChatUI.Instance.ShowUIForBit(7f);
            }
        }
        public static void DisplayError(string errorMsg)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color=red>[ERROR]: {errorMsg}</color>", false, $"<color=red>[ERROR]: {errorMsg}</color>", tts: false);
                ChatUI.Instance.ShowUIForBit(7f);
            }
        }
        public static void DisplaySkinTip()
        {
            if (ChatUI.Instance != null && !ItePlugin.disableSkinTip.value)
            {
                ChatUI.Instance.OnSubmitMessage("<color=#5EF527>[TIP]: You can change your skin in the plugin configurator! (Settings > Plugin Config > Cosmetic Config > Skins Config) (you can also disable this tip in the plugin configurator)</color>", false, "<color=#5EF527>[TIP]: You can change your skin in the plugin configurator! (Config > Cosmetic Config > Skins Config) (you can also disable this tip in the plugin configurator)</color>", tts: false);
                ChatUI.Instance.OnSubmitMessage($"<color=#27E7F5>[TIP]: You can see what you look like for other people by holding down {ChatUI.GetKeyName(ItePlugin.previewSkin.value)}</color>", false, $"<color=#27E7F5>[TIP]: You can see what you look like for other people by holding down {ChatUI.GetKeyName(ItePlugin.previewSkin.value)}</color>", tts: false);
                ChatUI.Instance.ShowUIForBit(7f);
            }
        }

        public static void DisplayJoin(string color, string msg)
        {
            if (ChatUI.Instance != null)
            {
                ChatUI.Instance.OnSubmitMessage($"<color={color}>[SERVER]: {msg}</color>", false, $"<color={color}>[LOBBY]: {msg}</color>", tts: false);
                ChatUI.Instance.ShowUIForBit(5f);
            }
        }


        // mapping helpers
        public static bool Contains(ulong id) => connections.ContainsKey(id);
        public static void AddMapping(Connection connection, ulong id)
        {
            Logs.Debug("Adding mapping for user: " + id, typeof(NetworkManager));
            if (!Contains(id)) connections.Add(id, connection);
        }
        public static void RemoveMapping(Connection connection, ulong id)
        {
            Logs.Debug("Removing mapping for user: " + typeof(NetworkManager));
            if (Contains(id)) connections.Remove(id);
        }
    }
}
