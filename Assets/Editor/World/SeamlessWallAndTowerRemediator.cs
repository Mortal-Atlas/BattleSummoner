#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.WorldGen
{
    public static class SeamlessWallAndTowerRemediator
    {
        public const float TIER1_RADIUS = 446.0f;
        public const float TIER2_RADIUS = 310.0f;
        public const float TIER3_RADIUS = 195.0f;
        public const float TIER4_RADIUS = 95.0f;

        public const float TIER1_HEIGHT = 22.0f;
        public const float TIER2_HEIGHT = 38.0f;
        public const float TIER3_HEIGHT = 58.0f;
        public const float TIER4_HEIGHT = 80.0f;

        public const float WATER_LEVEL = 18.0f;
        public const float WALL_BASE_Y = 0.0f;

        public const int ANGULAR_SEGMENTS = 288; // Silky smooth circular curvature (1.25 deg per segment)

        [MenuItem("BattleSummoner/World/Remediate Seamless Walls, Floors and High-Fidelity Towers", false, 15)]
        public static void RemediateAllWallsAndFloors()
        {
            Debug.Log("<color=#00E5FF><b>[SeamlessRemediator] Starting full overhaul of city walls, floors, and towers...</b></color>");

            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            if (stoneMat == null) stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Metro_StoneBase.mat");
            if (stoneMat == null) stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_WallStone.mat");

            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/Mat_City_PavedLimestone.mat");
            if (floorMat == null) floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AnimeCityStylized/MAT_AnimeCity_CobbleLimestone.mat");
            if (floorMat == null) floorMat = stoneMat;

            Material roofMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_RoofSlate.mat");
            if (roofMat == null) roofMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_RoofSlate.mat");

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_TimberWood.mat");

            // 1. Build High-Resolution, Seamless Multi-Tier Masonry Foundation & Floors
            BuildSeamlessCityFoundationsAndFloors(stoneMat, floorMat);

            // 2. Build High-Resolution, Curving Fortress Curtain Wall (288 segments with crenellations & open gates)
            BuildSeamlessFortressCurtainWall(stoneMat);

            // 3. Build High-Fidelity Architectural Wall Towers
            BuildHighFidelityWallTowers(stoneMat, roofMat, woodMat);

            // 4. Update Grand Avenue Gates to match stone masonry
            var gates = GameObject.Find("8Vector_Metropolis_Root/Metropolis_Street_Infrastructure_Root/06_Grand_Avenue_Gates");
            if (gates != null)
            {
                foreach (var mr in gates.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.sharedMaterial = stoneMat;
                }
            }

            // Mark Scene Dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("<color=green><b>[SeamlessRemediator] Completed! City walls, floors, and towers are now 100% seamless and high-fidelity.</b></color>");
        }

        // =========================================================================
        // 1. SEAMLESS FOUNDATION DRUMS & QUAD-GRID FLOORS
        // =========================================================================
        public static void BuildSeamlessCityFoundationsAndFloors(Material stoneMat, Material floorMat)
        {
            // Remove crude low-poly primitive cylinder hierarchies
            GameObject oldStructural = GameObject.Find("City_Structural_Masonry");
            if (oldStructural != null) Undo.DestroyObjectImmediate(oldStructural);

            GameObject oldCrisp = GameObject.Find("Kingdom_CrispMasonryTiers");
            if (oldCrisp != null) Undo.DestroyObjectImmediate(oldCrisp);

            GameObject root = new GameObject("City_Structural_Masonry");
            Undo.RegisterCreatedObjectUndo(root, "Create Seamless City Structural Masonry");

            // Generate seamless vertical retaining walls (smooth cylindrical normals) & quad planar floors
            // Tier 1 (Outer Ring): Drops from Y = 22m down to Y = 0m (water is at 18m)
            CreateRetainingCylinderWall(root.transform, "City_Tier1_OuterSeawall", 448.5f, WALL_BASE_Y, TIER1_HEIGHT, stoneMat, 48f);
            CreatePlanarFloorRing(root.transform, "City_Tier1_FloorPlaza", TIER2_RADIUS, 448.5f, TIER1_HEIGHT, floorMat, 6, 0.18f);

            // Tier 2 (Mid Ring): Drops from Y = 38m down to Y = 22m
            CreateRetainingCylinderWall(root.transform, "City_Tier2_RetainingWall", TIER2_RADIUS, TIER1_HEIGHT, TIER2_HEIGHT, stoneMat, 32f);
            CreatePlanarFloorRing(root.transform, "City_Tier2_FloorPlaza", TIER3_RADIUS, TIER2_RADIUS, TIER2_HEIGHT, floorMat, 5, 0.18f);

            // Tier 3 (Upper Ring): Drops from Y = 58m down to Y = 38m
            CreateRetainingCylinderWall(root.transform, "City_Tier3_RetainingWall", TIER3_RADIUS, TIER2_HEIGHT, TIER3_HEIGHT, stoneMat, 24f);
            CreatePlanarFloorRing(root.transform, "City_Tier3_FloorPlaza", TIER4_RADIUS, TIER3_RADIUS, TIER3_HEIGHT, floorMat, 4, 0.18f);

            // Tier 4 (Citadel Plateau): Drops from Y = 80m down to Y = 58m
            CreateRetainingCylinderWall(root.transform, "City_Tier4_RetainingWall", TIER4_RADIUS, TIER3_HEIGHT, TIER4_HEIGHT, stoneMat, 16f);
            CreatePlanarFloorRing(root.transform, "City_Tier4_CitadelFloor", 0.0f, TIER4_RADIUS, TIER4_HEIGHT, floorMat, 4, 0.18f);
        }

        private static void CreateRetainingCylinderWall(Transform parent, string name, float radius, float bottomY, float topY, Material mat, float uTile)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = name + "_SmoothMesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float deltaAngle = 360f / ANGULAR_SEGMENTS;
            float height = topY - bottomY;

            for (int i = 0; i <= ANGULAR_SEGMENTS; i++)
            {
                float ang = i * deltaAngle;
                float rad = Mathf.Deg2Rad * ang;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                Vector3 n = new Vector3(cos, 0f, sin); // Perfect smooth outward cylindrical normal

                Vector3 vBottom = new Vector3(cos * radius, bottomY, sin * radius);
                Vector3 vTop = new Vector3(cos * radius, topY, sin * radius);

                verts.Add(vBottom);
                norms.Add(n);
                uvs.Add(new Vector2((float)i / ANGULAR_SEGMENTS * uTile, 0f));

                verts.Add(vTop);
                norms.Add(n);
                uvs.Add(new Vector2((float)i / ANGULAR_SEGMENTS * uTile, height * 0.25f));
            }

            for (int i = 0; i < ANGULAR_SEGMENTS; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = (i + 1) * 2;
                int t1 = b1 + 1;

                // Outward
                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);

                // Inward
                tris.Add(b0); tris.Add(t1); tris.Add(t0);
                tris.Add(b0); tris.Add(b1); tris.Add(t1);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void CreatePlanarFloorRing(Transform parent, string name, float innerR, float outerR, float yLevel, Material mat, int radialBands, float uvScale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = name + "_SmoothMesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float deltaAngle = 360f / ANGULAR_SEGMENTS;
            int numRings = radialBands + 1;

            for (int r = 0; r < numRings; r++)
            {
                float t = (float)r / radialBands;
                float currentR = Mathf.Lerp(innerR, outerR, t);

                for (int i = 0; i <= ANGULAR_SEGMENTS; i++)
                {
                    float ang = i * deltaAngle;
                    float rad = Mathf.Deg2Rad * ang;
                    float x = Mathf.Cos(rad) * currentR;
                    float z = Mathf.Sin(rad) * currentR;

                    Vector3 pos = new Vector3(x, yLevel, z);
                    verts.Add(pos);
                    norms.Add(Vector3.up); // Strictly UP: completely eliminates diagonal or faceted shading!
                    uvs.Add(new Vector2(x * uvScale, z * uvScale)); // Continuous planar UV mapping
                }
            }

            int stride = ANGULAR_SEGMENTS + 1;
            for (int r = 0; r < radialBands; r++)
            {
                for (int i = 0; i < ANGULAR_SEGMENTS; i++)
                {
                    int i00 = r * stride + i;
                    int i01 = i00 + 1;
                    int i10 = (r + 1) * stride + i;
                    int i11 = i10 + 1;

                    // Upward (+Y)
                    tris.Add(i00); tris.Add(i10); tris.Add(i11);
                    tris.Add(i00); tris.Add(i11); tris.Add(i01);

                    // Downward (-Y)
                    tris.Add(i00); tris.Add(i11); tris.Add(i10);
                    tris.Add(i00); tris.Add(i01); tris.Add(i11);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        // =========================================================================
        // 2. SEAMLESS CURVING FORTRESS CURTAIN WALL WITH BATTLEMENTS & OPEN GATES
        // =========================================================================
        public static void BuildSeamlessFortressCurtainWall(Material stoneMat)
        {
            GameObject oldWall = GameObject.Find("City_Perimeter_16Towers");
            if (oldWall != null) Undo.DestroyObjectImmediate(oldWall);

            GameObject wallRoot = new GameObject("City_Perimeter_16Towers");
            Undo.RegisterCreatedObjectUndo(wallRoot, "Create Seamless Fortress Curtain Wall");

            float wallRadius = TIER1_RADIUS; // 446m
            float wallThickness = 5.0f;
            float wallBottomY = TIER1_HEIGHT; // 22m
            float wallWalkY = TIER1_HEIGHT + 6.0f; // 28.0m (walkway)
            float parapetOuterY = TIER1_HEIGHT + 7.5f; // 29.5m (outer battlements)
            float parapetInnerY = TIER1_HEIGHT + 6.8f; // 28.8m (inner safety rail)

            float rInner = wallRadius - wallThickness * 0.5f; // 443.5m
            float rOuter = wallRadius + wallThickness * 0.5f; // 448.5m
            float rWalkIn = rInner + 0.6f;
            float rWalkOut = rOuter - 0.8f;

            // 8 Gates at 0, 45, 90, 135, 180, 225, 270, 315 deg
            float[] gateAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            float gateHalfWidthAngle = (20.0f * 0.5f / wallRadius) * Mathf.Rad2Deg; // ~1.28 deg opening

            GameObject curtainMeshObj = new GameObject("Fortress_CurtainWall_CurvedRibbon");
            curtainMeshObj.transform.SetParent(wallRoot.transform, false);

            Mesh mesh = new Mesh { name = "CurtainWall_SmoothCurved_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float deltaAngle = 360f / ANGULAR_SEGMENTS;

            // Profile vertices per angular step (8 points along cross-section):
            // P0: (rInner, wallBottomY)   - Inner wall base
            // P1: (rInner, parapetInnerY) - Inner parapet top
            // P2: (rWalkIn, parapetInnerY)- Inner parapet coping
            // P3: (rWalkIn, wallWalkY)    - Inner walkway edge
            // P4: (rWalkOut, wallWalkY)   - Outer walkway edge
            // P5: (rWalkOut, crenelY)     - Outer parapet inner top
            // P6: (rOuter, crenelY)       - Outer parapet top coping
            // P7: (rOuter, wallBottomY)   - Outer seawall top junction

            for (int i = 0; i <= ANGULAR_SEGMENTS; i++)
            {
                float ang = i * deltaAngle;
                float rad = Mathf.Deg2Rad * ang;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                Vector3 radDir = new Vector3(cos, 0f, sin);
                Vector3 inDir = -radDir;
                Vector3 upDir = Vector3.up;

                // Crenellation pattern on outer parapet (alternating merlons and embrasures)
                bool isMerlon = (i % 4 < 2);
                float crenelY = isMerlon ? parapetOuterY : (wallWalkY + 0.6f);

                Vector3 p0 = new Vector3(cos * rInner, wallBottomY, sin * rInner);
                Vector3 p1 = new Vector3(cos * rInner, parapetInnerY, sin * rInner);
                Vector3 p2 = new Vector3(cos * rWalkIn, parapetInnerY, sin * rWalkIn);
                Vector3 p3 = new Vector3(cos * rWalkIn, wallWalkY, sin * rWalkIn);
                Vector3 p4 = new Vector3(cos * rWalkOut, wallWalkY, sin * rWalkOut);
                Vector3 p5 = new Vector3(cos * rWalkOut, crenelY, sin * rWalkOut);
                Vector3 p6 = new Vector3(cos * rOuter, crenelY, sin * rOuter);
                // Outer wall drops down into the water (water is at 18m, drop to 14m)
                Vector3 p7 = new Vector3(cos * rOuter, 14.0f, sin * rOuter);

                float u = (float)i / ANGULAR_SEGMENTS * 48f;

                verts.Add(p0); norms.Add(inDir); uvs.Add(new Vector2(u, 0f));
                verts.Add(p1); norms.Add(inDir); uvs.Add(new Vector2(u, 1.5f));
                verts.Add(p2); norms.Add(upDir); uvs.Add(new Vector2(u, 0.2f));
                verts.Add(p3); norms.Add(upDir); uvs.Add(new Vector2(u, 0f));
                verts.Add(p4); norms.Add(upDir); uvs.Add(new Vector2(u, 1f));
                verts.Add(p5); norms.Add(inDir); uvs.Add(new Vector2(u, 0.4f));
                verts.Add(p6); norms.Add(upDir); uvs.Add(new Vector2(u, 0.3f));
                verts.Add(p7); norms.Add(radDir); uvs.Add(new Vector2(u, 0f));
            }

            int pCount = 8;

            // Helper to add double-sided quad between profiles i and i+1
            void AddQuad(int i, int pA, int pB)
            {
                int cA = i * pCount + pA;
                int cB = i * pCount + pB;
                int nA = (i + 1) * pCount + pA;
                int nB = (i + 1) * pCount + pB;

                // Side 1
                tris.Add(cA); tris.Add(nA); tris.Add(nB);
                tris.Add(cA); tris.Add(nB); tris.Add(cB);

                // Side 2 (Reverse winding for 100% visibility from any angle)
                tris.Add(cA); tris.Add(nB); tris.Add(nA);
                tris.Add(cA); tris.Add(cB); tris.Add(nB);
            }

            for (int i = 0; i < ANGULAR_SEGMENTS; i++)
            {
                float midAng = (i + 0.5f) * deltaAngle;

                // Check gate opening
                bool isGateOpening = false;
                foreach (var ga in gateAngles)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(midAng, ga)) < gateHalfWidthAngle)
                    {
                        isGateOpening = true;
                        break;
                    }
                }
                if (isGateOpening) continue; // Keep avenue portals open!

                // Inner wall vertical face (P0 to P1)
                AddQuad(i, 0, 1);
                // Inner parapet top coping (P1 to P2)
                AddQuad(i, 1, 2);
                // Inner parapet walkway-drop (P2 to P3)
                AddQuad(i, 2, 3);
                // Stone Walkway deck (P3 to P4)
                AddQuad(i, 3, 4);
                // Outer parapet inner face (P4 to P5)
                AddQuad(i, 4, 5);
                // Outer parapet top coping (P5 to P6)
                AddQuad(i, 5, 6);
                // Outer wall vertical face (P6 to P7)
                AddQuad(i, 6, 7);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = curtainMeshObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = curtainMeshObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = stoneMat;
            var col = curtainMeshObj.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        // =========================================================================
        // 3. HIGH-FIDELITY ARCHITECTURAL FORTRESS TOWERS
        // =========================================================================
        public static void BuildHighFidelityWallTowers(Material stoneMat, Material roofMat, Material woodMat)
        {
            GameObject wallRoot = GameObject.Find("City_Perimeter_16Towers");
            if (wallRoot == null) wallRoot = new GameObject("City_Perimeter_16Towers");

            // Look for any newly generated Meshy model specifically named StylizedFortressTower
            GameObject meshyTowerPrefab = null;
            string[] guids = AssetDatabase.FindAssets("StylizedFortressTower t:Model", new[] { "Assets/Models/Metropolis" });
            if (guids.Length > 0)
            {
                meshyTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            float wallRadius = TIER1_RADIUS; // 446m
            float baseFloorY = TIER1_HEIGHT; // 22m

            // We place 16 towers:
            // 8 Gatehouse Bastions (paired flanking gates) + 8 Midpoint Curtain Watchtowers
            float[] allTowerAngles = new float[16];
            for (int i = 0; i < 16; i++)
            {
                allTowerAngles[i] = i * 22.5f;
            }

            for (int i = 0; i < 16; i++)
            {
                float ang = allTowerAngles[i];
                bool isGateAxis = (i % 2 == 0); // 0, 45, 90, ...

                // If on a gate axis, offset tower slightly to flank the portal so the road stays open
                float finalAng = isGateAxis ? (ang + 2.5f) : ang;
                float rad = Mathf.Deg2Rad * finalAng;
                Vector3 pos = new Vector3(Mathf.Cos(rad) * wallRadius, baseFloorY, Mathf.Sin(rad) * wallRadius);

                string towerName = isGateAxis ? $"Gatehouse_FlankingBastion_{i}" : $"CurtainWall_Watchtower_{i}";
                GameObject towerObj = new GameObject(towerName);
                towerObj.transform.SetParent(wallRoot.transform, false);
                towerObj.transform.position = pos;
                towerObj.transform.rotation = Quaternion.Euler(0f, -finalAng - 90f, 0f);

                if (meshyTowerPrefab != null)
                {
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(meshyTowerPrefab, towerObj.transform);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;
                }
                else
                {
                    // Procedural High-Fidelity Bastion Tower
                    BuildProceduralHighFidelityTower(towerObj, isGateAxis, stoneMat, roofMat, woodMat);
                }

                // If gate axis, spawn the matching twin bastion on the other flank!
                if (isGateAxis)
                {
                    float twinAng = ang - 2.5f;
                    float twinRad = Mathf.Deg2Rad * twinAng;
                    Vector3 twinPos = new Vector3(Mathf.Cos(twinRad) * wallRadius, baseFloorY, Mathf.Sin(twinRad) * wallRadius);

                    string twinName = $"Gatehouse_TwinBastion_{i}";
                    GameObject twinObj = new GameObject(twinName);
                    twinObj.transform.SetParent(wallRoot.transform, false);
                    twinObj.transform.position = twinPos;
                    twinObj.transform.rotation = Quaternion.Euler(0f, -twinAng - 90f, 0f);

                    if (meshyTowerPrefab != null)
                    {
                        GameObject inst2 = (GameObject)PrefabUtility.InstantiatePrefab(meshyTowerPrefab, twinObj.transform);
                        inst2.transform.localPosition = Vector3.zero;
                        inst2.transform.localRotation = Quaternion.identity;
                        inst2.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        BuildProceduralHighFidelityTower(twinObj, true, stoneMat, roofMat, woodMat);
                    }
                }
            }
        }

        private static void BuildProceduralHighFidelityTower(GameObject parent, bool isBastion, Material stoneMat, Material roofMat, Material woodMat)
        {
            int towerSegs = 32; // Silky smooth circular cylinder
            float towerRadius = isBastion ? 8.5f : 7.2f;
            float shaftHeight = isBastion ? 13.5f : 12.0f; // Y: 0 to 13.5m (world Y = 22 to 35.5m)
            float machicolationRadius = towerRadius + 1.6f;
            float machicolationHeight = 1.6f;
            float parapetHeight = 2.0f;
            float roofHeight = 7.5f;

            // 1. Lower Submerged Plinth (drops into water/foundation from Y=1.2m down to Y=-12m)
            CreateTowerCylinder(parent.transform, "Tower_SubmergedPlinth", towerRadius + 0.6f, -12f, 1.2f, towerSegs, stoneMat, true);

            // 2. Main Tower Shaft (Y=-0.5m to shaftHeight)
            CreateTowerCylinder(parent.transform, "Tower_MainShaft", towerRadius, -0.5f, shaftHeight, towerSegs, stoneMat, true);

            // 3. Flared Machicolation Corbel Gallery (flare from towerRadius to machicolationRadius)
            CreateTowerCorbelFlare(parent.transform, "Tower_MachicolationCorbels", towerRadius, machicolationRadius, shaftHeight, shaftHeight + machicolationHeight, towerSegs, stoneMat);

            // 4. Crenellated Parapet with Battlements (at machicolationRadius)
            CreateTowerBattlements(parent.transform, "Tower_CrenellatedBattlements", machicolationRadius, shaftHeight + machicolationHeight, shaftHeight + machicolationHeight + parapetHeight, towerSegs, stoneMat);

            // 5. Tower Platform Floor
            CreateTowerFloorDisc(parent.transform, "Tower_ObservationPlatform", machicolationRadius - 0.4f, shaftHeight + machicolationHeight, stoneMat);

            // 6. Conical Medieval Slate Roof
            CreateTowerConicalRoof(parent.transform, "Tower_ConicalSlateRoof", machicolationRadius + 0.4f, shaftHeight + machicolationHeight + parapetHeight - 0.3f, roofHeight, towerSegs, roofMat, woodMat);
        }

        private static void CreateTowerCylinder(Transform parent, string name, float radius, float yMin, float yMax, int segments, Material mat, bool outwardNormals)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float dAngle = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float ang = i * dAngle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);

                Vector3 n = outwardNormals ? new Vector3(cos, 0f, sin) : new Vector3(-cos, 0f, -sin);

                verts.Add(new Vector3(cos * radius, yMin, sin * radius));
                norms.Add(n);
                uvs.Add(new Vector2((float)i / segments * 4f, 0f));

                verts.Add(new Vector3(cos * radius, yMax, sin * radius));
                norms.Add(n);
                uvs.Add(new Vector2((float)i / segments * 4f, (yMax - yMin) * 0.25f));
            }

            for (int i = 0; i < segments; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = (i + 1) * 2;
                int t1 = b1 + 1;

                // Outward
                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);

                // Inward (Double sided)
                tris.Add(b0); tris.Add(t1); tris.Add(t0);
                tris.Add(b0); tris.Add(b1); tris.Add(t1);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = obj.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void CreateTowerCorbelFlare(Transform parent, string name, float rBottom, float rTop, float yMin, float yMax, int segments, Material mat)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float dAngle = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float ang = i * dAngle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);

                Vector3 flareNorm = new Vector3(cos * 0.7f, -0.4f, sin * 0.7f).normalized;

                verts.Add(new Vector3(cos * rBottom, yMin, sin * rBottom));
                norms.Add(flareNorm);
                uvs.Add(new Vector2((float)i / segments * 4f, 0f));

                verts.Add(new Vector3(cos * rTop, yMax, sin * rTop));
                norms.Add(flareNorm);
                uvs.Add(new Vector2((float)i / segments * 4f, 1f));
            }

            for (int i = 0; i < segments; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = (i + 1) * 2;
                int t1 = b1 + 1;

                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void CreateTowerBattlements(Transform parent, string name, float radius, float yMin, float yMax, int segments, Material mat)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float dAngle = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float ang = i * dAngle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);

                Vector3 n = new Vector3(cos, 0f, sin);
                bool isMerlon = (i % 4 < 2);
                float currentY = isMerlon ? yMax : (yMin + 0.6f);

                verts.Add(new Vector3(cos * radius, yMin, sin * radius));
                norms.Add(n);
                uvs.Add(new Vector2((float)i / segments * 6f, 0f));

                verts.Add(new Vector3(cos * radius, currentY, sin * radius));
                norms.Add(n);
                uvs.Add(new Vector2((float)i / segments * 6f, (currentY - yMin) * 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = (i + 1) * 2;
                int t1 = b1 + 1;

                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void CreateTowerFloorDisc(Transform parent, string name, float radius, float yLevel, Material mat)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            verts.Add(new Vector3(0f, yLevel, 0f));
            norms.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int segs = 24;
            float dAngle = 360f / segs;
            for (int i = 0; i <= segs; i++)
            {
                float ang = i * dAngle * Mathf.Deg2Rad;
                float x = Mathf.Cos(ang) * radius;
                float z = Mathf.Sin(ang) * radius;

                verts.Add(new Vector3(x, yLevel, z));
                norms.Add(Vector3.up);
                uvs.Add(new Vector2(x * 0.1f, z * 0.1f));
            }

            for (int i = 1; i <= segs; i++)
            {
                tris.Add(0); tris.Add(i + 1); tris.Add(i);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void CreateTowerConicalRoof(Transform parent, string name, float radius, float baseY, float height, int segments, Material roofMat, Material woodMat)
        {
            GameObject roofObj = new GameObject(name);
            roofObj.transform.SetParent(parent, false);

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            Vector3 apex = new Vector3(0f, baseY + height, 0f);
            float dAngle = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float ang0 = i * dAngle * Mathf.Deg2Rad;
                float ang1 = (i + 1) * dAngle * Mathf.Deg2Rad;

                Vector3 v0 = new Vector3(Mathf.Cos(ang0) * radius, baseY, Mathf.Sin(ang0) * radius);
                Vector3 v1 = new Vector3(Mathf.Cos(ang1) * radius, baseY, Mathf.Sin(ang1) * radius);

                Vector3 n = Vector3.Cross(v1 - v0, apex - v0).normalized;

                int idx = verts.Count;
                verts.Add(apex);
                norms.Add(n);
                uvs.Add(new Vector2(0.5f, 1f));

                verts.Add(v0);
                norms.Add(n);
                uvs.Add(new Vector2(0f, 0f));

                verts.Add(v1);
                norms.Add(n);
                uvs.Add(new Vector2(1f, 0f));

                tris.Add(idx);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var mf = roofObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = roofObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = roofMat;

            // Timber roof eaves collar
            CreateTowerCylinder(roofObj.transform, "Roof_EavesCollar", radius + 0.15f, baseY - 0.35f, baseY + 0.1f, segments, woodMat ?? roofMat, true);
        }
    }
}
#endif
