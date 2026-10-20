using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Comprehensive Metropolis Street Detailing System.
    /// Builds raised stone sidewalks, 32 intersection plazas with decorative medallions,
    /// district-specific sidewalk scenes (Market, Tavern, Forge, Arcane, Residential),
    /// overhead festoon paper lanterns, town wells, notice boards, street foliage, and street life.
    /// </summary>
    public static class EnrichMetropolisStreets
    {
        private const string STREET_DETAILS_ROOT_NAME = "07_Detailed_Street_Scenes";

        [MenuItem("Antigravity/World/Enrich Metropolis Streets Detailed")]
        public static void ExecuteStreetEnrichment()
        {
            Debug.Log("<color=cyan><b>[Street Details] Starting Comprehensive Metropolis Street Enrichment...</b></color>");

            // 1. Locate or create infrastructure roots
            GameObject infraRoot = GameObject.Find("METROPOLIS_INFRASTRUCTURE");
            if (infraRoot == null)
            {
                var envRoot = GameObject.Find("ENVIRONMENT_ROOT") ?? new GameObject("ENVIRONMENT_ROOT");
                infraRoot = new GameObject("METROPOLIS_INFRASTRUCTURE");
                infraRoot.transform.SetParent(envRoot.transform, false);
            }

            // Clean previous detail container
            Transform existing = infraRoot.transform.Find(STREET_DETAILS_ROOT_NAME);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject detailsRoot = new GameObject(STREET_DETAILS_ROOT_NAME);
            detailsRoot.transform.SetParent(infraRoot.transform, false);

            Transform sidewalksParent = new GameObject("01_Raised_Sidewalks").transform;
            sidewalksParent.SetParent(detailsRoot.transform, false);

            Transform intersectionsParent = new GameObject("02_Intersection_Plazas").transform;
            intersectionsParent.SetParent(detailsRoot.transform, false);

            Transform districtPropsParent = new GameObject("03_District_Street_Life").transform;
            districtPropsParent.SetParent(detailsRoot.transform, false);

            Transform festoonsParent = new GameObject("04_Overhead_Festoon_Lanterns").transform;
            festoonsParent.SetParent(detailsRoot.transform, false);

            Transform streetFoliageParent = new GameObject("05_Curb_Foliage_And_Moss").transform;
            streetFoliageParent.SetParent(detailsRoot.transform, false);

            // 2. Load / Create Materials
            Material sidewalkMat = GetOrCreateSidewalkMaterial();
            Material medallionMat = GetOrCreateMedallionMaterial();

            // 3. Load Prefabs
            var props = LoadPropCatalog();

            System.Random rng = new System.Random(4282026);

            // 4. Build Raised Sidewalks and Intersections for the 8 Radial Boulevards
            BuildRadialBoulevardSidewalks(sidewalksParent, intersectionsParent, sidewalkMat, medallionMat, props, rng);

            // 5. Build Raised Sidewalks for the 4 Concentric Ring Avenues
            BuildRingAvenueSidewalks(sidewalksParent, sidewalkMat, rng);

            // 6. Populate District-Themed Sidewalk Scenes along the 8 Boulevards
            PopulateDistrictScenes(districtPropsParent, festoonsParent, streetFoliageParent, props, rng);

            // 7. Save and Refresh
            EditorUtility.SetDirty(detailsRoot);
            Debug.Log("<color=green><b>[Street Details] Complete! Successfully enriched all Metropolis streets with raised sidewalks, 32 intersection plazas, district scenes, overhead festoons, and foliage!</b></color>");
        }

        #region Material Creation
        private static Material GetOrCreateSidewalkMaterial()
        {
            string path = "Assets/Materials/M_CityStoneSidewalk.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.name = "M_CityStoneSidewalk";
                mat.color = new Color(0.88f, 0.85f, 0.80f, 1f);
                mat.SetFloat("_Smoothness", 0.18f);

                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Dirt & Stone/Dirt_Stone_Albedo.png")
                          ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Rock/Rock_Albedo.png");
                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Dirt & Stone/Dirt_Stone_Normal.png")
                          ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Rock/Rock_Normal.png");

                if (albedo != null)
                {
                    mat.SetTexture("_BaseMap", albedo);
                    mat.SetTextureScale("_BaseMap", new Vector2(0.4f, 0.4f));
                }
                if (normal != null)
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", normal);
                    mat.SetFloat("_BumpScale", 0.75f);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static Material GetOrCreateMedallionMaterial()
        {
            string path = "Assets/Materials/M_CityPlazaMedallion.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.name = "M_CityPlazaMedallion";
                mat.color = new Color(0.95f, 0.88f, 0.72f, 1f);
                mat.SetFloat("_Smoothness", 0.25f);

                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Cobblestone/Cobblestone_Albedo.png");
                if (albedo != null)
                {
                    mat.SetTexture("_BaseMap", albedo);
                    mat.SetTextureScale("_BaseMap", new Vector2(0.8f, 0.8f));
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
        #endregion

        #region Prop Catalog
        private class PropCatalog
        {
            public GameObject noticeBoard;
            public GameObject villageWell;
            public GameObject diningSet;
            public GameObject chalkboard;
            public GameObject pastryDisplay;
            public GameObject anvil;
            public GameObject coalBasin;
            public GameObject grindstone;
            public GameObject weaponRack;
            public GameObject firewood;
            public GameObject crystalPedestal;
            public GameObject spellLectern;
            public GameObject distillationBench;
            public GameObject floralPlanter;
            public GameObject grassClump;
            public GameObject paperLanterns;
            public GameObject streetLamp;
            public GameObject parkBench;
            public GameObject bazaarStand;
            public GameObject stallsCluster;
            public GameObject handcart;
            public GameObject beerBarrel;
            public GameObject crate;
            public GameObject sack;
            public GameObject bucket;
            public GameObject oldTable;
            public GameObject vase;

            // NPCs & Characters
            public GameObject npcBarnaby;
            public GameObject npcMargo;
            public GameObject npcZephyr;
            public GameObject charBard;
            public GameObject charKids;
            public GameObject charFox;
            public GameObject critterOwl;
            public GameObject critterFairy;
            public GameObject mountChocobo;

            // Flowers
            public GameObject[] flowerPrefabs;
        }

        private static PropCatalog LoadPropCatalog()
        {
            return new PropCatalog
            {
                noticeBoard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab"),
                villageWell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab"),
                diningSet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab"),
                chalkboard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Bakery_MenuChalkboard.prefab"),
                pastryDisplay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Bakery_PastryDisplay.prefab"),
                anvil = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Anvil_Stump.prefab"),
                coalBasin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_CoalBasin.prefab"),
                grindstone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Grindstone.prefab"),
                weaponRack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab"),
                firewood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab"),
                crystalPedestal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Wizard_CrystalPedestal.prefab"),
                spellLectern = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Wizard_SpellbookLectern.prefab"),
                distillationBench = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Alchemy_DistillationBench.prefab"),
                floralPlanter = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/FantasyFloralPlanter.fbx"),
                grassClump = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/FantasyLushGrassClump.fbx"),
                paperLanterns = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_PaperLanterns_String.fbx"),
                streetLamp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Urban_StreetLampPost.prefab"),
                parkBench = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_Park_Bench_01.prefab"),
                bazaarStand = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_BazaarStand_Street.fbx"),
                stallsCluster = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_MarketStalls_Cluster.fbx"),
                handcart = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Handcart.prefab"),
                beerBarrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BeerBarrelV1.prefab"),
                crate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/BoxV1.prefab"),
                sack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval props/Prefabs/SackV1.prefab"),
                bucket = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Wooden_Bucket_1.prefab"),
                oldTable = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Old_Table.prefab"),
                vase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Medieval Props Pack 01/Universal Render Pipeline(URP)/Prefabs/Vase_1.prefab"),

                npcBarnaby = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Barnaby.prefab"),
                npcMargo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Margo.prefab"),
                npcZephyr = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Zephyr.prefab"),
                charBard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Character_BardLute.fbx"),
                charKids = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Character_RunningKids.fbx"),
                charFox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Character_Beastfolk_Fox.fbx"),
                critterOwl = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Critter_PerchedOwl.fbx"),
                critterFairy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Critter_GlowingFairy.fbx"),
                mountChocobo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Mount_FeatheredChocoboBird.fbx"),

                flowerPrefabs = new GameObject[]
                {
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_AzureBells.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_CrimsonPoppies.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_GoldenMarigolds.prefab"),
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Flowers/PFB_FlowerCluster_MixedWildflowers.prefab")
                }
            };
        }
        #endregion

        #region Sidewalk & Intersection Mesh Construction
        private static void BuildRadialBoulevardSidewalks(
            Transform sidewalksParent, Transform intersectionsParent,
            Material sidewalkMat, Material medallionMat,
            PropCatalog props, System.Random rng)
        {
            string[] dirNames = new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

            // Tier sections: (R_start, R_end, Y_elev, RoadWidth, SidewalkWidth)
            (float r0, float r1, float y, float rw, float sw)[] tiers = new (float, float, float, float, float)[]
            {
                (12f,  84f,  80.08f, 7.5f, 2.8f), // Tier 4 (Citadel)
                (118f, 200f, 58.08f, 8.0f, 3.0f), // Tier 3 (Upper District)
                (234f, 314f, 38.08f, 8.5f, 3.2f), // Tier 2 (Mid District)
                (350f, 462f, 22.08f, 9.0f, 3.5f)  // Tier 1 (Lower District)
            };

            // Intersection radii with the 4 concentric rings
            float[] ringRadii = new float[] { 60f, 160f, 274f, 405f };

            for (int d = 0; d < 8; d++)
            {
                float ang = d * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                Vector3 perp = new Vector3(Mathf.Cos(ang), 0f, -Mathf.Sin(ang));

                Transform dirParent = new GameObject($"Sidewalk_{dirNames[d]}").transform;
                dirParent.SetParent(sidewalksParent, false);

                foreach (var tier in tiers)
                {
                    float len = tier.r1 - tier.r0;
                    float midR = (tier.r0 + tier.r1) * 0.5f;

                    // Left Sidewalk
                    Vector3 leftCenter = dir * midR - perp * (tier.rw * 0.5f + tier.sw * 0.5f);
                    leftCenter.y = tier.y;
                    CreateSidewalkRibbon(dirParent, $"Walk_L_R{midR:F0}", leftCenter, dir, perp, len, tier.sw, sidewalkMat);

                    // Right Sidewalk
                    Vector3 rightCenter = dir * midR + perp * (tier.rw * 0.5f + tier.sw * 0.5f);
                    rightCenter.y = tier.y;
                    CreateSidewalkRibbon(dirParent, $"Walk_R_R{midR:F0}", rightCenter, dir, perp, len, tier.sw, sidewalkMat);
                }

                // 32 Intersection Plazas (8 boulevards * 4 rings)
                for (int ringIdx = 0; ringIdx < 4; ringIdx++)
                {
                    float ringR = ringRadii[ringIdx];
                    float elev = tiers[ringIdx].y;
                    Vector3 center = dir * ringR;
                    center.y = elev + 0.02f;

                    float plazaRadius = ringIdx switch
                    {
                        0 => 8.5f,
                        1 => 9.5f,
                        2 => 10.5f,
                        _ => 11.5f
                    };

                    CreateIntersectionMedallion(
                        intersectionsParent,
                        $"Intersection_Medallion_{dirNames[d]}_Tier{4 - ringIdx}",
                        center, plazaRadius, medallionMat
                    );

                    // Place Centerpiece / Amenity
                    PopulateIntersectionCenterpiece(intersectionsParent, center, dir, perp, ringIdx, d, props, rng);
                }
            }
        }

        private static void BuildRingAvenueSidewalks(
            Transform sidewalksParent, Material sidewalkMat, System.Random rng)
        {
            // 4 Concentric Rings: (R, Y, RoadWidth, SidewalkWidth)
            (float r, float y, float rw, float sw)[] rings = new (float, float, float, float)[]
            {
                (60f,  80.08f, 7.5f, 2.5f), // Tier 4
                (160f, 58.08f, 8.0f, 2.8f), // Tier 3
                (274f, 38.08f, 8.5f, 3.0f), // Tier 2
                (405f, 22.08f, 9.0f, 3.2f)  // Tier 1
            };

            foreach (var ring in rings)
            {
                // Inner ring sidewalk
                CreateConcentricSidewalkRing(
                    sidewalksParent,
                    $"RingWalk_Inner_R{ring.r - ring.rw * 0.5f - ring.sw * 0.5f:F0}",
                    ring.r - ring.rw * 0.5f - ring.sw * 0.5f,
                    ring.y, ring.sw, sidewalkMat
                );

                // Outer ring sidewalk
                CreateConcentricSidewalkRing(
                    sidewalksParent,
                    $"RingWalk_Outer_R{ring.r + ring.rw * 0.5f + ring.sw * 0.5f:F0}",
                    ring.r + ring.rw * 0.5f + ring.sw * 0.5f,
                    ring.y, ring.sw, sidewalkMat
                );
            }
        }

        private static void CreateSidewalkRibbon(
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
            Vector3 v1 = center + forward * halfL - right * halfW;
            Vector3 v2 = center + forward * halfL + right * halfW;
            Vector3 v3 = center - forward * halfL + right * halfW;

            mesh.vertices = new Vector3[] { v0, v1, v2, v3 };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(length * 0.35f, 0f),
                new Vector2(length * 0.35f, width * 0.35f),
                new Vector2(0f, width * 0.35f)
            };

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateConcentricSidewalkRing(
            Transform parent, string name,
            float radius, float yElev, float width, Material mat)
        {
            int segments = 128;
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            float rInner = radius - width * 0.5f;
            float rOuter = radius + width * 0.5f;

            Vector3[] verts = new Vector3[(segments + 1) * 2];
            Vector2[] uvs = new Vector2[(segments + 1) * 2];
            int[] tris = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float ang = ((float)i / segments) * Mathf.PI * 2f;
                float sin = Mathf.Sin(ang);
                float cos = Mathf.Cos(ang);

                verts[i * 2]     = new Vector3(sin * rInner, yElev, cos * rInner);
                verts[i * 2 + 1] = new Vector3(sin * rOuter, yElev, cos * rOuter);

                float u = ((float)i / segments) * (radius * 0.4f);
                uvs[i * 2]     = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);

                if (i < segments)
                {
                    int root = i * 2;
                    int tRoot = i * 6;
                    tris[tRoot]     = root;
                    tris[tRoot + 1] = root + 1;
                    tris[tRoot + 2] = root + 2;

                    tris[tRoot + 3] = root + 1;
                    tris[tRoot + 4] = root + 3;
                    tris[tRoot + 5] = root + 2;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateIntersectionMedallion(
            Transform parent, string name, Vector3 center, float radius, Material mat)
        {
            int segments = 48;
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            verts.Add(center);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i <= segments; i++)
            {
                float ang = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Sin(ang) * radius;
                float z = Mathf.Cos(ang) * radius;

                verts.Add(new Vector3(center.x + x, center.y, center.z + z));
                uvs.Add(new Vector2(0.5f + Mathf.Sin(ang) * 0.5f, 0.5f + Mathf.Cos(ang) * 0.5f));

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

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
        #endregion

        #region Intersection Centerpieces
        private static void PopulateIntersectionCenterpiece(
            Transform parent, Vector3 center, Vector3 forward, Vector3 right,
            int ringIdx, int dirIdx, PropCatalog props, System.Random rng)
        {
            // Tier 1 & 2 intersections: Village Well, Notice Board, or Floral Fountain Planter
            if (ringIdx >= 2)
            {
                if ((dirIdx % 2 == 0) && props.villageWell != null)
                {
                    var well = (GameObject)PrefabUtility.InstantiatePrefab(props.villageWell, parent);
                    well.transform.position = center + right * 4.8f;
                    well.transform.rotation = Quaternion.LookRotation(-right, Vector3.up);
                    well.transform.localScale = Vector3.one * 1.05f;

                    if (props.bucket != null)
                    {
                        var b = (GameObject)PrefabUtility.InstantiatePrefab(props.bucket, parent);
                        b.transform.position = center + right * 3.6f;
                        b.transform.localScale = Vector3.one * 1.1f;
                    }
                }
                else if (props.noticeBoard != null)
                {
                    var board = (GameObject)PrefabUtility.InstantiatePrefab(props.noticeBoard, parent);
                    board.transform.position = center - right * 5.0f;
                    board.transform.rotation = Quaternion.LookRotation(right, Vector3.up);
                    board.transform.localScale = Vector3.one * 1.1f;
                }
            }
            // Tier 3 intersections: Ornate Floral Planter Monument + Benches
            else if (ringIdx == 1)
            {
                if (props.floralPlanter != null)
                {
                    var planter = (GameObject)PrefabUtility.InstantiatePrefab(props.floralPlanter, parent);
                    planter.transform.position = center + right * 5.2f;
                    planter.transform.rotation = Quaternion.identity;
                    planter.transform.localScale = Vector3.one * 0.015f; // scale down cm to 1.5m

                    // Spawn flowers in planter
                    if (props.flowerPrefabs.Length > 0)
                    {
                        var fl = (GameObject)PrefabUtility.InstantiatePrefab(props.flowerPrefabs[dirIdx % props.flowerPrefabs.Length], parent);
                        fl.transform.position = center + right * 5.2f + Vector3.up * 0.8f;
                        fl.transform.localScale = Vector3.one * 1.3f;
                    }
                }
            }
        }
        #endregion

        #region District Theming
        private static void PopulateDistrictScenes(
            Transform districtRoot, Transform festoonsRoot, Transform foliageRoot,
            PropCatalog props, System.Random rng)
        {
            (float r0, float r1, float y, float rw, float sw)[] tiers = new (float, float, float, float, float)[]
            {
                (14f,  82f,  80.08f, 7.5f, 2.8f), // Tier 4
                (120f, 198f, 58.08f, 8.0f, 3.0f), // Tier 3
                (236f, 312f, 38.08f, 8.5f, 3.2f), // Tier 2
                (352f, 460f, 22.08f, 9.0f, 3.5f)  // Tier 1
            };

            for (int d = 0; d < 8; d++)
            {
                float ang = d * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                Vector3 perp = new Vector3(Mathf.Cos(ang), 0f, -Mathf.Sin(ang));

                foreach (var tier in tiers)
                {
                    float sidewalkDist = tier.rw * 0.5f + tier.sw * 0.5f;

                    // Step along the tier street at 14m intervals
                    for (float r = tier.r0 + 6f; r <= tier.r1 - 6f; r += 14f)
                    {
                        Vector3 leftWalk = dir * r - perp * sidewalkDist;
                        leftWalk.y = tier.y;

                        Vector3 rightWalk = dir * r + perp * sidewalkDist;
                        rightWalk.y = tier.y;

                        // Overhead festoon lanterns every 28m
                        if (r % 28f < 14f && props.paperLanterns != null)
                        {
                            Vector3 festoonPos = dir * r;
                            festoonPos.y = tier.y + 5.2f;

                            var festoon = (GameObject)PrefabUtility.InstantiatePrefab(props.paperLanterns, festoonsRoot);
                            festoon.transform.position = festoonPos;
                            festoon.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                            festoon.transform.localScale = new Vector3(tier.rw * 0.085f, 1f, 1f);

                            // Warm amber lantern glow
                            var light = festoon.GetComponentInChildren<Light>() ?? festoon.AddComponent<Light>();
                            light.type = LightType.Point;
                            light.range = 10f;
                            light.color = new Color(1f, 0.78f, 0.42f);
                            light.intensity = 1.8f;
                        }

                        // Grass and moss clumps along curb seam
                        if (props.grassClump != null && rng.NextDouble() < 0.65)
                        {
                            Vector3 grassPos = (rng.NextDouble() < 0.5 ? leftWalk : rightWalk) + perp * ((float)rng.NextDouble() * 0.6f - 0.3f);
                            var clump = (GameObject)PrefabUtility.InstantiatePrefab(props.grassClump, foliageRoot);
                            clump.transform.position = grassPos;
                            clump.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                            clump.transform.localScale = Vector3.one * 0.012f;
                        }

                        // District-specific scene spawning
                        switch (d)
                        {
                            case 3:
                            case 4:
                                PopulateMarketSidewalk(districtRoot, leftWalk, rightWalk, dir, perp, props, rng);
                                break;

                            case 5:
                                PopulateTavernSidewalk(districtRoot, leftWalk, rightWalk, dir, perp, props, rng);
                                break;

                            case 1:
                            case 2:
                                PopulateCraftsmenSidewalk(districtRoot, leftWalk, rightWalk, dir, perp, props, rng);
                                break;

                            case 0:
                            case 7:
                                PopulateArcaneSidewalk(districtRoot, leftWalk, rightWalk, dir, perp, props, rng);
                                break;

                            case 6:
                            default:
                                PopulateResidentialSidewalk(districtRoot, leftWalk, rightWalk, dir, perp, props, rng);
                                break;
                        }
                    }
                }
            }
        }

        private static void PopulateMarketSidewalk(
            Transform parent, Vector3 left, Vector3 right,
            Vector3 dir, Vector3 perp, PropCatalog props, System.Random rng)
        {
            if (props.bazaarStand != null && rng.NextDouble() < 0.45)
            {
                var stall = (GameObject)PrefabUtility.InstantiatePrefab(props.bazaarStand, parent);
                stall.transform.position = left;
                stall.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                stall.transform.localScale = Vector3.one * 1.05f;

                if (props.npcMargo != null && rng.NextDouble() < 0.35)
                {
                    var npc = (GameObject)PrefabUtility.InstantiatePrefab(props.npcMargo, parent);
                    npc.transform.position = left + dir * 1.8f;
                    npc.transform.rotation = Quaternion.LookRotation(perp, Vector3.up);
                    npc.transform.localScale = Vector3.one * 1.0f;
                }
            }
            else if (props.pastryDisplay != null && rng.NextDouble() < 0.50)
            {
                var pastry = (GameObject)PrefabUtility.InstantiatePrefab(props.pastryDisplay, parent);
                pastry.transform.position = left;
                pastry.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                pastry.transform.localScale = Vector3.one * 1.1f;

                if (props.chalkboard != null)
                {
                    var chalk = (GameObject)PrefabUtility.InstantiatePrefab(props.chalkboard, parent);
                    chalk.transform.position = left + dir * 1.5f;
                    chalk.transform.rotation = Quaternion.LookRotation(-perp + dir * 0.3f, Vector3.up);
                    chalk.transform.localScale = Vector3.one * 1.0f;
                }
            }

            if (props.handcart != null && rng.NextDouble() < 0.40)
            {
                var cart = (GameObject)PrefabUtility.InstantiatePrefab(props.handcart, parent);
                cart.transform.position = right;
                cart.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                cart.transform.localScale = Vector3.one * 1.0f;

                if (props.crate != null)
                {
                    var cr = (GameObject)PrefabUtility.InstantiatePrefab(props.crate, parent);
                    cr.transform.position = right + perp * 1.2f;
                    cr.transform.localScale = Vector3.one * 1.1f;
                }
            }
            else if (props.beerBarrel != null)
            {
                var b1 = (GameObject)PrefabUtility.InstantiatePrefab(props.beerBarrel, parent);
                b1.transform.position = right;
                b1.transform.localScale = Vector3.one * 1.1f;

                if (props.sack != null && rng.NextDouble() < 0.6)
                {
                    var sk = (GameObject)PrefabUtility.InstantiatePrefab(props.sack, parent);
                    sk.transform.position = right + dir * 1.1f;
                    sk.transform.localScale = Vector3.one * 1.1f;
                }
            }
        }

        private static void PopulateTavernSidewalk(
            Transform parent, Vector3 left, Vector3 right,
            Vector3 dir, Vector3 perp, PropCatalog props, System.Random rng)
        {
            if (props.diningSet != null && rng.NextDouble() < 0.55)
            {
                var dining = (GameObject)PrefabUtility.InstantiatePrefab(props.diningSet, parent);
                dining.transform.position = left;
                dining.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                dining.transform.localScale = Vector3.one * 1.0f;
            }

            if (props.charBard != null && rng.NextDouble() < 0.25)
            {
                var bard = (GameObject)PrefabUtility.InstantiatePrefab(props.charBard, parent);
                bard.transform.position = right;
                bard.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
                bard.transform.localScale = Vector3.one * 1.1f;
            }
            else if (props.parkBench != null && rng.NextDouble() < 0.60)
            {
                var bench = (GameObject)PrefabUtility.InstantiatePrefab(props.parkBench, parent);
                bench.transform.position = right;
                bench.transform.rotation = Quaternion.LookRotation(perp, Vector3.up);
                bench.transform.localScale = Vector3.one * 1.1f;
            }

            if (props.beerBarrel != null && rng.NextDouble() < 0.50)
            {
                var barrel = (GameObject)PrefabUtility.InstantiatePrefab(props.beerBarrel, parent);
                barrel.transform.position = right + dir * 1.8f;
                barrel.transform.localScale = Vector3.one * 1.15f;
            }
        }

        private static void PopulateCraftsmenSidewalk(
            Transform parent, Vector3 left, Vector3 right,
            Vector3 dir, Vector3 perp, PropCatalog props, System.Random rng)
        {
            if (props.anvil != null && rng.NextDouble() < 0.50)
            {
                var anvilGo = (GameObject)PrefabUtility.InstantiatePrefab(props.anvil, parent);
                anvilGo.transform.position = left;
                anvilGo.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                anvilGo.transform.localScale = Vector3.one * 1.1f;

                if (props.coalBasin != null)
                {
                    var coal = (GameObject)PrefabUtility.InstantiatePrefab(props.coalBasin, parent);
                    coal.transform.position = left + dir * 1.6f;
                    coal.transform.localScale = Vector3.one * 1.1f;
                }
            }
            else if (props.grindstone != null && rng.NextDouble() < 0.50)
            {
                var grind = (GameObject)PrefabUtility.InstantiatePrefab(props.grindstone, parent);
                grind.transform.position = left;
                grind.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                grind.transform.localScale = Vector3.one * 1.1f;
            }

            if (props.weaponRack != null && rng.NextDouble() < 0.50)
            {
                var rack = (GameObject)PrefabUtility.InstantiatePrefab(props.weaponRack, parent);
                rack.transform.position = right;
                rack.transform.rotation = Quaternion.LookRotation(perp, Vector3.up);
                rack.transform.localScale = Vector3.one * 1.1f;
            }

            if (props.firewood != null && rng.NextDouble() < 0.60)
            {
                var wood = (GameObject)PrefabUtility.InstantiatePrefab(props.firewood, parent);
                wood.transform.position = right + dir * 1.8f;
                wood.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                wood.transform.localScale = Vector3.one * 1.15f;
            }

            if (props.charFox != null && rng.NextDouble() < 0.20)
            {
                var fox = (GameObject)PrefabUtility.InstantiatePrefab(props.charFox, parent);
                fox.transform.position = left + dir * 2.8f;
                fox.transform.rotation = Quaternion.LookRotation(perp, Vector3.up);
                fox.transform.localScale = Vector3.one * 1.0f;
            }
        }

        private static void PopulateArcaneSidewalk(
            Transform parent, Vector3 left, Vector3 right,
            Vector3 dir, Vector3 perp, PropCatalog props, System.Random rng)
        {
            if (props.spellLectern != null && rng.NextDouble() < 0.50)
            {
                var lectern = (GameObject)PrefabUtility.InstantiatePrefab(props.spellLectern, parent);
                lectern.transform.position = left;
                lectern.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                lectern.transform.localScale = Vector3.one * 1.1f;
            }
            else if (props.distillationBench != null && rng.NextDouble() < 0.45)
            {
                var bench = (GameObject)PrefabUtility.InstantiatePrefab(props.distillationBench, parent);
                bench.transform.position = left;
                bench.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                bench.transform.localScale = Vector3.one * 1.1f;
            }

            if (props.crystalPedestal != null && rng.NextDouble() < 0.45)
            {
                var ped = (GameObject)PrefabUtility.InstantiatePrefab(props.crystalPedestal, parent);
                ped.transform.position = right;
                ped.transform.rotation = Quaternion.identity;
                ped.transform.localScale = Vector3.one * 1.15f;

                var light = ped.GetComponentInChildren<Light>() ?? ped.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 8f;
                light.color = new Color(0.4f, 0.7f, 1f);
                light.intensity = 2.2f;
            }

            if (props.critterFairy != null && rng.NextDouble() < 0.35)
            {
                var fairy = (GameObject)PrefabUtility.InstantiatePrefab(props.critterFairy, parent);
                fairy.transform.position = right + Vector3.up * 1.6f;
                fairy.transform.localScale = Vector3.one * 1.2f;
            }
            else if (props.npcZephyr != null && rng.NextDouble() < 0.25)
            {
                var zephyr = (GameObject)PrefabUtility.InstantiatePrefab(props.npcZephyr, parent);
                zephyr.transform.position = left + dir * 2.0f;
                zephyr.transform.rotation = Quaternion.LookRotation(perp, Vector3.up);
                zephyr.transform.localScale = Vector3.one * 1.0f;
            }
        }

        private static void PopulateResidentialSidewalk(
            Transform parent, Vector3 left, Vector3 right,
            Vector3 dir, Vector3 perp, PropCatalog props, System.Random rng)
        {
            if (props.parkBench != null && rng.NextDouble() < 0.60)
            {
                var bench = (GameObject)PrefabUtility.InstantiatePrefab(props.parkBench, parent);
                bench.transform.position = left;
                bench.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                bench.transform.localScale = Vector3.one * 1.1f;
            }

            if (props.flowerPrefabs.Length > 0 && rng.NextDouble() < 0.65)
            {
                int fIdx = rng.Next(props.flowerPrefabs.Length);
                var flower = (GameObject)PrefabUtility.InstantiatePrefab(props.flowerPrefabs[fIdx], parent);
                flower.transform.position = right;
                flower.transform.localScale = Vector3.one * 1.2f;
            }

            if (props.charKids != null && rng.NextDouble() < 0.20)
            {
                var kids = (GameObject)PrefabUtility.InstantiatePrefab(props.charKids, parent);
                kids.transform.position = left + dir * 3.0f;
                kids.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                kids.transform.localScale = Vector3.one * 1.0f;
            }

            if (props.critterOwl != null && rng.NextDouble() < 0.20)
            {
                var owl = (GameObject)PrefabUtility.InstantiatePrefab(props.critterOwl, parent);
                owl.transform.position = right + dir * 1.5f + Vector3.up * 1.1f;
                owl.transform.rotation = Quaternion.LookRotation(-perp, Vector3.up);
                owl.transform.localScale = Vector3.one * 1.0f;
            }
        }
        #endregion
    }
}
