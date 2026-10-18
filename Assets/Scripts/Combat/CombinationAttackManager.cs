using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Social;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// CombinationAttackManager coordinates Unison Attacks and Synergy between the player and bonded companions:
    /// - Manages the dynamic Synergy Gauge (0 to 100%) charged through combat performance
    /// - Triggers cinematic Duo Attacks unlocked through Social Links
    /// - Spawns white/red floating combat damage numbers on enemies and green ticking heal numbers on allies
    /// - Activates evolved Social Link Battle Form buffs during battle
    /// </summary>
    public class CombinationAttackManager : MonoBehaviour
    {
        private static CombinationAttackManager _instance;
        public static CombinationAttackManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<CombinationAttackManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        var go = new GameObject("CombinationAttackManager");
                        _instance = go.AddComponent<CombinationAttackManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Synergy Gauge Dynamics")]
        [Range(0f, 100f)]
        [SerializeField] private float currentSynergy = 35f; // Starts with a partial charge for quick testing
        private const float MAX_SYNERGY = 100f;

        public float SynergyPercent => currentSynergy / MAX_SYNERGY;
        public bool IsSynergyReady => currentSynergy >= MAX_SYNERGY;

        [Header("Active Bonded Partner")]
        [SerializeField] private string activeCompanionId = "npc_margo";

        public string ActiveCompanionId => activeCompanionId;

        // Events
        public static event Action<float> OnSynergyGaugeChanged;
        public static event Action<bool> OnSynergyReadyStateChanged;
        public static event Action<SocialEntityProfile, CombinationAttackDefinition> OnCombinationAttackExecuted;

        private void Awake()
        {
            if (_instance == null) _instance = this;
        }

        private void Update()
        {
            // Optional keyboard trigger for quick testing in editor
            if (UnityEngine.Input.GetKeyDown(KeyCode.U))
            {
                if (IsSynergyReady)
                {
                    TriggerActiveCombinationAttack();
                }
                else
                {
                    // For dev testing, allow charging gauge with Shift+U
                    if (UnityEngine.Input.GetKey(KeyCode.LeftShift))
                    {
                        AddSynergy(50f);
                    }
                }
            }
        }

        public void SetActiveCompanion(string companionId)
        {
            activeCompanionId = companionId;
        }

        public void AddSynergy(float amount)
        {
            bool wasReady = IsSynergyReady;
            currentSynergy = Mathf.Clamp(currentSynergy + amount, 0f, MAX_SYNERGY);
            OnSynergyGaugeChanged?.Invoke(SynergyPercent);

            if (!wasReady && IsSynergyReady)
            {
                OnSynergyReadyStateChanged?.Invoke(true);
                if (DevClassLoadoutManager.Instance != null)
                {
                    DevClassLoadoutManager.Instance.ShowToast("<color=#FFD700><size=125%><b>★ [UNISON GAUGE MAX] ★</b></size></color>\nPress [U] or Tap Unison Button for Duo Attack!");
                }
            }
        }

        /// <summary>
        /// Charges synergy when damaging enemies or performing combat actions.
        /// </summary>
        public void RecordPlayerCombatHit(float damage, bool isCrit)
        {
            float gain = isCrit ? 6.5f : 3.5f;
            AddSynergy(gain);
        }

        public void RecordPlayerHeal(float healAmount)
        {
            AddSynergy(4.0f);
        }

        public void RecordCompanionAction()
        {
            AddSynergy(5.0f);
        }

        public void RecordDamageTaken()
        {
            // Comeback mechanic: Taking damage charges the unison meter
            AddSynergy(5.0f);
        }

        public bool TriggerActiveCombinationAttack()
        {
            return ExecuteCombinationAttack(activeCompanionId);
        }

        public bool ExecuteCombinationAttack(string companionEntityId)
        {
            if (!IsSynergyReady)
            {
                Debug.LogWarning("[UNISON ATTACK] Synergy gauge is not fully charged yet!");
                return false;
            }

            var socialMgr = TomodachiSocialLinkManager.Instance;
            if (socialMgr == null) return false;

            var companion = socialMgr.GetEntity(companionEntityId) ?? socialMgr.GetEntity("npc_margo");
            if (companion == null)
            {
                Debug.LogWarning($"[UNISON ATTACK] Companion '{companionEntityId}' not found!");
                return false;
            }

            var combo = companion.combinationAttack;
            if (combo == null)
            {
                // Fallback default unison attack if not configured
                combo = new CombinationAttackDefinition
                {
                    comboId = "combo_unison_strike",
                    comboName = "Aegis & Blade Unison Strike",
                    baseDamage = 400f,
                    healAmount = 100f,
                    element = "Holy",
                    description = "Synchronized cross-cleave unleashing a brilliant radial shockwave.",
                    cinematicQuote = "Together now, under the light of Aegis!"
                };
            }

            // Consume gauge
            currentSynergy = 0f;
            OnSynergyGaugeChanged?.Invoke(0f);
            OnSynergyReadyStateChanged?.Invoke(false);

            // Execute attack coroutine
            StartCoroutine(ExecuteCombinationSequenceRoutine(companion, combo));
            return true;
        }

        private IEnumerator ExecuteCombinationSequenceRoutine(SocialEntityProfile companion, CombinationAttackDefinition combo)
        {
            // 1. Cinematic brief slowdown
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0.35f;

            // Audio fanfare
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
            }

            // 2. Announce Duo Attack Banner
            string quoteText = !string.IsNullOrEmpty(combo.cinematicQuote) ? $"\n<i>\"{combo.cinematicQuote}\"</i>" : "";
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(
                    $"<color=#FFE040><size=135%><b>★ COMBINATION ATTACK! ★</b></size></color>\n" +
                    $"<color=#00FFFF><b>[{companion.displayName} & Summoner]</b></color>\n" +
                    $"<b>{combo.comboName}</b>{quoteText}"
                );
            }

            yield return new WaitForSecondsRealtime(0.40f);
            Time.timeScale = originalTimeScale;

            // 3. Find Player Position
            Vector3 centerPos = Vector3.zero;
            var player = GameObject.FindWithTag("Player");
            if (player != null) centerPos = player.transform.position;
            else if (Camera.main != null) centerPos = Camera.main.transform.position + Camera.main.transform.forward * 4f;

            // 4. Deal Massive Damage to all enemies in 12m radius
            Collider[] hits = Physics.OverlapSphere(centerPos, 12f);
            int struckEnemies = 0;
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col.CompareTag("Player")) continue;

                if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    bool isCrit = combo.isGuaranteedCrit || (UnityEngine.Random.value < 0.40f);
                    float dmg = combo.baseDamage * (isCrit ? 1.5f : 1.0f);
                    enemy.TakeDamage(dmg, isCrit);
                    struckEnemies++;
                }
                else if (col.TryGetComponent<BossController>(out var boss))
                {
                    bool isCrit = combo.isGuaranteedCrit || (UnityEngine.Random.value < 0.40f);
                    float dmg = combo.baseDamage * (isCrit ? 1.5f : 1.0f);
                    boss.TakeDamage(dmg, isCrit);
                    struckEnemies++;
                }
                else if (col.TryGetComponent<StatusEffectReceiver>(out var receiver))
                {
                    var payload = new DamagePayload(combo.baseDamage, DamageType.RadiantHoly, col.transform.position, Vector3.up);
                    payload.critChance = combo.isGuaranteedCrit ? 1.0f : 0.45f;
                    receiver.TakeAdvancedDamage(payload);
                    struckEnemies++;
                }
            }

            // 5. If combination attack heals (like Cheryl's Genesis Bloom), heal player & companions
            if (combo.healAmount > 0f)
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null)
                {
                    prog.Heal(combo.healAmount);
                }
                else if (CombatFeedbackManager.Instance != null)
                {
                    CombatFeedbackManager.Instance.SpawnHealing(centerPos + Vector3.up * 1.2f, combo.healAmount);
                }
            }

            // 6. Deepen Social Link Camaraderie
            if (TomodachiSocialLinkManager.Instance != null)
            {
                TomodachiSocialLinkManager.Instance.RecordBattleVictoryWithCompanion(companion.entityId, combo.baseDamage);
            }

            // 7. Fire execution event
            OnCombinationAttackExecuted?.Invoke(companion, combo);
            Debug.Log($"⚔️ [COMBINATION ATTACK EXECUTED] {companion.displayName} & Summoner unleashed '{combo.comboName}' hitting {struckEnemies} enemies!");
        }
    }
}
