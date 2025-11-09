using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;
using BepInEx.Logging;

using Discord;

using HarmonyLib;

using PluginConfig.API;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;

using Polarite.Multiplayer;
using Polarite.Patches;

using Steamworks;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using NetworkManager = Polarite.Multiplayer.NetworkManager;
using LobbyType = Polarite.Multiplayer.LobbyType;
using PluginConfig.API.Decorators;
using Logic;

namespace Polarite
{
    public enum SkinType
    {
        V1,
        V2
    }
    public enum VoiceMode
    {
        PushToTalk,
        VoiceActivation,
        ToggleToTalk
    }

    public enum VoiceQuality
    {
        Low,
        Medium,
        High
    }

    [BepInPlugin("com.d1g1tal.polarite", "Polarite", "1.0.1")]
    public class ItePlugin : BaseUnityPlugin
    {
        public static PluginConfigurator config = PluginConfigurator.Create("Polarite Config", "com.d1g1tal.polarite");

        public static ConfigPanel mainGameRelated = new ConfigPanel(config.rootPanel, "Gameplay Config", "gameplay");

        public static ConfigPanel voiceRelated = new ConfigPanel(config.rootPanel, "Voice Config", "voice");

        public static ConfigPanel cosmeticRelated = new ConfigPanel(config.rootPanel, "Cosmetic Config", "cosmetic");

        public static BoolField bossHpIncrease = new BoolField(mainGameRelated, "Increase boss hp by player count", "g.b", true);

        public static FloatField bossHpMult = new FloatField(mainGameRelated, "Boss hp increase multiplier", "g.bm", 1.5f);

        public static KeyCodeField buttonToChat = new KeyCodeField(config.rootPanel, "Open chat key", "chat.key", KeyCode.T);

        public static ConfigHeader vcheaderstuff = new ConfigHeader(voiceRelated, "Stuff related to Voice Chat");

        public static KeyCodeField voicePushToTalk = new KeyCodeField(voiceRelated, "Push-to-talk key", "voice.ptt", KeyCode.V);

        // voice chat stuff
        // made by doomahreal

        public static EnumField<VoiceMode> voiceMode = new EnumField<VoiceMode>(voiceRelated, "Voice mode", "voice.mode", VoiceMode.PushToTalk);

        // voice quality setting
        public static EnumField<VoiceQuality> voiceQuality = new EnumField<VoiceQuality>(voiceRelated, "Voice quality", "voice.quality", VoiceQuality.High); // medium didn't sound good to other people

        public static IntField volume = new IntField(voiceRelated, "Volume", "voice.volume", 25);

        // which microphone index to use (0 = first device)
        public static IntField voiceMicIndex = new IntField(voiceRelated, "Microphone index", "voice.mic", 0);

        public static ConfigHeader wheresMyMic = new ConfigHeader(voiceRelated, "0: ");

        // voice activation sensitivity (linear 0-100)
        public static IntField voiceVADThreshold = new IntField(voiceRelated, "Voice activation threshold (0-100)", "voice.vad", 30);

        // whether to receive/hear voice chat
        public static BoolField receiveVoice = new BoolField(voiceRelated, "Receive voice chat", "voice.receive", true);

        // proximity distance for voice in world units
        public static FloatField voiceProximity = new FloatField(voiceRelated, "Voice proximity range", "voice.range", 15f);

        public static EnumField<SkinType> skin = new EnumField<SkinType>(cosmeticRelated, "Player skin (only others can see)", "player.skin", SkinType.V1);

        public static ConfigHeader ttsbad = new ConfigHeader(cosmeticRelated, "<color=yellow>TTS can crash the game!</color>");

        public static BoolField canTTS = new BoolField(cosmeticRelated, "Play TTS on chat message", "tts", false);

        internal readonly Harmony harm = new Harmony("com.d1g1tal.polarite");

        internal static ManualLogSource log = new ManualLogSource("Polarite");

        public static ItePlugin Instance;

        public static bool ignoreSpectate = false;

        public static AssetBundle mainBundle;

        public static GameObject currentUi;

        public static GameObject leaveButton, joinButton, hostButton, copyButton, inviteButton, playerListButton;

        public static Discord.Discord discord;

        public static bool HasDiscord;

        public static readonly string[] PathsToSoftlocks = new string[]
        {
            "Door (Large) With Controllers (9)/LockedDoorBlocker",
            "V2 - Arena/V2 Stuff(Clone)/Door",
            "4 - Heart Chamber/4 Stuff(Clone)/Door",
            "Main Section/9 - Boss Arena/Boss Stuff(Clone)/IntroObjects/WallCollider",
            "Main Section/Outside/2-4/2-4 Stuff(Clone)(Clone)/GlassDoor (Skull)",
            "2 - Organ Hall/2 Stuff(Clone)/Door",
            "Exteriors/14/Cube",
            "Exteriors/Armboy/Cube",
            "3 - Fuckatorium/3 Stuff(Clone)/EntranceCloser",
            "Main/Exterior/ExteriorStuff(Clone)/SecuritySystemFight/ArenaWalls",
            "Main/Interior/InteriorStuff(Clone)(Clone)/BrainFight/EntryForceField"
        };


        public void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            harm.PatchAll();
            NetworkManager netManager = gameObject.GetComponent<NetworkManager>();
            if (netManager == null)
            {
                netManager = gameObject.AddComponent<NetworkManager>();
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            config.SetIconWithURL("file://" + Path.Combine(Directory.GetParent(Info.Location).FullName, "globehaha.png"));
            skin.postValueChangeEvent += (SkinType skin) =>
            {
                if(NetworkManager.InLobby)
                {
                    PacketWriter write = new PacketWriter();
                    write.WriteEnum(ItePlugin.skin.value);
                    NetworkManager.Instance.BroadcastPacket(PacketType.Skin, write.GetBytes());
                    if (NetworkPlayer.LocalPlayer.testPlayer)
                    {
                        NetworkPlayer.LocalPlayer.UpdateSkin((int)skin);
                    }
                }
            };
            bossHpIncrease.postValueChangeEvent += (bool v) =>
            {
                if(NetworkManager.HostAndConnected)
                {
                    NetworkManager.Instance.CurrentLobby.SetData("bh", (v) ? "1" : "0");
                }
            };
            bossHpMult.postValueChangeEvent += (float v) =>
            {
                if (NetworkManager.HostAndConnected)
                {
                    NetworkManager.Instance.CurrentLobby.SetData("bhm", v.ToString());
                }
            };
            mainBundle = AssetBundle.LoadFromFile(Path.Combine(Directory.GetParent(Info.Location).FullName, "polariteassets.bundle"));
            TryRunDiscord();
        }
        public void OnApplicationQuit()
        {
            discord.Dispose();
        }
        public bool TryRunDiscord()
        {
            try
            {
                discord = new Discord.Discord(1432308384798867456, 1uL);
                HasDiscord = true;
                return true;
            }
            catch
            {
                log.LogWarning("User doesn't have discord in the background, Skipping discord!");
                return false;
            }
        }

        public void Update()
        {
            if(HasDiscord)
            {
                discord.RunCallbacks();
            }
            if(currentUi != null && SceneHelper.CurrentScene != "Main Menu")
            {
                currentUi.SetActive(MonoSingleton<OptionsManager>.Instance.paused);
                if (currentUi.GetComponentInChildren<PolariteMenuManager>().mainPanel.activeSelf && MonoSingleton<OptionsManager>.Instance.paused && MonoSingleton<OptionsManager>.Instance.pauseMenu != null)
                {
                    TogglePauseMenu(false);
                }
                if (!currentUi.GetComponentInChildren<PolariteMenuManager>().mainPanel.activeSelf && MonoSingleton<OptionsManager>.Instance.paused && MonoSingleton<OptionsManager>.Instance.pauseMenu != null)
                {
                    TogglePauseMenu(true);
                }
                if(!MonoSingleton<OptionsManager>.Instance.paused)
                {
                    currentUi.GetComponentInChildren<PolariteMenuManager>().mainPanel.SetActive(false);
                }
                PolariteMenuManager pMM = currentUi.GetComponentInChildren<PolariteMenuManager>();
                if (pMM != null)
                {
                    string status = (NetworkManager.InLobby) ? "STATUS: <color=green>IN LOBBY" : "STATUS: <color=red>NOT IN LOBBY";
                    pMM.statusHost.text = status;
                    pMM.statusJoin.text = status;
                    leaveButton.SetActive(NetworkManager.InLobby);
                    playerListButton.SetActive(NetworkManager.InLobby);
                    joinButton.GetComponent<Button>().interactable = !NetworkManager.InLobby;
                    hostButton.GetComponent<Button>().interactable = !NetworkManager.InLobby;

                    inviteButton.SetActive(NetworkManager.HostAndConnected);
                    copyButton.SetActive(NetworkManager.HostAndConnected);
                }
            }
            if(currentUi != null && SceneHelper.CurrentScene == "Main Menu")
            {
                currentUi.SetActive(false);
            }
            if(currentUi != null)
            {
                PolariteMenuManager pMM = currentUi.GetComponentInChildren<PolariteMenuManager>();
                pMM.uiOpen.interactable = SceneHelper.CurrentScene != "Endless";
            }
        }
        public void TogglePauseMenu(bool value)
        {
            foreach(Transform c in MonoSingleton<OptionsManager>.Instance.pauseMenu.transform)
            {
                if(c.name != "Level Stats")
                {
                    c.gameObject.SetActive(value);
                }
            }
            MonoSingleton<OptionsManager>.Instance.pauseMenu.GetComponent<Image>().enabled = value;
        }

        private void CreatePolariteUI()
        {
            GameObject uiObj = Instantiate(mainBundle.LoadAsset<GameObject>("PolariteCanvas"));
            if(uiObj != null)
            {
                DontDestroyOnLoad(uiObj);
                currentUi = uiObj;

                PolariteMenuManager pMM = currentUi.transform.Find("PolariteMenu").gameObject.AddComponent<PolariteMenuManager>();
                if(pMM != null)
                {
                    pMM.uiOpen = pMM.transform.Find("Activate").GetComponent<Button>();
                    pMM.uiOpen.onClick.AddListener(pMM.ToggleMainPanel);

                    Transform host = pMM.transform.Find("Main").Find("Host");
                    Transform join = pMM.transform.Find("Main").Find("Join");
                    Transform publicLobbies = pMM.transform.Find("Main").Find("PublicLobbies");
                    Transform playerList = pMM.transform.Find("Main").Find("PlayerList");

                    pMM.maxP = host.Find("MaxPlayers").GetComponent<TMP_InputField>();
                    pMM.lobbyName = host.Find("UsefulInputField").GetComponent<TMP_InputField>();
                    pMM.lobbyType = host.Find("UsefulDropdown").GetComponent<TMP_Dropdown>();
                    pMM.mainPanel = pMM.transform.Find("Main").gameObject;
                    pMM.code = join.Find("UsefulInputField").GetComponent<TMP_InputField>();
                    pMM.canCheat = host.Find("CanCheat").GetComponent<TMP_Dropdown>();

                    pMM.statusHost = host.Find("Status").GetComponent<TextMeshProUGUI>();
                    pMM.statusJoin = join.Find("Status").GetComponent<TextMeshProUGUI>();

                    Button leave = pMM.transform.Find("Main").Find("Leave").GetComponent<Button>();
                    Button invite = host.Find("UsefulButton (1)").GetComponent<Button>();
                    Button create = host.Find("UsefulButton").GetComponent<Button>();
                    Button joinL = join.Find("UsefulButton").GetComponent<Button>();
                    Button copyCode = host.Find("CopyCode").GetComponent<Button>();
                    Button onPublicClick = pMM.transform.Find("Main").Find("UsefulButton (3)").GetComponent<Button>();
                    Button refresh = publicLobbies.Find("RefreshButton").GetComponent<Button>();
                    Button pList = pMM.transform.Find("Main").Find("PlayerListButton").GetComponent<Button>();
                    Button discord = pMM.transform.Find("Main").Find("JoinDiscord").GetComponent<Button>();

                    leave.onClick.AddListener(NetworkManager.Instance.LeaveLobby);
                    invite.onClick.AddListener(NetworkManager.Instance.ShowInviteOverlay);
                    create.onClick.AddListener(CreateLobby);
                    joinL.onClick.AddListener(JoinLobby);
                    copyCode.onClick.AddListener(() =>
                    {
                        GUIUtility.systemCopyBuffer = pMM.codeHost;
                        NetworkManager.DisplaySystemChatMessage("Lobby code copied to clipboard");
                    });
                    discord.onClick.AddListener(() =>
                    {
                        Application.OpenURL("https://discord.gg/2jzJ9XWbS4");
                    });
                    onPublicClick.onClick.AddListener(PublicLobbyManager.RefreshLobbies);
                    refresh.onClick.AddListener(PublicLobbyManager.RefreshLobbies);
                    pList.onClick.AddListener(PlayerList.UpdatePList);

                    PublicLobbyManager.Content = publicLobbies.Find("LobbyList").Find("Content");
                    PlayerList.ContentB = playerList.Find("List").Find("Content");

                    leaveButton = leave.gameObject;
                    joinButton = joinL.gameObject;
                    hostButton = create.gameObject;
                    copyButton = copyCode.gameObject;
                    inviteButton = invite.gameObject;
                    playerListButton = pList.gameObject;

                    leave.gameObject.SetActive(false);
                    invite.gameObject.SetActive(false);
                    host.gameObject.SetActive(false);
                    join.gameObject.SetActive(false);
                    publicLobbies.gameObject.SetActive(false);
                    copyCode.gameObject.SetActive(false);
                    playerList.gameObject.SetActive(false);
                    pList.gameObject.SetActive(false);
                    pMM.mainPanel.SetActive(false);

                    pMM.lobbyName.text = $"{NetworkManager.GetNameOfId(NetworkManager.Id)}'s Lobby";
                }
            }
        }
        private async void CreateLobby()
        {
            PolariteMenuManager pMM = currentUi.GetComponentInChildren<PolariteMenuManager>();
            int max = int.Parse(pMM.maxP.text);
            string lobbyName = pMM.lobbyName.text;
            LobbyType type;
            bool allowCheats = false;
            switch(pMM.lobbyType.value)
            {
                case 0:
                    type = LobbyType.Public;
                    break;
                case 1:
                    type = LobbyType.FriendsOnly;
                    break;
                case 2:
                    type = LobbyType.Private;
                    break;
                default:
                    type = LobbyType.Public;
                    break;
            }
            switch(pMM.canCheat.value)
            {
                case 0:
                    allowCheats = false;
                    break;
                case 1:
                    allowCheats = true;
                    break;
            }
            if(max > 250)
            {
                max = 250;
            }
            await NetworkManager.Instance.CreateLobby(max, type, lobbyName, (string c) =>
            {
                pMM.codeHost = c;
            }, allowCheats);
            string levelName = StockMapInfo.Instance.levelName;
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = SceneHelper.CurrentScene;
            }
            NetworkManager.Instance.CurrentLobby.SetData("levelName", levelName);
        }
        private async void JoinLobby()
        {
            PolariteMenuManager pMM = currentUi.GetComponentInChildren<PolariteMenuManager>();
            await NetworkManager.Instance.JoinLobbyByCode(pMM.code.text);
            NetworkManager.Instance.GetAllPlayersInLobby(NetworkManager.Instance.CurrentLobby, out SteamId[] ids, false);
            foreach (var id in ids)
            {
                if (!NetworkManager.players.ContainsKey(id.Value.ToString()))
                {
                    NetworkPlayer newPlr = NetworkPlayer.Create(id.Value, NetworkManager.GetNameOfId(id));
                    NetworkManager.players.Add(id.Value.ToString(), newPlr);
                }
            }
        }
        public static void CleanLevelOfSoftlocks()
        {
            foreach (string softlock in PathsToSoftlocks)
            {
                bool v2Patch = softlock == "V2 - Arena/V2 Stuff(Clone)/Door";
                Transform obj = FindByPath(SceneManager.GetActiveScene().GetRootGameObjects(), softlock);
                if (obj != null)
                {
                    if(v2Patch)
                    {
                        obj.GetComponent<Door>().Open(skull: true);
                        Destroy(obj.GetComponent<Door>());
                    }
                    else
                    {
                        if (obj.GetComponent<ForceOff>() == null)
                        {
                            obj.gameObject.AddComponent<ForceOff>();
                        }
                    }
                }
            }
        }

        private static Transform FindByPath(GameObject[] roots, string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject root in roots)
            {
                Transform match = Recurse(root.transform, parts, 0);
                if (match != null)
                {
                    return match;
                }
            }
            return null;
        }

        private static Transform Recurse(Transform current, string[] parts, int index)
        {
            if (current.name != parts[index])
            {
                return null;
            }
            if (index == parts.Length - 1)
            {
                return current;
            }
            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = Recurse(current.GetChild(i), parts, index + 1);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private void OnSceneLoaded(Scene args1, LoadSceneMode args2)
        {
            if (SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap")
            {
                return;
            }
            NetworkManager netManager = gameObject.GetComponent<NetworkManager>();
            if (netManager == null)
            {
                netManager = gameObject.AddComponent<NetworkManager>();
            }
            ChatUI ui = gameObject.GetComponent<ChatUI>();
            if (ui == null)
            {
                ui = gameObject.AddComponent<ChatUI>();
            }
            VoiceChatManager vcManager = gameObject.GetComponent<VoiceChatManager>();
            if (vcManager == null)
            {
                vcManager = gameObject.AddComponent<VoiceChatManager>();
            }
            if (MonoSingleton<CameraController>.Instance != null && MonoSingleton<CameraController>.Instance.GetComponent<SpectatorCam>() == null)
            {
                SpectatorCam cam = MonoSingleton<CameraController>.Instance.gameObject.AddComponent<SpectatorCam>();
            }
            Instance.StopAllCoroutines();
            ignoreSpectate = false;
            if(SpectatorCam.isSpectating)
            {
                SpectatorCam.isSpectating = false;
            }
            if(SceneHelper.CurrentScene != "Main Menu" && currentUi == null)
            {
                CreatePolariteUI();
            }
            /*
            if (NetworkManager.ClientAndConnected && NetworkManager.Instance.CurrentLobby.GetData("forceS") == "1")
            {
                SpectatePlayers();
                ignoreSpectate = true;
                FinalDoor door = FindObjectOfType<FinalDoor>();
                if (door != null)
                {
                    door.Open();
                }
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "forcespec"
                });
                NetworkManager.DisplaySystemChatMessage("You have been forced into spectate. (Reason: Host already opened level door, wait for next level)");
            }
            */
            SceneObjectCache.Initialize();
            NetworkEnemyBootstrap boot = gameObject.GetComponent<NetworkEnemyBootstrap>();
            if (boot == null)
            {
                boot = gameObject.AddComponent<NetworkEnemyBootstrap>();
            }
            NetworkEnemyBootstrap.AttachSyncScripts();
            if(NetworkManager.HostAndConnected)
            {
                string levelName = StockMapInfo.Instance.levelName;
                if (string.IsNullOrEmpty(levelName))
                {
                    levelName = SceneHelper.CurrentScene;
                }
                NetworkManager.Instance.CurrentLobby.SetData("levelName", levelName);
            }
            if(HasDiscord)
            {
                if (NetworkManager.HasRichPresence)
                {
                    discord.GetActivityManager().UpdateActivity(new Activity
                    {
                        ApplicationId = 1432308384798867456,
                        Details = $"Playing in: {NetworkManager.Instance.CurrentLobby.GetData("levelName")}, In Polarite Lobby ({NetworkManager.Instance.CurrentLobby.MemberCount}/{NetworkManager.Instance.CurrentLobby.MaxMembers})",
                        Instance = true
                    }, delegate { });
                }
                else
                {
                    string levelName = StockMapInfo.Instance.levelName;
                    if (string.IsNullOrEmpty(levelName))
                    {
                        levelName = SceneHelper.CurrentScene;
                    }
                    discord.GetActivityManager().UpdateActivity(new Activity
                    {
                        ApplicationId = 1432308384798867456,
                        Details = $"Playing in: {levelName}, Not In Lobby",
                        Instance = true
                    }, delegate { });
                }
            }
            NetworkPlayer.ToggleColsForAll(false);
            Instance.StartCoroutine(RestartCols());
            NetworkManager.WasUsed = NetworkManager.InLobby;
            if(NetworkManager.InLobby)
            {
                CleanLevelOfSoftlocks();
            }
        }
        public static void SpawnSound(AudioClip clip, float pitch, Transform parent, float volume)
        {
            AudioSource audioSource = new GameObject("Noise").AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.pitch = pitch;
            audioSource.volume = volume;
            audioSource.spatialBlend = 1f;
            audioSource.gameObject.AddComponent<RemoveOnTime>().time = clip.length;
            audioSource.transform.SetParent(parent, false);
            audioSource.transform.position = parent.position;
            audioSource.Play();
        }
        public static void SpectatePlayers()
        {
            if(ignoreSpectate)
            {
                return;
            }
            Instance.StartCoroutine(SpectatePlayersB());
        }

        public static IEnumerator SpectatePlayersB()
        {
            yield return new WaitForSeconds(1.25f);
            List<Transform> playerTransforms = new List<Transform>();
            SpectatorCam cam = MonoSingleton<CameraController>.Instance.GetComponent<SpectatorCam>();
            if (cam == null)
            {
                cam = MonoSingleton<CameraController>.Instance.gameObject.AddComponent<SpectatorCam>();
            }
            foreach(var p in NetworkManager.players)
            {
                if(p.Value != NetworkPlayer.LocalPlayer)
                {
                    playerTransforms.Add(p.Value.transform);
                }
            }
            cam.GetComponent<CameraController>().enabled = false;
            MonoSingleton<NewMovement>.Instance.DeactivatePlayer();
            cam.StartSpectating(playerTransforms);
            foreach(var door in FindObjectsOfType<Door>())
            {
                door.Open(skull: true);
            }
            MonoSingleton<MusicManager>.Instance.ForceStartMusic();
        }
        public static IEnumerator RestartCols()
        {
            yield return new WaitForSeconds(4f);
            NetworkPlayer.ToggleColsForAll(true);
        }
    }
}
