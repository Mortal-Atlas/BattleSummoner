using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Prismatic
}

[System.Serializable]
public class TradeableItem
{
    public string itemId;
    public string itemName;
    public ItemRarity rarity = ItemRarity.Common;
    public int tier = 1;
    public float accessoryMultiplier = 1.0f;
    public bool isAccessory = false;
    public AccessoryItem accessoryData;

    public int GetSystemFixedPrice()
    {
        return TradeSystem.CalculateFixedMarketPrice(rarity, tier, accessoryMultiplier);
    }
}

[System.Serializable]
public class NPCBankAccount
{
    public string npcId;
    public string npcName;
    public bool isAuthorizedMerchant = false;
    public int currentBankBalanceCores = 500;
    public int maxBankCapacityCores = 2500;
    public float passiveReplenishPerMinute = 15f;
    public List<TradeableItem> merchantStock = new List<TradeableItem>();
}

[System.Serializable]
public class P2PMarketplaceListing
{
    public string listingId;
    public string sellerPlayerId;
    public string sellerDisplayName;
    public TradeableItem item;
    public int fixedPriceManaCores;
    public long timestampUnix;
    public bool isSold;
}

public class TradeTransactionResult
{
    public bool success;
    public string message;
    public int coresTransacted;
    public string counterpartyId;
}

/// <summary>
/// Universal Trading & Mercantile Engine (Phase 4.3).
/// Enforces hard-capped NPC bank account balances, strict merchant selling restrictions,
/// and a fixed-price player-to-player marketplace determined by system parameters.
/// </summary>
public class TradeSystem : MonoBehaviour
{
    public static TradeSystem Instance { get; private set; }

    [Header("NPC Mercantile Registry")]
    [SerializeField] private List<NPCBankAccount> npcAccounts = new List<NPCBankAccount>();
    private readonly Dictionary<string, NPCBankAccount> npcRegistry = new Dictionary<string, NPCBankAccount>();

    [Header("P2P Fixed-Rate Marketplace")]
    [SerializeField] private List<P2PMarketplaceListing> activeMarketplaceListings = new List<P2PMarketplaceListing>();

    public event Action<TradeTransactionResult> OnTradeExecuted;
    public event Action<string, int> OnNPCBankBalanceUpdated;
    public event Action OnMarketplaceListingsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeDefaultNPCAccounts();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void EnsureInitialized()
    {
        if (npcRegistry.Count == 0)
        {
            InitializeDefaultNPCAccounts();
        }
    }

    private GemologySystem GetGemology()
    {
        return GemologySystem.Instance != null ? GemologySystem.Instance : FindAnyObjectByType<GemologySystem>();
    }

    private void InitializeDefaultNPCAccounts()
    {
        npcRegistry.Clear();

        if (npcAccounts.Count == 0)
        {
            // Margo: Authorized Alchemist / Appraiser Merchant
            npcAccounts.Add(new NPCBankAccount
            {
                npcId = "npc_margo",
                npcName = "Margo",
                isAuthorizedMerchant = true,
                currentBankBalanceCores = 1200,
                maxBankCapacityCores = 5000,
                merchantStock = new List<TradeableItem>
                {
                    new TradeableItem { itemId = "elixir_satiety_t1", itemName = "Distilled Biomass Tonic", rarity = ItemRarity.Uncommon, tier = 1 },
                    new TradeableItem { itemId = "catalyst_amethyst_dust", itemName = "Void Dust Catalyst", rarity = ItemRarity.Rare, tier = 2 }
                }
            });

            // Barnaby: Authorized Master Blacksmith Merchant
            npcAccounts.Add(new NPCBankAccount
            {
                npcId = "npc_barnaby",
                npcName = "Barnaby",
                isAuthorizedMerchant = true,
                currentBankBalanceCores = 2500,
                maxBankCapacityCores = 10000,
                merchantStock = new List<TradeableItem>
                {
                    new TradeableItem { itemId = "whetstone_tempered", itemName = "Basalt Whetstone", rarity = ItemRarity.Common, tier = 1 },
                    new TradeableItem { itemId = "anima_stabilizer_core", itemName = "Anima Re-Anchoring Rod", rarity = ItemRarity.Epic, tier = 3 }
                }
            });

            // Zephyr: Non-Merchant Scout (Player cannot sell items to Zephyr; only buy/gift)
            npcAccounts.Add(new NPCBankAccount
            {
                npcId = "npc_zephyr",
                npcName = "Zephyr",
                isAuthorizedMerchant = false,
                currentBankBalanceCores = 180,
                maxBankCapacityCores = 500,
                merchantStock = new List<TradeableItem>
                {
                    new TradeableItem { itemId = "zephyr_feather_token", itemName = "Windstalker Feather", rarity = ItemRarity.Common, tier = 1 }
                }
            });
        }

        foreach (var acc in npcAccounts)
        {
            npcRegistry[acc.npcId] = acc;
        }

        // Populate initial P2P marketplace simulated listings
        if (activeMarketplaceListings.Count == 0)
        {
            CreateSimulatedP2PListing("delver_kayla", "Kayla_SpireWalker", new TradeableItem
            {
                itemId = "acc_gilded_seal",
                itemName = "Seal of the Gilded Topaz",
                rarity = ItemRarity.Rare,
                tier = 2,
                accessoryMultiplier = 1.35f,
                isAccessory = true
            });

            CreateSimulatedP2PListing("delver_orun", "Orun_TheShield", new TradeableItem
            {
                itemId = "relic_prismatic_shard",
                itemName = "Prismatic Core Splinter",
                rarity = ItemRarity.Epic,
                tier = 3,
                accessoryMultiplier = 2.0f
            });
        }
    }

    #region Fixed Price Calculation Engine
    /// <summary>
    /// Calculates immutable system price based strictly on rarity, tier, and accessory multipliers.
    /// Prevents hyperinflation and eliminates price manipulation in the player economy.
    /// </summary>
    public static int CalculateFixedMarketPrice(ItemRarity rarity, int tier, float accessoryMultiplier = 1.0f)
    {
        int baseRarityPrice = rarity switch
        {
            ItemRarity.Common => 50,
            ItemRarity.Uncommon => 150,
            ItemRarity.Rare => 400,
            ItemRarity.Epic => 1200,
            ItemRarity.Legendary => 3500,
            ItemRarity.Prismatic => 10000,
            _ => 50
        };

        float tierScale = Mathf.Pow(1.4f, Mathf.Max(0, tier - 1));
        float multiplierScale = Mathf.Max(1.0f, accessoryMultiplier);
        return Mathf.RoundToInt(baseRarityPrice * tierScale * multiplierScale);
    }
    #endregion

    #region NPC Mercantile & Hard-Capped Transactions
    public NPCBankAccount GetNPCAccount(string npcId)
    {
        EnsureInitialized();
        return npcRegistry.TryGetValue(npcId, out var account) ? account : null;
    }

    /// <summary>
    /// Sells an item from player to an NPC counterparty.
    /// Hard-capped by the counterparty's actual bank account balance:
    /// No player can extract more currency than an NPC holds.
    /// Selling items is strictly restricted to authorized merchants.
    /// </summary>
    public TradeTransactionResult SellItemToNPC(string npcId, TradeableItem item)
    {
        EnsureInitialized();
        var result = new TradeTransactionResult { counterpartyId = npcId };

        if (!npcRegistry.TryGetValue(npcId, out NPCBankAccount account))
        {
            result.success = false;
            result.message = $"Counterparty [{npcId}] is not registered in the mercantile ledger.";
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        // Rule 1: Merchant Restrictions (Authorized merchants only)
        if (!account.isAuthorizedMerchant)
        {
            result.success = false;
            result.message = $"Trade Rejected: {account.npcName} is not a licensed guild merchant. Selling items is restricted exclusively to authorized merchants and other players.";
            Debug.LogWarning($"[TradeSystem] {result.message}");
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        int itemValue = item.GetSystemFixedPrice();

        // Rule 2: Hard-Capped by NPC's actual bank account balance
        if (account.currentBankBalanceCores <= 0)
        {
            result.success = false;
            result.message = $"Transaction Denied: {account.npcName}'s vault is completely exhausted (0 Mana Cores).";
            Debug.LogWarning($"[TradeSystem] {result.message}");
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        if (account.currentBankBalanceCores < itemValue)
        {
            result.success = false;
            result.message = $"Insufficient NPC Funds: {account.npcName} only holds {account.currentBankBalanceCores} Mana Cores, but the fixed price is {itemValue}. Delvers cannot extract more currency than an NPC holds.";
            Debug.LogWarning($"[TradeSystem] {result.message}");
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        // Execute transaction
        account.currentBankBalanceCores -= itemValue;
        account.merchantStock.Add(item);

        var gemology = GetGemology();
        if (gemology != null)
        {
            gemology.totalManaCores += itemValue;
        }

        result.success = true;
        result.coresTransacted = itemValue;
        result.message = $"Sold {item.itemName} to {account.npcName} for {itemValue} Mana Cores. (NPC Vault remaining: {account.currentBankBalanceCores})";
        Debug.Log($"[TradeSystem] {result.message}");

        OnNPCBankBalanceUpdated?.Invoke(npcId, account.currentBankBalanceCores);
        OnTradeExecuted?.Invoke(result);
        return result;
    }

    /// <summary>
    /// Purchases an item from an NPC merchant stock.
    /// Player transfers Mana Cores into the NPC's bank account (capped at max capacity).
    /// </summary>
    public TradeTransactionResult BuyItemFromNPC(string npcId, TradeableItem item)
    {
        EnsureInitialized();
        var result = new TradeTransactionResult { counterpartyId = npcId };

        if (!npcRegistry.TryGetValue(npcId, out NPCBankAccount account))
        {
            result.success = false;
            result.message = $"Merchant [{npcId}] not found.";
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        int price = item.GetSystemFixedPrice();
        var gemology = GetGemology();
        if (gemology == null || gemology.totalManaCores < price)
        {
            result.success = false;
            result.message = $"Insufficient player funds: Requires {price} Mana Cores.";
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        // Deduct player funds, credit NPC vault up to maximum capacity
        gemology.totalManaCores -= price;
        account.currentBankBalanceCores = Mathf.Min(account.maxBankCapacityCores, account.currentBankBalanceCores + price);
        account.merchantStock.Remove(item);

        // If accessory, equip or add to inventory
        if (item.isAccessory && item.accessoryData != null)
        {
            var equip = FindAnyObjectByType<EquipmentSystem>();
            if (equip != null) equip.EquipAccessory(item.accessoryData);
        }

        result.success = true;
        result.coresTransacted = price;
        result.message = $"Purchased {item.itemName} from {account.npcName} for {price} Mana Cores.";
        Debug.Log($"[TradeSystem] {result.message}");

        OnNPCBankBalanceUpdated?.Invoke(npcId, account.currentBankBalanceCores);
        OnTradeExecuted?.Invoke(result);
        return result;
    }
    #endregion

    #region Player-to-Player (P2P) Fixed-Rate Marketplace
    public IReadOnlyList<P2PMarketplaceListing> GetActiveMarketplaceListings()
    {
        return activeMarketplaceListings.FindAll(l => !l.isSold);
    }

    public P2PMarketplaceListing CreateSimulatedP2PListing(string sellerId, string sellerName, TradeableItem item)
    {
        int fixedPrice = item.GetSystemFixedPrice();
        var listing = new P2PMarketplaceListing
        {
            listingId = Guid.NewGuid().ToString("N").Substring(0, 8),
            sellerPlayerId = sellerId,
            sellerDisplayName = sellerName,
            item = item,
            fixedPriceManaCores = fixedPrice,
            timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            isSold = false
        };

        activeMarketplaceListings.Add(listing);
        OnMarketplaceListingsChanged?.Invoke();
        return listing;
    }

    /// <summary>
    /// Lists an item on the P2P marketplace. Fixed price is strictly calculated by system parameters.
    /// </summary>
    public P2PMarketplaceListing ListPlayerItemOnMarketplace(string sellerPlayerId, string sellerDisplayName, TradeableItem item)
    {
        var listing = CreateSimulatedP2PListing(sellerPlayerId, sellerDisplayName, item);
        Debug.Log($"[P2P Bazaar] Listed {item.itemName} at system fixed price: {listing.fixedPriceManaCores} Mana Cores.");
        return listing;
    }

    /// <summary>
    /// Purchases an item from the fixed-rate P2P marketplace.
    /// </summary>
    public TradeTransactionResult PurchaseP2PListing(string listingId)
    {
        var result = new TradeTransactionResult();
        var listing = activeMarketplaceListings.Find(l => l.listingId == listingId && !l.isSold);

        if (listing == null)
        {
            result.success = false;
            result.message = "Listing no longer available or already purchased.";
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        if (GemologySystem.Instance == null || GemologySystem.Instance.totalManaCores < listing.fixedPriceManaCores)
        {
            result.success = false;
            result.message = $"Insufficient Mana Cores. Requires {listing.fixedPriceManaCores} cores.";
            OnTradeExecuted?.Invoke(result);
            return result;
        }

        // Deduct player currency
        GemologySystem.Instance.totalManaCores -= listing.fixedPriceManaCores;
        listing.isSold = true;

        // If accessory, equip
        if (listing.item.isAccessory && listing.item.accessoryData != null)
        {
            var equip = FindAnyObjectByType<EquipmentSystem>();
            if (equip != null) equip.EquipAccessory(listing.item.accessoryData);
        }

        result.success = true;
        result.counterpartyId = listing.sellerPlayerId;
        result.coresTransacted = listing.fixedPriceManaCores;
        result.message = $"Successfully bought {listing.item.itemName} from {listing.sellerDisplayName} for {listing.fixedPriceManaCores} Mana Cores.";
        Debug.Log($"[P2P Bazaar] {result.message}");

        OnMarketplaceListingsChanged?.Invoke();
        OnTradeExecuted?.Invoke(result);
        return result;
    }
    #endregion
}
