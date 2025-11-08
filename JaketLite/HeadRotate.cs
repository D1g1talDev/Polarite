using System;
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
        public Quaternion targetRotation;
        private Transform spine3;

        void Awake()
        {
            // traverse parents upward until finding a parent named "spine.003"
            Transform p = transform.parent;
            while (p != null)
            {
                if (p.name.Equals("spine.003", StringComparison.OrdinalIgnoreCase))
                {
                    spine3 = p;
                    break;
                }
                p = p.parent;
            }
        }

        public void Update()
        {
            if (head != null)
            {
                Vector3 currentHeadEuler = head.localRotation.eulerAngles;

                Vector3 targetHeadEuler = targetRotation.eulerAngles;

                float newX = Mathf.LerpAngle(currentHeadEuler.x, targetHeadEuler.x, Time.unscaledDeltaTime * 10f);

                head.localRotation = Quaternion.Euler(newX * 1, 0f, 0f);
                if (spine3 != null)
                {
                    // apply a smaller fraction of the head rotation to spine3
                    float spineX = newX * 0.25f; // reduced rotation
                    Vector3 cur = spine3.localRotation.eulerAngles;
                    float snewX = Mathf.LerpAngle(cur.x, spineX, Time.unscaledDeltaTime * 5f);
                    spine3.localRotation = Quaternion.Euler(snewX, cur.y, cur.z);
                }
            }
        }
    }
}
