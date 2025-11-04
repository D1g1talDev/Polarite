//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using HarmonyLib;
//using Polarite.Multiplayer;

//using UnityEngine;

//namespace Polarite.Patches
//{
//    [HarmonyPatch(typeof(TramControl))]
//    internal class TramPatch
//    {
//        public static Dictionary<TramControl, DeathZone> tramsToDeaths = new Dictionary<TramControl, DeathZone>();

//        [HarmonyPatch("Awake")]
//        [HarmonyPostfix]
//        static void PostfixAwake(TramControl __instance)
//        {
//            if(!SceneObjectCache.Contains(__instance.gameObject))
//            {
//                SceneObjectCache.Add(__instance.gameObject);
//            }
//            if(NetworkManager.InLobby && SceneHelper.CurrentScene != "Level 7-1")
//            {
//                // make tram deadly
//                __instance.transform.parent.Find("TramKillZones").gameObject.SetActive(true);
//                DeathZone death = __instance.transform.parent.Find("TramKillZones").GetComponentInChildren<DeathZone>(true);
//                death.gameObject.SetActive(true);
//                death.damage = 0;
//                death.notInstakill = true;
//                death.affected = AffectedSubjects.All;
//                death.deathType = "tram";
//                death.dontChangeRespawnTarget = false;
//                Light light = death.gameObject.AddComponent<Light>();
//                light.type = LightType.Spot;
//                light.intensity = 4.5f;
//                light.range = 300f;
//                light.spotAngle = 100f;

//                tramsToDeaths.Add(__instance, death);
//                /*
//                if(__instance.transform.parent.GetComponent<NetworkTram>() == null && SceneHelper.CurrentScene != "Level 2-4")
//                {
//                    NetworkTram.Create(SceneObjectCache.GetScenePath(__instance.transform.parent.gameObject), __instance.transform.parent.gameObject);
//                }
//                */
//            }
//        }

//        [HarmonyPatch(nameof(TramControl.SpeedUp), new Type[] {  })]
//        [HarmonyPostfix]
//        static void Postfix(TramControl __instance)
//        {
//            if (NetworkManager.InLobby && SceneHelper.CurrentScene != "Level 7-1")
//            {
//                NetworkManager.Instance.BroadcastPacket(new NetPacket
//                {
//                    type = "tramup",
//                    name = SceneObjectCache.GetScenePath(__instance.gameObject),
//                });
//                tramsToDeaths[__instance].damage = GetDamageForSpeedLevel(__instance);
//            }
//        }
//        [HarmonyPatch(nameof(TramControl.SpeedDown), new Type[] { })]
//        [HarmonyPostfix]
//        static void Postfix2(TramControl __instance)
//        {
//            if (NetworkManager.InLobby && SceneHelper.CurrentScene != "Level 7-1")
//            {
//                NetworkManager.Instance.BroadcastPacket(new NetPacket
//                {
//                    type = "tramdown",
//                    name = SceneObjectCache.GetScenePath(__instance.gameObject),
//                });
//                tramsToDeaths[__instance].damage = GetDamageForSpeedLevel(__instance);
//            }
//        }
//        [HarmonyPatch(nameof(TramControl.Zap))]
//        [HarmonyPostfix]
//        static void Prefix(TramControl __instance)
//        {
//            if (NetworkManager.InLobby && __instance.zapAmount < 3f && SceneHelper.CurrentScene != "Level 7-1")
//            {
//                NetworkManager.Instance.BroadcastPacket(new NetPacket
//                {
//                    type = "tramzap",
//                    name = SceneObjectCache.GetScenePath(__instance.gameObject),
//                });
//                tramsToDeaths[__instance].damage = GetDamageForSpeedLevel(__instance);
//            }
//        }
//        [HarmonyPatch("FixedUpdate")]
//        [HarmonyPrefix]
//        static bool MakeSureTramAlwaysRuns(TramControl __instance)
//        {
//            DeathZone zone = tramsToDeaths[__instance];
//            zone.respawnTarget = MonoSingleton<NewMovement>.Instance.transform.position;
//            zone.notInstakill = MonoSingleton<NewMovement>.Instance.hp >= zone.damage;
//            return !NetworkManager.InLobby;
//        }

//        public static int GetDamageForSpeedLevel(TramControl tram)
//        {
//            if(tram.zapAmount > 0)
//            {
//                return 200;
//            }
//            switch(tram.currentSpeedStep)
//            {
//                case 0: return 0;
//                case 1: return 25;
//                case 2: return 50;
//                case 3: return 70;
//                case 4: return 100;
//                case 5: return 150;
//                case 6: return 200;
//                default: return 100;
//            }
//        }
//    }
//}
