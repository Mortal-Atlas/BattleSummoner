using System;
using System.Collections;
using UnityEngine;
using BattleSummoner.Hobbies;

public class PlayerProgression : MonoBehaviour, ICombatEntity
{
    [Header("Resource Status")]
    public string playerName = "Wanderer";
    public int currentLevel = 1;
    public double currentExp = 0;
    public float currentHp = 100f;
    public float maxHp = 100f;
    public float currentMp = 50f;
    public float maxMp = 50f;

    [Header("Clutch Ratio Settings")]
    [SerializeField] private float missingHpHealPercent = 0.45f;
    [SerializeField] private float missingMpRestorePercent = 0.60f;
    [SerializeField] private float staggerPulseRadius = 2.5f;

    public event Action<int> OnLevelUpSurge;
    public event Action<int> OnLevelUpEvent;
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnManaChanged;

    public double GetRequiredExp(int level) => (120.0 * Math.Pow(level, 2.45)) + (80.0 * level);
    public double CalculateExpRequirement(int level) => GetRequiredExp(level);

    public void AddExperience(double rawExp, int partySize)
    {
        partySize = Mathf.Clamp(partySize, 1, 10);
        double multiplier = 1.0 + (0.25 * (partySize - 1));
        if (BattleSummoner.Social.TomodachiSocialLinkManager.Instance != null)
        {
            multiplier *= BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetPlayerXpMultiplier();
        }
        currentExp += (rawExp * multiplier) / partySize;

        while (currentExp >= GetRequiredExp(currentLevel))
        {
            currentExp -= GetRequiredExp(currentLevel);
            ExecuteLevelUp();
        }
    }

    public void AddSharedExp(double baseExp, int partyMembers) => AddExperience(baseExp, partyMembers);

    private void ExecuteLevelUp()
    {
        currentLevel++;
        maxHp += 25f;
        maxMp += 15f;

        // Clutch recovery calculation
        float missingHp = Mathf.Max(0f, maxHp - currentHp);
        float missingMp = Mathf.Max(0f, maxMp - currentMp);

        currentHp += missingHp * missingHpHealPercent;
        currentMp += missingMp * missingMpRestorePercent;

        // Kinetic stagger blast
        Collider[] hits = Physics.OverlapSphere(transform.position, staggerPulseRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Enemy") && hits[i].TryGetComponent<IEnemyStagger>(out var target))
            {
                target.Stagger(0.5f, transform.position);
            }
        }

        if (currentLevel >= 50 && SystemBroadcastManager.Instance != null)
        {
            SystemBroadcastManager.Instance.DispatchBroadcast($"Player reached Floor milestone Level {currentLevel}!", true);
        }

        OnLevelUpSurge?.Invoke(currentLevel);
        OnLevelUpEvent?.Invoke(currentLevel);
        OnHealthChanged?.Invoke(currentHp, maxHp);
        OnManaChanged?.Invoke(currentMp, maxMp);
    }

    private void ExecuteLevelSurge() => ExecuteLevelUp();

    public void ApplyTrueDamage(float damage) => TakeDirectDamage(damage);

    public void TakeDirectDamage(float amount)
    {
        // 1. Check Parry Window (100% Complete Damage Negation + Riposte)
        if (BattleSummoner.Combat.PlayerCombatStanceController.Instance != null)
        {
            if (BattleSummoner.Combat.PlayerCombatStanceController.Instance.IsParryActive)
            {
                BattleSummoner.Combat.PlayerCombatStanceController.Instance.TriggerSuccessfulParry(null);
                return; // 100% Negation!
            }

            // 2. Check Passive Shield Block (Sword & Shield stance)
            if (BattleSummoner.Combat.PlayerCombatStanceController.Instance.IsSwordAndShield)
            {
                BattleSummoner.Combat.PlayerCombatStanceController.Instance.TryProcessPassiveBlock(
                    ref amount,
                    transform.position + transform.forward * 2f,
                    transform.position,
                    transform.forward
                );
            }
        }

        if (BattleSummoner.Social.TomodachiSocialLinkManager.Instance != null)
        {
            float defMult = BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetPlayerDefenseMultiplier();
            if (defMult > 0.05f)
            {
                amount /= defMult;
            }
        }
        currentHp = Mathf.Max(0f, currentHp - amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);

        if (BattleSummoner.Combat.CombinationAttackManager.Instance != null)
        {
            BattleSummoner.Combat.CombinationAttackManager.Instance.RecordDamageTaken();
        }

        if (currentHp <= 0f)
        {
            Debug.Log("[Player State] Astral core collapsed. Entering downed revival window...");
            if (BattleSummoner.Progression.PlayerDownedReviveSystem.Instance != null)
            {
                BattleSummoner.Progression.PlayerDownedReviveSystem.Instance.EnterDownedState();
            }
            else
            {
                var downedSys = GetComponent<BattleSummoner.Progression.PlayerDownedReviveSystem>() ?? gameObject.AddComponent<BattleSummoner.Progression.PlayerDownedReviveSystem>();
                downedSys.EnterDownedState();
            }
        }
    }

    public void RestoreMana(float amount)
    {
        currentMp = Mathf.Min(maxMp, currentMp + amount);
        OnManaChanged?.Invoke(currentMp, maxMp);
    }

    public bool DrainMana(float amount)
    {
        if (amount <= 0f) return true;
        if (currentMp < amount) return false;
        currentMp = Mathf.Max(0f, currentMp - amount);
        OnManaChanged?.Invoke(currentMp, maxMp);
        return true;
    }

    public void ConsumeMana(float amount)
    {
        if (amount <= 0f) return;
        currentMp = Mathf.Max(0f, currentMp - amount);
        OnManaChanged?.Invoke(currentMp, maxMp);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);

        // Spawn ticking green numbers drifting upwards with offset
        if (BattleSummoner.Combat.CombatFeedbackManager.Instance != null)
        {
            BattleSummoner.Combat.CombatFeedbackManager.Instance.SpawnHealing(transform.position + Vector3.up * 1.2f, amount);
        }

        if (BattleSummoner.Combat.CombinationAttackManager.Instance != null)
        {
            BattleSummoner.Combat.CombinationAttackManager.Instance.RecordPlayerHeal(amount);
        }
    }

    [Header("Food Buffs")]
    [SerializeField] private CookedMealData activeMealBuff;
    private Coroutine activeFoodManaRegenRoutine;

    /// <summary>
    /// When a player consumes a CookedMealData item from their quick-access belt carousel:
    /// Adds meal.totalHealthRestore to currentHp and applies meal.manaRegenPerSecond to currentMp every second for meal.buffDurationSeconds.
    /// </summary>
    public void ConsumeCookedMeal(CookedMealData meal)
    {
        if (meal == null) return;

        activeMealBuff = meal;

        // 1. Add meal.totalHealthRestore to currentHp
        if (meal.totalHealthRestore > 0f)
        {
            currentHp = Mathf.Min(maxHp, currentHp + meal.totalHealthRestore);
            OnHealthChanged?.Invoke(currentHp, maxHp);

            if (BattleSummoner.Combat.CombatFeedbackManager.Instance != null)
            {
                BattleSummoner.Combat.CombatFeedbackManager.Instance.SpawnHealing(transform.position + Vector3.up * 1.2f, meal.totalHealthRestore);
            }
            if (BattleSummoner.Combat.CombinationAttackManager.Instance != null)
            {
                BattleSummoner.Combat.CombinationAttackManager.Instance.RecordPlayerHeal(meal.totalHealthRestore);
            }
        }

        // 2. Apply meal.manaRegenPerSecond to currentMp every second for meal.buffDurationSeconds
        if (meal.manaRegenPerSecond > 0f && meal.buffDurationSeconds > 0f)
        {
            if (activeFoodManaRegenRoutine != null && Application.isPlaying)
            {
                StopCoroutine(activeFoodManaRegenRoutine);
            }

            if (Application.isPlaying)
            {
                activeFoodManaRegenRoutine = StartCoroutine(FoodManaRegenRoutine(meal.manaRegenPerSecond, meal.buffDurationSeconds));
            }
            else
            {
                // In edit-mode unit test execution, simulate initial tick
                RestoreMana(meal.manaRegenPerSecond);
            }
        }

        Debug.Log($"🍲 [PLAYER PROGRESSION] Consumed Cooked Meal: {meal.mealName}! Restored +{meal.totalHealthRestore:F0} HP. Regenerating +{meal.manaRegenPerSecond:F1} MP/s for {meal.buffDurationSeconds:F0}s.");
    }

    private IEnumerator FoodManaRegenRoutine(float manaRegenPerSecond, float durationSeconds)
    {
        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            yield return new WaitForSeconds(1.0f);
            elapsed += 1.0f;
            currentMp = Mathf.Min(maxMp, currentMp + manaRegenPerSecond);
            OnManaChanged?.Invoke(currentMp, maxMp);
        }
        activeFoodManaRegenRoutine = null;
    }
}

public interface IEnemyStagger
{
    void Stagger(float duration, Vector3 origin);
}
