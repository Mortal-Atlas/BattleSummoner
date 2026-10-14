using UnityEngine;
using UnityEngine.UI;

public class StoreAccessoryModalController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Text statusFeedbackText;

    [Header("Accessory Purchase Buttons")]
    [SerializeField] private Button buyWhetstoneSigilBtn;
    [SerializeField] private Button buyAstralPendantBtn;
    [SerializeField] private Button buyVoracityRingBtn;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseModal);
        }

        if (buyWhetstoneSigilBtn != null)
        {
            buyWhetstoneSigilBtn.onClick.AddListener(() => PurchaseAccessory("sku_whetstone_sigil_t1", "Gilded Whetstone Sigil", 0.25f));
        }

        if (buyAstralPendantBtn != null)
        {
            buyAstralPendantBtn.onClick.AddListener(() => PurchaseAccessory("sku_astral_pendant_t1", "Astral Core Pendant", 0.25f));
        }

        if (buyVoracityRingBtn != null)
        {
            buyVoracityRingBtn.onClick.AddListener(() => PurchaseAccessory("sku_voracity_ring_t1", "Ring of Voracity", 0.25f));
        }
    }

    private void OnEnable()
    {
        StoreMonetizationManager.OnPurchaseSuccessful += HandlePurchaseSuccess;
        StoreMonetizationManager.OnPurchaseFailed += HandlePurchaseFailed;
        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = "Google Play Verified Store: Additive Utility Tier (+0.25x)";
        }
    }

    private void OnDisable()
    {
        StoreMonetizationManager.OnPurchaseSuccessful -= HandlePurchaseSuccess;
        StoreMonetizationManager.OnPurchaseFailed -= HandlePurchaseFailed;
    }

    public void CloseModal()
    {
        gameObject.SetActive(false);
    }

    private void PurchaseAccessory(string skuId, string itemName, float bonus)
    {
        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = $"Processing transaction for {itemName}...";
        }

        if (StoreMonetizationManager.Instance != null)
        {
            StoreMonetizationManager.Instance.ProcessAccessoryPurchase(skuId, itemName, bonus);
        }
        else
        {
            HandlePurchaseFailed("Monetization manager unavailable.");
        }
    }

    private void HandlePurchaseSuccess(string skuId)
    {
        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = $"Acquisition Confirmed! Equipped: {skuId}";
        }
    }

    private void HandlePurchaseFailed(string reason)
    {
        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = $"Transaction Incomplete: {reason}";
        }
    }
}
