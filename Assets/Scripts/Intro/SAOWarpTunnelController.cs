using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Audio;

namespace BattleSummoner.Intro
{
    /// <summary>
    /// SAOWarpTunnelController coordinates the Sword Art Online "Link Start" hyperspeed dive tunnel:
    /// - Tessellating hexagonal light pillars shooting past the camera at hyperspeed.
    /// - High-speed neon light streaks zooming past camera.
    /// - Dynamic HUD telemetry texts zooming by.
    /// - Camera FOV warp distortion and high-velocity camera shake.
    /// - Hyperspace audio whoosh and rising laser chord.
    /// - Seamless deceleration into Account Induction stage.
    /// </summary>
    public class SAOWarpTunnelController : MonoBehaviour
    {
        [Header("Tessellating Hexagon Pillar System")]
        public SAOHexagonPillarTunnel hexagonPillarTunnel;

        [Header("Tunnel Elements")]
        public Renderer tunnelRenderer;
        public ParticleSystem lightStreakParticles;
        public TextMeshProUGUI[] hudTelemetryBanners;
        public CanvasGroup hudCanvasGroup;
        public Camera mainCamera;

        [Header("Settings")]
        public float tunnelDuration = 4.2f;
        public float peakSpeed = 85.0f;
        public float baseSpeed = 22.0f;

        private Material tunnelMat;
        private static readonly int SpeedId = Shader.PropertyToID("_Speed");
        private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");

        private void Awake()
        {
            if (tunnelRenderer != null)
            {
                tunnelMat = tunnelRenderer.material;
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private Image fadeOverlayImage;

        public void BeginWarpSequence(Action onComplete)
        {
            gameObject.SetActive(true);
            StartCoroutine(WarpSequenceRoutine(onComplete));
        }

        private Image EnsureFadeOverlay()
        {
            var faderObj = GameObject.Find("SAO_ScreenFadeOverlay");
            if (faderObj != null) return faderObj.GetComponent<Image>();

            var canvasGo = new GameObject("SAO_ScreenFadeOverlayCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var imgGo = new GameObject("SAO_ScreenFadeOverlay");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rect = imgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var img = imgGo.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return img;
        }

        private IEnumerator WarpSequenceRoutine(Action onComplete)
        {
            fadeOverlayImage = EnsureFadeOverlay();
            if (fadeOverlayImage != null)
            {
                fadeOverlayImage.gameObject.SetActive(true);
                fadeOverlayImage.color = Color.black; // Starts completely black
            }

            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.black; // Void is pitch black
            }

            // Trigger SAO Link Start audio
            if (LoginAudioSynthesizer.Instance != null)
            {
                LoginAudioSynthesizer.Instance.PlaySAOLinkStart();
                LoginAudioSynthesizer.Instance.PlayWarpWhoosh();
            }

            if (lightStreakParticles != null)
            {
                lightStreakParticles.Play();
            }

            float elapsed = 0f;
            int textIndex = -1;

            string[] bannerTexts = new string[]
            {
                "✦ LINK START! ✦",
                "✦ NEURAL LINK ESTABLISHED ✦",
                "✦ COGNITIVE SYNC: 100% ✦",
                "✦ HEXAGON MATRIX: STABLE ✦",
                "✦ ENTERING SYSTEM MATRIX ✦"
            };

            float originalFov = mainCamera != null ? mainCamera.fieldOfView : 50f;
            Vector3 originalCamPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;

            float blackFadeDuration = 0.85f;
            float whiteFadeDuration = 1.10f;
            float whiteStartTime = Mathf.Max(0.5f, tunnelDuration - whiteFadeDuration);

            while (elapsed < tunnelDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / tunnelDuration;

                // 1. Start Black -> Fade out black to reveal white hexagons in void
                if (elapsed <= blackFadeDuration)
                {
                    float blackAlpha = 1.0f - (elapsed / blackFadeDuration);
                    if (fadeOverlayImage != null)
                    {
                        fadeOverlayImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(blackAlpha));
                    }
                }
                // 2. Final phase: As hexagons shoot by, screen turns solid pure WHITE
                else if (elapsed >= whiteStartTime)
                {
                    float whiteT = (elapsed - whiteStartTime) / whiteFadeDuration;
                    float whiteAlpha = Mathf.SmoothStep(0f, 1f, whiteT);
                    if (fadeOverlayImage != null)
                    {
                        fadeOverlayImage.color = new Color(1f, 1f, 1f, whiteAlpha);
                    }
                    if (tunnelMat != null) tunnelMat.SetFloat(FlashIntensityId, whiteAlpha * 2.5f);
                    if (hexagonPillarTunnel != null) hexagonPillarTunnel.SetFlashIntensity(whiteAlpha * 2.5f);
                }
                else
                {
                    if (fadeOverlayImage != null)
                    {
                        fadeOverlayImage.color = new Color(0f, 0f, 0f, 0f);
                    }
                }

                // Speed curve: smooth acceleration from base to peak hypersonic velocity
                float speedMultiplier = Mathf.Sin(progress * Mathf.PI);
                float currentSpeed = Mathf.Lerp(baseSpeed, peakSpeed, Mathf.SmoothStep(0f, 1f, speedMultiplier));

                if (tunnelMat != null)
                {
                    tunnelMat.SetFloat(SpeedId, currentSpeed * 0.2f);
                }

                if (hexagonPillarTunnel != null)
                {
                    hexagonPillarTunnel.SetWarpSpeed(currentSpeed);
                }

                // Gentle camera FOV warp widening
                if (mainCamera != null)
                {
                    mainCamera.fieldOfView = Mathf.Lerp(originalFov, originalFov + 16f, speedMultiplier);

                    // Gentle subtle cyber rumble (not overly aggressive)
                    float shakeStrength = speedMultiplier * 0.012f;
                    Vector3 shakeOffset = new Vector3(
                        (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * shakeStrength,
                        (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * shakeStrength,
                        0f
                    );
                    mainCamera.transform.position = originalCamPos + shakeOffset;
                }

                // Cycle animated HUD banners zooming past
                if (hudTelemetryBanners != null && hudTelemetryBanners.Length > 0)
                {
                    int expectedTextIndex = Mathf.Clamp((int)(progress * bannerTexts.Length), 0, bannerTexts.Length - 1);
                    if (expectedTextIndex != textIndex)
                    {
                        textIndex = expectedTextIndex;
                        AnimateBannerZoom(bannerTexts[textIndex]);
                    }
                }

                yield return null;
            }

            // Restore camera position and FOV
            if (mainCamera != null)
            {
                mainCamera.transform.position = originalCamPos;
                mainCamera.fieldOfView = originalFov;
            }

            // Ensure screen is 100% solid pure white
            if (fadeOverlayImage != null)
            {
                fadeOverlayImage.color = Color.white;
            }

            if (lightStreakParticles != null)
            {
                lightStreakParticles.Stop();
            }

            if (tunnelMat != null) tunnelMat.SetFloat(FlashIntensityId, 0f);
            if (hexagonPillarTunnel != null) hexagonPillarTunnel.SetFlashIntensity(0f);

            onComplete?.Invoke();
        }

        private void AnimateBannerZoom(string text)
        {
            if (hudTelemetryBanners == null || hudTelemetryBanners.Length == 0) return;
            var banner = hudTelemetryBanners[0];
            if (banner == null) return;

            banner.text = text;
            banner.transform.localScale = Vector3.one * 0.35f;
            banner.alpha = 0f;

            StopCoroutine(nameof(BannerZoomRoutine));
            StartCoroutine(BannerZoomRoutine(banner));
        }

        private IEnumerator BannerZoomRoutine(TextMeshProUGUI banner)
        {
            float d = 0.70f;
            float e = 0f;
            while (e < d)
            {
                e += Time.deltaTime;
                float t = e / d;
                banner.transform.localScale = Vector3.Lerp(Vector3.one * 0.35f, Vector3.one * 1.8f, t);
                banner.alpha = Mathf.Sin(t * Mathf.PI);
                yield return null;
            }
            banner.alpha = 0f;
        }
    }
}
