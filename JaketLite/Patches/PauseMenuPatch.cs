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
    }
}
