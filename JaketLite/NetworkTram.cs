//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using Polarite.Multiplayer;
//using System.Collections;

//namespace Polarite
//{
//    public class NetworkTram : MonoBehaviour
//    {
//        public string ID;

//        private Vector3 targetPosition;
//        private Quaternion targetRotation;
//        private float lerpSpeed = 10f;

//        private static readonly Dictionary<string, NetworkTram> Trams = new Dictionary<string, NetworkTram>();
//        private Coroutine broadcastRoutine;

//        void Awake()
//        {
//            if (!Trams.ContainsKey(ID))
//                Trams.Add(ID, this);
//        }

//        void OnEnable()
//        {
//            if (NetworkManager.HostAndConnected)
//                broadcastRoutine = StartCoroutine(BroadcastLoop());
//        }

//        void OnDisable()
//        {
//            if (broadcastRoutine != null)
//                StopCoroutine(broadcastRoutine);
//        }

//        void OnDestroy()
//        {
//            if (Trams.ContainsKey(ID))
//                Trams.Remove(ID);
//        }

//        void Update()
//        {
//            if (!NetworkManager.HostAndConnected)
//            {
//                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * lerpSpeed);
//                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.unscaledDeltaTime * lerpSpeed);
//            }
//        }

//        public static void Create(string id, GameObject tramObject)
//        {
//            NetworkTram tram = tramObject.AddComponent<NetworkTram>();
//            tram.enabled = true;
//            tram.ID = id;
//            tram.broadcastRoutine = tram.StartCoroutine(tram.BroadcastLoop());

//            if (!Trams.ContainsKey(id))
//                Trams.Add(id, tram);
//        }

//        public static NetworkTram Find(string id)
//        {
//            foreach (var t in Trams.Values)
//            {
//                if (t.ID == id)
//                    return t;
//            }
//            return null;
//        }

//        private IEnumerator BroadcastLoop()
//        {
//            while (NetworkManager.HostAndConnected)
//            {
//                BroadcastTramState();
//                yield return new WaitForSecondsRealtime(0.1f);
//            }
//        }

//        public void BroadcastTramState()
//        {
//            if (!NetworkManager.HostAndConnected) return;

//            NetworkManager.Instance.BroadcastPacket(new NetPacket
//            {
//                type = "tramsync",
//                name = ID,
//                parameters = new string[]
//                {
//                    transform.position.x.ToString(),
//                    transform.position.y.ToString(),
//                    transform.position.z.ToString(),
//                    transform.rotation.x.ToString(),
//                    transform.rotation.y.ToString(),
//                    transform.rotation.z.ToString(),
//                    transform.rotation.w.ToString()
//                }
//            });
//        }

//        public void ApplyNetworkUpdate(string[] parameters)
//        {
//            Vector3 pos = new Vector3(
//                float.Parse(parameters[0]),
//                float.Parse(parameters[1]),
//                float.Parse(parameters[2])
//            );

//            Quaternion rot = new Quaternion(
//                float.Parse(parameters[3]),
//                float.Parse(parameters[4]),
//                float.Parse(parameters[5]),
//                float.Parse(parameters[6])
//            );

//            targetPosition = pos;
//            targetRotation = rot;
//        }
//    }
//}


