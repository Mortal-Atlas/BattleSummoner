using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BattleSummoner.Audio;

namespace BattleSummoner.Intro
{
    /// <summary>
    /// TitleLogoScreenUI presents the iconic anime RPG game logo screen:
    /// - Smooth arrival fade-in from the SAO Link Start white dive transition.
    /// - High-fidelity "BATTLE SUMMONER" title presentation with animated glowing crest.
    /// - Pulsating "CLICK TO START" prompt with keyboard and mouse input handling.
    /// - Transition chime and smooth handoff into the Character Customizer studio.
    /// </summary>
    public class TitleLogoScreenUI : MonoBehaviour
    {
        public static TitleLogoScreenUI Instance { get; private set; }

        [Header("Target Character Scene")]
        public string targetCharacterSceneName = "CharacterEquipmentShowcase";

        [Header("UI References (Auto-created if null)")]
        public Canvas titleCanvas;
        public GameObject titleRoot;
        public TextMeshProUGUI titleLogoText;
        public TextMeshProUGUI subtitleText;
        public TextMeshProUGUI clickToStartText;
        public Button fullScreenStartButton;

        [Header("Styling")]
        public Color primaryGoldColor = new Color(1.0f, 0.82f, 0.28f);
        public Color accentCyanColor = new Color(0.22f, 0.74f, 0.97f);

        private bool isStarting = false;
        private Image fadeOverlayImage;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            EnsureUI();
        }

        private void OnEnable()
        {
            isStarting = false;
            StartCoroutine(ArrivalFadeInRoutine());
        }

        private void Update()
        {
            if (isStarting || !gameObject.activeInHierarchy) return;

            // Animate "CLICK TO START" pulsing breath
            if (clickToStartText != null)
            {
                float pulse = Mathf.Sin(Time.time * 3.5f) * 0.35f + 0.65f;
                Color col = clickToStartText.color;
                col.a = pulse;
                clickToStartText.color = col;
            }

            // Any mouse click or key press initiates game start
            if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            {
                OnStartClicked();
            }
        }

        public void EnsureUI()
        {
            if (titleCanvas != null && titleLogoText != null) return;

            // Check if existing canvas is present
            titleCanvas = GetComponentInChildren<Canvas>(true);
            if (titleCanvas == null)
            {
                var canvasGo = new GameObject("TitleLogoCanvas");
                canvasGo.transform.SetParent(transform, false);
                titleCanvas = canvasGo.AddComponent<Canvas>();
                titleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                titleCanvas.sortingOrder = 500;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            var fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // Fullscreen invisible button for mouse clicks
            if (fullScreenStartButton == null)
            {
                var btnGo = new GameObject("Btn_FullScreenStart");
                btnGo.transform.SetParent(titleCanvas.transform, false);
                var btnRt = btnGo.AddComponent<RectTransform>();
                btnRt.anchorMin = Vector2.zero;
                btnRt.anchorMax = Vector2.one;
                btnRt.sizeDelta = Vector2.zero;
                var btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0, 0, 0, 0.001f); // Transparent raycast target
                fullScreenStartButton = btnGo.AddComponent<Button>();
                fullScreenStartButton.onClick.AddListener(OnStartClicked);
            }

            // Dark atmospheric background with subtle radial glow
            var bgGo = new GameObject("Title_Background");
            bgGo.transform.SetParent(titleCanvas.transform, false);
            bgGo.transform.SetSiblingIndex(0);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);
            bgImg.raycastTarget = false;

            // Title Container
            var containerGo = new GameObject("Title_Container");
            containerGo.transform.SetParent(titleCanvas.transform, false);
            var contRt = containerGo.AddComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.5f, 0.5f);
            contRt.anchorMax = new Vector2(0.5f, 0.5f);
            contRt.pivot = new Vector2(0.5f, 0.5f);
            contRt.sizeDelta = new Vector2(1000, 500);

            // Crest / Emblem Top Marker
            var crestGo = new GameObject("Title_Crest");
            crestGo.transform.SetParent(contRt, false);
            var crestRt = crestGo.AddComponent<RectTransform>();
            crestRt.anchoredPosition = new Vector2(0, 160);
            crestRt.sizeDelta = new Vector2(500, 40);
            var crestTMP = crestGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) crestTMP.font = fontAsset;
            crestTMP.text = "✦   •   ⚔   •   ✦";
            crestTMP.fontSize = 24f;
            crestTMP.alignment = TextAlignmentOptions.Center;
            crestTMP.color = accentCyanColor;
            crestTMP.raycastTarget = false;

            // Main Logo Text
            var logoGo = new GameObject("Title_MainLogo");
            logoGo.transform.SetParent(contRt, false);
            var logoRt = logoGo.AddComponent<RectTransform>();
            logoRt.anchoredPosition = new Vector2(0, 75);
            logoRt.sizeDelta = new Vector2(900, 110);
            titleLogoText = logoGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleLogoText.font = fontAsset;
            titleLogoText.text = "<b>BATTLE SUMMONER</b>";
            titleLogoText.fontSize = 72f;
            titleLogoText.alignment = TextAlignmentOptions.Center;
            titleLogoText.color = primaryGoldColor;
            titleLogoText.characterSpacing = 8f;
            titleLogoText.raycastTarget = false;

            // Subtitle
            var subGo = new GameObject("Title_Subtitle");
            subGo.transform.SetParent(contRt, false);
            var subRt = subGo.AddComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, 0);
            subRt.sizeDelta = new Vector2(800, 45);
            subtitleText = subGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) subtitleText.font = fontAsset;
            subtitleText.text = "—  A E T H E L G A R D   O D Y S S E Y  —";
            subtitleText.fontSize = 20f;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(0.70f, 0.85f, 1.0f, 0.85f);
            subtitleText.characterSpacing = 6f;
            subtitleText.raycastTarget = false;

            // Pulsating "CLICK TO START"
            var startPromptGo = new GameObject("Title_ClickToStart");
            startPromptGo.transform.SetParent(contRt, false);
            var startRt = startPromptGo.AddComponent<RectTransform>();
            startRt.anchoredPosition = new Vector2(0, -140);
            startRt.sizeDelta = new Vector2(600, 50);
            clickToStartText = startPromptGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) clickToStartText.font = fontAsset;
            clickToStartText.text = "<b>▶   C L I C K   T O   S T A R T   ◀</b>";
            clickToStartText.fontSize = 26f;
            clickToStartText.alignment = TextAlignmentOptions.Center;
            clickToStartText.color = Color.white;
            clickToStartText.characterSpacing = 4f;
            clickToStartText.raycastTarget = false;

            // Bottom System Tagline
            var footGo = new GameObject("Title_FootNote");
            footGo.transform.SetParent(contRt, false);
            var footRt = footGo.AddComponent<RectTransform>();
            footRt.anchoredPosition = new Vector2(0, -200);
            footRt.sizeDelta = new Vector2(600, 30);
            var footTMP = footGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) footTMP.font = fontAsset;
            footTMP.text = "<color=#64748B>PRESS ANY KEY OR CLICK SCREEN TO ENTER CHARACTER MATRIX</color>";
            footTMP.fontSize = 12f;
            footTMP.alignment = TextAlignmentOptions.Center;
            footTMP.color = new Color(0.40f, 0.50f, 0.65f);
            footTMP.raycastTarget = false;
        }

        private IEnumerator ArrivalFadeInRoutine()
        {
            var faderObj = GameObject.Find("SAO_ScreenFadeOverlay");
            if (faderObj != null) fadeOverlayImage = faderObj.GetComponent<Image>();

            if (fadeOverlayImage != null)
            {
                fadeOverlayImage.gameObject.SetActive(true);
                fadeOverlayImage.color = Color.white; // Starts pure white from SAO dive flash
                float duration = 0.90f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = 1.0f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    fadeOverlayImage.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }

                fadeOverlayImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        public void OnStartClicked()
        {
            if (isStarting) return;
            isStarting = true;

            // Play confirmation chime
            if (LoginAudioSynthesizer.Instance != null)
            {
                LoginAudioSynthesizer.Instance.PlayInductionFanfare();
                LoginAudioSynthesizer.Instance.PlayUIClick();
            }

            StartCoroutine(TransitionToCharacterScreenRoutine());
        }

        private IEnumerator TransitionToCharacterScreenRoutine()
        {
            // Flash prompt text
            if (clickToStartText != null)
            {
                clickToStartText.color = accentCyanColor;
                clickToStartText.transform.localScale = Vector3.one * 1.15f;
            }

            var faderObj = GameObject.Find("SAO_ScreenFadeOverlay");
            if (faderObj != null) fadeOverlayImage = faderObj.GetComponent<Image>();

            if (fadeOverlayImage != null)
            {
                fadeOverlayImage.gameObject.SetActive(true);
                float duration = 0.55f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    fadeOverlayImage.color = new Color(0.02f, 0.04f, 0.08f, alpha);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }

            Debug.Log($"[TITLE LOGO] Launching Character Customizer: '{targetCharacterSceneName}'");
            SceneManager.LoadScene(targetCharacterSceneName);
        }
    }
}
