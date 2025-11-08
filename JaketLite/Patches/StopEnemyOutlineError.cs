using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Polarite.Patches{

//the code here is from dolfelive's own multiplayer mod
//doomahreal added it to the mod, and it was taken with permission

[HarmonyPatch(typeof(PostProcessV2_Handler), "SetupOutlines")]
internal class StopEnemyOutlineError
{
    [HarmonyPrefix]
    public static bool OutlinePatch(PostProcessV2_Handler __instance, bool forceOnePixelOutline)
    {
        var prefs = MonoSingleton<PrefsManager>.Instance;
        int simplify = prefs.GetInt("simplifyEnemies", 0);
        int simplifyDist = prefs.GetInt("simplifyEnemiesDistance", 0);
        int thickness = prefs.GetInt("outlineThickness", 0);

        Debug.Log($"Simplify: {simplify}");
        Debug.Log($"Distance: {simplifyDist}");
        Debug.Log($"Thickness: {thickness}");

        __instance.distance = thickness;

        if (__instance.mainCam == null)
        {
            __instance.SetupRTs();
            return false;
        }

        __instance.mainCam.RemoveCommandBuffers(CameraEvent.BeforeForwardOpaque);

        if (__instance.mainTex == null)
        {
            __instance.SetupRTs();
            return false;
        }

        if (__instance.isGLCore)
        {
            return false;
        }

        if (__instance.outlineCB == null)
        {
            __instance.outlineCB = new CommandBuffer();
            __instance.outlineCB.name = "Outlines";
        }

        Vector2 res = new Vector2(__instance.mainTex.width, __instance.mainTex.height);
        Vector2 resDiff = res / new Vector2(Screen.width, Screen.height);
        float num = __instance.distance;
        if (__instance.distance > 1)
            num = __instance.distance * Mathf.Max(resDiff.x, resDiff.y);

        __instance.outlineCB.Clear();

        if (__instance.outlineMat == null)
            __instance.outlineMat = new Material(__instance.outlinePx);

        __instance.outlineCB.SetGlobalVector("_Resolution", res);
        __instance.outlineCB.SetGlobalVector("_ResolutionDiff", resDiff);
        Mathf.CeilToInt(__instance.width / 8f);
        Mathf.CeilToInt(__instance.height / 8f);

        if (!forceOnePixelOutline && __instance.distance > 1 && num > 1f && __instance.oman.simplifyEnemies)
        {
            __instance.distance = Mathf.Min(__instance.distance, 16);
            __instance.outlineCB.Blit(__instance.reusableBufferA, __instance.reusableBufferB, __instance.outlineMat, 0);

            float num2 = 8f;
            int num3 = 0;
            while (num2 >= 0.5f || __instance.reusableBufferA.name == "Reusable B")
            {
                __instance.outlineCB.SetGlobalFloat("_TestDistance", num2);
                __instance.outlineCB.Blit(__instance.reusableBufferB, __instance.reusableBufferA, __instance.outlineMat, 1);

                var temp = __instance.reusableBufferB;
                __instance.reusableBufferB = __instance.reusableBufferA;
                __instance.reusableBufferA = temp;
                num2 *= 0.5f;
                num3++;
            }

            __instance.outlineCB.SetGlobalFloat("_OutlineDistance", __instance.distance);
            __instance.outlineCB.SetGlobalFloat("_TestDistance", num2);
            __instance.outlineCB.Blit(__instance.reusableBufferB, __instance.mainTex, __instance.outlineMat, 2);
            __instance.outlineCB.SetRenderTarget(__instance.reusableBufferA);
            __instance.outlineCB.ClearRenderTarget(false, true, Color.black);
            __instance.outlineCB.SetRenderTarget(__instance.reusableBufferB);
            __instance.outlineCB.ClearRenderTarget(false, true, Color.black);
        }
        else
        {
            int passToUse = Mathf.Min(2, __instance.outlineMat.passCount - 1);
            __instance.outlineCB.Blit(__instance.reusableBufferA, __instance.mainTex, __instance.outlineMat, passToUse);
            __instance.outlineCB.SetRenderTarget(__instance.reusableBufferA);
            __instance.outlineCB.ClearRenderTarget(false, true, Color.black);
        }

        __instance.mainCam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, __instance.outlineCB);

        CommandBuffer clearCmd = new CommandBuffer();
        clearCmd.name = "Clear Before Draw";
        clearCmd.SetRenderTarget(__instance.reusableBufferA);
        clearCmd.ClearRenderTarget(false, true, Color.black);
        __instance.mainCam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, clearCmd);

        return false;
    }
}
}