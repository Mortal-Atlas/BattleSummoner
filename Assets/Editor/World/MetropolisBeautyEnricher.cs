#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.Editor.World
{
    /// <summary>
    /// MetropolisBeautyEnricher
    /// Populates high-detail environmental nature (Anime Trees, Idyllic Blossom Trees, Bushes)
    /// and bustling medieval market plazas (food stalls, trader tents, produce crates, artisan barrels).
    /// </summary>
    public static class MetropolisBeautyEnricher
    {
        private const string ENRICHMENT_ROOT = "8Vector_Metropolis_Root/06_Metropolis_Beauty_Enrichment";

        [MenuItem("Antigravity/Metropolis/Enrich Nature Trees And Market Plazas")]
        public static void EnrichAll()
        {
            var masterRoot = GameObject.Find("8Vector_Metropolis_Root");
            if (masterRoot == null)
            {
                Debug.LogError("[BEAUTY ENRICHER] Could not find 8Vector_Metropolis_Root!");
                return;
            }

            var oldEnrichment = masterRoot.transform.Find("06_Metropolis_Beauty_Enrichment");
            if (oldEnrichment != null)
            {
                UnityEngine.Object.DestroyImmediate(oldEnrichment.gameObject);
            }

            var enrichRoot = new GameObject("06_Metropolis_Beauty_Enrichment");
            enrichRoot.transform.SetParent(masterRoot.transform, false);

            var natureGroveRoot = new GameObject("Anime_Nature_Tree_Groves").transform;
            natureGroveRoot.SetParent(enrichRoot.transform, false);

            var marketPlazaRoot = new GameObject("Bustling_Market_Bazaars").transform;
            marketPlazaRoot.SetParent(enrichRoot.transform, false);

            // 1. Load Tree & Bush Prefabs
            var treePrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Tree_01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Tree_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Tree_03.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Tree_04.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_02_Blue.prefab")
            };

            var bushPrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Bush_01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Bush_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Bush_03.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Anime Trees/Prefabs/Bush_04.prefab")
            };

            // 2. Load Food & Prop Prefabs
            var traderTablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG FOOD PROPS/UNIVERSAL RENDER PIPELINE/Prefabs/TraderTable.prefab");
            var traderTentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG FOOD PROPS/UNIVERSAL RENDER PIPELINE/Prefabs/TraderTent.prefab");
            var boxApplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG FOOD PROPS/UNIVERSAL RENDER PIPELINE/Prefabs/WoodenBox1 Apple.prefab");
            var boxOrangePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG FOOD PROPS/UNIVERSAL RENDER PIPELINE/Prefabs/WoodenBox1 Orange.prefab");
            var boxCheesePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG FOOD PROPS/UNIVERSAL RENDER PIPELINE/Prefabs/WoodenBox1 Cheese.prefab");
            var boxMelonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG FOOD PROPS/UNIVERSAL RENDER PIPELINE/Prefabs/WoodenBox2 Watermelon.prefab");
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Barrel 01.prefab");
            var cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Crate_01.prefab");
            var cartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab");
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_Park_Bench_01.prefab");

            System.Random rng = new System.Random(888123);

            // -------------------------------------------------------------
            // A. SPAWN ANIME TREE GROVES & FLOWER CORRIDORS ALONG BOULEVARDS
            // -------------------------------------------------------------
            // Along the 8 radial avenues (at 0, 45, 90, 135, 180, 225, 270, 315 deg)
            float[] radialAngles = new float[] { 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f, 337.5f };
            // Intermediate radii between tiers: Tier 1 (R=410, Y=22), Tier 2 (R=270, Y=38), Tier 3 (R=150, Y=58)
            (float rMin, float rMax, float yElevation)[] tierZones = new (float, float, float)[]
            {
                (360f, 430f, 22f),
                (240f, 300f, 38f),
                (120f, 180f, 58f)
            };

            int treeCount = 0;
            int bushCount = 0;

            foreach (var zone in tierZones)
            {
                foreach (float baseAngle in radialAngles)
                {
                    // Spawn 3-5 tree clusters per park pocket
                    int clusterSize = rng.Next(3, 6);
                    for (int c = 0; c < clusterSize; c++)
                    {
                        float angleOffset = (float)(rng.NextDouble() * 8.0 - 4.0);
                        float angleRad = (baseAngle + angleOffset) * Mathf.Deg2Rad;
                        float dist = Mathf.Lerp(zone.rMin, zone.rMax, (float)rng.NextDouble());

                        Vector3 treePos = new Vector3(
                            Mathf.Sin(angleRad) * dist,
                            zone.yElevation,
                            Mathf.Cos(angleRad) * dist
                        );

                        var chosenTree = treePrefabs[rng.Next(0, treePrefabs.Length)];
                        if (chosenTree != null)
                        {
                            var treeObj = (GameObject)PrefabUtility.InstantiatePrefab(chosenTree, natureGroveRoot);
                            treeObj.transform.position = treePos;
                            treeObj.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
                            float s = Mathf.Lerp(0.85f, 1.25f, (float)rng.NextDouble());
                            treeObj.transform.localScale = Vector3.one * s;
                            treeCount++;

                            // Spawn 1-2 undergrowth bushes around tree base
                            if (bushPrefabs.Length > 0 && rng.NextDouble() < 0.75)
                            {
                                var chosenBush = bushPrefabs[rng.Next(0, bushPrefabs.Length)];
                                if (chosenBush != null)
                                {
                                    Vector3 bushOffset = new Vector3(
                                        (float)(rng.NextDouble() * 2.5 - 1.25),
                                        0f,
                                        (float)(rng.NextDouble() * 2.5 - 1.25)
                                    );
                                    var bushObj = (GameObject)PrefabUtility.InstantiatePrefab(chosenBush, natureGroveRoot);
                                    bushObj.transform.position = treePos + bushOffset;
                                    bushObj.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f);
                                    bushObj.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.1f, (float)rng.NextDouble());
                                    bushCount++;
                                }
                            }
                        }
                    }
                }
            }

            // -------------------------------------------------------------
            // B. SPAWN BUSTLING MEDIEVAL MARKET BAZAAR PLAZAS
            // -------------------------------------------------------------
            // Place 6 lively market bazaar pockets across Tier 1 and Tier 2 plazas
            (Vector3 center, float heading)[] marketPlazaCenters = new (Vector3, float)[]
            {
                (new Vector3(-130f, 58f, -95f), 45f),   // Tier 3 Cathedral Plaza
                (new Vector3(-105f, 58f, -135f), 135f), // Tier 3 Artisan Guild Plaza
                (new Vector3(250f, 38f, 70f), -30f),    // Tier 2 East Bazaar Square
                (new Vector3(-240f, 38f, 90f), 60f),    // Tier 2 West Fountain Plaza
                (new Vector3(180f, 22f, 350f), 180f),   // Tier 1 North Gate Market
                (new Vector3(-280f, 22f, -280f), 120f)  // Tier 1 South-West Harbor Walk
            };

            int marketStallCount = 0;

            foreach (var plaza in marketPlazaCenters)
            {
                var plazaGroup = new GameObject($"MarketSquare_{Mathf.RoundToInt(plaza.center.x)}_{Mathf.RoundToInt(plaza.center.z)}");
                plazaGroup.transform.SetParent(marketPlazaRoot, false);
                plazaGroup.transform.position = plaza.center;
                plazaGroup.transform.rotation = Quaternion.Euler(0f, plaza.heading, 0f);

                // Trader Tent
                if (traderTentPrefab != null)
                {
                    var tent = (GameObject)PrefabUtility.InstantiatePrefab(traderTentPrefab, plazaGroup.transform);
                    tent.transform.localPosition = new Vector3(-3.5f, 0f, 0f);
                    tent.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
                    tent.transform.localScale = Vector3.one * 1.1f;
                }

                // Trader Table with Food
                if (traderTablePrefab != null)
                {
                    var table = (GameObject)PrefabUtility.InstantiatePrefab(traderTablePrefab, plazaGroup.transform);
                    table.transform.localPosition = new Vector3(0f, 0f, 0f);
                    table.transform.localRotation = Quaternion.identity;
                    table.transform.localScale = Vector3.one * 1.0f;
                    marketStallCount++;

                    // Produce Crates on Table & Floor
                    if (boxApplePrefab != null)
                    {
                        var appleBox = (GameObject)PrefabUtility.InstantiatePrefab(boxApplePrefab, plazaGroup.transform);
                        appleBox.transform.localPosition = new Vector3(1.2f, 0f, 0.4f);
                        appleBox.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
                    }
                    if (boxOrangePrefab != null)
                    {
                        var orangeBox = (GameObject)PrefabUtility.InstantiatePrefab(boxOrangePrefab, plazaGroup.transform);
                        orangeBox.transform.localPosition = new Vector3(1.2f, 0f, -0.6f);
                        orangeBox.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);
                    }
                    if (boxCheesePrefab != null)
                    {
                        var cheeseBox = (GameObject)PrefabUtility.InstantiatePrefab(boxCheesePrefab, plazaGroup.transform);
                        cheeseBox.transform.localPosition = new Vector3(-1.2f, 0f, 0.8f);
                        cheeseBox.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
                    }
                    if (boxMelonPrefab != null)
                    {
                        var melonBox = (GameObject)PrefabUtility.InstantiatePrefab(boxMelonPrefab, plazaGroup.transform);
                        melonBox.transform.localPosition = new Vector3(-1.4f, 0f, -0.6f);
                        melonBox.transform.localRotation = Quaternion.Euler(0f, -30f, 0f);
                    }
                }

                // Storage Crates & Artisan Barrels cluster
                if (barrelPrefab != null)
                {
                    var b1 = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, plazaGroup.transform);
                    b1.transform.localPosition = new Vector3(2.4f, 0f, -1.2f);
                    b1.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);

                    var b2 = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, plazaGroup.transform);
                    b2.transform.localPosition = new Vector3(2.9f, 0f, -0.6f);
                    b2.transform.localRotation = Quaternion.Euler(0f, -20f, 0f);
                }

                if (cratePrefab != null)
                {
                    var c1 = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, plazaGroup.transform);
                    c1.transform.localPosition = new Vector3(-2.6f, 0f, -1.5f);
                    c1.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);

                    var c2 = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, plazaGroup.transform);
                    c2.transform.localPosition = new Vector3(-2.6f, 0.6f, -1.5f);
                    c2.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                }

                // Handcart with goods
                if (cartPrefab != null)
                {
                    var cart = (GameObject)PrefabUtility.InstantiatePrefab(cartPrefab, plazaGroup.transform);
                    cart.transform.localPosition = new Vector3(3.8f, 0f, 1.2f);
                    cart.transform.localRotation = Quaternion.Euler(0f, -60f, 0f);
                }

                // Plaza Rest Bench
                if (benchPrefab != null)
                {
                    var bench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, plazaGroup.transform);
                    bench.transform.localPosition = new Vector3(0f, 0f, 3.2f);
                    bench.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            // Mark Scene Dirty
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"<color=#66BB6A><b>[BEAUTY ENRICHER] Done! Spawned {treeCount} Anime Trees, {bushCount} Undergrowth Bushes, and {marketStallCount} Bustling Market Bazaars!</b></color>");
        }
    }
}
#endif
