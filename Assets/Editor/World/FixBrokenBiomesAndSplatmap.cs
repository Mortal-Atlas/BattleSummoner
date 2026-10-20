#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.World.Editor
{
    public static class FixBrokenBiomesAndSplatmap
    {
        [MenuItem("BattleSummoner/MCP World Remediation/FIX BROKEN BIOMES, SPLATMAP & LAKES NOW")]
        public static void ExecuteBiomeAndTerrainFix()
        {
            Debug.Log("[Antigravity MCP] Inspecting scene terrain and repairing biome quadrants, splatmaps, and lakes...");

            // 1. Check if scene uses Unity Terrain component(s)
            Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            if (terrains != null && terrains.Length > 0)
            {
                foreach (Terrain t in terrains)
                {
                    RebuildUnityTerrainBiomes(t);
                }
            }

            // 2. Check for Custom Mesh Continent (e.g., Floor01_MacroContinent_Terrain / Floor01_MacroTerrain)
            MeshFilter[] meshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                string n = mf.gameObject.name.ToLowerInvariant();
                if (n.Contains("macro") || n.Contains("continent") || n.Contains("terrain") || n.Contains("island"))
                {
                    RebuildMeshContinentBiomesAndVertexColors(mf);
                }
            }

            // 3. Remove any standalone white/grey cylinder or sphere proxy objects pretending to be the volcano or SE lake hole
            PurgeBrokenProxyShapes();

            // 4. Snap all city buildings and street props flush to solid walkable ground with buffers
            SnapBuildingsToWalkableTerraces();

            SceneView.RepaintAll();
            Debug.Log("[Antigravity MCP] Biome Quadrant & Splatmap Repair Complete!");
        }

        public static float SmoothStep01(float edge0, float edge1, float x)
        {
            if (Mathf.Approximately(edge0, edge1)) return x >= edge1 ? 1f : 0f;
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        // =====================================================================
        // UNIFIED BIOME EVALUATOR (SHARED BY BOTH MESH & UNITY TERRAIN)
        // Guarantees:
        // - Top-Left (NW) = Dark Volcanic Basalt + Purple Crater (NOT white ice)
        // - Top-Center (N) = White Snow & Alpine Peaks (NOT yellow sand)
        // - Left Crescent (W) = Golden Desert Sand & Braided Delta (NOT black rock, NO straight vertical line)
        // - Bottom-Right (SE) = Organic Harbor Bay & Island (NO geometric circle cutout)
        // - Central Magic City = Generous Walkable Buffers for all houses (NO floating buildings!)
        // =====================================================================
        public static void EvaluateWorldPoint(
            float wx, float wz,
            out float finalHeightY,
            out Color biomeVertexColor,
            out float weightGrass,
            out float weightSand,
            out float weightSnow,
            out float weightBasaltRock)
        {
            // Domain-warped Perlin noise for organic biome borders & coastlines (eliminates straight lines & circles!)
            float warpX = (Mathf.PerlinNoise((wx + 3000f) * 0.0011f, (wz + 3000f) * 0.0011f) - 0.5f) * 320f;
            float warpZ = (Mathf.PerlinNoise((wx + 7000f) * 0.0011f, (wz + 7000f) * 0.0011f) - 0.5f) * 320f;
            float nx = wx + warpX;
            float nz = wz + warpZ;

            float rTrue = Mathf.Sqrt(wx * wx + wz * wz);
            float rWarped = Mathf.Sqrt(nx * nx + nz * nz);

            // Angle in degrees [0..360]: 0 = East (+X), 90 = North (+Z), 180 = West (-X), 270 = South (-Z)
            float angleDeg = Mathf.Atan2(nz, nx) * Mathf.Rad2Deg;
            if (angleDeg < 0f) angleDeg += 360f;

            weightGrass = 1f;
            weightSand = 0f;
            weightSnow = 0f;
            weightBasaltRock = 0f;

            // Stylized Color Palette matching image_f4321e.jpg
            Color colLushGrass    = new Color(0.27f, 0.54f, 0.18f, 1f); // Rich anime meadow green
            Color colForestGrass  = new Color(0.18f, 0.42f, 0.14f, 1f); // Deep woodland green
            Color colDesertSand   = new Color(0.86f, 0.65f, 0.35f, 1f); // Warm golden desert sand
            Color colDesertMesa   = new Color(0.74f, 0.42f, 0.24f, 1f); // Rust-ochre canyon rock
            Color colAlpineSnow   = new Color(0.92f, 0.96f, 1.00f, 1f); // Crisp glacier snow
            Color colVolcanoRock  = new Color(0.16f, 0.14f, 0.19f, 1f); // Dark obsidian volcanic rock
            Color colPurpleCrater = new Color(0.52f, 0.18f, 0.78f, 1f); // Purple Floor Boss mana crater
            Color colCityStone    = new Color(0.75f, 0.74f, 0.70f, 1f); // Kingdom masonry

            // 1. CENTRAL MAGIC CITY WITH SOLID WALKABLE LAND BUFFERS (NO FLOATING BUILDINGS!)
            // Tier 4: R <= 102m (houses R <= 95m, +7m buffer) -> 80m
            // Tier 3: 102m < R <= 208m (houses R <= 195m, +13m buffer) -> 58m
            // Tier 2: 208m < R <= 324m (houses R <= 310m, +14m buffer) -> 38m
            // Tier 1: 324m < R <= 468m (houses R <= 450m, +18m buffer) -> 22m
            // Moat: 468m < R <= 535m
            if (rTrue <= 468f)
            {
                finalHeightY = (rTrue <= 102f) ? 80f : (rTrue <= 208f) ? 58f : (rTrue <= 324f) ? 38f : 22f;
                biomeVertexColor = colCityStone;
                weightGrass = 0f; weightBasaltRock = 1f;
                return;
            }
            if (rTrue <= 535f)
            {
                float moatT = Mathf.Sin(((rTrue - 468f) / 67f) * Mathf.PI);
                finalHeightY = 22f - (moatT * 18f);
                biomeVertexColor = colCityStone;
                weightGrass = 0f; weightBasaltRock = 1f;
                return;
            }

            // 2. ORGANIC CONTINENT COASTLINE & SE OFFSHORE ISLAND (NO GEOMETRIC CIRCLE HOLES!)
            float maxContinentR = 2650f + (Mathf.PerlinNoise(wx * 0.0008f + 10f, wz * 0.0008f + 10f) - 0.5f) * 480f;

            // Carve SE Shipping Bay organically using warped coordinates around (1180, -820)
            float seBayDist = Vector2.Distance(new Vector2(nx, nz), new Vector2(1180f, -820f));
            // Organic SE Offshore Medium Island centered at (1680, -1180)
            float seIslandDist = Vector2.Distance(new Vector2(nx, nz), new Vector2(1680f, -1180f));

            if (seIslandDist < 440f)
            {
                float islFalloff = 1f - SmoothStep01(0f, 440f, seIslandDist);
                finalHeightY = 3f + islFalloff * 24f;
                biomeVertexColor = Color.Lerp(colDesertSand, colLushGrass, islFalloff);
                weightGrass = islFalloff; weightSand = 1f - islFalloff;
                return;
            }

            if (rTrue > maxContinentR || (seBayDist < 360f && seIslandDist >= 440f))
            {
                finalHeightY = -14f; // Underwater ocean / SE shipping bay
                biomeVertexColor = colDesertSand;
                weightGrass = 0f; weightSand = 1f;
                return;
            }

            // Base rolling meadow elevation
            float coastFade = Mathf.Clamp01((maxContinentR - rTrue) / 350f);
            float baseHeight = 20f + (Mathf.PerlinNoise((wx + 1000f) * 0.0018f, (wz + 1000f) * 0.0018f) * 16f);
            biomeVertexColor = colLushGrass;

            // -----------------------------------------------------------------
            // 3. TOP-LEFT (NW): MAGIC VOLCANIC VOLCANIA & PURPLE BOSS CRATER
            // Centered at (-1250, +1350) -> Dark Obsidian/Basalt + Purple Core!
            // -----------------------------------------------------------------
            Vector2 volcanoCenter = new Vector2(-1250f, 1350f);
            float volcDist = Vector2.Distance(new Vector2(nx, nz), volcanoCenter);
            if (volcDist < 850f)
            {
                float vBlend = 1f - SmoothStep01(0f, 850f, volcDist);
                float ridgeNoise = Mathf.Abs(Mathf.PerlinNoise(wx * 0.0045f, wz * 0.0045f) - 0.5f) * 2f;
                float coneHeight = Mathf.Pow(vBlend, 1.45f) * 315f + (ridgeNoise * 60f * vBlend);

                // Sunken Purple Boss Caldera inside R < 220m
                float trueVolcDist = Vector2.Distance(new Vector2(wx, wz), volcanoCenter);
                float craterBlend = 0f;
                if (trueVolcDist < 220f)
                {
                    craterBlend = 1f - SmoothStep01(0f, 220f, trueVolcDist);
                    coneHeight -= craterBlend * 160f;
                }

                baseHeight += coneHeight;
                Color vCol = Color.Lerp(colVolcanoRock, colPurpleCrater, craterBlend);
                biomeVertexColor = Color.Lerp(biomeVertexColor, vCol, Mathf.Clamp01(vBlend * 1.6f));
                weightBasaltRock = Mathf.Clamp01(vBlend * 1.5f);
                weightGrass = 1f - weightBasaltRock;
            }

            // -----------------------------------------------------------------
            // 4. TOP-CENTER (N): FROSTHOLD WASTES (SNOW & ALPINE GLACIER)
            // Angle 58 deg to 118 deg (North), R > 850m -> White Snow & Ice!
            // -----------------------------------------------------------------
            float northAngleWeight = SmoothAngleWindow(angleDeg, 58f, 118f, 12f);
            float northRadiusWeight = SmoothStep01(850f, 1300f, rWarped);
            float snowMask = northAngleWeight * northRadiusWeight;
            if (snowMask > 0.01f)
            {
                float alpinePeaks = Mathf.Pow(snowMask, 1.3f) * 240f * Mathf.PerlinNoise(wx * 0.0028f + 50f, wz * 0.0028f + 50f);
                baseHeight += alpinePeaks;
                biomeVertexColor = Color.Lerp(biomeVertexColor, colAlpineSnow, snowMask);
                weightSnow = snowMask;
                weightGrass = Mathf.Max(0f, 1f - weightSnow - weightBasaltRock);
            }

            // -----------------------------------------------------------------
            // 5. LEFT CRESCENT (W / SW): THE RUSTING DESOLATION (ARID DESERT)
            // Angle 160 deg to 240 deg (West & Southwest) -> Golden Sand & Mesas!
            // Blended smoothly by angle + warped radius (NO vertical black line!)
            // -----------------------------------------------------------------
            float westAngleWeight = SmoothAngleWindow(angleDeg, 160f, 240f, 12f);
            float westRadiusWeight = SmoothStep01(620f, 1020f, rWarped);
            float desertMask = westAngleWeight * westRadiusWeight;
            if (desertMask > 0.01f)
            {
                float duneWaves = Mathf.Sin(wx * 0.022f + Mathf.PerlinNoise(wx * 0.004f, wz * 0.004f) * 4f) * 14f;
                float mesaNoise = Mathf.PerlinNoise((wx + 400f) * 0.0022f, (wz - 200f) * 0.0022f);
                float mesaBoost = (mesaNoise > 0.60f) ? SmoothStep01(0.60f, 0.68f, mesaNoise) * 44f : 0f;

                float desertH = 16f + Mathf.Abs(duneWaves) + mesaBoost;
                baseHeight = Mathf.Lerp(baseHeight, desertH, desertMask);

                Color sandCol = Color.Lerp(colDesertSand, colDesertMesa, Mathf.Clamp01(mesaBoost / 44f));
                biomeVertexColor = Color.Lerp(biomeVertexColor, sandCol, desertMask);
                weightSand = desertMask;
                weightGrass = Mathf.Max(0f, 1f - weightSand - weightSnow - weightBasaltRock);
            }

            // -----------------------------------------------------------------
            // 6. TOP-RIGHT (NE) & SOUTH WOODS: LUSH FOREST BIOME SHADING
            // -----------------------------------------------------------------
            float neForestWeight = SmoothAngleWindow(angleDeg, 8f, 52f, 12f) * SmoothStep01(650f, 1000f, rWarped);
            if (neForestWeight > 0.01f)
            {
                biomeVertexColor = Color.Lerp(biomeVertexColor, colForestGrass, neForestWeight * 0.85f);
            }

            // -----------------------------------------------------------------
            // 7. ORGANIC STEPPED HIGHLAND PLATEAUS (NO BULLSEYE TARGET RINGS!)
            // Uses domain-warped coordinates (nx, nz) & smooth plateau ramps
            // -----------------------------------------------------------------
            (Vector2 center, float rad, float height)[] plateaus = {
                (new Vector2(-820f, -680f), 380f, 64f), // SW Highland
                (new Vector2(-380f, -1520f), 340f, 52f),// South Coastal Highland
                (new Vector2(1680f, -180f), 390f, 58f)  // East Coastal Highland
            };
            foreach (var p in plateaus)
            {
                float pd = Vector2.Distance(new Vector2(nx, nz), p.center);
                if (pd < p.rad)
                {
                    float norm = 1f - (pd / p.rad);
                    float smoothStep = (Mathf.Floor(norm * 3f) + Mathf.SmoothStep(0f, 1f, (norm * 3f) % 1f)) / 3f;
                    baseHeight += smoothStep * p.height;
                }
            }

            finalHeightY = baseHeight * coastFade;

            // -----------------------------------------------------------------
            // 8. CARVE BRANCHING RIVERS & INLAND FOREST LAKE
            // -----------------------------------------------------------------
            CarveRiversAndLakes(wx, wz, nx, nz, ref finalHeightY, ref biomeVertexColor, colDesertSand);
        }

        private static void CarveRiversAndLakes(
            float wx, float wz, float nx, float nz,
            ref float heightY, ref Color vertCol, Color riverbedColor)
        {
            // Organic NE Inland Forest Lake ("Brestuoil Lake" at (1480, 520))
            float neLakeDist = Vector2.Distance(new Vector2(nx, nz), new Vector2(1480f, 520f));
            if (neLakeDist < 220f)
            {
                float lakeDepth = (1f - SmoothStep01(0f, 220f, neLakeDist)) * 18f;
                heightY -= lakeDepth;
                vertCol = Color.Lerp(vertCol, riverbedColor, 0.6f);
            }

            // Primary River Channels radiating from Moat to Biomes & Ocean
            (Vector2 a, Vector2 b, float width)[] rivers = {
                // North Glacier Headwaters -> Moat
                (new Vector2(0f, 2200f), new Vector2(40f, 1200f), 45f),
                (new Vector2(40f, 1200f), new Vector2(0f, 520f), 48f),
                // West Moat -> 3-Pronged Braided Desert Delta -> Ocean
                (new Vector2(-520f, 80f), new Vector2(-1150f, 60f), 44f),
                (new Vector2(-1150f, 60f), new Vector2(-2150f, -380f), 38f),
                (new Vector2(-1150f, 60f), new Vector2(-1650f, -620f), 40f),
                (new Vector2(-1650f, -620f), new Vector2(-2100f, -1250f), 38f),
                (new Vector2(-1650f, -620f), new Vector2(-1350f, -1550f), 36f),
                // South Moat -> Waterfall Canyon -> Ocean
                (new Vector2(-60f, -520f), new Vector2(-120f, -1350f), 42f),
                (new Vector2(-120f, -1350f), new Vector2(60f, -2350f), 48f),
                // East Moat -> NE Forest Lake & SE Dockcity Shipping Bay
                (new Vector2(520f, -60f), new Vector2(1050f, -40f), 44f),
                (new Vector2(1050f, -40f), new Vector2(1480f, 520f), 36f),
                (new Vector2(1050f, -40f), new Vector2(1180f, -820f), 65f)
            };

            Vector2 p = new Vector2(wx, wz);
            float meander = (Mathf.PerlinNoise(wx * 0.005f, wz * 0.005f) - 0.5f) * 55f;

            foreach (var seg in rivers)
            {
                float d = DistancePointToSegment(p + new Vector2(meander, meander), seg.a, seg.b);
                if (d < seg.width)
                {
                    float carve = Mathf.Cos((d / seg.width) * Mathf.PI * 0.5f);
                    heightY = Mathf.Min(heightY, 10f - carve * 14f);
                    vertCol = Color.Lerp(vertCol, riverbedColor, carve * 0.75f);
                }
            }
        }

        private static float SmoothAngleWindow(float angle, float minAngle, float maxAngle, float feather)
        {
            if (angle < minAngle - feather || angle > maxAngle + feather) return 0f;
            if (angle < minAngle) return SmoothStep01(minAngle - feather, minAngle, angle);
            if (angle > maxAngle) return SmoothStep01(maxAngle + feather, maxAngle, angle);
            return 1f;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + t * ab);
        }

        // =====================================================================
        // APPLY TO CUSTOM MESH CONTINENT (VERTEX COLORS + HEIGHTS + MATERIAL)
        // =====================================================================
        private static void RebuildMeshContinentBiomesAndVertexColors(MeshFilter mf)
        {
            Mesh mesh = mf.sharedMesh;
            if (AssetDatabase.Contains(mesh))
            {
                mesh = UnityEngine.Object.Instantiate(mesh);
                mesh.name = mf.gameObject.name + "_RuntimeMesh";
                mf.sharedMesh = mesh;
            }
            Vector3[] verts = mesh.vertices;
            Color[] colors = new Color[verts.Length];
            Transform tr = mf.transform;

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 worldPt = tr.TransformPoint(verts[i]);

                EvaluateWorldPoint(
                    worldPt.x, worldPt.z,
                    out float newWorldY,
                    out Color biomeCol,
                    out _, out _, out _, out _
                );

                worldPt.y = newWorldY;
                verts[i] = tr.InverseTransformPoint(worldPt);
                colors[i] = biomeCol;
            }

            mesh.vertices = verts;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshCollider mc = mf.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = mesh;
            }

            // Ensure the MeshRenderer uses a Vertex-Color-Enabled Material so the biome colors render accurately
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Shader vcShader = Shader.Find("BattleSummoner/Environment/URP_CircularContinent_MacroTerrain")
                               ?? Shader.Find("Universal Render Pipeline/Lit");
                if (mr.sharedMaterial != null && vcShader != null)
                {
                    mr.sharedMaterial.shader = vcShader;
                }
            }
        }

        // =====================================================================
        // APPLY TO UNITY TERRAIN COMPONENT (HEIGHTMAP + 4-CHANNEL ALPHAMAP)
        // =====================================================================
        private static void RebuildUnityTerrainBiomes(Terrain terrain)
        {
            TerrainData td = terrain.terrainData;
            if (td == null) return;

            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = td.size;

            int hRes = td.heightmapResolution;
            float[,] heights = new float[hRes, hRes];
            for (int z = 0; z < hRes; z++)
            {
                for (int x = 0; x < hRes; x++)
                {
                    float wx = tPos.x + ((float)x / (hRes - 1)) * tSize.x;
                    float wz = tPos.z + ((float)z / (hRes - 1)) * tSize.z;

                    EvaluateWorldPoint(wx, wz, out float worldY, out _, out _, out _, out _, out _);
                    heights[z, x] = Mathf.Clamp01((worldY + 25f) / tSize.y);
                }
            }
            td.SetHeights(0, 0, heights);

            int aRes = td.alphamapResolution;
            int numLayers = td.alphamapLayers;
            if (numLayers >= 4)
            {
                float[,,] alpha = new float[aRes, aRes, numLayers];
                for (int z = 0; z < aRes; z++)
                {
                    for (int x = 0; x < aRes; x++)
                    {
                        float wx = tPos.x + ((float)x / (aRes - 1)) * tSize.x;
                        float wz = tPos.z + ((float)z / (aRes - 1)) * tSize.z;

                        EvaluateWorldPoint(
                            wx, wz, out _, out _,
                            out float wGrass, out float wSand, out float wSnow, out float wRock
                        );

                        float sum = Mathf.Max(0.0001f, wGrass + wSand + wSnow + wRock);
                        alpha[z, x, 0] = wGrass / sum;
                        alpha[z, x, 1] = wSand / sum;
                        alpha[z, x, 2] = wSnow / sum;
                        alpha[z, x, 3] = wRock / sum;
                    }
                }
                td.SetAlphamaps(0, 0, alpha);
            }
        }

        private static void PurgeBrokenProxyShapes()
        {
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                string n = go.name.ToLowerInvariant();
                if (n.Contains("volcano_proxy") || n.Contains("lake_cutout") || n.Contains("se_island_proxy"))
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }
        }

        private static void SnapBuildingsToWalkableTerraces()
        {
            var terrainGo = GameObject.Find("Floor01_MacroContinent_Terrain");
            var terrainCollider = terrainGo != null ? terrainGo.GetComponent<MeshCollider>() : null;
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            int snapped = 0;
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                if (go.transform.childCount > 0) continue;
                string n = go.name;
                if (n.Contains("Root") || n.Contains("Stepped") || n.Contains("Houses")) continue;

                bool isBuilding = n.StartsWith("SF_RowHouse") || n.StartsWith("RowHouse") ||
                                  n.Contains("Bakery") || n.Contains("GuildHall") ||
                                  n.Contains("House") || n.Contains("Shop") ||
                                  n.Contains("Tavern") || n.Contains("Forge") ||
                                  n.Contains("Inn_Lodge") || n.Contains("GuildArmory") ||
                                  n.Contains("Workshop");

                if (isBuilding)
                {
                    Vector3 pos = go.transform.position;
                    float r = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                    if (r <= 468f)
                    {
                        float correctTierY = (r <= 102f) ? 80f : (r <= 208f) ? 58f : (r <= 324f) ? 38f : 22f;
                        Ray ray = new Ray(new Vector3(pos.x, 150f, pos.z), Vector3.down);
                        if (terrainCollider != null && terrainCollider.Raycast(ray, out RaycastHit hit, 300f))
                        {
                            go.transform.position = new Vector3(pos.x, hit.point.y, pos.z);
                        }
                        else
                        {
                            go.transform.position = new Vector3(pos.x, correctTierY, pos.z);
                        }
                        snapped++;
                    }
                }
            }
            Debug.Log($"[Antigravity MCP] Walkable Terrace Buffer Snap: Verified {snapped} buildings onto solid ground buffers.");
        }
    }
}
#endif
