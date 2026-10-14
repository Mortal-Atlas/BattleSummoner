using System;
using UnityEngine;

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
        if (currentHp <= 0f)
        {
            Debug.Log("[Player State] Astral core collapsed.");
            if (BattleSummoner.DungeonDimension.DungeonDimensionManager.Instance != null &&
                BattleSummoner.DungeonDimension.DungeonDimensionManager.Instance.RunState == BattleSummoner.DungeonDimension.DungeonDimensionState.InRoom)
            {
                currentHp = maxHp * 0.5f; // Revived at half HP upon dimensional ejection
                BattleSummoner.DungeonDimension.DungeonDimensionManager.Instance.EndRunOnDefeat();
            }
        }
    }

    public void RestoreMana(float amount)
    {
        currentMp = Mathf.Min(maxMp, currentMp + amount);
        OnManaChanged?.Invoke(currentMp, maxMp);
    }

    public void Heal(float amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }
}

public interface IEnemyStagger
{
    void Stagger(float duration, Vector3 origin);
}
