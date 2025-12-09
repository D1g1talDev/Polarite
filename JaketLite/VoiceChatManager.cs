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
using Concentus.Structs;
using Concentus.Enums;
using TMPro;

// class made by doomahreal, also IM BECOMING THE BOILED ONE WITH THIS CODE AHHHHHHHHHHHHHH WHY DOES IT KEEP SOUNDING SHITTY!!!!!!!!

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
        private int currentIndex = 0;
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
        private bool wasInLobby = false;

        // Only Opus codec
        private const byte CODEC_OPUS = 1;

        // Concentus encoder for local mic and decoders per remote peer
        private OpusEncoder opusEncoder;
        private readonly Dictionary<ulong, OpusDecoder> opusDecoders = new Dictionary<ulong, OpusDecoder>();

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
            wasInLobby = NetworkManager.InLobby;
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

            // detect leaving lobby: cleanup audio resources to avoid hearing peers while disconnected
            if (wasInLobby && !inLobby)
            {
                CleanupAllPeers();
            }
            wasInLobby = inLobby;

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
                currentIndex = desired;

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

                // use a 20ms target chunk but clamp to reasonable bounds to avoid too small or too large packets
                chunkSamples = Mathf.Clamp(Mathf.Max(1, Mathf.RoundToInt(sampleRate * 0.02f)), 128, 2048);
                micClip = Microphone.Start(micDevice, true, 1, sampleRate);

                int attempts = 0;
                while (!(Microphone.GetPosition(micDevice) > 0) && attempts < 50)
                {
                    System.Threading.Thread.Sleep(10);
                    attempts++;
                }

                micPosition = Microphone.GetPosition(micDevice);

                // (re)create opus encoder with current settings
                try
                {
#pragma warning disable 0618
                    opusEncoder = new OpusEncoder(sampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
#pragma warning restore 0618
                    // choose bitrate based on configured voice quality
                    switch (ItePlugin.voiceQuality.value)
                    {
                        case VoiceQuality.Low:
                            opusEncoder.Bitrate = 12000; // ~12 kbps for low quality
                            break;
                        case VoiceQuality.Medium:
                            opusEncoder.Bitrate = 24000; // ~24 kbps for medium
                            break;
                        case VoiceQuality.High:
                            opusEncoder.Bitrate = 48000; // ~48 kbps for high
                            break;
                        default:
                            opusEncoder.Bitrate = 24000;
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Voice] Failed to create Opus encoder: " + e);
                    opusEncoder = null;
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
            return Mathf.Sqrt(sum / data.Length);
        }

        public void StartTalking()
        {
            if (micClip == null || ItePlugin.voiceMicIndex.value != currentIndex) TryStartMic();
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

                    if (opusEncoder == null)
                    {
                        // fallback: don't send if encoder unavailable
                        micPosition += chunkSamples;
                        if (micPosition >= micClip.samples) micPosition -= micClip.samples;
                        samplesAvailable -= chunkSamples;
                        Debug.Log("oops, no encoder");
                        continue;
                    }

                    // encode with opus
                    byte[] encoded = new byte[4000];
                    int encodedLen = 0;
                    try
                    {
#pragma warning disable 0618
                        encodedLen = opusEncoder.Encode(pcm, 0, chunkSamples, encoded, 0, encoded.Length);
#pragma warning restore 0618
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[Voice] Opus encode failed: " + e);
                        encodedLen = 0;
                    }

                    if (encodedLen <= 0)
                    {
                        micPosition += chunkSamples;
                        if (micPosition >= micClip.samples) micPosition -= micClip.samples;
                        samplesAvailable -= chunkSamples;
                        continue;
                    }

                    // payload: 1 byte magic, 2 bytes sampleRate, 1 byte channels, 1 byte codec, 2 bytes samples, then opus payload bytes
                    int headerSize = 1 + 2 + 1 + 1 + 2;
                    byte[] payload = new byte[headerSize + encodedLen];
                    int idx = 0;
                    payload[idx++] = 0x56;
                    Array.Copy(BitConverter.GetBytes((ushort)sampleRate), 0, payload, idx, 2); idx += 2;
                    payload[idx++] = 1; // channels
                    payload[idx++] = CODEC_OPUS;
                    Array.Copy(BitConverter.GetBytes((ushort)chunkSamples), 0, payload, idx, 2); idx += 2;
                    Array.Copy(encoded, 0, payload, idx, encodedLen);

                    if (NetworkManager.Instance != null && NetworkManager.Instance.CurrentLobby.Id != 0 && SteamClient.IsValid)
                    {
                        float range = 250f;
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
                        localPlayer.NameTag.SetTalking(Mathf.Clamp01(rms * 5f) > 0.05f);

                    micPosition += chunkSamples;
                    if (micPosition >= micClip.samples) micPosition -= micClip.samples;
                    samplesAvailable -= chunkSamples;
                }

                yield return null;
            }
        }

        public void OnP2PDataReceived(byte[] buffer, int length, SteamId sender)
        {
            // ignore voice packets when not in a valid lobby or Steam not initialized
            if (NetworkManager.Instance == null || NetworkManager.Instance.CurrentLobby.Id == 0 || !SteamClient.IsValid) return;
            // ignore packets from users not in our current player list
            if (!NetworkManager.players.ContainsKey(sender.Value.ToString())) return;

            // minimal header check: magic + sr + channels + codec + samples
            if (length < 1 + 2 + 1 + 1 + 2) return;
            if (buffer[0] != 0x56) return;

            int idx = 1;
            ushort sr = BitConverter.ToUInt16(buffer, idx); idx += 2;
            byte channels = buffer[idx++];
            byte codec = buffer[idx++];
            ushort samples = BitConverter.ToUInt16(buffer, idx); idx += 2;

            if (codec != CODEC_OPUS) return; // only accept opus

            int expectedBytes = length - idx;
            if (expectedBytes <= 0) return;

            // decode opus
            ulong senderId = sender.Value;
            OpusDecoder decoder;
            if (!opusDecoders.TryGetValue(senderId, out decoder) || decoder == null)
            {
                try
                {
#pragma warning disable 0618
                    decoder = new OpusDecoder(sr, Math.Max(1, (int)channels));
#pragma warning restore 0618
                    opusDecoders[senderId] = decoder;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Voice] Failed to create Opus decoder: " + e);
                    return;
                }
            }

            short[] decodedPcm = new short[samples * Math.Max(1, (int)channels)];
            int decodedSamples = 0;
            try
            {
#pragma warning disable 0618
                decodedSamples = decoder.Decode(buffer, idx, expectedBytes, decodedPcm, 0, samples, false);
#pragma warning restore 0618
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Voice] Opus decode failed: " + e);
                return;
            }

            if (decodedSamples <= 0) return;

            int totalSamples = decodedSamples * Math.Max(1, (int)channels);
            float[] floats = new float[totalSamples];
            float sum = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float v = decodedPcm[i] / (float)short.MaxValue;

                floats[i] = v;
                sum += v * v;
            }

            float level = Mathf.Clamp01(Mathf.Sqrt(sum / Math.Max(1, decodedSamples)) * 5f);
            NetworkPlayer plr = NetworkPlayer.Find(sender.Value);
            if (plr != null && plr.NameTag != null) plr.NameTag.SetTalking(level > 0.05f);
            if (!ItePlugin.receiveVoice.value) return;

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

                // use a 3 second ring buffer by default to reduce underruns while keeping latency reasonable
                int clipSamples = sr * 3;
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
                int clipLen = clip.samples;

                if (totalSamples >= clipLen)
                {
                    // payload bigger than clip: keep last clipLen samples
                    float[] small = new float[clipLen];
                    Array.Copy(floats, totalSamples - clipLen, small, 0, clipLen);
                    clip.SetData(small, 0);
                    head = 0;
                }
                else
                {
                    int spaceAtEnd = clipLen - head;
                    if (totalSamples <= spaceAtEnd)
                    {
                        // fits without wrapping
                        clip.SetData(floats, head);
                        head += totalSamples;
                        if (head >= clipLen) head = 0;
                    }
                    else
                    {
                        // split into two writes to wrap around circular buffer
                        float[] part1 = new float[spaceAtEnd];
                        Array.Copy(floats, 0, part1, 0, spaceAtEnd);
                        clip.SetData(part1, head);

                        int remaining = totalSamples - spaceAtEnd;
                        float[] part2 = new float[remaining];
                        Array.Copy(floats, spaceAtEnd, part2, 0, remaining);
                        clip.SetData(part2, 0);

                        head = remaining;
                    }
                }

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
                existing.maxDistance = 110f;
                existing.loop = true;
                // refresh the voice so the mute button works
                if(!Voice.idToSource.ContainsValue(existing))
                {
                    Voice.Remove(steamId);
                    Voice.Add(steamId, existing);
                }
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
            src.minDistance = 100f; // music kinda overlaps voice so this will make it so you can hear voice better with music
            src.dopplerLevel = 0f;
            src.maxDistance = 110f;
            src.priority = 0;
            src.loop = true;
            src.playOnAwake = false;
            remoteSources[steamId] = src;
            Voice.Add(steamId, src);
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
                Voice.Clear();

                // dispose decoders
                try
                {
                    opusEncoder = null;
                    opusDecoders.Clear();
                }
                catch { }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Voice] Failed to cleanup peers: " + e);
            }
        }

    }

    // static vc class to manage sources
    public static class Voice
    {
        public static Dictionary<ulong, AudioSource> idToSource = new Dictionary<ulong, AudioSource>();
        public static List<ulong> mutedPlayers = new List<ulong>();

        public static void Toggle(bool val, ulong id)
        {
            if(!idToSource.ContainsKey(id))
            {
                return;
            }
            if(mutedPlayers.Contains(id))
            {
                return;
            }
            AudioSource source = idToSource[id];
            if (source != null)
            {
                source.mute = val;
            }
        }
        public static void Mute(ulong id)
        {
            if(mutedPlayers.Contains(id))
            {
                return;
            }
            mutedPlayers.Add(id);
            AudioSource source = idToSource[id];
            if (source != null)
            {
                source.mute = true;
            }
        }
        public static void Unmute(ulong id)
        {
            if (!mutedPlayers.Contains(id))
            {
                return;
            }
            mutedPlayers.Remove(id);
            AudioSource source = idToSource[id];
            if (source != null)
            {
                source.mute = false;
            }
        }
        public static void Add(ulong id, AudioSource source)
        {
            idToSource.Add(id, source);
            if(mutedPlayers.Contains(id) && source != null)
            {
                source.mute = true;
            }
        }
        public static void Remove(ulong id)
        {
            idToSource.Remove(id);
        }
        public static void Clear()
        {
            idToSource.Clear();
        }
    }
}
