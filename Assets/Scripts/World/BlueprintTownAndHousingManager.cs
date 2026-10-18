using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.World
{
    [Serializable]
    public class TownPlotNode
    {
        public string plotId;
        public Vector3 worldPosition;
        public Vector2 normalizedDiagramCoords => new Vector2(worldPosition.x, worldPosition.z).normalized;
        public float facingAngleY;
        public bool isOwnedByPlayer;
        public string ownerPlayerId;
        public List<string> offlineShopItemIds = new List<string>();
        public GameObject spawnedBuildingInstance;
        public GameObject spawnedStorefrontTable;
    }

    /// <summary>
    /// Section 2: San Francisco High-Density Hillside Town Population & Player Home/Shop Ownership.
    /// Replaces sparse ring logic with shoulder-to-shoulder packed blocks climbing steep hillside slopes
    /// toward the Central Church, preserving wide 16m radial mount avenues and ring streets.
    /// </summary>
    [ExecuteInEditMode]
    public class BlueprintTownAndHousingManager : MonoBehaviour
    {
        public static BlueprintTownAndHousingManager Instance { get; private set; }

        [Header("Antigravity Diagram & San Francisco Hillside Density")]
        public Texture2D antigravityTownBlueprintDiagram;
        public float kingdomOuterWallRadius = 290f;
        public float centerChurchPeakElevation = 85f;

        [Tooltip("Width of a single row-house footprint so buildings pack wall-to-wall like San Francisco.")]
        public float averageRowHouseWidth = 7.2f;
        [Tooltip("Wide street clearance reserved for mounts & carriages between packed blocks.")]
        public float wideMountAvenueWidth = 16.0f;
        public float ringStreetGapWidth = 14.0f;

        [Header("Prefabs")]
        public GameObject[] residentialRowHousePrefabs;
        public GameObject[] bakeryAndShopPrefabs;
        public GameObject playerStorefrontTablePrefab;
        public GameObject weekendFestivalLanternsRoot;

        [Header("Town Plots Ledger")]
        public List<TownPlotNode> townPlots = new List<TownPlotNode>();

        public event Action<string, string> OnPlotClaimedByPlayer;
        public event Action<bool> OnWeekendFestivalStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
            EvaluateWeekendFestivalState();
        }

        private void Start()
        {
            EvaluateWeekendFestivalState();
        }

#if UNITY_EDITOR
        [ContextMenu("0. Auto-Load Metropolis Assets From Project")]
        public void AutoAssignBlueprintAssets()
        {
            if (antigravityTownBlueprintDiagram == null)
            {
                antigravityTownBlueprintDiagram = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Screenshots/blueprint_aerial_overview_final.png")
                    ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Screenshots/populated_town_blueprint_aerial.png");
            }

            bool needsReassign = residentialRowHousePrefabs == null || residentialRowHousePrefabs.Length == 0;
            if (needsReassign)
            {
                residentialRowHousePrefabs = LoadAssetsMatching(
                    "Assets/Models/Metropolis/Building_House_Residential.fbx",
                    "Assets/Models/Metropolis/Building_House_LowDistrict.fbx",
                    "Assets/Models/Metropolis/Building_Inn_Lodge.fbx",
                    "Assets/Models/Metropolis/Building_EnchanterWorkshop.fbx"
                );
            }

            if (bakeryAndShopPrefabs == null || bakeryAndShopPrefabs.Length == 0)
            {
                bakeryAndShopPrefabs = LoadAssetsMatching(
                    "Assets/Models/Metropolis/Building_Bakery_Shop.fbx",
                    "Assets/Models/Metropolis/Building_MagicItemStore.fbx",
                    "Assets/Models/Metropolis/Building_Blacksmith_OpenForge.fbx",
                    "Assets/Models/Metropolis/Building_Tavern_Open.fbx"
                );
            }

            EditorUtility.SetDirty(this);
            Debug.Log($"[Town Housing Manager] Auto-assigned blueprint diagram ({antigravityTownBlueprintDiagram?.name}) and prefab libraries.");
        }

        private GameObject[] LoadAssetsMatching(params string[] paths)
        {
            List<GameObject> list = new List<GameObject>();
            foreach (var p in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) list.Add(go);
            }
            return list.ToArray();
        }
#endif

        [ContextMenu("Populate San Francisco Packed Hillside Town")]
        public void PopulateTownFromDiagram()
        {
#if UNITY_EDITOR
            AutoAssignBlueprintAssets();
#endif
            // Clear existing spawned children if any
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (weekendFestivalLanternsRoot != null && child.gameObject == weekendFestivalLanternsRoot) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            townPlots.Clear();

            // Define tightly stepped hillside terraces from outside the Church plaza out to the Kingdom Walls
            float innerChurchPlazaRadius = 65f;
            float currentRadius = innerChurchPlazaRadius;
            int ringIndex = 0;

            while (currentRadius < kingdomOuterWallRadius - 35f)
            {
                // Pack 2 back-to-back rows of buildings per terrace block before leaving a wide street for mounts
                for (int subRow = 0; subRow < 2; subRow++)
                {
                    float rowRadius = currentRadius + (subRow * 9.5f);
                    float circumference = 2f * Mathf.PI * rowRadius;

                    // Steep San Francisco hillside elevation curve rising toward the Central Church
                    float normalizedDist = rowRadius / kingdomOuterWallRadius;
                    float baseElevation = Mathf.Pow(1f - normalizedDist, 1.35f) * centerChurchPeakElevation;

                    float cursorArcLength = 0f;
                    int plotIndex = 0;

                    while (cursorArcLength < circumference)
                    {
                        float angleRad = cursorArcLength / rowRadius;
                        float angleDeg = (angleRad * Mathf.Rad2Deg) % 360f;

                        // Keep 4 major radial avenues + 4 secondary diagonal streets wide open for mounts!
                        bool isMajorMountAvenue = IsWithinAvenueCorridor(angleDeg, 90f, wideMountAvenueWidth, rowRadius);
                        bool isSecondaryMountStreet = IsWithinAvenueCorridor(angleDeg - 45f, 90f, wideMountAvenueWidth * 0.75f, rowRadius);

                        if (isMajorMountAvenue || isSecondaryMountStreet)
                        {
                            cursorArcLength += averageRowHouseWidth * 0.5f;
                            continue;
                        }

                        // Pack buildings shoulder-to-shoulder with slight organic width jitter (San Francisco row-house style)
                        float organicWidth = averageRowHouseWidth * UnityEngine.Random.Range(0.88f, 1.12f);
                        float facadeDepthJitter = UnityEngine.Random.Range(-0.55f, 0.55f);
                        float effectiveRadius = rowRadius + facadeDepthJitter;

                        // Terrain height raycast to anchor houses firmly to the hillside
                        float groundY = baseElevation;
                        Vector3 rayOrigin = new Vector3(Mathf.Sin(angleRad) * effectiveRadius, baseElevation + 25f, Mathf.Cos(angleRad) * effectiveRadius);
                        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 60f))
                        {
                            groundY = hit.point.y;
                        }

                        Vector3 worldPos = new Vector3(
                            Mathf.Sin(angleRad) * effectiveRadius,
                            groundY,
                            Mathf.Cos(angleRad) * effectiveRadius
                        );

                        // Face the nearest street (subRow 0 faces uphill toward Church; subRow 1 faces downhill street)
                        float inwardAngle = Mathf.Atan2(-worldPos.x, -worldPos.z) * Mathf.Rad2Deg;
                        float facingAngle = (subRow == 0) ? inwardAngle : inwardAngle + 180f;

                        TownPlotNode plot = new TownPlotNode
                        {
                            plotId = $"Tier{ringIndex}_R{subRow}_House{plotIndex}",
                            worldPosition = worldPos,
                            facingAngleY = facingAngle
                        };

                        bool isCornerOrMainShop = (plotIndex % 5 == 0);
                        GameObject[] pool = (isCornerOrMainShop && bakeryAndShopPrefabs != null && bakeryAndShopPrefabs.Length > 0)
                            ? bakeryAndShopPrefabs
                            : residentialRowHousePrefabs;

                        GameObject prefab = (pool != null && pool.Length > 0)
                            ? pool[UnityEngine.Random.Range(0, pool.Length)]
                            : null;

                        if (prefab != null)
                        {
                            Quaternion rot = Quaternion.Euler(0f, facingAngle, 0f);
                            GameObject instance;
#if UNITY_EDITOR
                            instance = PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
                            if (instance == null) instance = Instantiate(prefab, worldPos, rot, transform);
                            else
                            {
                                instance.transform.position = worldPos;
                                instance.transform.rotation = rot;
                            }
#else
                            instance = Instantiate(prefab, worldPos, rot, transform);
#endif
                            // Subtle vertical scale variation so packed roofs step organically like San Francisco Victorian rows
                            float heightScale = UnityEngine.Random.Range(0.90f, 1.18f);
                            instance.transform.localScale = new Vector3(1f, heightScale, 1f);
                            instance.name = $"Plot_{plot.plotId}_{prefab.name}";
                            plot.spawnedBuildingInstance = instance;
                        }

                        townPlots.Add(plot);
                        cursorArcLength += organicWidth; // Immediately abut the next building wall-to-wall!
                        plotIndex++;
                    }
                }

                // Step outward across a wide mount-accessible ring street to the next packed block
                currentRadius += (9.5f * 2f) + ringStreetGapWidth;
                ringIndex++;
            }

            Debug.Log($"[San Francisco Hillside Builder] Packed {townPlots.Count} row-houses and shops with wide mount avenues!");
        }

        private bool IsWithinAvenueCorridor(float angleDeg, float repeatIntervalDeg, float corridorWidthMeters, float radius)
        {
            float halfAngleSpan = Mathf.Max(8.2f, ((corridorWidthMeters * 0.5f) / Mathf.Max(1f, radius)) * Mathf.Rad2Deg);
            float mod = Mathf.Abs((angleDeg + 360f) % repeatIntervalDeg);
            return (mod < halfAngleSpan) || (repeatIntervalDeg - mod < halfAngleSpan);
        }

        public bool ClaimPlayerHousePlot(string plotId, string playerId, int manaCoreCost, ref int playerManaCores)
        {
            var plot = townPlots.Find(p => p.plotId == plotId);
            if (plot == null || plot.isOwnedByPlayer || playerManaCores < manaCoreCost) return false;

            playerManaCores -= manaCoreCost;
            plot.isOwnedByPlayer = true;
            plot.ownerPlayerId = playerId;
            OnPlotClaimedByPlayer?.Invoke(plotId, playerId);
            return true;
        }

        public bool DeployPersonalStorefront(string plotId, List<string> shopItemIds)
        {
            var plot = townPlots.Find(p => p.plotId == plotId);
            if (plot == null || !plot.isOwnedByPlayer) return false;
            plot.offlineShopItemIds = new List<string>(shopItemIds);
            return true;
        }

        public void EvaluateWeekendFestivalState()
        {
            DayOfWeek today = DateTime.Now.DayOfWeek;
            int hour = DateTime.Now.Hour;
            bool isFestivalNight = (today == DayOfWeek.Friday || today == DayOfWeek.Saturday) && (hour >= 18 || hour <= 2);

            if (weekendFestivalLanternsRoot != null)
            {
                weekendFestivalLanternsRoot.SetActive(isFestivalNight);
            }
            OnWeekendFestivalStateChanged?.Invoke(isFestivalNight);
        }
    }
}
