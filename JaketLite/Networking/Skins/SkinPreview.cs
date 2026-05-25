using Polarite.Multiplayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Polarite.Networking.Skins
{
    public class SkinPreview : MonoBehaviour
    {
        public GameObject currentPreview;
        public AudioSource audioSource;
        public TextMeshProUGUI nameplate;
        public SkinnedMeshRenderer renderer;
        public SkinnedMeshRenderer[] otherRenderers;
        public bool previewing;

        public float spin;
        public float spinSpeed = 50f;
        public bool screenShotting;
        public bool doneScreenshotting = true;
        public bool canSpin = true;

        public void Start()
        {
            currentPreview = Instantiate(ItePlugin.mainBundle.LoadAsset<GameObject>("SkinPreview"));
            audioSource = currentPreview.GetComponentInChildren<AudioSource>(true);
            nameplate = currentPreview.GetComponentInChildren<TextMeshProUGUI>(true);
            renderer = currentPreview.transform.Find("v2_mdl").GetComponent<SkinnedMeshRenderer>();
            otherRenderers = currentPreview.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            currentPreview.transform.localScale = Vector3.zero;
            audioSource.pitch = 0;
        }
        public void Update()
        {
            spin += spinSpeed * Time.deltaTime;
            audioSource.loop = true;
            audioSource.mute = audioSource.pitch <= 0;
            audioSource.volume = 0.5f;
            if (!previewing && !screenShotting)
            {
                audioSource.pitch = Mathf.Lerp(audioSource.pitch, 0, Time.deltaTime * 5f);
                currentPreview.transform.localScale = Vector3.Lerp(currentPreview.transform.localScale, Vector3.zero, Time.deltaTime * 5f);
            }
            else
            {
                audioSource.pitch = (!screenShotting) ? Mathf.Lerp(audioSource.pitch, 1, Time.deltaTime * 5f) : Mathf.Lerp(audioSource.pitch, 2, Time.deltaTime * 3f);
                currentPreview.transform.localScale = Vector3.Lerp(currentPreview.transform.localScale, Vector3.one, Time.deltaTime * 5f);
                currentPreview.transform.position = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z) + transform.forward * 1.5f;
                currentPreview.transform.rotation = Quaternion.Euler(0f, spin, 0f);
            }
            if(Input.GetKeyDown(ItePlugin.screenShotSkin.value) && doneScreenshotting)
            {
                StartCoroutine(Screenshot());
            }
            SetPreview(Input.GetKey(ItePlugin.previewSkin.value));
        }
        public void SetPreview(bool value)
        {
            if(previewing == value)
            {
                return;
            }
            previewing = value;
            if(value)
            {
                UpdatePreview();
            }
        }
        public void UpdatePreview()
        {
            renderer.materials[0] = ItePlugin.mainBundle.LoadAsset<Material>("V1");
            renderer.materials[1] = ItePlugin.mainBundle.LoadAsset<Material>("V1Wing");

            SkinManagerV2.CustomColor(renderer, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.V1_BASE_MASK, "Base" + NetworkManager.Id, 0);
            SkinManagerV2.CustomColor(renderer, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.WingLight, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.V1_WING_MASK, "Wing" + NetworkManager.Id, 1);
            SkinManagerV2.CustomColor(renderer, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.KNUCKLEBLASTER_MASK, "KB" + NetworkManager.Id, 2);

            foreach (SkinnedMeshRenderer rend in otherRenderers)
            {
                if(rend.name == "Feedbacker")
                {
                    SkinManagerV2.CustomColor(rend, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.FEEDBACKER_MASK, "Feedbacker" + NetworkManager.Id, 0);
                }
                if(rend.name == "Arm" || rend.name == "Hook")
                {
                    SkinManagerV2.CustomColor(rend, ItePlugin.currentSkin.Base, ItePlugin.currentSkin.Light, ItePlugin.currentSkin.Metal, ItePlugin.currentSkin.Shinyness, MaskConsts.WHIPLASH_MASK, "Whip" + NetworkManager.Id, 0);
                }
            }

            nameplate.text = ItePlugin.currentSkin.Nameplate;
            nameplate.color = ItePlugin.currentSkin.NameplateColor;
        }
        public IEnumerator Screenshot()
        {
            NetworkManager.DisplaySystemChatMessage("Screenshotting your skin...");
            ChatUI.Instance.ShowUIForBit(1f);
            doneScreenshotting = false;
            screenShotting = true;
            UpdatePreview();
            spinSpeed = 4500f;
            yield return new WaitForSeconds(2f);
            spinSpeed = 50f;
            screenShotting = false;
            currentPreview.transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(transform.position - currentPreview.transform.position).eulerAngles.y, 0);
            SkinScreenshotter.Screenshot();
            yield return null;
            ItePlugin.SpawnSound(ItePlugin.mainBundle.LoadAsset<AudioClip>("Ding"), 1f, MonoSingleton<CameraController>.Instance.transform, 1f);
            doneScreenshotting = true;
            yield return new WaitForSeconds(0.5f);
            Application.OpenURL(SkinScreenshotter.Path);
        }
    }
}
