using System;
using UnityEngine;
using BattleSummoner.Combat;

public enum ApexMutationTrait
{
    None,
    ObsidianThermalCarapace, // 85% Fire resistance + reflects 35% Fire spells back
    ReactiveSerratedScales,  // Doubles attacker's WeaponTemper.DegradeTemper() rate on contact
    AetherGroundingOrgans   // Absorbs shock ticks to boost move speed
}

/// <summary>
/// Attached to an Apex Nemesis spawned due to species over-farming.
/// Implements adaptive counter-mutations and kin-scent target locking.
/// </summary>
public class ApexNemesisEntity : MonoBehaviour
{
    [Header("Apex Nemesis Configuration")]
    public string speciesId;
    public int floorNumber;
    public string huntedMaterialId;
    public ApexMutationTrait mutationTrait = ApexMutationTrait.None;
    public Transform kinScentTarget;

    [Header("Adaptive Counter Stats")]
    [SerializeField] private float fireResistance = 0.85f;
    [SerializeField] private float fireReflectRatio = 0.35f;
    [SerializeField] private float shockAbsorptionSpeedBoost = 1.35f;

    private SpireEnemy spireEnemy;

    private void Awake()
    {
        spireEnemy = GetComponent<SpireEnemy>();
    }

    private void Start()
    {
        ApplyVisualMutationEffect();
    }

    private void ApplyVisualMutationEffect()
    {
        // Enlarge scale to make the Apex Nemesis visually towering
        transform.localScale = transform.localScale * 1.55f;

        var rend = GetComponentInChildren<Renderer>();
        if (rend != null && rend.material.HasProperty("_BaseColor"))
        {
            Color auraCol = mutationTrait switch
            {
                ApexMutationTrait.ObsidianThermalCarapace => new Color(0.12f, 0.08f, 0.08f, 1f), // Pitch black obsidian
                ApexMutationTrait.ReactiveSerratedScales => new Color(0.85f, 0.15f, 0.15f, 1f),  // Serrated crimson
                ApexMutationTrait.AetherGroundingOrgans => new Color(0.2f, 0.75f, 1.0f, 1f),    // Crackling blue
                _ => Color.magenta
            };
            rend.material.SetColor("_BaseColor", auraCol);
            if (rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", auraCol * 2.0f);
            }
        }
    }

    /// <summary>
    /// Processes incoming damage against active adaptive counter-mutation traits.
    /// </summary>
    public float ModifyIncomingDamage(float damage, string damageType, Transform attacker)
    {
        // 1. Obsidian Thermal Carapace: 85% Fire resistance + reflects 35% back to caster
        if (mutationTrait == ApexMutationTrait.ObsidianThermalCarapace &&
            (damageType.Contains("Fire") || damageType.Contains("Thermal")))
        {
            float mitigatedDamage = damage * (1f - fireResistance);
            float reflectDmg = damage * fireReflectRatio;

            if (attacker != null)
            {
                var playerProg = attacker.GetComponent<PlayerProgression>() ?? attacker.GetComponentInParent<PlayerProgression>();
                if (playerProg != null)
                {
                    playerProg.TakeDirectDamage(reflectDmg);
                    Debug.Log($"🔥 [OBSIDIAN CARAPACE] Reflected {reflectDmg:F1} Fire damage back at caster!");
                }
            }
            return mitigatedDamage;
        }

        // 2. Reactive Serrated Scales: Doubles attacker's WeaponTemper degradation on blade contact
        if (mutationTrait == ApexMutationTrait.ReactiveSerratedScales &&
            (damageType.Contains("Physical") || damageType.Contains("Slash")))
        {
            var temper = attacker != null ? attacker.GetComponentInChildren<WeaponTemper>() : FindAnyObjectByType<WeaponTemper>();
            if (temper != null)
            {
                temper.DegradeTemper(2.5f); // Accelerated wear penalty
                Debug.Log("⚔️ [SERRATED SCALES] Attacker's blade temper severely degraded upon hitting reactive scales!");
            }
        }

        // 3. Aether Grounding Organs: Absorbs shock ticks to boost movement speed
        if (mutationTrait == ApexMutationTrait.AetherGroundingOrgans &&
            (damageType.Contains("Lightning") || damageType.Contains("Electric") || damageType.Contains("Shock")))
        {
            var receiver = GetComponent<StatusEffectReceiver>();
            if (receiver != null)
            {
                receiver.CleanseAllDebuffs();
            }
            Debug.Log("⚡ [AETHER GROUNDING] Nemesis absorbed shock tick and surged forward with accelerated velocity!");
            return damage * 0.40f; // 60% resistance
        }

        return damage;
    }
}
