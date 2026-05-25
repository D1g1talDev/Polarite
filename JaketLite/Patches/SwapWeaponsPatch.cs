using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;

using HarmonyLib;
using UnityEngine;
using Polarite.Networking.Skins;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GunControl))]
    internal class SwapWeaponsPatch
    {
        [HarmonyPatch(nameof(GunControl.SwitchWeapon))]
        [HarmonyPostfix]
        static void NetworkWeaponA(GunControl __instance, ref int targetSlotIndex)
        {
            if(NetworkManager.InLobby)
            {
                int index = targetSlotIndex - 1;
                if(index > 4)
                {
                    index = 4;
                }
                bool alt = AltWeapon(__instance.currentWeapon);
                PacketWriter w = new PacketWriter();
                w.WriteInt(index);
                w.WriteBool(alt);
                NetworkManager.Instance.BroadcastPacket(PacketType.Gun, w.GetBytes());
                if(NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.SetWeapon(alt, index);
                }

                if(index == 0)
                {
                    Transform arm = __instance.currentWeapon.transform.Find(alt ? "Revolver_Rerigged_Alternate/RightArm" : "Revolver_Rerigged_Standard/RightArm");
                    if (arm != null)
                    {
                        SkinnedMeshRenderer r = arm.GetComponent<SkinnedMeshRenderer>();
                        SkinManagerV2.CustomColor(r, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.RIGHT_ARM_MASK, "RArm" + NetworkManager.Id, 0, true);
                    }
                }
            }
        }
        [HarmonyPatch(nameof(GunControl.ForceWeapon))]
        [HarmonyPostfix]
        static void NetworkWeaponB(GunControl __instance)
        {
            if (NetworkManager.InLobby)
            {
                int index = GunControl.Instance.currentSlotIndex - 1;
                if (index > 4)
                {
                    index = 4;
                }
                bool alt = AltWeapon(__instance.currentWeapon);
                PacketWriter w = new PacketWriter();
                w.WriteInt(index);
                w.WriteBool(alt);
                NetworkManager.Instance.BroadcastPacket(PacketType.Gun, w.GetBytes());
                if (NetworkPlayer.LocalPlayer.testPlayer)
                {
                    NetworkPlayer.LocalPlayer.SetWeapon(alt, index);
                }

                if (index == 0)
                {
                    Transform arm = __instance.currentWeapon.transform.Find(alt ? "Revolver_Rerigged_Alternate/RightArm" : "Revolver_Rerigged_Standard/RightArm");
                    if (arm != null)
                    {
                        SkinnedMeshRenderer r = arm.GetComponent<SkinnedMeshRenderer>();
                        SkinManagerV2.CustomColor(r, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.RIGHT_ARM_MASK, "RArm" + NetworkManager.Id, 0, true);
                    }
                }
            }
        }
        public static bool AltWeapon(GameObject obj)
        {
            if(GunControl.Instance == null)
            {
                return false;
            }
            if(GunControl.Instance.currentWeapon == null)
            {
                return false;
            }

            if(obj.TryGetComponent<Revolver>(out var rev))
            {
                return rev.altVersion;
            }
            if(obj.TryGetComponent<ShotgunHammer>(out var sh))
            {
                return true;
            }
            if(obj.TryGetComponent<Nailgun>(out var na))
            {
                return na.altVersion;
            }
            return false;
        }
    }
}
