// skipping on this for now since i need to get to work on making packets not json.

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
    // Simple proximity voice chat using Unity Microphone and Steam P2P packets.
    // - Push-to-talk via PluginConfigurator (ItePlugin.voicePushToTalk)
    // - Supports Voice Activation and Toggle modes
    // - Select microphone by index from config
    // - Mono 16kHz, 20ms chunks (~320 samples)
    // - Sends raw PCM16 data with a small header
    // Packet format:
    // [0] = 0x56 ('V') marker
    // [1..2] = ushort sampleRate (little-endian)
    // [3] = byte channels
    // [4..5] = ushort samplesCount
    // [6..] = PCM16 little-endian samplesCount * 2 bytes

    // made by doomahreal

    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance;

        [Header("Voice Settings")]
        // keep a default here but overridden from config each frame
        public KeyCode pushToTalk = KeyCode.V;
        // proximity range is configurable via ItePlugin.voiceProximity
        public float proximityRange = 15f;
        public int sampleRate = 16000;
        public int chunkSamples = 320; // 20ms @16kHz

        private string micDevice;
        private AudioClip micClip;
        private int micPosition = 0;
        private bool isTalking = false;
        private Coroutine captureCoroutine;

        // AudioSources for remote players
        private Dictionary<ulong, AudioSource> remoteSources = new Dictionary<ulong, AudioSource>();

        private Dictionary<ulong, AudioClip> voiceClips = new Dictionary<ulong, AudioClip>();
        private Dictionary<ulong, int> writeHeads = new Dictionary<ulong, int>();
        private Dictionary<ulong, float> lastPacketTime = new Dictionary<ulong, float>();
        private float silenceTimeout = 0.6f; // seconds to consider remote stopped

        // indicator UI
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
            if (micClip != null && Microphone.IsRecording(micDevice))
            {
                Microphone.End(micDevice);
            }
        }

        void Update()
        {
            // update indicator visibility and sprite
            bool hasMic = Microphone.devices != null && Microphone.devices.Length > 0;
            bool inLobby = NetworkManager.InLobby;
            if (indicatorCanvas != null)
            {
                indicatorCanvas.SetActive(hasMic && inLobby);
                if (indicatorImage != null && indicatorCanvas.activeSelf)
                {
                    if (isTalking)
                    {
                        if (onSprite != null) indicatorImage.sprite = onSprite;
                    }
                    else
                    {
                        if (offSprite != null) indicatorImage.sprite = offSprite;
                    }
                }
            }

            if (!hasMic)
                return;

            KeyCode configured = ItePlugin.voicePushToTalk.value;
            var mode = ItePlugin.voiceMode.value;

            if (mode == VoiceMode.PushToTalk)
            {
                if (Input.GetKeyDown(configured)) StartTalking();
                if (Input.GetKeyUp(configured)) StopTalking();
            }
            else if (mode == VoiceMode.ToggleToTalk)
            {
                if (Input.GetKeyDown(configured))
                {
                    if (isTalking) StopTalking(); else StartTalking();
                }
            }
            else if (mode == VoiceMode.VoiceActivation)
            {
                // ensure mic running
                TryStartMic();
                float level = GetMicLevel();
                int threshold = Mathf.Clamp(ItePlugin.voiceVADThreshold.value, 0, 100);
                // map threshold 0-100 to a linear level; assume level roughly 0-1
                float thresh = threshold / 100f * 0.5f; // empirical scaling
                if (level >= thresh)
                {
                    if (!isTalking) StartTalking();
                }
                else
                {
                    if (isTalking) StopTalking();
                }
            }
        }

        private void TryStartMic()
        {
            try
            {
                if (Microphone.devices == null || Microphone.devices.Length == 0)
                    return;
                for (int i = 0; i < Microphone.devices.Length; i++)
                {
                    if (i == 0)
                    {
                        ItePlugin.wheresMyMic.text = "";
                    }
                    ItePlugin.wheresMyMic.text += $"{i}: " + Microphone.devices[i] + "\n";
                }
                int desired = Mathf.Clamp(ItePlugin.voiceMicIndex.value, 0, Microphone.devices.Length - 1);
                string desiredDevice = Microphone.devices[desired];
                if (micClip == null || micDevice != desiredDevice || !Microphone.IsRecording(micDevice))
                {
                    try
                    {
                        if (micClip != null && Microphone.IsRecording(micDevice))
                        {
                            Microphone.End(micDevice);
                        }
                    }
                    catch { }

                    micDevice = desiredDevice;
                    // choose sample rates based on quality setting
                    switch (ItePlugin.voiceQuality.value)
                    {
                        case VoiceQuality.Low:
                            sampleRate = 16000; // acceptable low quality
                            break;
                        case VoiceQuality.Medium:
                            sampleRate = 44100; // CD quality
                            break;
                        case VoiceQuality.High:
                            sampleRate = 48000; // standard pro audio / voice comms
                            break;
                    }
                    // use ~20ms chunks
                    chunkSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * 0.02f));
                    micClip = Microphone.Start(micDevice, true, 1, sampleRate);
                    // wait a little for mic to start
                    int attempts = 0;
                    while (!(Microphone.GetPosition(micDevice) > 0) && attempts < 50)
                    {
                        System.Threading.Thread.Sleep(10);
                        attempts++;
                    }
                    micPosition = Microphone.GetPosition(micDevice);
                }
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
            float rms = Mathf.Sqrt(sum / data.Length);
            return rms;
        }

        public void StartTalking()
        {
            if (micClip == null) TryStartMic();
            if (micClip == null) return;
            if (isTalking) return;
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
                int samplesAvailable = 0;

                if (pos < micPosition)
                {
                    samplesAvailable = micClip.samples - micPosition + pos;
                }
                else
                {
                    samplesAvailable = pos - micPosition;
                }

                while (samplesAvailable >= chunkSamples)
                {
                    micClip.GetData(buffer, micPosition);

                    // convert to PCM16
                    float sum = 0f;
                    for (int i = 0; i < chunkSamples; i++)
                    {
                        float f = Mathf.Clamp(buffer[i], -1f, 1f);
                        pcm[i] = (short)(f * short.MaxValue);
                        sum += f * f;
                    }

                    float rms = Mathf.Sqrt(sum / chunkSamples);

                    // build packet
                    byte[] payload = new byte[1 + 2 + 1 + 2 + chunkSamples * 2];
                    int idx = 0;
                    payload[idx++] = 0x56; // 'V' voice marker
                    Array.Copy(BitConverter.GetBytes((ushort)sampleRate), 0, payload, idx, 2); idx += 2;
                    payload[idx++] = 1; // channels
                    Array.Copy(BitConverter.GetBytes((ushort)chunkSamples), 0, payload, idx, 2); idx += 2;
                    for (int i = 0; i < chunkSamples; i++)
                    {
                        short s = pcm[i];
                        Array.Copy(BitConverter.GetBytes(s), 0, payload, idx, 2);
                        idx += 2;
                    }

                    // send to nearby players using configured range
                    if (NetworkManager.Instance != null && NetworkManager.Instance.CurrentLobby.Id != 0 && SteamClient.IsValid)
                    {
                        float range = ItePlugin.voiceProximity.value;
                        Vector3 myPos = NewMovement.Instance.transform.position;
                        foreach (var kv in NetworkManager.players)
                        {
                            NetworkPlayer plr = kv.Value;
                            if (plr == null) continue;
                            if (plr.SteamId == NetworkManager.Id) continue; // don't send to self

                            float dist = Vector3.Distance(myPos, plr.transform.position);
                            if (dist <= range)
                            {
                                try
                                {
                                    SteamNetworking.SendP2PPacket(plr.SteamId, payload);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning("[Voice] Failed sending P2P packet: " + e);
                                }
                            }
                        }
                    }

                    // locally show our talking avatar if local player exists
                    var localPlayer = NetworkPlayer.LocalPlayer;
                    if (localPlayer != null && localPlayer.NameTag != null)
                    {
                        // convert rms to 0..1
                        float lvl = Mathf.Clamp01(rms * 5f);
                        localPlayer.NameTag.SetTalkingLevel(lvl);
                    }

                    // advance micPosition
                    micPosition += chunkSamples;
                    if (micPosition >= micClip.samples) micPosition -= micClip.samples;

                    // lower available
                    samplesAvailable -= chunkSamples;
                }

                yield return null;
            }
        }

        // Called from NetworkManager when a non-JSON packet is received
        public void OnP2PDataReceived(byte[] buffer, int length, SteamId sender)
        {
            if (length < 1 || buffer[0] != 0x56) return;

            int idx = 1;
            ushort sr = BitConverter.ToUInt16(buffer, idx); idx += 2;
            byte channels = buffer[idx++];
            ushort samples = BitConverter.ToUInt16(buffer, idx); idx += 2;

            int expectedBytes = samples * 2;
            if (length < idx + expectedBytes) return;

            // decode pcm16 -> float
            float[] floats = new float[samples];
            float sum = 0f;
            for (int i = 0; i < samples; i++)
            {
                short s = BitConverter.ToInt16(buffer, idx); idx += 2;
                float v = s / (float)short.MaxValue;
                floats[i] = v; // assign decoded float
                // apply configured volume and clamp to valid range
                floats[i] = Mathf.Clamp(floats[i] * ItePlugin.volume.value, -1f, 1f);
                sum += v * v;
            }

            float rms = Mathf.Sqrt(sum / samples);
            float level = Mathf.Clamp01(rms * 5f);

            // name tag update AFTER audio started so icon syncs with playback
            NetworkPlayer plr = NetworkPlayer.Find(sender.Value);
            if (plr != null && plr.NameTag != null)
                plr.NameTag.SetTalkingLevel(level);

            if (!ItePlugin.receiveVoice.value)
                return;

            ulong senderId = sender.Value;
            AudioSource src = GetOrCreateSource(senderId);

            // make or retrieve persistent clip for streaming
            AudioClip clip;
            if (!voiceClips.TryGetValue(senderId, out clip) || clip == null)
            {
                int clipSamples = sr * 2; // 2 second buffer
                // create streaming clip so SetData works correctly
                clip = AudioClip.Create($"vc_stream_{senderId}", clipSamples, 1, sr, false);
                voiceClips[senderId] = clip;
                writeHeads[senderId] = 0;

                src.clip = clip;
                src.loop = true; // loop the ring buffer
                src.Play();
            }

            int head = writeHeads[senderId];

            // write incoming PCM floats to ring buffer
            clip.SetData(floats, head);
            head += samples;
            if (head >= clip.samples)
                head = 0;

            writeHeads[senderId] = head;

            // update last packet time so we know the player is speaking
            lastPacketTime[senderId] = Time.time;
        }

        void LateUpdate()
        {
            if (lastPacketTime.Count == 0) return;
            float now = Time.time;
            var ids = lastPacketTime.Keys.ToList();
            foreach (var id in ids)
            {
                if (now - lastPacketTime[id] > silenceTimeout)
                {
                    lastPacketTime.Remove(id);
                    writeHeads.Remove(id);
                    if (voiceClips.TryGetValue(id, out var clip))
                    {
                        voiceClips.Remove(id);
                        try
                        {
                            if (remoteSources.TryGetValue(id, out var src) && src != null)
                            {
                                if (src.isPlaying) src.Stop();
                                src.clip = null;
                            }
                            if (clip != null) Destroy(clip);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("[Voice] Failed to cleanup silent clip: " + e);
                        }
                    }
                }
            }
        }

        private AudioSource GetOrCreateSource(ulong steamId)
        {
            if (remoteSources.ContainsKey(steamId) && remoteSources[steamId] != null) return remoteSources[steamId];

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
            src.minDistance = 3f; // make prox slightly more louder
            src.dopplerLevel = 0f;
            // use configured proximity range for spatial maxDistance
            src.maxDistance = ItePlugin.voiceProximity.value;
            src.loop = false;
            src.playOnAwake = false;

            remoteSources[steamId] = src;
            return src;
        }

        private void CreateIndicator()
        {
            try
            {
                string dir = null;
                if (ItePlugin.Instance != null)
                {
                    dir = Path.GetDirectoryName(ItePlugin.Instance.Info.Location);
                }
                else
                {
                    dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
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

                // create canvas
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
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
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
    }
}
