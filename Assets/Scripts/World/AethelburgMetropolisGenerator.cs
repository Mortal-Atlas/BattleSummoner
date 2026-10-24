#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.World;
using BattleSummoner.Audio;
using BattleSummoner.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.World
{
    /// <summary>
    /// AethelburgMetropolisGenerator constructs the exact 4-Tier Blueprint Metropolis
    /// strictly matching the architectural blueprint diagram and overworld map concept art.
    ///
    /// Architectural Tiers & Blueprint Elevation:
    /// - Tier 4 (Inner Citadel, Center, Highest ~$18.0m$, R=0-75m): Grand Cathedral,
    ///   Mages' Quarter Arcane Spire, Hover Pads, Central Spire Waypoint, 8 Anti-Gravity flight lanes.
    /// - Tier 3 (Upper Ring, ~$12.0m$, R=75-155m): Residential Districts (West/NW),
    ///   Military Barracks & Training (NE), High-End Commercials / Grand Guildhall (East).
    /// - Tier 2 (Mid Ring, ~$6.0m$, R=155-235m): Commercial & Trade Shops (Bakeries,
    ///   Blacksmiths, Magic Item Stores, Enchanters, Taverns, Inns), Tents & Local Bazaars (South/SW),
    ///   Utility & Power Sector (SE).
    /// - Tier 1 (Outer Ground Ring, ~$0.0m$, R=235-300m): Outdoor Bazaars & Markets along streets,
    ///   Farming / Agri Plots (South/SE), Low-District Housing (West/NW).
    /// - Outer Curtain Wall (R=300m): 8 Bastion Gates with glowing Mana-Shield Portals &
    ///   Mana-Shield Generator Towers.
    /// - Circular Moat (R=305-345m): Azure canal spanned by 8 Grand Stone Bridges.
    /// - Outer Overworld (R=345-850m): NE Volcanic crags with purple rift crater,
    ///   Mistwood forest biomes with crimson runic circles, winding rivers to ocean.
    ///
    /// ALL BUILDINGS FEATURE OPEN ENTRANCE DOORWAYS (NO DOORS) - ZERO WALK-IN OBSTRUCTION!
    /// </summary>
    public static class AethelburgMetropolisGenerator
    {
        public const string WorldRootName = "--- AETHELBURG METROPOLIS ROOT ---";

        // Spatial Radii & Elevations (in Meters) strictly matching the blueprint
        public const float Tier4CitadelRadius = 75f;      // Tier 4 (Center, Highest)
        public const float Tier3UpperRadius = 155f;       // Tier 3
        public const float Tier2MidRadius = 235f;         // Tier 2
        public const float Tier1GroundRadius = 300f;      // Tier 1 (Curtain wall)
        public const float MoatInnerRadius = 300f;        // Moat inner bank
        public const float MoatOuterRadius = 345f;        // Moat outer bank
        public const float OuterwildsRadius = 850f;       // Island perimeter

        public const float Tier4Height = 18.0f;           // Citadel elevation
        public const float Tier3Height = 12.0f;           // Upper Ring elevation
        public const float Tier2Height = 6.0f;            // Mid Ring elevation
        public const float Tier1Height = 0.0f;            // Ground Ring elevation
        public const float AvenueWidth = 16.0f;

        private static Dictionary<string, Material> _matCache = new Dictionary<string, Material>();

#if UNITY_EDITOR
        [MenuItem("BattleSummoner/Build Massive Aethelburg Metropolis", false, 1)]
        public static void GenerateMetropolisMenu()
        {
            BuildFullMetropolis();
        }
#endif

        public static void BuildFullMetropolis()
        {
            Debug.Log("<color=#00E5FF><b>[AethelburgMetropolisGenerator] Building 4-Tier Blueprint Metropolis strictly following blueprint and map...</b></color>");

            // 1. Clean existing roots
            var oldRoot = GameObject.Find(WorldRootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);

            var oldMeshyRoot = GameObject.Find("--- MESHY OVERWORLD ROOT ---");
            if (oldMeshyRoot != null) Object.DestroyImmediate(oldMeshyRoot);

            GameObject root = new GameObject(WorldRootName);
            root.transform.position = Vector3.zero;

            // 2. Load Core Architectural Materials Cache
            InitMaterialCache();

            Material matTier4 = GetMat("Mat_Tier4_CitadelPlateau") ?? GetMat("Mat_City_PavedLimestone");
            Material matTier3 = GetMat("Mat_Tier3_UpperRing") ?? GetMat("Mat_City_PavedLimestone");
            Material matTier2 = GetMat("Mat_Tier2_MidRing") ?? GetMat("Mat_World_Cobblestone");
            Material matTier1 = GetMat("Mat_Tier1_GroundRing") ?? GetMat("Mat_World_Cobblestone");
            Material matPaving = GetMat("Mat_City_PavedLimestone");
            Material matAntiGravity = GetMat("Mat_City_AntiGravityLane");
            Material matFarmland = GetMat("Mat_City_Farmland");
            Material matVolcanic = GetMat("Mat_Volcanic_BasaltRift");
            Material matGrass = GetMat("Mat_World_ForestGrass");
            Material matStoneWall = GetMat("Mat_World_TownWall");
            Material matWater = GetMat("M_Metro_CascadeWater");

            // 3. Build Multi-Tier Concentric Foundation Terraces (4 Tiers strictly color-coded & elevated)
            Transform foundationGroup = new GameObject("01_Foundation_Terraces").transform;
            foundationGroup.SetParent(root.transform, false);

            // Tier 4: Inner Citadel Plateau (Center, Highest: +18m)
            BuildTierPlateau(foundationGroup, "Tier4_Citadel_Plateau", 0f, Tier4CitadelRadius, Tier4Height, matTier4);

            // Tier 3: Upper Ring (+12m)
            BuildTierRing(foundationGroup, "Tier3_UpperRing_Terrace", Tier4CitadelRadius, Tier3UpperRadius, Tier3Height, matTier3);

            // Tier 2: Mid Ring (+6m)
            BuildTierRing(foundationGroup, "Tier2_MidRing_Terrace", Tier3UpperRadius, Tier2MidRadius, Tier2Height, matTier2);

            // Tier 1: Ground Ring (+0m)
            BuildTierRing(foundationGroup, "Tier1_GroundRing_Terrace", Tier2MidRadius, Tier1GroundRadius, Tier1Height, matTier1);

            // Farming Plots in Tier 1 South/SE Sector (R=240-290m, Angle 105-175 deg)
            BuildFarmlandSector(foundationGroup, "Tier1_Farmland_AgriPlots", 240f, 290f, 105f, 175f, Tier1Height + 0.03f, matFarmland);

            // Tier 0: Outerwilds Plain (MoatOuterRadius to OuterwildsRadius, -0.2m)
            BuildTierRing(foundationGroup, "Tier0_Outerwilds_Plain", MoatOuterRadius, OuterwildsRadius, Tier1Height - 0.2f, matGrass, 0.03f);

            // NE Volcanic Chasm in Outerwilds (Angle 30-70 deg, R=420-750m)
            BuildVolcanicSector(foundationGroup, "Outerwilds_NE_VolcanicCrags", 420f, 750f, 30f, 70f, 15f, matVolcanic);

            // 4. Retaining Walls & Outer Barrier Wall
            Transform wallsGroup = new GameObject("02_Walls_And_Canals").transform;
            wallsGroup.SetParent(root.transform, false);

            BuildRetainingWall(wallsGroup, "RetainingWall_Tier4_Citadel", Tier4CitadelRadius, Tier3Height, Tier4Height + 1.2f, matStoneWall, true, 22.0f);
            BuildRetainingWall(wallsGroup, "RetainingWall_Tier3_Upper", Tier3UpperRadius, Tier2Height, Tier3Height + 1.2f, matStoneWall, true, 22.0f);
            BuildRetainingWall(wallsGroup, "RetainingWall_Tier2_Mid", Tier2MidRadius, Tier1Height, Tier2Height + 1.2f, matStoneWall, true, 22.0f);
            BuildRetainingWall(wallsGroup, "CurtainWall_Tier1_Fortress", Tier1GroundRadius, Tier1Height, Tier1Height + 14f, matStoneWall, true, 28.0f);

            // Circular Moat with Azure Water
            BuildMoatCanal(wallsGroup, MoatInnerRadius, MoatOuterRadius, -3.5f, -1.2f, matWater, matStoneWall);

            // 5. Build 8 Radial Imperial Avenues with Anti-Gravity Flight Lanes
            Transform avenuesGroup = new GameObject("03_Radial_Avenues_8Way").transform;
            avenuesGroup.SetParent(root.transform, false);
            Build8RadialAvenues(avenuesGroup, matPaving, matAntiGravity);

            // 6. Build Engineered Grand Stairs & Mount Ramps along the 8 Avenues
            Transform stairsGroup = new GameObject("04_Grand_Engineered_Stairs").transform;
            stairsGroup.SetParent(root.transform, false);
            BuildGrandStairs8Way(stairsGroup, matStoneWall);

            // 7. Build 8 Bastion Gates, Mana Shield Generators & Moat Bridges
            Transform gatesGroup = new GameObject("05_Bastion_Gates_And_Bridges").transform;
            gatesGroup.SetParent(root.transform, false);
            Build8BastionGatesAndBridges(gatesGroup);

            // 8. Populate Blueprint Districts (Strictly matching blueprint zoning & elevation)
            Transform landmarksGroup = new GameObject("06_Blueprint_City_Districts").transform;
            landmarksGroup.SetParent(root.transform, false);
            PopulateBlueprintDistricts(landmarksGroup);

            // 8b. Town Life, NPCs, Critters & Flora
            Transform lifeGroup = new GameObject("06b_Town_Life_And_Flora").transform;
            lifeGroup.SetParent(root.transform, false);
            PopulateTownLifeAndFlora(lifeGroup);

            // 8c. Props, Market Details & Treasures
            Transform propsGroup = new GameObject("06c_City_Props_And_Treasures").transform;
            propsGroup.SetParent(root.transform, false);
            PopulateCityPropsAndDetails(propsGroup);

            // 8d. Outerwilds Shrines & Mistwood Groves
            Transform outerwildsGroup = new GameObject("06d_Outerwilds_Biomes").transform;
            outerwildsGroup.SetParent(root.transform, false);
            PopulateOuterwildsBiomes(outerwildsGroup);

            // 9. Streetlamps & Atmosphere
            Transform lightingGroup = new GameObject("07_Lighting_And_Atmosphere").transform;
            lightingGroup.SetParent(root.transform, false);
            SetupLightingAndGridLamps(lightingGroup);
            SetupAtmosphereAndAudio(lightingGroup);

            // 10. Setup Player Hero, Camera & HUD
            SetupPlayerHero(root.transform);

            Debug.Log("<color=#76FF03><b>[AethelburgMetropolisGenerator] Blueprint City Generation Succeeded! 4-Tiers, 8 Gates, Open Walk-in Doorways.</b></color>");

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
        }

        private static void InitMaterialCache()
        {
            _matCache.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Material", new string[] { "Assets/Materials" });
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && !_matCache.ContainsKey(mat.name))
                {
                    _matCache[mat.name] = mat;
                }
            }
        }

        private static Material GetMat(string name)
        {
            _matCache.TryGetValue(name, out Material m);
            return m;
        }

        public static void ApplyMetropolisMaterials(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string rawName = mats[i].name.Replace(" (Instance)", "");
                    if (_matCache.TryGetValue(rawName, out Material projMat))
                    {
                        mats[i] = projMat;
                        changed = true;
                    }
                    else
                    {
                        // Intelligent fuzzy fallback for any model material name
                        Material fallback = null;
                        if (rawName.Contains("Plaster")) fallback = GetMat("M_Metro_PlasterTimber");
                        else if (rawName.Contains("Timber") || rawName.Contains("Wood") || rawName.Contains("Beam")) fallback = GetMat("M_Metro_TimberWood");
                        else if (rawName.Contains("Roof") || rawName.Contains("Slate") || rawName.Contains("Clay")) fallback = GetMat("M_Metro_RoofSlate");
                        else if (rawName.Contains("Stone") || rawName.Contains("Wall") || rawName.Contains("Masonry")) fallback = GetMat("M_Metro_StoneBase") ?? GetMat("Mat_World_TownWall");
                        else if (rawName.Contains("Fire") || rawName.Contains("Coal") || rawName.Contains("Furnace")) fallback = GetMat("M_Metro_ForgeFire");
                        else if (rawName.Contains("Iron") || rawName.Contains("Steel") || rawName.Contains("Wrought")) fallback = GetMat("M_Metro_RustedIron") ?? GetMat("M_WeaponSteel");
                        else if (rawName.Contains("Canopy") || rawName.Contains("Cloth") || rawName.Contains("Tassel")) fallback = GetMat("M_Metro_CanopyRed");
                        else if (rawName.Contains("Rune") || rawName.Contains("Magic") || rawName.Contains("Portal") || rawName.Contains("Aura")) fallback = GetMat("M_Metro_RuneGlow");
                        else if (rawName.Contains("Gold") || rawName.Contains("Brass") || rawName.Contains("Bronze")) fallback = GetMat("M_Metro_ClockworkBrass");
                        else if (rawName.Contains("Bread")) fallback = GetMat("M_Metro_BreadGold");
                        else if (rawName.Contains("Marble") || rawName.Contains("Limestone")) fallback = GetMat("M_Metro_Limestone") ?? GetMat("M_Metro_Marble");

                        if (fallback != null)
                        {
                            mats[i] = fallback;
                            changed = true;
                        }
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        // =========================================================================
        // PROCEDURAL MESH GENERATION HELPERS
        // =========================================================================

        private static void BuildTierPlateau(Transform parent, string name, float innerR, float outerR, float height, Material mat, float uvScale = 0.05f)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            int segments = 64;
            Mesh mesh = new Mesh { name = name + "_Mesh" };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            verts.Add(new Vector3(0f, height, 0f));
            uvs.Add(new Vector2(0f, 0f));

            float angleStep = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                float x = Mathf.Sin(rad) * outerR;
                float z = Mathf.Cos(rad) * outerR;
                verts.Add(new Vector3(x, height, z));
                uvs.Add(new Vector2(x * uvScale, z * uvScale));
            }

            for (int i = 1; i <= segments; i++)
            {
                tris.Add(0);
                tris.Add(i);
                tris.Add(i + 1);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = obj.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void BuildTierRing(Transform parent, string name, float innerR, float outerR, float height, Material mat, float uvScale = 0.06f)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            int segments = 96;
            Mesh mesh = new Mesh { name = name + "_Mesh" };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float angleStep = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                float sin = Mathf.Sin(rad);
                float cos = Mathf.Cos(rad);

                Vector3 vInner = new Vector3(sin * innerR, height, cos * innerR);
                Vector3 vOuter = new Vector3(sin * outerR, height, cos * outerR);

                verts.Add(vInner);
                verts.Add(vOuter);

                uvs.Add(new Vector2(vInner.x * uvScale, vInner.z * uvScale));
                uvs.Add(new Vector2(vOuter.x * uvScale, vOuter.z * uvScale));
            }

            for (int i = 0; i < segments; i++)
            {
                int idx = i * 2;
                tris.Add(idx);
                tris.Add(idx + 1);
                tris.Add(idx + 2);

                tris.Add(idx + 1);
                tris.Add(idx + 3);
                tris.Add(idx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = obj.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void BuildFarmlandSector(Transform parent, string name, float innerR, float outerR, float startAng, float endAng, float height, Material mat)
        {
            if (mat == null) return;
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            int segments = 24;
            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float angleStep = (endAng - startAng) / segments;
            for (int i = 0; i <= segments; i++)
            {
                float ang = startAng + i * angleStep;
                float rad = Mathf.Deg2Rad * ang;
                float sin = Mathf.Sin(rad);
                float cos = Mathf.Cos(rad);

                Vector3 vInner = new Vector3(sin * innerR, height, cos * innerR);
                Vector3 vOuter = new Vector3(sin * outerR, height, cos * outerR);
                verts.Add(vInner);
                verts.Add(vOuter);
                uvs.Add(new Vector2(vInner.x * 0.12f, vInner.z * 0.12f));
                uvs.Add(new Vector2(vOuter.x * 0.12f, vOuter.z * 0.12f));
            }

            for (int i = 0; i < segments; i++)
            {
                int idx = i * 2;
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
                tris.Add(idx + 1); tris.Add(idx + 3); tris.Add(idx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = obj.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void BuildVolcanicSector(Transform parent, string name, float innerR, float outerR, float startAng, float endAng, float maxElevation, Material mat)
        {
            if (mat == null) return;
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            int radialSegs = 16;
            int angularSegs = 24;
            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float rStep = (outerR - innerR) / radialSegs;
            float aStep = (endAng - startAng) / angularSegs;

            for (int r = 0; r <= radialSegs; r++)
            {
                float curR = innerR + r * rStep;
                float rNorm = (float)r / radialSegs;
                for (int a = 0; a <= angularSegs; a++)
                {
                    float curA = startAng + a * aStep;
                    float aNorm = (float)a / angularSegs;
                    float rad = Mathf.Deg2Rad * curA;

                    float mountainH = Mathf.Sin(rNorm * Mathf.PI) * Mathf.Sin(aNorm * Mathf.PI) * maxElevation;
                    float cragNoise = Mathf.PerlinNoise(curR * 0.05f, curA * 0.2f) * 4.0f;
                    float y = Tier1Height + mountainH + cragNoise;

                    Vector3 pos = new Vector3(Mathf.Sin(rad) * curR, y, Mathf.Cos(rad) * curR);
                    verts.Add(pos);
                    uvs.Add(new Vector2(pos.x * 0.05f, pos.z * 0.05f));
                }
            }

            int stride = angularSegs + 1;
            for (int r = 0; r < radialSegs; r++)
            {
                for (int a = 0; a < angularSegs; a++)
                {
                    int i0 = r * stride + a;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * stride + a;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();

            var mf = obj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = obj.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void BuildRetainingWall(Transform parent, string name, float radius, float bottomY, float topY, Material mat, bool hasGateOpenings = true, float gateWidth = 24.0f)
        {
            GameObject wallObj = new GameObject(name);
            wallObj.transform.SetParent(parent, false);

            int segments = 288;
            float wallThickness = 2.4f;
            float angleStep = 360f / segments;

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float rInner = radius - wallThickness * 0.5f;
            float rOuter = radius + wallThickness * 0.5f;
            float openHalfAngle = (gateWidth * 0.5f / radius) * Mathf.Rad2Deg;

            for (int i = 0; i <= segments; i++)
            {
                float ang = i * angleStep;
                float rad = Mathf.Deg2Rad * ang;
                float sin = Mathf.Sin(rad);
                float cos = Mathf.Cos(rad);

                Vector3 bIn = new Vector3(sin * rInner, bottomY, cos * rInner);
                Vector3 bOut = new Vector3(sin * rOuter, bottomY, cos * rOuter);
                Vector3 tIn = new Vector3(sin * rInner, topY, cos * rInner);
                Vector3 tOut = new Vector3(sin * rOuter, topY, cos * rOuter);

                verts.Add(bIn);  // 0
                verts.Add(bOut); // 1
                verts.Add(tIn);  // 2
                verts.Add(tOut); // 3

                float u = (float)i / segments * 32f;
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
                uvs.Add(new Vector2(u, 1f));
            }

            float[] gateAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

            for (int i = 0; i < segments; i++)
            {
                float midAng = (i + 0.5f) * angleStep;
                if (hasGateOpenings)
                {
                    bool isOpening = false;
                    foreach (var ga in gateAngles)
                    {
                        float diff = Mathf.Abs(Mathf.DeltaAngle(midAng, ga));
                        if (diff < openHalfAngle)
                        {
                            isOpening = true;
                            break;
                        }
                    }
                    if (isOpening) continue;
                }

                int cur = i * 4;
                int nxt = (i + 1) * 4;

                // Outer Face (+R)
                tris.Add(cur + 1); tris.Add(nxt + 3); tris.Add(cur + 3);
                tris.Add(cur + 1); tris.Add(nxt + 1); tris.Add(nxt + 3);

                // Top Walkway (+Y)
                tris.Add(cur + 2); tris.Add(nxt + 3); tris.Add(cur + 3);
                tris.Add(cur + 2); tris.Add(nxt + 2); tris.Add(nxt + 3);

                // Inner Face (-R)
                tris.Add(cur + 0); tris.Add(cur + 2); tris.Add(nxt + 2);
                tris.Add(cur + 0); tris.Add(nxt + 2); tris.Add(nxt + 0);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();

            var mf = wallObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = wallObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            var col = wallObj.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }

        private static void BuildMoatCanal(Transform parent, float innerR, float outerR, float bedY, float waterY, Material matWater, Material matStone)
        {
            GameObject canalRoot = new GameObject("Moat_AzureCanal");
            canalRoot.transform.SetParent(parent, false);

            BuildTierRing(canalRoot.transform, "Moat_WaterSurface", innerR, outerR, waterY, matWater, 0.05f);
            BuildTierRing(canalRoot.transform, "Moat_CanalBed", innerR, outerR, bedY, matStone, 0.1f);
            BuildRetainingWall(canalRoot.transform, "Moat_InnerBank_Wall", innerR, bedY, Tier1Height, matStone, false);
            BuildRetainingWall(canalRoot.transform, "Moat_OuterBank_Wall", outerR, bedY, Tier1Height - 0.2f, matStone, false);

            // 8 Winding River Canals radiating out into the ocean between sectors (matching concept art!)
            float[] riverAngles = new float[] { 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f, 337.5f };
            for (int r = 0; r < riverAngles.Length; r++)
            {
                float rad = Mathf.Deg2Rad * riverAngles[r];
                Vector3 rDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                BuildStraightRiver(canalRoot.transform, $"Outerwilds_River_{r + 1}", rDir * outerR, rDir, OuterwildsRadius - outerR, 14f, waterY, matWater);
            }
        }

        private static void BuildStraightRiver(Transform parent, string name, Vector3 origin, Vector3 dir, float length, float width, float waterY, Material matWater)
        {
            GameObject rObj = new GameObject(name);
            rObj.transform.SetParent(parent, false);

            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized * (width * 0.5f);
            int segs = 30;
            float step = length / segs;

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int i = 0; i <= segs; i++)
            {
                Vector3 center = origin + dir * (i * step);
                center.y = waterY;
                verts.Add(center - right);
                verts.Add(center + right);
                uvs.Add(new Vector2(0f, (float)i * 0.2f));
                uvs.Add(new Vector2(1f, (float)i * 0.2f));
            }

            for (int i = 0; i < segs; i++)
            {
                int idx = i * 2;
                tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
                tris.Add(idx + 1); tris.Add(idx + 3); tris.Add(idx + 2);
            }

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();

            var mf = rObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = rObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matWater;
        }

        // =========================================================================
        // 8 RADIAL IMPERIAL AVENUES WITH ANTI-GRAVITY LANES
        // =========================================================================

        private static void Build8RadialAvenues(Transform parent, Material matPaving, Material matAntiGravity)
        {
            float[] angles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            string[] names = new string[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" };

            for (int i = 0; i < 8; i++)
            {
                float rad = Mathf.Deg2Rad * angles[i];
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                BuildGradedRadialAvenue(parent, $"Avenue_{names[i]}", Vector3.zero, dir, 800f, AvenueWidth, matPaving, matAntiGravity);
            }

            // Concentric Boulevards along Tier Rims
            BuildConcentricBoulevardRibbon(parent, "Boulevard_Tier3_UpperRing", 115f, 12f, Tier3Height, matPaving);
            BuildConcentricBoulevardRibbon(parent, "Boulevard_Tier2_CivicRing", 195f, 14f, Tier2Height, matPaving);
            BuildConcentricBoulevardRibbon(parent, "Boulevard_Tier1_OuterRing", 265f, 16f, Tier1Height, matPaving);
        }

        private static void BuildGradedRadialAvenue(Transform parent, string name, Vector3 origin, Vector3 dir, float length, float width, Material matRoad, Material matAntiGravity)
        {
            GameObject roadRoot = new GameObject(name);
            roadRoot.transform.SetParent(parent, false);

            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized * (width * 0.5f);

            int segments = 80;
            float step = length / segments;

            // 1. Road Pavement Mesh
            Mesh roadMesh = new Mesh { name = name + "_RoadMesh" };
            List<Vector3> rVerts = new List<Vector3>();
            List<Vector2> rUvs = new List<Vector2>();
            List<int> rTris = new List<int>();

            for (int i = 0; i <= segments; i++)
            {
                float dist = i * step;
                float currentY = GetBlueprintAvenueHeight(dist);
                Vector3 center = origin + dir * dist;
                center.y = currentY + 0.12f;

                rVerts.Add(center - right);
                rVerts.Add(center + right);

                rUvs.Add(new Vector2(0f, dist * 0.06f));
                rUvs.Add(new Vector2(1f, dist * 0.06f));
            }

            for (int i = 0; i < segments; i++)
            {
                int idx = i * 2;
                rTris.Add(idx); rTris.Add(idx + 1); rTris.Add(idx + 2);
                rTris.Add(idx + 1); rTris.Add(idx + 3); rTris.Add(idx + 2);
            }

            roadMesh.SetVertices(rVerts);
            roadMesh.SetUVs(0, rUvs);
            roadMesh.SetTriangles(rTris, 0);
            roadMesh.RecalculateNormals();

            var mf = roadRoot.AddComponent<MeshFilter>();
            mf.sharedMesh = roadMesh;
            var mr = roadRoot.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matRoad;
            var col = roadRoot.AddComponent<MeshCollider>();
            col.sharedMesh = roadMesh;

            // 2. Anti-Gravity Lane (Centerline ribbon width=4.0m)
            if (matAntiGravity != null)
            {
                GameObject laneObj = new GameObject(name + "_AntiGravity_Lane");
                laneObj.transform.SetParent(roadRoot.transform, false);

                Vector3 laneRight = right.normalized * 2.0f;
                Mesh laneMesh = new Mesh { name = name + "_LaneMesh" };
                List<Vector3> lVerts = new List<Vector3>();
                List<Vector2> lUvs = new List<Vector2>();
                List<int> lTris = new List<int>();

                int laneSegs = 60;
                float laneLen = 295f;
                float lStep = laneLen / laneSegs;

                for (int i = 0; i <= laneSegs; i++)
                {
                    float dist = 10f + i * lStep;
                    float currentY = GetBlueprintAvenueHeight(dist);
                    Vector3 center = origin + dir * dist;
                    center.y = currentY + 0.22f;

                    lVerts.Add(center - laneRight);
                    lVerts.Add(center + laneRight);

                    lUvs.Add(new Vector2(0f, (float)i * 0.5f));
                    lUvs.Add(new Vector2(1f, (float)i * 0.5f));
                }

                for (int i = 0; i < laneSegs; i++)
                {
                    int idx = i * 2;
                    lTris.Add(idx); lTris.Add(idx + 1); lTris.Add(idx + 2);
                    lTris.Add(idx + 1); lTris.Add(idx + 3); lTris.Add(idx + 2);
                }

                laneMesh.SetVertices(lVerts);
                laneMesh.SetUVs(0, lUvs);
                laneMesh.SetTriangles(lTris, 0);
                laneMesh.RecalculateNormals();

                var lmf = laneObj.AddComponent<MeshFilter>();
                lmf.sharedMesh = laneMesh;
                var lmr = laneObj.AddComponent<MeshRenderer>();
                lmr.sharedMaterial = matAntiGravity;
            }
        }

        private static float GetBlueprintAvenueHeight(float r)
        {
            if (r <= 65f) return Tier4Height;
            if (r < 75f) return Mathf.Lerp(Tier4Height, Tier3Height, (r - 65f) / 10f);
            if (r <= 145f) return Tier3Height;
            if (r < 155f) return Mathf.Lerp(Tier3Height, Tier2Height, (r - 145f) / 10f);
            if (r <= 225f) return Tier2Height;
            if (r < 235f) return Mathf.Lerp(Tier2Height, Tier1Height, (r - 225f) / 10f);
            return Tier1Height;
        }

        private static void BuildConcentricBoulevardRibbon(Transform parent, string name, float radius, float width, float y, Material mat)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            float innerR = radius - width * 0.5f;
            float outerR = radius + width * 0.5f;
            // Elevate boulevard ribbon by +0.06m above terrace ground to permanently eliminate Z-fighting
            BuildTierRing(obj.transform, name + "_Ribbon", innerR, outerR, y + 0.06f, mat, 0.15f);
        }

        // =========================================================================
        // ENGINEERED GRAND CONNECTING STAIRCASES (SMOOTH MOUNT COLLIDERS)
        // =========================================================================

        private static void BuildGrandStairs8Way(Transform parent, Material matStone)
        {
            // A. 24 Grand Avenue Staircases (16m wide on the 8 Main Avenues)
            float[] angles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            string[] names = new string[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" };

            for (int i = 0; i < 8; i++)
            {
                float rad = Mathf.Deg2Rad * angles[i];
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                string name = names[i];

                BuildEngineeredStair(parent, $"AvenueStairs_Tier4to3_{name}", dir * 63f, dir * 77f, Tier4Height, Tier3Height, 16.0f, 22, matStone);
                BuildEngineeredStair(parent, $"AvenueStairs_Tier3to2_{name}", dir * 143f, dir * 157f, Tier3Height, Tier2Height, 16.0f, 22, matStone);
                BuildEngineeredStair(parent, $"AvenueStairs_Tier2to1_{name}", dir * 223f, dir * 237f, Tier2Height, Tier1Height, 16.0f, 22, matStone);
            }

            // B. 24 Secondary Sector Connecting Stairs (8.5m wide halfway between avenues)
            float[] sectorAngles = { 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f, 337.5f };
            for (int s = 0; s < 8; s++)
            {
                float sRad = Mathf.Deg2Rad * sectorAngles[s];
                Vector3 sDir = new Vector3(Mathf.Sin(sRad), 0f, Mathf.Cos(sRad));
                string sName = $"Sector_{sectorAngles[s]:F0}deg";

                BuildEngineeredStair(parent, $"SectorStairs_Tier4to3_{sName}", sDir * 65f, sDir * 77f, Tier4Height, Tier3Height, 8.5f, 18, matStone);
                BuildEngineeredStair(parent, $"SectorStairs_Tier3to2_{sName}", sDir * 145f, sDir * 157f, Tier3Height, Tier2Height, 8.5f, 18, matStone);
                BuildEngineeredStair(parent, $"SectorStairs_Tier2to1_{sName}", sDir * 225f, sDir * 237f, Tier2Height, Tier1Height, 8.5f, 18, matStone);
            }
        }

        private static void BuildEngineeredStair(Transform parent, string name, Vector3 topPos, Vector3 bottomPos, float topY, float bottomY, float width, int stepCount, Material matStone)
        {
            GameObject stairRoot = new GameObject(name);
            stairRoot.transform.SetParent(parent, false);

            Vector3 dir = (bottomPos - topPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized * (width * 0.5f);
            float totalRun = Vector3.Distance(topPos, bottomPos);
            float stepRise = (topY - bottomY) / stepCount;

            // Lift stairs by +0.28m so they sit proudly ABOVE the road pavement mesh!
            float baseElevationOffset = 0.28f;

            Mesh mesh = new Mesh { name = name + "_StepsMesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int vertIndex = 0;
            for (int s = 0; s < stepCount; s++)
            {
                float t0 = (float)s / stepCount;
                float t1 = (float)(s + 1) / stepCount;

                Vector3 p0 = Vector3.Lerp(topPos, bottomPos, t0);
                Vector3 p1 = Vector3.Lerp(topPos, bottomPos, t1);
                float yLevel = topY + baseElevationOffset - (s * stepRise);

                Vector3 treadBackL = p0 - right; treadBackL.y = yLevel;
                Vector3 treadBackR = p0 + right; treadBackR.y = yLevel;
                Vector3 treadFrontL = p1 - right; treadFrontL.y = yLevel;
                Vector3 treadFrontR = p1 + right; treadFrontR.y = yLevel;

                verts.Add(treadBackL); verts.Add(treadBackR);
                verts.Add(treadFrontL); verts.Add(treadFrontR);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(width * 0.2f, 0f));
                uvs.Add(new Vector2(0f, 0.4f)); uvs.Add(new Vector2(width * 0.2f, 0.4f));

                tris.Add(vertIndex + 0); tris.Add(vertIndex + 1); tris.Add(vertIndex + 2);
                tris.Add(vertIndex + 1); tris.Add(vertIndex + 3); tris.Add(vertIndex + 2);
                vertIndex += 4;

                Vector3 riserBotL = treadFrontL; riserBotL.y = yLevel - stepRise;
                Vector3 riserBotR = treadFrontR; riserBotR.y = yLevel - stepRise;

                verts.Add(treadFrontL); verts.Add(treadFrontR);
                verts.Add(riserBotL); verts.Add(riserBotR);
                uvs.Add(new Vector2(0f, 0.4f)); uvs.Add(new Vector2(width * 0.2f, 0.4f));
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(width * 0.2f, 0f));

                tris.Add(vertIndex + 0); tris.Add(vertIndex + 1); tris.Add(vertIndex + 2);
                tris.Add(vertIndex + 1); tris.Add(vertIndex + 3); tris.Add(vertIndex + 2);
                vertIndex += 4;
            }

            // Left & Right Carved Stone Balustrades / Railings
            float balustradeWidth = 0.65f;
            float balustradeHeight = 0.95f;
            BuildStairBalustrade(topPos, bottomPos, right, dir, topY + baseElevationOffset, bottomY + baseElevationOffset, balustradeWidth, balustradeHeight, true, verts, uvs, tris, ref vertIndex);
            BuildStairBalustrade(topPos, bottomPos, right, dir, topY + baseElevationOffset, bottomY + baseElevationOffset, balustradeWidth, balustradeHeight, false, verts, uvs, tris, ref vertIndex);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();

            var mf = stairRoot.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = stairRoot.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matStone;

            Vector3 midPoint = (topPos + bottomPos) * 0.5f;
            midPoint.y = (topY + bottomY) * 0.5f + baseElevationOffset;

            GameObject rampColliderObj = new GameObject("Mount_Ramp_Collider");
            rampColliderObj.transform.SetParent(stairRoot.transform, false);
            rampColliderObj.transform.position = midPoint;
            rampColliderObj.transform.LookAt(new Vector3(bottomPos.x, midPoint.y, bottomPos.z));

            float slopeAngle = Mathf.Atan2(topY - bottomY, totalRun) * Mathf.Rad2Deg;
            rampColliderObj.transform.rotation = Quaternion.Euler(-slopeAngle, rampColliderObj.transform.eulerAngles.y, 0f);

            var boxCol = rampColliderObj.AddComponent<BoxCollider>();
            float hyp = Mathf.Sqrt(totalRun * totalRun + (topY - bottomY) * (topY - bottomY));
            boxCol.size = new Vector3(width, 0.45f, hyp);

            // Grand Stone Lantern Pillars at Top and Bottom
            CreateStairPillarLantern(stairRoot.transform, topPos - right * 0.95f, topY + baseElevationOffset, matStone);
            CreateStairPillarLantern(stairRoot.transform, topPos + right * 0.95f, topY + baseElevationOffset, matStone);
            CreateStairPillarLantern(stairRoot.transform, bottomPos - right * 0.95f, bottomY + baseElevationOffset, matStone);
            CreateStairPillarLantern(stairRoot.transform, bottomPos + right * 0.95f, bottomY + baseElevationOffset, matStone);

            var prop = stairRoot.AddComponent<BattleSummoner.Optimization.CullableWorldProp>();
            prop.boundingRadius = totalRun * 0.75f;
            prop.isDynamicMover = false;
        }

        private static void BuildStairBalustrade(Vector3 topPos, Vector3 bottomPos, Vector3 right, Vector3 dir, float topY, float bottomY, float bWidth, float bHeight, bool isLeft, List<Vector3> verts, List<Vector2> uvs, List<int> tris, ref int vertIdx)
        {
            Vector3 sideOffset = isLeft ? -right : right;
            Vector3 inward = isLeft ? (right.normalized * bWidth) : (-right.normalized * bWidth);

            Vector3 pTopOut = topPos + sideOffset; pTopOut.y = topY;
            Vector3 pTopIn = pTopOut + inward;
            Vector3 pBotOut = bottomPos + sideOffset; pBotOut.y = bottomY;
            Vector3 pBotIn = pBotOut + inward;

            Vector3 topCapOut = pTopOut + Vector3.up * bHeight;
            Vector3 topCapIn = pTopIn + Vector3.up * bHeight;
            Vector3 botCapOut = pBotOut + Vector3.up * bHeight;
            Vector3 botCapIn = pBotIn + Vector3.up * bHeight;

            // Cap quad (+Y)
            verts.Add(topCapOut); verts.Add(topCapIn); verts.Add(botCapOut); verts.Add(botCapIn);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0f, 4f)); uvs.Add(new Vector2(1f, 4f));
            tris.Add(vertIdx + 0); tris.Add(vertIdx + 1); tris.Add(vertIdx + 2);
            tris.Add(vertIdx + 1); tris.Add(vertIdx + 3); tris.Add(vertIdx + 2);
            vertIdx += 4;

            // Inward Wall Quad
            verts.Add(topCapIn); verts.Add(pTopIn); verts.Add(botCapIn); verts.Add(pBotIn);
            uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(4f, 1f)); uvs.Add(new Vector2(4f, 0f));
            tris.Add(vertIdx + 0); tris.Add(vertIdx + 1); tris.Add(vertIdx + 2);
            tris.Add(vertIdx + 1); tris.Add(vertIdx + 3); tris.Add(vertIdx + 2);
            vertIdx += 4;
        }

        private static void CreateStairPillarLantern(Transform parent, Vector3 worldPos, float y, Material matStone)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Stair_Stone_Pillar_Lantern";
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = new Vector3(worldPos.x, y + 1.1f, worldPos.z);
            pillar.transform.localScale = new Vector3(0.85f, 2.2f, 0.85f);

            var mr = pillar.GetComponent<MeshRenderer>();
            if (mr != null && matStone != null) mr.sharedMaterial = matStone;

            GameObject lightObj = new GameObject("Lantern_Light");
            lightObj.transform.SetParent(pillar.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);

            var l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 8.5f;
            l.intensity = 1.8f;
            l.color = new Color(1.0f, 0.84f, 0.52f);
        }

        // =========================================================================
        // 8 BASTION GATES, MANA SHIELD GENERATOR TOWERS & MOAT BRIDGES
        // =========================================================================

        private static void Build8BastionGatesAndBridges(Transform parent)
        {
            var bastionGatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_Bastion_PortalGate.fbx");
            var manaTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_ManaShield_GeneratorTower.fbx");
            var bridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/FantasyStoneArchBridge.fbx");
            var manaTowerMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Metropolis/Prop_ManaShield_GeneratorTower_Corrected.asset");
            var bastionGateMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Metropolis/Prop_Bastion_PortalGate_Corrected.asset");
            var bridgeMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Metropolis/FantasyStoneArchBridge_Corrected.asset");

            float[] angles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            string[] names = new string[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" };

            for (int i = 0; i < 8; i++)
            {
                float ang = angles[i];
                float rad = Mathf.Deg2Rad * ang;
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                Vector3 gatePos = dir * Tier1GroundRadius;
                Vector3 bridgePos = dir * ((MoatInnerRadius + MoatOuterRadius) * 0.5f);

                if (bastionGatePrefab != null)
                {
                    GameObject gateObj = (GameObject)PrefabUtility.InstantiatePrefab(bastionGatePrefab, parent);
                    gateObj.name = $"BastionGate_{names[i]}";
                    gateObj.transform.position = gatePos;
                    gateObj.transform.rotation = Quaternion.Euler(0f, ang, 0f);
                    gateObj.transform.localScale = Vector3.one;
                    if (bastionGateMesh != null)
                    {
                        var mf = gateObj.GetComponent<MeshFilter>();
                        if (mf != null) mf.sharedMesh = bastionGateMesh;
                    }
                    ApplyMetropolisMaterials(gateObj);
                    AddMeshCollidersRecursive(gateObj);

                    GameObject pLight = new GameObject("Mana_Portal_Light");
                    pLight.transform.SetParent(gateObj.transform, false);
                    pLight.transform.localPosition = new Vector3(0f, 6f, 0f);
                    var pl = pLight.AddComponent<Light>();
                    pl.type = LightType.Point;
                    pl.color = new Color(0.1f, 0.85f, 1.0f);
                    pl.range = 24f;
                    pl.intensity = 4.0f;
                }

                if (manaTowerPrefab != null)
                {
                    Vector3 perp = Vector3.Cross(Vector3.up, dir).normalized * 13f;
                    GameObject towerL = (GameObject)PrefabUtility.InstantiatePrefab(manaTowerPrefab, parent);
                    towerL.name = $"ManaTower_{names[i]}_Left";
                    towerL.transform.position = gatePos - perp;
                    towerL.transform.rotation = Quaternion.Euler(0f, ang, 0f);
                    if (manaTowerMesh != null)
                    {
                        var mf = towerL.GetComponent<MeshFilter>();
                        if (mf != null) mf.sharedMesh = manaTowerMesh;
                    }
                    ApplyMetropolisMaterials(towerL);
                    AddMeshCollidersRecursive(towerL);

                    GameObject towerR = (GameObject)PrefabUtility.InstantiatePrefab(manaTowerPrefab, parent);
                    towerR.name = $"ManaTower_{names[i]}_Right";
                    towerR.transform.position = gatePos + perp;
                    towerR.transform.rotation = Quaternion.Euler(0f, ang, 0f);
                    if (manaTowerMesh != null)
                    {
                        var mf = towerR.GetComponent<MeshFilter>();
                        if (mf != null) mf.sharedMesh = manaTowerMesh;
                    }
                    ApplyMetropolisMaterials(towerR);
                    AddMeshCollidersRecursive(towerR);
                }

                if (bridgePrefab != null)
                {
                    GameObject bridgeObj = (GameObject)PrefabUtility.InstantiatePrefab(bridgePrefab, parent);
                    bridgeObj.name = $"MoatBridge_{names[i]}";
                    bridgeObj.transform.position = new Vector3(bridgePos.x, Tier1Height, bridgePos.z);
                    bridgeObj.transform.rotation = Quaternion.Euler(0f, ang - 90f, 0f);
                    bridgeObj.transform.localScale = new Vector3(2.65f, 1.15f, 2.85f);
                    if (bridgeMesh != null)
                    {
                        var mf = bridgeObj.GetComponentInChildren<MeshFilter>();
                        if (mf != null) mf.sharedMesh = bridgeMesh;
                    }
                    ApplyMetropolisMaterials(bridgeObj);
                    AddMeshCollidersRecursive(bridgeObj);
                }
            }
        }

        // =========================================================================
        // BLUEPRINT DISTRICT POPULATION (DENSE POPULATION & OPEN WALK-IN DOORWAYS)
        // =========================================================================

        private static void PopulateBlueprintDistricts(Transform parent)
        {
            var cathedralPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Metropolis/Landmark_CathedralGothic_OpenDoors.prefab")
                ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_CathedralGothic_OpenDoors.fbx");
            var arcaneSpirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_ArcaneSpire_MagesQuarter.fbx");
            var hoverPadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_HoverPad_Citadel.fbx");
            var guildhallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Guildhall_Grand.fbx");
            var resHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_House_Residential.fbx");
            var lowHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_House_LowDistrict.fbx");
            var forgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Blacksmith_OpenForge.fbx");
            var bakeryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Bakery_Shop.fbx");
            var magicStorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_MagicItemStore.fbx");
            var enchanterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_EnchanterWorkshop.fbx");
            var tavernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Tavern_Open.fbx");
            var innPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Inn_Lodge.fbx");
            var bazaarStandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_BazaarStand_Street.fbx");

            // ---------------------------------------------------------------------
            // TIER 4: INNER CITADEL (+18m, R=0-75m)
            // ---------------------------------------------------------------------
            GameObject tier4Root = new GameObject("Tier4_Citadel_District");
            tier4Root.transform.SetParent(parent, false);

            if (cathedralPrefab != null)
            {
                GameObject cathedral = (GameObject)PrefabUtility.InstantiatePrefab(cathedralPrefab, tier4Root.transform);
                cathedral.name = "The_Grand_Cathedral_MagicalHold";
                cathedral.transform.position = new Vector3(0f, Tier4Height + 0.29f, 20f);
                cathedral.transform.rotation = Quaternion.Euler(90f, 180f, 0f);
                cathedral.transform.localScale = Vector3.one;
                ApplyMetropolisMaterials(cathedral);
                AddMeshCollidersRecursive(cathedral);
            }

            if (arcaneSpirePrefab != null)
            {
                GameObject spire = (GameObject)PrefabUtility.InstantiatePrefab(arcaneSpirePrefab, tier4Root.transform);
                spire.name = "MagesQuarter_ArcaneSpire";
                spire.transform.position = new Vector3(-35f, Tier4Height, 35f);
                spire.transform.rotation = Quaternion.identity;
                spire.transform.localScale = Vector3.one;
                ApplyMetropolisMaterials(spire);
                AddMeshCollidersRecursive(spire);

                GameObject spireLight = new GameObject("Spire_Beacon_Light");
                spireLight.transform.SetParent(spire.transform, false);
                spireLight.transform.localPosition = new Vector3(0f, 28f, 0f);
                var sl = spireLight.AddComponent<Light>();
                sl.type = LightType.Point;
                sl.color = new Color(0.1f, 0.85f, 1.0f);
                sl.range = 35f;
                sl.intensity = 5.0f;
            }

            if (hoverPadPrefab != null)
            {
                float[] padAngles = new float[] { 25f, 65f, 115f, 205f, 245f, 295f };
                for (int p = 0; p < padAngles.Length; p++)
                {
                    float rad = Mathf.Deg2Rad * padAngles[p];
                    Vector3 pos = new Vector3(Mathf.Sin(rad) * 48f, Tier4Height, Mathf.Cos(rad) * 48f);
                    GameObject pad = (GameObject)PrefabUtility.InstantiatePrefab(hoverPadPrefab, tier4Root.transform);
                    pad.name = $"Citadel_HoverPad_{p + 1}";
                    pad.transform.position = pos;
                    pad.transform.rotation = Quaternion.Euler(0f, padAngles[p], 0f);
                    ApplyMetropolisMaterials(pad);
                    AddMeshCollidersRecursive(pad);
                }
            }

            SetupSpireWaypoint(tier4Root.transform, new Vector3(0f, Tier4Height, -22f));

            // ---------------------------------------------------------------------
            // TIER 3: UPPER RING (+12m, R=75-155m)
            // ---------------------------------------------------------------------
            GameObject tier3Root = new GameObject("Tier3_Upper_Districts");
            tier3Root.transform.SetParent(parent, false);

            // A. Residential Districts (West / North-West: Angles 195 to 345 deg)
            if (resHousePrefab != null)
            {
                float[] houseAngles = new float[] { 200f, 212f, 228f, 240f, 252f, 264f, 276f, 288f, 300f, 312f, 328f, 340f };
                float[] resRadii = new float[] { 95f, 115f, 135f };
                int hId = 1;
                foreach (var r in resRadii)
                {
                    foreach (var ang in houseAngles)
                    {
                        float rad = Mathf.Deg2Rad * ang;
                        Vector3 pos = new Vector3(Mathf.Sin(rad) * r, Tier3Height, Mathf.Cos(rad) * r);
                        GameObject house = (GameObject)PrefabUtility.InstantiatePrefab(resHousePrefab, tier3Root.transform);
                        house.name = $"UpperDistrict_Residence_{hId++}";
                        house.transform.position = pos;
                        house.transform.rotation = r <= 115f ? Quaternion.Euler(0f, ang, 0f) : Quaternion.Euler(0f, ang + 180f, 0f);
                        house.transform.localScale = Vector3.one;
                        ApplyMetropolisMaterials(house);
                        AddMeshCollidersRecursive(house);
                    }
                }
            }

            // B. Military Barracks & Sparring Arena (North-East: Angles 15 to 75 deg)
            var sparringPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_SparringArena_TargetsDummies.fbx");
            var armoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_GuildArmory.fbx");
            if (sparringPrefab != null)
            {
                GameObject arena = (GameObject)PrefabUtility.InstantiatePrefab(sparringPrefab, tier3Root.transform);
                arena.name = "Military_SparringArena";
                arena.transform.position = new Vector3(85f, Tier3Height, 85f);
                arena.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                arena.transform.localScale = Vector3.one * 1.5f;
                AddMeshCollidersRecursive(arena);
            }
            if (armoryPrefab != null)
            {
                GameObject armory = (GameObject)PrefabUtility.InstantiatePrefab(armoryPrefab, tier3Root.transform);
                armory.name = "Military_Barracks_Armory";
                armory.transform.position = new Vector3(65f, Tier3Height, 105f);
                armory.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                armory.transform.localScale = Vector3.one * 1.2f;
                AddMeshCollidersRecursive(armory);
            }

            // C. High-End Commercials / Grand Guildhall (East: Angles 75 to 160 deg)
            if (guildhallPrefab != null)
            {
                GameObject guildhall = (GameObject)PrefabUtility.InstantiatePrefab(guildhallPrefab, tier3Root.transform);
                guildhall.name = "HighEnd_GrandGuildhall";
                guildhall.transform.position = new Vector3(115f, Tier3Height, 15f);
                guildhall.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                guildhall.transform.localScale = Vector3.one;
                ApplyMetropolisMaterials(guildhall);
                AddMeshCollidersRecursive(guildhall);
            }
            var academyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Metropolis_AcademyBuildings.fbx");
            if (academyPrefab != null)
            {
                GameObject academy = (GameObject)PrefabUtility.InstantiatePrefab(academyPrefab, tier3Root.transform);
                academy.name = "HighEnd_ArcaneEmporiumAcademy";
                academy.transform.position = new Vector3(105f, Tier3Height, -45f);
                academy.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
                academy.transform.localScale = Vector3.one * 1.25f;
                AddMeshCollidersRecursive(academy);
            }

            // ---------------------------------------------------------------------
            // TIER 2: MID RING (+6m, R=155-235m)
            // ---------------------------------------------------------------------
            GameObject tier2Root = new GameObject("Tier2_Commercial_Trade_Sector");
            tier2Root.transform.SetParent(parent, false);

            // A. Commercial & Trade Shops (North, NE, East: Angles 345 deg to 105 deg)
            // Staggered along concentric street R=175m and R=215m
            float[] tradeAngles = new float[] { 348f, 15f, 28f, 58f, 72f, 85f, 98f };
            for (int t = 0; t < tradeAngles.Length; t++)
            {
                float ang = tradeAngles[t];
                float rad = Mathf.Deg2Rad * ang;
                Vector3 posInner = new Vector3(Mathf.Sin(rad) * 175f, Tier2Height, Mathf.Cos(rad) * 175f);
                Vector3 posOuter = new Vector3(Mathf.Sin(rad) * 215f, Tier2Height, Mathf.Cos(rad) * 215f);
                Quaternion rotInner = Quaternion.Euler(0f, ang, 0f);
                Quaternion rotOuter = Quaternion.Euler(0f, ang + 180f, 0f);

                // Alternating shop types
                switch (t % 6)
                {
                    case 0:
                        if (bakeryPrefab != null)
                        {
                            var b = (GameObject)PrefabUtility.InstantiatePrefab(bakeryPrefab, tier2Root.transform);
                            b.name = $"TradeRow_Bakery_{t + 1}";
                            b.transform.position = posInner;
                            b.transform.rotation = rotInner;
                            b.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(b);
                            AddMeshCollidersRecursive(b);
                        }
                        if (forgePrefab != null)
                        {
                            var f = (GameObject)PrefabUtility.InstantiatePrefab(forgePrefab, tier2Root.transform);
                            f.name = $"TradeRow_Blacksmith_{t + 1}";
                            f.transform.position = posOuter;
                            f.transform.rotation = rotOuter;
                            f.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(f);
                            AddMeshCollidersRecursive(f);
                        }
                        break;
                    case 1:
                        if (magicStorePrefab != null)
                        {
                            var m = (GameObject)PrefabUtility.InstantiatePrefab(magicStorePrefab, tier2Root.transform);
                            m.name = $"TradeRow_MagicStore_{t + 1}";
                            m.transform.position = posInner;
                            m.transform.rotation = rotInner;
                            m.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(m);
                            AddMeshCollidersRecursive(m);
                        }
                        if (enchanterPrefab != null)
                        {
                            var e = (GameObject)PrefabUtility.InstantiatePrefab(enchanterPrefab, tier2Root.transform);
                            e.name = $"TradeRow_Enchanter_{t + 1}";
                            e.transform.position = posOuter;
                            e.transform.rotation = rotOuter;
                            e.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(e);
                            AddMeshCollidersRecursive(e);
                        }
                        break;
                    case 2:
                        if (tavernPrefab != null)
                        {
                            var tv = (GameObject)PrefabUtility.InstantiatePrefab(tavernPrefab, tier2Root.transform);
                            tv.name = $"TradeRow_Tavern_{t + 1}";
                            tv.transform.position = posInner;
                            tv.transform.rotation = rotInner;
                            tv.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(tv);
                            AddMeshCollidersRecursive(tv);
                        }
                        if (innPrefab != null)
                        {
                            var inn = (GameObject)PrefabUtility.InstantiatePrefab(innPrefab, tier2Root.transform);
                            inn.name = $"TradeRow_Inn_{t + 1}";
                            inn.transform.position = posOuter;
                            inn.transform.rotation = rotOuter;
                            inn.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(inn);
                            AddMeshCollidersRecursive(inn);
                        }
                        break;
                    case 3:
                        if (forgePrefab != null)
                        {
                            var f = (GameObject)PrefabUtility.InstantiatePrefab(forgePrefab, tier2Root.transform);
                            f.name = $"TradeRow_Blacksmith_{t + 1}";
                            f.transform.position = posInner;
                            f.transform.rotation = rotInner;
                            f.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(f);
                            AddMeshCollidersRecursive(f);
                        }
                        if (bakeryPrefab != null)
                        {
                            var b = (GameObject)PrefabUtility.InstantiatePrefab(bakeryPrefab, tier2Root.transform);
                            b.name = $"TradeRow_Bakery_{t + 1}";
                            b.transform.position = posOuter;
                            b.transform.rotation = rotOuter;
                            b.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(b);
                            AddMeshCollidersRecursive(b);
                        }
                        break;
                    case 4:
                        if (enchanterPrefab != null)
                        {
                            var e = (GameObject)PrefabUtility.InstantiatePrefab(enchanterPrefab, tier2Root.transform);
                            e.name = $"TradeRow_Enchanter_{t + 1}";
                            e.transform.position = posInner;
                            e.transform.rotation = rotInner;
                            e.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(e);
                            AddMeshCollidersRecursive(e);
                        }
                        if (magicStorePrefab != null)
                        {
                            var m = (GameObject)PrefabUtility.InstantiatePrefab(magicStorePrefab, tier2Root.transform);
                            m.name = $"TradeRow_MagicStore_{t + 1}";
                            m.transform.position = posOuter;
                            m.transform.rotation = rotOuter;
                            m.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(m);
                            AddMeshCollidersRecursive(m);
                        }
                        break;
                    case 5:
                        if (innPrefab != null)
                        {
                            var inn = (GameObject)PrefabUtility.InstantiatePrefab(innPrefab, tier2Root.transform);
                            inn.name = $"TradeRow_Inn_{t + 1}";
                            inn.transform.position = posInner;
                            inn.transform.rotation = rotInner;
                            inn.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(inn);
                            AddMeshCollidersRecursive(inn);
                        }
                        if (tavernPrefab != null)
                        {
                            var tv = (GameObject)PrefabUtility.InstantiatePrefab(tavernPrefab, tier2Root.transform);
                            tv.name = $"TradeRow_Tavern_{t + 1}";
                            tv.transform.position = posOuter;
                            tv.transform.rotation = rotOuter;
                            tv.transform.localScale = Vector3.one;
                            ApplyMetropolisMaterials(tv);
                            AddMeshCollidersRecursive(tv);
                        }
                        break;
                }
            }

            // B. Tents & Local Bazaars (South & South-West: Angles 185 to 265 deg)
            if (bazaarStandPrefab != null)
            {
                float[] bzAngles = new float[] { 188f, 196f, 204f, 212f, 220f, 232f, 240f, 248f, 256f, 264f };
                float[] bzRadii = new float[] { 175f, 215f };
                int bId = 1;
                foreach (var r in bzRadii)
                {
                    foreach (var ang in bzAngles)
                    {
                        float rad = Mathf.Deg2Rad * ang;
                        Vector3 pos = new Vector3(Mathf.Sin(rad) * r, Tier2Height, Mathf.Cos(rad) * r);
                        GameObject stall = (GameObject)PrefabUtility.InstantiatePrefab(bazaarStandPrefab, tier2Root.transform);
                        stall.name = $"MidTier_BazaarStand_{bId++}";
                        stall.transform.position = pos;
                        stall.transform.rotation = r <= 195f ? Quaternion.Euler(0f, ang, 0f) : Quaternion.Euler(0f, ang + 180f, 0f);
                        stall.transform.localScale = Vector3.one * 1.15f;
                        ApplyMetropolisMaterials(stall);
                        AddMeshCollidersRecursive(stall);
                    }
                }
            }

            // C. Utility & Power Sector (South-East: Angles 105 to 165 deg)
            BuildMonumentalClockTower(tier2Root.transform, new Vector3(145f, Tier2Height, -135f));

            // ---------------------------------------------------------------------
            // TIER 1: OUTER GROUND RING (+0m, R=235-300m)
            // ---------------------------------------------------------------------
            GameObject tier1Root = new GameObject("Tier1_Ground_Districts");
            tier1Root.transform.SetParent(parent, false);

            // A. Outdoor Bazaars & Street Markets (Lining South & SW boulevards R=255m and R=280m)
            if (bazaarStandPrefab != null)
            {
                float[] outBzAngles = new float[] { 165f, 172f, 180f, 188f, 196f, 204f, 212f, 220f, 232f, 240f, 248f };
                int oId = 1;
                foreach (var ang in outBzAngles)
                {
                    float rad = Mathf.Deg2Rad * ang;
                    Vector3 pos = new Vector3(Mathf.Sin(rad) * 265f, Tier1Height, Mathf.Cos(rad) * 265f);
                    GameObject stall = (GameObject)PrefabUtility.InstantiatePrefab(bazaarStandPrefab, tier1Root.transform);
                    stall.name = $"Street_OutdoorBazaar_{oId++}";
                    stall.transform.position = pos;
                    stall.transform.rotation = Quaternion.Euler(0f, ang, 0f);
                    stall.transform.localScale = Vector3.one * 1.2f;
                    ApplyMetropolisMaterials(stall);
                    AddMeshCollidersRecursive(stall);
                }
            }

            // B. Low-District Housing (West & North-West: Angles 255 to 345 deg)
            if (lowHousePrefab != null)
            {
                float[] lowAngles = new float[] { 255f, 268f, 280f, 292f, 305f, 318f, 330f, 342f };
                float[] lowRadii = new float[] { 255f, 280f };
                int cId = 1;
                foreach (var r in lowRadii)
                {
                    foreach (var ang in lowAngles)
                    {
                        float rad = Mathf.Deg2Rad * ang;
                        Vector3 pos = new Vector3(Mathf.Sin(rad) * r, Tier1Height, Mathf.Cos(rad) * r);
                        GameObject cottage = (GameObject)PrefabUtility.InstantiatePrefab(lowHousePrefab, tier1Root.transform);
                        cottage.name = $"LowDistrict_Cottage_{cId++}";
                        cottage.transform.position = pos;
                        cottage.transform.rotation = r <= 265f ? Quaternion.Euler(0f, ang, 0f) : Quaternion.Euler(0f, ang + 180f, 0f);
                        cottage.transform.localScale = Vector3.one;
                        ApplyMetropolisMaterials(cottage);
                        AddMeshCollidersRecursive(cottage);
                    }
                }
            }

            // C. Mount Stables (Tier 1 South-East)
            BuildRoyalMountStables(tier1Root.transform, new Vector3(185f, Tier1Height, -200f));
        }

        // =========================================================================
        // OUTERWILDS BIOMES (MATCHING CONCEPT ART & OVERWORLD MAP)
        // =========================================================================

        private static void PopulateOuterwildsBiomes(Transform parent)
        {
            var pineTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/OverworldPieces/Prop_Fantasy_Tree.fbx");
            var matPineTree = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/OverworldPieces/Materials/M_Prop_Fantasy_Tree.mat");
            var matRedRune = GetMat("Mat_World_PortalEnergy");

            float[] forestAngles = new float[] { 180f, 225f, 270f, 315f };
            string[] forestNames = new string[] { "South_Mistwood", "SouthWest_Grove", "West_Deepwoods", "NorthWest_HighForest" };

            for (int f = 0; f < forestAngles.Length; f++)
            {
                float fAng = forestAngles[f];
                float fRad = Mathf.Deg2Rad * fAng;
                Vector3 center = new Vector3(Mathf.Sin(fRad) * 540f, Tier1Height, Mathf.Cos(fRad) * 540f);

                GameObject grove = new GameObject($"Mistwood_Grove_{forestNames[f]}");
                grove.transform.SetParent(parent, false);

                if (pineTreePrefab != null)
                {
                    for (int t = 0; t < 24; t++)
                    {
                        float tAng = t * 15f;
                        float tDist = Random.Range(35f, 130f);
                        Vector3 tOffset = new Vector3(Mathf.Sin(tAng) * tDist, 0f, Mathf.Cos(tAng) * tDist);
                        GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(pineTreePrefab, grove.transform);
                        tree.name = $"Pine_{t + 1}";
                        float scale = Random.Range(0.08f, 0.16f);
                        tree.transform.localScale = Vector3.one * scale;
                        float yaw = Random.Range(0f, 360f);
                        tree.transform.rotation = Quaternion.Euler(-90f, yaw, 0f);
                        // The mesh bottom is at local Z = -50.11. In upright (-90 X rot), local Z points along world +Y.
                        // Pivot needs to be lifted by ~50.11 * scale so trunk base rests on the ground.
                        float groundY = Tier1Height - 0.2f;
                        tree.transform.position = new Vector3(center.x + tOffset.x, groundY + 50.11f * scale, center.z + tOffset.z);

                        if (matPineTree != null)
                        {
                            foreach (var r in tree.GetComponentsInChildren<Renderer>(true))
                            {
                                r.sharedMaterial = matPineTree;
                            }
                        }

                        AddMeshCollidersRecursive(tree);
                    }
                }

                BuildCrimsonRunicHenge(grove.transform, center, matRedRune);
            }

            Vector3 volcanicCenter = new Vector3(Mathf.Sin(Mathf.Deg2Rad * 50f) * 580f, 25f, Mathf.Cos(Mathf.Deg2Rad * 50f) * 580f);
            BuildVolcanicRiftCore(parent, volcanicCenter);
        }

        private static void BuildCrimsonRunicHenge(Transform parent, Vector3 center, Material matRune)
        {
            GameObject hengeRoot = new GameObject("Crimson_Runic_StoneHenge");
            hengeRoot.transform.SetParent(parent, false);
            hengeRoot.transform.position = center;

            var matStone = GetMat("Mat_World_DungeonStone");

            int pillars = 8;
            for (int i = 0; i < pillars; i++)
            {
                float ang = i * (360f / pillars);
                float rad = Mathf.Deg2Rad * ang;
                Vector3 pos = new Vector3(Mathf.Sin(rad) * 12f, 3f, Mathf.Cos(rad) * 12f);

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Megalith_{i + 1}";
                pillar.transform.SetParent(hengeRoot.transform, false);
                pillar.transform.localPosition = pos;
                pillar.transform.localScale = new Vector3(1.8f, 6.0f, 1.2f);
                pillar.transform.rotation = Quaternion.Euler(0f, ang, 0f);
                if (matStone != null) pillar.GetComponent<Renderer>().sharedMaterial = matStone;
            }

            GameObject well = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            well.name = "Crimson_Portal_Core";
            well.transform.SetParent(hengeRoot.transform, false);
            well.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            well.transform.localScale = new Vector3(10f, 0.2f, 10f);
            if (matRune != null) well.GetComponent<Renderer>().sharedMaterial = matRune;

            GameObject rLight = new GameObject("Crimson_Aura_Light");
            rLight.transform.SetParent(hengeRoot.transform, false);
            rLight.transform.localPosition = new Vector3(0f, 4f, 0f);
            var rl = rLight.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = new Color(1.0f, 0.15f, 0.25f);
            rl.range = 30f;
            rl.intensity = 5.0f;
        }

        private static void BuildVolcanicRiftCore(Transform parent, Vector3 center)
        {
            GameObject riftRoot = new GameObject("Volcanic_PurpleRift_Core");
            riftRoot.transform.SetParent(parent, false);
            riftRoot.transform.position = center;

            var matVolcanicRift = GetMat("Mat_Volcanic_BasaltRift");

            GameObject crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.name = "Rift_Crater_Plate";
            crater.transform.SetParent(riftRoot.transform, false);
            crater.transform.localPosition = Vector3.zero;
            crater.transform.localScale = new Vector3(35f, 2.0f, 35f);
            if (matVolcanicRift != null) crater.GetComponent<Renderer>().sharedMaterial = matVolcanicRift;

            GameObject pLight = new GameObject("Purple_Rift_Light");
            pLight.transform.SetParent(riftRoot.transform, false);
            pLight.transform.localPosition = new Vector3(0f, 12f, 0f);
            var pl = pLight.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(0.85f, 0.15f, 1.0f);
            pl.range = 65f;
            pl.intensity = 8.0f;
        }

        // =========================================================================
        // TOWN LIFE, CIVIC PROPS & ATMOSPHERE
        // =========================================================================

        private static void PopulateTownLifeAndFlora(Transform parent)
        {
            var wolfCitizenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Character_Beastfolk_Wolf.fbx");
            var foxCitizenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Character_Beastfolk_Fox.fbx");
            var kidsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Character_RunningKids.fbx");
            var bardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Character_BardLute.fbx");

            if (bardPrefab != null)
            {
                GameObject bard = (GameObject)PrefabUtility.InstantiatePrefab(bardPrefab, parent);
                bard.name = "NPC_PlazaBard";
                bard.transform.position = new Vector3(-15f, Tier3Height, 25f);
                bard.transform.rotation = Quaternion.Euler(0f, 135f, 0f);
                bard.transform.localScale = Vector3.one * 1.1f;
                AddMeshCollidersRecursive(bard);
            }

            if (wolfCitizenPrefab != null)
            {
                GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(wolfCitizenPrefab, parent);
                guard.name = "NPC_CitadelGuard_Wolf";
                guard.transform.position = new Vector3(15f, Tier4Height, -15f);
                guard.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                guard.transform.localScale = Vector3.one * 1.15f;
                AddMeshCollidersRecursive(guard);
            }

            if (foxCitizenPrefab != null)
            {
                GameObject merchant = (GameObject)PrefabUtility.InstantiatePrefab(foxCitizenPrefab, parent);
                merchant.name = "NPC_MarketMerchant_Fox";
                merchant.transform.position = new Vector3(-25f, Tier2Height, -185f);
                merchant.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                merchant.transform.localScale = Vector3.one * 1.1f;
                AddMeshCollidersRecursive(merchant);
            }

            if (kidsPrefab != null)
            {
                GameObject kids = (GameObject)PrefabUtility.InstantiatePrefab(kidsPrefab, parent);
                kids.name = "NPC_StreetKids_Playing";
                kids.transform.position = new Vector3(45f, Tier1Height, -235f);
                kids.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                kids.transform.localScale = Vector3.one * 1.0f;
                AddMeshCollidersRecursive(kids);
            }
        }

        private static void PopulateCityPropsAndDetails(Transform parent)
        {
            var oakTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Foliage_OverhangingGrandOak.fbx");
            if (oakTreePrefab != null)
            {
                float[] oakAngles = new float[] { 40f, 130f, 220f, 310f };
                for (int i = 0; i < 4; i++)
                {
                    float rad = Mathf.Deg2Rad * oakAngles[i];
                    Vector3 pos = new Vector3(Mathf.Sin(rad) * 115f, Tier3Height, Mathf.Cos(rad) * 115f);
                    GameObject oak = (GameObject)PrefabUtility.InstantiatePrefab(oakTreePrefab, parent);
                    oak.name = $"CivicGrandOak_{i + 1}";
                    oak.transform.position = pos;
                    oak.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    oak.transform.localScale = Vector3.one * 1.25f;
                    AddMeshCollidersRecursive(oak);
                }
            }

            var lanternStringPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_PaperLanterns_String.fbx");
            if (lanternStringPrefab != null)
            {
                for (int s = 0; s < 6; s++)
                {
                    float ang = 170f + s * 12f;
                    float rad = Mathf.Deg2Rad * ang;
                    Vector3 pos = new Vector3(Mathf.Sin(rad) * 195f, Tier2Height + 3.5f, Mathf.Cos(rad) * 195f);
                    GameObject str = (GameObject)PrefabUtility.InstantiatePrefab(lanternStringPrefab, parent);
                    str.name = $"Bazaar_LanternString_{s + 1}";
                    str.transform.position = pos;
                    str.transform.rotation = Quaternion.Euler(0f, ang, 0f);
                    str.transform.localScale = Vector3.one * 1.2f;
                }
            }
        }

        private static void BuildMonumentalClockTower(Transform parent, Vector3 pos)
        {
            GameObject towerRoot = new GameObject("Landmark_MonumentalClockTower");
            towerRoot.transform.SetParent(parent, false);
            towerRoot.transform.position = pos;

            var matStone = GetMat("M_Metro_CathedralStone");
            var matBrass = GetMat("M_Metro_ClockworkBrass");

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Tower_Shaft";
            shaft.transform.SetParent(towerRoot.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 13f, 0f);
            shaft.transform.localScale = new Vector3(8f, 26f, 8f);
            if (matStone != null) shaft.GetComponent<Renderer>().sharedMaterial = matStone;

            GameObject dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dial.name = "Clock_Dial";
            dial.transform.SetParent(towerRoot.transform, false);
            dial.transform.localPosition = new Vector3(0f, 20f, -4.1f);
            dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dial.transform.localScale = new Vector3(4.8f, 0.2f, 4.8f);
            if (matBrass != null) dial.GetComponent<Renderer>().sharedMaterial = matBrass;

            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "Belfry_Spire";
            spire.transform.SetParent(towerRoot.transform, false);
            spire.transform.localPosition = new Vector3(0f, 29f, 0f);
            spire.transform.localScale = new Vector3(5f, 6f, 5f);
            if (matStone != null) spire.GetComponent<Renderer>().sharedMaterial = matStone;

            GameObject dialLight = new GameObject("Clock_Dial_Light");
            dialLight.transform.SetParent(towerRoot.transform, false);
            dialLight.transform.localPosition = new Vector3(0f, 20f, -6f);
            var l = dialLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.85f, 0.4f);
            l.range = 18f;
            l.intensity = 3.5f;
        }

        private static void BuildRoyalMountStables(Transform parent, Vector3 pos)
        {
            GameObject stablesRoot = new GameObject("Landmark_RoyalMountStables");
            stablesRoot.transform.SetParent(parent, false);
            stablesRoot.transform.position = pos;
            stablesRoot.transform.rotation = Quaternion.Euler(0f, 300f, 0f);

            var wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Mount_ArmoredWolfBeast.fbx");
            var chocoboPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Mount_FeatheredChocoboBird.fbx");
            var reptilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Mount_HornedReptilePackBeast.fbx");

            if (wolfPrefab != null)
            {
                GameObject wolf = (GameObject)PrefabUtility.InstantiatePrefab(wolfPrefab, stablesRoot.transform);
                wolf.name = "ShowcaseMount_ArmoredWolf";
                wolf.transform.localPosition = new Vector3(-5f, -0.6f, 0f);
                wolf.transform.localRotation = Quaternion.identity;
                wolf.transform.localScale = Vector3.one * 1.3f;
                AddMeshCollidersRecursive(wolf);
            }
            if (chocoboPrefab != null)
            {
                GameObject chocobo = (GameObject)PrefabUtility.InstantiatePrefab(chocoboPrefab, stablesRoot.transform);
                chocobo.name = "ShowcaseMount_FeatheredBird";
                chocobo.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                chocobo.transform.localRotation = Quaternion.identity;
                chocobo.transform.localScale = Vector3.one * 1.3f;
                AddMeshCollidersRecursive(chocobo);
            }
            if (reptilePrefab != null)
            {
                GameObject reptile = (GameObject)PrefabUtility.InstantiatePrefab(reptilePrefab, stablesRoot.transform);
                reptile.name = "ShowcaseMount_HornedReptile";
                reptile.transform.localPosition = new Vector3(5f, -0.6f, 0f);
                reptile.transform.localRotation = Quaternion.identity;
                reptile.transform.localScale = Vector3.one * 1.3f;
                AddMeshCollidersRecursive(reptile);
            }
        }

        private static void SetupSpireWaypoint(Transform parent, Vector3 pos)
        {
            GameObject waypoint = new GameObject("SpireWaypoint_CitadelCenter");
            waypoint.transform.SetParent(parent, false);
            waypoint.transform.position = pos;

            var waypointComp = waypoint.AddComponent<OverworldSpireWaypoint>();
            var boxCol = waypoint.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(10f, 5f, 10f);

            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crystal.name = "Resonance_CelestialCrystal";
            crystal.transform.SetParent(waypoint.transform, false);
            crystal.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            crystal.transform.localScale = new Vector3(1.2f, 3.2f, 1.2f);
            var cCol = crystal.GetComponent<Collider>();
            if (cCol != null) Object.DestroyImmediate(cCol);

            var matCrystal = GetMat("M_Metro_RuneGlow");
            if (matCrystal != null) crystal.GetComponent<Renderer>().sharedMaterial = matCrystal;

            GameObject runeDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeDisc.name = "Resonance_RuneDisc";
            runeDisc.transform.SetParent(waypoint.transform, false);
            runeDisc.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            runeDisc.transform.localScale = new Vector3(8.5f, 0.05f, 8.5f);
            var rCol = runeDisc.GetComponent<Collider>();
            if (rCol != null) Object.DestroyImmediate(rCol);
            if (matCrystal != null) runeDisc.GetComponent<Renderer>().sharedMaterial = matCrystal;

            GameObject wLight = new GameObject("Waypoint_Beacon_Light");
            wLight.transform.SetParent(waypoint.transform, false);
            wLight.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            var wl = wLight.AddComponent<Light>();
            wl.type = LightType.Point;
            wl.color = new Color(0.2f, 0.85f, 1.0f);
            wl.range = 18f;
            wl.intensity = 3.5f;
        }

        private static void SetupLightingAndGridLamps(Transform parent)
        {
            var sunObj = GameObject.Find("Directional Light");
            if (sunObj == null)
            {
                sunObj = new GameObject("Directional Light");
            }
            sunObj.transform.SetParent(parent, false);
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var lightComp = sunObj.GetComponent<Light>();
            if (lightComp == null)
            {
                lightComp = sunObj.AddComponent<Light>();
            }
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(1.0f, 0.96f, 0.90f);
            lightComp.intensity = 1.15f;
            lightComp.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.44f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.28f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.16f, 0.14f);
        }

        private static void SetupAtmosphereAndAudio(Transform parent)
        {
            GameObject audioObj = new GameObject("Audio_City_Ambience");
            audioObj.transform.SetParent(parent, false);
            var src = audioObj.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = true;
            src.volume = 0.45f;
        }

        private static void SetupPlayerHero(Transform parent)
        {
            Vector3 spawnPos = new Vector3(0f, Tier4Height + 0.1f, -10f);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = spawnPos;
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                return;
            }

            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/PlayerCharacter.prefab");
            if (heroPrefab != null)
            {
                GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, parent);
                hero.name = "PlayerHero_Overworld";
                hero.transform.position = spawnPos;
                hero.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        private static void AddMeshCollidersRecursive(GameObject go)
        {
            var meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                var col = mf.gameObject.GetComponent<Collider>();
                if (col == null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }
    }
}
#endif
