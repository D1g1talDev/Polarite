using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(ObjectActivator))]
    internal class ObjActPatch
    {
        [HarmonyPatch(nameof(ObjectActivator.Activate), typeof(bool))]
        [HarmonyPostfix]
        static void ActivatePostfix(ObjectActivator __instance)
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.ObjAct, w.GetBytes());
            }
        }
    }
}
