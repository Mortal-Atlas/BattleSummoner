using System;
using UnityEngine;
using BattleSummoner.UI;

namespace BattleSummoner.World
{
    public enum CollectibleType
    {
        Mushroom,
        Plant,
        Fruit,
        Insect,
        Item
    }

    /// <summary>
    /// WorldCollectibleItem represents interactive 3D harvestable nodes (mushrooms, wild herbs,
    /// orchard fruits, rare relics) placed across the world.
    /// Can be discovered and tracked on the Monster Hunter World Exploration Map.
    /// </summary>
    public class WorldCollectibleItem : MonoBehaviour
    {
        [Header("Collectible Identity")]
        public string itemId = "glow_mushroom";
        public string displayName = "Glow Cap Mushroom";
        public CollectibleType itemType = CollectibleType.Mushroom;
        [TextArea(2, 3)]
        public string description = "Bioluminescent mushroom harvested from damp forest roots. Potent alchemical ingredient.";

        [Header("Harvest State")]
        public bool isHarvested = false;
        public float respawnDuration = 60f;
        private float respawnTimer = 0f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private Light itemGlowLight;
        [SerializeField] private float bobbingSpeed = 2.5f;
        [SerializeField] private float bobbingHeight = 0.08f;
        private Vector3 initialLocalPos;

        public static event Action<WorldCollectibleItem> OnItemHarvested;

        private void Awake()
        {
            initialLocalPos = transform.localPosition;
            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0).gameObject;
            }
        }

        private void Update()
        {
            if (isHarvested)
            {
                respawnTimer += Time.deltaTime;
                if (respawnTimer >= respawnDuration)
                {
                    Respawn();
                }
                return;
            }

            // Gentle visual bobbing
            float bob = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
            transform.localPosition = initialLocalPos + Vector3.up * bob;

            // Check player proximity
            var player = GameObject.Find("Player_Hero");
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= 2.2f)
                {
                    // Automatic or tap collection
                    Harvest(player);
                }
            }
        }

        public void Harvest(GameObject player)
        {
            if (isHarvested) return;
            isHarvested = true;
            respawnTimer = 0f;

            if (visualModel != null) visualModel.SetActive(false);
            if (itemGlowLight != null) itemGlowLight.enabled = false;

            // Audio & VFX
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
            }

            // Toast
            string iconStr = itemType switch
            {
                CollectibleType.Mushroom => "🍄",
                CollectibleType.Plant => "🌿",
                CollectibleType.Fruit => "🍎",
                CollectibleType.Insect => "🦋",
                _ => "✨"
            };

            if (PlayerHUDController.Instance != null)
            {
                PlayerHUDController.Instance.ShowToast($"{iconStr} Collected {displayName}!", 2.5f);
            }

            // Add to Inventory if present
            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.AddItem(new InventoryItemEntry
                {
                    itemId = itemId,
                    displayName = displayName,
                    category = ItemCategory.Materials,
                    rarity = ItemRarity.Common,
                    quantity = 1,
                    description = description
                });
            }

            OnItemHarvested?.Invoke(this);
            Debug.Log($"🌾 [COLLECTED] Picked up {displayName} ({itemType}) at {transform.position}!");
        }

        public void Respawn()
        {
            isHarvested = false;
            respawnTimer = 0f;
            if (visualModel != null) visualModel.SetActive(true);
            if (itemGlowLight != null) itemGlowLight.enabled = true;
        }
    }
}
