using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Quests;

namespace BattleSummoner.Quests
{
    /// <summary>
    /// BTeamExpeditionDirector manages the physical in-dungeon presence of the reserve AI squad (B-Team):
    /// - Spawns and controls the physical B-Team convoy hauling a heavy ore cart across an elevated bridge above the player.
    /// - Features rotating ore cart wheels, luminescent raw crystal ores, mining gear, and tow ropes.
    /// - Displays 3D world overhead banners, speech bubble barks, and ambient mining animations.
    /// - Provides a proximity War-Horn prompt when near the squad.
    /// - When the War-Horn is blown, merges the B-Team into an overwhelming 18-person blitz army
    ///   at the cost of forfeiting the side-contract expedition reward!
    /// </summary>
    public class BTeamExpeditionDirector : MonoBehaviour
    {
        private static BTeamExpeditionDirector _instance;
        public static BTeamExpeditionDirector Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<BTeamExpeditionDirector>();
                    if (_instance == null)
                    {
                        var go = new GameObject("BTeamExpeditionDirector");
                        _instance = go.AddComponent<BTeamExpeditionDirector>();
                    }
                }
                return _instance;
            }
        }

        [Header("Scene Convoy Root & Bridge")]
        [SerializeField] private GameObject bridgeRootObj;
        [SerializeField] private GameObject convoyRootObj;
        [SerializeField] private Transform oreCartTransform;
        [SerializeField] private Transform[] wheelTransforms = new Transform[4];

        [Header("B-Team NPC Members (Units 10-18)")]
        [SerializeField] private List<GameObject> squadMemberObjs = new List<GameObject>();
        [SerializeField] private Transform leadHaulerTransform;
        [SerializeField] private Transform veinMinerTransform;
        [SerializeField] private Transform vanguardGuardTransform;
        [SerializeField] private Transform arcaneScoutTransform;
        [SerializeField] private Transform rearGuardTransform;

        [Header("Waypoints & Pathing")]
        [SerializeField] private Vector3[] bridgeWaypoints;
        [SerializeField] private float marchSpeed = 2.2f;
        [SerializeField] private float currentPathT = 0f;
        [SerializeField] private int pathDirection = 1;

        [Header("Proximity & War-Horn UI")]
        [SerializeField] private float warHornInteractionRange = 30f;
        [SerializeField] private GameObject warHornPromptCanvasObj;
        [SerializeField] private Button warHornTriggerBtn;
        [SerializeField] private TextMeshProUGUI warHornPromptTMP;

        [Header("Ambient Dialogue Barks")]
        [SerializeField] private GameObject overheadBadgeGo;
        [SerializeField] private TextMeshProUGUI speechBubbleTMP;
        [SerializeField] private GameObject speechBubbleRoot;
        private float speechTimer = 0f;
        private int speechIndex = 0;

        private static readonly string[] BARK_LINES = new string[]
        {
            "Heave-ho! Keep that ore cart moving across the bridge!",
            "Look down there—the Captain is clearing the dungeon floor!",
            "Vein cleared! 10 chunks of pure titan-iron in the cart!",
            "Stay sharp! Sound the War-Horn if you need our steel down there!",
            "Keith's forge will be ecstatic with these luminous crystals!",
            "Watch your footing on the archway, recruits!"
        };

        private bool isBlitzMerged = false;
        private bool isConvoySpawned = false;
        private float wheelAngle = 0f;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            SubscribeToQuestEvents();
            CheckAndInitializeConvoy();
        }

        private void OnDestroy()
        {
            UnsubscribeFromQuestEvents();
        }

        private void SubscribeToQuestEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnBTeamDispatched += HandleBTeamDispatched;
                QuestManager.Instance.OnWarHornBlitzTriggered += HandleWarHornBlitz;
                QuestManager.Instance.OnBTeamExpeditionCompleted += HandleExpeditionCompleted;
            }
        }

        private void UnsubscribeFromQuestEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnBTeamDispatched -= HandleBTeamDispatched;
                QuestManager.Instance.OnWarHornBlitzTriggered -= HandleWarHornBlitz;
                QuestManager.Instance.OnBTeamExpeditionCompleted -= HandleExpeditionCompleted;
            }
        }

        private void Update()
        {
            // Keyboard shortcut for War-Horn
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (CanTriggerWarHorn())
                {
                    TriggerWarHornMerge();
                }
            }

            if (!isConvoySpawned)
            {
                CheckAndInitializeConvoy();
                return;
            }

            if (!isBlitzMerged)
            {
                UpdateConvoyMarch();
                UpdateAmbientSpeech();
                UpdateWarHornProximity();
            }
            else
            {
                UpdateBlitzCombatAI();
            }
        }

        public void CheckAndInitializeConvoy()
        {
            if (QuestManager.Instance == null) return;
            Quest activeB = QuestManager.Instance.ActiveBTeamQuest;

            if (activeB != null && !isConvoySpawned)
            {
                SpawnInWorldConvoy(activeB);
            }
        }

        private void HandleBTeamDispatched(Quest q)
        {
            isBlitzMerged = false;
            SpawnInWorldConvoy(q);
        }

        private void HandleWarHornBlitz(Quest q)
        {
            ExecuteWarHornMergeSequence();
        }

        private void HandleExpeditionCompleted(Quest q)
        {
            if (!isBlitzMerged && convoyRootObj != null)
            {
                // Play celebration bark before retiring
                if (speechBubbleTMP != null && speechBubbleRoot != null)
                {
                    speechBubbleRoot.SetActive(true);
                    speechBubbleTMP.text = "★ Haul complete! Heading back to the Guild Hall!";
                }
            }
        }

        #region Physical Convoy & Bridge Spawning
        public void SpawnInWorldConvoy(Quest q)
        {
            if (isConvoySpawned && convoyRootObj != null)
            {
                convoyRootObj.SetActive(true);
                return;
            }

            EnsureElevatedBridgeExists();
            BuildConvoyHierarchy(q);
            BuildWarHornProximityUI();

            isConvoySpawned = true;
            isBlitzMerged = q.isBlitzMerged;
            Debug.Log($"📯 [B-TEAM DIRECTOR] In-Dungeon physical B-Team convoy spawned on elevated bridge for '{q.title}'!");
        }

        private void EnsureElevatedBridgeExists()
        {
            bridgeRootObj = GameObject.Find("BTeam_ElevatedStoneBridge");
            if (bridgeRootObj != null) return;

            bridgeRootObj = new GameObject("BTeam_ElevatedStoneBridge");
            bridgeRootObj.transform.position = new Vector3(0f, 6.2f, 8f);

            // Procedural stone masonry bridge deck & piers
            Material stoneMat = null;
#if UNITY_EDITOR
            stoneMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_DungeonStone.mat");
            if (stoneMat == null) stoneMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_Cobblestone.mat");
#endif

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "BridgeDeck";
            deck.transform.SetParent(bridgeRootObj.transform, false);
            deck.transform.localPosition = Vector3.zero;
            deck.transform.localScale = new Vector3(36f, 0.8f, 5.0f);
            if (stoneMat != null) deck.GetComponent<Renderer>().material = stoneMat;
            else deck.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.38f);

            // Parapets North and South (low protective masonry railings)
            GameObject parapetN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parapetN.name = "Parapet_North";
            parapetN.transform.SetParent(bridgeRootObj.transform, false);
            parapetN.transform.localPosition = new Vector3(0f, 0.6f, 2.3f);
            parapetN.transform.localScale = new Vector3(36f, 0.6f, 0.4f);
            if (stoneMat != null) parapetN.GetComponent<Renderer>().material = stoneMat;
            else parapetN.GetComponent<Renderer>().material.color = new Color(0.28f, 0.28f, 0.30f);

            GameObject parapetS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parapetS.name = "Parapet_South";
            parapetS.transform.SetParent(bridgeRootObj.transform, false);
            parapetS.transform.localPosition = new Vector3(0f, 0.6f, -2.3f);
            parapetS.transform.localScale = new Vector3(36f, 0.6f, 0.4f);
            if (stoneMat != null) parapetS.GetComponent<Renderer>().material = stoneMat;
            else parapetS.GetComponent<Renderer>().material.color = new Color(0.28f, 0.28f, 0.30f);

            // Massive stone support pillars extending to the arena floor
            float[] pierX = { -12f, 0f, 12f };
            for (int i = 0; i < pierX.Length; i++)
            {
                GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pier.name = $"SupportPier_{i}";
                pier.transform.SetParent(bridgeRootObj.transform, false);
                pier.transform.localPosition = new Vector3(pierX[i], -3.1f, 0f);
                pier.transform.localScale = new Vector3(2.5f, 3.1f, 2.5f);
                if (stoneMat != null) pier.GetComponent<Renderer>().material = stoneMat;
                else pier.GetComponent<Renderer>().material.color = new Color(0.22f, 0.22f, 0.25f);
            }

            // Bridge path waypoints (spanning from West edge to East edge of elevated bridge)
            bridgeWaypoints = new Vector3[]
            {
                new Vector3(-16f, 6.6f, 8f),
                new Vector3(-8f, 6.6f, 8f),
                new Vector3(0f, 6.6f, 8f),
                new Vector3(8f, 6.6f, 8f),
                new Vector3(16f, 6.6f, 8f)
            };
        }

        private void BuildConvoyHierarchy(Quest q)
        {
            if (convoyRootObj != null) Destroy(convoyRootObj);
            squadMemberObjs.Clear();

            convoyRootObj = new GameObject("BTeam_ExpeditionConvoy");
            convoyRootObj.transform.position = bridgeWaypoints[0];

            // 1. BUILD THE ORE CART
            GameObject cartObj = new GameObject("BTeam_OreCart");
            cartObj.transform.SetParent(convoyRootObj.transform, false);
            cartObj.transform.localPosition = Vector3.zero;
            oreCartTransform = cartObj.transform;

            // Cart wooden body
            GameObject cartBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cartBody.name = "CartTimberBody";
            cartBody.transform.SetParent(cartObj.transform, false);
            cartBody.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            cartBody.transform.localScale = new Vector3(2.6f, 0.7f, 1.8f);

            Material woodMat = null;
#if UNITY_EDITOR
            woodMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_WoodPlank.mat");
            if (woodMat == null) woodMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_WoodBeam.mat");
#endif
            if (woodMat != null) cartBody.GetComponent<Renderer>().material = woodMat;
            else cartBody.GetComponent<Renderer>().material.color = new Color(0.42f, 0.28f, 0.16f); // Rustic Timber Oak

            // Cart Iron Trim
            GameObject ironTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ironTrim.name = "CartIronBrace";
            ironTrim.transform.SetParent(cartObj.transform, false);
            ironTrim.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            ironTrim.transform.localScale = new Vector3(2.65f, 0.75f, 0.2f);
            ironTrim.GetComponent<Renderer>().material.color = new Color(0.22f, 0.24f, 0.28f);

            // 4 Rotating Spoke Wheels
            Vector3[] wheelOffsets = new Vector3[]
            {
                new Vector3(-0.9f, 0.35f, 1.0f),
                new Vector3(0.9f, 0.35f, 1.0f),
                new Vector3(-0.9f, 0.35f, -1.0f),
                new Vector3(0.9f, 0.35f, -1.0f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"CartWheel_{i}";
                wheel.transform.SetParent(cartObj.transform, false);
                wheel.transform.localPosition = wheelOffsets[i];
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.7f, 0.12f, 0.7f);
                wheel.GetComponent<Renderer>().material.color = new Color(0.18f, 0.18f, 0.20f);
                wheelTransforms[i] = wheel.transform;
            }

            // Raw Luminescent Ore Chunks inside the cart
            Color[] oreColors = new Color[]
            {
                new Color(0.20f, 0.85f, 1.0f), // Luminous Cyan Spire Crystal
                new Color(1.0f, 0.78f, 0.20f), // Golden Titan-Iron Ingot
                new Color(0.85f, 0.25f, 0.95f), // Voidfire Amethyst Shard
                new Color(0.25f, 0.95f, 0.45f)  // Emerald Life Moss Clump
            };

            for (int i = 0; i < 6; i++)
            {
                GameObject ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ore.name = $"OreChunk_{i}";
                ore.transform.SetParent(cartObj.transform, false);
                float rx = UnityEngine.Random.Range(-0.8f, 0.8f);
                float rz = UnityEngine.Random.Range(-0.5f, 0.5f);
                ore.transform.localPosition = new Vector3(rx, 0.95f, rz);
                ore.transform.localRotation = UnityEngine.Random.rotation;
                ore.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                var ren = ore.GetComponent<Renderer>();
                ren.material.color = oreColors[i % oreColors.Length];
            }

            // Cart Point Light (Radiant Ore Glow)
            GameObject oreLightObj = new GameObject("OreLuminescenceLight", typeof(Light));
            oreLightObj.transform.SetParent(cartObj.transform, false);
            oreLightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Light oreLight = oreLightObj.GetComponent<Light>();
            oreLight.type = LightType.Point;
            oreLight.color = new Color(0.3f, 0.85f, 1.0f);
            oreLight.range = 8.5f;
            oreLight.intensity = 2.5f;

            // 2. SPAWN B-TEAM RESERVE SQUAD MEMBERS (Units 10-18)
            // Character roles and relative offsets around the cart
            Vector3 haulerOffset = new Vector3(2.5f, 0f, 0f);    // Lead Hauler pulling harness in front
            Vector3 minerOffset = new Vector3(0.5f, 0f, 1.4f);    // Vein Miner beside the cart
            Vector3 guardOffset = new Vector3(3.2f, 0f, 1.2f);    // Vanguard Defender scouting ahead
            Vector3 scoutOffset = new Vector3(-1.8f, 0f, 1.2f);   // Arcane Scout with lantern
            Vector3 rearOffset = new Vector3(-2.6f, 0f, -0.8f);   // Rear Escort Guard

            leadHaulerTransform = SpawnSquadMember("BTeam_LeadHauler", "Cart Puller (Unit 10)", haulerOffset, new Color(0.85f, 0.45f, 0.20f), "Assets/Prefabs/NPCs/NPC_Barnaby.prefab").transform;
            veinMinerTransform = SpawnSquadMember("BTeam_VeinMiner", "Vein Miner (Unit 11)", minerOffset, new Color(0.95f, 0.75f, 0.20f), "Assets/Prefabs/NPCs/NPC_Zephyr.prefab").transform;
            vanguardGuardTransform = SpawnSquadMember("BTeam_VanguardGuard", "Aegis Guard (Unit 12)", guardOffset, new Color(0.25f, 0.55f, 0.95f), "Assets/Prefabs/Characters/Character_Anime_Fitted_Equipped.prefab").transform;
            arcaneScoutTransform = SpawnSquadMember("BTeam_ArcaneScout", "Arcane Scout (Unit 13)", scoutOffset, new Color(0.75f, 0.35f, 0.95f), "Assets/Prefabs/NPCs/NPC_Margo.prefab").transform;
            rearGuardTransform = SpawnSquadMember("BTeam_RearGuard", "Trail Watch (Unit 14)", rearOffset, new Color(0.35f, 0.85f, 0.45f), "Assets/Prefabs/Characters/Character_Anime_Modular_Hero.prefab").transform;

            // Tow Ropes connecting Lead Hauler to Ore Cart
            CreateTowRope("TowRope_L", new Vector3(1.3f, 0.6f, 0.6f), leadHaulerTransform);
            CreateTowRope("TowRope_R", new Vector3(1.3f, 0.6f, -0.6f), leadHaulerTransform);

            // Overhead 3D World Space Badge on Convoy
            BuildOverheadWorldBadge(cartObj.transform, q);
        }

        private GameObject SpawnSquadMember(string name, string roleTitle, Vector3 localOffset, Color badgeColor, string preferredPrefabPath)
        {
            GameObject memberRoot = new GameObject(name);
            memberRoot.transform.SetParent(convoyRootObj.transform, false);
            memberRoot.transform.localPosition = localOffset;

            GameObject prefab = null;
#if UNITY_EDITOR
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(preferredPrefabPath);
#endif
            if (prefab != null)
            {
                GameObject visual = Instantiate(prefab, memberRoot.transform);
                visual.name = "AvatarVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                visual.transform.localScale = Vector3.one * 1.0f;
            }
            else
            {
                // Fallback procedural stylized adventurer mesh
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "AvatarBody";
                body.transform.SetParent(memberRoot.transform, false);
                body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                body.transform.localScale = new Vector3(0.7f, 1.0f, 0.7f);
                body.GetComponent<Renderer>().material.color = badgeColor;

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "AvatarHead";
                head.transform.SetParent(memberRoot.transform, false);
                head.transform.localPosition = new Vector3(0f, 2.1f, 0f);
                head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                head.GetComponent<Renderer>().material.color = new Color(0.98f, 0.88f, 0.80f);
            }

            // Role equipment props (pickaxe, shield, sword)
            if (roleTitle.Contains("Miner"))
            {
                GameObject pickaxe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pickaxe.name = "MiningPickaxe";
                pickaxe.transform.SetParent(memberRoot.transform, false);
                pickaxe.transform.localPosition = new Vector3(0.4f, 1.1f, 0.3f);
                pickaxe.transform.localRotation = Quaternion.Euler(30f, 45f, 0f);
                pickaxe.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);
                pickaxe.GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 0.55f);
            }
            else if (roleTitle.Contains("Guard"))
            {
                GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shield.name = "TowerShield";
                shield.transform.SetParent(memberRoot.transform, false);
                shield.transform.localPosition = new Vector3(-0.45f, 1.0f, 0.2f);
                shield.transform.localScale = new Vector3(0.15f, 1.1f, 0.65f);
                shield.GetComponent<Renderer>().material.color = new Color(0.20f, 0.45f, 0.75f);
            }

            squadMemberObjs.Add(memberRoot);
            return memberRoot;
        }

        private void CreateTowRope(string ropeName, Vector3 cartAttachPoint, Transform targetHauler)
        {
            GameObject rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rope.name = ropeName;
            rope.transform.SetParent(oreCartTransform, false);
            rope.transform.localPosition = cartAttachPoint + new Vector3(0.6f, 0.2f, 0f);
            rope.transform.localRotation = Quaternion.Euler(0f, 0f, 75f);
            rope.transform.localScale = new Vector3(0.04f, 0.65f, 0.04f);
            rope.GetComponent<Renderer>().material.color = new Color(0.55f, 0.42f, 0.28f);
        }

        private void BuildOverheadWorldBadge(Transform parent, Quest q)
        {
            GameObject badgeCanvasGo = new GameObject("OverheadBTeamBadgeCanvas", typeof(RectTransform), typeof(Canvas));
            overheadBadgeGo = badgeCanvasGo;
            badgeCanvasGo.transform.SetParent(parent, false);
            badgeCanvasGo.transform.localPosition = new Vector3(0f, 3.2f, 0f);

            Canvas canvas = badgeCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = badgeCanvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460f, 150f);
            rt.localScale = Vector3.one * 0.012f;

            // Background Card
            GameObject cardBg = new GameObject("BadgeBg", typeof(RectTransform), typeof(Image));
            cardBg.transform.SetParent(badgeCanvasGo.transform, false);
            RectTransform cRt = cardBg.GetComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.sizeDelta = Vector2.zero;
            cardBg.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 0.90f);

            // Overhead Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(cardBg.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.04f, 0.48f);
            tRt.anchorMax = new Vector2(0.96f, 0.95f);
            tRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "<b><color=#70C8FF>[B-TEAM RESERVE SQUAD 2]</color></b>\n<size=75%><color=#FFD700>Sub-Contract: Mining Ore Expedition</color></size>";
            titleTMP.fontSize = 20;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = Color.white;

            // Speech Bubble Container
            speechBubbleRoot = new GameObject("SpeechBubble", typeof(RectTransform), typeof(Image));
            speechBubbleRoot.transform.SetParent(cardBg.transform, false);
            RectTransform sbRt = speechBubbleRoot.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.05f, 0.05f);
            sbRt.anchorMax = new Vector2(0.95f, 0.45f);
            sbRt.sizeDelta = Vector2.zero;
            speechBubbleRoot.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f, 0.95f);

            GameObject bubbleTxtObj = new GameObject("SpeechText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bubbleTxtObj.transform.SetParent(speechBubbleRoot.transform, false);
            RectTransform btRt = bubbleTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;
            speechBubbleTMP = bubbleTxtObj.GetComponent<TextMeshProUGUI>();
            speechBubbleTMP.text = $"<i>\"{BARK_LINES[0]}\"</i>";
            speechBubbleTMP.fontSize = 17;
            speechBubbleTMP.alignment = TextAlignmentOptions.Center;
            speechBubbleTMP.color = new Color(1f, 0.92f, 0.65f);
        }
        #endregion

        #region Pathing & Movement
        private void UpdateConvoyMarch()
        {
            if (bridgeWaypoints == null || bridgeWaypoints.Length < 2 || convoyRootObj == null) return;

            // Advance interpolation along waypoints
            currentPathT += (marchSpeed / 32f) * Time.deltaTime * pathDirection;

            if (currentPathT >= 1f)
            {
                currentPathT = 1f;
                pathDirection = -1; // Turn around and haul back
                FlipConvoyFacing(180f);
            }
            else if (currentPathT <= 0f)
            {
                currentPathT = 0f;
                pathDirection = 1;
                FlipConvoyFacing(0f);
            }

            // Interpolate position across waypoints
            float scaledT = currentPathT * (bridgeWaypoints.Length - 1);
            int idx = Mathf.Clamp(Mathf.FloorToInt(scaledT), 0, bridgeWaypoints.Length - 2);
            float segT = scaledT - idx;

            Vector3 newPos = Vector3.Lerp(bridgeWaypoints[idx], bridgeWaypoints[idx + 1], segT);
            convoyRootObj.transform.position = newPos;

            // Rotate wheels realistically with movement
            wheelAngle += (marchSpeed * 120f) * Time.deltaTime * pathDirection;
            for (int i = 0; i < wheelTransforms.Length; i++)
            {
                if (wheelTransforms[i] != null)
                {
                    wheelTransforms[i].localRotation = Quaternion.Euler(90f, wheelAngle, 0f);
                }
            }

            // Billboard overhead badge towards camera
            if (overheadBadgeGo != null && Camera.main != null)
            {
                Vector3 toCam = overheadBadgeGo.transform.position - Camera.main.transform.position;
                if (toCam.sqrMagnitude > 0.001f)
                {
                    overheadBadgeGo.transform.rotation = Quaternion.LookRotation(toCam);
                }
            }
        }

        private void FlipConvoyFacing(float yAngle)
        {
            if (convoyRootObj != null)
            {
                convoyRootObj.transform.rotation = Quaternion.Euler(0f, yAngle, 0f);
            }
        }

        private void UpdateAmbientSpeech()
        {
            if (speechBubbleTMP == null || speechBubbleRoot == null) return;

            speechTimer += Time.deltaTime;
            if (speechTimer >= 7.5f)
            {
                speechTimer = 0f;
                speechIndex = (speechIndex + 1) % BARK_LINES.Length;
                speechBubbleTMP.text = $"<i>\"{BARK_LINES[speechIndex]}\"</i>";
            }
        }
        #endregion

        #region War-Horn Proximity & Blitz Merge
        private void BuildWarHornProximityUI()
        {
            if (warHornPromptCanvasObj != null) return;

            // Search for HUD Canvas
            Canvas hudCanvas = null;
            var masterHUD = GameObject.Find("PlayerHUDCanvas");
            if (masterHUD != null) hudCanvas = masterHUD.GetComponent<Canvas>();
            if (hudCanvas == null)
            {
                var portraitCanvas = GameObject.Find("PortraitHUDCanvas");
                if (portraitCanvas != null) hudCanvas = portraitCanvas.GetComponent<Canvas>();
            }

            if (hudCanvas == null) return;

            // Prompt Root Button: Center-Right above action buttons
            warHornPromptCanvasObj = new GameObject("WarHornProximityPrompt", typeof(RectTransform), typeof(Image), typeof(Button));
            warHornPromptCanvasObj.transform.SetParent(hudCanvas.transform, false);

            RectTransform rt = warHornPromptCanvasObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.62f);
            rt.anchorMax = new Vector2(0.5f, 0.62f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 115f);
            rt.anchoredPosition = Vector2.zero;

            Image img = warHornPromptCanvasObj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);

            warHornTriggerBtn = warHornPromptCanvasObj.GetComponent<Button>();
            warHornTriggerBtn.onClick.AddListener(TriggerWarHornMerge);

            // Glowing golden border
            GameObject border = GameObject.CreatePrimitive(PrimitiveType.Quad);
            // UI text inside prompt
            GameObject txtObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(warHornPromptCanvasObj.transform, false);
            RectTransform tr = txtObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            warHornPromptTMP = txtObj.GetComponent<TextMeshProUGUI>();
            warHornPromptTMP.text = "<b><color=#FFD700>[BLOW WAR-HORN]</color> (Press 'H' or Tap)</b>\n<size=82%><color=#FFF0A0>MERGE B-TEAM FOR 18-PERSON BLITZ!</color></size>\n<size=68%><color=#FFA080>[CAUTION] Forfeits side-contract ore haul reward!</color></size>";
            warHornPromptTMP.fontSize = 17;
            warHornPromptTMP.alignment = TextAlignmentOptions.Center;
            warHornPromptTMP.color = Color.white;

            warHornPromptCanvasObj.SetActive(false);
        }

        private bool CanTriggerWarHorn()
        {
            if (isBlitzMerged || !isConvoySpawned) return false;
            if (QuestManager.Instance == null) return false;
            Quest activeB = QuestManager.Instance.ActiveBTeamQuest;
            return activeB != null && !activeB.isBlitzMerged;
        }

        private void UpdateWarHornProximity()
        {
            if (isBlitzMerged || warHornPromptCanvasObj == null) return;

            Transform playerTr = GetPlayerTransform();
            if (playerTr == null || convoyRootObj == null)
            {
                warHornPromptCanvasObj.SetActive(false);
                return;
            }

            float dist = Vector3.Distance(playerTr.position, convoyRootObj.transform.position);
            bool inRange = dist <= warHornInteractionRange && CanTriggerWarHorn();

            if (warHornPromptCanvasObj.activeSelf != inRange)
            {
                warHornPromptCanvasObj.SetActive(inRange);
            }
        }

        private Transform GetPlayerTransform()
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) return p.transform;
            var hero = GameObject.Find("Player_Hero");
            if (hero != null) return hero.transform;
            if (Camera.main != null) return Camera.main.transform;
            return null;
        }

        public void TriggerWarHornMerge()
        {
            if (!CanTriggerWarHorn()) return;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.SoundWarHornBlitz();
            }
        }

        private void ExecuteWarHornMergeSequence()
        {
            isBlitzMerged = true;
            if (warHornPromptCanvasObj != null) warHornPromptCanvasObj.SetActive(false);

            // 1. Play Fanfare Audio & Screen Shake
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("hunting_horn_fanfare", 1.0f);
            }

            // 2. Drop Ore Cart in place on the bridge
            if (oreCartTransform != null)
            {
                // Disconnect tow ropes
                Transform ropeL = oreCartTransform.Find("TowRope_L");
                if (ropeL != null) ropeL.gameObject.SetActive(false);
                Transform ropeR = oreCartTransform.Find("TowRope_R");
                if (ropeR != null) ropeR.gameObject.SetActive(false);
            }

            // 3. Speech bubble combat battle cry
            if (speechBubbleTMP != null && speechBubbleRoot != null)
            {
                speechBubbleRoot.SetActive(true);
                speechBubbleTMP.text = "⚔️ <b>WAR-HORN HEARD! DROP THE CARTS! TO THE CAPTAIN'S SIDE!</b>";
            }

            // 4. B-Team squad leaps down to player / main floor with weapons drawn
            if (Application.isPlaying)
            {
                StartCoroutine(RoutineRallySquadToPlayer());
            }
            else
            {
                RallySquadToPlayerInstant();
            }

            // 5. Update Party Targeting HUD for 18-Person Blitz mode
            if (BattleSummoner.UI.AllyPartyTargetingController.Instance != null)
            {
                BattleSummoner.UI.AllyPartyTargetingController.Instance.Activate18PersonBlitzUI();
            }
        }

        private void RallySquadToPlayerInstant()
        {
            Transform playerTr = GetPlayerTransform();
            Vector3 targetCenter = playerTr != null ? playerTr.position : Vector3.zero;

            for (int i = 0; i < squadMemberObjs.Count; i++)
            {
                if (squadMemberObjs[i] == null) continue;
                GameObject member = squadMemberObjs[i];

                member.transform.SetParent(null, true);

                GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Quad);
                aura.name = "FrenzyBlitzAura";
                aura.transform.SetParent(member.transform, false);
                aura.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                aura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                aura.transform.localScale = Vector3.one * 2.2f;
                aura.GetComponent<Renderer>().material.color = new Color(1.0f, 0.35f, 0.15f, 0.85f);

                float angle = (i / (float)squadMemberObjs.Count) * Mathf.PI * 2f;
                Vector3 destPos = targetCenter + new Vector3(Mathf.Cos(angle) * 4.5f, 0f, Mathf.Sin(angle) * 4.5f);
                member.transform.position = destPos;
            }
        }

        private IEnumerator RoutineRallySquadToPlayer()
        {
            Transform playerTr = GetPlayerTransform();
            Vector3 targetCenter = playerTr != null ? playerTr.position : Vector3.zero;

            // Form combat circle around player (radius 4.5m)
            for (int i = 0; i < squadMemberObjs.Count; i++)
            {
                if (squadMemberObjs[i] == null) continue;
                GameObject member = squadMemberObjs[i];

                // Detach from bridge convoy
                member.transform.SetParent(null, true);

                // Add Frenzy Blitz Aura at member's feet
                GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Quad);
                aura.name = "FrenzyBlitzAura";
                aura.transform.SetParent(member.transform, false);
                aura.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                aura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                aura.transform.localScale = Vector3.one * 2.2f;
                aura.GetComponent<Renderer>().material.color = new Color(1.0f, 0.35f, 0.15f, 0.85f); // Blazing Ember Ring

                float angle = (i / (float)squadMemberObjs.Count) * Mathf.PI * 2f;
                Vector3 destPos = targetCenter + new Vector3(Mathf.Cos(angle) * 4.5f, 0f, Mathf.Sin(angle) * 4.5f);

                // Quick leap animation down to combat floor
                StartCoroutine(RoutineLeapToFloor(member.transform, destPos));
                yield return new WaitForSeconds(0.12f);
            }

            Debug.Log("📯 [B-TEAM DIRECTOR] 18-Person Blitz Army successfully merged and deployed to combat arena!");
        }

        private IEnumerator RoutineLeapToFloor(Transform memberTr, Vector3 targetPos)
        {
            Vector3 startPos = memberTr.position;
            float elapsed = 0f;
            float duration = 0.85f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float heightArc = Mathf.Sin(t * Mathf.PI) * 3.5f;

                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                current.y += heightArc;
                memberTr.position = current;

                yield return null;
            }

            memberTr.position = targetPos;
        }

        private void UpdateBlitzCombatAI()
        {
            // Simple combat orbit & attack behavior around enemies or player
            Transform playerTr = GetPlayerTransform();
            Vector3 center = playerTr != null ? playerTr.position : Vector3.zero;

            for (int i = 0; i < squadMemberObjs.Count; i++)
            {
                if (squadMemberObjs[i] == null) continue;
                Transform mTr = squadMemberObjs[i].transform;

                // Orbit and face forward
                float offsetAngle = (Time.time * 1.5f) + (i * (Mathf.PI * 2f / squadMemberObjs.Count));
                Vector3 target = center + new Vector3(Mathf.Cos(offsetAngle) * 4.5f, 0f, Mathf.Sin(offsetAngle) * 4.5f);
                mTr.position = Vector3.Lerp(mTr.position, target, Time.deltaTime * 3.5f);
                mTr.LookAt(center + (mTr.position - center) * 2f);
            }
        }
        #endregion
    }
}
