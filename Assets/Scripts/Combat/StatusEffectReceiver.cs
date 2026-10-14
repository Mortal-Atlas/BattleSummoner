using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    [System.Serializable]
    public class ActiveStatusInstance
    {
        public StatusEffectType type;
        public float timeRemaining;
        public float totalDuration;
        public float potency;
        public float tickInterval = 0.5f;
        public float tickTimer = 0f;
    }

    /// <summary>
    /// StatusEffectReceiver manages active combat status afflictions, periodic DoT ticks,
    /// movement/action impairment (Freeze, Sleep, Paralyzed, Slowed), and cross-elemental combos:
    /// - Ice + Electric -> SHATTERED (Explosive ice break + AoE shrapnel)
    /// - Poison/Acid + Fire -> COMBUSTION (Volatile chemical detonation blast)
    /// - Ice + Fire -> MELT / THERMAL SHOCK (Double burst damage + flash steam)
    /// - Acid + Electric -> SUPERCONDUCT (Electro-corrosion chain lightning)
    /// - Sleep + Physical -> NIGHTMARE REAP (2.5x wake-up critical strike)
    /// </summary>
    public class StatusEffectReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Affliction State")]
        [SerializeField] private List<ActiveStatusInstance> activeEffects = new List<ActiveStatusInstance>();

        [Header("Target Properties")]
        [SerializeField] private float evasionRate = 0.05f; // 5% baseline dodge
        [SerializeField] private float defenseArmor = 0f;

        public bool IsFrozen { get; private set; }
        public bool IsAsleep { get; private set; }
        public bool IsParalyzed { get; private set; }
        public bool IsSlowed { get; private set; }
        public float CurrentSpeedMultiplier { get; private set; } = 1.0f;

        // Visual Aura Anchors
        private GameObject iceCageVFX;
        private GameObject fireAuraVFX;
        private GameObject shockAuraVFX;
        private GameObject poisonAuraVFX;
        private GameObject acidAuraVFX;
        private GameObject sleepAuraVFX;

        // Callbacks for health management
        private SpireEnemy spireEnemy;
        private BossController bossController;
        private PlayerProgression playerProgression;

        private void Awake()
        {
            spireEnemy = GetComponent<SpireEnemy>();
            bossController = GetComponent<BossController>();
            playerProgression = GetComponent<PlayerProgression>();
        }

        private void Update()
        {
            UpdateStatusEffects(Time.deltaTime);
        }

        #region Status Effect Lifecycle
        private void UpdateStatusEffects(float dt)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.timeRemaining -= dt;
                effect.tickTimer += dt;

                // Process periodic DoT ticks
                if (effect.tickTimer >= effect.tickInterval)
                {
                    effect.tickTimer = 0f;
                    ProcessTick(effect);
                }

                if (effect.timeRemaining <= 0f)
                {
                    activeEffects.RemoveAt(i);
                }
            }

            RecalculateStatusFlags();
            UpdateVisualAuras(IsFrozen, HasStatus(StatusEffectType.Burn), HasStatus(StatusEffectType.Chilled) || IsParalyzed, HasStatus(StatusEffectType.Poisoned), HasStatus(StatusEffectType.AcidCorroded), IsAsleep);
        }

        public void RecalculateStatusFlags()
        {
            bool hasFreeze = false;
            bool hasSleep = false;
            bool hasParalyze = false;
            bool hasSlow = false;
            bool hasChill = false;
            float speedMult = 1.0f;

            for (int i = 0; i < activeEffects.Count; i++)
            {
                switch (activeEffects[i].type)
                {
                    case StatusEffectType.Freeze:
                        hasFreeze = true;
                        speedMult = 0f;
                        break;
                    case StatusEffectType.Sleep:
                        hasSleep = true;
                        speedMult = 0f;
                        break;
                    case StatusEffectType.Paralyzed:
                        hasParalyze = true;
                        speedMult = 0f;
                        break;
                    case StatusEffectType.Slowed:
                        hasSlow = true;
                        speedMult = Mathf.Min(speedMult, 0.50f);
                        break;
                    case StatusEffectType.Chilled:
                        hasChill = true;
                        speedMult = Mathf.Min(speedMult, 0.60f);
                        break;
                }
            }

            IsFrozen = hasFreeze;
            IsAsleep = hasSleep;
            IsParalyzed = hasParalyze;
            IsSlowed = hasSlow || hasChill;
            CurrentSpeedMultiplier = speedMult;

            UpdateVisualAuras(IsFrozen, HasStatus(StatusEffectType.Burn), HasStatus(StatusEffectType.Chilled) || IsParalyzed, HasStatus(StatusEffectType.Poisoned), HasStatus(StatusEffectType.AcidCorroded), IsAsleep);
        }

        private void ProcessTick(ActiveStatusInstance effect)
        {
            float tickDamage = 0f;
            DamageType dType = DamageType.PhysicalSlash;

            switch (effect.type)
            {
                case StatusEffectType.Burn:
                    tickDamage = effect.potency;
                    dType = DamageType.Fire;
                    break;
                case StatusEffectType.Poisoned:
                    tickDamage = effect.potency;
                    dType = DamageType.Poison;
                    break;
                case StatusEffectType.AcidCorroded:
                    tickDamage = effect.potency * 0.75f;
                    dType = DamageType.Acid;
                    break;
            }

            if (tickDamage > 0f)
            {
                ApplyDirectHealthReduction(tickDamage);
                if (CombatFeedbackManager.Instance != null)
                {
                    var res = new DamageResult { finalDamage = tickDamage, outcome = HitOutcome.Normal, damageType = dType };
                    CombatFeedbackManager.Instance.SpawnDamagePopup(transform.position + Vector3.up * 0.5f, res);
                }
            }
        }

        public void ApplyStatus(StatusEffectType type, float duration, float potency)
        {
            if (type == StatusEffectType.None) return;

            // Check if already active: refresh duration and pick highest potency
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].type == type)
                {
                    activeEffects[i].timeRemaining = Mathf.Max(activeEffects[i].timeRemaining, duration);
                    activeEffects[i].potency = Mathf.Max(activeEffects[i].potency, potency);
                    RecalculateStatusFlags();
                    return;
                }
            }

            var instance = new ActiveStatusInstance
            {
                type = type,
                totalDuration = duration,
                timeRemaining = duration,
                potency = potency,
                tickInterval = (type == StatusEffectType.Burn) ? 0.5f : 1.0f
            };

            activeEffects.Add(instance);
            RecalculateStatusFlags();

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnStatusPopup(transform.position, type);
            }
        }

        public bool HasStatus(StatusEffectType type)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].type == type) return true;
            }
            return false;
        }

        public void RemoveStatus(StatusEffectType type)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                if (activeEffects[i].type == type)
                {
                    activeEffects.RemoveAt(i);
                }
            }
            RecalculateStatusFlags();
        }
        #endregion

        #region Cross-Element Reaction Engine
        public DamageResult TakeAdvancedDamage(DamagePayload payload)
        {
            var result = new DamageResult();
            result.damageType = payload.damageType;

            // 1. Accuracy vs Evasion Check (Can result in MISS)
            float effectiveHitChance = Mathf.Clamp01(payload.accuracy - (IsFrozen || IsAsleep || IsParalyzed ? 0f : evasionRate));
            if (UnityEngine.Random.value > effectiveHitChance)
            {
                result.outcome = HitOutcome.Miss;
                result.finalDamage = 0f;
                if (CombatFeedbackManager.Instance != null)
                {
                    CombatFeedbackManager.Instance.SpawnDamagePopup(payload.hitPoint != Vector3.zero ? payload.hitPoint : transform.position, result);
                }
                return result;
            }

            // 2. Critical Strike Evaluation
            bool isCrit = (UnityEngine.Random.value < payload.critChance);
            float damageMultiplier = isCrit ? payload.critMultiplier : 1.0f;
            result.outcome = isCrit ? HitOutcome.Critical : HitOutcome.Normal;

            // 3. Acid Corrosion Defense Debuff
            if (HasStatus(StatusEffectType.AcidCorroded))
            {
                damageMultiplier *= 1.35f; // +35% amplified damage on corroded targets
            }

            // 4. CROSS-ELEMENTAL COMBO APPRAISAL
            EvaluateElementalCombos(payload, ref result, ref damageMultiplier);

            // 5. Final Damage Calculation
            float rawDamage = payload.baseDamage * damageMultiplier + result.comboBonusDamage;
            result.finalDamage = Mathf.Max(1f, rawDamage);

            // Apply to entity health
            ApplyDirectHealthReduction(result.finalDamage);

            // 6. Apply Status Affliction if payload carries one
            if (payload.appliedStatus != StatusEffectType.None)
            {
                ApplyStatus(payload.appliedStatus, payload.statusDuration, payload.statusPotency);
            }

            // 7. Visual Feedback
            Vector3 popPos = payload.hitPoint != Vector3.zero ? payload.hitPoint : (transform.position + Vector3.up * 1.0f);
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnDamagePopup(popPos, result);
            }

            return result;
        }

        private void EvaluateElementalCombos(DamagePayload payload, ref DamageResult result, ref float damageMultiplier)
        {
            // === COMBO 1: ICE + ELECTRIC -> SHATTERED ===
            // Target is Chilled or Frozen, hit by Electric attack
            if ((HasStatus(StatusEffectType.Freeze) || HasStatus(StatusEffectType.Chilled)) && payload.damageType == DamageType.Electric)
            {
                result.comboTriggered = ElementalComboType.Shattered;
                result.comboName = "SHATTERED";
                result.comboBonusDamage = payload.baseDamage * 1.50f; // +150% bonus shatter damage!

                // Shatter breaks the ice
                RemoveStatus(StatusEffectType.Freeze);
                RemoveStatus(StatusEffectType.Chilled);

                // Spawn explosive ice shrapnel AoE burst affecting nearby enemies
                TriggerShatterAoEBurst(transform.position, result.comboBonusDamage * 0.70f);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_shatter", 1.0f);
                }
            }
            // === COMBO 2: POISON / ACID + FIRE -> COMBUSTION ===
            else if ((HasStatus(StatusEffectType.Poisoned) || HasStatus(StatusEffectType.AcidCorroded)) && payload.damageType == DamageType.Fire)
            {
                result.comboTriggered = ElementalComboType.Combustion;
                result.comboName = "COMBUSTION";
                result.comboBonusDamage = payload.baseDamage * 1.25f;

                RemoveStatus(StatusEffectType.Poisoned);
                RemoveStatus(StatusEffectType.AcidCorroded);

                TriggerCombustionAoEBurst(transform.position, result.comboBonusDamage);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.9f);
                }
            }
            // === COMBO 3: ICE + FIRE -> MELT / THERMAL SHOCK ===
            else if ((HasStatus(StatusEffectType.Freeze) || HasStatus(StatusEffectType.Chilled)) && payload.damageType == DamageType.Fire)
            {
                result.comboTriggered = ElementalComboType.MeltThermalShock;
                result.comboName = "THERMAL SHOCK MELT";
                damageMultiplier *= 2.0f; // 2x burst multiplier!

                RemoveStatus(StatusEffectType.Freeze);
                RemoveStatus(StatusEffectType.Chilled);

                // Blinding steam increases target miss rate
                evasionRate = Mathf.Clamp01(evasionRate + 0.35f);
            }
            // === COMBO 4: ACID + ELECTRIC -> SUPERCONDUCT ===
            else if (HasStatus(StatusEffectType.AcidCorroded) && payload.damageType == DamageType.Electric)
            {
                result.comboTriggered = ElementalComboType.Superconduct;
                result.comboName = "SUPERCONDUCT";
                result.comboBonusDamage = payload.baseDamage * 0.85f;

                TriggerSuperconductChain(transform.position, result.comboBonusDamage);
            }
            // === COMBO 5: SLEEP + PHYSICAL ATTACK -> NIGHTMARE REAP ===
            else if (HasStatus(StatusEffectType.Sleep) && (payload.damageType == DamageType.PhysicalSlash || payload.damageType == DamageType.PhysicalBlunt || payload.damageType == DamageType.PhysicalPierce))
            {
                result.comboTriggered = ElementalComboType.NightmareReap;
                result.comboName = "NIGHTMARE REAP";
                result.outcome = HitOutcome.Critical;
                damageMultiplier *= 2.50f; // 2.5x wake-up critical strike!

                RemoveStatus(StatusEffectType.Sleep);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 1.0f);
                }
            }
        }

        private void TriggerShatterAoEBurst(Vector3 origin, float shrapnelDamage)
        {
            Collider[] hits = Physics.OverlapSphere(origin, 4.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].TryGetComponent<StatusEffectReceiver>(out var receiver))
                {
                    var aoePayload = new DamagePayload(shrapnelDamage, DamageType.Ice, hits[i].transform.position, (hits[i].transform.position - origin).normalized);
                    aoePayload.appliedStatus = StatusEffectType.Chilled;
                    aoePayload.statusDuration = 3.5f;
                    receiver.TakeAdvancedDamage(aoePayload);
                }
            }

            // Visual Ice Shrapnel VFX
            SpawnIceShrapnelVFX(origin);
        }

        private void TriggerCombustionAoEBurst(Vector3 origin, float explosionDamage)
        {
            Collider[] hits = Physics.OverlapSphere(origin, 4.0f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].TryGetComponent<StatusEffectReceiver>(out var receiver))
                {
                    var aoePayload = new DamagePayload(explosionDamage, DamageType.Fire, hits[i].transform.position, (hits[i].transform.position - origin).normalized);
                    receiver.TakeAdvancedDamage(aoePayload);
                }
            }
            SpawnFireBurstVFX(origin);
        }

        private void TriggerSuperconductChain(Vector3 origin, float chainDamage)
        {
            Collider[] hits = Physics.OverlapSphere(origin, 6.0f);
            int chained = 0;
            for (int i = 0; i < hits.Length && chained < 4; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].TryGetComponent<StatusEffectReceiver>(out var receiver))
                {
                    var chainPayload = new DamagePayload(chainDamage, DamageType.Electric, hits[i].transform.position, Vector3.up);
                    receiver.TakeAdvancedDamage(chainPayload);
                    chained++;
                }
            }
        }
        #endregion

        #region Health Reduction Hook
        private void ApplyDirectHealthReduction(float amount)
        {
            if (spireEnemy != null)
            {
                spireEnemy.TakeDamage(amount);
            }
            else if (bossController != null)
            {
                bossController.TakeDamage(amount);
            }
            else if (playerProgression != null)
            {
                playerProgression.TakeDirectDamage(amount);
            }
        }
        #endregion

        #region Visual Auras
        private void UpdateVisualAuras(bool freeze, bool burn, bool shock, bool poison, bool acid, bool sleep)
        {
            // 1. Ice Cage
            if (freeze)
            {
                if (iceCageVFX == null) iceCageVFX = CreateAuraMesh("FreezeIceCage", PrimitiveType.Cube, new Vector3(1.3f, 2.1f, 1.3f), new Color(0.2f, 0.85f, 1.0f, 0.55f));
                iceCageVFX.SetActive(true);
            }
            else if (iceCageVFX != null) iceCageVFX.SetActive(false);

            // 2. Fire Aura
            if (burn)
            {
                if (fireAuraVFX == null) fireAuraVFX = CreateAuraMesh("BurnFireAura", PrimitiveType.Sphere, new Vector3(1.4f, 1.8f, 1.4f), new Color(1.0f, 0.35f, 0.1f, 0.45f));
                fireAuraVFX.SetActive(true);
            }
            else if (fireAuraVFX != null) fireAuraVFX.SetActive(false);

            // 3. Shock / Paralyze
            if (shock)
            {
                if (shockAuraVFX == null) shockAuraVFX = CreateAuraMesh("ShockAura", PrimitiveType.Cylinder, new Vector3(1.2f, 0.1f, 1.2f), new Color(1.0f, 0.95f, 0.3f, 0.70f));
                shockAuraVFX.SetActive(true);
            }
            else if (shockAuraVFX != null) shockAuraVFX.SetActive(false);

            // 4. Poison Aura
            if (poison)
            {
                if (poisonAuraVFX == null) poisonAuraVFX = CreateAuraMesh("PoisonAura", PrimitiveType.Sphere, new Vector3(1.3f, 1.6f, 1.3f), new Color(0.2f, 0.95f, 0.3f, 0.40f));
                poisonAuraVFX.SetActive(true);
            }
            else if (poisonAuraVFX != null) poisonAuraVFX.SetActive(false);

            // 5. Acid Aura
            if (acid)
            {
                if (acidAuraVFX == null) acidAuraVFX = CreateAuraMesh("AcidAura", PrimitiveType.Cylinder, new Vector3(1.4f, 0.2f, 1.4f), new Color(0.7f, 1.0f, 0.1f, 0.50f));
                acidAuraVFX.SetActive(true);
            }
            else if (acidAuraVFX != null) acidAuraVFX.SetActive(false);

            // 6. Sleep Aura
            if (sleep)
            {
                if (sleepAuraVFX == null) sleepAuraVFX = CreateAuraMesh("SleepAura", PrimitiveType.Sphere, new Vector3(0.5f, 0.5f, 0.5f), new Color(0.8f, 0.5f, 1.0f, 0.60f), new Vector3(0, 2.2f, 0));
                sleepAuraVFX.SetActive(true);
            }
            else if (sleepAuraVFX != null) sleepAuraVFX.SetActive(false);
        }

        private GameObject CreateAuraMesh(string name, PrimitiveType prim, Vector3 scale, Color col, Vector3 localOffset = default)
        {
            var go = GameObject.CreatePrimitive(prim);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset != default ? localOffset : Vector3.up * 1.0f;
            go.transform.localScale = scale;

            var colld = go.GetComponent<Collider>();
            if (colld != null) Destroy(colld);

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = col;
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", col * 1.5f);
                rend.sharedMaterial = mat;
            }
            return go;
        }

        private void SpawnIceShrapnelVFX(Vector3 pos)
        {
            var burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            burst.name = "IceShatterShrapnelBurst";
            burst.transform.position = pos + Vector3.up * 1.0f;
            burst.transform.localScale = Vector3.one * 1.8f;
            var c = burst.GetComponent<Collider>();
            if (c != null) Destroy(c);
            var r = burst.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.4f, 0.95f, 1.0f, 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1.0f) * 4f);
                r.sharedMaterial = mat;
            }
            Destroy(burst, 0.35f);
        }

        private void SpawnFireBurstVFX(Vector3 pos)
        {
            var burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            burst.name = "CombustionFireBurst";
            burst.transform.position = pos + Vector3.up * 1.0f;
            burst.transform.localScale = Vector3.one * 2.2f;
            var c = burst.GetComponent<Collider>();
            if (c != null) Destroy(c);
            var r = burst.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1.0f, 0.45f, 0.1f, 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.3f, 0.1f) * 4.5f);
                r.sharedMaterial = mat;
            }
            Destroy(burst, 0.35f);
        }
        #endregion
    }
}
