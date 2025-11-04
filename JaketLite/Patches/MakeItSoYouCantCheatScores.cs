using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using Polarite.Multiplayer;

namespace Polarite.Patches
{
    [HarmonyPatch(typeof(GameStateManager))]
    internal class MakeItSoYouCantCheatScores
    {
        [HarmonyPatch(nameof(GameStateManager.CanSubmitScores), MethodType.Getter)]
        [HarmonyPostfix]
        static void NoScore(ref bool __result)
        {
            __result = !NetworkManager.WasUsed;
        }
    }
    [HarmonyPatch(typeof(FinalRank))]
    internal class RankStuff
    {
        [HarmonyPatch(nameof(FinalRank.SetInfo))]
        [HarmonyPostfix]
        static void AddExtras(FinalRank __instance)
        {
            if(NetworkManager.WasUsed)
            {
                __instance.extraInfo.text += $"- <color=#91FFFF>POLARITE USED (WITH {NetworkManager.Instance.CurrentLobby.MemberCount} PLAYERS)</color>\n";
            }
        }
        [HarmonyPatch(nameof(FinalRank.SetRank))]
        [HarmonyPostfix]
        static void ShowRankUseless(FinalRank __instance, ref bool ___majorAssists)
        {
            if (ColorUtility.TryParseHtmlString("#91FFFF", out Color color) && NetworkManager.WasUsed)
            {
                __instance.totalRank.transform.parent.GetComponent<Image>().color = color;
                __instance.totalRank.text = Regex.Replace(__instance.totalRank.text, "<.*?>", "");
            }
        }
    }
}
