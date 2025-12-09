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
            yield return new WaitForSeconds(50f);
            HudMessageReceiver hud = HudMessageReceiver.Instance;
            hud.aud.pitch = 0.5f;
            hud.clickAud.pitch = 0.1f;
            pri.aud.Play();
            pri.enabled = false;
            yield return new WaitForSeconds(2f);
            hud.SendHudMessage("<color=red>W H A T ? </color>");
            yield return new WaitForSeconds(2f);
            hud.SendHudMessage("<color=red>T H I S  S H O U L D N ' T  B E  H A P P E N I N G . </color>");
            yield return new WaitForSeconds(3f);
            MonoSingleton<CameraController>.Instance.CameraShake(10f);
            pri.TryGetComponent<Statue>(out var stat);
            stat.DeathEnd();
            Explosion exp = Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.superExplosion, new Vector3(pri.transform.position.x, pri.transform.position.y + 5f, pri.transform.position.z), Quaternion.identity).GetComponentInChildren<Explosion>();
            exp.maxSize = 25f;
            exp.speed = 10f;
            hud.aud.pitch = 1f;
            hud.clickAud.pitch = 1f;
        }
    }
}
