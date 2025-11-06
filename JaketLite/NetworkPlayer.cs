using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Steamworks;

using TMPro;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

using Random = UnityEngine.Random;

namespace Polarite.Multiplayer
{
    public class NetworkPlayer : MonoBehaviour
    {
        public ulong SteamId { get; private set; }
        public string PlayerName { get; private set; }

        public NameTag NameTag;

        public bool testPlayer;

        public AudioSource spawnNoise, deathNoise, hurtNoise, jumpNoise, dashNoise;

        public Animator animator;

        public Animator armAnimator;

        public GameObject[] weapons;

        public HeadRotate head;

        public SkinnedMeshRenderer mainRenderer;

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private float lerpSpeed = 10f;

        public static NetworkPlayer LocalPlayer;

        public Coroutine updatePos;

        public void SpawnNoise()
        {
            spawnNoise.Play();
        }
        public void DeathNoise()
        {
            deathNoise.Play();
            GameObject expA = Instantiate(MonoSingleton<DefaultReferenceManager>.Instance.explosion, transform.position, Quaternion.identity);
            Explosion expB = expA.GetComponentInChildren<Explosion>();
            expB.canHit = AffectedSubjects.PlayerOnly;
            expB.damage = 0;
            expB.harmless = true;
        }
        public void HurtNoise()
        {
            hurtNoise.Play();
        }
        public void JumpNoise()
        {
            jumpNoise.Play();
            JumpAnim();
        }
        public void DashNoise()
        {
            dashNoise.Play();
        }

        public void Init(ulong steamId, string playerName)
        {
            SteamId = steamId;
            PlayerName = playerName;
            name = (steamId != SteamClient.SteamId.Value) ? $"NetworkPlayer_{playerName}_{Random.Range(0, 100000)}" : "LocalPlayer";
            if (SteamId == SteamClient.SteamId.Value)
            {
                updatePos = StartCoroutine(UpdatePos());
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            if (LocalPlayer == null && steamId == SteamClient.SteamId.Value)
            {
                LocalPlayer = this;
            }
            DontDestroyOnLoad(gameObject);
            SpawnNoise();
        }
        private void OnSceneLoaded(Scene args, LoadSceneMode args2)
        {
            updatePos = StartCoroutine(UpdatePos());
        }

        public IEnumerator UpdatePos()
        {
            Transform transform = MonoSingleton<NewMovement>.Instance.transform;
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                bool sliding = MonoSingleton<NewMovement>.Instance.sliding;
                bool grounded = !MonoSingleton<NewMovement>.Instance.gc.onGround;
                bool walking = MonoSingleton<NewMovement>.Instance.walking;
                Quaternion rotation = (sliding) ? Quaternion.LookRotation(MonoSingleton<NewMovement>.Instance.rb.velocity) : MonoSingleton<CameraController>.Instance.transform.rotation;
                NetworkManager.Instance.BroadcastPacket(new NetPacket
                {
                    type = "transform",
                    name = SteamId.ToString(),
                    parameters = new string[]
                    {
                        transform.position.x.ToString(), (sliding) ? (transform.position.y).ToString() : (transform.position.y - 1.5f).ToString(), transform.position.z.ToString(),
                        MonoSingleton<CameraController>.Instance.transform.rotation.x.ToString(), rotation.y.ToString(), MonoSingleton<CameraController>.Instance.transform.rotation.z.ToString(), MonoSingleton<CameraController>.Instance.transform.rotation.w.ToString(),

                        // animation stuff
                        sliding.ToString(), grounded.ToString(), walking.ToString(),

                        // hp
                        MonoSingleton<NewMovement>.Instance.hp.ToString(),
                    }
                });
            }
        }

        public void SetTargetTransform(Vector3 pos, Quaternion rot)
        {
            targetPosition = pos;
            targetRotation = rot;
        }
        public void SetAnimation(bool slide, bool air, bool walk)
        {
            animator.SetBool("Sliding", slide);
            animator.SetBool("InAir", air);
            if(!air)
            {
                if (walk)
                {
                    animator.SetLayerWeight(1, 1f);
                    animator.SetLayerWeight(2, 0f);
                }
                else
                {
                    animator.SetLayerWeight(1, 0f);
                    animator.SetLayerWeight(2, 0f);
                }
            }
            else
            {
                animator.SetLayerWeight(1, 1f);
                animator.SetLayerWeight(2, 0f);
            }
        }
        public void JumpAnim()
        {
            animator.SetTrigger("Jump");
        }
        /* in honor
        public void SetWeapon(int type)
        {
            switch (type)
            {
                case 0:
                    weapons[0].SetActive(true);
                    weapons[1].SetActive(false);
                    weapons[2].SetActive(false);
                    weapons[3].SetActive(false);
                    weapons[4].SetActive(false);
                    break;
                case 1:
                    weapons[0].SetActive(false);
                    weapons[1].SetActive(true);
                    weapons[2].SetActive(false);
                    weapons[3].SetActive(false);
                    weapons[4].SetActive(false);
                    break;
                case 2:
                    weapons[0].SetActive(false);
                    weapons[1].SetActive(false);
                    weapons[2].SetActive(true);
                    weapons[3].SetActive(false);
                    weapons[4].SetActive(false);
                    break;
                case 3:
                    weapons[0].SetActive(false);
                    weapons[1].SetActive(false);
                    weapons[2].SetActive(false);
                    weapons[3].SetActive(true);
                    weapons[4].SetActive(false);
                    break;
                case 4:
                    weapons[0].SetActive(false);
                    weapons[1].SetActive(false);
                    weapons[2].SetActive(false);
                    weapons[3].SetActive(false);
                    weapons[4].SetActive(true);
                    break;
                default:
                    weapons[0].SetActive(false);
                    weapons[1].SetActive(true);
                    weapons[2].SetActive(false);
                    weapons[3].SetActive(false);
                    weapons[4].SetActive(false);
                    break;
            }
        }
        */
        public void SetWeapon(int type)
        {
            foreach(var w in weapons)
            {
                w.SetActive(false);
            }
            weapons[type].SetActive(true);
        }
        public void CoinAnim()
        {
            CancelInvoke(nameof(GoBackToIdle));
            armAnimator.SetTrigger("Coin");
            Invoke(nameof(GoBackToIdle), 0.7f);
        }
        public void PunchAnim()
        {
            CancelInvoke(nameof(GoBackToIdle));
            armAnimator.SetTrigger("Punch");
            Invoke(nameof(GoBackToIdle), 0.967f);
        }
        public void WhipAnim()
        {
            CancelInvoke(nameof(GoBackToIdle));
            armAnimator.SetTrigger("Whiplash");
            Invoke(nameof(GoBackToIdle), 1);
        }
        public void GoBackToIdle()
        {
            armAnimator.Play("idle");
        }
        /*
        public void Pickup(ItemType type)
        {
            Skull[] skulls = armAnimator.transform.GetComponentsInChildren<Skull>();
            GameObject red = skulls[0].gameObject;
            GameObject blue = skulls[1].gameObject;
            GameObject torch = skulls[2].gameObject;
            GameObject soap = skulls[3].gameObject;

            red.SetActive(false);
            blue.SetActive(false);
            torch.SetActive(false);
            soap.SetActive(false);

            switch(type)
            {
                case ItemType.SkullRed:
                    red.SetActive(true); 
                    break;
                case ItemType.SkullBlue:
                    blue.SetActive(true);
                    break;
                case ItemType.Torch:
                    torch.SetActive(true);
                    break;
                case ItemType.Soap:
                    soap.SetActive(true); 
                    break;
            }
        }
        public void Drop()
        {
            Skull[] skulls = armAnimator.transform.GetComponentsInChildren<Skull>();
            foreach(var skull in skulls)
            {
                skull.gameObject.SetActive(false);
            }
        }
        */

        private void Update()
        {
            if(updatePos == null && SteamId == SteamClient.SteamId.Value)
            {
                updatePos = StartCoroutine(UpdatePos());
            }
            if (testPlayer)
            {
                Quaternion rotation = (NewMovement.Instance.sliding) ? Quaternion.LookRotation(MonoSingleton<NewMovement>.Instance.rb.velocity) : MonoSingleton<CameraController>.Instance.transform.rotation;
                SetTargetTransform(new Vector3(MonoSingleton<NewMovement>.Instance.transform.position.x, (NewMovement.Instance.sliding) ? MonoSingleton<NewMovement>.Instance.transform.position.y : MonoSingleton<NewMovement>.Instance.transform.position.y - 1.5f, MonoSingleton<NewMovement>.Instance.transform.position.z), new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
                SetAnimation(NewMovement.Instance.sliding, !NewMovement.Instance.gc.onGround, NewMovement.Instance.walking);
                SetHP(NewMovement.Instance.hp);
            }
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * lerpSpeed);

            Vector3 currentEuler = transform.rotation.eulerAngles;

            Vector3 targetEuler = targetRotation.eulerAngles;

            float newY = Mathf.LerpAngle(currentEuler.y, targetEuler.y, Time.unscaledDeltaTime * lerpSpeed);

            Quaternion newRotation = Quaternion.Euler(currentEuler.x, newY, currentEuler.z);

            transform.rotation = newRotation;

            head.targetRotation = targetRotation;
        }

        public void SetHP(int hp)
        {
            NameTag.SetHP(hp);
        }
        public void UpdateSkin(int id)
        {
            Material[] mats = mainRenderer.materials;
            switch (id)
            {
                case 0:
                    mats[0] = ItePlugin.mainBundle.LoadAsset<Material>("V1Glow");
                    mats[1] = ItePlugin.mainBundle.LoadAsset<Material>("V1WingGlow");
                    mats[0].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                    mats[1].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                    mats[2].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                    break;

                case 1:
                    mats[0] = ItePlugin.mainBundle.LoadAsset<Material>("V2Glow");
                    mats[1] = ItePlugin.mainBundle.LoadAsset<Material>("V2WingGlow");
                    mats[0].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                    mats[1].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                    mats[2].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                    break;
            }
            mainRenderer.materials = mats;
            SkinnedMeshRenderer[] armsStuff = armAnimator.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var a in armsStuff)
            {
                Material[] mats1 = a.materials;
                foreach (var m in mats1)
                {
                    m.shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                }
                a.materials = mats1;
            }
            foreach (var w in weapons)
            {
                SkinnedMeshRenderer s = w.GetComponentInChildren<SkinnedMeshRenderer>();
                Material[] mats2 = s.materials;
                foreach (var m in mats2)
                {
                    m.shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                }
                s.materials = mats2;
            }
        }

        public static NetworkPlayer Find(ulong id)
        {
            foreach (var p in NetworkManager.players)
            {
                if (p.Value.SteamId == id)
                {
                    return p.Value;
                }
            }
            return null;
        }
        public static NetworkPlayer Create(ulong id, string name)
        {
            NetworkPlayer possibleCopy = Find(id);
            if (possibleCopy != null)
            {
                Destroy(possibleCopy.gameObject);
                NetworkManager.players.Remove(id.ToString());
            }
            GameObject v2Rig = GameObject.Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("NetworkRig"));

            AudioClip spawn = v2Rig.GetComponent<V2>().wingChangeEffect.GetComponent<AudioSource>().clip;
            AudioClip death = v2Rig.GetComponent<V2>().KoScream.GetComponent<AudioSource>().clip;
            AudioClip hurt = MonoSingleton<NewMovement>.Instance.hurtScreen.GetComponent<AudioSource>().clip;
            AudioClip jump = MonoSingleton<NewMovement>.Instance.jumpSound;
            AudioClip dash = MonoSingleton<NewMovement>.Instance.dodgeSound;

            AudioSource spawnS = new GameObject("SpawnNoise").AddComponent<AudioSource>();
            AudioSource deathS = new GameObject("DeathNoise").AddComponent<AudioSource>();
            AudioSource hurtS = new GameObject("HurtNoise").AddComponent<AudioSource>();
            AudioSource jumpS = new GameObject("JumpNoise").AddComponent<AudioSource>();
            AudioSource dashS = new GameObject("DashNoise").AddComponent<AudioSource>();

            spawnS.transform.SetParent(v2Rig.transform, false);
            deathS.transform.SetParent(v2Rig.transform, false);
            hurtS.transform.SetParent(v2Rig.transform, false);
            jumpS.transform.SetParent(v2Rig.transform, false);
            dashS.transform.SetParent(v2Rig.transform, false);

            spawnS.clip = spawn;
            deathS.clip = death;
            hurtS.clip = hurt;
            jumpS.clip = jump;
            dashS.clip = dash;

            spawnS.spatialBlend = 1;
            deathS.spatialBlend = 1;
            hurtS.spatialBlend = 1;
            jumpS.spatialBlend = 1;
            dashS.spatialBlend = 1;

            GameObject[] weapons = v2Rig.GetComponent<V2>().weapons;
            Transform headT = v2Rig.GetComponent<V2>().aimAtTarget[0];
            HeadRotate headR = v2Rig.transform.Find("v2_combined").gameObject.AddComponent<HeadRotate>();
            headR.head = headT;

            Destroy(v2Rig.GetComponent<EnemyIdentifier>());
            Destroy(v2Rig.GetComponent<V2>());
            Destroy(v2Rig.transform.Find("v2_combined").Find("v2_mdl").GetComponent<EnemySimplifier>());
            SkinnedMeshRenderer smr = v2Rig.transform.Find("v2_combined").Find("v2_mdl").GetComponent<SkinnedMeshRenderer>();

            v2Rig.tag = "Untagged";
            v2Rig.layer = 0;

            EnsureAllObjectsAreCleaned(v2Rig.transform.Find("v2_combined").Find("metarig"), NetworkManager.Instance.CurrentLobby.Owner.Id == id);

            v2Rig.transform.Find("v2_combined").gameObject.tag = "Untagged";
            v2Rig.transform.Find("v2_combined").gameObject.layer = 0;

            foreach (Transform c in v2Rig.transform)
            {
                if (c.name == "Sphere")
                {
                    Destroy(c.gameObject);
                }
            }
            GameObject nameT = v2Rig.transform.Find("NameUI").gameObject;
            NameTag NameTag = nameT.AddComponent<NameTag>();
            NameTag.Init(id, name, v2Rig.transform);

            Animator animator = v2Rig.GetComponentInChildren<Animator>();
            Animator armAnim = v2Rig.transform.Find("v2_combined").GetComponentsInChildren<Animator>()[1];
            if (id == SteamClient.SteamId.Value)
            {
                v2Rig.transform.Find("v2_combined").gameObject.SetActive(false);
                nameT.SetActive(false);
            }
            Material[] mats = smr.materials;
            mats[0].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
            mats[1].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
            mats[2].shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
            smr.materials = mats;
            SkinnedMeshRenderer[] armsStuff = armAnim.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var a in armsStuff)
            {
                Material[] mats1 = a.materials;
                foreach (var m in mats1)
                {
                    m.shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                }
                a.materials = mats1;
            }
            foreach (var w in weapons)
            {
                SkinnedMeshRenderer s = w.GetComponentInChildren<SkinnedMeshRenderer>();
                Material[] mats2 = s.materials;
                foreach (var m in mats2)
                {
                    m.shader = MonoSingleton<DefaultReferenceManager>.Instance.masterShader;
                }
                s.materials = mats2;
            }
            NetworkPlayer plr = v2Rig.AddComponent<NetworkPlayer>();
            plr.NameTag = NameTag;
            plr.spawnNoise = spawnS;
            plr.deathNoise = deathS;
            plr.hurtNoise = hurtS;
            plr.jumpNoise = jumpS;
            plr.dashNoise = dashS;
            plr.animator = animator;
            plr.armAnimator = armAnim;
            plr.weapons = weapons;
            plr.head = headR;
            plr.mainRenderer = smr;
            plr.Init(id, name);
            return plr;
        }
        public static void EnsureAllObjectsAreCleaned(Transform t, bool local)
        {
            foreach(Transform c in t)
            {
                c.tag = "Floor";
                c.gameObject.layer = 0;
                if(c.GetComponent<Collider>() != null && local)
                {
                    c.GetComponent<Collider>().enabled = false;
                }
                if(c.GetComponent<EnemyIdentifierIdentifier>() != null)
                {
                    Destroy(c.GetComponent<EnemyIdentifierIdentifier>());
                }
                if(c.childCount > 0)
                {
                    EnsureAllObjectsAreCleaned(c, local);
                    continue;
                }
            }
        }
        public static void ToggleCols(Transform t, bool value)
        {
            foreach (Collider col in t.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = value;
            }
        }

        public static void ToggleColsForAll(bool value)
        {
            foreach (var plr in NetworkManager.players)
            {
                ToggleCols(plr.Value.transform, value);
            }
        }
    }
}

