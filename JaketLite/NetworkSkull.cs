using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polarite.Multiplayer;
using Polarite.Networking;

using UnityEngine;

namespace Polarite
{
    public class NetworkSkull : NetworkObject
    {
        public bool IsPlaced()
        {
            return transform.parent?.gameObject.layer == 22;
        }


        public bool isPlaced;
        public ulong holder;
        public bool holding;
        public ItemIdentifier iId;

        public static NetworkSkull Holding;

        public override void Start()
        {
            // kill thyself
            Destroy(this);
            iId = GetComponent<ItemIdentifier>();
            simpleId = iId.itemType.ToString();
            id = SceneObjectCache.GetScenePath(gameObject);
            syncTransform = false;

            iId.onPickUp.onActivate.AddListener(() =>
            {
                Holding = this;
                holder = NetworkManager.Id;
                Transfer(NetworkManager.Id);
            });

            base.Start();
        }

        public override void SendState(PacketWriter writer)
        {
            if(!Net.IsOwner(this))
            {
                return;
            }
            writer.WriteBool(IsPlaced());
            writer.WriteBool(iId.pickedUp);
            writer.WriteULong(holder);
            base.SendState(writer);
        }
        public override void State(Vector3 pos, BinaryPacketReader reader)
        {
            isPlaced = reader.ReadBool();
            holding = reader.ReadBool();
            holder = reader.ReadULong();
            base.State(pos, reader);
        }
        public override void TransferOwnerP2P(ulong newOwner)
        {
            holder = newOwner;
            if(Holding == this)
            {
                Discard();
            }
            base.TransferOwnerP2P(newOwner);
        }
        public void Discard()
        {
            FistControl fc = MonoSingleton<FistControl>.Instance;
            fc.currentPunch.ForceDrop();
            fc.currentPunch.PlaceHeldObject(new ItemPlaceZone[0], null);
            Holding = null;
        }

        public override void Update()
        {
            if (Net.IsOwner(this)) return;
            if (holding)
            {
                NetworkPlayer p = NetworkPlayer.Find(holder);
                transform.position = p.holderObject.transform.position;
                transform.rotation = p.holderObject.transform.rotation;
            }
            if(!isPlaced && IsPlaced())
            {
                ItemPlaceZone[] zones = GetComponentsInParent<ItemPlaceZone>();
                foreach (var zone in zones)
                {
                    zone.CheckItem();
                }
                transform.SetParent(null);
            }
            if(isPlaced && !IsPlaced())
            {
                foreach (var overlap in Physics.OverlapSphere(transform.position, 0.5f, 1 << 22))
                {
                    ItemPlaceZone[] zones = overlap.GetComponents<ItemPlaceZone>();
                    if (zones.Length > 0)
                    {
                        transform.SetParent(overlap.transform);
                        foreach (var zone in zones)
                        {
                            zone.CheckItem();
                        }
                    }
                }
            }
            base.Update();
        }
    }
}
