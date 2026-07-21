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
using Polarite.Web;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using ULTRAKILL.Cheats;
using ULTRAKILL.Enemy;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Core.Output;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using plog;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using LobbyType = Polarite.Multiplayer.LobbyType;
using NetworkManager = Polarite.Multiplayer.NetworkManager;
using Polarite.Networking.Skins;
using Polarite.SamTTS;
using UnityEngine.AddressableAssets;
using Polarite.VoiceChat;
using System;
using Random = UnityEngine.Random;
using System.Runtime.InteropServices;
using Polarite.Networking.Extensions;

namespace Polarite
{
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

    public enum SearchFolder
    {
        Downloads,
        Videos,
        Pictures,
        Desktop,
        Documents,
        Other
    }
    public enum ChatAlign
    {
        TopLeft,
        TopMiddle,
        TopRight,
        MiddleLeft,
        MiddleRight,
        BottomLeft,
        BottomMiddle,
        BottomRight
    }
    public enum VCAlign
    {
        Left,
        Middle,
        Right
    }
    public enum VCListAlign
    {
        TopToBottom,
        Center,
        BottomToTop
    }

    public struct Typewriter
    {
        public string text;
        public float delay;
        public TextMeshProUGUI target;

        public Typewriter(string text, float delay, TextMeshProUGUI target)
        {
            this.text = text;
            this.delay = delay;
            this.target = target;
        }
    }
    public struct BuildTag
    {
        public bool debug;
        public string buildName;
    }

    [BepInPlugin("com.d1g1tal.polarite", "Polarite", "1.1.0")]
    public class ItePlugin : BaseUnityPlugin
    {
        public static readonly PluginConfigurator config = PluginConfigurator.Create("Polarite Config", "com.d1g1tal.polarite");

        public static ConfigPanel mainGameRelated = new ConfigPanel(config.rootPanel, "Gameplay Config", "gameplay");

        public static ConfigPanel voiceRelated = new ConfigPanel(config.rootPanel, "Voice Config", "voice");

        public static ConfigPanel cosmeticRelated = new ConfigPanel(config.rootPanel, "Cosmetic Config", "cosmetic");

        public static ConfigPanel uiConfig = new ConfigPanel(config.rootPanel, "UI Config", "ui");

        public static ConfigPanel debugRelated = new ConfigPanel(config.rootPanel, "Debug zone", "debugzone");

        public static BoolField canBeFriendlyFired = new BoolField(mainGameRelated, "Can be friendly fired", "gameplay.client.friendlyfire", true);

        public static BoolField disableCheckpointSync = new BoolField(mainGameRelated, "Disable checkpoint sync", "gameplay.client.checkpointsync", false);

        public static BoolField timeStopDisable = new BoolField(mainGameRelated, "Disable timestop changes", "timestop.disable", false);

        public static BoolField disableHI = new BoolField(mainGameRelated, "Disable \"hidden indicator\"", "hi.disable", false);

        public static KeyCodeField killbind = new KeyCodeField(mainGameRelated, "Killbind", "killbind", KeyCode.K);

        public static ConfigPanel hostPanel = new ConfigPanel(mainGameRelated, "Extra host settings", "gameplay.host");

        public static ConfigHeader hostControl = new ConfigHeader(hostPanel, "<color=orange>These values are controlled by the host.</color>");

        public static BoolField bossHpIncrease = new BoolField(hostPanel, "Increase boss HP by player count", "g.b", true);

        public static FloatField bossHpMult = new FloatField(hostPanel, "Boss HP increase multiplier", "g.bm", 1.3f);

        public static KeyCodeField buttonToChat = new KeyCodeField(config.rootPanel, "Open chat key", "chat.key", KeyCode.T);

        public static ConfigHeader vcheaderstuff = new ConfigHeader(voiceRelated, "Stuff related to Voice Chat");

        public static KeyCodeField voicePushToTalk = new KeyCodeField(voiceRelated, "Push-to-talk key", "voice.pttkey", KeyCode.None);

        // voice chat stuff
        // made by doomahreal

        public static EnumField<VoiceMode> voiceMode = new EnumField<VoiceMode>(voiceRelated, "Voice mode", "voice.mode", VoiceMode.PushToTalk);

        // voice quality setting
        public static EnumField<VoiceQuality> voiceQuality = new EnumField<VoiceQuality>(voiceRelated, "Voice quality", "voice.quality", VoiceQuality.High); // medium didn't sound good to other people

        // which microphone index to use (0 = first device)
        public static IntField voiceMicIndex = new IntField(voiceRelated, "Microphone index", "voice.mic", 0);

        public static ConfigHeader wheresMyMic = new ConfigHeader(voiceRelated, "0: ");

        // voice activation sensitivity (linear 0-100)
        public static IntField voiceVADThreshold = new IntField(voiceRelated, "Voice activation threshold (0-100)", "voice.vad", 30);

        // whether to receive/hear voice chat
        public static BoolField receiveVoice = new BoolField(voiceRelated, "Hear players voices", "voice.receive", true);
        public static BoolField vcPlayback = new BoolField(voiceRelated, "Voice playback", "voice.playback", false);

        // skins
        public static ConfigPanel skinsMenu = new ConfigPanel(cosmeticRelated, "Skin Config", "skins");

        public static KeyCodeField previewSkin = new KeyCodeField(skinsMenu, "Preview skin key", "skin.preview", KeyCode.LeftAlt);
        public static KeyCodeField screenShotSkin = new KeyCodeField(skinsMenu, "Screenshot skin key", "skin.screenie", KeyCode.RightAlt);

        // saving skins
        public static ConfigPanel savePanel = new ConfigPanel(skinsMenu, "Saving/loading skins", "save.skins", ConfigPanel.PanelFieldType.StandardWithIcon);

        // public static ConfigHeader tutorial = new ConfigHeader(savePanel, "<color=yellow>To load other peoples skin manually, add their .polarskin file into the saved skins folder and refresh.</color>");
        // public static ButtonField openLocation = new ButtonField(savePanel, "Open saved skins folder", "skin.openloc");
        public static ButtonField refresh = new ButtonField(savePanel, "Refresh loaded skins", "skin.refresh");
        // import menu
        public static ConfigPanel import = new ConfigPanel(savePanel, "Import .polarskin files", "import.skin", ConfigPanel.PanelFieldType.StandardWithIcon);
        
        public static EnumField<SearchFolder> searchFolderEnum = new EnumField<SearchFolder>(import, "Search folder", "import.searchfolder", SearchFolder.Downloads);
        public static StringField customSearchFolder = new StringField(import, "Search folder path", "import.searchfolder.custom", @"C:\Users\" + Environment.UserName + @"\Downloads");
        public static ButtonField searchnImport = new ButtonField(import, "Search & Import", "import.IMPORT");
        public static ConfigHeader countOfImported = new ConfigHeader(import, "");

        public static ConfigPanel saveSkinPanel = new ConfigPanel(savePanel, "Save skin", "save.skin", ConfigPanel.PanelFieldType.StandardWithIcon);

        public static ConfigHeader loadedArea = new ConfigHeader(savePanel, "--- Loaded ---");

        // saving
        public static StringField saveName = new StringField(saveSkinPanel, "Skin name", "skin.name", "My skin");
        public static ButtonField save = new ButtonField(saveSkinPanel, "Save", "save.skin.reallysave");

        public static ColorField baseColor = new ColorField(skinsMenu, "Base color", "skin.base", Color.white);
        public static ColorField lightColor = new ColorField(skinsMenu, "Base light color", "skin.light", Color.white);
        public static ColorField wingLightColor = new ColorField(skinsMenu, "Wing light color", "skin.light.wing", Color.white);
        public static ColorField metalColor = new ColorField(skinsMenu, "Metal color", "skin.metal", Color.gray);
        public static FloatSliderField shinyness = new FloatSliderField(skinsMenu, "Shininess", "skin.SHINY", new System.Tuple<float, float>(0f, 1f), 0.7f);

        public static StringField namePlate = new StringField(skinsMenu, "V-Model nameplate", "name.plate", "V?");
        public static ColorField namePlateColor = new ColorField(skinsMenu, "V-Model nameplate color", "name.plate.color", Color.black);

        public static ButtonField apply = new ButtonField(skinsMenu, "Apply skin", "skin.apply");
        public static ButtonField random = new ButtonField(skinsMenu, "Randomize skin", "skin.random");

        public static BoolField disableSkinTip = new BoolField(config.rootPanel, "Disable skin tip message", "skin.tip.disable", false);

        public static BoolField chatNoise = new BoolField(cosmeticRelated, "Chat noise", "chat.noise", true);

        public static ConfigPanel ttsPanel = new ConfigPanel(cosmeticRelated, "TTS Config", "cosmetic.ttssam");

        public static BoolField canTTS = new BoolField(ttsPanel, "Enable Sam TTS", "tts.enabled", true);
        public static BoolField ttsHurtAndDeath = new BoolField(ttsPanel, "Play TTS when getting hurt or while dying", "tts.hurtndyning", true);
        public static BoolField ttsChat = new BoolField(ttsPanel, "Play TTS when sending/receiving a chat message", "tts.chatter", true);
        public static IntField ttsSpeed = new IntField(ttsPanel, "TTS speed", "tts.speed", SamPitch.BASE_SPEED);
        public static IntField ttsPitch = new IntField(ttsPanel, "TTS pitch", "tts.pitch", SamPitch.BASE_PITCH);
        public static IntField ttsMouth = new IntField(ttsPanel, "TTS mouth", "tts.mouth", SamPitch.BASE_MOUTH);
        public static IntField ttsThroat = new IntField(ttsPanel, "TTS throat", "tts.throat", SamPitch.BASE_THROAT);

        // ui
        public static EnumField<ChatAlign> chatAlignment = new EnumField<ChatAlign>(uiConfig, "Chat UI position", "ui.chat", ChatAlign.BottomLeft);
        public static ConfigPanel voiceUiConfig = new ConfigPanel(uiConfig, "Voice UI", "ui.voice");
        public static EnumField<VCAlign> vcAlignmentPos = new EnumField<VCAlign>(voiceUiConfig, "Voice chat UI position", "ui.vcpos", VCAlign.Right);
        public static EnumField<VCListAlign> vcAlignmentList = new EnumField<VCListAlign>(voiceUiConfig, "Voice chat UI list alignment", "ui.vclist", VCListAlign.BottomToTop);
        public static BoolField useSkinInsteadOfPFP = new BoolField(voiceUiConfig, "Show players custom skin instead of PFP", "ui.showskin", false);

        // hidden options
        public static BoolField hasHadRandomSkin = new BoolField(debugRelated, "Had random skin", "skin.randomized", false);
        public static BoolField openedPolariteMenu = new BoolField(debugRelated, "Pressed globe", "pressed.globe", false);
        public static BoolField hasHadRandomPitch = new BoolField(debugRelated, "Had random voice", "random.voice", false);
        public static BoolField didSetup = new BoolField(debugRelated, "Setup voice chat", "did.setup", false);
        public static BoolField logPacketParsing = new BoolField(debugRelated, "Log when parsing packets", "log.packets", false);
        public static BoolField logDebugLogs = new BoolField(debugRelated, "Allow debug logs", "log.allowdebug", false);
        public static BoolField logDebugErrorLogs = new BoolField(debugRelated, "Allow debug error logs", "log.allowerrordebug", false);

        internal readonly Harmony harm = new Harmony("com.d1g1tal.polarite");

        internal static ManualLogSource log;

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
            "Pre-Space/Rooms/7 - Heart Chamber/7 Nonstuff/Void/Backwall",
            "Pre-Space/Rooms/7 - Heart Chamber/7 Stuff/Door"
        };

        // background fx
        public Camera cam;
        public AudioLowPassFilter lowPass;

        public PolariteMenuManager polrMM;
        public static bool plrActive;
        public static bool cameFromPacketRestart;

        public static bool debugMode = false;
        public static bool debugSending = false;
        public static bool showNetDebug = false;

        private static float killbindCooldown = 5f;

        public static bool immuneToDeath = false;
        public static bool canBecomeGhost = false;

        public static readonly bool ReleaseBuild = false;
        public static readonly string Version = "v1.1.0-pre-release";
        public static bool CanEnableDebug
        {
            get
            {
                return !ReleaseBuild || Net.Dev(NetworkManager.Id);
            }
        }

        public static TargetData playerData;

        public static Skin currentSkin;

        public static List<Typewriter> typewriters = new List<Typewriter>();

        public static AudioClip message;

        public static Coroutine currentMoveY, currentScreaming, currentFlash;

        public static GameObject screaming;

        public static bool PolarMenuActive = false;


        public void Awake()
        {
            try
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
                log = Logger;
                harm.PatchAll();
                NetworkManager netManager = gameObject.GetComponent<NetworkManager>();
                if (netManager == null)
                {
                    netManager = gameObject.AddComponent<NetworkManager>();
                }
                SceneManager.sceneLoaded += OnSceneLoaded;
                config.SetIconWithURL("file://" + Path.Combine(Directory.GetParent(Info.Location).FullName, "icon.png"));
                apply.onClick += () =>
                {
                    HandleSkin();
                    SpawnSound(mainBundle.LoadAsset<AudioClip>("SkinChange"), 1f, MonoSingleton<CameraController>.Instance.transform, 1f);
                };
                random.onClick += () =>
                {
                    baseColor.value = Random.ColorHSV();
                    lightColor.value = Random.ColorHSV();
                    wingLightColor.value = Random.ColorHSV();
                    metalColor.value = Random.ColorHSV();
                    shinyness.value = Random.Range(0f, 1f);
                    namePlate.value = $"V{Random.Range(1, 1000)}";
                    namePlateColor.value = Random.ColorHSV();
                    HandleSkin();
                    SpawnSound(mainBundle.LoadAsset<AudioClip>("SkinChange"), 1f, MonoSingleton<CameraController>.Instance.transform, 1f);
                };
                bossHpIncrease.postValueChangeEvent += (bool v) =>
                {
                    if (NetworkManager.HostAndConnected)
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
                /*
                openLocation.onClick += () =>
                {
                    Application.OpenURL(SkinSaver.Path);
                };
                */
                refresh.onClick += () =>
                {
                    SkinSaver.Clear();
                    SkinSaver.LoadAllSkins();
                };
                save.onClick += () =>
                {
                    Skin data = GetSkin(baseColor.value, lightColor.value, wingLightColor.value, metalColor.value, shinyness.value, namePlate.value, namePlateColor.value);
                    SkinSaver.SaveSkin(new SaveableSkin()
                    {
                        name = saveName.value,
                        data = data
                    });
                };
                hasHadRandomSkin.onValueChange += (val) =>
                {
                    if (currentSkin.Base != Color.clear && !val.value)
                    {
                        baseColor.value = Random.ColorHSV();
                        lightColor.value = Random.ColorHSV();
                        wingLightColor.value = Random.ColorHSV();
                        metalColor.value = Random.ColorHSV();
                        shinyness.value = Random.Range(0f, 1f);
                        namePlate.value = $"V{Random.Range(1, 1000)}";
                        namePlateColor.value = Random.ColorHSV();
                        HandleSkin();
                        SpawnSound(mainBundle.LoadAsset<AudioClip>("SkinChange"), 1f, MonoSingleton<CameraController>.Instance.transform, 1f);
                        hasHadRandomSkin.value = true;
                    }
                };
                searchnImport.onClick += () =>
                {
                    string path = searchFolderEnum.value == SearchFolder.Other && !string.IsNullOrEmpty(customSearchFolder.value) ? customSearchFolder.value : "";
                    if(string.IsNullOrEmpty(path))
                    {
                        switch(searchFolderEnum.value)
                        {
                            case SearchFolder.Downloads:
                                path = @"C:\Users\" + Environment.UserName + @"\Downloads";
                                break;
                            case SearchFolder.Videos:
                                path = @"C:\Users\" + Environment.UserName + @"\Videos";
                                break;
                            case SearchFolder.Pictures:
                                path = @"C:\Users\" + Environment.UserName + @"\OneDrive\Pictures";
                                break;
                            case SearchFolder.Desktop:
                                path = @"C:\Users\" + Environment.UserName + @"\OneDrive\Desktop";
                                break;
                            case SearchFolder.Documents:
                                path = @"C:\Users\" + Environment.UserName + @"\OneDrive\Documents";
                                break;
                        }
                    }
                    SkinSaver.SearchAndImport(path);
                };
                import.onPannelOpenEvent += (p) =>
                {
                    countOfImported.text = "";
                };
                useSkinInsteadOfPFP.onValueChange += (val) =>
                {
                    VoiceUI.RefreshIcons(val.value);
                };
                ttsSpeed.onValueChange += (val) => SamPitch.configSam.speed = val.value;
                ttsPitch.onValueChange += (val) => SamPitch.configSam.pitch = val.value;
                ttsMouth.onValueChange += (val) => SamPitch.configSam.mouth = val.value;
                ttsThroat.onValueChange += (val) => SamPitch.configSam.throat = val.value;
                chatAlignment.onValueChange += (val) => UIAnchors.Refresh(val.value);
                vcAlignmentPos.onValueChange += (val) => UIAnchors.Refresh(val.value);
                vcAlignmentList.onValueChange += (val) => UIAnchors.Refresh(val.value);
                mainBundle = AssetBundle.LoadFromFile(Path.Combine(Directory.GetParent(Info.Location).FullName, "polariteassets.bundle"));
                TryRunDiscord();
                playerData = new TargetData();
                debugRelated.hidden = !CanEnableDebug;
                bossHpIncrease.interactable = false;
                bossHpMult.interactable = false;
                bossHpIncrease.value = false;
                SkinSaver.Init();
                SkinScreenshotter.Init();
                SkinSaver.LoadAllSkins();
                saveSkinPanel.icon = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/Cheats/Save 1.png").WaitForCompletion();
                import.icon = Addressables.LoadAssetAsync<Sprite>("Assets/Textures/UI/download_arrow.png").WaitForCompletion();
                savePanel.icon = mainBundle.LoadAsset<Sprite>("savenloadingicon");
            }
            catch (Exception ex)
            {
                Logger.LogError("Polarite failed to load! Error message: " + ex.Message);
                harm.UnpatchSelf();
            }
        }

        public Skin DefaultSkin()
        {
            Skin skin = new Skin();
            skin.Base = Color.gray;
            skin.Light = Color.white;
            skin.WingLight = Color.white;
            skin.Metal = Color.gray;
            skin.Shinyness = 0.5f;
            skin.Nameplate = "V?";
            skin.NameplateColor = Color.black;
            return skin;
        }
        public Skin GetSkin(Color baseColor, Color lightColor, Color wingLight, Color metal, float shiny, string name, Color namePlateColor)
        {
            Skin skin = new Skin();
            skin.Base = baseColor;
            skin.Light = lightColor;
            skin.WingLight = wingLight;
            skin.Metal = metal;
            skin.Shinyness = shiny;
            skin.Nameplate = name;
            skin.NameplateColor = namePlateColor;
            return skin;
        }
        public static void ArmCheck(bool alt)
        {
            try
            {
                Transform arm = MonoSingleton<GunControl>.Instance.currentWeapon.transform.Find(alt ? "Revolver_Rerigged_Alternate/RightArm" : "Revolver_Rerigged_Standard/RightArm");
                if (arm != null)
                {
                    SkinnedMeshRenderer r = arm.GetComponent<SkinnedMeshRenderer>();
                    SkinManagerV2.CustomColor(r, currentSkin.Base, currentSkin.Light, currentSkin.Metal, currentSkin.Shinyness, MaskConsts.RIGHT_ARM_MASK, (alt ? "RArmAlt" : "RArm") + NetworkManager.Id, 0, true);
                }
            }
            catch
            {
                // welp seems the player doesn't have the revolver out (or theres no skin for the arm)
            }
            try
            {
                // arms
                foreach(var rend in FistControl.Instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if(rend.name == "Feedbacker")
                    {
                        SkinManagerV2.CustomColor(rend, currentSkin.Base, currentSkin.Light, currentSkin.Metal, currentSkin.Shinyness, MaskConsts.FEEDBACKER_MASK, "Feedbacker" + NetworkManager.Id, 0, true);
                    }
                    if(rend.name == "arm_lp")
                    {
                        SkinManagerV2.CustomColor(rend, currentSkin.Base, currentSkin.Light, currentSkin.Metal, currentSkin.Shinyness, MaskConsts.KNUCKLEBLASTER_MASK, "KB" + NetworkManager.Id, 0, true);
                    }
                    if(rend.name == "Arm" || rend.name == "Hook")
                    {
                        SkinManagerV2.CustomColor(rend, currentSkin.Base, currentSkin.Light, currentSkin.Metal, currentSkin.Shinyness, MaskConsts.WHIPLASH_MASK, "Whip" + NetworkManager.Id, 0, true);
                    }
                }
            }
            catch
            {
                // skip
            }
        }
        public static void ReverseArmCheck()
        {
            SkinManagerV2.Reset("RArm" + NetworkManager.Id);
            SkinManagerV2.Reset("RArmAlt" + NetworkManager.Id);
            SkinManagerV2.Reset("Feedbacker" + NetworkManager.Id);
            SkinManagerV2.Reset("KB" + NetworkManager.Id);
            SkinManagerV2.Reset("Whip" + NetworkManager.Id);
        }
        public static void AnimationCheck()
        {
            if (MonoSingleton<NewMovement>.Instance != null)
            {
                PlayerAnimations rig = MonoSingleton<NewMovement>.Instance.gc.GetComponentInChildren<PlayerAnimations>();
                if (rig != null)
                {
                    SkinnedMeshRenderer r = rig.transform.Find("v1_mdl").GetComponent<SkinnedMeshRenderer>();
                    for (int i = 0; i < r.materials.Length; i++)
                    {
                        if (i == 0)
                        {
                            SkinManagerV2.CustomColor(r, currentSkin.Base, currentSkin.Light, currentSkin.Metal, currentSkin.Shinyness, MaskConsts.V1_BASE_MASK, "BaseMir" + NetworkManager.Id, i);
                        }
                        else
                        {
                            // turn the emissive flag off
                            r.materials[i].DisableKeyword("EMISSIVE");
                            SkinManagerV2.CustomColor(r, currentSkin.Base, currentSkin.WingLight, currentSkin.Metal, currentSkin.Shinyness, MaskConsts.V1_WING_MASK, "WingMir" + NetworkManager.Id, i);
                        }
                    }
                }
            }
        }
        public static void ReverseAnimationCheck()
        {
            if (MonoSingleton<NewMovement>.Instance != null)
            {
                PlayerAnimations rig = MonoSingleton<NewMovement>.Instance.gc.GetComponentInChildren<PlayerAnimations>();
                if (rig != null)
                {
                    SkinnedMeshRenderer r = rig.transform.Find("v1_mdl").GetComponent<SkinnedMeshRenderer>();
                    for (int i = 0; i < r.materials.Length; i++)
                    {
                        if (i >= 1)
                        {
                            // turn the emissive flag back on
                            r.materials[i].EnableKeyword("EMISSIVE");
                        }
                    }
                }
            }
            SkinManagerV2.Reset("BaseMir" + NetworkManager.Id);
            SkinManagerV2.Reset("WingMir" + NetworkManager.Id);
        }
        public static void LogDebug(string msg, bool ignore = false)
        {
            if (debugMode || ignore)
            {
                if (SubtitleController.Instance.subtitlesEnabled)
                {
                    SubtitleController.Instance.DisplaySubtitle(msg);
                }
                else
                {
                    HudMessageReceiver.Instance.SendHudMessage(msg);
                }
            }
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
                Logger.LogWarning("User doesn't have discord in the background, Skipping discord!");
                return false;
            }
        }
        private void Inputs()
        {
            if (!Net.Paused && Net.List != null && NetworkManager.InLobby)
            {
                Net.Tick();
            }
            if (debugMode && NetworkManager.InLobby && CanEnableDebug)
            {
                NetworkPlayer lo = NetworkPlayer.LocalPlayer;
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    showNetDebug = !showNetDebug;
                    LogDebug($"[DEBUG] Toggled networking debugging UI {showNetDebug}.");
                }
                if (Input.GetKeyDown(KeyCode.F11))
                {
                    ChatUI.Hide(!ChatUI.Instance.chatPanel.activeSelf);
                    LogDebug($"[DEBUG] Toggled chat {ChatUI.Instance.chatPanel.activeSelf}.");
                }
                else if (Input.GetKeyDown(KeyCode.F2))
                {
                    LogDebug($"[DEBUG] Teleported testing Dummy to your location! (Keep holding to make it follow)");
                    lo.testPlayer = true;
                    lo.ToggleRig(true);
                    lo.UpdateSkin(currentSkin);
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
            if (Input.GetKeyDown(KeyCode.F5) && CanEnableDebug)
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
                if (HasDiscord)
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
            if (SceneHelper.CurrentScene == "Level 8-4" && StatsManager.Instance.kills >= 7)
            {
                return true;
            }
            return false;
        }
        public void FraudThreeRotators()
        {
            string[] rotators = new string[]
            {
                "Pre-Space/Rooms/Red Path (Clockwork)/R1 - Clockwork Tutorial/R1 Stuff/RotatorSwitch",
                "Pre-Space/Rooms/Red Path (Clockwork)/R2 - Clockwork Arena/R2 Stuff/RotatorSwitch",
                "Pre-Space/Rooms/Red Path (Clockwork)/R3 - Clockwork Crushers/R3 Stuff/RotatorSwitch",
                "Pre-Space/Rooms/Red Path (Clockwork)/R4 - Clockwork Lava/R4 Stuff/Portal Stuff/RotatorSwitch (1)",
                "Pre-Space/Rooms/Red Path (Clockwork)/R5 - Clockwork Climb/R5 Stuff/RotatorSwitch"
            };
            foreach(var ro in rotators)
            {
                GameObject actualObj = TryGetSceneObject(ro);
                actualObj?.gameObject.SetActive(NetworkManager.HostAndConnected);
            }
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
                if(SceneHelper.CurrentScene == "Level 8-3")
                {
                    FraudThreeRotators();
                }
                Application.runInBackground = true;
                DeadPatch.SpectateOnDeath = NetworkManager.Instance.CurrentLobby.MemberCount > 1 || !NetworkManager.Sandbox;
                DeadPatch.TickTimer();
                BannedModsDetector.Tick();
                if (CheatsController.Instance.cheatsEnabled && NetworkManager.Instance.CurrentLobby.GetData("cheat") == "0" && NetworkManager.ClientAndConnected)
                {
                    foreach(var cheats in CheatsManager.Instance.allRegisteredCheats)
                    {
                        foreach(var c in cheats.Value)
                        {
                            if(c.IsActive)
                            {
                                CheatsManager.Instance.DisableCheat(c);
                            }
                        }
                    }
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
                if (cam != null && lowPass != null && !MonoSingleton<UnderwaterController>.Instance.inWater)
                {
                    lowPass.enabled = (!NetworkPlayer.selfIsGhost) ? false : true;
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
            }
            else
            {
                DeadPatch.SpectateOnDeath = false;
                Application.runInBackground = false;
                immuneToDeath = false;
            }
        }
        public void LateUpdate()
        {
            if (SceneHelper.CurrentScene == "Intro" || SceneHelper.CurrentScene == "Bootstrap" || SceneHelper.CurrentScene == "Main Menu")
            {
                return;
            }
            cam = MonoSingleton<CameraController>.Instance.cam;
            if (cam != null)
            {
                lowPass = cam.GetComponent<AudioLowPassFilter>();
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
        public void HandleSkin()
        {
            if (namePlate.value.Length > 5)
            {
                namePlate.value = namePlate.value.Substring(0, 5);
            }
            currentSkin = GetSkin(baseColor.value, lightColor.value, wingLightColor.value, metalColor.value, shinyness.value, namePlate.value, namePlateColor.value);
            if (NetworkManager.InLobby)
            {
                Skin skin = currentSkin;
                PacketWriter write = new PacketWriter();
                write.WriteSkin(skin);
                NetworkManager.Instance.BroadcastPacket(PacketType.Skin, write.GetBytes());
                if (NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.UpdateSkin(skin);
                }
                ArmCheck(SwapWeaponsPatch.AltWeapon(GunControl.Instance.currentWeapon));
                AnimationCheck();
            }
            SkinManagerV2.SetIcon(NetworkManager.Id);
            VoiceUI.RefreshIcons(useSkinInsteadOfPFP.value);
        }

        public void Update()
        {
            try
            {
                ttsHurtAndDeath.hidden = !canTTS.value;
                ttsChat.hidden = !canTTS.value;
                ttsSpeed.hidden = !canTTS.value;
                ttsPitch.hidden = !canTTS.value;
                ttsMouth.hidden = !canTTS.value;
                ttsThroat.hidden = !canTTS.value;

                customSearchFolder.hidden = searchFolderEnum.value != SearchFolder.Other;

                if (currentSkin.Base == Color.clear)
                {
                    if (!hasHadRandomSkin.value)
                    {
                        baseColor.value = Random.ColorHSV();
                        lightColor.value = Random.ColorHSV();
                        wingLightColor.value = Random.ColorHSV();
                        metalColor.value = Random.ColorHSV();
                        shinyness.value = Random.Range(0f, 1f);
                        namePlate.value = $"V{Random.Range(1, 1000)}";
                        namePlateColor.value = Random.ColorHSV();

                        hasHadRandomSkin.value = true;
                    }
                    HandleSkin();
                    SkinSaver.CurrentSkin.name = currentSkin.Nameplate;
                    SkinSaver.CurrentSkin.data = currentSkin;
                }
                if(!hasHadRandomPitch.value)
                {
                    ttsPitch.value = Random.Range(32, 128);
                    SamPitch.ReUpdateConfigSam();
                    hasHadRandomPitch.value = true;
                }
                Inputs();
                TryRunCalls();
                if(SpecialHudCameraAddPatch.specialHud != null)
                {
                    SpecialHudCameraAddPatch.specialHud.SetActive(NetworkManager.InLobby);
                    Camera camera = SpecialHudCameraAddPatch.specialHud.GetComponent<Camera>();
                    if(camera != null)
                    {
                        camera.fieldOfView = cam.fieldOfView;
                    }
                }
                if (NewMovement.Instance != null) HandleTargetData();

                if (currentUi != null && SceneHelper.CurrentScene != "Main Menu")
                {
                    if (polrMM != null)
                    {
                        currentUi.SetActive(MonoSingleton<OptionsManager>.Instance.paused && !SceneHelper.Instance.loadingBlocker.activeSelf);
                        if (polrMM.mainPanel.activeSelf && MonoSingleton<OptionsManager>.Instance.paused && MonoSingleton<OptionsManager>.Instance.pauseMenu != null)
                        {
                            TogglePauseMenu(false);
                            PolarMenuActive = true;
                        }
                        if (!polrMM.mainPanel.activeSelf && MonoSingleton<OptionsManager>.Instance.paused && MonoSingleton<OptionsManager>.Instance.pauseMenu != null)
                        {
                            TogglePauseMenu(true);
                            PolarMenuActive = false;
                        }
                        if (!MonoSingleton<OptionsManager>.Instance.paused)
                        {
                            polrMM.mainPanel.SetActive(false);
                            PolarMenuActive = false;
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
                            if(NetworkManager.HostAndConnected)
                            {
                                polrMM.saveLobSettings.gameObject.SetActive(NetworkManager.Instance.currentTypeRaw != polrMM.lobbyType.value || NetworkManager.Instance.currentMaxPlayers.ToString() != polrMM.maxP.text || NetworkManager.Instance.currentLobbyName != polrMM.lobbyName.text || NetworkManager.Instance.currentCheats != polrMM.canCheat.value);
                                if (polrMM.saveLobSettings.gameObject.activeSelf)
                                {
                                    polrMM.lowerMaxPWarn.gameObject.SetActive(int.Parse(polrMM.maxP.text) < NetworkManager.Instance.currentMaxPlayers);
                                }
                                else
                                {
                                    polrMM.lowerMaxPWarn.gameObject.SetActive(false);
                                }
                            }
                            else
                            {
                                polrMM.saveLobSettings.gameObject.SetActive(false);
                                polrMM.lowerMaxPWarn.gameObject.SetActive(false);
                            }
                            GameObject notif = polrMM.notifBox;
                            if(notif != null)
                            {
                                if(!NetworkManager.InLobby || !debugMode)
                                {
                                    NetworkDebugRef.SetUIActive(false);
                                }
                                else
                                {
                                    NetworkDebugRef.SetUIActive(showNetDebug);
                                    if (showNetDebug)
                                    {
                                        INetworkObject obj = NetworkDebugRef.GetNearestObject(MonoSingleton<CameraController>.Instance.transform.position);
                                        NetworkDebugRef.Update(Net.List.netTimer, Net.List.Objects.Count, obj);
                                    }
                                }
                            }
                        }
                    }
                    else if (currentUi != null)
                    {
                        currentUi.SetActive(true);
                        currentUi.transform.Find("PolariteMenu").TryGetComponent<PolariteMenuManager>(out var menu);
                        if (menu != null)
                        {
                            polrMM = menu;
                        }
                    }
                }
                if (currentUi != null && SceneHelper.CurrentScene == "Main Menu")
                {
                    currentUi.SetActive(false);
                }
                InNet();
            }
            catch
            {
                // this single update loop causes way too many errors
            }
        }
        public void TogglePauseMenu(bool value)
        {
            foreach (Transform c in MonoSingleton<OptionsManager>.Instance.pauseMenu.transform)
            {
                if (c.name != "Level Stats")
                {
                    c.gameObject.SetActive(value);
                }
            }
            MonoSingleton<OptionsManager>.Instance.pauseMenu.GetComponent<Image>().enabled = value;
        }
        public static void CustomTogglePlayer(bool val)
        {
            if (ChatUI.isTyping)
            {
                val = false;
            }
            if (val == plrActive)
            {
                CameraController.Instance.activated = val;
                GunControl.Instance.activated = val;
                NewMovement.Instance.activated = val;
                return;
            }
            if (!NetworkManager.InLobby)
            {
                val = true;
            }
            GameStateManager.Instance.CameraLocked = !val;
            GameStateManager.Instance.TimerModifier = 1f;
            GameStateManager.Instance.PlayerInputLocked = !val;
            CameraController.Instance.activated = val;
            GunControl.Instance.activated = val;
            if (val)
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
            GameObject notifUi = Instantiate(mainBundle.LoadAsset<GameObject>("NotifBoxCanvas"));
            try
            {
                if (uiObj != null)
                {
                    DontDestroyOnLoad(uiObj);
                    currentUi = uiObj;


                    TextMeshProUGUI tag = currentUi.transform.Find("BuildTag").GetComponent<TextMeshProUGUI>();
                    PolariteMenuManager pMM = currentUi.transform.Find("PolariteMenu").gameObject.AddComponent<PolariteMenuManager>();
                    if (pMM != null)
                    {
                        BuildTag bTag = JsonUtility.FromJson<BuildTag>(File.ReadAllText(Path.Combine(Directory.GetParent(Info.Location).FullName, "debug_info.json")));
                        if (bTag.debug)
                        {
                            tag.text = $"BUILD_ID: {bTag.buildName}";
                        }
                        else
                        {
                            tag.gameObject.SetActive(false);
                        }
                        pMM.uiOpen = null;
                        if (!IsFoolsDay())
                        {
                            pMM.uiOpen = pMM.transform.Find("Activate").GetComponent<Button>();
                            pMM.transform.Find("ActivateApr").gameObject.SetActive(false);
                        }
                        else
                        {
                            pMM.uiOpen = pMM.transform.Find("ActivateApr").GetComponent<Button>();
                            pMM.transform.Find("Activate").gameObject.SetActive(false);
                        }
                        pMM.uiOpen.onClick.AddListener(pMM.ToggleMainPanel);

                        Transform motd = pMM.transform.Find("Main").Find("MOTD");

                        Image img = motd.Find("MOTDPfp").GetComponent<Image>();
                        TextMeshProUGUI textName = motd.Find("MOTDUser").GetComponent<TextMeshProUGUI>();
                        TextMeshProUGUI textMsg = motd.Find("MOTDText").GetComponent<TextMeshProUGUI>();

                        Sprite unknown = mainBundle.LoadAsset<Sprite>("unknown");
                        XServers.HasInternet((val) =>
                        {
                            if (val)
                            {
                                img.sprite = unknown;
                                textMsg.text = "???";
                                textName.text = "(Loading MOTD...)";
                                XServers.GetMOTD((pfp, user, msg) =>
                                {
                                    img.sprite = pfp;
                                    Typewriter($"(MOTD wrote by{user})", 0.01f, textName);
                                    Typewriter(msg, 0.001f, textMsg);
                                }, (pfpF, userF, msgF) =>
                                {
                                    img.sprite = pfpF;
                                    Typewriter($"(MOTD wrote by{userF})", 0.01f, textName);
                                    Typewriter(msgF, 0.001f, textMsg);
                                });
                            }
                            else
                            {
                                img.sprite = unknown;
                                textMsg.text = "???";
                                textName.text = "Couldn't get if you had internet connection...";
                            }
                        });

                        Transform host = pMM.transform.Find("Main").Find("Host");
                        Transform join = pMM.transform.Find("Main").Find("Join");
                        Transform publicLobbies = pMM.transform.Find("Main").Find("PublicLobbies");
                        Transform playerList = pMM.transform.Find("Main").Find("PlayerList");
                        Transform pirateGuide = pMM.transform.Find("Main").Find("PiratesGuide").Find("PiratesGuideBG");
                        Transform blueScreen = pMM.transform.parent.Find("BackgroundStuff");
                        TextMeshProUGUI ver = pMM.transform.Find("Main").Find("Ver").GetComponent<TextMeshProUGUI>();

                        pMM.maxP = host.Find("MaxPlayers").GetComponent<TMP_InputField>();
                        pMM.lobbyName = host.Find("UsefulInputField").GetComponent<TMP_InputField>();
                        pMM.lobbyType = host.Find("UsefulDropdown").GetComponent<TMP_Dropdown>();
                        pMM.mainPanel = pMM.transform.Find("Main").gameObject;
                        pMM.code = join.Find("UsefulInputField").GetComponent<TMP_InputField>();
                        pMM.canCheat = host.Find("CanCheat").GetComponent<TMP_Dropdown>();

                        pMM.statusHost = host.Find("Status").GetComponent<TextMeshProUGUI>();
                        pMM.statusJoin = join.Find("Status").GetComponent<TextMeshProUGUI>();

                        string serverStat = "";
                        switch (NetworkManager.cMode.server_mode)
                        {
                            case "steam":
                                serverStat = "<color=#66c0f4>Steam</color>";
                                break;
                            case "pirated":
                                serverStat = "<color=#ff8522>Pirated</color>";
                                break;
                        }
                        pMM.transform.Find("Main").Find("ConnectedTo?").GetComponent<TextMeshProUGUI>().text = $"Currently connected to {serverStat} servers.";

                        TextMeshProUGUI warnText = host.Find("WarnText").GetComponent<TextMeshProUGUI>();
                        Button leave = pMM.transform.Find("Main").Find("Leave").GetComponent<Button>();
                        Button invite = host.Find("UsefulButton (1)").GetComponent<Button>();
                        Button create = host.Find("UsefulButton").GetComponent<Button>();
                        Button saveChanges = host.Find("SaveButton").GetComponent<Button>();
                        Button joinL = join.Find("UsefulButton").GetComponent<Button>();
                        Button copyCode = host.Find("CopyCode").GetComponent<Button>();
                        
                        Button onPublicClick = pMM.transform.Find("Main").Find("UsefulButton (3)").GetComponent<Button>();
                        Button refresh = publicLobbies.Find("RefreshButton").GetComponent<Button>();
                        Button pList = pMM.transform.Find("Main").Find("PlayerListButton").GetComponent<Button>();
                        Button discord = pMM.transform.Find("Main").Find("JoinDiscord").GetComponent<Button>();
                        Button donate = pMM.transform.Find("Main").Find("Donate").GetComponent<Button>();
                        Button gameFolder = pirateGuide.Find("UsefulButton").GetComponent<Button>();
                        Button noRead = pirateGuide.Find("UsefulButton (2)").GetComponent<Button>();
                        Button refreshMotd = motd.Find("MOTDRefresh").GetComponent<Button>();
                        Button backTrack = pMM.transform.Find("Main").Find("BackTrackLikeItsBackOnTrack").GetComponent<Button>();

                        gameFolder.onClick.AddListener(() => Application.OpenURL(Directory.GetParent("server_config.json").FullName));
                        noRead.onClick.AddListener(() => Application.OpenURL("https://www.youtube.com/watch?v=LKeof3dleS0"));
                        leave.onClick.AddListener(() => NetworkManager.Instance.LeaveLobby());
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
                            // use the shortcut link incase we have another server nuking :(
                            Application.OpenURL((!IsFoolsDay()) ? "https://polaritemod.com/discord" : "https://www.youtube.com/watch?v=dQw4w9WgXcQ");
                        });
                        donate.onClick.AddListener(() =>
                        {
                            Application.OpenURL("https://ko-fi.com/albertdevstein");
                        });
                        onPublicClick.onClick.AddListener(PublicLobbyManager.RefreshLobbies);
                        refresh.onClick.AddListener(PublicLobbyManager.RefreshLobbies);
                        pList.onClick.AddListener(PlayerList.UpdatePList);
                        refreshMotd.onClick.AddListener(() =>
                        {
                            XServers.HasInternet((val) =>
                            {
                                if (val)
                                {
                                    img.sprite = unknown;
                                    textMsg.text = "???";
                                    textName.text = "(Refreshing MOTD...)";
                                    XServers.GetMOTD((pfp, user, msg) =>
                                    {
                                        img.sprite = pfp;
                                        Typewriter($"(MOTD wrote by{user})", 0.01f, textName);
                                        Typewriter(msg, 0.001f, textMsg);
                                    }, (pfpF, userF, msgF) =>
                                    {
                                        img.sprite = pfpF;
                                        Typewriter($"(MOTD wrote by{userF})", 0.01f, textName);
                                        Typewriter(msgF, 0.001f, textMsg);
                                    });
                                }
                                else
                                {
                                    img.sprite = unknown;
                                    textMsg.text = "???";
                                    textName.text = "Couldn't get if you had internet connection...";
                                }
                            });
                        });

                        backTrack.onClick.AddListener(() =>
                        {
                            XServers.HasInternet((val) =>
                            {
                                if (val)
                                {
                                    img.sprite = unknown;
                                    textMsg.text = "???";
                                    textName.text = "(Loading MOTD...)";
                                    XServers.GetMOTD((pfp, user, msg) =>
                                    {
                                        img.sprite = pfp;
                                        Typewriter($"(MOTD wrote by{user})", 0.01f, textName);
                                        Typewriter(msg, 0.001f, textMsg);
                                    }, (pfpF, userF, msgF) =>
                                    {
                                        img.sprite = pfpF;
                                        Typewriter($"(MOTD wrote by{userF})", 0.01f, textName);
                                        Typewriter(msgF, 0.001f, textMsg);
                                    });
                                }
                                else
                                {
                                    img.sprite = unknown;
                                    textMsg.text = "???";
                                    textName.text = "Couldn't get if you had internet connection...";
                                }
                            });
                        });

                        pMM.uiOpen.onClick.AddListener(() =>
                        {
                            XServers.HasInternet((val) =>
                            {
                                if (val)
                                {
                                    img.sprite = unknown;
                                    textMsg.text = "???";
                                    textName.text = "(Loading MOTD...)";
                                    XServers.GetMOTD((pfp, user, msg) =>
                                    {
                                        img.sprite = pfp;
                                        Typewriter($"(MOTD wrote by{user})", 0.01f, textName);
                                        Typewriter(msg, 0.001f, textMsg);
                                    }, (pfpF, userF, msgF) =>
                                    {
                                        img.sprite = pfpF;
                                        Typewriter($"(MOTD wrote by{userF})", 0.01f, textName);
                                        Typewriter(msgF, 0.001f, textMsg);
                                    });
                                }
                                else
                                {
                                    img.sprite = unknown;
                                    textMsg.text = "???";
                                    textName.text = "Couldn't get if you had internet connection...";
                                }
                            });
                        });

                        saveChanges.onClick.AddListener(() =>
                        {
                            NetworkManager.Instance.ChangeLobbySettings(int.Parse(pMM.maxP.text), ParseLobbyType(pMM.lobbyType), pMM.canCheat.value == 0 ? false : true, pMM.lobbyName.text);
                        });
                        pMM.saveLobSettings = saveChanges;
                        pMM.lowerMaxPWarn = warnText;

                        PublicLobbyManager.Content = publicLobbies.Find("LobbyList").Find("Content");
                        PlayerList.ContentB = playerList.Find("List").Find("Content");

                        Typewriter(Version, 0.025f, ver);
                        ver.color = (ReleaseBuild) ? Color.white : Color.yellow;


                        leaveButton = leave.gameObject;
                        joinButton = joinL.gameObject;
                        hostButton = create.gameObject;
                        copyButton = copyCode.gameObject;
                        inviteButton = invite.gameObject;
                        playerListButton = pList.gameObject;

                        saveChanges.gameObject.SetActive(false);
                        warnText.gameObject.SetActive(false);
                        leave.gameObject.SetActive(false);
                        invite.gameObject.SetActive(false);
                        host.gameObject.SetActive(false);
                        join.gameObject.SetActive(false);
                        publicLobbies.gameObject.SetActive(false);
                        copyCode.gameObject.SetActive(false);
                        playerList.gameObject.SetActive(false);
                        pList.gameObject.SetActive(false);
                        backTrack.gameObject.SetActive(false);
                        pirateGuide.parent.gameObject.SetActive(false);
                        pMM.mainPanel.SetActive(false);

                        pMM.lobbyName.text = $"{NetworkManager.GetNameOfId(NetworkManager.Id)}'s Lobby";
                        pMM.notifBox = notifUi;
                        pMM.redFlash = notifUi.transform.Find("RedFlash").gameObject;
                        pMM.blueFlash = notifUi.transform.Find("BlueFlash").gameObject;
                        pMM.ghostFlash = notifUi.transform.Find("GhostFlash").gameObject;

                        pMM.redFlash.SetActive(false);
                        pMM.blueFlash.SetActive(false);
                        pMM.ghostFlash.SetActive(false);
                        DontDestroyOnLoad(notifUi);
                        polrMM = pMM;
                        message = notifUi.GetComponentInChildren<AudioSource>().clip;

                        // network debug ui
                        Transform debugUI = notifUi.transform.Find("NetworkDebug");
                        if(debugUI != null)
                        {
                            Transform netTick = debugUI.Find("NetTick");
                            Transform count = debugUI.Find("Count");
                            Transform id = debugUI.Find("ID");
                            NetworkDebugRef.mainObject = debugUI.gameObject;
                            NetworkDebugRef.netObjUI = id.gameObject;
                            NetworkDebugRef.netTick = netTick.GetComponentInChildren<TextMeshProUGUI>();
                            NetworkDebugRef.count = count.GetComponentInChildren<TextMeshProUGUI>();

                            NetworkDebugRef.simpleId = id.Find("simpleId").GetComponent<TextMeshProUGUI>();
                            NetworkDebugRef.owner = id.Find("owner").GetComponent<TextMeshProUGUI>();
                            NetworkDebugRef.index = id.Find("index").GetComponent<TextMeshProUGUI>();
                            NetworkDebugRef.alive = id.Find("alive").GetComponent<TextMeshProUGUI>();
                            NetworkDebugRef.syncTransform = id.Find("syncTransform").GetComponent<TextMeshProUGUI>();

                            id.gameObject.SetActive(false);
                            NetworkDebugRef.SetUIActive(false);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logs.Error("Attempt at making UI failed. Reason: " + ex, this);
                MonoSingleton<HudMessageReceiver>.Instance.SendHudMessage("It seems the mod assets are <b><i><color=red>OUT OF DATE!</b></i></color> Either you're trying to get an updated version of Polarite early, or it's something beyond my knowledge. Feel free to screenshot the console, the error will be there.");
                Destroy(uiObj);
                Destroy(notifUi);
            }
        }
        private LobbyType ParseLobbyType(TMP_Dropdown dropdown)
        {
            LobbyType type;
            switch (dropdown.value)
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
            return type;
        }
        public int LobbyTypeToRaw(LobbyType type)
        {
            int typeRaw;
            switch (type)
            {
                case LobbyType.Public:
                    typeRaw = 0;
                    break;
                case LobbyType.FriendsOnly:
                    typeRaw = 1;
                    break;
                case LobbyType.Private:
                    typeRaw = 2;
                    break;
                default:
                    typeRaw = 3;
                    break;
            }
            return typeRaw;
        }
        private async void CreateLobby()
        {
            PolariteMenuManager pMM = currentUi.GetComponentInChildren<PolariteMenuManager>();
            int max = int.Parse(pMM.maxP.text);
            string lobbyName = pMM.lobbyName.text;
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = $"{NetworkManager.GetNameOfId(NetworkManager.Id)}'s Lobby";
            }
            LobbyType type;
            bool allowCheats = false;
            switch (pMM.lobbyType.value)
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
            switch (pMM.canCheat.value)
            {
                case 0:
                    allowCheats = false;
                    break;
                case 1:
                    allowCheats = true;
                    break;
            }
            if (max > 250)
            {
                max = 250;
                pMM.maxP.text = "250";
            }
            await NetworkManager.Instance.CreateLobby(max, type, lobbyName, (string c) =>
            {
                pMM.codeHost = c;
            }, allowCheats);
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
                    if (v2Patch)
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
                SkinPreview preview = MonoSingleton<CameraController>.Instance.gameObject.GetComponent<SkinPreview>();
                if (MonoSingleton<CameraController>.Instance.GetComponent<SkinPreview>() == null)
                {
                    preview = MonoSingleton<CameraController>.Instance.gameObject.AddComponent<SkinPreview>();
                }
            }
        }
        public IEnumerator PsstCheck()
        {
            if(!openedPolariteMenu.value)
            {
                if(NetworkManager.InLobby)
                {
                    XServers.VisualNotif("Pss- Wait what?", true);
                    yield return new WaitForSeconds(4f);
                    ForceHideNotif();
                    yield return new WaitForSeconds(0.25f);
                    XServers.VisualNotif("You're already in a lobby...?", true);
                    yield return new WaitForSeconds(4.5f);
                    ForceHideNotif();
                    yield return new WaitForSeconds(0.25f);
                    XServers.VisualNotif("Oh whatever...", true);
                    yield return new WaitForSeconds(2f);
                    ForceHideNotif();
                    yield return new WaitForSeconds(0.25f);

                    bool passedCheck1 = false;
                    XServers.VisualNotif("Open the pause menu.", true);
                    while (!OptionsManager.Instance.paused && !passedCheck1)
                    {
                        yield return null;
                    }
                    ForceHideNotif();
                    yield return new WaitForSecondsRealtime(0.25f);
                    passedCheck1 = true;
                    XServers.VisualNotif("Click on the globe icon to access the Polarite Menu, where you can join/make lobbies.", true);
                }
                else
                {
                    bool passedCheck1 = false;
                    XServers.VisualNotif("Psst... Open the pause menu...", true);
                    while (!OptionsManager.Instance.paused && !passedCheck1)
                    {
                        yield return null;
                    }
                    ForceHideNotif();
                    yield return new WaitForSecondsRealtime(0.25f);
                    passedCheck1 = true;
                    XServers.VisualNotif("Click on the globe icon to access the Polarite Menu, where you can join/make lobbies.", true);
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
            if (SpectatorCam.isSpectating)
            {
                SpectatorCam.isSpectating = false;
            }
            if (SceneHelper.CurrentScene != "Main Menu" && currentUi == null)
            {
                CreatePolariteUI();
            }
            if (SceneHelper.CurrentScene != "Main Menu" && VoiceUI.currentCanvas == null)
            {
                VoiceUI.MakeUI();
            }
            SceneObjectCache.Init();
            if (NetworkManager.HostAndConnected)
            {
                NetworkManager.Instance.CurrentLobby.SetData("levelName", GetLevelName());
                NetworkManager.Instance.CurrentLobby.SetData("cyberHe", "");
                NetworkManager.Instance.CurrentLobby.SetData("cyberPr", "");
                NetworkManager.Instance.CurrentLobby.SetData("cyberWave", "");
                NetworkManager.Instance.CurrentLobby.SetData("levelStarted", "0");
            }
            if (HasDiscord)
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
            Instance.StartCoroutine(DelayPsstCheck());
            NetworkManager.WasUsed = NetworkManager.InLobby;
            if (NetworkManager.InLobby)
            {
                NetworkPlayer.ToggleEidForAll(true);
                CleanLevel();
                foreach (var p in NetworkManager.players.Values)
                {
                    p.ToggleRig(true);
                    p.isGhost = false;
                }
                ArmCheck(SwapWeaponsPatch.AltWeapon(MonoSingleton<GunControl>.Instance.currentWeapon));
                AnimationCheck();
            }
            NetworkPlayer.selfIsGhost = false;
            NetworkEnemy.Flush();
            immuneToDeath = false;
            if (SceneHelper.CurrentScene == "Level 0-4" && StockMapInfo.Instance != null && NetworkManager.InLobby)
            {
                StockMapInfo.Instance.levelName = $"A {NetworkManager.Instance.CurrentLobby.MemberCount}-MACHINE ARMY";
            }
            Instance.StartCoroutine(UnpauseNet());
            if (NetworkManager.InLobby)
            {
                CustomTogglePlayer(true);
            }
            AttemptToAddListener();
            if (polrMM != null)
            {
                ForceHideNotif();
            }
            canBecomeGhost = true;
            SkinManagerV2.Clear();
            NetworkPlayer.Shopping = false;
            NetworkManager.LocPlayerCheck();
            Net.List?.Clear();
        }
        public void ForceHideNotif()
        {
            Transform box = polrMM.notifBox.transform.Find("Box");
            Transform up = polrMM.notifBox.transform.Find("BoxUp");
            MoveY(box.GetComponent<RectTransform>(), up.GetComponent<RectTransform>().position.y);
            XServers.canShowNotif = true;
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
        public static AudioSource SpawnSound(AudioClip clip, float pitch, Transform parent, float volume, Vector3 overridePos = default, bool dontRemove = false)
        {
            AudioSource audioSource = new GameObject(clip.name).AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.pitch = pitch;
            audioSource.volume = volume;
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 30f;
            audioSource.minDistance = 100f;
            if(!dontRemove) audioSource.gameObject.AddComponent<RemoveOnTime>().time = clip.length;
            audioSource.transform.SetParent(parent, (overridePos == default) ? false : true);
            audioSource.transform.position = (overridePos == default && parent != null) ? parent.position : overridePos;
            audioSource.Play();
            return audioSource;
        }
        public static void SpectatePlayers(bool loadAll)
        {
            if (ignoreSpectate)
            {
                return;
            }
            Instance.StartCoroutine(SpectatePlayersB(loadAll));
            NetworkManager.SceneLoading = false;
            Net.Unpause();
        }
        public static void LeaveEffect(Vector3 pos)
        {
            GameObject deleterEffect = Addressables.LoadAssetAsync<GameObject>("Assets/Particles/SandboxDeleterEffect.prefab").WaitForCompletion();
            AudioClip sound = Addressables.LoadAssetAsync<AudioClip>("Assets/Sounds/Weapons/SandboxDelete.wav").WaitForCompletion();
            Instantiate(deleterEffect, pos, Quaternion.identity);
            SpawnSound(sound, 1f, null, 1f, pos);
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
            foreach (var p in NetworkManager.players)
            {
                if (p.Value != NetworkPlayer.LocalPlayer)
                {
                    playerTransforms.Add(p.Value.transform);
                }
            }
            cam.GetComponent<CameraController>().enabled = false;
            MonoSingleton<NewMovement>.Instance.DeactivatePlayer();
            cam.StartSpectating(playerTransforms);
            if (loadAll)
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
            if (NetworkPlayer.selfIsGhost == val)
            {
                if (val)
                {
                    DeadPatch.Respawn(new Vector3(0f, 80f, 62.5f), CameraController.Instance.transform.rotation);
                }
                return;
            }
            if (val)
            {
                bool wasLast = NetworkPlayer.LastPlayerAlive();
                DeadPatch.Respawn(new Vector3(0f, 80f, 62.5f), CameraController.Instance.transform.rotation);
                ChatUI.Instance.ForceOff();
                SpawnSound(mainBundle.LoadAsset<AudioClip>("GhostTransform2"), Random.Range(0.95f, 1.15f), CameraController.Instance.transform, 1f);
                if (CyberSync.Active)
                {
                    ChatUI.Instance.OnSubmitMessage($"<color=#91FFFF>You're now a ghost. Wait for the next wave to respawn! Currently, {NetworkPlayer.PlayersAlive() - 1} remain.</color>", false, $"<color=#91FFFF>You're now a ghost. Wait for the next wave to respawn! Currently, {NetworkPlayer.PlayersAlive()} remain.</color>", tts: false);
                    ChatUI.Instance.ShowUIForBit(5f);
                }
                else
                {
                    ChatUI.Instance.OnSubmitMessage($"<color=#91FFFF>You're now a ghost.</color>", false, $"<color=#91FFFF>You're now a ghost.</color>", tts: false);
                    ChatUI.Instance.ShowUIForBit(5f);
                }
                NetworkPlayer.selfIsGhost = true;

                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.BecameGhost, w.GetBytes());
                CameraController.Instance.cameraShaking = 0;
                DoubleCheckForSoftlock();
                if(!wasLast)
                {
                    Flash(false, true);
                }
            }
            else
            {
                GunControl.Instance.YesWeapon();
                FistControl.Instance.YesFist();
                NetworkPlayer.selfIsGhost = false;
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.ReviveGhost, w.GetBytes());
                Flash(true, false);
            }
        }
        public void AttemptToAddListener()
        {
            XServers.HasInternet((wedo) =>
            {
                if (wedo)
                {
                    GlobalNotificationListener listener = gameObject.GetComponent<GlobalNotificationListener>();
                    if (listener == null)
                    {
                        listener = gameObject.AddComponent<GlobalNotificationListener>();
                    }
                }
            });
        }
        public static void GameOver()
        {
            canBecomeGhost = false;
            Ghost(false);
            ForceKillSelf();
        }
        public static IEnumerator DelayPsstCheck()
        {
            if(SceneHelper.CurrentScene == "Main Menu")
            {
                yield break;
            }
            yield return new WaitForSecondsRealtime(2.5f);
            Instance.StartCoroutine(Instance.PsstCheck());
        }
        public static IEnumerator DelayLoadServerPattern(int wav)
        {
            yield return new WaitForSeconds(1f);
            CyberSync.BasicLoad(CyberSync.LobbyPattern, wav);
        }
        public static IEnumerator UnpauseNet()
        {
            if(!NetworkManager.InLobby)
            {
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
            Net.Unpause();
            NetworkManager.Instance.SceneLoad();
            try
            {
                if (!MonoSingleton<OnLevelStart>.Instance.activated && NetworkManager.ClientAndConnected && NetworkManager.Instance.CurrentLobby.GetData("levelStarted") == "1")
                {
                    MonoSingleton<OnLevelStart>.Instance.StartLevel();
                }
            }
            catch (Exception)
            {
                // ...
            }
        }
        public IEnumerator GooglePing(System.Action<bool> onComplete)
        {
            Ping ping = new Ping("8.8.8.8");
            while (!ping.isDone)
            {
                yield return null;
            }
            if (ping.time > 0 && ping.time < 8000)
            {
                onComplete?.Invoke(true);
                XServers.internet = true;
                ping.DestroyPing();
                yield break;
            }
            XServers.internet = false;
            ping.DestroyPing();
            onComplete?.Invoke(false);
        }
        // code by Xulfur, thank you :)
        public IEnumerator MOTDGet(System.Action<Sprite, string, string> onComplete, System.Action<Sprite, string, string> onFail)
        {
            byte[] rawbytes;
            Texture2D texture = new Texture2D(2, 2);
            string username = "username";
            string message = "MOTD";
            using (UnityWebRequest www = UnityWebRequest.Get("https://polaritemod.com/motd/motd.txt"))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Logs.Error(www.error + " " + www.downloadHandler.text, this);
                    onFail?.Invoke(mainBundle.LoadAsset<Sprite>("unknown"), "???", $"Failed to retrieve MOTD. ({www.error})");
                    yield break;
                }
                else
                {
                    Logs.Info("[MOTD] Server replied with: " + www.downloadHandler.text + " Proceeding to extract user PFP.", name: "X-Point");
                    string regex = @"pfp:.[^ ]+";
                    RegexOptions options = RegexOptions.Multiline;
                    string output = string.Empty;
                    string urlimg = string.Empty;

                    foreach (Match m in Regex.Matches(www.downloadHandler.text, regex, options))
                    {
                        output = output + www.downloadHandler.text.Replace(m.Value, string.Empty);
                        urlimg = urlimg + m.Value.Replace("pfp:", string.Empty);
                    }
                    using (UnityWebRequest img = UnityWebRequest.Get(urlimg))
                    {
                        yield return img.SendWebRequest();
                        if (img.result != UnityWebRequest.Result.Success)
                        {
                            Logs.Info(img.error + " " + img.downloadHandler.text, name: "X-Point");
                            onFail?.Invoke(mainBundle.LoadAsset<Sprite>("unknown"), "???", $"Failed to retrieve MOTD. ({img.error})");
                            yield break;
                        }
                        else
                        {
                            Logs.Info("[MOTD] PFP server replied with bytes: " + img.downloadHandler.data, name: "X-Point");
                            //reconstruct image
                            Texture2D tex = new Texture2D(2, 2);
                            rawbytes = img.downloadHandler.data;
                            if (ImageConversion.LoadImage(tex, rawbytes))
                            {
                                texture = tex;
                            }
                            else
                            {
                                texture = mainBundle.LoadAsset<Sprite>("unknown").texture;
                            }
                            string usernameregex = @".+:";
                            RegexOptions usernamer = RegexOptions.IgnoreCase;
                            // only needs one match, more than one could break it
                            Match m = Regex.Match(output, usernameregex, usernamer);
                            if (m.Success)
                            {
                                string usernameout = m.Groups[0].Value;
                                usernameout = usernameout.Replace(":", string.Empty);
                                username = usernameout;
                                output = output.Replace(m.Groups[0].Value, string.Empty);
                                message = output;
                                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector3.zero);
                                sprite.texture.filterMode = FilterMode.Point;

                                onComplete?.Invoke(sprite, username, message);
                            }
                        }
                    }
                }
            }
        }
        public IEnumerator PFPGet(System.Action<Sprite> onComplete, string url)
        {
            byte[] rawbytes;
            Texture2D texture = new Texture2D(2, 2);
            using (UnityWebRequest img = UnityWebRequest.Get(url))
            {
                yield return img.SendWebRequest();
                if (img.result != UnityWebRequest.Result.Success)
                {
                    Logs.Error(img.error + " " + img.downloadHandler.text, name: "X-Point");
                }
                else
                {
                    Logs.Info("[PFP] Server replied with bytes: " + img.downloadHandler.data, name: "X-Point");
                    //reconstruct image
                    Texture2D tex = new Texture2D(2, 2);
                    rawbytes = img.downloadHandler.data;
                    if (ImageConversion.LoadImage(tex, rawbytes))
                    {
                        texture = tex;
                    }
                    else
                    {
                        texture = mainBundle.LoadAsset<Sprite>("unknown").texture;
                    }
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector3.zero);
                    sprite.texture.filterMode = FilterMode.Point;

                    onComplete?.Invoke(sprite);
                }
            }
        }
        public static IEnumerator TypeEffect(string str, float typeRate, TextMeshProUGUI text)
        {
            string current = "";
            foreach (char c in str)
            {
                current += c;
                text.text = current;
                yield return new WaitForSecondsRealtime(typeRate / 2);
            }
            typewriters.Remove(new Typewriter(str, typeRate, text));
        }
        public static void Typewriter(string str, float typeRate, TextMeshProUGUI text)
        {
            if (!typewriters.Contains(new Typewriter(str, typeRate, text)))
            {
                typewriters.Add(new Typewriter(str, typeRate, text));
                Instance.StartCoroutine(TypeEffect(str, typeRate, text));
            }
            else
            {
                return;
            }
        }

        public void ShowNotif(GlobalNotification notif, bool showForever = false)
        {
            XServers.canShowNotif = false;
            Instance.StartCoroutine(ShowGlobal(notif, showForever));
        }

        public IEnumerator ShowGlobal(GlobalNotification notif, bool showForever = false)
        {
            if (polrMM == null)
            {
                yield break;
            }
            Logs.Info($"Showing global notification with message: {notif.message}, user: {notif.user}, and type: {notif.type}.", name: "GlobalNotificationListener");
            GameObject boxCanvas = polrMM.notifBox;
            if (boxCanvas != null)
            {
                Transform box = boxCanvas.transform.Find("Box");
                Transform boxD = boxCanvas.transform.Find("BoxDown");
                Transform boxU = boxCanvas.transform.Find("BoxUp");
                TextMeshProUGUI user = box.Find("User").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI msg = box.Find("Message").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI type = box.Find("Type").GetComponent<TextMeshProUGUI>();
                Image pfp = box.Find("PFP").GetComponent<Image>();

                if(notif.pfp == "polaricon")
                {
                    Texture2D tex = mainBundle.LoadAsset<Texture2D>("icon");
                    pfp.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    XServers.ExtractPFP(notif.pfp, pfp);
                }
                Typewriter(notif.user, 0.1f, user);
                Typewriter(notif.message, 0.1f, msg);
                Typewriter(notif.type, 0.25f, type);

                box.GetComponent<AudioSource>().Play();

                RectTransform rect = box.GetComponent<RectTransform>();
                float y1 = boxD.GetComponent<RectTransform>().position.y;
                float y2 = boxU.GetComponent<RectTransform>().position.y;
                if (rect != null)
                {
                    MoveY(rect, y1);
                    if(!showForever)
                    {
                        yield return new WaitForSecondsRealtime(7f);
                        MoveY(rect, y2);
                        XServers.canShowNotif = true;
                    }
                    yield break;
                }
            }
        }
        public static void MoveY(RectTransform rect, float y)
        {
            if(currentMoveY != null)
            {
                Instance.StopCoroutine(currentMoveY);
            }
            currentMoveY = Instance.StartCoroutine(Instance.MoveYCoro(rect, y));
        }
        public IEnumerator MoveYCoro(RectTransform rect, float y)
        {
            while (Mathf.Abs(rect.position.y - y) > 0.1f)
            {
                float newY = Mathf.Lerp(rect.position.y, y, Time.unscaledDeltaTime * 5f);
                rect.position = new Vector3(rect.position.x, newY, 0);
                yield return null;
            }
            rect.position = new Vector3(rect.position.x, y, 0);
        }
        public static void DeathScream(Sam sam, Transform parent = null)
        {
            currentScreaming = Instance.StartCoroutine(TTSDeathScream(sam, parent));
        }
        public static IEnumerator TTSDeathScream(Sam sam, Transform parent = null)
        {
            bool self = false;
            if (parent == null)
            {
                parent = MonoSingleton<CameraController>.Instance.transform;
                self = true;
            }
            Transform snapshotParent = new GameObject("TTSScreamer").transform;
            snapshotParent.transform.position = parent.transform.position;
            yield return new WaitForSeconds(0.25f);
            SamPitch.Set(sam);
            AudioSource samSource = TextReader.SayString("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", snapshotParent, true, !self);
            SamPitch.Reset();
            screaming = samSource.gameObject;
            yield return new WaitForSeconds(0.3f);
            while(samSource.pitch > 0f)
            {
                samSource.pitch = Mathf.MoveTowards(samSource.pitch, 0f, 1f * Time.deltaTime);
                yield return null;
            }
            AudioClip stat = mainBundle.LoadAsset<AudioClip>("ScreamOutOfPower");
            if(!self) SpawnSound(stat, 1f, snapshotParent, 1f, samSource.transform.position).transform.SetParent(null, true);
            Destroy(samSource.gameObject);
            yield return new WaitForSeconds(stat.length);
            Destroy(snapshotParent.gameObject);
        }
        public static void Flash(bool blue, bool ghost)
        {
            if(currentFlash != null)
            {
                Instance.polrMM.redFlash.SetActive(false);
                Instance.polrMM.blueFlash.SetActive(false);
                Instance.polrMM.ghostFlash.SetActive(false);
                Instance.StopCoroutine(currentFlash);
                currentFlash = null;
            }
            currentFlash = Instance.StartCoroutine(ScreenFlash(blue, ghost));
        }
        public static IEnumerator ScreenFlash(bool blue, bool ghost)
        {
            Image flash = blue ? Instance.polrMM.blueFlash.GetComponent<Image>() : ghost ? Instance.polrMM.ghostFlash.GetComponent<Image>() : Instance.polrMM.redFlash.GetComponent<Image>();
            Image icon = flash.gameObject.FindWithComponent<Image>("Image");
            Image crack = ghost ? flash.gameObject.FindWithComponent<Image>("Crack") : null;
            flash.gameObject.SetActive(true);
            flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 1f);
            icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 1f);
            if (ghost) crack.color = new Color(crack.color.r, crack.color.g, crack.color.b, 1f);
            while (flash.color.a > 0)
            {
                flash.color = Color.Lerp(flash.color, new Color(flash.color.r, flash.color.g, flash.color.b, 0f), 5f * Time.deltaTime);
                icon.color = Color.Lerp(icon.color, new Color(icon.color.r, icon.color.g, icon.color.b, 0f), 5f * Time.deltaTime);
                if (ghost) crack.color = Color.Lerp(crack.color, new Color(crack.color.r, crack.color.g, crack.color.b, 0f), 5f * Time.deltaTime);
                yield return null;
            }
            if (ghost) yield break;
            yield return new WaitUntil(() => !flash.GetComponent<AudioSource>().isPlaying);
            flash.gameObject.SetActive(false);

        }

        public bool IsFoolsDay()
        {
            return DateTime.Now.Month == 4 && DateTime.Now.Day == 1;
        }

        public static void DoubleCheckForSoftlock()
        {
            if(NetworkManager.Instance.CurrentLobby.MemberCount < 2 && !NetworkPlayer.selfIsGhost)
            {
                // player is alone and fine
                return;
            }
            int alive;
            if (NetworkPlayer.selfIsGhost)
            {
                alive = NetworkPlayer.PlayersAlive() - 1;
            }
            else
            {
                alive = NetworkPlayer.PlayersAlive();
            }
            if (alive < 1)
            {
                GameOver();
            }
        }
    }
}
