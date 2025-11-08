using System;
using System.Net.Sockets;

using Steamworks;

using UnityEngine;

namespace Polarite.Multiplayer
{
    public enum PacketType : byte
    {
        None = 0,

        // player health
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

        // enemies
        EnemyState = 19,
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
        SkipExecute = 35
    }

    public static class PacketReader
    {
        // Dispatch incoming packets
        public static void Handle(PacketType type, byte[] data, ulong senderId)
        {
            BinaryPacketReader reader = new BinaryPacketReader(data);
            switch (type)
            {
                case PacketType.Level:
                    {
                        string scene = reader.ReadString();
                        int diff = reader.ReadInt();

                        ItePlugin.ignoreSpectate = true;
                        SceneHelper.LoadScene(scene);
                        PrefsManager.Instance.SetInt("difficulty", diff);
                        SceneHelper.SetLoadingSubtext("<color=#91FFFF>/// VIA POLARITE ///");
                        break;
                    }

                case PacketType.Restart:
                    OptionsManager.Instance.RestartMission();
                    break;

                case PacketType.Kick:
                    NetworkManager.Instance.LeaveLobby();
                    NetworkManager.DisplaySystemChatMessage("You have been kicked from the lobby.");
                    break;

                case PacketType.Ban:
                    NetworkManager.Instance.LeaveLobby();
                    NetworkManager.DisplaySystemChatMessage("You have been banned from the lobby.");
                    break;

                case PacketType.Hurt:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.HurtNoise();
                        break;
                    }

                case PacketType.Die:
                    {
                        string msg = reader.ReadString();
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.DeathNoise();
                        NetworkManager.DisplayGameChatMessage(NetworkManager.GetNameOfId(senderId) + " " + msg);
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
                        if (p != null)
                            p.JumpNoise();
                        break;
                    }

                case PacketType.Dash:
                    {
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.DashNoise();
                        break;
                    }

                case PacketType.Gun:
                    {
                        int weapon = reader.ReadInt();
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.SetWeapon(weapon);
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
                        int skin = (int)reader.ReadEnum<SkinType>();
                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                            p.UpdateSkin(skin);
                        break;
                    }

                case PacketType.Transform:
                    {
                        Vector3 pos = reader.ReadVector3();
                        Quaternion rot = reader.ReadQuaternion();
                        bool sliding = reader.ReadBool();
                        bool air = reader.ReadBool();
                        bool walking = reader.ReadBool();
                        int hp = reader.ReadInt();

                        NetworkPlayer p = NetworkPlayer.Find(senderId);
                        if (p != null)
                        {
                            p.SetTargetTransform(pos, rot);
                            p.SetAnimation(sliding, air, walking);
                            p.SetHP(hp);
                        }
                        break;
                    }

                case PacketType.ChatMsg:
                    {
                        string text = reader.ReadString();
                        var p = NetworkPlayer.Find(senderId);
                        string name = NetworkManager.GetNameOfId(senderId);

                        string format;
                        if (senderId == 76561198893363168 || senderId == 76561199078878250)
                        {
                            format = $"<color=green>[DEV] {name}</color>: {text}";
                        }
                        else
                        {
                            format = (NetworkManager.Instance.CurrentLobby.Owner.Id == senderId)
                                ? $"<color=orange>{name}</color>: {text}"
                                : $"<color=grey>{name}</color>: {text}";
                        }

                        ChatUI.Instance.OnSubmitMessage(format, false, text, p.transform);
                        ChatUI.Instance.ShowUIForBit();
                        break;
                    }

                case PacketType.EnemyState:
                    {
                        string id = reader.ReadString();
                        Vector3 pos = reader.ReadVector3();
                        Quaternion rot = reader.ReadQuaternion();
                        NetworkEnemy e = NetworkEnemy.Find(id);
                        if (e != null)
                            e.ApplyState(pos, rot);
                        break;
                    }

                case PacketType.EnemyDmg:
                    {
                        string id = reader.ReadString();
                        float damage = reader.ReadFloat();
                        string hitter = reader.ReadString();
                        bool weakpoint = reader.ReadBool();
                        Vector3 poi = reader.ReadVector3();
                        NetworkEnemy e = NetworkEnemy.Find(id);
                        if (e != null)
                            e.ApplyDamage(damage, hitter, weakpoint, poi);
                        break;
                    }

                case PacketType.DeathEnemy:
                    {
                        string id = reader.ReadString();
                        NetworkEnemy e = NetworkEnemy.Find(id);
                        if (e != null)
                            e.HandleDeath();
                        break;
                    }

                case PacketType.Ownership:
                    {
                        string id = reader.ReadString();
                        ulong newOwner = reader.ReadULong();
                        NetworkEnemy e = NetworkEnemy.Find(id);
                        if (e != null)
                            e.TakeOwnerP2P(newOwner);
                        break;
                    }

                case PacketType.EnemySpawn:
                    {
                        string path = reader.ReadString();
                        GameObject go = SceneObjectCache.Find(path);
                        if (go != null)
                        {
                            var sync = go.GetComponent<NetworkEnemySync>();
                            sync.owner = senderId;
                            sync.here = true;
                            go.SetActive(true);
                        }
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
                        if (door && !door.aboutToOpen)
                        {
                            door.aboutToOpen = true;
                            door.Open();
                            door.Invoke("OpenDoors", 1f);
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
                        string path = reader.ReadString();
                        var cp = SceneObjectCache.Find(path).GetComponent<CheckPoint>();
                        if (cp && !cp.activated)
                        {
                            cp.activated = true;
                            cp.ActivateCheckPoint();
                            NetworkManager.ShoutCheckpoint(NetworkManager.GetNameOfId(senderId));
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
                        NetworkManager.Instance.HandleMemberJoinedP2P(new Friend(who));
                        break;
                    }

                case PacketType.Left:
                    {
                        ulong who = reader.ReadULong();
                        NetworkManager.Instance.HandleMemberLeftP2P(new Friend(who));
                        break;
                    }

                case PacketType.HostLeave:
                    NetworkManager.Instance.LeaveLobby();
                    break;

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
            }
        }
    }
}
