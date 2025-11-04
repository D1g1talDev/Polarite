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
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "punch",
                    name = "p"
                });
                if(NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.PunchAnim();
                }
            }
        }
        [HarmonyPatch(nameof(Punch.CoinFlip))]
        [HarmonyPostfix]
        static void CoinPatch()
        {
            if (NetworkManager.InLobby)
            {
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "coin",
                    name = "c"
                });
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
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "whip",
                    name = "w"
                });
                if (NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.WhipAnim();
                }
            }
        }
    }
}
