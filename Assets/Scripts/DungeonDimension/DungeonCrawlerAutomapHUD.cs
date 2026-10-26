using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.DungeonDimension
{
    public class DungeonCrawlerAutomapHUD : MonoBehaviour
    {
        private static DungeonCrawlerAutomapHUD _instance;
        public static DungeonCrawlerAutomapHUD Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<DungeonCrawlerAutomapHUD>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        [Header("UI Roots")]
        [SerializeField] private GameObject mapRoot;
        [SerializeField] private RectTransform mapCanvasRect;
        [SerializeField] private RectTransform mapGridContainer;
        [SerializeField] private RectTransform playerMarkerRect;
        [SerializeField] private TextMeshProUGUI floorBadgeTMP;
        [SerializeField] private TextMeshProUGUI affixTMP;

        private readonly Dictionary<Vector2Int, Image> renderedCells = new Dictionary<Vector2Int, Image>();
        private readonly Dictionary<string, Image> renderedCorridors = new Dictionary<string, Image>();
        private Transform playerTransform;
        private float cellSizePx = 28f;
        private float corridorWidthPx = 8f;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            if (mapRoot == null) mapRoot = gameObject;
        }

        private void Start()
        {
            FindPlayer();
        }

        public void InitializeMap(FloorThemeInfo theme, List<DungeonCellNode> cells)
        {
            if (floorBadgeTMP != null)
            {
                floorBadgeTMP.text = $"<color=#40EBFF><b>[FLOOR B{theme.floorNumber}]</b></color> {theme.floorTitle.ToUpper()}";
            }

            if (affixTMP != null)
            {
                string affixHex = theme.activeAffix == FloorAffix.CrimsonMist ? "FF3355" :
                                  theme.activeAffix == FloorAffix.GildedTreasury ? "FFD700" :
                                  theme.activeAffix == FloorAffix.AetherConvergence ? "40EBFF" : "AAAAAA";
                affixTMP.text = $"<color=#{affixHex}>AFFIX: {theme.activeAffix}</color>";
            }

            ClearMap();
            BuildInitialGridNodes(cells);
        }

        private void ClearMap()
        {
            foreach (var img in renderedCells.Values)
            {
                if (img != null) Destroy(img.gameObject);
            }
            renderedCells.Clear();

            foreach (var img in renderedCorridors.Values)
            {
                if (img != null) Destroy(img.gameObject);
            }
            renderedCorridors.Clear();
        }

        private void BuildInitialGridNodes(List<DungeonCellNode> cells)
        {
            if (mapGridContainer == null) return;

            // Find bounds
            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var c in cells)
            {
                min = Vector2Int.Min(min, c.gridPos);
                max = Vector2Int.Max(max, c.gridPos);
            }

            Vector2 centerOffset = (Vector2)(min + max) * 0.5f;

            foreach (var cell in cells)
            {
                GameObject nodeObj = new GameObject($"MapNode_{cell.gridPos.x}_{cell.gridPos.y}");
                nodeObj.transform.SetParent(mapGridContainer, false);
                RectTransform rt = nodeObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(cellSizePx, cellSizePx);
                rt.anchoredPosition = new Vector2((cell.gridPos.x - centerOffset.x) * (cellSizePx + 14f),
                                                  (cell.gridPos.y - centerOffset.y) * (cellSizePx + 14f));

                Image img = nodeObj.AddComponent<Image>();
                img.color = GetArchetypeColor(cell.archetype);

                // Initially hidden in fog-of-war except entry
                bool startVisible = cell.archetype == DungeonRoomArchetype.EntrySanctuary;
                img.enabled = startVisible;
                cell.isExplored = startVisible;

                renderedCells[cell.gridPos] = img;

                // Corridors
                if (cell.HasDoor(DoorwayDirection.East))
                {
                    CreateCorridorVisual(cell.gridPos, cell.gridPos + new Vector2Int(1, 0), centerOffset, true);
                }
                if (cell.HasDoor(DoorwayDirection.North))
                {
                    CreateCorridorVisual(cell.gridPos, cell.gridPos + new Vector2Int(0, 1), centerOffset, false);
                }
            }
        }

        private void CreateCorridorVisual(Vector2Int from, Vector2Int to, Vector2 centerOffset, bool isHorizontal)
        {
            string key = $"{Mathf.Min(from.x, to.x)}_{Mathf.Min(from.y, to.y)}_to_{Mathf.Max(from.x, to.x)}_{Mathf.Max(from.y, to.y)}";
            if (renderedCorridors.ContainsKey(key)) return;

            GameObject corObj = new GameObject($"MapCorridor_{key}");
            corObj.transform.SetParent(mapGridContainer, false);
            RectTransform rt = corObj.AddComponent<RectTransform>();

            float spacing = cellSizePx + 14f;
            Vector2 midGrid = ((Vector2)from + (Vector2)to) * 0.5f;
            rt.anchoredPosition = new Vector2((midGrid.x - centerOffset.x) * spacing, (midGrid.y - centerOffset.y) * spacing);

            if (isHorizontal) rt.sizeDelta = new Vector2(14f, corridorWidthPx);
            else rt.sizeDelta = new Vector2(corridorWidthPx, 14f);

            Image img = corObj.AddComponent<Image>();
            img.color = new Color(0.6f, 0.65f, 0.75f, 0.85f);
            img.enabled = false; // Hidden in fog until explored
            renderedCorridors[key] = img;
        }

        public void RevealRoom(Vector2Int gridPos)
        {
            if (renderedCells.TryGetValue(gridPos, out var img))
            {
                img.enabled = true;
            }

            // Reveal adjacent connecting corridors
            Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            foreach (var d in dirs)
            {
                Vector2Int n = gridPos + d;
                string key = $"{Mathf.Min(gridPos.x, n.x)}_{Mathf.Min(gridPos.y, n.y)}_to_{Mathf.Max(gridPos.x, n.x)}_{Mathf.Max(gridPos.y, n.y)}";
                if (renderedCorridors.TryGetValue(key, out var corImg))
                {
                    corImg.enabled = true;
                }
            }
        }

        private Color GetArchetypeColor(DungeonRoomArchetype arch)
        {
            return arch switch
            {
                DungeonRoomArchetype.EntrySanctuary => new Color(0.25f, 0.85f, 1.0f, 0.95f),   // Cyan
                DungeonRoomArchetype.CombatChamber => new Color(0.95f, 0.25f, 0.35f, 0.95f),   // Crimson
                DungeonRoomArchetype.TreasureVault => new Color(1.0f, 0.85f, 0.25f, 0.95f),    // Gold
                DungeonRoomArchetype.ArcanaShrine => new Color(0.75f, 0.35f, 1.0f, 0.95f),     // Violet
                DungeonRoomArchetype.TrapGauntlet => new Color(1.0f, 0.55f, 0.15f, 0.95f),     // Orange
                DungeonRoomArchetype.GuardianChamber => new Color(1.0f, 0.10f, 0.20f, 1.0f),   // Royal Red
                _ => Color.white
            };
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            // Update player marker rotation to match 3rd person heading
            if (playerMarkerRect != null)
            {
                float rotY = playerTransform.eulerAngles.y;
                playerMarkerRect.localRotation = Quaternion.Euler(0f, 0f, -rotY);
            }
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null) playerTransform = prog.transform;
            }
        }
    }
}
