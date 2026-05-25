using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using UnityEngine;
using Polarite.Multiplayer;

namespace Polarite
{
    public class CustomP2Event : MonoBehaviour
    {
        public EnemyIdentifier eid;
        public FleshPrison pri;
        public void Start()
        {
            if(pri.altVersion && NetworkManager.InLobby && SceneHelper.CurrentScene == "Level P-2")
            {
                StartCoroutine(WaitToTrigger());
            }
        }
        public IEnumerator WaitToTrigger()
        {
            yield return new WaitForSeconds(30f);
            pri.aud.Play();
            pri.enabled = false;
            yield return new WaitForSeconds(2f);
            pri.TryGetComponent<Statue>(out var stat);
            stat.DeathEnd();
        }
    }
}
