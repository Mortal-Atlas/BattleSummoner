using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpellElement { Healing, Fire, Rock, Water }

/// <summary>
/// Phase 4: Rune Circle Shape Casting & Modular Spell Modification
/// Analyzes inner drawn glyphs (Heart = Healing, Triangle = Fire, Square = Rock, Wavy Lines = Water)
/// and dispatches parameterized elemental spells to the active spellcraft subsystem.
/// </summary>
public class RuneShapeCastingEngine : MonoBehaviour
{
    private static RuneShapeCastingEngine _instance;
    public static RuneShapeCastingEngine Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<RuneShapeCastingEngine>();
                if (_instance == null)
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

    private List<Vector2> drawnPoints = new List<Vector2>();
    private bool isDrawingRune = false;

    public event Action<SpellElement, Vector2> OnSpellCastDispatched;

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

    public void FinalizeCastButton(Vector2 launchDirection)
    {
        SpellElement element = AnalyzeInnerGlyph();
        Vector2 dir = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.up;
        OnSpellCastDispatched?.Invoke(element, dir);
        isDrawingRune = false;

        Debug.Log($"[RUNE CAST DISPATCHED] Element: {element}, Launch Dir: {dir}");

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
        Debug.Log("[RUNE CASTING] Spell drawing dispelled and cleared.");
    }
}
