using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Polarite.VoiceChat
{
    public class VoicePTTBind : MonoBehaviour
    {
        public Button button;
        public bool listening;
        public KeyCode selected = KeyCode.V;

        public void Act()
        {
            listening = true;
            button.GetComponent<Image>().color = Color.red;
            button.GetComponentInChildren<TextMeshProUGUI>().text = "...";
            button.onClick.RemoveAllListeners();
        }
        public void UnAct()
        {
            listening = false;
            button.GetComponent<Image>().color = Color.white;
        }
        public void Set(KeyCode key)
        {
            UnAct();
            button.GetComponentInChildren<TextMeshProUGUI>().text = ChatUI.GetKeyName(key);
            selected = key;
            button.onClick.AddListener(Act);
        }
        public void OnGUI()
        {
            if (!listening) return;
            Event key = Event.current;
            if(key.isKey)
            {
                Set(key.keyCode);
            }
            else if(key.isMouse)
            {
                switch(key.button)
                {
                    case 0:
                        Set(KeyCode.Mouse0);
                        break;
                    case 1:
                        Set(KeyCode.Mouse1);
                        break;
                    case 2:
                        Set(KeyCode.Mouse2);
                        break;
                    case 3:
                        Set(KeyCode.Mouse3);
                        break;
                    case 4:
                        Set(KeyCode.Mouse4);
                        break;
                    case 5:
                        Set(KeyCode.Mouse5);
                        break;
                    case 6:
                        Set(KeyCode.Mouse6);
                        break;
                }
            }
        }
    }
}
