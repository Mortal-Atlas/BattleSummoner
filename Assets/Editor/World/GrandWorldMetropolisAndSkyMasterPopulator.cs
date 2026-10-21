#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BattleSummoner.EditorScripts;

namespace BattleSummoner.World
{
    public static class GrandWorldMetropolisAndSkyMasterPopulator
    {
        private const string SCENE_PATH = "Assets/Scenes/OverworldMap.unity";

        private struct IslandSpec
        {
            public Vector3 pos;
            public float scale;
            public int type; // 0=Large, 1=Medium, 2=Small
            public bool hasArch;
            public bool hasBlueWillow;
        }

        private struct NPCSpec
        {
            public string name;
            public GameObject model;
            public Vector3 pos;
            public float yaw;
            public string role;
        }

        [MenuItem("BattleSummoner/Populate Grand Sky, Metropolis & Landscapes (Anime Aesthetic)", false, 20)]
        public static void ExecuteCompletePopulation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            Debug.Log("🌟 [MASTER POPULATOR] Starting Grand Anime World & Metropolis Population...");

            // 1. Sky & Celestial Archipelago Overhaul
            OverhaulSkyArchipelago();

            // 2. Town Beautification & Dark Academia Royal Blue Dressing
            EnrichTownAndResidentialStreets();

            // 3. NPC Community Population
            PopulateLivelyTownNPCs();

            // 4. Surrounding Landscape & Mainland Outskirts Enrichment
            EnrichSurroundingLandscapes();

            // 5. Clean up any invalid particle systems or ghost renderers
            CleanUpGhostRenderersAndFixParticles();

            // Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("✅ [MASTER POPULATOR] Complete World Population finished and scene saved successfully!");
        }

        // =========================================================================
        // 1. SKY & CELESTIAL ARCHIPELAGO OVERHAUL
        // =========================================================================
        public static void OverhaulSkyArchipelago()
        {
            Debug.Log("☁️ [SKY] Overhauling Sky Archipelago & Shinkai Clouds...");

            var oldSkyLayer = GameObject.Find("Sky_Layer_Aetherion");
            if (oldSkyLayer != null) Undo.DestroyObjectImmediate(oldSkyLayer);

            while (true)
            {
                var existingArchipelago = GameObject.Find("Grand_Sky_Archipelago_System");
                if (existingArchipelago == null) break;
                Undo.DestroyObjectImmediate(existingArchipelago);
            }

            GameObject skyRoot = new GameObject("Grand_Sky_Archipelago_System");
            GameObject islandsGroup = new GameObject("Celestial_Floating_Islands");
            islandsGroup.transform.SetParent(skyRoot.transform, false);

            // Load high quality island models
            GameObject islandLargeFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environments/Sky/FloatingIsland_Large.fbx");
            GameObject islandMedFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environments/Sky/FloatingIsland_Medium.fbx");
            GameObject islandSmallFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environments/Sky/FloatingIsland_Small.fbx");

            // Island deco assets
            GameObject sakuraTree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Tree_01.prefab");
            GameObject blueWillow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Blue.prefab")
                ?? sakuraTree;
            GameObject blueCrystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Prop_ManaCrystals_Cluster.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx");
            GameObject ruinArchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/MeshyGen/Royal_Celestial_Sanctuary_Arch.glb")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Doors & Doorways/Level_Doorway_Arched.prefab");

            // 16 floating island positions (spiral ascending constellation)
            List<IslandSpec> specs = new List<IslandSpec>
            {
                // Grand Citadel Islands
                new IslandSpec { pos = new Vector3(480f, 440f, 260f), scale = 1.35f, type = 0, hasArch = true, hasBlueWillow = true },
                new IslandSpec { pos = new Vector3(-680f, 490f, 720f), scale = 1.40f, type = 0, hasArch = true, hasBlueWillow = false },
                new IslandSpec { pos = new Vector3(1150f, 560f, -780f), scale = 1.60f, type = 0, hasArch = false, hasBlueWillow = true },
                new IslandSpec { pos = new Vector3(-1280f, 530f, -590f), scale = 1.50f, type = 0, hasArch = true, hasBlueWillow = false },

                // Medium Sanctuary Islands
                new IslandSpec { pos = new Vector3(260f, 380f, 540f), scale = 1.25f, type = 1, hasArch = false, hasBlueWillow = true },
                new IslandSpec { pos = new Vector3(-340f, 410f, 1050f), scale = 1.30f, type = 1, hasArch = false, hasBlueWillow = false },
                new IslandSpec { pos = new Vector3(820f, 450f, 920f), scale = 1.20f, type = 1, hasArch = true, hasBlueWillow = true },
                new IslandSpec { pos = new Vector3(-920f, 470f, 220f), scale = 1.35f, type = 1, hasArch = false, hasBlueWillow = false },
                new IslandSpec { pos = new Vector3(640f, 480f, -1350f), scale = 1.40f, type = 1, hasArch = false, hasBlueWillow = true },
                new IslandSpec { pos = new Vector3(-420f, 510f, -880f), scale = 1.25f, type = 1, hasArch = true, hasBlueWillow = false },

                // Stepping Stone Islands (spiral path across the heavens)
                new IslandSpec { pos = new Vector3(180f, 350f, 320f), scale = 1.15f, type = 2, hasArch = false, hasBlueWillow = false },
                new IslandSpec { pos = new Vector3(-120f, 375f, 440f), scale = 1.20f, type = 2, hasArch = false, hasBlueWillow = true },
                new IslandSpec { pos = new Vector3(-380f, 420f, 380f), scale = 1.10f, type = 2, hasArch = false, hasBlueWillow = false },
                new IslandSpec { pos = new Vector3(-510f, 460f, 120f), scale = 1.25f, type = 2, hasArch = false, hasBlueWillow = true },
                new IslandSpec { pos = new Vector3(-460f, 520f, -280f), scale = 1.15f, type = 2, hasArch = false, hasBlueWillow = false },
                new IslandSpec { pos = new Vector3(-240f, 570f, -580f), scale = 1.30f, type = 2, hasArch = false, hasBlueWillow = true }
            };

            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                GameObject template = spec.type == 0 ? islandLargeFbx : (spec.type == 1 ? islandMedFbx : islandSmallFbx);
                if (template == null) continue;

                GameObject island = UnityEngine.Object.Instantiate(template, spec.pos, Quaternion.Euler(0f, i * 47f, 0f), islandsGroup.transform);
                island.name = $"Celestial_SkyIsland_{i + 1:D2}_{(spec.type == 0 ? "Grand" : (spec.type == 1 ? "Medium" : "Stepping"))}";
                island.transform.localScale = Vector3.one * spec.scale;

                // Determine top plateau elevation
                var mr = island.GetComponentInChildren<MeshRenderer>();
                float topY = mr != null ? (mr.bounds.center.y + mr.bounds.extents.y * 0.72f) : spec.pos.y + 12f;
                Vector3 plateauCenter = new Vector3(spec.pos.x, topY, spec.pos.z);

                // Decorate top: Tree
                GameObject chosenTree = spec.hasBlueWillow ? blueWillow : sakuraTree;
                if (chosenTree != null)
                {
                    GameObject tObj = UnityEngine.Object.Instantiate(chosenTree, plateauCenter + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0f, UnityEngine.Random.Range(-3f, 3f)), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), island.transform);
                    tObj.name = "Island_SacredTree";
                    tObj.transform.localScale = Vector3.one * UnityEngine.Random.Range(1.1f, 1.4f);
                }

                // Decorate top: Royal Blue Mana Crystals
                if (blueCrystalPrefab != null)
                {
                    Vector3 cPos = plateauCenter + new Vector3(UnityEngine.Random.Range(4f, 8f), 0f, UnityEngine.Random.Range(3f, 7f));
                    GameObject cry = UnityEngine.Object.Instantiate(blueCrystalPrefab, cPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), island.transform);
                    cry.name = "Island_ManaCrystal_Spire";
                    cry.transform.localScale = Vector3.one * 1.5f;

                    // Add subtle celestial glow
                    GameObject lightGo = new GameObject("Crystal_CelestialGlow");
                    lightGo.transform.SetParent(cry.transform, false);
                    lightGo.transform.localPosition = Vector3.up * 1.5f;
                    Light l = lightGo.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(0.25f, 0.65f, 1.0f); // Royal Blue / Cyan
                    l.range = 25f;
                    l.intensity = 2.5f;
                }

                // Decorate top: Ancient Ruin Archway (Meshy celestial sanctuary arch)
                if (spec.hasArch && ruinArchPrefab != null)
                {
                    Vector3 archPos = plateauCenter + new Vector3(UnityEngine.Random.Range(-7f, -4f), 0f, UnityEngine.Random.Range(-4f, 4f));
                    GameObject arch = UnityEngine.Object.Instantiate(ruinArchPrefab, archPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), island.transform);
                    arch.name = "Island_AncientRuin_Arch";
                    arch.transform.localScale = Vector3.one * 4.2f;
                }
            }

            // 2. Deploy Multi-Tiered Stretched & Drifting Makoto Shinkai / Anime Clouds (5 Archetypes)
            AnimeSkyCloudMasterOverhaul.OverhaulAllSkyCloudsInScene();

            Debug.Log("☁️ [SKY] Spawned 16 celestial floating islands & multi-tiered dynamic anime clouds.");
        }

        // =========================================================================
        // 2. TOWN BEAUTIFICATION & DARK ACADEMIA ROYAL BLUE DRESSING
        // =========================================================================
        public static void EnrichTownAndResidentialStreets()
        {
            Debug.Log("🏰 [TOWN] Enriching residential streets, avenues, and market plazas...");

            var existingDecor = GameObject.Find("Grand_Metropolis_StreetLife_And_Decor");
            if (existingDecor != null) Undo.DestroyObjectImmediate(existingDecor);

            GameObject decorRoot = new GameObject("Grand_Metropolis_StreetLife_And_Decor");

            // Meshy Project Props (URP Prefabs)
            GameObject streetLampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab");
            GameObject astrolabePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/MeshyGen/Royal_Celestial_Astrolabe.glb");
            GameObject alchemistCauldronPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_AlchemistCauldron_Stylized.prefab");
            GameObject marketMerchantSetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_MarketMerchantSet_CratesAndBaskets.prefab");
            GameObject flowerBoxBenchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab");
            GameObject sakuraPlanterRingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MeshyProps/Prop_SakuraStonePlanterRing.prefab");

            // Project Props library
            GameObject barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BeerBarrelV1.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            GameObject woodStackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/PileOfWoodV1.prefab");
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Old_Table.prefab");
            GameObject benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BenchV1.prefab");
            GameObject cartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            GameObject flagBluePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/propsv2/Prefabs/flag1_low.prefab");
            GameObject scrollTablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/propsv2/Prefabs/scroll1_low.prefab");
            GameObject sakuraTree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab");
            GameObject blueWillow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Blue.prefab");

            // Dark Academia scholar props
            GameObject spellbookLectern = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TownProps/PFB_Wizard_SpellbookLectern.prefab");
            GameObject alchemyBench = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TownProps/PFB_Alchemy_DistillationBench.prefab");
            GameObject booksStack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3Digitalis/LowPolyInteriorPropsPack/Prefabs/P_Books_01.prefab");

            // 1. Centerpiece Royal Celestial Astrolabe in Cathedral Plaza (Firmly grounded on Tier 4 pavement Y=80.09)
            if (astrolabePrefab != null)
            {
                // Pivot is at center (min.y = -0.95m). At scale 1.8x, bottom offset is 1.71m.
                Vector3 astrolabePos = new Vector3(0f, 80.09f + 1.71f, 26f);
                GameObject ast = UnityEngine.Object.Instantiate(astrolabePrefab, astrolabePos, Quaternion.Euler(0f, 180f, 0f), decorRoot.transform);
                ast.name = "Monument_Royal_Celestial_Astrolabe";
                ast.transform.localScale = Vector3.one * 1.8f;

                // Dynamic bounds ground alignment
                var rends = ast.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                    float diff = 80.09f - b.min.y;
                    if (Mathf.Abs(diff) > 0.02f) ast.transform.position += Vector3.up * diff;
                }

                GameObject astLight = new GameObject("Astrolabe_SapphireGlow");
                astLight.transform.SetParent(ast.transform, false);
                astLight.transform.localPosition = Vector3.up * 1.5f;
                Light al = astLight.AddComponent<Light>();
                al.type = LightType.Point;
                al.color = new Color(0.2f, 0.55f, 1.0f); // Royal Blue
                al.range = 18f;
                al.intensity = 3.5f;

                Debug.Log("✨ [TOWN] Placed Monument_Royal_Celestial_Astrolabe firmly grounded on Cathedral Plaza!");
            }

            // 2. Meshy Sakura Planter Rings in Cathedral Plaza and Central Squares
            if (sakuraPlanterRingPrefab != null)
            {
                // Flanking the Astrolabe in Cathedral Plaza and in central plazas
                Vector3[] planterPositions = {
                    new Vector3(-16f, 80.09f, 26f),
                    new Vector3(16f, 80.09f, 26f),
                    new Vector3(0f, 58.06f, 135f), // Tier 3 Noble Plaza
                    new Vector3(0f, 38.06f, 210f)  // Tier 2 Commercial Plaza
                };

                for (int p = 0; p < planterPositions.Length; p++)
                {
                    var ring = UnityEngine.Object.Instantiate(sakuraPlanterRingPrefab, planterPositions[p], Quaternion.identity, decorRoot.transform);
                    ring.name = $"Cathedral_Sakura_PlanterRing_{p + 1}";
                    ring.transform.localScale = Vector3.one * 1.0f;

                    // Place sacred tree inside the planter ring
                    GameObject centerTree = p % 2 == 0 ? sakuraTree : blueWillow;
                    if (centerTree != null)
                    {
                        var t = UnityEngine.Object.Instantiate(centerTree, planterPositions[p], Quaternion.Euler(0f, p * 60f, 0f), ring.transform);
                        t.name = "Planter_CenterTree";
                        t.transform.localScale = Vector3.one * 1.1f;
                    }
                }
            }

            // 3. Meshy Plaza Flower Boxes & Integrated Benches along Overlooks
            if (flowerBoxBenchPrefab != null)
            {
                Vector3[] benchPositions = {
                    new Vector3(-24f, 80.09f, 18f),
                    new Vector3(24f, 80.09f, 18f),
                    new Vector3(-18f, 58.06f, 110f),
                    new Vector3(18f, 58.06f, 110f),
                    new Vector3(-22f, 38.06f, 175f),
                    new Vector3(22f, 38.06f, 175f)
                };

                for (int b = 0; b < benchPositions.Length; b++)
                {
                    var fb = UnityEngine.Object.Instantiate(flowerBoxBenchPrefab, benchPositions[b], Quaternion.Euler(0f, b * 30f + 180f, 0f), decorRoot.transform);
                    fb.name = $"Plaza_FlowerBox_Bench_{b + 1}";
                    fb.transform.localScale = Vector3.one * 1.0f;
                }
            }

            // 4. Meshy Alchemist Cauldron in Tier 3 Scholar Courtyard
            if (alchemistCauldronPrefab != null)
            {
                Vector3 alchPos = new Vector3(-12f, 58.06f, 118f);
                var cauldron = UnityEngine.Object.Instantiate(alchemistCauldronPrefab, alchPos, Quaternion.Euler(0f, 45f, 0f), decorRoot.transform);
                cauldron.name = "Academy_Alchemist_Cauldron";
                cauldron.transform.localScale = Vector3.one * 1.0f;

                // Glowing magical light inside cauldron
                GameObject cLight = new GameObject("Cauldron_MagicalGlow");
                cLight.transform.SetParent(cauldron.transform, false);
                cLight.transform.localPosition = Vector3.up * 1.2f;
                Light cl = cLight.AddComponent<Light>();
                cl.type = LightType.Point;
                cl.color = new Color(0.1f, 0.85f, 1.0f); // Royal Cyan/Sapphire
                cl.range = 8f;
                cl.intensity = 2.8f;
            }

            // 5. Meshy Market Merchant Sets in Tier 2 Bazaar
            if (marketMerchantSetPrefab != null)
            {
                Vector3[] stallPositions = {
                    new Vector3(178f, 38.06f, 175f),
                    new Vector3(192f, 38.06f, 188f),
                    new Vector3(-172f, 38.06f, 178f),
                    new Vector3(-188f, 38.06f, 192f)
                };

                for (int s = 0; s < stallPositions.Length; s++)
                {
                    var stall = UnityEngine.Object.Instantiate(marketMerchantSetPrefab, stallPositions[s], Quaternion.Euler(0f, s * 45f + 30f, 0f), decorRoot.transform);
                    stall.name = $"Market_MerchantSet_{s + 1}";
                    stall.transform.localScale = Vector3.one * 1.0f;
                }
            }

            // 6. Dress Residential Doorsteps (Terrace Wall & Rowhouses)
            var terraceWall = GameObject.Find("RowHouse_Tier1_TerraceWall");
            int decoratedDoorsteps = 0;

            if (terraceWall != null)
            {
                for (int i = 0; i < terraceWall.transform.childCount; i += 3)
                {
                    var house = terraceWall.transform.GetChild(i);
                    Vector3 fwd = house.forward;
                    Vector3 right = house.right;
                    Vector3 pos = house.position + fwd * 4.8f + right * 2.2f;

                    int style = (i / 3) % 4;
                    if (style == 0 && barrelPrefab != null)
                    {
                        var b = UnityEngine.Object.Instantiate(barrelPrefab, pos, Quaternion.identity, decorRoot.transform);
                        b.name = $"Doorstep_Barrel_{i}";
                        b.transform.localScale = Vector3.one * 1.1f;
                    }
                    else if (style == 1 && cratePrefab != null)
                    {
                        var c = UnityEngine.Object.Instantiate(cratePrefab, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), decorRoot.transform);
                        c.name = $"Doorstep_Crate_{i}";
                        c.transform.localScale = Vector3.one * 1.0f;
                    }
                    else if (style == 2 && woodStackPrefab != null)
                    {
                        var w = UnityEngine.Object.Instantiate(woodStackPrefab, pos, Quaternion.identity, decorRoot.transform);
                        w.name = $"Doorstep_Firewood_{i}";
                        w.transform.localScale = Vector3.one * 1.0f;
                    }
                    else if (style == 3 && benchPrefab != null)
                    {
                        var bn = UnityEngine.Object.Instantiate(benchPrefab, pos, Quaternion.LookRotation(fwd), decorRoot.transform);
                        bn.name = $"Doorstep_Bench_{i}";
                        bn.transform.localScale = Vector3.one * 1.0f;
                    }
                    decoratedDoorsteps++;
                }
            }

            // 7. Populate 8 Grand Boulevards with Mondstadt Streetlamps & Royal Banners
            float[] boulevardAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            float[] lampRadii = { 90f, 140f, 190f, 240f, 290f, 340f, 390f };

            int lampCount = 0;
            foreach (float ang in boulevardAngles)
            {
                float rad = ang * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x);

                foreach (float r in lampRadii)
                {
                    float y = r > 310f ? 22.06f : (r > 210f ? 38.06f : 58.06f);

                    for (int side = -1; side <= 1; side += 2)
                    {
                        // Mondstadt streetlamp upright prefab: base is at Y=0, height is 3.8m tall.
                        Vector3 lampPos = dir * r + perp * (7.2f * side) + Vector3.up * y;
                        if (streetLampPrefab != null)
                        {
                            var lamp = UnityEngine.Object.Instantiate(streetLampPrefab, lampPos, Quaternion.LookRotation(-perp * side), decorRoot.transform);
                            lamp.name = $"Boulevard_StreetLamp_R{r:F0}_{ang:F0}_{side}";
                            lamp.transform.localScale = Vector3.one * 1.0f;
                            lampCount++;
                        }
                    }
                }

                if (flagBluePrefab != null)
                {
                    Vector3 flagPos = dir * 110f + perp * 8.0f + Vector3.up * 58.06f;
                    var flg = UnityEngine.Object.Instantiate(flagBluePrefab, flagPos, Quaternion.LookRotation(dir), decorRoot.transform);
                    flg.name = $"Royal_Blue_AvenueBanner_{ang:F0}";
                    flg.transform.localScale = Vector3.one * 1.5f;
                }
            }

            // 8. Dark Academia Scholar Lecterns, Tables & Books
            Vector3 mktDir = new Vector3(Mathf.Sin(45f * Mathf.Deg2Rad), 0f, Mathf.Cos(45f * Mathf.Deg2Rad));
            Vector3 mktCenter = mktDir * 260f + Vector3.up * 38.06f;

            if (cartPrefab != null)
            {
                var cart1 = UnityEngine.Object.Instantiate(cartPrefab, mktCenter + new Vector3(3f, 0f, 4f), Quaternion.Euler(0f, 35f, 0f), decorRoot.transform);
                cart1.name = "Market_Merchant_Cart_01";
            }
            if (tablePrefab != null)
            {
                var tbl1 = UnityEngine.Object.Instantiate(tablePrefab, mktCenter + new Vector3(-3f, 0f, 3f), Quaternion.Euler(0f, 75f, 0f), decorRoot.transform);
                tbl1.name = "Market_Display_Table_01";
                if (scrollTablePrefab != null)
                {
                    var scr = UnityEngine.Object.Instantiate(scrollTablePrefab, tbl1.transform.position + Vector3.up * 0.85f, Quaternion.identity, tbl1.transform);
                    scr.name = "Table_ScholarScrolls";
                }
                if (booksStack != null)
                {
                    var bk = UnityEngine.Object.Instantiate(booksStack, tbl1.transform.position + Vector3.up * 0.85f + Vector3.right * 0.4f, Quaternion.identity, tbl1.transform);
                    bk.name = "Table_ArcaneTomes";
                }
            }
            if (spellbookLectern != null)
            {
                var lect = UnityEngine.Object.Instantiate(spellbookLectern, mktCenter + new Vector3(5f, 0f, -2f), Quaternion.Euler(0f, -45f, 0f), decorRoot.transform);
                lect.name = "Plaza_Scholar_SpellbookLectern";
            }
            if (alchemyBench != null)
            {
                var alc = UnityEngine.Object.Instantiate(alchemyBench, mktCenter + new Vector3(-5f, 0f, -3f), Quaternion.Euler(0f, 135f, 0f), decorRoot.transform);
                alc.name = "Plaza_Alchemical_Distillery";
            }

            Debug.Log($"🏰 [TOWN] Finished town decoration: {decoratedDoorsteps} doorsteps, {lampCount} streetlamps, Meshy painterly props, and royal academia dressing.");
        }

        // =========================================================================
        // 3. NPC COMMUNITY POPULATION (WITH DYNAMIC GROUND ALIGNMENT)
        // =========================================================================
        public static void PopulateLivelyTownNPCs()
        {
            Debug.Log("👥 [NPCS] Populating lively anime character NPCs across the metropolis...");

            var existingNPCs = GameObject.Find("Grand_Metropolis_NPC_Community");
            if (existingNPCs != null) Undo.DestroyObjectImmediate(existingNPCs);

            GameObject npcRoot = new GameObject("Grand_Metropolis_NPC_Community");

            GameObject catherineModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Catherine.glb");
            GameObject maleStarterModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/MaleStarter.glb");
            GameObject femaleStarterModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/femalestarter.glb");
            GameObject manBaseModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/manbase.glb");
            GameObject womanBaseModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/womanbase.glb");

            List<NPCSpec> npcList = new List<NPCSpec>
            {
                // Cathedral Plaza (Tier 4, Y=80.09)
                new NPCSpec { name = "NPC_GrandScholar_Catherine", model = catherineModel, pos = new Vector3(-2.8f, 80.09f, 22.0f), yaw = 10f, role = "Grand Scholar Archivist" },
                new NPCSpec { name = "NPC_Paladin_Knight_Vance", model = maleStarterModel, pos = new Vector3(-6.5f, 80.09f, 15.0f), yaw = 15f, role = "Cathedral Royal Sentinel" },
                new NPCSpec { name = "NPC_Paladin_Knight_Roland", model = maleStarterModel, pos = new Vector3(6.5f, 80.09f, 15.0f), yaw = -15f, role = "Cathedral Royal Sentinel" },
                new NPCSpec { name = "NPC_Philosopher_Aria", model = femaleStarterModel, pos = new Vector3(-7.5f, 80.09f, 27.0f), yaw = 25f, role = "Royal Astrologer" },
                new NPCSpec { name = "NPC_Acolyte_Julian", model = maleStarterModel, pos = new Vector3(7.5f, 80.09f, 27.0f), yaw = -25f, role = "Academy Disciple" },

                // Noble Tier 3 Plaza (Y=58.06)
                new NPCSpec { name = "NPC_Guildmaster_Theron", model = maleStarterModel, pos = new Vector3(12.0f, 58.06f, 120.0f), yaw = -75f, role = "Adventurers Guildmaster" },
                new NPCSpec { name = "NPC_Alchemist_Lyra", model = femaleStarterModel, pos = new Vector3(-15.0f, 58.06f, 115.0f), yaw = 45f, role = "Master Herbalist" },
                new NPCSpec { name = "NPC_Blacksmith_Garrick", model = maleStarterModel, pos = new Vector3(25.0f, 58.06f, 95.0f), yaw = -120f, role = "Royal Weaponsmith" },
                new NPCSpec { name = "NPC_Sorceress_Elena", model = femaleStarterModel, pos = new Vector3(-8.0f, 58.06f, 140.0f), yaw = 180f, role = "Arcane Summoner" },

                // Commercial Tier 2 Bazaar (Y=38.06)
                new NPCSpec { name = "NPC_Merchant_Kael", model = maleStarterModel, pos = new Vector3(185.0f, 38.06f, 182.0f), yaw = -45f, role = "Silk & Spices Merchant" },
                new NPCSpec { name = "NPC_Florist_Sora", model = femaleStarterModel, pos = new Vector3(188.0f, 38.06f, 178.0f), yaw = 135f, role = "Sakura Blossom Florist" },
                new NPCSpec { name = "NPC_Baker_Lucia", model = femaleStarterModel, pos = new Vector3(-180.0f, 38.06f, 185.0f), yaw = 90f, role = "Head Pastry Chef" },
                new NPCSpec { name = "NPC_Traveler_Rin", model = femaleStarterModel, pos = new Vector3(-176.0f, 38.06f, 180.0f), yaw = -60f, role = "Wandering Ronin" },
                new NPCSpec { name = "NPC_TavernHost_Barkeep", model = maleStarterModel, pos = new Vector3(175.0f, 38.06f, -185.0f), yaw = 15f, role = "Tavern Proprietor" },
                new NPCSpec { name = "NPC_Mercenary_Doran", model = maleStarterModel, pos = new Vector3(180.0f, 38.06f, -180.0f), yaw = -110f, role = "Veteran Mercenary" },

                // Residential Tier 1 Terrace & Avenues (Y=22.06)
                new NPCSpec { name = "NPC_TerraceResident_Mina", model = femaleStarterModel, pos = new Vector3(225.0f, 22.06f, 225.0f), yaw = 35f, role = "Terrace Resident" },
                new NPCSpec { name = "NPC_TerraceResident_Finn", model = maleStarterModel, pos = new Vector3(228.0f, 22.06f, 227.0f), yaw = -145f, role = "Townsfolk Craftsman" },
                new NPCSpec { name = "NPC_AvenuePatrol_Knight1", model = maleStarterModel, pos = new Vector3(0.0f, 22.06f, 335.0f), yaw = 90f, role = "City Watch Sentinel" },
                new NPCSpec { name = "NPC_AvenuePatrol_Knight2", model = maleStarterModel, pos = new Vector3(335.0f, 22.06f, 0.0f), yaw = 0f, role = "City Watch Sentinel" },

                // 4 Ordinal Bridge Gate Captains & Travelers (Y=22.06)
                new NPCSpec { name = "NPC_GateCaptain_NorthEast", model = maleStarterModel, pos = new Vector3(304.0f, 22.06f, 304.0f), yaw = 45f, role = "Gate Captain (North-East)" },
                new NPCSpec { name = "NPC_Caravaneer_Tatsuya", model = maleStarterModel, pos = new Vector3(298.0f, 22.06f, 298.0f), yaw = -135f, role = "Mainland Caravaneer" },
                new NPCSpec { name = "NPC_GateCaptain_NorthWest", model = maleStarterModel, pos = new Vector3(-304.0f, 22.06f, 304.0f), yaw = -45f, role = "Gate Captain (North-West)" },
                new NPCSpec { name = "NPC_Pilgrim_Yuki", model = femaleStarterModel, pos = new Vector3(-298.0f, 22.06f, 298.0f), yaw = 135f, role = "Aetherion Pilgrim" },
                new NPCSpec { name = "NPC_GateCaptain_SouthEast", model = maleStarterModel, pos = new Vector3(304.0f, 22.06f, -304.0f), yaw = 135f, role = "Gate Captain (South-East)" },
                new NPCSpec { name = "NPC_Summoner_Clara", model = femaleStarterModel, pos = new Vector3(298.0f, 22.06f, -298.0f), yaw = -45f, role = "Initiate Summoner" },
                new NPCSpec { name = "NPC_GateCaptain_SouthWest", model = maleStarterModel, pos = new Vector3(-304.0f, 22.06f, -304.0f), yaw = -135f, role = "Gate Captain (South-West)" },
                new NPCSpec { name = "NPC_Ranger_Leon", model = maleStarterModel, pos = new Vector3(-298.0f, 22.06f, -298.0f), yaw = 45f, role = "Wilds Scout" }
            };

            int spawnedCount = 0;
            foreach (var spec in npcList)
            {
                if (spec.model == null) continue;

                // Center pivot at (0,0,0) with min.y = -0.95m. Offset position by +0.95m initially.
                Vector3 initialPos = spec.pos + Vector3.up * 0.95f;
                GameObject npc = UnityEngine.Object.Instantiate(spec.model, initialPos, Quaternion.Euler(0f, spec.yaw, 0f), npcRoot.transform);
                npc.name = spec.name;
                npc.transform.localScale = Vector3.one * 1.0f;

                // Exact dynamic bounds clamp to ensure feet touch cobblestone pavement
                var rends = npc.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                    float diff = spec.pos.y - b.min.y;
                    if (Mathf.Abs(diff) > 0.01f)
                    {
                        npc.transform.position += Vector3.up * diff;
                    }
                }

                // Anime role nameplate facing forward with NPC
                GameObject nameplate = new GameObject("Nameplate_Prompt");
                nameplate.transform.SetParent(npc.transform, false);
                nameplate.transform.localPosition = Vector3.up * 2.15f;
                nameplate.transform.localRotation = Quaternion.identity;
                var tm = nameplate.AddComponent<TextMesh>();
                tm.text = $"<{spec.role}>\n{spec.name.Replace("NPC_", "").Replace("_", " ")}";
                tm.fontSize = 20;
                tm.characterSize = 0.08f;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = new Color(0.95f, 0.95f, 1.0f);

                spawnedCount++;
            }

            Debug.Log($"👥 [NPCS] Successfully placed {spawnedCount} anime NPCs with accurate ground elevation across the kingdom!");
        }

        // =========================================================================
        // 4. SURROUNDING LANDSCAPES & MAINLAND NATURE
        // =========================================================================
        public static void EnrichSurroundingLandscapes()
        {
            Debug.Log("🌿 [LANDSCAPES] Enriching mainland nature, bridgeheads, and riverside cliffs...");

            var continentGo = GameObject.Find("CircularContinent_MacroLevel1");
            if (continentGo == null)
            {
                Debug.LogWarning("CircularContinent_MacroLevel1 not found for terrain raycasts.");
                return;
            }

            MeshCollider terrainCollider = continentGo.GetComponentInChildren<MeshCollider>();
            if (terrainCollider == null) return;

            // Remove any existing outskirts roots to avoid duplicates stacking
            while (true)
            {
                var existingOutskirts = GameObject.Find("Grand_Mainland_Nature_And_Outskirts");
                if (existingOutskirts == null) break;
                Undo.DestroyObjectImmediate(existingOutskirts);
            }

            GameObject outskirtsRoot = new GameObject("Grand_Mainland_Nature_And_Outskirts");

            GameObject blueWillow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Blue.prefab");
            GameObject sakuraTree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab");
            GameObject flowerMeadowBlue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/FlowerMeadow_Blue.prefab");
            GameObject flowerMeadowPurple = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/FlowerMeadow_BluePurple.prefab");
            GameObject rockBig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/Rock_Big_01.prefab");
            GameObject reeds = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/Reeds_01.prefab");
            GameObject waterlily = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/Waterlily_01.prefab");

            Vector2[] ordinalBridgeExits = {
                new Vector2(385f, 385f),    // NorthEast
                new Vector2(-385f, 385f),   // NorthWest
                new Vector2(385f, -385f),   // SouthEast
                new Vector2(-385f, -385f)   // SouthWest
            };

            int bridgeDecorCount = 0;
            foreach (var bExit in ordinalBridgeExits)
            {
                Ray ray = new Ray(new Vector3(bExit.x, 500f, bExit.y), Vector3.down);
                if (terrainCollider.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    // STRICT DRY LAND CHECK: Water level is Y=18.0m. Skip if point is submerged or at waterline
                    if (hit.point.y < 19.5f) continue;

                    Vector3 exitPoint = hit.point;
                    Vector3 outwardDir = exitPoint.normalized;
                    Vector3 perp = new Vector3(-outwardDir.z, 0f, outwardDir.x);

                    // Sacred Grove at bridgehead
                    if (blueWillow != null)
                    {
                        var t1 = UnityEngine.Object.Instantiate(blueWillow, exitPoint + perp * 12f + outwardDir * 10f, Quaternion.identity, outskirtsRoot.transform);
                        t1.name = "Bridgehead_SacredWillow_Left";
                        t1.transform.localScale = Vector3.one * 1.3f;
                    }
                    if (sakuraTree != null)
                    {
                        var t2 = UnityEngine.Object.Instantiate(sakuraTree, exitPoint - perp * 12f + outwardDir * 10f, Quaternion.identity, outskirtsRoot.transform);
                        t2.name = "Bridgehead_Sakura_Right";
                        t2.transform.localScale = Vector3.one * 1.3f;
                    }

                    // Flower meadows
                    if (flowerMeadowBlue != null)
                    {
                        var m1 = UnityEngine.Object.Instantiate(flowerMeadowBlue, exitPoint + perp * 8f + outwardDir * 16f, Quaternion.identity, outskirtsRoot.transform);
                        m1.name = "Bridgehead_FlowerMeadow_Blue";
                    }
                    if (flowerMeadowPurple != null)
                    {
                        var m2 = UnityEngine.Object.Instantiate(flowerMeadowPurple, exitPoint - perp * 8f + outwardDir * 16f, Quaternion.identity, outskirtsRoot.transform);
                        m2.name = "Bridgehead_FlowerMeadow_Purple";
                    }

                    // Granite Boulder
                    if (rockBig != null)
                    {
                        var r1 = UnityEngine.Object.Instantiate(rockBig, exitPoint + perp * 18f + outwardDir * 5f, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), outskirtsRoot.transform);
                        r1.name = "Bridgehead_GraniteBoulder";
                        r1.transform.localScale = Vector3.one * 1.2f;
                    }
                    bridgeDecorCount++;
                }
            }

            // Moat waterlines decoration (Waterlilies and Reeds)
            // DEFERRED per user request: "we will implement water props and other wild life there in a future update when im ready to tackle it"
            int waterDecoCount = 0;

            Debug.Log($"🌿 [LANDSCAPES] Decorated {bridgeDecorCount} ordinal bridgeheads and {waterDecoCount} moat waterline spots.");
        }

        // =========================================================================
        // 5. CLEAN UP GHOST RENDERERS AND FIX PARTICLES
        // =========================================================================
        public static void CleanUpGhostRenderersAndFixParticles()
        {
            Debug.Log("🧹 [CLEANUP] Cleaning up ghost renderers and assigning soft anime smoke particles...");

            string smokeMatPath = "Assets/Materials/Environment/M_Anime_ChimneySmoke.mat";
            var smokeMat = AssetDatabase.LoadAssetAtPath<Material>(smokeMatPath);

            var psrs = UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None);
            int psrFixed = 0;
            foreach (var psr in psrs)
            {
                if (psr.sharedMaterial == null && smokeMat != null)
                {
                    psr.sharedMaterial = smokeMat;
                    psrFixed++;
                }
            }

            int stripped = 0;
            var rends = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var mr in rends)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    UnityEngine.Object.DestroyImmediate(mr);
                    if (mf != null) UnityEngine.Object.DestroyImmediate(mf);
                    stripped++;
                }
            }

            Debug.Log($"🧹 [CLEANUP] Assigned smoke material to {psrFixed} particle systems and stripped {stripped} empty mesh renderers.");
        }
    }
}
#endif
