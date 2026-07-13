using Polarite.Multiplayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;


namespace Polarite
{
    public class HeadRotate : MonoBehaviour
    {
        public Transform head;
        public Transform arm;
        public Quaternion targetRotation;
        public Transform spine3;
        private NetworkPlayer plr;

        void Start()
        {
            // head is inside of spine.005
            spine3 = head.parent.parent.parent;
            plr = GetComponentInParent<NetworkPlayer>();
        }

        public void LateUpdate()
        {
            if (head != null)
            {
                Vector3 currentHeadEuler = head.localRotation.eulerAngles;

                Vector3 targetHeadEuler = targetRotation.eulerAngles;

                float newX = Mathf.LerpAngle(currentHeadEuler.x, targetHeadEuler.x, Time.unscaledDeltaTime * 20f);

                head.localRotation = Quaternion.Euler(newX * 1, 0f, 0f);
                if (spine3 != null)
                {
                    spine3.localRotation *= head.localRotation;
                }
            }
        }
    }
}
