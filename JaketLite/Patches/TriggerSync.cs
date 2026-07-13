using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using HarmonyLib;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(ObjectActivator))]
    internal class TriggerSync
    {
        [HarmonyPatch(typeof(ObjectActivator), nameof(ObjectActivator.Activate), typeof(bool))]
        [HarmonyPostfix]
        static void Postfix(ObjectActivator __instance)
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.Trigger, w.GetBytes());
            }
        }

        public static bool IsRoom(GameObject obj)
        {
            return obj.GetComponentInChildren<GoreZone>(true) != null;
        }
        public static bool CanTriggerSync()
        {
            if (SceneHelper.CurrentScene == "Level 8-3") return false;
            if (SceneHelper.CurrentScene == "Level 0-2") return false;
            if (SceneHelper.CurrentScene == "Level 0-4") return false;
            if (SceneHelper.CurrentScene == "Level P-1") return false;
            if (SceneHelper.CurrentScene == "Level P-2") return false;
            return true;
            // return SceneHelper.CurrentScene == "Level 0-5" || SceneHelper.CurrentScene == "Level 1-4" || SceneHelper.CurrentScene == "Level 2-4" || SceneHelper.CurrentScene == "Level 3-2" || SceneHelper.CurrentScene == "Level P-1" || SceneHelper.CurrentScene == "Level 4-4" || SceneHelper.CurrentScene == "Level 5-4" || SceneHelper.CurrentScene == "Level 6-2";
        }
    }
}
