using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// SpellcraftLiveFeedbackHUD provides real-time visual feedback for the player:
    /// 1. "Currently i cannot tell what spell i am casting":
    ///    - Shows live recognized spell name, elemental icon, and match confidence (0% - 100%).
    ///    - Displays a dynamic mechanics hint (e.g. "Hold to channel continuous stream of lightning. Drains mana.").
    /// 2. "Nor if i could dispel it":
    ///    - Displays a prominent, unmistakable DISPEL button [X] whenever drawing or locked in a spell.
    ///    - Clear visual prompt: "Tap [DISPEL] or press [X] to cancel."
    /// </summary>
    public class SpellcraftLiveFeedbackHUD : MonoBehaviour
    {
        public static SpellcraftLiveFeedbackHUD Instance { get; private set; }

        [Header("Banner UI Elements")]
        [SerializeField] private GameObject feedbackRoot;
        [SerializeField] private TextMeshProUGUI spellNameTMP;
        [SerializeField] private TextMeshProUGUI matchAccuracyTMP;
        [SerializeField] private Slider matchAccuracySlider;
        [SerializeField] private TextMeshProUGUI mechanicsHintTMP;
        [SerializeField] private Image elementIconImage;

        [Header("Dispel Controls")]
        [SerializeField] private GameObject dispelButtonObj;
        [SerializeField] private Button dispelButtonComponent;
        [SerializeField] private TextMeshProUGUI dispelLabelTMP;

        [Header("Ghost Rune Guide")]
        [SerializeField] private Image ghostRuneGuideImage;

        public event Action OnDispelRequested;
        public event Action OnCastRequested;

        private Coroutine autoHideCoroutine;
        private RuneRecognitionResult lastRecognizedResult;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            EnsureReferences();
        }

        private void Start()
        {
            EnsureReferences();
            HideBanner();
            HideDispelButton();
        }

        private void Update()
        {
            // Keyboard shortcut [X] or [Escape] to dispel
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
            {
                TriggerDispel();
            }
        }

        public void EnsureReferences()
        {
            if (feedbackRoot == null)
            {
                var fObj = transform.Find("SpellcraftFeedbackRoot");
                if (fObj != null) feedbackRoot = fObj.gameObject;
            }

            if (dispelButtonObj == null)
            {
                var dObj = GameObject.Find("DispelSpellButton");
                if (dObj != null)
                {
                    dispelButtonObj = dObj;
                    dispelButtonComponent = dObj.GetComponent<Button>();
                    dispelLabelTMP = dObj.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (dispelButtonComponent != null)
            {
                dispelButtonComponent.onClick.RemoveListener(TriggerDispel);
                dispelButtonComponent.onClick.AddListener(TriggerDispel);
            }
        }

        public void BindReferences(GameObject root, TextMeshProUGUI spellName, TextMeshProUGUI accuracy, Slider slider, TextMeshProUGUI mechanics, Image icon, GameObject dispelBtn, Button dispelBtnComp, TextMeshProUGUI dispelLabel)
        {
            feedbackRoot = root;
            spellNameTMP = spellName;
            matchAccuracyTMP = accuracy;
            matchAccuracySlider = slider;
            mechanicsHintTMP = mechanics;
            elementIconImage = icon;
            dispelButtonObj = dispelBtn;
            dispelButtonComponent = dispelBtnComp;
            dispelLabelTMP = dispelLabel;

            if (dispelButtonComponent != null)
            {
                dispelButtonComponent.onClick.RemoveListener(TriggerDispel);
                dispelButtonComponent.onClick.AddListener(TriggerDispel);
            }
        }

        /// <summary>
        /// Called in real-time as the player draws or when a rune is recognized.
        /// </summary>
        public void UpdateLiveRecognition(RuneRecognitionResult result, bool isActivelyDrawing)
        {
            EnsureReferences();
            lastRecognizedResult = result;

            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
                autoHideCoroutine = null;
            }

            if (feedbackRoot != null) feedbackRoot.SetActive(true);
            ShowDispelButton();

            // 1. Spell Name & Element Color
            Color elemColor = GetElementColor(result.element);
            string iconPrefix = GetElementEmoji(result.element);

            if (spellNameTMP != null)
            {
                string statePrefix = isActivelyDrawing ? "INSCRIBING: " : "PRIMED: ";
                spellNameTMP.text = $"{statePrefix}<color=#{ColorUtility.ToHtmlStringRGB(elemColor)}><b>{iconPrefix} {result.displayName}</b></color>";
            }

            // 2. Accuracy & Confidence
            int percent = Mathf.RoundToInt(result.accuracy * 100f);
            if (matchAccuracyTMP != null)
            {
                if (result.shape == RecognizedRuneShape.None)
                {
                    matchAccuracyTMP.text = "<color=#AAAAAA>Tracing glyph markings...</color>";
                }
                else
                {
                    matchAccuracyTMP.text = $"Match Confidence: <b>{percent}%</b>";
                }
            }

            if (matchAccuracySlider != null)
            {
                matchAccuracySlider.value = result.accuracy;
            }

            // 3. Mechanics Description
            if (mechanicsHintTMP != null)
            {
                if (!string.IsNullOrEmpty(result.description))
                {
                    mechanicsHintTMP.text = result.description;
                }
                else
                {
                    mechanicsHintTMP.text = "Inscribe runes on screen to awaken ancient magic. Tap [DISPEL] to abort.";
                }
            }

            // 4. Update Ghost Rune Silhouette if present
            if (ghostRuneGuideImage != null)
            {
                ghostRuneGuideImage.gameObject.SetActive(result.shape != RecognizedRuneShape.None);
                Color gColor = elemColor;
                gColor.a = Mathf.Clamp(result.accuracy * 0.45f, 0.15f, 0.55f);
                ghostRuneGuideImage.color = gColor;
            }
        }

        public void ShowLockedSpellStatus(string spellTitle, string instructions, SpellElement element)
        {
            EnsureReferences();
            if (feedbackRoot != null) feedbackRoot.SetActive(true);
            ShowDispelButton();

            Color elemColor = GetElementColor(element);
            string icon = GetElementEmoji(element);

            if (spellNameTMP != null)
            {
                spellNameTMP.text = $"<color=#{ColorUtility.ToHtmlStringRGB(elemColor)}><b>[ACTIVE: {icon} {spellTitle.ToUpper()}]</b></color>";
            }

            if (matchAccuracyTMP != null)
            {
                matchAccuracyTMP.text = "<color=#00FFAA><b>SPELL READY</b></color>";
            }

            if (mechanicsHintTMP != null)
            {
                mechanicsHintTMP.text = $"{instructions} | <color=#FF6666><b>[DISPEL Button] to Exit</b></color>";
            }
        }

        public void TriggerDispel()
        {
            Debug.Log("💨 [DISPEL TRIGGERED] Player pressed DISPEL button. Canceling spell drawing and resetting stance.");

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("glitch", 0.7f);
            }

            OnDispelRequested?.Invoke();

            if (SpellDrawingInkCanvas.Instance != null)
            {
                SpellDrawingInkCanvas.Instance.DispelDrawnInk();
            }

            if (ActiveSpellLockManager.Instance != null)
            {
                ActiveSpellLockManager.Instance.DispelActiveSpell();
            }

            if (WizardSpellSlingerController.Instance != null)
            {
                WizardSpellSlingerController.Instance.ToggleSpellMode(false);
            }

            if (SpellCastingFlowController.Instance != null)
            {
                SpellCastingFlowController.Instance.OnDispelButtonPressed();
            }

            HideBanner();
            HideDispelButton();
        }

        public void ShowDispelButton()
        {
            EnsureReferences();
            if (dispelButtonObj != null)
            {
                dispelButtonObj.SetActive(true);
            }
        }

        public void HideDispelButton()
        {
            EnsureReferences();
            if (dispelButtonObj != null)
            {
                dispelButtonObj.SetActive(false);
            }
        }

        public void HideBanner()
        {
            if (feedbackRoot != null)
            {
                feedbackRoot.SetActive(false);
            }
        }

        public void ScheduleAutoHide(float delaySeconds = 2.5f)
        {
            if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = StartCoroutine(RoutineAutoHide(delaySeconds));
        }

        private IEnumerator RoutineAutoHide(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideBanner();
            HideDispelButton();
        }

        private Color GetElementColor(SpellElement elem)
        {
            return elem switch
            {
                SpellElement.Lightning => new Color(0.25f, 0.85f, 1.0f),
                SpellElement.Fire => new Color(1.0f, 0.45f, 0.10f),
                SpellElement.Water => new Color(0.15f, 0.65f, 1.0f),
                SpellElement.Healing => new Color(0.20f, 0.95f, 0.40f),
                SpellElement.Rock => new Color(0.70f, 0.55f, 0.35f),
                SpellElement.Arcane => new Color(0.75f, 0.40f, 1.0f),
                SpellElement.Protection => new Color(0.35f, 0.75f, 1.0f),
                _ => Color.white
            };
        }

        private string GetElementEmoji(SpellElement elem)
        {
            return elem switch
            {
                SpellElement.Lightning => "⚡",
                SpellElement.Fire => "🔥",
                SpellElement.Water => "💧",
                SpellElement.Healing => "💚",
                SpellElement.Rock => "🪨",
                SpellElement.Arcane => "✨",
                SpellElement.Protection => "⛨",
                _ => "🔮"
            };
        }
    }
}
