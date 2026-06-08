using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Polarite
{
    public class PolariteMenuManager : MonoBehaviour
    {
        public Button uiOpen, saveLobSettings;
        public UnityEvent host, leave, invite, join;
        public GameObject mainPanel;
        public TMP_InputField maxP, lobbyName, code;
        public TMP_Dropdown lobbyType, canCheat;

        public TextMeshProUGUI statusHost, statusJoin, lowerMaxPWarn;
        public string codeHost;

        public GameObject notifBox;

        public void ToggleMainPanel()
        {
            mainPanel.SetActive(!mainPanel.activeSelf);
            if(!ItePlugin.openedPolariteMenu.value)
            {
                ItePlugin.openedPolariteMenu.value = true;
                ItePlugin.Instance.ForceHideNotif();
            }
        }
    }
}
