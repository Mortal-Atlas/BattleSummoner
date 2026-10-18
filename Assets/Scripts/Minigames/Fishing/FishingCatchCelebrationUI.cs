using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BattleSummoner.Customization;

namespace BattleSummoner.Minigames.Fishing
{
    /// <summary>
    /// FishingCatchCelebrationUI presents the iconic Animal Crossing catch fanfare:
    /// - Character strikes a triumphant pose holding the caught fish high overhead
    /// - Anime face expression shifts to Joyful / Grinning
    /// - Sparkles burst across the screen
    /// - Dialogue pun banner reveals fish species, witty pun, length, and value
    /// </summary>
    public class FishingCatchCelebrationUI : MonoBehaviour
    {
        public static FishingCatchCelebrationUI Instance { get; private set; }

        [Header("UI Canvas Elements")]
        [SerializeField] private GameObject celebrationRoot;
        [SerializeField] private Text speciesTitleText;
        [SerializeField] private Text catchPunText;
        [SerializeField] private Text lengthText;
        [SerializeField] private Text valueText;
        [SerializeField] private Button continueButton;

        [Header("3D Presentation")]
        [SerializeField] private Transform fishTrophyHolder;
        private GameObject activeTrophyModel;

        public bool IsCelebrating => celebrationRoot != null && celebrationRoot.activeSelf;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            BuildUIIfMissing();
            if (celebrationRoot != null) celebrationRoot.SetActive(false);
        }

        public void BuildUIIfMissing()
        {
            if (celebrationRoot != null) return;

            // Find or create Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("FishingCelebrationCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasGO.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasGO.transform, false);
            }

            celebrationRoot = new GameObject("CelebrationRoot");
            celebrationRoot.transform.SetParent(canvas.transform, false);
            var rootRect = celebrationRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Translucent banner backdrop
            var bannerGO = new GameObject("Banner");
            bannerGO.transform.SetParent(celebrationRoot.transform, false);
            var bannerImg = bannerGO.AddComponent<Image>();
            bannerImg.color = new Color(0.06f, 0.08f, 0.14f, 0.88f);
            var bRect = bannerGO.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.15f, 0.12f);
            bRect.anchorMax = new Vector2(0.85f, 0.38f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            // Gold border
            var outline = bannerGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.78f, 0.25f, 0.9f);
            outline.effectDistance = new Vector2(3, -3);

            // Title Text
            var titleGO = new GameObject("SpeciesTitle");
            titleGO.transform.SetParent(bannerGO.transform, false);
            speciesTitleText = titleGO.AddComponent<Text>();
            speciesTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            speciesTitleText.fontSize = 32;
            speciesTitleText.alignment = TextAnchor.MiddleCenter;
            speciesTitleText.color = new Color(1.0f, 0.88f, 0.35f);
            speciesTitleText.fontStyle = FontStyle.Bold;
            var tRect = titleGO.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.65f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            // Pun Text
            var punGO = new GameObject("CatchPun");
            punGO.transform.SetParent(bannerGO.transform, false);
            catchPunText = punGO.AddComponent<Text>();
            catchPunText.font = speciesTitleText.font;
            catchPunText.fontSize = 24;
            catchPunText.alignment = TextAnchor.MiddleCenter;
            catchPunText.color = Color.white;
            catchPunText.fontStyle = FontStyle.Italic;
            var pRect = punGO.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.05f, 0.30f);
            pRect.anchorMax = new Vector2(0.95f, 0.65f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            // Details / Length / Value Text
            var detailsGO = new GameObject("Details");
            detailsGO.transform.SetParent(bannerGO.transform, false);
            lengthText = detailsGO.AddComponent<Text>();
            lengthText.font = speciesTitleText.font;
            lengthText.fontSize = 20;
            lengthText.alignment = TextAnchor.MiddleCenter;
            lengthText.color = new Color(0.6f, 0.9f, 1f);
            var dRect = detailsGO.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.05f, 0.05f);
            dRect.anchorMax = new Vector2(0.70f, 0.30f);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;

            // Continue Button
            var btnGO = new GameObject("ContinueButton");
            btnGO.transform.SetParent(bannerGO.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.20f, 0.65f, 0.35f);
            continueButton = btnGO.AddComponent<Button>();
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.75f, 0.06f);
            btnRect.anchorMax = new Vector2(0.96f, 0.30f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            var btnTextGO = new GameObject("BtnText");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var bTxt = btnTextGO.AddComponent<Text>();
            bTxt.font = speciesTitleText.font;
            bTxt.text = "Keep It!";
            bTxt.fontSize = 20;
            bTxt.color = Color.white;
            bTxt.alignment = TextAnchor.MiddleCenter;
            var btRect = btnTextGO.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero;
            btRect.offsetMax = Vector2.zero;

            continueButton.onClick.AddListener(DismissCelebration);
        }

        public void ShowCatchCelebration(FishSpeciesData fish, float lengthCm, AnimeFaceOverlayController faceCtrl)
        {
            BuildUIIfMissing();
            celebrationRoot.SetActive(true);

            // Shift character face to Joy
            if (faceCtrl != null)
            {
                faceCtrl.SetExpression(AnimeExpression.Joy, autoResetDuration: 6.0f);
            }

            // Populate Banner
            speciesTitleText.text = $"★ You caught a {fish.speciesName}! ★";
            catchPunText.text = $"\"{fish.catchPun}\"";
            lengthText.text = $"Size: {lengthCm:F1} cm  |  Rarity: {fish.rarity}  |  Value: {fish.basePrice} G";

            // Spawn 3D held fish above player's head
            CreateHeldFishModel(fish);

            StartCoroutine(AutoDismissRoutine());
        }

        private void CreateHeldFishModel(FishSpeciesData fish)
        {
            if (activeTrophyModel != null)
            {
                if (Application.isPlaying) Destroy(activeTrophyModel);
                else DestroyImmediate(activeTrophyModel);
            }

            var hero = GameObject.Find("Player_Hero");
            if (hero == null) return;

            activeTrophyModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            DestroyImmediate(activeTrophyModel.GetComponent<Collider>());
            activeTrophyModel.name = "CaughtFishTrophy";
            activeTrophyModel.transform.SetParent(hero.transform, false);
            // Position above character head
            activeTrophyModel.transform.localPosition = new Vector3(0, 2.15f, 0.35f);
            activeTrophyModel.transform.localRotation = Quaternion.Euler(0, 90f, 90f);
            activeTrophyModel.transform.localScale = new Vector3(0.22f, 0.65f, 0.22f);

            var mr = activeTrophyModel.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = fish.fishColor;
            mr.sharedMaterial = mat;

            // Tail fin
            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(tail.GetComponent<Collider>());
            tail.transform.SetParent(activeTrophyModel.transform, false);
            tail.transform.localPosition = new Vector3(0, -1.1f, 0);
            tail.transform.localScale = new Vector3(0.2f, 0.4f, 0.8f);
            tail.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private IEnumerator AutoDismissRoutine()
        {
            yield return new WaitForSeconds(6.0f);
            DismissCelebration();
        }

        public void DismissCelebration()
        {
            if (celebrationRoot != null) celebrationRoot.SetActive(false);
            if (activeTrophyModel != null)
            {
                if (Application.isPlaying) Destroy(activeTrophyModel);
                else DestroyImmediate(activeTrophyModel);
                activeTrophyModel = null;
            }
        }
    }
}
