#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BattleSummoner.WorldGen;

namespace BattleSummoner.World.Editor
{
    [InitializeOnLoad]
    public static class MasterSceneRemediationRunner
    {
        private const string LockFile = "Temp/MasterRemediation_Ran.lock";

        static MasterSceneRemediationRunner()
        {
            EditorApplication.delayCall += CheckAndRun;
        }

        [MenuItem("BattleSummoner/Execute Master Scene Overhaul Now")]
        public static void ForceRun()
        {
            if (File.Exists(LockFile)) File.Delete(LockFile);
            RunAllRemediations();
        }

        private static void CheckAndRun()
        {
            if (File.Exists(LockFile)) return;
            RunAllRemediations();
        }

        private static void RunAllRemediations()
        {
            Debug.Log("[MasterSceneRemediation] Starting complete scene overhaul for image_817dfe.jpg / image_81825c.jpg...");

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/OverworldMap.unity")
            {
                activeScene = EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity", OpenSceneMode.Single);
            }
            string log = $"=== MASTER SCENE REMEDIATION EXECUTION REPORT ===\nTimestamp: {DateTime.Now}\nScene: {activeScene.path}\n\n";

            try
            {
                // 1. Terrain Mesh Sync from FBX
                string fbxPath = "Assets/Models/Environments/CircularContinent_MacroLevel1.fbx";
                string meshAssetPath = "Assets/Models/Environments/Floor01_MacroContinent_Terrain_Subdivided.asset";
                
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                UnityEngine.Object[] fbxAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                Mesh terrainMesh = null;
                foreach (var obj in fbxAssets)
                {
                    if (obj is Mesh m && m.name.Contains("Floor01_MacroContinent_Terrain"))
                    {
                        terrainMesh = m;
                        break;
                    }
                }
                if (terrainMesh == null)
                {
                    foreach (var obj in fbxAssets)
                    {
                        if (obj is Mesh m) { terrainMesh = m; break; }
                    }
                }

                GameObject continentGo = GameObject.Find("CircularContinent_MacroLevel1");
                if (continentGo != null && terrainMesh != null)
                {
                    // Create an independent copy to store as asset
                    Mesh newMesh = UnityEngine.Object.Instantiate(terrainMesh);
                    newMesh.name = "Floor01_MacroContinent_Terrain_Subdivided";
                    
                    AssetDatabase.CreateAsset(newMesh, meshAssetPath);
                    AssetDatabase.SaveAssets();

                    Transform tTr = continentGo.transform.Find("Floor01_MacroContinent_Terrain");
                    if (tTr == null) tTr = continentGo.transform;

                    var mf = tTr.GetComponent<MeshFilter>();
                    if (mf == null) mf = tTr.gameObject.AddComponent<MeshFilter>();
                    mf.sharedMesh = newMesh;

                    var mc = tTr.GetComponent<MeshCollider>();
                    if (mc == null) mc = tTr.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = null;
                    mc.sharedMesh = newMesh;

                    log += $"1. Terrain Mesh Updated: {newMesh.name} ({newMesh.vertexCount} verts, {newMesh.triangles.Length / 3} tris)\n";
                }
                else
                {
                    log += $"1. Terrain Mesh: continentGo={(continentGo != null)}, terrainMesh={(terrainMesh != null)}\n";
                }

                // 2. Fix Buried Houses, Vertical Tier Drums, River Rocks, Paths & Animator
                FixBuriedHousesAndMountainTraversal.PurgeFloatingRiverRocksAndAdware();
                FixBuriedHousesAndMountainTraversal.BuildCrispVerticalTierDrums();
                FixBuriedHousesAndMountainTraversal.UnburyAndPlinthAllKingdomBuildings();
                FixBuriedHousesAndMountainTraversal.PopulateMountainTraversalObstaclesAndPaths();
                FixBuriedHousesAndMountainTraversal.FixPlayerHeroAnimatorBrawlerStrikeParameter();
                log += "2. Buried houses unclipped & plinthed, crisp masonry drums built, floating rocks purged, mountain paths and animator updated.\n";

                // 3. Scene Defect Remediation: Moat Water, 16 Perimeter Towers, Sky Islands, Outer Foliage
                SceneDefectRemediationBuilder.BuildMoatAndTierRetainingWalls();
                SceneDefectRemediationBuilder.BuildPerimeterWallAnd16Towers();
                SceneDefectRemediationBuilder.SpawnSkyIslandsLayer();
                SceneDefectRemediationBuilder.PopulateAnimeNatureFoliage();
                log += "3. Moat water ring (R=535m, Y=18m), 16 Perimeter Towers & curtain walls, 12 Sky Islands, and outer biome foliage built.\n";

                // 4. Mountain Switchback & River Gorge Anime Nature Scaffolder
                MountainSwitchbackAnimeNatureScaffolder.PopulateMountainTrailAndRiverGorge();
                log += "4. Mountain switchback trail & meandering river gorge populated with Anime Nature prefabs.\n";

                // 5. Mark Scene Dirty & Save
                EditorSceneManager.MarkSceneDirty(activeScene);
                bool saved = EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
                log += $"5. Scene Saved: {saved} (Path: {activeScene.path})\n";

                // Write lock file so it doesn't repeat on recompile
                string lockDir = Path.GetDirectoryName(LockFile);
                if (!string.IsNullOrEmpty(lockDir) && !Directory.Exists(lockDir)) Directory.CreateDirectory(lockDir);
                File.WriteAllText(LockFile, log);

                // Also save log to Assets/Screenshots
                string reportPath = "Assets/Screenshots/MasterRemediationReport.txt";
                File.WriteAllText(reportPath, log);

                Debug.Log($"[MasterSceneRemediation] SUCCESS!\n{log}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MasterSceneRemediation] Exception during execution: {ex}");
                log += $"\nERROR: {ex}\n";
                File.WriteAllText("Assets/Screenshots/MasterRemediationReport.txt", log);
            }
        }
    }
}
#endif
