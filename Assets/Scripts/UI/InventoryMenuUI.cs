using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ItemCategory
{
    All,
    Consumables,
    Weapons,
    Relics,
    Materials
}


[System.Serializable]
public class InventoryItemEntry
{
    public string itemId;
    public string displayName;
    public ItemCategory category;
    public ItemRarity rarity;
    public int quantity;
    public int maxStack = 99;
    public int goldValue = 25;
    [TextArea(2, 4)]
    public string description;
    public string statModifierText;
    public string iconTextureName;

    public Color GetRarityColor()
    {
        return rarity switch
        {
            ItemRarity.Legendary => new Color(1.0f, 0.82f, 0.20f),
            ItemRarity.Epic => new Color(0.75f, 0.35f, 1.0f),
            ItemRarity.Rare => new Color(0.25f, 0.85f, 1.0f),
            _ => new Color(0.85f, 0.85f, 0.90f)
        };
    }
}

/// <summary>
/// InventoryMenuUI manages the player's Messenger Bag / Inventory system:
/// - Category filtering (All, Consumables, Weapons, Relics, Materials)
/// - Visual grid of inventory slots with item icons, counts, and rarity frames
/// - Selected item inspector with lore, stat bonuses, and actions (USE, EQUIP, DISCARD)
/// - Gold and Astral Shard currency tracking
/// </summary>
public class InventoryMenuUI : MonoBehaviour
{
    private static InventoryMenuUI _instance;
    public static InventoryMenuUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<InventoryMenuUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Root & Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button closeButton;

    [Header("Currency Readouts")]
    [SerializeField] private TextMeshProUGUI goldCurrencyTMP;
    [SerializeField] private TextMeshProUGUI shardsCurrencyTMP;

    [Header("Category Filter Buttons")]
    [SerializeField] private Button tabAllBtn;
    [SerializeField] private Button tabConsumablesBtn;
    [SerializeField] private Button tabWeaponsBtn;
    [SerializeField] private Button tabRelicsBtn;
    [SerializeField] private Button tabMaterialsBtn;

    [Header("Item Grid Container")]
    [SerializeField] private Transform slotGridContainer;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Item Inspector Detail Panel")]
    [SerializeField] private Image detailIconImg;
    [SerializeField] private TextMeshProUGUI detailNameTMP;
    [SerializeField] private TextMeshProUGUI detailRarityCategoryTMP;
    [SerializeField] private TextMeshProUGUI detailDescriptionTMP;
    [SerializeField] private TextMeshProUGUI detailStatsTMP;
    [SerializeField] private Button useActionButton;
    [SerializeField] private TextMeshProUGUI useActionLabelTMP;
    [SerializeField] private Button discardActionButton;

    [Header("Wallet State")]
    [SerializeField] private int playerGold = 1450;
    [SerializeField] private int playerAstralShards = 85;

    [Header("Inventory Data")]
    [SerializeField] private List<InventoryItemEntry> items = new List<InventoryItemEntry>();

    private ItemCategory activeFilter = ItemCategory.All;
    private InventoryItemEntry selectedItem;
    private readonly List<GameObject> activeSlotObjects = new List<GameObject>();

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;
    public int PlayerGold => playerGold;
    public int PlayerAstralShards => playerAstralShards;
    public IReadOnlyList<InventoryItemEntry> Items
    {
        get
        {
            if (items.Count == 0) InitializeDefaultInventory();
            return items;
        }
    }

    public static event Action<InventoryItemEntry> OnItemUsed;
    public static event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        InitializeDefaultInventory();
    }

    private void Start()
    {
        if (overlayRoot == null) overlayRoot = gameObject;
        BindButtons();
        SetVisible(false);
    }

    private void Update()
    {
        // Desktop shortcut: 'I' toggles Inventory satchel
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        SetVisible(!IsOpen);
    }

    public void SetVisible(bool visible)
    {
        if (overlayRoot != null) overlayRoot.SetActive(visible);

        Transform parentTrans = transform.parent;
        if (parentTrans != null)
        {
            Transform hud = parentTrans.Find("MockupMasterHUD");
            if (hud != null) hud.gameObject.SetActive(!visible);

            Transform safeArea = parentTrans.Find("SafeAreaContainer");
            if (safeArea != null) safeArea.gameObject.SetActive(!visible);
        }
        else
        {
            GameObject hudGo = GameObject.Find("MockupMasterHUD");
            if (hudGo != null) hudGo.SetActive(!visible);
            GameObject safeGo = GameObject.Find("SafeAreaContainer");
            if (safeGo != null) safeGo.SetActive(!visible);
        }

        if (visible)
        {
            RefreshWalletUI();
            RefreshGrid();
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.9f);
            }
        }
        else
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.8f);
            }
        }
    }

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SetVisible(false));
        }

        if (tabAllBtn != null) tabAllBtn.onClick.AddListener(() => SetFilter(ItemCategory.All));
        if (tabConsumablesBtn != null) tabConsumablesBtn.onClick.AddListener(() => SetFilter(ItemCategory.Consumables));
        if (tabWeaponsBtn != null) tabWeaponsBtn.onClick.AddListener(() => SetFilter(ItemCategory.Weapons));
        if (tabRelicsBtn != null) tabRelicsBtn.onClick.AddListener(() => SetFilter(ItemCategory.Relics));
        if (tabMaterialsBtn != null) tabMaterialsBtn.onClick.AddListener(() => SetFilter(ItemCategory.Materials));

        if (useActionButton != null) useActionButton.onClick.AddListener(OnUseClicked);
        if (discardActionButton != null) discardActionButton.onClick.AddListener(OnDiscardClicked);
    }

    public void InitializeDefaultInventory()
    {
        if (items.Count > 0) return;

        items.Add(new InventoryItemEntry
        {
            itemId = "potion_crimson",
            displayName = "Crimson Draught",
            category = ItemCategory.Consumables,
            rarity = ItemRarity.Common,
            quantity = 5,
            goldValue = 50,
            description = "A standard Spire curative flask restoring vitality and physical stamina.",
            statModifierText = "+250 HP Restoration",
            iconTextureName = "Mockup_PotionRed.png"
        });

        items.Add(new InventoryItemEntry
        {
            itemId = "potion_amber",
            displayName = "Amber Mana Draught",
            category = ItemCategory.Consumables,
            rarity = ItemRarity.Common,
            quantity = 3,
            goldValue = 75,
            description = "Distilled ether offering a surge of mental clarity to fuel high-tier spells.",
            statModifierText = "+150 MP Restoration",
            iconTextureName = "Mockup_PotionAmber.png"
        });

        items.Add(new InventoryItemEntry
        {
            itemId = "potion_elixir",
            displayName = "Primordial Elixir",
            category = ItemCategory.Consumables,
            rarity = ItemRarity.Rare,
            quantity = 2,
            goldValue = 180,
            description = "Rare celestial extract healing wounds and fortifying soul ether simultaneously.",
            statModifierText = "+250 HP & +150 MP",
            iconTextureName = "Mockup_PotionElixir.png"
        });

        items.Add(new InventoryItemEntry
        {
            itemId = "relic_whetstone",
            displayName = "Whetstone of Eternity",
            category = ItemCategory.Relics,
            rarity = ItemRarity.Epic,
            quantity = 1,
            goldValue = 350,
            description = "Adamantine whetstone that permanently hones weapon edges and restores fracture debt.",
            statModifierText = "+25% Blade Critical Damage",
            iconTextureName = "Mockup_Whetstone.png"
        });

        items.Add(new InventoryItemEntry
        {
            itemId = "weapon_bow",
            displayName = "Elven Recurve Bow",
            category = ItemCategory.Weapons,
            rarity = ItemRarity.Rare,
            quantity = 1,
            goldValue = 600,
            description = "Flexible yew recurve bow capable of launching precision ballistic arrows.",
            statModifierText = "Unlocks Ranger Loadout & Scope ADS",
            iconTextureName = "Mockup_ScopeButton.png"
        });

        items.Add(new InventoryItemEntry
        {
            itemId = "weapon_staff",
            displayName = "Astral Archmage Staff",
            category = ItemCategory.Weapons,
            rarity = ItemRarity.Rare,
            quantity = 1,
            goldValue = 750,
            description = "Channeled hardwood staff crowned with a spinning astral mana core.",
            statModifierText = "Unlocks Mage Loadout & Arcane Bolts",
            iconTextureName = "Mockup_CastButton.png"
        });

        items.Add(new InventoryItemEntry
        {
            itemId = "mat_ruby",
            displayName = "Spire Ruby Shard",
            category = ItemCategory.Materials,
            rarity = ItemRarity.Common,
            quantity = 12,
            goldValue = 35,
            description = "Chiseled mineral shard extracted from subterranean dungeon walls.",
            statModifierText = "Refining Material for Alchemical Gear",
            iconTextureName = "Mockup_CarouselFrame.png"
        });
    }

    public void SetFilter(ItemCategory cat)
    {
        activeFilter = cat;
        RefreshGrid();
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
        }
    }

    public void RefreshWalletUI()
    {
        if (goldCurrencyTMP != null) goldCurrencyTMP.text = $"<color=#FFD700><b>{playerGold:N0} G</b></color>";
        if (shardsCurrencyTMP != null) shardsCurrencyTMP.text = $"<color=#40EBFF><b>{playerAstralShards} Shards</b></color>";
    }

    public void AddGold(int amount)
    {
        playerGold += amount;
        RefreshWalletUI();
        OnGoldChanged?.Invoke(playerGold);
    }

    public bool DeductGold(int amount)
    {
        if (playerGold >= amount)
        {
            playerGold -= amount;
            RefreshWalletUI();
            OnGoldChanged?.Invoke(playerGold);
            return true;
        }
        return false;
    }

    public void AddItem(InventoryItemEntry item)
    {
        var existing = items.Find(x => x.itemId == item.itemId);
        if (existing != null)
        {
            existing.quantity += item.quantity;
        }
        else
        {
            items.Add(item);
        }
        RefreshGrid();
    }

    public bool DeductItem(string itemId, int count = 1)
    {
        var existing = items.Find(x => x.itemId == itemId);
        if (existing != null && existing.quantity >= count)
        {
            existing.quantity -= count;
            if (existing.quantity <= 0)
            {
                items.Remove(existing);
            }
            RefreshGrid();
            return true;
        }
        return false;
    }

    public void RefreshGrid()
    {
        // Clear slots
        for (int i = 0; i < activeSlotObjects.Count; i++)
        {
            if (activeSlotObjects[i] != null) Destroy(activeSlotObjects[i]);
        }
        activeSlotObjects.Clear();

        if (slotGridContainer == null) return;

        List<InventoryItemEntry> filtered = items.FindAll(x => activeFilter == ItemCategory.All || x.category == activeFilter);

        for (int i = 0; i < filtered.Count; i++)
        {
            var item = filtered[i];
            GameObject slotObj = CreateSlotGameObject(item, i);
            activeSlotObjects.Add(slotObj);
        }

        if (filtered.Count > 0)
        {
            SelectItem(filtered[0]);
        }
        else
        {
            ClearInspector();
        }
    }

    private GameObject CreateSlotGameObject(InventoryItemEntry item, int index)
    {
        GameObject slotObj = new GameObject($"Slot_{index}_{item.itemId}", typeof(RectTransform), typeof(Image), typeof(Button));
        slotObj.transform.SetParent(slotGridContainer, false);

        RectTransform rt = slotObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 80f);

        Image slotImg = slotObj.GetComponent<Image>();
        Sprite slotSprite = LoadSprite(item.iconTextureName) ?? LoadSprite("Mockup_ItemSlot.png");
        if (slotSprite != null) slotImg.sprite = slotSprite;
        slotImg.color = item.GetRarityColor();

        Button btn = slotObj.GetComponent<Button>();
        btn.onClick.AddListener(() => SelectItem(item));

        // Count label badge
        GameObject badgeObj = new GameObject("CountBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeObj.transform.SetParent(slotObj.transform, false);
        RectTransform brt = badgeObj.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0f);
        brt.anchorMax = new Vector2(1f, 0.4f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        TextMeshProUGUI btmp = badgeObj.GetComponent<TextMeshProUGUI>();
        btmp.text = $"x{item.quantity}";
        btmp.fontSize = 12;
        btmp.alignment = TextAlignmentOptions.BottomRight;
        btmp.color = Color.white;

        return slotObj;
    }

    public void SelectItem(InventoryItemEntry item)
    {
        selectedItem = item;
        if (item == null)
        {
            ClearInspector();
            return;
        }

        if (detailNameTMP != null)
        {
            string hex = ColorUtility.ToHtmlStringRGB(item.GetRarityColor());
            detailNameTMP.text = $"<color=#{hex}><b>{item.displayName.ToUpper()}</b></color>";
        }

        if (detailRarityCategoryTMP != null)
        {
            detailRarityCategoryTMP.text = $"{item.rarity.ToString().ToUpper()} // {item.category.ToString().ToUpper()}";
        }

        if (detailDescriptionTMP != null)
        {
            detailDescriptionTMP.text = item.description;
        }

        if (detailStatsTMP != null)
        {
            detailStatsTMP.text = $"<color=#FFD700><b>EFFECT:</b></color> {item.statModifierText}\n<color=#A0A0A0>Value: {item.goldValue} G (x{item.quantity})</color>";
        }

        if (detailIconImg != null)
        {
            Sprite s = LoadSprite(item.iconTextureName);
            if (s != null) detailIconImg.sprite = s;
            detailIconImg.color = item.GetRarityColor();
        }

        if (useActionLabelTMP != null)
        {
            useActionLabelTMP.text = item.category switch
            {
                ItemCategory.Consumables => "USE POTION",
                ItemCategory.Weapons => "EQUIP WEAPON",
                _ => "INSPECT"
            };
        }

        if (useActionButton != null) useActionButton.interactable = true;
        if (discardActionButton != null) discardActionButton.interactable = true;
    }

    private void ClearInspector()
    {
        selectedItem = null;
        if (detailNameTMP != null) detailNameTMP.text = "<color=#808080>NO ITEM SELECTED</color>";
        if (detailRarityCategoryTMP != null) detailRarityCategoryTMP.text = "SELECT AN ITEM FROM INVENTORY";
        if (detailDescriptionTMP != null) detailDescriptionTMP.text = "";
        if (detailStatsTMP != null) detailStatsTMP.text = "";
        if (useActionButton != null) useActionButton.interactable = false;
        if (discardActionButton != null) discardActionButton.interactable = false;
    }

    private void OnUseClicked()
    {
        if (selectedItem == null) return;

        if (selectedItem.category == ItemCategory.Consumables)
        {
            // Execute potion use
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#30FF80>[USED ITEM]</color>\nConsumed {selectedItem.displayName}!");
            }
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
            }

            selectedItem.quantity--;
            if (selectedItem.quantity <= 0)
            {
                items.Remove(selectedItem);
            }
            RefreshGrid();
            OnItemUsed?.Invoke(selectedItem);
        }
        else if (selectedItem.category == ItemCategory.Weapons)
        {
            if (DevClassLoadoutManager.Instance != null)
            {
                if (selectedItem.itemId.Contains("bow")) DevClassLoadoutManager.Instance.SetClassLoadout(PlayerClassType.Ranger);
                else if (selectedItem.itemId.Contains("staff")) DevClassLoadoutManager.Instance.SetClassLoadout(PlayerClassType.Mage);
                else DevClassLoadoutManager.Instance.SetClassLoadout(PlayerClassType.Warrior);
            }
            SetVisible(false);
        }
    }

    private void OnDiscardClicked()
    {
        if (selectedItem == null) return;
        items.Remove(selectedItem);
        RefreshGrid();
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.6f);
        }
    }

    private Sprite LoadSprite(string textureName)
    {
#if UNITY_EDITOR
        string path = "Assets/Textures/UI/" + textureName;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }
}
