using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Punch))]
    internal class PunchPatch
    {
        [HarmonyPatch(nameof(Punch.PunchStart))]
        [HarmonyPostfix]
        static void PunchP(Punch __instance)
        {
            if(NetworkManager.InLobby)
            {
                ItePlugin.ArmCheck(SwapWeaponsPatch.AltWeapon(GunControl.Instance.currentWeapon));
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Punch, w.GetBytes());
                if(NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.PunchAnim();
                }
                if(__instance.type == FistType.Standard)
                {
                    PacketWriter w1 = new PacketWriter();
                    NetworkManager.Instance.BroadcastPacket(PacketType.PunchNormal, w1.GetBytes());
                }
                else
                {
                    PacketWriter w2 = new PacketWriter();
                    NetworkManager.Instance.BroadcastPacket(PacketType.PunchHeavy, w2.GetBytes());
                }
            }
        }
        [HarmonyPatch(nameof(Punch.CoinFlip))]
        [HarmonyPostfix]
        static void CoinPatch()
        {
            if (NetworkManager.InLobby)
            {
                ItePlugin.ArmCheck(SwapWeaponsPatch.AltWeapon(GunControl.Instance.currentWeapon));
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Coin, w.GetBytes());
                if (NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.CoinAnim();
                }
            }
        }
        [HarmonyPatch(nameof(Punch.ShopMode))]
        [HarmonyPostfix]
        static void ShopModePatch()
        {
           NetworkPlayer.ToggleColsForAll(false);
        }
        [HarmonyPatch(nameof(Punch.StopShop))]
        [HarmonyPostfix]
        static void StopShopPatch()
        {
           NetworkPlayer.ToggleColsForAll(true);
        }
    }
    [HarmonyPatch(typeof(HookArm))]
    internal class WhiplashFixes
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void WhiplashAnim(ref float ___cooldown)
        {
            if (NetworkManager.InLobby && MonoSingleton<InputManager>.Instance.InputSource.Hook.WasPerformedThisFrame && ___cooldown <= 0f)
            {
                ItePlugin.ArmCheck(SwapWeaponsPatch.AltWeapon(GunControl.Instance.currentWeapon));
                PacketWriter w = new PacketWriter();
                NetworkManager.Instance.BroadcastPacket(PacketType.Whip, w.GetBytes());
                if (NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.WhipAnim();
                }
            }
        }
    }
}
