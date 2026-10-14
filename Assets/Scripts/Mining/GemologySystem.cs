using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SocketedGem
{
    public MineralRarity gemType;
    public string gemTitle;
    public float statBonusValue;
    public bool isVolatile;
}

public class GemologySystem : MonoBehaviour
{
    public static GemologySystem Instance { get; private set; }

    [Header("Inventory Reserves")]
    public Dictionary<MineralRarity, int> gemInventory = new Dictionary<MineralRarity, int>();

    [Header("Player Currency")]
    public int totalManaCores = 250;

    public event Action<MineralRarity, int> OnInventoryUpdated;
    public event Action<int> OnManaCoresChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInventory();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        CaveMiningNode.OnMineralMined += HandleMinedYield;
    }

    private void OnDisable()
    {
        CaveMiningNode.OnMineralMined -= HandleMinedYield;
    }

    private void InitializeInventory()
    {
        gemInventory.Clear();
        gemInventory[MineralRarity.AzuriteCore] = 3;
        gemInventory[MineralRarity.CinnabarRuby] = 2;
        gemInventory[MineralRarity.GildedTopaz] = 1;
        gemInventory[MineralRarity.VoidAmethyst] = 1;
        gemInventory[MineralRarity.PrismaticHeart] = 0;
    }

    private void HandleMinedYield(MineralRarity gem, int count, int coreValue)
    {
        if (gemInventory.ContainsKey(gem))
        {
            gemInventory[gem] += count;
        }
        else
        {
            gemInventory[gem] = count;
        }

        totalManaCores += coreValue;
        OnInventoryUpdated?.Invoke(gem, gemInventory[gem]);
        OnManaCoresChanged?.Invoke(totalManaCores);

        Debug.Log($"[GEMOLOGY] Unearthed {count}x {gem} (+{coreValue} Mana Cores)! Vault updated.");
    }

    public void AddGemToInventory(string gemDropId)
    {
        if (Enum.TryParse<MineralRarity>(gemDropId, true, out var rarity))
        {
            HandleMinedYield(rarity, 1, 100);
        }
        else
        {
            if (!gemInventory.ContainsKey(MineralRarity.PrismaticHeart)) gemInventory[MineralRarity.PrismaticHeart] = 0;
            gemInventory[MineralRarity.PrismaticHeart]++;
            OnInventoryUpdated?.Invoke(MineralRarity.PrismaticHeart, gemInventory[MineralRarity.PrismaticHeart]);
        }
    }

    public int GetGemCount(MineralRarity gem)
    {
        return gemInventory.TryGetValue(gem, out int count) ? count : 0;
    }

    public bool SellGemToMerchant(MineralRarity gem, int quantity, int unitPrice)
    {
        if (!gemInventory.ContainsKey(gem) || gemInventory[gem] < quantity)
        {
            return false;
        }

        gemInventory[gem] -= quantity;
        totalManaCores += quantity * unitPrice;

        OnInventoryUpdated?.Invoke(gem, gemInventory[gem]);
        OnManaCoresChanged?.Invoke(totalManaCores);

        Debug.Log($"[GEM BAZAAR] Sold {quantity}x {gem} for {quantity * unitPrice} Mana Cores.");
        return true;
    }

    public bool SocketGemIntoWeapon(LivingGearSystem weapon, MineralRarity gem)
    {
        if (weapon == null || !gemInventory.ContainsKey(gem) || gemInventory[gem] <= 0)
        {
            return false;
        }

        gemInventory[gem]--;
        OnInventoryUpdated?.Invoke(gem, gemInventory[gem]);

        switch (gem)
        {
            case MineralRarity.AzuriteCore:
                weapon.maxSatiety += 20f;
                weapon.currentSatiety = Mathf.Min(weapon.maxSatiety, weapon.currentSatiety + 20f);
                break;
            case MineralRarity.CinnabarRuby:
                weapon.maxTemper += 25f;
                weapon.currentTemper = Mathf.Min(weapon.maxTemper, weapon.currentTemper + 25f);
                break;
            case MineralRarity.GildedTopaz:
                weapon.currentTemper = weapon.maxTemper;
                break;
            case MineralRarity.VoidAmethyst:
                weapon.fractureDebt = 0f;
                break;
            case MineralRarity.PrismaticHeart:
                weapon.maxTemper += 100f;
                weapon.maxSatiety += 100f;
                weapon.currentTemper = weapon.maxTemper;
                weapon.currentSatiety = weapon.maxSatiety;
                break;
        }

        Debug.Log($"[GEMOLOGY FORGE] Successfully socketed {gem} into {weapon.weaponName}!");
        return true;
    }
}
