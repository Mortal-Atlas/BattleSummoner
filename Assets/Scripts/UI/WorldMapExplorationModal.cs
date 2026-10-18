using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Minigames.Fishing;
using BattleSummoner.Minigames.Insects;
using BattleSummoner.Minigames.Gardening;
using BattleSummoner.World;

namespace BattleSummoner.UI
{
    public enum MapFilterCategory
    {
        All,
        Mushroom,
        Plant,
        Fruit,
        Insect,
        Fishing,
        Ore,
        Settlement
    }

    [System.Serializable]
    public class WorldMapPOI
    {
        public string id;
        public string name;
        public MapFilterCategory category;
        public Vector3 worldPosition;
        [TextArea(2, 3)]
        public string description;
        public bool isDiscovered = false;
        public bool isSettlement = false;
        public string iconEmoji = "📍";
        public string iconBadge = "";
        public Color pinColor = Color.white;
        public GameObject inWorldObject;

        public string GetBadge()
        {
            if (!string.IsNullOrEmpty(iconBadge)) return iconBadge;
            return category switch
            {
                MapFilterCategory.Settlement => "C",
                MapFilterCategory.Mushroom => "M",
                MapFilterCategory.Plant => "P",
                MapFilterCategory.Fruit => "F",
                MapFilterCategory.Insect => "B",
                MapFilterCategory.Fishing => "W",
                MapFilterCategory.Ore => "O",
                _ => "•"
            };
        }
    }

    /// <summary>
    /// WorldMapExplorationModal implements a Monster Hunter-style World Exploration Map:
    /// - Brought up when tapping the Mini Map radar (or pressing 'M').
    /// - Top-right [X] close button.
    /// - Dynamic exploration fog-of-war tracking: reveals terrain & collectibles once explored.
    /// - Collectibles tracking: Mushrooms, Plants & Crops, Fruits, Wild Insects, Fishing Spots, Ore Veins.
    /// - Fast Travel: Tap any discovered settlement/camp to instantly fast travel there!
    /// </summary>
    public class WorldMapExplorationModal : MonoBehaviour
    {
        public static WorldMapExplorationModal Instance { get; private set; }

        [Header("Modal Root")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;

        [Header("Header Readouts")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;

        [Header("Filter Buttons")]
        [SerializeField] private Transform filterBarContainer;
        private readonly Dictionary<MapFilterCategory, Button> filterButtons = new Dictionary<MapFilterCategory, Button>();
        private MapFilterCategory activeFilter = MapFilterCategory.All;

        [Header("Tactical Map Canvas")]
        [SerializeField] private RectTransform mapViewportRect;
        [SerializeField] private RectTransform mapContentRect;
        [SerializeField] private RectTransform playerBlipRect;
        [SerializeField] private Transform pinsContainer;

        [Header("Bottom POI Inspect Card")]
        [SerializeField] private RectTransform inspectCardRect;
        [SerializeField] private TextMeshProUGUI poiNameTMP;
        [SerializeField] private TextMeshProUGUI poiCategoryCoordsTMP;
        [SerializeField] private TextMeshProUGUI poiDescriptionTMP;
        [SerializeField] private TextMeshProUGUI poiStatusTMP;
        [SerializeField] private Button fastTravelButton;
        [SerializeField] private TextMeshProUGUI fastTravelBtnLabelTMP;

        [Header("Map Coordinate Bounds")]
        // World coordinates mapping to UI coordinates
        private readonly Vector2 worldMin = new Vector2(-48f, -235f);
        private readonly Vector2 worldMax = new Vector2(58f, 52f);
        private readonly Vector2 mapCanvasSize = new Vector2(690f, 740f);

        [Header("Exploration State")]
        private readonly HashSet<Vector2Int> exploredGridCells = new HashSet<Vector2Int>();
        private const float GRID_CELL_SIZE = 14f;
        private const float DISCOVERY_RADIUS = 28f;
        private readonly List<WorldMapPOI> allPOIs = new List<WorldMapPOI>();
        private readonly List<GameObject> activePinObjects = new List<GameObject>();
        private WorldMapPOI selectedPOI;
        private Transform playerTransform;

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            InitializePOIDatabase();
        }

        private void Start()
        {
            FindPlayer();
            EnsureUIBuilt();
            CloseModal(true); // Closed by default
        }

        private void Update()
        {
            // Desktop keyboard shortcut M for map, Escape to close
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleModal();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && IsOpen)
            {
                CloseModal();
            }

            // Real-time player exploration tracking
            TrackPlayerExploration();

            // Real-time map blip update if modal is open
            if (IsOpen)
            {
                UpdatePlayerBlipOnMap();
            }
        }

        public void FindPlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.Find("Player_Hero");
                if (player != null) playerTransform = player.transform;
            }
        }

        #region Exploration & POI Database
        private void InitializePOIDatabase()
        {
            allPOIs.Clear();

            // --- 1. SETTLEMENTS & CAMPS (Fast Travel Points) ---
            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_solas_plaza",
                name = "Solas Town Plaza",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(-13f, 0.9f, -14f),
                description = "Central Town Hub of Solas. Features the Grand Sun Fountain, town notice boards, and merchant stalls.",
                isSettlement = true,
                isDiscovered = true, // Starting settlement is always discovered!
                iconEmoji = "🏰",
                pinColor = new Color(1f, 0.85f, 0.2f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_guild_hall",
                name = "Adventurer's Guild Hall",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(2f, 0.9f, -5f),
                description = "Headquarters for registered hunters and summoners. Features the Expedition Task Board and quest archives.",
                isSettlement = true,
                isDiscovered = true,
                iconEmoji = "🛡️",
                pinColor = new Color(0.9f, 0.3f, 0.25f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_forge",
                name = "Ironheart Blacksmith Forge",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(-2f, 0.9f, -25f),
                description = "Workshop of Master Keith. Blade tempering, weapon crafting, and metal armor reinforcement.",
                isSettlement = true,
                isDiscovered = true,
                iconEmoji = "⚒️",
                pinColor = new Color(1f, 0.55f, 0.15f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_tavern",
                name = "The Wandering Wyvern Tavern",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(-24f, 0.9f, -5f),
                description = "Lively tavern where travelers rest, share rumors, and purchase hearty campfire provisions.",
                isSettlement = true,
                isDiscovered = true,
                iconEmoji = "🍺",
                pinColor = new Color(0.85f, 0.65f, 0.35f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_lakeside_pier",
                name = "Lakeside Pier & Camp",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(18f, 0.9f, -210f),
                description = "Tranquil fishing sanctuary with rustic timber pier and lantern camp. Idyllic spot for river angling.",
                isSettlement = true,
                isDiscovered = true, // Discovered with life-sim pond
                iconEmoji = "🎣",
                pinColor = new Color(0.2f, 0.85f, 1f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_garden_outpost",
                name = "Sunlit Community Garden",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(-14f, 0.9f, -212f),
                description = "Lush agricultural haven with fertile garden beds, irrigation channels, and rare floral cultivars.",
                isSettlement = true,
                isDiscovered = true,
                iconEmoji = "🌱",
                pinColor = new Color(0.4f, 0.95f, 0.45f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_mistwood_shrine",
                name = "Mistwood Ancient Shrine",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(42f, 0.9f, 4f),
                description = "Sacred altar nestled in the deep forest. Channel ancient blessings and replenish mana reserves.",
                isSettlement = true,
                isDiscovered = false,
                iconEmoji = "⛩️",
                pinColor = new Color(0.75f, 0.4f, 1f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "settlement_spire_gate",
                name = "Abyssal Spire Dungeon Gate",
                category = MapFilterCategory.Settlement,
                worldPosition = new Vector3(-13f, 0.9f, 38f),
                description = "Towering gothic archway leading into the hundredfold dungeon dimensions. High danger level.",
                isSettlement = true,
                isDiscovered = false,
                iconEmoji = "🌀",
                pinColor = new Color(0.6f, 0.2f, 0.9f)
            });

            // --- 2. 🍄 MUSHROOMS & FUNGI ---
            allPOIs.Add(new WorldMapPOI
            {
                id = "shroom_glow_cap_1",
                name = "Glow Cap Mushrooms",
                category = MapFilterCategory.Mushroom,
                worldPosition = new Vector3(20f, 0f, -22f),
                description = "Bioluminescent mushrooms thriving in tree shade. Used in night-vision and mana restoration potions.",
                iconEmoji = "🍄",
                pinColor = new Color(0.2f, 0.95f, 0.85f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "shroom_azure_spore",
                name = "Azure Spore Cluster",
                category = MapFilterCategory.Mushroom,
                worldPosition = new Vector3(32f, 0f, 14f),
                description = "Fragrant blue caps growing near mineral seepage. Essential for frost protection balms.",
                iconEmoji = "🍄",
                pinColor = new Color(0.3f, 0.7f, 1f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "shroom_forest_truffle",
                name = "Mistwood Black Truffle",
                category = MapFilterCategory.Mushroom,
                worldPosition = new Vector3(44f, 0f, -26f),
                description = "Rare subterranean delicacy prized by chefs and alchemists for high nutritional vitality.",
                iconEmoji = "🍄",
                pinColor = new Color(0.8f, 0.5f, 0.3f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "shroom_phosphor_fungus",
                name = "Phosphor Tree Fungus",
                category = MapFilterCategory.Mushroom,
                worldPosition = new Vector3(38f, 0f, 24f),
                description = "Glows with a warm amber cadence. Can be extracted into illuminating lantern oils.",
                iconEmoji = "🍄",
                pinColor = new Color(1f, 0.75f, 0.2f)
            });

            // --- 3. 🌿 PLANTS & CROPS ---
            allPOIs.Add(new WorldMapPOI
            {
                id = "plant_red_rose_plot",
                name = "Garden Bed: Red Roses",
                category = MapFilterCategory.Plant,
                worldPosition = new Vector3(-14f, 0f, -212f),
                description = "Prized crimson blooms. Can be harvested for ceremonial gifts and passion perfume.",
                iconEmoji = "🌹",
                pinColor = new Color(1f, 0.25f, 0.35f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "plant_golden_cosmos",
                name = "Garden Bed: Golden Cosmos",
                category = MapFilterCategory.Plant,
                worldPosition = new Vector3(-11.6f, 0f, -212f),
                description = "Radiant golden flowers that absorb solar energy. Valuable trade item.",
                iconEmoji = "🌼",
                pinColor = new Color(1f, 0.85f, 0.2f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "plant_astral_orchid",
                name = "Garden Bed: Astral Orchid",
                category = MapFilterCategory.Plant,
                worldPosition = new Vector3(-9.2f, 0f, -212f),
                description = "Mystical flora pulsing with celestial light. Highly sought after by guild mages.",
                iconEmoji = "🪻",
                pinColor = new Color(0.7f, 0.4f, 1f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "plant_wild_azure_herb",
                name = "Wild Mistwood Herb",
                category = MapFilterCategory.Plant,
                worldPosition = new Vector3(15f, 0f, -8f),
                description = "Potent restorative herb growing along dirt paths. Heals minor wounds upon consumption.",
                iconEmoji = "🌿",
                pinColor = new Color(0.3f, 0.9f, 0.4f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "plant_sunlit_fern",
                name = "Sunlit Maidenhair Fern",
                category = MapFilterCategory.Plant,
                worldPosition = new Vector3(26f, 0f, 6f),
                description = "Delicate fronds carrying high moisture content. Great for soothing burn ointments.",
                iconEmoji = "🌿",
                pinColor = new Color(0.4f, 0.95f, 0.3f)
            });

            // --- 4. 🍎 FRUIT & ORCHARDS ---
            allPOIs.Add(new WorldMapPOI
            {
                id = "fruit_strawberry_bed",
                name = "Strawberry Patch",
                category = MapFilterCategory.Fruit,
                worldPosition = new Vector3(-14f, 0f, -209.6f),
                description = "Sweet ruby strawberries ready for picking. Restores immediate stamina on the move.",
                iconEmoji = "🍓",
                pinColor = new Color(1f, 0.25f, 0.4f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "fruit_wild_apple_tree",
                name = "Sweet Sun Apple Tree",
                category = MapFilterCategory.Fruit,
                worldPosition = new Vector3(25f, 0f, -18f),
                description = "Crisp, golden-red apples dangling from high boughs. Refreshing adventurer snack.",
                iconEmoji = "🍎",
                pinColor = new Color(0.95f, 0.2f, 0.2f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "fruit_star_berry_bush",
                name = "Wild Berry Bush",
                category = MapFilterCategory.Fruit,
                worldPosition = new Vector3(18f, 0f, 12f),
                description = "Dense cluster of sweet forest berries. Loved by both summons and adventurers.",
                iconEmoji = "🫐",
                pinColor = new Color(0.5f, 0.3f, 0.85f)
            });

            // --- 5. 🦋 WILD INSECTS ---
            allPOIs.Add(new WorldMapPOI
            {
                id = "insect_monarch_grove",
                name = "Monarch Butterfly Meadow",
                category = MapFilterCategory.Insect,
                worldPosition = new Vector3(-6f, 0f, -213f),
                description = "Flocks of fluttering orange monarchs. Equip Bug Net and sneak up slowly to catch.",
                iconEmoji = "🦋",
                pinColor = new Color(1f, 0.65f, 0.2f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "insect_golden_stag",
                name = "Golden Stag Tree Perch",
                category = MapFilterCategory.Insect,
                worldPosition = new Vector3(14f, 0f, -221f),
                description = "Extremely rare armored stag beetle with gleaming carapace. Requires master stealth to capture.",
                iconEmoji = "🪲",
                pinColor = new Color(1f, 0.85f, 0.25f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "insect_firefly_glade",
                name = "Glowing Firefly Glade",
                category = MapFilterCategory.Insect,
                worldPosition = new Vector3(0f, 0f, -215f),
                description = "Gentle glowing insects that illuminate the meadow at twilight. Keep in decorative bug cage.",
                iconEmoji = "✨",
                pinColor = new Color(0.6f, 1f, 0.3f)
            });

            // --- 6. 🎣 FISHING SPOTS ---
            allPOIs.Add(new WorldMapPOI
            {
                id = "fishing_whispering_pond",
                name = "Whispering Fishing Pond",
                category = MapFilterCategory.Fishing,
                worldPosition = new Vector3(18f, 0f, -210f),
                description = "Calm freshwater basin stocked with River Trout, Crucian Carp, and rare Golden Koi. Cast near shadow ripples.",
                iconEmoji = "🐟",
                pinColor = new Color(0.2f, 0.8f, 1f)
            });

            // --- 7. ⛏️ ORE VEINS ---
            allPOIs.Add(new WorldMapPOI
            {
                id = "ore_iron_vein",
                name = "Iron Ore Vein",
                category = MapFilterCategory.Ore,
                worldPosition = new Vector3(20f, 0f, -20f),
                description = "Sturdy mineral deposit. Chisel with Pickaxe or strike with weapon to yield raw iron chunks.",
                iconEmoji = "⛏️",
                pinColor = new Color(0.8f, 0.75f, 0.7f)
            });

            allPOIs.Add(new WorldMapPOI
            {
                id = "ore_sapphire_monolith",
                name = "Sapphire Crystal Monolith",
                category = MapFilterCategory.Ore,
                worldPosition = new Vector3(38f, 0f, -12f),
                description = "Gleaming gemstone formation resonating with astral mana. Yields cut sapphire gems.",
                iconEmoji = "💎",
                pinColor = new Color(0.25f, 0.65f, 1f)
            });
        }

        private void TrackPlayerExploration()
        {
            if (playerTransform == null) FindPlayer();
            if (playerTransform == null) return;

            Vector3 pos = playerTransform.position;
            Vector2Int currentCell = new Vector2Int(
                Mathf.RoundToInt(pos.x / GRID_CELL_SIZE),
                Mathf.RoundToInt(pos.z / GRID_CELL_SIZE)
            );

            // Add surrounding cells in discovery radius
            int cellRadius = Mathf.CeilToInt(DISCOVERY_RADIUS / GRID_CELL_SIZE);
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dz = -cellRadius; dz <= cellRadius; dz++)
                {
                    Vector2Int cell = new Vector2Int(currentCell.x + dx, currentCell.y + dz);
                    Vector3 cellWorld = new Vector3(cell.x * GRID_CELL_SIZE, 0f, cell.y * GRID_CELL_SIZE);
                    if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), cellWorld) <= DISCOVERY_RADIUS)
                    {
                        exploredGridCells.Add(cell);
                    }
                }
            }

            // Check POIs discovery
            for (int i = 0; i < allPOIs.Count; i++)
            {
                var poi = allPOIs[i];
                if (!poi.isDiscovered)
                {
                    float dist = Vector3.Distance(pos, poi.worldPosition);
                    if (dist <= DISCOVERY_RADIUS)
                    {
                        poi.isDiscovered = true;
                        Debug.Log($"🗺️ [MAP DISCOVERY] Discovered new POI: {poi.name} ({poi.category})!");
                    }
                }
            }
        }

        private float CalculateExploredPercentage()
        {
            // Total estimated cells across overworld region ~ 75
            float percent = Mathf.Clamp((exploredGridCells.Count / 65f) * 100f, 15f, 100f);
            return percent;
        }
        #endregion

        #region Modal Controls
        public void ToggleModal()
        {
            if (IsOpen) CloseModal();
            else OpenModal();
        }

        public void OpenModal()
        {
            EnsureUIBuilt();
            FindPlayer();
            if (allPOIs.Count == 0) InitializePOIDatabase();
            TrackPlayerExploration();

            if (modalRoot != null)
            {
                modalRoot.transform.SetAsLastSibling();
                modalRoot.SetActive(true);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
            }

            float percent = CalculateExploredPercentage();
            if (subtitleTMP != null)
            {
                subtitleTMP.text = $"<b>SECTOR: SOLAS OVERWORLD</b>  •  EXPLORED: <color=#FFD700>{percent:F0}%</color>  •  TACTICAL GRID";
            }

            // Default to first discovered settlement or player location
            if (selectedPOI == null && allPOIs.Count > 0)
            {
                selectedPOI = allPOIs.Find(p => p.id == "settlement_solas_plaza") ?? allPOIs[0];
            }

            UpdateInspectCard(selectedPOI);
            RefreshMapPins();
            UpdatePlayerBlipOnMap();

            StartCoroutine(FadeModalRoutine(true));
        }

        public void CloseModal(bool immediate = false)
        {
            if (modalRoot == null) return;

            if (immediate)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                modalRoot.SetActive(false);
            }
            else
            {
                StartCoroutine(FadeModalRoutine(false));
            }
        }

        private IEnumerator FadeModalRoutine(bool fadeIn)
        {
            if (canvasGroup == null)
            {
                modalRoot.SetActive(fadeIn);
                yield break;
            }

            float duration = 0.15f;
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            float targetAlpha = fadeIn ? 1f : 0f;

            if (fadeIn) modalRoot.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            if (!fadeIn) modalRoot.SetActive(false);
        }
        #endregion

        #region Fast Travel Execution
        public void ExecuteFastTravel(WorldMapPOI targetSettlement)
        {
            if (targetSettlement == null || !targetSettlement.isSettlement) return;

            if (!targetSettlement.isDiscovered)
            {
                if (PlayerHUDController.Instance != null)
                {
                    PlayerHUDController.Instance.ShowToast("⚠️ Settlement not yet discovered on foot!");
                }
                return;
            }

            FindPlayer();
            if (playerTransform == null) return;

            Vector3 dest = targetSettlement.worldPosition + Vector3.up * 0.95f;

            // Safe CharacterController teleportation
            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = dest;

            if (cc != null) cc.enabled = true;

            // Audio & VFX
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("portal_hum", 1.0f);
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.75f);
            }

            // Notification Broadcast
            if (PlayerHUDController.Instance != null)
            {
                PlayerHUDController.Instance.ShowToast($"⚡ <color=#FFD700>Fast traveled to {targetSettlement.name}!</color>", 3.0f);
            }

            Debug.Log($"🚀 [FAST TRAVEL] Teleported Player_Hero to '{targetSettlement.name}' at {dest}!");

            // Close map after teleport
            CloseModal();
        }
        #endregion

        #region UI Construction & Refresh
        public void SetFilterCategory(MapFilterCategory category)
        {
            activeFilter = category;
            HighlightActiveFilterButton();
            RefreshMapPins();
        }

        private void HighlightActiveFilterButton()
        {
            foreach (var kvp in filterButtons)
            {
                var btn = kvp.Value;
                var img = btn.GetComponent<Image>();
                bool isActive = (kvp.Key == activeFilter);

                if (img != null)
                {
                    img.color = isActive 
                        ? new Color(0.92f, 0.72f, 0.22f, 0.95f) // Vibrant gold
                        : new Color(0.12f, 0.16f, 0.24f, 0.85f); // Deep tactical navy
                }

                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.color = isActive ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.9f, 0.92f, 0.95f);
                }
            }
        }

        private void RefreshMapPins()
        {
            // Clean up existing pins
            for (int i = 0; i < activePinObjects.Count; i++)
            {
                if (activePinObjects[i] != null) Destroy(activePinObjects[i]);
            }
            activePinObjects.Clear();

            if (pinsContainer == null) return;

            for (int i = 0; i < allPOIs.Count; i++)
            {
                var poi = allPOIs[i];

                // Filter check
                if (activeFilter != MapFilterCategory.All && poi.category != activeFilter) continue;

                // Create pin GameObject
                GameObject pinGo = new GameObject($"Pin_{poi.id}", typeof(RectTransform), typeof(Image), typeof(Button));
                pinGo.transform.SetParent(pinsContainer, false);
                activePinObjects.Add(pinGo);

                var rt = pinGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = poi.isSettlement ? new Vector2(34f, 34f) : new Vector2(26f, 26f);
                rt.anchoredPosition = WorldToMapPosition(poi.worldPosition);

                var img = pinGo.GetComponent<Image>();
                img.color = poi.isDiscovered 
                    ? (poi.isSettlement ? new Color(1f, 0.85f, 0.25f, 0.95f) : poi.pinColor)
                    : new Color(0.4f, 0.45f, 0.55f, 0.5f); // Dimmed unknown blip

                if (poi.isSettlement)
                {
                    var pOutline = pinGo.AddComponent<Outline>();
                    pOutline.effectColor = Color.white;
                    pOutline.effectDistance = new Vector2(1.5f, 1.5f);
                }

                // Inner Alphanumeric Badge Label
                var labelGo = new GameObject("Badge", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(pinGo.transform, false);
                var lRt = labelGo.GetComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero;
                lRt.anchorMax = Vector2.one;
                lRt.offsetMin = Vector2.zero;
                lRt.offsetMax = Vector2.zero;

                var lTmp = labelGo.GetComponent<TextMeshProUGUI>();
                lTmp.text = poi.isDiscovered ? poi.GetBadge() : "?";
                lTmp.fontStyle = FontStyles.Bold;
                lTmp.fontSize = poi.isSettlement ? 16 : 13;
                lTmp.alignment = TextAlignmentOptions.Center;
                lTmp.color = poi.isSettlement ? new Color(0.08f, 0.08f, 0.12f) : Color.white;

                // Click listener
                var btn = pinGo.GetComponent<Button>();
                var targetPoi = poi;
                btn.onClick.AddListener(() =>
                {
                    selectedPOI = targetPoi;
                    UpdateInspectCard(targetPoi);
                    if (BattleSummonerAudioMaster.Instance != null)
                    {
                        BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.75f);
                    }
                });
            }
        }

        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            float normX = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
            float normZ = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.z);

            float mapX = (normX - 0.5f) * mapCanvasSize.x;
            float mapY = (normZ - 0.5f) * mapCanvasSize.y;

            return new Vector2(mapX, mapY);
        }

        private void UpdatePlayerBlipOnMap()
        {
            if (playerTransform == null) FindPlayer();
            if (playerTransform == null || playerBlipRect == null) return;

            playerBlipRect.anchoredPosition = WorldToMapPosition(playerTransform.position);
            playerBlipRect.localEulerAngles = new Vector3(0f, 0f, -playerTransform.eulerAngles.y);
        }

        private void UpdateInspectCard(WorldMapPOI poi)
        {
            if (poi == null) return;

            if (poiNameTMP != null)
            {
                poiNameTMP.text = $"<b>{poi.name}</b>";
            }

            if (poiCategoryCoordsTMP != null)
            {
                string catName = poi.category switch
                {
                    MapFilterCategory.Settlement => "SETTLEMENT / CAMP",
                    MapFilterCategory.Mushroom => "WILD FORAGING: MUSHROOM",
                    MapFilterCategory.Plant => "BOTANICAL: PLANT & HERB",
                    MapFilterCategory.Fruit => "ORCHARD: FRUIT & BERRIES",
                    MapFilterCategory.Insect => "ECOLOGY: WILD INSECTS",
                    MapFilterCategory.Fishing => "AQUATIC: FISHING POND",
                    MapFilterCategory.Ore => "MINING: ORE VEIN",
                    _ => "EXPLORATION POINT"
                };
                poiCategoryCoordsTMP.text = $"[{catName}]  •  LOC: [X: {poi.worldPosition.x:F0}, Z: {poi.worldPosition.z:F0}]";
            }

            if (poiDescriptionTMP != null)
            {
                poiDescriptionTMP.text = poi.description;
            }

            if (poiStatusTMP != null)
            {
                if (poi.isDiscovered)
                {
                    poiStatusTMP.text = "<color=#76FF03>[DISCOVERED]</color> Explored & Discovered";
                }
                else
                {
                    poiStatusTMP.text = "<color=#FF9100>[UNEXPLORED]</color> Visit on foot to reveal";
                }
            }

            // Fast Travel button is only active for discovered settlements!
            if (fastTravelButton != null)
            {
                bool canFastTravel = poi.isSettlement && poi.isDiscovered;
                fastTravelButton.gameObject.SetActive(canFastTravel);

                fastTravelButton.onClick.RemoveAllListeners();
                if (canFastTravel)
                {
                    fastTravelButton.onClick.AddListener(() => ExecuteFastTravel(poi));
                }
            }
        }
        #endregion

        #region Procedural Monster Hunter UI Assembly
        public void EnsureUIBuilt()
        {
            if (modalRoot != null)
            {
                modalRoot.transform.SetAsLastSibling();
                return;
            }

            // Locate Player HUD Canvas or Overworld HUD Canvas
            Canvas targetCanvas = null;
            var playerHud = FindAnyObjectByType<PlayerHUDController>();
            if (playerHud != null) targetCanvas = playerHud.GetComponentInParent<Canvas>();

            if (targetCanvas == null)
            {
                var overworldCanvas = GameObject.Find("OverworldHUDCanvas");
                if (overworldCanvas != null) targetCanvas = overworldCanvas.GetComponent<Canvas>();
            }

            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }

            if (targetCanvas == null) return;

            // 1. Modal Overlay Root (fullscreen darkened backdrop)
            modalRoot = new GameObject("WorldMapExplorationModalRoot", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            modalRoot.layer = 5; // UI
            modalRoot.transform.SetParent(targetCanvas.transform, false);

            var rootRt = modalRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var bgImg = modalRoot.GetComponent<Image>();
            bgImg.color = new Color(0.03f, 0.05f, 0.08f, 0.88f); // Tactical parchment vignette

            canvasGroup = modalRoot.GetComponent<CanvasGroup>();

            // 2. Tactical Monster Hunter Frame Container
            var frameGo = new GameObject("TacticalMapFrame", typeof(RectTransform), typeof(Image));
            frameGo.layer = 5;
            frameGo.transform.SetParent(modalRoot.transform, false);

            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(760f, 1140f); // Generous mobile portrait frame

            var frameImg = frameGo.GetComponent<Image>();
            frameImg.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            var frameOutline = frameGo.AddComponent<Outline>();
            frameOutline.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.85f); // Monster Hunter gold bezel
            frameOutline.effectDistance = new Vector2(3f, 3f);

            // 3. Top Header Bar
            var headerGo = new GameObject("HeaderBar", typeof(RectTransform));
            headerGo.transform.SetParent(frameGo.transform, false);
            var hRt = headerGo.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -14f);
            hRt.sizeDelta = new Vector2(-28f, 62f);

            // Header Title
            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(headerGo.transform, false);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.45f);
            tRt.anchorMax = new Vector2(0.82f, 1f);
            tRt.offsetMin = new Vector2(10f, 0f);
            tRt.offsetMax = Vector2.zero;

            titleTMP = titleGo.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "<b>WORLD EXPLORATION MAP</b>";
            titleTMP.fontSize = 24;
            titleTMP.color = new Color(1f, 0.92f, 0.65f);
            titleTMP.alignment = TextAlignmentOptions.Left;

            // Subtitle
            var subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGo.transform.SetParent(headerGo.transform, false);
            var sRt = subGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(0.82f, 0.45f);
            sRt.offsetMin = new Vector2(10f, 0f);
            sRt.offsetMax = Vector2.zero;

            subtitleTMP = subGo.GetComponent<TextMeshProUGUI>();
            subtitleTMP.text = "<b>SECTOR: SOLAS OVERWORLD</b>  •  EXPLORED: <color=#FFD700>25%</color>  •  TACTICAL GRID";
            subtitleTMP.fontSize = 13;
            subtitleTMP.color = new Color(0.7f, 0.75f, 0.85f);

            // Top-Right [X] Close Button
            var closeGo = new GameObject("CloseButton_X", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(headerGo.transform, false);
            var cRt = closeGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 0.5f);
            cRt.anchorMax = new Vector2(1f, 0.5f);
            cRt.pivot = new Vector2(1f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, 0f);
            cRt.sizeDelta = new Vector2(44f, 44f);

            var cImg = closeGo.GetComponent<Image>();
            cImg.color = new Color(0.75f, 0.15f, 0.18f, 0.95f); // Bold crimson close

            var cOutline = closeGo.AddComponent<Outline>();
            cOutline.effectColor = new Color(1f, 0.85f, 0.3f);
            cOutline.effectDistance = new Vector2(2f, 2f);

            var xLabelGo = new GameObject("XText", typeof(RectTransform), typeof(TextMeshProUGUI));
            xLabelGo.transform.SetParent(closeGo.transform, false);
            var xRt = xLabelGo.GetComponent<RectTransform>();
            xRt.anchorMin = Vector2.zero; xRt.anchorMax = Vector2.one;
            xRt.offsetMin = Vector2.zero; xRt.offsetMax = Vector2.zero;
            var xTmp = xLabelGo.GetComponent<TextMeshProUGUI>();
            xTmp.text = "X";
            xTmp.fontSize = 24;
            xTmp.fontStyle = FontStyles.Bold;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;

            closeButton = closeGo.GetComponent<Button>();
            closeButton.onClick.AddListener(() => CloseModal());

            // 4. Horizontal Category Filter Ribbon
            var filterRibbonGo = new GameObject("FilterRibbon", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            filterRibbonGo.transform.SetParent(frameGo.transform, false);
            filterBarContainer = filterRibbonGo.transform;

            var frRt = filterRibbonGo.GetComponent<RectTransform>();
            frRt.anchorMin = new Vector2(0f, 1f);
            frRt.anchorMax = new Vector2(1f, 1f);
            frRt.pivot = new Vector2(0.5f, 1f);
            frRt.anchoredPosition = new Vector2(0f, -86f);
            frRt.sizeDelta = new Vector2(-28f, 38f);

            var hlg = filterRibbonGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateFilterTab(MapFilterCategory.All, "ALL");
            CreateFilterTab(MapFilterCategory.Settlement, "CAMPS");
            CreateFilterTab(MapFilterCategory.Mushroom, "MUSH");
            CreateFilterTab(MapFilterCategory.Plant, "PLANTS");
            CreateFilterTab(MapFilterCategory.Fruit, "FRUIT");
            CreateFilterTab(MapFilterCategory.Insect, "BUGS");
            CreateFilterTab(MapFilterCategory.Fishing, "FISH");
            CreateFilterTab(MapFilterCategory.Ore, "MINING");

            // 5. Central Tactical Map Viewport
            var viewGo = new GameObject("MapViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewGo.transform.SetParent(frameGo.transform, false);
            mapViewportRect = viewGo.GetComponent<RectTransform>();
            mapViewportRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapViewportRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapViewportRect.pivot = new Vector2(0.5f, 0.5f);
            mapViewportRect.anchoredPosition = new Vector2(0f, 36f);
            mapViewportRect.sizeDelta = new Vector2(704f, 760f);

            var vImg = viewGo.GetComponent<Image>();
            vImg.color = new Color(0.04f, 0.06f, 0.09f, 1f); // Deep tactical canvas

            var vOutline = viewGo.AddComponent<Outline>();
            vOutline.effectColor = new Color(0.25f, 0.55f, 0.75f, 0.6f);
            vOutline.effectDistance = new Vector2(1.5f, 1.5f);

            // 6. Map Content & Terrain Visual Guide
            var contentGo = new GameObject("MapContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewGo.transform, false);
            mapContentRect = contentGo.GetComponent<RectTransform>();
            mapContentRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapContentRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapContentRect.pivot = new Vector2(0.5f, 0.5f);
            mapContentRect.sizeDelta = mapCanvasSize;
            mapContentRect.anchoredPosition = Vector2.zero;

            // Stylized terrain zones
            CreateMapZoneRect(contentGo.transform, "Zone_Town", new Vector2(-150f, -40f), new Vector2(260f, 240f), new Color(0.28f, 0.25f, 0.22f, 0.35f), "Solas Town Plaza");
            CreateMapZoneRect(contentGo.transform, "Zone_Forest", new Vector2(160f, 40f), new Vector2(240f, 320f), new Color(0.12f, 0.35f, 0.18f, 0.35f), "Mistwood Forest");
            CreateMapZoneRect(contentGo.transform, "Zone_Pond", new Vector2(110f, -220f), new Vector2(180f, 140f), new Color(0.12f, 0.45f, 0.75f, 0.35f), "Fishing Lake");
            CreateMapZoneRect(contentGo.transform, "Zone_Garden", new Vector2(-160f, -220f), new Vector2(160f, 140f), new Color(0.2f, 0.55f, 0.25f, 0.35f), "Community Garden");
            CreateMapZoneRect(contentGo.transform, "Zone_Spire", new Vector2(-150f, 220f), new Vector2(240f, 160f), new Color(0.35f, 0.15f, 0.55f, 0.35f), "Abyssal Spire");

            // Pins Container
            var pinsGo = new GameObject("PinsContainer", typeof(RectTransform));
            pinsGo.transform.SetParent(contentGo.transform, false);
            pinsContainer = pinsGo.transform;
            var pRt = pinsGo.GetComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero; pRt.anchorMax = Vector2.one;
            pRt.offsetMin = Vector2.zero; pRt.offsetMax = Vector2.zero;

            // Player Blip
            var blipGo = new GameObject("PlayerMapBlip", typeof(RectTransform), typeof(Image));
            blipGo.transform.SetParent(contentGo.transform, false);
            playerBlipRect = blipGo.GetComponent<RectTransform>();
            playerBlipRect.anchorMin = new Vector2(0.5f, 0.5f);
            playerBlipRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerBlipRect.pivot = new Vector2(0.5f, 0.5f);
            playerBlipRect.sizeDelta = new Vector2(22f, 22f);

            var bImg = blipGo.GetComponent<Image>();
            bImg.sprite = MinimapRadarController.Instance != null ? null : null;
            bImg.color = new Color(0.18f, 0.95f, 0.45f); // Glowing radiant green

            var bOutline = blipGo.AddComponent<Outline>();
            bOutline.effectColor = Color.white;
            bOutline.effectDistance = new Vector2(2f, 2f);

            // 7. Bottom Selected POI & Fast Travel Card
            var cardGo = new GameObject("InspectCard", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(frameGo.transform, false);
            inspectCardRect = cardGo.GetComponent<RectTransform>();
            inspectCardRect.anchorMin = new Vector2(0f, 0f);
            inspectCardRect.anchorMax = new Vector2(1f, 0f);
            inspectCardRect.pivot = new Vector2(0.5f, 0f);
            inspectCardRect.anchoredPosition = new Vector2(0f, 16f);
            inspectCardRect.sizeDelta = new Vector2(-28f, 175f);

            var cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0.10f, 0.14f, 0.20f, 0.98f); // Dark tactical card

            var cardOutline = cardGo.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.85f, 0.65f, 0.25f, 0.6f);
            cardOutline.effectDistance = new Vector2(1.5f, 1.5f);

            // POI Name
            var pNameGo = new GameObject("POIName", typeof(RectTransform), typeof(TextMeshProUGUI));
            pNameGo.transform.SetParent(cardGo.transform, false);
            var pnRt = pNameGo.GetComponent<RectTransform>();
            pnRt.anchorMin = new Vector2(0f, 1f); pnRt.anchorMax = new Vector2(0.68f, 1f);
            pnRt.pivot = new Vector2(0f, 1f);
            pnRt.anchoredPosition = new Vector2(18f, -14f);
            pnRt.sizeDelta = new Vector2(0f, 32f);
            poiNameTMP = pNameGo.GetComponent<TextMeshProUGUI>();
            poiNameTMP.fontSize = 22;
            poiNameTMP.color = new Color(1f, 0.95f, 0.75f);

            // POI Category & Coordinates
            var pCatGo = new GameObject("POICatCoords", typeof(RectTransform), typeof(TextMeshProUGUI));
            pCatGo.transform.SetParent(cardGo.transform, false);
            var pcRt = pCatGo.GetComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0f, 1f); pcRt.anchorMax = new Vector2(0.68f, 1f);
            pcRt.pivot = new Vector2(0f, 1f);
            pcRt.anchoredPosition = new Vector2(18f, -48f);
            pcRt.sizeDelta = new Vector2(0f, 22f);
            poiCategoryCoordsTMP = pCatGo.GetComponent<TextMeshProUGUI>();
            poiCategoryCoordsTMP.fontSize = 13;
            poiCategoryCoordsTMP.color = new Color(0.35f, 0.85f, 1f);

            // POI Description
            var pDescGo = new GameObject("POIDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            pDescGo.transform.SetParent(cardGo.transform, false);
            var pdRt = pDescGo.GetComponent<RectTransform>();
            pdRt.anchorMin = new Vector2(0f, 1f); pdRt.anchorMax = new Vector2(0.68f, 1f);
            pdRt.pivot = new Vector2(0f, 1f);
            pdRt.anchoredPosition = new Vector2(18f, -74f);
            pdRt.sizeDelta = new Vector2(0f, 65f);
            poiDescriptionTMP = pDescGo.GetComponent<TextMeshProUGUI>();
            poiDescriptionTMP.fontSize = 12;
            poiDescriptionTMP.color = new Color(0.85f, 0.88f, 0.92f);
            poiDescriptionTMP.textWrappingMode = TextWrappingModes.Normal;

            // Status indicator
            var pStatGo = new GameObject("POIStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            pStatGo.transform.SetParent(cardGo.transform, false);
            var psRt = pStatGo.GetComponent<RectTransform>();
            psRt.anchorMin = new Vector2(0f, 0f); psRt.anchorMax = new Vector2(0.68f, 0f);
            psRt.pivot = new Vector2(0f, 0f);
            psRt.anchoredPosition = new Vector2(18f, 10f);
            psRt.sizeDelta = new Vector2(0f, 22f);
            poiStatusTMP = pStatGo.GetComponent<TextMeshProUGUI>();
            poiStatusTMP.fontSize = 12;

            // FAST TRAVEL BUTTON (Monster Hunter style camp warp)
            var ftGo = new GameObject("FastTravelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            ftGo.transform.SetParent(cardGo.transform, false);
            var ftRt = ftGo.GetComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(1f, 0.5f);
            ftRt.anchorMax = new Vector2(1f, 0.5f);
            ftRt.pivot = new Vector2(1f, 0.5f);
            ftRt.anchoredPosition = new Vector2(-18f, 0f);
            ftRt.sizeDelta = new Vector2(180f, 85f);

            var ftImg = ftGo.GetComponent<Image>();
            ftImg.color = new Color(0.92f, 0.68f, 0.15f, 0.95f); // Glowing fast travel amber

            var ftOutline = ftGo.AddComponent<Outline>();
            ftOutline.effectColor = new Color(1f, 0.95f, 0.6f);
            ftOutline.effectDistance = new Vector2(2f, 2f);

            var ftLabelGo = new GameObject("FastTravelLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            ftLabelGo.transform.SetParent(ftGo.transform, false);
            var ftlRt = ftLabelGo.GetComponent<RectTransform>();
            ftlRt.anchorMin = Vector2.zero; ftlRt.anchorMax = Vector2.one;
            ftlRt.offsetMin = Vector2.zero; ftlRt.offsetMax = Vector2.zero;

            fastTravelBtnLabelTMP = ftLabelGo.GetComponent<TextMeshProUGUI>();
            fastTravelBtnLabelTMP.text = "<b>FAST TRAVEL</b>\n<size=11>WARP TO CAMP</size>";
            fastTravelBtnLabelTMP.fontSize = 16;
            fastTravelBtnLabelTMP.alignment = TextAlignmentOptions.Center;
            fastTravelBtnLabelTMP.color = new Color(0.08f, 0.08f, 0.10f);

            fastTravelButton = ftGo.GetComponent<Button>();

            HighlightActiveFilterButton();
            Debug.Log("✨ [WORLD MAP MODAL] Successfully built procedural Monster Hunter World Exploration Map UI!");
        }

        private void CreateFilterTab(MapFilterCategory cat, string label)
        {
            var btnGo = new GameObject($"FilterBtn_{cat}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(filterBarContainer, false);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.85f);

            var labelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(btnGo.transform, false);
            var lRt = labelGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.92f, 0.95f);

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => SetFilterCategory(cat));
            filterButtons[cat] = btn;
        }

        private void CreateMapZoneRect(Transform parent, string name, Vector2 pos, Vector2 size, Color color, string label)
        {
            var zoneGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            zoneGo.transform.SetParent(parent, false);

            var rt = zoneGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = zoneGo.GetComponent<Image>();
            img.color = color;

            var textGo = new GameObject("ZoneLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(zoneGo.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<size=11><b>{label.ToUpper()}</b></size>";
            tmp.color = new Color(1f, 1f, 1f, 0.25f);
            tmp.alignment = TextAlignmentOptions.Center;
        }
        #endregion
    }
}
