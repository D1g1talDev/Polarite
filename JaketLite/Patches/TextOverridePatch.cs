using HarmonyLib;
using UnityEngine;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(TextOverride))]
    internal class TextOverridePatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(TextOverride __instance)
        {
            if (__instance == null) return true;

            if (!NetworkManager.InLobby) return true;

            if (__instance.gameObject.GetComponent<CutsceneSkipText>() != null)
                return false;

            return true;
        }
    }
}
