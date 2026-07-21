using Polarite.Debugging;
using Polarite.Networking.Skins;
using Polarite.Patches;
using Polarite.VoiceChat;
using ScriptableObjects;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using ULTRAKILL.Enemy;
using ULTRAKILL.Portal;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.Localization.Pseudo;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ScriptableObjects.FootstepSet;
using Random = UnityEngine.Random;

namespace Polarite.Multiplayer
{
    public class NetworkPlayer : MonoBehaviour, ITarget
    {
        public ulong SteamId { get; private set; }
        public string PlayerName { get; private set; }

        public int Id => GetInstanceID();

        public TargetType Type => TargetType.PLAYER;

        public bool isPlayer => false;
        public bool isEnemy => false;

        public EnemyIdentifier EID => null;

        public GameObject GameObject => gameObject;

        public Rigidbody Rigidbody => null;
        public Transform Transform => transform;

        public Vector3 Position => transform.position;

        public Vector3 HeadPosition => head.head.position;

        public NameTag NameTag;

        public bool testPlayer;

        public AudioSource spawnNoise, deathNoise, hurtNoise, jumpNoise, dashNoise;

        public Animator animator;

        public Animator armAnimator, weaponAnimator;

        public AudioSource spinSound;

        public GameObject[] weapons;

        public HeadRotate head;

        public SkinnedMeshRenderer mainRenderer;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Quaternion headTargetRotation;

        private Vector3 previousPosition;

        private readonly float lerpSpeed = 20f;

        public static NetworkPlayer LocalPlayer;

        public static bool selfIsGhost = false, hadLocPlr;

        public bool isGhost = false;

        public bool rigActive;

        public Coroutine updatePos;

        public Transform holderObject;

        public Skin currentSkin = new Skin();

        public TextMeshProUGUI namePlate;
        public Image showWhenHidden;

        public bool forceDisplayHiddenIndi = false;

        public bool typing, sliding = false;

        // for targetting purposes

        public TargetData customData;

        public static bool Shopping = false;
        public bool shopping = false, currentlySpinning = false;
        public Vector3 gravOffset;
        public Vector3 gravity = new Vector3(0, -40, 0);
        public Vector3 rbVel, dodge;
        public bool wall, ground;

        private GameObject fallingPart, slidingPart;
        public GameObject slideScrape;
        public GameObject wallScrape;

        // footsteps
        private int lastStep = -1;

        public void SpawnNoise()
        {
            SpawnSound(spawnNoise.clip);
            ToggleRig(true);
        }
        public void DeathNoise()
        {
            SpawnSound(deathNoise.clip);
            GameObject blood = Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Particles/Blood/BS Head.prefab").WaitForCompletion(), transform.position, Quaternion.identity);
            GameObject ragdoll = Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("DeathRagdoll"), transform.position, transform.rotation);
            blood.SetActive(true);
            ragdoll.AddComponent<Ragdoll>().SetValues(currentSkin, SteamId);
            ToggleRig(false);
            ragdoll.GetComponentInChildren<Rigidbody>().velocity = rbVel;
        }
        public void HurtNoise()
        {
            SpawnSound(hurtNoise.clip);
        }
        public void JumpNoise()
        {
            SpawnSound(jumpNoise.clip);
            JumpAnim();
        }
        public void SetFalling(bool value)
        {
            if(value && fallingPart == null)
            {
                fallingPart = Instantiate(MonoSingleton<NewMovement>.Instance.fallParticle, transform);
            }
            else if(fallingPart != null)
            {
                Destroy(fallingPart);
            }
        }

        public void SetGhost(bool val)
        {
            isGhost = val;
            if (val)
            {
                ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("GhostTransform2"), Random.Range(0.95f, 1.15f), MonoSingleton<CameraController>.Instance.transform, 1f);
            }
        }
        public void SpawnSound(AudioClip clip)
        {
            if (isGhost)
            {
                return;
            }
            ItePlugin.SpawnSound(clip, 1f, head.transform, 1f);
        }
        public void Footsteps(Vector3 pos, float volume)
        {
            if(isGhost)
            {
                return;
            }    
            if(MonoSingleton<SceneHelper>.Instance.TryGetSurfaceData(pos, out var hit) && PlayerFootsteps.Instance.footstepSet.TryGetFootstepClips(hit.surfaceType, out var clips))
            {
                ActualFootsteps(clips, pos, volume);
            }
        }
        public void ActualFootsteps(AudioClip[] clips, Vector3 pos, float volume)
        {
            if(clips != null && clips.Length > 0)
            {
                int i = Random.Range(0, clips.Length);
                if(clips.Length > 1 && i == lastStep)
                {
                    i = (i + 1) % clips.Length;
                }
                lastStep = i;
                ItePlugin.SpawnSound(clips[i], 1f, null, volume, pos);
            }
        }

        public void Init(ulong steamId, string playerName)
        {
            SteamId = steamId;
            PlayerName = playerName;
            name = (steamId != NetworkManager.Id) ? $"NetworkPlayer_{playerName}_{steamId}" : "LocalPlayer";
            if (SteamId == NetworkManager.Id)
            {
                updatePos = StartCoroutine(UpdatePos());
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            if (LocalPlayer == null && steamId == NetworkManager.Id)
            {
                LocalPlayer = this;
                hadLocPlr = true;
            }
            DontDestroyOnLoad(gameObject);
            SpawnNoise();

            customData = new TargetData();
            customData.ResetToDefault();
            customData.handle = new TargetHandle(this);
            foreach (var w in weapons)
            {
                foreach (Transform par in w.transform)
                {
                    par.gameObject.SetActive(false);
                }
            }
            UpdGravOff();
        }
        private void OnSceneLoaded(Scene args, LoadSceneMode args2)
        {
            updatePos = StartCoroutine(UpdatePos());
        }
        private void UpdGravOff()
        {
            gravOffset = transform.rotation * new Vector3(0f, -1.5f, 0f);
        }

        public IEnumerator UpdatePos()
        {
            Transform transform;
            if (MonoSingleton<NewMovement>.Instance != null)
            {
                transform = MonoSingleton<NewMovement>.Instance.transform;
            }
            else
            {
                transform = null;
            }
            while (true)
            {
                if(transform == null)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(0.1f);
                try
                {
                    bool sliding = MonoSingleton<NewMovement>.Instance.sliding;
                    bool grounded = !MonoSingleton<NewMovement>.Instance.gc.onGround;
                    bool onWall = MonoSingleton<NewMovement>.Instance.wcGroup.OnWall();
                    bool walking = MonoSingleton<NewMovement>.Instance.walking;
                    bool spin = TryGetSpinning();
                    bool shop = Shopping;
                    Quaternion rotation = (sliding) ? Quaternion.LookRotation(MonoSingleton<NewMovement>.Instance.rb.velocity) : MonoSingleton<CameraController>.Instance.transform.rotation;
                    PacketWriter writer = new PacketWriter();
                    Vector3 pos = new Vector3(transform.position.x, (sliding) ? transform.position.y : (transform.position + gravOffset).y, transform.position.z);
                    Quaternion rot = new Quaternion(MonoSingleton<CameraController>.Instance.transform.rotation.x, rotation.y, MonoSingleton<CameraController>.Instance.transform.rotation.z, MonoSingleton<CameraController>.Instance.transform.rotation.w);
                    Quaternion rot2 = new Quaternion(MonoSingleton<CameraController>.Instance.transform.localRotation.x, MonoSingleton<CameraController>.Instance.transform.localRotation.y, MonoSingleton<CameraController>.Instance.transform.localRotation.z, MonoSingleton<CameraController>.Instance.transform.localRotation.w);
                    writer.WriteVector3(pos);
                    writer.WriteQuaternion(rot);
                    writer.WriteQuaternion(rot2);
                    writer.WriteBool(sliding);
                    writer.WriteBool(grounded);
                    writer.WriteBool(walking);
                    writer.WriteBool(spin);
                    writer.WriteBool(shop);
                    writer.WriteBool(MonoSingleton<NewMovement>.Instance.currentFallParticle != null);

                    writer.WriteInt(MonoSingleton<NewMovement>.Instance.hp);
                    writer.WriteBool(ChatUI.isActuallyTyping);
                    writer.WriteVector3(MonoSingleton<NewMovement>.Instance.dodgeDirection);
                    writer.WriteVector3(MonoSingleton<NewMovement>.Instance.rb.velocity);
                    writer.WriteBool(onWall);

                    NetworkManager.Instance.BroadcastPacket(PacketType.Transform, writer.GetBytes(), sendtype: SendTypeConsts.ST_PLRSTATE);
                }
                catch (Exception)
                {
                    NetworkManager.LocPlayerCheck();
                }
            }
        }

        public void SetTargetTransform(Vector3 pos, Quaternion rot, Quaternion rot2)
        {
            previousPosition = transform.position;

            targetPosition = pos;
            targetRotation = rot;
            headTargetRotation = rot2;
        }
        public void SetAnimation(bool slide, bool air, bool walk, bool spin)
        {
            sliding = slide;
            ground = !air;
            if (spin && !currentlySpinning)
            {
                weaponAnimator.SetTrigger("Spin");
                spinSound.mute = false;
                currentlySpinning = true;
            }
            if (!spin && currentlySpinning)
            {
                weaponAnimator.ResetTrigger("Spin");
                spinSound.mute = true;
                currentlySpinning = false;
            }
            animator.SetBool("Sliding", slide);
            animator.SetBool("InAir", air);
            if (!air)
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
        public void SlideScrape(GameObject part, Vector3 dodgeDir)
        {
            if (slideScrape != null) Detach(false);
            slideScrape = Instantiate(part, transform.position + dodgeDir * 2f, Quaternion.LookRotation(-dodgeDir));
        }
        public void SlidePart(bool sliding, Vector3 dodgeDir)
        {
            if(sliding)
            {
                if (slidingPart != null) Destroy(slidingPart);
                slidingPart = Instantiate(MonoSingleton<NewMovement>.Instance.slideParticle, transform.position + dodgeDir * 10f, Quaternion.LookRotation(-dodgeDir));
            }
            else if(slidingPart != null)
            {
                Destroy(slidingPart);
            }
        }
        public void WallScrape(GameObject part, Vector3 pos)
        {
            if (wallScrape != null) Detach(true);
            wallScrape = Instantiate(part, pos, Quaternion.identity);
        }
        public void Detach(bool wall)
        {
            if (wall)
            {
                MonoSingleton<NewMovement>.Instance.DetachScrape(wallScrape);
                wallScrape = null;
            }
            else
            {
                MonoSingleton<NewMovement>.Instance.DetachScrape(slideScrape);
                slideScrape = null;
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
        public void SetWeapon(bool alt, int type)
        {
            foreach (var w in weapons)
            {
                foreach (Transform par in w.transform)
                {
                    par.gameObject.SetActive(false);
                }
            }
            GameObject parent = weapons[(alt) ? 1 : 0];
            parent.transform.GetChild(type).gameObject.SetActive(true);
            MasterShaderizer.MasterShaderize(parent.transform.GetChild(type).GetComponentInChildren<SkinnedMeshRenderer>());
            SelectAnim();
            GunSync.Cutoff(SteamId);
        }
        public void CoinAnim()
        {
            if (NetworkManager.SceneLoading) return;
            armAnimator.SetBool("Idle", false);
            armAnimator.SetBool("Nerd", false);
            CancelInvoke(nameof(GoBackToIdle));
            CancelInvoke(nameof(GoBackToIdleShop));
            armAnimator.SetTrigger("Coin");
            Invoke(nameof(GoBackToIdle), 0.7f);
            Invoke(nameof(GoBackToIdleShop), 0.7f);
        }
        public void PunchAnim()
        {
            if (NetworkManager.SceneLoading) return;
            armAnimator.SetBool("Idle", false);
            armAnimator.SetBool("Nerd", false);
            CancelInvoke(nameof(GoBackToIdle));
            CancelInvoke(nameof(GoBackToIdleShop));
            armAnimator.SetTrigger("Punch");
            Invoke(nameof(GoBackToIdle), 0.967f);
            Invoke(nameof(GoBackToIdleShop), 0.967f);
        }
        public void WhipAnim()
        {
            if (NetworkManager.SceneLoading) return;
            armAnimator.SetBool("Idle", false);
            armAnimator.SetBool("Nerd", false);
            CancelInvoke(nameof(GoBackToIdle));
            CancelInvoke(nameof(GoBackToIdleShop));
            armAnimator.SetTrigger("Whiplash");
            Invoke(nameof(GoBackToIdle), 1);
            Invoke(nameof(GoBackToIdleShop), 1);
        }
        public void ShootAnim()
        {
            weaponAnimator.SetTrigger("WeaponShoot");
        }
        public void SelectAnim()
        {
            weaponAnimator.SetTrigger("WeaponSelect");
        }
        public void GoBackToIdle()
        {
            armAnimator.SetBool("Idle", true);
        }
        public void SceneLoading()
        {
            armAnimator.SetBool("Nerd", false);
            armAnimator.SetBool("Idle", true);
            transform.rotation = Quaternion.identity;
            gravity = new Vector3(0, -40, 0);
            UpdGravOff();
        }
        public void GoBackToIdleShop()
        {
            if (NetworkManager.SceneLoading) return;
            if (!shopping)
            {
                return;
            }
            armAnimator.SetBool("Nerd", true);
        }
        public void ShopMode(bool shop)
        {
            if (NetworkManager.SceneLoading)
            {
                armAnimator.SetBool("Nerd", false);
                armAnimator.SetBool("Idle", true);
                return;
            }
            if (shopping == shop)
            {
                return;
            }
            shopping = shop;
            if (shop)
            {
                armAnimator.SetBool("Nerd", true);
                armAnimator.SetBool("Idle", false);
            }
            else
            {
                armAnimator.SetBool("Nerd", false);
                armAnimator.SetBool("Idle", true);
            }
        }
        public void TapAnim()
        {
            if (NetworkManager.SceneLoading) return;
            armAnimator.SetBool("Nerd", false);
            CancelInvoke(nameof(GoBackToIdleShop));
            armAnimator.SetTrigger("Tap");
            Invoke(nameof(GoBackToIdleShop), 0.450f);
        }
        public bool TryGetSpinning()
        {
            try
            {
                if (!MonoSingleton<GunControl>.Instance.currentWeapon.TryGetComponent<Revolver>(out var rev))
                {
                    return false;
                }
                return rev.gunVariation == 2 && rev.chargingPierce;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Update()
        {
            if (updatePos == null && SteamId == NetworkManager.Id)
            {
                updatePos = StartCoroutine(UpdatePos());
            }
            if (testPlayer)
            {
                Quaternion rotation = (NewMovement.Instance.sliding) ? Quaternion.LookRotation(MonoSingleton<NewMovement>.Instance.rb.velocity) : MonoSingleton<CameraController>.Instance.transform.rotation;
                SetTargetTransform(new Vector3(MonoSingleton<NewMovement>.Instance.transform.position.x, (NewMovement.Instance.sliding) ? MonoSingleton<NewMovement>.Instance.transform.position.y : (MonoSingleton<NewMovement>.Instance.transform.position + gravOffset).y, MonoSingleton<NewMovement>.Instance.transform.position.z), new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w), new Quaternion(MonoSingleton<CameraController>.Instance.transform.localRotation.x, MonoSingleton<CameraController>.Instance.transform.localRotation.y, MonoSingleton<CameraController>.Instance.transform.localRotation.z, MonoSingleton<CameraController>.Instance.transform.localRotation.w));
                SetAnimation(NewMovement.Instance.sliding, !NewMovement.Instance.gc.onGround, NewMovement.Instance.walking, TryGetSpinning());
                SetHP(NewMovement.Instance.hp);
                ShopMode(Shopping);
            }
            if (isGhost)
            {
                ToggleRig(false);
            }
            NameTag.dummy = this == LocalPlayer;
            if (Vector3.Distance(transform.position, targetPosition) > 10f)
            {
                transform.position = targetPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * lerpSpeed);
            }
            SlidePosStuff();
            Transform rig = transform.Find("v2_combined");
            Vector3 currentEuler = rig.localRotation.eulerAngles;

            Vector3 targetEuler = targetRotation.eulerAngles;

            float newY = Mathf.LerpAngle(currentEuler.y, targetEuler.y, Time.unscaledDeltaTime * lerpSpeed);

            Quaternion newRotation = Quaternion.Euler(currentEuler.x, newY, currentEuler.z);

            rig.localRotation = newRotation;

            head.targetRotation = headTargetRotation;

            customData.position = transform.position;
            customData.rotation = transform.rotation;
            customData.headPosition = head.head.position;

            CameraController gameCam = MonoSingleton<CameraController>.Instance;
            Vector3 dir = (showWhenHidden.transform.position - gameCam.GetDefaultPos());
            showWhenHidden.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            showWhenHidden.transform.parent.gameObject.layer = SpecialHudCameraAddPatch.mask;
            showWhenHidden.gameObject.layer = SpecialHudCameraAddPatch.mask;
            Graphic imgGraph = showWhenHidden;
            try
            {
                if(LocalPlayer == this && !ItePlugin.debugMode)
                {
                    imgGraph.color = Color.clear;
                    return;
                }
                if (PortalPhysicsV2.Raycast(gameCam.GetDefaultPos(), dir, Vector3.Distance(gameCam.GetDefaultPos(), showWhenHidden.transform.position), LayerMaskDefaults.Get(LMD.Environment)) && rigActive && !ItePlugin.disableHI.value && !SpectatorCam.isSpectating)
                {
                    imgGraph.color = Color.Lerp(imgGraph.color, new Color(1f, 1f, 1f, 1f), 10f * Time.deltaTime);
                }
                else
                {
                    imgGraph.color = Color.Lerp(imgGraph.color, new Color(1f, 1f, 1f, !rigActive ? 0.5f : 0f), 10f * Time.deltaTime);
                }
            }
            catch (Exception e)
            {
                Logs.DebugError("[Hidden indicator] Error: " + e.Message, this);
                imgGraph.color = Color.Lerp(imgGraph.color, new Color(1f, 1f, 1f, !rigActive ? 0.5f : 0f), 10f * Time.deltaTime);
            }
        }
        private void SlidePosStuff()
        {
            if(!sliding)
            {
                return;
            }
            Vector3 normal = Vector3.ProjectOnPlane(rbVel.normalized, transform.up).normalized;
            if(slidingPart != null)
            {
                slidingPart.transform.position = transform.position + normal * 10f;
                slidingPart.transform.forward = -dodge;
            }
            if (slideScrape != null)
            {
                if (ground || wall)
                {
                    slideScrape.transform.position = transform.position + normal;
                    slideScrape.transform.forward = -normal;
                }
                else
                {
                    slideScrape.transform.position = Vector3.one * 5000f;
                }
            }
        }

        public void SetHP(int hp)
        {
            NameTag.SetHP(hp);
        }
        public void UpdateSkin(Skin skin)
        {
            currentSkin = skin;
            SkinManagerV2.CustomColor(mainRenderer, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.V1_BASE_MASK, "Base" + SteamId, 0);
            SkinManagerV2.CustomColor(mainRenderer, skin.Base, skin.WingLight, skin.Metal, skin.Shinyness, MaskConsts.V1_WING_MASK, "Wing" + SteamId, 1);
            SkinManagerV2.CustomColor(mainRenderer, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.KNUCKLEBLASTER_MASK, "KB" + SteamId, 2);

            if (namePlate != null)
            {
                namePlate.text = skin.Nameplate;
                namePlate.color = skin.NameplateColor;
            }

            ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("SkinChange"), 1f, MonoSingleton<CameraController>.Instance.transform, 1f);
            SkinnedMeshRenderer[] allMats = head.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var a in allMats)
            {
                if (a.name == "Feedbacker")
                {
                    SkinManagerV2.CustomColor(a, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.FEEDBACKER_MASK, "Feedbacker" + NetworkManager.Id, 0);
                }
                if (a.name == "Arm" || a.name == "Hook")
                {
                    SkinManagerV2.CustomColor(a, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.WHIPLASH_MASK, "Whip" + NetworkManager.Id, 0);
                }
            }
            // Additionally apply shader to all renderers under the specified path
            try
            {
                Transform root = transform.Find("v2_combined/metarig/spine/spine.001/spine.002/spine.003/shoulder.R/upper_arm.R/forearm.R/hand.R");
                if (root != null)
                {
                    ApplyShaderRecursively(root, MonoSingleton<DefaultReferenceManager>.Instance.masterShader);
                }
            }
            catch (Exception e)
            {
                Logs.Warn("Failed applying shader to rig path: " + e, this);
            }
            SkinManagerV2.SetIcon(SteamId);
            VoiceUI.RefreshIcons(ItePlugin.useSkinInsteadOfPFP.value);
            if(SkinManagerV2.Previews.TryGetValue(SteamId, out var icon)) showWhenHidden.sprite = icon;
        }

        public static void SetSkinOfRagdoll(SkinnedMeshRenderer rend, Skin skin, ulong target)
        {
            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (i == 0)
                {
                    SkinManagerV2.CustomColor(rend, skin.Base, Color.black, skin.Metal, skin.Shinyness, MaskConsts.V1_BASE_MASK, "BaseDead" + target, i);
                }
                else
                {
                    SkinManagerV2.CustomColor(rend, skin.Base, Color.black, skin.Metal, skin.Shinyness, MaskConsts.V1_WING_MASK, "WingDead" + target, i);
                }
            }
            rend.materials = mats;
        }

        private void ApplyShaderRecursively(Transform node, Shader shader)
        {
            if (node == null || shader == null) return;
            // apply to Renderer (MeshRenderer) if present
            var renderer = node.GetComponent<Renderer>();
            if (renderer != null && renderer.materials != null)
            {
                // use new master shaderizer
                MasterShaderizer.MasterShaderize(renderer);
            }
            // apply to SkinnedMeshRenderer
            var sk = node.GetComponent<SkinnedMeshRenderer>();
            if (sk != null && sk.materials != null)
            {
                // use new master shaderizer
                MasterShaderizer.MasterShaderize(sk);
            }

            for (int i = 0; i < node.childCount; i++)
            {
                ApplyShaderRecursively(node.GetChild(i), shader);
            }
        }

        public void HandleFriendlyFire(ulong whoDidIt, int damage)
        {
            if (NetworkManager.Instance.CurrentLobby.GetData("pvp") == "0")
            {
                return;
            }
            if (damage == 0)
            {
                damage = 1;
            }
            PacketWriter w = new PacketWriter();
            w.WriteULong(whoDidIt);
            w.WriteInt(damage);
            NetworkManager.Instance.SendPacket(PacketType.PVP, w.GetBytes(), SteamId);
            if (this == LocalPlayer)
            {
                // for testing
                DoFriendlyDamage(whoDidIt, damage);
            }
        }

        public static NetworkPlayer Find(ulong id)
        {
            if (id == NetworkManager.Id)
            {
                return LocalPlayer;
            }
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
                NetworkManager.players.Remove(id);
            }
            GameObject v2Rig = GameObject.Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("NetworkRig"));
            v2Rig.GetComponent<Collider>().enabled = false;

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
            Transform armT = v2Rig.GetComponent<V2>().aimAtTarget[1];
            HeadRotate headR = v2Rig.transform.Find("v2_combined").gameObject.AddComponent<HeadRotate>();
            headR.head = headT;
            headR.arm = armT;

            Transform holder = v2Rig.transform.Find("v2_combined/metarig/spine/spine.001/spine.002/spine.003/Arms/Feedbacker/Armature/UpperArm/Forearm/Hand/HoldPos");
            Transform nameplate = v2Rig.transform.Find("v2_combined/metarig/spine/spine.001/spine.002/spine.003/NameplateCanvas/Nameplate");
            Transform showHidden = v2Rig.transform.Find("ShowWhenHidden/PlrIndi");

            TextMeshProUGUI tmp = null;
            if (nameplate != null)
            {
                tmp = nameplate.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = ItePlugin.namePlate.value;
                }
            }

            EnsureAllObjectsAreCleaned(v2Rig.transform, id == NetworkManager.Id);

            Destroy(v2Rig.GetComponent<EnemyIdentifier>());
            Destroy(v2Rig.GetComponent<V2>());
            Destroy(v2Rig.transform.Find("v2_combined").Find("v2_mdl").GetComponent<EnemySimplifier>());
            SkinnedMeshRenderer smr = v2Rig.transform.Find("v2_combined").Find("v2_mdl").GetComponent<SkinnedMeshRenderer>();

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
            Animator wepAnim = v2Rig.transform.Find("v2_combined").GetComponentsInChildren<Animator>()[1];
            Animator armAnim = v2Rig.transform.Find("v2_combined").GetComponentsInChildren<Animator>()[2];
            AudioSource wepNoise = wepAnim.GetComponent<AudioSource>();
            if (id == NetworkManager.Id)
            {
                v2Rig.transform.Find("v2_combined").gameObject.SetActive(false);
                nameT.SetActive(false);
            }
            NetworkPlayer plr = v2Rig.AddComponent<NetworkPlayer>();
            PlayerRocketManager rm = v2Rig.AddComponent<PlayerRocketManager>();
            rm.owner = id;
            plr.NameTag = NameTag;
            plr.spawnNoise = spawnS;
            plr.deathNoise = deathS;
            plr.hurtNoise = hurtS;
            plr.jumpNoise = jumpS;
            plr.dashNoise = dashS;
            plr.animator = animator;
            plr.armAnimator = armAnim;
            plr.weaponAnimator = wepAnim;
            plr.spinSound = wepNoise;
            plr.weapons = weapons;
            plr.head = headR;
            plr.mainRenderer = smr;
            plr.holderObject = holder;
            plr.namePlate = tmp;
            plr.showWhenHidden = showHidden.GetComponent<Image>();
            plr.Init(id, name);

            Logs.Info($"Created player {name} with ID {id}", name: "NetworkPlayer");
            return plr;
        }
        public void ToggleRig(bool value)
        {
            if (this == LocalPlayer && !testPlayer)
            {
                transform.Find("v2_combined").gameObject.SetActive(false);
                NameTag.gameObject.SetActive(false);
                return;
            }
            transform.Find("v2_combined").gameObject.SetActive(value);
            NameTag.gameObject.SetActive(value);
            rigActive = value;
        }
        public void ClearHolder()
        {
            foreach (Transform t in holderObject)
            {
                Destroy(t.gameObject);
            }
            SceneLoading();
        }
        public void PortalRotate(Vector3 grav)
        {
            gravity = grav;
            Transform rig = transform.Find("v2_combined");
            Quaternion quaternion = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, grav.normalized), -grav.normalized);
            transform.rotation = quaternion * Quaternion.Euler(-rig.localRotation.x, rig.localRotation.y, rig.localRotation.z);
            UpdGravOff();
        }
        public static void EnsureAllObjectsAreCleaned(Transform t, bool local)
        {
            foreach (Transform c in t)
            {
                if (c.name == "StandableCube")
                {
                    continue;
                }
                t.tag = "Floor";
                t.gameObject.layer = 0;
                if (c.GetComponent<EnemyIdentifierIdentifier>() != null)
                {
                    Destroy(c.GetComponent<EnemyIdentifierIdentifier>());
                }
                if (c.childCount > 0)
                {
                    EnsureAllObjectsAreCleaned(c, local);
                    continue;
                }
            }
        }
        public static void ToggleCols(Transform t, bool value)
        {
            /*
            foreach (Collider col in t.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = value;
            }
            */
        }


        public static void ToggleColsForAll(bool value)
        {
            foreach (var plr in NetworkManager.players)
            {
                ToggleCols(plr.Value.transform, value);
            }
        }

        public static void ToggleEid(Transform t, bool value)
        {
            /*
            EnemyIdentifier eid = t.GetComponent<EnemyIdentifier>();
            eid.enabled = value;
            eid.dead = false;
            eid.health = Mathf.Infinity;
            eid.beenGasolined = false;
            */
        }
        public static void ToggleEidForAll(bool value)
        {
            /*
            foreach (var plr in NetworkManager.players)
            {
                ToggleEid(plr.Value.transform, value);
            }
            */
        }

        public static void DoFriendlyDamage(ulong whoDidIt, int damage)
        {
            MonoSingleton<NewMovement>.Instance.GetHurt(damage, false);
            DeadPatch.Death("was friendly fired by ", whoDidIt, true);
        }

        public void SetData(ref TargetData data)
        {
            data.position = transform.position;
            data.rotation = transform.rotation;
            data.headPosition = head.head.position;
        }

        public void UpdateCachedTransformData()
        {
            // not implemented
        }

        public static bool IsPlayer(GameObject obj)
        {
            return obj.GetComponent<NetworkPlayer>() != null || obj.GetComponent<NewMovement>() != null;
        }


        // move these here
        public static bool LastPlayerAlive()
        {
            int alive = 0;
            foreach (NetworkPlayer p in NetworkManager.players.Values)
            {
                if (!p.isGhost)
                {
                    alive++;
                }
            }
            return alive <= 1;
        }
        public static int PlayersAlive()
        {
            int alive = 0;
            foreach (NetworkPlayer p in NetworkManager.players.Values)
            {
                if (!p.isGhost)
                {
                    alive++;
                }
            }
            return alive;
        }
    }
}

