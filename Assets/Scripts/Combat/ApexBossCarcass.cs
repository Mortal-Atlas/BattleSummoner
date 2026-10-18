using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// ApexShinyLootDrop represents a scattered "shiny tear/geode" drop that flies out
    /// when a Boss or Apex Nemesis is defeated, requiring proximity pickup like Monster Hunter.
    /// </summary>
    public class ApexShinyLootDrop : MonoBehaviour
    {
        public string itemId = "item_shiny_geode";
        public string displayName = "Luminescent Dragon Tear";
        public ItemCategory category = ItemCategory.Materials;
        public ItemRarity rarity = ItemRarity.Rare;
        public int quantity = 1;
        public int bonusGold = 120;

        [SerializeField] private float pickupRadius = 2.2f;
        [SerializeField] private GameObject promptCanvasObj;
        [SerializeField] private TextMeshProUGUI promptText;

        private Transform playerTransform;
        private bool isCollected = false;
        private float rotationSpeed = 90f;
        private Vector3 startPos;
        private float bobSpeed = 2.5f;

        private void Start()
        {
            startPos = transform.position;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null) playerTransform = prog.transform;
            }

            SetupPromptUI();
        }

        private void SetupPromptUI()
        {
            if (promptCanvasObj == null)
            {
                promptCanvasObj = new GameObject("ShinyPromptCanvas", typeof(Canvas));
                promptCanvasObj.transform.SetParent(transform, false);
                var canvas = promptCanvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = promptCanvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(250f, 60f);
                promptCanvasObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                promptCanvasObj.transform.localScale = Vector3.one * 0.01f;

                var textObj = new GameObject("PromptTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(promptCanvasObj.transform, false);
                var trt = textObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                promptText = textObj.GetComponent<TextMeshProUGUI>();
                promptText.alignment = TextAlignmentOptions.Center;
                promptText.fontSize = 20;
                promptText.color = new Color(1f, 0.9f, 0.3f);
                promptText.text = "✦ [TAP / E] PICK UP SHINY ✦";
            }
            promptCanvasObj.SetActive(false);
        }

        private void Update()
        {
            if (isCollected) return;

            // Gentle visual bob and rotation
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float bob = Mathf.Sin(Time.time * bobSpeed) * 0.08f;
            transform.position = new Vector3(transform.position.x, startPos.y + bob, transform.position.z);

            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= pickupRadius;

            if (promptCanvasObj != null)
            {
                promptCanvasObj.SetActive(inRange);
                if (inRange && Camera.main != null)
                {
                    promptCanvasObj.transform.rotation = Camera.main.transform.rotation;
                }
            }

            // Keyboard/touch pickup interaction
            if (inRange && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)))
            {
                CollectShiny();
            }
        }

        public void CollectShiny()
        {
            if (isCollected) return;
            isCollected = true;

            // 1. Add to inventory
            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.AddItem(new InventoryItemEntry
                {
                    itemId = itemId,
                    displayName = displayName,
                    category = category,
                    rarity = rarity,
                    quantity = quantity,
                    description = "A glittering teardrop crystallized from slain Spire apex beasts. Highly sought after by guild smiths."
                });

                if (bonusGold > 0)
                {
                    InventoryMenuUI.Instance.AddGold(bonusGold);
                }
            }

            // 2. Play audio
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayLootPickupChime();
            }

            // 3. Combat feedback floating text
            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnLootFeedback(transform.position, $"Shiny Collected: {displayName} (+{bonusGold}G)", new Color(0.3f, 1f, 0.7f));
            }

            Destroy(gameObject, 0.1f);
        }
    }

    /// <summary>
    /// ApexBossCarcass manages the Monster Hunter style carving mechanic for Bosses & Apex Nemeses:
    /// - Interactive carcass remains with towering prismatic light pillar
    /// - Multi-carve limit (typically 3 carves)
    /// - Interactive carving hold/tap sequence with radial/linear progress bar
    /// - Blade slicing SFX & celebratory fanfare
    /// - Grants rare boss materials directly into InventoryMenuUI
    /// - Carcass dissolution particle effect once fully harvested
    /// </summary>
    public class ApexBossCarcass : MonoBehaviour
    {
        public static ApexBossCarcass ActiveCarcass { get; private set; }

        [Header("Boss Identity")]
        public string bossName = "Aethelgard, Core Sentinel";
        public int maxCarves = 3;
        public int carvesRemaining = 3;
        public float carveDuration = 1.35f;
        public float interactionRange = 3.6f;

        [Header("Carve Material Tables")]
        [SerializeField] private List<InventoryItemEntry> carveDropTable = new List<InventoryItemEntry>();

        [Header("World Space UI")]
        [SerializeField] private GameObject worldPromptCanvas;
        [SerializeField] private TextMeshProUGUI promptHeaderTMP;
        [SerializeField] private Image carveProgressBar;
        [SerializeField] private GameObject carveProgressRoot;

        private Transform playerTransform;
        private bool isCarving = false;
        private float carveProgress = 0f;
        private bool isDissolving = false;

        public event Action<string, int> OnCarveSuccess;
        public event Action OnAllCarvesCompleted;

        private void Awake()
        {
            ActiveCarcass = this;
            carvesRemaining = maxCarves;
            InitializeDefaultDropTable();
        }

        private void Start()
        {
            ResolvePlayer();
            BuildVisualBeacon();
            BuildWorldSpacePromptUI();
            ScatterShinyGeodes();
        }

        private void ResolvePlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null) playerTransform = prog.transform;
            }
        }

        private void InitializeDefaultDropTable()
        {
            if (carveDropTable.Count > 0) return;

            carveDropTable.Add(new InventoryItemEntry
            {
                itemId = "boss_carve_horn",
                displayName = "Apex Sentinel Horn",
                category = ItemCategory.Materials,
                rarity = ItemRarity.Legendary,
                quantity = 1,
                goldValue = 450,
                description = "Dense obsidian horn infused with ancient Spire thermal glyphs. Used to forge mythical armaments.",
                iconTextureName = "Mockup_Acc_Fang.png"
            });

            carveDropTable.Add(new InventoryItemEntry
            {
                itemId = "boss_carve_core",
                displayName = "Prismatic Astral Core",
                category = ItemCategory.Materials,
                rarity = ItemRarity.Epic,
                quantity = 1,
                goldValue = 350,
                description = "A shimmering geometric core that pulses with harmonic energy.",
                iconTextureName = "Mockup_Acc_Orb.png"
            });

            carveDropTable.Add(new InventoryItemEntry
            {
                itemId = "boss_carve_scale",
                displayName = "Serrated Carapace Plate",
                category = ItemCategory.Materials,
                rarity = ItemRarity.Epic,
                quantity = 2,
                goldValue = 280,
                description = "Razor-sharp carapace that deflects physical blades.",
                iconTextureName = "Mockup_Acc_Kinetic.png"
            });
        }

        private void BuildVisualBeacon()
        {
            // Vertical light beam beacon rising into the sky
            GameObject beamObj = new GameObject("ApexLootBeaconLight");
            beamObj.transform.SetParent(transform, false);
            beamObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var l = beamObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.85f, 0.3f);
            l.range = 14f;
            l.intensity = 2.5f;

            // Sparkling pillar cylinder
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "PillarBeamMesh";
            pillar.transform.SetParent(beamObj.transform, false);
            pillar.transform.localScale = new Vector3(0.35f, 12f, 0.35f);
            pillar.transform.localPosition = new Vector3(0f, 6f, 0f);
            var col = pillar.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = pillar.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Sprites/Default"));
                rend.material.color = new Color(1.0f, 0.82f, 0.25f, 0.45f);
            }
        }

        private void BuildWorldSpacePromptUI()
        {
            if (worldPromptCanvas != null) return;

            worldPromptCanvas = new GameObject("CarcassCarveUI", typeof(Canvas));
            worldPromptCanvas.transform.SetParent(transform, false);
            var canvas = worldPromptCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = worldPromptCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 120f);
            worldPromptCanvas.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            worldPromptCanvas.transform.localScale = Vector3.one * 0.012f;

            // Background banner
            GameObject bgObj = new GameObject("PromptBG", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(worldPromptCanvas.transform, false);
            var bgrt = bgObj.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = Vector2.zero;
            bgrt.offsetMax = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f);

            // Header prompt text
            GameObject headerObj = new GameObject("PromptHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(worldPromptCanvas.transform, false);
            var hrt = headerObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0.45f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.offsetMin = new Vector2(10f, 0f);
            hrt.offsetMax = new Vector2(-10f, 0f);

            promptHeaderTMP = headerObj.GetComponent<TextMeshProUGUI>();
            promptHeaderTMP.alignment = TextAlignmentOptions.Center;
            promptHeaderTMP.fontSize = 20;
            promptHeaderTMP.fontStyle = FontStyles.Bold;
            promptHeaderTMP.color = new Color(1f, 0.85f, 0.3f);
            promptHeaderTMP.text = $"⚔️ [HOLD E / TAP TO CARVE]\n<size=80%><color=#AADDFF>CARVES REMAINING: {carvesRemaining}/{maxCarves}</color></size>";

            // Progress bar container
            carveProgressRoot = new GameObject("ProgressBarRoot", typeof(RectTransform), typeof(Image));
            carveProgressRoot.transform.SetParent(worldPromptCanvas.transform, false);
            var prt = carveProgressRoot.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.1f, 0.12f);
            prt.anchorMax = new Vector2(0.9f, 0.38f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            var prtImg = carveProgressRoot.GetComponent<Image>();
            prtImg.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

            // Progress bar fill
            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(carveProgressRoot.transform, false);
            var frt = fillObj.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(2f, 2f);
            frt.offsetMax = new Vector2(-2f, -2f);

            carveProgressBar = fillObj.GetComponent<Image>();
            carveProgressBar.color = new Color(1f, 0.75f, 0.15f);
            carveProgressBar.type = Image.Type.Filled;
            carveProgressBar.fillMethod = Image.FillMethod.Horizontal;
            carveProgressBar.fillAmount = 0f;

            worldPromptCanvas.SetActive(false);
        }

        private void ScatterShinyGeodes()
        {
            int count = UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                float angle = (i * (360f / count) + UnityEngine.Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
                float radius = UnityEngine.Random.Range(2.0f, 3.8f);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.2f, Mathf.Sin(angle) * radius);
                Vector3 spawnPos = transform.position + offset;

                GameObject shinyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shinyObj.name = $"ApexShinyGeode_{i + 1}";
                shinyObj.transform.position = spawnPos;
                shinyObj.transform.localScale = Vector3.one * 0.45f;

                var rend = shinyObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = new Material(Shader.Find("Sprites/Default"));
                    rend.material.color = new Color(0.2f, 0.9f, 1.0f, 0.95f);
                }

                shinyObj.AddComponent<ApexShinyLootDrop>();
            }
        }

        private void Update()
        {
            if (isDissolving) return;

            if (playerTransform == null)
            {
                ResolvePlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= interactionRange && carvesRemaining > 0;

            if (worldPromptCanvas != null)
            {
                worldPromptCanvas.SetActive(inRange || isCarving);
                if (Camera.main != null && (inRange || isCarving))
                {
                    worldPromptCanvas.transform.rotation = Camera.main.transform.rotation;
                }
            }

            if (!inRange && !isCarving)
            {
                carveProgress = 0f;
                if (carveProgressBar != null) carveProgressBar.fillAmount = 0f;
                return;
            }

            // Check if player is holding carve button (Key E or Space or UI tap)
            bool isInteracting = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

            if (isInteracting && carvesRemaining > 0)
            {
                if (!isCarving)
                {
                    isCarving = true;
                    if (BattleSummonerAudioMaster.Instance != null)
                    {
                        BattleSummonerAudioMaster.Instance.PlayMonsterHunterCarveSfx();
                    }
                }

                carveProgress += Time.deltaTime / carveDuration;
                if (carveProgressBar != null) carveProgressBar.fillAmount = Mathf.Clamp01(carveProgress);

                if (promptHeaderTMP != null)
                {
                    promptHeaderTMP.text = $"🔪 <b>CARVING APEX CARCASS...</b>\n<size=80%><color=#FFDD44>{Mathf.RoundToInt(carveProgress * 100f)}%</color></size>";
                }

                if (carveProgress >= 1.0f)
                {
                    CompleteCarve();
                }
            }
            else
            {
                // Released early: reset progress
                if (isCarving)
                {
                    isCarving = false;
                    carveProgress = 0f;
                    if (carveProgressBar != null) carveProgressBar.fillAmount = 0f;
                    UpdatePromptHeader();
                }
            }
        }

        private void UpdatePromptHeader()
        {
            if (promptHeaderTMP != null)
            {
                if (carvesRemaining > 0)
                {
                    promptHeaderTMP.text = $"⚔️ [HOLD E / TAP TO CARVE]\n<size=80%><color=#AADDFF>CARVES REMAINING: {carvesRemaining}/{maxCarves}</color></size>";
                }
                else
                {
                    promptHeaderTMP.text = $"✦ <b>CARCASS FULLY HARVESTED</b> ✦\n<size=80%><color=#88FF88>All materials extracted.</color></size>";
                }
            }
        }

        private void CompleteCarve()
        {
            isCarving = false;
            carveProgress = 0f;
            if (carveProgressBar != null) carveProgressBar.fillAmount = 0f;

            carvesRemaining--;

            // Pick drop from table
            if (carveDropTable == null || carveDropTable.Count == 0)
            {
                InitializeDefaultDropTable();
            }

            int dropIndex = Mathf.Clamp(maxCarves - carvesRemaining - 1, 0, Mathf.Max(0, carveDropTable.Count - 1));
            InventoryItemEntry drop = (carveDropTable != null && carveDropTable.Count > 0) ? carveDropTable[dropIndex] : new InventoryItemEntry
            {
                itemId = "boss_carve_horn",
                displayName = "Apex Sentinel Horn",
                category = ItemCategory.Materials,
                rarity = ItemRarity.Legendary,
                quantity = 1
            };

            // 1. Add to inventory
            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.AddItem(drop);
            }

            // 2. Play celebratory fanfare
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayCarveFanfare();
            }

            // 3. Floating 3D toast feedback
            if (CombatFeedbackManager.Instance != null)
            {
                Color rarityColor = drop.GetRarityColor();
                CombatFeedbackManager.Instance.SpawnLootFeedback(transform.position, $"CARVE: {drop.displayName} x{drop.quantity}", rarityColor);
            }

            OnCarveSuccess?.Invoke(drop.displayName, carvesRemaining);
            UpdatePromptHeader();

            if (carvesRemaining <= 0)
            {
                StartCoroutine(DissolveAndDestroyRoutine());
            }
        }

        private IEnumerator DissolveAndDestroyRoutine()
        {
            isDissolving = true;
            OnAllCarvesCompleted?.Invoke();

            if (promptHeaderTMP != null)
            {
                promptHeaderTMP.text = "✦ <b>CARCASS DISSIPATING...</b> ✦\n<size=80%><color=#FFDD66>Ascending into astral mana dust.</color></size>";
            }

            yield return new WaitForSeconds(3.5f);

            // Smooth shrink fade
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1.5f;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t * t);
                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Static spawner to convert any defeated boss or apex entity into a carveable carcass.
        /// </summary>
        public static ApexBossCarcass SpawnCarcass(Vector3 position, Quaternion rotation, string bossName, List<InventoryItemEntry> customDrops = null)
        {
            GameObject carcassObj = new GameObject($"ApexCarcass_{bossName}");
            carcassObj.transform.position = position;
            carcassObj.transform.rotation = rotation;

            // Fallen beast carcass mesh representation
            GameObject meshObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshObj.name = "FallenCarcassBody";
            meshObj.transform.SetParent(carcassObj.transform, false);
            meshObj.transform.localScale = new Vector3(2.4f, 1.1f, 3.2f);
            meshObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            var rend = meshObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
                rend.material.color = new Color(0.15f, 0.12f, 0.14f);
            }

            var carcass = carcassObj.AddComponent<ApexBossCarcass>();
            ActiveCarcass = carcass;
            carcass.bossName = bossName;
            if (customDrops != null && customDrops.Count > 0)
            {
                carcass.carveDropTable = new List<InventoryItemEntry>(customDrops);
            }

            Debug.Log($"⚔️ [MONSTER HUNTER CARCASS] Spawned interactive carcass for {bossName} at {position} with 3 carves!");
            return carcass;
        }
    }
}
