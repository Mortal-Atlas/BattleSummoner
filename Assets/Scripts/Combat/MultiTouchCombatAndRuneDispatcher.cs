using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// MultiTouchCombatAndRuneDispatcher integrates:
    /// 1. Single vs. Dual-Wield Blade Slicing (FruitNinjaBladeCutter).
    /// 2. Passive Shield Blocking state (when not actively swinging or drawing).
    /// 3. Universal Two-Finger Tap Parry (DeltaTime <= 0.06s, movement < 25px, hold < 0.12s).
    /// 4. Second-Hand Tomes & Wizarding Books (+15% Affinity Matching Buffs).
    /// 5. Protective Shield Rune (⛨) with drag-to-ally targeting onto the 9 right-column Persona portraits.
    /// </summary>
    public class MultiTouchCombatAndRuneDispatcher : MonoBehaviour
    {
        private static MultiTouchCombatAndRuneDispatcher _instance;
        public static MultiTouchCombatAndRuneDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<MultiTouchCombatAndRuneDispatcher>();
                    if (_instance == null && Application.isPlaying)
                    {
                        var go = new GameObject("MultiTouchCombatAndRuneDispatcher");
                        _instance = go.AddComponent<MultiTouchCombatAndRuneDispatcher>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("System References")]
        [SerializeField] private SpireLoadoutAndTomeSystem loadoutSystem;
        [SerializeField] private FruitNinjaBladeCutter primaryBladeTrail;   // Finger 0 Blade
        [SerializeField] private FruitNinjaBladeCutter secondaryBladeTrail; // Finger 1 Blade (Dual-Wield Only)
        [SerializeField] private RuneShapeCastingEngine runeCastingEngine;

        [Header("Right-Column 9-Ally Persona Portraits (Anchor X: 0.82 - 0.98)")]
        [SerializeField] private RectTransform[] allyPortraitSlots = new RectTransform[9];

        [Header("Two-Finger Tap Parry Thresholds")]
        [SerializeField] private float chordSyncWindow = 0.06f; // Max 60ms between both fingers touching
        [SerializeField] private float tapMaxMovePixels = 25f;  // Max movement before becoming a slash/draw
        [SerializeField] private float tapMaxHoldSeconds = 0.12f;

        public bool isShieldCurrentlyBlocking { get; private set; }

        private int _finger0Id = -1;
        private int _finger1Id = -1;
        private float _f0StartTime, _f1StartTime;
        private Vector2 _f0StartPos, _f1StartPos;
        private Vector2 _f0PrevPos, _f1PrevPos;
        private bool _twoFingerParryArmed;
        private bool _isPerformingOffensiveAction;

        public event Action OnTwoFingerParryTriggered;
        public event Action<int, float> OnAllyProtectedWithShieldSpell; // (allyIndex, barrierHp)

        private void Awake()
        {
            if (_instance == null || !_instance) _instance = this;
            else if (_instance != this)
            {
                if (Application.isPlaying) Destroy(this);
                else DestroyImmediate(this);
                return;
            }

            ResolveDependencies();
        }

        private void OnEnable()
        {
            if (!EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Enable();
            }
        }

        private void OnDisable()
        {
            if (EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Disable();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            ResolveDependencies();
        }

        private void ResolveDependencies()
        {
            if (loadoutSystem == null)
            {
                loadoutSystem = GetComponent<SpireLoadoutAndTomeSystem>() 
                             ?? SpireLoadoutAndTomeSystem.Instance 
                             ?? FindAnyObjectByType<SpireLoadoutAndTomeSystem>();
            }

            if (primaryBladeTrail == null)
            {
                primaryBladeTrail = FruitNinjaBladeCutter.Instance;
            }

            if (secondaryBladeTrail == null && primaryBladeTrail != null)
            {
                secondaryBladeTrail = primaryBladeTrail;
            }

            if (runeCastingEngine == null)
            {
                runeCastingEngine = RuneShapeCastingEngine.Instance;
            }

            // Automatically resolve 9 ally portrait slots from PlayerHUDController if empty
            if (allyPortraitSlots == null || allyPortraitSlots.Length == 0 || allyPortraitSlots[0] == null)
            {
                var hud = PlayerHUDController.Instance ?? FindAnyObjectByType<PlayerHUDController>();
                if (hud != null && hud.allyButtons != null)
                {
                    allyPortraitSlots = new RectTransform[hud.allyButtons.Length];
                    for (int i = 0; i < hud.allyButtons.Length; i++)
                    {
                        if (hud.allyButtons[i] != null)
                        {
                            allyPortraitSlots[i] = hud.allyButtons[i].GetComponent<RectTransform>();
                        }
                    }
                }
            }
        }

        private void Update()
        {
            ProcessCanvasTouches();

            // Desktop / Editor Fallback Input when no touch screen is active
            ProcessDesktopFallbackInput();

            // Passive Shield blocks whenever a Shield is in Second-Hand and player is not actively swinging/casting
            isShieldCurrentlyBlocking = (loadoutSystem != null &&
                                         loadoutSystem.hasPassiveShieldBlock &&
                                         !_isPerformingOffensiveAction);
        }

        private void ProcessCanvasTouches()
        {
            _isPerformingOffensiveAction = false;

            if (Touch.activeTouches.Count == 0) return;

            foreach (var touch in Touch.activeTouches)
            {
                if (!IsInsideCentralCombatCanvas(touch.screenPosition))
                    continue;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnFingerDown(touch);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        OnFingerDrag(touch);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        OnFingerUp(touch);
                        break;
                }
            }
        }

        private void OnFingerDown(Touch touch)
        {
            if (_finger0Id == -1)
            {
                _finger0Id = touch.touchId;
                _f0StartTime = Time.unscaledTime;
                _f0StartPos = touch.screenPosition;
                _f0PrevPos = touch.screenPosition;
                _twoFingerParryArmed = false;
            }
            else if (_finger1Id == -1 && touch.touchId != _finger0Id)
            {
                _finger1Id = touch.touchId;
                _f1StartTime = Time.unscaledTime;
                _f1StartPos = touch.screenPosition;
                _f1PrevPos = touch.screenPosition;

                // Arm the Universal 2-Finger Tap Parry if both fingers landed together within chord sync window
                if (Mathf.Abs(_f1StartTime - _f0StartTime) <= chordSyncWindow)
                {
                    _twoFingerParryArmed = true;
                }
            }
        }

        private void OnFingerDrag(Touch touch)
        {
            if (touch.touchId == _finger0Id)
            {
                float moveDist = Vector2.Distance(touch.screenPosition, _f0StartPos);
                if (moveDist > tapMaxMovePixels)
                {
                    _twoFingerParryArmed = false;
                    _isPerformingOffensiveAction = true;

                    // Route Finger 0 to Main-Hand Blade OR Rune Drawing
                    if (loadoutSystem != null && loadoutSystem.allowedSlashFingers >= 1 && primaryBladeTrail != null)
                    {
                        primaryBladeTrail.enabled = true;
                        float velocity = (touch.screenPosition - _f0PrevPos).magnitude / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                        if (velocity > 600f)
                        {
                            PortraitGestureCanvas.TriggerBladeSlash(_f0PrevPos, touch.screenPosition, velocity);
                        }
                    }
                    else if (loadoutSystem != null && loadoutSystem.allowedRuneFingers >= 1 && runeCastingEngine != null)
                    {
                        if (!runeCastingEngine.IsDrawingRune) runeCastingEngine.BeginDrawing();
                        runeCastingEngine.AddDrawingPoint(touch.screenPosition);
                    }
                }
                _f0PrevPos = touch.screenPosition;
            }
            else if (touch.touchId == _finger1Id)
            {
                float moveDist = Vector2.Distance(touch.screenPosition, _f1StartPos);
                if (moveDist > tapMaxMovePixels)
                {
                    _twoFingerParryArmed = false;

                    // Finger 1 ONLY spawns a second blade trail if Dual-Wielding (2 swords/daggers)
                    if (loadoutSystem != null && loadoutSystem.allowedSlashFingers >= 2 && secondaryBladeTrail != null)
                    {
                        _isPerformingOffensiveAction = true;
                        secondaryBladeTrail.enabled = true;
                        float velocity = (touch.screenPosition - _f1PrevPos).magnitude / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                        if (velocity > 600f)
                        {
                            // Trigger second blade slash with off-hand amber wave
                            secondaryBladeTrail.ExecuteBladeStrike(_f1PrevPos, touch.screenPosition, velocity, new Color(1f, 0.75f, 0.2f), true);
                        }
                    }
                    else if (loadoutSystem != null && loadoutSystem.allowedRuneFingers >= 2 && runeCastingEngine != null)
                    {
                        // Twin-Glyph Weaving with second finger
                        _isPerformingOffensiveAction = true;
                        runeCastingEngine.AddDrawingPoint(touch.screenPosition);
                    }
                }
                _f1PrevPos = touch.screenPosition;
            }
        }

        private void OnFingerUp(Touch touch)
        {
            // Check if a clean Two-Finger Tap Parry occurred
            if (_twoFingerParryArmed && (touch.touchId == _finger0Id || touch.touchId == _finger1Id))
            {
                float duration = Time.unscaledTime - Mathf.Min(_f0StartTime, _f1StartTime);
                if (duration <= tapMaxHoldSeconds)
                {
                    ExecuteTwoFingerParry();
                    _twoFingerParryArmed = false;
                }
            }

            if (touch.touchId == _finger0Id)
            {
                if (runeCastingEngine != null) runeCastingEngine.FinishDrawingStroke();
                _finger0Id = -1;
            }
            else if (touch.touchId == _finger1Id)
            {
                _finger1Id = -1;
            }
        }

        public void ExecuteTwoFingerParry()
        {
            Debug.Log("⚡ [COMBAT DISPATCHER] Universal Two-Finger Tap PARRY Executed!");
            OnTwoFingerParryTriggered?.Invoke();

            if (FruitNinjaBladeCutter.Instance != null)
            {
                FruitNinjaBladeCutter.Instance.TriggerTwoFingerParry();
            }

            var stanceCtrl = GetComponent<PlayerCombatStanceController>() 
                          ?? PlayerCombatStanceController.Instance 
                          ?? FindAnyObjectByType<PlayerCombatStanceController>();
            if (stanceCtrl != null)
            {
                stanceCtrl.ExecuteParry();
            }
        }

        /// <summary>
        /// Invoked when the player draws a Shield Glyph (⛨) inside the magic circle and drags/flicks
        /// from the [CAST] button toward an ally portrait on the right column or into the arena.
        /// </summary>
        public void CastDrawnShieldMagic(Vector2 castButtonPos, Vector2 releaseScreenPos, float baseBarrierHp)
        {
            if (loadoutSystem == null) ResolveDependencies();

            // Apply Staff/Wand multiplier + Second-Hand Tome Affinity Buff (if Tome is Protection affinity)
            float finalBarrierHp = baseBarrierHp;
            if (loadoutSystem != null)
            {
                finalBarrierHp = loadoutSystem.EvaluateSpellOutputWithTomeAffinity(SpellAffinity.Protection, baseBarrierHp);
            }

            // Check if released over any of the 9 Ally Portraits on the right edge
            for (int i = 0; i < allyPortraitSlots.Length; i++)
            {
                if (allyPortraitSlots[i] != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(allyPortraitSlots[i], releaseScreenPos, null))
                {
                    Debug.Log($"🛡️ [PROTECTIVE WARD] Cast Drawn Shield (⛨) onto Ally #{i + 1} (Barrier: {finalBarrierHp:F0} HP)");
                    OnAllyProtectedWithShieldSpell?.Invoke(i, finalBarrierHp);

                    // Visual shield cast feedback on portrait
                    SpawnAllyBarrierVisual(allyPortraitSlots[i].position, finalBarrierHp);
                    return;
                }
            }

            // Otherwise self-cast on the player if tapped in place
            Debug.Log($"🛡️ [PROTECTIVE WARD] Cast Drawn Shield (⛨) onto Player (Barrier: {finalBarrierHp:F0} HP)");
            OnAllyProtectedWithShieldSpell?.Invoke(-1, finalBarrierHp);

            var hero = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Hero");
            if (hero != null)
            {
                SpawnAllyBarrierVisual(hero.transform.position + Vector3.up * 1.1f, finalBarrierHp);
            }
        }

        private void SpawnAllyBarrierVisual(Vector3 worldPos, float barrierHp)
        {
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnFloatingText(
                    worldPos + Vector3.up * 0.4f,
                    $"<color=#60A5FA><b>🛡️ PROTECTIVE WARD +{barrierHp:F0} HP</b></color>",
                    1.2f
                );
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("shield_block", 0.9f);
            }
        }

        public bool IsInsideCentralCombatCanvas(Vector2 pos)
        {
            float nx = pos.x / Screen.width;
            float ny = pos.y / Screen.height;
            return (nx > 0.08f && nx < 0.82f && ny > 0.22f && ny < 0.90f);
        }

        #region Desktop / Editor Fallback Input
        private float mouse0DownTime = -10f;
        private float mouse1DownTime = -10f;

        private void ProcessDesktopFallbackInput()
        {
            // Desktop Key 'F' or Right-Click Mouse triggers Universal Two-Finger Parry
            if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1))
            {
                ExecuteTwoFingerParry();
            }

            // Desktop Simultaneous Left + Right Mouse Click Parry Simulation
            if (Input.GetMouseButtonDown(0)) mouse0DownTime = Time.unscaledTime;
            if (Input.GetMouseButtonDown(1)) mouse1DownTime = Time.unscaledTime;

            if (Mathf.Abs(mouse0DownTime - mouse1DownTime) <= chordSyncWindow && mouse0DownTime > 0 && mouse1DownTime > 0)
            {
                if (Time.unscaledTime - Mathf.Min(mouse0DownTime, mouse1DownTime) < tapMaxHoldSeconds)
                {
                    ExecuteTwoFingerParry();
                    mouse0DownTime = -10f;
                    mouse1DownTime = -10f;
                }
            }
        }
        #endregion
    }
}
