using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Hobbies;

/// <summary>
/// QuickItemCarouselController manages the combat usable item carousel positioned
/// right above the controls in the bottom middle of the screen.
/// Features:
/// - Left and Right arrow buttons to cycle through usable items during combat.
/// - Prominent center slot displaying the active item icon, name, and quantity count badge.
/// - Tapping the center slot consumes the active item (healing, mana, buffs) with audio/visual feedback.
/// - Keyboard shortcuts: Q (previous), E (next), R / F (use item).
/// </summary>
public class QuickItemCarouselController : MonoBehaviour
{
    private static QuickItemCarouselController _instance;
    public static QuickItemCarouselController Instance
    {
        get
        {
            if (_instance == null) _instance = UnityEngine.Object.FindAnyObjectByType<QuickItemCarouselController>();
            return _instance;
        }
    }

    /// <summary>
    /// Ensures QuickItemCarouselController exists in the scene and is positioned directly above the movement controls.
    /// </summary>
    public static QuickItemCarouselController EnsureInScene(Transform parentTransform = null)
    {
        if (_instance != null)
        {
            _instance.AlignAboveMovementControls();
            return _instance;
        }

        var existing = UnityEngine.Object.FindAnyObjectByType<QuickItemCarouselController>();
        if (existing != null)
        {
            _instance = existing;
            _instance.AlignAboveMovementControls();
            return _instance;
        }

        if (parentTransform == null)
        {
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) parentTransform = canvas.transform;
            else
            {
                var canvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                parentTransform = canvasGo.transform;
            }
        }

        GameObject carouselRoot = new GameObject("QuickItemCarousel", typeof(RectTransform), typeof(Image));
        carouselRoot.transform.SetParent(parentTransform, false);
        var controller = carouselRoot.AddComponent<QuickItemCarouselController>();
        controller.BuildProceduralHierarchy();
        _instance = controller;
        return controller;
    }

    public void AlignAboveMovementControls()
    {
        var stick = UnityEngine.Object.FindAnyObjectByType<FloatingVirtualStick>();
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        if (stick != null)
        {
            var stickRt = stick.GetComponent<RectTransform>();
            float bottomY = Mathf.Max(0.22f, stickRt.anchorMax.y + 0.01f);
            float topY = bottomY + 0.08f;
            rt.anchorMin = new Vector2(stickRt.anchorMin.x, bottomY);
            rt.anchorMax = new Vector2(stickRt.anchorMax.x, topY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = new Vector2(0.04f, 0.23f);
            rt.anchorMax = new Vector2(0.38f, 0.31f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    private void BuildProceduralHierarchy()
    {
        AlignAboveMovementControls();

        Image bg = GetComponent<Image>();
        bg.color = new Color(0.08f, 0.10f, 0.16f, 0.88f);

        // Prev Arrow Button (<)
        GameObject prevObj = new GameObject("ArrowLeftButton", typeof(RectTransform), typeof(Image), typeof(Button));
        prevObj.transform.SetParent(transform, false);
        RectTransform prevRt = prevObj.GetComponent<RectTransform>();
        prevRt.anchorMin = new Vector2(0.02f, 0.12f);
        prevRt.anchorMax = new Vector2(0.18f, 0.88f);
        prevRt.offsetMin = Vector2.zero;
        prevRt.offsetMax = Vector2.zero;
        prevArrowButton = prevObj.GetComponent<Button>();
        Image prevImg = prevObj.GetComponent<Image>();
        prevImg.color = new Color(0.20f, 0.25f, 0.35f, 0.85f);
        GameObject prevTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        prevTxtObj.transform.SetParent(prevObj.transform, false);
        var prevTxt = prevTxtObj.GetComponent<TextMeshProUGUI>();
        prevTxt.rectTransform.anchorMin = Vector2.zero;
        prevTxt.rectTransform.anchorMax = Vector2.one;
        prevTxt.rectTransform.offsetMin = Vector2.zero;
        prevTxt.rectTransform.offsetMax = Vector2.zero;
        prevTxt.text = "◄";
        prevTxt.fontSize = 14f;
        prevTxt.alignment = TextAlignmentOptions.Center;
        prevTxt.color = Color.white;

        // Next Arrow Button (>)
        GameObject nextObj = new GameObject("ArrowRightButton", typeof(RectTransform), typeof(Image), typeof(Button));
        nextObj.transform.SetParent(transform, false);
        RectTransform nextRt = nextObj.GetComponent<RectTransform>();
        nextRt.anchorMin = new Vector2(0.82f, 0.12f);
        nextRt.anchorMax = new Vector2(0.98f, 0.88f);
        nextRt.offsetMin = Vector2.zero;
        nextRt.offsetMax = Vector2.zero;
        nextArrowButton = nextObj.GetComponent<Button>();
        Image nextImg = nextObj.GetComponent<Image>();
        nextImg.color = new Color(0.20f, 0.25f, 0.35f, 0.85f);
        GameObject nextTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        nextTxtObj.transform.SetParent(nextObj.transform, false);
        var nextTxt = nextTxtObj.GetComponent<TextMeshProUGUI>();
        nextTxt.rectTransform.anchorMin = Vector2.zero;
        nextTxt.rectTransform.anchorMax = Vector2.one;
        nextTxt.rectTransform.offsetMin = Vector2.zero;
        nextTxt.rectTransform.offsetMax = Vector2.zero;
        nextTxt.text = "►";
        nextTxt.fontSize = 14f;
        nextTxt.alignment = TextAlignmentOptions.Center;
        nextTxt.color = Color.white;

        // Active Item Slot Button (Center)
        GameObject itemSlotObj = new GameObject("ActiveItemButton", typeof(RectTransform), typeof(Image), typeof(Button));
        itemSlotObj.transform.SetParent(transform, false);
        RectTransform itemSlotRt = itemSlotObj.GetComponent<RectTransform>();
        itemSlotRt.anchorMin = new Vector2(0.20f, 0.05f);
        itemSlotRt.anchorMax = new Vector2(0.80f, 0.95f);
        itemSlotRt.offsetMin = Vector2.zero;
        itemSlotRt.offsetMax = Vector2.zero;
        activeItemButton = itemSlotObj.GetComponent<Button>();
        Image slotImg = itemSlotObj.GetComponent<Image>();
        slotImg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);

        // Item Icon Image
        GameObject iconObj = new GameObject("ActiveItemIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(itemSlotObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.08f, 0.20f);
        iconRt.anchorMax = new Vector2(0.45f, 0.92f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        activeItemIcon = iconObj.GetComponent<Image>();
        activeItemIcon.preserveAspect = true;

        // Item Count Badge (x5)
        GameObject countObj = new GameObject("ItemCountText", typeof(RectTransform), typeof(TextMeshProUGUI));
        countObj.transform.SetParent(itemSlotObj.transform, false);
        itemCountTMP = countObj.GetComponent<TextMeshProUGUI>();
        itemCountTMP.rectTransform.anchorMin = new Vector2(0.48f, 0.45f);
        itemCountTMP.rectTransform.anchorMax = new Vector2(0.96f, 0.95f);
        itemCountTMP.rectTransform.offsetMin = Vector2.zero;
        itemCountTMP.rectTransform.offsetMax = Vector2.zero;
        itemCountTMP.fontSize = 12f;
        itemCountTMP.alignment = TextAlignmentOptions.MidlineRight;
        itemCountTMP.color = new Color(1f, 0.92f, 0.45f);

        // Item Name Label
        GameObject nameObj = new GameObject("ItemNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(itemSlotObj.transform, false);
        itemNameTMP = nameObj.GetComponent<TextMeshProUGUI>();
        itemNameTMP.rectTransform.anchorMin = new Vector2(0.04f, 0.02f);
        itemNameTMP.rectTransform.anchorMax = new Vector2(0.96f, 0.42f);
        itemNameTMP.rectTransform.offsetMin = Vector2.zero;
        itemNameTMP.rectTransform.offsetMax = Vector2.zero;
        itemNameTMP.fontSize = 10f;
        itemNameTMP.alignment = TextAlignmentOptions.Center;
        itemNameTMP.color = new Color(0.95f, 0.85f, 0.45f);

        InitializeDefaultInventory();
        BindUIEvents();
        RefreshDisplay();
    }

    [System.Serializable]
    public class UsableCombatItem
    {
        public string id;
        public string displayName;
        public string effectDescription;
        public string spriteFilename;
        public int count;
        public Color themeColor = Color.white;
        public CookedMealData cookedMealData;
    }

    [Header("UI References")]
    [SerializeField] private Button prevArrowButton;
    [SerializeField] private Button nextArrowButton;
    [SerializeField] private Button activeItemButton;
    [SerializeField] private Image activeItemIcon;
    [SerializeField] private TextMeshProUGUI itemCountTMP;
    [SerializeField] private TextMeshProUGUI itemNameTMP;
    [SerializeField] private RectTransform carouselContainer;

    [Header("Inventory Data")]
    [SerializeField] private List<UsableCombatItem> items = new List<UsableCombatItem>();
    [SerializeField] private int currentIndex = 0;

    [Header("Animation")]
    [SerializeField] private float punchScale = 1.25f;
    private Coroutine punchCoroutine;

    public int CurrentIndex => currentIndex;
    public UsableCombatItem CurrentItem => (items != null && items.Count > 0) ? items[currentIndex] : null;
    public IReadOnlyList<UsableCombatItem> Items => items;

    public event Action<UsableCombatItem> OnItemUsed;
    public event Action<int, UsableCombatItem> OnItemChanged;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        InitializeDefaultInventory();
    }

    private void Start()
    {
        BindUIEvents();
        RefreshDisplay();
    }

    public void InitializeDefaultInventory()
    {
        if (items != null && items.Count > 0)
        {
            if (!items.Exists(it => it.id == "item_whetstone"))
            {
                items.Insert(1, new UsableCombatItem
                {
                    id = "item_whetstone",
                    displayName = "Whetstone Polish",
                    effectDescription = "Hones Blade (+25% Temper / Slash DMG)",
                    spriteFilename = "Mockup_Whetstone.png",
                    count = 5,
                    themeColor = new Color(0.95f, 0.82f, 0.45f)
                });
            }
            if (!items.Exists(it => it.id == "meal_campfire_stew"))
            {
                items.Insert(2, new UsableCombatItem
                {
                    id = "meal_campfire_stew",
                    displayName = "Campfire Stew",
                    effectDescription = "Restores +220 HP & +12 MP/s (20s)",
                    spriteFilename = "Mockup_PotionAmber.png",
                    count = 3,
                    themeColor = new Color(1.0f, 0.65f, 0.20f),
                    cookedMealData = CookingMamaMinigame.CreatePresetMeal("meal_campfire_stew", "Campfire Stew", 220f, 12f, 20f)
                });
            }
            return;
        }

        items = new List<UsableCombatItem>
        {
            new UsableCombatItem
            {
                id = "tool_fishing_rod",
                displayName = "Fishing Rod",
                effectDescription = "Cast & reel fish in water (F key)",
                spriteFilename = "Mockup_PotionBlue.png",
                count = 1,
                themeColor = new Color(0.20f, 0.75f, 0.95f)
            },
            new UsableCombatItem
            {
                id = "tool_bug_net",
                displayName = "Bug Net",
                effectDescription = "Hold to stalk, release to swoop (G key)",
                spriteFilename = "Mockup_Whetstone.png",
                count = 1,
                themeColor = new Color(0.35f, 0.85f, 0.40f)
            },
            new UsableCombatItem
            {
                id = "tool_watering_can",
                displayName = "Watering Can",
                effectDescription = "Hydrate crops & flowers (H key)",
                spriteFilename = "Mockup_PotionAmber.png",
                count = 1,
                themeColor = new Color(0.40f, 0.65f, 0.85f)
            },
            new UsableCombatItem
            {
                id = "item_rose_seeds",
                displayName = "Rose Seeds",
                effectDescription = "Plant in soil garden beds",
                spriteFilename = "Mockup_PotionRed.png",
                count = 8,
                themeColor = new Color(0.95f, 0.30f, 0.35f)
            },
            new UsableCombatItem
            {
                id = "potion_hp",
                displayName = "Crimson Draught",
                effectDescription = "Restores +250 Health",
                spriteFilename = "Mockup_PotionRed.png",
                count = 5,
                themeColor = new Color(0.95f, 0.25f, 0.30f)
            },
            new UsableCombatItem
            {
                id = "item_whetstone",
                displayName = "Whetstone Polish",
                effectDescription = "Hones Blade (+25% Temper / Slash DMG)",
                spriteFilename = "Mockup_Whetstone.png",
                count = 5,
                themeColor = new Color(0.95f, 0.82f, 0.45f)
            },
            new UsableCombatItem
            {
                id = "potion_amber",
                displayName = "Amber Tonic",
                effectDescription = "Restores +150 Mana",
                spriteFilename = "Mockup_PotionAmber.png",
                count = 4,
                themeColor = new Color(1.0f, 0.75f, 0.20f)
            },
            new UsableCombatItem
            {
                id = "potion_mp",
                displayName = "Cerulean Philter",
                effectDescription = "Restores +280 Mana",
                spriteFilename = "Mockup_PotionBlue.png",
                count = 3,
                themeColor = new Color(0.20f, 0.70f, 0.95f)
            },
            new UsableCombatItem
            {
                id = "potion_elixir",
                displayName = "Elixir of Life",
                effectDescription = "Full Recovery (+250 HP, +150 MP)",
                spriteFilename = "Mockup_PotionElixir.png",
                count = 2,
                themeColor = new Color(0.85f, 0.35f, 0.95f)
            },
            new UsableCombatItem
            {
                id = "meal_campfire_stew",
                displayName = "Campfire Stew",
                effectDescription = "Restores +220 HP & +12 MP/s (20s)",
                spriteFilename = "Mockup_PotionAmber.png",
                count = 3,
                themeColor = new Color(1.0f, 0.65f, 0.20f),
                cookedMealData = CookingMamaMinigame.CreatePresetMeal("meal_campfire_stew", "Campfire Stew", 220f, 12f, 20f)
            }
        };
    }

    /// <summary>
    /// Adds a CookedMealData item to the quick-access belt carousel.
    /// </summary>
    public void AddCookedMeal(CookedMealData meal, int count = 1)
    {
        if (meal == null) return;
        InitializeDefaultInventory();

        var existing = items.Find(it => it.id == meal.mealId);
        if (existing != null)
        {
            existing.count += count;
            existing.cookedMealData = meal;
            existing.displayName = meal.mealName;
            existing.effectDescription = $"+{meal.totalHealthRestore:F0} HP, +{meal.manaRegenPerSecond:F1} MP/s ({meal.buffDurationSeconds:F0}s)";
        }
        else
        {
            items.Add(new UsableCombatItem
            {
                id = meal.mealId,
                displayName = meal.mealName,
                effectDescription = $"+{meal.totalHealthRestore:F0} HP, +{meal.manaRegenPerSecond:F1} MP/s ({meal.buffDurationSeconds:F0}s)",
                spriteFilename = "Mockup_PotionAmber.png",
                count = count,
                themeColor = meal.themeColor,
                cookedMealData = meal
            });
        }
        RefreshDisplay();
    }

    public void BindReferences(
        Button prevBtn,
        Button nextBtn,
        Button itemBtn,
        Image iconImg,
        TextMeshProUGUI countText,
        TextMeshProUGUI nameText,
        RectTransform container)
    {
        prevArrowButton = prevBtn;
        nextArrowButton = nextBtn;
        activeItemButton = itemBtn;
        activeItemIcon = iconImg;
        itemCountTMP = countText;
        itemNameTMP = nameText;
        carouselContainer = container;

        InitializeDefaultInventory();
        BindUIEvents();
        RefreshDisplay();
    }

    private void BindUIEvents()
    {
        if (prevArrowButton != null)
        {
            prevArrowButton.onClick.RemoveListener(CyclePrevious);
            prevArrowButton.onClick.AddListener(CyclePrevious);
        }
        if (nextArrowButton != null)
        {
            nextArrowButton.onClick.RemoveListener(CycleNext);
            nextArrowButton.onClick.AddListener(CycleNext);
        }
        if (activeItemButton != null)
        {
            activeItemButton.onClick.RemoveListener(UseCurrentItem);
            activeItemButton.onClick.AddListener(UseCurrentItem);
        }
    }

    private void Update()
    {
        HandleKeyboardShortcuts();
    }

    private void HandleKeyboardShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftBracket))
        {
            CyclePrevious();
        }
        else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.RightBracket))
        {
            CycleNext();
        }
        else if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F))
        {
            UseCurrentItem();
        }
    }

    /// <summary>
    /// Cycles to the previous usable combat item in the carousel.
    /// </summary>
    public void CyclePrevious()
    {
        if (items == null || items.Count == 0) return;

        currentIndex--;
        if (currentIndex < 0) currentIndex = items.Count - 1;

        PlayCycleAudio();
        TriggerPunchAnimation(-1);
        RefreshDisplay();
        SyncEquippedTool(CurrentItem);
        OnItemChanged?.Invoke(currentIndex, CurrentItem);
    }

    /// <summary>
    /// Cycles to the next usable combat item in the carousel.
    /// </summary>
    public void CycleNext()
    {
        if (items == null || items.Count == 0) return;

        currentIndex = (currentIndex + 1) % items.Count;

        PlayCycleAudio();
        TriggerPunchAnimation(1);
        RefreshDisplay();
        SyncEquippedTool(CurrentItem);
        OnItemChanged?.Invoke(currentIndex, CurrentItem);
    }

    private void SyncEquippedTool(UsableCombatItem item)
    {
        if (item == null) return;
        if (BattleSummoner.World.OverworldLifeSimDirector.Instance != null)
        {
            int toolIdx = item.id switch
            {
                "tool_fishing_rod" => 0,
                "tool_bug_net" => 1,
                "tool_watering_can" => 2,
                _ => -1
            };
            BattleSummoner.World.OverworldLifeSimDirector.Instance.SelectTool(toolIdx);
        }
    }

    /// <summary>
    /// Directly selects an item in the carousel by ID.
    /// </summary>
    public void SelectItemById(string id)
    {
        if (items == null || items.Count == 0) return;
        int idx = items.FindIndex(it => it.id == id);
        if (idx >= 0)
        {
            currentIndex = idx;
            RefreshDisplay();
            SyncEquippedTool(CurrentItem);
            OnItemChanged?.Invoke(currentIndex, CurrentItem);
        }
    }

    /// <summary>
    /// Consumes the currently selected combat item, applies healing/mana/buffs,
    /// decrements item count, and triggers audio/visual feedback.
    /// </summary>
    public void UseCurrentItem()
    {
        var item = CurrentItem;
        if (item == null) return;

        if (item.count <= 0)
        {
            Debug.Log($"⚠️ [QUICK ITEM] Out of '{item.displayName}'!");
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#FF4444>[DEPLETED]</color>\nNo {item.displayName} left in pouch!");
            }
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("glitch", 0.6f);
            }
            return;
        }

        // Apply item effect to PlayerProgression / WeaponTemper / Life Sim Tools
        var progression = UnityEngine.Object.FindAnyObjectByType<PlayerProgression>();
        var hero = GameObject.Find("Player_Hero");

        // Check if item is a cooked meal from CookingMamaMinigame or belt carousel
        if (item.cookedMealData != null && progression != null)
        {
            progression.ConsumeCookedMeal(item.cookedMealData);
        }
        else if (item.id.StartsWith("meal_") && progression != null)
        {
            var fallbackMeal = CookingMamaMinigame.CreatePresetMeal(item.id, item.displayName, 220f, 12f, 20f);
            progression.ConsumeCookedMeal(fallbackMeal);
        }
        else
        {
            switch (item.id)
            {
                case "tool_fishing_rod":
                    var rod = hero?.GetComponentInChildren<BattleSummoner.Minigames.Fishing.FishingRodTool>();
                    if (rod != null)
                    {
                        rod.SetEquipped(true);
                        rod.UseFishingRod();
                    }
                    return; // Do not decrement tool count

                case "tool_bug_net":
                    var net = hero?.GetComponentInChildren<BattleSummoner.Minigames.Insects.BugNetTool>();
                    if (net != null)
                    {
                        net.SetEquipped(true);
                        net.SwingNet();
                    }
                    return; // Do not decrement tool count

                case "tool_watering_can":
                    var can = hero?.GetComponentInChildren<BattleSummoner.Minigames.Gardening.WateringCanTool>();
                    if (can != null)
                    {
                        can.SetEquipped(true);
                        can.UseWateringCan();
                    }
                    return; // Do not decrement tool count

                case "item_rose_seeds":
                    // Plant seeds in closest GardenPlot
                    var plots = UnityEngine.Object.FindObjectsByType<BattleSummoner.Minigames.Gardening.GardenPlot>(FindObjectsSortMode.None);
                    BattleSummoner.Minigames.Gardening.GardenPlot nearestPlot = null;
                    float nearestDist = 3.5f;
                    if (hero != null)
                    {
                        foreach (var p in plots)
                        {
                            float d = Vector3.Distance(hero.transform.position, p.transform.position);
                            if (d < nearestDist && p.stage == BattleSummoner.Minigames.Gardening.GrowthStage.EmptyTilled)
                            {
                                nearestDist = d;
                                nearestPlot = p;
                            }
                        }
                    }
                    if (nearestPlot != null)
                    {
                        nearestPlot.PlantSeed("red_rose");
                    }
                    else
                    {
                        Debug.Log("🌱 [SEEDS] Approach an empty garden plot to plant Rose Seeds!");
                        return;
                    }
                    break;

                case "potion_hp":
                    if (progression != null) progression.Heal(250f);
                    break;
                case "potion_amber":
                    if (progression != null) progression.RestoreMana(150f);
                    break;
                case "potion_mp":
                    if (progression != null) progression.RestoreMana(280f);
                    break;
                case "potion_elixir":
                    if (progression != null)
                    {
                        progression.Heal(250f);
                        progression.RestoreMana(150f);
                    }
                    break;
                case "item_whetstone":
                    var weaponTemper = UnityEngine.Object.FindAnyObjectByType<WeaponTemper>();
                    if (weaponTemper != null)
                    {
                        weaponTemper.HoneWithWhetstone();
                    }
                    else
                    {
                        Debug.Log("⚔️ [QUICK ITEM] Whetstone Polish applied: Blade honed to razor temper (+25% Slash DMG)!");
                    }
                    break;
            }
        }

        // Decrement item count
        item.count--;

        Debug.Log($"🧪 [QUICK ITEM USED] Consumed '{item.displayName}'! Remaining: {item.count}.");

        // Show feedback toast
        if (DevClassLoadoutManager.Instance != null)
        {
            string hexColor = ColorUtility.ToHtmlStringRGB(item.themeColor);
            DevClassLoadoutManager.Instance.ShowToast($"<color=#{hexColor}>[{item.displayName.ToUpper()}]</color>\n{item.effectDescription}! ({item.count} remaining)");
        }

        // Play preset audio SFX
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.95f);
        }

        TriggerUseAnimation();
        RefreshDisplay();
        OnItemUsed?.Invoke(item);
    }

    public void RefreshDisplay()
    {
        var item = CurrentItem;
        if (item == null) return;

        if (activeItemIcon != null)
        {
            Sprite sp = LoadSprite(item.spriteFilename);
            if (sp != null) activeItemIcon.sprite = sp;
            activeItemIcon.color = (item.count > 0) ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.6f);
        }

        if (itemCountTMP != null)
        {
            itemCountTMP.text = $"x{item.count}";
            itemCountTMP.color = (item.count > 0) ? new Color(1f, 0.95f, 0.6f) : new Color(0.8f, 0.3f, 0.3f);
        }

        if (itemNameTMP != null)
        {
            itemNameTMP.text = item.displayName;
            itemNameTMP.color = (item.count > 0) ? item.themeColor : Color.gray;
        }
    }

    private void TriggerPunchAnimation(int direction)
    {
        if (activeItemIcon == null) return;
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(DoPunchScale(direction));
    }

    private void TriggerUseAnimation()
    {
        if (activeItemIcon == null) return;
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(DoUsePunch());
    }

    private IEnumerator DoPunchScale(int direction)
    {
        Transform t = activeItemIcon.transform;
        float elapsed = 0f;
        float duration = 0.16f;

        Vector3 startScale = Vector3.one;
        Vector3 peakScale = new Vector3(punchScale, punchScale, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            t.localScale = (progress < 0.5f)
                ? Vector3.Lerp(startScale, peakScale, progress * 2f)
                : Vector3.Lerp(peakScale, startScale, (progress - 0.5f) * 2f);
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    private IEnumerator DoUsePunch()
    {
        Transform t = activeItemIcon.transform;
        float elapsed = 0f;
        float duration = 0.22f;

        Vector3 startScale = Vector3.one;
        Vector3 peakScale = new Vector3(1.35f, 1.35f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            t.localScale = (progress < 0.4f)
                ? Vector3.Lerp(startScale, peakScale, progress / 0.4f)
                : Vector3.Lerp(peakScale, startScale, (progress - 0.4f) / 0.6f);
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    private void PlayCycleAudio()
    {
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.5f);
        }
    }

    private Sprite LoadSprite(string filename)
    {
#if UNITY_EDITOR
        string path = "Assets/Textures/UI/" + filename;
        Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
#endif
        return null;
    }
}
