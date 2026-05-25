using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GameProgressSaver))]
    internal class SaveFilePatches
    {
        [HarmonyPatch(nameof(GameProgressSaver.SaveRank))]
        [HarmonyPrefix]
        static bool NoRank()
        {
            return !NetworkManager.WasUsed;
        }
        [HarmonyPatch(nameof(GameProgressSaver.SetPrime))]
        [HarmonyPrefix]
        static bool NoPrime()
        {
            return !NetworkManager.WasUsed;
        }
        [HarmonyPatch(nameof(GameProgressSaver.SetBestCyber))]
        [HarmonyPrefix]
        static bool NoCyber()
        {
            return !NetworkManager.WasUsed;
        }
        [HarmonyPatch(nameof(GameProgressSaver.SetSecretMission))]
        [HarmonyPrefix]
        static bool NoSecret()
        {
            return !NetworkManager.WasUsed;
        }
        [HarmonyPatch(nameof(GameProgressSaver.SetEncoreProgress))]
        [HarmonyPrefix]
        static bool NoEncore()
        {
            return !NetworkManager.WasUsed;
        }
    }
}
