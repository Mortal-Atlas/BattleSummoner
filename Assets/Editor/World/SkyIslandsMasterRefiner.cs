#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace BattleSummoner.EditorScripts
{
    public static class SkyIslandsMasterRefiner
    {
        private const string LargeIslandPrefabPath = "Assets/Prefabs/World/PFB_FloatingIsland_Large.prefab";
        private const string MedIslandPrefabPath = "Assets/Prefabs/World/PFB_FloatingIsland_Medium.prefab";
        private const string SmallIslandPrefabPath = "Assets/Prefabs/World/PFB_FloatingIsland_Small.prefab";

        private const string LargeIslandFbxPath = "Assets/Models/Environments/Sky/FloatingIsland_Large.fbx";
        private const string MedIslandFbxPath = "Assets/Models/Environments/Sky/FloatingIsland_Medium.fbx";
        private const string SmallIslandFbxPath = "Assets/Models/Environments/Sky/FloatingIsland_Small.fbx";

        private const string BlossomTreePrefabPath = "Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab";
        private const string BlueWillowPrefabPath = "Assets/Idyllic Fantasy Nature/Prefabs/WillowTree_01_Blue.prefab";
        private const string ManaCrystalsPrefabPath = "Assets/Prefabs/Environment/Prop_ManaCrystals_Cluster.prefab";
        private const string RuinArchGlbPath = "Assets/Models/MeshyGen/Royal_Celestial_Sanctuary_Arch.glb";
        private const string ChestFbxPath = "Assets/Models/QuaterniusMegaKits/FantasyPropsMegaKit/Exports/FBX/Chest_Wood.fbx";

        private const string GrassAlbedoPath = "Assets/Idyllic Fantasy Nature/Textures/Ground/Grass/Grass_Albedo.png";
        private const string GrassNormalPath = "Assets/Idyllic Fantasy Nature/Textures/Ground/Grass/Grass_Normal.png";
        private const string RockAlbedoPath = "Assets/Idyllic Fantasy Nature/Textures/Ground/Rock/Rock_Albedo.png";
        private const string RockNormalPath = "Assets/Idyllic Fantasy Nature/Textures/Ground/Rock/Rock_Normal.png";

        public struct IslandSpec
        {
            public Vector3 pos;
            public float scale;
            public int type; // 0 = Grand (Large), 1 = Medium, 2 = Stepping (Small)
            public bool hasArch;
            public bool hasBlueWillow;
            public bool hasMasterChest;
        }

        [MenuItem("BattleSummoner/World/Refine Sky Islands and Fix Floating Items")]
        public static void RefineSkyAndFixFloatingItems()
        {
            Debug.Log("====================================================================");
            Debug.Log("🚀 [SKY OVERHAUL] INITIATING MASTER SKY ISLAND REFINEMENT & CLEANUP");
            Debug.Log("====================================================================");

            var activeScene = EditorSceneManager.GetActiveScene();

            // -----------------------------------------------------------------
            // STEP 1: PURGE ALL MISPLACED FLOATING SKY OBJECTS
            // -----------------------------------------------------------------
            int purgedCount = 0;

            // 1.1 Purge crude squashed sphere stepping stones in Sky_Layer_Aetherion
            var aetherion = GameObject.Find("Sky_Layer_Aetherion");
            if (aetherion != null)
            {
                purgedCount += aetherion.GetComponentsInChildren<Transform>(true).Length;
                Undo.DestroyObjectImmediate(aetherion);
                Debug.Log("🧹 [PURGE] Destroyed crude squashed spheres in 'Sky_Layer_Aetherion'");
            }

            // 1.2 Purge duplicate / clipping islands in Sky_Archipelago_And_Clouds_Root
            var oldSkyRoot = GameObject.Find("Sky_Archipelago_And_Clouds_Root");
            if (oldSkyRoot != null)
            {
                purgedCount += oldSkyRoot.GetComponentsInChildren<Transform>(true).Length;
                Undo.DestroyObjectImmediate(oldSkyRoot);
                Debug.Log("🧹 [PURGE] Destroyed duplicate islands in 'Sky_Archipelago_And_Clouds_Root'");
            }

            // 1.3 Purge all mid-air golden elevator platforms and orphaned Blender sky props
            var poiModel = GameObject.Find("World_And_Sky_Activities_POIs_Model");
            if (poiModel != null)
            {
                var childrenToDestroy = new List<GameObject>();
                foreach (Transform child in poiModel.transform)
                {
                    string cn = child.name;
                    if (cn.StartsWith("SkyElevatorHeist_") || cn.StartsWith("SkyIsland_"))
                    {
                        childrenToDestroy.Add(child.gameObject);
                    }
                }

                foreach (var go in childrenToDestroy)
                {
                    purgedCount += go.GetComponentsInChildren<Transform>(true).Length;
                    Undo.DestroyObjectImmediate(go);
                }
                Debug.Log($"🧹 [PURGE] Removed {childrenToDestroy.Count} ungrounded sky activity roots/platforms from 'World_And_Sky_Activities_POIs_Model'");
            }

            // 1.4 Purge the 52 mid-air floating cube slabs & spheres from Mountain_Traversal_Paths_And_Obstacles
            var mountainCubes = GameObject.Find("Mountain_Traversal_Paths_And_Obstacles");
            if (mountainCubes != null)
            {
                purgedCount += mountainCubes.GetComponentsInChildren<Transform>(true).Length;
                Undo.DestroyObjectImmediate(mountainCubes);
                Debug.Log("🧹 [PURGE] Destroyed crude floating cubes/spheres in 'Mountain_Traversal_Paths_And_Obstacles'");
            }

            // 1.5 Clean up floating houses in --- METROPOLIS_MESHY_HOUSES ---
            var meshyHouses = GameObject.Find("--- METROPOLIS_MESHY_HOUSES ---");
            if (meshyHouses != null)
            {
                purgedCount += meshyHouses.GetComponentsInChildren<Transform>(true).Length;
                Undo.DestroyObjectImmediate(meshyHouses);
                Debug.Log("🧹 [PURGE] Removed floating leftover houses in '--- METROPOLIS_MESHY_HOUSES ---'");
            }

            // 1.6 Fix 60m-floating blacksmith forge in Seaward Port Town
            var blacksmithForge = GameObject.Find("Seaward_Port_Town_Lively/04_Shipwright_Blacksmith_Forge");
            if (blacksmithForge != null)
            {
                Vector3 curPos = blacksmithForge.transform.position;
                if (curPos.y > 60f)
                {
                    blacksmithForge.transform.position = new Vector3(curPos.x, 25.4f, curPos.z);
                    Debug.Log($"🔧 [FIX] Grounded '04_Shipwright_Blacksmith_Forge' from Y={curPos.y:F1} to Y=25.4m");
                }
            }

            // -----------------------------------------------------------------
            // STEP 2: CREATE / PREPARE REFINED CELESTIAL ISLAND MATERIALS
            // -----------------------------------------------------------------
            Material grassMat = GetOrCreateIslandMaterial(
                "Assets/Materials/M_CelestialIsland_Grass.mat",
                GrassAlbedoPath, GrassNormalPath,
                new Color(0.40f, 0.78f, 0.35f, 1f),
                new Vector2(8f, 8f), 0.12f
            );

            Material rockMat = GetOrCreateIslandMaterial(
                "Assets/Materials/M_CelestialIsland_Rock.mat",
                RockAlbedoPath, RockNormalPath,
                new Color(0.68f, 0.65f, 0.62f, 1f),
                new Vector2(6f, 6f), 0.15f
            );

            Material archMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CityStoneCurb.mat")
                ?? rockMat;

            // -----------------------------------------------------------------
            // STEP 3: REBUILD & REFINE THE CELESTIAL SKY ARCHIPELAGO
            // -----------------------------------------------------------------
            while (true)
            {
                var existingArch = GameObject.Find("Grand_Sky_Archipelago_System");
                if (existingArch == null) break;
                Undo.DestroyObjectImmediate(existingArch);
            }

            GameObject skyRoot = new GameObject("Grand_Sky_Archipelago_System");
            Undo.RegisterCreatedObjectUndo(skyRoot, "Create Sky Root");

            GameObject islandsGroup = new GameObject("Celestial_Floating_Islands");
            Undo.RegisterCreatedObjectUndo(islandsGroup, "Create Islands Group");
            islandsGroup.transform.SetParent(skyRoot.transform, false);

            // Load island assets
            GameObject islandLargeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LargeIslandPrefabPath)
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(LargeIslandFbxPath);
            GameObject islandMedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MedIslandPrefabPath)
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(MedIslandFbxPath);
            GameObject islandSmallAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SmallIslandPrefabPath)
                ?? AssetDatabase.LoadAssetAtPath<GameObject>(SmallIslandFbxPath);

            // Cache split meshes so we don't recreate for each instance of same size
            Dictionary<int, Mesh> splitMeshCache = new Dictionary<int, Mesh>();

            // Load decor assets
            GameObject sakuraTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlossomTreePrefabPath);
            GameObject blueWillowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BlueWillowPrefabPath) ?? sakuraTreePrefab;
            GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManaCrystalsPrefabPath);
            GameObject ruinArchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RuinArchGlbPath);
            GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChestFbxPath);

            // 16 Celestial Island Specifications
            List<IslandSpec> specs = new List<IslandSpec>
            {
                // Grand Citadel Islands (Majestic outer landmarks)
                new IslandSpec { pos = new Vector3(480f, 440f, 260f), scale = 1.45f, type = 0, hasArch = true, hasBlueWillow = true, hasMasterChest = true },
                new IslandSpec { pos = new Vector3(-680f, 490f, 720f), scale = 1.50f, type = 0, hasArch = true, hasBlueWillow = false, hasMasterChest = true },
                new IslandSpec { pos = new Vector3(1150f, 560f, -780f), scale = 1.65f, type = 0, hasArch = true, hasBlueWillow = true, hasMasterChest = true },
                new IslandSpec { pos = new Vector3(-1280f, 530f, -590f), scale = 1.55f, type = 0, hasArch = true, hasBlueWillow = false, hasMasterChest = true },

                // Medium Sanctuary Islands (Mid-tier sanctuaries)
                new IslandSpec { pos = new Vector3(260f, 380f, 540f), scale = 1.30f, type = 1, hasArch = false, hasBlueWillow = true, hasMasterChest = true },
                new IslandSpec { pos = new Vector3(-340f, 410f, 1050f), scale = 1.35f, type = 1, hasArch = true, hasBlueWillow = false, hasMasterChest = false },
                new IslandSpec { pos = new Vector3(820f, 450f, 920f), scale = 1.25f, type = 1, hasArch = true, hasBlueWillow = true, hasMasterChest = true },
                new IslandSpec { pos = new Vector3(-920f, 470f, 220f), scale = 1.35f, type = 1, hasArch = false, hasBlueWillow = false, hasMasterChest = false },
                new IslandSpec { pos = new Vector3(640f, 480f, -1350f), scale = 1.40f, type = 1, hasArch = true, hasBlueWillow = true, hasMasterChest = true },
                new IslandSpec { pos = new Vector3(-420f, 510f, -880f), scale = 1.30f, type = 1, hasArch = false, hasBlueWillow = false, hasMasterChest = false },

                // Stepping Stone Sanctuary Islands (Graceful ascending bridge)
                new IslandSpec { pos = new Vector3(180f, 350f, 320f), scale = 1.15f, type = 2, hasArch = false, hasBlueWillow = false, hasMasterChest = false },
                new IslandSpec { pos = new Vector3(-120f, 375f, 440f), scale = 1.20f, type = 2, hasArch = false, hasBlueWillow = true, hasMasterChest = false },
                new IslandSpec { pos = new Vector3(-380f, 420f, 380f), scale = 1.15f, type = 2, hasArch = false, hasBlueWillow = false, hasMasterChest = false },
                new IslandSpec { pos = new Vector3(-510f, 460f, 120f), scale = 1.25f, type = 2, hasArch = false, hasBlueWillow = true, hasMasterChest = false },
                new IslandSpec { pos = new Vector3(-460f, 520f, -280f), scale = 1.15f, type = 2, hasArch = false, hasBlueWillow = false, hasMasterChest = false },
                new IslandSpec { pos = new Vector3(-240f, 570f, -580f), scale = 1.30f, type = 2, hasArch = false, hasBlueWillow = true, hasMasterChest = false }
            };

            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                GameObject template = spec.type == 0 ? islandLargeAsset : (spec.type == 1 ? islandMedAsset : islandSmallAsset);
                if (template == null) continue;

                GameObject island = (GameObject)PrefabUtility.InstantiatePrefab(template, islandsGroup.transform);
                if (island == null) island = UnityEngine.Object.Instantiate(template, islandsGroup.transform);

                string typeName = spec.type == 0 ? "Grand" : (spec.type == 1 ? "Medium" : "Stepping");
                island.name = $"Celestial_SkyIsland_{i + 1:D2}_{typeName}";
                island.transform.position = spec.pos;
                island.transform.rotation = Quaternion.Euler(0f, i * 47f + 15f, 0f);
                island.transform.localScale = Vector3.one * spec.scale;

                // Configure Island MeshRenderer with Two-Tone Split: Grass (top) and Rock (sides/bottom)
                var meshRenderers = island.GetComponentsInChildren<MeshRenderer>(true);
                float plateauY = spec.pos.y;

                foreach (var mr in meshRenderers)
                {
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        // Split into 2 submeshes: Submesh 0 (Grass Plateau), Submesh 1 (Rocky Crag & Underside)
                        if (!splitMeshCache.TryGetValue(spec.type, out Mesh twoToneMesh))
                        {
                            twoToneMesh = SplitIslandMesh(mf.sharedMesh, mr.transform.localRotation);
                            splitMeshCache[spec.type] = twoToneMesh;
                        }

                        mf.sharedMesh = twoToneMesh;
                        mr.sharedMaterials = new Material[] { grassMat, rockMat };

                        // Calculate exact maximum vertex Y in world space for the flat plateau
                        Vector3[] verts = twoToneMesh.vertices;
                        float maxY = float.MinValue;
                        foreach (var v in verts)
                        {
                            Vector3 wv = mr.transform.TransformPoint(v);
                            if (wv.y > maxY) maxY = wv.y;
                        }
                        if (maxY > float.MinValue + 10f) plateauY = maxY;

                        // Ensure precise MeshCollider for grounding & collision
                        var mc = mr.GetComponent<MeshCollider>();
                        if (mc == null) mc = mr.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = twoToneMesh;
                    }
                }

                Vector3 plateauCenter = new Vector3(spec.pos.x, plateauY, spec.pos.z);

                // --- 1. Decorate: Sacred Trees ---
                GameObject chosenTree = spec.hasBlueWillow ? blueWillowPrefab : sakuraTreePrefab;
                if (chosenTree != null)
                {
                    float treeDist = spec.type == 0 ? 8.0f : (spec.type == 1 ? 4.5f : 2.0f);
                    Vector3 treePos = plateauCenter + new Vector3(-treeDist, 0f, -treeDist * 0.4f);

                    GameObject tObj = (GameObject)PrefabUtility.InstantiatePrefab(chosenTree, island.transform);
                    if (tObj == null) tObj = UnityEngine.Object.Instantiate(chosenTree, island.transform);

                    tObj.name = "Island_SacredTree";
                    tObj.transform.position = treePos;
                    tObj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    tObj.transform.localScale = Vector3.one * (spec.type == 0 ? 1.5f : (spec.type == 1 ? 1.3f : 1.1f));

                    // Grand islands get a second tree (Blue Willow + Sakura)
                    if (spec.type == 0 && sakuraTreePrefab != null)
                    {
                        Vector3 tree2Pos = plateauCenter + new Vector3(treeDist * 1.1f, 0f, treeDist * 0.5f);
                        GameObject t2Obj = (GameObject)PrefabUtility.InstantiatePrefab(sakuraTreePrefab, island.transform);
                        if (t2Obj == null) t2Obj = UnityEngine.Object.Instantiate(sakuraTreePrefab, island.transform);

                        t2Obj.name = "Island_SacredTree_Auxiliary";
                        t2Obj.transform.position = tree2Pos;
                        t2Obj.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                        t2Obj.transform.localScale = Vector3.one * 1.35f;
                    }
                }

                // --- 2. Decorate: Luminous Celestial Mana Crystals ---
                if (crystalPrefab != null)
                {
                    float dist = spec.type == 0 ? 13f : (spec.type == 1 ? 7f : 3.5f);
                    Vector3 crystalPos = plateauCenter + new Vector3(dist * 0.7f, 0f, -dist * 0.7f);

                    GameObject cry = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab, island.transform);
                    if (cry == null) cry = UnityEngine.Object.Instantiate(crystalPrefab, island.transform);

                    cry.name = "Island_ManaCrystal_Spire";
                    cry.transform.position = crystalPos;
                    cry.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    cry.transform.localScale = Vector3.one * (spec.type == 0 ? 1.6f : 1.25f);

                    // Add soft celestial point light
                    GameObject glowGo = new GameObject("Crystal_CelestialGlow");
                    glowGo.transform.SetParent(cry.transform, false);
                    glowGo.transform.localPosition = Vector3.up * 1.5f;
                    Light l = glowGo.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.color = new Color(0.2f, 0.72f, 1.0f); // Ethereal cyan/azure
                    l.range = 30f * spec.scale;
                    l.intensity = 2.6f;
                }

                // --- 3. Decorate: Ancient Ruin Archway ---
                if (spec.hasArch && ruinArchPrefab != null)
                {
                    float archDist = spec.type == 0 ? 15f : 8f;
                    Vector3 archPos = plateauCenter + new Vector3(-archDist * 0.5f, 0f, archDist * 0.8f);

                    GameObject arch = (GameObject)PrefabUtility.InstantiatePrefab(ruinArchPrefab, island.transform);
                    if (arch == null) arch = UnityEngine.Object.Instantiate(ruinArchPrefab, island.transform);

                    arch.name = "Island_AncientRuin_Arch";
                    arch.transform.position = archPos;
                    Vector3 lookDir = (plateauCenter - archPos).normalized;
                    lookDir.y = 0f;
                    if (lookDir != Vector3.zero) arch.transform.rotation = Quaternion.LookRotation(lookDir);
                    arch.transform.localScale = Vector3.one * (spec.type == 0 ? 1.4f : 1.1f);

                    // Apply weathered ancient cliff stone material to the arch
                    foreach (var r in arch.GetComponentsInChildren<MeshRenderer>())
                    {
                        r.sharedMaterial = archMat;
                    }
                }

                // --- 4. Decorate: Celestial Master Treasure Chest ---
                if (spec.hasMasterChest && chestPrefab != null)
                {
                    GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    plinth.name = "Island_CelestialChest_Plinth";
                    plinth.transform.SetParent(island.transform, false);
                    plinth.transform.position = plateauCenter + new Vector3(0.5f, 0.12f, 2.5f);
                    plinth.transform.localScale = new Vector3(2.5f, 0.12f, 2.5f);
                    plinth.GetComponent<Renderer>().sharedMaterial = archMat;

                    GameObject chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, plinth.transform);
                    if (chest == null) chest = UnityEngine.Object.Instantiate(chestPrefab, plinth.transform);

                    chest.name = "Celestial_MasterChest";
                    chest.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                    chest.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    chest.transform.localScale = Vector3.one * 1.35f;

                    // Golden aura light
                    GameObject chestLightGo = new GameObject("Chest_AuraLight");
                    chestLightGo.transform.SetParent(plinth.transform, false);
                    chestLightGo.transform.localPosition = Vector3.up * 1.2f;
                    Light cl = chestLightGo.AddComponent<Light>();
                    cl.type = LightType.Point;
                    cl.color = new Color(1.0f, 0.85f, 0.35f);
                    cl.range = 8f;
                    cl.intensity = 1.6f;
                }
            }

            Debug.Log($"✨ [SKY ARCHIPELAGO] Created 16 refined Two-Tone Celestial Floating Islands with grounded sacred trees, luminous crystals, and ruin arches.");

            // -----------------------------------------------------------------
            // STEP 4: REGENERATE PRISTINE ANIME CUMULUS CLOUDS
            // -----------------------------------------------------------------
            AnimeSkyCloudMasterOverhaul.OverhaulAllSkyCloudsInScene();
            Debug.Log("☁️ [CLOUDS] Regenerated pristine Makoto Shinkai anime cloud banks.");

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log($"🎉 [SKY OVERHAUL] SUCCESS! Purged {purgedCount} misplaced floating items and refined the entire Sky Archipelago!");
        }

        private static Mesh SplitIslandMesh(Mesh srcMesh, Quaternion localRot)
        {
            Mesh m = UnityEngine.Object.Instantiate(srcMesh);
            m.name = srcMesh.name + "_TwoTone";

            int[] triangles = m.triangles;
            Vector3[] verts = m.vertices;
            Vector3[] normals = m.normals;

            List<int> grassTriangles = new List<int>();
            List<int> rockTriangles = new List<int>();

            // Find the highest vertex Y in transformed space
            float maxY = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                float ty = (localRot * verts[i]).y;
                if (ty > maxY) maxY = ty;
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                Vector3 n0 = localRot * normals[i0];
                Vector3 n1 = localRot * normals[i1];
                Vector3 n2 = localRot * normals[i2];
                Vector3 avgN = (n0 + n1 + n2).normalized;

                float avgY = ((localRot * verts[i0]).y + (localRot * verts[i1]).y + (localRot * verts[i2]).y) / 3f;

                // Face normal points generally upward (avgN.y > 0.35) and is near the top plateau (within 3.5m of maxY)
                if (avgN.y > 0.35f && avgY >= (maxY - 3.5f))
                {
                    grassTriangles.Add(i0);
                    grassTriangles.Add(i1);
                    grassTriangles.Add(i2);
                }
                else
                {
                    rockTriangles.Add(i0);
                    rockTriangles.Add(i1);
                    rockTriangles.Add(i2);
                }
            }

            m.subMeshCount = 2;
            m.SetTriangles(grassTriangles.ToArray(), 0); // Slot 0: Grass
            m.SetTriangles(rockTriangles.ToArray(), 1);  // Slot 1: Rock
            m.RecalculateBounds();
            m.RecalculateNormals();
            m.RecalculateTangents();
            return m;
        }

        private static Material GetOrCreateIslandMaterial(string matPath, string albedoPath, string normalPath, Color tint, Vector2 tiling, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

                if (albedoTex != null) mat.SetTexture("_BaseMap", albedoTex);
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }

                mat.SetColor("_BaseColor", tint);
                mat.SetTextureScale("_BaseMap", tiling);
                if (normalTex != null) mat.SetTextureScale("_BumpMap", tiling);
                mat.SetFloat("_Smoothness", smoothness);

                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
            }
            return mat;
        }
    }
}
#endif
