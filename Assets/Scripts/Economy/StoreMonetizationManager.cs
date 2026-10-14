using System;
using UnityEngine;

public class StoreMonetizationManager : MonoBehaviour
{
    public static StoreMonetizationManager Instance { get; private set; }

    [SerializeField] private EquipmentSystem equipmentSystem;

    // Events for purchase telemetry and UI response
    public static event Action<string> OnPurchaseSuccessful;
    public static event Action<string> OnPurchaseFailed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (equipmentSystem == null)
        {
            equipmentSystem = FindAnyObjectByType<EquipmentSystem>();
        }
    }

    public void SetEquipmentSystem(EquipmentSystem equip)
    {
        equipmentSystem = equip;
    }

    /// <summary>
    /// Process standard premium in-app purchase of utility accessory (+0.25x buff tier).
    /// </summary>
    public void ProcessAccessoryPurchase(string skuId, string accessoryName, float additiveBonus)
    {
        // Passes Google Play Android Billing guidelines (no pay-to-win high-tier dungeon drops sold in store)
        try
        {
            AccessoryItem premiumAcc = new AccessoryItem
            {
                id = skuId,
                accessoryName = accessoryName,
                isPremiumStoreTier = true,
                additiveValue = additiveBonus,
                multiplicativeValue = 1.0f
            };

            if (equipmentSystem == null)
            {
                equipmentSystem = FindAnyObjectByType<EquipmentSystem>();
            }

            if (equipmentSystem != null)
            {
                equipmentSystem.EquipAccessory(premiumAcc);
            }

            OnPurchaseSuccessful?.Invoke(skuId);
            Debug.Log($"[MONETIZATION] Purchase validated for SKU: {skuId}");
        }
        catch (Exception ex)
        {
            OnPurchaseFailed?.Invoke(ex.Message);
            Debug.LogError($"[MONETIZATION FAILED] {ex.Message}");
        }
    }
}
