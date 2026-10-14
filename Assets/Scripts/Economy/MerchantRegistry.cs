using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MerchantRegistry tracks registered NPC merchants and their bank liquidity balances.
/// </summary>
public class MerchantRegistry : MonoBehaviour
{
    public static MerchantRegistry Instance { get; private set; }

    [SerializeField] private List<TraderAccount> registeredMerchants = new List<TraderAccount>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    public TraderAccount GetMerchantAccount(string merchantName)
    {
        return registeredMerchants.Find(m => string.Equals(m.entityName, merchantName, StringComparison.OrdinalIgnoreCase));
    }
}
