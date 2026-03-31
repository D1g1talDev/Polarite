using Polarite.Multiplayer;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Polarite.Web
{
    public static class XServers
    {
        public static bool internet;
        public static bool canShowNotif = true;
        public static void HasInternet(Action<bool> onComplete)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.GooglePing("8.8.8.8", onComplete));
        }
        /// <summary>
        /// order in action: pfp, name, message
        /// </summary>
        /// <param name="complete"></param>
        public static void GetMOTD(Action<Sprite, string, string> onComplete, Action<Sprite, string, string> onFail)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.MOTDGet(onComplete, onFail));
        }

        public static void ExtractPFP(string url, Image img)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.PFPGet((spr) =>
            {
                img.sprite = spr;
            }, url));
        }
    }
    public class GlobalNotificationListener : MonoBehaviour
    {
        public string link = "https://polarite.xulfur.me/global";
        public HttpClient client = new HttpClient();
        public bool listening = false;
        public ConcurrentQueue<GlobalNotification> notifications = new ConcurrentQueue<GlobalNotification>();

        public void Start()
        {
            listening = true;
            Task.Run(() => Listen());
        }
        public async Task Listen()
        {
            HttpClient client = new HttpClient();
            while (listening)
            {
                HttpResponseMessage response = null;
                try
                {
                    response = await client.GetAsync(link, HttpCompletionOption.ResponseHeadersRead);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStreamAsync();
                        StreamReader reader = new StreamReader(content);
                        while (listening && !reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                            {
                                string json = line.Substring(6).Trim();
                                GlobalNotification notification = JsonUtility.FromJson<GlobalNotification>(json);
                                notifications.Enqueue(notification);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ItePlugin.LogDebug($"Error while listening for global notifications: {ex.Message}");
                }
                response?.Dispose();
                await Task.Delay(2000);
            }
        }
        public void Update()
        {
            if(XServers.canShowNotif)
            {
                while (notifications.TryDequeue(out GlobalNotification notif))
                {
                    if(ShouldFlag(notif))
                    {
                        HandleNotif(notif);
                    }
                }
            }
        }
        public void HandleNotif(GlobalNotification notif)
        {
            ulong target = ulong.Parse(notif.userreference);
            if (target != 0)
            {
                if(NetworkManager.Id == target)
                {
                    notif.type = "targeted";
                    ItePlugin.Instance.ShowNotif(notif);
                }
            }
            else
            {
                ItePlugin.Instance.ShowNotif(notif);
            }
        }
        public static bool ShouldFlag(GlobalNotification notif)
        {
            return notif.type == "newglobal";
        }
        public void OnApplicationQuit()
        {
            listening = false;
        }
    }
    public struct GlobalNotification
    {
        public string type;
        public string pfp;
        public string user;
        public string message;
        public string userreference;
    }
}
