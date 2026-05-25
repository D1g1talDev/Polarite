using Polarite.Multiplayer;
using Polarite.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Polarite
{
    public static class NetworkDebugRef
    {
        public static GameObject mainObject;
        public static TextMeshProUGUI netTick, count;
        public static GameObject netObjUI;
        public static TextMeshProUGUI simpleId, owner, index, alive, syncTransform;

        public static void SetUIActive(bool value)
        {
            mainObject.SetActive(value);
        }

        public static void Update(float netTickRef, int countRef, INetworkObject obj)
        {
            if(!mainObject.activeSelf)
            {
                return;
            }
            netTick.text = $"TICK RATE: {netTickRef}";
            count.text = $"OBJECT COUNT: {countRef}";

            if(Net.List.Objects.Count < 1)
            {
                netObjUI.SetActive(false);
            }
            else
            {
                if (NetworkList.ValidObjectCheck(obj))
                {
                    netObjUI.SetActive(true);
                    simpleId.text = $"SIMPLE ID: {obj.SimpleID}";
                    owner.text = $"OWNER: {NetworkManager.GetNameOfId(obj.Owner)}";
                    index.text = $"INDEX: {obj.Index}";
                    alive.text = $"ALIVE: {obj.Alive}";
                    syncTransform.text = $"TRANSFORM SYNCED: {obj.TransformSynced}";
                }
                else
                {
                    netObjUI.SetActive(false);
                }
            }
        }
        public static INetworkObject GetNearestObject(Vector3 from)
        {
            if(!NetworkManager.InLobby)
            {
                return null;
            }
            if(Net.List.Objects.Count < 1)
            {
                return null;
            }
            INetworkObject nearest = null;
            float nearDist = float.MaxValue;
            foreach (var obj in Net.List.Objects)
            {
                float dist = (obj.Base.transform.position - from).sqrMagnitude;
                if (dist < nearDist)
                {
                    nearDist = dist;
                    nearest = obj;
                }
            }
            return nearest;
        }
    }
}
