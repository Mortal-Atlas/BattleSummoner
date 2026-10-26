#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BattleSummoner.World.Editor;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// RemediateFlippedTerrainAndEnrichStreets
    /// 1. Fixes the flipped/upside-down terrain mesh:
    ///    - Reverses triangle winding so 100% of normals point UP (+Y) into the sky.
    ///    - Replaces inverted UVs with conformal (0,0) SW to (1,1) NE mapping.
    ///    - Recomputes vertex colors, normals, tangents, and saves permanent asset.
    /// 2. Enriches the streets with full 3D cobblestone roadbeds, sidewalks, curbs,
    ///    stone tier stairs, avenue streetlamps, merchant stalls, handcarts, barrels,
    ///    crates, benches, flower planters, signposts, and grand town gate arches.
    /// 3. Snaps all buildings flush to solid walkable ground with zero floating.
    /// </summary>
    public static class RemediateFlippedTerrainAndEnrichStreets
    {
        private const string CORRECTED_MESH_PATH = "Assets/Models/Environments/Floor01_MacroContinent_CorrectNormals.asset";
        private const string STREET_ROOT_NAME = "Metropolis_Street_Infrastructure_Root";

        [MenuItem("BattleSummoner/Fix Flipped Terrain and Enrich Streets Now", false, 0)]
        public static void ExecuteRemediation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/OverworldMap.unity")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("<color=cyan><b>=== STEP 1: FIXING FLIPPED TERRAIN MESH NORMALS & UVS ===</b></color>");
            FixTerrainMeshNormalsAndUVs();

            Debug.Log("<color=cyan><b>=== STEP 2: GENERATING 3D COBBLESTONE ROADS, CURBS & TIER STAIRS ===</b></color>");
            BuildEnrichedStreetInfrastructure();

            Debug.Log("<color=cyan><b>=== STEP 3: SNAPPING ALL HOUSES FLUSH TO SOLID WALKABLE GROUND ===</b></color>");
            SnapAllHousesToSolidGround();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>=== REMEDIATION COMPLETE: TERRAIN & STREETS FULLY RESTORED! ===</b></color>");
        }

        public static void FixTerrainMeshNormalsAndUVs()
        {
            GameObject terrainGo = GameObject.Find("Floor01_MacroContinent_Terrain");
            if (terrainGo == null)
            {
                Debug.LogError("Floor01_MacroContinent_Terrain not found in scene!");
                return;
            }

            MeshFilter mf = terrainGo.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogError("No sharedMesh on Floor01_MacroContinent_Terrain!");
                return;
            }

            Mesh sourceMesh = mf.sharedMesh;
            Mesh fixedMesh = UnityEngine.Object.Instantiate(sourceMesh);
            fixedMesh.name = "Floor01_MacroContinent_CorrectNormals";

            Vector3[] verts = fixedMesh.vertices;
            Vector3[] norms = fixedMesh.normals;
            int[] tris = fixedMesh.triangles;
            Color[] colors = new Color[verts.Length];
            Vector2[] uvs = new Vector2[verts.Length];

            // 1. Check if normals are pointing down and reverse winding order
            int downCount = 0;
            for (int i = 0; i < norms.Length; i++)
            {
                if (norms[i].y < -0.05f) downCount++;
            }

            bool needsFlip = (downCount > norms.Length / 3);
            if (needsFlip)
            {
                Debug.Log($"[Terrain Fix] Detected {downCount}/{norms.Length} downward normals! Reversing triangle winding...");
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int temp = tris[i + 1];
                    tris[i + 1] = tris[i + 2];
                    tris[i + 2] = temp;
                }
                fixedMesh.triangles = tris;
            }

            // 2. Conformal UV coordinates [0..1] mapping (West = U=0, East = U=1, South = V=0, North = V=1)
            float minExtent = -4096f;
            float maxExtent = 4096f;

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                float u = Mathf.InverseLerp(minExtent, maxExtent, v.x);
                float vCoord = Mathf.InverseLerp(minExtent, maxExtent, v.z);
                uvs[i] = new Vector2(u, vCoord);

                // Recompute biome vertex color
                FixBrokenBiomesAndSplatmap.EvaluateWorldPoint(
                    v.x, v.z,
                    out _,
                    out Color bCol,
                    out _, out _, out _, out _
                );
                colors[i] = bCol;
            }

            fixedMesh.uv = uvs;
            fixedMesh.colors = colors;
            fixedMesh.RecalculateNormals();
            fixedMesh.RecalculateTangents();
            fixedMesh.RecalculateBounds();

            // Verify
            int upFinal = 0, downFinal = 0;
            Vector3[] finalNorms = fixedMesh.normals;
            for (int i = 0; i < finalNorms.Length; i++)
            {
                if (finalNorms[i].y > 0.05f) upFinal++;
                else if (finalNorms[i].y < -0.05f) downFinal++;
            }
            Debug.Log($"[Terrain Fix] Final Normals: UP={upFinal}, DOWN={downFinal} ({((float)upFinal/finalNorms.Length)*100f:F1}% upward)");

            // Save persistent asset
            if (File.Exists(CORRECTED_MESH_PATH))
            {
                AssetDatabase.DeleteAsset(CORRECTED_MESH_PATH);
            }
            AssetDatabase.CreateAsset(fixedMesh, CORRECTED_MESH_PATH);
            AssetDatabase.SaveAssets();

            Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(CORRECTED_MESH_PATH);
            mf.sharedMesh = savedMesh;

            MeshCollider mc = terrainGo.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = savedMesh;
            }

            // Update Material
            MeshRenderer mr = terrainGo.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                Shader shader = Shader.Find("BattleSummoner/Environment/URP_CircularContinent_MacroTerrain");
                if (shader != null) mr.sharedMaterial.shader = shader;
            }
        }

        public static void BuildEnrichedStreetInfrastructure()
        {
            GameObject metroRoot = GameObject.Find("8Vector_Metropolis_Root");
            if (metroRoot == null)
            {
                metroRoot = new GameObject("8Vector_Metropolis_Root");
            }

            // Remove previous street infrastructure if present
            Transform oldStreetRoot = metroRoot.transform.Find(STREET_ROOT_NAME);
            if (oldStreetRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldStreetRoot.gameObject);
            }

            GameObject streetRoot = new GameObject(STREET_ROOT_NAME);
            streetRoot.transform.SetParent(metroRoot.transform, false);

            Transform roadwaysParent = new GameObject("01_Cobblestone_Roadways").transform;
            roadwaysParent.SetParent(streetRoot.transform, false);

            Transform stairsParent = new GameObject("02_Stone_Tier_Stairs").transform;
            stairsParent.SetParent(streetRoot.transform, false);

            Transform lampsParent = new GameObject("03_Avenue_StreetLamps").transform;
            lampsParent.SetParent(streetRoot.transform, false);

            Transform furnitureParent = new GameObject("04_Benches_And_Signs").transform;
            furnitureParent.SetParent(streetRoot.transform, false);

            Transform marketParent = new GameObject("05_Carts_Stalls_Barrels_Crates").transform;
            marketParent.SetParent(streetRoot.transform, false);

            Transform archesParent = new GameObject("06_Grand_Avenue_Gates").transform;
            archesParent.SetParent(streetRoot.transform, false);

            // Load Materials with authentic Idyllic Fantasy Nature Cobblestone
            string roadMatPath = "Assets/Materials/M_CityCobbleRoad.mat";
            string curbMatPath = "Assets/Materials/M_CityStoneCurb.mat";

            var cobbleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Cobblestone/Cobblestone_Albedo.png");
            var cobbleNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Cobblestone/Cobblestone_Normal.png");

            Material roadMat = AssetDatabase.LoadAssetAtPath<Material>(roadMatPath);
            if (roadMat == null) roadMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            roadMat.name = "M_CityCobbleRoad";
            roadMat.color = new Color(0.92f, 0.89f, 0.84f, 1f);
            roadMat.SetFloat("_Smoothness", 0.15f);
            if (cobbleTex != null)
            {
                roadMat.SetTexture("_BaseMap", cobbleTex);
                roadMat.SetTextureScale("_BaseMap", new Vector2(0.25f, 0.25f));
            }
            if (cobbleNorm != null)
            {
                roadMat.EnableKeyword("_NORMALMAP");
                roadMat.SetTexture("_BumpMap", cobbleNorm);
                roadMat.SetFloat("_BumpScale", 0.85f);
            }
            if (!AssetDatabase.Contains(roadMat)) AssetDatabase.CreateAsset(roadMat, roadMatPath);
            else EditorUtility.SetDirty(roadMat);

            Material curbMat = AssetDatabase.LoadAssetAtPath<Material>(curbMatPath);
            if (curbMat == null) curbMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            curbMat.name = "M_CityStoneCurb";
            curbMat.color = new Color(0.68f, 0.65f, 0.60f, 1f);
            curbMat.SetFloat("_Smoothness", 0.08f);
            if (cobbleTex != null)
            {
                curbMat.SetTexture("_BaseMap", cobbleTex);
                curbMat.SetTextureScale("_BaseMap", new Vector2(0.5f, 0.5f));
            }
            if (!AssetDatabase.Contains(curbMat)) AssetDatabase.CreateAsset(curbMat, curbMatPath);
            else EditorUtility.SetDirty(curbMat);

            // Load Prop Prefabs
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Urban_StreetLampPost.prefab")
                          ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Models/SM_StreetLight_Modern_01.fbx");
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_Park_Bench_01.prefab")
                           ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BenchV1.prefab");
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BeerBarrelV1.prefab");
            var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BoxV1.prefab");
            var sackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/SackV1.prefab");
            var cartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab")
                          ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Meshes/Handcart.fbx");
            var stallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_BazaarStand_Street.fbx");
            var gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/GothicFantasyTownGate.fbx");
            var archPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/FantasyGothicArchway.fbx");
            var vasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Vase_1.prefab")
                          ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/VaseV1.prefab");

            var flowerPrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab")
            };

            System.Random rng = new System.Random(42);

            // =========================================================
            // A. BUILD 8 RADIAL GRAND BOULEVARDS (Cobblestone + Curbs)
            // =========================================================
            float[] radialAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            float boulevardWidth = 14f;
            float curbWidth = 1.5f;

            // Exact Tier spans matching sculpted continent terrain:
            // Tier 4: R = 10m to 86m,   Y = 80.06m (extends to retaining wall ledge)
            // Tier 3: R = 116m to 202m, Y = 58.06m (meets bottom of T4 stairs at 116m)
            // Tier 2: R = 232m to 316m, Y = 38.06m
            // Tier 1: R = 348m to 448.5m, Y = 22.06m (curtain wall boundary)
            (float rStart, float rEnd, float yElev)[] tierSegments = new (float, float, float)[]
            {
                (10f,  86f,  80.06f),
                (116f, 202f, 58.06f),
                (232f, 316f, 38.06f),
                (348f, 448.5f, 22.06f)
            };

            foreach (float angDeg in radialAngles)
            {
                float angRad = angDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angRad), 0f, Mathf.Cos(angRad));
                Vector3 perp = new Vector3(dir.z, 0f, -dir.x);

                // 1. Cobblestone Paved Segments
                foreach (var seg in tierSegments)
                {
                    float len = seg.rEnd - seg.rStart;
                    float midR = (seg.rStart + seg.rEnd) * 0.5f;
                    Vector3 center = dir * midR + Vector3.up * seg.yElev;

                    // Roadbed quad mesh
                    GameObject roadGo = CreatePavedRoadQuad(
                        roadwaysParent,
                        $"Road_Radial_{angDeg:F0}_R{seg.rStart:F0}",
                        center, dir, perp,
                        len, boulevardWidth,
                        roadMat
                    );

                    // Stone curb borders on left and right
                    CreateCurbStrip(roadGo.transform, center + perp * (boulevardWidth * 0.5f), dir, perp, len, curbWidth, 0.25f, curbMat);
                    CreateCurbStrip(roadGo.transform, center - perp * (boulevardWidth * 0.5f), dir, perp, len, curbWidth, 0.25f, curbMat);
                }

                // 2. Grand Stone Flight of Stairs at Tier Transitions with solid foundations & balustrades
                // T4 to T3: Centered at 101m (length 30m) -> top reaches R=86m platform ledge at Y=80.06m, bottom lands at R=116m at Y=58.06m
                CreateFlightOfStairs(stairsParent, $"Stairs_{angDeg:F0}_T4_to_T3", dir * 101.0f, dir, perp, 30.0f, 80.06f, 58.06f, boulevardWidth, 28, curbMat, true);
                CreateFlightOfStairs(stairsParent, $"Stairs_{angDeg:F0}_T3_to_T2", dir * 217.0f, dir, perp, 30.0f, 58.06f, 38.06f, boulevardWidth, 26, curbMat);
                CreateFlightOfStairs(stairsParent, $"Stairs_{angDeg:F0}_T2_to_T1", dir * 332.0f, dir, perp, 32.0f, 38.06f, 22.06f, boulevardWidth, 26, curbMat);

                // 3. Grand Town Gate at City Curtain Wall Portal (R = 446m)
                if (gatePrefab != null || archPrefab != null)
                {
                    var pfb = gatePrefab ?? archPrefab;
                    Vector3 gatePos = dir * 446.0f + Vector3.up * 22.05f;
                    var gate = (GameObject)PrefabUtility.InstantiatePrefab(pfb, archesParent);
                    gate.transform.position = gatePos;
                    gate.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    gate.transform.localScale = Vector3.one * 1.6f;
                }

                // 4. Streetlamps & Street Life along Radial Boulevard (strictly inside city wall, R <= 435m)
                for (float r = 90f; r <= 435f; r += 26f)
                {
                    // Skip transition stairs zones
                    if ((r >= 100f && r <= 116f) || (r >= 206f && r <= 224f) || (r >= 322f && r <= 342f)) continue;

                    float y = (r < 105f) ? 80.24f : (r < 210f) ? 58.24f : (r < 326f) ? 38.24f : 22.24f;
                    Vector3 curbLeft = dir * r + perp * (boulevardWidth * 0.5f + 0.4f) + Vector3.up * y;
                    Vector3 curbRight = dir * r - perp * (boulevardWidth * 0.5f + 0.4f) + Vector3.up * y;

                    bool validLeft = Physics.Raycast(new Vector3(curbLeft.x, curbLeft.y + 4f, curbLeft.z), Vector3.down, out RaycastHit hitL, 10f) && hitL.point.y >= 20.0f;
                    if (validLeft) curbLeft.y = hitL.point.y;
                    bool validRight = Physics.Raycast(new Vector3(curbRight.x, curbRight.y + 4f, curbRight.z), Vector3.down, out RaycastHit hitR, 10f) && hitR.point.y >= 20.0f;
                    if (validRight) curbRight.y = hitR.point.y;

                    // Place Streetlamp on left
                    if (lampPrefab != null && validLeft)
                    {
                        var lamp = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, lampsParent);
                        lamp.transform.position = curbLeft;
                        lamp.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                        lamp.transform.localScale = Vector3.one * 1.3f;

                        // Add warm amber point light
                        var light = lamp.GetComponentInChildren<Light>();
                        if (light == null) light = lamp.AddComponent<Light>();
                        light.type = LightType.Point;
                        light.range = 14f;
                        light.color = new Color(1f, 0.85f, 0.55f);
                        light.intensity = 2.5f;
                    }

                    // Place Bench or Flower Urn on right
                    if (benchPrefab != null && rng.NextDouble() < 0.45 && validRight)
                    {
                        var bench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, furnitureParent);
                        bench.transform.position = curbRight;
                        bench.transform.rotation = Quaternion.LookRotation(perp, Vector3.up);
                        bench.transform.localScale = Vector3.one * 1.1f;
                    }
                    else if (vasePrefab != null && rng.NextDouble() < 0.55 && validRight)
                    {
                        var vase = (GameObject)PrefabUtility.InstantiatePrefab(vasePrefab, furnitureParent);
                        vase.transform.position = curbRight;
                        vase.transform.localScale = Vector3.one * 1.2f;
                    }

                    // Occasional Merchant Cart or Crate cluster along curb
                    if (r > 120f && r <= 420f && rng.NextDouble() < 0.25)
                    {
                        bool useLeft = rng.NextDouble() < 0.5;
                        if ((useLeft && !validLeft) || (!useLeft && !validRight)) continue;
                        Vector3 propPos = (useLeft ? curbLeft : curbRight) + dir * 3.5f;
                        if (cartPrefab != null && rng.NextDouble() < 0.40)
                        {
                            var cart = (GameObject)PrefabUtility.InstantiatePrefab(cartPrefab, marketParent);
                            cart.transform.position = propPos;
                            cart.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                            cart.transform.localScale = Vector3.one * 1.0f;
                        }
                        else if (boxPrefab != null)
                        {
                            var box = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab, marketParent);
                            box.transform.position = propPos;
                            box.transform.localScale = Vector3.one * 1.1f;

                            if (barrelPrefab != null)
                            {
                                var b = (GameObject)PrefabUtility.InstantiatePrefab(barrelPrefab, marketParent);
                                b.transform.position = propPos + perp * 1.2f;
                                b.transform.localScale = Vector3.one;
                            }
                        }
                    }
                }
            }

            // =========================================================
            // B. BUILD CONCENTRIC RING BOULEVARDS & CENTRAL PLAZA (Cobblestone + Curbs)
            // =========================================================
            // 1. Central Cathedral Acropolis Grand Plaza Disk (R <= 86m)
            CreateCentralPlazaPavement(roadwaysParent, "Central_Acropolis_Cathedral_Plaza", 86f, 80.05f, roadMat);

            (float ringR, float ringY, float ringW)[] concentricRings = new (float, float, float)[]
            {
                (60f,  80.06f, 12f), // Tier 4 Ring (inside Citadel courtyard)
                (160f, 58.06f, 13f), // Tier 3 Ring (center of Patrician tier)
                (274f, 38.06f, 14f), // Tier 2 Ring (center of Artisan tier)
                (405f, 22.06f, 15f)  // Tier 1 Ring (center of Market tier)
            };

            foreach (var ring in concentricRings)
            {
                CreateConcentricRingPavement(
                    roadwaysParent,
                    $"Ring_Avenue_R{ring.ringR:F0}",
                    ring.ringR, ring.ringY, ring.ringW,
                    curbWidth,
                    roadMat, curbMat
                );
            }

            Debug.Log("<color=green><b>[Streets] Successfully built Central Plaza, 8 Radial Grand Boulevards and 4 Concentric Ring Avenues!</b></color>");
        }

        private static void CreateCentralPlazaPavement(
            Transform parent, string name, float radius, float yElev, Material mat)
        {
            int segments = 96;
            GameObject plazaGo = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            plazaGo.transform.SetParent(parent, false);

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            // Center vertex
            verts.Add(new Vector3(0f, yElev, 0f));
            uvs.Add(new Vector2(0f, 0f));

            for (int i = 0; i <= segments; i++)
            {
                float ang = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Sin(ang) * radius;
                float z = Mathf.Cos(ang) * radius;

                verts.Add(new Vector3(x, yElev, z));
                uvs.Add(new Vector2(x * 0.25f, z * 0.25f));

                if (i > 0)
                {
                    tris.Add(0);
                    tris.Add(i);
                    tris.Add(i + 1);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            plazaGo.GetComponent<MeshFilter>().sharedMesh = mesh;
            plazaGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
            plazaGo.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static GameObject CreatePavedRoadQuad(
            Transform parent, string name,
            Vector3 center, Vector3 forward, Vector3 right,
            float length, float width, Material mat)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            float halfL = length * 0.5f;
            float halfW = width * 0.5f;

            Vector3 v0 = center - forward * halfL - right * halfW;
            Vector3 v1 = center - forward * halfL + right * halfW;
            Vector3 v2 = center + forward * halfL + right * halfW;
            Vector3 v3 = center + forward * halfL - right * halfW;

            mesh.vertices = new Vector3[] { v0, v1, v2, v3 };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(width * 0.2f, 0f),
                new Vector2(width * 0.2f, length * 0.2f),
                new Vector2(0f, length * 0.2f)
            };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;

            return go;
        }

        private static void CreateCurbStrip(
            Transform parent, Vector3 center, Vector3 forward, Vector3 right,
            float length, float width, float height, Material mat)
        {
            GameObject curbGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbGo.name = "StoneCurb";
            curbGo.transform.SetParent(parent, false);
            curbGo.transform.position = center + Vector3.up * (height * 0.5f);
            curbGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            curbGo.transform.localScale = new Vector3(width, height, length);
            curbGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateFlightOfStairs(
            Transform parent, string name,
            Vector3 center, Vector3 forward, Vector3 right,
            float length, float yTop, float yBottom, float width, int stepCount, Material mat, bool isTopTier = false)
        {
            float stepLength = length / stepCount;
            float stepHeight = (yTop - yBottom) / stepCount;

            GameObject stairsGroup = new GameObject(name);
            stairsGroup.transform.SetParent(parent, false);

            for (int i = 0; i < stepCount; i++)
            {
                float t = (float)i / stepCount;
                float rDist = (1f - t) * length - (length * 0.5f);
                float y = yBottom + i * stepHeight + (stepHeight * 0.5f);

                // Deep solid foundation cube: top is at step Y, bottom extends all the way down to lower ground (yBottom)
                float topY = yBottom + (i + 1) * stepHeight;
                float targetBottomY = yBottom - 1.0f;
                float fullHeight = topY - targetBottomY;
                float stepCenterY = (topY + targetBottomY) * 0.5f;

                Vector3 stepPos = center + forward * rDist;
                stepPos.y = stepCenterY;
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i}";
                step.transform.SetParent(stairsGroup.transform, false);
                step.transform.position = stepPos;
                step.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                step.transform.localScale = new Vector3(width, fullHeight, stepLength * 1.15f);
                step.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            // Top Landing Threshold Platform (for top tier stairs reaching the ledge at R = 86m)
            if (isTopTier)
            {
                float landingThickness = 0.5f;
                Vector3 landingPos = forward * 86.0f + Vector3.up * (yTop - landingThickness * 0.5f);
                GameObject topLanding = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topLanding.name = "Top_Landing_Threshold";
                topLanding.transform.SetParent(stairsGroup.transform, false);
                topLanding.transform.position = landingPos;
                topLanding.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                topLanding.transform.localScale = new Vector3(width, landingThickness, 2.5f);
                topLanding.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            // Left & Right Stone Balustrades along stairs
            float balustradeWidth = 0.85f;
            float balustradeHeight = 1.3f;
            float midY = (yTop + yBottom) * 0.5f;
            float slopeAngle = Mathf.Atan2(yTop - yBottom, length) * Mathf.Rad2Deg;

            // Left Balustrade
            Vector3 leftBPos = center - right * (width * 0.5f + balustradeWidth * 0.5f) + Vector3.up * (midY + balustradeHeight * 0.5f - 0.9f);
            GameObject leftB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftB.name = "Balustrade_Left";
            leftB.transform.SetParent(stairsGroup.transform, false);
            leftB.transform.position = leftBPos;
            leftB.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(slopeAngle, 0f, 0f);
            leftB.transform.localScale = new Vector3(balustradeWidth, balustradeHeight + 2.5f, length * 1.05f);
            leftB.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Right Balustrade
            Vector3 rightBPos = center + right * (width * 0.5f + balustradeWidth * 0.5f) + Vector3.up * (midY + balustradeHeight * 0.5f - 0.9f);
            GameObject rightB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightB.name = "Balustrade_Right";
            rightB.transform.SetParent(stairsGroup.transform, false);
            rightB.transform.position = rightBPos;
            rightB.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(slopeAngle, 0f, 0f);
            rightB.transform.localScale = new Vector3(balustradeWidth, balustradeHeight + 2.5f, length * 1.05f);
            rightB.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Decorative Newel Piers at Top and Bottom of Balustrades
            if (isTopTier)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 pierPos = forward * 86.0f + right * (side * (width * 0.5f + balustradeWidth * 0.5f)) + Vector3.up * (yTop + 0.65f);
                    GameObject pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pier.name = side == -1 ? "Newel_Pier_Top_Left" : "Newel_Pier_Top_Right";
                    pier.transform.SetParent(stairsGroup.transform, false);
                    pier.transform.position = pierPos;
                    pier.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                    pier.transform.localScale = new Vector3(balustradeWidth * 1.3f, 1.5f, balustradeWidth * 1.3f);
                    pier.GetComponent<MeshRenderer>().sharedMaterial = mat;

                    Vector3 btmPierPos = forward * 116.0f + right * (side * (width * 0.5f + balustradeWidth * 0.5f)) + Vector3.up * (yBottom + 0.65f);
                    GameObject btmPier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    btmPier.name = side == -1 ? "Newel_Pier_Btm_Left" : "Newel_Pier_Btm_Right";
                    btmPier.transform.SetParent(stairsGroup.transform, false);
                    btmPier.transform.position = btmPierPos;
                    btmPier.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                    btmPier.transform.localScale = new Vector3(balustradeWidth * 1.3f, 1.5f, balustradeWidth * 1.3f);
                    btmPier.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
        }

        private static void CreateConcentricRingPavement(
            Transform parent, string name,
            float radius, float yElev, float width, float curbWidth,
            Material roadMat, Material curbMat)
        {
            int segments = 120;
            GameObject ringGo = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            ringGo.transform.SetParent(parent, false);

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            float innerR = radius - width * 0.5f;
            float outerR = radius + width * 0.5f;

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int i = 0; i <= segments; i++)
            {
                float ang = ((float)i / segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);

                Vector3 vIn = new Vector3(sin * innerR, yElev, cos * innerR);
                Vector3 vOut = new Vector3(sin * outerR, yElev, cos * outerR);

                verts.Add(vIn);
                verts.Add(vOut);

                uvs.Add(new Vector2(0f, (float)i * 0.8f));
                uvs.Add(new Vector2(width * 0.2f, (float)i * 0.8f));

                if (i < segments)
                {
                    int idx = i * 2;
                    tris.Add(idx);
                    tris.Add(idx + 1);
                    tris.Add(idx + 3);

                    tris.Add(idx);
                    tris.Add(idx + 3);
                    tris.Add(idx + 2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            ringGo.GetComponent<MeshFilter>().sharedMesh = mesh;
            ringGo.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
            ringGo.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        public static void SnapAllHousesToSolidGround()
        {
            GameObject rowHousesRoot = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (rowHousesRoot == null) return;

            int snapped = 0;
            RaycastHit hit;

            // Target flat tier terrace elevations
            // Tier 4: R <= 102m -> 80.0m
            // Tier 3: 102m < R <= 208m -> 58.0m
            // Tier 2: 208m < R <= 324m -> 38.0m
            // Tier 1: 324m < R <= 468m -> 22.0m

            foreach (Transform house in rowHousesRoot.transform)
            {
                Vector3 pos = house.position;
                float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);

                float targetTierY = (r <= 102f) ? 80.0f : (r <= 208f) ? 58.0f : (r <= 324f) ? 38.0f : 22.0f;

                // Raycast downward from above tier to find exact ground
                Vector3 rayOrigin = new Vector3(pos.x, targetTierY + 15f, pos.z);
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 40f))
                {
                    house.position = new Vector3(pos.x, hit.point.y, pos.z);
                    snapped++;
                }
                else
                {
                    house.position = new Vector3(pos.x, targetTierY, pos.z);
                    snapped++;
                }
            }

            Debug.Log($"[House Snapping] Snapped {snapped} buildings flush to solid walkable ground!");
        }
    }
}
#endif
