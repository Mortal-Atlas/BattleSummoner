using System;
using UnityEngine;

public enum SummonAffinity
{
    Fire,
    Void,
    Earth,
    Wind,
    Blood,
    Celestial
}

public enum SummonTemperament
{
    Aggressive,
    Stoic,
    Chaotic,
    Loyal,
    Gluttonous
}

public enum SummonQuirk
{
    SpellVamp,
    KineticReflect,
    GemFinder,
    StaggerMaster,
    PotatoLimp
}

/// <summary>
/// Autonomous summoned familiar/golem entity in the 3D Spire world.
/// Synthesized from the Patronus Engine quiz and Unistroke magic circle accuracy score.
/// </summary>
public class SummonCompanionEntity : MonoBehaviour
{
    [Header("Summon Identity & Lore")]
    public string summonName = "Aegis";
    public SummonAffinity affinity = SummonAffinity.Celestial;
    public SummonTemperament temperament = SummonTemperament.Loyal;
    public SummonQuirk quirk = SummonQuirk.StaggerMaster;

    [Header("Patronus Synthesis Stats")]
    public float circleAccuracyScore = 100f;
    public bool isApexSummon = false;
    public bool isMalformedPotato = false;

    public float currentHp = 250f;
    public float maxHp = 250f;
    public float attackPower = 65f;
    public float defense = 20f;
    public float attackCooldown = 2.5f;

    private Transform playerTarget;
    private float lastAttackTime = 0f;
    private CharacterController controller;
    private float limpTimer = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.radius = 0.4f;
            controller.height = 1.4f;
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }

        ApplyVisualDeformations();
    }

    /// <summary>
    /// Initializes synthesized stats based on baseline level and circle accuracy score.
    /// Formula: BaseStats = BaselineLevel * (AccuracyScore / 100.0)
    /// </summary>
    public void InitializeSynthesizedStats(string name, SummonAffinity aff, SummonTemperament temp, SummonQuirk qk, float accuracy, int baselineLevel)
    {
        summonName = name;
        affinity = aff;
        temperament = temp;
        quirk = qk;
        circleAccuracyScore = Mathf.Clamp(accuracy, 0f, 100f);

        isApexSummon = circleAccuracyScore >= 98f;
        isMalformedPotato = circleAccuracyScore < 50f;

        float accuracyRatio = circleAccuracyScore / 100f;
        maxHp = Mathf.Max(10f, (baselineLevel * 40f) * accuracyRatio);
        attackPower = Mathf.Max(5f, (baselineLevel * 15f) * accuracyRatio);
        defense = Mathf.Max(2f, (baselineLevel * 8f) * accuracyRatio);

        if (isApexSummon)
        {
            // Apex bonus: +50% stats and radiant titles
            maxHp *= 1.5f;
            attackPower *= 1.5f;
            defense *= 1.5f;
            Debug.Log($"★ [APEX SUMMON]: {summonName} awakened with 98%+ geometric perfection! Celestial resonance maxed.");
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("apex_celestial_roar", 1.0f);
            }
        }
        else if (isMalformedPotato)
        {
            // Margo's Margin Note: Squashed potato summon penalty
            quirk = SummonQuirk.PotatoLimp;
            maxHp = Mathf.Max(5f, maxHp * 0.4f);
            attackPower = Mathf.Max(2f, attackPower * 0.35f);
            Debug.LogWarning($"[MALFORMED SUMMON]: {summonName} summoned from squashed potato circle ({circleAccuracyScore:F1}% accuracy). Severely impaired.");
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("malformed_wheeze", 1.0f);
            }
        }

        if (BattleSummoner.Social.TomodachiSocialLinkManager.Instance != null)
        {
            BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetSummonStatsMultiplier(out float atkMult, out float hpMult, out float cdMult, out float autoShieldThreshold);
            attackPower *= atkMult;
            maxHp *= hpMult;
            attackCooldown *= cdMult;
        }

        currentHp = maxHp;
        ApplyVisualDeformations();
    }

    private void ApplyVisualDeformations()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color affinityColor = affinity switch
            {
                SummonAffinity.Fire => new Color(1f, 0.3f, 0.1f),
                SummonAffinity.Void => new Color(0.35f, 0.05f, 0.55f),
                SummonAffinity.Earth => new Color(0.55f, 0.4f, 0.15f),
                SummonAffinity.Wind => new Color(0.2f, 0.85f, 0.7f),
                SummonAffinity.Blood => new Color(0.85f, 0.05f, 0.15f),
                SummonAffinity.Celestial => new Color(1f, 0.9f, 0.3f),
                _ => Color.white
            };

            mat.SetColor("_BaseColor", affinityColor);
            if (isApexSummon)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", affinityColor * 2.5f);
                transform.localScale = Vector3.one * 1.35f; // Majestic stature
            }
            else if (isMalformedPotato)
            {
                // Squashed potato visual scaling
                transform.localScale = new Vector3(1.35f, 0.45f, 1.25f);
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            rend.sharedMaterial = mat;
        }
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
            return;
        }

        // Autonomous combat and player following
        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distToPlayer > 3.2f)
        {
            Vector3 followDir = (playerTarget.position - transform.position).normalized;
            followDir.y = 0f;

            float speed = isApexSummon ? 6.5f : (isMalformedPotato ? 2.2f : 4.5f);

            // Potato limp wobble
            if (isMalformedPotato)
            {
                limpTimer += Time.deltaTime * 6f;
                followDir += transform.right * (Mathf.Sin(limpTimer) * 0.4f);
            }

            controller.Move(followDir * (speed * Time.deltaTime));
            if (followDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(followDir), Time.deltaTime * 8f);
            }
        }

        // Scan for hostile targets
        if (Time.time - lastAttackTime > attackCooldown)
        {
            CheckAndAttackNearbyEnemies();
        }
    }

    private void CheckAndAttackNearbyEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 7.5f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Enemy"))
            {
                lastAttackTime = Time.time;
                ExecuteElementalStrike(hits[i].gameObject);
                break;
            }
        }
    }

    private void ExecuteElementalStrike(GameObject enemy)
    {
        float damage = attackPower;
        if (BattleSummoner.Social.TomodachiSocialLinkManager.Instance != null)
        {
            BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetSummonStatsMultiplier(out float atkMult, out float hpMult, out float cdMult, out float autoShieldThreshold);
            damage *= atkMult;
        }

        // Quirk modifiers
        if (quirk == SummonQuirk.SpellVamp)
        {
            currentHp = Mathf.Min(maxHp, currentHp + damage * 0.20f);
        }

        BossController boss = enemy.GetComponent<BossController>();
        if (boss != null)
        {
            boss.TakeDirectDamage(damage);
        }

        SpireEnemy spireEnemy = enemy.GetComponent<SpireEnemy>();
        if (spireEnemy != null)
        {
            spireEnemy.TakeDamage(damage);
        }

        if (quirk == SummonQuirk.StaggerMaster && enemy.TryGetComponent<IEnemyStagger>(out var stagger))
        {
            stagger.Stagger(0.6f, transform.position);
        }

        Debug.Log($"[SUMMON STRIKE] {summonName} struck {enemy.name} for {damage:F1} {affinity} damage.");
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX(affinity == SummonAffinity.Fire ? "cinematic_boom" : "relic_chime", 0.7f);
        }
    }
}
