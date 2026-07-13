using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(OnCameraVisible))]
    internal class CamVisiblePatch
    {
        [HarmonyPatch(nameof(OnCameraVisible.Update))]
        [HarmonyPrefix]
        static bool Prefix()
        {
            return !NetworkManager.InLobby;
        }
    }
}
