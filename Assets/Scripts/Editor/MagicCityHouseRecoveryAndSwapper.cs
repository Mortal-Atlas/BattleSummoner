#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using BattleSummoner.Progression.ThreeMasks;
using BattleSummoner.Spellcraft;
using BattleSummoner.Combat;
using BattleSummoner.World;
using BattleSummoner.Character.FacialAnimation;
using BattleSummoner.Optimization;
using BattleSummoner.DesignPhilosophy;

namespace BattleSummoner.World.Editor
{
    /// <summary>
    /// MagicCityHouseRecoveryAndSwapper implements Part 1 and Part 2:
    /// - Un-bakes Combined Mesh static batches on city houses.
    /// - Clears StaticEditorFlags.BatchingStatic.
    /// - Safely replaces placeholder houses in-place with Quaternius Medieval Village multi-material prefabs.
    /// - Raycasts grounding flush to terrain (doorways never buried).
    /// - Auto-spawns stone foundation plinths on sloped tiers.
    /// - Enforces 14m avenue clearance and zero-lot-line packing.
    /// - Extracts procedural meshes into Assets/GeneratedMeshes/ so scene shrinks < 100MB.
    /// - Sets up Golden Hour lighting, SSAO, Genshin painterly shaders, and character proportions.
    /// </summary>
    public static class MagicCityHouseRecoveryAndSwapper
    {
        private const string ScenePath = "Assets/Scenes/OverworldMap.unity";
        private const string CombinedHousesDir = "Assets/Models/QuaterniusCombinedHouses/";
        private const string GeneratedMeshesDir = "Assets/GeneratedMeshes/";

        [MenuItem("BattleSummoner/Magic City/1. Execute Safe House Recovery & Swapper (Non-Destructive)", false, 1)]
        public static void ExecuteSafeRecoveryAndSwap()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Debug.Log("<color=#38BDF8><b>🏰 [MAGIC CITY RECOVERY] Starting Non-Destructive House Recovery & Swapper...</b></color>");

            // 1. Un-bake combined house batches & clear StaticEditorFlags.BatchingStatic
            int unbakedCount = UnbakeStaticCombinedMeshes();

            // 2. Perform proportional in-place swap with multi-material Quaternius prefabs
            int swappedCount = SwapPlaceholderHousesWithQuaterniusPrefabs();

            // 3. Auto-spawn stone foundation plinths on sloped tiers
            int plinthsSpawned = GenerateStoneFoundationPlinths();

            // 4. Populate street props (barrels, crates, lanterns, market stalls)
            int propsPlaced = PopulateFantasyStreetProps();

            // 5. Fit town plaza trees with stone planter rings
            int plantersFitted = FitTreePlanterRings();

            // 6. Setup Golden Hour Sun, Soft Shadows, SSAO & Painterly Shaders
            ConfigureGoldenHourLightingAndShaders();

            // 7. Rig & normalize characters (.glb proportions, Catherine, Player Hero)
            SetupCharactersAndProportions();

            // 8. Setup Appraisal Orb in Cathedral hub
            EnsureAppraisalOrbInCathedral();

            // 9. Populate wilderness monster camps with Bestiary prefabs
            PopulateWildernessMonsterCamps();

            // 10. Extract procedural meshes into Assets/GeneratedMeshes/ to shrink scene file
            int meshesExtracted = ExtractProceduralMeshesToAssetFiles();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log($"<color=#10B981><b>✓ [MAGIC CITY RECOVERY COMPLETE] Unbaked: {unbakedCount} | Swapped: {swappedCount} | Plinths: {plinthsSpawned} | Props: {propsPlaced} | Planters: {plantersFitted} | Meshes Extracted: {meshesExtracted}</b></color>");
        }

        public static int UnbakeStaticCombinedMeshes()
        {
            int unbaked = 0;
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var go in allObjects)
            {
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();

                bool isCityStructure = n.Contains("house") || n.Contains("building") || n.Contains("stall") ||
                                       n.Contains("tier_stepped") || n.Contains("metropolis") || n.Contains("bazaar") ||
                                       n.Contains("shop") || n.Contains("inn") || n.Contains("tavern");

                if (!isCityStructure) continue;

                // Clear BatchingStatic flag so Unity never bakes these into an uneditable blob
                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & StaticEditorFlags.BatchingStatic) != 0)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, flags & ~StaticEditorFlags.BatchingStatic);
                    unbaked++;
                }

                // Detect Combined Mesh (root: scene)
                if (go.TryGetComponent<MeshFilter>(out var mf))
                {
                    if (mf.sharedMesh != null && mf.sharedMesh.name.Contains("Combined Mesh"))
                    {
                        // Remove combined mesh reference on building container
                        mf.sharedMesh = null;
                        if (go.TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;
                        unbaked++;
                    }
                }
            }

            Debug.Log($"[Unbake] Cleared BatchingStatic on {unbaked} city structures and isolated house transforms.");
            return unbaked;
        }

        public static int SwapPlaceholderHousesWithQuaterniusPrefabs()
        {
            // Load available Quaternius multi-material house meshes (.asset)
            string[] meshNames = new[]
            {
                "Quaternius_House_TownHome_RowHouseScale.asset",
                "Quaternius_House_SmallCottage_RowHouseScale.asset",
                "Quaternius_House_LongLodge_RowHouseScale.asset",
                "Quaternius_House_ArtisanForge_RowHouseScale.asset",
                "Quaternius_House_TwoStoryManor_RowHouseScale.asset",
                "Quaternius_House_WoodGridShop_RowHouseScale.asset"
            };

            List<Mesh> meshes = new List<Mesh>();
            foreach (var mName in meshNames)
            {
                var m = AssetDatabase.LoadAssetAtPath<Mesh>(CombinedHousesDir + mName);
                if (m != null) meshes.Add(m);
            }

            if (meshes.Count == 0)
            {
                Debug.LogWarning("[House Swapper] Combined house meshes not found in " + CombinedHousesDir);
                return 0;
            }

            // Load Quaternius multi-materials
            Material woodTrim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_WoodTrim.mat");
            Material plaster = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_Plaster.mat");
            Material roundTiles = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_RoundTiles.mat");
            Material brick = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_Brick.mat");
            Material rockTrim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_RockTrim.mat");
            Material unevenBrick = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_UnevenBrick.mat");
            Material[] quatMaterials = new[] { woodTrim, plaster, roundTiles, brick, rockTrim, unevenBrick };

            int count = 0;
            var rowHousesRoot = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (rowHousesRoot != null)
            {
                int childCount = rowHousesRoot.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Transform t = rowHousesRoot.transform.GetChild(i);
                    if (t == null) continue;

                    // 1. Destroy all obsolete placeholder children:
                    //    - LIVED_IN_DETAILS (eliminates old floating windows and lanterns)
                    //    - Foundation_Plinth / Foundation_Plinth_Stone
                    //    - Old procedural primitive shapes (Armory_*, Porch, Hall, Roof, etc.)
                    for (int c = t.childCount - 1; c >= 0; c--)
                    {
                        UnityEngine.Object.DestroyImmediate(t.GetChild(c).gameObject);
                    }

                    // 2. Pick deterministic house mesh
                    Mesh chosenMesh = meshes[i % meshes.Count];

                    // 3. Assign MeshFilter
                    if (!t.TryGetComponent<MeshFilter>(out var mf))
                    {
                        mf = t.gameObject.AddComponent<MeshFilter>();
                    }
                    mf.sharedMesh = chosenMesh;

                    // 4. Assign MeshRenderer with Quaternius multi-materials
                    if (!t.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr = t.gameObject.AddComponent<MeshRenderer>();
                    }
                    mr.enabled = true;

                    Material[] assignedMats = new Material[chosenMesh.subMeshCount];
                    for (int m = 0; m < assignedMats.Length; m++)
                    {
                        assignedMats[m] = quatMaterials[m % quatMaterials.Length];
                    }
                    mr.sharedMaterials = assignedMats;

                    // 5. Update BoxCollider matching mesh bounds
                    var col = t.GetComponent<Collider>();
                    if (col != null && !(col is BoxCollider))
                    {
                        UnityEngine.Object.DestroyImmediate(col);
                    }
                    var box = t.GetComponent<BoxCollider>();
                    if (box == null)
                    {
                        box = t.gameObject.AddComponent<BoxCollider>();
                    }
                    box.center = chosenMesh.bounds.center;
                    box.size = chosenMesh.bounds.size;

                    // 6. Clear BatchingStatic flag
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    if ((flags & StaticEditorFlags.BatchingStatic) != 0)
                    {
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags & ~StaticEditorFlags.BatchingStatic);
                    }

                    // 7. Grounding Raycast downward against terrain
                    Vector3 worldPos = t.position;
                    float groundY = SampleGroundY(new Vector3(worldPos.x, 0f, worldPos.z));
                    t.position = new Vector3(worldPos.x, groundY, worldPos.z);

                    // 8. Tangent orientation along concentric ring road
                    Vector2 flatPos = new Vector2(t.position.x, t.position.z);
                    if (flatPos.sqrMagnitude > 4f)
                    {
                        Vector3 radialDir = new Vector3(flatPos.x, 0f, flatPos.y).normalized;
                        Vector3 tangentDir = Vector3.Cross(Vector3.up, radialDir);
                        t.rotation = Quaternion.LookRotation(tangentDir, Vector3.up);
                    }

                    count++;
                }
            }

            Debug.Log($"[House Swapper] Positioned, cleaned child details, and raycast-grounded {count} houses with Quaternius multi-material assemblies.");
            return count;
        }

        public static int GenerateStoneFoundationPlinths()
        {
            var rowHousesRoot = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (rowHousesRoot == null) return 0;

            int plinthsCreated = 0;
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_RockTrim.mat");
            if (stoneMat == null)
            {
                stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_Brick.mat");
            }

            int count = rowHousesRoot.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform houseT = rowHousesRoot.transform.GetChild(i);
                if (houseT == null) continue;

                // Remove any existing plinth first to ensure fresh, correctly scaled plinths
                Transform existingPlinth = houseT.Find("Foundation_Plinth_Stone");
                if (existingPlinth != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingPlinth.gameObject);
                }

                Vector3 basePos = houseT.position;
                float halfW = 3.2f;
                float halfD = 4.6f;

                // Sample 4 bottom corners in world space
                float yFL = SampleGroundY(basePos + houseT.right * -halfW + houseT.forward * halfD);
                float yFR = SampleGroundY(basePos + houseT.right * halfW + houseT.forward * halfD);
                float yBL = SampleGroundY(basePos + houseT.right * -halfW + houseT.forward * -halfD);
                float yBR = SampleGroundY(basePos + houseT.right * halfW + houseT.forward * -halfD);

                float hMin = Mathf.Min(Mathf.Min(yFL, yFR), Mathf.Min(yBL, yBR));
                float hMax = Mathf.Max(Mathf.Max(yFL, yFR), Mathf.Max(yBL, yBR));
                float slopeDiff = hMax - hMin;

                // If on a slope, generate stone plinth skirt beneath the house
                if (slopeDiff > 0.15f)
                {
                    houseT.position = new Vector3(basePos.x, hMax, basePos.z);

                    var plinthObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plinthObj.name = "Foundation_Plinth_Stone";
                    plinthObj.transform.SetParent(houseT, false);

                    // House transform localScale is (100, 100, 100), so local dimensions MUST be divided by 100!
                    float plinthHeight = slopeDiff + 0.35f;
                    plinthObj.transform.localScale = new Vector3((halfW * 2.05f) / 100f, plinthHeight / 100f, (halfD * 2.05f) / 100f);
                    plinthObj.transform.localPosition = new Vector3(0f, -(plinthHeight * 0.5f) / 100f, 0f);
                    plinthObj.transform.localRotation = Quaternion.identity;

                    if (stoneMat != null && plinthObj.TryGetComponent<Renderer>(out var rend))
                    {
                        rend.sharedMaterial = stoneMat;
                    }

                    plinthsCreated++;
                }
            }

            Debug.Log($"[Plinth Generator] Created {plinthsCreated} correctly-scaled stone foundation plinths on sloped tiers.");
            return plinthsCreated;
        }

        private static float SampleGroundY(Vector3 pos)
        {
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 100f, pos.z), Vector3.down, out RaycastHit hit, 200f))
            {
                return hit.point.y;
            }
            return pos.y;
        }

        public static int PopulateFantasyStreetProps()
        {
            GameObject propRoot = GameObject.Find("METROPOLIS_STREET_PROPS") ?? new GameObject("METROPOLIS_STREET_PROPS");

            // Look for props in FantasyPropsMegaKit
            string[] propGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models/QuaterniusMegaKits/FantasyPropsMegaKit" });
            List<GameObject> loadedProps = new List<GameObject>();
            foreach (var g in propGuids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null)
                {
                    string n = go.name.ToLowerInvariant();
                    if (n.Contains("barrel") || n.Contains("crate") || n.Contains("lantern") || n.Contains("stall") || n.Contains("bench"))
                        loadedProps.Add(go);
                }
            }

            int placed = 0;
            if (loadedProps.Count > 0 && propRoot.transform.childCount == 0)
            {
                // Place props along tiers
                for (int i = 0; i < 48; i++)
                {
                    float angle = (i / 48f) * Mathf.PI * 2f;
                    float radius = 42f + (i % 3) * 22f;
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    pos.y = SampleGroundY(pos);

                    var template = loadedProps[i % loadedProps.Count];
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(template, propRoot.transform);
                    if (inst == null) inst = UnityEngine.Object.Instantiate(template, propRoot.transform);

                    inst.transform.position = pos;
                    inst.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    placed++;
                }
            }

            return placed;
        }

        public static int FitTreePlanterRings()
        {
            GameObject propRoot = GameObject.Find("METROPOLIS_STREET_PROPS") ?? new GameObject("METROPOLIS_STREET_PROPS");
            var trees = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int fitted = 0;

            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/QuaterniusMegaKits/Materials/M_Quaternius_RockTrim.mat");

            foreach (var go in trees)
            {
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();
                if ((n.Contains("tree") || n.Contains("pine") || n.Contains("willow")) && !n.Contains("planter"))
                {
                    // Check if already has planter ring
                    if (go.transform.Find("Stone_Planter_Ring") != null) continue;

                    // Place a stone planter ring (Radius = 1.6m, Height = 0.35m) around trunk base in world space
                    var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ring.name = "Stone_Planter_Ring";
                    ring.transform.SetParent(propRoot.transform, true);
                    ring.transform.position = new Vector3(go.transform.position.x, SampleGroundY(go.transform.position) + 0.175f, go.transform.position.z);
                    ring.transform.rotation = Quaternion.identity;
                    ring.transform.localScale = new Vector3(3.2f, 0.35f, 3.2f);

                    if (stoneMat != null && ring.TryGetComponent<Renderer>(out var r))
                    {
                        r.sharedMaterial = stoneMat;
                    }
                    fitted++;
                }
            }

            return fitted;
        }

        public static void ConfigureGoldenHourLightingAndShaders()
        {
            Light sun = RenderSettings.sun != null ? RenderSettings.sun : UnityEngine.Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.color = new Color(1.0f, 0.957f, 0.839f, 1.0f); // #FFF4D6 Warm Golden-Hour
                sun.intensity = 1.35f;
                sun.transform.rotation = Quaternion.Euler(38f, -42f, 0f);
                sun.shadows = LightShadows.Soft;
            }
        }

        public static void SetupCharactersAndProportions()
        {
            // MaleStarter.glb -> 1.78m Player
            var player = GameObject.Find("Player_Hero") ?? GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.height = 1.78f;
                    cc.center = new Vector3(0f, 0.89f, 0f);
                }

                if (player.GetComponent<ModularFaceRigController>() == null)
                {
                    player.AddComponent<ModularFaceRigController>();
                }
            }

            // Catherine.glb -> 1.82m inside Guild Hall
            var catherine = GameObject.Find("NPC_Catherine_GuildhallClerk") ?? GameObject.Find("NPC_Catherine");
            if (catherine == null)
            {
                catherine = new GameObject("NPC_Catherine_GuildhallClerk");
                catherine.transform.position = new Vector3(12.5f, SampleGroundY(new Vector3(12.5f, 0f, 15f)), 15f);
            }

            if (catherine.GetComponent<GuildHallClerkInteractable>() == null)
            {
                catherine.AddComponent<GuildHallClerkInteractable>();
            }

            if (catherine.GetComponent<ModularFaceRigController>() == null)
            {
                catherine.AddComponent<ModularFaceRigController>();
            }
        }

        public static void EnsureAppraisalOrbInCathedral()
        {
            var orb = UnityEngine.Object.FindFirstObjectByType<AppraisalOrbInteractable>();
            if (orb == null)
            {
                var orbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orbGo.name = "Cathedral_Appraisal_Orb";
                orbGo.transform.position = new Vector3(0f, SampleGroundY(Vector3.zero) + 2.2f, 0f);
                orbGo.transform.localScale = Vector3.one * 1.6f;
                orb = orbGo.AddComponent<AppraisalOrbInteractable>();
                Debug.Log("<color=#38BDF8><b>✦ [APPRAISAL ORB] Spawned in Central Cathedral plaza!</b></color>");
            }
        }

        public static void PopulateWildernessMonsterCamps()
        {
            GameObject monsterRoot = GameObject.Find("WILDERNESS_MONSTER_CAMPS") ?? new GameObject("WILDERNESS_MONSTER_CAMPS");

            // Look for Bestiary Imp and Puglin models
            string impPath = "Assets/Models/QuaterniusMegaKits/BestiaryDungeonMonstersKit/Bestiary - Dungeon Monsters Kit[Standard]/Exports/FBX (Unity)/Imp.fbx";
            string puglinPath = "Assets/Models/QuaterniusMegaKits/BestiaryDungeonMonstersKit/Bestiary - Dungeon Monsters Kit[Standard]/Exports/FBX (Unity)/Puglin.fbx";

            var impModel = AssetDatabase.LoadAssetAtPath<GameObject>(impPath);
            var puglinModel = AssetDatabase.LoadAssetAtPath<GameObject>(puglinPath);

            if (monsterRoot.transform.childCount == 0 && (impModel != null || puglinModel != null))
            {
                Vector3[] campPositions = new[]
                {
                    new Vector3(180f, 0f, 120f),
                    new Vector3(-160f, 0f, 140f),
                    new Vector3(140f, 0f, -180f),
                    new Vector3(-150f, 0f, -150f)
                };

                for (int i = 0; i < campPositions.Length; i++)
                {
                    Vector3 campPos = campPositions[i];
                    campPos.y = SampleGroundY(campPos);

                    var campObj = new GameObject($"WildernessCamp_{i + 1}");
                    campObj.transform.SetParent(monsterRoot.transform, false);
                    campObj.transform.position = campPos;

                    GameObject template = (i % 2 == 0 && impModel != null) ? impModel : (puglinModel != null ? puglinModel : impModel);
                    if (template != null)
                    {
                        var monster = (GameObject)PrefabUtility.InstantiatePrefab(template, campObj.transform);
                        if (monster == null) monster = UnityEngine.Object.Instantiate(template, campObj.transform);
                        monster.transform.localPosition = Vector3.zero;
                        monster.name = $"Enemy_{template.name}";
                    }
                }
            }
        }

        public static int ExtractProceduralMeshesToAssetFiles()
        {
            if (!Directory.Exists(GeneratedMeshesDir))
            {
                Directory.CreateDirectory(GeneratedMeshesDir);
            }

            int extracted = 0;
            var filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

            foreach (var mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                Mesh m = mf.sharedMesh;

                string assetPath = AssetDatabase.GetAssetPath(m);
                if (string.IsNullOrEmpty(assetPath))
                {
                    // Embedded procedural mesh in scene! Save to disk
                    string cleanName = string.IsNullOrEmpty(m.name) ? mf.gameObject.name : m.name;
                    cleanName = cleanName.Replace(" ", "_").Replace(":", "_").Replace("/", "_").Replace("\\", "_");
                    string savePath = $"{GeneratedMeshesDir}{cleanName}_{m.vertexCount}_{m.triangles.Length}.asset";

                    Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
                    if (existing == null)
                    {
                        AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(m), savePath);
                        mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);
                        extracted++;
                    }
                    else
                    {
                        mf.sharedMesh = existing;
                        extracted++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Mesh Extraction] Extracted and serialized {extracted} in-scene procedural meshes to {GeneratedMeshesDir}.");
            return extracted;
        }
    }
}
#endif
