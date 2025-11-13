using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UI;
using Polarite;
using SteamImage = Steamworks.Data.Image;

namespace Polarite.Multiplayer
{
    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance;

        [Header("Voice Settings")]
        public KeyCode pushToTalk = KeyCode.V;
        public float proximityRange = 15f;
        public int sampleRate = 16000;
        public int chunkSamples = 320;

        private string micDevice;
        private AudioClip micClip;
        private int micPosition = 0;
        private bool isTalking = false;
        private Coroutine captureCoroutine;

        private readonly Dictionary<ulong, AudioSource> remoteSources = new Dictionary<ulong, AudioSource>();
        private readonly Dictionary<ulong, AudioClip> voiceClips = new Dictionary<ulong, AudioClip>();
        private readonly Dictionary<ulong, int> writeHeads = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, float> lastPacketTime = new Dictionary<ulong, float>();
        private float silenceTimeout = 0.6f;

        private GameObject indicatorCanvas;
        private UnityEngine.UI.Image indicatorImage;
        private Sprite onSprite, offSprite;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            TryStartMic();
            CreateIndicator();
        }

        void OnDestroy()
        {
            if (micClip != null && Microphone.IsRecording(micDevice)) Microphone.End(micDevice);
            CleanupAllPeers();
        }

        void OnDisable()
        {
            CleanupAllPeers();
        }

        void Update()
        {
            bool hasMic = Microphone.devices != null && Microphone.devices.Length > 0;
            bool inLobby = NetworkManager.InLobby;

            if (indicatorCanvas != null)
                indicatorCanvas.SetActive(hasMic && inLobby);

            if (!hasMic) return;

            if (indicatorCanvas != null && indicatorCanvas.activeSelf && indicatorImage != null)
                indicatorImage.sprite = isTalking ? onSprite : offSprite;

            KeyCode configured = ItePlugin.voicePushToTalk.value;
            var mode = ItePlugin.voiceMode.value;

            if (mode == VoiceMode.PushToTalk)
            {
                if (Input.GetKeyDown(configured)) StartTalking();
                if (Input.GetKeyUp(configured)) StopTalking();
                return;
            }

            if (mode == VoiceMode.ToggleToTalk)
            {
                if (Input.GetKeyDown(configured))
                {
                    if (isTalking) StopTalking();
                    else StartTalking();
                }
                return;
            }

            if (mode != VoiceMode.VoiceActivation) return;

            TryStartMic();
            float level = GetMicLevel();
            int threshold = Mathf.Clamp(ItePlugin.voiceVADThreshold.value, 0, 100);
            float thresh = threshold / 100f * 0.5f;
            if (level >= thresh)
            {
                if (!isTalking) StartTalking();
                return;
            }

            if (isTalking) StopTalking();
        }

        private void TryStartMic()
        {
            try
            {
                if (Microphone.devices == null || Microphone.devices.Length == 0) return;

                ItePlugin.wheresMyMic.text = "";
                for (int i = 0; i < Microphone.devices.Length; i++)
                    ItePlugin.wheresMyMic.text += $"{i}: " + Microphone.devices[i] + "\n";

                int desired = Mathf.Clamp(ItePlugin.voiceMicIndex.value, 0, Microphone.devices.Length - 1);
                string desiredDevice = Microphone.devices[desired];

                if (micClip != null && micDevice == desiredDevice && Microphone.IsRecording(micDevice))
                    return;

                try { if (micClip != null && Microphone.IsRecording(micDevice)) Microphone.End(micDevice); } catch { }

                micDevice = desiredDevice;

                switch (ItePlugin.voiceQuality.value)
                {
                    case VoiceQuality.Low: sampleRate = 16000; break;
                    case VoiceQuality.Medium: sampleRate = 44100; break;
                    case VoiceQuality.High: sampleRate = 48000; break;
                }

                chunkSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * 0.02f));
                micClip = Microphone.Start(micDevice, true, 1, sampleRate);

                int attempts = 0;
                while (!(Microphone.GetPosition(micDevice) > 0) && attempts < 50)
                {
                    System.Threading.Thread.Sleep(10);
                    attempts++;
                }

                micPosition = Microphone.GetPosition(micDevice);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Voice] Failed to start microphone: " + e);
            }
        }

        private float GetMicLevel()
        {
            if (micClip == null) return 0f;
            int pos = Microphone.GetPosition(micDevice);
            int len = Mathf.Min(256, micClip.samples);
            float[] data = new float[len];
            int start = pos - len;
            if (start < 0) start += micClip.samples;
            micClip.GetData(data, start);
            float sum = 0f;
            for (int i = 0; i < data.Length; i++) sum += data[i] * data[i];
            return Mathf.Sqrt(sum / data.Length);
        }

        public void StartTalking()
        {
            if (micClip == null) TryStartMic();
            if (micClip == null || isTalking) return;
            isTalking = true;
            captureCoroutine = StartCoroutine(CaptureAndSend());
        }

        public void StopTalking()
        {
            if (!isTalking) return;
            isTalking = false;
            if (captureCoroutine != null)
            {
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
            }
        }

        private IEnumerator CaptureAndSend()
        {
            float[] buffer = new float[chunkSamples];
            short[] pcm = new short[chunkSamples];

            while (isTalking)
            {
                int pos = Microphone.GetPosition(micDevice);
                int samplesAvailable = pos < micPosition ? micClip.samples - micPosition + pos : pos - micPosition;

                while (samplesAvailable >= chunkSamples)
                {
                    micClip.GetData(buffer, micPosition);
                    float sum = 0f;
                    for (int i = 0; i < chunkSamples; i++)
                    {
                        float f = Mathf.Clamp(buffer[i], -1f, 1f);
                        pcm[i] = (short)(f * short.MaxValue);
                        sum += f * f;
                    }

                    float rms = Mathf.Sqrt(sum / chunkSamples);

                    byte[] payload = new byte[1 + 2 + 1 + 2 + chunkSamples * 2];
                    int idx = 0;
                    payload[idx++] = 0x56;
                    Array.Copy(BitConverter.GetBytes((ushort)sampleRate), 0, payload, idx, 2); idx += 2;
                    payload[idx++] = 1;
                    Array.Copy(BitConverter.GetBytes((ushort)chunkSamples), 0, payload, idx, 2); idx += 2;
                    for (int i = 0; i < chunkSamples; i++)
                    {
                        Array.Copy(BitConverter.GetBytes(pcm[i]), 0, payload, idx, 2);
                        idx += 2;
                    }

                    if (NetworkManager.Instance != null && NetworkManager.Instance.CurrentLobby.Id != 0 && SteamClient.IsValid)
                    {
                        float range = ItePlugin.voiceProximity.value;
                        Vector3 myPos = NewMovement.Instance.transform.position;
                        foreach (var kv in NetworkManager.players)
                        {
                            NetworkPlayer plr = kv.Value;
                            if (plr == null || plr.SteamId == NetworkManager.Id) continue;
                            if (Vector3.Distance(myPos, plr.transform.position) > range) continue;
                            try { SteamNetworking.SendP2PPacket(plr.SteamId, payload); }
                            catch (Exception e) { Debug.LogWarning("[Voice] Failed sending P2P packet: " + e); }
                        }
                    }

                    var localPlayer = NetworkPlayer.LocalPlayer;
                    if (localPlayer != null && localPlayer.NameTag != null)
                        localPlayer.NameTag.SetTalkingLevel(Mathf.Clamp01(rms * 5f));

                    micPosition += chunkSamples;
                    if (micPosition >= micClip.samples) micPosition -= micClip.samples;
                    samplesAvailable -= chunkSamples;
                }

                yield return null;
            }
        }

        public void OnP2PDataReceived(byte[] buffer, int length, SteamId sender)
        {
            if (length < 1 || buffer[0] != 0x56) return;

            int idx = 1;
            ushort sr = BitConverter.ToUInt16(buffer, idx); idx += 2;
            byte channels = buffer[idx++];
            ushort samples = BitConverter.ToUInt16(buffer, idx); idx += 2;
            int expectedBytes = samples * 2;
            if (length < idx + expectedBytes) return;

            float[] floats = new float[samples];
            float sum = 0f;
            for (int i = 0; i < samples; i++)
            {
                short s = BitConverter.ToInt16(buffer, idx); idx += 2;
                float v = s / (float)short.MaxValue;
                floats[i] = Mathf.Clamp(v * ItePlugin.volume.value, -1f, 1f);
                sum += v * v;
            }

            float level = Mathf.Clamp01(Mathf.Sqrt(sum / samples) * 5f);
            NetworkPlayer plr = NetworkPlayer.Find(sender.Value);
            if (plr != null && plr.NameTag != null) plr.NameTag.SetTalkingLevel(level);
            if (!ItePlugin.receiveVoice.value) return;

            ulong senderId = sender.Value;
            AudioSource src = GetOrCreateSource(senderId);

            AudioClip clip;
            bool recreateClip = !voiceClips.TryGetValue(senderId, out clip) || clip == null || clip.frequency != sr || clip.channels != channels;
            if (recreateClip)
            {
                if (voiceClips.ContainsKey(senderId) && voiceClips[senderId] != null)
                {
                    try { Destroy(voiceClips[senderId]); } catch { }
                    voiceClips.Remove(senderId);
                }

                int clipSamples = sr * 2;
                clip = AudioClip.Create($"vc_stream_{senderId}", clipSamples, Math.Max(1, (int)channels), sr, false);
                voiceClips[senderId] = clip;
                writeHeads[senderId] = 0;
                src.clip = clip;
                src.loop = true;
                try { if (!src.isPlaying) src.Play(); } catch { }
            }

            int head = writeHeads.ContainsKey(senderId) ? writeHeads[senderId] : 0;
            try
            {
                if (samples > clip.samples)
                {
                    float[] small = new float[clip.samples];
                    Array.Copy(floats, 0, small, 0, clip.samples);
                    clip.SetData(small, head);
                    head += clip.samples;
                }
                else
                {
                    clip.SetData(floats, head);
                    head += samples;
                }

                if (head >= clip.samples) head = 0;
                writeHeads[senderId] = head;
                lastPacketTime[senderId] = Time.time;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Voice] Failed writing audio data: " + e);
            }
        }

        void LateUpdate()
        {
            if (lastPacketTime.Count == 0) return;
            float now = Time.time;
            var ids = lastPacketTime.Keys.ToList();
            foreach (var id in ids)
            {
                if (now - lastPacketTime[id] <= silenceTimeout) continue;
                lastPacketTime.Remove(id);
                writeHeads.Remove(id);

                if (!voiceClips.TryGetValue(id, out var clip)) continue;
                voiceClips.Remove(id);

                try
                {
                    if (remoteSources.TryGetValue(id, out var src) && src != null)
                    {
                        if (src.isPlaying) src.Stop();
                        src.clip = null;
                        try { Destroy(src.gameObject); } catch { }
                        remoteSources.Remove(id);
                    }
                    if (clip != null) Destroy(clip);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Voice] Failed to cleanup silent clip: " + e);
                }
            }
        }

        private AudioSource GetOrCreateSource(ulong steamId)
        {
            if (remoteSources.TryGetValue(steamId, out var existing) && existing != null)
            {
                existing.maxDistance = ItePlugin.voiceProximity.value;
                existing.loop = true;
                return existing;
            }

            NetworkPlayer plr = NetworkPlayer.Find(steamId);
            GameObject go;
            if (plr != null)
            {
                go = new GameObject("VoiceSrc_" + steamId);
                go.transform.SetParent(plr.transform, false);
                go.transform.localPosition = Vector3.zero;
            }
            else
            {
                go = new GameObject("VoiceSrc_" + steamId + "_global");
                go.transform.SetParent(transform, false);
                go.transform.position = Vector3.zero;
            }

            AudioSource src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 4f;
            src.dopplerLevel = 0f;
            src.maxDistance = ItePlugin.voiceProximity.value;
            src.loop = true;
            src.playOnAwake = false;
            remoteSources[steamId] = src;
            return src;
        }

        private void CreateIndicator()
        {
            try
            {
                string dir = ItePlugin.Instance != null
                    ? Path.GetDirectoryName(ItePlugin.Instance.Info.Location)
                    : Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                string onPath = Path.Combine(dir, "on.png");
                string offPath = Path.Combine(dir, "off.png");

                if (File.Exists(onPath))
                {
                    byte[] d = File.ReadAllBytes(onPath);
                    Texture2D t = new Texture2D(2, 2);
                    t.LoadImage(d);
                    t.filterMode = FilterMode.Point;
                    onSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                }

                if (File.Exists(offPath))
                {
                    byte[] d = File.ReadAllBytes(offPath);
                    Texture2D t = new Texture2D(2, 2);
                    t.LoadImage(d);
                    t.filterMode = FilterMode.Point;
                    offSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                }

                indicatorCanvas = new GameObject("VoiceIndicatorCanvas");
                var canvas = indicatorCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                indicatorCanvas.AddComponent<CanvasScaler>();
                indicatorCanvas.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(indicatorCanvas);

                GameObject imgGO = new GameObject("VoiceIndicatorImg", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                imgGO.transform.SetParent(indicatorCanvas.transform, false);
                indicatorImage = imgGO.GetComponent<UnityEngine.UI.Image>();
                indicatorImage.sprite = offSprite;
                RectTransform rt = imgGO.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-10f, 10f);
                rt.sizeDelta = new Vector2(48f, 48f);
                indicatorCanvas.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Voice] Failed to create indicator: " + e);
            }
        }

        private void CleanupAllPeers()
        {
            try
            {
                foreach (var kv in remoteSources.ToList())
                {
                    try
                    {
                        if (kv.Value == null) continue;
                        if (kv.Value.isPlaying) kv.Value.Stop();
                        try { Destroy(kv.Value.gameObject); } catch { }
                    }
                    catch { }
                }
                remoteSources.Clear();

                foreach (var kv in voiceClips.ToList())
                {
                    try { if (kv.Value != null) Destroy(kv.Value); } catch { }
                }
                voiceClips.Clear();
                writeHeads.Clear();
                lastPacketTime.Clear();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Voice] Failed to cleanup peers: " + e);
            }
        }
    }
}
