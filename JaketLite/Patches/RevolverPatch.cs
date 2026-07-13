using HarmonyLib;
using Polarite.Networking.Skins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(Revolver))]
    internal class RevolverPatch
    {
        [HarmonyPatch(nameof(Revolver.Start))]
        [HarmonyPostfix]
        static void Postfix(Revolver __instance)
        {
            if(NetworkManager.InLobby)
            {
                Transform arm = __instance.transform.Find(__instance.altVersion ? "Revolver_Rerigged_Alternate/RightArm" : "Revolver_Rerigged_Standard/RightArm");
                if (arm != null)
                {
                    SkinnedMeshRenderer r = arm.GetComponent<SkinnedMeshRenderer>();
                    SkinManagerV2.CustomColor(r, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.RIGHT_ARM_MASK, "RArm" + NetworkManager.Id, 0, true);
                }
            }
        }
    }
}
