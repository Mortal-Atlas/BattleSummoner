using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Minigames.Fishing;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// MoatFishingSetupEditor constructs 4 scenic medieval timber fishing piers along the circular moat canal
    /// and stocks each pier with authentic swimming fish shadows (Carp, Salmon, Trout, Dorado, Finned rare fish).
    /// </summary>
    public static class MoatFishingSetupEditor
    {
        [MenuItem("Tools/BattleSummoner/Build Moat Fishing Piers & Populate Fish", false, 12)]
        public static void BuildMoatFishingEcosystem()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("🎣 Building Moat Fishing Piers and Populating Fish Shadows...");

            // Remove existing moat fishing ecosystem root if present to avoid duplicates
            var oldEcosystem = GameObject.Find("Moat_Fishing_Ecosystem");
            if (oldEcosystem != null)
            {
                Object.DestroyImmediate(oldEcosystem);
            }

            var rootGO = new GameObject("Moat_Fishing_Ecosystem");
            var metroRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metroRoot != null) rootGO.transform.SetParent(metroRoot.transform, false);

            // Wood materials - URP Lit
            var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Mat_MoatPier_WoodPlanks.mat")
                          ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Materials/Wooden_Box.mat");

            if (woodMat == null)
            {
                woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                woodMat.color = new Color(0.55f, 0.38f, 0.22f);
            }

            // Props prefabs
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var bucketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Wooden_Bucket_1.prefab");

            // Build 4 Moat Pier Hubs:
            // Moat canal width: R in [300, 345], water level Y = -0.75m.
            // South Pier: near South Gate bridge (X=16, Z=-318), jutting into moat canal with bridge gangway
            BuildSinglePierHub("MoatPier_South", rootGO.transform, new Vector3(16f, 0.20f, -318f), 180f, woodMat, cratePrefab, barrelPrefab, bucketPrefab, new Vector3(16f, -0.75f, -325f), true, new Vector3(11f, 0.20f, -318f), new Vector3(10f, 0.22f, 3.5f));
            
            // North Pier: near North Gate bridge (X=-16, Z=318), jutting north into moat canal
            BuildSinglePierHub("MoatPier_North", rootGO.transform, new Vector3(-16f, 0.20f, 318f), 0f, woodMat, cratePrefab, barrelPrefab, bucketPrefab, new Vector3(-16f, -0.75f, 325f), true, new Vector3(-11f, 0.20f, 318f), new Vector3(10f, 0.22f, 3.5f));
            
            // East Pier: near East Gate bridge (X=318, Z=16), jutting east into moat canal
            BuildSinglePierHub("MoatPier_East", rootGO.transform, new Vector3(318f, 0.20f, 16f), 90f, woodMat, cratePrefab, barrelPrefab, bucketPrefab, new Vector3(325f, -0.75f, 16f), true, new Vector3(318f, 0.20f, 11f), new Vector3(3.5f, 0.22f, 10f));
            
            // West Pier: near West Gate bridge (X=-318, Z=-16), jutting west into moat canal
            BuildSinglePierHub("MoatPier_West", rootGO.transform, new Vector3(-318f, 0.20f, -16f), 270f, woodMat, cratePrefab, barrelPrefab, bucketPrefab, new Vector3(-325f, -0.75f, -16f), true, new Vector3(-318f, 0.20f, -11f), new Vector3(3.5f, 0.22f, 10f));

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("🎉 [MOAT FISHING] Successfully placed 4 timber fishing piers and 24 swimming fish shadows throughout the kingdom moat!");
        }

        private static void BuildSinglePierHub(
            string hubName,
            Transform parent,
            Vector3 pierPos,
            float yRot,
            Material woodMat,
            GameObject cratePrefab,
            GameObject barrelPrefab,
            GameObject bucketPrefab,
            Vector3 fishCenter,
            bool addGangway = false,
            Vector3 gangwayWorldPos = default,
            Vector3 gangwayScale = default)
        {
            var hub = new GameObject(hubName);
            hub.transform.SetParent(parent, false);
            hub.transform.position = pierPos;
            hub.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // 1. Main Timber Deck (4.2m wide, 15m long, 0.25m thick)
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "PierDeck";
            deck.transform.SetParent(hub.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0f, 0f);
            deck.transform.localScale = new Vector3(4.2f, 0.25f, 15.0f);
            deck.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // 1b. Gangway walkway connecting Bridge to Pier (positioned in world coordinates)
            if (addGangway)
            {
                var gangway = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gangway.name = "PierBridgeGangway";
                gangway.transform.SetParent(hub.transform, true);
                gangway.transform.position = gangwayWorldPos;
                gangway.transform.rotation = Quaternion.identity;
                gangway.transform.localScale = gangwayScale;
                gangway.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

                var gPiling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var gCol = gPiling.GetComponent<Collider>();
                if (gCol != null) Object.DestroyImmediate(gCol);
                gPiling.name = "GangwayPiling";
                gPiling.transform.SetParent(hub.transform, true);
                gPiling.transform.position = gangwayWorldPos + new Vector3(0f, -1.9f, 0f);
                gPiling.transform.localScale = new Vector3(0.30f, 2.0f, 0.30f);
                gPiling.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            // 2. Pier Support Pilings down to canal bed (Y = -3.8m)
            Vector3[] pilingLocs = new Vector3[] {
                new Vector3(-1.9f, -1.9f, -5.5f),
                new Vector3( 1.9f, -1.9f, -5.5f),
                new Vector3(-1.9f, -1.9f,  0.0f),
                new Vector3( 1.9f, -1.9f,  0.0f),
                new Vector3(-1.9f, -1.9f,  5.5f),
                new Vector3( 1.9f, -1.9f,  5.5f),
                new Vector3(-1.9f, -1.9f,  7.2f),
                new Vector3( 1.9f, -1.9f,  7.2f)
            };

            foreach (var pl in pilingLocs)
            {
                var piling = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var col = piling.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);

                piling.name = "PierPiling";
                piling.transform.SetParent(hub.transform, false);
                piling.transform.localPosition = pl;
                piling.transform.localScale = new Vector3(0.32f, 2.0f, 0.32f);
                piling.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            // 3. Handrail posts & horizontal rails
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 2.0f;
                for (int z = -6; z <= 6; z += 3)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    var col = post.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);

                    post.name = "RailingPost";
                    post.transform.SetParent(hub.transform, false);
                    post.transform.localPosition = new Vector3(x, 0.55f, z);
                    post.transform.localScale = new Vector3(0.10f, 0.45f, 0.10f);
                    post.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                }

                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rCol = rail.GetComponent<Collider>();
                if (rCol != null) Object.DestroyImmediate(rCol);

                rail.name = "SideRail";
                rail.transform.SetParent(hub.transform, false);
                rail.transform.localPosition = new Vector3(x, 0.95f, 0f);
                rail.transform.localScale = new Vector3(0.12f, 0.10f, 14.5f);
                rail.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }

            // 4. Moat Pier Lantern Post at the water end of dock
            var lanternPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var lpCol = lanternPost.GetComponent<Collider>();
            if (lpCol != null) Object.DestroyImmediate(lpCol);

            lanternPost.name = "MoatPierLantern";
            lanternPost.transform.SetParent(hub.transform, false);
            lanternPost.transform.localPosition = new Vector3(1.7f, 1.25f, 6.8f);
            lanternPost.transform.localScale = new Vector3(0.08f, 1.25f, 0.08f);
            lanternPost.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            var lampGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var lgCol = lampGlow.GetComponent<Collider>();
            if (lgCol != null) Object.DestroyImmediate(lgCol);

            lampGlow.name = "LanternLamp";
            lampGlow.transform.SetParent(lanternPost.transform, false);
            lampGlow.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            lampGlow.transform.localScale = new Vector3(2.5f, 0.35f, 2.5f);

            var lampMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            lampMat.color = new Color(1.0f, 0.90f, 0.60f);
            lampGlow.GetComponent<MeshRenderer>().sharedMaterial = lampMat;

            var pLight = lanternPost.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.85f, 0.48f);
            pLight.range = 8.5f;
            pLight.intensity = 1.8f;

            // 5. Fishing Props (crates, barrels, bait bucket)
            if (cratePrefab != null)
            {
                var crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, hub.transform);
                crate.name = "Tackle_Crate";
                crate.transform.localPosition = new Vector3(-1.3f, 0.13f, -4.5f);
                crate.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
                crate.transform.localScale = Vector3.one * 0.85f;
            }

            if (barrelPrefab != null)
            {
                var barrel = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, hub.transform);
                barrel.name = "Fish_Barrel";
                barrel.transform.localPosition = new Vector3(-1.25f, 0.13f, -2.8f);
                barrel.transform.localRotation = Quaternion.identity;
                barrel.transform.localScale = Vector3.one * 0.85f;
            }

            if (bucketPrefab != null)
            {
                var bucket = (GameObject)PrefabUtility.InstantiatePrefab(bucketPrefab, hub.transform);
                bucket.name = "Bait_Bucket";
                bucket.transform.localPosition = new Vector3(1.35f, 0.13f, 5.5f);
                bucket.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
                bucket.transform.localScale = Vector3.one * 0.9f;
            }

            // 6. Stock 6 Active Fish Shadows in the canal around this pier
            FishShadowSize[] sizes = new FishShadowSize[] {
                FishShadowSize.Large,
                FishShadowSize.Medium,
                FishShadowSize.Huge,
                FishShadowSize.Finned,
                FishShadowSize.Small,
                FishShadowSize.Medium
            };

            var fishParent = new GameObject("MoatFishSchool");
            fishParent.transform.SetParent(hub.transform, true);

            for (int i = 0; i < sizes.Length; i++)
            {
                var fishGO = new GameObject($"FishShadow_Moat_{hubName}_{i + 1}");
                fishGO.transform.SetParent(fishParent.transform, false);

                float ang = (i * 60f + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                float rDist = Random.Range(3.5f, 10.5f);
                Vector3 fishPos = fishCenter + new Vector3(Mathf.Sin(ang) * rDist, 0f, Mathf.Cos(ang) * rDist);
                fishPos.y = -0.74f; // Float right at the water surface (water level Y = -0.75m)
                fishGO.transform.position = fishPos;

                var fish = fishGO.AddComponent<FishShadowEntity>();
                fish.shadowSize = sizes[i];
                fish.wanderRadius = new Vector2(9.0f, 9.0f);
                fish.moveSpeed = 1.25f;
                fish.BuildShadowMesh();
            }
        }
    }
}
