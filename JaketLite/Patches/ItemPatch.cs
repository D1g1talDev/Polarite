using HarmonyLib;
using Polarite.Multiplayer;
using Polarite.Networking.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Skull))]
    internal class SkullPatch
    {
        [HarmonyPatch(nameof(Skull.Awake))]
        [HarmonyPostfix]
        static void Postfix(Skull __instance)
        {
            if(NetworkManager.InLobby && !__instance.gameObject.IsNetwork())
            {
                __instance.gameObject.AddComponent<NetworkSkull>();
            }
        }
    }
    [HarmonyPatch(typeof(Torch))]
    internal class TorchPatch
    {
        [HarmonyPatch(nameof(Torch.Start))]
        [HarmonyPostfix]
        static void Postfix(Torch __instance)
        {
            if (NetworkManager.InLobby && !__instance.gameObject.IsNetwork())
            {
                __instance.gameObject.AddComponent<NetworkSkull>();
            }
        }
    }
    [HarmonyPatch(typeof(Flammable))]
    internal class FirePatch
    {
        [HarmonyPatch(nameof(Flammable.Burn))]
        [HarmonyPostfix]
        static void Postfix(Flammable __instance, ref float newHeat, ref bool noInstaDamage)
        {
            if(__instance.specialFlammable && NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteFloat(newHeat);
                w.WriteBool(noInstaDamage);
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                NetworkManager.Instance.BroadcastPacket(PacketType.Flammable, w.GetBytes());
            }
        }
    }
}
