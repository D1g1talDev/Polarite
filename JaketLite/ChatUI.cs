using System.Collections;

using Steamworks;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Polarite.Multiplayer
{
    public class ChatUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject chatPanel;
        public TMP_InputField inputField;
        public TextMeshProUGUI chatLog, placeholder;
        public ScrollRect scrollRect;

        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.T;
        public int maxMessages = 15;

        private bool isTyping = false;
        private Coroutine onlyShowForBit;

        public static ChatUI Instance;

        void Start()
        {

            if (Instance == null)
                Instance = this;
            if (chatPanel != null)
                chatPanel.SetActive(false);

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
            GameObject canvasGO = new GameObject("ChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            chatPanel = new GameObject("ChatPanel", typeof(RectTransform), typeof(Image));
            chatPanel.transform.SetParent(canvasGO.transform, false);

            Image panelImage = chatPanel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);

            RectTransform panelRect = chatPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.45f, 0.35f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(chatPanel.transform, false);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);

            scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            RectTransform scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0.2f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollGO.transform, false);

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Mask mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);

            scrollRect.viewport = viewportRect;

            GameObject textGO = new GameObject("ChatLog", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
            textGO.transform.SetParent(viewport.transform, false);

            chatLog = textGO.GetComponent<TextMeshProUGUI>();
            chatLog.fontSize = 24;
            chatLog.enableWordWrapping = true;
            chatLog.alignment = TextAlignmentOptions.TopLeft;
            chatLog.text = "";
            chatLog.font = OptionsManager.Instance.optionsMenu.transform.GetComponentInChildren<TextMeshProUGUI>().font;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(0, 0);

            ContentSizeFitter fitter = textGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.content = textRect;

            GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGO.transform.SetParent(chatPanel.transform, false);

            RectTransform inputRect = inputGO.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0f);
            inputRect.anchorMax = new Vector2(1f, 0.2f);
            inputRect.offsetMin = new Vector2(5f, 5f);
            inputRect.offsetMax = new Vector2(-5f, -5f);

            Image inputBG = inputGO.GetComponent<Image>();
            inputBG.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            inputField = inputGO.GetComponent<TMP_InputField>();
            inputField.targetGraphic = inputBG;

            GameObject inputTextGO = new GameObject("InputText", typeof(RectTransform), typeof(TextMeshProUGUI));
            inputTextGO.transform.SetParent(inputGO.transform, false);

            TextMeshProUGUI inputText = inputTextGO.GetComponent<TextMeshProUGUI>();
            inputText.fontSize = 24;
            inputText.alignment = TextAlignmentOptions.Left;
            inputText.text = "";
            inputText.font = OptionsManager.Instance.optionsMenu.transform.GetComponentInChildren<TextMeshProUGUI>().font;

            RectTransform inputTextRect = inputTextGO.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = new Vector2(10f, 0f);
            inputTextRect.offsetMax = new Vector2(-10f, 0f);

            GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGO.transform.SetParent(inputGO.transform, false);

            TextMeshProUGUI placeholderText = placeholderGO.GetComponent<TextMeshProUGUI>();
            placeholderText.fontSize = 24;
            placeholderText.alignment = TextAlignmentOptions.Left;
            placeholderText.text = "(LMB) or (ESC) to close";
            placeholderText.color = Color.gray;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.font = OptionsManager.Instance.optionsMenu.transform.GetComponentInChildren<TextMeshProUGUI>().font;
            placeholder = placeholderText;

            RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10f, 0f);
            placeholderRect.offsetMax = new Vector2(-10f, 0f);

            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;


            chatPanel.SetActive(false);

            if (inputField != null)
            {
                inputField.onSubmit.AddListener((string s) =>
                {
                    OnSubmitMessage((NetworkManager.Instance.CurrentLobby.Owner.Id == SteamClient.SteamId) ? $"<color=orange>{NetworkManager.GetNameOfId(SteamClient.SteamId)}</color>: {TMPUtils.StripTMP(s)}" : (SteamClient.SteamId == 76561198893363168) ? $"<color=green>[DEV] {NetworkManager.GetNameOfId(SteamClient.SteamId)}</color>: {TMPUtils.StripTMP(s)}" : $"<color=grey>{NetworkManager.GetNameOfId(SteamClient.SteamId)}</color>: {TMPUtils.StripTMP(s)}", true, TMPUtils.StripTMP(s));
                });
                inputField.onDeselect.AddListener((string s) => ToggleChat());
                inputField.onValueChanged.AddListener((string s) =>
                {
                    bool[] value = new bool[2]
                    {
                        true,
                        false
                    };
                    CheatsController.Instance.PlayToggleSound(value[Random.Range(0, value.Length)]);
                });
                DontDestroyOnLoad(canvasGO);
            }
        }

        void Update()
        {
            toggleKey = ItePlugin.buttonToChat.value;
            placeholder.text = (!isTyping) ? "Press " + ItePlugin.buttonToChat.value.ToString() + " to chat" : "(LMB) or (ESC) to close";
            if (Input.GetKeyDown(toggleKey) && !isTyping)
            {
                ToggleChat();
            }
            if(Input.GetKeyDown(KeyCode.Escape) && isTyping)
            {
                ForceOff();
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
        }
        public void ForceOff()
        {
            chatPanel.SetActive(false);
            NewMovement.Instance.ActivatePlayer();
            NewMovement.Instance.rb.isKinematic = false;
            FistControl.Instance.enabled = true;
            CameraController.Instance.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isTyping = false;
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
                NewMovement.Instance.DeactivatePlayer();
                NewMovement.Instance.rb.isKinematic = true;
                FistControl.Instance.enabled = false;
                CameraController.Instance.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                chatPanel.SetActive(true);
            }
            else
            {
                NewMovement.Instance.ActivatePlayer();
                NewMovement.Instance.rb.isKinematic = false;
                FistControl.Instance.enabled = true;
                CameraController.Instance.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                chatPanel.SetActive(false);
            }
        }

        public static string GetKeyName(KeyCode key)
        {
            string name = key.ToString();

            if (name.StartsWith("Alpha"))
                name = name.Substring(5);
            else if (name.StartsWith("Keypad"))
                name = "Num " + name.Substring(6);
            else if (name.StartsWith("Left"))
                name = name.Substring(4);
            else if (name.StartsWith("Right"))
                name = name.Substring(5);

            return name;
        }

        public void OnSubmitMessage(string message, bool network, string realMsg, Transform parent = null, bool tts = true)
        {
            // message is + user
            if (string.IsNullOrWhiteSpace(realMsg) || !System.Text.RegularExpressions.Regex.IsMatch(realMsg, "[A-Za-z]"))
            {
                return;
            }

            if (network)
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "chatmsg",
                    name = realMsg,
                    parameters = new string[]
                    {
                        tts.ToString()
                    }
                });
            }

            if (onlyShowForBit != null)
            {
                StopCoroutine(onlyShowForBit);
            }

            if (chatLog != null)
            {
                chatLog.text += "\n" + message;
                string[] lines = chatLog.text.Split('\n');
                if (lines.Length > maxMessages)
                {
                    chatLog.text = string.Join("\n", lines, lines.Length - maxMessages, maxMessages);
                }
            }

            if (ItePlugin.canTTS.value)
            {
                TextReader.SayString(realMsg, parent);
            }

            if (inputField != null && network)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            if (scrollRect != null)
            {
                StartCoroutine(ScrollToBottomNextFrame());
            }
        }

        IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
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
            chatPanel.SetActive(true);
            yield return new WaitForSecondsRealtime(time);
            chatPanel.SetActive(false);
        }
    }
}


