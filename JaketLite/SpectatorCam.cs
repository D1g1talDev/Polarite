using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Polarite.Multiplayer;

namespace Polarite
{
    public class SpectatorCam : MonoBehaviour
    {
        [Header("Follow Settings")]
        public Vector3 followOffset = new Vector3(0, 2f, -4f);
        public float distance = 5f;
        public float height = 2f;

        private List<Transform> playerTargets = new List<Transform>();
        private int currentIndex = 0;
        public static bool isSpectating;

        private float yaw = 0f;
        private float pitch = 15f;

        private Camera spectatorCamera;

        // UI
        private Canvas spectatorCanvas;
        private TextMeshProUGUI spectatingText;
        private TextMeshProUGUI controlsText;

        private void Awake()
        {
            spectatorCamera = GetComponent<Camera>();
            CreateSpectatorUI();
        }

        private void LateUpdate()
        {
            if (!isSpectating || playerTargets.Count == 0)
                return;

            HandleCameraRotation();

            Transform target = playerTargets[currentIndex];
            if (target == null) NextPlayer();

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

            Vector3 desiredPosition = target.position + rotation * new Vector3(0, height, -distance);

            transform.position = desiredPosition;

            transform.rotation = rotation;

            UpdateUI(target);

            if (Input.GetMouseButtonDown(0))
            {
                NextPlayer();
            }
        }


        private void HandleCameraRotation()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX;
            pitch -= mouseY;

            pitch = Mathf.Clamp(pitch, -10f, 60f);
        }

        public void StartSpectating(List<Transform> targets)
        {
            if (targets == null || targets.Count == 0)
                return;

            playerTargets = targets;
            currentIndex = 0;
            isSpectating = true;
            spectatorCamera.enabled = true;

            if (spectatorCanvas != null)
                spectatorCanvas.enabled = true;

            Vector3 dir = (targets[0].position - transform.position).normalized;
            yaw = Quaternion.LookRotation(dir).eulerAngles.y;
        }

        public void StopSpectating()
        {
            isSpectating = false;
            spectatorCamera.enabled = false;
            playerTargets.Clear();

            if (spectatorCanvas != null)
                spectatorCanvas.enabled = false;
        }

        public void NextPlayer()
        {
            if (playerTargets.Count == 0) return;
            currentIndex = (currentIndex + 1) % playerTargets.Count;
        }

        private void UpdateUI(Transform target)
        {
            if (spectatingText != null)
            {
                spectatingText.text = $"Spectating: {target.GetComponent<NetworkPlayer>().PlayerName}";
            }

            if (controlsText != null)
            {
                controlsText.text = "<color=orange>LMB</color> to change player";
            }
        }

        private void CreateSpectatorUI()
        {
            GameObject canvasObj = new GameObject("SpectatorCanvas");
            spectatorCanvas = canvasObj.AddComponent<Canvas>();
            spectatorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject spectatingObj = new GameObject("SpectatingText");
            spectatingObj.transform.SetParent(canvasObj.transform, false);
            spectatingText = spectatingObj.AddComponent<TextMeshProUGUI>();
            spectatingText.alignment = TextAlignmentOptions.Center;
            spectatingText.fontSize = 64;
            spectatingText.color = Color.white;
            spectatingText.enableWordWrapping = false;
            spectatingText.font = OptionsManager.Instance.optionsMenu.transform.GetComponentInChildren<TextMeshProUGUI>().font;
            spectatingText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            spectatingText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            spectatingText.rectTransform.pivot = new Vector2(0.5f, 1f);
            spectatingText.rectTransform.anchoredPosition = new Vector2(0, -40);

            GameObject controlsObj = new GameObject("ControlsText");
            controlsObj.transform.SetParent(canvasObj.transform, false);
            controlsText = controlsObj.AddComponent<TextMeshProUGUI>();
            controlsText.alignment = TextAlignmentOptions.Center;
            controlsText.fontSize = 64;
            controlsText.color = Color.gray;
            controlsText.enableWordWrapping = false;
            controlsText.font = OptionsManager.Instance.optionsMenu.transform.GetComponentInChildren<TextMeshProUGUI>().font;
            controlsText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            controlsText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            controlsText.rectTransform.pivot = new Vector2(0.5f, 0f);
            controlsText.rectTransform.anchoredPosition = new Vector2(0, 40);

            spectatorCanvas.enabled = false;
        }
    }
}
