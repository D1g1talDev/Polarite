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

        public void Update()
        {
            if (head != null)
            {
                Vector3 currentHeadEuler = head.localRotation.eulerAngles;

                Vector3 targetHeadEuler = targetRotation.eulerAngles;

                float newX = Mathf.LerpAngle(currentHeadEuler.x, targetHeadEuler.x, Time.unscaledDeltaTime * 10f);

                head.localRotation = Quaternion.Euler(newX * 1, 0f, 0f);
            }
        }
    }
}
