using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BattleSummoner.Combat;

public enum InteractionMode
{
    Combat = 0,
    CombatSlash = 0,
    Spellcraft = 1,
    RuneCast = 1,
    BlacksmithForging = 2,
    RelicInspection = 3,
    RelicInspect = 3,
    MiningChisel = 4,
    InstrumentKalimba = 5,
    InstrumentPlay = 5,
    SummoningCircle = 6,
    GuardianSignature = 7,
    CookingMamaSlicing = 8,
    PracticeRoomRecording = 9,
    TavernArmWrestling = 10,
    SecretKnockDoor = 11
}

public class PortraitGestureCanvas : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Mode Configuration")]
    public InteractionMode currentMode = InteractionMode.CombatSlash;

    [Header("Swipe Parameters")]
    [SerializeField] private float minSlashDistance = 60f;
    [SerializeField] private float minSlashVelocity = 750f;

    [Header("Rune Detection")]
    [SerializeField] private float circleTolerance = 0.35f;
    [SerializeField] private float minCircleRadius = 50f;

    private Vector2 startTouchPosition;
    private float startTouchTime;
    private Vector2 swipeStartPos;
    private float swipeStartTime;
    private readonly List<Vector2> touchTrail = new List<Vector2>();
    private bool trackingTouch = false;

    // Multi-stroke tracking for composite runes (e.g., 'V' with dash)
    private readonly List<List<Vector2>> completedStrokes = new List<List<Vector2>>();
    private float lastStrokeTime = 0f;
    private const float MULTI_STROKE_WINDOW = 0.65f;

    // Phase 3 Spec Events
    public event Action<Vector2, float> OnSwipeRecognized;
    public event Action<Vector2> OnChiselTap;
    public event Action<Vector2> OnInspectDrag;

    // Global Event Dispatchers with strict single-swipe debounce
    public static event Action<Vector2, float> OnBladeSlash;
    public static event Action<Vector2, Vector2, float> OnScreenBladeSlash; // (startScreenPos, endScreenPos, velocity)
    private static float lastGlobalBladeSlashTime = -10f;
    private const float MIN_SLASH_COOLDOWN = 0.20f;

    public static void TriggerBladeSlash(Vector2 direction, float velocity)
    {
        if (Time.time - lastGlobalBladeSlashTime < MIN_SLASH_COOLDOWN) return;
        lastGlobalBladeSlashTime = Time.time;

        OnBladeSlash?.Invoke(direction, velocity);
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        OnScreenBladeSlash?.Invoke(center - direction * 120f, center + direction * 120f, velocity);
    }

    public static void TriggerBladeSlash(Vector2 startScreenPos, Vector2 endScreenPos, float velocity)
    {
        if (Time.time - lastGlobalBladeSlashTime < MIN_SLASH_COOLDOWN) return;
        lastGlobalBladeSlashTime = Time.time;

        Vector2 dir = (endScreenPos - startScreenPos).normalized;
        OnBladeSlash?.Invoke(dir, velocity);
        OnScreenBladeSlash?.Invoke(startScreenPos, endScreenPos, velocity);
    }
    public static event Action<Vector2, float> OnCircleRuneDrawn;
    public static event Action<float, Vector2, float> OnSummoningCircleDrawn; // (accuracyScore, centroid, avgRadius)
    public static event Action<float> OnSpellPinchCompression;
    public static event Action<Vector2> OnSpellTapCast;
    public static event Action<Vector2> OnHammerTap;
    public static event Action<Vector2> OnMiningChiselTap;
    public static event Action<Vector2> OnRelicPointerDown;
    public static event Action<Vector2> OnRelicPointerUp;
    public static event Action<Vector2, Vector2> OnRelicDrag;
    public static event Action<Vector2> OnRelicOrbit;
    public static event Action<float> OnRelicZoom;
    public static event Action<Vector2> OnRelicTap;

    private void Awake()
    {
        // Enforce Phase 3: Central gesture tracking zone MUST be transparent (Alpha = 0)
        // with RaycastTarget = true so it catches swipes without visually suffocating the 3D dungeon.
        UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = 0.002f; // Non-zero so GraphicRaycaster never culls on mobile devices
            img.color = c;
            img.raycastTarget = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        startTouchPosition = eventData.position;
        startTouchTime = Time.time;
        swipeStartPos = eventData.position;
        swipeStartTime = Time.time;
        touchTrail.Clear();
        touchTrail.Add(eventData.position);
        trackingTouch = true;

        if (ActiveSpellLockManager.Instance != null && ActiveSpellLockManager.Instance.CurrentState == ActiveSpellLockState.Locked_Fireball)
        {
            ActiveSpellLockManager.Instance.OnFireballPointerDown(eventData.position);
            return;
        }

        if (BattleSummoner.Combat.WizardSpellSlingerController.Instance != null && 
            BattleSummoner.Combat.WizardSpellSlingerController.Instance.IsSpellModeActive &&
            (BattleSummoner.Combat.WizardSpellSlingerController.Instance.ActivePreset == BattleSummoner.Combat.SpellSlingerPreset.LightningRay ||
             BattleSummoner.Combat.WizardSpellSlingerController.Instance.ActivePreset == BattleSummoner.Combat.SpellSlingerPreset.WaterTorrent ||
             BattleSummoner.Combat.WizardSpellSlingerController.Instance.IsHoldingSpell))
        {
            BattleSummoner.Combat.WizardSpellSlingerController.Instance.OnPointerDown(eventData.position);
            return;
        }

        // Forward to SpellDrawingInkCanvas for real-time paper ink markings
        if (SpellDrawingInkCanvas.Instance != null)
        {
            SpellDrawingInkCanvas.Instance.OnPointerDown(eventData);
        }

        if (currentMode == InteractionMode.MiningChisel)
        {
            OnChiselTap?.Invoke(eventData.position);
            OnMiningChiselTap?.Invoke(eventData.position);
        }
        else if (currentMode == InteractionMode.BlacksmithForging)
        {
            OnHammerTap?.Invoke(eventData.position);
        }
        else if (currentMode == InteractionMode.RelicInspection || currentMode == InteractionMode.RelicInspect)
        {
            OnRelicPointerDown?.Invoke(eventData.position);
        }
        else if (currentMode == InteractionMode.Spellcraft || currentMode == InteractionMode.RuneCast)
        {
            if (RuneShapeCastingEngine.Instance != null)
            {
                RuneShapeCastingEngine.Instance.BeginDrawingRune();
                RuneShapeCastingEngine.Instance.AddPoint(eventData.position);
            }
        }
        else if (currentMode == InteractionMode.GuardianSignature)
        {
            if (BattleSummoner.Core.GuardianSignatureBinder.Instance != null)
            {
                BattleSummoner.Core.GuardianSignatureBinder.Instance.BeginSignatureStroke();
                BattleSummoner.Core.GuardianSignatureBinder.Instance.RecordSignaturePoint(eventData.position);
            }
        }
        else if (currentMode == InteractionMode.TavernArmWrestling)
        {
            BattleSummoner.Social.TavernSocialAndWorldEventsManager.Instance?.RegisterArmWrestleAlternateTap();
        }
        else if (currentMode == InteractionMode.SecretKnockDoor)
        {
            BattleSummoner.Social.TavernSocialAndWorldEventsManager.Instance?.ValidateSecretDoorKnockTap();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentMode == InteractionMode.GuardianSignature)
        {
            BattleSummoner.Core.GuardianSignatureBinder.Instance?.RecordSignaturePoint(eventData.position);
            return;
        }

        if (currentMode == InteractionMode.RelicInspect || currentMode == InteractionMode.RelicInspection)
        {
            OnInspectDrag?.Invoke(eventData.delta);
        }

        if (!trackingTouch) return;
        touchTrail.Add(eventData.position);

        if (ActiveSpellLockManager.Instance != null && ActiveSpellLockManager.Instance.CurrentState == ActiveSpellLockState.Locked_Fireball)
        {
            ActiveSpellLockManager.Instance.OnFireballDrag(eventData.position);
            return;
        }

        if (BattleSummoner.Combat.WizardSpellSlingerController.Instance != null && BattleSummoner.Combat.WizardSpellSlingerController.Instance.IsSpellModeActive && BattleSummoner.Combat.WizardSpellSlingerController.Instance.IsHoldingSpell)
        {
            BattleSummoner.Combat.WizardSpellSlingerController.Instance.OnDrag(eventData.position);
            return;
        }

        // Forward to SpellDrawingInkCanvas for real-time paper ink markings
        if (SpellDrawingInkCanvas.Instance != null && SpellDrawingInkCanvas.Instance.IsCurrentlyDrawing)
        {
            SpellDrawingInkCanvas.Instance.OnDrag(eventData);
        }

        if (ActiveSpellLockManager.Instance != null && ActiveSpellLockManager.Instance.CurrentState == ActiveSpellLockState.Locked_Healing)
        {
            // Continuous heart circuit / circle loop tracking
            if (RuneGestureRecognizer.CheckClosedCircuit(touchTrail, 12, out var closedCircuit, out float acc, out float rad))
            {
                ActiveSpellLockManager.Instance.ProcessCompletedHealCircuit(acc, rad);
                Vector2 lastPt = touchTrail[touchTrail.Count - 1];
                touchTrail.Clear();
                touchTrail.Add(lastPt);
            }
            return;
        }

        if (currentMode == InteractionMode.RelicInspection || currentMode == InteractionMode.RelicInspect)
        {
            try
            {
                if (Input.touchSupported && Input.touchCount >= 2)
                {
                    Touch t0 = Input.GetTouch(0);
                    Touch t1 = Input.GetTouch(1);
                    float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                    float currentDist = (t0.position - t1.position).magnitude;
                    OnRelicZoom?.Invoke(currentDist - prevDist);
                }
                else
                {
                    OnRelicDrag?.Invoke(eventData.position, eventData.delta);
                    OnRelicOrbit?.Invoke(eventData.delta);
                }
            }
            catch
            {
                OnRelicDrag?.Invoke(eventData.position, eventData.delta);
                OnRelicOrbit?.Invoke(eventData.delta);
            }
            return;
        }

        if (currentMode == InteractionMode.Spellcraft || currentMode == InteractionMode.RuneCast)
        {
            if (RuneShapeCastingEngine.Instance != null && RuneShapeCastingEngine.Instance.IsDrawingRune)
            {
                RuneShapeCastingEngine.Instance.AddPoint(eventData.position);
            }

            try
            {
                if (Input.touchSupported && Input.touchCount >= 2)
                {
                    Touch t0 = Input.GetTouch(0);
                    Touch t1 = Input.GetTouch(1);
                    float prevDist = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                    float currentDist = (t0.position - t1.position).magnitude;
                    OnSpellPinchCompression?.Invoke(currentDist - prevDist);
                }
            }
            catch
            {
                // Silently fallback if pure new input system is active
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Phase 3 Spec swipe recognition check
        if (currentMode == InteractionMode.CombatSlash || currentMode == InteractionMode.Combat)
        {
            Vector2 delta = eventData.position - swipeStartPos;
            float duration = Time.time - swipeStartTime;

            if (delta.magnitude > 50f && duration < 0.4f)
            {
                Vector2 velocityVector = delta / duration;
                OnSwipeRecognized?.Invoke(velocityVector.normalized, velocityVector.magnitude);
            }
        }

        if (!trackingTouch) return;
        trackingTouch = false;

        if (ActiveSpellLockManager.Instance != null && ActiveSpellLockManager.Instance.CurrentState == ActiveSpellLockState.Locked_Fireball)
        {
            ActiveSpellLockManager.Instance.OnFireballPointerUp(eventData.position);
            touchTrail.Clear();
            return;
        }

        if (BattleSummoner.Combat.WizardSpellSlingerController.Instance != null && BattleSummoner.Combat.WizardSpellSlingerController.Instance.IsSpellModeActive && BattleSummoner.Combat.WizardSpellSlingerController.Instance.IsHoldingSpell)
        {
            BattleSummoner.Combat.WizardSpellSlingerController.Instance.OnPointerUp(eventData.position);
            touchTrail.Clear();
            return;
        }

        // Forward to SpellDrawingInkCanvas for final shape recognition
        if (SpellDrawingInkCanvas.Instance != null && SpellDrawingInkCanvas.Instance.IsCurrentlyDrawing)
        {
            SpellDrawingInkCanvas.Instance.OnPointerUp(eventData);
        }

        if (ActiveSpellLockManager.Instance != null && ActiveSpellLockManager.Instance.CurrentState == ActiveSpellLockState.Locked_Healing)
        {
            if (RuneGestureRecognizer.CheckClosedCircuit(touchTrail, 8, out var closedCircuit, out float acc, out float rad))
            {
                ActiveSpellLockManager.Instance.ProcessCompletedHealCircuit(acc, rad);
            }
            touchTrail.Clear();
            return;
        }

        if (currentMode == InteractionMode.RelicInspection || currentMode == InteractionMode.RelicInspect)
        {
            OnRelicPointerUp?.Invoke(eventData.position);
            float touchDur = Mathf.Max(0.001f, Time.time - startTouchTime);
            Vector2 touchDelta = eventData.position - startTouchPosition;
            if (touchDur <= 0.28f && touchDelta.magnitude <= 24f)
            {
                OnRelicTap?.Invoke(eventData.position);
            }
            return;
        }

        if (currentMode == InteractionMode.GuardianSignature)
        {
            BattleSummoner.Core.GuardianSignatureBinder.Instance?.EndSignatureStroke();
            touchTrail.Clear();
            return;
        }

        if (currentMode == InteractionMode.CookingMamaSlicing)
        {
            Vector2 deltaSlice = eventData.position - startTouchPosition;
            float dur = Mathf.Max(0.001f, Time.time - startTouchTime);
            if (deltaSlice.magnitude >= minSlashDistance && (deltaSlice.magnitude / dur) >= minSlashVelocity)
            {
                FruitNinjaBladeCutter.Instance?.HandleScreenBladeSlash(startTouchPosition, eventData.position, deltaSlice.magnitude / dur);
            }
            touchTrail.Clear();
            return;
        }

        if (currentMode == InteractionMode.TavernArmWrestling)
        {
            Vector2 deltaWrestle = eventData.position - startTouchPosition;
            if (deltaWrestle.magnitude >= 75f)
            {
                BattleSummoner.Social.TavernSocialAndWorldEventsManager.Instance?.CounterArmWrestleSurge();
            }
            touchTrail.Clear();
            return;
        }

        if (currentMode == InteractionMode.PracticeRoomRecording)
        {
            Vector2 deltaPractice = eventData.position - startTouchPosition;
            float durPractice = Mathf.Max(0.001f, Time.time - startTouchTime);
            float velPractice = deltaPractice.magnitude / durPractice;
            if (deltaPractice.magnitude >= minSlashDistance && velPractice >= minSlashVelocity)
            {
                var recorder = BattleSummoner.Combat.CustomSpecialMoveRecorder.Instance;
                if (recorder != null)
                {
                    var move = new BattleSummoner.Combat.RecordedMoveNode
                    {
                        actionType = BattleSummoner.Combat.RecordedActionType.MeleeDirectionalSlash,
                        swipeDirectionOrCastVector = deltaPractice.normalized,
                        basePowerMultiplier = 1.0f
                    };
                    recorder.RecordStepInPracticeRoom(true, move);
                }
            }
        }

        if (currentMode == InteractionMode.MiningChisel || currentMode == InteractionMode.InstrumentKalimba || currentMode == InteractionMode.InstrumentPlay || currentMode == InteractionMode.SecretKnockDoor) return;

        if (currentMode == InteractionMode.SummoningCircle)
        {
            EvaluateSummoningCircle();
            return;
        }

        // Multi-stroke tracking & shape recognition (Cartoon Heart & V with Dash)
        if (Time.time - lastStrokeTime > MULTI_STROKE_WINDOW)
        {
            completedStrokes.Clear();
        }
        completedStrokes.Add(new List<Vector2>(touchTrail));
        lastStrokeTime = Time.time;

        if (touchTrail.Count >= 6)
        {
            var shapeResult = RuneGestureRecognizer.AnalyzeShape(touchTrail, completedStrokes);
            if (shapeResult.shape == RecognizedRuneShape.CartoonHeart || shapeResult.shape == RecognizedRuneShape.VWithDash)
            {
                completedStrokes.Clear();
                touchTrail.Clear();
                if (ActiveSpellLockManager.Instance != null)
                {
                    ActiveSpellLockManager.Instance.OnShapeRecognized(shapeResult);
                }
                return;
            }
        }

        float durationGeneral = Mathf.Max(0.001f, Time.time - startTouchTime);
        Vector2 deltaGeneral = eventData.position - startTouchPosition;
        float distance = deltaGeneral.magnitude;
        float velocity = distance / durationGeneral;

        if (currentMode == InteractionMode.Combat || currentMode == InteractionMode.CombatSlash)
        {
            // Check if a circle rune was drawn
            if (touchTrail.Count >= 10 && EvaluateCircleRune())
            {
                return;
            }

            if (distance >= minSlashDistance && velocity >= minSlashVelocity)
            {
                if (Time.time - lastGlobalBladeSlashTime >= MIN_SLASH_COOLDOWN)
                {
                    lastGlobalBladeSlashTime = Time.time;
                    OnBladeSlash?.Invoke(deltaGeneral.normalized, velocity);
                }
            }
        }
        else if (currentMode == InteractionMode.Spellcraft || currentMode == InteractionMode.RuneCast)
        {
            if (RuneShapeCastingEngine.Instance != null && RuneShapeCastingEngine.Instance.IsDrawingRune && touchTrail.Count >= 8)
            {
                SpellElement element = RuneShapeCastingEngine.Instance.AnalyzeInnerGlyph();
                if (SpellCastingFlowController.Instance != null)
                {
                    SpellCastingFlowController.Instance.PrimeSpell(element, eventData.position, 120f, $"Rune Shape ({element})");
                }
                else
                {
                    RuneShapeCastingEngine.Instance.FinalizeCastButton(deltaGeneral);
                }
            }
            else if (!EvaluateCircleRune())
            {
                if (distance >= minSlashDistance && velocity >= minSlashVelocity)
                {
                    OnBladeSlash?.Invoke(deltaGeneral.normalized, velocity);
                }
                else if (durationGeneral <= 0.4f && distance <= 45f)
                {
                    OnSpellTapCast?.Invoke(eventData.position);
                }
            }
        }
    }

    private bool EvaluateCircleRune()
    {
        if (touchTrail.Count < 14) return false;

        Vector2 start = touchTrail[0];
        Vector2 end = touchTrail[touchTrail.Count - 1];
        if (Vector2.Distance(start, end) > 90f) return false;

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < touchTrail.Count; i++) centroid += touchTrail[i];
        centroid /= touchTrail.Count;

        float avgRadius = 0f;
        for (int i = 0; i < touchTrail.Count; i++) avgRadius += Vector2.Distance(centroid, touchTrail[i]);
        avgRadius /= touchTrail.Count;

        if (avgRadius < minCircleRadius) return false;

        float variance = 0f;
        for (int i = 0; i < touchTrail.Count; i++)
        {
            variance += Mathf.Abs(Vector2.Distance(centroid, touchTrail[i]) - avgRadius);
        }
        variance /= touchTrail.Count;

        if (variance < avgRadius * circleTolerance)
        {
            OnCircleRuneDrawn?.Invoke(centroid, avgRadius);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Step 3 of Patronus Summoning Ritual: Calculates the geometric variance of the drawn magic circle
    /// from a mathematically perfect circle to generate an Accuracy Score (0% - 100%).
    /// </summary>
    private void EvaluateSummoningCircle()
    {
        if (touchTrail.Count < 10)
        {
            OnSummoningCircleDrawn?.Invoke(0f, Vector2.zero, 0f);
            return;
        }

        Vector2 start = touchTrail[0];
        Vector2 end = touchTrail[touchTrail.Count - 1];

        // 1. Calculate centroid
        Vector2 centroid = Vector2.zero;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int i = 0; i < touchTrail.Count; i++)
        {
            Vector2 p = touchTrail[i];
            centroid += p;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        centroid /= touchTrail.Count;

        // 2. Average radius
        float avgRadius = 0f;
        for (int i = 0; i < touchTrail.Count; i++)
        {
            avgRadius += Vector2.Distance(centroid, touchTrail[i]);
        }
        avgRadius /= touchTrail.Count;

        if (avgRadius < 25f)
        {
            OnSummoningCircleDrawn?.Invoke(0f, centroid, avgRadius);
            return;
        }

        // 3. Radial Variance Error: 1/N * sum(|r_i - avg_r| / avg_r)
        float radialVarianceError = 0f;
        for (int i = 0; i < touchTrail.Count; i++)
        {
            radialVarianceError += Mathf.Abs(Vector2.Distance(centroid, touchTrail[i]) - avgRadius);
        }
        radialVarianceError /= (touchTrail.Count * avgRadius);

        // 4. Closure Error: |start - end| / avg_r
        float closureError = Vector2.Distance(start, end) / avgRadius;

        // 5. Aspect Ratio Error: |1.0 - width / height|
        float width = Mathf.Max(1f, maxX - minX);
        float height = Mathf.Max(1f, maxY - minY);
        float aspectError = Mathf.Abs(1.0f - (width / height));

        // 6. Final Accuracy Score Calculation (0% - 100%)
        // Perfect circle = 0 variance, 0 closure error, 0 aspect error -> 100%
        float combinedPenalty = (radialVarianceError * 1.5f) + (closureError * 0.45f) + (aspectError * 0.45f);
        float accuracyScore = Mathf.Clamp((1.0f - combinedPenalty) * 100f, 0f, 100f);

        Debug.Log($"[ASTRAL CIRCLE UNISTROKE] RadialVar: {radialVarianceError:F3}, ClosureErr: {closureError:F3}, AspectErr: {aspectError:F3} => Accuracy Score: {accuracyScore:F1}%");

        if (accuracyScore >= 98f)
        {
            if (BattleSummonerAudioMaster.Instance != null) BattleSummonerAudioMaster.Instance.PlayPresetSFX("apex_celestial_roar", 0.9f);
        }
        else if (accuracyScore < 50f)
        {
            if (BattleSummonerAudioMaster.Instance != null) BattleSummonerAudioMaster.Instance.PlayPresetSFX("malformed_wheeze", 0.9f);
        }
        else
        {
            if (BattleSummonerAudioMaster.Instance != null) BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.8f);
        }

        OnSummoningCircleDrawn?.Invoke(accuracyScore, centroid, avgRadius);
    }
}
