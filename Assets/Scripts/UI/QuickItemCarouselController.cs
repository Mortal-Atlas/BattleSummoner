using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BattleSummoner.Core;

[System.Serializable]
public class QuickItemData
{
    public string itemId;
    public string itemName;
    public Sprite itemIcon;
    public int count;
    public string description;
    public System.Action onUse;
}

/// <summary>
/// QuickItemCarouselController implements the 3-node horizontal quick-item carousel chassis.
/// Follows Gemini specification for normalized anchor coordinates, hitboxes, drag thresholds,
/// scale/alpha interpolation, and arrow step triggers.
/// </summary>
public class QuickItemCarouselController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static QuickItemCarouselController Instance { get; private set; }

    [Header("Chassis & Sockets")]
    [SerializeField] private Image chassisImage;
    [SerializeField] private RectTransform leftSocket;
    [SerializeField] private RectTransform centerSocket;
    [SerializeField] private RectTransform rightSocket;

    [Header("Item Slot Visuals")]
    [SerializeField] private Image leftIcon;
    [SerializeField] private CanvasGroup leftCanvasGroup;
    [SerializeField] private TextMeshProUGUI leftCountText;

    [SerializeField] private Image centerIcon;
    [SerializeField] private CanvasGroup centerCanvasGroup;
    [SerializeField] private TextMeshProUGUI centerCountText;
    [SerializeField] private TextMeshProUGUI centerNameText;

    [SerializeField] private Image rightIcon;
    [SerializeField] private CanvasGroup rightCanvasGroup;
    [SerializeField] private TextMeshProUGUI rightCountText;

    [Header("Navigation Chevrons")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private RectTransform leftArrowRect;
    [SerializeField] private Image leftArrowImage;

    [SerializeField] private Button rightArrowButton;
    [SerializeField] private RectTransform rightArrowRect;
    [SerializeField] private Image rightArrowImage;

    [Header("Hitbox Zones")]
    [SerializeField] private Button centerActivationButton;
    [SerializeField] private RectTransform trackDragZone;

    [Header("Carousel State")]
    [SerializeField] private int activeIndex = 0;
    [SerializeField] private float transitionDuration = 0.20f;
    [SerializeField] private float dragDeadzoneThreshold = 15f;

    private List<QuickItemData> items = new List<QuickItemData>();
    private bool isTransitioning = false;
    private Vector2 dragStartPos;
    private bool isDragging = false;
    private Coroutine punchCoroutine;

    public static QuickItemCarouselController EnsureInScene(Transform parent)
    {
        if (Instance != null) return Instance;
        var existing = UnityEngine.Object.FindFirstObjectByType<QuickItemCarouselController>();
        if (existing != null) return existing;
        return null;
    }

    private void Awake()
    {
        Instance = this;
        InitializeDefaultItemsIfEmpty();
    }

    private void Start()
    {
        BindEventListeners();
        RefreshDisplay(false);
    }

    public void BindEventListeners()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveAllListeners();
            leftArrowButton.onClick.AddListener(() => StepCarousel(-1));
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveAllListeners();
            rightArrowButton.onClick.AddListener(() => StepCarousel(1));
        }

        if (centerActivationButton != null)
        {
            centerActivationButton.onClick.RemoveAllListeners();
            centerActivationButton.onClick.AddListener(OnCenterSocketClicked);
        }
    }

    public void BindReferences(Button leftBtn, Button rightBtn, Button itemSlot, Image icon, TextMeshProUGUI countText, TextMeshProUGUI nameText, RectTransform root)
    {
        if (leftBtn != null) leftArrowButton = leftBtn;
        if (rightBtn != null) rightArrowButton = rightBtn;
        if (itemSlot != null) centerActivationButton = itemSlot;
        if (icon != null) centerIcon = icon;
        if (countText != null) centerCountText = countText;
        if (nameText != null) centerNameText = nameText;
        BindEventListeners();
        RefreshDisplay(false);
    }

    public void BindReferences(Button leftBtn, Button rightBtn, GameObject itemSlot, Image icon, TextMeshProUGUI countText, TextMeshProUGUI nameText, RectTransform root)
    {
        BindReferences(leftBtn, rightBtn, itemSlot != null ? itemSlot.GetComponent<Button>() : null, icon, countText, nameText, root);
    }

    public void AddCookedMeal(object meal, int count)
    {
        if (meal == null) return;
        string mealName = meal.ToString();
        var prop = meal.GetType().GetProperty("dishName") ?? meal.GetType().GetProperty("mealName") ?? meal.GetType().GetProperty("name");
        if (prop != null) mealName = prop.GetValue(meal)?.ToString() ?? mealName;

        var existing = items.Find(i => i.itemName == mealName);
        if (existing != null)
        {
            existing.count += count;
        }
        else
        {
            var amber = Resources.Load<Sprite>("UI/Mockup_PotionAmber");
            items.Add(new QuickItemData
            {
                itemId = "meal_" + mealName.ToLower().Replace(" ", "_"),
                itemName = mealName,
                itemIcon = amber,
                count = count,
                description = "Nutritious and delicious dish."
            });
        }
        RefreshDisplay(false);
    }

    public void InitializeDefaultItemsIfEmpty()
    {
        if (items != null && items.Count > 0) return;

        items = new List<QuickItemData>();

        // Load potions
        var red = Resources.Load<Sprite>("UI/Mockup_PotionRed") 
                  ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_PotionRed.png");
        var blue = Resources.Load<Sprite>("UI/Mockup_PotionBlue") 
                   ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_PotionBlue.png");
        var amber = Resources.Load<Sprite>("UI/Mockup_PotionAmber") 
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_PotionAmber.png");
        var elixir = Resources.Load<Sprite>("UI/Mockup_PotionElixir") 
                     ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Mockup_PotionElixir.png");

        items.Add(new QuickItemData
        {
            itemId = "potion_crimson_heal",
            itemName = "Crimson Vitality Draught",
            itemIcon = red,
            count = 5,
            description = "Restores 350 Health over 4 seconds."
        });

        items.Add(new QuickItemData
        {
            itemId = "potion_azure_mana",
            itemName = "Azure Aether Extract",
            itemIcon = blue,
            count = 8,
            description = "Instantly replenishes 250 Mana."
        });

        items.Add(new QuickItemData
        {
            itemId = "potion_amber_haste",
            itemName = "Amber Swiftness Tonic",
            itemIcon = amber,
            count = 3,
            description = "Boosts dash and sprint speed by 40% for 12 seconds."
        });

        items.Add(new QuickItemData
        {
            itemId = "potion_stellar_elixir",
            itemName = "Stellar Elixir of Awakening",
            itemIcon = elixir,
            count = 2,
            description = "Full recovery and resets special skill cooldowns."
        });
    }

    #region Drag & Swipe Navigation
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isTransitioning) return;
        dragStartPos = eventData.position;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || isTransitioning) return;
        isDragging = false;

        float deltaX = eventData.position.x - dragStartPos.x;
        if (Mathf.Abs(deltaX) >= dragDeadzoneThreshold)
        {
            if (deltaX < 0)
            {
                // Swipe Left: Advance inventory forward
                StepCarousel(1);
            }
            else
            {
                // Swipe Right: Reverse inventory backward
                StepCarousel(-1);
            }
        }
    }
    #endregion

    #region Step & Center Action
    public void StepCarousel(int direction)
    {
        if (isTransitioning || items.Count == 0) return;

        // Animate Chevron feedback
        StartCoroutine(AnimateChevronFeedback(direction));

        // Direction +1: forward (Swipe Left: Right -> Center, Center -> Left)
        // Direction -1: backward (Swipe Right: Left -> Center, Center -> Right)
        int targetIndex = (activeIndex + direction) % items.Count;
        if (targetIndex < 0) targetIndex += items.Count;

        StartCoroutine(AnimateTransition(direction, targetIndex));
    }

    public void OnCenterSocketClicked()
    {
        if (isTransitioning || items.Count == 0) return;
        UseActiveItem();
    }

    public void UseActiveItem()
    {
        if (activeIndex < 0 || activeIndex >= items.Count) return;

        var activeItem = items[activeIndex];

        // 1. Play brief scale-punch animation on Center Icon (0.9 -> 1.0 over 0.12s)
        if (punchCoroutine != null) StopCoroutine(punchCoroutine);
        punchCoroutine = StartCoroutine(AnimateCenterScalePunch());

        // 2. Execute custom action or notify
        activeItem.onUse?.Invoke();
        activeItem.count--;

        if (SystemBroadcastManager.Instance != null)
        {
            SystemBroadcastManager.Instance.DispatchBroadcast(
                $"🧪 [ITEM CONSUMED] {activeItem.itemName}! (Remaining: {activeItem.count})", false);
        }

        Debug.Log($"🧪 [Carousel] Consumed 1x '{activeItem.itemName}'. Remaining: {activeItem.count}");

        // 3. If stack reaches 0, remove and smoothly collapse
        if (activeItem.count <= 0)
        {
            items.RemoveAt(activeIndex);
            if (items.Count > 0)
            {
                activeIndex %= items.Count;
            }
            else
            {
                activeIndex = 0;
            }
        }

        RefreshDisplay(false);
    }
    #endregion

    #region Animations & Interpolation
    private IEnumerator AnimateCenterScalePunch()
    {
        if (centerSocket == null) yield break;
        Vector3 startScale = Vector3.one * 0.90f;
        Vector3 endScale = Vector3.one * 1.0f;
        float duration = 0.12f;
        float elapsed = 0f;

        centerSocket.localScale = startScale;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            centerSocket.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        centerSocket.localScale = endScale;
    }

    private IEnumerator AnimateChevronFeedback(int direction)
    {
        RectTransform chevron = (direction < 0) ? leftArrowRect : rightArrowRect;
        Image chevronImg = (direction < 0) ? leftArrowImage : rightArrowImage;
        if (chevron == null) yield break;

        Vector2 basePos = chevron.anchoredPosition;
        float shiftX = (direction < 0) ? 5f : -5f; // shift inward
        Vector2 shiftedPos = basePos + new Vector2(shiftX, 0f);

        Color origColor = chevronImg != null ? chevronImg.color : Color.white;
        Color flashColor = new Color(1.2f, 1.0f, 0.6f, 1f); // bright gold flash

        if (chevronImg != null) chevronImg.color = flashColor;
        chevron.anchoredPosition = shiftedPos;

        yield return new WaitForSecondsRealtime(0.10f);

        chevron.anchoredPosition = basePos;
        if (chevronImg != null) chevronImg.color = origColor;
    }

    private IEnumerator AnimateTransition(int direction, int targetIndex)
    {
        isTransitioning = true;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            // EaseOutCubic
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            if (direction > 0)
            {
                // Forward step
                if (centerCanvasGroup != null) centerCanvasGroup.alpha = Mathf.Lerp(1.0f, 0.4f, ease);
                if (centerSocket != null) centerSocket.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.75f, ease);

                if (rightCanvasGroup != null) rightCanvasGroup.alpha = Mathf.Lerp(0.4f, 1.0f, ease);
                if (rightSocket != null) rightSocket.localScale = Vector3.Lerp(Vector3.one * 0.75f, Vector3.one, ease);
            }
            else
            {
                // Backward step
                if (centerCanvasGroup != null) centerCanvasGroup.alpha = Mathf.Lerp(1.0f, 0.4f, ease);
                if (centerSocket != null) centerSocket.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.75f, ease);

                if (leftCanvasGroup != null) leftCanvasGroup.alpha = Mathf.Lerp(0.4f, 1.0f, ease);
                if (leftSocket != null) leftSocket.localScale = Vector3.Lerp(Vector3.one * 0.75f, Vector3.one, ease);
            }

            yield return null;
        }

        activeIndex = targetIndex;
        RefreshDisplay(true);
        isTransitioning = false;
    }

    public void RefreshDisplay(bool resetTransforms = true)
    {
        if (items.Count == 0)
        {
            SetSlotVisuals(centerIcon, centerCanvasGroup, centerCountText, null, 0);
            SetSlotVisuals(leftIcon, leftCanvasGroup, leftCountText, null, 0);
            SetSlotVisuals(rightIcon, rightCanvasGroup, rightCountText, null, 0);
            if (centerNameText != null) centerNameText.text = "Empty Bag";
            return;
        }

        int n = items.Count;
        int idxCenter = activeIndex;
        int idxLeft = (activeIndex - 1 + n) % n;
        int idxRight = (activeIndex + 1) % n;

        var itemCenter = items[idxCenter];
        var itemLeft = items[idxLeft];
        var itemRight = items[idxRight];

        SetSlotVisuals(centerIcon, centerCanvasGroup, centerCountText, itemCenter.itemIcon, itemCenter.count);
        SetSlotVisuals(leftIcon, leftCanvasGroup, leftCountText, itemLeft.itemIcon, itemLeft.count);
        SetSlotVisuals(rightIcon, rightCanvasGroup, rightCountText, itemRight.itemIcon, itemRight.count);

        if (centerNameText != null) centerNameText.text = itemCenter.itemName;

        if (resetTransforms)
        {
            if (centerSocket != null)
            {
                centerSocket.localScale = Vector3.one;
                centerSocket.SetAsLastSibling(); // top layer
            }
            if (centerCanvasGroup != null) centerCanvasGroup.alpha = 1.0f;

            if (leftSocket != null)
            {
                leftSocket.localScale = Vector3.one * 0.75f;
                leftSocket.SetAsFirstSibling();
            }
            if (leftCanvasGroup != null) leftCanvasGroup.alpha = 0.4f;

            if (rightSocket != null)
            {
                rightSocket.localScale = Vector3.one * 0.75f;
                rightSocket.SetSiblingIndex(1);
            }
            if (rightCanvasGroup != null) rightCanvasGroup.alpha = 0.4f;
        }
    }

    private void SetSlotVisuals(Image icon, CanvasGroup cg, TextMeshProUGUI countText, Sprite sprite, int count)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = (sprite != null);
        }
        if (countText != null)
        {
            countText.text = count > 1 ? $"x{count}" : "";
            countText.enabled = (count > 0);
        }
    }
    #endregion
}

namespace BattleSummoner.UI
{
    // Alias namespace support
    public class QuickItemCarouselControllerProxy : QuickItemCarouselController { }
}
