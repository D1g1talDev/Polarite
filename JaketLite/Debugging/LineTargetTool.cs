using Polarite.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Polarite.Debugging
{
    public class LineTargetTool : MonoBehaviour
    {
        private void Update()
        {
            if(ItePlugin.debugMode && Input.GetKeyDown(KeyCode.F4))
            {
                if (PortalPhysicsV2.Raycast(MonoSingleton<CameraController>.Instance.GetDefaultPos(), MonoSingleton<CameraController>.Instance.transform.forward, out PhysicsCastResult hit, 100f, LayerMaskDefaults.Get(LMD.EnemiesAndEnvironment)))
                {
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
                        GUIUtility.systemCopyBuffer = SceneObjectCache.GetScenePath(hit.transform.gameObject);
                    }
                    if(hit.collider.GetComponentInParent<EnemyIdentifier>() != null)
                    {
                        EnemyIdentifier enemy = hit.collider.GetComponentInParent<EnemyIdentifier>();
                        if(enemy != null)
                        {
                            ItePlugin.LogDebug($"[LINE TARGET] Hit enemy: {hit.transform.name}, Name: {enemy.FullName}, Health: {enemy.health}, Type: {enemy.enemyType}");
                        }
                    }
                }
            }
        }
    }
}
