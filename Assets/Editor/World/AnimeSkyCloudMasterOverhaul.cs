using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BattleSummoner.Environment;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master editor tool that transforms sky clouds from uniform, static clusters into
    /// a dynamic, multi-layered anime sky with non-uniform stretching, vertical pulling,
    /// dramatic size hierarchies (from low wisps to colossal horizon thunderheads),
    /// natural 3D orientation, and continuous atmospheric wind drifting.
    /// </summary>
    public static class AnimeSkyCloudMasterOverhaul
    {
        public enum CloudArchetype
        {
            HorizontalStratocumulusBank, // Flat, widely stretched horizontal cloud decks
            ToweringCumulonimbus,        // Tall, vertically pulled thunderheads and anvil pillars
            HighCirrusStreamer,          // Aerodynamically stretched thin wind wisps
            HorizonMegaBank,             // Colossal distant backdrop formations (5x - 8.5x scale)
            LowDriftPuff                 // Varied small-to-medium drifting clouds near peaks
        }

        private const string CLOUD_MAT_DIR = "Assets/Materials/Environment";
        private const string BASE_CLOUD_MAT_PATH = "Assets/Materials/Environment/Mat_WispySlalomCloud.mat";
        private const string GOLDEN_CLOUD_MAT_PATH = "Assets/Materials/Environment/Mat_AnimeCloud_GoldenSunlit.mat";
        private const string AZURE_CLOUD_MAT_PATH = "Assets/Materials/Environment/Mat_AnimeCloud_TwilightAzure.mat";

        [MenuItem("BattleSummoner/Sky/Overhaul Clouds (Stretch, Scale & Drift)")]
        public static void OverhaulAllSkyCloudsInScene()
        {
            Debug.Log("☁️ [SKY OVERHAUL] Initiating dynamic anime sky cloud transformation...");

            // 1. Prepare material palette
            Material matBase = AssetDatabase.LoadAssetAtPath<Material>(BASE_CLOUD_MAT_PATH);
            Material matGolden = GetOrCreateMaterialVariant(
                GOLDEN_CLOUD_MAT_PATH,
                matBase,
                new Color(1.0f, 0.98f, 0.92f, 0.52f),
                new Color(1.0f, 0.84f, 0.54f, 1.0f),
                new Color(0.68f, 0.62f, 0.82f, 1.0f)
            );
            Material matAzure = GetOrCreateMaterialVariant(
                AZURE_CLOUD_MAT_PATH,
                matBase,
                new Color(0.94f, 0.97f, 1.0f, 0.48f),
                new Color(0.74f, 0.90f, 1.0f, 1.0f),
                new Color(0.42f, 0.50f, 0.74f, 1.0f)
            );

            // 2. Load high-quality volumetric Meshy cloud models pool (rich puffy cauliflower billows)
            List<GameObject> cloudModelPool = new List<GameObject>
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environments/Sky/Meshy/Meshy_Cloud_Large.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environments/Sky/Meshy/Meshy_Cloud_Medium.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environments/Sky/Meshy/Meshy_Cloud_Small.fbx")
            };
            cloudModelPool.RemoveAll(x => x == null);

            if (cloudModelPool.Count == 0)
            {
                Debug.LogError("[SKY OVERHAUL] No Meshy cloud models found in Assets/Models/Environments/Sky/Meshy/!");
                return;
            }

            // 3. Find or setup parent roots
            var skyArch = GameObject.Find("Grand_Sky_Archipelago_System")
                ?? GameObject.Find("Sky_Archipelago_And_Clouds_Root");
            if (skyArch == null)
            {
                skyArch = new GameObject("Grand_Sky_Archipelago_System");
                Undo.RegisterCreatedObjectUndo(skyArch, "Create Sky Root");
            }

            // Clean up all old uniform clouds in all groups (using FindObjectsOfTypeAll to catch any hidden or duplicate instances)
            var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGos)
            {
                if (go == null || EditorUtility.IsPersistent(go)) continue;
                if (go.name == "Shinkai_Anime_Cumulus_Clouds" || go.name == "Slalom_Clouds_Group")
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            GameObject mainCloudsGroup = new GameObject("Shinkai_Anime_Cumulus_Clouds");
            Undo.RegisterCreatedObjectUndo(mainCloudsGroup, "Create Dynamic Clouds Group");
            mainCloudsGroup.transform.SetParent(skyArch.transform, false);

            // Global atmospheric wind heading: blowing towards East-Northeast (~52 degrees)
            Vector2 prevailingWind = new Vector2(0.62f, 0.78f).normalized;
            float windAngleDeg = Mathf.Atan2(prevailingWind.y, prevailingWind.x) * Mathf.Rad2Deg;

            int totalSpawned = 0;

            // =========================================================================
            // LAYER 1: VAST HORIZONTAL STRATOCUMULUS BANKS (14 clouds)
            // Stretched horizontally along wind vector while retaining thick pillowy volume
            // =========================================================================
            for (int i = 0; i < 14; i++)
            {
                float ang = (i * (360f / 14f) + Random.Range(-8f, 8f)) * Mathf.Deg2Rad;
                float dist = Random.Range(1400f, 2900f);
                float alt = Random.Range(460f, 720f);
                Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, alt, Mathf.Sin(ang) * dist);

                GameObject model = cloudModelPool[Random.Range(0, cloudModelPool.Count)];
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model, mainCloudsGroup.transform);
                go.name = $"Stratocumulus_Bank_{i + 1:D2}";
                go.transform.position = pos;

                // Volumetric horizontal stretching: elongated along wind, gently squashed Y (never paper-thin)
                float baseS = Random.Range(1.8f, 3.0f);
                float stretchX = Random.Range(1.6f, 2.5f);
                float thickY = Random.Range(0.80f, 1.15f); // Keeps rich puffy depth!
                float stretchZ = Random.Range(1.2f, 1.8f);
                go.transform.localScale = new Vector3(baseS * stretchX, baseS * thickY, baseS * stretchZ);

                // Rotation: aligned with wind flow + very subtle atmospheric tilt
                float yaw = windAngleDeg + Random.Range(-20f, 20f);
                float pitch = Random.Range(-3f, 3f);
                float roll = Random.Range(-3f, 3f);
                go.transform.rotation = Quaternion.Euler(pitch, yaw, roll);

                // Material styling
                Material m = (i % 3 == 0) ? matGolden : ((i % 3 == 1) ? matAzure : matBase);
                AssignMaterial(go, m);

                // Dynamic drift component
                var drifter = go.AddComponent<DynamicSkyCloudDrifter>();
                drifter.Configure(prevailingWind, Random.Range(2.0f, 3.0f), Random.Range(2.5f, 5.0f), Random.Range(0.18f, 0.28f), Random.Range(-0.04f, 0.04f), 3800f);

                totalSpawned++;
            }

            // =========================================================================
            // LAYER 2: TOWERING CUMULONIMBUS & ANVIL PILLARS (12 clouds)
            // Pulled upward on Y, forming dramatic Ghibli-esque vertical thunderheads
            // =========================================================================
            for (int i = 0; i < 12; i++)
            {
                float ang = (i * (360f / 12f) + Random.Range(-10f, 10f) + 15f) * Mathf.Deg2Rad;
                float dist = Random.Range(1300f, 2800f);
                float alt = Random.Range(420f, 650f);
                Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, alt, Mathf.Sin(ang) * dist);

                GameObject model = cloudModelPool[Random.Range(0, cloudModelPool.Count)];
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model, mainCloudsGroup.transform);
                go.name = $"Towering_Cumulonimbus_{i + 1:D2}";
                go.transform.position = pos;

                // Anisotropic scale: vertically pulled upwards for majestic height
                float baseS = Random.Range(1.8f, 2.8f);
                float baseW = Random.Range(1.0f, 1.4f);
                float pullY = Random.Range(1.6f, 2.5f); // Majestic vertical height
                float baseD = Random.Range(1.0f, 1.4f);
                go.transform.localScale = new Vector3(baseS * baseW, baseS * pullY, baseS * baseD);

                // Subtle upright orientation
                float yaw = Random.Range(0f, 360f);
                float pitch = Random.Range(-4f, 4f);
                float roll = Random.Range(-3f, 3f);
                go.transform.rotation = Quaternion.Euler(pitch, yaw, roll);

                Material m = (i % 2 == 0) ? matBase : matGolden;
                AssignMaterial(go, m);

                var drifter = go.AddComponent<DynamicSkyCloudDrifter>();
                drifter.Configure(prevailingWind, Random.Range(1.2f, 2.0f), Random.Range(2.0f, 4.0f), Random.Range(0.12f, 0.22f), Random.Range(-0.03f, 0.03f), 3800f);

                totalSpawned++;
            }

            // =========================================================================
            // LAYER 3: WIND-STREAKED CIRROCUMULUS STREAMERS (10 clouds)
            // High altitude, gracefully elongated wisps stretching across the sky
            // =========================================================================
            for (int i = 0; i < 10; i++)
            {
                float ang = (i * (360f / 10f) + Random.Range(-12f, 12f)) * Mathf.Deg2Rad;
                float dist = Random.Range(1700f, 3100f);
                float alt = Random.Range(680f, 920f); // High stratosphere
                Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, alt, Mathf.Sin(ang) * dist);

                GameObject model = cloudModelPool[Random.Range(0, cloudModelPool.Count)];
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model, mainCloudsGroup.transform);
                go.name = $"HighCirrus_Streamer_{i + 1:D2}";
                go.transform.position = pos;

                // Graceful elongation along wind direction
                float baseS = Random.Range(1.4f, 2.4f);
                float longStretch = Random.Range(2.0f, 3.2f); // Gracefully stretched billows
                float thinY = Random.Range(0.75f, 1.05f);     // Volumetric thickness
                float narrowZ = Random.Range(0.9f, 1.4f);
                go.transform.localScale = new Vector3(baseS * longStretch, baseS * thinY, baseS * narrowZ);

                // Aligned with wind trajectory
                float yaw = windAngleDeg + Random.Range(-12f, 12f);
                float pitch = Random.Range(-2f, 2f);
                float roll = Random.Range(-3f, 3f);
                go.transform.rotation = Quaternion.Euler(pitch, yaw, roll);

                AssignMaterial(go, matAzure); // Cool high-altitude periwinkle tint

                var drifter = go.AddComponent<DynamicSkyCloudDrifter>();
                drifter.Configure(prevailingWind, Random.Range(3.0f, 4.2f), Random.Range(1.5f, 3.0f), Random.Range(0.25f, 0.38f), 0.02f, 3900f);

                totalSpawned++;
            }

            // =========================================================================
            // LAYER 4: COLOSSAL HORIZON MEGA-BANKS (8 clouds)
            // Enormous backdrop formations (3.5x to 5.5x scale) providing vast world scale
            // =========================================================================
            for (int i = 0; i < 8; i++)
            {
                float ang = (i * (360f / 8f) + Random.Range(-8f, 8f) + 22.5f) * Mathf.Deg2Rad;
                float dist = Random.Range(2600f, 3600f); // Far horizon
                float alt = Random.Range(500f, 780f);
                Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, alt, Mathf.Sin(ang) * dist);

                GameObject model = cloudModelPool[0]; // Large Meshy cloud
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model, mainCloudsGroup.transform);
                go.name = $"Horizon_MegaBank_{i + 1:D2}";
                go.transform.position = pos;

                // Colossal scale: massive presence on the world edge!
                float megaScale = Random.Range(3.4f, 5.2f);
                float sx = Random.Range(1.3f, 2.0f);
                float sy = Random.Range(1.1f, 1.6f);
                float sz = Random.Range(1.2f, 1.7f);
                go.transform.localScale = new Vector3(megaScale * sx, megaScale * sy, megaScale * sz);

                float yaw = windAngleDeg + Random.Range(-25f, 25f);
                float pitch = Random.Range(-2f, 2f);
                float roll = Random.Range(-3f, 3f);
                go.transform.rotation = Quaternion.Euler(pitch, yaw, roll);

                Material m = (i % 2 == 0) ? matGolden : matAzure;
                AssignMaterial(go, m);

                var drifter = go.AddComponent<DynamicSkyCloudDrifter>();
                drifter.Configure(prevailingWind, Random.Range(0.8f, 1.4f), Random.Range(3.0f, 6.0f), Random.Range(0.08f, 0.16f), Random.Range(-0.02f, 0.02f), 4200f);

                totalSpawned++;
            }

            // =========================================================================
            // LAYER 5: LOW-ALTITUDE DRIFT PUFFS & MOUNTAIN SCUDS (14 clouds)
            // Smaller, agile clouds floating at lower altitudes near peaks and city
            // =========================================================================
            for (int i = 0; i < 14; i++)
            {
                float ang = (i * (360f / 14f) + Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
                float dist = Random.Range(850f, 1900f); // Closer to player and kingdom
                float alt = Random.Range(260f, 420f);  // Low altitude skimming
                Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, alt, Mathf.Sin(ang) * dist);

                GameObject model = cloudModelPool[Random.Range(1, cloudModelPool.Count)]; // Medium or Small
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model, mainCloudsGroup.transform);
                go.name = $"LowDrift_Puff_{i + 1:D2}";
                go.transform.position = pos;

                // Subtle asymmetric puff stretching
                float baseS = Random.Range(0.85f, 1.6f);
                float sx = Random.Range(1.1f, 1.5f);
                float sy = Random.Range(0.9f, 1.3f);
                float sz = Random.Range(1.0f, 1.4f);
                go.transform.localScale = new Vector3(baseS * sx, baseS * sy, baseS * sz);

                float yaw = Random.Range(0f, 360f);
                float pitch = Random.Range(-4f, 4f);
                float roll = Random.Range(-3f, 3f);
                go.transform.rotation = Quaternion.Euler(pitch, yaw, roll);

                AssignMaterial(go, matBase);

                var drifter = go.AddComponent<DynamicSkyCloudDrifter>();
                drifter.Configure(prevailingWind, Random.Range(2.2f, 3.4f), Random.Range(1.8f, 3.2f), Random.Range(0.22f, 0.36f), Random.Range(-0.05f, 0.05f), 3600f);

                totalSpawned++;
            }

            EditorUtility.SetDirty(skyArch);
            Debug.Log($"☁️ [SKY OVERHAUL COMPLETE] Spawned {totalSpawned} dynamically sculpted, stretched, and drifting clouds across 5 atmospheric layers.");
        }

        private static Material GetOrCreateMaterialVariant(string path, Material template, Color baseCol, Color rimCol, Color shadowCol)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                if (template != null)
                {
                    mat = new Material(template);
                }
                else
                {
                    Shader s = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyEnvironment")
                        ?? Shader.Find("Universal Render Pipeline/Lit");
                    mat = new Material(s);
                }

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);
                if (mat.HasProperty("_RimColor")) mat.SetColor("_RimColor", rimCol);
                if (mat.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", shadowCol);

                AssetDatabase.CreateAsset(mat, path);
                AssetDatabase.SaveAssets();
            }
            return mat;
        }

        private static void AssignMaterial(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                mr.sharedMaterial = mat;
            }
        }
    }
}
