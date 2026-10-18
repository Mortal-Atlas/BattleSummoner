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
        public Vector2 normalizedDiagramCoords;
        public float elevationY;
        public float facingAngleY;
        public bool isOwnedByPlayer;
        public string ownerPlayerId;
        public List<string> offlineShopItemIds = new List<string>();
        public GameObject spawnedBuildingInstance;
        public GameObject spawnedStorefrontTable;
    }

    /// <summary>
    /// Blueprint Town House Population & Player Home / Personal Storefront Ownership.
    /// Directly references Antigravity's self-generated circular multi-tiered town diagram.
    /// Stepped elevation rises toward the Central Church of Revival, while wide radial avenues
    /// are strictly preserved for mount squads (wolves, birds, reptiles).
    /// </summary>
    [ExecuteInEditMode]
    public class BlueprintTownAndHousingManager : MonoBehaviour
    {
        public static BlueprintTownAndHousingManager Instance { get; private set; }

        [Header("Antigravity Diagram Reference")]
        [Tooltip("Direct reference to Antigravity's self-generated circular multi-tiered town blueprint diagram.")]
        public Texture2D antigravityTownBlueprintDiagram;
        public float townRadius = 450f;
        public float centerChurchPeakElevation = 85f;

        [Header("Prefabs")]
        public GameObject[] residentialHousePrefabs;
        public GameObject[] bakeryAndShopPrefabs;
        public GameObject playerStorefrontTablePrefab;
        public GameObject weekendFestivalLanternsRoot;

        [Header("Town Plot Ledger")]
        public List<TownPlotNode> townPlots = new List<TownPlotNode>();

        public event Action<string, string> OnPlotClaimedByPlayer; // (plotId, playerId)
        public event Action<string, int> OnStorefrontStocked;     // (plotId, itemCount)
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

        [ContextMenu("0. Auto-Load Metropolis Assets From Project")]
        public void AutoAssignBlueprintAssets()
        {
#if UNITY_EDITOR
            if (antigravityTownBlueprintDiagram == null)
            {
                antigravityTownBlueprintDiagram = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Screenshots/blueprint_aerial_overview_final.png")
                    ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Screenshots/populated_town_blueprint_aerial.png");
            }

            // Ensure clean Metropolis catalog free of raw oversized FBXs
            bool needsReassign = residentialHousePrefabs == null || residentialHousePrefabs.Length == 0;
            if (!needsReassign)
            {
                foreach (var p in residentialHousePrefabs)
                {
                    if (p == null || p.name.Contains("Shop_Building_Exterior") || p.name.Contains("Alchemist") || p.name.Contains("Armory"))
                    {
                        needsReassign = true;
                        break;
                    }
                }
            }

            if (needsReassign)
            {
                residentialHousePrefabs = LoadAssetsMatching(
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
#endif
        }

#if UNITY_EDITOR
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

        [ContextMenu("1. Populate Town From Self-Generated Diagram")]
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

            // Concentric rings matching Antigravity's circular tiered diagram
            float[] ringRadii = { 0.82f, 0.55f, 0.30f };
            int[] counts = { 36, 24, 12 };

            for (int r = 0; r < ringRadii.Length; r++)
            {
                float rad = ringRadii[r];
                int count = counts[r];

                for (int i = 0; i < count; i++)
                {
                    // Leave clear openings for the 8 radial avenues (every 45 degrees)
                    float angleRad = (i / (float)count) * Mathf.PI * 2f;
                    float angleDeg = angleRad * Mathf.Rad2Deg;

                    // Skip plots directly cutting through the 8 grand radial avenues
                    if (Mathf.Abs(angleDeg % 45f) < 8f) continue;

                    Vector2 coords = new Vector2(Mathf.Sin(angleRad) * rad, Mathf.Cos(angleRad) * rad);
                    // The concentric avenue is outward relative to the inner terrace plots.
                    // Facade (+Z) faces outward directly toward the avenue so front doors open to the street.
                    float faceAngle = angleDeg;
                    Vector3 dirOut = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
                    Quaternion houseRotation = Quaternion.LookRotation(dirOut, Vector3.up);

                    // Determine ground elevation from actual terrace surface (prevents floating buildings)
                    float dist = rad * townRadius;
                    float elev = 0f;
                    if (dist <= 75f) elev = 18f;
                    else if (dist <= 155f) elev = 12f;
                    else if (dist <= 235f) elev = 6f;
                    else elev = 0f;

                    Vector3 rayStart = new Vector3(coords.x * townRadius, elev + 20f, coords.y * townRadius);
                    if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, 50f))
                    {
                        elev = groundHit.point.y;
                    }

                    TownPlotNode plot = new TownPlotNode
                    {
                        plotId = $"Tier{r}_Plot{i:D2}",
                        normalizedDiagramCoords = coords,
                        elevationY = elev,
                        facingAngleY = faceAngle
                    };

                    Vector3 worldPos = new Vector3(coords.x * townRadius, elev, coords.y * townRadius);
                    GameObject prefab = null;

                    if (i % 4 == 0 && bakeryAndShopPrefabs != null && bakeryAndShopPrefabs.Length > 0)
                    {
                        prefab = bakeryAndShopPrefabs[(r + i) % bakeryAndShopPrefabs.Length];
                    }
                    else if (residentialHousePrefabs != null && residentialHousePrefabs.Length > 0)
                    {
                        prefab = residentialHousePrefabs[(r + i) % residentialHousePrefabs.Length];
                    }

                    if (prefab != null)
                    {
                        GameObject spawned;
#if UNITY_EDITOR
                        spawned = PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
                        if (spawned == null) spawned = Instantiate(prefab, worldPos, houseRotation, transform);
                        else
                        {
                            spawned.transform.position = worldPos;
                            spawned.transform.rotation = houseRotation;
                        }
#else
                        spawned = Instantiate(prefab, worldPos, houseRotation, transform);
#endif
                        spawned.transform.localScale = Vector3.one;
                        spawned.name = $"Plot_{plot.plotId}_{prefab.name}";
                        plot.spawnedBuildingInstance = spawned;
                    }

                    townPlots.Add(plot);
                }
            }

            Debug.Log($"[Town Housing Manager] Successfully populated {townPlots.Count} stepped circular plots from Antigravity's blueprint diagram!");
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public bool ClaimPlayerHousePlot(string plotId, string playerId, int manaCoreCost, ref int playerManaCores)
        {
            var plot = townPlots.Find(p => p.plotId == plotId);
            if (plot == null || plot.isOwnedByPlayer || playerManaCores < manaCoreCost)
            {
                Debug.LogWarning($"[Town Housing] Cannot claim plot {plotId}: Already owned or insufficient Mana Cores ({playerManaCores}/{manaCoreCost}).");
                return false;
            }

            playerManaCores -= manaCoreCost;
            plot.isOwnedByPlayer = true;
            plot.ownerPlayerId = playerId;

            // Spawn Personal Storefront Table outside the claimed house
            if (playerStorefrontTablePrefab != null && plot.spawnedBuildingInstance != null && plot.spawnedStorefrontTable == null)
            {
                Vector3 tablePos = plot.spawnedBuildingInstance.transform.position + plot.spawnedBuildingInstance.transform.forward * 4.5f;
                plot.spawnedStorefrontTable = Instantiate(playerStorefrontTablePrefab, tablePos, plot.spawnedBuildingInstance.transform.rotation, plot.spawnedBuildingInstance.transform);
                plot.spawnedStorefrontTable.name = $"StorefrontTable_{playerId}";
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    $"[Town Housing] Player '{playerId}' has established a home at {plotId}!", false);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.9f);
            }

            OnPlotClaimedByPlayer?.Invoke(plotId, playerId);
            return true;
        }

        public bool DeployPersonalStorefront(string plotId, List<string> itemIdsToSell)
        {
            var plot = townPlots.Find(p => p.plotId == plotId);
            if (plot == null || !plot.isOwnedByPlayer) return false;

            plot.offlineShopItemIds = new List<string>(itemIdsToSell);
            Debug.Log($"[Personal Storefront] Plot {plotId} stocked with {itemIdsToSell.Count} items for offline autopilot sales.");
            OnStorefrontStocked?.Invoke(plotId, itemIdsToSell.Count);
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

            if (isFestivalNight)
            {
                Debug.Log("🏮 [TOWN FESTIVAL NIGHT] Lantern Festival Active! Floating lanterns, fireworks, and town-wide morale buff enabled.");
            }
        }
    }
}
