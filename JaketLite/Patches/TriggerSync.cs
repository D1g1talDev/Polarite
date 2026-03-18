using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using HarmonyLib;

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
    }
}
