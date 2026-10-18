using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// CentralMagicCircleLockWidget displays an ornate, pulsing, rotating magic circle
    /// in the center of the screen when a drawn spell rune is recognized:
    /// "when it recognizes the spell drawn, a magic circle in the middle shows up,
    ///  if the user presses it then they are locked into that spell. until they dispel it."
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CentralMagicCircleLockWidget : MonoBehaviour
    {
        private static CentralMagicCircleLockWidget _instance;
        public static CentralMagicCircleLockWidget Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<CentralMagicCircleLockWidget>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("UI Visual Elements")]
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private RectTransform outerGlyphRing;
        [SerializeField] private RectTransform innerSigilCore;
        [SerializeField] private Image outerRingImage;
        [SerializeField] private Image innerSigilImage;
        [SerializeField] private Image centerGlowGlow;
        [SerializeField] private Button lockConfirmButton;
        [SerializeField] private TextMeshProUGUI spellNameTMP;
        [SerializeField] private TextMeshProUGUI tapPromptTMP;

        [Header("Animation Tuning")]
        [SerializeField] private float outerSpinSpeed = -35f;
        [SerializeField] private float innerSpinSpeed = 65f;
        [SerializeField] private float pulseSpeed = 4.5f;
        [SerializeField] private float pulseAmount = 0.06f;

        private Action onLockConfirmedCallback;
        private Coroutine popCoroutine;
        private bool isVisible = false;
        private RuneRecognitionResult activeRecognition;

        public bool IsVisible => isVisible;
        public RuneRecognitionResult ActiveRecognition => activeRecognition;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (containerRect == null) containerRect = GetComponent<RectTransform>();
            EnsureVisualComponents();
        }

        private void Start()
        {
            EnsureVisualComponents();
            if (!isVisible)
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isVisible) return;

            // Spin outer and inner rings in opposing directions
            if (outerGlyphRing != null)
            {
                outerGlyphRing.Rotate(Vector3.forward, outerSpinSpeed * Time.deltaTime, Space.Self);
            }
            if (innerSigilCore != null)
            {
                innerSigilCore.Rotate(Vector3.forward, innerSpinSpeed * Time.deltaTime, Space.Self);
            }

            // Pulsing scale for arcane life
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            if (containerRect != null)
            {
                containerRect.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void EnsureVisualComponents()
        {
            if (lockConfirmButton == null)
            {
                lockConfirmButton = GetComponentInChildren<Button>();
                if (lockConfirmButton == null)
                {
                    lockConfirmButton = gameObject.AddComponent<Button>();
                }
            }

            if (lockConfirmButton != null)
            {
                lockConfirmButton.onClick.RemoveListener(OnMagicCircleClicked);
                lockConfirmButton.onClick.AddListener(OnMagicCircleClicked);
            }
        }

        /// <summary>
        /// Displays the central magic circle for a recognized rune glyph.
        /// </summary>
        public void ShowMagicCircle(RuneRecognitionResult recognition, Action onConfirm)
        {
            activeRecognition = recognition;
            onLockConfirmedCallback = onConfirm;
            isVisible = true;
            gameObject.SetActive(true);

            Color elemColor = GetElementColor(recognition.element);

            if (outerRingImage != null) outerRingImage.color = elemColor;
            if (innerSigilImage != null) innerSigilImage.color = new Color(elemColor.r, elemColor.g, elemColor.b, 0.85f);
            if (centerGlowGlow != null) centerGlowGlow.color = new Color(elemColor.r, elemColor.g, elemColor.b, 0.45f);

            if (spellNameTMP != null)
            {
                spellNameTMP.text = $"<b>{recognition.displayName.ToUpper()}</b>";
                spellNameTMP.color = elemColor;
            }

            if (tapPromptTMP != null)
            {
                tapPromptTMP.text = "<size=13><color=#FFF4CC>TAP TO LOCK SPELL</color></size>";
            }

            // Pop in animation
            if (popCoroutine != null) StopCoroutine(popCoroutine);
            popCoroutine = StartCoroutine(RoutinePopIn());

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
            }
        }

        public void HideMagicCircle()
        {
            if (!isVisible && !gameObject.activeSelf) return;
            isVisible = false;
            if (popCoroutine != null) StopCoroutine(popCoroutine);
            popCoroutine = StartCoroutine(RoutinePopOut());
        }

        private void OnMagicCircleClicked()
        {
            if (!isVisible) return;
            Debug.Log($"🔯 [MAGIC CIRCLE PRESSED] Player confirmed lock for spell '{activeRecognition.displayName}' ({activeRecognition.element})!");

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("apex_celestial_roar", 0.95f);
            }

            var callback = onLockConfirmedCallback;
            HideMagicCircle();
            callback?.Invoke();
        }

        private IEnumerator RoutinePopIn()
        {
            if (containerRect == null) yield break;
            containerRect.localScale = Vector3.zero;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Overshoot bounce
                float s = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.08f;
                containerRect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            containerRect.localScale = Vector3.one;
            popCoroutine = null;
        }

        private IEnumerator RoutinePopOut()
        {
            if (containerRect == null)
            {
                gameObject.SetActive(false);
                yield break;
            }

            float elapsed = 0f;
            float duration = 0.15f;
            Vector3 startScale = containerRect.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = Mathf.Lerp(startScale.x, 0f, t * t);
                containerRect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            containerRect.localScale = Vector3.zero;
            gameObject.SetActive(false);
            popCoroutine = null;
        }

        private Color GetElementColor(SpellElement elem)
        {
            return elem switch
            {
                SpellElement.Fire => new Color(1.0f, 0.45f, 0.12f, 1f),       // Solar amber
                SpellElement.Lightning => new Color(0.2f, 0.85f, 1.0f, 1f),    // Electric cyan
                SpellElement.Water => new Color(0.15f, 0.65f, 1.0f, 1f),       // Azure ocean
                SpellElement.Healing => new Color(0.2f, 0.95f, 0.45f, 1f),     // Emerald bloom
                SpellElement.Rock => new Color(0.95f, 0.75f, 0.25f, 1f),       // Golden earth
                SpellElement.Arcane => new Color(0.75f, 0.35f, 1.0f, 1f),      // Astral violet
                SpellElement.Protection => new Color(0.35f, 0.75f, 1.0f, 1f),  // Bastion barrier blue
                _ => new Color(1.0f, 0.9f, 0.5f, 1f)
            };
        }

        public void BindReferences(
            RectTransform container,
            RectTransform outerRing,
            RectTransform innerSigil,
            Image outerImg,
            Image innerImg,
            Image centerGlow,
            Button confirmBtn,
            TextMeshProUGUI spellName,
            TextMeshProUGUI tapPrompt)
        {
            containerRect = container;
            outerGlyphRing = outerRing;
            innerSigilCore = innerSigil;
            outerRingImage = outerImg;
            innerSigilImage = innerImg;
            centerGlowGlow = centerGlow;
            lockConfirmButton = confirmBtn;
            spellNameTMP = spellName;
            tapPromptTMP = tapPrompt;

            EnsureVisualComponents();
        }
    }
}
