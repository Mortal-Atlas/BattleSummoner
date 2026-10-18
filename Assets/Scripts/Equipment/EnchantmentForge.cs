using System;
using UnityEngine;
using BattleSummoner.Hobbies;

/// <summary>
/// EnchantmentForge represents a campfire or hub anvil forge station where
/// players hammer, temper, quench, and slot enchantments onto their weapons.
/// </summary>
public class EnchantmentForge : MonoBehaviour
{
    public static EnchantmentForge Instance { get; private set; }

    [Header("Forge Station Type")]
    public bool isCampfire = false;
    public bool isHubAnvil = true;

    [Header("Forged Sword Stats")]
    public float baseDamage = 45f;
    public int maxDurability = 150;
    public int maxSockets = 3;

    [Header("Hammering State")]
    public int hammerStrikesRemaining = 3;
    public int totalStrikesPerformed = 0;

    public event Action<int, int> OnHammerStrikeProgress;
    public event Action<ForgedSwordStats> OnForgeFinished;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Performs an interactive hammer strike against the sword being forged.
    /// </summary>
    public void PerformHammerStrike(WeaponTemper weapon = null)
    {
        totalStrikesPerformed++;
        hammerStrikesRemaining--;

        Debug.Log($"🔨 [FORGE HAMMER] Strike struck! Strikes left: {hammerStrikesRemaining}");
        OnHammerStrikeProgress?.Invoke(totalStrikesPerformed, hammerStrikesRemaining);

        if (hammerStrikesRemaining <= 0)
        {
            FinishHammering(weapon);
        }
    }

    /// <summary>
    /// Called when the player finishes hammering at a campfire or hub anvil.
    /// Calls BlacksmithForgeController.QuenchAndFinishSword() to overwrite the sword's
    /// baseDamage, maxDurability, and maxSockets.
    /// </summary>
    public ForgedSwordStats FinishHammering(WeaponTemper weapon = null)
    {
        if (weapon == null)
        {
            weapon = FindAnyObjectByType<WeaponTemper>();
        }

        var livingGear = (weapon != null) ? weapon.GetComponent<LivingGearSystem>() : FindAnyObjectByType<LivingGearSystem>();

        // Overwrites the sword's baseDamage, maxDurability, and maxSockets
        var stats = BlacksmithForgeController.QuenchAndFinishSword(weapon, livingGear, isCampfire);

        if (stats != null)
        {
            baseDamage = stats.baseDamage;
            maxDurability = stats.maxDurability;
            maxSockets = stats.maxSockets;
        }

        hammerStrikesRemaining = 3;
        OnForgeFinished?.Invoke(stats);
        return stats;
    }

    /// <summary>
    /// Sets whether this forge station operates as a crude campfire or an advanced hub anvil.
    /// </summary>
    public void ConfigureStation(bool campfire)
    {
        isCampfire = campfire;
        isHubAnvil = !campfire;
    }
}
