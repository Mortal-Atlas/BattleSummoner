using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Master Road Architecture & Continental Thoroughfare Detailing Pipeline.
    /// Replaces flat planar road quads with high-fidelity architectural road ribbons featuring:
    /// 1. Parabolic Cambered Roadbeds (raised center crown for drainage specular highlights).
    /// 2. Recessed Stone Drainage Gutters and Chamfered Granite Curbs.
    /// 3. Slotted Wrought-Iron Storm Drain Grates in curbs and gutters.
    /// 4. Transverse Dressed Flagstone Threshold Bands & Pedestrian Crosswalks.
    /// 5. 32 Architectural Intersection Medallions (fan paver rosettes in marble and basalt).
    /// 6. Raised Flagstone Sidewalks that cleanly border the roadway without clipping.
    /// 7. Continental Regional Highways extending outside city gates:
    ///    - The Royal Southeast Port Highway (SE Gate to Harbor Quay)
    ///    - The Great North Alpine Highway (North Gate to Lake Brestuoil Bridge & Frosthold)
    ///    - The Southern Viaduct King's Way (South Gate to Southern Viaduct & Silvermirror Lake)
    ///    - The West Desert Caravan Route (West Gate to Golden Dunes)
    ///    - The Outer Ring Beltway Highway (linking highways at R ~ 620m)
    /// 8. Roadside Furnishings:
    ///    - Directional Fingerposts with carved destination placards.
    ///    - Carved Stone Milestones / League Obelisks.
    ///    - Horse Hitching Rails & Stone Water Troughs.
    ///    - Classical Carved Stone Flower Urns & Planters.
    ///    - Gutter Moss & Cobblestone Weed Tufts.
    /// </summary>
    public static class MasterRoadDetailingPipeline
    {
        private const string ROAD_NETWORK_ROOT_NAME = "01_Master_Road_Network";

        [MenuItem("Antigravity/World/Build Detailed Master Road Network")]
        public static void ExecuteRoadEnhancement()
        {
            Debug.Log("<color=cyan><b>[Master Roads] Starting Comprehensive Road Detailing & Continental Network Construction...</b></color>");

            // 1. Locate or create infrastructure roots
            GameObject infraRoot = GameObject.Find("METROPOLIS_INFRASTRUCTURE");
            if (infraRoot == null)
            {
                var envRoot = GameObject.Find("ENVIRONMENT_ROOT") ?? new GameObject("ENVIRONMENT_ROOT");
                infraRoot = new GameObject("METROPOLIS_INFRASTRUCTURE");
                infraRoot.transform.SetParent(envRoot.transform, false);
            }

            // Remove legacy / conflicting roadway objects under 8Vector_Metropolis_Root
            var oldRoadways = GameObject.Find("8Vector_Metropolis_Root/Metropolis_Street_Infrastructure_Root/01_Cobblestone_Roadways");
            if (oldRoadways != null)
            {
                Undo.DestroyObjectImmediate(oldRoadways);
            }

            // Clean previous master road container
            Transform existing = infraRoot.transform.Find(ROAD_NETWORK_ROOT_NAME);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            // Also clean old conflicting sidewalk rings in 07_Detailed_Street_Scenes to prevent overlap
            var oldSidewalks = GameObject.Find("METROPOLIS_INFRASTRUCTURE/07_Detailed_Street_Scenes/01_Raised_Sidewalks");
            if (oldSidewalks != null)
            {
                Undo.DestroyObjectImmediate(oldSidewalks);
            }

            GameObject roadRoot = new GameObject(ROAD_NETWORK_ROOT_NAME);
            roadRoot.transform.SetParent(infraRoot.transform, false);

            Transform boulevardsParent = new GameObject("01_Radial_Grand_Boulevards").transform;
            boulevardsParent.SetParent(roadRoot.transform, false);

            Transform ringAvenuesParent = new GameObject("02_Concentric_Ring_Avenues").transform;
            ringAvenuesParent.SetParent(roadRoot.transform, false);

            Transform intersectionsParent = new GameObject("03_Intersection_Plazas_And_Medallions").transform;
            intersectionsParent.SetParent(roadRoot.transform, false);

            Transform sidewalksParent = new GameObject("04_Raised_Flagstone_Sidewalks").transform;
            sidewalksParent.SetParent(roadRoot.transform, false);

            Transform guttersParent = new GameObject("05_Curbs_Gutters_And_DrainGrates").transform;
            guttersParent.SetParent(roadRoot.transform, false);

            Transform furnishingsParent = new GameObject("06_Roadside_Wayfinding_And_Furnishings").transform;
            furnishingsParent.SetParent(roadRoot.transform, false);

            Transform highwaysParent = new GameObject("07_Continental_Regional_Highways").transform;
            highwaysParent.SetParent(roadRoot.transform, false);

            // 2. Load / Create Dedicated Road Materials
            RoadMaterials mats = GetOrCreateRoadMaterials();

            // 3. Build Central Cathedral Acropolis Grand Plaza (R <= 86m)
            BuildCentralCathedralPlaza(boulevardsParent, mats);

            // 4. Build 8 Radial Grand Boulevards with Camber, Curbs, Gutters, and Crosswalks
            BuildRadialBoulevards(boulevardsParent, sidewalksParent, guttersParent, furnishingsParent, mats);

            // 5. Build 4 Concentric Ring Avenues with Camber, Curbs, and Sidewalks
            BuildConcentricRingAvenues(ringAvenuesParent, sidewalksParent, guttersParent, mats);

            // 6. Build 32 Architectural Intersection Medallions & Wayfinding Signs
            BuildIntersectionPlazas(intersectionsParent, furnishingsParent, mats);

            // 7. Build Continental Regional Highways (SE Port Highway, North Alpine Way, South Viaduct Road, West Desert Route)
            BuildContinentalHighways(highwaysParent, furnishingsParent, mats);

            // 8. Populate Roadside Details (Milestones, Hitching Rails, Troughs, Planters, Gutter Moss)
            PopulateRoadsideFurnishings(furnishingsParent, mats);

            EditorUtility.SetDirty(roadRoot);
            Debug.Log("<color=green><b>[Master Roads] Complete! Successfully constructed high-detail cambered boulevards, curbs, gutters, drain grates, 32 intersection medallions, and 5 continental regional highways!</b></color>");
        }

        #region Materials
        public class RoadMaterials
        {
            public Material cobbleRoad;
            public Material graniteCurb;
            public Material flagstoneSidewalk;
            public Material plazaMedallion;
            public Material ironGrate;
            public Material thresholdBand;
            public Material rusticHighway;
            public Material woodSignpost;
            public Material waterSurface;
        }

        private static RoadMaterials GetOrCreateRoadMaterials()
        {
            var mats = new RoadMaterials();

            // 1. Urban Cobblestone Roadbed Material
            string roadPath = "Assets/Materials/M_CityCobbleRoad_Detailed.mat";
            mats.cobbleRoad = AssetDatabase.LoadAssetAtPath<Material>(roadPath);
            if (mats.cobbleRoad == null)
            {
                mats.cobbleRoad = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.cobbleRoad.name = "M_CityCobbleRoad_Detailed";
                AssetDatabase.CreateAsset(mats.cobbleRoad, roadPath);
            }
            mats.cobbleRoad.color = new Color(0.92f, 0.90f, 0.86f, 1f);
            mats.cobbleRoad.SetFloat("_Smoothness", 0.28f);

            var cobbleAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Cobblestone/Cobblestone_Albedo.png");
            var cobbleNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Cobblestone/Cobblestone_Normal.png");

            if (cobbleAlbedo != null)
            {
                mats.cobbleRoad.SetTexture("_BaseMap", cobbleAlbedo);
                mats.cobbleRoad.SetTextureScale("_BaseMap", new Vector2(1.0f, 1.0f));
            }
            if (cobbleNormal != null)
            {
                mats.cobbleRoad.EnableKeyword("_NORMALMAP");
                mats.cobbleRoad.SetTexture("_BumpMap", cobbleNormal);
                mats.cobbleRoad.SetTextureScale("_BumpMap", new Vector2(1.0f, 1.0f));
                mats.cobbleRoad.SetFloat("_BumpScale", 1.5f);
            }

            // 2. Chiseled Granite Curb Material
            string curbPath = "Assets/Materials/M_GraniteRoadCurb.mat";
            mats.graniteCurb = AssetDatabase.LoadAssetAtPath<Material>(curbPath);
            if (mats.graniteCurb == null)
            {
                mats.graniteCurb = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.graniteCurb.name = "M_GraniteRoadCurb";
                AssetDatabase.CreateAsset(mats.graniteCurb, curbPath);
            }
            mats.graniteCurb.color = new Color(0.84f, 0.82f, 0.78f, 1f);
            mats.graniteCurb.SetFloat("_Smoothness", 0.18f);
            var curbAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Dirt & Stone/Dirt_Stone_Albedo.png");
            var curbNormal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Idyllic Fantasy Nature/Textures/Ground/Dirt & Stone/Dirt_Stone_Normal.png");
            if (curbAlbedo != null) mats.graniteCurb.SetTexture("_BaseMap", curbAlbedo);
            if (curbNormal != null)
            {
                mats.graniteCurb.EnableKeyword("_NORMALMAP");
                mats.graniteCurb.SetTexture("_BumpMap", curbNormal);
                mats.graniteCurb.SetFloat("_BumpScale", 1.2f);
            }

            // 3. Raised Flagstone Sidewalk Material
            string walkPath = "Assets/Materials/M_CityFlagstoneSidewalk.mat";
            mats.flagstoneSidewalk = AssetDatabase.LoadAssetAtPath<Material>(walkPath);
            if (mats.flagstoneSidewalk == null)
            {
                mats.flagstoneSidewalk = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.flagstoneSidewalk.name = "M_CityFlagstoneSidewalk";
                AssetDatabase.CreateAsset(mats.flagstoneSidewalk, walkPath);
            }
            mats.flagstoneSidewalk.color = new Color(0.95f, 0.92f, 0.88f, 1f);
            mats.flagstoneSidewalk.SetFloat("_Smoothness", 0.22f);
            var flagstoneTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Metropolis/tex_metropolis_flagstone_paving.png");
            if (flagstoneTex != null)
            {
                mats.flagstoneSidewalk.SetTexture("_BaseMap", flagstoneTex);
                mats.flagstoneSidewalk.SetTextureScale("_BaseMap", new Vector2(0.5f, 0.5f));
            }

            // 4. Architectural Intersection Medallion Material
            string medPath = "Assets/Materials/M_PlazaMedallion_Detailed.mat";
            mats.plazaMedallion = AssetDatabase.LoadAssetAtPath<Material>(medPath);
            if (mats.plazaMedallion == null)
            {
                mats.plazaMedallion = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.plazaMedallion.name = "M_PlazaMedallion_Detailed";
                AssetDatabase.CreateAsset(mats.plazaMedallion, medPath);
            }
            mats.plazaMedallion.color = new Color(0.96f, 0.90f, 0.78f, 1f);
            mats.plazaMedallion.SetFloat("_Smoothness", 0.35f);
            if (cobbleAlbedo != null)
            {
                mats.plazaMedallion.SetTexture("_BaseMap", cobbleAlbedo);
                mats.plazaMedallion.SetTextureScale("_BaseMap", new Vector2(0.8f, 0.8f));
            }
            if (cobbleNormal != null)
            {
                mats.plazaMedallion.EnableKeyword("_NORMALMAP");
                mats.plazaMedallion.SetTexture("_BumpMap", cobbleNormal);
                mats.plazaMedallion.SetFloat("_BumpScale", 1.2f);
            }

            // 5. Wrought Iron Storm Drain Grate Material
            string gratePath = "Assets/Materials/M_WroughtIronGrate.mat";
            mats.ironGrate = AssetDatabase.LoadAssetAtPath<Material>(gratePath);
            if (mats.ironGrate == null)
            {
                mats.ironGrate = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.ironGrate.name = "M_WroughtIronGrate";
                AssetDatabase.CreateAsset(mats.ironGrate, gratePath);
            }
            mats.ironGrate.color = new Color(0.20f, 0.20f, 0.22f, 1f);
            mats.ironGrate.SetFloat("_Metallic", 0.85f);
            mats.ironGrate.SetFloat("_Smoothness", 0.38f);

            // 6. Transverse Flagstone Threshold Band Material
            string bandPath = "Assets/Materials/M_RoadThresholdBand.mat";
            mats.thresholdBand = AssetDatabase.LoadAssetAtPath<Material>(bandPath);
            if (mats.thresholdBand == null)
            {
                mats.thresholdBand = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.thresholdBand.name = "M_RoadThresholdBand";
                AssetDatabase.CreateAsset(mats.thresholdBand, bandPath);
            }
            mats.thresholdBand.color = new Color(0.78f, 0.75f, 0.70f, 1f);
            mats.thresholdBand.SetFloat("_Smoothness", 0.20f);
            if (flagstoneTex != null)
            {
                mats.thresholdBand.SetTexture("_BaseMap", flagstoneTex);
                mats.thresholdBand.SetTextureScale("_BaseMap", new Vector2(2.0f, 0.5f));
            }

            // 7. Rustic Continental Highway Material
            string highPath = "Assets/Materials/M_ContinentalHighway_Cobble.mat";
            mats.rusticHighway = AssetDatabase.LoadAssetAtPath<Material>(highPath);
            if (mats.rusticHighway == null)
            {
                mats.rusticHighway = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.rusticHighway.name = "M_ContinentalHighway_Cobble";
                AssetDatabase.CreateAsset(mats.rusticHighway, highPath);
            }
            mats.rusticHighway.color = new Color(0.86f, 0.83f, 0.76f, 1f);
            mats.rusticHighway.SetFloat("_Smoothness", 0.16f);
            if (cobbleAlbedo != null)
            {
                mats.rusticHighway.SetTexture("_BaseMap", cobbleAlbedo);
                mats.rusticHighway.SetTextureScale("_BaseMap", new Vector2(1.0f, 1.0f));
            }
            if (cobbleNormal != null)
            {
                mats.rusticHighway.EnableKeyword("_NORMALMAP");
                mats.rusticHighway.SetTexture("_BumpMap", cobbleNormal);
                mats.rusticHighway.SetFloat("_BumpScale", 1.4f);
            }

            // 8. Wooden Signpost Material
            string woodPath = "Assets/Materials/M_CarvedWoodSignpost.mat";
            mats.woodSignpost = AssetDatabase.LoadAssetAtPath<Material>(woodPath);
            if (mats.woodSignpost == null)
            {
                mats.woodSignpost = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.woodSignpost.name = "M_CarvedWoodSignpost";
                AssetDatabase.CreateAsset(mats.woodSignpost, woodPath);
            }
            mats.woodSignpost.color = new Color(0.55f, 0.40f, 0.26f, 1f);
            mats.woodSignpost.SetFloat("_Smoothness", 0.30f);

            // 9. Water Surface Material for Troughs
            string waterPath = "Assets/Materials/M_TroughClearWater.mat";
            mats.waterSurface = AssetDatabase.LoadAssetAtPath<Material>(waterPath);
            if (mats.waterSurface == null)
            {
                mats.waterSurface = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mats.waterSurface.name = "M_TroughClearWater";
                AssetDatabase.CreateAsset(mats.waterSurface, waterPath);
            }
            mats.waterSurface.color = new Color(0.25f, 0.55f, 0.70f, 0.85f);
            mats.waterSurface.SetFloat("_Smoothness", 0.92f);

            return mats;
        }
        #endregion

        #region Central Cathedral Plaza
        private static void BuildCentralCathedralPlaza(Transform parent, RoadMaterials mats)
        {
            GameObject plazaGo = new GameObject("Central_Cathedral_Grand_Acropolis_Plaza", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            plazaGo.transform.SetParent(parent, false);

            int radialSegments = 120;
            float radius = 86f;
            float yCenter = 80.12f;
            float yEdge = 80.06f;

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> tris = new List<int>();

            // Center vertex (subtly crowned)
            verts.Add(new Vector3(0f, yCenter, 0f));
            uvs.Add(new Vector2(0f, 0f));
            normals.Add(Vector3.up);

            for (int i = 0; i <= radialSegments; i++)
            {
                float ang = ((float)i / radialSegments) * Mathf.PI * 2f;
                float x = Mathf.Sin(ang) * radius;
                float z = Mathf.Cos(ang) * radius;

                verts.Add(new Vector3(x, yEdge, z));
                uvs.Add(new Vector2(x * 0.45f, z * 0.45f));
                normals.Add(new Vector3(Mathf.Sin(ang) * 0.05f, 0.998f, Mathf.Cos(ang) * 0.05f).normalized);

                if (i > 0)
                {
                    tris.Add(0);
                    tris.Add(i);
                    tris.Add(i + 1);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "Central_Cathedral_Plaza_Mesh";
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            plazaGo.GetComponent<MeshFilter>().sharedMesh = mesh;
            plazaGo.GetComponent<MeshRenderer>().sharedMaterial = mats.cobbleRoad;
            plazaGo.GetComponent<MeshCollider>().sharedMesh = mesh;

            // Decorative concentric ring border
            CreateCurvedCurbBorder(plazaGo.transform, "Cathedral_Plaza_Perimeter_Curb", Vector3.zero, radius, 0.35f, 0.20f, yEdge, mats.graniteCurb);
        }
        #endregion

        #region Radial Grand Boulevards
        private static void BuildRadialBoulevards(
            Transform boulevardsParent, Transform sidewalksParent, Transform guttersParent, Transform furnishingsParent,
            RoadMaterials mats)
        {
            string[] dirNames = new string[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" };
            float[] angles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

            // (rStart, rEnd, yElev, roadWidth, sidewalkWidth)
            (float r0, float r1, float y, float rw, float sw)[] tiers = new (float, float, float, float, float)[]
            {
                (12f,  86f,  80.06f, 10.0f, 2.2f), // Tier 4 (Citadel)
                (116f, 202f, 58.06f, 10.0f, 2.4f), // Tier 3 (Upper District)
                (232f, 316f, 38.06f, 10.0f, 2.6f), // Tier 2 (Mid District)
                (348f, 468f, 22.06f, 12.0f, 2.8f)  // Tier 1 (Lower District)
            };

            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab")
                          ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Idyllic Fantasy Nature/Prefabs/BlossomTree_01.prefab");

            for (int d = 0; d < 8; d++)
            {
                float angDeg = angles[d];
                float angRad = angDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angRad), 0f, Mathf.Cos(angRad));
                Vector3 right = new Vector3(dir.z, 0f, -dir.x);

                Transform bvdRoot = new GameObject($"Boulevard_{dirNames[d]}_{angDeg:F0}").transform;
                bvdRoot.SetParent(boulevardsParent, false);

                Transform walkRoot = new GameObject($"Sidewalks_{dirNames[d]}").transform;
                walkRoot.SetParent(sidewalksParent, false);

                foreach (var tier in tiers)
                {
                    float len = tier.r1 - tier.r0;
                    float midR = (tier.r0 + tier.r1) * 0.5f;
                    Vector3 center = dir * midR + Vector3.up * tier.y;

                    // 1. Cambered 7-Point Cross-Section Roadbed with Carriage Wheel Ruts
                    CreateCamberedRoadbed(
                        bvdRoot,
                        $"Roadbed_{dirNames[d]}_T{tier.y:F0}_R{tier.r0:F0}",
                        center, dir, right,
                        len, tier.rw, tier.y,
                        crownHeight: 0.12f,
                        mats.cobbleRoad
                    );

                    // 2. Chamfered Granite Curbs (Left and Right)
                    float curbDist = tier.rw * 0.5f + 0.15f;
                    CreateGraniteCurbStrip(guttersParent, center - right * curbDist, dir, right, len, 0.32f, 0.20f, mats.graniteCurb);
                    CreateGraniteCurbStrip(guttersParent, center + right * curbDist, dir, right, len, 0.32f, 0.20f, mats.graniteCurb);

                    // 3. Slotted Storm Drain Grates in gutters every 20m
                    for (float r = tier.r0 + 10f; r < tier.r1 - 6f; r += 20f)
                    {
                        Vector3 gratePosL = dir * r - right * (tier.rw * 0.5f - 0.25f) + Vector3.up * (tier.y + 0.02f);
                        CreateSlottedDrainGrate(guttersParent, $"DrainGrate_{dirNames[d]}_L_R{r:F0}", gratePosL, dir, right, mats.ironGrate);

                        Vector3 gratePosR = dir * (r + 10f) + right * (tier.rw * 0.5f - 0.25f) + Vector3.up * (tier.y + 0.02f);
                        if (r + 10f < tier.r1 - 6f)
                        {
                            CreateSlottedDrainGrate(guttersParent, $"DrainGrate_{dirNames[d]}_R_R{r + 10f:F0}", gratePosR, dir, right, mats.ironGrate);
                        }
                    }

                    // 4. Prominent Transverse Flagstone Threshold Bands & Pedestrian Crossings every 22m
                    for (float r = tier.r0 + 22f; r < tier.r1 - 10f; r += 22f)
                    {
                        Vector3 bandPos = dir * r + Vector3.up * (tier.y + 0.125f);
                        CreateThresholdCrosswalkBand(guttersParent, $"Threshold_{dirNames[d]}_R{r:F0}", bandPos, dir, right, 1.6f, tier.rw - 0.4f, mats.thresholdBand);
                    }

                    // 5. Raised Flagstone Sidewalks (Left and Right)
                    float walkDist = tier.rw * 0.5f + 0.32f + tier.sw * 0.5f;
                    CreateRaisedSidewalkRibbon(walkRoot, $"Walk_L_R{tier.r0:F0}", center - right * walkDist, dir, right, len, tier.sw, tier.y + 0.18f, mats.flagstoneSidewalk);
                    CreateRaisedSidewalkRibbon(walkRoot, $"Walk_R_R{tier.r0:F0}", center + right * walkDist, dir, right, len, tier.sw, tier.y + 0.18f, mats.flagstoneSidewalk);

                    // 6. Avenue Trees along sidewalks on Tiers 1 and 2
                    if ((tier.y < 50f) && treePrefab != null)
                    {
                        for (float r = tier.r0 + 16f; r < tier.r1 - 12f; r += 24f)
                        {
                            Vector3 treePosL = dir * r - right * (walkDist + 0.6f) + Vector3.up * (tier.y + 0.18f);
                            var treeL = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, walkRoot);
                            treeL.name = $"AvenueTree_{dirNames[d]}_L_R{r:F0}";
                            treeL.transform.position = treePosL;
                            treeL.transform.localScale = Vector3.one * 0.65f;

                            Vector3 treePosR = dir * (r + 12f) + right * (walkDist + 0.6f) + Vector3.up * (tier.y + 0.18f);
                            if (r + 12f < tier.r1 - 12f)
                            {
                                var treeR = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, walkRoot);
                                treeR.name = $"AvenueTree_{dirNames[d]}_R_R{r + 12f:F0}";
                                treeR.transform.position = treePosR;
                                treeR.transform.localScale = Vector3.one * 0.65f;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Concentric Ring Avenues
        private static void BuildConcentricRingAvenues(
            Transform ringAvenuesParent, Transform sidewalksParent, Transform guttersParent,
            RoadMaterials mats)
        {
            // (rCenter, yElev, roadWidth, sidewalkWidth)
            (float r, float y, float rw, float sw)[] rings = new (float, float, float, float)[]
            {
                (60f,  80.06f, 8.0f,  2.0f), // Tier 4 Ring
                (160f, 58.06f, 10.0f, 2.2f), // Tier 3 Ring
                (274f, 38.06f, 10.0f, 2.4f), // Tier 2 Ring
                (405f, 22.06f, 12.0f, 2.6f)  // Tier 1 Ring
            };

            int segmentsPerQuadrant = 32;
            int totalSegments = segmentsPerQuadrant * 4;

            foreach (var ring in rings)
            {
                GameObject ringGo = new GameObject($"Ring_Avenue_R{ring.r:F0}", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                ringGo.transform.SetParent(ringAvenuesParent, false);

                // Build Arched Cambered Ring Ribbon with wheel ruts
                Mesh ringMesh = BuildCamberedRingMesh(ring.r, ring.y, ring.rw, 0.11f, totalSegments);
                ringGo.GetComponent<MeshFilter>().sharedMesh = ringMesh;
                ringGo.GetComponent<MeshRenderer>().sharedMaterial = mats.cobbleRoad;
                ringGo.GetComponent<MeshCollider>().sharedMesh = ringMesh;

                // Inner and Outer Curbs
                float rInnerCurb = ring.r - ring.rw * 0.5f;
                float rOuterCurb = ring.r + ring.rw * 0.5f;
                CreateCurvedCurbBorder(guttersParent, $"Curb_Inner_R{ring.r:F0}", Vector3.zero, rInnerCurb, 0.32f, 0.20f, ring.y, mats.graniteCurb);
                CreateCurvedCurbBorder(guttersParent, $"Curb_Outer_R{ring.r:F0}", Vector3.zero, rOuterCurb, 0.32f, 0.20f, ring.y, mats.graniteCurb);

                // Inner and Outer Sidewalks
                float rInnerWalk = rInnerCurb - 0.16f - ring.sw * 0.5f;
                float rOuterWalk = rOuterCurb + 0.16f + ring.sw * 0.5f;
                CreateConcentricSidewalkRing(sidewalksParent, $"Sidewalk_Inner_R{ring.r:F0}", rInnerWalk, ring.y + 0.18f, ring.sw, mats.flagstoneSidewalk);
                CreateConcentricSidewalkRing(sidewalksParent, $"Sidewalk_Outer_R{ring.r:F0}", rOuterWalk, ring.y + 0.18f, ring.sw, mats.flagstoneSidewalk);
            }
        }
        #endregion

        #region 32 Intersection Plazas & Medallions
        private static void BuildIntersectionPlazas(
            Transform intersectionsParent, Transform furnishingsParent,
            RoadMaterials mats)
        {
            string[] dirNames = new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            float[] angles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

            (float r, float y, float medR)[] rings = new (float, float, float)[]
            {
                (60f,  80.08f, 7.0f),
                (160f, 58.08f, 8.5f),
                (274f, 38.08f, 8.5f),
                (405f, 22.08f, 10.0f)
            };

            for (int d = 0; d < 8; d++)
            {
                float angDeg = angles[d];
                float angRad = angDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angRad), 0f, Mathf.Cos(angRad));
                Vector3 right = new Vector3(dir.z, 0f, -dir.x);

                for (int ringIdx = 0; ringIdx < 4; ringIdx++)
                {
                    var ring = rings[ringIdx];
                    Vector3 center = dir * ring.r + Vector3.up * (ring.y + 0.03f);

                    // 1. Circular Fan Paver Medallion Disk
                    GameObject medGo = new GameObject($"Medallion_{dirNames[d]}_Tier{4 - ringIdx}", typeof(MeshFilter), typeof(MeshRenderer));
                    medGo.transform.SetParent(intersectionsParent, false);

                    Mesh medMesh = BuildDiskMesh(center, ring.medR, 48);
                    medGo.GetComponent<MeshFilter>().sharedMesh = medMesh;
                    medGo.GetComponent<MeshRenderer>().sharedMaterial = mats.plazaMedallion;

                    // 2. Corner Directional Fingerposts (at Boulevard & Ring Corners)
                    Vector3 cornerPos = center + (dir * 7.5f + right * 7.5f);
                    CreateDirectionalFingerpost(furnishingsParent, $"Signpost_{dirNames[d]}_T{4 - ringIdx}", cornerPos, dir, mats.woodSignpost);

                    // 3. Roadside Twin-Lantern Streetlamp Post
                    Vector3 lampPos = center + (dir * -7.5f + right * 7.5f);
                    CreateTwinLanternPost(furnishingsParent, $"AvenueLamp_{dirNames[d]}_T{4 - ringIdx}", lampPos, dir);
                }
            }
        }
        #endregion

        #region Continental Regional Highways
        private static void BuildContinentalHighways(Transform highwaysParent, Transform furnishingsParent, RoadMaterials mats)
        {
            var terrainGo = GameObject.Find("Floor01_MacroContinent_Terrain") ?? GameObject.Find("CircularContinent_MacroLevel1");
            MeshCollider terrainCol = terrainGo?.GetComponent<MeshCollider>();

            // 1. THE ROYAL SOUTHEAST PORT HIGHWAY (SE Gate -> Scenic Meadow Ridge -> Harbor Quay)
            // Meandering path with 12 organic waypoints following landscape contours
            Vector3[] portWaypoints = new Vector3[]
            {
                new Vector3(330f, 20.14f, -330f), // SE Gate Exit (Tier 1)
                new Vector3(390f, 21.80f, -370f), // East Foothill Curve
                new Vector3(460f, 23.50f, -430f), // Meadow Ridge Ascent
                new Vector3(540f, 25.40f, -500f), // Blossom Hill Crest
                new Vector3(620f, 26.20f, -580f), // Scenic Ridge Lookout & Waystation
                new Vector3(700f, 23.00f, -670f), // Orchard Valley Descent
                new Vector3(770f, 18.00f, -750f), // Coastal Terrace Bend
                new Vector3(830f, 12.00f, -820f), // Estuary Bluff Sweep
                new Vector3(890f, 6.00f,  -880f), // Harbor Canal Approach
                new Vector3(940f, 4.00f,  -930f), // Port Town Main Quay Gate
                new Vector3(960f, 2.80f,  -960f)  // Harbor Docks & Pier Terminal
            };
            BuildTerrainSnappingHighway(
                highwaysParent,
                "Highway_Royal_Southeast_Port",
                portWaypoints,
                roadWidth: 10.0f,
                terrainCol,
                mats.rusticHighway,
                mats.graniteCurb,
                furnishingsParent,
                mats
            );

            // 2. THE GREAT NORTH ALPINE HIGHWAY (North Gate -> Lake Shore -> Alpine Glacier Pass)
            Vector3[] northWaypoints = new Vector3[]
            {
                new Vector3(0f, 22.80f, 468f),    // North Gate (Elevated at Gate Threshold)
                new Vector3(15f, 23.50f, 540f),   // Grand Stone Bridge Arch across Moat / River
                new Vector3(-10f, 24.00f, 620f),  // North Lake Brestuoil Causeway Bridge
                new Vector3(-80f, 24.50f, 700f),  // Stone Arch Bridge Landing on Foothills
                new Vector3(-170f, 24.50f, 780f), // Glacial Foothills
                new Vector3(-250f, 26.50f, 850f), // Frosthold Canyon Pass
                new Vector3(-320f, 28.00f, 930f)  // Alpine Glacier Trailhead
            };
            BuildTerrainSnappingHighway(
                highwaysParent,
                "Highway_Great_North_Alpine",
                northWaypoints,
                roadWidth: 10.0f,
                terrainCol,
                mats.rusticHighway,
                mats.graniteCurb,
                furnishingsParent,
                mats
            );

            // 3. THE SOUTHERN VIADUCT KING'S WAY (South Gate -> Gorge Rim -> Silvermirror Lake)
            Vector3[] southWaypoints = new Vector3[]
            {
                new Vector3(0f, 22.80f, -468f),   // South Gate (Elevated at Gate Threshold)
                new Vector3(25f, 23.50f, -540f),  // Elevated Viaduct Bridge across Moat
                new Vector3(10f, 23.80f, -620f),  // Viaduct Lookout Crest
                new Vector3(-10f, 23.00f, -700f), // Viaduct South Pier
                new Vector3(-10f, 19.50f, -800f), // Viaduct River Crossing
                new Vector3(-50f, 18.50f, -890f), // Silvermirror Lake Boardwalk Trail
                new Vector3(-120f, 18.20f, -970f) // South Estuary Delta
            };
            BuildTerrainSnappingHighway(
                highwaysParent,
                "Highway_Southern_Kings_Way",
                southWaypoints,
                roadWidth: 10.0f,
                terrainCol,
                mats.rusticHighway,
                mats.graniteCurb,
                furnishingsParent,
                mats
            );

            // 4. THE SILK & SAND WEST DESERT CARAVAN ROUTE (West Gate -> Desert Dunes)
            Vector3[] westWaypoints = new Vector3[]
            {
                new Vector3(-468f, 22.80f, 0f),  // West Gate (Elevated at Gate Threshold)
                new Vector3(-540f, 23.50f, 25f), // Elevated Bridge Causeway across Moat
                new Vector3(-630f, 24.50f, 40f), // Sandstone Mesa Bend
                new Vector3(-730f, 26.50f, 20f), // Sunken Dunes Pass
                new Vector3(-840f, 22.00f, -10f),// Caravan Oasis Trail
                new Vector3(-950f, 18.50f, -30f) // Deep West Desert Route
            };
            BuildTerrainSnappingHighway(
                highwaysParent,
                "Highway_West_Desert_Caravan_Route",
                westWaypoints,
                roadWidth: 10.0f,
                terrainCol,
                mats.rusticHighway,
                mats.graniteCurb,
                furnishingsParent,
                mats
            );

            // 5. THE OUTER RING FOOTHILL BELTWAY HIGHWAY (R ~ 620m)
            BuildOuterBeltwayHighway(highwaysParent, 620f, 10.0f, terrainCol, mats.rusticHighway, mats.graniteCurb);
        }

        private static void BuildTerrainSnappingHighway(
            Transform parent, string name, Vector3[] waypoints, float roadWidth,
            MeshCollider terrainCol, Material roadMat, Material curbMat,
            Transform furnishingsParent, RoadMaterials mats)
        {
            // Subdivide waypoints into dense spline steps
            List<Vector3> densePoints = new List<Vector3>();
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector3 p0 = (i > 0) ? waypoints[i - 1] : waypoints[i];
                Vector3 p1 = waypoints[i];
                Vector3 p2 = waypoints[i + 1];
                Vector3 p3 = (i < waypoints.Length - 2) ? waypoints[i + 2] : waypoints[i + 1];

                int steps = 16;
                for (int s = 0; s < steps; s++)
                {
                    float t = (float)s / steps;
                    Vector3 pt = CatmullRom(p0, p1, p2, p3, t);

                    // Snap Y to terrain surface, BUT NEVER dip below water line (water surface is at Y=18.0m)
                    if (terrainCol != null)
                    {
                        Ray ray = new Ray(new Vector3(pt.x, 150f, pt.z), Vector3.down);
                        if (terrainCol.Raycast(ray, out RaycastHit hit, 300f))
                        {
                            // If terrain is dry land above the water line, snap to it
                            if (hit.point.y >= 21.0f)
                            {
                                pt.y = hit.point.y + 0.08f; // Sit right on terrain
                            }
                            else if (hit.point.y <= 1.0f)
                            {
                                // Port / coastal approach: stay elevated above ocean surface (Y=0.0m)
                                pt.y = Mathf.Max(pt.y, 2.5f);
                            }
                            else
                            {
                                // Bridge over water/moat: stay safely elevated above the 18m water line!
                                pt.y = Mathf.Max(pt.y, 22.8f);
                            }
                        }
                    }
                    densePoints.Add(pt);
                }
            }
            densePoints.Add(waypoints[waypoints.Length - 1]);

            // Build Cambered Spline Mesh
            GameObject highGo = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            highGo.transform.SetParent(parent, false);

            Mesh mesh = BuildCamberedSplineRoadMesh(densePoints, roadWidth, 0.12f);
            highGo.GetComponent<MeshFilter>().sharedMesh = mesh;
            highGo.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
            highGo.GetComponent<MeshCollider>().sharedMesh = mesh;

            // Place Roadside Milestones and Lanterns along the highway (strictly on dry, solid ground)
            for (int i = 8; i < densePoints.Count - 6; i += 24)
            {
                Vector3 pt = densePoints[i];
                Vector3 next = densePoints[i + 1];
                Vector3 forward = (next - pt).normalized;
                Vector3 right = new Vector3(forward.z, 0f, -forward.x);

                // Milestone on left
                Vector3 milePos = pt - right * (roadWidth * 0.5f + 0.8f);
                if (terrainCol != null && terrainCol.Raycast(new Ray(milePos + Vector3.up * 4f, Vector3.down), out RaycastHit hitM, 15f))
                {
                    if (hitM.point.y >= 20.0f && Mathf.Abs(milePos.y - hitM.point.y) < 1.2f)
                    {
                        milePos.y = hitM.point.y;
                        CreateStoneMilestone(furnishingsParent, $"{name}_Mile_{i}", milePos, right);
                    }
                }

                // Waypoint lantern on right
                Vector3 lampPos = pt + right * (roadWidth * 0.5f + 0.8f);
                if (terrainCol != null && terrainCol.Raycast(new Ray(lampPos + Vector3.up * 4f, Vector3.down), out RaycastHit hitL, 15f))
                {
                    if (hitL.point.y >= 20.0f && Mathf.Abs(lampPos.y - hitL.point.y) < 1.2f)
                    {
                        lampPos.y = hitL.point.y;
                        CreateHighwayWaypointLantern(furnishingsParent, $"{name}_Lamp_{i}", lampPos, -right);
                    }
                }
            }
        }

        private static void BuildOuterBeltwayHighway(
            Transform parent, float radius, float roadWidth, MeshCollider terrainCol, Material roadMat, Material curbMat)
        {
            int segments = 96;
            List<Vector3> circlePoints = new List<Vector3>();

            for (int i = 0; i <= segments; i++)
            {
                float ang = ((float)i / segments) * Mathf.PI * 2f;
                float x = Mathf.Sin(ang) * radius;
                float z = Mathf.Cos(ang) * radius;
                float y = 20f;

                if (terrainCol != null)
                {
                    Ray ray = new Ray(new Vector3(x, 150f, z), Vector3.down);
                    if (terrainCol.Raycast(ray, out RaycastHit hit, 300f))
                    {
                        if (hit.point.y >= 21.0f)
                        {
                            y = hit.point.y + 0.08f;
                        }
                        else
                        {
                            // Elevated bridge causeway across water/gorges: stay above water!
                            y = Mathf.Max(hit.point.y + 0.08f, 22.5f);
                        }
                    }
                }
                circlePoints.Add(new Vector3(x, y, z));
            }

            GameObject beltGo = new GameObject("Highway_Great_Foothill_Beltway", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            beltGo.transform.SetParent(parent, false);

            Mesh mesh = BuildCamberedSplineRoadMesh(circlePoints, roadWidth, 0.10f);
            beltGo.GetComponent<MeshFilter>().sharedMesh = mesh;
            beltGo.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
            beltGo.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
        #endregion

        #region Roadside Furnishings & Street Details
        private static void PopulateRoadsideFurnishings(Transform parent, RoadMaterials mats)
        {
            // 1. Classical Flower Urns & Planters along curbs
            var flowerUrnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/FantasyFloralPlanter.fbx");
            var benchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Urban_Props_Pack_Rozity/Prefabs/URP/SM_Park_Bench_01.prefab");
            var grassClumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/FantasyLushGrassClump.fbx");

            float[] avenueAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            System.Random rng = new System.Random(992026);

            foreach (float angDeg in avenueAngles)
            {
                float angRad = angDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angRad), 0f, Mathf.Cos(angRad));
                Vector3 right = new Vector3(dir.z, 0f, -dir.x);

                // Tier 1 and Tier 2 Curbside Details
                for (float r = 130f; r < 450f; r += 32f)
                {
                    if ((r >= 200f && r <= 234f) || (r >= 314f && r <= 350f)) continue; // Skip stairs

                    float y = (r < 210f) ? 58.06f : (r < 326f) ? 38.06f : 22.06f;
                    Vector3 curbL = dir * r - right * 5.6f + Vector3.up * (y + 0.15f);
                    Vector3 curbR = dir * r + right * 5.6f + Vector3.up * (y + 0.15f);

                    // Flower Urn
                    if (flowerUrnPrefab != null && rng.NextDouble() < 0.65)
                    {
                        var urn = (GameObject)PrefabUtility.InstantiatePrefab(flowerUrnPrefab, parent);
                        urn.name = $"Roadside_FlowerUrn_{angDeg:F0}_R{r:F0}";
                        urn.transform.position = curbL;
                        urn.transform.localScale = Vector3.one * 0.9f;
                    }

                    // Park Bench
                    if (benchPrefab != null && rng.NextDouble() < 0.50)
                    {
                        var bench = (GameObject)PrefabUtility.InstantiatePrefab(benchPrefab, parent);
                        bench.name = $"Roadside_Bench_{angDeg:F0}_R{r:F0}";
                        bench.transform.position = curbR;
                        bench.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                        bench.transform.localScale = Vector3.one * 1.0f;
                    }

                    // Horse Hitching Rail & Water Trough outside commercial areas
                    if (r > 240f && r < 310f && rng.NextDouble() < 0.40)
                    {
                        Vector3 troughPos = curbR + right * 1.5f;
                        CreateHitchingRailAndTrough(parent, $"HitchingTrough_{angDeg:F0}_R{r:F0}", troughPos, dir, mats);
                    }

                    // Gutter Moss / Grass Clump in curb joints
                    if (grassClumpPrefab != null && rng.NextDouble() < 0.60)
                    {
                        Vector3 mossPos = dir * (r + 4f) - right * 5.1f + Vector3.up * (y + 0.02f);
                        var moss = (GameObject)PrefabUtility.InstantiatePrefab(grassClumpPrefab, parent);
                        moss.name = $"Gutter_Moss_{angDeg:F0}_R{r:F0}";
                        moss.transform.position = mossPos;
                        moss.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
                    }
                }
            }
        }
        #endregion

        #region Procedural Mesh Generators
        /// <summary>
        /// Generates an authentic arched roadbed quad ribbon with 5-point cambered cross-section.
        /// Peak crown is in the center, sloping to recessed gutters on left and right.
        /// </summary>
        private static void CreateCamberedRoadbed(
            Transform parent, string name,
            Vector3 center, Vector3 forward, Vector3 right,
            float length, float width, float yElev, float crownHeight,
            Material mat)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            float halfL = length * 0.5f;
            float halfW = width * 0.5f;
            int lengthSteps = Mathf.CeilToInt(length / 3.0f); // Step every 3 meters for extra curvature smoothness
            lengthSteps = Mathf.Max(2, lengthSteps);

            // 7 cross-section lateral fractions with carriage wheel ruts at +/- 0.35
            float[] latOffsets = new float[] { -halfW, -halfW * 0.65f, -halfW * 0.35f, 0f, halfW * 0.35f, halfW * 0.65f, halfW };
            float[] heights = new float[] { 0f, crownHeight * 0.60f, crownHeight * 0.42f, crownHeight, crownHeight * 0.42f, crownHeight * 0.60f, 0f };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> tris = new List<int>();

            for (int l = 0; l <= lengthSteps; l++)
            {
                float tL = (float)l / lengthSteps;
                float distL = (tL - 0.5f) * length;
                Vector3 basePt = center + forward * distL;

                for (int c = 0; c < 7; c++)
                {
                    Vector3 pt = basePt + right * latOffsets[c] + Vector3.up * heights[c];
                    verts.Add(pt);

                    // UV: U across width, V along length (1.8m scale for authentic hand-cut cobblestones)
                    float u = (float)c / 6.0f * (width * 1.8f);
                    float v = tL * (length * 1.8f);
                    uvs.Add(new Vector2(u, v));

                    float tilt = (c - 3) * 0.06f;
                    Vector3 n = (Vector3.up + right * tilt).normalized;
                    normals.Add(n);
                }
            }

            // Build Quad Grid
            for (int l = 0; l < lengthSteps; l++)
            {
                int row0 = l * 7;
                int row1 = (l + 1) * 7;

                for (int c = 0; c < 6; c++)
                {
                    int v0 = row0 + c;
                    int v1 = row0 + c + 1;
                    int v2 = row1 + c + 1;
                    int v3 = row1 + c;

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static Mesh BuildCamberedRingMesh(float rCenter, float yElev, float roadWidth, float crownHeight, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"Ring_Mesh_R{rCenter:F0}";

            float halfW = roadWidth * 0.5f;
            float[] rOffsets = new float[] { -halfW, -halfW * 0.65f, -halfW * 0.35f, 0f, halfW * 0.35f, halfW * 0.65f, halfW };
            float[] hOffsets = new float[] { 0f, crownHeight * 0.60f, crownHeight * 0.42f, crownHeight, crownHeight * 0.42f, crownHeight * 0.60f, 0f };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> tris = new List<int>();

            for (int s = 0; s <= segments; s++)
            {
                float ang = ((float)s / segments) * Mathf.PI * 2f;
                float sin = Mathf.Sin(ang);
                float cos = Mathf.Cos(ang);
                Vector3 radDir = new Vector3(sin, 0f, cos);

                for (int c = 0; c < 7; c++)
                {
                    float r = rCenter + rOffsets[c];
                    Vector3 pt = new Vector3(sin * r, yElev + hOffsets[c], cos * r);
                    verts.Add(pt);

                    float u = (float)c / 6.0f * (roadWidth * 1.8f);
                    float v = ((float)s / segments) * (2f * Mathf.PI * rCenter * 1.8f);
                    uvs.Add(new Vector2(u, v));

                    float tilt = (c - 3) * 0.06f;
                    Vector3 n = (Vector3.up + radDir * tilt).normalized;
                    normals.Add(n);
                }
            }

            for (int s = 0; s < segments; s++)
            {
                int row0 = s * 7;
                int row1 = (s + 1) * 7;

                for (int c = 0; c < 6; c++)
                {
                    int v0 = row0 + c;
                    int v1 = row0 + c + 1;
                    int v2 = row1 + c + 1;
                    int v3 = row1 + c;

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Mesh BuildCamberedSplineRoadMesh(List<Vector3> path, float width, float crownHeight)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Cambered_Spline_Road_Mesh";

            float halfW = width * 0.5f;
            float[] latOffsets = new float[] { -halfW, -halfW * 0.65f, -halfW * 0.35f, 0f, halfW * 0.35f, halfW * 0.65f, halfW };
            float[] heights = new float[] { 0f, crownHeight * 0.60f, crownHeight * 0.42f, crownHeight, crownHeight * 0.42f, crownHeight * 0.60f, 0f };

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> tris = new List<int>();

            float accumDist = 0f;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 pt = path[i];
                Vector3 forward = (i < path.Count - 1) ? (path[i + 1] - pt).normalized : (pt - path[i - 1]).normalized;
                Vector3 right = new Vector3(forward.z, 0f, -forward.x).normalized;

                if (i > 0)
                {
                    accumDist += Vector3.Distance(pt, path[i - 1]);
                }

                for (int c = 0; c < 7; c++)
                {
                    Vector3 v = pt + right * latOffsets[c] + Vector3.up * heights[c];
                    verts.Add(v);

                    float u = (float)c / 6.0f * (width * 1.8f);
                    float vCoord = accumDist * 1.8f;
                    uvs.Add(new Vector2(u, vCoord));

                    float tilt = (c - 3) * 0.06f;
                    Vector3 n = (Vector3.up + right * tilt).normalized;
                    normals.Add(n);
                }
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                int row0 = i * 7;
                int row1 = (i + 1) * 7;

                for (int c = 0; c < 6; c++)
                {
                    int v0 = row0 + c;
                    int v1 = row0 + c + 1;
                    int v2 = row1 + c + 1;
                    int v3 = row1 + c;

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Mesh BuildDiskMesh(Vector3 center, float radius, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Disk_Medallion_Mesh";

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

                verts.Add(center + new Vector3(x, 0f, z));
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

            return mesh;
        }

        private static void CreateGraniteCurbStrip(
            Transform parent, Vector3 center, Vector3 forward, Vector3 right,
            float length, float width, float height, Material mat)
        {
            GameObject curbGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbGo.name = "GraniteCurb";
            curbGo.transform.SetParent(parent, false);
            curbGo.transform.position = center + Vector3.up * (height * 0.5f);
            curbGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            curbGo.transform.localScale = new Vector3(width, height, length);
            curbGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateCurvedCurbBorder(
            Transform parent, string name, Vector3 center, float radius,
            float curbWidth, float curbHeight, float yElev, Material mat)
        {
            int segments = 96;
            GameObject curbGo = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            curbGo.transform.SetParent(parent, false);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float r0 = radius - curbWidth * 0.5f;
            float r1 = radius + curbWidth * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float ang = ((float)i / segments) * Mathf.PI * 2f;
                float sin = Mathf.Sin(ang);
                float cos = Mathf.Cos(ang);

                verts.Add(new Vector3(sin * r0, yElev + curbHeight, cos * r0));
                verts.Add(new Vector3(sin * r1, yElev + curbHeight, cos * r1));

                float u = ((float)i / segments) * 20f;
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));

                if (i > 0)
                {
                    int v0 = (i - 1) * 2;
                    int v1 = (i - 1) * 2 + 1;
                    int v2 = i * 2 + 1;
                    int v3 = i * 2;

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            curbGo.GetComponent<MeshFilter>().sharedMesh = mesh;
            curbGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateRaisedSidewalkRibbon(
            Transform parent, string name,
            Vector3 center, Vector3 forward, Vector3 right,
            float length, float width, float yElev, Material mat)
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

            v0.y = yElev;
            v1.y = yElev;
            v2.y = yElev;
            v3.y = yElev;

            mesh.vertices = new Vector3[] { v0, v1, v2, v3 };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(length * 0.4f, 0f),
                new Vector2(length * 0.4f, width * 0.4f),
                new Vector2(0f, width * 0.4f)
            };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateConcentricSidewalkRing(
            Transform parent, string name, float radius, float yElev, float width, Material mat)
        {
            int segments = 96;
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            float r0 = radius - width * 0.5f;
            float r1 = radius + width * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float ang = ((float)i / segments) * Mathf.PI * 2f;
                float sin = Mathf.Sin(ang);
                float cos = Mathf.Cos(ang);

                verts.Add(new Vector3(sin * r0, yElev, cos * r0));
                verts.Add(new Vector3(sin * r1, yElev, cos * r1));

                float u = ((float)i / segments) * (2f * Mathf.PI * radius * 0.4f);
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, width * 0.4f));

                if (i > 0)
                {
                    int v0 = (i - 1) * 2;
                    int v1 = (i - 1) * 2 + 1;
                    int v2 = i * 2 + 1;
                    int v3 = i * 2;

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateSlottedDrainGrate(
            Transform parent, string name, Vector3 pos, Vector3 forward, Vector3 right, Material mat)
        {
            GameObject grateGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grateGo.name = name;
            grateGo.transform.SetParent(parent, false);
            grateGo.transform.position = pos;
            grateGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            grateGo.transform.localScale = new Vector3(0.40f, 0.04f, 0.65f); // 40cm x 65cm slotted grate
            grateGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateThresholdCrosswalkBand(
            Transform parent, string name, Vector3 center, Vector3 forward, Vector3 right,
            float length, float width, Material mat)
        {
            GameObject bandGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bandGo.name = name;
            bandGo.transform.SetParent(parent, false);
            bandGo.transform.position = center;
            bandGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            bandGo.transform.localScale = new Vector3(width, 0.03f, length);
            bandGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateDirectionalFingerpost(
            Transform parent, string name, Vector3 pos, Vector3 forward, Material woodMat)
        {
            GameObject postGo = new GameObject(name);
            postGo.transform.SetParent(parent, false);
            postGo.transform.position = pos;
            postGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // Vertical octagonal post
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "WoodPole";
            pole.transform.SetParent(postGo.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            pole.transform.localScale = new Vector3(0.20f, 1.35f, 0.20f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Pointed Finial Cap
            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "FinialCap";
            cap.transform.SetParent(postGo.transform, false);
            cap.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            cap.transform.localScale = Vector3.one * 0.28f;
            cap.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // Direction Arrow Boards (Board 1 pointing forward, Board 2 pointing right)
            var board1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board1.name = "ArrowBoard_Citadel";
            board1.transform.SetParent(postGo.transform, false);
            board1.transform.localPosition = new Vector3(0f, 2.40f, 0.40f);
            board1.transform.localScale = new Vector3(0.06f, 0.22f, 0.85f);
            board1.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            var board2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board2.name = "ArrowBoard_Ring";
            board2.transform.SetParent(postGo.transform, false);
            board2.transform.localPosition = new Vector3(0.40f, 2.15f, 0f);
            board2.transform.localScale = new Vector3(0.85f, 0.22f, 0.06f);
            board2.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
        }

        private static void CreateStoneMilestone(Transform parent, string name, Vector3 pos, Vector3 right)
        {
            GameObject stoneGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stoneGo.name = name;
            stoneGo.transform.SetParent(parent, false);
            stoneGo.transform.position = pos + Vector3.up * 0.55f;
            stoneGo.transform.rotation = Quaternion.LookRotation(right, Vector3.up);
            stoneGo.transform.localScale = new Vector3(0.40f, 1.10f, 0.30f);

            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_GraniteRoadCurb.mat");
            if (mat != null) stoneGo.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void CreateTwinLanternPost(Transform parent, string name, Vector3 pos, Vector3 forward)
        {
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Urban_StreetLampPost.prefab");
            if (lampPrefab != null)
            {
                var lamp = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, parent);
                lamp.name = name;
                lamp.transform.position = pos;
                lamp.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                lamp.transform.localScale = Vector3.one * 1.35f;

                var light = lamp.GetComponentInChildren<Light>();
                if (light == null) light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 16f;
                light.color = new Color(1f, 0.88f, 0.58f);
                light.intensity = 2.8f;
            }
        }

        private static void CreateHighwayWaypointLantern(Transform parent, string name, Vector3 pos, Vector3 forward)
        {
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Urban_StreetLampPost.prefab");
            if (lampPrefab != null)
            {
                var lamp = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab, parent);
                lamp.name = name;
                lamp.transform.position = pos;
                lamp.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                lamp.transform.localScale = Vector3.one * 1.25f;

                var light = lamp.GetComponentInChildren<Light>();
                if (light == null) light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 14f;
                light.color = new Color(1f, 0.82f, 0.50f);
                light.intensity = 2.4f;
            }
        }

        private static void CreateHitchingRailAndTrough(
            Transform parent, string name, Vector3 pos, Vector3 forward, RoadMaterials mats)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.position = pos;
            group.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            // 1. Wooden Hitching Rail
            var postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postL.name = "Post_L";
            postL.transform.SetParent(group.transform, false);
            postL.transform.localPosition = new Vector3(-1.0f, 0.50f, 0f);
            postL.transform.localScale = new Vector3(0.12f, 0.50f, 0.12f);
            postL.GetComponent<MeshRenderer>().sharedMaterial = mats.woodSignpost;

            var postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postR.name = "Post_R";
            postR.transform.SetParent(group.transform, false);
            postR.transform.localPosition = new Vector3(1.0f, 0.50f, 0f);
            postR.transform.localScale = new Vector3(0.12f, 0.50f, 0.12f);
            postR.GetComponent<MeshRenderer>().sharedMaterial = mats.woodSignpost;

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rail.name = "CrossRail";
            rail.transform.SetParent(group.transform, false);
            rail.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            rail.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rail.transform.localScale = new Vector3(0.08f, 1.10f, 0.08f);
            rail.GetComponent<MeshRenderer>().sharedMaterial = mats.woodSignpost;

            // 2. Chiseled Stone Water Trough
            var trough = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trough.name = "StoneTrough";
            trough.transform.SetParent(group.transform, false);
            trough.transform.localPosition = new Vector3(0f, 0.25f, -0.65f);
            trough.transform.localScale = new Vector3(1.6f, 0.50f, 0.70f);
            trough.GetComponent<MeshRenderer>().sharedMaterial = mats.graniteCurb;

            // Water surface
            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "WaterSurface";
            water.transform.SetParent(group.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.45f, -0.65f);
            water.transform.localScale = new Vector3(1.45f, 0.05f, 0.55f);
            water.GetComponent<MeshRenderer>().sharedMaterial = mats.waterSurface;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }
        #endregion
    }
}
