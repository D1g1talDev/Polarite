using Polarite.Multiplayer;
using Polarite.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Polarite.Networking
{
    public class PingUI : MonoBehaviour
    {
        public GameObject smallBG;
        public TextMeshProUGUI ping;
        public static PingUI Instance;
        public float tick = 0f;

        public void Start()
        {
            if (Instance == null) Instance = this;
            smallBG = transform.Find("SmallPingBG").gameObject;
            ping = gameObject.FindWithComponent<TextMeshProUGUI>("Ping");
        }
        public void Update()
        {
            if(NetworkManager.ClientAndConnected && NetworkManager.IsConnectedSocket && ItePlugin.showPing.value && !NetworkManager.SceneLoading)
            {
                tick += Time.deltaTime;
                if (tick < 0.1f) return;
                tick = 0f;
                ping.gameObject.SetActive(true);
                ping.text = $"{NetworkManager.ClientToHost.Connection.QuickStatus().Ping} ms";
                smallBG.SetActive(true);
            }
            else
            {
                tick = 0f;
                smallBG.SetActive(false);
                ping.gameObject.SetActive(false);
            }
        }
    }
}
