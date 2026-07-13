using HarmonyLib;
using Polarite.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(CameraController))]
    internal class SpecialHudCameraAddPatch
    {
        public static GameObject specialHud;
        public static LayerMask mask;

        [HarmonyPatch(nameof(CameraController.Start))]
        [HarmonyPostfix]
        static void Postfix(CameraController __instance)
        {
            mask = 3;
            specialHud = new GameObject("PolariteHUDCamera");
            specialHud.transform.SetParent(__instance.transform);
            Camera cam = specialHud.AddComponent<Camera>();
            cam.CopyFrom(__instance.hudCamera);
            cam.cullingMask = 8;
        }
        [HarmonyPatch(typeof(PostProcessV2_Handler))]
        internal class SpecialHudCameraAddPatchPartII
        {
            [HarmonyPatch(nameof(PostProcessV2_Handler.SetupRTs))]
            [HarmonyPostfix]
            static void Postfix(PostProcessV2_Handler __instance)
            {
                specialHud?.GetComponent<Camera>().SetTargetBuffers(__instance.mainTex.colorBuffer, __instance.mainTex.depthBuffer);
            }
            [HarmonyPatch(nameof(PostProcessV2_Handler.OnPreRenderCallback))]
            [HarmonyPostfix]
            static void PostfixTWO(PostProcessV2_Handler __instance, ref Camera cam)
            {
                if(cam == __instance.hudCam)
                {
                    specialHud?.GetComponent<Camera>().SetTargetBuffers(__instance.mainTex.colorBuffer, __instance.depthBuffer.depthBuffer);
                }
            }
        }
    }
}
