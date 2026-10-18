using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Minigames.Fishing;
using BattleSummoner.Minigames.Insects;
using BattleSummoner.Minigames.Gardening;
using BattleSummoner.Customization;
using BattleSummoner.UI;

namespace BattleSummoner.World
{
    /// <summary>
    /// OverworldLifeSimDirector orchestrates the complete Animal Crossing life-sim ecosystem
    /// in the Overworld:
    /// - Fishing Pier & tranquil fishing pond populated with dynamic fish shadow silhouettes
    /// - Diverse insect ecology (fluttering butterflies, perching stags, glowing fireflies)
    /// - Community garden plots with flowers, crops, and hydration systems
    /// - Player starter model setup (MaleStarter / FemaleStarter with 2D anime facial expressions)
    /// - Tool integration (Fishing Rod, Bug Net, Watering Can)
    /// </summary>
    public class OverworldLifeSimDirector : MonoBehaviour
    {
        public static OverworldLifeSimDirector Instance { get; private set; }

        [Header("Ecosystem Containers")]
        [SerializeField] private Transform fishingPondRoot;
        [SerializeField] private Transform insectRoot;
        [SerializeField] private Transform gardenPlotsRoot;

        [Header("Starter Character Config")]
        public bool useFemaleStarter = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            SetupPlayerStarterAvatar();
            BuildFishingPierAndPond();
            BuildMoatFishingPiersAndStockFish();
            SpawnWildInsects();
            BuildCommunityGarden();
            SpawnWorldCollectibles();
            SetupCelebrationUIs();
        }

        public void SetupPlayerStarterAvatar()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero == null) return;

            // Check if StarterVisualRoot already exists
            Transform visualRoot = hero.transform.Find("StarterVisualRoot");
            if (visualRoot == null)
            {
                var vGO = new GameObject("StarterVisualRoot");
                vGO.transform.SetParent(hero.transform, false);
                vGO.transform.localPosition = new Vector3(0, 0.95f, 0); // align feet to ground
                visualRoot = vGO.transform;
            }

            // Clean old visual children if any
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                var child = visualRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            // Load starter prefab
            string prefabPath = useFemaleStarter
                ? "Assets/Prefabs/Characters/Character_Starter_Female.prefab"
                : "Assets/Prefabs/Characters/Character_Starter_Male.prefab";

            GameObject starterPrefab = null;
#if UNITY_EDITOR
            starterPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
#endif
            if (starterPrefab != null)
            {
                var instance = Instantiate(starterPrefab, visualRoot);
                instance.name = useFemaleStarter ? "Starter_Female_Active" : "Starter_Male_Active";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                var faceCtrl = instance.GetComponent<AnimeFaceOverlayController>();
                if (faceCtrl != null)
                {
                    faceCtrl.InitializeFaceMeshes();
                }
            }

            // Equip Tools onto hero
            var rod = hero.GetComponentInChildren<FishingRodTool>() ?? hero.AddComponent<FishingRodTool>();
            var net = hero.GetComponentInChildren<BugNetTool>() ?? hero.AddComponent<BugNetTool>();
            var can = hero.GetComponentInChildren<WateringCanTool>() ?? hero.AddComponent<WateringCanTool>();

            // Default to fishing rod visible
            rod.SetEquipped(true);
            net.SetEquipped(false);
            can.SetEquipped(false);

            Debug.Log("[LIFE SIM] Successfully initialized Player Starter Avatar with 2D anime facial expressions and tools!");
        }

        private void BuildFishingPierAndPond()
        {
            if (fishingPondRoot != null) return;

            var pondRoot = new GameObject("FishingEcosystem_Pond");
            fishingPondRoot = pondRoot.transform;
            pondRoot.transform.position = new Vector3(18f, 0.05f, -210f); // Idyllic pond spot near village spawn

            // Pond Water Basin (Cylinder)
            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(water.GetComponent<Collider>());
            water.name = "FishingPond_Water";
            water.transform.SetParent(pondRoot.transform, false);
            water.transform.localScale = new Vector3(16f, 0.08f, 16f);

            var wmr = water.GetComponent<MeshRenderer>();
            var wmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            wmat.color = new Color(0.12f, 0.45f, 0.65f, 0.85f);
            wmr.sharedMaterial = wmat;

            // Pond Stone Rim
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(rim.GetComponent<Collider>());
            rim.name = "PondStoneRim";
            rim.transform.SetParent(pondRoot.transform, false);
            rim.transform.localPosition = new Vector3(0, -0.05f, 0);
            rim.transform.localScale = new Vector3(17.2f, 0.15f, 17.2f);
            var rmr = rim.GetComponent<MeshRenderer>();
            var rmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            rmat.color = new Color(0.45f, 0.46f, 0.48f);
            rmr.sharedMaterial = rmat;

            // Wooden Fishing Pier extending into water
            var pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "WoodenFishingPier";
            pier.transform.SetParent(pondRoot.transform, false);
            pier.transform.localPosition = new Vector3(-6.5f, 0.18f, 0);
            pier.transform.localScale = new Vector3(5.5f, 0.25f, 2.8f);

            var pmr = pier.GetComponent<MeshRenderer>();
            var pmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pmat.color = new Color(0.48f, 0.32f, 0.20f); // Warm timber
            pmr.sharedMaterial = pmat;

            // Pier Lantern
            var lanternPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(lanternPost.GetComponent<Collider>());
            lanternPost.name = "PierLanternPost";
            lanternPost.transform.SetParent(pier.transform, false);
            lanternPost.transform.localPosition = new Vector3(0.4f, 1.2f, 0.4f);
            lanternPost.transform.localScale = new Vector3(0.06f, 1.2f, 0.06f);
            lanternPost.GetComponent<MeshRenderer>().sharedMaterial = pmat;

            var lanternLight = lanternPost.AddComponent<Light>();
            lanternLight.type = LightType.Point;
            lanternLight.color = new Color(1.0f, 0.85f, 0.50f);
            lanternLight.range = 6.0f;
            lanternLight.intensity = 1.8f;

            // Spawn Fish Shadows in Pond
            FishShadowSize[] sizes = new FishShadowSize[] {
                FishShadowSize.Small, FishShadowSize.Medium, FishShadowSize.Large, FishShadowSize.Tiny, FishShadowSize.Finned
            };

            for (int i = 0; i < 5; i++)
            {
                var fishGO = new GameObject($"FishShadow_{i + 1}");
                fishGO.transform.SetParent(pondRoot.transform, false);
                Vector2 randPos = Random.insideUnitCircle * 5.0f;
                fishGO.transform.localPosition = new Vector3(randPos.x, 0.02f, randPos.y);

                var fish = fishGO.AddComponent<FishShadowEntity>();
                fish.shadowSize = sizes[i % sizes.Length];
                fish.wanderRadius = new Vector2(4.5f, 4.5f);
            }

            Debug.Log("[LIFE SIM] Built Animal Crossing Fishing Pier & Pond with swimming fish shadows!");
        }

        private void BuildMoatFishingPiersAndStockFish()
        {
            var existing = GameObject.Find("Moat_Fishing_Ecosystem");
            if (existing != null) return;

            var rootGO = new GameObject("Moat_Fishing_Ecosystem");
            var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metroRoot != null) rootGO.transform.SetParent(metroRoot.transform, false);

            var woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            woodMat.color = new Color(0.44f, 0.28f, 0.16f);

            BuildMoatHub("MoatPier_South", rootGO.transform, new Vector3(22f, 0.20f, -315f), 180f, woodMat, new Vector3(22f, -0.73f, -324f));
            BuildMoatHub("MoatPier_North", rootGO.transform, new Vector3(-22f, 0.20f, 315f), 0f, woodMat, new Vector3(-22f, -0.73f, 324f));
            BuildMoatHub("MoatPier_East", rootGO.transform, new Vector3(315f, 0.20f, 22f), 90f, woodMat, new Vector3(324f, -0.73f, 22f));
            BuildMoatHub("MoatPier_West", rootGO.transform, new Vector3(-315f, 0.20f, -22f), 270f, woodMat, new Vector3(-324f, -0.73f, -22f));

            Debug.Log("🎣 [LIFE SIM] Built 4 Moat Fishing Piers & stocked 24 fish shadows in kingdom moat!");
        }

        private void BuildMoatHub(string hubName, Transform parent, Vector3 pierPos, float yRot, Material woodMat, Vector3 fishCenter)
        {
            var hub = new GameObject(hubName);
            hub.transform.SetParent(parent, false);
            hub.transform.position = pierPos;
            hub.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "PierDeck";
            deck.transform.SetParent(hub.transform, false);
            deck.transform.localPosition = Vector3.zero;
            deck.transform.localScale = new Vector3(4.2f, 0.25f, 15.0f);
            deck.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            Vector3[] pilings = new Vector3[] {
                new Vector3(-1.9f, -1.9f, -5.5f), new Vector3(1.9f, -1.9f, -5.5f),
                new Vector3(-1.9f, -1.9f, 0f), new Vector3(1.9f, -1.9f, 0f),
                new Vector3(-1.9f, -1.9f, 5.5f), new Vector3(1.9f, -1.9f, 5.5f)
            };
            foreach (var p in pilings)
            {
                var piling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var col = piling.GetComponent<Collider>();
                if (col != null) Destroy(col);
                piling.name = "PierPiling";
                piling.transform.SetParent(hub.transform, false);
                piling.transform.localPosition = p;
                piling.transform.localScale = new Vector3(0.32f, 2.0f, 0.32f);
                piling.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            var lanternPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var lpCol = lanternPost.GetComponent<Collider>();
            if (lpCol != null) Destroy(lpCol);
            lanternPost.name = "MoatPierLantern";
            lanternPost.transform.SetParent(hub.transform, false);
            lanternPost.transform.localPosition = new Vector3(1.7f, 1.25f, 6.8f);
            lanternPost.transform.localScale = new Vector3(0.08f, 1.25f, 0.08f);
            lanternPost.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            var pLight = lanternPost.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.85f, 0.48f);
            pLight.range = 8.5f;
            pLight.intensity = 1.8f;

            FishShadowSize[] sizes = new FishShadowSize[] {
                FishShadowSize.Large, FishShadowSize.Medium, FishShadowSize.Huge,
                FishShadowSize.Finned, FishShadowSize.Small, FishShadowSize.Medium
            };
            for (int i = 0; i < sizes.Length; i++)
            {
                var fishGO = new GameObject($"FishShadow_Moat_{hubName}_{i + 1}");
                fishGO.transform.SetParent(hub.transform, true);
                float ang = (i * 60f + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                float rDist = Random.Range(3.5f, 10.5f);
                Vector3 fishPos = fishCenter + new Vector3(Mathf.Sin(ang) * rDist, 0f, Mathf.Cos(ang) * rDist);
                fishPos.y = -0.73f;
                fishGO.transform.position = fishPos;

                var fish = fishGO.AddComponent<FishShadowEntity>();
                fish.shadowSize = sizes[i];
                fish.wanderRadius = new Vector2(9f, 9f);
                fish.moveSpeed = 1.25f;
            }
        }

        private void SpawnWildInsects()
        {
            if (insectRoot != null) return;

            var rootGO = new GameObject("WildInsectsRoot");
            insectRoot = rootGO.transform;
            rootGO.transform.position = new Vector3(0, 0, -215f);

            // Butterflies over greenery
            string[] butterflyIds = new string[] { "common_butterfly", "monarch_butterfly", "emperor_butterfly" };
            Vector3[] bPositions = new Vector3[] {
                new Vector3(-6f, 1.0f, 2f),
                new Vector3(-8f, 1.2f, -4f),
                new Vector3(4f, 0.9f, 6f),
                new Vector3(10f, 1.1f, -2f)
            };

            for (int i = 0; i < bPositions.Length; i++)
            {
                var bugGO = new GameObject($"WildButterfly_{i + 1}");
                bugGO.transform.SetParent(insectRoot, false);
                bugGO.transform.localPosition = bPositions[i];

                var bug = bugGO.AddComponent<InsectEntity>();
                bug.speciesId = butterflyIds[i % butterflyIds.Length];
                bug.insectType = InsectType.Butterfly;
                bug.wanderRadius = 4.5f;
            }

            // Beetles (Horned Dynastid, Golden Stag)
            string[] beetleIds = new string[] { "horned_dynastid", "golden_stag" };
            Vector3[] beetlePositions = new Vector3[] {
                new Vector3(-12f, 1.4f, 4f),
                new Vector3(14f, 1.5f, -6f)
            };

            for (int i = 0; i < beetlePositions.Length; i++)
            {
                var beetleGO = new GameObject($"WildBeetle_{i + 1}");
                beetleGO.transform.SetParent(insectRoot, false);
                beetleGO.transform.localPosition = beetlePositions[i];

                var beetle = beetleGO.AddComponent<InsectEntity>();
                beetle.speciesId = beetleIds[i];
                beetle.insectType = InsectType.Beetle;
            }

            // Glowing Fireflies
            for (int i = 0; i < 3; i++)
            {
                var ffGO = new GameObject($"WildFirefly_{i + 1}");
                ffGO.transform.SetParent(insectRoot, false);
                ffGO.transform.localPosition = new Vector3(Random.Range(-10f, 10f), Random.Range(1.0f, 2.2f), Random.Range(-8f, 8f));

                var ff = ffGO.AddComponent<InsectEntity>();
                ff.speciesId = "firefly";
                ff.insectType = InsectType.Firefly;
                ff.wanderRadius = 5.0f;
            }

            Debug.Log("[LIFE SIM] Spawned wild butterflies, beetles, and fireflies with stealth detection!");
        }

        private void BuildCommunityGarden()
        {
            if (gardenPlotsRoot != null) return;

            var gardenGO = new GameObject("CommunityGardenRoot");
            gardenPlotsRoot = gardenGO.transform;
            gardenPlotsRoot.transform.position = new Vector3(-14f, 0.02f, -212f); // Garden zone near village

            // 6 Garden Beds in a 2x3 grid
            string[] starterCrops = new string[] {
                "red_rose", "golden_cosmos", "astral_orchid", "strawberry", "pumpkin", "frost_mint"
            };
            GrowthStage[] starterStages = new GrowthStage[] {
                GrowthStage.MatureHarvestable, GrowthStage.Growing, GrowthStage.Sprout, 
                GrowthStage.MatureHarvestable, GrowthStage.Growing, GrowthStage.PlantedSeed
            };

            int idx = 0;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var plotGO = new GameObject($"GardenPlot_R{row}_C{col}");
                    plotGO.transform.SetParent(gardenPlotsRoot, false);
                    plotGO.transform.localPosition = new Vector3(col * 2.4f, 0, row * 2.4f);

                    var plot = plotGO.AddComponent<GardenPlot>();
                    plot.currentPlantId = starterCrops[idx];
                    plot.stage = starterStages[idx];
                    idx++;
                }
            }

            Debug.Log("[LIFE SIM] Built community garden with 6 plant varieties across growth stages!");
        }

        private void SpawnWorldCollectibles()
        {
            var root = GameObject.Find("WorldCollectiblesRoot");
            if (root != null) return;

            root = new GameObject("WorldCollectiblesRoot");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Glow Cap Mushrooms (Forest West)
            Create3DCollectible(root.transform, "GlowCapMushrooms", new Vector3(20f, 0.2f, -22f), CollectibleType.Mushroom, "glow_mushroom", "Glow Cap Mushroom", "Bioluminescent forest mushroom used in mana recovery potions.", new Color(0.2f, 0.95f, 0.85f), shader);

            // 2. Azure Spore Cluster (Forest Central)
            Create3DCollectible(root.transform, "AzureSporeCluster", new Vector3(32f, 0.2f, 14f), CollectibleType.Mushroom, "azure_spore", "Azure Spore Mushroom", "Fragrant cerulean cap found near mineral seeps.", new Color(0.3f, 0.7f, 1.0f), shader);

            // 3. Mistwood Black Truffle (Forest East)
            Create3DCollectible(root.transform, "ForestBlackTruffle", new Vector3(44f, 0.15f, -26f), CollectibleType.Mushroom, "black_truffle", "Mistwood Truffle", "Savory subterranean fungus prized by tavern chefs.", new Color(0.6f, 0.4f, 0.25f), shader);

            // 4. Wild Mistwood Herb (Trailside)
            Create3DCollectible(root.transform, "WildMistwoodHerb", new Vector3(15f, 0.25f, -8f), CollectibleType.Plant, "mistwood_herb", "Wild Mistwood Herb", "Potent restorative herb that heals minor wounds upon consumption.", new Color(0.3f, 0.9f, 0.4f), shader);

            // 5. Sunlit Maidenhair Fern (Stream Glade)
            Create3DCollectible(root.transform, "SunlitMaidenhairFern", new Vector3(26f, 0.25f, 6f), CollectibleType.Plant, "sunlit_fern", "Sunlit Maidenhair Fern", "Tender fronds carrying high moisture content for healing balms.", new Color(0.45f, 0.95f, 0.35f), shader);

            // 6. Wild Berry Bush (Meadow Edge)
            Create3DCollectible(root.transform, "WildBerryBush", new Vector3(18f, 0.35f, 12f), CollectibleType.Fruit, "wild_berries", "Wild Berry Bush", "Dense cluster of sweet forest berries.", new Color(0.75f, 0.25f, 0.85f), shader);

            // 7. Sweet Sun Apples (Orchard Grove)
            Create3DCollectible(root.transform, "SweetSunApples", new Vector3(25f, 0.35f, -18f), CollectibleType.Fruit, "sun_apples", "Sweet Sun Apple", "Crisp, sun-kissed orchard fruit that restores stamina.", new Color(0.95f, 0.25f, 0.2f), shader);

            Debug.Log("[LIFE SIM] Successfully populated overworld with harvestable 3D mushrooms, plants, and fruits!");
        }

        private void Create3DCollectible(Transform parent, string goName, Vector3 worldPos, CollectibleType type, string id, string displayName, string desc, Color color, Shader shader)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            // Visual mesh
            PrimitiveType primType = type switch
            {
                CollectibleType.Mushroom => PrimitiveType.Cylinder,
                CollectibleType.Plant => PrimitiveType.Cylinder,
                CollectibleType.Fruit => PrimitiveType.Sphere,
                _ => PrimitiveType.Cube
            };

            var meshObj = GameObject.CreatePrimitive(primType);
            meshObj.name = "Model";
            meshObj.transform.SetParent(go.transform, false);
            meshObj.transform.localPosition = Vector3.zero;

            var col = meshObj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            if (type == CollectibleType.Mushroom)
            {
                meshObj.transform.localScale = new Vector3(0.45f, 0.15f, 0.45f);
                // Stalk
                var stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalk.name = "Stalk";
                stalk.transform.SetParent(go.transform, false);
                stalk.transform.localPosition = new Vector3(0f, -0.15f, 0f);
                stalk.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f);
                DestroyImmediate(stalk.GetComponent<Collider>());
                var stalkRend = stalk.GetComponent<Renderer>();
                var stalkMat = new Material(shader);
                stalkMat.color = new Color(0.92f, 0.90f, 0.82f);
                stalkRend.sharedMaterial = stalkMat;
            }
            else if (type == CollectibleType.Plant)
            {
                meshObj.transform.localScale = new Vector3(0.2f, 0.45f, 0.2f);
            }
            else // Fruit
            {
                meshObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            }

            var rend = meshObj.GetComponent<Renderer>();
            var mat = new Material(shader);
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.6f);
            rend.sharedMaterial = mat;

            // Glow light
            var lGo = new GameObject("GlowLight", typeof(Light));
            lGo.transform.SetParent(go.transform, false);
            lGo.transform.localPosition = Vector3.up * 0.25f;
            var l = lGo.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = 3.5f;
            l.intensity = 1.2f;

            // Collectible logic component
            var collectible = go.AddComponent<WorldCollectibleItem>();
            collectible.itemId = id;
            collectible.displayName = displayName;
            collectible.itemType = type;
            collectible.description = desc;
        }

        private void SetupCelebrationUIs()
        {
            // Ensure celebration modals exist
            if (FishingCatchCelebrationUI.Instance == null)
            {
                var fGO = new GameObject("FishingCelebrationManager");
                fGO.AddComponent<FishingCatchCelebrationUI>();
            }
            if (InsectCatchCelebrationUI.Instance == null)
            {
                var iGO = new GameObject("InsectCelebrationManager");
                iGO.AddComponent<InsectCatchCelebrationUI>();
            }
        }

        /// <summary>
        /// Equips the requested life-sim tool (0=Fishing Rod, 1=Bug Net, 2=Watering Can)
        /// </summary>
        public void SelectTool(int toolIndex)
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero == null) return;

            var rod = hero.GetComponentInChildren<FishingRodTool>();
            var net = hero.GetComponentInChildren<BugNetTool>();
            var can = hero.GetComponentInChildren<WateringCanTool>();

            if (rod != null) rod.SetEquipped(toolIndex == 0);
            if (net != null) net.SetEquipped(toolIndex == 1);
            if (can != null) can.SetEquipped(toolIndex == 2);

            string name = toolIndex switch
            {
                0 => "Fishing Rod",
                1 => "Bug Net",
                2 => "Watering Can",
                _ => "None"
            };
            Debug.Log($"[LIFE SIM TOOL] Switched active tool to: {name}");
        }
    }
}
