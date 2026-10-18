using System;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// ElementalSpellArsenal allows player and party companions to cast elemental attacks
    /// designed to react together into cross-elemental combos:
    /// 1. Frost Nova (Ice -> Freezes target solid)
    /// 2. Lightning Arc (Electric -> SHATTERS frozen targets for massive bonus AoE damage!)
    /// 3. Pyre Blast (Fire -> Ignites COMBUSTION detonation on Poison/Acid targets!)
    /// 4. Caustic Acid Flask (Acid -> Corrodes armor for +35% amplified damage)
    /// 5. Venom Dart (Poison -> Toxic DoT)
    /// 6. Slumber Hex (Sleep -> Sets up 2.5x Nightmare Reap Wake-up Crits)
    /// </summary>
    public class ElementalSpellArsenal : MonoBehaviour
    {
        public static ElementalSpellArsenal Instance { get; private set; }

        [Header("Targeting")]
        [SerializeField] private float spellRange = 14f;
        [SerializeField] private LayerMask enemyLayers = ~0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Update()
        {
            // Desktop Hotkeys 1-6 for elemental casting
            if (Input.GetKeyDown(KeyCode.Alpha1)) CastFrostNova();
            if (Input.GetKeyDown(KeyCode.Alpha2)) CastLightningArc();
            if (Input.GetKeyDown(KeyCode.Alpha3)) CastPyreBlast();
            if (Input.GetKeyDown(KeyCode.Alpha4)) CastAcidFlask();
            if (Input.GetKeyDown(KeyCode.Alpha5)) CastVenomDart();
            if (Input.GetKeyDown(KeyCode.Alpha6)) CastSlumberHex();
        }

        #region Spell 1: Frost Nova (Ice -> Freeze / Chilled)
        public void CastFrostNova()
        {
            Vector3 origin = transform.position + transform.forward * 2.0f;
            float radius = 5.0f;
            float baseDamage = 60f;

            // Spawn radial icy shockwave
            BattleSummoner.VFX.ElementalSpellParticlePack.PlaySpellEffect(BattleSummoner.VFX.SpellEffectType.FrostNova, origin);
            SpawnSpellVFX("FrostNovaVFX", origin, radius, new Color(0.2f, 0.85f, 1.0f, 0.7f));

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("spell_cast", 0.95f);
            }

            Collider[] hits = Physics.OverlapSphere(origin, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    var payload = new DamagePayload(baseDamage, DamageType.Ice, hits[i].transform.position, (hits[i].transform.position - transform.position).normalized, gameObject)
                    {
                        appliedStatus = StatusEffectType.Freeze,
                        statusDuration = 4.5f,
                        statusPotency = 8f,
                        isAoE = true,
                        aoeRadius = radius
                    };
                    receiver.TakeAdvancedDamage(payload);
                }
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#00E0FF>[FROST NOVA]</color>\nEnemies encased in crystalline ice!");
            }
        }
        #endregion

        #region Spell 2: Lightning Arc (Electric -> Shocked / SHATTERED!)
        public void CastLightningArc()
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 forward = transform.forward;
            float baseDamage = 85f;

            // Find closest target in front
            Transform target = FindTargetInFront(origin, forward, spellRange);
            Vector3 hitPos = target != null ? target.position : origin + forward * spellRange;

            // Spawn electric beam VFX
            SpawnLightningBeamVFX(origin, hitPos);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }

            if (target != null && target.TryGetComponent<IDamageReceiver>(out var receiver))
            {
                var payload = new DamagePayload(baseDamage, DamageType.Electric, hitPos, forward, gameObject)
                {
                    appliedStatus = StatusEffectType.Shocked,
                    statusDuration = 4.0f,
                    statusPotency = 14f,
                    critChance = 0.25f,
                    critMultiplier = 2.2f
                };
                receiver.TakeAdvancedDamage(payload);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#FFEE22>[LIGHTNING ARC]</color>\nGalvanic bolt unleashed! Shatters frozen targets!");
            }
        }
        #endregion

        #region Spell 3: Pyre Blast (Fire -> Burn / COMBUSTION / MELT)
        public void CastPyreBlast()
        {
            Vector3 origin = transform.position + transform.forward * 2.5f;
            float radius = 4.5f;
            float baseDamage = 80f;

            BattleSummoner.VFX.StylizedFireEffectsPack.SpawnFlamePillar(origin, 1.2f);
            BattleSummoner.VFX.StylizedFireEffectsPack.SpawnBurningGroundPatch(origin, radius, 4.0f);
            SpawnSpellVFX("PyreBlastVFX", origin, radius, new Color(1.0f, 0.40f, 0.1f, 0.8f));

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.9f);
            }

            Collider[] hits = Physics.OverlapSphere(origin, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    var payload = new DamagePayload(baseDamage, DamageType.Fire, hits[i].transform.position, (hits[i].transform.position - transform.position).normalized, gameObject)
                    {
                        appliedStatus = StatusEffectType.Burn,
                        statusDuration = 5.0f,
                        statusPotency = 15f,
                        isAoE = true,
                        aoeRadius = radius
                    };
                    receiver.TakeAdvancedDamage(payload);
                }
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#FF5511>[PYRE BLAST]</color>\nScorching fire wave! Triggers Combustion on Poison/Acid!");
            }
        }
        #endregion

        #region Spell 4: Caustic Acid Flask (Acid -> Corrodes Armor)
        public void CastAcidFlask()
        {
            Vector3 origin = transform.position + transform.forward * 2.5f;
            float radius = 4.0f;
            float baseDamage = 50f;

            BattleSummoner.VFX.ElementalSpellParticlePack.PlaySpellEffect(BattleSummoner.VFX.SpellEffectType.AcidSplashPuddle, origin);
            SpawnSpellVFX("AcidFlaskVFX", origin, radius, new Color(0.6f, 1.0f, 0.1f, 0.75f));

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.8f);
            }

            Collider[] hits = Physics.OverlapSphere(origin, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    var payload = new DamagePayload(baseDamage, DamageType.Acid, hits[i].transform.position, Vector3.up, gameObject)
                    {
                        appliedStatus = StatusEffectType.AcidCorroded,
                        statusDuration = 6.0f,
                        statusPotency = 10f,
                        isAoE = true,
                        aoeRadius = radius
                    };
                    receiver.TakeAdvancedDamage(payload);
                }
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#AAFF00>[CAUSTIC ACID]</color>\nArmor dissolved! Enemies take +35% damage!");
            }
        }
        #endregion

        #region Spell 5: Venom Dart (Poison -> Poisoned DoT)
        public void CastVenomDart()
        {
            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3 forward = transform.forward;
            float baseDamage = 45f;

            Transform target = FindTargetInFront(origin, forward, spellRange);
            Vector3 hitPos = target != null ? target.position : origin + forward * spellRange;

            BattleSummoner.VFX.ElementalSpellParticlePack.PlaySpellEffect(BattleSummoner.VFX.SpellEffectType.ToxicPoisonCloud, hitPos);
            SpawnSpellVFX("VenomDartVFX", hitPos, 1.5f, new Color(0.2f, 0.95f, 0.3f, 0.85f));

            if (target != null && target.TryGetComponent<IDamageReceiver>(out var receiver))
            {
                var payload = new DamagePayload(baseDamage, DamageType.Poison, hitPos, forward, gameObject)
                {
                    appliedStatus = StatusEffectType.Poisoned,
                    statusDuration = 6.0f,
                    statusPotency = 14f
                };
                receiver.TakeAdvancedDamage(payload);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#44EE44>[VENOM DART]</color>\nToxic poison injected!");
            }
        }
        #endregion

        #region Spell 6: Slumber Hex (Sleep -> Nightmare Reap Setup)
        public void CastSlumberHex()
        {
            Vector3 origin = transform.position + transform.forward * 2.0f;
            float radius = 4.5f;

            BattleSummoner.VFX.ElementalSpellParticlePack.PlaySpellEffect(BattleSummoner.VFX.SpellEffectType.CurseHexSkull, origin);
            SpawnSpellVFX("SlumberHexVFX", origin, radius, new Color(0.75f, 0.45f, 1.0f, 0.70f));

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.9f);
            }

            Collider[] hits = Physics.OverlapSphere(origin, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].TryGetComponent<StatusEffectReceiver>(out var receiver))
                {
                    receiver.ApplyStatus(StatusEffectType.Sleep, 6.0f, 0f);
                }
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#D0A0FF>[SLUMBER HEX]</color>\nEnemies put to sleep! Wake them with a heavy swipe for a 2.5x Crit!");
            }
        }
        #endregion

        #region Helpers & Visuals
        private Transform FindTargetInFront(Vector3 origin, Vector3 forward, float range)
        {
            Collider[] hits = Physics.OverlapSphere(origin, range);
            Transform best = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject || hits[i].CompareTag("Player")) continue;
                if (hits[i].GetComponent<SpireEnemy>() != null || hits[i].GetComponent<BossController>() != null || hits[i].GetComponent<StatusEffectReceiver>() != null)
                {
                    float dist = Vector3.Distance(origin, hits[i].transform.position);
                    float dot = Vector3.Dot(forward, (hits[i].transform.position - origin).normalized);
                    if (dot > 0.3f && dist < closestDist)
                    {
                        closestDist = dist;
                        best = hits[i].transform;
                    }
                }
            }
            return best;
        }

        private void SpawnSpellVFX(string name, Vector3 pos, float scale, Color color)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.position = pos + Vector3.up * 0.5f;
            sphere.transform.localScale = Vector3.one * scale;

            var col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 3.5f);
                rend.sharedMaterial = mat;
            }
            Destroy(sphere, 0.45f);
        }

        private void SpawnLightningBeamVFX(Vector3 from, Vector3 to)
        {
            BattleSummoner.VFX.LightningSpellManager.CastBolt(from, to, new Color(0.25f, 0.88f, 1.0f, 1f), 0.32f);
        }
        #endregion
    }
}
