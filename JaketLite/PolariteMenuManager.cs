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
        public Button uiOpen;
        public UnityEvent host, leave, invite, join;
        public GameObject mainPanel;
        public TMP_InputField maxP, lobbyName, code;
        public TMP_Dropdown lobbyType, canCheat;

        public TextMeshProUGUI statusHost, statusJoin;
        public string codeHost;

        public void CallHost()
        {
            host.Invoke();
            Debug.Log("Called host");
        }
        public void CallLeave()
        {
            leave.Invoke();
            Debug.Log("Called leave");
        }
        public void CallInvite()
        {
            invite.Invoke();
            Debug.Log("Called invite");
        }
        public void CallJoin()
        {
            join.Invoke();
            Debug.Log("Called join");
        }
        public void ToggleMainPanel()
        {
            mainPanel.SetActive(!mainPanel.activeSelf);
        }
    }
}
