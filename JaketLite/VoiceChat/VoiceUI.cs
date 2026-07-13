using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;
using System.Security.Permissions;
using Polarite.Networking.Extensions;
using Polarite.Networking.Skins;

namespace Polarite.VoiceChat
{
    public class VCUserHud
    {
        public ulong player;
        public GameObject hud;
        public Image pfp;
        public TextMeshProUGUI name;
        public Slider bar, vaBar;
        public bool talking;
        public float level, lastUpdate;
    }
    public class VoiceUI : MonoBehaviour
    {
        public static VoiceUI Instance { get; private set; }
        public static GameObject currentCanvas, talkingHolder, setupBar;
        public static GameObject setupScreen, va, ptt;
        public static VCUserHud preview;
        public static TMP_Dropdown micDrop, vcModeDrop;
        public static TMP_InputField vat;
        public static Button pttBind, done, cancel;
        public static int currentMic = 0;
        public static int currentVAT = 10;
        public static int currentVCMode = 1;
        public static VoicePTTBind bind;

        private static bool tttActivated;

        public Dictionary<ulong, VCUserHud> uis = new Dictionary<ulong, VCUserHud>();

        public static void MakeUI()
        {
            currentCanvas = Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("VCCanvas"));
            VoiceUI cur = currentCanvas.AddComponent<VoiceUI>();
            Instance = cur;
            talkingHolder = currentCanvas.transform.Find("VCHud").gameObject;

            Transform tutorialParent = currentCanvas.transform.Find("TutorialStuff");
            setupBar = tutorialParent.Find("TutorialIntroGuide").gameObject;
            setupScreen = tutorialParent.Find("SetupScreen").gameObject;
            micDrop = setupScreen.FindWithComponent<TMP_Dropdown>("MicDrop");
            vcModeDrop = setupScreen.FindWithComponent<TMP_Dropdown>("VCMode");
            va = setupScreen.transform.Find("IfVA").gameObject;
            ptt = setupScreen.transform.Find("IfPTT").gameObject;
            vat = va.FindWithComponent<TMP_InputField>("VAActT");
            pttBind = ptt.FindWithComponent<Button>("KeyButton");
            done = setupScreen.FindWithComponent<Button>("Done");
            cancel = setupScreen.FindWithComponent<Button>("Cancel");
            preview = SetupTUser(setupScreen.transform.Find("TalkingUserSmaller").gameObject, NetworkManager.Id);

            micDrop.onValueChanged.AddListener((val) =>
            {
                if (VoiceChatManager.Instance == null) return;
                VoiceChatManager.inSetup = true;
                currentMic = val;
                ItePlugin.voiceMicIndex.value = val;
                VoiceChatManager.Instance.TryStartMic();
            });
            vcModeDrop.onValueChanged.AddListener(val => currentVCMode = val);
            vat.onValueChanged.AddListener((val) =>
            {
                if (val.Length <= 0) return;
                int valParsed = int.Parse(val);
                if (valParsed > 100) vat.text = "100";
                if (valParsed < 0) vat.text = "0";
                currentVAT = Mathf.Clamp(valParsed, 0, 100);
            });
            bind = pttBind.GetOrAddComponent<VoicePTTBind>();
            bind.button = pttBind;
            pttBind.onClick.AddListener(bind.Act);
            done.onClick.AddListener(() =>
            {
                ItePlugin.voiceMicIndex.value = currentMic;
                ItePlugin.voiceMode.value = (VoiceMode)currentVCMode;
                ItePlugin.voiceVADThreshold.value = currentVAT;
                ItePlugin.voicePushToTalk.value = bind.selected;

                ItePlugin.didSetup.value = true;
                VoiceChatManager.inSetup = false;
                setupScreen.SetActive(false);
                VoiceChatManager.Instance.TryStartMic();

                ItePlugin.CustomTogglePlayer(true);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            });
            cancel.onClick.AddListener(() =>
            {
                setupScreen.SetActive(false);
                ItePlugin.CustomTogglePlayer(true);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            });

            setupBar.SetActive(false);
            setupScreen.SetActive(false);
            va.SetActive(false);
            UIAnchors.SetVC(currentCanvas.GetComponent<RectTransform>(), talkingHolder.GetComponent<RectTransform>());
            DontDestroyOnLoad(currentCanvas);
        }
        public void SetupMicDropdown()
        {
            micDrop.options.Clear();
            micDrop.options.Capacity = Microphone.devices.Length;
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                micDrop.options.Insert(i, new TMP_Dropdown.OptionData(Microphone.devices[i]));
            }
        }
        public void SetLevel(ulong plr, float val)
        {
            VCUserHud hud = GetHud(plr);
            float lev = Mathf.Lerp(0f, 1f, val);
            hud.talking = lev > 0.01f;
            hud.level = lev;
            hud.lastUpdate = 0.3f;
        }
        public bool SomeoneTalking()
        {
            if(!NetworkManager.InLobby || !ItePlugin.receiveVoice.value)
            {
                return false;
            }
            foreach(var plr in uis.Values)
            {
                if(plr.talking && plr.lastUpdate > 0f)
                {
                    return true;
                }    
            }
            return false;
        }
        public void Update()
        {
            talkingHolder.SetActive(SomeoneTalking());
            foreach(var plr in uis.Values)
            {
                if(plr.lastUpdate <= 0f)
                {
                    plr.talking = false;
                    plr.level = 0f;
                    plr.hud.SetActive(false);
                    continue;
                }
                if(plr.talking && !Voice.mutedPlayers.Contains(plr.player) && NetworkManager.InLobby && ItePlugin.receiveVoice.value)
                {
                    plr.hud.SetActive(true);
                    plr.bar.value = plr.level;
                    plr.bar.fillRect.GetComponent<Image>().color = Color.Lerp(Color.green, Color.red, plr.level);
                    plr.lastUpdate -= Time.deltaTime;
                    UIAnchors.Refresh();
                }
                else
                {
                    plr.hud.SetActive(false);
                }
            }
            if(!ItePlugin.didSetup.value && NetworkManager.InLobby && VoiceChatManager.Instance != null && Microphone.devices.Length > 0)
            {
                setupBar.SetActive(true);
                if(Input.GetKeyDown(KeyCode.V) && !ChatUI.isTyping && !ItePlugin.PolarMenuActive)
                {
                    setupScreen.SetActive(true);
                    ItePlugin.voiceMicIndex.value = currentMic;
                    VoiceChatManager.Instance.TryStartMic();
                }
                if(setupScreen.activeSelf)
                {
                    ItePlugin.CustomTogglePlayer(false);
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    if(micDrop.options.Count < Microphone.devices.Length)
                    {
                        SetupMicDropdown();
                    }
                    va.SetActive(currentVCMode == 1);
                    ptt.SetActive(currentVCMode != 1);

                    float vaAmount = Mathf.Lerp(0f, 1f, currentVAT / 100f * 0.5f);
                    preview.level = Mathf.Lerp(0f, 1f, VoiceChatManager.Instance.GetMicLevel());
                    bool vaActivated = preview.level >= currentVAT / 100f * 0.5f;
                    if (Input.GetKeyDown(bind.selected) && currentVCMode == 2) tttActivated = !tttActivated;
                    bool activated = currentVCMode == 1 ? vaActivated : currentVCMode == 2 ? tttActivated : Input.GetKey(bind.selected);
                    preview.bar.fillRect.GetComponent<Image>().color = activated ? Color.Lerp(Color.green, Color.red, preview.level) : Color.Lerp(new Color(0f, 0.5f, 0f), new Color(0.5f, 0f, 0f), preview.level);
                    preview.bar.value = preview.level;
                    preview.vaBar.gameObject.SetActive(currentVCMode == 1);
                    preview.vaBar.value = vaAmount;
                }
            }
            else
            {
                setupBar.SetActive(false);
            }
        }
        public VCUserHud GetHud(ulong plr)
        {
            if(uis.TryGetValue(plr, out var vc) && vc.hud != null)
            {
                return vc;
            }
            return CreateTalker(plr);
        }
        public VCUserHud CreateTalker(ulong plr)
        {
            GameObject ui = Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("TalkingUserSmaller"), talkingHolder.transform);
            VCUserHud vc = new VCUserHud();
            vc.player = plr;
            vc.hud = ui;
            vc.pfp = ui.transform.Find("PFP").GetComponent<Image>();
            vc.bar = ui.GetComponentInChildren<Slider>();
            vc.name = ui.transform.Find("Name").GetComponent<TextMeshProUGUI>();

            if (!ItePlugin.useSkinInsteadOfPFP.value)
            {
                PlayerList.FetchAvatar(vc.pfp, new Friend(plr), false);
            }
            else if (SkinManagerV2.Previews.TryGetValue(plr, out var icon))
            {
                vc.pfp.sprite = icon;
            }
            vc.name.text = NetworkManager.GetNameOfId(plr, true);
            if(uis.ContainsKey(plr)) uis[plr] = vc; else uis.Add(plr, vc);
            return vc;
        }
        public static VCUserHud SetupTUser(GameObject ui, ulong plr)
        {
            VCUserHud vc = new VCUserHud();
            vc.player = plr;
            vc.hud = ui;
            vc.pfp = ui.transform.Find("PFP").GetComponent<Image>();
            vc.bar = ui.GetComponentInChildren<Slider>();
            vc.vaBar = ui.FindWithComponent<Slider>("PreviewVASlider");
            vc.name = ui.transform.Find("Name").GetComponent<TextMeshProUGUI>();

            if (!ItePlugin.useSkinInsteadOfPFP.value)
            {
                PlayerList.FetchAvatar(vc.pfp, new Friend(plr), false);
            }
            else if(SkinManagerV2.Previews.TryGetValue(plr, out var icon))
            {
                vc.pfp.sprite = icon;
            }
            vc.name.text = NetworkManager.GetNameOfId(plr, true);
            return vc;
        }
        public static void RefreshIcons(bool val)
        {
            if (Instance != null)
            {
                foreach (var plr in Instance.uis)
                {
                    if (val && SkinManagerV2.Previews.TryGetValue(plr.Value.player, out var icon))
                    {
                        plr.Value.pfp.sprite = icon;
                    }
                    else
                    {
                        PlayerList.FetchAvatar(plr.Value.pfp, new Friend(plr.Value.player), false);
                    }
                }
            }
        }
    }
}
