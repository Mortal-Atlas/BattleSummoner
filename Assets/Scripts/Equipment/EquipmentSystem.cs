using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AccessoryItem
{
    public string id;
    public string accessoryName;
    public bool isPremiumStoreTier; // True = additive ~0.25x; False = multiplicative dungeon drop
    public float additiveValue = 0.25f;
    public float multiplicativeValue = 1.0f; // e.g., 3.0x Relic Strike
    public int marketPriceManaCores = 100;
}

public class EquipmentSystem : MonoBehaviour
{
    [Header("Core Slots")]
    public string mainHandWeaponId = "voracious_scythe_tier1";
    public string offHandItemId = "astral_tome_tier1";
    public string armorItemId = "spire_warden_cuirass_tier1";
    public bool isTwoHandedEquipped = false;

    public WeaponSlotType ActiveWeaponSlotType => isTwoHandedEquipped ? WeaponSlotType.TwoHanded : WeaponSlotType.OneHanded;

    public float GetActiveProficiencyExpRate()
    {
        var contractSys = GetComponent<BindingContractSystem>() ?? BindingContractSystem.Instance;
        return contractSys != null ? contractSys.GetWeaponProficiencyExpRate(ActiveWeaponSlotType) : 1.0f;
    }

    [Header("Uncapped Accessory Buffer")]
    public List<AccessoryItem> equippedAccessories = new List<AccessoryItem>();

    public event Action<AccessoryItem> OnAccessoryEquipped;

    public void EquipAccessory(AccessoryItem accessory)
    {
        if (accessory == null) return;
        equippedAccessories.Add(accessory);
        OnAccessoryEquipped?.Invoke(accessory);

        // Bridge into UncappedAccessorySystem
        if (UncappedAccessorySystem.Instance != null)
        {
            var def = new AccessoryDefinition
            {
                accessoryId = accessory.id,
                accessoryName = accessory.accessoryName,
                isPremiumStoreTier = accessory.isPremiumStoreTier,
                scalingType = accessory.isPremiumStoreTier ? ScalingCurveType.LinearAdditive : ScalingCurveType.ExponentialCompound,
                baseModifier = accessory.isPremiumStoreTier ? accessory.additiveValue : accessory.multiplicativeValue,
                marketValueInManaCores = accessory.marketPriceManaCores
            };
            UncappedAccessorySystem.Instance.AcquireAccessory(def, 1);
        }

        if (!accessory.isPremiumStoreTier && accessory.multiplicativeValue >= 3.0f)
        {
            SystemBroadcastManager.Instance?.TriggerAchievement("ach_relic_3x_drop", "LocalPlayer");
        }
    }

    public float CalculateTotalDamage(float baseDamage)
    {
        if (UncappedAccessorySystem.Instance != null)
        {
            return UncappedAccessorySystem.Instance.CalculateTotalStrikeOutput(baseDamage);
        }

        float additivePool = 0f;
        float compoundMultiplier = 1.0f;

        for (int i = 0; i < equippedAccessories.Count; i++)
        {
            var acc = equippedAccessories[i];
            if (acc.isPremiumStoreTier)
            {
                additivePool += acc.additiveValue;
            }
            else
            {
                compoundMultiplier *= acc.multiplicativeValue;
            }
        }

        return (baseDamage * (1.0f + additivePool)) * compoundMultiplier;
    }
}
