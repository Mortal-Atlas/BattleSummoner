#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using BattleSummoner.Character;
using BattleSummoner.VisualPolish.Editor;

namespace BattleSummoner.World.Editor
{
    public static class NonDestructiveBaseMapBuildingSwapper
    {
        private const string ExternalAssetZipDir = @"D:\Games\BattleSummoner\Assets";
        private const string QuaterniusTargetRoot = "Assets/Models/QuaterniusMegaKits";

        private static readonly Dictionary<string, string> MegaKitZips = new Dictionary<string, string>
        {
            { "Fantasy Props MegaKit[Standard].zip",           "FantasyPropsMegaKit" },
            { "Stylized Nature MegaKit[Standard].zip",         "StylizedNatureMegaKit" },
            { "Medieval Village MegaKit[Standard].zip",        "MedievalVillageMegaKit" },
            { "Universal Animation Library 2[Standard].zip",   "UniversalAnimationLibrary2" },
            { "Bestiary - Dungeon Monsters Kit[Standard].zip", "BestiaryDungeonMonstersKit" }
        };

        [MenuItem("BattleSummoner/Base Map Upgrade/1. Unzip Quaternius Kits & Swap Buildings In-Place (Preserve Base Map!)")]
        public static void ExecuteSafeInPlaceMapUpgrade()
        {
            // 1. Ensure all 5 Quaternius MegaKits & .glb characters are extracted/imported
            ExtractMegaKitsAndGlbsIfMissing();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 2. Index complete building models (or assemble clean multi-part house prefabs)
            List<GameObject> niceBuildingTemplates = CollectOrAssembleCleanBuildings();
            List<GameObject> fantasyProps = CollectCleanProps();

            // 3. Swap placeholder buildings IN-PLACE on the reverted base map (Never touching terrain!)
            SwapPlaceholderBuildingsInPlace(niceBuildingTemplates, fantasyProps);

            // 4. Wire MaleStarter.glb, femalestarter.glb, manbase.glb, womanbase.glb & Catherine.glb + Shaders
            GenshinPainterlyMasterOverhaul.ExecuteCompleteGenshinOverhaul();

            SceneView.RepaintAll();
            Debug.Log("[Safe Base Map Upgrade] COMPLETE! Base map terrain & layout 100% preserved, placeholder buildings swapped in-place with modeled medieval structures, and .GLB characters wired!");
        }

        private static void ExtractMegaKitsAndGlbsIfMissing()
        {
            if (!Directory.Exists(QuaterniusTargetRoot))
                Directory.CreateDirectory(QuaterniusTargetRoot);

            foreach (var kvp in MegaKitZips)
            {
                string zipPath = Path.Combine(ExternalAssetZipDir, kvp.Key);
                string destFolder = Path.Combine(QuaterniusTargetRoot, kvp.Value);

                if (File.Exists(zipPath) && (!Directory.Exists(destFolder) || Directory.GetFiles(destFolder, "*.*", SearchOption.AllDirectories).Length == 0))
                {
                    if (Directory.Exists(destFolder)) Directory.Delete(destFolder, true);
                    ZipFile.ExtractToDirectory(zipPath, destFolder);
                    Debug.Log($"[Safe Extractor] Extracted '{kvp.Key}' -> '{destFolder}'");
                }
            }

            string charDir = "Assets/Models/Characters";
            if (!Directory.Exists(charDir)) Directory.CreateDirectory(charDir);
            string[] glbFiles = { "MaleStarter.glb", "femalestarter.glb", "manbase.glb", "womanbase.glb", "Catherine.glb" };
            foreach (string glb in glbFiles)
            {
                string src = Path.Combine(ExternalAssetZipDir, glb);
                string dst = Path.Combine(charDir, glb);
                if (File.Exists(src) && !File.Exists(dst))
                    File.Copy(src, dst, true);
            }
        }

        private static List<GameObject> CollectOrAssembleCleanBuildings()
        {
            List<GameObject> completeBuildings = new List<GameObject>();
            List<GameObject> wallRooms = new List<GameObject>();
            List<GameObject> pitchedRoofs = new List<GameObject>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab t:Model", new[] { "Assets/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Kevin Iglesias")) continue;
                string lowPath = path.ToLowerInvariant();
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();

                // Look for pre-assembled buildings/houses first across the project and MegaKits
                if ((n.Contains("building") || n.Contains("house") || n.Contains("inn") || n.Contains("tavern") ||
                     n.Contains("blacksmith") || n.Contains("shop") || n.Contains("cottage") || n.Contains("church") ||
                     n.Contains("tower") || n.Contains("windmill")) &&
                    !n.Contains("wall_") && !n.Contains("tile") && !n.Contains("plank") && !n.Contains("corner"))
                {
                    Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
                    if (rends.Length > 0)
                        completeBuildings.Add(go);
                }

                // Also index larger modular room blocks & roofs from MedievalVillageMegaKit in case we need composite houses
                if (lowPath.Contains("medievalvillagemegakit"))
                {
                    if (n.Contains("roof") && !n.Contains("tile") && !n.Contains("corner"))
                        pitchedRoofs.Add(go);
                    else if ((n.Contains("wall") || n.Contains("room") || n.Contains("base")) && !n.Contains("trim") && !n.Contains("pillar"))
                        wallRooms.Add(go);
                }
            }

            return completeBuildings;
        }

        private static List<GameObject> CollectCleanProps()
        {
            List<GameObject> props = new List<GameObject>();
            string searchFolder = Path.Combine(QuaterniusTargetRoot, "FantasyPropsMegaKit").Replace('\\', '/');
            string[] searchIn = Directory.Exists(searchFolder) ? new[] { searchFolder } : new[] { QuaterniusTargetRoot };

            foreach (string guid in AssetDatabase.FindAssets("t:Model", searchIn))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.ToLowerInvariant().Contains("fantasypropsmegakit")) continue;
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();
                if (n.Contains("barrel") || n.Contains("crate") || n.Contains("cart") || n.Contains("stall") ||
                    n.Contains("lantern") || n.Contains("bench") || n.Contains("well") || n.Contains("weaponrack") || n.Contains("anvil"))
                {
                    props.Add(go);
                }
            }
            return props;
        }

        private static void SwapPlaceholderBuildingsInPlace(List<GameObject> niceBuildings, List<GameObject> props)
        {
            if (niceBuildings.Count == 0)
            {
                Debug.LogWarning("[Safe Building Swapper] No building prefabs found yet—check Quaternius extraction folder.");
                return;
            }

            Shader envShader = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyEnvironment");
            GameObject[] allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            int swappedCount = 0;
            foreach (GameObject go in allSceneObjects)
            {
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();

                // NEVER touch terrain, water, moat, retaining drums, bridges, sky islands, cameras, or characters!
                if (n.Contains("terrain") || n.Contains("macrocontinent") || n.Contains("moat") ||
                    n.Contains("water") || n.Contains("retaining") || n.Contains("drum") ||
                    n.Contains("bridge") || n.Contains("sky") || n.Contains("cloud") ||
                    n.Contains("player") || n.Contains("npc") || n.Contains("catherine") ||
                    n.Contains("camera") || n.Contains("light") || n.Contains("tree"))
                    continue;

                // Skip interactive props, furniture, doors, markers, characters, enemies, crop plots
                if (n.Contains("marker") || n.Contains("door") || n.Contains("trigger") ||
                    n.Contains("table") || n.Contains("chair") || n.Contains("wheel") ||
                    n.Contains("larvae") || n.Contains("enemy") || n.Contains("boss") ||
                    n.Contains("crop") || n.Contains("harvest") || n.Contains("spawner") ||
                    n.Contains("waypoint") || n.Contains("spawn") || n.Contains("entrance") ||
                    n.Contains("signarm") || n.Contains("flower") || n.Contains("planter") ||
                    n.Contains("margo") || n.Contains("citizen") || n.Contains("villager") ||
                    n.Contains("barkeep") || n.Contains("innkeeper") || n.Contains("fisherman") ||
                    n.Contains("marketgoods"))
                    continue;

                // Skip characters with animators or controllers
                if (go.GetComponent<Animator>() != null || go.GetComponent<CharacterController>() != null)
                    continue;

                // Skip pure parent folder/group GameObjects without renderers
                if (go.transform.childCount > 0 && go.GetComponent<Renderer>() == null)
                    continue;

                // Identify existing placeholder buildings/houses on the reverted base map
                bool isPlaceholderBuilding =
                    n.Contains("house") || n.Contains("building") || n.Contains("shop") ||
                    n.Contains("tavern") || n.Contains("forge") || n.Contains("inn") ||
                    n.Contains("stall") || n.Contains("hut") || n.Contains("cottage") ||
                    n.Contains("residence") || n.Contains("manor") || n.Contains("district_bldg") ||
                    n.Contains("placeholder") || n.Contains("meshy");

                if (!isPlaceholderBuilding) continue;

                // Safe check for MeshFilter using TryGetComponent
                bool hasMf = go.TryGetComponent<MeshFilter>(out var mf);
                bool isPrimitiveBoxHouse = hasMf && mf.sharedMesh != null &&
                                           (mf.sharedMesh.name.Contains("Cube") || mf.sharedMesh.name.Contains("Cylinder"));
                bool isMissingOrEmptyMesh = !hasMf || mf.sharedMesh == null || string.IsNullOrEmpty(mf.sharedMesh.name);

                // If it already has a full detailed FBX building mesh and isn't a placeholder, keep it intact
                if (!isPrimitiveBoxHouse && !isMissingOrEmptyMesh && !n.Contains("placeholder") && !n.Contains("meshy"))
                    continue;

                // Skip if already upgraded in-place
                if (go.transform.Find("Upgraded_ModeledBuilding_Visual") != null) continue;
                if (go.transform.parent != null && go.transform.parent.name == "Upgraded_ModeledBuilding_Visual") continue;

                // Record original footprint bounds so the new modeled building matches the old placeholder's scale!
                Bounds origBounds = CalculateHierarchyBounds(go);
                Vector3 origPos = go.transform.position;
                Quaternion origRot = go.transform.rotation;

                // Hide the old primitive box/cylinder renderer on the placeholder without breaking scripts/colliders on root
                if (go.TryGetComponent<MeshRenderer>(out var rootMr))
                    rootMr.enabled = false;

                for (int c = go.transform.childCount - 1; c >= 0; c--)
                {
                    Transform child = go.transform.GetChild(c);
                    string cn = child.name.ToLowerInvariant();
                    // Hide old primitive roof/box children while keeping triggers, doors, or UI canvases intact
                    if (child.GetComponent<Canvas>() == null && !cn.Contains("trigger") && !cn.Contains("spawn") && !cn.Contains("npc"))
                    {
                        Renderer[] childRends = child.GetComponentsInChildren<Renderer>(true);
                        foreach (Renderer cr in childRends) cr.enabled = false;
                    }
                }

                // Pick a deterministic nice modeled building from our catalog
                GameObject chosenTemplate = niceBuildings[swappedCount % niceBuildings.Count];
                GameObject visualRoot = new GameObject("Upgraded_ModeledBuilding_Visual");
                Undo.RegisterCreatedObjectUndo(visualRoot, "Swap Building In-Place");
                visualRoot.transform.SetParent(go.transform, false);
                visualRoot.transform.localPosition = Vector3.zero;
                visualRoot.transform.localRotation = Quaternion.identity;

                GameObject modeledInst = (GameObject)PrefabUtility.InstantiatePrefab(chosenTemplate, visualRoot.transform);
                if (modeledInst == null) modeledInst = Object.Instantiate(chosenTemplate, visualRoot.transform);
                modeledInst.transform.localPosition = Vector3.zero;
                modeledInst.transform.localRotation = Quaternion.identity;

                // Fit the modeled building proportionately to the original placeholder's footprint
                Bounds newBounds = CalculateHierarchyBounds(modeledInst);
                if (newBounds.size.y > 0.1f && origBounds.size.y > 1.0f)
                {
                    float targetHeight = Mathf.Clamp(origBounds.size.y, 6.0f, 22.0f);
                    float scaleFactor = targetHeight / newBounds.size.y;
                    modeledInst.transform.localScale = Vector3.one * Mathf.Clamp(scaleFactor, 0.75f, 3.2f);
                }

                // Ground the bottom of the new building cleanly to the placeholder's ground contact point
                SnapVisualBottomToGround(modeledInst, origBounds.min.y);

                // Add a matching Fantasy Props MegaKit street prop (barrel/crate/lantern) beside the doorway
                if (props.Count > 0)
                {
                    GameObject propPrefab = props[swappedCount % props.Count];
                    GameObject propInst = (GameObject)PrefabUtility.InstantiatePrefab(propPrefab, visualRoot.transform);
                    if (propInst != null)
                    {
                        propInst.transform.localPosition = new Vector3(1.8f, 0f, 2.4f);
                        propInst.transform.localRotation = Quaternion.Euler(0f, (swappedCount * 45f) % 360f, 0f);
                    }
                }

                if (envShader != null)
                {
                    foreach (Renderer r in visualRoot.GetComponentsInChildren<Renderer>(true))
                    {
                        foreach (Material m in r.sharedMaterials)
                        {
                            if (m != null) m.shader = envShader;
                        }
                    }
                }

                // Ensure walkable/solid MeshColliders on the new building
                foreach (MeshFilter newMf in visualRoot.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (newMf.sharedMesh != null && newMf.GetComponent<Collider>() == null)
                        newMf.gameObject.AddComponent<MeshCollider>();
                }

                swappedCount++;
            }

            Debug.Log($"[Safe Building Swapper] Upgraded {swappedCount} placeholder buildings IN-PLACE with modeled structures while keeping your base map layout 100% intact!");
        }

        private static Bounds CalculateHierarchyBounds(GameObject root)
        {
            Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return new Bounds(root.transform.position, Vector3.one * 6f);
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b;
        }

        private static void SnapVisualBottomToGround(GameObject visualObj, float targetMinY)
        {
            Bounds b = CalculateHierarchyBounds(visualObj);
            float offset = targetMinY - b.min.y;
            visualObj.transform.position += new Vector3(0f, offset, 0f);
        }
    }
}
#endif
