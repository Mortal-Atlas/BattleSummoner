using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Core.DimensionalCompiler;

public enum SpellElement { Healing, Fire, Rock, Water, Lightning, Arcane, Protection }

/// <summary>
/// Phase 4: Rune Circle Shape Casting & Modular Spell Modification
/// Analyzes inner drawn glyphs (Heart = Healing, Triangle = Fire, Square = Rock, Wavy Lines = Water)
/// and dispatches parameterized elemental spells to the active spellcraft subsystem.
/// </summary>
public class RuneShapeCastingEngine : MonoBehaviour
{
    private static RuneShapeCastingEngine _instance;
    private static bool _isQuitting = false;

    public static RuneShapeCastingEngine Instance
    {
        get
        {
            if (_isQuitting) return null;
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<RuneShapeCastingEngine>();
                if (_instance == null && Application.isPlaying && !_isQuitting)
                {
                    GameObject go = new GameObject("RuneShapeCastingEngine");
                    _instance = go.AddComponent<RuneShapeCastingEngine>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private List<Vector2> drawnPoints = new List<Vector2>();
    private bool isDrawingRune = false;

    [Header("12D Compiler Integration")]
    [SerializeField] private DimensionalArtifact activeSpellArtifact;
    public DimensionalArtifact ActiveSpellArtifact => activeSpellArtifact;

    [Header("Visual & UI Cast Integration")]
    [SerializeField] private LineRenderer persistentRuneLine;
    [SerializeField] private GameObject floatingCastButton;

    public SpellAffinity detectedAffinity { get; private set; } = SpellAffinity.Fire;

    public event Action<SpellElement, Vector2> OnSpellCastDispatched;
    public event Action<DimensionalArtifact> OnDimensionalArtifactCompiled;
    public event Action<SpellAffinity, Vector2, float> OnOffensiveSpellLaunched;
    public event Action<int, float> OnAllyProtectiveShieldCast; // allyIndex (0-8), barrierStrength

    public bool IsDrawingRune => isDrawingRune;
    public IReadOnlyList<Vector2> DrawnPoints => drawnPoints;

    public void BeginDrawingRune()
    {
        drawnPoints.Clear();
        isDrawingRune = true;
        Debug.Log("[RUNE CASTING] Began drawing rune shape.");
    }

    public void AddPoint(Vector2 point)
    {
        if (isDrawingRune)
        {
            if (drawnPoints.Count == 0 || Vector2.Distance(drawnPoints[drawnPoints.Count - 1], point) > 4f)
            {
                drawnPoints.Add(point);
            }
        }
    }

    public void BeginDrawing() => BeginDrawingRune();
    public void AddDrawingPoint(Vector2 point) => AddPoint(point);
    public void FinishDrawingStroke() { isDrawingRune = false; }

    public SpellElement AnalyzeInnerGlyph()
    {
        // Heart = Healing, Triangle = Fire, Square = Rock, Wavy Lines = Water
        if (drawnPoints == null || drawnPoints.Count < 5)
        {
            return SpellElement.Fire; // Default elemental fallback
        }

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < drawnPoints.Count; i++)
        {
            minX = Mathf.Min(minX, drawnPoints[i].x);
            maxX = Mathf.Max(maxX, drawnPoints[i].x);
            minY = Mathf.Min(minY, drawnPoints[i].y);
            maxY = Mathf.Max(maxY, drawnPoints[i].y);
        }

        float width = maxX - minX;
        float height = maxY - minY;
        if (width < 10f || height < 10f) return SpellElement.Fire;

        // Count direction reversals (inflections) along Y axis to detect wavy lines
        int horizontalInflections = 0;
        float lastDy = 0f;
        for (int i = 1; i < drawnPoints.Count; i++)
        {
            float dy = drawnPoints[i].y - drawnPoints[i - 1].y;
            if (Mathf.Abs(dy) > 2f)
            {
                if (lastDy != 0f && Mathf.Sign(dy) != Mathf.Sign(lastDy))
                {
                    horizontalInflections++;
                }
                lastDy = dy;
            }
        }

        // Multiple sinusoidal undulations -> Water
        if (horizontalInflections >= 4)
        {
            Debug.Log("[RUNE SHAPE] Analyzed glyph: Wavy Lines -> Water Element");
            return SpellElement.Water;
        }

        // Check for 5-Point Shield Crest (⛨):
        // Flat/broad top spanning wide left & right, with sides tapering down to a single V-apex at the bottom center.
        float midX = minX + width * 0.5f;
        var topPoints = drawnPoints.FindAll(p => p.y > maxY - height * 0.30f);
        var bottomPoints = drawnPoints.FindAll(p => p.y < minY + height * 0.25f);
        bool hasWideTop = topPoints.Exists(p => p.x < minX + width * 0.35f) && topPoints.Exists(p => p.x > maxX - width * 0.35f);
        bool hasNarrowBottom = bottomPoints.Count > 0 && bottomPoints.TrueForAll(p => Mathf.Abs(p.x - midX) < width * 0.35f);
        bool topCenterNotDipped = !drawnPoints.Exists(p => Mathf.Abs(p.x - midX) < width * 0.20f && p.y < maxY - height * 0.25f && p.y > minY + height * 0.5f);

        if (hasWideTop && hasNarrowBottom && topCenterNotDipped && drawnPoints.Count >= 6)
        {
            Debug.Log("[RUNE SHAPE] Analyzed glyph: 5-Point Shield Crest (⛨) -> Protection Element");
            return SpellElement.Protection;
        }

        // Check if start and end are near top center and dips in the middle -> Heart (Healing)
        Vector2 first = drawnPoints[0];
        Vector2 last = drawnPoints[drawnPoints.Count - 1];
        float midY = minY + height * 0.5f;

        // If top has two lobes or closure near bottom tip -> Heart
        if (first.y > midY && last.y > midY && drawnPoints.Exists(p => p.y < minY + height * 0.25f))
        {
            // Heart detected
            Debug.Log("[RUNE SHAPE] Analyzed glyph: Heart Shape -> Healing Element");
            return SpellElement.Healing;
        }

        // Bounding box aspect ratio comparison
        float ratio = width / height;
        if (ratio > 0.82f && ratio < 1.18f && drawnPoints.Count > 12)
        {
            Debug.Log("[RUNE SHAPE] Analyzed glyph: Box / Square -> Rock Element");
            return SpellElement.Rock;
        }

        // Sharp top peak with flat bottom -> Triangle (Fire)
        Debug.Log("[RUNE SHAPE] Analyzed glyph: Triangle -> Fire Element");
        return SpellElement.Fire;
    }

    public void Inscribe9DIncantation(string incantationText)
    {
        EnsureActiveArtifactExists();
        activeSpellArtifact.semanticDescription_9D = incantationText;
        activeSpellArtifact.Compile9DMetadata();
        OnDimensionalArtifactCompiled?.Invoke(activeSpellArtifact);
    }

    private void EnsureActiveArtifactExists()
    {
        if (activeSpellArtifact == null)
        {
            activeSpellArtifact = ScriptableObject.CreateInstance<DimensionalArtifact>();
            activeSpellArtifact.name = "RuntimeRuneArtifact";
            activeSpellArtifact.baseClassName_8D = "RuneSpell_Glyph";
        }
    }

    public void FinalizeCast(Vector2 launchDirection) => FinalizeCastButton(launchDirection);

    public void FinalizeCastButton(Vector2 launchDirection, string incantationText = null)
    {
        SpellElement element = AnalyzeInnerGlyph();
        Vector2 dir = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.up;

        // 12D Dimensional Compiler Hook: Compile 9D semantic incantation
        EnsureActiveArtifactExists();
        activeSpellArtifact.baseClassName_8D = $"RuneSpell_{element}";
        if (!string.IsNullOrEmpty(incantationText))
        {
            activeSpellArtifact.semanticDescription_9D = incantationText;
        }
        else if (string.IsNullOrEmpty(activeSpellArtifact.semanticDescription_9D) || activeSpellArtifact.semanticDescription_9D == "A standard construct.")
        {
            activeSpellArtifact.semanticDescription_9D = $"{element} elemental glyph strike with concentrated kinetic force.";
        }
        activeSpellArtifact.Compile9DMetadata();
        OnDimensionalArtifactCompiled?.Invoke(activeSpellArtifact);

        OnSpellCastDispatched?.Invoke(element, dir);
        isDrawingRune = false;

        // Phase 1 Co-Op Resonance Broadcast
        if (CoopSpellResonanceEngine.Instance != null)
        {
            ExtendedSpellElement extElem = element switch
            {
                SpellElement.Healing => ExtendedSpellElement.Healing,
                SpellElement.Fire => ExtendedSpellElement.Fire,
                SpellElement.Rock => ExtendedSpellElement.Rock,
                SpellElement.Water => ExtendedSpellElement.Water,
                SpellElement.Lightning => ExtendedSpellElement.Lightning,
                _ => ExtendedSpellElement.Wind
            };

            CoopSpellResonanceEngine.Instance.RegisterCastFromRuneCanvas(new ActiveCastIntent
            {
                casterEntityId = "LocalPlayer",
                originPosition = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f : Vector3.zero,
                launchDirection = dir,
                element = extElem,
                geometry = SpellGeometry.Sphere,
                basePower = 100f,
                carrierFrequency10D = activeSpellArtifact != null ? activeSpellArtifact.carrierFrequency_10D : 1.0f,
                timestamp = Time.time
            });
        }

        Debug.Log($"[RUNE CAST DISPATCHED] Element: {element}, Launch Dir: {dir} | 9D Incantation compiled. Mass: {activeSpellArtifact.inertialMass_4D}kg, Density: {activeSpellArtifact.densityMatrix_5D:F1}");

        // Trigger Audio Feedback
        if (BattleSummonerAudioMaster.Instance != null)
        {
            string sfx = element switch
            {
                SpellElement.Healing => "relic_chime",
                SpellElement.Fire => "spell_fireball_launch",
                SpellElement.Rock => "cinematic_boom",
                SpellElement.Water => "malformed_wheeze",
                _ => "spell_fireball_launch"
            };
            BattleSummonerAudioMaster.Instance.PlayPresetSFX(sfx, 0.95f);
        }
    }

    public void DispelDrawnRune()
    {
        drawnPoints.Clear();
        isDrawingRune = false;
        if (persistentRuneLine != null) persistentRuneLine.positionCount = 0;
        if (floatingCastButton != null) floatingCastButton.SetActive(false);
        Debug.Log("[RUNE CASTING] Spell drawing dispelled and cleared.");
    }

    public void DispelRuneCanvas() => DispelDrawnRune();

    public void AddPersistentRunePoint(Vector3 worldPoint)
    {
        AddPoint(new Vector2(worldPoint.x, worldPoint.y));
        if (persistentRuneLine != null)
        {
            persistentRuneLine.positionCount = drawnPoints.Count;
            for (int i = 0; i < drawnPoints.Count; i++)
            {
                persistentRuneLine.SetPosition(i, new Vector3(drawnPoints[i].x, drawnPoints[i].y, worldPoint.z));
            }
        }
    }

    public void FinishDrawingStrokeAndShowCastButton(SpellAffinity recognizedGlyph)
    {
        detectedAffinity = recognizedGlyph;
        FinishDrawingStroke();
        if (floatingCastButton != null) floatingCastButton.SetActive(true);
    }

    /// <summary>
    /// Called when the player flicks from the [CAST] button into the arena or onto the 9-Ally Right Column.
    /// </summary>
    public void ExecuteCastFlick(Vector2 screenReleasePosition, Vector2 flickDirection)
    {
        float potencyMult = SpireLoadoutAndTomeSystem.Instance != null
            ? SpireLoadoutAndTomeSystem.Instance.GetCatalystAndTomeSpellMultiplier(detectedAffinity)
            : 1.0f;

        // Check if dragging a Protective Shield (or Heal) onto the Right-Edge 9-Ally Column (X > 0.82)
        float normalizedX = screenReleasePosition.x / Screen.width;
        float normalizedY = screenReleasePosition.y / Screen.height;

        if ((detectedAffinity == SpellAffinity.Protection || detectedAffinity == SpellAffinity.Healing) &&
            normalizedX >= 0.82f && normalizedY >= 0.15f && normalizedY <= 0.75f)
        {
            int allyIndex = Mathf.Clamp(Mathf.FloorToInt(((0.75f - normalizedY) / 0.60f) * 9f), 0, 8);
            float barrierAmount = 250f * potencyMult;
            OnAllyProtectiveShieldCast?.Invoke(allyIndex, barrierAmount);
            Debug.Log($"[Protective Ward] Cast {detectedAffinity} on Ally Slot #{allyIndex + 1} (Potency: {barrierAmount:F0})!");
        }
        else
        {
            OnOffensiveSpellLaunched?.Invoke(detectedAffinity, flickDirection.normalized, potencyMult);
            FinalizeCast(flickDirection);
        }

        DispelRuneCanvas();
    }
}
