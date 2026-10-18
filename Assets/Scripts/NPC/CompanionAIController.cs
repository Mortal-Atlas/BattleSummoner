using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Social;

public class CompanionAIController : MonoBehaviour
{
    [Header("Profile Link")]
    public BespokeNPCObject companionProfile;

    [Header("Locomotion & Formation")]
    [SerializeField] private float followDistance = 2.4f;
    [SerializeField] private float moveSpeed = 5.2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Combat Support")]
    [SerializeField] private float attackRange = 3.0f;
    [SerializeField] private float supportCooldown = 6.0f;

    [Header("Active Social Link Buffs")]
    [SerializeField] private string activeBattleFormName = "Basic Bond";
    [SerializeField] private float attackMultiplier = 1.0f;
    [SerializeField] private float defenseMultiplier = 1.0f;
    [SerializeField] private string specialTrait = "Standard Support";

    private Transform playerTransform;
    private PlayerProgression playerProgression;
    private float lastSupportTime = 0f;
    private SocialEntityProfile cachedSocialProfile;

    private void Start()
    {
        playerProgression = FindAnyObjectByType<PlayerProgression>();
        if (playerProgression != null)
        {
            playerTransform = playerProgression.transform;
        }

        if (companionProfile != null)
        {
            supportCooldown = companionProfile.companionSupportCooldown;
        }

        RefreshSocialLinkForm();

        // Subscribe to social link updates
        TomodachiSocialLinkManager.OnRankUp += HandleRankUp;
    }

    private void OnDestroy()
    {
        TomodachiSocialLinkManager.OnRankUp -= HandleRankUp;
    }

    private void HandleRankUp(SocialEntityProfile entity, int newRank)
    {
        if (companionProfile != null && entity != null && entity.entityId.Equals(companionProfile.npcId, System.StringComparison.OrdinalIgnoreCase))
        {
            RefreshSocialLinkForm();
        }
    }

    public void RefreshSocialLinkForm()
    {
        if (TomodachiSocialLinkManager.Instance == null || companionProfile == null) return;

        cachedSocialProfile = TomodachiSocialLinkManager.Instance.GetEntity(companionProfile.npcId);
        if (cachedSocialProfile != null)
        {
            var form = cachedSocialProfile.GetActiveBattleForm();
            if (form != null)
            {
                activeBattleFormName = form.formName;
                attackMultiplier = Mathf.Max(1.0f, form.bonusAttackMultiplier);
                defenseMultiplier = Mathf.Max(1.0f, form.bonusDefenseMultiplier);
                specialTrait = form.specialAidTrait;
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer > followDistance)
        {
            Vector3 targetPos = playerTransform.position - (playerTransform.forward * 1.5f) + (playerTransform.right * 1.2f);
            targetPos.y = transform.position.y;
            Vector3 moveDir = (targetPos - transform.position).normalized;

            transform.position += moveDir * (moveSpeed * Time.deltaTime);
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        // Support attack nearest enemy
        if (Time.time - lastSupportTime >= supportCooldown)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange + 4f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Enemy"))
                {
                    float basePower = companionProfile != null ? companionProfile.combatAttackPower : 35f;
                    float effectivePower = basePower * attackMultiplier;

                    if (hits[i].TryGetComponent<SpireEnemy>(out var enemy))
                    {
                        enemy.TakeDamage(effectivePower);
                        lastSupportTime = Time.time;
                        RecordSupportStrike(effectivePower);
                        break;
                    }
                    else if (hits[i].TryGetComponent<BossController>(out var boss))
                    {
                        boss.TakeDamage(effectivePower);
                        lastSupportTime = Time.time;
                        RecordSupportStrike(effectivePower);
                        break;
                    }
                }
            }
        }
    }

    private void RecordSupportStrike(float damage)
    {
        Debug.Log($"⚔️ [COMPANION STRIKE] {name} ({activeBattleFormName}) assists player with {damage:F0} damage! [{specialTrait}]");

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("heavy_impact", 0.8f);
        }

        // Deepen bond through battle camaraderie
        if (companionProfile != null && TomodachiSocialLinkManager.Instance != null)
        {
            TomodachiSocialLinkManager.Instance.RecordBattleVictoryWithCompanion(companionProfile.npcId, damage);
        }

        if (BattleSummoner.Combat.CombinationAttackManager.Instance != null)
        {
            BattleSummoner.Combat.CombinationAttackManager.Instance.RecordCompanionAction();
        }
    }

    /// <summary>
    /// Triggers the Duo Unison / Combination Attack unlocked via Social Links.
    /// </summary>
    public bool TriggerUnisonAttack(Vector3 targetCenter, out string attackName, out float damageDealt)
    {
        attackName = "None";
        damageDealt = 0f;

        if (cachedSocialProfile == null)
        {
            RefreshSocialLinkForm();
        }

        if (cachedSocialProfile == null || cachedSocialProfile.combinationAttack == null)
        {
            return false;
        }

        var combo = cachedSocialProfile.combinationAttack;
        attackName = combo.comboName;
        damageDealt = combo.baseDamage * attackMultiplier;

        // Deal AoE damage to enemies around the target center
        Collider[] hits = Physics.OverlapSphere(targetCenter, 7.5f);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Enemy"))
            {
                if (hits[i].TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage(damageDealt);
                }
                else if (hits[i].TryGetComponent<BossController>(out var boss))
                {
                    boss.TakeDamage(damageDealt);
                }
            }
        }

        // Apply Unison Healing if combo has heal amount
        if (combo.healAmount > 0f && playerProgression != null)
        {
            playerProgression.Heal(combo.healAmount);
        }

        // SFX and Visual feedback
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("heavy_impact", 1.0f);
        }

        if (DevClassLoadoutManager.Instance != null)
        {
            DevClassLoadoutManager.Instance.ShowToast(
                $"<color=#FFE040><b>⚡ UNISON STRIKE: {combo.comboName}! ⚡</b></color>\n" +
                $"<i>\"{combo.cinematicQuote}\"</i>\n" +
                $"Dealt <b>{damageDealt:F0}</b> {combo.element} damage!"
            );
        }

        return true;
    }
}
