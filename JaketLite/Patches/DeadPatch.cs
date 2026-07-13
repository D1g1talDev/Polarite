using HarmonyLib;
using Polarite.Multiplayer;
using Polarite.SamTTS;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Polarite.Patches
{
    public struct DeathMsg
    {
        public string Base;
        public string WhileTag;
        public bool FriendlyFire;

        public bool IsDefault()
        {
            return Base == "died";
        }
        public string Translate(ulong arg, bool whileTag = false)
        {
            string msg = whileTag ? WhileTag : Base;
            if (FriendlyFire) msg += NetworkManager.GetNameOfId(arg, true);
            else if (arg != 0) msg += ((EnemyType)arg).ToString();
            msg = msg.Replace("{0}", "");
            if(FriendlyFire) msg = $"<color=orange>{msg}</color>";
            return msg;
        }
        public DeathMsg(string msg, string whileTag, bool friendlyFire = false)
        {
            Base = msg;
            WhileTag = whileTag;
            FriendlyFire = friendlyFire;
        }
    }

    [HarmonyPatch(typeof(DeathSequence))]
    internal class DeadPatch
    {
        public static DeathMsg[] DeathMessages = new DeathMsg[]
        {
            new DeathMsg("died", ""),
            new DeathMsg("was friendly fired by {0}", "while being friendly fired by {0}", true),
            new DeathMsg("was shot by {0}", "while being shot by {0}"),
            new DeathMsg("was ran over by a tram", "while being ran over by a tram"),
            new DeathMsg("walked into the danger zone", "while walking into the danger zone"), /* : */ new DeathMsg("fell into danger", "while falling into danger"),
            new DeathMsg("exploded", "while being exploded"),
            new DeathMsg("was exploded by {0}", "while being exploded by {0}"),
            new DeathMsg("was exploded by {0}", "while being exploded by {0}", true),
            new DeathMsg("was slain by {0}", "while being slain by {0}"),
            new DeathMsg("was smited", "while being smited"),
            new DeathMsg("killed themselves", "while killing themselves"),
            new DeathMsg("broke the law", "while breaking the law"),
            new DeathMsg("waltzed into the blast of a knuckleblaster from {0}", "while waltzing into the blast of a knuckleblaster from {0}", true),
            new DeathMsg("ran into the explosion of a parried projectile from {0}", "while running into the explosion of a parried projectile from {0}", true)
        };

        public static int DeadPlayers = 0;
        public static List<NetworkPlayer> DeadPs = new List<NetworkPlayer>();

        public static void Death(string str, ulong arg = 0, bool friendlyFire = false) {
            for (int i = 0; i < DeathMessages.Length; i++) {
                if (DeathMessages[i].Base.Contains(str) && DeathMessages[i].FriendlyFire == friendlyFire) {
                    if(lastMessage != (byte)i && LastArg != arg)
                    {
                        WhileResetTimer = 10f;
                        lastMessage = deathMessage;
                        LastArg = Arg;
                    }
                    deathMessage = (byte)i;
                    Arg = arg;
                    return;
                }
            }
        }

        public static DeathMsg DeathMessage => DeathMessages[deathMessage];
        public static byte deathMessage = 0, lastMessage = 0;
        public static ulong Arg = 0, LastArg = 0;
        public static bool SpectateOnDeath;
        public static bool IsDeadInSpectate;
        public static float WhileResetTimer = 10f;

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        static void Postfix(DeathSequence __instance)
        {
            if (!NetworkManager.InLobby)
            {
                __instance.tabl.fullText = "<color=orange>WARNING: EXTREME DAMAGE SUSTAINED.</color>\r\n<color=orange>RUNNING DIAGNOSTIC</color>\r\nERROR: ARM CORE MODULE #1 NOT RESPONDING\r\nERROR: ARM CORE MODULE #2 NOT RESPONDING\r\n<color=orange>WARNING: COMBAT SYSTEMS INOPERABLE</color>\r\n<color=orange>ATTEMPTING RECONSTRUCTION</color>\r\nERROR: SELF-REPAIR NEXUS NOT RESPONDING\r\nINSUFFICIENT BLOOD.\r\nINSUFFICIENT BLOOD.\r\n<color=orange>INITIATING ESCAPE PROTOCOL</color>\r\n<color=orange>ATTEMPTING CONNECTION WITH LIMBIC MODULES</color>\r\nERROR: LEG CORE MODULE #1 NOT RESPONDING\r\nERROR: LEG CORE MODULE #2 NOT RESPONDING\r\n<color=orange>WARNING: UNABLE TO SUSTAIN MOTOR FUNCTIONS</color>\r\nERROR: VISUAL CORTEX MALFUNCTION\r\nERROR: LIMBIC FUNCTION NOT RESPONDING\r\nINSUFFICIENT BLOOD.\r\nINSUFFICIENT BLOOD.\r\n<color=orange>WARNING: UNABLE TO SUSTAIN INTERNAL ORGANS</color>\r\n! PULSE FAILURE !\r\n! PULSE FAILURE !\r\n! PULSE FAILURE !\r\n-!- SHUTDOWN IMMINENT -!-\r\nERROR: NO VOCAL INTERFACE DETECTED, UNABLE TO COMPLETE TASK\r\n! PULSE FAILURE !\r\n! PULSE FAILURE !\r\nINSUFFICIENT BLOOD.\r\nINSUFFICIENT BLOOD.\r\n<color=orange>WARNING: UNABLE TO SUSTAIN BASIC FUNCTIONS</color>\r\n-!- SHUTDOWN IMMINENT -!-\r\n-!- SHUTDOWN IMMINENT -!-\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.";
                return;
            }
            if(ItePlugin.canTTS.value && ItePlugin.ttsHurtAndDeath.value)
            {
                __instance.tabl.fullText = "<color=orange>WARNING: EXTREME DAMAGE SUSTAINED.</color>\r\n<color=orange>RUNNING DIAGNOSTIC</color>\r\nERROR: ARM CORE MODULE #1 NOT RESPONDING\r\nERROR: ARM CORE MODULE #2 NOT RESPONDING\r\n\r\n<color=orange>WARNING: COMBAT SYSTEMS INOPERABLE</color>\r\n<color=orange>FINDING VOCAL INTERFACE...</color>\r\n<color=green>FOUND VOCAL INTERFACE: RUNNING ALERT PROTOCOL</color><color=orange>\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"</color>\r\n-!- SHUTDOWN IMMINENT -!-<color=orange>\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"</color>\r\n-!- SHUTDOWN IMMINENT -!-<color=orange>\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAAAAAAAAA\"\r\n\"AAAAAAAAAAA</color>\r\nERROR: INSUFFICIENT BLOOD. UNABLE TO KEEP RUNNING ALERT PROTOCOL.\r\n-!- SHUTDOWN IMMINENT -!-\r\n-!- SHUTDOWN IMMINENT -!-\r\n-!- SHUTDOWN IMMINENT -!-\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.";
            }
            else
            {
                __instance.tabl.fullText = "<color=orange>WARNING: EXTREME DAMAGE SUSTAINED.</color>\r\n<color=orange>RUNNING DIAGNOSTIC</color>\r\nERROR: ARM CORE MODULE #1 NOT RESPONDING\r\nERROR: ARM CORE MODULE #2 NOT RESPONDING\r\n<color=orange>WARNING: COMBAT SYSTEMS INOPERABLE</color>\r\n<color=orange>ATTEMPTING RECONSTRUCTION</color>\r\nERROR: SELF-REPAIR NEXUS NOT RESPONDING\r\nINSUFFICIENT BLOOD.\r\nINSUFFICIENT BLOOD.\r\n<color=orange>INITIATING ESCAPE PROTOCOL</color>\r\n<color=orange>ATTEMPTING CONNECTION WITH LIMBIC MODULES</color>\r\nERROR: LEG CORE MODULE #1 NOT RESPONDING\r\nERROR: LEG CORE MODULE #2 NOT RESPONDING\r\n<color=orange>WARNING: UNABLE TO SUSTAIN MOTOR FUNCTIONS</color>\r\nERROR: VISUAL CORTEX MALFUNCTION\r\nERROR: LIMBIC FUNCTION NOT RESPONDING\r\nINSUFFICIENT BLOOD.\r\nINSUFFICIENT BLOOD.\r\n<color=orange>WARNING: UNABLE TO SUSTAIN INTERNAL ORGANS</color>\r\n! PULSE FAILURE !\r\n! PULSE FAILURE !\r\n! PULSE FAILURE !\r\n-!- SHUTDOWN IMMINENT -!-\r\nERROR: NO VOCAL INTERFACE DETECTED, UNABLE TO COMPLETE TASK\r\n! PULSE FAILURE !\r\n! PULSE FAILURE !\r\nINSUFFICIENT BLOOD.\r\nINSUFFICIENT BLOOD.\r\n<color=orange>WARNING: UNABLE TO SUSTAIN BASIC FUNCTIONS</color>\r\n-!- SHUTDOWN IMMINENT -!-\r\n-!- SHUTDOWN IMMINENT -!-\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.\r\nI DON'T WANT TO DIE.";
            }
            PacketWriter w = new PacketWriter();
            w.WriteByte(deathMessage);
            w.WriteByte(lastMessage);
            w.WriteULong(Arg);
            w.WriteULong(LastArg);
            w.WriteSam(SamPitch.configSam);
            NetworkManager.Instance.BroadcastPacket(PacketType.Die, w.GetBytes());

            DeathMsg msg = DeathMessages[deathMessage];
            DeathMsg whileMsg = DeathMessages[lastMessage];
            if(!whileMsg.IsDefault() && LastArg != 0 && LastArg != Arg)
            {
                NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(NetworkManager.Id, true) + " " + msg.Translate(Arg) + " " + whileMsg.Translate(LastArg, true));
            }
            else
            {
                NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(NetworkManager.Id, true) + " " + msg.Translate(Arg));
            }
            GameObject blood = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Particles/Blood/BS Head.prefab").WaitForCompletion(), CameraController.Instance.transform.position, Quaternion.identity);
            GameObject ragdoll = GameObject.Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("DeathRagdoll"), CameraController.Instance.transform.position, CameraController.Instance.transform.rotation);
            blood.SetActive(true);
            ragdoll.AddComponent<Ragdoll>().SetValues(ItePlugin.currentSkin, NetworkManager.Id);
            ragdoll.GetComponentInChildren<Rigidbody>().AddForce(MonoSingleton<NewMovement>.Instance.rb.velocity, ForceMode.VelocityChange);
            if(ItePlugin.ttsHurtAndDeath.value && ItePlugin.canTTS.value)
            {
                ItePlugin.DeathScream(SamPitch.configSam);
            }
        }
        public static void TickTimer()
        {
            if(WhileResetTimer > 0f)
            {
                WhileResetTimer -= Time.deltaTime;
            }
            else
            {
                lastMessage = 0;
                LastArg = 0;
                WhileResetTimer = 0f;
            }
        }

        public static void Respawn(Vector3 pos, Quaternion rot, bool ignoreCgCheck = false)
        {
            NewMovement m = MonoSingleton<NewMovement>.Instance;
            if(m.hp > 0)
            {
                return;
            }
            if(SceneHelper.CurrentScene == "Endless" || ignoreCgCheck)
            {
                m.transform.position = pos;
                m.transform.rotation = rot;
            }
            else
            {
                // launch player up incase they fell into a pit
                m.rb.AddForce(Vector3.up * 4f, ForceMode.VelocityChange);
            }
            m.cc.ResetCamera(m.transform.eulerAngles.y + 0.01f);
            m.dead = false;
            m.activated = true;
            m.hp = 1;
            m.Respawn();
        }
    }
}
