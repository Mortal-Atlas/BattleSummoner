using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// SpellDrawingInkCanvas handles interactive spell gesture drawing on the screen:
    /// "When casting a spell allow the user to see what they have currently drawn.
    ///  Like drawing on a piece of paper. You see markings."
    /// 
    /// Captures touch/mouse strokes, renders glowing ink via RuneInkGraphic,
    /// performs live shape recognition, and updates SpellcraftLiveFeedbackHUD.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SpellDrawingInkCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static SpellDrawingInkCanvas Instance { get; private set; }

        [Header("Ink Renderer Reference")]
        [SerializeField] private RuneInkGraphic inkGraphic;
        [SerializeField] private RectTransform stylusNibCursor;

        [Header("Drawing Parameters")]
        [SerializeField] private float minStrokeDistance = 4f;
        [SerializeField] private float multiStrokeWindow = 0.75f;
        [SerializeField] private float recognitionThreshold = 0.50f;
        [SerializeField] private bool autoFadeUnrecognizedInk = false; // "lingers until you dispel or cast it"
        [SerializeField] private float unrecognizedFadeDelay = 1.4f;

        [Header("Fruit Ninja Melee Slicing")]
        [SerializeField] private float minSlashVelocity = 200f;
        [SerializeField] private float minSlashDistance = 25f;
        [SerializeField] private int maxBladeTrailPoints = 12;

        private RectTransform rectTransform;
        private readonly List<List<Vector2>> completedStrokes = new List<List<Vector2>>();
        private readonly List<Vector2> currentStroke = new List<Vector2>();
        private float lastStrokeEndTime = -10f;
        private bool isCurrentlyDrawing = false;
        private bool isMeleeSlicing = false;
        private Coroutine fadeCoroutine;
        private RuneRecognitionResult currentRecognition;

        // Fruit Ninja Tracking
        private Vector2 meleeTouchStartScreenPos;
        private float meleeTouchStartTime;
        private Vector2 lastDragScreenPos;
        private float lastDragScreenTime;
        private float lastSliceTriggerTime = -10f;
        private bool meleeSwipedInCurrentStroke = false;

        public bool IsCurrentlyDrawing => isCurrentlyDrawing;
        public RuneRecognitionResult CurrentRecognition => currentRecognition;

        public event Action<RuneRecognitionResult> OnRuneGestureCompleted;
        public event Action OnInkDispelled;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            rectTransform = GetComponent<RectTransform>();
            EnsureInkGraphic();
        }

        private void Start()
        {
            EnsureInkGraphic();
            if (stylusNibCursor != null) stylusNibCursor.gameObject.SetActive(false);
        }

        public bool IsPhysicalMeleeEquipped()
        {
            // 1. Check DevClassLoadoutManager
            if (DevClassLoadoutManager.Instance != null)
            {
                if (DevClassLoadoutManager.Instance.CurrentClass == PlayerClassType.Warrior) return true;
                if (DevClassLoadoutManager.Instance.CurrentClass == PlayerClassType.Mage) return false;
            }

            // 2. Check EquipmentSystem
            var eq = UnityEngine.Object.FindAnyObjectByType<EquipmentSystem>();
            if (eq != null && !string.IsNullOrEmpty(eq.mainHandWeaponId))
            {
                string id = eq.mainHandWeaponId.ToLowerInvariant();
                if (id.Contains("sword") || id.Contains("scythe") || id.Contains("blade") || id.Contains("axe") || id.Contains("claymore")) return true;
                if (id.Contains("staff") || id.Contains("wand") || id.Contains("tome") || id.Contains("catalyst")) return false;
            }

            // 3. Fallback: check if Warrior
            return DevClassLoadoutManager.Instance == null || DevClassLoadoutManager.Instance.CurrentClass == PlayerClassType.Warrior;
        }

        public void EnsureInkGraphic()
        {
            if (inkGraphic == null)
            {
                inkGraphic = GetComponentInChildren<RuneInkGraphic>();
                if (inkGraphic == null)
                {
                    var inkGo = new GameObject("RuneInkGraphicLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(RuneInkGraphic));
                    inkGo.transform.SetParent(transform, false);
                    var rt = inkGo.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    inkGraphic = inkGo.GetComponent<RuneInkGraphic>();
                    inkGraphic.raycastTarget = false;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // If the user taps on UI buttons (like Dispel or Joystick), do not intercept
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                string selName = EventSystem.current.currentSelectedGameObject.name;
                if (selName == "DispelSpellButton" || selName == "CentralMagicCircleWidget" || selName == "MagicCircleConfirmButton") return;
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            // -------------------------------------------------------------
            // MODE A: PHYSICAL MELEE (Fruit Ninja Slicing)
            // -------------------------------------------------------------
            if (IsPhysicalMeleeEquipped())
            {
                isMeleeSlicing = true;
                isCurrentlyDrawing = true;
                meleeSwipedInCurrentStroke = false;
                meleeTouchStartScreenPos = eventData.position;
                meleeTouchStartTime = Time.unscaledTime;
                lastDragScreenPos = eventData.position;
                lastDragScreenTime = Time.unscaledTime;

                if (inkGraphic != null)
                {
                    inkGraphic.ClearAll();
                    inkGraphic.StrokeWidth = 8f;
                    inkGraphic.SetInkCoreColor(new Color(1f, 1f, 1f, 0.98f)); // Pure white katana edge
                    inkGraphic.SetInkEdgeColor(new Color(0.25f, 0.85f, 1.0f, 0.55f)); // Electric cyan blade glow

                    bool isDual = PlayerCombatStanceController.Instance != null && PlayerCombatStanceController.Instance.IsDualWielding;
                    if (isDual)
                    {
                        // Two different sword slashing at the same time: Blade 1 Cyan + Blade 2 Amber
                        inkGraphic.SetDualBladeMode(true, new Vector2(-22f, 18f), new Color(1f, 0.95f, 0.70f, 0.98f), new Color(1f, 0.65f, 0.15f, 0.60f));
                    }
                    else
                    {
                        // 1 Sword only: exactly one swipe finger blade
                        inkGraphic.SetDualBladeMode(false);
                    }

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 lPos);
                    inkGraphic.BeginStroke(lPos);
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    bool isDual = PlayerCombatStanceController.Instance != null && PlayerCombatStanceController.Instance.IsDualWielding;
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX(isDual ? "dual_blade_whoosh" : "sword_whoosh", 0.85f);
                }
                return;
            }

            // -------------------------------------------------------------
            // MODE B: MAGIC ITEM CASTING (Lingering Multi-Stroke Rune Drawing)
            // -------------------------------------------------------------
            isMeleeSlicing = false;

            // Multi-stroke drawing: The player can lift their finger and keep drawing freely.
            // Completed strokes accumulate and linger until cast or dispelled.
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (completedStrokes.Count > 0 && lastStrokeEndTime > 0 && (Time.unscaledTime - lastStrokeEndTime > multiStrokeWindow * 8f))
            {
                completedStrokes.Clear();
                if (inkGraphic != null) inkGraphic.ClearAll();
            }

            isCurrentlyDrawing = true;
            currentStroke.Clear();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
            currentStroke.Add(localPos);

            if (inkGraphic != null)
            {
                inkGraphic.StrokeWidth = 14f;
                inkGraphic.SetInkCoreColor(new Color(1.0f, 0.92f, 0.65f, 0.95f)); // Arcane radiant gold
                inkGraphic.SetInkEdgeColor(new Color(0.25f, 0.85f, 1.0f, 0.35f));  // Arcane cyan aura
                inkGraphic.BeginStroke(localPos);
            }

            UpdateStylusNib(localPos, true);

            // Play initial stroke sound
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.65f);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isCurrentlyDrawing) return;

            // -------------------------------------------------------------
            // MODE A: PHYSICAL MELEE (Fruit Ninja Real-Time Screen Slicing)
            // -------------------------------------------------------------
            if (isMeleeSlicing)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 lPos);
                if (inkGraphic != null)
                {
                    inkGraphic.AddPoint(lPos);
                    // Fast-pruning blade trail following finger like Fruit Ninja
                    inkGraphic.TrimActiveStroke(maxBladeTrailPoints);
                }

                // Strictly enforce 1 sword swipe per 1 screen swipe:
                // Only trigger the strike once per drag stroke when swipe criteria are met
                if (!meleeSwipedInCurrentStroke)
                {
                    float dt = Time.unscaledTime - lastDragScreenTime;
                    if (dt >= 0.025f)
                    {
                        Vector2 delta = eventData.position - lastDragScreenPos;
                        float dist = delta.magnitude;
                        float velocity = dist / dt;

                        Vector2 totalDelta = eventData.position - meleeTouchStartScreenPos;
                        float totalDist = totalDelta.magnitude;

                        if ((totalDist >= minSlashDistance && velocity >= minSlashVelocity) || (dist >= 35f && velocity >= 650f))
                        {
                            meleeSwipedInCurrentStroke = true;
                            lastSliceTriggerTime = Time.unscaledTime;
                            // Fire blade slash event with screen coordinates
                            PortraitGestureCanvas.TriggerBladeSlash(meleeTouchStartScreenPos, eventData.position, velocity);
                            if (BattleSummonerAudioMaster.Instance != null)
                            {
                                BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 0.88f);
                            }
                        }

                        lastDragScreenPos = eventData.position;
                        lastDragScreenTime = Time.unscaledTime;
                    }
                }
                return;
            }

            // -------------------------------------------------------------
            // MODE B: MAGIC ITEM CASTING (Lingering Rune Drawing)
            // -------------------------------------------------------------
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

            if (currentStroke.Count == 0 || Vector2.Distance(currentStroke[currentStroke.Count - 1], localPos) >= minStrokeDistance)
            {
                currentStroke.Add(localPos);
                if (inkGraphic != null)
                {
                    inkGraphic.AddPoint(localPos);
                }
                UpdateStylusNib(localPos, true);

                // Run real-time shape analysis during drawing for live feedback!
                RunLiveRecognition(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isCurrentlyDrawing) return;
            isCurrentlyDrawing = false;

            // -------------------------------------------------------------
            // MODE A: PHYSICAL MELEE (Fruit Ninja Swipe Release)
            // -------------------------------------------------------------
            if (isMeleeSlicing)
            {
                isMeleeSlicing = false;
                // Only fire if this stroke hasn't already executed a sword swipe during drag
                if (!meleeSwipedInCurrentStroke)
                {
                    Vector2 totalDelta = eventData.position - meleeTouchStartScreenPos;
                    float duration = Mathf.Max(0.001f, Time.unscaledTime - meleeTouchStartTime);
                    float velocity = totalDelta.magnitude / duration;

                    if (totalDelta.magnitude >= minSlashDistance && velocity >= minSlashVelocity)
                    {
                        meleeSwipedInCurrentStroke = true;
                        PortraitGestureCanvas.TriggerBladeSlash(meleeTouchStartScreenPos, eventData.position, velocity);
                    }
                }
                meleeSwipedInCurrentStroke = false;

                // Smoothly fade blade trail out in 0.08s
                if (inkGraphic != null)
                {
                    StartCoroutine(RoutineQuickBladeTrailFade());
                }
                return;
            }

            // -------------------------------------------------------------
            // MODE B: MAGIC ITEM CASTING (Recognize & Show Central Magic Circle)
            // -------------------------------------------------------------
            lastStrokeEndTime = Time.unscaledTime;

            if (currentStroke.Count > 1)
            {
                completedStrokes.Add(new List<Vector2>(currentStroke));
            }

            if (inkGraphic != null)
            {
                inkGraphic.EndStroke();
            }

            UpdateStylusNib(Vector2.zero, false);

            // Final evaluation of all combined strokes
            RunLiveRecognition(false);

            if (currentRecognition.shape != RecognizedRuneShape.None && currentRecognition.accuracy >= recognitionThreshold)
            {
                // High confidence match! Show the central magic circle confirmation button
                PromptCentralMagicCircleLock(currentRecognition);
            }
            else
            {
                // Unrecognized gesture: Ink LINGERS on screen!
                // Player can continue drawing strokes or press [X] DISPEL to clear.
                Debug.Log($"📜 [RUNE INK LINGERING] Inscribed markings linger on screen until player dispels or completes a spell.");
                if (SpellcraftLiveFeedbackHUD.Instance != null)
                {
                    SpellcraftLiveFeedbackHUD.Instance.ShowDispelButton();
                }

                if (autoFadeUnrecognizedInk)
                {
                    if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                    fadeCoroutine = StartCoroutine(RoutineFadeInkAfterDelay(unrecognizedFadeDelay));
                }
            }
        }

        private IEnumerator RoutineFadeInkAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            DispelDrawnInk();
        }

        private IEnumerator RoutineQuickBladeTrailFade()
        {
            float elapsed = 0f;
            float duration = 0.08f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float a = 1f - (elapsed / duration);
                if (inkGraphic != null) inkGraphic.SetGlobalAlpha(a);
                yield return null;
            }
            if (inkGraphic != null)
            {
                inkGraphic.ClearAll();
                inkGraphic.SetGlobalAlpha(1f);
            }
        }

        private void RunLiveRecognition(bool isActivelyDrawing)
        {
            // Collect all points from completed strokes and current stroke
            var allPoints = new List<Vector2>();
            for (int i = 0; i < completedStrokes.Count; i++)
            {
                allPoints.AddRange(completedStrokes[i]);
            }
            if (isActivelyDrawing)
            {
                allPoints.AddRange(currentStroke);
            }

            if (allPoints.Count < 5) return;

            // Analyze shape using extended vocabulary (Lightning, Water, Fire, Healing, Rock, Arcane)
            currentRecognition = RuneGestureRecognizer.AnalyzeShape(allPoints, completedStrokes);

            // Update live feedback HUD
            if (SpellcraftLiveFeedbackHUD.Instance != null)
            {
                SpellcraftLiveFeedbackHUD.Instance.UpdateLiveRecognition(currentRecognition, isActivelyDrawing);
            }
        }

        /// <summary>
        /// When a spell is recognized:
        /// "when it recognizes the spell drawn, a magic circle in the middle shows up,
        ///  if the user presses it then they are locked into that spell. until they dispel it."
        /// </summary>
        private void PromptCentralMagicCircleLock(RuneRecognitionResult result)
        {
            Debug.Log($"✨ [RUNE RECOGNIZED] '{result.displayName}' ({result.element}) with accuracy {result.accuracy * 100:F0}%! Inscribed ink lingers. Spawning Central Magic Circle.");

            // 1. Show the DISPEL button so the player can always cancel
            if (SpellcraftLiveFeedbackHUD.Instance != null)
            {
                SpellcraftLiveFeedbackHUD.Instance.ShowDispelButton();
            }

            // 2. Summon Central Magic Circle in the middle of the screen
            var magicCircle = CentralMagicCircleLockWidget.Instance ?? FindAnyObjectByType<CentralMagicCircleLockWidget>();
            if (magicCircle != null)
            {
                magicCircle.ShowMagicCircle(result, () =>
                {
                    HandleMagicCirclePressed(result);
                });
            }
            else
            {
                // Fallback: If widget not yet instantiated in scene, execute lock directly
                HandleMagicCirclePressed(result);
            }

            OnRuneGestureCompleted?.Invoke(result);
        }

        /// <summary>
        /// User pressed the central magic circle: LOCK INTO THAT SPELL until dispelled!
        /// </summary>
        public void HandleMagicCirclePressed(RuneRecognitionResult result)
        {
            Debug.Log($"🔒 [SPELL LOCKED IN] Player pressed Magic Circle! Locked into '{result.displayName}' ({result.element}) until dispelled.");

            // Dispatch to Spell Slinger & Spellcraft subsystems based on element
            switch (result.element)
            {
                case SpellElement.Lightning:
                    if (WizardSpellSlingerController.Instance != null)
                    {
                        WizardSpellSlingerController.Instance.EquipPreset(SpellSlingerPreset.LightningRay);
                        WizardSpellSlingerController.Instance.ToggleSpellMode(true);
                    }
                    if (SpellcraftLiveFeedbackHUD.Instance != null)
                    {
                        SpellcraftLiveFeedbackHUD.Instance.ShowLockedSpellStatus(
                            "Lightning Ray",
                            "Touch & Hold anywhere to fire continuous lightning! Drains mana over time.",
                            SpellElement.Lightning
                        );
                    }
                    break;

                case SpellElement.Fire:
                    if (ActiveSpellLockManager.Instance != null)
                    {
                        ActiveSpellLockManager.Instance.OnShapeRecognized(result);
                        ActiveSpellLockManager.Instance.EnterFireballLock();
                    }
                    break;

                case SpellElement.Healing:
                    if (ActiveSpellLockManager.Instance != null)
                    {
                        ActiveSpellLockManager.Instance.OnShapeRecognized(result);
                        ActiveSpellLockManager.Instance.EnterHealingLock();
                    }
                    break;

                case SpellElement.Water:
                    if (WizardSpellSlingerController.Instance != null)
                    {
                        WizardSpellSlingerController.Instance.EquipPreset(SpellSlingerPreset.WaterTorrent);
                        WizardSpellSlingerController.Instance.ToggleSpellMode(true);
                    }
                    if (SpellcraftLiveFeedbackHUD.Instance != null)
                    {
                        SpellcraftLiveFeedbackHUD.Instance.ShowLockedSpellStatus(
                            "Water Torrent",
                            "Touch & Hold anywhere to unleash pressurized water torrent!",
                            SpellElement.Water
                        );
                    }
                    break;

                case SpellElement.Rock:
                    if (WizardSpellSlingerController.Instance != null)
                    {
                        WizardSpellSlingerController.Instance.EquipPreset(SpellSlingerPreset.RockThrow);
                        WizardSpellSlingerController.Instance.ToggleSpellMode(true);
                    }
                    if (SpellcraftLiveFeedbackHUD.Instance != null)
                    {
                        SpellcraftLiveFeedbackHUD.Instance.ShowLockedSpellStatus(
                            "Earth Boulder",
                            "Hold to charge blunt force, sling to shatter enemy posture!",
                            SpellElement.Rock
                        );
                    }
                    break;

                case SpellElement.Arcane:
                default:
                    if (WizardSpellSlingerController.Instance != null)
                    {
                        WizardSpellSlingerController.Instance.EquipPreset(SpellSlingerPreset.ArcaneBolt);
                        WizardSpellSlingerController.Instance.ToggleSpellMode(true);
                    }
                    if (SpellcraftLiveFeedbackHUD.Instance != null)
                    {
                        SpellcraftLiveFeedbackHUD.Instance.ShowLockedSpellStatus(
                            "Arcane Bolt",
                            "Hold & release to fling piercing ether bolt straight ahead!",
                            SpellElement.Arcane
                        );
                    }
                    break;
            }

            // -------------------------------------------------------------
            // Ni No Kuni Spell Cast Sequence (refer to 3:44-3:47):
            // The drawing glows, bolds, ripples an outward echo wave, then disappears!
            // -------------------------------------------------------------
            Color elemColor = GetElementColor(result.element);
            if (inkGraphic != null)
            {
                inkGraphic.PlayNiNoKuniCastSequence(elemColor, () =>
                {
                    completedStrokes.Clear();
                    currentStroke.Clear();
                    isCurrentlyDrawing = false;
                });
            }
        }

        private Color GetElementColor(SpellElement elem)
        {
            return elem switch
            {
                SpellElement.Fire => new Color(1.0f, 0.45f, 0.12f, 1f),
                SpellElement.Lightning => new Color(0.2f, 0.85f, 1.0f, 1f),
                SpellElement.Water => new Color(0.15f, 0.65f, 1.0f, 1f),
                SpellElement.Healing => new Color(0.2f, 0.95f, 0.45f, 1f),
                SpellElement.Rock => new Color(0.95f, 0.75f, 0.25f, 1f),
                SpellElement.Arcane => new Color(0.75f, 0.35f, 1.0f, 1f),
                _ => new Color(1.0f, 0.9f, 0.5f, 1f)
            };
        }

        public void DispelDrawnInk()
        {
            Debug.Log("💨 [INK DISPELLED] Dispelled all drawn ink strokes & dismissed central magic circle.");
            completedStrokes.Clear();
            currentStroke.Clear();
            isCurrentlyDrawing = false;
            isMeleeSlicing = false;

            if (CentralMagicCircleLockWidget.Instance != null)
            {
                CentralMagicCircleLockWidget.Instance.HideMagicCircle();
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (inkGraphic != null)
            {
                inkGraphic.ClearAll();
            }

            UpdateStylusNib(Vector2.zero, false);
            OnInkDispelled?.Invoke();
        }

        private IEnumerator RoutineFadeInk(float delay)
        {
            yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1.0f - (elapsed / duration));
                if (inkGraphic != null) inkGraphic.SetGlobalAlpha(alpha);
                yield return null;
            }

            if (inkGraphic != null) inkGraphic.ClearAll();
            completedStrokes.Clear();
            fadeCoroutine = null;
        }

        private void UpdateStylusNib(Vector2 localPos, bool active)
        {
            if (stylusNibCursor == null) return;
            stylusNibCursor.gameObject.SetActive(active);
            if (active)
            {
                stylusNibCursor.anchoredPosition = localPos;
            }
        }
    }
}
