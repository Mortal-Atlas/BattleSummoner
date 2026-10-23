using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BattleSummoner.Customization;

namespace BattleSummoner.Minigames.Insects
{
    /// <summary>
    /// InsectCatchCelebrationUI presents the Animal Crossing insect fanfare:
    /// - Character strikes a victory pose holding the specimen high
    /// - Face shifts to Joyful / Grinning
    /// - Dialogue banner presents the insect name, witty pun, rarity, and value
    /// </summary>
    public class InsectCatchCelebrationUI : MonoBehaviour
    {
        public static InsectCatchCelebrationUI Instance { get; private set; }

        [Header("UI Canvas Elements")]
        [SerializeField] private GameObject celebrationRoot;
        [SerializeField] private Text speciesTitleText;
        [SerializeField] private Text catchPunText;
        [SerializeField] private Text detailsText;
        [SerializeField] private Button continueButton;

        private GameObject activeSpecimenModel;

        public bool IsCelebrating => celebrationRoot != null && celebrationRoot.activeSelf;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            BuildUIIfMissing();
            if (celebrationRoot != null) celebrationRoot.SetActive(false);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = false;
        }

        public void BuildUIIfMissing()
        {
            if (celebrationRoot != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("InsectCelebrationCanvas");
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

            var bannerGO = new GameObject("Banner");
            bannerGO.transform.SetParent(celebrationRoot.transform, false);
            var bannerImg = bannerGO.AddComponent<Image>();
            bannerImg.color = new Color(0.08f, 0.12f, 0.10f, 0.90f);
            var bRect = bannerGO.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.15f, 0.12f);
            bRect.anchorMax = new Vector2(0.85f, 0.38f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            var outline = bannerGO.AddComponent<Outline>();
            outline.effectColor = new Color(0.40f, 0.88f, 0.40f, 0.9f);
            outline.effectDistance = new Vector2(3, -3);

            var titleGO = new GameObject("SpeciesTitle");
            titleGO.transform.SetParent(bannerGO.transform, false);
            speciesTitleText = titleGO.AddComponent<Text>();
            speciesTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            speciesTitleText.fontSize = 32;
            speciesTitleText.alignment = TextAnchor.MiddleCenter;
            speciesTitleText.color = new Color(0.55f, 1.0f, 0.65f);
            speciesTitleText.fontStyle = FontStyle.Bold;
            var tRect = titleGO.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.65f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

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

            var detailsGO = new GameObject("Details");
            detailsGO.transform.SetParent(bannerGO.transform, false);
            detailsText = detailsGO.AddComponent<Text>();
            detailsText.font = speciesTitleText.font;
            detailsText.fontSize = 20;
            detailsText.alignment = TextAnchor.MiddleCenter;
            detailsText.color = new Color(0.85f, 0.95f, 0.85f);
            var dRect = detailsGO.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.05f, 0.05f);
            dRect.anchorMax = new Vector2(0.70f, 0.30f);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;

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
            bTxt.text = "Caught!";
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

        public void ShowCatchCelebration(InsectSpeciesData insect, AnimeFaceOverlayController faceCtrl)
        {
            BuildUIIfMissing();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = true;
            celebrationRoot.SetActive(true);

            if (faceCtrl != null)
            {
                faceCtrl.SetExpression(AnimeExpression.Joy, autoResetDuration: 5.5f);
            }

            speciesTitleText.text = $"★ You caught a {insect.insectName}! ★";
            catchPunText.text = $"\"{insect.catchPun}\"";
            detailsText.text = $"Type: {insect.type}  |  Rarity: {insect.rarity}  |  Value: {insect.basePrice} G";

            CreateHeldSpecimenJar(insect);
            StartCoroutine(AutoDismissRoutine());
        }

        private void CreateHeldSpecimenJar(InsectSpeciesData insect)
        {
            if (activeSpecimenModel != null)
            {
                if (Application.isPlaying) Destroy(activeSpecimenModel);
                else DestroyImmediate(activeSpecimenModel);
            }

            var hero = GameObject.Find("Player_Hero");
            if (hero == null) return;

            // Specimen jar
            activeSpecimenModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(activeSpecimenModel.GetComponent<Collider>());
            activeSpecimenModel.name = "CaughtSpecimenJar";
            activeSpecimenModel.transform.SetParent(hero.transform, false);
            activeSpecimenModel.transform.localPosition = new Vector3(0, 2.10f, 0.30f);
            activeSpecimenModel.transform.localScale = new Vector3(0.25f, 0.30f, 0.25f);

            var mr = activeSpecimenModel.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.9f, 0.95f, 1f, 0.6f);
            mr.sharedMaterial = mat;

            // Mini insect inside
            var bugInside = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(bugInside.GetComponent<Collider>());
            bugInside.transform.SetParent(activeSpecimenModel.transform, false);
            bugInside.transform.localPosition = Vector3.zero;
            bugInside.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var bmr = bugInside.GetComponent<MeshRenderer>();
            var bmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bmat.color = insect.wingColor;
            bmr.sharedMaterial = bmat;
        }

        private IEnumerator AutoDismissRoutine()
        {
            yield return new WaitForSeconds(5.5f);
            DismissCelebration();
        }

        public void DismissCelebration()
        {
            if (celebrationRoot != null) celebrationRoot.SetActive(false);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = false;
            if (activeSpecimenModel != null)
            {
                if (Application.isPlaying) Destroy(activeSpecimenModel);
                else DestroyImmediate(activeSpecimenModel);
                activeSpecimenModel = null;
            }
        }
    }
}
