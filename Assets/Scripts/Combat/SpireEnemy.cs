using UnityEngine;
using BattleSummoner.Combat;

public class SpireEnemy : MonoBehaviour, IEnemyStagger, IDamageReceiver
{
    [Header("Enemy Stats")]
    [SerializeField] private string enemyName = "Spire Gargoyle";
    [SerializeField] private int mobTier = 1;
    [SerializeField] private bool isBoss = false;
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
    private string lastDamageType = "PhysicalSlash";

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
        lastDamageType = payload.damageType.ToString();
        if (statusReceiver != null)
        {
            return statusReceiver.TakeAdvancedDamage(payload);
        }

        bool isCrit = UnityEngine.Random.value < payload.critChance;
        TakeDamage(payload.baseDamage, isCrit);
        return new DamageResult { finalDamage = payload.baseDamage, outcome = isCrit ? HitOutcome.Critical : HitOutcome.Normal, damageType = payload.damageType };
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

    public void TakeDamage(float damage) => TakeDamage(damage, false, false);

    public void TakeDamage(float damage, bool isCrit, bool suppressPopup = false)
    {
        if (TryGetComponent<ApexNemesisEntity>(out var nemesis))
        {
            damage = nemesis.ModifyIncomingDamage(damage, lastDamageType, playerTransform);
        }

        currentHealth -= damage;

        // Floating combat text: White for normal damage, Red CRIT! for critical
        if (!suppressPopup && CombatFeedbackManager.Instance != null)
        {
            CombatFeedbackManager.Instance.SpawnEnemyDamage(transform.position + Vector3.up * 1.0f, damage, isCrit);
        }

        if (CombinationAttackManager.Instance != null)
        {
            CombinationAttackManager.Instance.RecordPlayerCombatHit(damage, isCrit);
        }

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

        // Phase 3: Spire Ecological Memory & Apex Nemesis tracking
        if (SpireEcologyNemesisManager.Instance != null)
        {
            int floorNum = playerProgression != null ? playerProgression.currentLevel : 1;
            SpireEcologyNemesisManager.Instance.RecordMonsterKill(floorNum, enemyName, lastDamageType, maxHealth);
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

        // Differentiated Loot System:
        // Regular small enemies grant loot immediately into inventory.
        // Bosses & Apex Nemesis entities spawn an interactive Monster Hunter carcass for manual carving!
        if (isBoss || mobTier >= 3 || GetComponent<ApexNemesisEntity>() != null)
        {
            ApexBossCarcass.SpawnCarcass(transform.position, transform.rotation, enemyName);
            Debug.Log($"⚔️ [BOSS DEFEATED] {enemyName} collapsed into a carveable apex carcass!");
        }
        else
        {
            GrantInstantMinionLoot();
            Debug.Log($"[ENEMY DEFEATED] {enemyName} collapsed into mana dust with instant loot.");
        }

        Destroy(gameObject);
    }

    private void GrantInstantMinionLoot()
    {
        // 1. Calculate instant minion drops
        int goldDrop = UnityEngine.Random.Range(15, 35) * Mathf.Max(1, mobTier);
        if (InventoryMenuUI.Instance != null)
        {
            InventoryMenuUI.Instance.AddGold(goldDrop);

            // Random common or uncommon material drop
            string[] minionItems = { "mat_spire_shard", "mat_gargoyle_stone", "mat_astral_thread", "potion_crimson" };
            string[] minionNames = { "Spire Shard", "Gargoyle Stone", "Astral Thread", "Crimson Draught" };
            int pick = UnityEngine.Random.Range(0, minionItems.Length);
            int qty = pick == 3 ? 1 : UnityEngine.Random.Range(1, 3);

            InventoryMenuUI.Instance.AddItem(new InventoryItemEntry
            {
                itemId = minionItems[pick],
                displayName = minionNames[pick],
                category = pick == 3 ? ItemCategory.Consumables : ItemCategory.Materials,
                rarity = ItemRarity.Common,
                quantity = qty,
                description = "Acquired immediately upon defeating a Spire minion."
            });

            // 2. Spawn 3D floating loot toast
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnLootFeedback(transform.position, $"+{qty} {minionNames[pick]} (+{goldDrop}G)", new Color(0.2f, 1f, 0.5f));
            }
        }

        // 3. Play instant pickup chime
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayLootPickupChime();
        }
    }
}
