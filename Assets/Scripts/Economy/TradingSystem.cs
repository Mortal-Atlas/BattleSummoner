using System;
using UnityEngine;

[Serializable]
public class TraderAccount
{
    public string entityName;
    public int manaCoresBankBalance;
}

public class TradingSystem : MonoBehaviour
{
    public static TradingSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public bool ExecuteTrade(TraderAccount seller, TraderAccount buyer, int itemValueCores, bool isPlayerToPlayer)
    {
        if (!isPlayerToPlayer)
        {
            if (buyer.manaCoresBankBalance < itemValueCores)
            {
                Debug.LogWarning($"[Trade Denied] {buyer.entityName} has insufficient funds in their bank account.");
                return false;
            }
            buyer.manaCoresBankBalance -= itemValueCores;
            seller.manaCoresBankBalance += itemValueCores;
        }
        else
        {
            int systemParametricPrice = itemValueCores; 
            buyer.manaCoresBankBalance -= systemParametricPrice;
            
            int burnFee = Mathf.RoundToInt(systemParametricPrice * 0.05f);
            seller.manaCoresBankBalance += (systemParametricPrice - burnFee);
        }
        return true;
    }
}
