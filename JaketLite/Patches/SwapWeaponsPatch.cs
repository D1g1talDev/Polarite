using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using HarmonyLib;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GunControl))]
    internal class SwapWeaponsPatch
    {
        [HarmonyPatch(nameof(GunControl.SwitchWeapon))]
        [HarmonyPostfix]
        static void NetworkWeaponA(ref int targetSlotIndex)
        {
            if(NetworkManager.InLobby)
            {
                int index = targetSlotIndex - 1;
                if(index > 4)
                {
                    index = 4;
                }
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "gun",
                    name = index.ToString(),
                });
                if(NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.SetWeapon(index);
                }
            }
        }
        [HarmonyPatch(nameof(GunControl.ForceWeapon))]
        [HarmonyPostfix]
        static void NetworkWeaponB()
        {
            if (NetworkManager.InLobby)
            {
                int index = GunControl.Instance.currentSlotIndex - 1;
                if (index > 4)
                {
                    index = 4;
                }
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "gun",
                    name = index.ToString(),
                });
                if (NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.SetWeapon(index);
                }
            }
        }
    }
}
