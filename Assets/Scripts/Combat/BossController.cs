using System;
using UnityEngine;
using BattleSummoner.Combat;

public class BossController : MonoBehaviour, IEnemyStagger, IDamageReceiver
{
    [Header("Boss Identity")]
    public string bossName = "Aethelgard, Core Sentinel";
    public float maxHealth = 2500f;
    public float currentHealth = 2500f;

    [Header("Abilities")]
    [SerializeField] private GameObject shockwaveVFXPrefab;
    [SerializeField] private Transform slamOrigin;
    [SerializeField] private float slamInterval = 6.0f;
    [SerializeField] private float slamWarningDuration = 1.0f;

    public event Action<float, float> OnBossHealthChanged;
    public event Action OnBossDefeated;
    public event Action OnBossTelegraphSlam;

    public bool IsPreparingSlam => isPreparingSlam;

    private float slamTimer = 0f;
    private bool isPreparingSlam = false;
    private Renderer bossRenderer;
    private Color normalColor = Color.white;
    private StatusEffectReceiver statusReceiver;

    private void Awake()
    {
        currentHealth = maxHealth;
        statusReceiver = GetComponent<StatusEffectReceiver>();
        if (statusReceiver == null)
        {
            statusReceiver = gameObject.AddComponent<StatusEffectReceiver>();
        }

        bossRenderer = GetComponentInChildren<Renderer>();
        if (bossRenderer != null && bossRenderer.material.HasProperty("_BaseColor"))
        {
            normalColor = bossRenderer.material.GetColor("_BaseColor");
        }
    }

    private void Start()
    {
        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        // Check status incapacitation (Freeze, Sleep, Paralyze)
        if (statusReceiver != null && (statusReceiver.IsFrozen || statusReceiver.IsAsleep || statusReceiver.IsParalyzed))
        {
            return; // Boss is frozen solid, sleeping, or paralyzed
        }

        float speedMult = statusReceiver != null ? statusReceiver.CurrentSpeedMultiplier : 1.0f;
        slamTimer += Time.deltaTime * speedMult;

        if (!isPreparingSlam && slamTimer >= (slamInterval - slamWarningDuration))
        {
            isPreparingSlam = true;
            OnBossTelegraphSlam?.Invoke();
            if (bossRenderer != null && bossRenderer.material.HasProperty("_BaseColor"))
            {
                bossRenderer.material.SetColor("_BaseColor", Color.magenta);
            }
        }

        if (slamTimer >= slamInterval)
        {
            slamTimer = 0f;
            isPreparingSlam = false;
            ExecuteKineticSlam();
        }
    }

    public DamageResult TakeAdvancedDamage(DamagePayload payload)
    {
        if (statusReceiver != null)
        {
            return statusReceiver.TakeAdvancedDamage(payload);
        }

        TakeDamage(payload.baseDamage);
        return new DamageResult { finalDamage = payload.baseDamage, outcome = HitOutcome.Normal, damageType = payload.damageType };
    }

    private void ExecuteKineticSlam()
    {
        if (bossRenderer != null && bossRenderer.material.HasProperty("_BaseColor"))
        {
            bossRenderer.material.SetColor("_BaseColor", normalColor);
        }

        Vector3 spawnPos = slamOrigin != null ? slamOrigin.position : transform.position;
        if (shockwaveVFXPrefab != null)
        {
            Instantiate(shockwaveVFXPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[BOSS ACTION] {bossName} unleashes colossal kinetic ground slam!");
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void TakeDirectDamage(float damage) => TakeDamage(damage);

    public void Stagger(float duration, Vector3 origin)
    {
        // Boss is resistant but gets interrupted briefly
        slamTimer = Mathf.Max(0f, slamTimer - duration * 0.5f);
        Debug.Log($"[BOSS RESIST] {bossName} withstood kinetic blast with minor stagger.");
    }

    private void Die()
    {
        Debug.Log($"[VICTORY] {bossName} shattered into ancient glyph fragments!");
        OnBossDefeated?.Invoke();
        Destroy(gameObject, 0.5f);
    }
}
