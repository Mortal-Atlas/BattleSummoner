using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponSlotType
{
    OneHanded,
    TwoHanded
}

[Serializable]
public class BindingContractDefinition
{
    public string contractId;
    public string contractTitle;
    [Header("Boons")]
    public float flatStatMultiplierBonus = 0.30f; // +30% output
    public WeaponSlotType favoredWeaponType = WeaponSlotType.OneHanded;
    public float favoredProficiencyExpMultiplier = 1.50f; // +50% faster mastery

    [Header("Permanent Costs")]
    public float downedPickupTimePenalty = 0.65f; // +65% longer to get picked up while knocked down
    public float respawnTimerPenalty = 0.80f;     // +80% longer to respawn after death
    public WeaponSlotType penalizedWeaponType = WeaponSlotType.TwoHanded;
    public float penalizedProficiencyExpMultiplier = 0.45f; // 55% slower mastery on opposing grip
}

public class BindingContractSystem : MonoBehaviour
{
    public static BindingContractSystem Instance { get; private set; }

    [Header("Base Revive Timers (Seconds)")]
    [SerializeField] private float baseKnockedDownPickupSeconds = 4.0f;
    [SerializeField] private float baseFullRespawnSeconds = 15.0f;

    [Header("Signed Permanent Contracts (Persisted in SaveSystem)")]
    public List<BindingContractDefinition> signedContracts = new List<BindingContractDefinition>();

    private EquipmentSystem equipmentSystem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
        equipmentSystem = GetComponent<EquipmentSystem>();
    }

    private void Start()
    {
        UpdateVisualChainOverlay();
    }

    public void SignPermanentContract(BindingContractDefinition newContract)
    {
        if (signedContracts.Exists(c => c.contractId == newContract.contractId)) return;
        signedContracts.Add(newContract);
        Debug.Log($"⛓️ [BINDING CONTRACT] Permanently bound to '{newContract.contractTitle}'. Revive timers and weapon proficiencies mutated.");

        // Hook into UncappedAccessorySystem: Feed additive stat-for-soul compound node
        if (UncappedAccessorySystem.Instance != null && newContract.flatStatMultiplierBonus > 0f)
        {
            var def = new AccessoryDefinition
            {
                accessoryId = $"contract_node_{newContract.contractId}",
                accessoryName = $"{newContract.contractTitle} Boon",
                isPremiumStoreTier = false,
                scalingType = ScalingCurveType.LinearAdditive,
                baseModifier = newContract.flatStatMultiplierBonus,
                marketValueInManaCores = 0
            };
            UncappedAccessorySystem.Instance.AcquireAccessory(def, 1);
        }

        UpdateVisualChainOverlay();
    }

    public void LoadContractsFromSave(List<string> contractIds)
    {
        if (contractIds == null) return;
        foreach (var id in contractIds)
        {
            if (signedContracts.Exists(c => c.contractId == id)) continue;

            if (id == "contract_singular_grip")
            {
                signedContracts.Add(CreateSingularGripContract());
            }
            else if (id == "contract_stat_for_soul")
            {
                signedContracts.Add(CreateStatForSoulContract());
            }
            else
            {
                signedContracts.Add(new BindingContractDefinition
                {
                    contractId = id,
                    contractTitle = $"Ancient Pact {id}"
                });
            }
        }
        UpdateVisualChainOverlay();
    }

    public static BindingContractDefinition CreateSingularGripContract()
    {
        return new BindingContractDefinition
        {
            contractId = "contract_singular_grip",
            contractTitle = "Singular Grip Pact",
            flatStatMultiplierBonus = 0.20f,
            favoredWeaponType = WeaponSlotType.OneHanded,
            favoredProficiencyExpMultiplier = 1.45f, // +45% proficiency EXP
            penalizedWeaponType = WeaponSlotType.TwoHanded,
            penalizedProficiencyExpMultiplier = 0.45f, // 0.45x proficiency EXP on TwoHanded
            downedPickupTimePenalty = 0.40f,
            respawnTimerPenalty = 0.50f
        };
    }

    public static BindingContractDefinition CreateStatForSoulContract()
    {
        return new BindingContractDefinition
        {
            contractId = "contract_stat_for_soul",
            contractTitle = "Stat-for-Soul Pact",
            flatStatMultiplierBonus = 0.35f, // +35% base combat stats
            favoredWeaponType = WeaponSlotType.OneHanded,
            favoredProficiencyExpMultiplier = 1.20f,
            penalizedWeaponType = WeaponSlotType.TwoHanded,
            penalizedProficiencyExpMultiplier = 0.60f,
            downedPickupTimePenalty = 0.75f, // +75% longer downed pickup
            respawnTimerPenalty = 1.0f      // +100% longer respawn timer
        };
    }

    public void UpdateVisualChainOverlay()
    {
        var rebalancer = FindAnyObjectByType<BattleViewportRebalancer>();
        if (rebalancer != null)
        {
            bool hasContracts = signedContracts != null && signedContracts.Count > 0;
            rebalancer.SetContractChainOverlay(hasContracts);
        }
    }

    public float GetKnockedDownPickupDuration()
    {
        float penaltySum = 0f;
        foreach (var c in signedContracts) penaltySum += c.downedPickupTimePenalty;
        return baseKnockedDownPickupSeconds * (1f + penaltySum);
    }

    public float GetFullRespawnDuration()
    {
        float penaltySum = 0f;
        foreach (var c in signedContracts) penaltySum += c.respawnTimerPenalty;
        return baseFullRespawnSeconds * (1f + penaltySum);
    }

    public float GetContractCombatStatMultiplier()
    {
        float bonusSum = 1.0f;
        foreach (var c in signedContracts) bonusSum += c.flatStatMultiplierBonus;
        return bonusSum;
    }

    public float GetWeaponProficiencyExpRate(WeaponSlotType activeHandType)
    {
        float rate = 1.0f;
        foreach (var c in signedContracts)
        {
            if (activeHandType == c.favoredWeaponType)
                rate *= c.favoredProficiencyExpMultiplier;
            else if (activeHandType == c.penalizedWeaponType)
                rate *= c.penalizedProficiencyExpMultiplier;
        }
        return rate;
    }
}
