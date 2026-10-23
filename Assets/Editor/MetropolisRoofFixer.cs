using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleSummoner.Editor
{
    public static class MetropolisRoofFixer
    {
        private const string ROOF_TEXTURE_PATH = "Assets/Textures/DarkAcademia/tex_gothic_slate_roof.jpg";

        [MenuItem("BattleSummoner/Fix Metropolis Roofs and Materials", false, 150)]
        public static void ExecuteFixAll()
        {
            Debug.Log("<color=#00E5FF><b>[ROOF FIXER] Starting Comprehensive Metropolis Roof & Material Fix...</b></color>");

            // Step 1: Configure Roof Materials (Texture, 2-Sided Cull = 0, URP Lit, Palette)
            FixRoofMaterials();

            // Step 2: Restore Original FBX Meshes for Multi-Part Buildings (Fixing Placements)
            int meshesRestored = RestoreMultiPartBuildingMeshes();

            // Step 3: Fix Submesh Materials Across All Scene Rowhouses
            int buildingsRestored = RestoreAllRowhouseSubmeshMaterials();

            // Step 4: Save Scene and AssetDatabase
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#76FF03><b>[ROOF FIXER] Completed Successfully! Restored {meshesRestored} multi-part meshes and {buildingsRestored} rowhouse material configurations.</b></color>");
        }

        private static void FixRoofMaterials()
        {
            var roofTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ROOF_TEXTURE_PATH);
            if (roofTex == null)
            {
                Debug.LogError($"[ROOF FIXER] Roof texture not found at {ROOF_TEXTURE_PATH}!");
                return;
            }

            var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[ROOF FIXER] Universal Render Pipeline/Lit shader not found!");
                return;
            }

            // Define target materials and their harmonic base colors
            var matConfigs = new Dictionary<string, (string path, Color baseColor, Vector2 tiling)>
            {
                {
                    "M_Metro_RoofSlate",
                    ("Assets/Materials/Metropolis/M_Metro_RoofSlate.mat",
                     new Color(0.48f, 0.60f, 0.74f, 1.0f), // Classic Royal Slate Blue
                     new Vector2(6f, 6f))
                },
                {
                    "M_AlchemistClayRoof",
                    ("Assets/Materials/Metropolis/M_AlchemistClayRoof.mat",
                     new Color(0.82f, 0.42f, 0.24f, 1.0f), // Authentic Terracotta Red Clay (Fixes neon cyan bug!)
                     new Vector2(6f, 6f))
                },
                {
                    "M_BakeryMossyRoof",
                    ("Assets/Materials/Metropolis/M_BakeryMossyRoof.mat",
                     new Color(0.38f, 0.55f, 0.36f, 1.0f), // Weathered Moss Green Shingle
                     new Vector2(6f, 6f))
                },
                {
                    "M_HouseMossySlate",
                    ("Assets/Materials/Metropolis/M_HouseMossySlate.mat",
                     new Color(0.44f, 0.58f, 0.50f, 1.0f), // Rustic Forest Sage Slate
                     new Vector2(6f, 6f))
                },
                {
                    "M_Metro_Roof_ArcanePlum",
                    ("Assets/Materials/Metropolis/M_Metro_Roof_ArcanePlum.mat",
                     new Color(0.58f, 0.32f, 0.50f, 1.0f), // Noble Arcane Plum Shingle
                     new Vector2(6f, 6f))
                },
                {
                    "M_Metro_Roof_WarmOchre",
                    ("Assets/Materials/Metropolis/M_Metro_Roof_WarmOchre.mat",
                     new Color(0.85f, 0.58f, 0.28f, 1.0f), // Warm Tuscan Ochre Tile
                     new Vector2(6f, 6f))
                },
                {
                    "MAT_AnimeCity_TerracottaRoof",
                    ("Assets/Materials/AnimeCityStylized/MAT_AnimeCity_TerracottaRoof.mat",
                     new Color(0.80f, 0.40f, 0.25f, 1.0f), // Stylized Terracotta Clay
                     new Vector2(6f, 6f))
                },
                {
                    "Mat_World_RoofSlate",
                    ("Assets/Materials/World/Mat_World_RoofSlate.mat",
                     new Color(0.48f, 0.60f, 0.74f, 1.0f),
                     new Vector2(6f, 6f))
                },
                {
                    "Mat_World_RoofTile",
                    ("Assets/Materials/World/Mat_World_RoofTile.mat",
                     new Color(0.82f, 0.42f, 0.24f, 1.0f),
                     new Vector2(6f, 6f))
                },
                {
                    "M_Metro_CanopyBlue",
                    ("Assets/Materials/Metropolis/M_Metro_CanopyBlue.mat",
                     new Color(0.28f, 0.48f, 0.72f, 1.0f), // Royal Navy Cloth
                     new Vector2(4f, 4f))
                },
                {
                    "M_Metro_CanopyGold",
                    ("Assets/Materials/Metropolis/M_Metro_CanopyGold.mat",
                     new Color(0.86f, 0.68f, 0.22f, 1.0f), // Warm Merchant Gold
                     new Vector2(4f, 4f))
                }
            };

            foreach (var kvp in matConfigs)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(kvp.Value.path);
                if (mat == null)
                {
                    Debug.LogWarning($"[ROOF FIXER] Material {kvp.Key} not found at {kvp.Value.path}");
                    continue;
                }

                // Ensure URP Lit shader
                if (mat.shader != urpLitShader)
                {
                    mat.shader = urpLitShader;
                }

                // CRITICAL FOR UNDERSIDE TEXTURE: Set _Cull to 0 (Two-Sided / Cull Off)
                // This ensures that when walking through streets and looking up at roof eaves and overhangs,
                // the underside faces render the full textured roof surface instead of being culled to transparent void!
                mat.SetFloat("_Cull", 0f);
                mat.SetInt("_Cull", 0);

                var clothTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Textures/Tent_Cloth_AlbedoTransparency.png");
                Texture2D assignedTex = kvp.Key.Contains("Canopy") && clothTex != null ? clothTex : roofTex;

                // Assign Gothic Slate Roof or Cloth texture
                mat.SetTexture("_BaseMap", assignedTex);
                mat.SetTextureScale("_BaseMap", kvp.Value.tiling);

                // Set Base Color Tint
                mat.SetColor("_BaseColor", kvp.Value.baseColor);

                // Pleasant satin roof finish
                mat.SetFloat("_Smoothness", 0.22f);
                mat.SetFloat("_Metallic", 0.05f);

                EditorUtility.SetDirty(mat);
                Debug.Log($"[ROOF FIXER] Configured Material '{kvp.Key}' -> 2-Sided (Cull=0), BaseMap: {assignedTex.name}, Color: {kvp.Value.baseColor}");
            }
        }

        private static int RestoreMultiPartBuildingMeshes()
        {
            // Load pristine FBX assets
            var armoryFbx = "Assets/Models/Metropolis/Building_GuildArmory.fbx";
            var alchemistFbx = "Assets/Models/Metropolis/Building_AlchemistWorkshop.fbx";
            var observatoryFbx = "Assets/Models/Metropolis/Building_WizardObservatory.fbx";

            var armoryMeshes = LoadMeshDictionary(armoryFbx);
            var alchemistMeshes = LoadMeshDictionary(alchemistFbx);
            var observatoryMeshes = LoadMeshDictionary(observatoryFbx);

            var root = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (root == null)
            {
                Debug.LogWarning("[ROOF FIXER] '01_Tier_Stepped_RowHouses' not found in active scene!");
                return 0;
            }

            int count = 0;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var building = root.transform.GetChild(i);
                string bName = building.name;

                if (bName.Contains("Building_GuildArmory"))
                {
                    count += RestoreChildMeshes(building, armoryMeshes);
                }
                else if (bName.Contains("Building_AlchemistWorkshop"))
                {
                    count += RestoreChildMeshes(building, alchemistMeshes);
                }
                else if (bName.Contains("Building_WizardObservatory"))
                {
                    count += RestoreChildMeshes(building, observatoryMeshes);
                }
            }

            return count;
        }

        private static Dictionary<string, Mesh> LoadMeshDictionary(string fbxPath)
        {
            var dict = new Dictionary<string, Mesh>();
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is Mesh m && !dict.ContainsKey(m.name))
                {
                    dict[m.name] = m;
                }
            }
            return dict;
        }

        private static int RestoreChildMeshes(Transform building, Dictionary<string, Mesh> cleanMeshes)
        {
            int restored = 0;
            foreach (Transform child in building)
            {
                if (child.name == "LIVED_IN_DETAILS" || child.name == "Foundation_Plinth") continue;

                var mf = child.GetComponent<MeshFilter>();
                if (mf != null && cleanMeshes.TryGetValue(child.name, out var cleanMesh))
                {
                    if (mf.sharedMesh != cleanMesh)
                    {
                        mf.sharedMesh = cleanMesh;
                        restored++;
                    }
                }
            }
            return restored;
        }

        private static int RestoreAllRowhouseSubmeshMaterials()
        {
            var root = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (root == null) return 0;

            // Load shared Metropolis base materials
            var woodFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_WoodFloor.mat");
            var stoneBase = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            var timberWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_TimberWood.mat");
            var plasterTimber = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_PlasterTimber.mat");
            var canopyGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_CanopyGold.mat");
            var canopyRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_CanopyRed.mat");
            var canopyBlue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_CanopyBlue.mat");
            var breadGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_BreadGold.mat");
            var marble = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Marble.mat");
            var limestone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Limestone.mat");
            var forgeFire = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_ForgeFire.mat");
            var rustedIron = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_RustedIron.mat");
            var cathedralStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_CathedralStone.mat");
            var runeGlow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_RuneGlow.mat");
            var brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_ClockworkBrass.mat");

            // Palette of roof materials for neighborhoods
            var roofMaterials = new Material[]
            {
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_RoofSlate.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_AlchemistClayRoof.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_BakeryMossyRoof.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_HouseMossySlate.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Roof_ArcanePlum.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Roof_WarmOchre.mat")
            };

            int restoredCount = 0;
            System.Random rng = new System.Random(4282026);

            for (int i = 0; i < root.transform.childCount; i++)
            {
                var house = root.transform.GetChild(i);
                string hName = house.name;

                // Pick harmonious neighborhood roof material based on angular position
                float houseAngleDeg = Mathf.Atan2(house.position.x, house.position.z) * Mathf.Rad2Deg;
                if (houseAngleDeg < 0) houseAngleDeg += 360f;
                int clusterIndex = Mathf.FloorToInt(houseAngleDeg / 8.0f) % roofMaterials.Length;
                if (rng.NextDouble() < 0.20)
                {
                    clusterIndex = (clusterIndex + rng.Next(1, roofMaterials.Length)) % roofMaterials.Length;
                }
                Material chosenRoofMat = roofMaterials[clusterIndex] ?? roofMaterials[0];

                // Check single-renderer rowhouse buildings
                var mr = house.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Material[] correctMats = null;

                    if (hName.Contains("Building_House_Residential"))
                    {
                        // 5 submeshes: WoodFloor, StoneBase, TimberWood, PlasterTimber, Roof
                        correctMats = new Material[] { woodFloor, stoneBase, timberWood, plasterTimber, chosenRoofMat };
                    }
                    else if (hName.Contains("Building_House_LowDistrict"))
                    {
                        // 4 submeshes: WoodFloor, StoneBase, Roof, TimberWood
                        correctMats = new Material[] { woodFloor, stoneBase, chosenRoofMat, timberWood };
                    }
                    else if (hName.Contains("Building_Tavern_Open"))
                    {
                        // 5 submeshes: WoodFloor, StoneBase, TimberWood, CanopyGold, Roof
                        correctMats = new Material[] { woodFloor, stoneBase, timberWood, canopyGold, chosenRoofMat };
                    }
                    else if (hName.Contains("Building_Bakery_Shop"))
                    {
                        // 6 submeshes: WoodFloor, StoneBase, TimberWood, BreadGold, CanopyGold, Roof
                        correctMats = new Material[] { woodFloor, stoneBase, timberWood, breadGold, canopyGold, chosenRoofMat };
                    }
                    else if (hName.Contains("Building_Inn_Lodge") || hName.Contains("Terrace_"))
                    {
                        // 4 submeshes: WoodFloor, StoneBase, TimberWood, Roof
                        correctMats = new Material[] { woodFloor, stoneBase, timberWood, chosenRoofMat };
                    }
                    else if (hName.Contains("Building_Guildhall_Grand"))
                    {
                        // 4 submeshes: Marble, Limestone, CanopyRed, Roof
                        correctMats = new Material[] { marble, limestone, canopyRed, chosenRoofMat };
                    }
                    else if (hName.Contains("Building_Blacksmith_OpenForge"))
                    {
                        // 5 submeshes: StoneBase, ForgeFire, TimberWood, RustedIron, Roof
                        correctMats = new Material[] { stoneBase, forgeFire, timberWood, rustedIron, chosenRoofMat };
                    }
                    else if (hName.Contains("Building_EnchanterWorkshop"))
                    {
                        // 4 submeshes: CathedralStone, RuneGlow, Roof, CanopyGold
                        correctMats = new Material[] { cathedralStone, runeGlow, chosenRoofMat, canopyGold };
                    }
                    else if (hName.Contains("Building_MagicItemStore"))
                    {
                        // 4 submeshes: Marble, CanopyGold, RuneGlow, Roof
                        correctMats = new Material[] { marble, canopyGold, runeGlow, chosenRoofMat };
                    }

                    if (correctMats != null)
                    {
                        mr.sharedMaterials = correctMats;
                        restoredCount++;
                    }
                }

                // Multi-part buildings: handle child renderers
                if (hName.Contains("Building_GuildArmory"))
                {
                    foreach (Transform child in house)
                    {
                        var childMr = child.GetComponent<MeshRenderer>();
                        if (childMr == null) continue;

                        if (child.name == "Armory_Front_Porch") childMr.sharedMaterial = timberWood;
                        else if (child.name == "Armory_Stone_Hall") childMr.sharedMaterial = stoneBase;
                        else if (child.name == "Armory_Pitched_Roof") childMr.sharedMaterial = chosenRoofMat;
                    }
                    restoredCount++;
                }
                else if (hName.Contains("Building_AlchemistWorkshop"))
                {
                    foreach (Transform child in house)
                    {
                        var childMr = child.GetComponent<MeshRenderer>();
                        if (childMr == null) continue;

                        if (child.name == "Alchemist_Brick_Chimney") childMr.sharedMaterial = stoneBase;
                        else if (child.name == "Alchemist_Cottage_Base") childMr.sharedMaterial = plasterTimber;
                        else if (child.name == "Alchemist_Clay_Roof") childMr.sharedMaterial = roofMaterials[1]; // Authentic Terracotta Clay
                    }
                    restoredCount++;
                }
                else if (hName.Contains("Building_WizardObservatory"))
                {
                    foreach (Transform child in house)
                    {
                        var childMr = child.GetComponent<MeshRenderer>();
                        if (childMr == null) continue;

                        if (child.name == "Observatory_Tower_Shaft") childMr.sharedMaterial = stoneBase;
                        else if (child.name == "Observatory_Balcony") childMr.sharedMaterial = timberWood;
                        else if (child.name == "Observatory_Telescope") childMr.sharedMaterial = brassMat;
                        else if (child.name == "Observatory_Bronze_Dome") childMr.sharedMaterial = roofMaterials[4]; // Arcane Plum
                    }
                    restoredCount++;
                }
            }

            return restoredCount;
        }
    }
}
