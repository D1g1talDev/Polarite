using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;

namespace Polarite.Web
{
    public static class XServers
    {
        public static bool internet;
        public static void HasInternet(Action<bool> onComplete)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.GooglePing("8.8.8.8", onComplete));
        }
        /// <summary>
        /// order in action: pfp, name, message
        /// </summary>
        /// <param name="complete"></param>
        public static void GetMOTD(Action<Sprite, string, string> onComplete)
        {
            ItePlugin.Instance.StartCoroutine(ItePlugin.Instance.MOTDGet(onComplete));
        }
    }
}
