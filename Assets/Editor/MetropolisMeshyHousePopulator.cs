using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Populates and permanently integrates the 4 distinct Meshy architectural house styles
    /// across the Solas Metropolis districts in OverworldMap.unity:
    /// - PFB_House_Medieval_ArtisanRow (Scale 5.53): District IIIB (Eastern Artisan Quarter) & District III (Western Craftsmen)
    /// - PFB_House_Alchemist_Apothecary (Scale 6.49): District IV (Arcane Enclave) & District II (Grand Bazaar)
    /// - PFB_House_Gothic_ScholarSpire (Scale 6.32): District I (Citadel Acropolis) & District VI (Grand Guildhall)
    /// - PFB_House_Merchant_HarborManse (Scale 6.67): District V (Seaward Harbor Vista) & District VII (Southern Gatehouse)
    /// </summary>
    public static class MetropolisMeshyHousePopulator
    {
        private const string ScenePath = "Assets/Scenes/OverworldMap.unity";
        private const string RootName = "--- METROPOLIS_MESHY_HOUSES ---";

        public struct HousePlacement
        {
            public string prefabPath;
            public string districtName;
            public string instanceName;
            public Vector3 rayOrigin;
            public float yaw;
            public float scale;
            public float groundOffset;

            public HousePlacement(string path, string district, string name, Vector3 ray, float y, float sc, float offset)
            {
                prefabPath = path;
                districtName = district;
                instanceName = name;
                rayOrigin = ray;
                yaw = y;
                scale = sc;
                groundOffset = offset;
            }
        }

        private static readonly HousePlacement[] Placements = new HousePlacement[]
        {
            // 1. District I: Acropolis East - Gothic Scholar Spire
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Gothic_ScholarSpire.prefab",
                "District_I_Citadel_Acropolis",
                "Meshy_House_GothicScholarSpire_AcropolisEast",
                new Vector3(14.5f, 100f, 32f),
                -135f,
                6.32f,
                6.00f
            ),

            // 2. District VI: Guildhall Terrace - Gothic Scholar Spire
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Gothic_ScholarSpire.prefab",
                "District_VI_Grand_Guildhall",
                "Meshy_House_GothicScholarSpire_GuildhallTerrace",
                new Vector3(-195f, 60f, 190f),
                45f,
                6.32f,
                6.00f
            ),

            // 3. District IV: Margo's Arcane Enclave - Alchemist Apothecary
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Alchemist_Apothecary.prefab",
                "District_IV_Arcane_Enclave",
                "Meshy_House_AlchemistApothecary_MargoSanctuary",
                new Vector3(-150f, 70f, 142f),
                -60f,
                6.49f,
                4.28f
            ),

            // 4. District II: Grand Bazaar Merchant Row - Alchemist Apothecary
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Alchemist_Apothecary.prefab",
                "District_II_Grand_Bazaar",
                "Meshy_House_AlchemistApothecary_BazaarRow",
                new Vector3(30f, 85f, -170f),
                -110f,
                6.49f,
                4.26f
            ),

            // 5. District IIIB: Eastern Artisan Quarter - Medieval Artisan Row
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab",
                "District_IIIB_Eastern_Artisan",
                "Meshy_House_MedievalArtisanRow_BarnabyForge",
                new Vector3(262f, 50f, 12f),
                15f,
                5.53f,
                5.25f
            ),

            // 6. District III: Western Craftsmen Forge - Medieval Artisan Row
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Medieval_ArtisanRow.prefab",
                "District_III_Western_Forge",
                "Meshy_House_MedievalArtisanRow_CraftsmenTerrace",
                new Vector3(-235f, 50f, 148f),
                110f,
                5.53f,
                5.25f
            ),

            // 7. District V: Seaward Harbor Pier Vista - Merchant Harbor Manse
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Merchant_HarborManse.prefab",
                "District_V_Seaward_Harbor",
                "Meshy_House_MerchantHarborManse_PierVista",
                new Vector3(348f, 40f, 16f),
                -80f,
                6.67f,
                4.60f
            ),

            // 8. District VII: Southern Gatehouse Bastion - Merchant Harbor Manse
            new HousePlacement(
                "Assets/Prefabs/Houses/PFB_House_Merchant_HarborManse.prefab",
                "District_VII_Southern_Gatehouse",
                "Meshy_House_MerchantHarborManse_ZephyrBastion",
                new Vector3(22f, 40f, -438f),
                10f,
                6.67f,
                4.60f
            )
        };

        [MenuItem("BattleSummoner/City/Populate Meshy Houses Into Districts", false, 5)]
        public static void PopulateMeshyHouses()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("<color=cyan><b>🏘️ [MESHY HOUSES] Populating 4 Meshy architectural house styles across Solas Metropolis...</b></color>");

            // Clean existing root if present
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject masterRoot = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(masterRoot, "Create Meshy Houses Master Root");

            var districtRoots = new Dictionary<string, Transform>();

            int count = 0;
            foreach (var p in Placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"⚠️ Could not load prefab at {p.prefabPath}");
                    continue;
                }

                if (!districtRoots.TryGetValue(p.districtName, out Transform dParent))
                {
                    var dGo = new GameObject(p.districtName);
                    dGo.transform.SetParent(masterRoot.transform, false);
                    dParent = dGo.transform;
                    districtRoots[p.districtName] = dParent;
                }

                // Raycast highest walkable ground/floor/plaza
                Vector3 spawnPos = p.rayOrigin;
                var hits = Physics.RaycastAll(p.rayOrigin, Vector3.down, 150f);
                float highestGroundY = float.MinValue;
                bool hitValid = false;
                foreach (var h in hits)
                {
                    string hn = h.collider.name;
                    if (hn.Contains("Floor") || hn.Contains("Plaza") || hn.Contains("Continent") || hn.Contains("Terrain") || hn.Contains("Roadbed") || hn.Contains("Walk"))
                    {
                        if (h.point.y > highestGroundY)
                        {
                            highestGroundY = h.point.y;
                            hitValid = true;
                        }
                    }
                }
                if (hitValid)
                {
                    spawnPos = new Vector3(p.rayOrigin.x, highestGroundY + p.groundOffset, p.rayOrigin.z);
                }
                else if (Physics.Raycast(p.rayOrigin, Vector3.down, out RaycastHit hit, 150f))
                {
                    spawnPos = hit.point + new Vector3(0f, p.groundOffset, 0f);
                }

                // If placing Alchemist house, remove colliding placeholder rowhouse
                if (p.instanceName == "Meshy_House_AlchemistApothecary_BazaarRow")
                {
                    var gh = GameObject.Find("8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses/RowHouse_R182_Building_Guildhall_Grand");
                    if (gh == null) gh = GameObject.Find("RowHouse_R182_Building_Guildhall_Grand");
                    if (gh != null) Undo.DestroyObjectImmediate(gh);
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, dParent);
                inst.name = p.instanceName;
                inst.transform.position = spawnPos;
                inst.transform.rotation = Quaternion.Euler(0f, p.yaw, 0f);
                inst.transform.localScale = new Vector3(p.scale, p.scale, p.scale);

                count++;
                Debug.Log($"✅ Placed {p.instanceName} at {spawnPos} (Scale: {p.scale})");
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green><b>🎉 [MESHY HOUSES] Successfully placed {count} architectural houses across all districts!</b></color>");
        }
    }
}
