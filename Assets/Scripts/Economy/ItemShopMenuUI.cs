using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class ShopWareItem
{
    public string itemId;
    public string displayName;
    public ItemCategory category;
    public ItemRarity rarity;
    public int priceGold;
    public int stockQuantity = 99;
    [TextArea(2, 4)]
    public string description;
    public string statBonusText;
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
/// ItemShopMenuUI provides a full-featured Spire Item Shop:
/// - Gilded merchant canopy banner & greeting
/// - Merchant dialogue integration via PersonaDialogueUI
/// - Buy and Sell mode tabs
/// - Real-time wallet deductions and inventory additions
/// - Quantity selector with price calculations
/// </summary>
public class ItemShopMenuUI : MonoBehaviour
{
    private static ItemShopMenuUI _instance;
    public static ItemShopMenuUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<ItemShopMenuUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Root & Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button closeButton;

    [Header("Merchant Header & Dialogue Trigger")]
    [SerializeField] private TextMeshProUGUI merchantTitleTMP;
    [SerializeField] private TextMeshProUGUI walletCurrencyTMP;
    [SerializeField] private Button talkToMerchantButton;

    [Header("Shop Mode Tabs")]
    [SerializeField] private Button buyTabBtn;
    [SerializeField] private Button sellTabBtn;

    [Header("Wares Grid Container")]
    [SerializeField] private Transform waresGridContainer;
    [SerializeField] private GameObject wareSlotPrefab;

    [Header("Selected Ware Inspector & Checkout")]
    [SerializeField] private Image detailIconImg;
    [SerializeField] private TextMeshProUGUI detailTitleTMP;
    [SerializeField] private TextMeshProUGUI detailPriceTMP;
    [SerializeField] private TextMeshProUGUI detailLoreTMP;
    [SerializeField] private TextMeshProUGUI detailStatsTMP;
    [SerializeField] private Button decrementQtyBtn;
    [SerializeField] private Button incrementQtyBtn;
    [SerializeField] private TextMeshProUGUI quantityLabelTMP;
    [SerializeField] private Button transactionActionButton;
    [SerializeField] private TextMeshProUGUI transactionActionTMP;

    [Header("Merchant Catalog")]
    [SerializeField] private List<ShopWareItem> catalog = new List<ShopWareItem>();

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    private bool isBuyMode = true;
    private ShopWareItem selectedWare;
    private int selectedQuantity = 1;
    private readonly List<GameObject> activeWareObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        InitializeDefaultCatalog();
    }

    private void Start()
    {
        if (overlayRoot == null) overlayRoot = gameObject;
        BindButtonListeners();
        SetVisible(false);
    }

    private void Update()
    {
        // Desktop shortcut: 'P' or 'K' toggles shop
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
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
            isBuyMode = true;
            selectedQuantity = 1;
            RefreshWalletUI();
            RefreshWaresList();
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

    private void BindButtonListeners()
    {
        if (closeButton != null) closeButton.onClick.AddListener(() => SetVisible(false));
        if (buyTabBtn != null) buyTabBtn.onClick.AddListener(() => SetMode(true));
        if (sellTabBtn != null) sellTabBtn.onClick.AddListener(() => SetMode(false));
        if (talkToMerchantButton != null) talkToMerchantButton.onClick.AddListener(TriggerMerchantPersonaDialogue);

        if (decrementQtyBtn != null) decrementQtyBtn.onClick.AddListener(DecrementQuantity);
        if (incrementQtyBtn != null) incrementQtyBtn.onClick.AddListener(IncrementQuantity);
        if (transactionActionButton != null) transactionActionButton.onClick.AddListener(ExecuteTransaction);
    }

    public void InitializeDefaultCatalog()
    {
        if (catalog.Count > 0) return;

        catalog.Add(new ShopWareItem
        {
            itemId = "potion_crimson",
            displayName = "Crimson Draught",
            category = ItemCategory.Consumables,
            rarity = ItemRarity.Common,
            priceGold = 50,
            description = "Potent restorative tonic boiled from Crimson Spire lichen.",
            statBonusText = "+250 HP Restoration",
            iconTextureName = "Mockup_PotionRed.png"
        });

        catalog.Add(new ShopWareItem
        {
            itemId = "potion_amber",
            displayName = "Amber Mana Draught",
            category = ItemCategory.Consumables,
            rarity = ItemRarity.Common,
            priceGold = 75,
            description = "Ether-infused nectar replenishing magical reserves instantly.",
            statBonusText = "+150 MP Restoration",
            iconTextureName = "Mockup_PotionAmber.png"
        });

        catalog.Add(new ShopWareItem
        {
            itemId = "potion_elixir",
            displayName = "Primordial Elixir",
            category = ItemCategory.Consumables,
            rarity = ItemRarity.Rare,
            priceGold = 180,
            description = "Rare elixir combining essence of blood and stellar starlight.",
            statBonusText = "+250 HP & +150 MP",
            iconTextureName = "Mockup_PotionElixir.png"
        });

        catalog.Add(new ShopWareItem
        {
            itemId = "relic_whetstone",
            displayName = "Whetstone of Eternity",
            category = ItemCategory.Relics,
            rarity = ItemRarity.Epic,
            priceGold = 350,
            description = "Adamantine hone that permanently sharpens blades and restores fracture debt.",
            statBonusText = "+25% Blade Critical Damage",
            iconTextureName = "Mockup_Whetstone.png"
        });

        catalog.Add(new ShopWareItem
        {
            itemId = "weapon_bow",
            displayName = "Elven Recurve Bow",
            category = ItemCategory.Weapons,
            rarity = ItemRarity.Rare,
            priceGold = 600,
            description = "Finely strung recurve bow enabling Over-The-Shoulder Scope sniper aim.",
            statBonusText = "Enables Ranger Class & Scope ADS",
            iconTextureName = "Mockup_ScopeButton.png"
        });

        catalog.Add(new ShopWareItem
        {
            itemId = "weapon_staff",
            displayName = "Astral Archmage Staff",
            category = ItemCategory.Weapons,
            rarity = ItemRarity.Rare,
            priceGold = 750,
            description = "Living staff channeled to launch concentrated fire and singularity runes.",
            statBonusText = "Enables Mage Class & Projectile Bolts",
            iconTextureName = "Mockup_CastButton.png"
        });

        catalog.Add(new ShopWareItem
        {
            itemId = "relic_pendant",
            displayName = "Astral Ward Pendant",
            category = ItemCategory.Relics,
            rarity = ItemRarity.Epic,
            priceGold = 420,
            description = "Ancient silver amulet that absorbs 15% of all elemental incoming harm.",
            statBonusText = "+15% Damage Reduction Aura",
            iconTextureName = "Mockup_MinimapRadar.png"
        });
    }

    public void SetMode(bool buy)
    {
        isBuyMode = buy;
        selectedQuantity = 1;
        RefreshWaresList();
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
        }
    }

    public void RefreshWalletUI()
    {
        int gold = InventoryMenuUI.Instance != null ? InventoryMenuUI.Instance.PlayerGold : 1450;
        int shards = InventoryMenuUI.Instance != null ? InventoryMenuUI.Instance.PlayerAstralShards : 85;
        if (walletCurrencyTMP != null)
        {
            walletCurrencyTMP.text = $"<color=#FFD700><b>{gold:N0} G</b></color> | <color=#40EBFF><b>{shards} Shards</b></color>";
        }
    }

    public void RefreshWaresList()
    {
        if (catalog.Count == 0) InitializeDefaultCatalog();
        for (int i = 0; i < activeWareObjects.Count; i++)
        {
            if (activeWareObjects[i] != null) Destroy(activeWareObjects[i]);
        }
        activeWareObjects.Clear();

        if (waresGridContainer == null) return;

        if (isBuyMode)
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                var ware = catalog[i];
                GameObject obj = CreateWareSlot(ware, i);
                activeWareObjects.Add(obj);
            }
            if (catalog.Count > 0) SelectWare(catalog[0]);
        }
        else
        {
            // Sell Mode: show player inventory items
            if (InventoryMenuUI.Instance != null)
            {
                var pItems = InventoryMenuUI.Instance.Items;
                for (int i = 0; i < pItems.Count; i++)
                {
                    var pItem = pItems[i];
                    ShopWareItem converted = new ShopWareItem
                    {
                        itemId = pItem.itemId,
                        displayName = pItem.displayName,
                        category = pItem.category,
                        rarity = pItem.rarity,
                        priceGold = Mathf.RoundToInt(pItem.goldValue * 0.7f), // 70% sell value
                        stockQuantity = pItem.quantity,
                        description = pItem.description,
                        statBonusText = pItem.statModifierText,
                        iconTextureName = pItem.iconTextureName
                    };
                    GameObject obj = CreateWareSlot(converted, i);
                    activeWareObjects.Add(obj);
                }
                if (pItems.Count > 0)
                {
                    var first = pItems[0];
                    SelectWare(new ShopWareItem
                    {
                        itemId = first.itemId,
                        displayName = first.displayName,
                        category = first.category,
                        rarity = first.rarity,
                        priceGold = Mathf.RoundToInt(first.goldValue * 0.7f),
                        stockQuantity = first.quantity,
                        description = first.description,
                        statBonusText = first.statModifierText,
                        iconTextureName = first.iconTextureName
                    });
                }
            }
        }
    }

    private GameObject CreateWareSlot(ShopWareItem ware, int index)
    {
        GameObject slotObj = new GameObject($"WareSlot_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        slotObj.transform.SetParent(waresGridContainer, false);

        RectTransform rt = slotObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 80f);

        Image slotImg = slotObj.GetComponent<Image>();
        Sprite s = LoadSprite(ware.iconTextureName) ?? LoadSprite("Mockup_ItemSlot.png");
        if (s != null) slotImg.sprite = s;
        slotImg.color = ware.GetRarityColor();

        Button btn = slotObj.GetComponent<Button>();
        btn.onClick.AddListener(() => SelectWare(ware));

        // Price pill badge
        GameObject priceObj = new GameObject("PriceBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        priceObj.transform.SetParent(slotObj.transform, false);
        RectTransform prt = priceObj.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(1f, 0.35f);
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        TextMeshProUGUI ptmp = priceObj.GetComponent<TextMeshProUGUI>();
        int unitPrice = GetEffectivePrice(ware);
        ptmp.text = $"{unitPrice}G";
        ptmp.fontSize = 11;
        ptmp.alignment = TextAlignmentOptions.Center;
        ptmp.color = new Color(1.0f, 0.90f, 0.35f);

        return slotObj;
    }

    public int GetEffectivePrice(ShopWareItem ware)
    {
        if (ware == null) return 0;
        float mult = 1.0f;
        if (BattleSummoner.Social.TomodachiSocialLinkManager.Instance != null)
        {
            mult = isBuyMode 
                ? BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetShopBuyPriceMultiplier()
                : BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetShopSellPriceMultiplier();
        }
        return Mathf.Max(1, Mathf.RoundToInt(ware.priceGold * mult));
    }

    public void SelectWare(ShopWareItem ware)
    {
        selectedWare = ware;
        selectedQuantity = 1;
        UpdateDetailPanel();
    }

    private void IncrementQuantity()
    {
        if (selectedWare == null) return;
        selectedQuantity++;
        UpdateDetailPanel();
    }

    private void DecrementQuantity()
    {
        if (selectedWare == null) return;
        if (selectedQuantity > 1) selectedQuantity--;
        UpdateDetailPanel();
    }

    private void UpdateDetailPanel()
    {
        if (selectedWare == null) return;

        if (detailTitleTMP != null)
        {
            string hex = ColorUtility.ToHtmlStringRGB(selectedWare.GetRarityColor());
            detailTitleTMP.text = $"<color=#{hex}><b>{selectedWare.displayName.ToUpper()}</b></color>";
        }

        int unitPrice = GetEffectivePrice(selectedWare);
        int totalCost = unitPrice * selectedQuantity;
        if (detailPriceTMP != null)
        {
            float mult = BattleSummoner.Social.TomodachiSocialLinkManager.Instance != null 
                ? (isBuyMode ? BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetShopBuyPriceMultiplier() : BattleSummoner.Social.TomodachiSocialLinkManager.Instance.GetShopSellPriceMultiplier())
                : 1f;
            string linkTag = "";
            if (mult < 0.99f) linkTag = $" <size=85%><color=#30FF80>({Mathf.RoundToInt((1f - mult) * 100f)}% Link Discount!)</color></size>";
            else if (mult > 1.01f) linkTag = $" <size=85%><color=#FF3040>(+{Mathf.RoundToInt((mult - 1f) * 100f)}% Link Price Gouging!)</color></size>";

            detailPriceTMP.text = isBuyMode 
                ? $"<color=#FFD700><b>PRICE: {totalCost:N0} Gold</b></color> ({unitPrice} G each){linkTag}"
                : $"<color=#FFD700><b>PAYOUT: {totalCost:N0} Gold</b></color> ({unitPrice} G each){linkTag}";
        }

        if (detailLoreTMP != null) detailLoreTMP.text = selectedWare.description;
        if (detailStatsTMP != null) detailStatsTMP.text = $"<color=#30FF80><b>EFFECT:</b></color> {selectedWare.statBonusText}";

        if (quantityLabelTMP != null) quantityLabelTMP.text = $"{selectedQuantity}";

        if (detailIconImg != null)
        {
            Sprite s = LoadSprite(selectedWare.iconTextureName);
            if (s != null) detailIconImg.sprite = s;
            detailIconImg.color = selectedWare.GetRarityColor();
        }

        if (transactionActionTMP != null)
        {
            transactionActionTMP.text = isBuyMode ? $"[ PURCHASE ( {totalCost:N0} G ) ]" : $"[ SELL ( +{totalCost:N0} G ) ]";
        }
    }

    private void ExecuteTransaction()
    {
        if (selectedWare == null) return;
        int unitPrice = GetEffectivePrice(selectedWare);
        int total = unitPrice * selectedQuantity;

        if (isBuyMode)
        {
            if (InventoryMenuUI.Instance != null)
            {
                if (InventoryMenuUI.Instance.DeductGold(total))
                {
                    InventoryMenuUI.Instance.AddItem(new InventoryItemEntry
                    {
                        itemId = selectedWare.itemId,
                        displayName = selectedWare.displayName,
                        category = selectedWare.category,
                        rarity = selectedWare.rarity,
                        quantity = selectedQuantity,
                        goldValue = selectedWare.priceGold,
                        description = selectedWare.description,
                        statModifierText = selectedWare.statBonusText,
                        iconTextureName = selectedWare.iconTextureName
                    });

                    if (DevClassLoadoutManager.Instance != null)
                    {
                        DevClassLoadoutManager.Instance.ShowToast($"<color=#FFD700>[PURCHASED]</color>\nAcquired {selectedQuantity}x {selectedWare.displayName}!");
                    }
                    if (BattleSummonerAudioMaster.Instance != null)
                    {
                        BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
                    }
                    RefreshWalletUI();
                }
                else
                {
                    if (DevClassLoadoutManager.Instance != null)
                    {
                        DevClassLoadoutManager.Instance.ShowToast("<color=#FF6060>[INSUFFICIENT GOLD]</color>\nYou lack the coins for this purchase!");
                    }
                }
            }
        }
        else
        {
            // Sell
            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.AddGold(total);
                if (DevClassLoadoutManager.Instance != null)
                {
                    DevClassLoadoutManager.Instance.ShowToast($"<color=#30FF80>[SOLD]</color>\nSold {selectedQuantity}x {selectedWare.displayName} for +{total:N0} G!");
                }
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
                }
                RefreshWalletUI();
                RefreshWaresList();
            }
        }
    }

    public void TriggerMerchantPersonaDialogue()
    {
        if (PersonaDialogueUI.Instance == null) return;

        SetVisible(false);

        Sprite merchantSprite = LoadSprite("Mockup_PersonaPortrait_Merchant.png");
        List<PersonaDialogueLine> lines = new List<PersonaDialogueLine>
        {
            new PersonaDialogueLine
            {
                speakerName = "Balthazar",
                speakerTitle = "Shadow Broker of the Spire",
                portraitSprite = merchantSprite,
                text = "Welcome, traveler... Few survive the ascent to this floor with their purses intact."
            },
            new PersonaDialogueLine
            {
                speakerName = "Balthazar",
                speakerTitle = "Shadow Broker of the Spire",
                portraitSprite = merchantSprite,
                text = "I trade only in curiosities that guarantee survival. Potions brewed from dragon-blood, bows blessed by starlight, and forgotten whetstones."
            }
        };

        PersonaDialogueUI.Instance.StartDialogueSequence(lines, () =>
        {
            // Offer Persona-style choices
            PersonaChoiceOption[] options = new PersonaChoiceOption[]
            {
                new PersonaChoiceOption("Let me browse your rarest wares.", () => SetVisible(true)),
                new PersonaChoiceOption("What dangers lie ahead on this floor?", () =>
                {
                    PersonaDialogueUI.Instance.ShowSimpleDialogue("Balthazar", "Shadow Broker", merchantSprite, new string[]
                    {
                        "The Spire's automated monoliths slumber no longer.",
                        "Equip your bow or staff and utilize Scope ADS to strike from range before their blades pierce your guard!"
                    }, () => SetVisible(true));
                }),
                new PersonaChoiceOption("I'll take my leave for now.", null)
            };
            PersonaDialogueUI.Instance.ShowChoices(options);
        });
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
