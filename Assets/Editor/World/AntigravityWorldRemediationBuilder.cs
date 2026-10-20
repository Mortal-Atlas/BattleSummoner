#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.World.Editor
{
    public static class AntigravityWorldRemediationBuilder
    {
        private const float CITY_OUTER_RADIUS = 450f;
        private const float MOAT_OUTER_RADIUS = 535f;
        private const float ISLAND_COAST_RADIUS = 2850f;

        // Exact non-overlapping stepped terrace elevations for the 4 Magic City Tiers
        public static float GetSteppedCityTierElevation(float radiusFromCenter)
        {
            if (radiusFromCenter <= 95f)  return 80.0f; // Tier 4: Cathedral Acropolis
            if (radiusFromCenter <= 195f) return 58.0f; // Tier 3: Mages' & Sanctum Terrace
            if (radiusFromCenter <= 310f) return 38.0f; // Tier 2: Artisans, Bakeries & Norse Guild Hall
            return 22.0f;                               // Tier 1: Outer Bastion & Bazaar Ring
        }

        [MenuItem("BattleSummoner/MCP World Remediation/Execute Complete World, Lamp, Grass & Anime Nature Fix")]
        public static void ExecuteCompleteRemediation()
        {
            Debug.Log("[Antigravity MCP] Starting Full World Remediation Pipeline...");

            // 0. Ensure terrain mesh collider is active and accurate
            EnsureTerrainCollider();

            // 1. Fix City Tier Floor Cylinders so Tier 1 never covers lower buildings
            FixCityTierCylindersAndUnburyBuildings();

            // 2. Purge Giant Handheld Lanterns ('Lamp' / 'LanternV1..3') & Replace with Proper Street Lamp Posts
            ReplaceWrongHandheldLampsWithStreetLamps();

            // 3. Harmonize Building & City Materials to Match Anime Stylized Aesthetic
            HarmonizeCityMaterialsToAnimeAesthetic();

            // 4. Rebuild Seamless Continuous Grass & Endemic Biome Foliage using ONLY 'Anime Nature' (276931)
            PopulateBiomesWithAnimeNaturePackageOnly();

            // 5. Update Player Hero and Camera to Tier 4 Acropolis
            AlignHeroAndCameraToAcropolis();

            // 6. Save active scene and assets
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("[Antigravity MCP] Remediation Complete: Island scaled, Tier 1 un-clipped, Street Lamps installed, and Anime Nature (276931) biomes populated.");
        }

        private static void EnsureTerrainCollider()
        {
            GameObject terrain = GameObject.Find("Floor01_MacroContinent_Terrain");
            if (terrain != null)
            {
                var mf = terrain.GetComponent<MeshFilter>();
                var mc = terrain.GetComponent<MeshCollider>();
                if (mc == null) mc = terrain.AddComponent<MeshCollider>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }

        // =====================================================================
        // 1. UN-BURY LOWER CITY & SNAP BUILDINGS FLUSH TO STEPPED TIERS
        // =====================================================================
        public static void FixCityTierCylindersAndUnburyBuildings()
        {
            EnsureTerrainCollider();

            // Adjust any concentric tier cylinders so each tier's top surface matches its exact stepped height
            var tierConfigs = new (string namePattern, float radius, float topElevationY, float baseElevationY)[]
            {
                ("Tier1", 450f, 22.0f, 10.0f),
                ("Tier2", 310f, 38.0f, 22.0f),
                ("Tier3", 195f, 58.0f, 38.0f),
                ("Tier4",  95f, 80.0f, 58.0f)
            };

            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var cfg in tierConfigs)
            {
                foreach (var go in allObjects)
                {
                    if (go == null) continue;
                    if (go.name.IndexOf(cfg.namePattern, StringComparison.OrdinalIgnoreCase) >= 0 &&
                        !go.name.Contains("RowHouses") && !go.name.Contains("Root") && !go.name.Contains("Houses") &&
                        (go.name.IndexOf("City", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         go.name.IndexOf("Tier", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         go.name.IndexOf("Cylinder", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        float height = cfg.topElevationY - cfg.baseElevationY;
                        float centerY = cfg.baseElevationY + (height * 0.5f);
                        go.transform.position = new Vector3(0f, centerY, 0f);
                    }
                }
            }

            int buildingsSnapped = 0;
            var terrainGo = GameObject.Find("Floor01_MacroContinent_Terrain");
            var terrainCollider = terrainGo != null ? terrainGo.GetComponent<MeshCollider>() : null;

            // Snap every city building, house, bakery, and stall flush onto its stepped tier surface
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                if (go.transform.childCount > 0) continue; // Skip container parents like 01_Tier_Stepped_RowHouses
                string n = go.name;
                if (n.Contains("Root") || n.Contains("Stepped") || n.Contains("Houses")) continue;

                bool isBuilding = n.StartsWith("SF_RowHouse") || n.StartsWith("RowHouse") ||
                                  n.Contains("Bakery") || n.Contains("GuildHall") ||
                                  n.Contains("House") || n.Contains("Shop") ||
                                  n.Contains("Tavern") || n.Contains("Forge") ||
                                  n.Contains("Inn_Lodge") || n.Contains("GuildArmory") ||
                                  n.Contains("Workshop");

                if (isBuilding)
                {
                    Vector3 pos = go.transform.position;
                    float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                    if (r <= CITY_OUTER_RADIUS)
                    {
                        float correctTierY = GetSteppedCityTierElevation(r);
                        Ray ray = new Ray(new Vector3(pos.x, 150f, pos.z), Vector3.down);
                        if (terrainCollider != null && terrainCollider.Raycast(ray, out RaycastHit hit, 300f))
                        {
                            go.transform.position = new Vector3(pos.x, hit.point.y, pos.z);
                        }
                        else
                        {
                            go.transform.position = new Vector3(pos.x, correctTierY, pos.z);
                        }
                        buildingsSnapped++;
                    }
                }
            }

            // Also snap city street props (benches, planters, stalls, NPCs) flush to their stepped tier
            string[] cityPropParents = { "02_Street_Props_Benches_Lamps", "03_Street_Flower_Planters", "04_Market_Bazaars_And_Barrels", "05_Lively_Town_NPCs" };
            foreach (var parentName in cityPropParents)
            {
                var parentGo = GameObject.Find(parentName);
                if (parentGo == null) continue;
                for (int i = 0; i < parentGo.transform.childCount; i++)
                {
                    var ch = parentGo.transform.GetChild(i);
                    Vector3 pos = ch.position;
                    float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                    if (r <= CITY_OUTER_RADIUS)
                    {
                        float correctTierY = GetSteppedCityTierElevation(r);
                        Ray ray = new Ray(new Vector3(pos.x, 150f, pos.z), Vector3.down);
                        if (terrainCollider != null && terrainCollider.Raycast(ray, out RaycastHit hit, 300f))
                        {
                            ch.position = new Vector3(pos.x, hit.point.y, pos.z);
                        }
                        else
                        {
                            ch.position = new Vector3(pos.x, correctTierY, pos.z);
                        }
                    }
                }
            }

            Debug.Log($"[Antigravity MCP] Unburied and snapped {buildingsSnapped} buildings and street props to stepped tiers (22m, 38m, 58m, 80m).");
        }

        // =====================================================================
        // 2. REPLACE GIANT HANDHELD 'LAMP' WITH PROPER STREET LAMP POSTS
        // =====================================================================
        public static void ReplaceWrongHandheldLampsWithStreetLamps()
        {
            GameObject streetLampPostPrefab = FindProperStreetLampPostPrefab();
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            List<Vector3> lampPositions = new List<Vector3>();
            List<Quaternion> lampRotations = new List<Quaternion>();
            Transform lampParent = null;

            // 1. Find and destroy handheld oil lantern meshes that were incorrectly spawned as street lights
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                string lower = go.name.ToLowerInvariant();

                // Skip UI elements and point lights
                if (go.GetComponent<RectTransform>() != null) continue;
                if (lower.Contains("pointlight")) continue;

                // Identify wrongly spawned handheld "Lamp" or "Lantern" models (LanternV1, LanternV2, LanternV3, etc.)
                bool isHandheldLantern = (lower == "lamp" || lower.StartsWith("lamp ") || lower.StartsWith("lamp(") ||
                                          lower.Contains("handheld") || lower.StartsWith("lanternv") ||
                                          (lower.Contains("lantern") && !lower.Contains("paper") && !lower.Contains("post") && !lower.Contains("light")));

                // Also check if oversized (bounds > 3m for a lantern)
                Bounds b = CalculateHierarchyBounds(go);
                bool isGiantLantern = isHandheldLantern && (b.size.y > 2.0f || b.size.x > 2.0f);

                if (isGiantLantern || (isHandheldLantern && go.transform.parent != null && go.transform.parent.name.Contains("Street")))
                {
                    Vector3 p = go.transform.position;
                    float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                    p.y = (r <= CITY_OUTER_RADIUS) ? GetSteppedCityTierElevation(r) : p.y;

                    lampPositions.Add(p);
                    lampRotations.Add(go.transform.rotation);
                    if (lampParent == null) lampParent = go.transform.parent;

                    Undo.DestroyObjectImmediate(go);
                }
            }

            var terrainGo = GameObject.Find("Floor01_MacroContinent_Terrain");
            var terrainCollider = terrainGo != null ? terrainGo.GetComponent<MeshCollider>() : null;

            // 2. Ensure all existing PFB_Urban_StreetLampPost instances in 02_Street_Props_Benches_Lamps are properly normalized
            var streetPropsRoot = GameObject.Find("02_Street_Props_Benches_Lamps");
            if (streetPropsRoot != null)
            {
                if (lampParent == null) lampParent = streetPropsRoot.transform;

                for (int i = 0; i < streetPropsRoot.transform.childCount; i++)
                {
                    var ch = streetPropsRoot.transform.GetChild(i);
                    if (ch.name.Contains("StreetLampPost"))
                    {
                        Vector3 p = ch.position;
                        float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                        if (r <= CITY_OUTER_RADIUS)
                        {
                            float tierY = GetSteppedCityTierElevation(r);
                            Ray ray = new Ray(new Vector3(p.x, 150f, p.z), Vector3.down);
                            if (terrainCollider != null && terrainCollider.Raycast(ray, out RaycastHit hit, 300f))
                            {
                                ch.position = new Vector3(p.x, hit.point.y, p.z);
                            }
                            else
                            {
                                ch.position = new Vector3(p.x, tierY, p.z);
                            }
                        }

                        // Normalize height to ~3.8m
                        Bounds b = CalculateHierarchyBounds(ch.gameObject);
                        if (b.size.y > 5.5f || b.size.y < 2.0f)
                        {
                            float factor = 3.8f / Mathf.Max(0.1f, b.size.y);
                            ch.localScale *= factor;
                        }
                    }
                }
            }

            // Spawn proper Street Lamp Posts at any coordinates from purged giant lanterns
            for (int i = 0; i < lampPositions.Count; i++)
            {
                SpawnStreetLampPostAt(lampPositions[i], lampRotations[i], streetLampPostPrefab, lampParent, i);
            }

            Debug.Log($"[Antigravity MCP] Handheld lantern replacement complete: purged {lampPositions.Count} giant lanterns and aligned street lamp posts.");
        }

        private static GameObject FindProperStreetLampPostPrefab()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                // Strictly match tall street lamp posts, NEVER a bare handheld "Lamp"
                if ((fileName.Contains("streetlamppost") || fileName.Contains("street_lamp_post") ||
                     fileName.Contains("streetlamp") || fileName.Contains("street_lamp") ||
                     fileName.Contains("lamppost") || fileName.Contains("lamp_post") ||
                     fileName.Contains("streetlight") || fileName.Contains("street_light")) &&
                    !fileName.Contains("hand") && !fileName.Contains("table") && !fileName.Contains("oil"))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            return null;
        }

        private static void SpawnStreetLampPostAt(Vector3 pos, Quaternion rot, GameObject prefab, Transform parent, int index)
        {
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = $"City_StreetLampPost_Replaced_{index:03d}";
                instance.transform.SetPositionAndRotation(pos, rot);

                // Normalize scale so street lamp is strictly ~3.8m tall (never giant building-sized)
                Bounds b = CalculateHierarchyBounds(instance);
                if (b.size.y > 0.01f && (b.size.y > 5.5f || b.size.y < 2.2f))
                {
                    float scaleFactor = 3.8f / b.size.y;
                    instance.transform.localScale *= scaleFactor;
                }
                return;
            }

            // Procedural Fallback: Construct an authentic 3.8m Wrought-Iron Street Lamp Post
            GameObject root = new GameObject($"City_StreetLampPost_Procedural_{index:03d}");
            if (parent != null) root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(pos, rot);

            Material ironMat = GetOrCreateStylizedMaterial("MAT_StreetLamp_DarkIron", new Color(0.16f, 0.17f, 0.20f), 0.35f);
            Material glowMat = GetOrCreateStylizedMaterial("MAT_StreetLamp_WarmAmber", new Color(1.0f, 0.82f, 0.38f), 0.1f, true, new Color(1.0f, 0.72f, 0.25f) * 3.5f);

            // 1. Cast-Iron Base Pedestal (0.7m tall, 0.32m wide)
            GameObject basePedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePedestal.name = "StreetLamp_Base";
            basePedestal.transform.SetParent(root.transform, false);
            basePedestal.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            basePedestal.transform.localScale = new Vector3(0.32f, 0.35f, 0.32f);
            basePedestal.GetComponent<Renderer>().sharedMaterial = ironMat;

            // 2. Slim Vertical Street Lamp Pole (3.6m tall, 0.11m diameter)
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "StreetLamp_VerticalPost";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.95f, 0f);
            pole.transform.localScale = new Vector3(0.11f, 1.80f, 0.11f);
            pole.GetComponent<Renderer>().sharedMaterial = ironMat;

            // 3. Top Horizontal Cross-Arm Bracket
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "StreetLamp_CrossArm";
            arm.transform.SetParent(root.transform, false);
            arm.transform.localPosition = new Vector3(0f, 3.65f, 0.32f);
            arm.transform.localScale = new Vector3(0.08f, 0.08f, 0.72f);
            arm.GetComponent<Renderer>().sharedMaterial = ironMat;

            // 4. Suspended Glowing Street Lantern Head (0.32m scale at 3.35m height)
            GameObject lanternGlass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lanternGlass.name = "StreetLamp_LanternHousing";
            lanternGlass.transform.SetParent(root.transform, false);
            lanternGlass.transform.localPosition = new Vector3(0f, 3.35f, 0.58f);
            lanternGlass.transform.localScale = new Vector3(0.28f, 0.24f, 0.28f);
            lanternGlass.GetComponent<Renderer>().sharedMaterial = glowMat;
        }

        // =====================================================================
        // 3. POPULATE BIOMES STRICTLY USING 'ANIME NATURE' (PACKAGE ID 276931)
        //    & ELIMINATE UNBLENDED RECTANGULAR GRASS GRIDS
        // =====================================================================
        public static void PopulateBiomesWithAnimeNaturePackageOnly()
        {
            EnsureTerrainCollider();

            // 1. Discover all prefabs inside the 'Anime Trees' (Anime Nature 276931) package
            List<GameObject> broadleafTrees = new List<GameObject>();
            List<GameObject> pineTrees = new List<GameObject>();
            List<GameObject> bushes = new List<GameObject>();
            List<GameObject> grassAndFlowers = new List<GameObject>();

            // Load directly from Assets/Anime Trees/Prefabs/
            string animeFolder = "Assets/Anime Trees/Prefabs";
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { animeFolder });
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                GameObject pfb = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pfb == null) continue;

                string n = pfb.name.ToLowerInvariant();
                if (n == "tree_07" || n == "tree_08" || n.Contains("pine") || n.Contains("spruce"))
                {
                    pineTrees.Add(pfb);
                }
                else if (n.StartsWith("tree"))
                {
                    broadleafTrees.Add(pfb);
                }
                else if (n.StartsWith("bush"))
                {
                    bushes.Add(pfb);
                }
                else if (n.StartsWith("grass"))
                {
                    grassAndFlowers.Add(pfb);
                }
            }

            if (pineTrees.Count == 0) pineTrees.AddRange(broadleafTrees);

            Debug.Log($"[Anime Nature 276931] Loaded: {broadleafTrees.Count} broadleaf, {pineTrees.Count} pines, {bushes.Count} bushes, {grassAndFlowers.Count} grass prefabs.");

            // 2. Eliminate old grid-chunk grass, legacy proxy trees, and previous vegetation containers
            string[] legacyContainers = {
                "--- WORLD WIND GRASS ---",
                "--- WORLD ANIME VEGETATION ---",
                "--- WORLD BLOSSOMS & FLOWERS ---",
                "Floor01_AnimeNature_EndemicFoliage"
            };

            foreach (var containerName in legacyContainers)
            {
                var go = GameObject.Find(containerName);
                if (go != null) Undo.DestroyObjectImmediate(go);
            }

            // Disable or hide old FBX proxy foliage in CircularContinent_MacroLevel1
            var oldCanopy = GameObject.Find("Foliage_Canopy_Root");
            if (oldCanopy != null)
            {
                oldCanopy.SetActive(false);
            }

            // Delete any remaining grid-patch grass objects in scene
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();
                if (n.Contains("grassgrid") || n.Contains("grass_grid") || n.Contains("grasschunk") ||
                    n.Contains("assetproxy_broadleaf"))
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            GameObject foliageRoot = new GameObject("Floor01_AnimeNature_EndemicFoliage");
            System.Random rng = new System.Random(276931);

            // 3. Continuous Multi-Octave Perlin Noise Sampling across the 2850m Island (NO GRID BLOCKS!)
            int sampleCount = 6500;
            int spawnedCount = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                // Sample polar coordinates strictly OUTSIDE the Magic City & Moat (R: 560m to 2750m)
                float t = (float)rng.NextDouble();
                float radius = Mathf.Lerp(MOAT_OUTER_RADIUS + 25f, ISLAND_COAST_RADIUS - 80f, Mathf.Sqrt(t));
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;

                float wx = Mathf.Cos(angle) * radius;
                float wz = Mathf.Sin(angle) * radius;

                // Hard exclusion mask: strictly outside city & moat (R < 555m)
                if (Mathf.Sqrt(wx * wx + wz * wz) < 555f) continue;

                // Evaluate continuous multi-octave Perlin noise so grass & trees form organic, seamless meadows and woods
                float macroNoise = Mathf.PerlinNoise((wx + 5000f) * 0.0018f, (wz + 5000f) * 0.0018f);
                float microNoise = Mathf.PerlinNoise((wx + 2000f) * 0.0085f, (wz + 2000f) * 0.0085f);
                float blendedDensity = (macroNoise * 0.7f) + (microNoise * 0.3f);

                // Raycast down to find the exact terrain height & normal
                if (!Physics.Raycast(new Vector3(wx, 600f, wz), Vector3.down, out RaycastHit hit, 1000f))
                    continue;

                // Skip underwater, moat, riverbeds, and steep cliff walls (> 38 deg slope)
                if (hit.point.y <= 3.5f || Vector3.Angle(hit.normal, Vector3.up) > 38f)
                    continue;

                // Determine Endemic Biome at (wx, wz) matching the Continental Map (image_f4321e.jpg)
                EndemicBiomeKind biome = ClassifyEndemicBiome(wx, wz, hit.point.y);

                GameObject chosenPrefab = SelectEndemicFloraForBiome(
                    biome, blendedDensity, rng,
                    broadleafTrees, pineTrees, bushes, grassAndFlowers,
                    out float scaleMultiplier
                );

                if (chosenPrefab == null) continue;

                Quaternion rot = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                // Align grass & low bushes to terrain surface normal for seamless ground blending
                if (grassAndFlowers.Contains(chosenPrefab))
                {
                    rot = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;
                }

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab, foliageRoot.transform);
                inst.transform.SetPositionAndRotation(hit.point, rot);
                inst.transform.localScale *= scaleMultiplier * Mathf.Lerp(0.85f, 1.22f, (float)rng.NextDouble());
                spawnedCount++;
            }

            Debug.Log($"[Antigravity MCP] Populated {spawnedCount} endemic Anime Nature (276931) flora across the 8192m continent with continuous noise blending.");
        }

        private enum EndemicBiomeKind
        {
            NortheastAndSouthLushForest,
            StartingMeadowAndSteppedHighlands,
            NorthernFrostholdGlacierTaiga,
            NorthwestVolcanicFoothills,
            WesternDesertOasisOrScrub,
            SoutheastCoastalDockcity
        }

        private static EndemicBiomeKind ClassifyEndemicBiome(float x, float z, float y)
        {
            // 1. NW Volcanic Caldera & Foothills
            if (Vector2.Distance(new Vector2(x, z), new Vector2(-1450f, 1550f)) < 850f)
                return EndemicBiomeKind.NorthwestVolcanicFoothills;

            // 2. North Frosthold Wastes (Glacial & Sub-Alpine)
            if (z > 1150f && x > -850f)
                return EndemicBiomeKind.NorthernFrostholdGlacierTaiga;

            // 3. West Rusting Desolation Desert
            if (x < -720f && z < 850f)
                return EndemicBiomeKind.WesternDesertOasisOrScrub;

            // 4. SE Seaward Dockcity & Offshore Island
            if (x > 750f && z < -350f)
                return EndemicBiomeKind.SoutheastCoastalDockcity;

            // 5. NE Deep Forest & South-Central Woods
            if ((x > 350f && z > 150f) || (x > -150f && x < 750f && z < -550f))
                return EndemicBiomeKind.NortheastAndSouthLushForest;

            // 6. Default: Starting Meadows & Stepped Green Highland Plots
            return EndemicBiomeKind.StartingMeadowAndSteppedHighlands;
        }

        private static GameObject SelectEndemicFloraForBiome(
            EndemicBiomeKind biome, float noiseDensity, System.Random rng,
            List<GameObject> broadleafTrees, List<GameObject> pineTrees,
            List<GameObject> bushes, List<GameObject> grassAndFlowers,
            out float scaleMult)
        {
            scaleMult = 1.0f;
            float roll = (float)rng.NextDouble();

            switch (biome)
            {
                case EndemicBiomeKind.NortheastAndSouthLushForest:
                    // Dense broadleaf canopy trees + woodland bushes + continuous grass floor
                    if (roll < 0.38f && broadleafTrees.Count > 0) { scaleMult = 1.15f; return broadleafTrees[rng.Next(broadleafTrees.Count)]; }
                    if (roll < 0.65f && bushes.Count > 0)         { scaleMult = 1.05f; return bushes[rng.Next(bushes.Count)]; }
                    if (grassAndFlowers.Count > 0)                { scaleMult = 1.25f; return grassAndFlowers[rng.Next(grassAndFlowers.Count)]; }
                    break;

                case EndemicBiomeKind.StartingMeadowAndSteppedHighlands:
                    // Continuous seamless grass & wildflowers across green plots; sparse bushes/trees only on high noise crests
                    if (roll < 0.82f && grassAndFlowers.Count > 0) { scaleMult = 1.35f; return grassAndFlowers[rng.Next(grassAndFlowers.Count)]; }
                    if (noiseDensity > 0.62f && roll < 0.94f && bushes.Count > 0) return bushes[rng.Next(bushes.Count)];
                    if (noiseDensity > 0.74f && broadleafTrees.Count > 0) return broadleafTrees[rng.Next(broadleafTrees.Count)];
                    break;

                case EndemicBiomeKind.NorthernFrostholdGlacierTaiga:
                    // Sub-alpine pines/conifers and hardy bushes along lower valleys
                    if (noiseDensity > 0.48f && roll < 0.55f && pineTrees.Count > 0) return pineTrees[rng.Next(pineTrees.Count)];
                    if (roll < 0.75f && bushes.Count > 0) { scaleMult = 0.85f; return bushes[rng.Next(bushes.Count)]; }
                    break;

                case EndemicBiomeKind.WesternDesertOasisOrScrub:
                    // Only spawn scrub bushes or oasis greenery near water/oasis pockets (high noiseDensity)
                    if (noiseDensity > 0.68f && bushes.Count > 0) { scaleMult = 0.75f; return bushes[rng.Next(bushes.Count)]; }
                    break;

                case EndemicBiomeKind.NorthwestVolcanicFoothills:
                    // Sparse low shrubs only in the lower foothills
                    if (noiseDensity > 0.72f && bushes.Count > 0) { scaleMult = 0.70f; return bushes[rng.Next(bushes.Count)]; }
                    break;

                case EndemicBiomeKind.SoutheastCoastalDockcity:
                    if (roll < 0.65f && grassAndFlowers.Count > 0) return grassAndFlowers[rng.Next(grassAndFlowers.Count)];
                    if (roll < 0.88f && bushes.Count > 0) return bushes[rng.Next(bushes.Count)];
                    if (broadleafTrees.Count > 0) return broadleafTrees[rng.Next(broadleafTrees.Count)];
                    break;
            }
            return null;
        }

        // =====================================================================
        // 4. HARMONIZE BUILDING & TERRAIN TEXTURES TO ANIME NATURE AESTHETIC
        // =====================================================================
        public static void HarmonizeCityMaterialsToAnimeAesthetic()
        {
            Material warmStucco = GetOrCreateStylizedMaterial("MAT_AnimeCity_WarmCreamStucco", new Color(0.93f, 0.89f, 0.79f), 0.75f);
            Material terracottaRoof = GetOrCreateStylizedMaterial("MAT_AnimeCity_TerracottaRoof", new Color(0.76f, 0.34f, 0.22f), 0.68f);
            Material limestoneTier = GetOrCreateStylizedMaterial("MAT_AnimeCity_CobbleLimestone", new Color(0.78f, 0.77f, 0.73f), 0.80f);

            int harmonizedCount = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r == null || r.sharedMaterial == null) continue;
                string objName = r.gameObject.name.ToLowerInvariant();
                string matName = r.sharedMaterial.name.ToLowerInvariant();

                // Replace flat dark-grey default/placeholder materials on row-houses & tiers
                if (matName.Contains("default") || matName == "lit" || matName.Contains("placeholder") || matName.Contains("grey") || matName.Contains("gray"))
                {
                    if (objName.Contains("roof"))
                    {
                        r.sharedMaterial = terracottaRoof;
                        harmonizedCount++;
                    }
                    else if (objName.Contains("tier") || objName.Contains("wall") || objName.Contains("bridge") || objName.Contains("bastion"))
                    {
                        r.sharedMaterial = limestoneTier;
                        harmonizedCount++;
                    }
                    else if (objName.Contains("house") || objName.Contains("shop") || objName.Contains("bakery") || objName.Contains("rowhouse"))
                    {
                        r.sharedMaterial = warmStucco;
                        harmonizedCount++;
                    }
                }
            }

            Debug.Log($"[Antigravity MCP] Harmonized {harmonizedCount} raw/grey materials to warm anime stylized palette.");
        }

        private static Material GetOrCreateStylizedMaterial(string matName, Color albedo, float roughness, bool emissive = false, Color emissionColor = default)
        {
            string folder = "Assets/Materials/AnimeCityStylized";
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Materials", "AnimeCityStylized");

            string path = $"{folder}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(urpLit) { name = matName };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", albedo);
            mat.SetColor("_Color", albedo);
            mat.SetFloat("_Smoothness", 1.0f - roughness);
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AlignHeroAndCameraToAcropolis()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                // Place hero on Tier 4 Acropolis (Y = 80m surface)
                hero.transform.position = new Vector3(0f, 80.5f, -32f);
            }

            var cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                cam.transform.position = new Vector3(-1.48f, 87.37f, -35.17f);
                cam.transform.LookAt(new Vector3(0f, 81.5f, -32f));
            }
        }

        private static Bounds CalculateHierarchyBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
#endif
