using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Polarite.Debugging
{
    public static class TransformUtils
    {
        public static string GetFullPath(Transform t)
        {
            if (t == null)
                return "<null>";

            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
    public class LineTargetTool : MonoBehaviour
    {
        public float checkDistance = 100f;
        public LayerMask collisionMask = LayerMask.GetMask("Environment", "Outdoors", "EnvironmentBaked", "OutdoorsBaked", "Invisible", "EnemyTrigger");

        private void Update()
        {
            if(ItePlugin.debugMode && Input.GetKeyDown(KeyCode.F4))
            {
                Vector3 origin = transform.position;
                Vector3 direction = transform.forward;

                if (Physics.Raycast(origin, direction, out RaycastHit hit, checkDistance, collisionMask))
                {
                    Debug.DrawLine(origin, hit.point, Color.red);

                    string path = TransformUtils.GetFullPath(hit.collider.transform);

                    if (hit.collider.GetComponentInParent<INetworkObject>() != null)
                    {
                        INetworkObject obj = hit.collider.GetComponentInParent<INetworkObject>();
                        if (obj != null)
                        {
                            ItePlugin.LogDebug($"[LINE TARGET] Hit networked object: {hit.transform.name}, Simple ID: {obj.SimpleID}, ID was copied to your clipboard.");
                            GUIUtility.systemCopyBuffer = obj.ID;
                        }
                    }
                    else
                    {
                        ItePlugin.LogDebug($"[LINE TARGET] Hit: {hit.transform.name}, Path was copied to your clipboard.");
                        GUIUtility.systemCopyBuffer = TransformUtils.GetFullPath(hit.transform);
                    }
                    if(hit.collider.GetComponentInParent<EnemyIdentifier>() != null)
                    {
                        EnemyIdentifier enemy = hit.collider.GetComponentInParent<EnemyIdentifier>();
                        if(enemy != null)
                        {
                            ItePlugin.LogDebug($"[LINE TARGET] Hit enemy: {hit.transform.name}, Name: {enemy.FullName}, Health: {enemy.health}, Type: {enemy.enemyType.ToString()}");
                        }
                    }
                }
            }
        }
    }
}
