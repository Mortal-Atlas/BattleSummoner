using UnityEngine;
using BattleSummoner.Combat;

public class SpireEnemy : MonoBehaviour, IEnemyStagger, IDamageReceiver
{
    [Header("Enemy Stats")]
    [SerializeField] private string enemyName = "Spire Gargoyle";
    [SerializeField] private int mobTier = 1;
    [SerializeField] private float maxHealth = 150f;
    [SerializeField] private float currentHealth = 150f;
    [SerializeField] private double expReward = 65.0;

    [Header("Behavior")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 12f;
    [SerializeField] private float attackCooldown = 1.8f;

    private Transform playerTransform;
    private PlayerProgression playerProgression;
    private LivingGearSystem livingGear;
    private StatusEffectReceiver statusReceiver;
    private bool isStaggered = false;
    private float staggerTimeRemaining = 0f;
    private float lastAttackTime = 0f;
    private Renderer enemyRenderer;
    private Color originalColor = Color.white;

    private void Awake()
    {
        currentHealth = maxHealth;
        statusReceiver = GetComponent<StatusEffectReceiver>();
        if (statusReceiver == null)
        {
            statusReceiver = gameObject.AddComponent<StatusEffectReceiver>();
        }

        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null && enemyRenderer.material.HasProperty("_BaseColor"))
        {
            originalColor = enemyRenderer.material.GetColor("_BaseColor");
        }
    }

    private void Start()
    {
        playerProgression = FindAnyObjectByType<PlayerProgression>();
        if (playerProgression != null)
        {
            playerTransform = playerProgression.transform;
            livingGear = playerProgression.GetComponentInChildren<LivingGearSystem>();
        }
    }

    private void Update()
    {
        if (isStaggered)
        {
            staggerTimeRemaining -= Time.deltaTime;
            if (staggerTimeRemaining <= 0f)
            {
                isStaggered = false;
                if (enemyRenderer != null && enemyRenderer.material.HasProperty("_BaseColor"))
                {
                    enemyRenderer.material.SetColor("_BaseColor", originalColor);
                }
            }
            return;
        }

        // Check status incapacitation (Freeze, Sleep, Paralyze)
        if (statusReceiver != null && (statusReceiver.IsFrozen || statusReceiver.IsAsleep || statusReceiver.IsParalyzed))
        {
            return; // Target cannot move or act while frozen, asleep, or paralyzed
        }

        if (playerTransform == null) return;

        float effectiveSpeed = moveSpeed * (statusReceiver != null ? statusReceiver.CurrentSpeedMultiplier : 1.0f);

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > attackRange)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0f;
            transform.position += direction * (effectiveSpeed * Time.deltaTime);
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
            }
        }
        else
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                if (playerProgression != null)
                {
                    playerProgression.TakeDirectDamage(attackDamage);
                }
            }
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

    public void Stagger(float duration, Vector3 origin)
    {
        isStaggered = true;
        staggerTimeRemaining = duration;

        // Push back away from origin
        Vector3 pushDir = (transform.position - origin).normalized;
        pushDir.y = 0f;
        transform.position += pushDir * 1.2f;

        if (enemyRenderer != null && enemyRenderer.material.HasProperty("_BaseColor"))
        {
            enemyRenderer.material.SetColor("_BaseColor", Color.yellow);
        }

        Debug.Log($"[ENEMY STAGGER] {enemyName} staggered for {duration}s by kinetic shockwave!");
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // Check Execute Strike boon (instant spatial shatter below threshold)
        if (currentHealth > 0f && BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance != null)
        {
            float execThresh = BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance.GetExecuteThreshold();
            if (execThresh > 0f && currentHealth <= maxHealth * execThresh)
            {
                currentHealth = 0f;
                Debug.Log($"💀 [EXECUTE STRIKE] {enemyName} shattered instantly below {execThresh:P0} HP!");
            }
        }

        if (enemyRenderer != null && enemyRenderer.material.HasProperty("_BaseColor"))
        {
            enemyRenderer.material.SetColor("_BaseColor", Color.red);
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void ResetColor()
    {
        if (enemyRenderer != null && enemyRenderer.material.HasProperty("_BaseColor"))
        {
            enemyRenderer.material.SetColor("_BaseColor", isStaggered ? Color.yellow : originalColor);
        }
    }

    private void Die()
    {
        if (playerProgression != null)
        {
            playerProgression.AddSharedExp(expReward, 1);
        }

        if (livingGear != null)
        {
            livingGear.OnMobSlain(mobTier);
        }

        // Check Cull the Weak kinetic blast on death
        if (BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance != null &&
            BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance.HasSlainExplosion(out float explodePct))
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 5.0f);
            float blastDmg = maxHealth * explodePct;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject != gameObject && hits[i].CompareTag("Enemy") && hits[i].TryGetComponent<SpireEnemy>(out var nearby))
                {
                    nearby.TakeDamage(blastDmg);
                }
            }
        }

        Debug.Log($"[ENEMY DEFEATED] {enemyName} collapsed into mana dust.");
        Destroy(gameObject);
    }
}
