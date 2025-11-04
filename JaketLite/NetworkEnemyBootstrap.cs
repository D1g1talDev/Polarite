using System.Collections.Generic;

using UnityEngine;

namespace Polarite.Multiplayer
{
    public class NetworkEnemyBootstrap : MonoBehaviour
    {
        public static float lastScanTime;

        private void Update()
        {
            if(!NetworkManager.InLobby)
            {
                return;
            }
            if (Time.unscaledTime - lastScanTime > 2f)
            {
                lastScanTime = Time.unscaledTime;
                AttachSyncScripts();
            }
        }

        public static void AttachSyncScripts()
        {
            if (!NetworkManager.InLobby)
                return;

            foreach (EnemyIdentifier eid in GameObject.FindObjectsOfType<EnemyIdentifier>(true))
            {
                if (eid.GetComponent<NetworkEnemySync>() == null)
                    eid.gameObject.AddComponent<NetworkEnemySync>();
            }
        }
    }
}