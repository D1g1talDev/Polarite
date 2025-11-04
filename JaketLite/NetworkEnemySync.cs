using UnityEngine;

namespace Polarite.Multiplayer
{
    public class NetworkEnemySync : MonoBehaviour
    {
        public string id;
        public bool here;

        void Awake()
        {
            id = SceneObjectCache.GetScenePath(gameObject);
        }

        void OnEnable()
        {
            if(GetComponent<NetworkEnemy>() == null && NetworkManager.InLobby)
            {
                NetworkEnemy.Create(id, GetComponent<EnemyIdentifier>());
            }
            if (NetworkManager.InLobby && !here)
            {
                here = true;
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "enemySpawn",
                    name = id,
                });
            }
        }
    }
}