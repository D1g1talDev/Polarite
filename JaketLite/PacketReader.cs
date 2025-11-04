using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;

using HarmonyLib;

using Polarite.Patches;

using Steamworks;

using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Polarite.Multiplayer
{
    public static class PacketReader
    {
        public static void ReadPacket(NetPacket packet)
        {
            switch(packet.type)
            {
                case "DamageT":
                    if (bool.Parse(packet.parameters[1]))
                    {
                        MonoSingleton<NewMovement>.Instance.GetHurt(int.Parse(packet.parameters[0]), false);
                    }
                    break;
                case "HealT":
                    if (bool.Parse(packet.parameters[1]))
                    {
                        MonoSingleton<NewMovement>.Instance.GetHealth(int.Parse(packet.parameters[0]), false);
                    }
                    break;
                case "Level":
                    ItePlugin.ignoreSpectate = true;
                    SceneHelper.LoadScene(packet.name);
                    PrefsManager.Instance.SetInt("difficulty", int.Parse(packet.parameters[0]));
                    SceneHelper.SetLoadingSubtext("<color=#91FFFF>/// VIA POLARITE ///");
                    break;
                case "restart":
                    OptionsManager.Instance.RestartMission();
                    break;
                case "kick":
                    NetworkManager.Instance.LeaveLobby();
                    NetworkManager.DisplaySystemChatMessage("You have been kicked from the lobby.");
                    break;
                case "ban":
                    NetworkManager.Instance.LeaveLobby();
                    NetworkManager.DisplaySystemChatMessage("You have been banned from the lobby.");
                    break;
                case "hurt":
                    NetworkPlayer player2 = NetworkPlayer.Find(packet.senderId);
                    if (player2 != null)
                    {
                        player2.HurtNoise();
                    }
                    break;
                case "die":
                    NetworkPlayer player3 = NetworkPlayer.Find(packet.senderId);
                    if (player3 != null)
                    {
                        player3.DeathNoise();
                    }
                    NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(packet.senderId) + " " + packet.name);
                    break;
                case "respawn":
                    NetworkPlayer player4 = NetworkPlayer.Find(packet.senderId);
                    if (player4 != null)
                    {
                        player4.SpawnNoise();
                    }
                    break;
                case "jump":
                    NetworkPlayer player5 = NetworkPlayer.Find(packet.senderId);
                    if (player5 != null)
                    {
                        player5.JumpNoise();
                    }
                    break;
                case "dash":
                    NetworkPlayer player6 = NetworkPlayer.Find(packet.senderId);
                    if (player6 != null)
                    {
                        player6.DashNoise();
                    }
                    break;
                case "gun":
                    NetworkPlayer player7 = NetworkPlayer.Find(packet.senderId);
                    if (player7 != null)
                    {
                        player7.SetWeapon(int.Parse(packet.name));
                    }
                    break;
                case "punch":
                    NetworkPlayer player8 = NetworkPlayer.Find(packet.senderId);
                    if (player8 != null)
                    {
                        player8.PunchAnim();
                    }
                    break;
                case "coin":
                    NetworkPlayer player9 = NetworkPlayer.Find(packet.senderId);
                    if (player9 != null)
                    {
                        player9.CoinAnim();
                    }
                    break;
                case "whip":
                    NetworkPlayer player10 = NetworkPlayer.Find(packet.senderId);
                    if (player10 != null)
                    {
                        player10.WhipAnim();
                    }
                    break;

                /*
                case "item":
                    NetworkPlayer player12 = NetworkPlayer.Find(packet.senderId);
                    if (player12 != null)
                    {
                        player12.Pickup((ItemType)Enum.Parse(typeof(ItemType), packet.name, true));
                    }
                    break;
                case "drop":
                    NetworkPlayer player13 = NetworkPlayer.Find(packet.senderId);
                    if (player13 != null)
                    {
                        player13.Drop();
                    }
                    break;
                */
                case "skin":
                    NetworkPlayer player14 = NetworkPlayer.Find(packet.senderId);
                    if (player14 != null)
                    {
                        player14.UpdateSkin(int.Parse(packet.name));
                    }
                    break;
                // rig
                case "transform":
                    float x = float.Parse(packet.parameters[0]);
                    float y = float.Parse(packet.parameters[1]);
                    float z = float.Parse(packet.parameters[2]);

                    float rotX = float.Parse(packet.parameters[3]);
                    float rotY = float.Parse(packet.parameters[4]);
                    float rotZ = float.Parse(packet.parameters[5]);
                    float rotW = float.Parse(packet.parameters[6]);

                    bool sliding = bool.Parse(packet.parameters[7]);
                    bool air = bool.Parse(packet.parameters[8]);
                    bool walking = bool.Parse(packet.parameters[9]);

                    int hp = int.Parse(packet.parameters[10]);

                    NetworkPlayer player15 = NetworkPlayer.Find(packet.senderId);
                    if(player15 != null)
                    {
                        player15.SetTargetTransform(new Vector3(x, y, z), new Quaternion(rotX, rotY, rotZ, rotW));
                        player15.SetAnimation(sliding, air, walking);
                        player15.SetHP(hp);
                    }
                    break;
                // chat
                case "chatmsg":
                    ChatUI.Instance.OnSubmitMessage((NetworkManager.Instance.CurrentLobby.Owner.Id == packet.senderId) ? $"<color=orange>{NetworkManager.GetNameOfId(packet.senderId)}</color>: {packet.name}" : $"<color=grey>{NetworkManager.GetNameOfId(packet.senderId)}</color>: {packet.name}", false, packet.name, NetworkPlayer.Find(packet.senderId).transform);
                    ChatUI.Instance.ShowUIForBit();
                    break;
                // enemy
                case "enemystate":
                    NetworkEnemy netE = NetworkEnemy.Find(packet.name);
                    if(netE != null)
                    {
                       netE.ApplyState(packet.parameters);
                       netE.HP = float.Parse(packet.parameters[7]);
                    }
                    break;
                case "enemydmg":
                    NetworkEnemy netE1 = NetworkEnemy.Find(packet.name);
                    if (netE1 != null)
                    {
                        netE1.ApplyDamage(packet.parameters);
                    }
                    break;
                case "deathenemy":
                    NetworkEnemy netE2 = NetworkEnemy.Find(packet.name);
                    if (netE2 != null)
                    {
                        netE2.HandleDeath();
                    }
                    break;
                case "ownership":
                    NetworkEnemy netE3 = NetworkEnemy.Find(packet.name);
                    if (netE3 != null)
                    {
                        netE3.TakeOwnerP2P(ulong.Parse(packet.parameters[0]));
                    }
                    break;
                case "arena":
                    GameObject go = SceneObjectCache.Find(packet.name);
                    if(go != null)
                    {
                        ActivateArena arena = go.GetComponent<ActivateArena>();
                        if(arena != null)
                        {
                            arena.Activate();
                        }
                    }
                    break;
                case "forcespec":
                    NetworkManager.DisplaySystemChatMessage($"{NetworkManager.GetNameOfId(packet.senderId)} has been forced into spectate. (Reason: Host already opened level door, wait for next level)");
                    break;

                case "enemySpawn":
                    GameObject enemy = SceneObjectCache.Find(packet.name);
                    if (enemy != null)
                    {
                        enemy.GetComponent<NetworkEnemySync>().here = true;
                        enemy.SetActive(true);
                    }
                    break;

                /*
                case "tramup":
                    TramControl tram = SceneObjectCache.Find(packet.name).GetComponent<TramControl>();
                    if (tram != null)
                    {
                        tram.SpeedUp(1);
                        TramPatch.tramsToDeaths[tram].damage = TramPatch.GetDamageForSpeedLevel(tram);
                    }
                    break;
                case "tramdown":
                    TramControl tram2 = SceneObjectCache.Find(packet.name).GetComponent<TramControl>();
                    if (tram2 != null)
                    {
                        tram2.SpeedDown(1);
                        TramPatch.tramsToDeaths[tram2].damage = TramPatch.GetDamageForSpeedLevel(tram2);
                    }
                    break;
                case "tramzap":
                    TramControl tram3 = SceneObjectCache.Find(packet.name).GetComponent<TramControl>();
                    if (tram3 != null)
                    {
                        tram3.Zap();
                        TramPatch.tramsToDeaths[tram3].damage = TramPatch.GetDamageForSpeedLevel(tram3);
                    }
                    break;
                */

                case "finalopen":
                    FinalDoor door = SceneObjectCache.Find(packet.name).GetComponent<FinalDoor>();
                    if(door != null && !door.aboutToOpen)
                    {
                        door.aboutToOpen = true;
                        door.Open();
                        door.Invoke("OpenDoors", 1f);
                    }
                    break;

                // breakables
                case "break":
                    Breakable breakable = SceneObjectCache.Find(packet.name).GetComponent<Breakable>();
                    if (breakable != null)
                    {
                        breakable.ForceBreak();
                    }
                    break;
                // hook point
                case "hookS":
                    HookPoint hookPoint = SceneObjectCache.Find(packet.name).GetComponent<HookPoint>();
                    if (hookPoint != null && hookPoint.timer <= 0f)
                    {
                        hookPoint.timer = hookPoint.reactivationTime;
                        hookPoint.Reached();
                        hookPoint.SwitchPulled();
                    }
                    break;

                // checkpoint & cheater
                case "checkpoint":
                    CheckPoint checkpoint = SceneObjectCache.Find(packet.name).GetComponent<CheckPoint>();
                    if (checkpoint != null && !checkpoint.activated)
                    {
                        checkpoint.activated = true;
                        checkpoint.ActivateCheckPoint();
                        NetworkManager.ShoutCheckpoint(NetworkManager.GetNameOfId(packet.senderId));
                    }
                    break;
                case "cheater":
                    NetworkManager.ShoutCheater(packet.name);
                    break;
                // networking
                case "join":
                    NetworkManager.Instance.HandleMemberJoinedP2P(new Friend(ulong.Parse(packet.name)));
                    break;
                case "left":
                    NetworkManager.Instance.HandleMemberLeftP2P(new Friend(ulong.Parse(packet.name)));
                    break;
                case "hostleave":
                    NetworkManager.Instance.LeaveLobby();
                    break;

            }
        }
    }
}
