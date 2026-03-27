using BepInEx;
using BepInEx.Logging;
using Discord;
using HarmonyLib;
using Logic;
using PluginConfig.API;
using PluginConfig.API.Decorators;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using Polarite.Debugging;
using Polarite.Multiplayer;
using Polarite.Networking;
using Polarite.Patches;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using ULTRAKILL.Cheats;
using ULTRAKILL.Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LobbyType = Polarite.Multiplayer.LobbyType;
using NetworkManager = Polarite.Multiplayer.NetworkManager;

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

    [BepInPlugin("com.d1g1tal.polarite", "Polarite", "1.1.0")]
    public class ItePlugin : BaseUnityPlugin
    {
        public static readonly PluginConfigurator config = PluginConfigurator.Create("Polarite Config", "com.d1g1tal.polarite");

        public static ConfigPanel mainGameRelated = new ConfigPanel(config.rootPanel, "Gameplay Config", "gameplay");

        public static ConfigPanel voiceRelated = new ConfigPanel(config.rootPanel, "Voice Config", "voice");

        public static ConfigPanel cosmeticRelated = new ConfigPanel(config.rootPanel, "Cosmetic Config", "cosmetic");

        public static ConfigHeader hostControl = new ConfigHeader(mainGameRelated, "<color=orange>These values are controlled by the host.</color>");

        public static BoolField bossHpIncrease = new BoolField(mainGameRelated, "Increase boss hp by player count", "g.b", true);

        public static FloatField bossHpMult = new FloatField(mainGameRelated, "Boss hp increase multiplier", "g.bm", 1.3f);

        public static BoolField pvpOn = new BoolField(mainGameRelated, "PVP/Friendly fire", "pvp", false);

        public static KeyCodeField buttonToChat = new KeyCodeField(config.rootPanel, "Open chat key", "chat.key", KeyCode.T);

        public static ConfigHeader vcheaderstuff = new ConfigHeader(voiceRelated, "Stuff related to Voice Chat");

        public static KeyCodeField voicePushToTalk = new KeyCodeField(voiceRelated, "Push-to-talk key", "voice.ptt", KeyCode.V);

        // voice chat stuff
        // made by doomahreal

        public static EnumField<VoiceMode> voiceMode = new EnumField<VoiceMode>(voiceRelated, "Voice mode", "voice.mode", VoiceMode.PushToTalk);

        // voice quality setting
        public static EnumField<VoiceQuality> voiceQuality = new EnumField<VoiceQuality>(voiceRelated, "Voice quality", "voice.quality", VoiceQuality.High); // medium didn't sound good to other people

        public static BoolField disableVoiceChatTip = new BoolField(voiceRelated, "Disable voice chat tip message", "voice.tip.disable", false);

        // which microphone index to use (0 = first device)
        public static IntField voiceMicIndex = new IntField(voiceRelated, "Microphone index", "voice.mic", 0);

        public static ConfigHeader wheresMyMic = new ConfigHeader(voiceRelated, "0: ");

        // voice activation sensitivity (linear 0-100)
        public static IntField voiceVADThreshold = new IntField(voiceRelated, "Voice activation threshold (0-100)", "voice.vad", 30);

        // whether to receive/hear voice chat
        public static BoolField receiveVoice = new BoolField(voiceRelated, "Hear players voices", "voice.receive", true);

        public static EnumField<SkinType> skin = new EnumField<SkinType>(cosmeticRelated, "Player skin (only others can see)", "player.skin", SkinType.V1);

        public static ConfigHeader ttsbad = new ConfigHeader(cosmeticRelated, "<color=yellow>TTS can crash the game!</color>");

        public static BoolField canTTS = new BoolField(cosmeticRelated, "Play TTS on chat message", "tts", false);

        public static BoolField timeStopDisable = new BoolField(cosmeticRelated, "Disable timestop", "timestop.disable", false);

        public static KeyCodeField killbind = new KeyCodeField(config.rootPanel, "Killbind", "killbind", KeyCode.K);

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
            "2 - Organ Hall/2 Stuff(Clone)/Door",
            "Exteriors/14/Cube",
            "Exteriors/Armboy/Cube",
            "3 - Fuckatorium/3 Stuff(Clone)/EntranceCloser",
            "Main/Exterior/ExteriorStuff(Clone)/SecuritySystemFight/ArenaWalls",
            "Main/Interior/InteriorStuff(Clone)(Clone)/BrainFight/EntryForceField",
            "Door (Large) With Controllers (17)/LockedDoorBlocker",
            "Door (Large) With Controllers (18)/LockedDoorBlocker",
            "3 - First Encounter/3 Stuff(Clone)/Blockers",
            "4 - Heart Chamber/4 Stuff(Clone)/Backwall",
            "Main Section/Inside/8 - Elevator/8 Stuff/Hellgate 1/Door",
            "Main Section/Inside/8 - Elevator/8 Stuff/Hellgate 1/Door (1)",
            "Pre-Space/Rooms/7 - Heart Chamber/7 Stuff/Door/Door (1)",
            "Pre-Space/Rooms/7 - Heart Chamber/7 Nonstuff/Void/Backwall"
        };

        // background fx
        public Camera cam;
        public AudioLowPassFilter lowPass;

        public PolariteMenuManager polrMM;
        public static bool plrActive;
        public static bool cameFromPacketRestart;

        public static bool debugMode = false;
        public static bool debugSending = false;

        // rip catto mode value :(

        private static float killbindCooldown = 5f;

        public static bool immuneToDeath = false;

        public static readonly bool ReleaseBuild = false;
        public static readonly string Version = "v1.1.0-beta1";
        public static readonly string MOTD = "Welcome back, to slomp live!";

        public static TargetData playerData;


        public void Awake()
        {
            var asm = Assembly.GetExecutingAssembly();
            var stream = asm.GetManifestResourceStream("Polarite.Concentus.dll");
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            Assembly.Load(ms.ToArray());
            ms.Dispose();
            stream.Dispose();
            if (Instance == null)
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
            pvpOn.postValueChangeEvent += (bool v) =>
            {
                if (NetworkManager.HostAndConnected)
                {
                    NetworkManager.Instance.CurrentLobby.SetData("pvp", (v) ? "1" : "0");
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
            playerData = new TargetData();
        }
        public static void LogDebug(string msg, bool ignore = false)
        {
            if(debugMode || ignore)
            {
                if(SubtitleController.Instance.subtitlesEnabled)
                {
                    SubtitleController.Instance.DisplaySubtitle(msg);
                }
                else
                {
                    HudMessageReceiver.Instance.SendHudMessage(msg);
                }
            }
        }
        public void Start()
        {
            EntityStorage.StoreAll();
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
        private void Inputs()
        {
            if (!Net.Paused && Net.List != null && NetworkManager.InLobby)
            {
                Net.Tick();
            }
            if (debugMode && NetworkManager.InLobby)
            {
                NetworkPlayer lo = NetworkPlayer.LocalPlayer;
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    LogDebug($"[DEBUG] Currently there are {Net.List.Objects.Count} objects that are networked.");
                }
                else if (Input.GetKeyDown(KeyCode.F2))
                {
                    LogDebug($"[DEBUG] Teleported testing Dummy to your location! (Keep holding to make it follow)");
                    lo.testPlayer = true;
                    lo.ToggleRig(true);
                    lo.UpdateSkin((int)skin.value);
                }
                else if (Input.GetKeyDown(KeyCode.F1))
                {
                    if (NameTag.DisabledByDebug)
                    {
                        NameTag.DisabledByDebug = false;
                        foreach (var p in NetworkManager.players.Values)
                        {
                            p.NameTag.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        NameTag.DisabledByDebug = true;
                    }
                    LogDebug($"[DEBUG] Toggled name tags {!NameTag.DisabledByDebug}.");
                }
                if (!Input.GetKey(KeyCode.F2))
                {
                    lo.testPlayer = false;
                }
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                debugMode = !debugMode;
                LogDebug($"[POLARITE] Toggled debug mode {debugMode}.", true);
            }
            if (Input.GetKeyDown(KeyCode.F6) && debugMode)
            {
                debugSending = !debugSending;
                LogDebug($"[POLARITE] Toggled packet messages {debugSending}.");
            }
            if (Input.GetKeyDown(killbind.value) && NetworkManager.InLobby && MonoSingleton<NewMovement>.Instance.activated && MonoSingleton<NewMovement>.Instance.hp != 0 && killbindCooldown <= 0f && !immuneToDeath)
            {
                killbindCooldown = 5f;
                ForceKillSelf("killed themselves");
            }
            killbindCooldown -= Time.deltaTime;
        }
        public static void ForceKillSelf(string deathMsg = "died")
        {
            NewMovement nM = MonoSingleton<NewMovement>.Instance;
            DeadPatch.Death(deathMsg);
            nM.hp = 0;
            if (!nM.endlessMode)
            {
                nM.deathSequence.gameObject.SetActive(value: true);

                MonoSingleton<TimeController>.Instance.controlPitch = false;
                nM.screenHud.SetActive(value: false);
            }
            else
            {
                nM.GetComponentInChildren<FinalCyberRank>().GameOver();
                CrowdReactions crowdReactions = MonoSingleton<CrowdReactions>.Instance;
                if (crowdReactions != null)
                {
                    crowdReactions.React(crowdReactions.aww);
                }
            }

            nM.rb.constraints = RigidbodyConstraints.None;
            nM.rb.AddTorque(Vector3.right * -1f, ForceMode.VelocityChange);
            if ((bool)MonoSingleton<PowerUpMeter>.Instance)
            {
                MonoSingleton<PowerUpMeter>.Instance.juice = 0f;
            }

            nM.cc.enabled = false;
            if (nM.gunc == null)
            {
                nM.gunc = nM.GetComponentInChildren<GunControl>();
            }

            nM.gunc.NoWeapon();
            nM.rb.constraints = RigidbodyConstraints.None;
            nM.dead = true;
            nM.activated = false;
            if (nM.punch == null)
            {
                nM.punch = nM.GetComponentInChildren<FistControl>();
            }

            nM.punch.NoFist();
        }
        private void TryRunCalls()
        {
            try
            {
                if(HasDiscord)
                {
                    discord.RunCallbacks();
                }
            }
            catch
            {
                HasDiscord = false;
            }
        }
        public bool ShouldBeImmune()
        {
            if(SceneHelper.CurrentScene == "Level 8-4" && StatsManager.Instance.kills >= 7)
            {
                return true;
            }
            return false;
        }
        private void InNet()
        {
            if (NetworkManager.InLobby && SceneHelper.CurrentScene != "Main Menu")
            {
                if (immuneToDeath)
                {
                    NewMovement mo = MonoSingleton<NewMovement>.Instance;
                    mo.hp = 100;
                }
                immuneToDeath = ShouldBeImmune();
                // force game to run everything even if paused or timestopped
                if (!timeStopDisable.value)
                {
                    Time.timeScale = 1f;
                }
                Application.runInBackground = true;
                DeadPatch.SpectateOnDeath = NetworkManager.Instance.CurrentLobby.MemberCount > 1 || !NetworkManager.Sandbox;
                if (CheatsController.Instance.cheatsEnabled && NetworkManager.Instance.CurrentLobby.GetData("cheat") == "0" && NetworkManager.ClientAndConnected)
                {
                    CheatsController.Instance.cheatsEnabled = false;
                }
                if (OptionsManager.Instance.paused)
                {
                    CustomTogglePlayer(false);
                }
                else
                {
                    CustomTogglePlayer(true);
                }
                if (cam != null && lowPass != null)
                {
                    if (!Application.isFocused)
                    {
                        cam.enabled = false;
                        lowPass.cutoffFrequency = 500f;
                        lowPass.enabled = true;
                    }
                    else
                    {
                        cam.enabled = true;
                        lowPass.cutoffFrequency = 1000f;
                        if (!MonoSingleton<UnderwaterController>.Instance.inWater)
                        {
                            lowPass.enabled = (!NetworkPlayer.selfIsGhost) ? false : true;
                        }
                    }
                }
                if (CyberSync.Active)
                {
                    if (NetworkPlayer.selfIsGhost)
                    {
                        FistControl.Instance.NoFist();
                        GunControl.Instance.NoWeapon();
                        NewMovement.Instance.rb.constraints = NewMovement.Instance.defaultRBConstraints;
                        NewMovement.Instance.dead = false;
                        bool shouldMove = !OptionsManager.Instance.paused && !ChatUI.isTyping;
                        NewMovement.Instance.activated = shouldMove;
                        NewMovement.Instance.hp = 99;
                        CameraController.Instance.enabled = true;
                    }
                }
                cameFromPacketRestart = false;
            }
            else
            {
                DeadPatch.SpectateOnDeath = false;
                Application.runInBackground = false;
                immuneToDeath = false;
            }
        }
        public void HandleTargetData()
        {
            playerData.portalMatrix = Matrix4x4.identity;
            playerData.handle = new TargetHandle(NewMovement.Instance);
            playerData.position = NewMovement.Instance.transform.position;
            playerData.rotation = NewMovement.Instance.transform.rotation;
            playerData.headPosition = CameraController.Instance.transform.position;
            playerData.velocity = NewMovement.Instance.targetVel;
        }

        public void Update()
        {
            Inputs();
            TryRunCalls();
            if(NewMovement.Instance != null) HandleTargetData();

            if (currentUi != null && SceneHelper.CurrentScene != "Main Menu")
            {
                if(polrMM != null)
                {
                    currentUi.SetActive(MonoSingleton<OptionsManager>.Instance.paused);
                    if (polrMM.mainPanel.activeSelf && MonoSingleton<OptionsManager>.Instance.paused && MonoSingleton<OptionsManager>.Instance.pauseMenu != null)
                    {
                        TogglePauseMenu(false);
                    }
                    if (!polrMM.mainPanel.activeSelf && MonoSingleton<OptionsManager>.Instance.paused && MonoSingleton<OptionsManager>.Instance.pauseMenu != null)
                    {
                        TogglePauseMenu(true);
                    }
                    if (!MonoSingleton<OptionsManager>.Instance.paused)
                    {
                        polrMM.mainPanel.SetActive(false);
                    }
                    if (polrMM != null)
                    {
                        string status = (NetworkManager.InLobby) ? "STATUS: <color=green>IN LOBBY" : "STATUS: <color=red>NOT IN LOBBY";
                        polrMM.statusHost.text = status;
                        polrMM.statusJoin.text = status;
                        leaveButton.SetActive(NetworkManager.InLobby);
                        playerListButton.SetActive(NetworkManager.InLobby);
                        joinButton.GetComponent<Button>().interactable = !NetworkManager.InLobby;
                        hostButton.GetComponent<Button>().interactable = !NetworkManager.InLobby;

                        inviteButton.SetActive(NetworkManager.HostAndConnected);
                        copyButton.SetActive(NetworkManager.HostAndConnected);
                    }
                }
                else if(currentUi != null)
                {
                    currentUi.SetActive(true);
                    currentUi.transform.Find("PolariteMenu").TryGetComponent<PolariteMenuManager>(out var menu);
                    if(menu != null)
                    {
                        polrMM = menu;
                    }
                }
            }
            if(currentUi != null && SceneHelper.CurrentScene == "Main Menu")
            {
                currentUi.SetActive(false);
            }
            InNet();
        }
        public void LateUpdate()
        {
            if (SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap" || SceneHelper.CurrentScene == "Main Menu")
            {
                return;
            }
            cam = MonoSingleton<CameraController>.Instance.cam;
            if(cam != null)
            {
                lowPass = cam.GetComponent<AudioLowPassFilter>();
            }
        }
        // unused but me stupid and i like being stupid
        public static void FlyToggle(bool val)
        {
            if (!CyberSync.Active)
            {
                return;
            }
            ICheat flyCheat = CheatsManager.Instance.idToCheat["ultrakill.flight"];
            if (flyCheat != null)
            {
                if (val)
                {
                    flyCheat.Enable(CheatsManager.Instance);
                }
                else
                {
                    flyCheat.Disable();
                }
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
        public static void CustomTogglePlayer(bool val)
        {
            if(ChatUI.isTyping)
            {
                val = false;
            }
            if(val == plrActive)
            {
                return;
            }
            if(!NetworkManager.InLobby)
            {
                val = true;
            }
            GameStateManager.Instance.CameraLocked = !val;
            GameStateManager.Instance.TimerModifier = 1f;
            GameStateManager.Instance.PlayerInputLocked = !val;
            CameraController.Instance.activated = val;
            GunControl.Instance.activated = val;
            if(val)
            {
                FistControl.Instance.YesFist();
            }
            else
            {
                FistControl.Instance.NoFist();
            }
            if (!val && NewMovement.Instance.gc.onGround)
            {
                NewMovement.Instance.StopMovement();
            }
            NewMovement.Instance.activated = val;
            plrActive = val;
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
                    Transform pirateGuide = pMM.transform.Find("Main").Find("PiratesGuide").Find("PiratesGuideBG");
                    Transform blueScreen = pMM.transform.parent.Find("BackgroundStuff");
                    TextMeshProUGUI ver = pMM.transform.Find("Main").Find("Ver").GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI motd = pMM.transform.Find("Main").Find("MOTD").GetComponent<TextMeshProUGUI>();

                    pMM.maxP = host.Find("MaxPlayers").GetComponent<TMP_InputField>();
                    pMM.lobbyName = host.Find("UsefulInputField").GetComponent<TMP_InputField>();
                    pMM.lobbyType = host.Find("UsefulDropdown").GetComponent<TMP_Dropdown>();
                    pMM.mainPanel = pMM.transform.Find("Main").gameObject;
                    pMM.code = join.Find("UsefulInputField").GetComponent<TMP_InputField>();
                    pMM.canCheat = host.Find("CanCheat").GetComponent<TMP_Dropdown>();

                    pMM.statusHost = host.Find("Status").GetComponent<TextMeshProUGUI>();
                    pMM.statusJoin = join.Find("Status").GetComponent<TextMeshProUGUI>();

                    string serverStat = "";
                    switch(NetworkManager.cMode.server_mode)
                    {
                        case "steam":
                            serverStat = "<color=#66c0f4>Steam</color>";
                            break;
                        case "pirated":
                            serverStat = "<color=#ff8522>Pirated</color>";
                            break;
                    }
                    pMM.transform.Find("Main").Find("ConnectedTo?").GetComponent<TextMeshProUGUI>().text = $"Currently connected to {serverStat} servers.";

                    Button leave = pMM.transform.Find("Main").Find("Leave").GetComponent<Button>();
                    Button invite = host.Find("UsefulButton (1)").GetComponent<Button>();
                    Button create = host.Find("UsefulButton").GetComponent<Button>();
                    Button joinL = join.Find("UsefulButton").GetComponent<Button>();
                    Button copyCode = host.Find("CopyCode").GetComponent<Button>();
                    Button onPublicClick = pMM.transform.Find("Main").Find("UsefulButton (3)").GetComponent<Button>();
                    Button refresh = publicLobbies.Find("RefreshButton").GetComponent<Button>();
                    Button pList = pMM.transform.Find("Main").Find("PlayerListButton").GetComponent<Button>();
                    Button discord = pMM.transform.Find("Main").Find("JoinDiscord").GetComponent<Button>();
                    Button gameFolder = pirateGuide.Find("UsefulButton").GetComponent<Button>();
                    Button noRead = pirateGuide.Find("UsefulButton (2)").GetComponent<Button>();

                    gameFolder.onClick.AddListener(() => Application.OpenURL(Directory.GetParent("server_config.json").FullName));
                    noRead.onClick.AddListener(() => Application.OpenURL("https://www.youtube.com/watch?v=LKeof3dleS0"));
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
                        // new discord link :)
                        Application.OpenURL("https://discord.gg/mNdbhdaHzH");
                    });
                    onPublicClick.onClick.AddListener(PublicLobbyManager.RefreshLobbies);
                    refresh.onClick.AddListener(PublicLobbyManager.RefreshLobbies);
                    pList.onClick.AddListener(PlayerList.UpdatePList);

                    PublicLobbyManager.Content = publicLobbies.Find("LobbyList").Find("Content");
                    PlayerList.ContentB = playerList.Find("List").Find("Content");

                    ver.text = Version;
                    ver.color = (ReleaseBuild) ? Color.white : Color.yellow;
                    
                    motd.text = MOTD;

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
                    pirateGuide.parent.gameObject.SetActive(false);
                    pMM.mainPanel.SetActive(false);

                    pMM.lobbyName.text = $"{NetworkManager.GetNameOfId(NetworkManager.Id)}'s Lobby";
                    polrMM = pMM;
                }
            }
        }
        private async void CreateLobby()
        {
            PolariteMenuManager pMM = currentUi.GetComponentInChildren<PolariteMenuManager>();
            int max = int.Parse(pMM.maxP.text);
            string lobbyName = pMM.lobbyName.text;
            if(string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = $"{NetworkManager.GetNameOfId(NetworkManager.Id)}'s Lobby";
            }
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
                if (!NetworkManager.players.ContainsKey(id.Value))
                {
                    NetworkPlayer newPlr = NetworkPlayer.Create(id.Value, NetworkManager.GetNameOfId(id));
                    NetworkManager.players.Add(id.Value, newPlr);
                }
            }
        }
        public void CleanLevel()
        {
            Invoke(nameof(CleanLevelOfSoftlocks), 0.1f);
        }
        public void CleanLevelOfSoftlocks()
        {
            foreach (string softlock in PathsToSoftlocks)
            {
                bool v2Patch = softlock == "V2 - Arena/V2 Stuff(Clone)/Door";
                GameObject go = TryGetSceneObject(softlock);
                if (go == null) continue;
                Transform obj = go.transform;
                if (obj != null)
                {
                    if(v2Patch)
                    {
                        obj.GetComponent<Door>().Open(skull: true);
                        Destroy(obj.GetComponent<Door>());
                    }
                    else
                    {
                        Destroy(obj.gameObject);
                    }
                }
            }
        }
        public GameObject TryGetSceneObject(string path)
        {
            try
            {
                GameObject obj = FindByPath(SceneManager.GetActiveScene().GetRootGameObjects(), path).gameObject;
                if (obj != null)
                {
                    return obj;
                }
                return null;
            }
            catch { return null; }
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
            if (current.name == parts[index])
            {
                if (index == parts.Length - 1)
                    return current;

                for (int i = 0; i < current.childCount; i++)
                {
                    Transform found = Recurse(current.GetChild(i), parts, index + 1);
                    if (found != null)
                        return found;
                }
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = Recurse(current.GetChild(i), parts, index);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void InitComps()
        {
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
            if (MonoSingleton<CameraController>.Instance != null)
            {
                SpectatorCam cam = MonoSingleton<CameraController>.Instance.gameObject.GetComponent<SpectatorCam>();
                if (MonoSingleton<CameraController>.Instance.GetComponent<SpectatorCam>() == null)
                {
                    cam = MonoSingleton<CameraController>.Instance.gameObject.AddComponent<SpectatorCam>();
                }
                LineTargetTool tool = MonoSingleton<CameraController>.Instance.gameObject.GetComponent<LineTargetTool>();
                if (MonoSingleton<CameraController>.Instance.GetComponent<LineTargetTool>() == null)
                {
                    tool = MonoSingleton<CameraController>.Instance.gameObject.AddComponent<LineTargetTool>();
                }
            }
        }


        private void OnSceneLoaded(Scene args1, LoadSceneMode args2)
        {
            if (SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap")
            {
                return;
            }
            InitComps();
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
            SceneObjectCache.Initialize();
            if(NetworkManager.HostAndConnected)
            {
                NetworkManager.Instance.CurrentLobby.SetData("levelName", GetLevelName());
            }
            if(HasDiscord)
            {
                if (NetworkManager.HasRichPresence)
                {
                    DiscordController.Instance.enabled = false;
                    discord.GetActivityManager().UpdateActivity(new Activity
                    {
                        ApplicationId = 1432308384798867456,
                        Details = $"Playing in: {NetworkManager.Instance.CurrentLobby.GetData("levelName")}, In Polarite Lobby ({NetworkManager.Instance.CurrentLobby.MemberCount}/{NetworkManager.Instance.CurrentLobby.MaxMembers})",
                        Instance = true
                    }, delegate { });
                }
                else
                {
                    DiscordController.Instance.enabled = true;
                }
            }
            NetworkPlayer.ToggleColsForAll(false);
            Instance.StartCoroutine(RestartCols());
            NetworkManager.WasUsed = NetworkManager.InLobby;
            if(NetworkManager.InLobby)
            {
                NetworkPlayer.ToggleEidForAll(true);
                CleanLevel();
                foreach(var p in NetworkManager.players.Values)
                {
                    p.ToggleRig(true);
                    p.isGhost = false;
                }
                NetworkManager.Instance.SceneLoad();
            }
            NetworkPlayer.selfIsGhost = false;
            NetworkEnemy.Flush();
            immuneToDeath = false;
            if(SceneHelper.CurrentScene == "Level 0-4" && StockMapInfo.Instance != null && NetworkManager.InLobby)
            {
                StockMapInfo.Instance.levelName = $"A {NetworkManager.Instance.CurrentLobby.MemberCount}-MACHINE ARMY";
            }
            Instance.StartCoroutine(UnpauseNet());
            if(NetworkManager.InLobby)
            {
                CustomTogglePlayer(true);
            }
        }
        public static string GetLevelName()
        {
            string levelName = StockMapInfo.Instance.levelName;
            string sceneName = SceneHelper.CurrentScene;
            if (sceneName == "uk_construct")
            {
                levelName = "SANDBOX";
            }
            if (sceneName == "Endless")
            {
                levelName = "CYBERGRIND";
            }
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = sceneName;
            }
            return levelName;
        }
        public static void SpawnSound(AudioClip clip, float pitch, Transform parent, float volume)
        {
            AudioSource audioSource = new GameObject("Noise").AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.pitch = pitch;
            audioSource.volume = volume;
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 250f;
            audioSource.minDistance = 100f;
            audioSource.gameObject.AddComponent<RemoveOnTime>().time = clip.length;
            audioSource.transform.SetParent(parent, false);
            audioSource.transform.position = parent.position;
            audioSource.Play();
        }
        public static void SpectatePlayers(bool loadAll)
        {
            if(ignoreSpectate)
            {
                return;
            }
            Instance.StartCoroutine(SpectatePlayersB(loadAll));
            NetworkManager.SceneLoading = false;
            Net.Unpause();
        }

        public static IEnumerator SpectatePlayersB(bool loadAll)
        {
            yield return new WaitForSeconds(1.25f);
            MonoSingleton<NewMovement>.Instance.ActivatePlayer();
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
            if(loadAll)
            {
                foreach (var door in FindObjectsOfType<Door>())
                {
                    door.Open(skull: true);
                }
                MonoSingleton<MusicManager>.Instance.ForceStartMusic();
            }
            MonoSingleton<NewMovement>.Instance.deathSequence.deathScreen.SetActive(false);
        }
        public static void StopSpectating()
        {
            SpectatorCam cam = MonoSingleton<CameraController>.Instance.GetComponent<SpectatorCam>();
            if (cam == null)
            {
                cam = MonoSingleton<CameraController>.Instance.gameObject.AddComponent<SpectatorCam>();
            }
            cam.StopSpectating();
        }

        public static void Ghost(bool val)
        {
            if(NetworkPlayer.selfIsGhost == val)
            {
                if(val)
                {
                    DeadPatch.Respawn(new Vector3(0f, 80f, 62.5f), CameraController.Instance.transform.rotation);
                }
                return;
            }
            if(val)
            {
                // thank jaket that they have a respawn position i'll be taking that
                DeadPatch.Respawn(new Vector3(0f, 80f, 62.5f), CameraController.Instance.transform.rotation);
                ChatUI.Instance.ForceOff();
                SpawnSound(mainBundle.LoadAsset<AudioClip>("GhostTransform2"), Random.Range(0.95f, 1.15f), CameraController.Instance.transform, 1f);
                if (ChatUI.Instance != null)
                {
                    ChatUI.Instance.OnSubmitMessage($"<color=#91FFFF>You're now a ghost. Wait for the next wave to respawn! Currently, {CyberSync.PlayersAlive() - 1} remain.</color>", false, $"<color=#91FFFF>You're now a ghost. Wait for the next wave to respawn! Currently, {CyberSync.PlayersAlive()} remain.</color>", tts: false);
                    ChatUI.Instance.ShowUIForBit(5f);
                }
                NetworkPlayer.selfIsGhost = true;

                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.BecameGhost, w.GetBytes());
                CameraController.Instance.cameraShaking = 0;
            }
            else
            {
                GunControl.Instance.YesWeapon();
                FistControl.Instance.YesFist();
                NetworkPlayer.selfIsGhost = false;
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.ReviveGhost, w.GetBytes());
            }
        }
        public static void GameOver()
        {
            Ghost(false);
            NewMovement.Instance.hp = 0;
            NewMovement.Instance.GetHurt(9999, false);
        }
        public static IEnumerator RestartCols()
        {
            yield return new WaitForSeconds(4f);
            NetworkPlayer.ToggleColsForAll(true);
        }
        public static IEnumerator UnpauseNet()
        {
            yield return new WaitForSeconds(0.1f);
            Net.Unpause();
            NetworkManager.Instance.SceneLoad();
        }
    }
}
