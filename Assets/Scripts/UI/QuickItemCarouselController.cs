using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [System.Serializable]
    public class UsableCombatItem
    {
        public string id;
        public string displayName;
        public string effectDescription;
        public string spriteFilename;
        public int count;
        public Color themeColor = Color.white;
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
            return;
        }

        items = new List<UsableCombatItem>
        {
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
            }
        };
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
        OnItemChanged?.Invoke(currentIndex, CurrentItem);
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

        // Apply item effect to PlayerProgression / WeaponTemper
        var progression = UnityEngine.Object.FindAnyObjectByType<PlayerProgression>();
        switch (item.id)
        {
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
