using System.Collections;

using Steamworks;
using System.Text;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Polarite.Networking;
using System.Collections.Generic;
using Polarite.Debugging;
using Polarite.SamTTS;
using Polarite.Networking.Extensions;

namespace Polarite.Multiplayer
{
    public class ChatUI : MonoBehaviour
    {
        public GameObject chatPanel;
        public TMP_InputField inputField;
        public TextMeshProUGUI chatLog, placeholder, typeIndi;
        public Image typingIndiBG;
        public ScrollRect scrollRect;

        public KeyCode toggleKey = KeyCode.T;

        private List<ulong> peopleTyping = new List<ulong>();
        private string overrideTypeIndicatorText = string.Empty;

        // unlimited history storage
        private readonly List<string> chatMessages = new List<string>();
        private readonly StringBuilder chatBuilder = new StringBuilder();
        private readonly int hardCapMessages = 100000; // very high cap to avoid unbounded memory in pathological cases

        public static bool isTyping = false;
        public static bool isActuallyTyping = false;
        public static float typeTimer = 0f;
        private Coroutine onlyShowForBit;
        private bool toggled;

        public static ChatUI Instance;

        void Start()
        {
            if (Instance == null)
                Instance = this;

            chatPanel?.SetActive(false);
            CreateUI();
        }
        public void CreateUI()
        {
            if(onlyShowForBit != null)
            {
                StopCoroutine(onlyShowForBit);
            }
            if(isTyping)
            {
                ForceOff();
            }

            GameObject canvas = GameObject.Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("ChatCanvas"));
            try
            {
                chatPanel = canvas.transform.Find("ChatPanel").gameObject;
                inputField = chatPanel.GetComponentInChildren<TMP_InputField>();
                scrollRect = chatPanel.GetComponentInChildren<ScrollRect>();
                placeholder = inputField.placeholder.GetComponent<TextMeshProUGUI>();
                typingIndiBG = chatPanel.transform.Find("TypingIndiBG").GetComponent<Image>();
                typeIndi = typingIndiBG.GetComponentInChildren<TextMeshProUGUI>();
                chatLog = scrollRect.transform.Find("Viewport").GetComponentInChildren<TextMeshProUGUI>();
                inputField.characterLimit = 300;
            }
            catch (System.Exception ex)
            {
                Logs.Error("Failed to create chat canvas! " + ex.Message);
            }
            UIAnchors.SetChat(canvas.GetComponent<RectTransform>(), chatPanel.GetComponent<RectTransform>());
            Toggle(false, true);

            if (inputField != null)
            {
                inputField.onSubmit.AddListener((string s) =>
                {
                    // prioritise DEV tag if this user is a developer
                    string author;
                    if (Net.Dev(NetworkManager.Id))
                        author = $"<color=green>[DEV] {NetworkManager.GetNameOfId(NetworkManager.Id)}</color>: {s.WithoutTMP()}";
                    else if (NetworkManager.Instance.CurrentLobby.Owner.Id == NetworkManager.Id)
                        author = $"<color=#00F2FF>{NetworkManager.GetNameOfId(NetworkManager.Id)}</color>: {s.WithoutTMP()}";
                    else
                        author = $"<color=grey>{NetworkManager.GetNameOfId(NetworkManager.Id)}</color>: {s.WithoutTMP()}";

                    OnSubmitMessage(author, true, s.WithoutTMP(), sam: SamPitch.configSam);
                    StopTyping();
                });
                inputField.onValueChanged.AddListener((string s) =>
                {
                    bool[] value = new bool[2]
                    {
                        true,
                        false
                    };
                    CheatsController.Instance.PlayToggleSound(value[Random.Range(0, value.Length)]);
                    FlagIsTyping();
                });
                DontDestroyOnLoad(canvas);
            }
        }
        public void OpenEffect(bool value)
        {
            HudOpenEffect[] effects = chatPanel.GetComponentsInChildren<HudOpenEffect>();
            foreach(var effect in effects)
            {
                if(value)
                {
                    effect.targetDimensions = new Vector2(1, 1);
                    effect.OnEnable();
                }
                else
                {
                    effect.Reverse(effect.speed);
                }
            }
        }
        public void Toggle(bool value, bool instant = false)
        {
            scrollRect.gameObject.SetActive(value);
            inputField.gameObject.SetActive(value);
            UIAnchors.Refresh();
            if(value)
            {
                if(!toggled)
                {
                    OpenEffect(true);
                }
            }
            else
            {
                OpenEffect(false);
            }
            toggled = value;
        }
        public void FlagIsTyping()
        {
            typeTimer = 5f;
            isActuallyTyping = true;
        }
        public void StopTyping()
        {
            typeTimer = 0f;
            isActuallyTyping = false;
        }

        void Update()
        {
            toggleKey = ItePlugin.buttonToChat.value;
            if(MonoSingleton<OptionsManager>.Instance.paused || ItePlugin.PolarMenuActive)
            {
                placeholder.text = "You can't chat while paused.";
                return;
            }
            else
            {
                placeholder.text = (!isTyping) ? "Press " + GetKeyName(toggleKey) + " to chat" : $"Pause to exit chat";
            }
            if (Input.GetKeyDown(toggleKey) && !isTyping)
            {
                ToggleChat();
            }
            if(MonoSingleton<OptionsManager>.Instance.paused && isTyping)
            {
                ToggleChat();
                OptionsManager.Instance.UnPause();
            }
            if(isTyping && !inputField.isFocused)
            {
                inputField.ActivateInputField();
            }
            if(isTyping)
            {
                if (onlyShowForBit != null)
                {
                    StopCoroutine(onlyShowForBit);
                }
            }

            if(typeIndi != null)
            {
                HandleTypeIndicator();
            }
        }
        public List<ulong> GetTypingPlayers()
        {
            List<ulong> result = new List<ulong>();
            foreach(var plr in NetworkManager.players.Values)
            {
                if(plr.typing)
                {
                    result.Add(plr.SteamId);
                }
            }
            return result;
        }
        public void HandleTypeIndicator()
        {
            typeTimer -= Time.deltaTime;
            if(typeTimer > 0f)
            {
                isActuallyTyping = true;
            }
            else
            {
                isActuallyTyping = false;
                typeTimer = 0f;
            }
            peopleTyping = GetTypingPlayers();
            if(overrideTypeIndicatorText != string.Empty)
            {
                typeIndi.text = overrideTypeIndicatorText;
                typingIndiBG.enabled = true;
                return;
            }
            if(peopleTyping.Count < 1)
            {
                typeIndi.text = string.Empty;
                typingIndiBG.enabled = false;
                return;
            }
            typingIndiBG.enabled = true;
            switch(peopleTyping.Count)
            {
                case 1:
                    typeIndi.text = $"{NetworkManager.GetNameOfId(peopleTyping[0], true)} is typing...";
                    break;
                case 2:
                    typeIndi.text = $"{NetworkManager.GetNameOfId(peopleTyping[0], true)} and {NetworkManager.GetNameOfId(peopleTyping[1])} are typing...";
                    break;
                case 3:
                    typeIndi.text = $"{NetworkManager.GetNameOfId(peopleTyping[0], true)}, {NetworkManager.GetNameOfId(peopleTyping[1])}, {NetworkManager.GetNameOfId(peopleTyping[2])} are typing...";
                    break;
                default:
                    typeIndi.text = $"{peopleTyping.Count} players are typing...";
                    break; 
            }
        }
        public void ForceOff()
        {
            if(!NetworkManager.InLobby)
            {
                return;
            }
            Toggle(false);
            ItePlugin.CustomTogglePlayer(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isTyping = false;
            StopTyping();
            inputField.DeactivateInputField();
        }

        void ToggleChat()
        {
            if (!NetworkManager.InLobby)
            {
                return;
            }
            /* why don't these even work :sob:
            bool wereCheatsOn = CheatsController.Instance.cheatsEnabled;
            bool wereFistControlOn = FistControl.Instance.enabled;
            bool wereCameraOn = CameraController.Instance.enabled;
            */

            isTyping = !isTyping;

            if (isTyping)
            {
                ItePlugin.CustomTogglePlayer(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Toggle(true);
            }
            else
            {
                inputField.DeactivateInputField();
                StopTyping();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                ItePlugin.CustomTogglePlayer(true);
                ShowUIForBit(5f);
            }
        }

        public static string GetKeyName(KeyCode key)
        {
            string name = key.ToString();

            if (name.StartsWith("Alpha"))
                name = name.Substring(5);
            else if (name.StartsWith("Keypad"))
                name = "Keypad " + name.Substring(6);
            else if (name.StartsWith("Left"))
                name = "Left " + name.Substring(4);
            else if (name.StartsWith("Right"))
                name = "Right " + name.Substring(5);
            else if(name.StartsWith("Mouse"))
                name = "M" + name.Substring(5);

            return name;
        }

        public void OnSubmitMessage(string message, bool network, string realMsg, Transform parent = null, bool tts = true, Sam sam = null)
        {
            if(sam == null)
            {
                sam = new Sam();
            }
            if(CommandManager.IsCommand(realMsg))
            {
                CommandManager.CheckCommand(realMsg);
                inputField.text = "";
                return;
            }

            // message is + user
            if (string.IsNullOrWhiteSpace(realMsg))
            {
                return;
            }
            if (network)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(realMsg);
                w.WriteSam(sam);
                NetworkManager.Instance.BroadcastPacket(PacketType.ChatMsg, w.GetBytes());
                if(ItePlugin.chatNoise.value)
                {
                    ItePlugin.SpawnSound(ItePlugin.message, 1f, CameraController.Instance.transform, 1f);
                }
            }
            if (onlyShowForBit != null)
            {
                StopCoroutine(onlyShowForBit);
            }

            if (chatLog != null)
            {
                // append to message list and string builder to avoid repeated splits/joins
                // enforce hard cap by removing oldest messages when needed
                if (chatMessages.Count >= hardCapMessages)
                {
                    // remove oldest 10%
                    int removeCount = Mathf.Max(1, hardCapMessages / 10);
                    chatMessages.RemoveRange(0, removeCount);
                    // rebuild builder
                    chatBuilder.Clear();
                    foreach (var m in chatMessages) chatBuilder.AppendLine(m);
                }
                chatMessages.Add(message);
                chatBuilder.AppendLine(message);
                chatLog.text = chatBuilder.ToString();
            }

            if (ItePlugin.ttsChat.value && ItePlugin.canTTS.value && tts)
            {
                SamPitch.Set(sam);
                TextReader.SayString(realMsg, parent);
                SamPitch.Reset();
            }

            if (inputField != null && network)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            if (scrollRect != null)
            {
                StartCoroutine(ScrollAfterFrame());
            }
        }
        public IEnumerator ScrollAfterFrame()
        {
            yield return null;
            scrollRect.verticalNormalizedPosition = 0f;
        }
        public void ShowUIForBit(float time = 10f)
        {
            if(onlyShowForBit != null)
            {
                StopCoroutine(onlyShowForBit);
            }
            onlyShowForBit = StartCoroutine(OnlyShowUIForSecond(time));
        }
        public IEnumerator OnlyShowUIForSecond(float time = 10f)
        {
            Toggle(true);
            yield return new WaitForSecondsRealtime(time);
            Toggle(false);
        }

        /// <summary>
        /// Sends a message only the player sees to the chat.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="displayTime">The time it takes for the UI to hide itself after this message</param>
        public static void Message(string msg, float displayTime)
        {
            if (Instance != null)
            {
                Instance.OnSubmitMessage(msg, false, msg, tts: false);
                Instance.ShowUIForBit(displayTime);
            }
        }
    }
}


