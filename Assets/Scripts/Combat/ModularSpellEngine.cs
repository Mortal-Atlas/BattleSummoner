using System;
using UnityEngine;

public enum SpellCatalyst
{
    Wind,
    Fire,
    Earth,
    VoidGravity,
    Blood
}

public enum SpellGeometry
{
    Sphere,
    RazorDisc,
    PlanarDisc = RazorDisc,
    JetThrust,
    DualFunnelThrust = JetThrust,
    Singularity,
    RadialBurst
}

public enum SpellBehavior
{
    Piercing,
    Homing,
    StationaryVortex,
    ChainDetonation
}

[Serializable]
public struct SpellPayload
{
    public string spellName;
    public string baseCatalyst;
    public string geometryShape;
    public int requiredFloorTier;
    public bool isBannedWorldBreaker;
}

public interface ICombatEntity
{
    void ApplyTrueDamage(float damage);
}

/// <summary>
/// ModularSpellEngine compiles spells from Base Catalyst + Geometry + Behavior.
/// Enforces Spire Anti-Nuke Thermodynamic Laws: If Instability (I) > 1.0, triggers
/// an instant backfire: 10,000 True Damage to player, destroys active weapon durability,
/// and logs catastrophic death.
/// </summary>
public class ModularSpellEngine : MonoBehaviour
{
    public static ModularSpellEngine Instance { get; private set; }

    [Header("Current Floor Clearance")]
    [SerializeField] private int currentFloor = 5;
    [SerializeField] private int currentFloorTier = 4;

    [Header("Linkages")]
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private LivingGearSystem activeWeapon;

    public void ExecuteSpell(SpellPayload spell, ICombatEntity caster)
    {
        if (spell.isBannedWorldBreaker || spell.requiredFloorTier > currentFloorTier)
        {
            TriggerCatastrophicBackfire(caster, "Thermodynamic Limit Exceeded: Conceptual instability triggered spontaneous core implosion.");
            return;
        }

        Debug.Log($"[Spell Engine] Successfully executed modified spell: {spell.spellName}");
    }

    private void TriggerCatastrophicBackfire(ICombatEntity caster, string reason)
    {
        Debug.LogWarning($"[BACKFIRE] {reason}");
        caster.ApplyTrueDamage(99999f);
    }

    public static event Action<SpellCatalyst, SpellGeometry, SpellBehavior, float> OnSpellCastSucceeded;
    public static event Action<string, float> OnCatastrophicBackfireOccurred;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (playerProgression == null) playerProgression = GetComponent<PlayerProgression>();
        if (playerProgression == null) playerProgression = FindAnyObjectByType<PlayerProgression>();
        if (activeWeapon == null) activeWeapon = FindAnyObjectByType<LivingGearSystem>();
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnCircleRuneDrawn += HandleCircleSpawn;
        PortraitGestureCanvas.OnSpellPinchCompression += HandlePinchCompress;
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnCircleRuneDrawn -= HandleCircleSpawn;
        PortraitGestureCanvas.OnSpellPinchCompression -= HandlePinchCompress;
    }

    private void HandleCircleSpawn(Vector2 screenPosition, float radius)
    {
        Debug.Log($"[ModularSpellEngine] Circle Rune inscribed at screen point {screenPosition} (radius: {radius:F1}). Matrix primed.");
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("glitch", 0.5f);
        }
    }

    private void HandlePinchCompress(float pinchDelta)
    {
        if (pinchDelta < -15f)
        {
            // Rapid inward pinch casts the primed Gale Chakram (RazorDisc + Wind + Piercing)
            CastModularSpell(SpellCatalyst.Wind, SpellGeometry.RazorDisc, SpellBehavior.Piercing);
        }
    }

    public void ExecuteStructuredSpell(string catalystStr, string geometryStr, string behaviorStr, float requestedInstability)
    {
        SpellCatalyst catalyst = ParseEnum(catalystStr, SpellCatalyst.VoidGravity);
        SpellGeometry geometry = ParseEnum(geometryStr, SpellGeometry.Singularity);
        SpellBehavior behavior = ParseEnum(behaviorStr, SpellBehavior.ChainDetonation);

        CastModularSpell(catalyst, geometry, behavior, requestedInstability);
    }

    public void CastModularSpell(SpellCatalyst catalyst, SpellGeometry geometry, SpellBehavior behavior, float overrideInstability = -1f)
    {
        float instability = overrideInstability >= 0f ? overrideInstability : CalculateInstability(catalyst, geometry, behavior, currentFloor);

        // Thermodynamic Anti-Nuke Rule Check: I > 1.0 triggers instant catastrophic backfire
        if (instability > 1.0f)
        {
            TriggerCatastrophicBackfire(catalyst, geometry, behavior, instability);
            return;
        }

        // Valid Spell Execution
        Debug.Log($"[ModularSpellEngine] Spell materialized: [{catalyst}] [{geometry}] [{behavior}] with Instability {instability:F2} (Safe).");
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX(catalyst == SpellCatalyst.Fire ? "cinematic_boom" : "relic_chime", 0.8f);
        }

        OnSpellCastSucceeded?.Invoke(catalyst, geometry, behavior, instability);
    }

    public float CalculateInstability(SpellCatalyst catalyst, SpellGeometry geometry, SpellBehavior behavior, int floor)
    {
        float catalystWeight = catalyst switch
        {
            SpellCatalyst.Wind => 0.20f,
            SpellCatalyst.Fire => 0.35f,
            SpellCatalyst.Earth => 0.25f,
            SpellCatalyst.Blood => 0.60f,
            SpellCatalyst.VoidGravity => 1.80f,
            _ => 0.30f
        };

        float geometryWeight = geometry switch
        {
            SpellGeometry.Sphere => 1.0f,
            SpellGeometry.RazorDisc => 1.25f,
            SpellGeometry.JetThrust => 1.15f,
            SpellGeometry.RadialBurst => 1.40f,
            SpellGeometry.Singularity => 2.40f,
            _ => 1.0f
        };

        float behaviorWeight = behavior switch
        {
            SpellBehavior.Piercing => 1.1f,
            SpellBehavior.Homing => 1.2f,
            SpellBehavior.StationaryVortex => 1.35f,
            SpellBehavior.ChainDetonation => 1.60f,
            _ => 1.0f
        };

        // Higher floors provide greater celestial containment fields
        float floorContainmentFactor = Mathf.Max(1.0f, floor * 0.05f);

        return (catalystWeight * geometryWeight * behaviorWeight) / floorContainmentFactor;
    }

    private void TriggerCatastrophicBackfire(SpellCatalyst catalyst, SpellGeometry geometry, SpellBehavior behavior, float instability)
    {
        string deathReason = $"Thermodynamic Singularity Backfire! Instability {instability:F2} exceeded 1.0 limit on Floor {currentFloor}.";
        Debug.LogError($"[FATAL BACKFIRE]: {deathReason}");

        // 1. Play procedural boom & glitch audio
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("singularity_whir", 1.0f);
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("anvil_shatter", 1.0f);
        }

        // 2. Action: Deal 10,000 True Damage to the player
        if (playerProgression != null)
        {
            playerProgression.TakeDirectDamage(10000f);
        }

        // 3. Action: Destroy active weapon durability
        if (activeWeapon != null)
        {
            activeWeapon.currentTemper = 0f;
            activeWeapon.fractureDebt = activeWeapon.maxTemper;
            Debug.LogWarning($"[ModularSpellEngine] Active weapon {activeWeapon.weaponName} destroyed by thermodynamic collapse. Anima core fractured.");
        }

        // 4. Log secret achievement
        if (SystemBroadcastManager.Instance != null && playerProgression != null)
        {
            SystemBroadcastManager.Instance.TriggerAchievement("ach_secret_forbidden_collapse", playerProgression.playerName);
        }

        // 5. Partition flag check: If severe thermodynamic override was attempted on low floors, flag quarantine
        if (currentFloor <= 10 && catalyst == SpellCatalyst.VoidGravity && instability >= 1.4f)
        {
            TribunalAppealController.FlagPlayerForQuarantine("Attempted Gravitational Singularity on Lower Floors");
        }

        OnCatastrophicBackfireOccurred?.Invoke(deathReason, instability);
    }

    private T ParseEnum<T>(string value, T defaultValue) where T : struct
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (Enum.TryParse<T>(value, true, out T result)) return result;
        return defaultValue;
    }
}
