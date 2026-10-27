using System;
using System.Collections;
using UnityEngine;
using TMPro;
using BattleSummoner.UI;

namespace BattleSummoner.WorldActivities
{
    public enum DesertBeachInteractableKind
    {
        DesertSunAgave,
        DesertSolarCactus,
        MiragedewSucculent,
        BuriedSandstoneUrn,
        SunStoneObelisk,
        BeachPearlescentShell,
        FlotsamCargoCrate,
        TidalGeode
    }

    /// <summary>
    /// Interactive gameplay element for Deserts and Beaches.
    /// Supports harvestable flora, half-buried relic urns, ancient solar obelisks,
    /// washed-up flotsam cargo crates, pearlescent shells, and tidal geodes.
    /// Handles proximity detection, tactile 3D floating prompts, touch/click/E trigger,
    /// audio chime, particle VFX burst, inventory integration, and player buffs.
    /// </summary>
    public class DesertAndBeachInteractable : MonoBehaviour
    {
        [Header("Interactable Identity")]
        public DesertBeachInteractableKind kind = DesertBeachInteractableKind.DesertSunAgave;
        public string displayName = "Sun-Fire Agave";
        [TextArea(2, 3)]
        public string itemDescription = "Hardy succulent native to the western dunes. Restores mana and provides alchemical fire essence.";
        public string itemId = "item_desert_sun_agave";
        public int yieldAmount = 2;
        public int goldValue = 35;

        [Header("Interaction Settings")]
        public float interactionDistance = 3.4f;
        public KeyCode interactKey = KeyCode.E;
        public bool isHarvested = false;
        public float respawnTime = 75f;
        public bool previewBillboardInEditor = false;

        [Header("Visual Feedback")]
        public GameObject visualRoot;
        public Light auraLight;
        public ParticleSystem ambientVFX;
        public ParticleSystem harvestVFX;
        public GameObject harvestVFXPrefab;
        public AudioSource audioSource;
        public AudioClip harvestSoundClip;

        [Header("Interactive 3D Billboard Prompt")]
        public GameObject promptCanvasObj;
        public TextMeshPro promptTextTMP;

        private float currentRespawnTimer = 0f;
        private Vector3 initialScale;
        private bool isPlayerNearby = false;
        private Transform cachedPlayer;
        private float billboardBaseY = 1.4f;

        public static event Action<DesertAndBeachInteractable> OnInteracted;

        private void Awake()
        {
            initialScale = (visualRoot != null) ? visualRoot.transform.localScale : transform.localScale;
            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0).gameObject;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0.85f;
                    audioSource.minDistance = 2.0f;
                    audioSource.maxDistance = 25.0f;
                }
            }

            SetupPromptBillboard(previewBillboardInEditor);
            UpdateVisualAura();
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            var p = GameObject.Find("Player_Hero");
            if (p != null) cachedPlayer = p.transform;
            else if (Camera.main != null) cachedPlayer = Camera.main.transform;
        }

        public void SetupPromptBillboard(bool forceShow = false)
        {
            billboardBaseY = GetBillboardHeight();

            if (promptCanvasObj == null)
            {
                var existingChild = transform.Find("InteractionPromptBillboard");
                if (existingChild != null)
                {
                    promptCanvasObj = existingChild.gameObject;
                    promptTextTMP = promptCanvasObj.GetComponent<TextMeshPro>();
                }
            }

            if (promptCanvasObj == null)
            {
                promptCanvasObj = new GameObject("InteractionPromptBillboard");
                promptCanvasObj.transform.SetParent(transform, false);
            }

            promptCanvasObj.transform.localPosition = Vector3.up * billboardBaseY;

            if (promptTextTMP == null)
            {
                promptTextTMP = promptCanvasObj.GetComponent<TextMeshPro>() ?? promptCanvasObj.AddComponent<TextMeshPro>();
            }

            string hex = ColorUtility.ToHtmlStringRGB(GetPromptTextColor());
            promptTextTMP.richText = true;
            promptTextTMP.text = $"<color=#{hex}><b>[E / Tap]</b></color>  {displayName}";
            promptTextTMP.fontSize = 2.4f;
            promptTextTMP.alignment = TextAlignmentOptions.Center;
            promptTextTMP.color = Color.white;
            promptTextTMP.outlineWidth = 0.28f;
            promptTextTMP.outlineColor = new Color32(10, 10, 15, 255);

            var rt = promptCanvasObj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(8f, 1.4f);

            promptCanvasObj.SetActive(forceShow || previewBillboardInEditor);
        }

        private float GetBillboardHeight()
        {
            return kind switch
            {
                DesertBeachInteractableKind.DesertSolarCactus => 3.5f,
                DesertBeachInteractableKind.SunStoneObelisk => 5.8f,
                DesertBeachInteractableKind.BuriedSandstoneUrn => 1.4f,
                DesertBeachInteractableKind.FlotsamCargoCrate => 1.4f,
                DesertBeachInteractableKind.BeachPearlescentShell => 1.0f,
                DesertBeachInteractableKind.TidalGeode => 1.4f,
                DesertBeachInteractableKind.DesertSunAgave => 2.6f,
                DesertBeachInteractableKind.MiragedewSucculent => 1.5f,
                _ => 1.6f
            };
        }

        public string GetInteractionPromptText()
        {
            return kind switch
            {
                DesertBeachInteractableKind.DesertSunAgave => "[E / Tap] Harvest Sun-Fire Agave ✦",
                DesertBeachInteractableKind.DesertSolarCactus => "[E / Tap] Gather Prickly Pear & Solar Sap 🌵",
                DesertBeachInteractableKind.MiragedewSucculent => "[E / Tap] Collect Miragedew Nectar ✨",
                DesertBeachInteractableKind.BuriedSandstoneUrn => "[E / Tap] Unearth Sandstone Relic Urn 🏺",
                DesertBeachInteractableKind.SunStoneObelisk => "[E / Tap] Resonate with Sun-Stone Shrine ☀️",
                DesertBeachInteractableKind.BeachPearlescentShell => "[E / Tap] Gather Pearlescent Shell 🐚",
                DesertBeachInteractableKind.FlotsamCargoCrate => "[E / Tap] Salvage Cargo Flotsam 📦",
                DesertBeachInteractableKind.TidalGeode => "[E / Tap] Harvest Tidal Quartz Geode 💎",
                _ => "[E / Tap] Interact"
            };
        }

        public Color GetPromptTextColor()
        {
            return kind switch
            {
                DesertBeachInteractableKind.DesertSunAgave => new Color(1.0f, 0.78f, 0.22f),
                DesertBeachInteractableKind.DesertSolarCactus => new Color(0.60f, 0.95f, 0.38f),
                DesertBeachInteractableKind.MiragedewSucculent => new Color(0.35f, 0.92f, 0.95f),
                DesertBeachInteractableKind.BuriedSandstoneUrn => new Color(1.0f, 0.82f, 0.30f),
                DesertBeachInteractableKind.SunStoneObelisk => new Color(1.0f, 0.88f, 0.35f),
                DesertBeachInteractableKind.BeachPearlescentShell => new Color(0.82f, 0.94f, 1.0f),
                DesertBeachInteractableKind.FlotsamCargoCrate => new Color(0.96f, 0.80f, 0.50f),
                DesertBeachInteractableKind.TidalGeode => new Color(0.30f, 0.85f, 1.0f),
                _ => Color.white
            };
        }

        public void UpdateVisualAura()
        {
            if (auraLight == null)
            {
                var lightGo = new GameObject("AuraLight");
                lightGo.transform.SetParent(transform, false);
                lightGo.transform.localPosition = Vector3.up * (billboardBaseY * 0.45f);
                auraLight = lightGo.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.range = (kind == DesertBeachInteractableKind.SunStoneObelisk) ? 14f : 4.5f;
                auraLight.intensity = (kind == DesertBeachInteractableKind.SunStoneObelisk) ? 3.0f : 1.4f;
                auraLight.shadows = LightShadows.None;
                auraLight.color = GetPromptTextColor();
            }
        }

        private void Update()
        {
            if (isHarvested)
            {
                currentRespawnTimer += Time.deltaTime;
                if (currentRespawnTimer >= respawnTime)
                {
                    Respawn();
                }
                return;
            }

            if (cachedPlayer == null)
            {
                FindPlayer();
                if (cachedPlayer == null) return;
            }

            float dist = Vector3.Distance(transform.position, cachedPlayer.position);
            bool nearby = (dist <= interactionDistance);

            if (nearby != isPlayerNearby)
            {
                isPlayerNearby = nearby;
                if (promptCanvasObj != null) promptCanvasObj.SetActive(isPlayerNearby);
            }

            if (isPlayerNearby)
            {
                // Face billboard prompt toward active camera
                if (promptCanvasObj != null && promptCanvasObj.activeSelf)
                {
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        promptCanvasObj.transform.rotation = Quaternion.LookRotation(promptCanvasObj.transform.position - cam.transform.position);
                    }
                }

                // Check keyboard trigger
                if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Space))
                {
                    ExecuteInteraction();
                }
            }
        }

        private void OnMouseDown()
        {
            if (isHarvested) return;
            ExecuteInteraction();
        }

        public void ExecuteInteraction()
        {
            if (isHarvested) return;
            isHarvested = true;
            currentRespawnTimer = 0f;

            if (promptCanvasObj != null) promptCanvasObj.SetActive(false);

            StartCoroutine(TactileHarvestRoutine());
        }

        private IEnumerator TactileHarvestRoutine()
        {
            // Micro tactile scale bounce
            Transform targetTr = (visualRoot != null) ? visualRoot.transform : transform;
            float elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                targetTr.localScale = initialScale * Mathf.Lerp(1.0f, 1.25f, elapsed / 0.12f);
                yield return null;
            }

            // Spawn harvest particle VFX burst
            if (harvestVFXPrefab != null)
            {
                var vfx = Instantiate(harvestVFXPrefab, transform.position + Vector3.up * (billboardBaseY * 0.6f), Quaternion.identity);
                Destroy(vfx, 3.5f);
            }
            else if (harvestVFX != null)
            {
                harvestVFX.transform.SetParent(null);
                harvestVFX.Play();
                Destroy(harvestVFX.gameObject, 3.0f);
            }

            // Audio feedback
            if (audioSource != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.15f);
                if (harvestSoundClip != null) audioSource.PlayOneShot(harvestSoundClip, 1.0f);
                else audioSource.Play();
            }

            // Apply rewards, buffs & inventory additions
            ApplyReward();

            // Hide visual mesh during respawn window (except Obelisks which stay as permanent landmarks)
            if (kind != DesertBeachInteractableKind.SunStoneObelisk)
            {
                if (visualRoot != null) visualRoot.SetActive(false);
                if (auraLight != null) auraLight.enabled = false;
            }

            targetTr.localScale = initialScale;
            OnInteracted?.Invoke(this);
        }

        private void ApplyReward()
        {
            string toastMsg = "";

            switch (kind)
            {
                case DesertBeachInteractableKind.DesertSunAgave:
                    toastMsg = $"🌿 [HARVESTED] {yieldAmount}x {displayName}! (+35 MP Restore)";
                    break;
                case DesertBeachInteractableKind.DesertSolarCactus:
                    toastMsg = $"🌵 [GATHERED] Prickly Pear & Solar Sap! (+45 HP Restore)";
                    break;
                case DesertBeachInteractableKind.MiragedewSucculent:
                    toastMsg = $"✨ [DISCOVERED] {displayName}! (+Alchemy Mastery +50)";
                    break;
                case DesertBeachInteractableKind.BuriedSandstoneUrn:
                    toastMsg = $"🏺 [UNEARTHED] Ancient Sandstone Relic Urn! (+{goldValue * yieldAmount} Gold & Sunstone Core)";
                    break;
                case DesertBeachInteractableKind.SunStoneObelisk:
                    toastMsg = $"☀️ [RESONANCE] Sun-Stone Blessing Activated! (+20% Move Speed for 120s)";
                    break;
                case DesertBeachInteractableKind.BeachPearlescentShell:
                    toastMsg = $"🐚 [GATHERED] {yieldAmount}x Luminous Sea Pearls & Iridescent Shells!";
                    break;
                case DesertBeachInteractableKind.FlotsamCargoCrate:
                    toastMsg = $"📦 [SALVAGED] Cargo Flotsam! (+{goldValue} Gold & Maritime Planks)";
                    break;
                case DesertBeachInteractableKind.TidalGeode:
                    toastMsg = $"💎 [EXTRACTED] Tidal Geode Crystals! (+2 Azure Mana Prisms)";
                    break;
            }

            // Display Toast via active HUD
            if (OverworldPlayerHUDController.Instance != null)
            {
                OverworldPlayerHUDController.Instance.ShowToast(toastMsg, 3.0f);
            }
            else if (PlayerHUDController.Instance != null)
            {
                PlayerHUDController.Instance.ShowToast(toastMsg, 3.0f);
            }
            else if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(toastMsg);
            }
            else
            {
                Debug.Log(toastMsg);
            }

            // Add item to player inventory
            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.AddItem(new InventoryItemEntry
                {
                    itemId = itemId,
                    displayName = displayName,
                    category = (kind == DesertBeachInteractableKind.FlotsamCargoCrate || kind == DesertBeachInteractableKind.BuriedSandstoneUrn)
                        ? ItemCategory.Relics
                        : ItemCategory.Materials,
                    rarity = (kind == DesertBeachInteractableKind.SunStoneObelisk || kind == DesertBeachInteractableKind.BuriedSandstoneUrn)
                        ? ItemRarity.Rare
                        : ItemRarity.Common,
                    quantity = yieldAmount,
                    goldValue = goldValue,
                    description = itemDescription
                });
            }
        }

        public void Respawn()
        {
            isHarvested = false;
            currentRespawnTimer = 0f;
            if (visualRoot != null) visualRoot.SetActive(true);
            if (auraLight != null) auraLight.enabled = true;
            if (visualRoot != null) visualRoot.transform.localScale = initialScale;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GetPromptTextColor();
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
