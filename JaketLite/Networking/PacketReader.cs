using Polarite.Debugging;
using Polarite.Networking;
using Polarite.Networking.Skins;
using Polarite.Patches;
using Polarite.SamTTS;
using Steamworks;
using System;
using System.Net.Http;
using System.Net.Sockets;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.AI;
using static SceneHelper;
using static Unity.Burst.Intrinsics.Arm;
using Random = UnityEngine.Random;

namespace Polarite.Multiplayer
{
    public enum PacketType : byte
    {
        None = 0,

        // unused
        DamageT = 1,
        HealT = 2,

        // level flow
        Level = 3,
        Restart = 4,
        Kick = 5,
        Ban = 6,

        // player actions / noises
        Hurt = 7,
        Die = 8,
        Respawn = 9,
        Jump = 10,
        Dash = 11,

        // weapons & cosmetics
        Gun = 12,
        Punch = 13,
        Coin = 14,
        Whip = 15,
        Skin = 16,

        // transform + animation + hp
        Transform = 17,

        // chat
        ChatMsg = 18,

        // objects + enemies
        ObjectState = 19,
        EnemyDmg = 20,
        DeathEnemy = 21,
        Ownership = 22,
        EnemySpawn = 23,

        // level objects
        Arena = 24,
        FinalOpen = 25,
        Break = 26,

        // other
        HookS = 27,
        Checkpoint = 28,
        Cheater = 29,

        // connection events
        Join = 30,
        Left = 31,
        HostLeave = 32,

        // cutscene skip voting
        SkipVoteRequest = 33,
        SkipVoteUpdate = 34,
        SkipExecute = 35,

        // cybergrind
        PVP = 36,
        CyberPattern = 37,
        CyberGameOver = 38,
        BecameGhost = 39,
        ReviveGhost = 40,
        ObjectRemoved = 41,
        // bullet sync
        Bullet = 42,
        Blast = 43,
        ProjExplode = 44,
        // rocket
        RocketFreeze = 45,
        // trigger
        Trigger = 46,
        // elevation
        Elevator = 47,
        // suicide trees
        SuicideFill = 48,
        // deathcatchers
        DeathcatchRespawn = 49,
        GlobalConnectionJoin = 50,
        GlobalConnectionLeave = 51,
        // enemy state
        EnemyState = 52,
        // weapon sounds
        PunchNormal = 53,
        PunchHeavy = 54,
        PunchParry = 55,
        // cybergrind progress
        CyberProgress = 56,
        // level start
        LevelStart = 57,
        // more player animations
        ShopTap = 58,
        // 💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀☠️💀
        SkullState = 59,
        // flammables
        Flammable = 60,
        // changing lobby settings
        LobbySettings = 61,
        // voice chat
        Voice = 62,
        // level finish
        LevelFinished = 63,
        // gravity
        GravVol = 64,
        // player details
        Slam = 65,
        SlamShockwave = 66,
        Footstep = 67,
        SlideScrape = 68,
        WallScrape = 69,
        DetachSlideScrape = 70,
        DetachWallScrape = 71,
        StopSlide = 72
    }

    public static class PacketReader
    {
        public static void Handle(PacketType type, byte[] data, int len, ulong senderId)
        {
            BinaryPacketReader reader = new BinaryPacketReader(data, len);
            switch (type)
            {
                case PacketType.Level:
                    {
                        string scene = reader.ReadString();
                        int diff = reader.ReadInt();

                        ItePlugin.ignoreSpectate = true;
                        SceneHelper.LoadScene(scene);
                        PrefsManager.Instance.SetInt("difficulty", diff);
                        SceneHelper.SetLoadingSubtext("<color=#91FFFF>+++ VIA POLARITE ---");
                        break;
                    }

                case PacketType.Restart:
                    ItePlugin.cameFromPacketRestart = true;
                    OptionsManager.Instance.RestartMission();
                    break;

                case PacketType.Kick:
                    NetworkManager.Instance.LeaveLobby(true);
                    NetworkManager.DisplaySystemChatMessage("You have been kicked from the lobby.");
                    break;

                case PacketType.Ban:
                    NetworkManager.Instance.LeaveLobby(true);
                    NetworkManager.DisplaySystemChatMessage("You have been banned from the lobby.");
                    break;

                case PacketType.Hurt:
                    {
                        Sam sam = reader.ReadSam();
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                        {
                            p.HurtNoise();
                            if (ItePlugin.ttsHurtAndDeath.value && ItePlugin.canTTS.value && !p.isGhost)
                            {
                                SamPitch.Set(sam);
                                TextReader.SayString(HurtPatch.HurtNoises[Random.Range(0, HurtPatch.HurtNoises.Length)], p.head.transform);
                                SamPitch.Reset();
                            }
                        }
                        break;
                    }

                case PacketType.Die:
                    {
                        byte msgB = reader.ReadByte();
                        byte wMsgB = reader.ReadByte();
                        ulong arg = reader.ReadULong();
                        ulong wArg = reader.ReadULong();
                        Sam sam = reader.ReadSam();

                        DeathMsg msg = DeadPatch.DeathMessages[msgB];
                        DeathMsg whileMsg = DeadPatch.DeathMessages[wMsgB];

                        NetworkPlayer plr = NetworkPlayer.Find(senderId);
                        if (plr == null)
                        {
                            return;
                        }
                        plr.DeathNoise();
                        if (!whileMsg.IsDefault() && wArg != 0 && wArg != arg)
                        {
                            NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(senderId, true) + " " + msg.Translate(arg) + " " + whileMsg.Translate(wArg, true));
                        }
                        else
                        {
                            NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(senderId, true) + " " + msg.Translate(arg));
                        }
                        if (ItePlugin.ttsHurtAndDeath.value && ItePlugin.canTTS.value)
                        {
                            ItePlugin.DeathScream(sam, plr.transform);
                        }
                        break;
                    }

                case PacketType.Respawn:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.SpawnNoise();
                        break;
                    }

                case PacketType.Jump:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        bool quake = reader.ReadBool();
                        if (p != null)
                        {
                            p.JumpNoise();
                            if (quake)
                                p.SpawnSound(MonoSingleton<NewMovement>.Instance.quakeJumpSound.GetComponent<AudioSource>().clip);
                        }
                        break;
                    }

                case PacketType.Dash:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        Vector3 inputDir = reader.ReadVector3();
                        Vector3 pos = reader.ReadVector3();
                        if (p != null)
                        {
                            ItePlugin.SpawnSound(p.dashNoise.clip, 1f, null, 1f, pos);
                            Vector3 dir = ((inputDir == Vector3.zero) ? p.transform.forward : inputDir);
                            GameObject.Instantiate(MonoSingleton<NewMovement>.Instance.dodgeParticle, pos + dir * 10f, Quaternion.LookRotation(dir * -1f));
                        }
                        break;
                    }

                case PacketType.Gun:
                    {
                        int weapon = reader.ReadInt();
                        bool alt = reader.ReadBool();
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.SetWeapon(alt, weapon);
                        break;
                    }

                case PacketType.Punch:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.PunchAnim();
                        break;
                    }

                case PacketType.Coin:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.CoinAnim();
                        break;
                    }

                case PacketType.Whip:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.WhipAnim();
                        break;
                    }

                case PacketType.Skin:
                    {
                        Skin skin = reader.ReadSkin();
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.UpdateSkin(skin);
                        break;
                    }

                case PacketType.Transform:
                    {
                        Vector3 pos = reader.ReadVector3();
                        Quaternion rot = reader.ReadQuaternion();
                        Quaternion rot2 = reader.ReadQuaternion();
                        bool sliding = reader.ReadBool();
                        bool air = reader.ReadBool();
                        bool walking = reader.ReadBool();
                        bool spin = reader.ReadBool();
                        bool shop = reader.ReadBool();
                        bool fallPart = reader.ReadBool();
                        int hp = reader.ReadInt();
                        bool typing = reader.ReadBool();
                        Vector3 dodgeDir = reader.ReadVector3();
                        Vector3 rbVel = reader.ReadVector3();
                        bool wall = reader.ReadBool();

                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                        {
                            p.SetTargetTransform(pos, rot, rot2);
                            p.SetAnimation(sliding, air, walking, spin);
                            p.SetHP(hp);
                            p.typing = typing;
                            p.ShopMode(shop);
                            p.SetFalling(fallPart);
                            p.SlidePart(sliding, dodgeDir);
                            p.rbVel = rbVel;
                            p.dodge = dodgeDir;
                            p.wall = wall;
                        }
                        // double check rig state to prevent weird edge cases where a player gets stuck invisible after dying
                        if (hp > 0 && !p.rigActive)
                        {
                            p.ToggleRig(true);
                        }
                        if (hp <= 0 && p.rigActive)
                        {
                            p.ToggleRig(false);
                        }
                        break;
                    }

                case PacketType.ChatMsg:
                    {
                        string text = reader.ReadString();
                        Sam sam = reader.ReadSam();
                        var p = NetworkPlayer.Find(senderId);
                        string name = NetworkManager.GetNameOfId(senderId, true);
                        bool tts;

                        string format;
                        if (Net.Dev(senderId))
                        {
                            format = $"<color=green>[DEV] {name}</color>: {text}";
                            tts = true;
                        }
                        else
                        {
                            format = (NetworkManager.Instance.CurrentLobby.Owner.Id == senderId)
                                ? $"<color=#00F2FF>{name}</color>: {text}"
                                : $"<color=grey>{name}</color>: {text}";
                            tts = true;
                        }
                        /* disabling for now
                        if (Voice.mutedPlayers.Contains(senderId))
                        {
                            format = $"<i><color=grey>{NetworkManager.GetNameOfId(senderId)} is muted in your player list.</color></i>";
                            text = "mutedlol";
                            tts = false;
                        }
                        */

                        ChatUI.Instance.OnSubmitMessage(format, false, text, p.transform, tts, sam: sam);
                        ChatUI.Instance.ShowUIForBit();
                        if(ItePlugin.chatNoise.value)
                        {
                            ItePlugin.SpawnSound(ItePlugin.message, 1f, p.transform, 1f);
                        }
                        break;
                    }

                case PacketType.ObjectState:
                    {
                        string id = reader.ReadString();
                        Vector3 pos = reader.ReadVector3();
                        Quaternion rot = reader.ReadQuaternion();

                        if (Net.TryGet(id, senderId, pos, out var e))
                            e.State(pos, rot, reader);
                        break;
                    }
                case PacketType.EnemyState:
                    {
                        bool hBuff = reader.ReadBool();
                        bool sBuff = reader.ReadBool();
                        bool dBuff = reader.ReadBool();

                        float hMod = reader.ReadFloat();
                        float sMod = reader.ReadFloat();
                        float dMod = reader.ReadFloat();

                        bool sand = reader.ReadBool();
                        bool blood = reader.ReadBool();

                        bool idol = reader.ReadBool();
                        bool attackEids = reader.ReadBool();

                        ulong owner = reader.ReadULong();
                        Vector3 vel = reader.ReadVector3();

                        string id = reader.ReadString();
                        Vector3 pos = reader.ReadVector3();
                        Quaternion rot = reader.ReadQuaternion();

                        if (Net.TryGet(id, owner, pos, out var e))
                        {
                            e.Owner = owner;
                            if (e.Base.TryGetComponent<EnemyIdentifier>(out var eid))
                            {
                                NetworkEnemy netE = e as NetworkEnemy;
                                eid.TryGetComponent<NavMeshAgent>(out var nma);
                                eid.healthBuff = hBuff;
                                eid.speedBuff = sBuff;
                                eid.damageBuff = dBuff;

                                eid.healthBuffModifier = hMod;
                                eid.speedBuffModifier = sMod;
                                eid.damageBuffModifier = dMod;

                                if (sand) eid.Sandify(); else eid.sandified = false;
                                if (blood && !eid.puppet) eid.PuppetSpawn();

                                eid.blessed = idol;
                                eid.attackEnemies = attackEids;
                                netE.SetAgentVelocity(vel);
                                e.State(pos, rot, reader);
                            }
                        }
                        break;
                    }

                case PacketType.EnemyDmg:
                    {
                        string id = reader.ReadString();
                        Vector3 pos = reader.ReadVector3();
                        if (Net.TryGet(id, senderId, pos, out var e, true))
                            e.Respond(reader, PacketType.EnemyDmg, senderId);
                        break;
                    }

                case PacketType.DeathEnemy:
                    {
                        string id = reader.ReadString();
                        Vector3 pos = reader.ReadVector3();
                        if (Net.TryGet(id, senderId, pos, out var e, true))
                            e.Respond(reader, PacketType.DeathEnemy, senderId);
                        break;
                    }

                case PacketType.Ownership:
                    {
                        string id = reader.ReadString();
                        ulong newOwner = reader.ReadULong();
                        Vector3 pos = reader.ReadVector3();
                        if (Net.TryGet(id, senderId, pos, out var e))
                            e.TransferOwnerP2P(newOwner);
                        break;
                    }

                case PacketType.EnemySpawn:
                    {
                        ulong owner = reader.ReadULong();
                        string name = reader.ReadString();
                        string path = reader.ReadString();
                        string fallback = reader.ReadString();
                        Vector3 pos = reader.ReadVector3();
                        Quaternion rot = reader.ReadQuaternion();
                        bool hasHp = reader.ReadBool();
                        bool hasSpeed = reader.ReadBool();
                        bool hasDamage = reader.ReadBool();
                        bool bloodPup = reader.ReadBool();
                        float hpMod = reader.ReadFloat();
                        float speedMod = reader.ReadFloat();
                        float damageMod = reader.ReadFloat();
                        bool boss = reader.ReadBool();

                        if (owner == NetworkManager.Id)
                        {
                            return;
                        }
                        if(!Net.TryGet(path, owner, pos, out var obj))
                        {
                            return;
                        }
                        if (!obj.Base.TryGetComponent<EnemyIdentifier>(out var eid))
                        {
                            return;
                        }
                        eid.name = name;
                        eid.isBoss = boss;
                        if (boss && eid.GetComponent<BossHealthBar>() == null)
                        {
                            BossHealthBar bhb = eid.gameObject.AddComponent<BossHealthBar>();
                            bhb.bossName = eid.FullName;
                        }

                        if (hasHp) eid.HealthBuff(hpMod);
                        if (hasSpeed) eid.SpeedBuff(speedMod);
                        if (hasDamage) eid.DamageBuff(damageMod);
                        if (bloodPup) eid.PuppetSpawn();
                        break;
                    }

                case PacketType.Arena:
                    {
                        string path = reader.ReadString();
                        GameObject go = SceneObjectCache.Find(path);
                        if (go)
                        {
                            var arena = go.GetComponent<ActivateArena>();
                            if (arena)
                                arena.Activate();
                        }
                        break;
                    }

                case PacketType.FinalOpen:
                    {
                        string path = reader.ReadString();
                        var door = SceneObjectCache.Find(path).GetComponent<FinalDoor>();
                        if (door && !door.aboutToOpen && door.isActiveAndEnabled)
                        {
                            door.aboutToOpen = true;
                            door.Open();
                            if (!OnLevelStart.Instance.activated)
                            {
                                OnLevelStart.Instance.Invoke("StartLevel", 1f);
                            }
                        }
                        break;
                    }

                case PacketType.Break:
                    {
                        string path = reader.ReadString();
                        var b = SceneObjectCache.Find(path).GetComponent<Breakable>();
                        if (b)
                            b.ForceBreak();
                        break;
                    }

                case PacketType.HookS:
                    {
                        string path = reader.ReadString();
                        var h = SceneObjectCache.Find(path).GetComponent<HookPoint>();
                        if (h && h.timer <= 0f)
                        {
                            h.timer = h.reactivationTime;
                            h.Reached();
                            h.SwitchPulled();
                        }
                        break;
                    }

                case PacketType.Checkpoint:
                    {
                        if(ItePlugin.disableCheckpointSync.value)
                        {
                            break;
                        }
                        string path = reader.ReadString();
                        var checkpoint = SceneObjectCache.Find(path).GetComponent<CheckPoint>();
                        if (checkpoint && !checkpoint.activated)
                        {
                            checkpoint.activated = true;
                            checkpoint.ActivateCheckPoint();
                            NetworkManager.ShoutCheckpoint(NetworkManager.GetNameOfId(senderId, true));
                            if (NetworkPlayer.selfIsGhost)
                            {
                                ItePlugin.Ghost(false);
                                NetworkPlayer sender = NetworkPlayer.Find(senderId);
                                DeadPatch.Respawn(sender.transform.position + Vector3.up * 1.25f, sender.transform.rotation, true);
                            }
                        }
                        break;
                    }

                case PacketType.Cheater:
                    {
                        string who = reader.ReadString();
                        NetworkManager.ShoutCheater(who);
                        break;
                    }

                case PacketType.Join:
                    {
                        ulong who = reader.ReadULong();
                        NetworkManager.Instance.HandleMemberJoinedNet(new Friend(who));
                        break;
                    }

                case PacketType.Left:
                    {
                        ulong who = reader.ReadULong();
                        NetworkManager.Instance.HandleMemberLeftNet(new Friend(who));
                        break;
                    }

                case PacketType.HostLeave:
                    NetworkManager.Instance.LeaveLobby(true);
                    break;
                /*
            case PacketType.SkipVoteRequest:
                {
                    bool accept = reader.ReadBool();
                    if (NetworkManager.HostAndConnected)
                    {
                        Polarite.Patches.SkipVotePatch.SkipVoteManager.HandleClientVote(senderId, accept);
                    }
                    break;
                }

            case PacketType.SkipVoteUpdate:
                {
                    int acceptCount = reader.ReadInt();
                    int needed = reader.ReadInt();
                    int total = reader.ReadInt();
                    Polarite.Patches.SkipVotePatch.SkipVoteManager.ApplyUpdateClientSide(acceptCount, needed, total);
                    break;
                }

            case PacketType.SkipExecute:
                {
                    Polarite.Patches.SkipVotePatch.SkipVoteManager.ExecuteSkipLocal();
                    break;
                }
                */
                case PacketType.PVP:
                    {
                        ulong who = reader.ReadULong();
                        int dmg = reader.ReadInt();
                        // NetworkPlayer.DoFriendlyDamage(who, dmg);
                        break;
                    }
                case PacketType.CyberPattern:
                    {
                        int wave = reader.ReadInt();
                        string h = reader.ReadString();
                        string p = reader.ReadString();
                        CyberSync.LoadPattern(new ArenaPattern
                        {
                            heights = h,
                            prefabs = p
                        }, wave);
                        break;
                    }
                case PacketType.CyberGameOver:
                    {
                        if (!CyberSync.Active)
                        {
                            return;
                        }
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null && !p.isGhost)
                        {
                            p.SetGhost(true);
                            p.ToggleRig(false);
                        }
                        ItePlugin.GameOver();
                        break;
                    }
                case PacketType.BecameGhost:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null && !p.isGhost)
                        {
                            p.SetGhost(true);
                            p.ToggleRig(false);
                            if (ChatUI.Instance != null)
                            {
                                if(CyberSync.Active)
                                {
                                    ChatUI.Instance.OnSubmitMessage($"<color=#91FFFF>{NetworkManager.GetNameOfId(senderId, true)} became a ghost. {NetworkPlayer.PlayersAlive()} remain.</color>", false, $"<color=#91FFFF>{NetworkManager.GetNameOfId(senderId, true)} became a ghost. {NetworkPlayer.PlayersAlive()} remain.</color>", tts: false);
                                    if (NetworkPlayer.LastPlayerAlive() && !NetworkPlayer.selfIsGhost)
                                    {
                                        NetworkManager.DisplayWarningChatMessage("You're the last player alive! If you die, it's over.");
                                        ChatUI.Instance.ShowUIForBit(7f);
                                    }
                                    else
                                    {
                                        ChatUI.Instance.ShowUIForBit(5f);
                                    }
                                }
                                else
                                {
                                    ChatUI.Instance.OnSubmitMessage($"<color=#91FFFF>{NetworkManager.GetNameOfId(senderId, true)} became a ghost.</color>", false, $"<color=#91FFFF>{NetworkManager.GetNameOfId(senderId, true)} became a ghost.</color>", tts: false);
                                    ChatUI.Instance.ShowUIForBit(5f);
                                }
                            }
                            ItePlugin.DoubleCheckForSoftlock();
                        }
                        break;
                    }
                case PacketType.ReviveGhost:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null && p.isGhost)
                        {
                            p.SetGhost(false);
                            p.ToggleRig(true);
                        }
                        break;
                    }
                case PacketType.ObjectRemoved:
                    {
                        Vector3 pos = reader.ReadVector3();
                        string id = reader.ReadString();

                        if (Net.TryGet(id, senderId, pos, out var e, true))
                            e.PrepDestroy();
                        break;
                    }
                case PacketType.Bullet:
                    {
                        GunSync.Read(reader, senderId);
                        break;
                    }
                case PacketType.Blast:
                    {
                        Vector3 pos = reader.ReadVector3();
                        GunSync.Blast(pos, senderId);
                        break;
                    }
                case PacketType.ProjExplode:
                    {
                        Vector3 pos = reader.ReadVector3();
                        GunSync.ProjBoost(pos, senderId);
                        break;
                    }
                case PacketType.RocketFreeze:
                    {
                        bool val = reader.ReadBool();
                        PlayerRocketManager rm = PlayerRocketManager.Get(senderId);
                        if (rm != null)
                        {
                            rm.rocketsFrozen = val;
                        }
                        break;
                    }
                case PacketType.Trigger:
                    {
                        // you don't want to see minos with trigger sync enabled...
                        if(SceneHelper.CurrentScene == "Level P-1" || SceneHelper.CurrentScene == "Level P-2")
                        {
                            break;
                        }
                        GameObject trigger = SceneObjectCache.Find(reader.ReadString());
                        ObjectActivator act = trigger.GetComponent<ObjectActivator>();
                        if (act != null)
                        {
                            act.activating = false;
                            act.activated = true;
                            if(act.canUseEvents)
                            {
                                if(SceneHelper.CurrentScene == "Level 5-4")
                                {
                                    act.events.Invoke();
                                }
                                else
                                {
                                    if (act.events.toActivateObjects != null)
                                    {
                                        GameObject[] toActivateObjects = act.events.toActivateObjects;
                                        foreach (GameObject obj in toActivateObjects)
                                        {
                                            if (obj != null)
                                            {
                                                obj.SetActive(true);
                                            }
                                        }
                                    }
                                    act.events.onActivate?.Invoke();
                                }
                            }
                        }
                        break;
                    }
                case PacketType.Elevator:
                    {
                        string path = reader.ReadString();
                        int target = reader.ReadInt();
                        bool teleport = reader.ReadBool();
                        Elevator e = SceneObjectCache.Find(path).GetComponent<Elevator>();
                        if (e != null && e.targetStop != target)
                        {
                            if (teleport)
                            {
                                e.TeleportToFloor(target);
                            }
                            else
                            {
                                e.MoveToFloor(target);
                            }
                        }
                        break;
                    }
                case PacketType.SuicideFill:
                    {
                        string path = reader.ReadString();
                        BloodFiller fill = SceneObjectCache.Find(path).GetComponent<BloodFiller>();
                        if (fill != null)
                        {
                            fill.FullyFilled();
                        }
                        break;
                    }
                case PacketType.DeathcatchRespawn:
                    {
                        string path = reader.ReadString();
                        Deathcatcher catcher = SceneObjectCache.Find(path).GetComponent<Deathcatcher>();
                        if(catcher != null)
                        {
                            GameObject.Instantiate(catcher.respawnEffect, catcher.chargeSphere.transform.position, Quaternion.identity);
                        }
                        break;
                    }
                case PacketType.GlobalConnectionJoin:
                    {
                        ulong id = reader.ReadULong();
                        NetworkManager.Instance.ConnectionNet(id);
                        break;
                    }
                case PacketType.PunchNormal:
                    {
                        NetworkPlayer plr = NetworkPlayer.Find(senderId);
                        if(plr != null)
                        {
                            ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("PunchSwoosh"), Random.Range(0.95f, 1.15f), null, 1f, plr.head.transform.position);
                        }
                        break;
                    }
                case PacketType.PunchHeavy:
                    {
                        NetworkPlayer plr = NetworkPlayer.Find(senderId);
                        if (plr != null)
                        {
                            ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("PunchSwooshHeavy"), Random.Range(0.95f, 1.15f), null, 1f, plr.head.transform.position);
                        }
                        break;
                    }
                case PacketType.PunchParry:
                    {
                        NetworkPlayer plr = NetworkPlayer.Find(senderId);
                        if (plr != null)
                        {
                            ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("punch_projectile"), Random.Range(0.95f, 1.15f), null, 1f, plr.head.transform.position);
                        }
                        break;
                    }
                case PacketType.CyberProgress:
                    {
                        float prog = reader.ReadFloat();
                        if(!CyberSync.Active)
                        {
                            break;
                        }
                        if(prog >= 0.33f && CyberSync.catchers.Count > 0)
                        {
                            for(int i = CyberSync.catchers.Count - 1; i >= 0; i--)
                            {
                                if (CyberSync.catchers[i] != null)
                                {
                                    CyberSync.catchers[i].IsActive(true);
                                }
                                CyberSync.catchers.RemoveAt(i);
                            }
                        }
                        if(CyberSync.events.Count > 0)
                        {
                            for(int j = CyberSync.events.Count - 1; j >= 0; j--)
                            {
                                if (CyberSync.events[j] != null && prog >= CyberSync.events[j].waveProgressToActivate)
                                {
                                    CyberSync.events[j].onActivate.Invoke();
                                    CyberSync.events.RemoveAt(j);
                                }
                            }
                        }
                        break;
                    }
                case PacketType.LevelStart:
                    {
                        bool startTimer = reader.ReadBool();
                        bool startMusic = reader.ReadBool();
                        MonoSingleton<OnLevelStart>.Instance.StartLevel(startTimer, startMusic);
                        break;
                    }
                case PacketType.ShopTap:
                    {
                        NetworkPlayer plr = NetworkPlayer.Find(senderId);
                        if(plr != null)
                        {
                            plr.TapAnim();
                        }
                        break;
                    }
                case PacketType.SkullState:
                    {
                        string placed = reader.ReadString();
                        bool dropped = reader.ReadBool();
                        string id = reader.ReadString();
                        Vector3 pos = reader.ReadVector3();
                        Quaternion rot = reader.ReadQuaternion();
                        if(Net.TryGet(id, senderId, pos, out var e, true))
                        {
                            if(Net.IsOwner(e))
                            {
                                break;
                            }
                            NetworkSkull skull = e.Base as NetworkSkull;
                            skull.placedOn = placed;
                            skull.dropped = dropped;
                            e.State(pos, rot, reader);
                        }
                        else
                        {
                            Logs.Error($"(Skull) Couldn't get {id}", name: "PacketReader");
                        }
                        break;
                    }
                case PacketType.Flammable:
                    {
                        float heat = reader.ReadFloat();
                        bool instaDmg = reader.ReadBool();
                        string id = reader.ReadString();
                        GameObject flammableObj = SceneObjectCache.Find(id);
                        if(flammableObj != null)
                        {
                            Flammable flam = flammableObj.GetComponent<Flammable>();
                            flam.Burn(heat, instaDmg);
                        }
                        break;
                    }
                case PacketType.LobbySettings:
                    {
                        LobbyType lobType = reader.ReadEnum<LobbyType>();
                        string newLobName = reader.ReadString();
                        int max = reader.ReadInt();
                        string newAllowCheats = reader.ReadString();
                        if (lobType != LobbyType.None)
                            NetworkManager.DisplaySystemChatMessage("Lobby has been set to: " + lobType.ToString());
                        if(newLobName != "0")
                            NetworkManager.DisplaySystemChatMessage("Lobby name has been changed to: " + newLobName);
                        if(max != 0)
                            NetworkManager.DisplaySystemChatMessage("Lobby max player limit has been changed to: " + max);
                        if(newAllowCheats != "null")
                            NetworkManager.DisplaySystemChatMessage("Allow cheats has been changed to: " + newAllowCheats);
                        break;
                    }
                case PacketType.LevelFinished:
                    {
                        string time = reader.ReadString();
                        ChatUI.Message($"<color=#FF5E36>{NetworkManager.GetNameOfId(senderId, true)} finished the level with the time: </color><color=#FFDD57>{time}</color>", 7f);
                        break;
                    }
                case PacketType.GravVol:
                    {
                        Vector3 grav = reader.ReadVector3();
                        NetworkPlayer.Find(senderId)?.PortalRotate(grav);
                        break;
                    }
                case PacketType.Slam:
                    {
                        Vector3 gcVec = reader.ReadVector3();
                        Vector3 pos = reader.ReadVector3();
                        Vector3 up = reader.ReadVector3();
                        float fallSpeed = reader.ReadFloat();
                        GameObject dust = GameObject.Instantiate(MonoSingleton<NewMovement>.Instance.impactDust, gcVec, Quaternion.identity);
                        dust.transform.forward = up;
                        AudioSource src = dust.GetComponent<AudioSource>();
                        src.spatialBlend = 1f;
                        src.minDistance = 100f;
                        src.maxDistance = 30f;
                        MonoSingleton<SceneHelper>.Instance.CreateEnviroGibs(pos, up * -1f, 5f, Mathf.RoundToInt(Mathf.Lerp(3f, 5f, (Mathf.Abs(fallSpeed) - 50f) / 50f)));
                        break;
                    }
                case PacketType.SlamShockwave:
                    {
                        Vector3 pos = reader.ReadVector3();
                        Vector3 _for = reader.ReadVector3();
                        Vector3 up = reader.ReadVector3();
                        float force = reader.ReadFloat();
                        GunSync.Shockwave(pos, _for, up, force);
                        MonoSingleton<SceneHelper>.Instance.CreateEnviroGibs(pos, up * -1f, 5f, 10);
                        break;
                    }
                case PacketType.Footstep:
                    {
                        Vector3 pos = reader.ReadVector3();
                        float vol = reader.ReadFloat();
                        NetworkPlayer.Find(senderId)?.Footsteps(pos, vol);
                        break;
                    }
                case PacketType.SlideScrape:
                    {
                        SurfaceType surf = reader.ReadEnum<SurfaceType>();
                        Vector3 dodge = reader.ReadVector3();
                        Color col = reader.ReadColor();
                        MonoSingleton<DefaultReferenceManager>.Instance.footstepSet.TryGetSlideParticle(surf, out var part);
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p == null)
                        {
                            break;
                        }    
                        p.SlideScrape(part, dodge);
                        if(p.slideScrape != null) MonoSingleton<SceneHelper>.Instance.SetParticlesColors(p.slideScrape.GetComponentsInChildren<EnviroGibModifier>(), col);
                        break;
                    }
                case PacketType.WallScrape:
                    {
                        SurfaceType surf = reader.ReadEnum<SurfaceType>();
                        Vector3 pos = reader.ReadVector3();
                        bool insteadSetPos = reader.ReadBool();
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if(p == null)
                        {
                            break;
                        }
                        if(insteadSetPos && p.wallScrape != null)
                        {
                            p.wallScrape.transform.position = pos;
                        }
                        else
                        {
                            MonoSingleton<DefaultReferenceManager>.Instance.footstepSet.TryGetWallScrapeParticle(surf, out var part);
                            p.WallScrape(part, pos);
                        }
                        break;
                    }
                case PacketType.DetachSlideScrape:
                    {
                        NetworkPlayer.Find(senderId)?.Detach(false);
                        break;
                    }
                case PacketType.DetachWallScrape:
                    {
                        NetworkPlayer.Find(senderId)?.Detach(true);
                        break;
                    }
                case PacketType.StopSlide:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if(p == null)
                        {
                            break;
                        }
                        AudioSource sound = MonoSingleton<NewMovement>.Instance.slideStopSound.GetComponent<AudioSource>();
                        ItePlugin.SpawnSound(sound.clip, sound.pitch, null, sound.volume, p.transform.position);
                        break;
                    }
            }
        }
    }
}
