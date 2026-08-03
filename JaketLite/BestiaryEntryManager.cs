using Polarite.Multiplayer;
using Polarite.Networking.Skins;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Polarite.SamTTS;

namespace Polarite
{
    public static class BestiaryEntryManager
    {
        public class IsPolarV2 : MonoBehaviour
        {
            // this class only exists as info for the server to decide which v2 to spawn
        }
        private class V2Polar : MonoBehaviour
        {
            private void Start()
            {
                GameObject newV2 = Instantiate(Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Enemies/V2.prefab").WaitForCompletion(), transform.position, transform.rotation);
                newV2.transform.SetParent(transform.parent, true);
                newV2.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                newV2.AddComponent<SkinPreviewBestiary>();
                newV2.AddComponent<IsPolarV2>();
                if(newV2.TryGetComponent<V2>(out var v2))
                {
                    v2.dontEnrage = true;
                    v2.secondEncounter = true;
                    v2.dontDie = false;
                    if (newV2.TryGetComponent<EnemyIdentifier>(out var eid))
                    {
                        eid.overrideFullName = ItePlugin.currentSkin.Nameplate;
                        eid.onDeath.AddListener(() =>
                        {
                            GameObject ragdoll = Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("DeathRagdoll"), eid.transform.position, eid.transform.rotation);
                            ragdoll.AddComponent<Ragdoll>().SetValues(ItePlugin.currentSkin, NetworkManager.Id);
                            if (ItePlugin.ttsHurtAndDeath.value && ItePlugin.canTTS.value && ItePlugin.playerSounds.value)
                            {
                                ItePlugin.DeathScream(SamPitch.configSam, ragdoll.transform);
                            }
                            Destroy(eid.gameObject);
                        });
                    }
                }
                Destroy(gameObject);
            }
        }
        public class SkinPreviewBestiary : MonoBehaviour
        {
            public TextMeshProUGUI nameplate;
            public SkinnedMeshRenderer renderer;
            public SkinnedMeshRenderer[] otherRenderers;
            public bool enemy = false;
            public ulong target = 0;

            public void Awake()
            {
                enemy = GetComponent<EnemyIdentifier>() != null;
                renderer = !enemy ? transform.Find("v2_mdl").GetComponent<SkinnedMeshRenderer>() : transform.Find("v2_combined").Find("v2_mdl").GetComponent<SkinnedMeshRenderer>();
                if (!enemy)
                {
                    otherRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    nameplate = GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
            public void OnEnable()
            {
                Skin skin = target == 0 ? ItePlugin.currentSkin : NetworkPlayer.Find(target).currentSkin;
                renderer.materials[0] = ItePlugin.mainBundle.LoadAsset<Material>("V1");
                renderer.materials[1] = ItePlugin.mainBundle.LoadAsset<Material>("V1Wing");

                SkinManagerV2.CustomColor(renderer, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.V1_BASE_MASK, "Base" + (target == 0 ? NetworkManager.Id : target), 0);
                SkinManagerV2.CustomColor(renderer, skin.Base, skin.WingLight, skin.Metal, skin.Shinyness, MaskConsts.V1_WING_MASK, "Wing" + (target == 0 ? NetworkManager.Id : target), 1);
                SkinManagerV2.CustomColor(renderer, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.KNUCKLEBLASTER_MASK, "KB" + (target == 0 ? NetworkManager.Id : target), 2);

                if(!enemy)
                {
                    foreach (SkinnedMeshRenderer rend in otherRenderers)
                    {
                        if (rend.name == "Feedbacker")
                        {
                            SkinManagerV2.CustomColor(rend, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.FEEDBACKER_MASK, "Feedbacker" + (target == 0 ? NetworkManager.Id : target), 0);
                        }
                        if (rend.name == "Arm" || rend.name == "Hook")
                        {
                            SkinManagerV2.CustomColor(rend, skin.Base, skin.Light, skin.Metal, skin.Shinyness, MaskConsts.WHIPLASH_MASK, "Whip" + (target == 0 ? NetworkManager.Id : target), 0);
                        }
                    }

                    nameplate.text = ItePlugin.currentSkin.Nameplate;
                    nameplate.color = ItePlugin.currentSkin.NameplateColor;
                }
            }
        }

        public static SpawnableObject data;
        public static SkinPreviewBestiary skinModel;
        public static V2 spawnable;

        public static void UpdateSOAndPreview()
        {
            if(data == null) data = SpawnableObject.CreateInstance("SpawnableObject") as SpawnableObject;
            data.name = "Player";
            data.description = "Up to your interpretation.";
            data.backgroundColor = Color.gray;
            if (skinModel == null) MakePreview();
            data.preview = skinModel.gameObject;
            data.identifier = "polar.player";
            data.iconKey = "polar.YOU";
            data.objectName = ItePlugin.currentSkin.Nameplate;
            data.strategy = "Up to your interpretation.";
            data.type = "Supreme Machine";
            data.gridIcon = ItePlugin.mainBundle.LoadAsset<Sprite>("YOU");
            if (spawnable == null) MakeSpawnable();
            data.gameObject = spawnable.gameObject;
            data.spawnableObjectType = SpawnableObject.SpawnableObjectDataType.Enemy;
            data.enemyType = EnemyType.V2;
        }
        private static void MakePreview()
        {
            GameObject obj = ItePlugin.mainBundle.LoadAsset<GameObject>("SkinPreviewEnemy");
            skinModel = obj.GetOrAddComponent<SkinPreviewBestiary>();
        }
        private static void MakeSpawnable()
        {
            GameObject v2 = ItePlugin.mainBundle.LoadAsset<GameObject>("V2Polar");
            spawnable = v2.GetComponent<V2>();
            v2.GetOrAddComponent<V2Polar>();
        }
    }
}
