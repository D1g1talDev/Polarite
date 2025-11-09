using Steamworks;
using TMPro;
using UnityEngine;
using Polarite.Multiplayer;
using UnityEngine.UI;

namespace Polarite
{
    public class NameTag : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public Transform background;
        public Transform mainLookAt;
        public Transform hostIcon;
        public Transform player;

        public string playerName;
        public ulong id;
        public int currentHp = 100;

        public float padding = 0.1f;
        public float scaleMultiplier = 0.1f;

        public float hpLerpSpeed = 5f;
        public float hpPulseScale = 1.15f;
        public float hpPulseSpeed = 5f;

        private float lastWidth;
        private float displayedHP;
        private float targetHP;
        private Vector3 baseHPScale;

        // voice avatar
        private Image talkingAvatar;
        private float talkingLevel = 0f;
        private float lastTalkTime = -9999f;
        private float talkFadeSeconds = 2f;

        void Start()
        {
            displayedHP = currentHp;
            targetHP = currentHp;
            if (hpText != null)
                baseHPScale = hpText.transform.localScale;
        }
        public void Init(ulong steamId, string name, Transform playerT)
        {
            mainLookAt = transform.Find("Canvas/LookAt");
            background = mainLookAt.Find("Image");
            hostIcon = mainLookAt.Find("IsHostBG");
            nameText = mainLookAt.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            hpText = mainLookAt.Find("Text (TMP) (1)").GetComponent<TextMeshProUGUI>();
            playerName = name;
            id = steamId;
            transform.Find("Canvas").GetComponent<Canvas>().worldCamera = MonoSingleton<CameraController>.Instance.cam;
            player = playerT;

            // create or find avatar image
            Transform existing = mainLookAt.Find("TalkingPFP");
            if (existing != null)
            {
                talkingAvatar = existing.GetComponent<Image>();
            }
            else
            {
                GameObject go = new GameObject("TalkingPFP", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(mainLookAt, false);
                talkingAvatar = go.GetComponent<Image>();
                RectTransform rt = go.GetComponent<RectTransform>();
                // position above hpText
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(5f, 5f);
            }

            if (talkingAvatar != null)
            {
                talkingAvatar.color = new Color(1f, 1f, 1f, 0f);
                // fetch avatar image
                PlayerList.FetchAvatar(talkingAvatar, new Friend(id));
            }
        }

        public void Update()
        {
            if (nameText == null) return;

            float textWidth = nameText.preferredWidth / 6.25f;
            if (Mathf.Abs(textWidth - lastWidth) > 0.001f)
            {
                lastWidth = textWidth;

                float normalized = Mathf.InverseLerp(0f, 200f, textWidth);
                float scaledWidth = Mathf.Lerp(0.1f, 1.5f, normalized);

                Vector3 scale = background.localScale;
                scale.x = scaledWidth;
                background.localScale = scale;
            }

            Transform cam = Camera.current.transform;
            Vector3 dir = (mainLookAt.transform.position - cam.position).normalized;
            mainLookAt.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            nameText.text = playerName;

            bool isHost = false;
            if (NetworkManager.Instance != null && NetworkManager.Instance.CurrentLobby.Id != 0)
            {
                try { isHost = NetworkManager.Instance.CurrentLobby.Owner.Id == id; } catch { isHost = false; }
            }
            hostIcon.gameObject.SetActive(isHost);
            nameText.color = isHost ? Color.cyan : Color.white;

            if (hpText != null)
            {
                displayedHP = Mathf.Lerp(displayedHP, targetHP, Time.deltaTime * hpLerpSpeed);

                float clamped = Mathf.Clamp(displayedHP, 0f, 200f);
                hpText.text = $"({Mathf.RoundToInt(clamped)})";

                float t = Mathf.Clamp01(clamped / 100f);
                Color low = Color.red;
                Color mid = Color.yellow;
                Color high = Color.green;
                Color super = Color.cyan;

                Color lerpedColor = (t < 0.5f) ? Color.Lerp(low, mid, t * 2f) : (t > 1f) ? Color.Lerp(high, super, (t - 0.5f) * 2f) : Color.Lerp(mid, high, (t - 0.5f) * 2f);

                hpText.color = lerpedColor;

                float heartbeatSpeed = Mathf.Lerp(0.8f, 3f, 1f - t);
                float heartbeatStrength = Mathf.Lerp(1f, 1.3f, 1f - t);

                float beat = Mathf.Pow(Mathf.Sin(Time.time * heartbeatSpeed * Mathf.PI), 8f);

                float scale = Mathf.Lerp(1f, heartbeatStrength, beat);
                hpText.transform.localScale = baseHPScale * scale;
            }

            // update talking avatar alpha based on lastTalkTime and level
            if (talkingAvatar != null)
            {
                float age = Time.time - lastTalkTime;
                float target = 0f;
                if (age < talkFadeSeconds)
                {
                    target = talkingLevel;
                }
                float flicker = 0f;
                if (talkingLevel > 0.01f)
                {
                    flicker = Mathf.Sin(Time.time * 20f) * 0.1f * talkingLevel;
                }
                float currentAlpha = talkingAvatar.color.a;
                float desired = Mathf.Clamp01(target + flicker);
                float next = Mathf.Lerp(currentAlpha, desired, Time.deltaTime * 8f);
                Color c = talkingAvatar.color;
                c.a = next;
                talkingAvatar.color = c;
            }
        }
        public void SetHP(float newHP)
        {
            targetHP = Mathf.Clamp(newHP, 0f, 200f);
        }

        public void SetTalkingLevel(float level)
        {
            talkingLevel = Mathf.Clamp01(level);
            lastTalkTime = Time.time;
        }
    }
}
