using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameConsole.pcon;

using HarmonyLib;

using ULTRAKILL.Cheats;

using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(PlayerMovementParenting))]
    internal class TrackerPatch
    {
        [HarmonyPatch(nameof(PlayerMovementParenting.FixedUpdate))]
        [HarmonyPrefix]
        static bool LetsNotDetachIfFast(PlayerMovementParenting __instance)
        {
            __instance.currentDelta = Vector3.zero;
            if (__instance.playerTracker == null)
            {
                return false;
            }

            if (!MonoSingleton<NewMovement>.Instance.enabled)
            {
                __instance.DetachPlayer();
                return false;
            }

            Vector3 position = __instance.playerTracker.transform.position;
            float y = __instance.playerTracker.transform.eulerAngles.y;
            Vector3 vector = position - __instance.lastTrackedPos;
            __instance.lastTrackedPos = position;
            bool flag = true;
            if ((bool)MonoSingleton<NewMovement>.Instance && (bool)MonoSingleton<NewMovement>.Instance.groundProperties && MonoSingleton<NewMovement>.Instance.groundProperties.dontRotateCamera)
            {
                flag = false;
            }

            float num = y - __instance.lastAngle;
            __instance.lastAngle = y;
            float num2 = Mathf.Abs(num);
            if (num2 > 180f)
            {
                num2 = 360f - num2;
            }

            if (__instance.rb != null)
            {
                __instance.rb.MovePosition(__instance.rb.position + vector);
            }
            else
            {
                __instance.deltaReceiver.position += vector;
            }

            __instance.playerTracker.transform.position = __instance.deltaReceiver.position;
            __instance.lastTrackedPos = __instance.playerTracker.transform.position;
            __instance.currentDelta = vector;
            if (flag)
            {
                MonoSingleton<CameraController>.Instance.rotationY += num;
            }
            return false;
        }
    }
}
