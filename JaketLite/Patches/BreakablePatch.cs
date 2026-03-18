

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Breakable))]
    internal class BreakablePatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Postfix(Breakable __instance)
        {
            if (!SceneObjectCache.Contains(__instance.gameObject))
            {
                SceneObjectCache.Add(__instance.gameObject);
            }
        }
        [HarmonyPatch(nameof(Breakable.Break), typeof(float))]
        [HarmonyPrefix]
        static void Postfix(Breakable __instance, ref bool ___broken)
        {
            if(NetworkManager.InLobby && !___broken)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.Break, w.GetBytes());
            }
        }
    }
}

