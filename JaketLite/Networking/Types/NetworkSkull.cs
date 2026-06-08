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
        public string placedOn;
        public bool dropped;
        public bool Placed
        {
            get => !string.IsNullOrEmpty(placedOn);
        }
        public bool BeingHeld
        {
            get => owner != 0 && !Placed;
        }
        private ItemIdentifier item;

        public override void Start()
        {
            base.Start();
            item = GetComponent<ItemIdentifier>();
            if(item != null)
            {
                simpleId = item.itemType.ToString();
                item.onPickUp.onActivate.AddListener(() =>
                {
                    Transfer(NetworkManager.Id);
                    placedOn = "";
                });
                item.onPutDown.onActivate.AddListener(() =>
                {
                    placedOn = SceneObjectCache.GetScenePath(transform.parent?.gameObject);
                });
            }
        }
        public override void SendState(PacketWriter writer, PacketType type)
        {
            writer.WriteString(placedOn);
            writer.WriteBool(!item.pickedUp);
            base.SendState(writer, PacketType.SkullState);
        }
        public override void TransferOwnerP2P(ulong newOwner)
        {
            base.TransferOwnerP2P(newOwner);
        }
        private void ExtraPlaceChecks()
        {
            if (string.IsNullOrEmpty(placedOn) && item.ipz != null && Owns)
            {
                placedOn = SceneObjectCache.GetScenePath(item.ipz.gameObject);
            }
            if(item.pickedUp && !string.IsNullOrEmpty(placedOn) && Owns)
            {
                placedOn = "";
            }
        }
        public override void Update()
        {
            syncTransform = false;
            base.Update();
            if (Owns)
            {
                ExtraPlaceChecks();
                return;
            }
            CheckHold();
            if(Placed)
            {
                GameObject ipzObj = SceneObjectCache.Find(placedOn);
                ItemPlaceZone ipz = ipzObj.GetComponent<ItemPlaceZone>();
                if(ipz != null && transform.parent != ipzObj.transform)
                {
                    Place(ipz);
                }
            }
        }
        private void Place(ItemPlaceZone ipz)
        {
            transform.SetParent(ipz.transform);
            item.pickedUp = false;
            item.ipz = ipz;
            if (item.reverseTransformSettings)
            {
                item.transform.localPosition = Vector3.zero;
                item.transform.localScale = Vector3.one;
                item.transform.localRotation = Quaternion.identity;
            }
            else
            {
                item.transform.localPosition = item.putDownPosition;
                item.transform.localScale = item.putDownScale;
                item.transform.localRotation = Quaternion.Euler(item.putDownRotation);
            }
            Transform[] comps = item.GetComponentsInChildren<Transform>();
            foreach (Transform comp in comps)
            {
                comp.gameObject.layer = 22;
                if (comp.TryGetComponent<OutdoorsChecker>(out var comp1) && comp1.enabled)
                {
                    comp1.CancelInvoke("SlowUpdate");
                    comp1.SlowUpdate();
                }
            }
            Collider[] cols = item.GetComponentsInChildren<Collider>();
            foreach(var col in cols)
            { 
                col.enabled = true;
            }
            ipz.CheckItem();
            GameObject.Instantiate(item.pickUpSound, transform.position, Quaternion.identity);
            item.gameObject.SetActive(true);
        }
        private void CheckHold()
        {
            if(Placed || dropped)
            {
                return;
            }
            NetworkPlayer plr = NetworkPlayer.Find(owner);
            if(plr == null)
            {
                return;
            }
            if(transform.parent != plr.holderObject)
            {
                Hold(plr);
            }
            else
            {
                return;
            }
        }
        private void Hold(NetworkPlayer plr)
        {
            if (plr == null) return;
            ItemPlaceZone[] ipzs = item.GetComponentsInParent<ItemPlaceZone>();
            Transform holderObj = plr.holderObject;
            item.pickedUp = false;
            item.ipz = null;
            item.transform.position = holderObj.transform.position;
            item.transform.SetParent(holderObj);
            Rigidbody[] bodies = item.GetComponentsInChildren<Rigidbody>();
            foreach(var body in bodies)
            {
                body.isKinematic = true;
            }
            Collider[] cols = item.GetComponentsInChildren<Collider>();
            foreach(var col in cols)
            {
                col.enabled = false;
            }
            foreach(var ipz in ipzs)
            {
                ipz.CheckItem();
            }
            GameObject.Instantiate(item.pickUpSound, transform.position, Quaternion.identity);
        }
        public static void ClearHolders()
        {
            foreach(var plr in NetworkManager.players.Values)
            {
                plr.ClearHolder();
            }
        }
    }
}
