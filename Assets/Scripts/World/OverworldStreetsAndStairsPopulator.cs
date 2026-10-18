using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Optimization;
using BattleSummoner.Animation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.World
{
    /// <summary>
    /// OverworldStreetsAndStairsPopulator constructs the complete urban infrastructure:
    /// - Widened Moat Bridges with grand stone lantern bridgeheads
    /// - 24 Grand Imperial Avenue Staircases with elevated steps, side balustrades, and lanterns
    /// - 24 Secondary Sector Connecting Stairs along the tiered retaining walls
    /// - Dense street props (bazaar stalls, crates, barrels, planters, resting mounts)
    /// - 12 Interactive Land NPCs with overhead nameplates, proximity prompts, and Persona dialogues.
    /// </summary>
    [ExecuteAlways]
    public class OverworldStreetsAndStairsPopulator : MonoBehaviour
    {
        public const string StairsRootName = "04_Grand_Engineered_Stairs";
        public const string PropsRootName = "06_Lively_Street_Props";
        public const string NpcsRootName = "07_Living_Land_NPCs";

        public const float Tier4Height = 18.0f;
        public const float Tier3Height = 12.0f;
        public const float Tier2Height = 6.0f;
        public const float Tier1Height = 0.0f;

        [ContextMenu("Build Full Streets, Stairs, Props & NPCs")]
        public void BuildFullStreetsAndStairs()
        {
            FixAndWidenMoatBridges();
            BuildAll48Staircases();
            PopulateStreetProps();
            PopulateLandNPCs();
            Debug.Log("🎉 [Streets & Stairs Populator] Successfully populated widened bridges, 48 grand stairways, street props, and 12 interactive land NPCs!");
        }

        // =========================================================================
        // 1. WIDEN MOAT BRIDGES
        // =========================================================================

        [ContextMenu("1. Fix and Widen Moat Bridges")]
        public void FixAndWidenMoatBridges()
        {
            var bridges = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int count = 0;
            foreach (var b in bridges)
            {
                if (b.name.StartsWith("MoatBridge_"))
                {
                    // Scale X=2.65 (spans moat), Y=1.15 (sturdy arch), Z=2.85 (grand wide deck matching avenue)
                    b.localScale = new Vector3(2.65f, 1.15f, 2.85f);

                    // Add lantern pedestals to bridgehead corners if missing
                    if (b.Find("Bridge_Lantern_Inner_L") == null)
                    {
                        CreateBridgeheadLantern(b, "Bridge_Lantern_Inner_L", new Vector3(7.8f, 3.2f, -2.8f));
                        CreateBridgeheadLantern(b, "Bridge_Lantern_Inner_R", new Vector3(7.8f, 3.2f, 2.8f));
                        CreateBridgeheadLantern(b, "Bridge_Lantern_Outer_L", new Vector3(-7.8f, 3.2f, -2.8f));
                        CreateBridgeheadLantern(b, "Bridge_Lantern_Outer_R", new Vector3(-7.8f, 3.2f, 2.8f));
                    }
                    count++;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(b.gameObject);
#endif
                }
            }
            Debug.Log($"[Streets Populator] Widened {count} Moat Bridges to full avenue scale (Z=2.85) with symmetrical stone arches.");
        }

        private void CreateBridgeheadLantern(Transform parent, string name, Vector3 localPos)
        {
            GameObject lanternObj = new GameObject(name);
            lanternObj.transform.SetParent(parent, false);
            lanternObj.transform.localPosition = localPos;

            var light = lanternObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = 2.2f;
            light.color = new Color(1.0f, 0.88f, 0.55f);
        }

        // =========================================================================
        // 2. BUILD 48 GRAND VISIBLE STAIRCASES (24 AVENUE + 24 SECTOR)
        // =========================================================================

        [ContextMenu("2. Build All 48 Grand Staircases")]
        public void BuildAll48Staircases()
        {
            GameObject oldRoot = GameObject.Find(StairsRootName);
            if (oldRoot != null)
            {
                if (Application.isPlaying) Destroy(oldRoot);
                else DestroyImmediate(oldRoot);
            }

            GameObject stairsRoot = new GameObject(StairsRootName);
            var metropolisRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metropolisRoot != null) stairsRoot.transform.SetParent(metropolisRoot.transform, false);

            Material matStone = LoadStoneMaterial();

            // A. 24 Grand Avenue Staircases (16m wide on the 8 Main Avenues)
            float[] avenueAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            string[] avenueNames = { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" };

            for (int i = 0; i < 8; i++)
            {
                float rad = Mathf.Deg2Rad * avenueAngles[i];
                Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                string name = avenueNames[i];

                // Tier 4 -> Tier 3 (Citadel to Upper Ring)
                BuildGrandStairFlight(stairsRoot.transform, $"AvenueStairs_Tier4to3_{name}",
                    dir * 63f, dir * 77f, Tier4Height, Tier3Height, 16.0f, 22, matStone);

                // Tier 3 -> Tier 2 (Upper Ring to Mid Ring)
                BuildGrandStairFlight(stairsRoot.transform, $"AvenueStairs_Tier3to2_{name}",
                    dir * 143f, dir * 157f, Tier3Height, Tier2Height, 16.0f, 22, matStone);

                // Tier 2 -> Tier 1 (Mid Ring to Ground Ring)
                BuildGrandStairFlight(stairsRoot.transform, $"AvenueStairs_Tier2to1_{name}",
                    dir * 223f, dir * 237f, Tier2Height, Tier1Height, 16.0f, 22, matStone);
            }

            // B. 24 Secondary Sector Connecting Stairs (8m wide halfway between avenues)
            float[] sectorAngles = { 22.5f, 67.5f, 112.5f, 157.5f, 202.5f, 247.5f, 292.5f, 337.5f };
            for (int s = 0; s < 8; s++)
            {
                float sRad = Mathf.Deg2Rad * sectorAngles[s];
                Vector3 sDir = new Vector3(Mathf.Sin(sRad), 0f, Mathf.Cos(sRad));
                string sName = $"Sector_{sectorAngles[s]:F0}deg";

                // Tier 4 -> Tier 3
                BuildGrandStairFlight(stairsRoot.transform, $"SectorStairs_Tier4to3_{sName}",
                    sDir * 65f, sDir * 77f, Tier4Height, Tier3Height, 8.5f, 18, matStone);

                // Tier 3 -> Tier 2
                BuildGrandStairFlight(stairsRoot.transform, $"SectorStairs_Tier3to2_{sName}",
                    sDir * 145f, sDir * 157f, Tier3Height, Tier2Height, 8.5f, 18, matStone);

                // Tier 2 -> Tier 1
                BuildGrandStairFlight(stairsRoot.transform, $"SectorStairs_Tier2to1_{sName}",
                    sDir * 225f, sDir * 237f, Tier2Height, Tier1Height, 8.5f, 18, matStone);
            }

            Debug.Log($"[Streets Populator] Successfully built 48 Grand Connecting Staircases across all tiered levels.");
        }

        private void BuildGrandStairFlight(Transform parent, string name, Vector3 topPos, Vector3 bottomPos, float topY, float bottomY, float width, int stepCount, Material matStone)
        {
            GameObject flightObj = new GameObject(name);
            flightObj.transform.SetParent(parent, false);

            Vector3 dir = (bottomPos - topPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized * (width * 0.5f);
            float totalRun = Vector3.Distance(topPos, bottomPos);
            float stepRise = (topY - bottomY) / stepCount;

            // Lift stairs by +0.28m so they sit proudly ABOVE the road pavement mesh!
            float baseElevationOffset = 0.28f;

            Mesh mesh = new Mesh { name = name + "_Mesh" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            int vertIdx = 0;

            // 1. Generate Stone Steps
            for (int s = 0; s < stepCount; s++)
            {
                float t0 = (float)s / stepCount;
                float t1 = (float)(s + 1) / stepCount;

                Vector3 p0 = Vector3.Lerp(topPos, bottomPos, t0);
                Vector3 p1 = Vector3.Lerp(topPos, bottomPos, t1);
                float yLevel = topY + baseElevationOffset - (s * stepRise);

                // Tread Quad (Top Surface)
                Vector3 bL = p0 - right; bL.y = yLevel;
                Vector3 bR = p0 + right; bR.y = yLevel;
                Vector3 fL = p1 - right; fL.y = yLevel;
                Vector3 fR = p1 + right; fR.y = yLevel;

                verts.Add(bL); verts.Add(bR); verts.Add(fL); verts.Add(fR);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(width * 0.2f, 0f));
                uvs.Add(new Vector2(0f, 0.4f)); uvs.Add(new Vector2(width * 0.2f, 0.4f));

                tris.Add(vertIdx + 0); tris.Add(vertIdx + 1); tris.Add(vertIdx + 2);
                tris.Add(vertIdx + 1); tris.Add(vertIdx + 3); tris.Add(vertIdx + 2);
                vertIdx += 4;

                // Riser Quad (Vertical Face)
                Vector3 riserBL = fL;
                Vector3 riserBR = fR;
                Vector3 riserBotL = fL; riserBotL.y = yLevel - stepRise;
                Vector3 riserBotR = fR; riserBotR.y = yLevel - stepRise;

                verts.Add(riserBL); verts.Add(riserBR); verts.Add(riserBotL); verts.Add(riserBotR);
                uvs.Add(new Vector2(0f, 0.4f)); uvs.Add(new Vector2(width * 0.2f, 0.4f));
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(width * 0.2f, 0f));

                tris.Add(vertIdx + 0); tris.Add(vertIdx + 1); tris.Add(vertIdx + 2);
                tris.Add(vertIdx + 1); tris.Add(vertIdx + 3); tris.Add(vertIdx + 2);
                vertIdx += 4;
            }

            // 2. Generate Left & Right Carved Stone Balustrades / Railings
            float balustradeWidth = 0.65f;
            float balustradeHeight = 0.95f;

            BuildBalustradeRibbon(topPos, bottomPos, right, dir, topY + baseElevationOffset, bottomY + baseElevationOffset, balustradeWidth, balustradeHeight, true, verts, uvs, tris, ref vertIdx);
            BuildBalustradeRibbon(topPos, bottomPos, right, dir, topY + baseElevationOffset, bottomY + baseElevationOffset, balustradeWidth, balustradeHeight, false, verts, uvs, tris, ref vertIdx);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();

            var mf = flightObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = flightObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matStone;

            // 3. Smooth Ramp Box Collider (prevents player stutter on step edges)
            Vector3 midPoint = (topPos + bottomPos) * 0.5f;
            midPoint.y = (topY + bottomY) * 0.5f + baseElevationOffset;

            GameObject rampColObj = new GameObject("Mount_Ramp_Collider");
            rampColObj.transform.SetParent(flightObj.transform, false);
            rampColObj.transform.position = midPoint;
            rampColObj.transform.LookAt(new Vector3(bottomPos.x, midPoint.y, bottomPos.z));

            float slopeAngle = Mathf.Atan2(topY - bottomY, totalRun) * Mathf.Rad2Deg;
            rampColObj.transform.rotation = Quaternion.Euler(-slopeAngle, rampColObj.transform.eulerAngles.y, 0f);

            var boxCol = rampColObj.AddComponent<BoxCollider>();
            float hyp = Mathf.Sqrt(totalRun * totalRun + (topY - bottomY) * (topY - bottomY));
            boxCol.size = new Vector3(width, 0.45f, hyp);

            // 4. Grand Stone Lantern Pillars at Top and Bottom
            CreateStairPillarLantern(flightObj.transform, topPos - right * 0.95f, topY + baseElevationOffset);
            CreateStairPillarLantern(flightObj.transform, topPos + right * 0.95f, topY + baseElevationOffset);
            CreateStairPillarLantern(flightObj.transform, bottomPos - right * 0.95f, bottomY + baseElevationOffset);
            CreateStairPillarLantern(flightObj.transform, bottomPos + right * 0.95f, bottomY + baseElevationOffset);

            // Auto-attach CullableWorldProp
            var prop = flightObj.AddComponent<CullableWorldProp>();
            prop.boundingRadius = totalRun * 0.75f;
            prop.isDynamicMover = false;
        }

        private void BuildBalustradeRibbon(Vector3 topPos, Vector3 bottomPos, Vector3 right, Vector3 dir, float topY, float bottomY, float bWidth, float bHeight, bool isLeft, List<Vector3> verts, List<Vector2> uvs, List<int> tris, ref int vertIdx)
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

        private void CreateStairPillarLantern(Transform parent, Vector3 worldPos, float y)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Stair_Stone_Pillar_Lantern";
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = new Vector3(worldPos.x, y + 1.1f, worldPos.z);
            pillar.transform.localScale = new Vector3(0.85f, 2.2f, 0.85f);

            var mr = pillar.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = LoadStoneMaterial();

            // Warm Glowing Lantern Light on Pillar
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
        // 3. POPULATE STREET PROPS (BAZAAR STANDS, CRATES, PLANTERS, LANTERNS)
        // =========================================================================

        [ContextMenu("3. Populate Street Props")]
        public void PopulateStreetProps()
        {
            GameObject oldProps = GameObject.Find(PropsRootName);
            if (oldProps != null)
            {
                if (Application.isPlaying) Destroy(oldProps);
                else DestroyImmediate(oldProps);
            }

            GameObject propsRoot = new GameObject(PropsRootName);
            var metropolisRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metropolisRoot != null) propsRoot.transform.SetParent(metropolisRoot.transform, false);

#if UNITY_EDITOR
            // 1. Urban Street Lamps (urban-props-pack-377016)
            var streetLampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Urban_StreetLampPost.prefab");

            // 2. Stylized Blacksmith Props (free-stylized-blacksmith-props-starter-kit-371198)
            var anvilPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Anvil_Stump.prefab");
            var quenchTroughPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_QuenchTrough.prefab");
            var coalBasinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_CoalBasin.prefab");
            var grindstonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Blacksmith_Grindstone.prefab");

            // 3. Wizard Props (magic-spellbook-book-stand-wizard-props-392142)
            var spellbookLecternPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Wizard_SpellbookLectern.prefab");
            var crystalPedestalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Wizard_CrystalPedestal.prefab");

            // 4. CozyShop Bakery Props (cozyshops-coffee-shop-lite-10-free-props-401100)
            var bakeryDisplayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Bakery_PastryDisplay.prefab");
            var bakeryChalkboardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Bakery_MenuChalkboard.prefab");

            // 5. Alchemy Lab Props (alchemy-lab-props-41758)
            var distillationBenchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Alchemy_DistillationBench.prefab");
            var mortarPestlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Alchemy_MortarPestleRack.prefab");

            // 6. RPG Medieval & Survival Village Props (294298, 258935, 41540, 59280)
            var villageWellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_VillageWell.prefab");
            var noticeBoardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_NoticeBoard.prefab");
            var weaponRackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_WeaponRack.prefab");
            var firewoodStackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Medieval_FirewoodStack.prefab");

            // 7. Low Poly Interior Tavern Props (400108)
            var tavernTablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Props/PFB_Tavern_DiningTableSet.prefab");

            // Legacy & Decorative Props
            var boothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Market_Booth.prefab");
            var barrelsCratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TownProps/Prop_WoodenBarrelsCrates.fbx");
            var bazaarStandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Prop_BazaarStand_Street.fbx");
            var planterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/StonePlanter_BloomingFlora.fbx");
            var crystalsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx");
            var wolfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Mount_ArmoredWolfBeast.fbx");
            var chocoboPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Mount_FeatheredChocoboBird.fbx");

            int spawnedCount = 0;

            // -------------------------------------------------------------------------
            // 1. URBAN STREET LAMPS (urban-props-pack-377016)
            // -------------------------------------------------------------------------
            if (streetLampPrefab != null)
            {
                // A. Citadel Ring Promenade (Tier 4, R=32m)
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * 45f;
                    float rad = ang * Mathf.Deg2Rad;
                    Vector3 pos = new Vector3(Mathf.Sin(rad) * 32f, Tier4Height, Mathf.Cos(rad) * 32f);
                    SpawnProp(streetLampPrefab, propsRoot.transform, pos, Quaternion.Euler(0f, ang + 180f, 0f), 1.0f);
                    spawnedCount++;
                }

                // B. Grand Radial Avenues (8 avenues, placed along both curbs)
                float[] avenueRadii = new float[] { 75f, 105f, 155f, 210f, 255f, 285f };
                for (int a = 0; a < 8; a++)
                {
                    float avenueAngle = a * 45f;
                    float rad = avenueAngle * Mathf.Deg2Rad;
                    Vector3 forwardDir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                    Vector3 sideDir = new Vector3(forwardDir.z, 0f, -forwardDir.x); // perpendicular

                    foreach (float r in avenueRadii)
                    {
                        float h = r < 90f ? Tier3Height : (r < 190f ? Tier2Height : Tier1Height);
                        Vector3 centerPos = forwardDir * r; centerPos.y = h;

                        // Left curb lamp
                        Vector3 leftPos = centerPos - sideDir * 6.5f;
                        SpawnProp(streetLampPrefab, propsRoot.transform, leftPos, Quaternion.Euler(0f, avenueAngle + 90f, 0f), 1.0f);

                        // Right curb lamp
                        Vector3 rightPos = centerPos + sideDir * 6.5f;
                        SpawnProp(streetLampPrefab, propsRoot.transform, rightPos, Quaternion.Euler(0f, avenueAngle - 90f, 0f), 1.0f);
                        spawnedCount += 2;
                    }
                }

                // C. Moat Bridgehead Gate Lamps (4 Gates at R=305m)
                Vector3[] bridgeGates = new Vector3[]
                {
                    new Vector3(0f, Tier1Height, 305f),   // North
                    new Vector3(0f, Tier1Height, -305f),  // South
                    new Vector3(305f, Tier1Height, 0f),   // East
                    new Vector3(-305f, Tier1Height, 0f)   // West
                };
                for (int g = 0; g < bridgeGates.Length; g++)
                {
                    Vector3 gPos = bridgeGates[g];
                    Vector3 lateral = (gPos.x == 0f) ? Vector3.right : Vector3.forward;
                    SpawnProp(streetLampPrefab, propsRoot.transform, gPos - lateral * 6f, Quaternion.identity, 1.0f);
                    SpawnProp(streetLampPrefab, propsRoot.transform, gPos + lateral * 6f, Quaternion.identity, 1.0f);
                    spawnedCount += 2;
                }

                // D. Civic Bazaar Ring Boulevard Lamps (Tier 2, R=195m)
                for (int b = 0; b < 6; b++)
                {
                    float bAng = 55f + (b * 26f);
                    float bRad = bAng * Mathf.Deg2Rad;
                    Vector3 bPos = new Vector3(Mathf.Sin(bRad) * 190f, Tier2Height, Mathf.Cos(bRad) * 190f);
                    SpawnProp(streetLampPrefab, propsRoot.transform, bPos, Quaternion.Euler(0f, bAng, 0f), 1.0f);
                    spawnedCount++;
                }
            }

            // -------------------------------------------------------------------------
            // 2. STYLIZED BLACKSMITH STARTER KIT (free-stylized-blacksmith-props-starter-kit-371198)
            // -------------------------------------------------------------------------
            Vector3 torvaldPos = new Vector3(115f, Tier3Height, 10f);
            if (anvilPrefab != null)
            {
                SpawnProp(anvilPrefab, propsRoot.transform, torvaldPos + new Vector3(-2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 1.0f);
                spawnedCount++;
            }
            if (quenchTroughPrefab != null)
            {
                SpawnProp(quenchTroughPrefab, propsRoot.transform, torvaldPos + new Vector3(-2f, 0f, 2.2f), Quaternion.Euler(0f, 0f, 0f), 1.0f);
                spawnedCount++;
            }
            if (coalBasinPrefab != null)
            {
                SpawnProp(coalBasinPrefab, propsRoot.transform, torvaldPos + new Vector3(-2f, 0f, -2.2f), Quaternion.identity, 1.0f);
                spawnedCount++;
            }
            if (grindstonePrefab != null)
            {
                SpawnProp(grindstonePrefab, propsRoot.transform, torvaldPos + new Vector3(1.5f, 0f, -2.5f), Quaternion.Euler(0f, 45f, 0f), 1.0f);
                spawnedCount++;
            }

            // Secondary Smithy Station at North Bastion
            Vector3 bastionSmithyPos = new Vector3(12f, Tier1Height, 282f);
            if (anvilPrefab != null) { SpawnProp(anvilPrefab, propsRoot.transform, bastionSmithyPos, Quaternion.Euler(0f, 180f, 0f), 1.0f); spawnedCount++; }
            if (quenchTroughPrefab != null) { SpawnProp(quenchTroughPrefab, propsRoot.transform, bastionSmithyPos + new Vector3(2f, 0f, 0f), Quaternion.identity, 1.0f); spawnedCount++; }
            if (coalBasinPrefab != null) { SpawnProp(coalBasinPrefab, propsRoot.transform, bastionSmithyPos + new Vector3(-2f, 0f, 0f), Quaternion.identity, 1.0f); spawnedCount++; }

            // -------------------------------------------------------------------------
            // 3. WIZARD PROPS & SPELLBOOKS (magic-spellbook-book-stand-wizard-props-392142)
            // -------------------------------------------------------------------------
            // A. High Priestess Vespera Shrine (Tier 4)
            Vector3 vesperaPos = new Vector3(0f, Tier4Height, 35f);
            if (spellbookLecternPrefab != null)
            {
                SpawnProp(spellbookLecternPrefab, propsRoot.transform, vesperaPos + new Vector3(1.6f, 0f, -0.6f), Quaternion.Euler(0f, 200f, 0f), 1.0f);
                spawnedCount++;
            }
            if (crystalPedestalPrefab != null)
            {
                SpawnProp(crystalPedestalPrefab, propsRoot.transform, vesperaPos + new Vector3(-2.8f, 0f, 0.5f), Quaternion.identity, 1.0f);
                SpawnProp(crystalPedestalPrefab, propsRoot.transform, vesperaPos + new Vector3(2.8f, 0f, 0.5f), Quaternion.identity, 1.0f);
                SpawnProp(crystalPedestalPrefab, propsRoot.transform, new Vector3(0f, Tier4Height, 0f), Quaternion.identity, 1.0f); // Center Plaza Nexus
                spawnedCount += 3;
            }

            // B. Alchemist Lysandra Apothecary (Tier 3 West)
            Vector3 lysandraPos = new Vector3(-105f, Tier3Height, 35f);
            if (spellbookLecternPrefab != null)
            {
                SpawnProp(spellbookLecternPrefab, propsRoot.transform, lysandraPos + new Vector3(1.8f, 0f, -0.8f), Quaternion.Euler(0f, 150f, 0f), 1.0f);
                spawnedCount++;
            }
            if (crystalPedestalPrefab != null)
            {
                SpawnProp(crystalPedestalPrefab, propsRoot.transform, lysandraPos + new Vector3(-2.5f, 0f, 1.5f), Quaternion.identity, 1.0f);
                spawnedCount++;
            }

            // -------------------------------------------------------------------------
            // 4. COZYSHOP BAKERY PROPS (cozyshops-coffee-shop-lite-10-free-props-401100)
            // -------------------------------------------------------------------------
            Vector3 pierrePos = new Vector3(25f, Tier2Height, -190f);
            if (bakeryDisplayPrefab != null)
            {
                SpawnProp(bakeryDisplayPrefab, propsRoot.transform, pierrePos + new Vector3(0f, 0f, 1.8f), Quaternion.identity, 1.0f);
                spawnedCount++;
            }
            if (bakeryChalkboardPrefab != null)
            {
                SpawnProp(bakeryChalkboardPrefab, propsRoot.transform, pierrePos + new Vector3(-2.2f, 0f, 1.5f), Quaternion.Euler(0f, 25f, 0f), 1.0f);
                spawnedCount++;
            }

            // -------------------------------------------------------------------------
            // 5. ALCHEMY LAB PROPS (alchemy-lab-props-41758)
            // -------------------------------------------------------------------------
            if (distillationBenchPrefab != null)
            {
                SpawnProp(distillationBenchPrefab, propsRoot.transform, lysandraPos + new Vector3(0f, 0f, 2.2f), Quaternion.Euler(0f, 180f, 0f), 1.0f);
                spawnedCount++;
            }
            if (mortarPestlePrefab != null)
            {
                SpawnProp(mortarPestlePrefab, propsRoot.transform, lysandraPos + new Vector3(-2.2f, 0f, -1.2f), Quaternion.Euler(0f, 45f, 0f), 1.0f);
                // Also at Guild Registrar Donald
                Vector3 donaldPos = new Vector3(10f, Tier3Height, 115f);
                SpawnProp(mortarPestlePrefab, propsRoot.transform, donaldPos + new Vector3(2.2f, 0f, 1.2f), Quaternion.Euler(0f, 210f, 0f), 1.0f);
                spawnedCount += 2;
            }

            // -------------------------------------------------------------------------
            // 6. RPG MEDIEVAL & SURVIVAL VILLAGE PROPS (294298, 258935, 41540, 59280)
            // -------------------------------------------------------------------------
            // A. Village Wells
            if (villageWellPrefab != null)
            {
                SpawnProp(villageWellPrefab, propsRoot.transform, new Vector3(142f, Tier2Height, -125f), Quaternion.identity, 1.0f); // Civic Bazaar
                SpawnProp(villageWellPrefab, propsRoot.transform, new Vector3(-142f, Tier2Height, -125f), Quaternion.identity, 1.0f); // West Residential
                SpawnProp(villageWellPrefab, propsRoot.transform, new Vector3(0f, Tier1Height, 175f), Quaternion.identity, 1.0f); // North Commons
                spawnedCount += 3;
            }

            // B. Notice Boards
            if (noticeBoardPrefab != null)
            {
                SpawnProp(noticeBoardPrefab, propsRoot.transform, new Vector3(7.5f, Tier3Height, 114.5f), Quaternion.Euler(0f, 180f, 0f), 1.0f); // Guild Registrar
                SpawnProp(noticeBoardPrefab, propsRoot.transform, new Vector3(138f, Tier2Height, -138f), Quaternion.Euler(0f, 315f, 0f), 1.0f); // Civic Bazaar
                SpawnProp(noticeBoardPrefab, propsRoot.transform, new Vector3(6f, Tier1Height, 286f), Quaternion.Euler(0f, 180f, 0f), 1.0f); // North Bastion Gate
                SpawnProp(noticeBoardPrefab, propsRoot.transform, new Vector3(6f, Tier1Height, -286f), Quaternion.Euler(0f, 0f, 0f), 1.0f); // South Moat Gate
                spawnedCount += 4;
            }

            // C. Weapon Racks
            if (weaponRackPrefab != null)
            {
                // Grand Marshal Theresa Guard Post
                Vector3 theresaPos = new Vector3(12f, Tier4Height, -15f);
                SpawnProp(weaponRackPrefab, propsRoot.transform, theresaPos + new Vector3(2.5f, 0f, 1.0f), Quaternion.Euler(0f, 210f, 0f), 1.0f);
                SpawnProp(weaponRackPrefab, propsRoot.transform, theresaPos + new Vector3(-2.5f, 0f, 1.0f), Quaternion.Euler(0f, 150f, 0f), 1.0f);

                // Blacksmith Torvald
                SpawnProp(weaponRackPrefab, propsRoot.transform, torvaldPos + new Vector3(1.5f, 0f, 2.5f), Quaternion.Euler(0f, 90f, 0f), 1.0f);

                // Gate Sentinels
                SpawnProp(weaponRackPrefab, propsRoot.transform, new Vector3(-5f, Tier1Height, 288f), Quaternion.Euler(0f, 180f, 0f), 1.0f);
                SpawnProp(weaponRackPrefab, propsRoot.transform, new Vector3(-5f, Tier1Height, -288f), Quaternion.identity, 1.0f);
                spawnedCount += 5;
            }

            // D. Firewood Stacks
            if (firewoodStackPrefab != null)
            {
                SpawnProp(firewoodStackPrefab, propsRoot.transform, pierrePos + new Vector3(2.8f, 0f, -2.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f); // Bakery Hearth
                SpawnProp(firewoodStackPrefab, propsRoot.transform, torvaldPos + new Vector3(2.8f, 0f, -1.0f), Quaternion.Euler(0f, 90f, 0f), 1.0f); // Smithy Forge
                SpawnProp(firewoodStackPrefab, propsRoot.transform, new Vector3(85f, Tier2Height, -145f), Quaternion.identity, 1.0f);
                SpawnProp(firewoodStackPrefab, propsRoot.transform, new Vector3(-85f, Tier2Height, -145f), Quaternion.identity, 1.0f);
                SpawnProp(firewoodStackPrefab, propsRoot.transform, new Vector3(-40f, Tier1Height, 210f), Quaternion.Euler(0f, 45f, 0f), 1.0f);
                SpawnProp(firewoodStackPrefab, propsRoot.transform, new Vector3(40f, Tier1Height, 210f), Quaternion.Euler(0f, -45f, 0f), 1.0f);
                spawnedCount += 6;
            }

            // -------------------------------------------------------------------------
            // 7. LOW POLY INTERIOR TAVERN PROPS (low-poly-interior-props-pack-3digitalis-400108)
            // -------------------------------------------------------------------------
            Vector3 lyraPos = new Vector3(45f, Tier3Height, -45f);
            if (tavernTablePrefab != null)
            {
                // Outdoor minstrel patio around Bard Lyra
                SpawnProp(tavernTablePrefab, propsRoot.transform, lyraPos + new Vector3(0f, 0f, 3.8f), Quaternion.Euler(0f, 180f, 0f), 1.0f);
                SpawnProp(tavernTablePrefab, propsRoot.transform, lyraPos + new Vector3(-3.5f, 0f, -1.0f), Quaternion.Euler(0f, 75f, 0f), 1.0f);
                SpawnProp(tavernTablePrefab, propsRoot.transform, lyraPos + new Vector3(3.5f, 0f, -1.0f), Quaternion.Euler(0f, -75f, 0f), 1.0f);

                // Lower Tier 1 Open Tavern outdoor garden
                SpawnProp(tavernTablePrefab, propsRoot.transform, new Vector3(65f, Tier1Height, 65f), Quaternion.identity, 1.0f);
                SpawnProp(tavernTablePrefab, propsRoot.transform, new Vector3(72f, Tier1Height, 68f), Quaternion.Euler(0f, 30f, 0f), 1.0f);
                SpawnProp(tavernTablePrefab, propsRoot.transform, new Vector3(68f, Tier1Height, 75f), Quaternion.Euler(0f, -20f, 0f), 1.0f);
                spawnedCount += 6;
            }

            // -------------------------------------------------------------------------
            // 8. CITADEL PLANTERS & GARDENS
            // -------------------------------------------------------------------------
            if (planterPrefab != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * 45f + 22.5f;
                    float rad = ang * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                    Vector3 pos = dir * 42f; pos.y = Tier4Height;

                    SpawnProp(planterPrefab, propsRoot.transform, pos + Vector3.Cross(Vector3.up, dir) * 3.5f, Quaternion.Euler(-90f, ang, 0f), 0.035f);
                    SpawnProp(planterPrefab, propsRoot.transform, pos - Vector3.Cross(Vector3.up, dir) * 3.5f, Quaternion.Euler(-90f, ang, 0f), 0.035f);
                    spawnedCount += 2;
                }
            }

            // -------------------------------------------------------------------------
            // 9. CIVIC RING BAZAAR MARKET STALLS & BARRELS
            // -------------------------------------------------------------------------
            for (int i = 0; i < 10; i++)
            {
                float ang = 60f + (i * 22f);
                float rad = ang * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Sin(rad) * 195f, Tier2Height, Mathf.Cos(rad) * 195f);

                if (i % 2 == 0 && boothPrefab != null)
                {
                    SpawnProp(boothPrefab, propsRoot.transform, pos, Quaternion.Euler(-90f, ang + 180f, 0f), 0.045f);
                    spawnedCount++;
                }
                else if (bazaarStandPrefab != null)
                {
                    SpawnProp(bazaarStandPrefab, propsRoot.transform, pos, Quaternion.Euler(-90f, ang + 180f, 0f), 0.045f);
                    spawnedCount++;
                }

                if (barrelsCratesPrefab != null)
                {
                    SpawnProp(barrelsCratesPrefab, propsRoot.transform, pos + Vector3.right * 2.5f, Quaternion.Euler(-90f, UnityEngine.Random.Range(0, 360), 0f), 0.035f);
                    spawnedCount++;
                }
            }

            // -------------------------------------------------------------------------
            // 10. MOUNT STABLES (Armored Wolves & Chocobos)
            // -------------------------------------------------------------------------
            Vector3 stablePos = new Vector3(-265f, Tier1Height, 15f);
            if (wolfPrefab != null)
            {
                SpawnProp(wolfPrefab, propsRoot.transform, stablePos, Quaternion.Euler(-90f, 90f, 0f), 0.038f);
                SpawnProp(wolfPrefab, propsRoot.transform, stablePos + new Vector3(0f, 0f, 6f), Quaternion.Euler(-90f, 75f, 0f), 0.038f);
                spawnedCount += 2;
            }
            if (chocoboPrefab != null)
            {
                SpawnProp(chocoboPrefab, propsRoot.transform, stablePos + new Vector3(0f, 0f, -6f), Quaternion.Euler(-90f, 105f, 0f), 0.038f);
                spawnedCount++;
            }

            Debug.Log($"[Streets Populator] Spawned {spawnedCount} lively props across urban street lamps, blacksmith forge, wizard lecterns, bakery displays, alchemy benches, village wells, notice boards, weapon racks, and tavern terraces.");
#endif
        }

        private GameObject SpawnProp(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            GameObject obj = Instantiate(prefab, pos, rot, parent);
            obj.transform.localScale = Vector3.one * scale;

            // Auto-attach CullableWorldProp
            var prop = obj.GetComponent<CullableWorldProp>();
            if (prop == null) prop = obj.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 4.0f;
            prop.isDynamicMover = false;

            return obj;
        }

        // =========================================================================
        // 4. POPULATE 12 LIVING INTERACTIVE LAND NPCS
        // =========================================================================

        [ContextMenu("4. Populate Living Land NPCs")]
        public void PopulateLandNPCs()
        {
            GameObject oldNpcs = GameObject.Find(NpcsRootName);
            if (oldNpcs != null)
            {
                if (Application.isPlaying) Destroy(oldNpcs);
                else DestroyImmediate(oldNpcs);
            }

            GameObject npcsRoot = new GameObject(NpcsRootName);
            var metropolisRoot = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---");
            if (metropolisRoot != null) npcsRoot.transform.SetParent(metropolisRoot.transform, false);

#if UNITY_EDITOR
            var fGenshin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Base_Female_Genshin.prefab");
            var mGenshin = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Base_Male_Genshin.prefab");
            var fRpg = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Hero Duo/Prefab/FemaleCharacterPBR.prefab");
            var mRpg = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Hero Duo/Prefab/MaleCharacterPBR.prefab");
            var golem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Golem_ClockworkBrassAssistant.fbx");

            var npcConfigs = new[]
            {
                // Citadel (Tier 4)
                new { name = "High Priestess Vespera", role = "Church of Revival", prefab = fGenshin, pos = new Vector3(0f, Tier4Height, 35f), yaw = 180f,
                      dialogue = new[] { "Welcome to the Church of Revival, brave Summoner.", "The Soul-Binder altar will resurrect you should you fall in the spire.", "Keep your spirit contract untarnished!" } },

                new { name = "Grand Marshal Theresa", role = "Royal Defense Commander", prefab = fRpg, pos = new Vector3(12f, Tier4Height, -15f), yaw = 210f,
                      dialogue = new[] { "Stand tall, soldier! The Kingdom walls remain secure.", "Our scouts report unchecked portals festering beyond the circular moat.", "Eliminate them before siege camps form!" } },

                new { name = "Clockwork Assistant Gearhart", role = "Hoverpad Attendant", prefab = golem, pos = new Vector3(-22f, Tier4Height, 8f), yaw = 90f,
                      dialogue = new[] { "*Whirrr-click* Systems optimal! Ready for transit.", "Hoverpad energy matrices recharged to 100%.", "Have a pleasant descent down the imperial terraces, Summoner!" } },

                // Upper Ring (Tier 3)
                new { name = "Master Blacksmith Torvald", role = "Grand Armory & Forge", prefab = mRpg, pos = new Vector3(115f, Tier3Height, 10f), yaw = 270f,
                      dialogue = new[] { "Hah! Smells like tempered steel and dragon breath!", "Bring me Mithril ores and mana cores. I'll forge blades that slice clean through stone.", "Remember to quench at the chime peak!" } },

                new { name = "Alchemist Lysandra", role = "Mages Quarter Apothecary", prefab = fGenshin, pos = new Vector3(-105f, Tier3Height, 35f), yaw = 135f,
                      dialogue = new[] { "Careful around the alembics! That brew is volatile.", "Spire mushrooms make potent instability antidotes.", "Stock up on Elixirs of Haste before entering deep dungeon modules." } },

                new { name = "Guild Registrar Donald", role = "Expedition Bureau", prefab = mGenshin, pos = new Vector3(10f, Tier3Height, 115f), yaw = 180f,
                      dialogue = new[] { "Looking for bounties? The Adventurers Guild always has open contracts.", "Team synergy combos yield 1.35x rewards on registered boss kills.", "Register your party before departing!" } },

                // Civic District (Tier 2)
                new { name = "Baker Pierre", role = "Aethelburg Delicacies", prefab = mRpg, pos = new Vector3(25f, Tier2Height, -190f), yaw = 0f,
                      dialogue = new[] { "Fresh croissants and hot brioche, straight from the brick oven!", "A warm potluck meal increases party critical rate by 15%.", "Don't explore the spire on an empty stomach!" } },

                new { name = "Merchant Cedric", role = "Bazaar Quartermaster", prefab = mGenshin, pos = new Vector3(135f, Tier2Height, -135f), yaw = 315f,
                      dialogue = new[] { "Rare trinkets, enchanted rings, and mysterious summoning tomes!", "Claim a personal storefront plot along the avenue to earn offline gold.", "Business has never been better!" } },

                new { name = "Beastmaster Kaelen", role = "Mount Stablemaster", prefab = fRpg, pos = new Vector3(-190f, Tier2Height, 15f), yaw = 90f,
                      dialogue = new[] { "These armored wolves were bred for the 8 Grand Avenues.", "Hold reins tight and press sprint to clear the wide bridges.", "Never mistreat a chocobo—they hold grudges!" } },

                // Ground Ring & Gates (Tier 1)
                new { name = "Gatekeeper Sarah", role = "North Bastion Sentinel", prefab = fGenshin, pos = new Vector3(0f, Tier1Height, 290f), yaw = 180f,
                      dialogue = new[] { "Halt! Beyond this moat lies the wild exterior perimeter.", "Monsters roam the mistwood and volcanic crags.", "Ensure your weapons are tempered before stepping onto the outer bridge." } },

                new { name = "Veteran Scout Roland", role = "South Moat Guard", prefab = mRpg, pos = new Vector3(0f, Tier1Height, -290f), yaw = 0f,
                      dialogue = new[] { "I've patrolled these bridges for twenty years.", "The bridges were widened to let whole phalanxes march side-by-side.", "Keep your eyes on the southern horizon!" } },

                new { name = "Wandering Bard Lyra", role = "Street Minstrel", prefab = fRpg, pos = new Vector3(45f, Tier3Height, -45f), yaw = 300f,
                      dialogue = new[] { "♪ 'O Hundredfold Spire, reach to the star, where ancient summoners fought near and far...' ♪", "Care to hear the ballad of the First Guardian?", "May music soothe your blade!" } }
            };

            foreach (var cfg in npcConfigs)
            {
                if (cfg.prefab == null) continue;

                GameObject npcObj = (GameObject)PrefabUtility.InstantiatePrefab(cfg.prefab, npcsRoot.transform);
                npcObj.name = $"NPC_{cfg.name.Replace(" ", "_")}";
                npcObj.transform.position = cfg.pos;
                npcObj.transform.rotation = Quaternion.Euler(0f, cfg.yaw, 0f);

                // Ensure upright 1.0 humanoid scale
                npcObj.transform.localScale = Vector3.one;

                // Configure CapsuleCollider for interaction
                var cap = npcObj.GetComponent<CapsuleCollider>();
                if (cap == null) cap = npcObj.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 1.0f, 0f);
                cap.height = 2.0f;
                cap.radius = 0.55f;
                cap.isTrigger = true;

                // Attach OverworldNPCInteractable
                var interactable = npcObj.AddComponent<OverworldNPCInteractable>();
                interactable.npcName = cfg.name;
                interactable.npcRole = cfg.role;
                interactable.dialogueLines = cfg.dialogue;
                interactable.interactionRadius = 4.2f;

                EditorUtility.SetDirty(npcObj);
            }

            Debug.Log($"[Streets Populator] Spawned {npcConfigs.Length} interactive land NPCs across Citadel, Promenade, Bazaar, and Bastion Gates.");
#endif
        }

        private Material LoadStoneMaterial()
        {
#if UNITY_EDITOR
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/OverworldPieces/Materials/M_Prop_Stone_Stairs.mat")
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/World/Mat_World_TownWall.mat");
            if (mat != null) return mat;
#endif
            return new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
    }
}
