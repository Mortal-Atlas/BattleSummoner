using System;
using UnityEngine;
using BattleSummoner.Hobbies;

public class WeaponTemper : MonoBehaviour
{
    [Header("Living Gear Linkage")]
    [SerializeField] private LivingGearSystem livingGear;

    [Header("Edge Temper Dynamics")]
    [SerializeField] private float hardnessHRC = 61.5f;
    [SerializeField] private float sharpnessDegradationPerSlash = 0.85f;
    [SerializeField] private float whetstoneHoneRestore = 25f;
    [SerializeField] private float heatBuildup = 0f;
    [SerializeField] private float maxHeatThreshold = 100f;

    [Header("Forged Sword Stats")]
    public float baseDamage = 45f;
    public float maxDurability = 150f;
    public float currentDurability = 150f;
    public int maxSockets = 3;

    public event Action<float> OnTemperChanged;
    public event Action OnEdgeDullWarning;
    public event Action OnOverheatWarning;

    public float HardnessHRC => hardnessHRC;
    public float HeatBuildup => heatBuildup;

    private void Awake()
    {
        if (livingGear == null)
        {
            livingGear = GetComponent<LivingGearSystem>();
            if (livingGear == null)
            {
                livingGear = FindAnyObjectByType<LivingGearSystem>();
            }
        }
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnBladeSlash += HandleBladeSlash;
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnBladeSlash -= HandleBladeSlash;
    }

    private void HandleBladeSlash(Vector2 direction, float velocity)
    {
        ApplySlashWear(velocity);
    }

    public void ApplySlashWear(float velocity)
    {
        float wear = (velocity / 1000f) * sharpnessDegradationPerSlash;
        DegradeTemper(wear);
    }

    public void DegradeTemper(float wear)
    {
        if (livingGear == null) return;

        // Singular Grip Contract: -25% temper degradation on OneHanded
        var contractSys = BindingContractSystem.Instance;
        if (contractSys != null && contractSys.signedContracts.Exists(c => c.favoredWeaponType == WeaponSlotType.OneHanded))
        {
            wear *= 0.75f;
        }

        livingGear.currentTemper = Mathf.Max(0f, livingGear.currentTemper - wear);
        heatBuildup = Mathf.Min(maxHeatThreshold, heatBuildup + wear * 1.5f);

        OnTemperChanged?.Invoke(livingGear.currentTemper);

        if (livingGear.currentTemper < 20f)
        {
            OnEdgeDullWarning?.Invoke();
        }

        if (heatBuildup >= maxHeatThreshold * 0.9f)
        {
            OnOverheatWarning?.Invoke();
        }
    }

    public void HoneWithWhetstone()
    {
        if (livingGear == null) return;

        float maxAllowed = Mathf.Max(0f, livingGear.maxTemper - livingGear.fractureDebt);
        livingGear.currentTemper = Mathf.Min(maxAllowed, livingGear.currentTemper + whetstoneHoneRestore);
        heatBuildup = Mathf.Max(0f, heatBuildup - 30f);

        OnTemperChanged?.Invoke(livingGear.currentTemper);
        Debug.Log($"[WEAPON TEMPER] Blade honed with whetstone. Current temper: {livingGear.currentTemper:F1}/{maxAllowed:F1}");
    }

    public void QuenchCooling(float amount)
    {
        heatBuildup = Mathf.Max(0f, heatBuildup - amount);
    }

    public void RepairWeapon(float amount)
    {
        if (livingGear != null)
        {
            livingGear.fractureDebt = Mathf.Max(0f, livingGear.fractureDebt - amount);
            livingGear.currentTemper = Mathf.Min(livingGear.maxTemper, livingGear.currentTemper + amount);
            heatBuildup = 0f;
            OnTemperChanged?.Invoke(livingGear.currentTemper);
        }
    }

    /// <summary>
    /// Called when the player finishes hammering the sword at a campfire or hub anvil.
    /// Calls BlacksmithForgeController.QuenchAndFinishSword() to overwrite the sword's baseDamage, maxDurability, and maxSockets.
    /// </summary>
    public ForgedSwordStats FinishHammeringAtForge(bool atCampfire = false)
    {
        var forgedStats = BlacksmithForgeController.QuenchAndFinishSword(this, livingGear, atCampfire);
        if (forgedStats != null)
        {
            baseDamage = forgedStats.baseDamage;
            maxDurability = forgedStats.maxDurability;
            maxSockets = forgedStats.maxSockets;
        }
        return forgedStats;
    }

    /// <summary>
    /// Pipes raw swipe velocity damage through the UncappedAccessorySystem stack aggregation matrix.
    /// Incorporates blade temper, base damage, and hardness HRC.
    /// </summary>
    public float CalculateTotalStrikeDamage(float rawSwipeDamage)
    {
        float effectiveBase = (baseDamage > 0f) ? (rawSwipeDamage + baseDamage) : rawSwipeDamage;
        float temperedBase = effectiveBase * (hardnessHRC / 50f);
        if (livingGear != null && livingGear.maxTemper > 0f)
        {
            temperedBase *= Mathf.Clamp01(livingGear.currentTemper / livingGear.maxTemper);
        }

        if (UncappedAccessorySystem.Instance != null)
        {
            return UncappedAccessorySystem.Instance.CalculateTotalStrikeOutput(temperedBase);
        }

        return temperedBase;
    }
}
