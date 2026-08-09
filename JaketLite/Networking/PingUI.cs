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
        public class Pulser : MonoBehaviour
        {
            public Image target;
            private float speed;
            private void Start() => target = GetComponent<Image>();
            private void Update() => target.color = new Color(1f, 0f, 0f, Mathf.PingPong(Time.time * speed, 1f));
            public void SetSpeed(float val) => speed = val;
        }
        public GameObject smallBG, bigBG;
        public Pulser ring;
        public TextMeshProUGUI ping, loss;

        public static PingUI Instance;
        public float tick = 0f;

        public void Start()
        {
            if (Instance == null) Instance = this;
            smallBG = transform.Find("SmallPingBG").gameObject;
            bigBG = transform.Find("PingWithLossBG").gameObject;
            ring = transform.Find("PingWithLossRing").gameObject.GetOrAddComponent<Pulser>();
            ping = gameObject.FindWithComponent<TextMeshProUGUI>("Ping");
            loss = gameObject.FindWithComponent<TextMeshProUGUI>("Loss");
        }
        private float Loss()
        {
            return ((32f - NetworkManager.ClientToHost.Connection.QuickStatus().InPacketsPerSec) / 32f) * 100f;
        }
        public void Update()
        {
            if(NetworkManager.ClientAndConnected && NetworkManager.IsConnectedSocket && ItePlugin.showPing.value)
            {
                tick += Time.deltaTime;
                if (tick < 0.2f) return;
                tick = 0f;
                ping.gameObject.SetActive(true);
                ping.text = $"{NetworkManager.ClientToHost.Connection.QuickStatus().Ping} ms";
                float lVal = Loss();
                loss.text = $"{lVal:F1}% loss";
                bool showLoss = lVal >= 1.0f;
                smallBG.SetActive(!showLoss);
                bigBG.SetActive(showLoss);
                ring.gameObject.SetActive(showLoss);
                loss.gameObject.SetActive(showLoss);
                ring.SetSpeed(lVal);
            }
            else
            {
                tick = 0f;
                smallBG.SetActive(false);
                bigBG.SetActive(false);
                ring.gameObject.SetActive(false);
                ping.gameObject.SetActive(false);
                loss.gameObject.SetActive(false);
            }
        }
    }
}
