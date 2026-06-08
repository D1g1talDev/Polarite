using HarmonyLib;
using Polarite.Multiplayer;
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
        public string Whilst;
        public bool FriendlyFire;

        public bool IsDefault()
        {
            return Base == "died";
        }
        public string Translate(ulong arg, bool whilst = false)
        {
            string msg = whilst ? Whilst : Base;
            if (FriendlyFire) msg += NetworkManager.GetNameOfId(arg, true);
            else if (arg != 0) msg += ((EnemyType)arg).ToString();
            msg = msg.Replace("{0}", "");
            if(FriendlyFire) msg = $"<color=orange>{msg}</color>";
            return msg;
        }
        public DeathMsg(string msg, string whilst, bool friendlyFire = false)
        {
            Base = msg;
            Whilst = whilst;
            FriendlyFire = friendlyFire;
        }
    }

    [HarmonyPatch(typeof(DeathSequence))]
    internal class DeadPatch
    {
        public static DeathMsg[] DeathMessages = new DeathMsg[]
        {
            new DeathMsg("died", ""),
            new DeathMsg("was friendly fired by {0}", "whilst being friendly fired by {0}", true),
            new DeathMsg("was shot by {0}", "whilst being shot by {0}"),
            new DeathMsg("was ran over by a tram", "whilst being ran over by a tram"),
            new DeathMsg("walked into the danger zone", "whilst walking into the danger zone"), /* : */ new DeathMsg("fell into danger", "whilst falling into danger"),
            new DeathMsg("exploded", "whilst being exploded"),
            new DeathMsg("was exploded by {0}", "whilst being exploded by {0}"),
            new DeathMsg("was exploded by {0}", "whilst being exploded by {0}", true),
            new DeathMsg("was slain by {0}", "whilst being slain by {0}"),
            new DeathMsg("was smited", "whilst being smited"),
            new DeathMsg("killed themselves", "whilst killing themselves"),
            new DeathMsg("broke the law", "whilst breaking the law"),
            new DeathMsg("waltzed into the blast of a knuckleblaster from {0}", "whilst waltzing into the blast of a knuckleblaster from {0}", true),
            new DeathMsg("ran into the explosion of a parried projectile from {0}", "whilst running into the explosion of a parried projectile from {0}", true)
        };

        public static int DeadPlayers = 0;
        public static List<NetworkPlayer> DeadPs = new List<NetworkPlayer>();

        public static void Death(string str, ulong arg = 0, bool friendlyFire = false) {
            for (int i = 0; i < DeathMessages.Length; i++) {
                if (DeathMessages[i].Base.Contains(str) && DeathMessages[i].FriendlyFire == friendlyFire) {
                    if(lastMessage != (byte)i && LastArg != arg)
                    {
                        WhilstResetTimer = 10f;
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
        public static float WhilstResetTimer = 10f;

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        static void Postfix(DeathSequence __instance)
        {
            if (!NetworkManager.InLobby)
                return;

            
            PacketWriter w = new PacketWriter();
            w.WriteByte(deathMessage);
            w.WriteByte(lastMessage);
            w.WriteULong(Arg);
            w.WriteULong(LastArg);
            NetworkManager.Instance.BroadcastPacket(PacketType.Die, w.GetBytes());

            DeathMsg msg = DeathMessages[deathMessage];
            DeathMsg whilstMsg = DeathMessages[lastMessage];
            if(!whilstMsg.IsDefault() && LastArg != 0 && LastArg != Arg)
            {
                NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(NetworkManager.Id, true) + " " + msg.Translate(Arg) + " " + whilstMsg.Translate(LastArg, true));
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
        }
        public static void TickTimer()
        {
            if(WhilstResetTimer > 0f)
            {
                WhilstResetTimer -= Time.deltaTime;
            }
            else
            {
                lastMessage = 0;
                LastArg = 0;
                WhilstResetTimer = 0f;
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
