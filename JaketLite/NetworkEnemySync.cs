using Steamworks;

using UnityEngine;

namespace Polarite.Multiplayer
{
    public class NetworkEnemySync : MonoBehaviour
    {
        public string id;
        public bool here;
        public ulong owner;

        void Awake()
        {
            id = SceneObjectCache.GetScenePath(gameObject);
        }

        void OnEnable()
        {
            if(GetComponent<NetworkEnemy>() == null && NetworkManager.InLobby)
            {
                NetworkEnemy.Create(id, GetComponent<EnemyIdentifier>(), (GetComponent<EnemyIdentifier>().isBoss) ? SteamClient.SteamId : NetworkManager.Instance.CurrentLobby.Owner.Id);
            }
            if (NetworkManager.InLobby && !here)
            {
                here = true;
                owner = (GetComponent<EnemyIdentifier>().isBoss) ? SteamClient.SteamId : NetworkManager.Instance.CurrentLobby.Owner.Id;
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "enemySpawn",
                    name = id,
                });
            }
        }
    }
}