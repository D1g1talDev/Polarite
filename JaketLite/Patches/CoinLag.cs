using HarmonyLib;
using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(CoinActivated))]
    internal class CoinLag
    {
        [HarmonyPatch(nameof(CoinActivated.OnTriggerEnter))]
        [HarmonyPrefix]
        static bool StopTrigger(CoinActivated __instance, ref Collider other)
        {
            if (!NetworkManager.InLobby || SceneHelper.CurrentScene == "Level 1-1")
            {
                return true;
            }
            if(other.GetComponent<Coin>() != null)
            {
                GameObject.Instantiate(DefaultReferenceManager.Instance.superExplosion, __instance.transform.position, Quaternion.identity);
                __instance.transform.parent.gameObject.SetActive(false);
            }
            return !NetworkManager.InLobby;
        }
    }
}
