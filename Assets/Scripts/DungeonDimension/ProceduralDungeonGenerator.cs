using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.Optimization;

namespace BattleSummoner.DungeonDimension
{
    public class ProceduralDungeonGenerator : MonoBehaviour
    {
        private static ProceduralDungeonGenerator _instance;
        public static ProceduralDungeonGenerator Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<ProceduralDungeonGenerator>(FindObjectsInactive.Include);
                    if (!_instance)
                    {
                        var found = GameObject.Find("[ProceduralDungeonDirector]");
                        if (found != null)
                        {
                            _instance = found.GetComponent<ProceduralDungeonGenerator>();
                        }
                    }
                    if (!_instance)
                    {
                        var go = new GameObject("[ProceduralDungeonDirector]");
                        _instance = go.AddComponent<ProceduralDungeonGenerator>();
                    }
                }
                return _instance;
            }
            set => _instance = value;
        }

        [Header("Generation Parameters")]
        public int minRoomsPerRun = 9;
        public int maxRoomsPerRun = 14;
        public float roomGridSnapSize = 24f;
        public float wallHeight = 5.0f;
        public float doorwayWidth = 2.45f;
        public float doorwayHeight = 3.15f;
        public float corridorWidth = 6.0f;

        [Header("Subterranean Elevation Offset")]
        [Tooltip("Y offset so dungeon exists isolated beneath overworld terrain")]
        public float dungeonElevationY = -400f;

        [Header("Asset Paths (Auto-loaded if null)")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material pillarMaterial;
        public GameObject archedDoorwayPrefab;
        public GameObject fireDishPrefab;
        public GameObject chestPrefab;
        public GameObject bookshelfPrefab;
        public GameObject tablePrefab;
        public GameObject weaponRackPrefab;
        public GameObject barrelPrefab;
        public GameObject cratePrefab;
        public GameObject stoneStairsPrefab;
        public GameObject bossGolemPrefab;
        public GameObject shadowPrefab;

        private readonly List<GameObject> spawnedDungeonPieces = new List<GameObject>();
        private readonly Dictionary<Vector2Int, DungeonCellNode> dungeonGraph = new Dictionary<Vector2Int, DungeonCellNode>();
        private FloorThemeInfo activeTheme;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }
            _instance = this;
            LoadDefaultAssets();
        }

        private void LoadDefaultAssets()
        {
#if UNITY_EDITOR
            if (floorMaterial == null)
                floorMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DarkAcademia_Floor.mat");
            if (wallMaterial == null)
                wallMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_DarkAcademia_GothicWall.mat");
            if (pillarMaterial == null)
                pillarMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DarkAcademia/M_DA_CarvedStone.mat");

            if (archedDoorwayPrefab == null)
                archedDoorwayPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Doors & Doorways/Level_Doorway_Arched.prefab");
            if (fireDishPrefab == null)
                fireDishPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Props/Fire_Dish.prefab");
            if (chestPrefab == null)
                chestPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Furniture/Chest_With_Doors.prefab");
            if (bookshelfPrefab == null)
                bookshelfPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Furniture/Bookshelf_A.prefab");
            if (tablePrefab == null)
                tablePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Furniture/Table_Large.prefab");
            if (weaponRackPrefab == null)
                weaponRackPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Furniture/Weapon_Rack.prefab");
            if (barrelPrefab == null)
                barrelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Props/Barrel.prefab");
            if (cratePrefab == null)
                cratePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Prefabs/Props/Crate_Large.prefab");
            if (bossGolemPrefab == null)
                bossGolemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Golem_AncientMossStone.fbx");
            if (shadowPrefab == null)
                shadowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mini Simple Characters Skeleton Demo/Prefabs/Characters/mini simple skeleton demo.prefab");
#endif
        }

        /// <summary>
        /// Builds a brand-new, non-repeating procedural dungeon floor layout.
        /// Guarantees 100% room connectivity, walk-through arched doorways, and gothic dungeon crawler aesthetic.
        /// </summary>
        public void BuildDungeonFloor(int entropySeed, FloorThemeInfo theme)
        {
            ClearExistingDungeonGeometry();
            LoadDefaultAssets();
            activeTheme = theme;

            System.Random rng = new System.Random(entropySeed);
            int targetRooms = rng.Next(minRoomsPerRun, maxRoomsPerRun + 1);

            // 1. Generate Connected Graph Topology
            GenerateTopologyGraph(targetRooms, rng);

            // 2. Construct 3D Physical Geometry (Floors, Walls, Arches, Braziers)
            ConstructDungeonGeometry(rng);

            // 3. Populate Gameplay Props, Roaming Shadows, Chests, Shrines, and Boss
            PopulateGameplayEntities(rng);

            // 4. Teleport Player to Entry Sanctuary
            TeleportPlayerToEntry();

            // 5. Initialize Minimap
            if (DungeonCrawlerAutomapHUD.Instance != null)
            {
                var cellList = new List<DungeonCellNode>(dungeonGraph.Values);
                DungeonCrawlerAutomapHUD.Instance.InitializeMap(theme, cellList);
            }

            Debug.Log($"🏰 [PROCEDURAL DUNGEON] Floor B{theme.floorNumber} ('{theme.floorTitle}') successfully generated with {dungeonGraph.Count} connected rooms!");
        }

        private void GenerateTopologyGraph(int roomCount, System.Random rng)
        {
            dungeonGraph.Clear();

            Vector2Int current = Vector2Int.zero;
            DungeonCellNode entryNode = new DungeonCellNode
            {
                gridPos = current,
                worldPos = GridToWorld(current),
                archetype = DungeonRoomArchetype.EntrySanctuary,
                distanceFromEntry = 0
            };
            dungeonGraph[current] = entryNode;

            Vector2Int[] cardinals = {
                new Vector2Int(0, 1),   // North
                new Vector2Int(0, -1),  // South
                new Vector2Int(1, 0),   // East
                new Vector2Int(-1, 0)   // West
            };

            List<Vector2Int> occupiedList = new List<Vector2Int> { current };

            // Random Walk with branching to create rich layout
            while (occupiedList.Count < roomCount)
            {
                Vector2Int parent = occupiedList[rng.Next(0, occupiedList.Count)];
                Vector2Int dir = cardinals[rng.Next(0, cardinals.Length)];
                Vector2Int candidate = parent + dir;

                if (!dungeonGraph.ContainsKey(candidate))
                {
                    DungeonCellNode node = new DungeonCellNode
                    {
                        gridPos = candidate,
                        worldPos = GridToWorld(candidate),
                        archetype = DungeonRoomArchetype.CombatChamber,
                        distanceFromEntry = Mathf.Abs(candidate.x) + Mathf.Abs(candidate.y)
                    };

                    dungeonGraph[candidate] = node;
                    occupiedList.Add(candidate);

                    // Connect parent and candidate
                    ConnectCells(parent, candidate);
                }
            }

            // Add Cycle Loops (25% chance per adjacent pair) to ensure multiple interconnected pathways
            foreach (var pos in occupiedList)
            {
                foreach (var d in cardinals)
                {
                    Vector2Int neighbor = pos + d;
                    if (dungeonGraph.ContainsKey(neighbor) && rng.NextDouble() < 0.25)
                    {
                        ConnectCells(pos, neighbor);
                    }
                }
            }

            // Find Furthest Node for Guardian / Boss Chamber
            int maxDist = -1;
            Vector2Int bossPos = current;
            foreach (var kvp in dungeonGraph)
            {
                if (kvp.Value.distanceFromEntry > maxDist)
                {
                    maxDist = kvp.Value.distanceFromEntry;
                    bossPos = kvp.Key;
                }
            }
            dungeonGraph[bossPos].archetype = DungeonRoomArchetype.GuardianChamber;

            // Distribute Special Archetypes (Treasure Vaults, Arcana Shrine, Trap Gauntlet)
            List<Vector2Int> candidateSpecials = new List<Vector2Int>();
            foreach (var kvp in dungeonGraph)
            {
                if (kvp.Key != Vector2Int.zero && kvp.Key != bossPos)
                {
                    candidateSpecials.Add(kvp.Key);
                }
            }

            // Shuffle candidates
            for (int i = candidateSpecials.Count - 1; i > 0; i--)
            {
                int swapIdx = rng.Next(0, i + 1);
                var temp = candidateSpecials[i];
                candidateSpecials[i] = candidateSpecials[swapIdx];
                candidateSpecials[swapIdx] = temp;
            }

            int assigned = 0;
            if (candidateSpecials.Count > assigned) dungeonGraph[candidateSpecials[assigned++]].archetype = DungeonRoomArchetype.TreasureVault;
            if (candidateSpecials.Count > assigned) dungeonGraph[candidateSpecials[assigned++]].archetype = DungeonRoomArchetype.ArcanaShrine;
            if (candidateSpecials.Count > assigned) dungeonGraph[candidateSpecials[assigned++]].archetype = DungeonRoomArchetype.TrapGauntlet;
            if (candidateSpecials.Count > assigned && activeTheme.activeAffix == FloorAffix.GildedTreasury)
            {
                dungeonGraph[candidateSpecials[assigned++]].archetype = DungeonRoomArchetype.TreasureVault;
            }
        }

        private void ConnectCells(Vector2Int a, Vector2Int b)
        {
            if (!dungeonGraph.TryGetValue(a, out var nodeA) || !dungeonGraph.TryGetValue(b, out var nodeB)) return;

            Vector2Int delta = b - a;
            if (delta == new Vector2Int(0, 1)) // b is North of a
            {
                nodeA.AddDoor(DoorwayDirection.North);
                nodeB.AddDoor(DoorwayDirection.South);
            }
            else if (delta == new Vector2Int(0, -1)) // b is South of a
            {
                nodeA.AddDoor(DoorwayDirection.South);
                nodeB.AddDoor(DoorwayDirection.North);
            }
            else if (delta == new Vector2Int(1, 0)) // b is East of a
            {
                nodeA.AddDoor(DoorwayDirection.East);
                nodeB.AddDoor(DoorwayDirection.West);
            }
            else if (delta == new Vector2Int(-1, 0)) // b is West of a
            {
                nodeA.AddDoor(DoorwayDirection.West);
                nodeB.AddDoor(DoorwayDirection.East);
            }
        }

        private Vector3 GridToWorld(Vector2Int grid)
        {
            return new Vector3(grid.x * roomGridSnapSize, dungeonElevationY, grid.y * roomGridSnapSize);
        }

        private void ConstructDungeonGeometry(System.Random rng)
        {
            Transform dungeonRoot = transform;

            foreach (var kvp in dungeonGraph)
            {
                DungeonCellNode cell = kvp.Value;
                GameObject roomObj = new GameObject($"DungeonRoom_{cell.archetype}_{cell.gridPos.x}_{cell.gridPos.y}");
                roomObj.transform.SetParent(dungeonRoot, false);
                roomObj.transform.position = cell.worldPos;
                cell.roomInstance = roomObj;
                spawnedDungeonPieces.Add(roomObj);

                float halfSize = roomGridSnapSize * 0.5f;

                // 1. Floor Slab
                CreateBoxMesh(roomObj, "Floor", new Vector3(0f, -0.3f, 0f), new Vector3(roomGridSnapSize, 0.6f, roomGridSnapSize), floorMaterial);

                // 2. Ceiling Slab
                CreateBoxMesh(roomObj, "Ceiling", new Vector3(0f, wallHeight + 0.3f, 0f), new Vector3(roomGridSnapSize, 0.6f, roomGridSnapSize), floorMaterial);

                // 3. Walls on all 4 Cardinal directions
                // North (+Z)
                ConstructWallOrDoor(roomObj, DoorwayDirection.North, cell.HasDoor(DoorwayDirection.North),
                    new Vector3(0f, 0f, halfSize), Quaternion.Euler(0f, 0f, 0f));

                // South (-Z)
                ConstructWallOrDoor(roomObj, DoorwayDirection.South, cell.HasDoor(DoorwayDirection.South),
                    new Vector3(0f, 0f, -halfSize), Quaternion.Euler(0f, 180f, 0f));

                // East (+X)
                ConstructWallOrDoor(roomObj, DoorwayDirection.East, cell.HasDoor(DoorwayDirection.East),
                    new Vector3(halfSize, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

                // West (-X)
                ConstructWallOrDoor(roomObj, DoorwayDirection.West, cell.HasDoor(DoorwayDirection.West),
                    new Vector3(-halfSize, 0f, 0f), Quaternion.Euler(0f, 270f, 0f));

                // 4. Corner Pillars
                CreatePillar(roomObj, new Vector3(halfSize - 0.5f, 0f, halfSize - 0.5f));
                CreatePillar(roomObj, new Vector3(-halfSize + 0.5f, 0f, halfSize - 0.5f));
                CreatePillar(roomObj, new Vector3(halfSize - 0.5f, 0f, -halfSize + 0.5f));
                CreatePillar(roomObj, new Vector3(-halfSize + 0.5f, 0f, -halfSize + 0.5f));

                // 5. Room Discovery Trigger Volume
                BoxCollider trigger = roomObj.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(roomGridSnapSize - 2f, wallHeight, roomGridSnapSize - 2f);
                trigger.center = new Vector3(0f, wallHeight * 0.5f, 0f);

                var watcher = roomObj.AddComponent<DungeonRoomDiscoveryTrigger>();
                watcher.Initialize(cell);
            }
        }

        private void ConstructWallOrDoor(GameObject parent, DoorwayDirection dir, bool hasDoor, Vector3 localPos, Quaternion localRot)
        {
            GameObject wallGroup = new GameObject($"Wall_{dir}");
            wallGroup.transform.SetParent(parent.transform, false);
            wallGroup.transform.localPosition = localPos;
            wallGroup.transform.localRotation = localRot;

            float wallThick = 0.8f;
            float halfWallLen = roomGridSnapSize;

            if (hasDoor)
            {
                // Flanking wall wings leaving central doorway
                float wingLength = (halfWallLen - doorwayWidth) * 0.5f;
                float wingCenterOffset = (doorwayWidth + wingLength) * 0.5f;

                // Left Wing
                CreateBoxMesh(wallGroup, "Wing_Left", new Vector3(-wingCenterOffset, wallHeight * 0.5f, 0f),
                    new Vector3(wingLength, wallHeight, wallThick), wallMaterial);

                // Right Wing
                CreateBoxMesh(wallGroup, "Wing_Right", new Vector3(wingCenterOffset, wallHeight * 0.5f, 0f),
                    new Vector3(wingLength, wallHeight, wallThick), wallMaterial);

                // Top Lintel Header
                float headerHeight = wallHeight - doorwayHeight;
                if (headerHeight > 0.1f)
                {
                    CreateBoxMesh(wallGroup, "Door_Header", new Vector3(0f, doorwayHeight + headerHeight * 0.5f, 0f),
                        new Vector3(doorwayWidth, headerHeight, wallThick), wallMaterial);
                }

                // Instantiate Authentic Gothic Arched Doorway Frame
                if (archedDoorwayPrefab != null)
                {
                    GameObject arch = Instantiate(archedDoorwayPrefab, wallGroup.transform);
                    arch.name = "Arched_Doorway_Frame";
                    arch.transform.localPosition = Vector3.zero;
                    arch.transform.localRotation = Quaternion.identity;
                    arch.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
                }

                // Mount Flanking Torch Braziers on Left & Right
                if (fireDishPrefab != null)
                {
                    MountTorchBrazier(wallGroup, new Vector3(-doorwayWidth * 0.5f - 0.7f, 1.8f, -0.45f));
                    MountTorchBrazier(wallGroup, new Vector3(doorwayWidth * 0.5f + 0.7f, 1.8f, -0.45f));
                }
            }
            else
            {
                // Solid Continuous Stone Wall
                CreateBoxMesh(wallGroup, "Solid_Wall", new Vector3(0f, wallHeight * 0.5f, 0f),
                    new Vector3(halfWallLen, wallHeight, wallThick), wallMaterial);

                // Decorative Center Pilaster
                CreateBoxMesh(wallGroup, "Pilaster_Center", new Vector3(0f, wallHeight * 0.5f, -0.3f),
                    new Vector3(1.4f, wallHeight, 0.6f), pillarMaterial);

                // Center Torch Brazier
                if (fireDishPrefab != null)
                {
                    MountTorchBrazier(wallGroup, new Vector3(0f, 1.8f, -0.6f));
                }
            }
        }

        private void MountTorchBrazier(GameObject parent, Vector3 localPos)
        {
            GameObject dish = Instantiate(fireDishPrefab, parent.transform);
            dish.name = "Torch_Brazier";
            dish.transform.localPosition = localPos;
            dish.transform.localRotation = Quaternion.identity;
            dish.transform.localScale = Vector3.one * 0.9f;

            // Warm Flickering Point Light
            GameObject lightObj = new GameObject("Brazier_Light", typeof(Light));
            lightObj.transform.SetParent(dish.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            Light l = lightObj.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = activeTheme.torchColor;
            l.range = 7.5f;
            l.intensity = 2.2f;

            var flicker = lightObj.AddComponent<LightFlickerEffect>();
            flicker.baseIntensity = 2.2f;
            flicker.flickerAmplitude = 0.5f;
        }

        private void CreatePillar(GameObject parent, Vector3 localPos)
        {
            CreateBoxMesh(parent, "Corner_Pillar", localPos + new Vector3(0f, wallHeight * 0.5f, 0f),
                new Vector3(1.2f, wallHeight, 1.2f), pillarMaterial);
        }

        private GameObject CreateBoxMesh(GameObject parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;

            if (mat != null)
            {
                var ren = go.GetComponent<Renderer>();
                if (ren != null) ren.sharedMaterial = mat;
            }

            return go;
        }

        private void PopulateGameplayEntities(System.Random rng)
        {
            foreach (var kvp in dungeonGraph)
            {
                DungeonCellNode cell = kvp.Value;
                GameObject room = cell.roomInstance;
                Vector3 center = cell.worldPos;

                switch (cell.archetype)
                {
                    case DungeonRoomArchetype.EntrySanctuary:
                        PopulateEntrySanctuary(room, center);
                        break;

                    case DungeonRoomArchetype.CombatChamber:
                        PopulateCombatChamber(room, center, rng);
                        break;

                    case DungeonRoomArchetype.TreasureVault:
                        PopulateTreasureVault(room, center, rng);
                        break;

                    case DungeonRoomArchetype.ArcanaShrine:
                        PopulateArcanaShrine(room, center);
                        break;

                    case DungeonRoomArchetype.TrapGauntlet:
                        PopulateTrapGauntlet(room, center);
                        break;

                    case DungeonRoomArchetype.GuardianChamber:
                        PopulateGuardianChamber(room, center);
                        break;
                }
            }
        }

        private void PopulateEntrySanctuary(GameObject room, Vector3 center)
        {
            // Central Safe Seal
            GameObject seal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seal.name = "Sanctuary_Recovery_Seal";
            seal.transform.SetParent(room.transform, false);
            seal.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            seal.transform.localScale = new Vector3(4.5f, 0.1f, 4.5f);
            if (pillarMaterial != null) seal.GetComponent<Renderer>().sharedMaterial = pillarMaterial;

            // Soft Cyan Sanctuary Light
            GameObject lightObj = new GameObject("SanctuaryAuraLight", typeof(Light));
            lightObj.transform.SetParent(seal.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Light l = lightObj.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 0.9f, 1.0f);
            l.range = 10f;
            l.intensity = 2.5f;

            // Rest Furniture
            if (tablePrefab != null)
            {
                GameObject t = Instantiate(tablePrefab, room.transform);
                t.transform.localPosition = new Vector3(5f, 0f, 5f);
            }
            if (bookshelfPrefab != null)
            {
                GameObject b = Instantiate(bookshelfPrefab, room.transform);
                b.transform.localPosition = new Vector3(5.5f, 0f, 2f);
                b.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }
        }

        private void PopulateCombatChamber(GameObject room, Vector3 center, System.Random rng)
        {
            // Spawn 2 to 4 Roaming Shadows
            int shadowCount = rng.Next(2, 5);
            if (activeTheme.activeAffix == FloorAffix.ShadowSwarm) shadowCount += 2;

            for (int i = 0; i < shadowCount; i++)
            {
                Vector2 circle = UnityEngine.Random.insideUnitCircle * (roomGridSnapSize * 0.35f);
                Vector3 shadowPos = center + new Vector3(circle.x, 0.5f, circle.y);

                bool isElite = (i == 0 && activeTheme.activeAffix == FloorAffix.CrimsonMist) || (rng.NextDouble() < 0.20);

                GameObject shadowObj;
                if (shadowPrefab != null)
                {
                    shadowObj = Instantiate(shadowPrefab, shadowPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                    shadowObj.name = isElite ? $"Elite_Void_Shadow_{i + 1}" : $"Void_Shadow_{i + 1}";
                    shadowObj.transform.localScale = isElite ? Vector3.one * 1.35f : Vector3.one;

                    var renList = shadowObj.GetComponentsInChildren<Renderer>();
                    foreach (var r in renList)
                    {
                        if (r != null)
                        {
                            r.material.color = isElite ? new Color(0.95f, 0.2f, 0.35f) : new Color(0.35f, 0.18f, 0.55f);
                        }
                    }

                    var col = shadowObj.GetComponent<Collider>();
                    if (col == null)
                    {
                        var cap = shadowObj.AddComponent<CapsuleCollider>();
                        cap.height = 1.8f;
                        cap.radius = 0.45f;
                        cap.center = new Vector3(0f, 0.9f, 0f);
                    }
                }
                else
                {
                    shadowObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    shadowObj.name = isElite ? $"Elite_Void_Shadow_{i + 1}" : $"Void_Shadow_{i + 1}";
                    shadowObj.transform.position = shadowPos;
                    shadowObj.transform.localScale = isElite ? Vector3.one * 1.35f : Vector3.one;

                    var ren = shadowObj.GetComponent<Renderer>();
                    if (ren != null)
                    {
                        ren.material.color = isElite ? new Color(0.95f, 0.15f, 0.85f) : new Color(0.28f, 0.15f, 0.45f);
                    }
                }

                // Ominous Void Eye Glow
                GameObject glowObj = new GameObject("ShadowGlowLight", typeof(Light));
                glowObj.transform.SetParent(shadowObj.transform, false);
                glowObj.transform.localPosition = new Vector3(0f, 1.4f, 0.2f);
                Light sl = glowObj.GetComponent<Light>();
                sl.type = LightType.Point;
                sl.color = isElite ? new Color(1.0f, 0.2f, 0.2f) : new Color(0.7f, 0.2f, 1.0f);
                sl.range = 3.5f;
                sl.intensity = 2.0f;

                shadowObj.tag = "Enemy";

                // Add SpireEnemy component
                var enemy = shadowObj.AddComponent<SpireEnemy>();
                ScaleEnemyStats(enemy, isElite);

                // Add Persona/Metaphor Roaming & Advantage Strike Controller
                var shadowComp = shadowObj.AddComponent<DungeonShadowEntity>();
                shadowComp.Initialize(isElite, center, roomGridSnapSize * 0.5f);

                shadowObj.transform.SetParent(room.transform, true);
                spawnedDungeonPieces.Add(shadowObj);
            }

            // Props: Crates and Barrels in corners
            if (barrelPrefab != null)
            {
                GameObject b1 = Instantiate(barrelPrefab, room.transform);
                b1.transform.localPosition = new Vector3(7f, 0f, 7f);
            }
            if (cratePrefab != null)
            {
                GameObject c1 = Instantiate(cratePrefab, room.transform);
                c1.transform.localPosition = new Vector3(7f, 0f, 5.5f);
            }
        }

        private void ScaleEnemyStats(SpireEnemy enemy, bool isElite)
        {
            float baseHp = isElite ? 380f : 150f;
            float baseDmg = isElite ? 22f : 12f;

            float hp = baseHp * activeTheme.enemyHealthScale;
            float dmg = baseDmg * activeTheme.enemyDamageScale;

            var t = typeof(SpireEnemy);
            var fMaxHp = t.GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fCurHp = t.GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fDmg = t.GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fTier = t.GetField("mobTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fMaxHp != null) fMaxHp.SetValue(enemy, hp);
            if (fCurHp != null) fCurHp.SetValue(enemy, hp);
            if (fDmg != null) fDmg.SetValue(enemy, dmg);
            if (fTier != null) fTier.SetValue(enemy, isElite ? 3 : 1);
        }

        private void PopulateTreasureVault(GameObject room, Vector3 center, System.Random rng)
        {
            // Raised Stone Dais
            GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dais.name = "Treasure_Dais";
            dais.transform.SetParent(room.transform, false);
            dais.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            dais.transform.localScale = new Vector3(5f, 0.3f, 5f);
            if (pillarMaterial != null) dais.GetComponent<Renderer>().sharedMaterial = pillarMaterial;

            // Spawn Chest
            if (chestPrefab != null)
            {
                GameObject chestObj = Instantiate(chestPrefab, room.transform);
                chestObj.name = "Treasure_Chest_Vault";
                chestObj.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                chestObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                ChestTier tier = (activeTheme.activeAffix == FloorAffix.GildedTreasury || rng.NextDouble() < 0.40)
                    ? ChestTier.RareGilded : ChestTier.CommonWood;

                var chestComp = chestObj.AddComponent<DungeonChestEntity>();
                chestComp.Initialize(tier);
            }

            // Golden Glow Light
            GameObject lightObj = new GameObject("VaultGoldLight", typeof(Light));
            lightObj.transform.SetParent(room.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            Light l = lightObj.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.85f, 0.3f);
            l.range = 10f;
            l.intensity = 3.0f;
        }

        private void PopulateArcanaShrine(GameObject room, Vector3 center)
        {
            // Ancient Carved Altar
            GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "Arcana_Altar_Pedestal";
            altar.transform.SetParent(room.transform, false);
            altar.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            altar.transform.localScale = new Vector3(2.5f, 1.0f, 2.5f);
            if (pillarMaterial != null) altar.GetComponent<Renderer>().sharedMaterial = pillarMaterial;

            // Hovering Arcana Crystal / Tarot Monolith
            GameObject crystal;
#if UNITY_EDITOR
            var crystalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PurePoly/Mining_Free_Assets/Prefabs/PP_Gemstone_13_Blue.prefab");
            if (crystalPrefab != null)
            {
                crystal = Instantiate(crystalPrefab, room.transform);
                crystal.transform.localPosition = new Vector3(0f, 1.85f, 0f);
                crystal.transform.localRotation = Quaternion.Euler(15f, 45f, 15f);
                crystal.transform.localScale = Vector3.one * 1.8f;
            }
            else
#endif
            {
                crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crystal.transform.SetParent(room.transform, false);
                crystal.transform.localPosition = new Vector3(0f, 1.85f, 0f);
                crystal.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
                var ren = crystal.GetComponent<Renderer>();
                if (ren != null) ren.material.color = new Color(0.7f, 0.3f, 1.0f);
            }
            crystal.name = "Hovering_Arcana_Crystal";

            // Light
            GameObject lightObj = new GameObject("ShrineLight", typeof(Light));
            lightObj.transform.SetParent(crystal.transform, false);
            lightObj.transform.localPosition = Vector3.zero;
            Light l = lightObj.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.75f, 0.4f, 1.0f);
            l.range = 10f;
            l.intensity = 3.5f;

            // Add Shrine Component
            var shrine = crystal.AddComponent<DungeonArcanaShrine>();
        }

        private void PopulateTrapGauntlet(GameObject room, Vector3 center)
        {
            // Hazard Rune Floor Spikes
#if UNITY_EDITOR
            var runeTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/dark_academia_rune_circle.jpg");
#endif
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    GameObject trapTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    trapTile.name = $"TrapTile_{x}_{z}";
                    trapTile.transform.SetParent(room.transform, false);
                    trapTile.transform.localPosition = new Vector3(x * 3.5f, 0.05f, z * 3.5f);
                    trapTile.transform.localScale = new Vector3(2.8f, 0.1f, 2.8f);

                    var ren = trapTile.GetComponent<Renderer>();
                    if (ren != null)
                    {
#if UNITY_EDITOR
                        if (runeTex != null)
                        {
                            ren.material.mainTexture = runeTex;
                            ren.material.EnableKeyword("_EMISSION");
                            ren.material.SetColor("_EmissionColor", new Color(1.0f, 0.35f, 0.05f) * 1.5f);
                        }
#endif
                        ren.material.color = new Color(0.95f, 0.45f, 0.15f);
                    }
                }
            }
        }

        private void PopulateGuardianChamber(GameObject room, Vector3 center)
        {
            // Grand Boss Dais
            GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dais.name = "Guardian_Arena_Dais";
            dais.transform.SetParent(room.transform, false);
            dais.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            dais.transform.localScale = new Vector3(10f, 0.4f, 10f);
            if (pillarMaterial != null) dais.GetComponent<Renderer>().sharedMaterial = pillarMaterial;

            // Spawn Grand Exit Staircase / Portal
            GameObject stairObj = new GameObject("Grand_Exit_Staircase_Gateway");
            stairObj.transform.SetParent(room.transform, false);
            stairObj.transform.localPosition = new Vector3(0f, 0.2f, 7.5f);

            GameObject portalVortex = GameObject.CreatePrimitive(PrimitiveType.Quad);
            portalVortex.name = "Portal_Vortex_Mesh";
            portalVortex.transform.SetParent(stairObj.transform, false);
            portalVortex.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            portalVortex.transform.localScale = new Vector3(3.5f, 4.5f, 1f);
            portalVortex.GetComponent<Renderer>().material.color = new Color(0.2f, 0.9f, 1.0f, 0.9f);

            GameObject lightObj = new GameObject("StairLight", typeof(Light));
            lightObj.transform.SetParent(stairObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.2f, 0.5f);
            Light l = lightObj.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 0.9f, 1.0f);
            l.range = 10f;
            l.intensity = 3.5f;

            var stairComp = stairObj.AddComponent<DungeonStaircaseEntity>();
            // Initially unlocked if in test mode or unlocks after boss
            stairComp.SetUnlocked(true);

            // Spawn Boss Golem / Guardian
            GameObject bossObj;
            if (bossGolemPrefab != null)
            {
                bossObj = Instantiate(bossGolemPrefab, center + new Vector3(0f, 0.2f, 0f), Quaternion.Euler(0f, 180f, 0f));
                bossObj.name = $"Floor_Guardian_Golem_B{activeTheme.floorNumber}";
                bossObj.transform.localScale = Vector3.one * 0.45f;

#if UNITY_EDITOR
                var stoneTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Textures/Unity Level Doorway Arched Moss Textures/T_Level_doorway_arched_Stone_Moss_Base.png");
                var normalTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/CobraGamesAssets/Stylized_Dungeon_Props_Pack/Textures/Unity Level Doorway Arched Moss Textures/T_Level_doorway_arched_Stone_Moss_Normal.png");
                var mossTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/ground_forest_moss_turf.jpg");
                var runeTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/DarkAcademia/dark_academia_rune_circle.jpg");

                foreach (var r in bossObj.GetComponentsInChildren<Renderer>())
                {
                    var m = r.sharedMaterial;
                    if (m == null) continue;
                    if (m.name.Contains("GolemStone"))
                    {
                        m.mainTexture = stoneTex;
                        m.SetTexture("_BumpMap", normalTex);
                        m.color = Color.white;
                    }
                    else if (m.name.Contains("GolemMoss"))
                    {
                        m.mainTexture = mossTex;
                        m.color = new Color(0.7f, 0.85f, 0.6f);
                    }
                    else if (m.name.Contains("GolemRuneGlow"))
                    {
                        m.mainTexture = runeTex;
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", new Color(0.2f, 0.85f, 1.0f) * 2.5f);
                    }
                }
#endif

                // Face boss toward the room's entrance doorway
                Vector2Int bossGrid = new Vector2Int(Mathf.RoundToInt(center.x / roomGridSnapSize), Mathf.RoundToInt(center.z / roomGridSnapSize));
                if (dungeonGraph.TryGetValue(bossGrid, out var bossNode))
                {
                    foreach (DoorwayDirection dir in System.Enum.GetValues(typeof(DoorwayDirection)))
                    {
                        if (dir != DoorwayDirection.None && bossNode.HasDoor(dir))
                        {
                            float yaw = dir == DoorwayDirection.North ? 180f :
                                        dir == DoorwayDirection.South ? 0f :
                                        dir == DoorwayDirection.East ? 90f : -90f;
                            bossObj.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                            break;
                        }
                    }
                }
            }
            else
            {
                bossObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bossObj.name = $"Floor_Guardian_Titan_B{activeTheme.floorNumber}";
                bossObj.transform.position = center + new Vector3(0f, 1.5f, 0f);
                bossObj.transform.localScale = new Vector3(2f, 3f, 2f);
            }

            bossObj.transform.SetParent(room.transform, true);
            bossObj.tag = "Enemy";
            var bossEnemy = bossObj.AddComponent<SpireEnemy>();
            ScaleEnemyStats(bossEnemy, true);

            var shadowComp = bossObj.AddComponent<DungeonShadowEntity>();
            shadowComp.Initialize(true, center, roomGridSnapSize * 0.4f);

            spawnedDungeonPieces.Add(bossObj);
        }

        private void TeleportPlayerToEntry()
        {
            if (!dungeonGraph.TryGetValue(Vector2Int.zero, out var entryNode)) return;

            Vector3 spawnPos = entryNode.worldPos + new Vector3(0f, 0.6f, 0f);

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = spawnPos;
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Face North
                if (cc != null) cc.enabled = true;

                Debug.Log($"👤 [DUNGEON CRAWLER] Teleported Player to Entry Sanctuary at {spawnPos}.");
            }
        }

        public void ClearExistingDungeonGeometry()
        {
            foreach (var piece in spawnedDungeonPieces)
            {
                if (piece != null)
                {
                    if (Application.isPlaying) Destroy(piece);
                    else DestroyImmediate(piece);
                }
            }
            spawnedDungeonPieces.Clear();
            dungeonGraph.Clear();

            // Clear all child transforms under this director
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            // Safety cleanup for any residual Floor_Guardian or DungeonRoom objects in scene
            var allGos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGos)
            {
                if (go != null && (go.name.StartsWith("DungeonRoom_") || go.name.StartsWith("Floor_Guardian_") || go.name.StartsWith("Void_Shadow_") || go.name.StartsWith("Elite_Void_Shadow_")))
                {
                    if (Application.isPlaying) Destroy(go);
                    else DestroyImmediate(go);
                }
            }
        }

        public void GenerateUniqueProceduralDungeon(int? overrideSeed = null, int fortressEscalationBonusRooms = 0)
        {
            int seed = overrideSeed ?? (Guid.NewGuid().GetHashCode() ^ System.Environment.TickCount);
            if (DungeonCrawlerFloorMaster.Instance != null)
            {
                DungeonCrawlerFloorMaster.Instance.StartNewDungeonCrawl(1);
            }
            else
            {
                var theme = new FloorThemeInfo
                {
                    floorNumber = 1,
                    floorTitle = "Crypt of the Banished",
                    subtitle = "Tartarus Abyss • Depth 60m",
                    activeAffix = FloorAffix.StandardDeep,
                    ambientColor = new Color(0.08f, 0.10f, 0.16f),
                    torchColor = new Color(1.0f, 0.65f, 0.25f),
                    enemyHealthScale = 1.0f,
                    enemyDamageScale = 1.0f
                };
                BuildDungeonFloor(seed, theme);
            }
        }

        public List<GeneratedRoomNode> BuildUniqueDungeonRun(int entropySeed, float inheritedFortressInstability)
        {
            var theme = new FloorThemeInfo
            {
                floorNumber = 1,
                floorTitle = "Crypt of the Banished",
                subtitle = "Tartarus Abyss • Depth 60m",
                activeAffix = FloorAffix.StandardDeep,
                ambientColor = new Color(0.08f, 0.10f, 0.16f),
                torchColor = new Color(1.0f, 0.65f, 0.25f),
                enemyHealthScale = Mathf.Max(1.0f, inheritedFortressInstability),
                enemyDamageScale = Mathf.Max(1.0f, inheritedFortressInstability * 0.8f)
            };
            BuildDungeonFloor(entropySeed, theme);

            // Legacy conversion for callers
            List<GeneratedRoomNode> nodes = new List<GeneratedRoomNode>();
            int idx = 0;
            foreach (var cell in dungeonGraph.Values)
            {
                nodes.Add(new GeneratedRoomNode
                {
                    nodeIndex = idx++,
                    gridCoordinate = cell.worldPos,
                    orientation = Quaternion.identity,
                    enemyWaveSeed = entropySeed ^ idx
                });
            }
            return nodes;
        }
    }


    /// <summary>
    /// Trigger component attached to room bounds to reveal rooms on the automap as the 3rd person player enters.
    /// </summary>
    public class DungeonRoomDiscoveryTrigger : MonoBehaviour
    {
        private DungeonCellNode cellNode;

        public void Initialize(DungeonCellNode cell)
        {
            cellNode = cell;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && cellNode != null)
            {
                if (!cellNode.isExplored)
                {
                    cellNode.isExplored = true;
                    if (DungeonCrawlerAutomapHUD.Instance != null)
                    {
                        DungeonCrawlerAutomapHUD.Instance.RevealRoom(cellNode.gridPos);
                    }

                    if (cellNode.archetype == DungeonRoomArchetype.TreasureVault ||
                        cellNode.archetype == DungeonRoomArchetype.ArcanaShrine ||
                        cellNode.archetype == DungeonRoomArchetype.GuardianChamber)
                    {
                        string title = cellNode.archetype switch
                        {
                            DungeonRoomArchetype.TreasureVault => "<color=#FFD700><b>[TREASURE VAULT DISCOVERED]</b></color>",
                            DungeonRoomArchetype.ArcanaShrine => "<color=#B066FF><b>[ANCIENT ARCANA SHRINE FOUND]</b></color>",
                            DungeonRoomArchetype.GuardianChamber => "<color=#FF2233><b>[GUARDIAN CHAMBER BREACHED]</b></color>",
                            _ => ""
                        };
                        if (DevClassLoadoutManager.Instance != null && !string.IsNullOrEmpty(title))
                        {
                            DevClassLoadoutManager.Instance.ShowToast(title);
                        }
                    }
                }
            }
        }
    }
}
