using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.WorldActivities;
using BattleSummoner.DesignPhilosophy;

namespace BattleSummoner.EditorScripts
{
    public static class WorldAndSkyActivitiesSceneIntegrator
    {
        private const string SCENE_PATH = "Assets/Scenes/OverworldMap.unity";
        private const string FBX_PATH = "Assets/Models/Environments/World_And_Sky_Activities_POIs.fbx";
        private const string ROOT_NAME = "--- WORLD & SKY ACTIVITIES & POIs ---";
        private const string MAT_FOLDER = "Assets/Materials/POI";

        [MenuItem("BattleSummoner/World Activities/Integrate World and Sky POIs Into Scene", false, 20)]
        public static string IntegratePOIs()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SCENE_PATH)
            {
                scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
            if (fbxAsset == null)
            {
                string err = $"[POI Integrator] Error: FBX asset missing at {FBX_PATH}";
                Debug.LogError(err);
                return err;
            }

            var continent = GameObject.Find("CircularContinent_MacroLevel1");
            var terrainCol = continent != null ? continent.GetComponentInChildren<MeshCollider>() : null;
            if (terrainCol == null)
            {
                Debug.LogError("[POI Integrator] MeshCollider missing on CircularContinent_MacroLevel1!");
                return "Failed: MeshCollider missing";
            }

            // Create/load URP painterly materials
            var materials = SetupPOIMaterials();

            // Remove previous instance if present
            var existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            var rootGo = new GameObject(ROOT_NAME);
            Undo.RegisterCreatedObjectUndo(rootGo, "Integrate World & Sky POIs");

            // Instantiate FBX model as child of root
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, rootGo.transform);
            instance.name = "World_And_Sky_Activities_POIs_Model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Coordinate axis compensation
            instance.transform.localScale = Vector3.one;

            // Unpack completely so child transforms can be modified freely
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);

            // Wire all interactive components & snap to terrain elevation
            int pillarsWired = 0;
            int chestsWired = 0;
            int golemsWired = 0;
            int geodesWired = 0;
            int ringsWired = 0;
            int windmillsWired = 0;
            int cropsWired = 0;
            int snappedObjects = 0;

            var allTransforms = instance.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                string n = t.name;

                // 1. Snap surface root objects and dirt roads to terrain height
                if (n.EndsWith("_Root") || n.StartsWith("DirtPathway_"))
                {
                    // Skip sky islands (sky islands start at Z/Y >= 350m in Blender)
                    if (!n.StartsWith("SkyIsland_") && !n.StartsWith("SkyElevatorHeist_"))
                    {
                        Vector3 worldPos = t.position;

                        // Coastal / ocean water surface handling
                        if (n.StartsWith("Village_01_Seaward_Dockcity"))
                        {
                            t.position = new Vector3(worldPos.x, 19f, worldPos.z);
                            snappedObjects++;
                        }
                        else if (n.StartsWith("Temple_05_TideCaller_Island"))
                        {
                            t.position = new Vector3(worldPos.x, 26f, worldPos.z);
                            snappedObjects++;
                        }
                        else
                        {
                            Ray ray = new Ray(new Vector3(worldPos.x, 1600f, worldPos.z), Vector3.down);
                            if (terrainCol.Raycast(ray, out RaycastHit hit, 2200f))
                            {
                                float offset = n.StartsWith("DirtPathway_") ? 0.25f : 0f;
                                t.position = new Vector3(worldPos.x, hit.point.y + offset, worldPos.z);
                                snappedObjects++;
                            }
                        }
                    }
                }

                // 2. Destructible Arena Pillars
                if (n.Contains("DestructibleArenaPillar"))
                {
                    if (t.GetComponent<Collider>() == null)
                    {
                        var col = t.gameObject.AddComponent<CapsuleCollider>();
                        col.height = 14f;
                        col.radius = 1.8f;
                    }
                    if (t.GetComponent<ArenaDestructiblePillar>() == null)
                    {
                        t.gameObject.AddComponent<ArenaDestructiblePillar>();
                        pillarsWired++;
                    }
                }
                // 3. Giant Master Chests
                else if (n.Contains("GiantMasterChest"))
                {
                    if (t.GetComponent<Collider>() == null)
                    {
                        var col = t.gameObject.AddComponent<BoxCollider>();
                        col.size = new Vector3(4.5f, 3f, 3f);
                    }
                    if (t.GetComponent<ZeroWaitInstantChest>() == null)
                    {
                        var chest = t.gameObject.AddComponent<ZeroWaitInstantChest>();
                        chest.isCentralGiantMasterChest = true;
                        chest.rewardItemTitle = "Aether-Master Kinetic Relic & 500 Mana Cores";
                        chestsWired++;
                    }
                }
                // 4. Stealable Golem Ride-Armors
                else if (n.Contains("StealableGolemRideArmor"))
                {
                    if (t.GetComponent<Collider>() == null)
                    {
                        var col = t.gameObject.AddComponent<BoxCollider>();
                        col.size = new Vector3(4f, 6f, 3.5f);
                    }
                    if (t.GetComponent<StealableGolemRideArmor>() == null)
                    {
                        t.gameObject.AddComponent<StealableGolemRideArmor>();
                        golemsWired++;
                    }
                }
                // 5. Upside-Down Geode Mining Nodes
                else if (n.Contains("UpsideDownGeodeNode"))
                {
                    if (t.GetComponent<Collider>() == null)
                    {
                        var col = t.gameObject.AddComponent<SphereCollider>();
                        col.radius = 2.6f;
                    }
                    if (t.GetComponent<CaveMiningNode>() == null)
                    {
                        t.gameObject.AddComponent<CaveMiningNode>();
                        geodesWired++;
                    }
                }
                // 6. Inversion Rune Rings
                else if (n.Contains("GravityFlipRuneRing"))
                {
                    var col = t.GetComponent<SphereCollider>();
                    if (col == null) col = t.gameObject.AddComponent<SphereCollider>();
                    col.radius = 5.0f;
                    col.isTrigger = true;

                    if (t.GetComponent<SkyIslandGravityRingTrigger>() == null)
                    {
                        t.gameObject.AddComponent<SkyIslandGravityRingTrigger>();
                        ringsWired++;
                    }
                }
                // 7. Windmills & Rotating Sails
                else if (n.Contains("WindmillSails_Rotor"))
                {
                    if (t.GetComponent<WindmillRotorSpinner>() == null)
                    {
                        t.gameObject.AddComponent<WindmillRotorSpinner>();
                        windmillsWired++;
                    }
                }
                // 8. Interactive Crop Plots
                else if (n.Contains("HarvestableCropPlot"))
                {
                    if (t.GetComponent<Collider>() == null)
                    {
                        var col = t.gameObject.AddComponent<BoxCollider>();
                        col.size = new Vector3(12f, 2f, 9f);
                        col.isTrigger = true;
                    }
                    cropsWired++;
                }

                // 9. Assign Painterly URP Materials
                var mr = t.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    AssignProperMaterial(mr, n, materials);
                }
            }

            // Attach & configure WorldAndSkyActivityDirector
            var director = rootGo.GetComponent<WorldAndSkyActivityDirector>();
            if (director == null)
            {
                director = rootGo.AddComponent<WorldAndSkyActivityDirector>();
            }

            // Auto-clear trees around building footprints
            director.AutoRegisterAndClearTreesAroundAllPOIs();

            // Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            string report = $"[POI Integrator] Success! Integrated {ROOT_NAME} into {SCENE_PATH}.\n" +
                $"• Terrain Snapped Elements: {snappedObjects}\n" +
                $"• Arena Pillars: {pillarsWired}\n" +
                $"• Giant Master Chests: {chestsWired}\n" +
                $"• Stealable Golem Armors: {golemsWired}\n" +
                $"• Upside-Down Geodes: {geodesWired}\n" +
                $"• Gravity-Flip Rings: {ringsWired}\n" +
                $"• Windmill Rotors: {windmillsWired}\n" +
                $"• Crop Plots: {cropsWired}\n" +
                $"• Vegetation Footprints Cleared: {director.settlementTreeClearanceRadius}m radius";

            Debug.Log(report);
            return report;
        }

        private static Dictionary<string, Material> SetupPOIMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials/POI"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateFolder("Assets/Materials", "POI");
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var dict = new Dictionary<string, Material>();

            dict["IvoryStone"] = GetOrCreateMat("MAT_POI_IvoryStone", shader, new Color(0.91f, 0.89f, 0.82f), 0.28f, 0f);
            dict["HoneyCedar"] = GetOrCreateMat("MAT_POI_HoneyCedar", shader, new Color(0.65f, 0.43f, 0.30f), 0.22f, 0f);
            dict["RoofTerracotta"] = GetOrCreateMat("MAT_POI_RoofTerracotta", shader, new Color(0.76f, 0.38f, 0.24f), 0.32f, 0f);
            dict["PeriwinkleSlate"] = GetOrCreateMat("MAT_POI_PeriwinkleSlate", shader, new Color(0.38f, 0.46f, 0.62f), 0.40f, 0f);
            dict["AstralGold"] = GetOrCreateMat("MAT_POI_AstralGold", shader, new Color(0.89f, 0.75f, 0.47f), 0.75f, 0.92f);
            dict["AetherCyan"] = GetOrCreateMat("MAT_POI_AetherCyan", shader, new Color(0.22f, 0.74f, 0.98f), 0.85f, 0f, new Color(0.22f, 0.74f, 0.98f) * 3.8f);
            dict["GoldenWheat"] = GetOrCreateMat("MAT_POI_GoldenWheat", shader, new Color(0.88f, 0.74f, 0.26f), 0.15f, 0f);
            dict["DirtPathway"] = GetOrCreateMat("MAT_POI_DirtPathway", shader, new Color(0.58f, 0.46f, 0.34f), 0.08f, 0f);

            return dict;
        }

        private static Material GetOrCreateMat(string name, Shader shader, Color baseColor, float smoothness, float metallic, Color? emission = null)
        {
            string path = $"{MAT_FOLDER}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AssignProperMaterial(MeshRenderer mr, string name, Dictionary<string, Material> mats)
        {
            if (name.Contains("Plinth") || name.Contains("Pillar") || name.Contains("WindmillTower") || name.Contains("Watchtower"))
            {
                mr.sharedMaterial = mats["IvoryStone"];
            }
            else if (name.Contains("BarnHouse") || name.Contains("RowHouse") || name.Contains("WindmillSails"))
            {
                mr.sharedMaterial = mats["HoneyCedar"];
            }
            else if (name.Contains("Roof"))
            {
                mr.sharedMaterial = name.Contains("Slate") ? mats["PeriwinkleSlate"] : mats["RoofTerracotta"];
            }
            else if (name.Contains("Chest") || name.Contains("Plaza") || name.Contains("Geode") || name.Contains("Platform"))
            {
                mr.sharedMaterial = mats["AstralGold"];
            }
            else if (name.Contains("Spire") || name.Contains("RuneRing") || name.Contains("Golem"))
            {
                mr.sharedMaterial = mats["AetherCyan"];
            }
            else if (name.Contains("Crop"))
            {
                mr.sharedMaterial = mats["GoldenWheat"];
            }
            else if (name.Contains("Pathway"))
            {
                mr.sharedMaterial = mats["DirtPathway"];
            }
        }
    }
}
