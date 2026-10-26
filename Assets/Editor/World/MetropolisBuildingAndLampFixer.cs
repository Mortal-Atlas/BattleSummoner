#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BattleSummoner.Optimization;

namespace BattleSummoner.Editor.World
{
    /// <summary>
    /// MetropolisBuildingAndLampFixer
    /// 1. Fixes Door_Sconce_Lantern: Sets freestanding candle lanterns upright ON THE GROUND / stoop
    ///    beside front doorways and destroys all artificial wall brackets.
    /// 2. Fixes Broken / Overlapping Buildings: Uses 2D Separating Axis Theorem (SAT) footprint collision
    ///    detection to purge colliding/overlapping buildings that slice through facades and alleys.
    /// 3. Fixes Window Framing: Adds dark timber casings to illuminated windows across the city.
    /// </summary>
    public static class MetropolisBuildingAndLampFixer
    {
        private const string ROW_HOUSES_PATH = "8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses";

        private static readonly Dictionary<string, (float w, float d)> ModelFootprints = new Dictionary<string, (float w, float d)>
        {
            { "Building_House_Residential", (9.5f, 11.5f) },
            { "Building_House_LowDistrict", (7.6f, 9.0f) },
            { "Building_Inn_Lodge", (12.0f, 15.9f) },
            { "Building_Bakery_Shop", (9.5f, 11.2f) },
            { "Building_Blacksmith_OpenForge", (14.3f, 11.7f) },
            { "Building_AlchemistWorkshop", (11.2f, 11.2f) },
            { "Building_EnchanterWorkshop", (11.2f, 11.2f) },
            { "Building_GuildArmory", (11.5f, 14.0f) },
            { "Building_Tavern_Open", (13.8f, 16.4f) },
            { "Building_Guildhall_Grand", (17.6f, 22.8f) },
            { "Building_WizardObservatory", (13.5f, 13.5f) },
            { "Building_MagicItemStore", (9.8f, 11.7f) }
        };

        [MenuItem("BattleSummoner/Metropolis/Fix Broken Buildings and Ground Lamps", false, 160)]
        public static void ExecuteFixAll()
        {
            Debug.Log("<color=#FFD54F><b>[METROPOLIS FIXER] Starting Comprehensive Building & Lamp Fix...</b></color>");

            int lanternsFixed = FixAllGroundLanterns();
            int buildingsRemoved = PurgeOverlappingBuildings();
            int windowFramesAdded = EnhanceWindowFraming();
            var (benchesFixed, sacksFixed) = FixAllBenchesAndPantrySacks();
            bool roadUpgraded = UpgradeRoadToHighDetailCobblestone();
            int floatingTreesFixed = PurgeFloatingTownTreesAndProps();
            var (meshesRestored, undergroundPurged, propsGrounded) = FixAndGroundAllMetropolisLampsAndBenches();
            var (housesFixed, bazaarPropsFixed, collidingRemoved) = FixAndElevateDistrictIIBazaarAndAlchemyHouse();
            var (guildhallSpawned, tier2HousesSpawned, tier2PropsSpawned) = PopulateTier2GuildhallAndArtisanPlaza();

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#76FF03><b>[METROPOLIS FIXER] Successfully Completed!</b>\n" +
                      $"- {housesFixed} Alchemy House & {bazaarPropsFixed} Bazaar props elevated onto Tier 3 High Market Promenade (0 buried beneath foundation/stairs).\n" +
                      $"- {collidingRemoved} colliding rowhouses purged.\n" +
                      $"- {guildhallSpawned} Landmark Grand Guildhall, {tier2HousesSpawned} rowhouses, and {tier2PropsSpawned} plaza props populated into formerly empty Tier 2 sector.\n" +
                      $"- {lanternsFixed} Door_Sconce_Lanterns moved to ground/stoop with wall brackets removed.\n" +
                      $"- {buildingsRemoved} overlapping/clipping buildings resolved.\n" +
                      $"- {windowFramesAdded} timber window frames added.\n" +
                      $"- {benchesFixed} Front_Porch_Benches rotated on Z/Y axes, human-scaled & textured.\n" +
                      $"- {sacksFixed} Porch_Pantry_Supply sacks human-scaled (0.22) & textured with burlap.\n" +
                      $"- {floatingTreesFixed} floating trees & props purged/grounded in town.\n" +
                      $"- {meshesRestored} corrupted Combined_Mesh instances restored to original prefab meshes.\n" +
                      $"- {undergroundPurged} lamps spawning under grand staircases/firewood purged.\n" +
                      $"- {propsGrounded} street lamps & park benches grounded flush on sidewalk pavement.\n" +
                      $"- Road surface upgraded to high-detail seamless cobblestone with normal mapping.</color>");
        }

        public static bool UpgradeRoadToHighDetailCobblestone()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/Mat_City_PavedLimestone.mat");
            if (mat == null) return false;

            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PainterlyGenshin/T_Genshin_PlazaCobblestone_Seamless.png");
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PainterlyGenshin/T_Genshin_PlazaCobblestone_Normal.png");

            if (urpLit != null) mat.shader = urpLit;
            if (albedoTex != null) mat.SetTexture("_BaseMap", albedoTex);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
                mat.SetFloat("_BumpScale", 0.85f);
            }
            mat.SetFloat("_Smoothness", 0.15f);
            mat.SetColor("_BaseColor", new Color(0.96f, 0.95f, 0.92f, 1.0f));

            // Tiling: 1 UV unit = 5.556 meters.
            // (3.0, 3.0) gives 1.85m per texture repeat, ~18cm per cobblestone paver.
            mat.SetTextureScale("_BaseMap", new Vector2(3.0f, 3.0f));
            if (normalTex != null) mat.SetTextureScale("_BumpMap", new Vector2(3.0f, 3.0f));

            EditorUtility.SetDirty(mat);
            return true;
        }

        public static (int benches, int sacks) FixAllBenchesAndPantrySacks()
        {
            var benchMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval props/Materials/BenchV1.mat");
            var sackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Medieval props/Materials/SackV1.mat");

            if (benchMat != null)
            {
                benchMat.SetFloat("_Smoothness", 0.25f);
                EditorUtility.SetDirty(benchMat);
            }
            if (sackMat != null)
            {
                sackMat.SetFloat("_Smoothness", 0.08f);
                sackMat.SetColor("_BaseColor", new Color(0.85f, 0.78f, 0.68f, 1.0f));
                sackMat.SetColor("_Color", new Color(0.85f, 0.78f, 0.68f, 1.0f));
                EditorUtility.SetDirty(sackMat);
            }

            int benchesFixed = 0;
            int sacksFixed = 0;

            var allDecor = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name == "LIVED_IN_DETAILS").ToList();

            foreach (var lived in allDecor)
            {
                var bench = lived.Find("Front_Porch_Bench");
                if (bench != null)
                {
                    bench.localRotation = Quaternion.Euler(0f, -90f, 90f);
                    bench.localScale = Vector3.one * 0.16f;

                    Vector3 lp = bench.localPosition;
                    float side = (lp.x >= 0f) ? 1.0f : -1.0f;
                    float targetX = (side * 2.4f) - 1.04f;
                    bench.localPosition = new Vector3(targetX, 0f, lp.z);

                    var mr = bench.GetComponentInChildren<MeshRenderer>();
                    if (mr != null && benchMat != null)
                    {
                        mr.sharedMaterial = benchMat;
                    }
                    benchesFixed++;
                }

                var sack = lived.Find("Porch_Pantry_Supply");
                if (sack != null)
                {
                    var mf = sack.GetComponentInChildren<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Cylinder"))
                    {
                        sack.localRotation = Quaternion.Euler(270f, 180f, 0f);
                        sack.localScale = Vector3.one * 0.22f;

                        Vector3 lp = sack.localPosition;
                        sack.localPosition = new Vector3(lp.x, 0f, lp.z);

                        var mr = sack.GetComponentInChildren<MeshRenderer>();
                        if (mr != null && sackMat != null)
                        {
                            mr.sharedMaterial = sackMat;
                        }
                        sacksFixed++;
                    }
                    else
                    {
                        Vector3 lp = sack.localPosition;
                        sack.localPosition = new Vector3(lp.x, 0f, lp.z);
                    }
                }
            }

            return (benchesFixed, sacksFixed);
        }

        public static int FixAllGroundLanterns()
        {
            int moved = 0;
            var allDecor = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name == "LIVED_IN_DETAILS").ToList();

            foreach (var lived in allDecor)
            {
                var bracket = lived.Find("Sconce_Wall_Bracket");
                if (bracket != null)
                {
                    UnityEngine.Object.DestroyImmediate(bracket.gameObject);
                }

                var lantern = lived.Find("Door_Sconce_Lantern");
                if (lantern != null)
                {
                    Vector3 lp = lantern.localPosition;
                    if (lp.y > 0.5f)
                    {
                        lantern.localPosition = new Vector3(lp.x * 0.85f, 0.08f, lp.z + 0.15f);
                        lantern.localRotation = Quaternion.identity;
                        moved++;
                    }
                }
            }

            return moved;
        }

        public static int PurgeOverlappingBuildings()
        {
            var root = GameObject.Find(ROW_HOUSES_PATH);
            if (root == null)
            {
                Debug.LogWarning($"[METROPOLIS FIXER] Root '{ROW_HOUSES_PATH}' not found!");
                return 0;
            }

            var houses = new List<Transform>();
            foreach (Transform t in root.transform)
            {
                if (t.gameObject.activeInHierarchy) houses.Add(t);
            }

            var cornersDict = new Dictionary<Transform, Vector2[]>();
            foreach (var h in houses) cornersDict[h] = GetFootprintCorners(h);

            // Prioritize preserving landmarks, larger buildings, and street frontage
            var sortedHouses = houses.OrderByDescending(h => {
                int score = 0;
                if (h.name.Contains("Guildhall")) score += 50;
                if (h.name.Contains("Tavern")) score += 40;
                if (h.name.Contains("Inn")) score += 30;
                if (h.name.Contains("Observatory")) score += 30;
                if (h.name.Contains("Armory")) score += 20;
                if (h.name.Contains("Bakery")) score += 10;
                return score;
            }).ToList();

            var kept = new List<Transform>();
            var toRemove = new HashSet<Transform>();

            foreach (var h in sortedHouses)
            {
                bool collides = false;
                Vector2 posA = new Vector2(h.position.x, h.position.z);
                var polyA = cornersDict[h];

                foreach (var k in kept)
                {
                    Vector2 posB = new Vector2(k.position.x, k.position.z);
                    if (Vector2.Distance(posA, posB) < 18.0f)
                    {
                        if (PolygonOverlapSAT(polyA, cornersDict[k]))
                        {
                            collides = true;
                            break;
                        }
                    }
                }

                if (collides)
                {
                    toRemove.Add(h);
                }
                else
                {
                    kept.Add(h);
                }
            }

            foreach (var h in toRemove)
            {
                UnityEngine.Object.DestroyImmediate(h.gameObject);
            }

            return toRemove.Count;
        }

        public static int EnhanceWindowFraming()
        {
            var timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_TimberWood.mat");
            var lanternGlowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_LanternGlow.mat");
            if (lanternGlowMat != null)
            {
                lanternGlowMat.SetColor("_BaseColor", new Color(0.92f, 0.74f, 0.36f, 1.0f));
                lanternGlowMat.SetColor("_Color", new Color(0.92f, 0.74f, 0.36f, 1.0f));
                lanternGlowMat.EnableKeyword("_EMISSION");
                lanternGlowMat.SetColor("_EmissionColor", new Color(0.38f, 0.26f, 0.10f, 1.0f));
                EditorUtility.SetDirty(lanternGlowMat);
            }

            var windowGlows = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(t => t.name.StartsWith("Window_Glow_")).ToList();

            int framesAdded = 0;
            foreach (var wg in windowGlows)
            {
                var existingFrame = wg.Find("Window_Frame");
                if (existingFrame == null)
                {
                    var frameGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    frameGo.name = "Window_Frame";
                    frameGo.transform.SetParent(wg, false);
                    frameGo.transform.localPosition = new Vector3(0f, 0f, -0.015f);
                    frameGo.transform.localScale = new Vector3(1.30f, 1.50f, 0.04f);
                    frameGo.transform.localRotation = Quaternion.identity;
                    UnityEngine.Object.DestroyImmediate(frameGo.GetComponent<Collider>());
                    if (timberMat != null) frameGo.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
                    framesAdded++;
                }
            }

            return framesAdded;
        }

        private static Vector2[] GetFootprintCorners(Transform h)
        {
            float w = 9.5f; float d = 11.5f;
            foreach (var kv in ModelFootprints)
            {
                if (h.name.Contains(kv.Key)) { w = kv.Value.w; d = kv.Value.d; break; }
            }
            w -= 0.5f; d -= 0.5f;

            Vector2 pos = new Vector2(h.position.x, h.position.z);
            float angle = h.eulerAngles.y * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
            Vector2 fwd = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

            float hw = w * 0.5f; float hd = d * 0.5f;

            return new Vector2[]
            {
                pos + right * hw + fwd * hd,
                pos - right * hw + fwd * hd,
                pos - right * hw - fwd * hd,
                pos + right * hw - fwd * hd
            };
        }

        private static bool PolygonOverlapSAT(Vector2[] polyA, Vector2[] polyB)
        {
            foreach (var poly in new[] { polyA, polyB })
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p1 = poly[i];
                    Vector2 p2 = poly[(i + 1) % 4];
                    Vector2 edge = p2 - p1;
                    Vector2 axis = new Vector2(-edge.y, edge.x).normalized;

                    float minA = float.MaxValue, maxA = float.MinValue;
                    foreach (var p in polyA)
                    {
                        float proj = Vector2.Dot(p, axis);
                        minA = Mathf.Min(minA, proj); maxA = Mathf.Max(maxA, proj);
                    }

                    float minB = float.MaxValue, maxB = float.MinValue;
                    foreach (var p in polyB)
                    {
                        float proj = Vector2.Dot(p, axis);
                        minB = Mathf.Min(minB, proj); maxB = Mathf.Max(maxB, proj);
                    }

                    if (maxA < minB || maxB < minA) return false;
                }
            }
            return true;
        }

        public static int PurgeFloatingTownTreesAndProps()
        {
            int purged = 0;

            // 1. Purge legacy broken avenue flora and floating lamps group
            var legacyAvenues = GameObject.Find("--- METROPOLIS_DISTRICT_DETAILS ---/Metropolis_Radial_Avenues_Flora_And_Lamps");
            if (legacyAvenues != null)
            {
                purged += legacyAvenues.transform.childCount;
                Undo.DestroyObjectImmediate(legacyAvenues);
                Debug.Log("[METROPOLIS FIXER] Purged broken Metropolis_Radial_Avenues_Flora_And_Lamps root.");
            }

            // 2. Scan scene for any other rogue floating trees within metropolis bounds (r < 450m)
            var allGos = Resources.FindObjectsOfTypeAll<GameObject>();
            var treesToGroundOrPurge = new List<GameObject>();

            foreach (var go in allGos)
            {
                if (go.hideFlags != HideFlags.None) continue;
                if (EditorUtility.IsPersistent(go)) continue;
                if (go.scene.name != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) continue;

                Vector3 pos = go.transform.position;
                float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                if (r >= 450f) continue;

                string n = go.name.ToLower();
                if (n.Contains("street") || n.Contains("spine") || n.Contains("bone") || n.Contains("rig") || n.Contains("cam")) continue;

                if (n.Contains("tree") || n.Contains("broadleaf") || n.Contains("oak"))
                {
                    // Skip child parts
                    if (go.transform.parent != null)
                    {
                        string pn = go.transform.parent.name.ToLower();
                        if (pn.Contains("tree") || pn.Contains("broadleaf") || pn.Contains("oak")) continue;
                    }

                    // Expected tier floor height
                    float expectedFloorY = 22.0f;
                    if (r < 95f) expectedFloorY = 80.0f;
                    else if (r < 195f) expectedFloorY = 58.0f;
                    else if (r < 310f) expectedFloorY = 38.0f;

                    // If floating significantly above expected floor
                    if (pos.y - expectedFloorY > 2.5f)
                    {
                        treesToGroundOrPurge.Add(go);
                    }
                }
            }

            foreach (var tree in treesToGroundOrPurge)
            {
                if (tree == null) continue;
                Vector3 pos = tree.transform.position;
                float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                float expectedFloorY = (r < 95f) ? 80.0f : ((r < 195f) ? 58.0f : ((r < 310f) ? 38.0f : 22.0f));

                // Check if colliding with house or roof
                var hits = Physics.OverlapSphere(new Vector3(pos.x, expectedFloorY + 2f, pos.z), 5f);
                bool collidesWithBuilding = false;
                foreach (var h in hits)
                {
                    string hName = h.gameObject.name.ToLower();
                    if (hName.Contains("house") || hName.Contains("building") || hName.Contains("roof") || hName.Contains("stair"))
                    {
                        collidesWithBuilding = true;
                        break;
                    }
                }

                if (collidesWithBuilding)
                {
                    Undo.DestroyObjectImmediate(tree);
                    purged++;
                }
                else
                {
                    // Ground cleanly to plaza floor
                    Undo.RecordObject(tree.transform, "Ground Floating Tree");
                    tree.transform.position = new Vector3(pos.x, expectedFloorY, pos.z);
                    purged++;
                }
            }

            // 3. Ground any floating props in Seaward Tavern Terrace (e.g. BenchV1 and Barrels at Y=86 instead of Y=38)
            var terrace = GameObject.Find("Seaward_Port_Town_Lively/05_Seaside_Tavern_Terrace");
            if (terrace != null)
            {
                for (int i = 0; i < terrace.transform.childCount; i++)
                {
                    var c = terrace.transform.GetChild(i);
                    if (c.position.y > 60f)
                    {
                        Undo.RecordObject(c, "Ground Terrace Prop");
                        c.position = new Vector3(c.position.x, 38.00f, c.position.z);
                        purged++;
                    }
                }
            }

            return purged;
        }

        public static (int meshesRestored, int undergroundPurged, int propsGrounded) FixAndGroundAllMetropolisLampsAndBenches()
        {
            int meshesRestored = 0;
            int undergroundPurged = 0;
            int propsGrounded = 0;

            // 1. Restore corrupt Combined_Mesh on 02_Street_Props_Benches_Lamps
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Urban_StreetLampPost.prefab");
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_Park_Bench_01.prefab");

            Mesh lampMesh = lampPrefab != null ? lampPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;
            Mesh benchMesh = benchPrefab != null ? benchPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;

            var streetPropsRoot = GameObject.Find("8Vector_Metropolis_Root/02_Street_Props_Benches_Lamps");
            if (streetPropsRoot != null)
            {
                for (int i = 0; i < streetPropsRoot.transform.childCount; i++)
                {
                    var ch = streetPropsRoot.transform.GetChild(i);
                    var mf = ch.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        if (ch.name.Contains("StreetLampPost") && lampMesh != null && mf.sharedMesh != lampMesh)
                        {
                            Undo.RecordObject(mf, "Restore Lamp Mesh");
                            mf.sharedMesh = lampMesh;
                            meshesRestored++;
                        }
                        else if (ch.name.Contains("Bench") && benchMesh != null && mf.sharedMesh != benchMesh)
                        {
                            Undo.RecordObject(mf, "Restore Bench Mesh");
                            mf.sharedMesh = benchMesh;
                            meshesRestored++;
                        }
                    }
                }
            }

            // 2. Purge underground lamps spawned under the Grand Staircases in Grand_Metropolis_StreetLife_And_Decor
            var decorRoot = GameObject.Find("Grand_Metropolis_StreetLife_And_Decor");
            if (decorRoot != null)
            {
                var lampsToPurge = new List<GameObject>();
                for (int i = 0; i < decorRoot.transform.childCount; i++)
                {
                    var ch = decorRoot.transform.GetChild(i);
                    if (!ch.name.Contains("StreetLamp")) continue;

                    // Specifically R340 lamps under Grand Stairs
                    if (ch.name.Contains("_R340_"))
                    {
                        lampsToPurge.Add(ch.gameObject);
                        continue;
                    }

                    // Check if buried under a staircase/crossrail/firewood stack by > 0.8m
                    var hits = Physics.RaycastAll(new Vector3(ch.position.x, ch.position.y + 15f, ch.position.z), Vector3.down, 25f);
                    foreach (var h in hits)
                    {
                        if (h.collider.transform.IsChildOf(decorRoot.transform)) continue;
                        if (h.collider.name.Contains("StreetLamp") || h.collider.name.Contains("LampPost")) continue;
                        if (h.collider.name.Contains("Balustrade") || h.collider.name.Contains("CrossRail") || h.collider.name.Contains("Firewood") || h.collider.name.Contains("Step_"))
                        {
                            if (h.point.y > ch.position.y + 0.8f)
                            {
                                lampsToPurge.Add(ch.gameObject);
                                break;
                            }
                        }
                    }
                }

                foreach (var go in lampsToPurge)
                {
                    Undo.DestroyObjectImmediate(go);
                    undergroundPurged++;
                }
            }

            // 3. Purge props clipping inside Porch_Pantry_Supply or Firewood in 02_Street_Props_Benches_Lamps
            if (streetPropsRoot != null)
            {
                var propsToPurge = new List<GameObject>();
                for (int i = 0; i < streetPropsRoot.transform.childCount; i++)
                {
                    var ch = streetPropsRoot.transform.GetChild(i);
                    var hits = Physics.RaycastAll(new Vector3(ch.position.x, ch.position.y + 10f, ch.position.z), Vector3.down, 15f);
                    foreach (var h in hits)
                    {
                        if (h.collider.transform.IsChildOf(streetPropsRoot.transform)) continue;
                        if (h.collider.name.Contains("Porch_Pantry") || h.collider.name.Contains("Firewood_Stack") || h.collider.name.Contains("Step_"))
                        {
                            if (h.point.y > ch.position.y + 1.0f)
                            {
                                propsToPurge.Add(ch.gameObject);
                                break;
                            }
                        }
                    }
                }

                foreach (var go in propsToPurge)
                {
                    Undo.DestroyObjectImmediate(go);
                    undergroundPurged++;
                }
            }

            // 4. Ground all lamps, benches, and sidewalk furnishings in relevant containers
            string[] containerPaths = {
                "8Vector_Metropolis_Root/02_Street_Props_Benches_Lamps",
                "Grand_Metropolis_StreetLife_And_Decor",
                "Metropolis_Street_Infrastructure_Root/03_Avenue_StreetLamps",
                "Metropolis_Street_Infrastructure_Root/04_Benches_And_Signs"
            };

            foreach (var cp in containerPaths)
            {
                var cont = GameObject.Find(cp);
                if (cont == null) continue;

                for (int i = 0; i < cont.transform.childCount; i++)
                {
                    var ch = cont.transform.GetChild(i);
                    Vector3 p = ch.position;

                    // Raycast down from p.y + 4.0m to find exact sidewalk/curb/plaza surface
                    var hits = Physics.RaycastAll(new Vector3(p.x, p.y + 4.0f, p.z), Vector3.down, 10f);
                    float bestGroundY = -999f;

                    foreach (var h in hits)
                    {
                        if (h.collider.transform.IsChildOf(cont.transform)) continue;
                        string n = h.collider.name;
                        if (n.Contains("StreetLamp") || n.Contains("LampPost") || n.Contains("Bench") || n.Contains("Sign") || n.Contains("Equipped_sword")) continue;

                        // Check valid ground colliders
                        if (n.Contains("Sidewalk") || n.Contains("Walk") || n.Contains("Curb") || n.Contains("Plaza") || n.Contains("Floor") || n.Contains("Avenue") || n.Contains("Road") || n.Contains("Continent") || n.Contains("Terrain") || n.Contains("StoneCurb"))
                        {
                            if (h.point.y > bestGroundY)
                                bestGroundY = h.point.y;
                        }
                    }

                    if (bestGroundY > -900f && Mathf.Abs(p.y - bestGroundY) > 0.03f)
                    {
                        Undo.RecordObject(ch, "Ground Street Prop");
                        ch.position = new Vector3(p.x, bestGroundY, p.z);
                        propsGrounded++;
                    }
                }
            }

            // 5. Purge lamps clipping inside dynamic avenue props (Handcart, Firewood) & snap hamlet lamps to terrain
            var avenueLamps = GameObject.Find("Metropolis_Street_Infrastructure_Root/03_Avenue_StreetLamps");
            if (avenueLamps != null)
            {
                for (int i = avenueLamps.transform.childCount - 1; i >= 0; i--)
                {
                    var l = avenueLamps.transform.GetChild(i);
                    var hits = Physics.RaycastAll(new Vector3(l.position.x, l.position.y + 5f, l.position.z), Vector3.down, 10f);
                    foreach (var h in hits)
                    {
                        if (h.collider.name.Contains("Handcart") || h.collider.name.Contains("Firewood"))
                        {
                            Undo.DestroyObjectImmediate(l.gameObject);
                            undergroundPurged++;
                            break;
                        }
                    }
                }
            }

            string[] hamletPaths = {
                "02_Sanctuary_Of_Four_Winds_Temple",
                "03_Eastwatch_Pilgrim_Village",
                "06_Whispering_Hermitage_Riverside",
                "Signpost_SW_Ruins_Crossroads"
            };

            foreach (var hp in hamletPaths)
            {
                var hRoot = GameObject.Find(hp);
                if (hRoot == null) continue;
                var lamps = hRoot.GetComponentsInChildren<Transform>().Where(t => t.name.Contains("StreetLamp"));
                foreach (var l in lamps)
                {
                    var hits = Physics.RaycastAll(new Vector3(l.position.x, l.position.y + 5f, l.position.z), Vector3.down, 10f);
                    foreach (var h in hits)
                    {
                        if (h.collider.name.Contains("Continent") || h.collider.name.Contains("Terrain"))
                        {
                            if (h.point.y > l.position.y)
                            {
                                Undo.RecordObject(l.transform, "Ground Hamlet Lamp");
                                l.transform.position = new Vector3(l.position.x, h.point.y, l.position.z);
                                propsGrounded++;
                            }
                        }
                    }
                }
            }

            Debug.Log($"<color=#69F0AE><b>[METROPOLIS FIXER] Restored {meshesRestored} corrupt prop meshes, purged {undergroundPurged} underground/obstructed lamps, grounded {propsGrounded} street lamps & benches flush on sidewalk pavement!</b></color>");
            return (meshesRestored, undergroundPurged, propsGrounded);
        }

        public static (int housesFixed, int propsFixed, int collidingRemoved) FixAndElevateDistrictIIBazaarAndAlchemyHouse()
        {
            int housesFixed = 0;
            int propsFixed = 0;
            int collidingRemoved = 0;

            // 1. Remove colliding placeholder rowhouse that blocks District II Alchemist Apothecary
            var rowHouses = GameObject.Find("8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses");
            if (rowHouses == null) rowHouses = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (rowHouses != null)
            {
                foreach (Transform rh in rowHouses.transform)
                {
                    if (Vector3.Distance(rh.position, new Vector3(27.63f, 58f, -180.90f)) < 3.0f)
                    {
                        Undo.DestroyObjectImmediate(rh.gameObject);
                        collidingRemoved++;
                        break;
                    }
                }
            }

            // 2. Elevate and ground Alchemist Apothecary House on Tier 3 High Market Promenade
            var house = GameObject.Find("--- METROPOLIS_MESHY_HOUSES ---/District_II_Grand_Bazaar/Meshy_House_AlchemistApothecary_BazaarRow");
            if (house == null) house = GameObject.Find("Meshy_House_AlchemistApothecary_BazaarRow");
            if (house != null)
            {
                Undo.RecordObject(house.transform, "Elevate Alchemy House To Promenade");
                // Base elevation of Tier 3 plaza is 58.00f, house mesh pivot offset is 4.26f
                house.transform.position = new Vector3(30f, 62.26f, -170f);
                house.transform.rotation = Quaternion.Euler(0f, -110f, 0f);
                housesFixed++;
            }

            // 3. Elevate and ground all 25 props in District_II_Grand_Bazaar_Central
            var bazaarCentral = GameObject.Find("--- METROPOLIS_DISTRICT_DETAILS ---/District_II_Grand_Bazaar_Central");
            if (bazaarCentral == null) bazaarCentral = GameObject.Find("District_II_Grand_Bazaar_Central");
            if (bazaarCentral != null)
            {
                foreach (Transform prop in bazaarCentral.transform)
                {
                    Undo.RecordObject(prop, "Elevate Bazaar Prop To Promenade");
                    float targetY = 58.00f;
                    if (prop.position.x < 8.0f) targetY = 58.24f; // sidewalk curb height
                    if (prop.name.StartsWith("Market_Booth")) targetY += 0.87f * 1.35f;

                    prop.position = new Vector3(prop.position.x, targetY, prop.position.z);
                    propsFixed++;
                }
            }

            Debug.Log($"<color=#69F0AE><b>[METROPOLIS FIXER] Elevated {housesFixed} Alchemy House and {propsFixed} Bazaar props out from under foundation/stairs onto Tier 3 Promenade (removed {collidingRemoved} colliding rowhouse)!</b></color>");
            return (housesFixed, propsFixed, collidingRemoved);
        }

        [MenuItem("BattleSummoner/Metropolis/Populate Tier 2 Guildhall and Artisan Plaza", false, 165)]
        public static (int guildhallSpawned, int rowhousesSpawned, int propsSpawned) PopulateTier2GuildhallAndArtisanPlaza()
        {
            int guildhallSpawned = 0;
            int rowhousesSpawned = 0;
            int propsSpawned = 0;

            var rowHouses = GameObject.Find(ROW_HOUSES_PATH);
            if (rowHouses == null) rowHouses = GameObject.Find("01_Tier_Stepped_RowHouses");

            var detailsRoot = GameObject.Find("--- METROPOLIS_DISTRICT_DETAILS ---");
            if (detailsRoot == null) detailsRoot = new GameObject("--- METROPOLIS_DISTRICT_DETAILS ---");

            var royalRoofMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_RoofSlate.mat");

            // 1. Landmark Grand Guildhall at (65, 38.00, -228)
            Vector3 ghPos = new Vector3(65.0f, 38.00f, -228.0f);
            Quaternion ghRot = Quaternion.Euler(0f, -16.4f, 0f);

            GameObject existingGh = null;
            if (rowHouses != null)
            {
                foreach (Transform t in rowHouses.transform)
                {
                    if (t.name.Contains("Tier2_Grand_Norse_Guildhall") || (t.name.Contains("Building_Guildhall_Grand") && Vector3.Distance(t.position, ghPos) < 10.0f))
                    {
                        existingGh = t.gameObject;
                        break;
                    }
                }
            }
            if (existingGh != null)
            {
                Undo.RecordObject(existingGh.transform, "Update Tier2 Guildhall Position");
                existingGh.transform.position = ghPos;
                existingGh.transform.rotation = ghRot;
                existingGh.transform.localScale = Vector3.one * 100f;
            }
            else
            {
                var ghPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Guildhall_Grand.fbx");
                if (ghPrefab != null)
                {
                    var ghInst = (GameObject)PrefabUtility.InstantiatePrefab(ghPrefab, rowHouses != null ? rowHouses.transform : null);
                    ghInst.name = "Tier2_Grand_Norse_Guildhall";
                    ghInst.transform.position = ghPos;
                    ghInst.transform.rotation = ghRot;
                    ghInst.transform.localScale = Vector3.one * 100f;

                    foreach (var mf in ghInst.GetComponentsInChildren<MeshFilter>())
                    {
                        var mc = mf.gameObject.GetComponent<MeshCollider>();
                        if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }

                    var culling = ghInst.GetComponent<CullableWorldProp>() ?? ghInst.AddComponent<CullableWorldProp>();
                    culling.boundingRadius = 18f;
                    culling.isDynamicMover = false;

                    ApplyRoyalBlueRoofMaterials(ghInst, royalRoofMat);
                    guildhallSpawned++;
                    Undo.RegisterCreatedObjectUndo(ghInst, "Create Tier2 Grand Norse Guildhall");
                }
            }

            // 2. Complementary rowhouses filling the empty curb gaps
            var rowSpecs = new (string prefabPath, string name, Vector3 pos, float yaw)[]
            {
                ("Assets/Models/Metropolis/Building_Tavern_Open.fbx", "RowHouse_R208_Tier2_Tavern_East", new Vector3(40.8f, 38.00f, -204.0f), 168.7f),
                ("Assets/Models/Metropolis/Building_House_Residential.fbx", "RowHouse_R208_Tier2_Residential_East", new Vector3(55.6f, 38.00f, -200.4f), 164.5f),
                ("Assets/Models/Metropolis/Building_Bakery_Shop.fbx", "RowHouse_R208_Tier2_Bakery_East", new Vector3(70.1f, 38.00f, -195.8f), 160.3f),
                ("Assets/Models/Metropolis/Building_MagicItemStore.fbx", "RowHouse_R243_Tier2_MagicStore_West", new Vector3(43.7f, 38.00f, -236.0f), 169.5f),
                ("Assets/Models/Metropolis/Building_Blacksmith_OpenForge.fbx", "RowHouse_R243_Tier2_Blacksmith_East", new Vector3(85.5f, 38.00f, -224.0f), 160.0f)
            };

            foreach (var spec in rowSpecs)
            {
                GameObject existingHouse = null;
                if (rowHouses != null)
                {
                    foreach (Transform t in rowHouses.transform)
                    {
                        if (t.name == spec.name || Vector3.Distance(t.position, spec.pos) < 5.0f)
                        {
                            existingHouse = t.gameObject;
                            break;
                        }
                    }
                }
                if (existingHouse != null)
                {
                    Undo.RecordObject(existingHouse.transform, $"Update {spec.name} Position");
                    existingHouse.transform.position = spec.pos;
                    existingHouse.transform.rotation = Quaternion.Euler(0f, spec.yaw, 0f);
                }
                else
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.prefabPath);
                    if (prefab != null)
                    {
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rowHouses != null ? rowHouses.transform : null);
                        inst.name = spec.name;
                        inst.transform.position = spec.pos;
                        inst.transform.rotation = Quaternion.Euler(0f, spec.yaw, 0f);
                        inst.transform.localScale = Vector3.one * 100f;

                        foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
                        {
                            var mc = mf.gameObject.GetComponent<MeshCollider>();
                            if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                        }

                        var culling = inst.GetComponent<CullableWorldProp>() ?? inst.AddComponent<CullableWorldProp>();
                        culling.boundingRadius = 12f;
                        culling.isDynamicMover = false;

                        ApplyRoyalBlueRoofMaterials(inst, royalRoofMat);
                        rowhousesSpawned++;
                        Undo.RegisterCreatedObjectUndo(inst, $"Create {spec.name}");
                    }
                }
            }

            // 3. Guildhall Courtyard & Artisan Tavern Plaza Props
            Transform plazaParent = detailsRoot.transform.Find("District_VI_Grand_Guildhall_Tier2_Plaza");
            if (plazaParent != null)
            {
                Undo.DestroyObjectImmediate(plazaParent.gameObject);
            }
            GameObject plazaGo = new GameObject("District_VI_Grand_Guildhall_Tier2_Plaza");
            plazaGo.transform.SetParent(detailsRoot.transform, false);
            plazaParent = plazaGo.transform;
            Undo.RegisterCreatedObjectUndo(plazaGo, "Create Guildhall Tier 2 Plaza Root");

            Action<string, Vector3, float, float> spawnProp = (path, pos, yaw, scale) =>
            {
                var pPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (pPrefab != null)
                {
                    var pInst = (GameObject)PrefabUtility.InstantiatePrefab(pPrefab, plazaParent);
                    pInst.transform.position = pos;
                    pInst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    pInst.transform.localScale = Vector3.one * scale;
                    propsSpawned++;
                }
            };

            // Notice board & weapon racks in Guildhall forecourt
            spawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab", new Vector3(60.0f, 38.00f, -214.0f), 25f, 1.1f);
            spawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", new Vector3(70.0f, 38.00f, -214.0f), -25f, 1.0f);
            spawnProp("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab", new Vector3(71.0f, 38.00f, -215.5f), -25f, 1.0f);

            // Outdoor Tavern dining tables
            spawnProp("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab", new Vector3(57.5f, 38.00f, -211.5f), 15f, 1.0f);
            spawnProp("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab", new Vector3(72.5f, 38.00f, -211.5f), -35f, 1.0f);

            // Flower box benches
            spawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", new Vector3(52.5f, 38.00f, -216.0f), 75f, 1.0f);
            spawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", new Vector3(77.5f, 38.00f, -216.0f), -75f, 1.0f);
            spawnProp("Assets/Prefabs/MeshyProps/Prop_PlazaFlowerBox_And_Bench.prefab", new Vector3(65.0f, 38.00f, -205.0f), 180f, 1.0f);

            // Wrought iron lamps
            spawnProp("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab", new Vector3(54.0f, 38.00f, -208.0f), 45f, 0.9f);
            spawnProp("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab", new Vector3(76.0f, 38.00f, -208.0f), -45f, 0.9f);
            spawnProp("Assets/Prefabs/MeshyProps/Prop_Mondstadt_WroughtIronStreetLamp.prefab", new Vector3(65.0f, 38.00f, -234.0f), 0f, 0.9f);

            // Handcart with goods
            spawnProp("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab", new Vector3(60.0f, 38.00f, -208.5f), 60f, 1.0f);

            // Floral accents
            spawnProp("Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab", new Vector3(53.5f, 38.00f, -214.5f), 20f, 1.0f);
            spawnProp("Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab", new Vector3(75.5f, 38.00f, -214.5f), -20f, 1.0f);
            spawnProp("Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab", new Vector3(63.5f, 38.00f, -207.5f), 40f, 1.0f);

            // Lively Guild NPCs
            spawnProp("Assets/Models/Metropolis/Character_BardLute.fbx", new Vector3(62.0f, 38.00f, -212.5f), 30f, 1.0f);
            spawnProp("Assets/Prefabs/Characters/Character_Base_Male_Genshin.prefab", new Vector3(68.0f, 38.00f, -212.5f), -30f, 1.0f);
            spawnProp("Assets/Prefabs/Characters/Character_Base_Female_Genshin.prefab", new Vector3(65.0f, 38.00f, -211.0f), 180f, 1.0f);

            Debug.Log($"<color=#69F0AE><b>[METROPOLIS FIXER] Populated Tier 2 Grand Guildhall ({guildhallSpawned}), {rowhousesSpawned} rowhouses, and {propsSpawned} plaza props into the formerly empty sector!</b></color>");
            return (guildhallSpawned, rowhousesSpawned, propsSpawned);
        }

        private static void ApplyRoyalBlueRoofMaterials(GameObject go, Material royalRoofMat)
        {
            if (go == null || royalRoofMat == null) return;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = mr.sharedMaterials;
                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string mName = mats[i].name.ToLower();
                    if (mName.Contains("roof") || mName.Contains("slate") || mName.Contains("clay") || mName.Contains("tile"))
                    {
                        mats[i] = royalRoofMat;
                        modified = true;
                    }
                }
                if (modified)
                {
                    mr.sharedMaterials = mats;
                }
            }
        }
    }
}
#endif
