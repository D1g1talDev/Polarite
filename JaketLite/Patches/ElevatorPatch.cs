using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Elevator))]
    internal class ElevatorPatch
    {
        [HarmonyPatch(nameof(Elevator.MoveToFloor))]
        static void Postfix(Elevator __instance, int target)
        {
            if(NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                w.WriteInt(target);
                // teleporting?
                w.WriteBool(false);
                NetworkManager.Instance.BroadcastPacket(PacketType.Elevator, w.GetBytes());
            }
        }
        [HarmonyPatch(nameof(Elevator.TeleportToFloor))]
        static void PostfixTwo(Elevator __instance, int target)
        {
            if (NetworkManager.InLobby)
            {
                PacketWriter w = new PacketWriter();
                w.WriteString(SceneObjectCache.GetScenePath(__instance.gameObject));
                w.WriteInt(target);
                // teleporting?
                w.WriteBool(true);
                NetworkManager.Instance.BroadcastPacket(PacketType.Elevator, w.GetBytes());
            }
        }
    }
}
