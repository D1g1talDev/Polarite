using Polarite.Debugging;
using Polarite.Multiplayer;
using Polarite.VoiceChat;
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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Polarite.Web
{
    // polarite website by Xulfur

    public static class XServers
    {
        public static bool internet;
        public static bool canShowNotif = true;
        public static bool Servers = true;
        public static void HasInternet(Action<bool> onComplete)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.GooglePing(onComplete));
        }
        /// <summary>
        /// order in action: pfp, name, message
        /// </summary>
        /// <param name="complete"></param>
        public static void GetMOTD(Action<Sprite, string, string> onComplete, Action<Sprite, string, string> onFail)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.MOTDGet(onComplete, onFail));
        }
        public static void Banned(Action<bool, string> values, ulong target)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.GetIsBanned(values, target));
        }

        public static void ExtractPFP(string url, Image img)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.PFPGet((spr) =>
            {
                img.sprite = spr;
            }, url));
        }
        public static void VisualNotif(string msg, bool showForever)
        {
            if (!canShowNotif)
            {
                return;
            }
            GlobalNotification notif = new GlobalNotification();
            notif.pfp = "polaricon";
            notif.type = "visual";
            notif.message = msg;
            notif.userreference = "0";
            notif.user = "Polarite";
            ItePlugin.Instance.ShowNotif(notif, showForever);
        }
        public static void Roles(Action<List<ServerRole>> onRole)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.GetRolesFromServer(onRole));
        }
    }
    public class GlobalNotificationListener : MonoBehaviour
    {
        public string link = "https://polaritemod.com/global";
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
            Logs.Info("Started listening for global notifications", this);
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
                catch (Exception)
                {
                    // Logs.DebugError($"Error while listening for global notifications: {ex.Message}", this);
                }
                response?.Dispose();
                await Task.Delay(5000);
            }
        }
        public void Update()
        {
            if(XServers.canShowNotif)
            {
                while (notifications.TryDequeue(out GlobalNotification notif))
                {
                    if(notif.type == "usergotbanned")
                    {
                        UserJustGotBanned(notif.userreference);
                        return;
                    }
                    if(notif.type == "usergotrole" || notif.type == "userungotrole")
                    {
                        ChatRoles.OnChatRoleUpdate(notif.role, notif.userreference);
                        HandleNotif(notif);
                        return;
                    }
                    if(ShouldFlag(notif))
                    {
                        HandleNotif(notif);
                    }
                }
            }
        }
        public void UserJustGotBanned(string user)
        {
            ulong actual = ulong.Parse(user);
            if(actual == NetworkManager.Id)
            {
                XServers.Banned((i, r) =>
                {
                    if(i)
                    {
                        if (NetworkManager.InLobby) NetworkManager.Instance.LeaveLobby();
                        Logs.Error($"You have been banned from the Polarite servers with the reason: {r}");
                        ItePlugin.Instance.Goodbye();
                        Destroy(ItePlugin.currentUi.gameObject);
                        Destroy(ItePlugin.Instance);
                        if (NetworkPlayer.LocalPlayer != null) Destroy(NetworkPlayer.LocalPlayer);
                        Destroy(NetworkManager.Instance);
                        Destroy(VoiceChatManager.Instance);
                        Destroy(ChatUI.Instance.chatPanel);
                        Destroy(ChatUI.Instance);
                        Destroy(VoiceUI.currentCanvas);
                        Destroy(VoiceUI.Instance);
                        Destroy(this);
                        SceneHelper.RestartSceneAsync();
                    }
                }, actual);
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
        public string role;
    }
}
