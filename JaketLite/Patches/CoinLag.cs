using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(CoinActivated))]
    internal class CoinLag
    {
        [HarmonyPatch(nameof(CoinActivated.OnTriggerEnter))]
        [HarmonyPrefix]
        static bool StopTrigger()
        {
            if (!NetworkManager.InLobby || SceneHelper.CurrentScene == "Level 1-1")
            {
                return true;
            }
            return !NetworkManager.InLobby;
        }
    }
}
