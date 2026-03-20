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

using Polarite.Networking;
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

        public event Action<Friend, SteamId> OnPlayerJoined;
        public event Action<Friend, SteamId> OnPlayerLeft;
        public Lobby CurrentLobby;

        public static bool HostAndConnected;
        public static bool ClientAndConnected;
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

        static Dictionary<ulong, string> nameCache = new Dictionary<ulong, string>();

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
            SetRichPresenceForLobby(default);
            LeaveLobby();
            SteamClient.Shutdown();
        }

        public static string GetNameOfId(ulong id)
        {
            if (!nameCache.TryGetValue(id, out var name))
            {
                name = TMPUtils.StripTMP(new Friend(id).Name);
                nameCache[id] = name;
            }
            return name;
        }
        public async Task CreateLobby(int maxPlayers = 4, LobbyType lobbyType = LobbyType.Public, string lobbyName = "My Lobby", Action<string> onJoin = null, bool canCheat = false)
        {
            if (!SteamClient.IsValid) return;
            if (InLobby) LeaveLobby();
            if (!ItePlugin.ReleaseBuild && lobbyType == LobbyType.Public)
            {
                DisplayError("You cannot make public lobbies on beta builds.");
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
                CurrentLobby.SetData("level", SceneHelper.CurrentScene);
                CurrentLobby.SetData("difficulty", PrefsManager.Instance.GetInt("difficulty").ToString());
                CurrentLobby.SetData("cheat", (canCheat) ? "1" : "0");
                CurrentLobby.SetData("bh", (ItePlugin.bossHpIncrease.value) ? "1" : "0");
                CurrentLobby.SetData("bhm", ItePlugin.bossHpMult.value.ToString());
                CurrentLobby.SetData("pvp", (ItePlugin.pvpOn.value) ? "1" : "0");
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
                DisplayVoiceTip();
                PauseMenuPatch.DisablePauseEffects();
                Net.Setup();
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
                    if (!players.ContainsKey(member.Id.Value))
                    {
                        NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                        players.Add(member.Id.Value, newPlr);
                    }
                }
                LoadLevelAndDifficulty(lobby);
                PacketWriter write = new PacketWriter();
                write.WriteEnum(ItePlugin.skin.value);
                BroadcastPacket(PacketType.Skin, write.GetBytes());
                PlayerList.UpdatePList();
                DisplayVoiceTip();
                PauseMenuPatch.DisablePauseEffects();
                Net.Setup();
                PrivateLobby = lobby.Value.GetData("priv") == "1";
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

        public void LeaveLobby()
        {
            if (CurrentLobby.Id == 0)
                return;

            if(HostAndConnected)
            {
                PacketWriter w = new PacketWriter();
                BroadcastPacket(PacketType.HostLeave, w.GetBytes());
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

            // devs can just bypass this :troll:
            if (!AmIHost() && !Net.Dev(Id))
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
                DisplaySystemChatMessage(GetNameOfId(member.Id) + " has joined this lobby");
                PacketWriter w = new PacketWriter();
                w.WriteULong(member.Id);
                foreach (var member1 in CurrentLobby.Members)
                {
                    if (member1.Id != NetworkManager.Id && member1.Id != member.Id)
                        SendPacket(PacketType.Join, w.GetBytes(), member1.Id);
                }
                EnsureP2PSessionWithAll();
                PacketWriter write = new PacketWriter();
                write.WriteEnum(ItePlugin.skin.value);
                BroadcastPacket(PacketType.Skin, write.GetBytes());
                // from raw id to Net.Dev
                if (Net.Dev(member.Id))
                {
                    ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("DevJoin"), Random.Range(0.95f, 1.15f), CameraController.Instance.transform, 1f);
                }
                PlayerList.UpdatePList();
            }
        }

        public void HandleMemberJoinedP2P(Friend member)
        {
            OnPlayerJoined?.Invoke(member, member.Id);
            if (!players.ContainsKey(member.Id.Value))
            {
                NetworkPlayer newPlr = NetworkPlayer.Create(member.Id.Value, GetNameOfId(member.Id));
                players.Add(member.Id.Value, newPlr);
            }
            DisplaySystemChatMessage(GetNameOfId(member.Id) + " has joined this lobby");
            EnsureP2PSessionWithAll();
            PacketWriter write = new PacketWriter();
            write.WriteEnum(ItePlugin.skin.value);
            BroadcastPacket(PacketType.Skin, write.GetBytes());
            HostAndConnected = AmIHost();
            ClientAndConnected = !AmIHost();
            InLobby = CurrentLobby.Id != 0;
            if(Net.Dev(member.Id))
            {
                ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("DevJoin"), Random.Range(0.95f, 1.15f), CameraController.Instance.transform, 1f);
            }
            PlayerList.UpdatePList();
        }

        public void EnsureP2PSessionWithAll()
        {
            if (CurrentLobby.Id == 0) return;
            foreach (var member in CurrentLobby.Members)
            {
                if (member.Id != NetworkManager.Id)
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
                if (players.ContainsKey(member.Id.Value))
                {
                    Destroy(players[member.Id.Value].gameObject);
                    players.Remove(member.Id.Value);
                }
                DisplaySystemChatMessage(GetNameOfId(member.Id) + " has left this lobby");
                PacketWriter write = new PacketWriter();
                write.WriteULong(member.Id);
                foreach (var member1 in CurrentLobby.Members)
                {
                    if (member1.Id != NetworkManager.Id && member1.Id != member.Id)
                        SendPacket(PacketType.Left, write.GetBytes(), member1.Id);
                }
                PlayerList.UpdatePList();
                if(CyberSync.Active)
                {
                    CyberSync.DoubleCheckForSoftlock();
                }
                if (Voice.idToSource.ContainsKey(member.Id))
                {
                    Voice.Remove(member.Id);
                }
            }
        }

        public void HandleMemberLeftP2P(Friend member)
        {
            OnPlayerLeft?.Invoke(member, member.Id);
            if (players.ContainsKey(member.Id.Value))
            {
                Destroy(players[member.Id.Value].gameObject);
                players.Remove(member.Id.Value);
            }
            DisplaySystemChatMessage(GetNameOfId(member.Id) + " has left this lobby");
            HostAndConnected = AmIHost();
            ClientAndConnected = !AmIHost();
            InLobby = CurrentLobby.Id != 0;
            PlayerList.UpdatePList();
            if (CyberSync.Active)
            {
                CyberSync.DoubleCheckForSoftlock();
            }
            if (Voice.idToSource.ContainsKey(member.Id))
            {
                Voice.Remove(member.Id);
            }
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
        /// <summary>
        /// Don't set the owner value to anything to make packet funnel to host
        /// </summary>
        public void BroadcastPacket(PacketType type, byte[] data, ulong owner = 0)
        {
            // fix infinite loop using the fact owner starts off at 0
            if (CurrentLobby.Id == 0 || !SteamClient.IsValid) return;

            if(ClientAndConnected && owner == 0)
            {
                // funnel to host
                SendToHost(type, data);
                return;
            }

            foreach (var member in CurrentLobby.Members)
            {
                if (member.Id != Id && member.Id != owner)
                {
                    SendPacket(type, data, member.Id.Value, owner);
                }
            }
        }

        public void SendPacket(PacketType type, byte[] data, ulong targetId, ulong owner = 0)
        {
            if(owner == 0)
            {
                owner = Id;
            }

            PacketWriter w = new PacketWriter();

            w.WriteByte((byte)type);
            w.WriteULong(owner);
            w.WriteInt(data.Length);
            w.WriteBytes(data);

            byte[] bytes = w.GetBytes();

            SteamNetworking.SendP2PPacket(targetId, bytes);
        }


        // send to host only
        public void SendToHost(PacketType type, byte[] payload)
        {
            if (CurrentLobby.Owner.Id == NetworkManager.Id) return;
            SendPacket(type, payload, CurrentLobby.Owner.Id);
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
            yield return null;
            yield return null;

            SceneLoading = false;
            SceneObjectCache.Rebuild();
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
            if (SceneLoading)
            {
                while (SteamNetworking.IsP2PPacketAvailable(out uint size))
                {
                    byte[] buff = new byte[size];

                    SteamId id = default;
                    SteamNetworking.ReadP2PPacket(buff, ref size, ref id);
                }
                return;
            }

            while (SteamNetworking.IsP2PPacketAvailable(out uint packetSize))
            {
                byte[] buff = new byte[packetSize];

                SteamId id = default;

                if (!SteamNetworking.ReadP2PPacket(buff, ref packetSize, ref id))
                    continue;

                // voice packets
                if (packetSize > 0 && buff[0] == 0x56)
                {
                    VoiceChatManager.Instance?.OnP2PDataReceived(buff, (int)packetSize, id);
                    continue;
                }

                try
                {
                    if (CurrentLobby.GetData("banned_" + id.Value) == "1")
                    {
                        LeaveLobby();
                        continue;
                    }

                    BinaryPacketReader reader = new BinaryPacketReader(buff, (int)packetSize);

                    PacketType type = (PacketType)reader.ReadByte();
                    ulong sender = reader.ReadULong();
                    int len = reader.ReadInt();
                    byte[] data = reader.ReadBytes();

                    if (sender == Id)
                        continue;
                    if (sender != id)
                        continue;

                    if(HostAndConnected)
                    {
                        // send it to everyone else
                        BroadcastPacket(type, data, sender);
                    }

                    Handle(type, data, (int)packetSize, sender);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Net] Failed to parse binary packet: " + ex);
                }
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

        public static void DisplayVoiceTip()
        {
            if(ChatUI.Instance != null && !ItePlugin.disableVoiceChatTip.value)
            {
                ChatUI.Instance.OnSubmitMessage("<color=#34eba4>[TIP]: You can setup proximity voice chat in the plugin configurator! (you can also disable this tip in the plugin configurator)</color>", false, "<color=#34eba4>[TIP]: You can setup proximity voice chat in the plugin configurator! (you can also disable this tip in the plugin configurator)</color>", tts: false);
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
