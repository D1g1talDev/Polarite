using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

using TMPro;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(OptionsManager))]
    internal class PauseMenuPatch
    {
        [HarmonyPatch(nameof(OptionsManager.Pause))]
        [HarmonyPostfix]
        static void ShowLeaveInstead()
        {
            if(NetworkManager.InLobby)
            {
                Transform menu = MonoSingleton<OptionsManager>.Instance.pauseMenu.transform;
                Transform quitButton = menu.Find("Quit Mission");
                if (quitButton != null)
                {
                    quitButton.GetComponentInChildren<TextMeshProUGUI>().text = "LEAVE LOBBY";
                    ColorUtility.TryParseHtmlString("#91FFFF", out Color color);
                    quitButton.GetComponent<Image>().color = color;
                }
                // re-enable stuff disabled by pause
                DisablePauseEffects();
            }
            else
            {
                Transform menu = MonoSingleton<OptionsManager>.Instance.pauseMenu.transform;
                Transform quitButton = menu.Find("Quit Mission");
                if (quitButton != null)
                {
                    quitButton.GetComponentInChildren<TextMeshProUGUI>().text = "QUIT MISSION";
                    quitButton.GetComponent<Image>().color = Color.white;
                }
            }
        }
        [HarmonyPatch(nameof(OptionsManager.QuitMission))]
        [HarmonyPostfix]
        static void LeaveAswell()
        {
            if (NetworkManager.InLobby)
            {
                NetworkManager.Instance.LeaveLobby();
            }
        }
        [HarmonyPatch(nameof(OptionsManager.LateUpdate))]
        [HarmonyPrefix]
        static bool LateUpdateFix()
        {
            return !NetworkManager.InLobby;
        }

        public static void DisablePauseEffects()
        {
            MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allPitch", 1f);
            MonoSingleton<AudioMixerController>.Instance.doorSound.SetFloat("allPitch", 1f);
            if (MonoSingleton<MusicManager>.Instance != null)
            {
                MonoSingleton<MusicManager>.Instance.UnfilterMusic();
            }
            MonoSingleton<NewMovement>.Instance.enabled = true;
            // video
            VideoPlayer[] videos = GameObject.FindObjectsOfType<VideoPlayer>();
            foreach (var vid in videos)
            {
                if (vid.isPlaying)
                {
                    vid.Play();
                }
            }
        }
    }
}
