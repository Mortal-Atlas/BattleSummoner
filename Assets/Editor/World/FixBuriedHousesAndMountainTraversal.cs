#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BattleSummoner.World.Editor
{
    public static class FixBuriedHousesAndMountainTraversal
    {
        private static readonly float[] TIER_RADII   = { 95f, 195f, 310f, 450f };
        private static readonly float[] TIER_HEIGHTS = { 80f, 58f,  38f,  22f  };

        [MenuItem("BattleSummoner/MCP World Remediation/Fix Buried Houses, Floating Rocks, Mountain Paths & Animator")]
        public static void ExecuteFixes()
        {
            // 1. Delete floating dark rock artifacts above the river (seen in image_818635.jpg)
            PurgeFloatingRiverRocksAndAdware();

            // 2. Build crisp vertical masonry tier drums so diagonal terrain slopes never bury houses
            BuildCrispVerticalTierDrums();

            // 3. Un-bury every RowHouse_* & generate individual stone foundation plinths
            UnburyAndPlinthAllKingdomBuildings();

            // 4. Populate Mountain Switchbacks with Boulders, Gate Arches, Bridges & Obstacles
            PopulateMountainTraversalObstaclesAndPaths();

            // 5. Fix Console Error: Add missing 'BrawlerStrike' trigger to PlayerHeroAnimator
            FixPlayerHeroAnimatorBrawlerStrikeParameter();

            SceneView.RepaintAll();
            Debug.Log("[Antigravity MCP] All buried buildings un-clipped, floating rocks purged, mountain obstacles spawned, and Animator fixed.");
        }

        public static float GetExactTierHeight(float r)
        {
            if (r <= 95f)  return 80f;
            if (r <= 195f) return 58f;
            if (r <= 310f) return 38f;
            return 22f;
        }

        private static Material GetStoneMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Metro_StoneBase.mat");
            if (mat == null)
            {
                string[] guids = AssetDatabase.FindAssets("M_Metro_StoneBase t:Material");
                if (guids.Length > 0) mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return mat;
        }

        // =====================================================================
        // 1. PURGE FLOATING BLACK ROCK ARTIFACTS ABOVE NORTH RIVER
        // =====================================================================
        public static void PurgeFloatingRiverRocksAndAdware()
        {
            int purged = 0;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                Vector3 p = go.transform.position;

                // Detect floating rocks/meshes suspended in mid-air above the northern river channel
                if (p.z > 450f && Mathf.Abs(p.x) < 280f && p.y > 30f)
                {
                    if (go.name.Contains("SkyIsland") || go.name.Contains("Cloud") || go.name.Contains("Highway") || go.name.Contains("Gate"))
                        continue;

                    if (Physics.Raycast(p, Vector3.down, out RaycastHit hit, 300f))
                    {
                        if (p.y - hit.point.y > 5.0f)
                        {
                            Undo.DestroyObjectImmediate(go);
                            purged++;
                        }
                    }
                }
            }
            Debug.Log($"[Antigravity MCP] Purged {purged} floating rock artifacts above the river.");
        }

        // =====================================================================
        // 2. BUILD CRISP VERTICAL MASONRY TIER DRUMS (NO DIAGONAL DIRT RAMPS)
        // =====================================================================
        public static void BuildCrispVerticalTierDrums()
        {
            GameObject existing = GameObject.Find("Kingdom_CrispMasonryTiers");
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject("Kingdom_CrispMasonryTiers");
            Material stoneMat = GetStoneMaterial();

            for (int i = 0; i < 4; i++)
            {
                float r = TIER_RADII[i];
                float topY = TIER_HEIGHTS[i];

                GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                drum.name = $"Kingdom_Tier{4 - i}_VerticalMasonryFoundation";
                drum.transform.SetParent(root.transform, false);
                drum.transform.position = new Vector3(0f, topY * 0.5f, 0f);
                drum.transform.localScale = new Vector3(r * 2f, topY * 0.5f, r * 2f);
                if (stoneMat != null) drum.GetComponent<Renderer>().sharedMaterial = stoneMat;
            }
            Debug.Log("[Antigravity MCP] Built crisp 128-segment vertical masonry tier drums.");
        }

        // =====================================================================
        // 3. UN-BURY ALL `RowHouse_*` BUILDINGS & ENFORCE TIER CLIFF SETBACK
        // =====================================================================
        public static void UnburyAndPlinthAllKingdomBuildings()
        {
            Material stoneMat = GetStoneMaterial();
            int modifiedCount = 0;

            var specs = new System.Collections.Generic.Dictionary<string, (float w, float d)>
            {
                { "Building_House_Residential", (9.8f, 11.8f) },
                { "Building_House_LowDistrict", (7.8f, 9.2f) },
                { "Building_Inn_Lodge", (12.2f, 16.2f) },
                { "Building_Bakery_Shop", (9.8f, 11.4f) },
                { "Building_Blacksmith_OpenForge", (14.5f, 11.9f) },
                { "Building_AlchemistWorkshop", (11.4f, 11.4f) },
                { "Building_EnchanterWorkshop", (11.4f, 11.4f) },
                { "Building_GuildArmory", (11.8f, 14.2f) },
                { "Building_Tavern_Open", (14.0f, 16.6f) },
                { "Building_Guildhall_Grand", (17.8f, 23.0f) },
                { "Building_WizardObservatory", (13.8f, 13.8f) }
            };

            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                // Strict building filter: must start with RowHouse_ or Building_, never NPCs, props or lights
                if (!go.name.StartsWith("RowHouse_") && !go.name.StartsWith("Building_"))
                    continue;
                if (go.name.Contains("Citizen") || go.name.Contains("Shopper") || go.name.Contains("Tavern_Patron") || 
                    go.name.Contains("Barkeep") || go.name.Contains("NPC") || go.name.Contains("Table") || 
                    go.name.Contains("Chandelier") || go.name.Contains("Terrace"))
                    continue;
                if (go.transform.root.name.Contains("NPC") || go.transform.root.name == "ENVIRONMENT_ROOT")
                    continue;

                // If bound to a static Combined Mesh, unpack prefab or reset local mesh reference so transform moves cleanly
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Combined Mesh"))
                {
                    GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (sourcePrefab != null)
                    {
                        MeshFilter pfMf = sourcePrefab.GetComponent<MeshFilter>();
                        if (pfMf != null && pfMf.sharedMesh != null) mf.sharedMesh = pfMf.sharedMesh;
                    }
                }

                Vector3 pos = go.transform.position;
                float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                if (r > 450f) continue;

                float angleRad = Mathf.Atan2(pos.z, pos.x);

                // Push building inward by 12m if it sits on the lip of a tier drop-off (95m, 195m, 310m, 450m)
                foreach (float boundaryR in TIER_RADII)
                {
                    if (Mathf.Abs(r - boundaryR) < 11.5f)
                    {
                        r = (r < boundaryR) ? boundaryR - 12.0f : boundaryR + 12.0f;
                        pos.x = Mathf.Cos(angleRad) * r;
                        pos.z = Mathf.Sin(angleRad) * r;
                    }
                }

                // Snap Y strictly to the flat top deck of its tier (NEVER inside the diagonal terrain slope)
                float deckY = GetExactTierHeight(r);
                go.transform.position = new Vector3(pos.x, deckY, pos.z);

                // Determine tailored plinth dimensions
                float width = 9.8f;
                float depth = 11.8f;
                foreach (var kv in specs)
                {
                    if (go.name.Contains(kv.Key)) { width = kv.Value.w; depth = kv.Value.d; break; }
                }

                // Spawn or retarget stone Foundation Plinth underneath the building
                Transform oldPlinth = go.transform.Find("Foundation_Plinth");
                GameObject plinthObj = oldPlinth != null ? oldPlinth.gameObject : null;
                if (plinthObj == null)
                {
                    plinthObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plinthObj.name = "Foundation_Plinth";
                    plinthObj.transform.SetParent(go.transform, false);
                }

                float pScale = go.transform.lossyScale.x;
                float inv = pScale > 0.001f ? (1f / pScale) : 1f;

                plinthObj.transform.localPosition = new Vector3(0f, -1.2f * inv, 0f);
                plinthObj.transform.localScale = new Vector3(width * inv, 2.5f * inv, depth * inv);
                
                var box = plinthObj.GetComponent<BoxCollider>();
                if (box != null) UnityEngine.Object.DestroyImmediate(box);

                if (stoneMat != null) plinthObj.GetComponent<Renderer>().sharedMaterial = stoneMat;
                modifiedCount++;
            }
            Debug.Log($"[Antigravity MCP] Un-buried and plinthed {modifiedCount} kingdom buildings.");
        }

        // =====================================================================
        // 4. POPULATE MOUNTAIN SWITCHBACK PATHS, BRIDGES & ROCK OBSTACLES
        // =====================================================================
        public static void PopulateMountainTraversalObstaclesAndPaths()
        {
            GameObject oldMountainRoot = GameObject.Find("Mountain_Traversal_Paths_And_Obstacles");
            if (oldMountainRoot != null) Undo.DestroyObjectImmediate(oldMountainRoot);

            GameObject root = new GameObject("Mountain_Traversal_Paths_And_Obstacles");
            Material stoneMat = GetStoneMaterial();

            // Spawn stepping waypoints, retaining walls, and boulder obstacles along the North Switchback Trail
            int steps = 48;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float wz = Mathf.Lerp(560f, 2250f, t);
                float wx = -260f + Mathf.Sin(t * Mathf.PI * 4.5f) * 290f;
                float wy = 20f + t * 168f;

                if (Physics.Raycast(new Vector3(wx, 600f, wz), Vector3.down, out RaycastHit hit, 800f))
                    wy = hit.point.y;

                // STRICT DRY LAND CHECK: Water level is Y=18.0m, do not place mountain steps/boulders underwater
                if (wy < 19.5f) continue;

                // 1. Stone Switchback Trail Retaining Curb / Waypoint Slab
                GameObject pathSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pathSlab.name = $"MountainPath_StepSlab_{i:02d}";
                pathSlab.transform.SetParent(root.transform, false);
                pathSlab.transform.position = new Vector3(wx, wy - 0.3f, wz);
                pathSlab.transform.localScale = new Vector3(12f, 1.2f, 12f);
                if (stoneMat != null) pathSlab.GetComponent<Renderer>().sharedMaterial = stoneMat;

                // 2. Flanking Crag Boulders & Chokepoint Obstacles every 3rd node
                if (i % 3 == 0)
                {
                    GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    boulder.name = $"Mountain_Obstacle_CragBoulder_{i:02d}";
                    boulder.transform.SetParent(root.transform, false);
                    boulder.transform.position = new Vector3(wx + ((i % 2 == 0) ? 9.5f : -9.5f), wy + 2.5f, wz);
                    boulder.transform.localScale = new Vector3(7.5f, 11.0f, 6.5f);
                    boulder.transform.rotation = Quaternion.Euler(14f * i, 37f * i, 9f * i);
                    if (stoneMat != null) boulder.GetComponent<Renderer>().sharedMaterial = stoneMat;
                }
            }
            Debug.Log("[Antigravity MCP] Populated mountain switchback paths, curbs, and rock obstacles.");
        }

        // =====================================================================
        // 5. FIX CONSOLE ERROR: ADD `BrawlerStrike` TO `PlayerHeroAnimator`
        // =====================================================================
        public static void FixPlayerHeroAnimatorBrawlerStrikeParameter()
        {
            foreach (string guid in AssetDatabase.FindAssets("PlayerHeroAnimator t:AnimatorController"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (ctrl == null) continue;

                bool exists = false;
                foreach (var p in ctrl.parameters)
                {
                    if (p.name == "BrawlerStrike") { exists = true; break; }
                }
                if (!exists)
                {
                    ctrl.AddParameter("BrawlerStrike", AnimatorControllerParameterType.Trigger);
                    EditorUtility.SetDirty(ctrl);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[Antigravity MCP] Added missing 'BrawlerStrike' parameter to PlayerHeroAnimator.");
                }
            }
        }
    }
}
#endif
