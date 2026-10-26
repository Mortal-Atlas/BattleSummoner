#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BattleSummoner.World.Editor
{
    /// <summary>
    /// Upgrades all houses inside the Magic City (8Vector_Metropolis_Root) to the new
    /// downloaded Quaternius Medieval Village & Fantasy Props MegaKits models.
    /// 
    /// 1. Assembles 6 distinct architectural house styles from modular FBX pieces:
    ///    - TownHome (4x6m, RoundTile roof, chimney, front timber door & windows)
    ///    - SmallCottage (4x4m, RoundTile roof, stone chimney, round door)
    ///    - LongLodge (4x8m, RoundTile roof, dual chimneys, timber & plaster)
    ///    - ArtisanForge (4x6m, Uneven brick masonry, forge chimney, wide openings)
    ///    - TwoStoryManor (6x6m, 2 stories, front balcony, wood grid facade, round roof)
    ///    - WoodGridShop (4x4m, Wood-grid plaster walls, round roof, storefront)
    /// 
    /// 2. Bakes combined single-mesh assets with 5 material submeshes mapped to Quaternius URP Lit materials:
    ///    - Submesh 0: M_Quaternius_WoodTrim
    ///    - Submesh 1: M_Quaternius_Plaster
    ///    - Submesh 2: M_Quaternius_RoundTiles
    ///    - Submesh 3: M_Quaternius_Brick
    ///    - Submesh 4: M_Quaternius_RockTrim
    /// 
    /// 3. In-place updates all 1,454 row houses in 8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses
    ///    and all district landmark houses in --- METROPOLIS_MESHY_HOUSES ---.
    ///    Preserves 100% of terrain, tiers, bridges, roads, and road-facing rotations!
    /// </summary>
    public static class QuaterniusMagicCityHouseUpgrader
    {
        private const string ScenePath = "Assets/Scenes/OverworldMap.unity";
        private const string FbxRoot = "Assets/Models/QuaterniusMegaKits/MedievalVillageMegaKit/Medieval Village MegaKit[Standard]/FBX/";
        private const string MatRoot = "Assets/Models/QuaterniusMegaKits/Materials/";
        private const string OutputAssetDir = "Assets/Models/QuaterniusCombinedHouses/";

        private static Material _woodTrimMat;
        private static Material _plasterMat;
        private static Material _roundTilesMat;
        private static Material _brickMat;
        private static Material _rockTrimMat;
        private static Material _unevenBrickMat;

        private struct PiecePlacement
        {
            public string fbxName;
            public Vector3 localPos;
            public Vector3 localEuler;

            public PiecePlacement(string name, Vector3 pos, Vector3 rot)
            {
                fbxName = name;
                localPos = pos;
                localEuler = rot;
            }
        }

        [MenuItem("BattleSummoner/Magic City/Upgrade Magic City Houses to Quaternius Pack", false, 10)]
        public static void ExecuteUpgrade()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("<color=#00E5FF><b>🏰 [MAGIC CITY HOUSES] Starting Quaternius Model Upgrade for Magic City...</b></color>");

            // 1. Ensure Materials are loaded
            LoadMaterials();

            // 2. Build or load combined house meshes (both row-house scale and landmark scale)
            if (!Directory.Exists(OutputAssetDir))
            {
                Directory.CreateDirectory(OutputAssetDir);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Mesh[] rowMeshes = GenerateOrLoadHouseMeshes(vertexScale: 0.015f, suffix: "_RowHouseScale");
            Mesh[] landmarkMeshes = GenerateOrLoadHouseMeshes(vertexScale: 1.0f, suffix: "_LandmarkScale");

            if (rowMeshes == null || rowMeshes.Length == 0)
            {
                Debug.LogError("[MAGIC CITY HOUSES] Failed to generate Quaternius house meshes!");
                return;
            }

            // 3. Upgrade all 1,454 Row Houses in 8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses
            int rowHousesUpdated = UpgradeRowHouses(rowMeshes);

            // 4. Upgrade Landmark Houses in --- METROPOLIS_MESHY_HOUSES ---
            int landmarksUpdated = UpgradeLandmarkHouses(landmarkMeshes);

            // 5. Upgrade Crafting / District Stalls in Step3_TownAndCrafting if present
            int craftingUpdated = UpgradeTownAndCrafting(landmarkMeshes);

            // 6. Save Scene and Assets
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#76FF03><b>🎉 [MAGIC CITY HOUSES] SUCCESS! Upgraded {rowHousesUpdated} Row Houses, {landmarksUpdated} Landmark Houses, and {craftingUpdated} Crafting Buildings with Quaternius models!</b></color>");
        }

        private static void LoadMaterials()
        {
            _woodTrimMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Quaternius_WoodTrim.mat");
            _plasterMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Quaternius_Plaster.mat");
            _roundTilesMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Quaternius_RoundTiles.mat");
            _brickMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Quaternius_Brick.mat");
            _rockTrimMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Quaternius_RockTrim.mat");
            _unevenBrickMat = AssetDatabase.LoadAssetAtPath<Material>(MatRoot + "M_Quaternius_UnevenBrick.mat");

            if (_woodTrimMat == null || _roundTilesMat == null)
            {
                Debug.LogWarning("[MAGIC CITY HOUSES] Quaternius materials not found at " + MatRoot);
            }
        }

        private static Mesh[] GenerateOrLoadHouseMeshes(float vertexScale, string suffix)
        {
            string[] styleNames = new string[]
            {
                "Quaternius_House_TownHome",
                "Quaternius_House_SmallCottage",
                "Quaternius_House_LongLodge",
                "Quaternius_House_ArtisanForge",
                "Quaternius_House_TwoStoryManor",
                "Quaternius_House_WoodGridShop"
            };

            Mesh[] meshes = new Mesh[styleNames.Length];
            bool needRebuild = false;

            for (int i = 0; i < styleNames.Length; i++)
            {
                string assetPath = $"{OutputAssetDir}{styleNames[i]}{suffix}.asset";
                meshes[i] = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (meshes[i] == null)
                {
                    needRebuild = true;
                }
            }

            if (needRebuild)
            {
                Debug.Log($"[MAGIC CITY HOUSES] Assembling Quaternius modular pieces into combined meshes (scale: {vertexScale})...");
                for (int i = 0; i < styleNames.Length; i++)
                {
                    List<PiecePlacement> placements = GetPlacementsForStyle(i);
                    Mesh combined = AssembleHouseMesh(placements, vertexScale, styleNames[i] + suffix);
                    if (combined != null)
                    {
                        string assetPath = $"{OutputAssetDir}{styleNames[i]}{suffix}.asset";
                        AssetDatabase.CreateAsset(combined, assetPath);
                        meshes[i] = combined;
                        Debug.Log($"[MAGIC CITY HOUSES] Saved combined mesh '{styleNames[i]}{suffix}' to {assetPath} ({combined.vertexCount} verts, {combined.subMeshCount} submeshes)");
                    }
                }
                AssetDatabase.SaveAssets();
            }

            return meshes;
        }

        private static List<PiecePlacement> GetPlacementsForStyle(int styleIndex)
        {
            List<PiecePlacement> list = new List<PiecePlacement>();

            switch (styleIndex)
            {
                // Style 0: TownHome (4x6m footprint, round tile 4x6 roof, front door + windows, chimney)
                case 0:
                    // Front Wall (Z = +3.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-1f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Door_Flat.fbx", new Vector3(1f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Door_1_Flat.fbx", new Vector3(1f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("DoorFrame_Flat_WoodDark.fbx", new Vector3(1f, 0f, 3f), new Vector3(0, 0, 0)));
                    // Back Wall (Z = -3.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-1f, 0f, -3f), new Vector3(0, 180, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(1f, 0f, -3f), new Vector3(0, 180, 0)));
                    // Left Wall (X = -2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-2f, 0f, 2f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-2f, 0f, 0f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-2f, 0f, -2f), new Vector3(0, -90, 0)));
                    // Right Wall (X = +2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(2f, 0f, 2f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(2f, 0f, 0f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(2f, 0f, -2f), new Vector3(0, 90, 0)));
                    // Roof & Chimney
                    list.Add(new PiecePlacement("Roof_RoundTiles_4x6.fbx", new Vector3(0f, 3.12f, 0f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Prop_Chimney.fbx", new Vector3(-1.2f, 5.0f, -1.5f), new Vector3(0, 0, 0)));
                    break;

                // Style 1: SmallCottage (4x4m footprint, round tile 4x4 roof, round door, round windows, chimney)
                case 1:
                    // Front Wall (Z = +2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Thin_Round.fbx", new Vector3(-1f, 0f, 2f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Door_Round.fbx", new Vector3(1f, 0f, 2f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Door_1_Round.fbx", new Vector3(1f, 0f, 2f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("DoorFrame_Round_WoodDark.fbx", new Vector3(1f, 0f, 2f), new Vector3(0, 0, 0)));
                    // Back Wall (Z = -2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-1f, 0f, -2f), new Vector3(0, 180, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Thin_Round.fbx", new Vector3(1f, 0f, -2f), new Vector3(0, 180, 0)));
                    // Left Wall (X = -2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-2f, 0f, 1f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Thin_Round.fbx", new Vector3(-2f, 0f, -1f), new Vector3(0, -90, 0)));
                    // Right Wall (X = +2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Thin_Round.fbx", new Vector3(2f, 0f, 1f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(2f, 0f, -1f), new Vector3(0, 90, 0)));
                    // Roof & Chimney
                    list.Add(new PiecePlacement("Roof_RoundTiles_4x4.fbx", new Vector3(0f, 3.12f, 0f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Prop_Chimney2.fbx", new Vector3(1.2f, 5.0f, -0.8f), new Vector3(0, 0, 0)));
                    break;

                // Style 2: LongLodge (4x8m footprint, round tile 4x8 roof, timber & plaster, dual chimneys)
                case 2:
                    // Front Wall (Z = +4.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-1f, 0f, 4f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Door_Flat.fbx", new Vector3(1f, 0f, 4f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Door_1_Flat.fbx", new Vector3(1f, 0f, 4f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("DoorFrame_Flat_WoodDark.fbx", new Vector3(1f, 0f, 4f), new Vector3(0, 0, 0)));
                    // Back Wall (Z = -4.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-1f, 0f, -4f), new Vector3(0, 180, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(1f, 0f, -4f), new Vector3(0, 180, 0)));
                    // Left Wall (X = -2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-2f, 0f, 3f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-2f, 0f, 1f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-2f, 0f, -1f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-2f, 0f, -3f), new Vector3(0, -90, 0)));
                    // Right Wall (X = +2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(2f, 0f, 3f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(2f, 0f, 1f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(2f, 0f, -1f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(2f, 0f, -3f), new Vector3(0, 90, 0)));
                    // Roof & Chimneys
                    list.Add(new PiecePlacement("Roof_RoundTiles_4x8.fbx", new Vector3(0f, 3.12f, 0f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Prop_Chimney.fbx", new Vector3(-1.2f, 5.0f, -2.5f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Prop_Chimney2.fbx", new Vector3(1.2f, 5.0f, 2.0f), new Vector3(0, 0, 0)));
                    break;

                // Style 3: ArtisanForge (4x6m footprint, uneven brick walls, stone chimney, wide flat windows)
                case 3:
                    // Front Wall (Z = +3.0)
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Window_Wide_Flat.fbx", new Vector3(-1f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Door_Flat.fbx", new Vector3(1f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Door_1_Flat.fbx", new Vector3(1f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("DoorFrame_Flat_Brick.fbx", new Vector3(1f, 0f, 3f), new Vector3(0, 0, 0)));
                    // Back Wall (Z = -3.0)
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Straight.fbx", new Vector3(-1f, 0f, -3f), new Vector3(0, 180, 0)));
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Straight.fbx", new Vector3(1f, 0f, -3f), new Vector3(0, 180, 0)));
                    // Left Wall (X = -2.0)
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Straight.fbx", new Vector3(-2f, 0f, 2f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Window_Wide_Flat.fbx", new Vector3(-2f, 0f, 0f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Straight.fbx", new Vector3(-2f, 0f, -2f), new Vector3(0, -90, 0)));
                    // Right Wall (X = +2.0)
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Window_Wide_Flat.fbx", new Vector3(2f, 0f, 2f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Straight.fbx", new Vector3(2f, 0f, 0f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_UnevenBrick_Straight.fbx", new Vector3(2f, 0f, -2f), new Vector3(0, 90, 0)));
                    // Roof & Big Forge Chimney
                    list.Add(new PiecePlacement("Roof_RoundTiles_4x6.fbx", new Vector3(0f, 3.12f, 0f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Prop_Chimney.fbx", new Vector3(-1.2f, 5.0f, 1.0f), new Vector3(0, 0, 0)));
                    break;

                // Style 4: TwoStoryManor (6x6m footprint, round tile 6x6 roof, balcony, wood grid upper facade)
                case 4:
                    // Ground Floor Front (Z = +3.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-2f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Door_Flat.fbx", new Vector3(0f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Door_1_Flat.fbx", new Vector3(0f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("DoorFrame_Flat_WoodDark.fbx", new Vector3(0f, 0f, 3f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(2f, 0f, 3f), new Vector3(0, 0, 0)));
                    // Ground Floor Back (Z = -3.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-2f, 0f, -3f), new Vector3(0, 180, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(0f, 0f, -3f), new Vector3(0, 180, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(2f, 0f, -3f), new Vector3(0, 180, 0)));
                    // Ground Floor Left (X = -3.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-3f, 0f, 2f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(-3f, 0f, 0f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-3f, 0f, -2f), new Vector3(0, -90, 0)));
                    // Ground Floor Right (X = +3.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(3f, 0f, 2f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Window_Wide_Flat.fbx", new Vector3(3f, 0f, 0f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(3f, 0f, -2f), new Vector3(0, 90, 0)));
                    // Balcony over front door
                    list.Add(new PiecePlacement("Balcony_Simple_Straight.fbx", new Vector3(0f, 3.12f, 3.2f), new Vector3(0, 0, 0)));
                    // Roof & Chimney
                    list.Add(new PiecePlacement("Roof_RoundTiles_6x6.fbx", new Vector3(0f, 3.12f, 0f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Prop_Chimney.fbx", new Vector3(-2.0f, 6.0f, -1.0f), new Vector3(0, 0, 0)));
                    break;

                // Style 5: WoodGridShop (4x4m footprint, round tile 4x4 roof, wood grid plaster facade)
                case 5:
                    // Front Wall (Z = +2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_WoodGrid.fbx", new Vector3(-1f, 0f, 2f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Door_Flat.fbx", new Vector3(1f, 0f, 2f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Door_1_Flat.fbx", new Vector3(1f, 0f, 2f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("DoorFrame_Flat_WoodDark.fbx", new Vector3(1f, 0f, 2f), new Vector3(0, 0, 0)));
                    // Back Wall (Z = -2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_WoodGrid.fbx", new Vector3(-1f, 0f, -2f), new Vector3(0, 180, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(1f, 0f, -2f), new Vector3(0, 180, 0)));
                    // Left Wall (X = -2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_WoodGrid.fbx", new Vector3(-2f, 0f, 1f), new Vector3(0, -90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(-2f, 0f, -1f), new Vector3(0, -90, 0)));
                    // Right Wall (X = +2.0)
                    list.Add(new PiecePlacement("Wall_Plaster_Straight.fbx", new Vector3(2f, 0f, 1f), new Vector3(0, 90, 0)));
                    list.Add(new PiecePlacement("Wall_Plaster_WoodGrid.fbx", new Vector3(2f, 0f, -1f), new Vector3(0, 90, 0)));
                    // Roof & Chimney
                    list.Add(new PiecePlacement("Roof_RoundTiles_4x4.fbx", new Vector3(0f, 3.12f, 0f), new Vector3(0, 0, 0)));
                    list.Add(new PiecePlacement("Prop_Chimney2.fbx", new Vector3(-1.2f, 5.0f, -0.8f), new Vector3(0, 0, 0)));
                    break;
            }

            return list;
        }

        private static Mesh AssembleHouseMesh(List<PiecePlacement> placements, float vertexScale, string meshName)
        {
            // Maps each Quaternius material slot to its combined submesh list
            // 0: WoodTrim, 1: Plaster, 2: RoundTiles, 3: Brick, 4: RockTrim, 5: UnevenBrick
            List<CombineInstance>[] submeshCombines = new List<CombineInstance>[6];
            for (int i = 0; i < 6; i++) submeshCombines[i] = new List<CombineInstance>();

            GameObject rootTemp = new GameObject("Temp_AssemblyRoot");

            foreach (var p in placements)
            {
                string path = FbxRoot + p.fbxName;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[MAGIC CITY HOUSES] Modular FBX piece missing: {path}");
                    continue;
                }

                GameObject inst = UnityEngine.Object.Instantiate(prefab, rootTemp.transform);
                inst.transform.localPosition = p.localPos;
                inst.transform.localRotation = Quaternion.Euler(p.localEuler);
                inst.transform.localScale = Vector3.one;

                MeshFilter[] mfs = inst.GetComponentsInChildren<MeshFilter>();
                Renderer[] rends = inst.GetComponentsInChildren<Renderer>();

                for (int rIdx = 0; rIdx < rends.Length; rIdx++)
                {
                    Renderer rend = rends[rIdx];
                    MeshFilter mf = rend.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;

                    Mesh mesh = mf.sharedMesh;
                    Material[] mats = rend.sharedMaterials;
                    Matrix4x4 pieceLocalToRoot = rootTemp.transform.worldToLocalMatrix * rend.transform.localToWorldMatrix;

                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        string matName = (sub < mats.Length && mats[sub] != null) ? mats[sub].name.ToLowerInvariant() : "";

                        int targetSlot = 0; // default to WoodTrim
                        if (matName.Contains("plaster")) targetSlot = 1;
                        else if (matName.Contains("roundtiles") || matName.Contains("roof")) targetSlot = 2;
                        else if (matName.Contains("unevenbrick")) targetSlot = 5;
                        else if (matName.Contains("brick")) targetSlot = 3;
                        else if (matName.Contains("rocktrim") || matName.Contains("stone")) targetSlot = 4;
                        else if (matName.Contains("wood")) targetSlot = 0;

                        CombineInstance ci = new CombineInstance
                        {
                            mesh = mesh,
                            subMeshIndex = sub,
                            transform = pieceLocalToRoot
                        };
                        submeshCombines[targetSlot].Add(ci);
                    }
                }
            }

            UnityEngine.Object.DestroyImmediate(rootTemp);

            // Combine each submesh
            List<Mesh> combinedSubmeshes = new List<Mesh>();
            List<int> validSlotIndices = new List<int>();

            for (int s = 0; s < 6; s++)
            {
                if (submeshCombines[s].Count > 0)
                {
                    Mesh subM = new Mesh();
                    subM.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    subM.CombineMeshes(submeshCombines[s].ToArray(), true, true);
                    combinedSubmeshes.Add(subM);
                    validSlotIndices.Add(s);
                }
            }

            if (combinedSubmeshes.Count == 0) return null;

            // Combine all submeshes into a final multi-submesh mesh
            CombineInstance[] finalCombines = new CombineInstance[combinedSubmeshes.Count];
            for (int i = 0; i < combinedSubmeshes.Count; i++)
            {
                finalCombines[i] = new CombineInstance
                {
                    mesh = combinedSubmeshes[i],
                    subMeshIndex = 0,
                    transform = Matrix4x4.identity
                };
            }

            Mesh finalMesh = new Mesh();
            finalMesh.name = meshName;
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(finalCombines, false, false); // false = keep submeshes separate!

            // Scale vertices if vertexScale != 1.0f
            if (Mathf.Abs(vertexScale - 1.0f) > 0.0001f)
            {
                Vector3[] verts = finalMesh.vertices;
                for (int v = 0; v < verts.Length; v++)
                {
                    verts[v] *= vertexScale;
                }
                finalMesh.vertices = verts;
            }

            finalMesh.RecalculateBounds();
            finalMesh.RecalculateNormals();
            finalMesh.RecalculateTangents();

            return finalMesh;
        }

        private static int UpgradeRowHouses(Mesh[] rowMeshes)
        {
            var rowHousesRoot = GameObject.Find("8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses");
            if (rowHousesRoot == null) rowHousesRoot = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (rowHousesRoot == null)
            {
                Debug.LogWarning("[MAGIC CITY HOUSES] 01_Tier_Stepped_RowHouses root not found!");
                return 0;
            }

            Material[] rowMaterials = new Material[]
            {
                _woodTrimMat,
                _plasterMat,
                _roundTilesMat,
                _brickMat,
                _rockTrimMat,
                _unevenBrickMat
            };

            int updated = 0;
            int total = rowHousesRoot.transform.childCount;

            for (int i = 0; i < total; i++)
            {
                Transform houseT = rowHousesRoot.transform.GetChild(i);
                if (houseT == null) continue;

                // Pick deterministic style based on index
                int styleIdx = i % rowMeshes.Length;
                Mesh chosenMesh = rowMeshes[styleIdx];
                if (chosenMesh == null) continue;

                Undo.RecordObject(houseT.gameObject, "Upgrade Row House Mesh");

                // Update MeshFilter
                if (!houseT.TryGetComponent<MeshFilter>(out var mf))
                {
                    mf = houseT.gameObject.AddComponent<MeshFilter>();
                }
                mf.sharedMesh = chosenMesh;

                // Update MeshRenderer
                if (!houseT.TryGetComponent<MeshRenderer>(out var mr))
                {
                    mr = houseT.gameObject.AddComponent<MeshRenderer>();
                }
                mr.enabled = true;

                // Assign materials matching submesh count
                Material[] mats = new Material[chosenMesh.subMeshCount];
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = rowMaterials[m % rowMaterials.Length];
                }
                mr.sharedMaterials = mats;

                // Ensure solid collider
                var collider = houseT.GetComponent<Collider>();
                if (collider is BoxCollider box)
                {
                    box.center = chosenMesh.bounds.center;
                    box.size = chosenMesh.bounds.size;
                }
                else if (collider is MeshCollider mc)
                {
                    mc.sharedMesh = chosenMesh;
                }

                updated++;
            }

            Debug.Log($"<color=cyan><b>[MAGIC CITY HOUSES] Successfully updated {updated}/{total} Row Houses to Quaternius models!</b></color>");
            return updated;
        }

        private static int UpgradeLandmarkHouses(Mesh[] landmarkMeshes)
        {
            var landmarksRoot = GameObject.Find("--- METROPOLIS_MESHY_HOUSES ---");
            if (landmarksRoot == null)
            {
                Debug.LogWarning("[MAGIC CITY HOUSES] --- METROPOLIS_MESHY_HOUSES --- root not found, searching direct landmarks...");
            }

            Material[] landmarkMaterials = new Material[]
            {
                _woodTrimMat,
                _plasterMat,
                _roundTilesMat,
                _brickMat,
                _rockTrimMat,
                _unevenBrickMat
            };

            int updated = 0;

            // Search both under landmarksRoot and entire scene for any Meshy_House objects
            GameObject[] allSceneObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allSceneObjects)
            {
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();
                if (!n.Contains("meshy_house") && !n.Contains("meshyhouses")) continue;

                var mf = go.GetComponentInChildren<MeshFilter>(true);
                var mr = go.GetComponentInChildren<MeshRenderer>(true);
                if (mf == null || mr == null) continue;

                // Choose a style based on name
                int styleIdx = 0; // TownHome
                if (n.Contains("apothecary") || n.Contains("alchemist")) styleIdx = 1; // SmallCottage
                else if (n.Contains("forge") || n.Contains("blacksmith")) styleIdx = 3; // ArtisanForge
                else if (n.Contains("guild") || n.Contains("scholar") || n.Contains("spire")) styleIdx = 4; // TwoStoryManor
                else if (n.Contains("merchant") || n.Contains("harbor")) styleIdx = 2; // LongLodge

                Mesh chosenMesh = landmarkMeshes[styleIdx % landmarkMeshes.Length];
                if (chosenMesh == null) continue;

                Undo.RecordObject(mf.gameObject, "Upgrade Landmark House");
                mf.sharedMesh = chosenMesh;
                mr.enabled = true;

                Material[] mats = new Material[chosenMesh.subMeshCount];
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = landmarkMaterials[m % landmarkMaterials.Length];
                }
                mr.sharedMaterials = mats;

                // If landmark had small localScale (e.g. 1.0f or 6.0f), ensure it sits proportionally
                if (go.transform.localScale.x < 1.5f)
                {
                    go.transform.localScale = Vector3.one * 1.8f;
                }

                updated++;
                Debug.Log($"[MAGIC CITY HOUSES] Upgraded Landmark '{go.name}' with {chosenMesh.name}");
            }

            return updated;
        }

        private static int UpgradeTownAndCrafting(Mesh[] landmarkMeshes)
        {
            var craftingRoot = GameObject.Find("Step3_TownAndCrafting");
            if (craftingRoot == null) return 0;

            int updated = 0;
            Material[] mats = new Material[]
            {
                _woodTrimMat,
                _plasterMat,
                _roundTilesMat,
                _brickMat,
                _rockTrimMat,
                _unevenBrickMat
            };

            foreach (Transform t in craftingRoot.transform)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("house") || n.Contains("shop") || n.Contains("forge") || n.Contains("building"))
                {
                    var mf = t.GetComponentInChildren<MeshFilter>(true);
                    var mr = t.GetComponentInChildren<MeshRenderer>(true);
                    if (mf == null || mr == null) continue;

                    int style = n.Contains("forge") ? 3 : (n.Contains("shop") ? 5 : 0);
                    Mesh m = landmarkMeshes[style % landmarkMeshes.Length];
                    if (m != null)
                    {
                        Undo.RecordObject(mf.gameObject, "Upgrade Crafting Building");
                        mf.sharedMesh = m;
                        mr.enabled = true;
                        Material[] assignedMats = new Material[m.subMeshCount];
                        for (int i = 0; i < assignedMats.Length; i++) assignedMats[i] = mats[i % mats.Length];
                        mr.sharedMaterials = assignedMats;
                        updated++;
                    }
                }
            }

            return updated;
        }
    }
}
#endif
