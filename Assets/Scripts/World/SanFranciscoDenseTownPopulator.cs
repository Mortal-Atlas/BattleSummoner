using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.World
{
    [Serializable]
    public struct DenseRowBuildingSpec
    {
        public GameObject prefab;
        [Tooltip("Exact lateral frontage width of the building mesh in meters.")]
        public float frontageWidthMeters;
        [Tooltip("Depth of the building footprint from the street curb.")]
        public float lotDepthMeters;
    }

    public class SanFranciscoDenseTownPopulator : MonoBehaviour
    {
        [Header("Town Tier & Elevation Geometry")]
        public float townOuterRadius = 300f;
        public float centerChurchPeakElevation = 18f;
        public LayerMask groundRaycastMask = ~0;
        public bool snapToGroundSurface = true;

        [Header("Mount-Accessible Street Clearances")]
        [Tooltip("Wide concentric street width so ridable mounts & squads pass freely.")]
        public float concentricBoulevardWidth = 14f;
        [Tooltip("Wide radial avenue width cutting from the outer walls up to the Central Church.")]
        public float radialMountAvenueWidth = 16f;
        public int numberOfRadialAvenues = 8; // 8-way radial avenues matching Aethelburg Metropolis

        [Header("San Francisco Row-House Packing Rules")]
        [Tooltip("Wall-to-wall gap between adjacent buildings (0.15m - 0.45m for tight row-house packing).")]
        public Vector2 wallToWallGapRange = new Vector2(0.15f, 0.45f);
        [Tooltip("Vertical elevation jitter and slight facade depth stagger for organic SF hillside silhouettes.")]
        public float facadeDepthStaggerMax = 0.8f;
        [Tooltip("Insert a narrow pedestrian side-alley only after every N packed buildings.")]
        public Vector2Int buildingsPerContiguousBlock = new Vector2Int(5, 9);
        public float narrowAlleyWidth = 2.6f;

        [Header("Building Prefab Catalogs (With Frontage Widths)")]
        public DenseRowBuildingSpec[] outerRingRowHousesAndBazaars;
        public DenseRowBuildingSpec[] midTierSteppedTownhomes;
        public DenseRowBuildingSpec[] upperTierSanctuaryTownhomes;
        public DenseRowBuildingSpec[] cornerBakeryAndShopPrefabs;

        [ContextMenu("0. Auto-Assign Metropolis Catalog")]
        public void AutoAssignMetropolisCatalog()
        {
#if UNITY_EDITOR
            var lowHouse = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_House_LowDistrict.fbx");
            var resHouse = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_House_Residential.fbx");
            var inn = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Inn_Lodge.fbx");
            var alchemist = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_AlchemistWorkshop.fbx");
            var enchanter = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_EnchanterWorkshop.fbx");
            var armory = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_GuildArmory.fbx");
            var bakery = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Bakery_Shop.fbx");
            var magicStore = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_MagicItemStore.fbx");
            var forge = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Blacksmith_OpenForge.fbx");
            var tavern = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Tavern_Open.fbx");
            var guildhall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Building_Guildhall_Grand.fbx");

            outerRingRowHousesAndBazaars = new DenseRowBuildingSpec[]
            {
                new DenseRowBuildingSpec { prefab = lowHouse, frontageWidthMeters = 7.6f, lotDepthMeters = 9.0f },
                new DenseRowBuildingSpec { prefab = resHouse, frontageWidthMeters = 9.5f, lotDepthMeters = 11.5f },
                new DenseRowBuildingSpec { prefab = inn, frontageWidthMeters = 12.0f, lotDepthMeters = 15.9f }
            };

            midTierSteppedTownhomes = new DenseRowBuildingSpec[]
            {
                new DenseRowBuildingSpec { prefab = resHouse, frontageWidthMeters = 9.5f, lotDepthMeters = 11.5f },
                new DenseRowBuildingSpec { prefab = forge, frontageWidthMeters = 14.3f, lotDepthMeters = 11.7f },
                new DenseRowBuildingSpec { prefab = enchanter, frontageWidthMeters = 11.2f, lotDepthMeters = 11.2f },
                new DenseRowBuildingSpec { prefab = tavern, frontageWidthMeters = 13.8f, lotDepthMeters = 16.4f },
                new DenseRowBuildingSpec { prefab = bakery, frontageWidthMeters = 9.5f, lotDepthMeters = 11.2f }
            };

            upperTierSanctuaryTownhomes = new DenseRowBuildingSpec[]
            {
                new DenseRowBuildingSpec { prefab = guildhall, frontageWidthMeters = 17.6f, lotDepthMeters = 22.8f },
                new DenseRowBuildingSpec { prefab = resHouse, frontageWidthMeters = 9.5f, lotDepthMeters = 11.5f },
                new DenseRowBuildingSpec { prefab = inn, frontageWidthMeters = 12.0f, lotDepthMeters = 15.9f },
                new DenseRowBuildingSpec { prefab = magicStore, frontageWidthMeters = 9.8f, lotDepthMeters = 11.7f },
                new DenseRowBuildingSpec { prefab = enchanter, frontageWidthMeters = 11.2f, lotDepthMeters = 11.2f }
            };

            cornerBakeryAndShopPrefabs = new DenseRowBuildingSpec[]
            {
                new DenseRowBuildingSpec { prefab = bakery, frontageWidthMeters = 9.5f, lotDepthMeters = 11.2f },
                new DenseRowBuildingSpec { prefab = magicStore, frontageWidthMeters = 9.8f, lotDepthMeters = 11.7f }
            };

            EditorUtility.SetDirty(this);
            Debug.Log("[SF Dense Populator] Auto-assigned Metropolis catalog with exact frontage widths!");
#endif
        }

        private List<Vector2> existingBuildingPositions = new List<Vector2>();

        [ContextMenu("Rebuild Town With San Francisco High-Density Packing")]
        public void PopulateHighDensityTown()
        {
            ClearExistingBuildings();
            AutoAssignMetropolisCatalog();
            CollectExistingBuildingPositions();

            System.Random rng = new System.Random(10172026);

            // Street center radii and exact ribbon half-widths aligned with Metropolis terraces:
            // Tier 1: R = 265m (Street between R=257m and R=273m, halfWidth=8m)
            // Tier 2: R = 195m (Street between R=188m and R=202m, halfWidth=7m)
            // Tier 3: R = 115m (Street between R=109m and R=121m, halfWidth=6m)
            float[] streetCenterRadii = { 265f, 195f, 115f };
            float[] streetHalfWidths = { 8.0f, 7.0f, 6.0f };

            for (int s = 0; s < streetCenterRadii.Length; s++)
            {
                float streetRadius = streetCenterRadii[s];
                float halfWidth = streetHalfWidths[s];
                float innerCurbRadius = streetRadius - halfWidth - 0.5f;
                float outerCurbRadius = streetRadius + halfWidth + 0.5f;

                PackContiguousRowAlongCurb(innerCurbRadius, facingOutward: true, rng);
                PackContiguousRowAlongCurb(outerCurbRadius, facingOutward: false, rng);
            }

            StaticBatchingUtility.Combine(gameObject);
            Debug.Log($"<color=#76FF03><b>[SF Dense Town Populator] Successfully populated {transform.childCount} contiguous row houses with ZERO floating gaps & Static Batching!</b></color>");
        }

        private void CollectExistingBuildingPositions()
        {
            existingBuildingPositions.Clear();
            var districts = GameObject.Find("06_Blueprint_City_Districts");
            if (districts != null)
            {
                foreach (var r in districts.GetComponentsInChildren<MeshRenderer>())
                {
                    existingBuildingPositions.Add(new Vector2(r.transform.position.x, r.transform.position.z));
                }
            }
            var housing = GameObject.Find("BlueprintTownHousingManager");
            if (housing != null)
            {
                foreach (var r in housing.GetComponentsInChildren<MeshRenderer>())
                {
                    existingBuildingPositions.Add(new Vector2(r.transform.position.x, r.transform.position.z));
                }
            }
        }

        private bool IsPositionOccupied(Vector2 pos, float clearanceRadius)
        {
            for (int i = 0; i < existingBuildingPositions.Count; i++)
            {
                if (Vector2.Distance(pos, existingBuildingPositions[i]) < clearanceRadius) return true;
            }
            return false;
        }

        private void PackContiguousRowAlongCurb(float curbRadius, bool facingOutward, System.Random rng)
        {
            if (curbRadius <= 78f || curbRadius >= townOuterRadius - 10f) return;

            float circumference = 2f * Mathf.PI * curbRadius;
            float currentArcMeters = 0f;

            int buildingsInCurrentBlock = 0;
            int targetBlockLength = rng.Next(buildingsPerContiguousBlock.x, buildingsPerContiguousBlock.y + 1);

            while (currentArcMeters < circumference)
            {
                float angleRad = currentArcMeters / curbRadius;

                // 1. Check if current arc intersects a wide Radial Mount Avenue (preserve 16m avenues)
                if (IsInsideRadialMountAvenue(angleRad, curbRadius))
                {
                    currentArcMeters += 3.0f;
                    buildingsInCurrentBlock = 0;
                    continue;
                }

                // 2. Pick building spec based on elevation tier (corner plots become Bakeries/Shops)
                bool isBlockCorner = (buildingsInCurrentBlock == 0 || buildingsInCurrentBlock == targetBlockLength - 1);
                DenseRowBuildingSpec spec = SelectBuildingSpec(curbRadius, isBlockCorner, rng);
                if (spec.prefab == null) break;

                float halfWidth = Mathf.Max(2.5f, spec.frontageWidthMeters * 0.5f);
                float centerArc = currentArcMeters + halfWidth;
                if (centerArc >= circumference) break;

                float centerAngleRad = centerArc / curbRadius;

                // Verify the far edge of the building doesn't clip into a radial avenue
                if (IsInsideRadialMountAvenue((currentArcMeters + spec.frontageWidthMeters) / curbRadius, curbRadius))
                {
                    currentArcMeters += 3.5f;
                    continue;
                }

                // 3. Calculate stepped San Francisco hillside elevation + curb depth alignment
                float depthStagger = ((float)rng.NextDouble() * 2f - 1f) * facadeDepthStaggerMax;
                float halfLotDepth = spec.lotDepthMeters * 0.5f;
                // For inner row (facing outward), front of building touches curbRadius, so center is inner (smaller radius).
                // For outer row (facing inward), front of building touches curbRadius, so center is outer (larger radius).
                float finalRadius = facingOutward
                    ? (curbRadius - halfLotDepth - depthStagger)
                    : (curbRadius + halfLotDepth + depthStagger);

                // Check landmark proximity clearance (skip if too close to key landmarks)
                if (IsNearMajorLandmark(centerAngleRad, finalRadius))
                {
                    currentArcMeters += spec.frontageWidthMeters + 2f;
                    continue;
                }

                Vector2 candidateXZ = new Vector2(Mathf.Sin(centerAngleRad) * finalRadius, Mathf.Cos(centerAngleRad) * finalRadius);
                if (IsPositionOccupied(candidateXZ, spec.frontageWidthMeters * 0.5f + 2f))
                {
                    currentArcMeters += spec.frontageWidthMeters + 1f;
                    continue;
                }

                // Determine base elevation from stepped terrace hierarchy:
                // Tier 4: R <= 95m => 80.0m
                // Tier 3: 95m < R <= 195m => 58.0m
                // Tier 2: 195m < R <= 310m => 38.0m
                // Tier 1: 310m < R <= 450m => 22.0m
                float steppedTierY = (finalRadius <= 95f)  ? 80.0f :
                                     (finalRadius <= 195f) ? 58.0f :
                                     (finalRadius <= 310f) ? 38.0f : 22.0f;

                Vector3 worldPos = new Vector3(
                    Mathf.Sin(centerAngleRad) * finalRadius,
                    steppedTierY,
                    Mathf.Cos(centerAngleRad) * finalRadius
                );

                // Ensure NO FLOATING OR BURIED BUILDINGS: Raycast snap to terrace surface collider
                if (snapToGroundSurface)
                {
                    Vector3 rayOrigin = new Vector3(worldPos.x, steppedTierY + 15f, worldPos.z);
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f, groundRaycastMask))
                    {
                        worldPos.y = hit.point.y;
                    }
                }

                // Orient facade directly toward the wide mount-accessible street
                Vector3 dirOut = new Vector3(Mathf.Sin(centerAngleRad), 0f, Mathf.Cos(centerAngleRad));
                Quaternion rotation = Quaternion.LookRotation(facingOutward ? dirOut : -dirOut, Vector3.up);

                GameObject instance;
#if UNITY_EDITOR
                instance = (GameObject)PrefabUtility.InstantiatePrefab(spec.prefab, transform);
                if (instance == null) instance = Instantiate(spec.prefab, worldPos, rotation, transform);
                else
                {
                    instance.transform.position = worldPos;
                    instance.transform.rotation = rotation;
                }
#else
                instance = Instantiate(spec.prefab, worldPos, rotation, transform);
#endif
                instance.transform.localScale = Vector3.one;
                instance.name = $"SF_RowHouse_R{Mathf.RoundToInt(curbRadius)}_{buildingsInCurrentBlock}_{spec.prefab.name}";

                ApplyCorrectedMeshes(instance);

                // Auto-register to Spire Spatial Culling Manager (30ft + frustum culling)
                var prop = instance.GetComponent<BattleSummoner.Optimization.CullableWorldProp>();
                if (prop == null) prop = instance.AddComponent<BattleSummoner.Optimization.CullableWorldProp>();
                prop.boundingRadius = Mathf.Max(spec.frontageWidthMeters, spec.lotDepthMeters) * 0.75f;
                prop.isDynamicMover = false;

                // 4. Advance cursor by exact building width + ultra-tight wall-to-wall gap (NO equidistant spacing!)
                float tightGap = Mathf.Lerp(wallToWallGapRange.x, wallToWallGapRange.y, (float)rng.NextDouble());
                currentArcMeters += spec.frontageWidthMeters + tightGap;
                buildingsInCurrentBlock++;

                // Inject a narrow service alley after a dense contiguous row block
                if (buildingsInCurrentBlock >= targetBlockLength)
                {
                    currentArcMeters += narrowAlleyWidth;
                    buildingsInCurrentBlock = 0;
                    targetBlockLength = rng.Next(buildingsPerContiguousBlock.x, buildingsPerContiguousBlock.y + 1);
                }
            }
        }

        private bool IsInsideRadialMountAvenue(float angleRad, float radius)
        {
            float sectorAngle = (Mathf.PI * 2f) / numberOfRadialAvenues;
            float nearestAvenueAngle = Mathf.Round(angleRad / sectorAngle) * sectorAngle;
            float angularDistance = Mathf.Abs(Mathf.DeltaAngle(angleRad * Mathf.Rad2Deg, nearestAvenueAngle * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
            float lateralMetersFromAvenueCenter = angularDistance * radius;

            return lateralMetersFromAvenueCenter < (radialMountAvenueWidth * 0.5f);
        }

        private static readonly Vector2[] LandmarkLocations = new Vector2[]
        {
            new Vector2(0f, 20f),     // Cathedral
            new Vector2(-35f, 35f),   // Arcane Spire
            new Vector2(145f, -135f), // Clock Tower
            new Vector2(185f, -200f), // Royal Mount Stables
            new Vector2(85f, 85f),    // Sparring Arena
            new Vector2(65f, 105f),   // Barracks Armory
            new Vector2(115f, 15f),   // Grand Guildhall
            new Vector2(105f, -45f)   // Arcane Academy
        };

        private bool IsNearMajorLandmark(float angleRad, float radius)
        {
            Vector2 pos = new Vector2(Mathf.Sin(angleRad) * radius, Mathf.Cos(angleRad) * radius);
            for (int i = 0; i < LandmarkLocations.Length; i++)
            {
                if (Vector2.Distance(pos, LandmarkLocations[i]) < 18f) return true;
            }
            return false;
        }

        private DenseRowBuildingSpec SelectBuildingSpec(float radius, bool isCornerPlot, System.Random rng)
        {
            if (isCornerPlot && cornerBakeryAndShopPrefabs != null && cornerBakeryAndShopPrefabs.Length > 0)
            {
                return cornerBakeryAndShopPrefabs[rng.Next(0, cornerBakeryAndShopPrefabs.Length)];
            }

            float norm = radius / townOuterRadius;
            DenseRowBuildingSpec[] pool = norm > 0.68f ? outerRingRowHousesAndBazaars :
                                          norm > 0.42f ? midTierSteppedTownhomes :
                                                         upperTierSanctuaryTownhomes;

            if (pool == null || pool.Length == 0) return new DenseRowBuildingSpec { frontageWidthMeters = 6f };
            return pool[rng.Next(0, pool.Length)];
        }

        public void ClearExistingBuildings()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
                DestroyImmediate(child);
#else
                Destroy(child);
#endif
            }
        }

        private static void ApplyCorrectedMeshes(GameObject go)
        {
#if UNITY_EDITOR
            var mfs = go.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                string mName = mf.sharedMesh.name;
                if (!mName.EndsWith("_Corrected"))
                {
                    string correctedPath = $"Assets/Models/Metropolis/{mName}_Corrected.asset";
                    var corrected = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(correctedPath);
                    if (corrected != null) mf.sharedMesh = corrected;
                }
            }

            var mrs = go.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in mrs)
            {
                var sharedMats = mr.sharedMaterials;
                bool dirty = false;
                for (int m = 0; m < sharedMats.Length; m++)
                {
                    var mat = sharedMats[m];
                    if (mat == null) continue;
                    string matPath = $"Assets/Materials/Metropolis/{mat.name}.mat";
                    var standAloneMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (standAloneMat != null && standAloneMat != mat)
                    {
                        sharedMats[m] = standAloneMat;
                        mat = standAloneMat;
                        dirty = true;
                    }
                    if (mat.HasProperty("_Cull") && mat.GetFloat("_Cull") != 0f)
                    {
                        mat.SetFloat("_Cull", 0f);
                        mat.doubleSidedGI = true;
                        UnityEditor.EditorUtility.SetDirty(mat);
                    }
                }
                if (dirty) mr.sharedMaterials = sharedMats;
            }
#endif
        }
    }
}
