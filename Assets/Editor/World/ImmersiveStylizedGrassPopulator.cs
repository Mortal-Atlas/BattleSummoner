using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Optimization;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master Immersive Stylized Grass Populator
    /// Completely blankets all green floor/meadows of the circular continent with dense,
    /// interlocking, waving stylized anime grass and wildflowers (Zelda: TotK / Genshin Impact quality).
    /// Eliminates all patchiness and bald gaps while maintaining high framerates via GPU instancing
    /// and 80m spatial chunking with CameraFrustumPropUnloader.
    /// </summary>
    public static class ImmersiveStylizedGrassPopulator
    {
        private const string SCENE_PATH = "Assets/Scenes/OverworldMap.unity";
        private const string ROOT_NAME = "--- DYNAMIC STYLIZED GRASS & WILDFLOWERS ---";

        private const string NYXIS_GRASS_PREFAB = "Assets/Nyxis_GrassInteractionSystem/Prefab/Grass_Patch.prefab";
        private const string STYLIZED_GRASS_PREFAB = "Assets/Anime Trees/Prefabs/GrassMesh.prefab";
        private const string ALBEDO_TEX_PATH = "Assets/Textures/Environments/Continent/T_Continent_MacroAlbedo.png";
        private const string BIOME_MASK_PATH = "Assets/Textures/Environments/Continent/T_Continent_BiomeMask.png";

        private static readonly string[] FLOWER_PREFAB_PATHS = new string[]
        {
            "Assets/Prefabs/Flowers/PFB_Flower_01.prefab",
            "Assets/Prefabs/Flowers/PFB_Flower_03.prefab",
            "Assets/Prefabs/Flowers/PFB_Flower_05.prefab",
            "Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab",
            "Assets/Anime Trees/Prefabs/Bush_01.prefab",
            "Assets/Anime Trees/Prefabs/Bush_02.prefab"
        };

        private const int LAYER_GRASS = 6; // Foliage_Grass

        [MenuItem("BattleSummoner/World Refinement/Populate Immersive Stylized Grass (Every Inch Covered)", false, 1)]
        public static string PopulateEveryInchOfGrass()
        {
            return PopulateEveryInchOfGrass(true);
        }

        public static string PopulateEveryInchOfGrass(bool saveScene)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            var continentObj = GameObject.Find("CircularContinent_MacroLevel1");
            if (continentObj == null)
            {
                Debug.LogError("[Immersive Grass Engine] CircularContinent_MacroLevel1 not found in scene!");
                return "Failed: CircularContinent_MacroLevel1 missing";
            }

            var terrainCollider = continentObj.GetComponent<MeshCollider>();
            if (terrainCollider == null)
            {
                terrainCollider = continentObj.GetComponentInChildren<MeshCollider>();
            }
            if (terrainCollider == null)
            {
                Debug.LogError("[Immersive Grass Engine] No MeshCollider found on CircularContinent_MacroLevel1!");
                return "Failed: MeshCollider missing";
            }

            var dynamicGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NYXIS_GRASS_PREFAB);
            var stylizedGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(STYLIZED_GRASS_PREFAB);

            var flowerPrefabs = new List<GameObject>();
            foreach (var path in FLOWER_PREFAB_PATHS)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null) flowerPrefabs.Add(p);
            }

            var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ALBEDO_TEX_PATH);
            var biomeMaskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(BIOME_MASK_PATH);

            EnsureReadableTexture(albedoTex);
            EnsureReadableTexture(biomeMaskTex);

            Color32[] aPix = (albedoTex != null) ? albedoTex.GetPixels32() : null;
            Color32[] mPix = (biomeMaskTex != null) ? biomeMaskTex.GetPixels32() : null;
            int aW = (albedoTex != null) ? albedoTex.width : 0;
            int aH = (albedoTex != null) ? albedoTex.height : 0;
            int mW = (biomeMaskTex != null) ? biomeMaskTex.width : 0;
            int mH = (biomeMaskTex != null) ? biomeMaskTex.height : 0;

            // Destroy any previous grass root cleanly
            var oldWindGrass = GameObject.Find(ROOT_NAME);
            if (oldWindGrass != null)
            {
                Undo.DestroyObjectImmediate(oldWindGrass);
            }

            var grassRoot = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(grassRoot, "Populate Immersive Stylized Grass");

            // Attach / verify WindGrassInteractionDriver on Player_Hero
            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                var driver = hero.GetComponent<BattleSummoner.Environment.WindGrassInteractionDriver>();
                if (driver == null)
                {
                    driver = Undo.AddComponent<BattleSummoner.Environment.WindGrassInteractionDriver>(hero);
                }
                driver.SetInteractionRadius(3.5f);
            }

            var grassChunks = new Dictionary<Vector2Int, Transform>();
            int totalGrassSpawned = 0;
            int totalFlowersSpawned = 0;

            UnityEngine.Random.InitState(424242);

            // Two-tier dense population system:
            // Tier 1: Inhabited & Active Exploration Realm (Radius <= 1280m) at 8.0m step with 4-patch interlocking clusters (Center + 3 Satellites)
            // Tier 2: Outer Mountain Horizon & Wilderness (1280m < Radius <= 2250m) at 16.0m step for background parallax
            float innerStep = 8.0f;
            float outerStep = 16.0f;
            float realmRadius = 1280f;
            float maxCoverageRadius = 2250f;
            float innerMoatRadius = 470f;

            // --- TIER 1: ACTIVE PLAYABLE REALM (DENSE INTERLOCKING CARPET) ---
            for (float x = -realmRadius; x <= realmRadius; x += innerStep)
            {
                float rowOffset = ((Mathf.FloorToInt((x + 10000f) / innerStep) % 2) != 0) ? (0.5f * innerStep) : 0f;

                for (float z = -realmRadius; z <= realmRadius; z += innerStep)
                {
                    float jx = UnityEngine.Random.Range(-0.20f, 0.20f) * innerStep;
                    float jz = UnityEngine.Random.Range(-0.20f, 0.20f) * innerStep;

                    float candidateX = x + jx;
                    float candidateZ = z + rowOffset + jz;

                    float distSq = candidateX * candidateX + candidateZ * candidateZ;

                    // Skip central citadel/moat water (dist < 470m) and outer boundary
                    if (distSq < innerMoatRadius * innerMoatRadius || distSq > realmRadius * realmRadius) continue;

                    // Exclude volcano crater and lava fields around (-1140, 1360)
                    float vx = candidateX + 1140f;
                    float vz = candidateZ - 1360f;
                    if (vx * vx + vz * vz < 460f * 460f) continue;

                    // Exclude main cardinal bridge stone approaches (N, S, E, W within 470m - 520m and width 16m)
                    if (distSq <= 520f * 520f)
                    {
                        if ((Mathf.Abs(candidateX) < 14f && Mathf.Abs(candidateZ) < 520f) || (Mathf.Abs(candidateZ) < 14f && Mathf.Abs(candidateX) < 520f))
                        {
                            continue;
                        }
                    }

                    // Green floor detection using raw Color32 arrays
                    bool isAlbedoGreen = false;
                    if (aPix != null && aW > 0 && aH > 0)
                    {
                        int px = Mathf.Clamp((int)((candidateX + 4096f) / 8192f * aW), 0, aW - 1);
                        int py = Mathf.Clamp((int)((candidateZ + 4096f) / 8192f * aH), 0, aH - 1);
                        Color32 a = aPix[py * aW + px];
                        isAlbedoGreen = (a.g > 38 && a.g > (int)(a.r * 0.90f) && a.g > (int)(a.b * 0.90f));
                    }

                    bool isMaskGrass = false;
                    if (mPix != null && mW > 0 && mH > 0)
                    {
                        int mx = Mathf.Clamp((int)((candidateX + 4096f) / 8192f * mW), 0, mW - 1);
                        int my = Mathf.Clamp((int)((candidateZ + 4096f) / 8192f * mH), 0, mH - 1);
                        Color32 m = mPix[my * mW + mx];
                        isMaskGrass = (m.r > 25 && m.b < 128);
                    }

                    if (!isAlbedoGreen && !isMaskGrass) continue;

                    // Raycast down to find terrain elevation and normal
                    Ray ray = new Ray(new Vector3(candidateX, 1500f, candidateZ), Vector3.down);
                    if (!terrainCollider.Raycast(ray, out RaycastHit hitCenter, 2000f)) continue;

                    // Strict water level check: ocean/river water is at Y = 18.0m
                    if (hitCenter.point.y <= 18.6f) continue;

                    // Slope check: walkable ground (steep cliffs normal.y < 0.50 remain rock)
                    if (hitCenter.normal.y < 0.50f) continue;

                    // Spatial chunk key (80m chunks)
                    Vector2Int chunkKey = new Vector2Int(Mathf.FloorToInt(hitCenter.point.x / 80f), Mathf.FloorToInt(hitCenter.point.z / 80f));
                    if (!grassChunks.TryGetValue(chunkKey, out Transform chunkTr))
                    {
                        GameObject chunkGo = new GameObject($"GrassChunk_{chunkKey.x}_{chunkKey.y}");
                        chunkGo.transform.SetParent(grassRoot.transform);
                        chunkTr = chunkGo.transform;
                        grassChunks[chunkKey] = chunkTr;
                    }

                    // 1. PRIMARY CENTER PATCH (Delicate, slender anime blades, knee-to-waist height)
                    bool isDynamic = (dynamicGrassPrefab != null) && (stylizedGrassPrefab == null || UnityEngine.Random.value < 0.50f);
                    GameObject primaryPrefab = isDynamic ? dynamicGrassPrefab : stylizedGrassPrefab;

                    var p1 = (GameObject)PrefabUtility.InstantiatePrefab(primaryPrefab, chunkTr);
                    p1.layer = LAYER_GRASS;
                    p1.transform.position = hitCenter.point;

                    Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, hitCenter.normal);
                    Quaternion yawRot1 = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    p1.transform.rotation = Quaternion.Slerp(yawRot1, slopeRot * yawRot1, 0.40f);

                    float sc1 = isDynamic ? UnityEngine.Random.Range(1.20f, 1.65f) : UnityEngine.Random.Range(1.15f, 1.50f);
                    p1.transform.localScale = Vector3.one * sc1;

                    OptimizeGrassRenderers(p1);
                    totalGrassSpawned++;

                    // 2. INTERLOCKING 3-SATELLITE RING (angles 0, 120, 240 deg, alternating prefab for rich bi-color depth)
                    float clusterBaseAngle = UnityEngine.Random.Range(0f, 360f);
                    for (int s = 0; s < 3; s++)
                    {
                        float angleDeg = clusterBaseAngle + s * 120f + UnityEngine.Random.Range(-18f, 18f);
                        float dist = UnityEngine.Random.Range(2.0f, 3.4f);
                        float rad = angleDeg * Mathf.Deg2Rad;
                        Vector3 satOffset = new Vector3(Mathf.Cos(rad) * dist, 0f, Mathf.Sin(rad) * dist);

                        Ray raySat = new Ray(hitCenter.point + satOffset + Vector3.up * 25f, Vector3.down);
                        if (terrainCollider.Raycast(raySat, out RaycastHit hitSat, 50f) && hitSat.point.y > 18.6f && hitSat.normal.y >= 0.48f)
                        {
                            var satPrefab = (s % 2 == 0) ? (isDynamic ? stylizedGrassPrefab : dynamicGrassPrefab) : primaryPrefab;
                            if (satPrefab == null) satPrefab = primaryPrefab;

                            var pSat = (GameObject)PrefabUtility.InstantiatePrefab(satPrefab, chunkTr);
                            pSat.layer = LAYER_GRASS;
                            pSat.transform.position = hitSat.point;
                            Quaternion yawRotSat = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            pSat.transform.rotation = Quaternion.Slerp(yawRotSat, Quaternion.FromToRotation(Vector3.up, hitSat.normal) * yawRotSat, 0.40f);

                            float scSat = (satPrefab == dynamicGrassPrefab) ? UnityEngine.Random.Range(1.10f, 1.50f) : UnityEngine.Random.Range(1.05f, 1.45f);
                            pSat.transform.localScale = Vector3.one * scSat;

                            OptimizeGrassRenderers(pSat);
                            totalGrassSpawned++;
                        }
                    }

                    // 3. WILDFLOWERS & BLOOMS (20% probability)
                    if (flowerPrefabs.Count > 0 && UnityEngine.Random.value < 0.20f)
                    {
                        var fPrefab = flowerPrefabs[UnityEngine.Random.Range(0, flowerPrefabs.Count)];
                        Vector3 fOffset = new Vector3(UnityEngine.Random.Range(-2.2f, 2.2f), 0f, UnityEngine.Random.Range(-2.2f, 2.2f));
                        Ray rayF = new Ray(hitCenter.point + fOffset + Vector3.up * 30f, Vector3.down);
                        if (terrainCollider.Raycast(rayF, out RaycastHit hitF, 60f) && hitF.point.y > 18.6f && hitF.normal.y >= 0.48f)
                        {
                            var fInst = (GameObject)PrefabUtility.InstantiatePrefab(fPrefab, chunkTr);
                            fInst.layer = LAYER_GRASS;
                            fInst.transform.position = hitF.point;
                            fInst.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                            fInst.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.75f, 1.20f);

                            OptimizeGrassRenderers(fInst);
                            totalFlowersSpawned++;
                        }
                    }
                }
            }

            // --- TIER 2: OUTER MOUNTAIN HORIZON & RIM WILDERNESS (PARALLAX COVERAGE) ---
            for (float x = -maxCoverageRadius; x <= maxCoverageRadius; x += outerStep)
            {
                float rowOffset = ((Mathf.FloorToInt((x + 10000f) / outerStep) % 2) != 0) ? (0.5f * outerStep) : 0f;

                for (float z = -maxCoverageRadius; z <= maxCoverageRadius; z += outerStep)
                {
                    float jx = UnityEngine.Random.Range(-0.25f, 0.25f) * outerStep;
                    float jz = UnityEngine.Random.Range(-0.25f, 0.25f) * outerStep;

                    float candidateX = x + jx;
                    float candidateZ = z + rowOffset + jz;

                    float distSq = candidateX * candidateX + candidateZ * candidateZ;

                    // Skip inner realm (already populated with high density) and outer perimeter
                    if (distSq <= realmRadius * realmRadius || distSq > maxCoverageRadius * maxCoverageRadius) continue;

                    // Exclude volcano crater and lava fields around (-1140, 1360)
                    float vx = candidateX + 1140f;
                    float vz = candidateZ - 1360f;
                    if (vx * vx + vz * vz < 460f * 460f) continue;

                    // Green floor detection using raw Color32 arrays
                    bool isAlbedoGreen = false;
                    if (aPix != null && aW > 0 && aH > 0)
                    {
                        int px = Mathf.Clamp((int)((candidateX + 4096f) / 8192f * aW), 0, aW - 1);
                        int py = Mathf.Clamp((int)((candidateZ + 4096f) / 8192f * aH), 0, aH - 1);
                        Color32 a = aPix[py * aW + px];
                        isAlbedoGreen = (a.g > 38 && a.g > (int)(a.r * 0.90f) && a.g > (int)(a.b * 0.90f));
                    }

                    bool isMaskGrass = false;
                    if (mPix != null && mW > 0 && mH > 0)
                    {
                        int mx = Mathf.Clamp((int)((candidateX + 4096f) / 8192f * mW), 0, mW - 1);
                        int my = Mathf.Clamp((int)((candidateZ + 4096f) / 8192f * mH), 0, mH - 1);
                        Color32 m = mPix[my * mW + mx];
                        isMaskGrass = (m.r > 25 && m.b < 128);
                    }

                    if (!isAlbedoGreen && !isMaskGrass) continue;

                    Ray ray = new Ray(new Vector3(candidateX, 1500f, candidateZ), Vector3.down);
                    if (!terrainCollider.Raycast(ray, out RaycastHit hitCenter, 2000f)) continue;

                    if (hitCenter.point.y <= 18.6f || hitCenter.normal.y < 0.50f) continue;

                    Vector2Int chunkKey = new Vector2Int(Mathf.FloorToInt(hitCenter.point.x / 80f), Mathf.FloorToInt(hitCenter.point.z / 80f));
                    if (!grassChunks.TryGetValue(chunkKey, out Transform chunkTr))
                    {
                        GameObject chunkGo = new GameObject($"GrassChunk_{chunkKey.x}_{chunkKey.y}");
                        chunkGo.transform.SetParent(grassRoot.transform);
                        chunkTr = chunkGo.transform;
                        grassChunks[chunkKey] = chunkTr;
                    }

                    bool isDynamic = (dynamicGrassPrefab != null) && (stylizedGrassPrefab == null || UnityEngine.Random.value < 0.50f);
                    GameObject outPrefab = isDynamic ? dynamicGrassPrefab : stylizedGrassPrefab;

                    var pOut = (GameObject)PrefabUtility.InstantiatePrefab(outPrefab, chunkTr);
                    pOut.layer = LAYER_GRASS;
                    pOut.transform.position = hitCenter.point;
                    Quaternion slopeRot = Quaternion.FromToRotation(Vector3.up, hitCenter.normal);
                    Quaternion yawRotOut = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    pOut.transform.rotation = Quaternion.Slerp(yawRotOut, slopeRot * yawRotOut, 0.40f);

                    float scOut = isDynamic ? UnityEngine.Random.Range(1.30f, 1.70f) : UnityEngine.Random.Range(1.20f, 1.60f);
                    pOut.transform.localScale = Vector3.one * scOut;

                    OptimizeGrassRenderers(pOut);
                    totalGrassSpawned++;
                }
            }

            // Update CameraFrustumPropUnloader distance threshold to 160m for far-distance lush views
            var unloader = CameraFrustumPropUnloader.Instance;
            if (unloader == null)
            {
                unloader = UnityEngine.Object.FindAnyObjectByType<CameraFrustumPropUnloader>();
            }
            if (unloader != null)
            {
                unloader.grassCullDistance = 160f;
                unloader.RefreshPropRegistry();
            }

            // Save scene if requested
            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }

            string resultMsg = $"[Immersive Grass Engine] Populated {totalGrassSpawned} grass patches and {totalFlowersSpawned} wildflowers across {grassChunks.Count} chunks! Every green inch is now densely covered.";
            Debug.Log(resultMsg);
            return resultMsg;
        }

        private static void OptimizeGrassRenderers(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            for (int r = 0; r < renderers.Length; r++)
            {
                renderers[r].shadowCastingMode = ShadowCastingMode.Off;
                renderers[r].receiveShadows = false;
                var mats = renderers[r].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null) mats[m].enableInstancing = true;
                }
            }
        }

        private static void EnsureReadableTexture(Texture2D tex)
        {
            if (tex == null) return;
            string path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
    }
}
