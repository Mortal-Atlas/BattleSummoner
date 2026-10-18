using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Combat;
using BattleSummoner.World;
using BattleSummoner.Audio;
using BattleSummoner.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BattleSummoner.World
{
    /// <summary>
    /// OverworldSceneGenerator constructs the entire high-fidelity Solas Overworld:
    /// - Clean Meshy Base Floor layout with accurate terrain MeshCollider.
    /// - Raycast-snapped placement of all architecture, market booths, stairs, trees, boulders, and bushes.
    /// - Central Spire Waypoint Monolith with floating rotating celestial crystal, rune circle, and resonance trigger.
    /// - Walk-up Shop Building with porch lanterns, barrels, crates, and BuildingEntranceTrigger to ShopInterior.
    /// - 3 Detailed Market Bazaar booths (Potions, Bakery, Smithy) with display items and merchant NPCs.
    /// - Streetlamp posts with warm amber point lights.
    /// - Golden hour directional lighting, atmospheric pollen particle system, and town ambiance audio.
    /// - Grounded Player spawn with CharacterController, PlayerProgression (Lv 42), and PortraitARPGCamera.
    /// - Full Anime ARPG Player HUD on OverworldHUDCanvas (Profile, HP/MP telemetry, Combat Deck, Virtual Stick).
    /// </summary>
    public static class OverworldSceneGenerator
    {
        private const string RootName = "--- MESHY OVERWORLD ROOT ---";

#if UNITY_EDITOR
        [MenuItem("BattleSummoner/Generate Overworld Scene Layout", false, 10)]
        public static void GenerateFromMenu()
        {
            GenerateOverworld();
        }
#endif

        public static void GenerateOverworld()
        {
            Debug.Log("<color=#00E5FF><b>[OverworldSceneGenerator] Starting Full Overworld Generation...</b></color>");

            // 1. Clean existing root if present
            var existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(existingRoot);
#else
                Object.DestroyImmediate(existingRoot);
#endif
            }

            GameObject worldRoot = new GameObject(RootName);
            worldRoot.transform.position = Vector3.zero;
            worldRoot.transform.rotation = Quaternion.identity;
            worldRoot.transform.localScale = Vector3.one;

            // 2. Instantiate and Setup Meshy Floor
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/BaseMap_Floor.prefab");
            if (floorPrefab == null)
            {
                Debug.LogError("[OverworldSceneGenerator] Failed to find BaseMap_Floor.prefab!");
                return;
            }

            GameObject floorObj = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, worldRoot.transform);
            floorObj.name = "Meshy_BaseMap_Floor";
            floorObj.transform.position = Vector3.zero;
            floorObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // 270 deg X
            floorObj.transform.localScale = Vector3.one;

            var floorCollider = floorObj.GetComponent<MeshCollider>();
            if (floorCollider == null) floorCollider = floorObj.AddComponent<MeshCollider>();
            var meshFilter = floorObj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                floorCollider.sharedMesh = meshFilter.sharedMesh;
            }
            Physics.SyncTransforms();

            // 3. Setup Directional Sun & Atmosphere
            SetupLightingAndAtmosphere(worldRoot.transform);

            // 4. Central Spire Waypoint Monolith
            SetupSpireWaypoint(worldRoot.transform, floorCollider);

            // 5. Shop Building Exterior
            SetupShopBuilding(worldRoot.transform, floorCollider);

            // 6. Market Bazaar Row (3 booths: Potions, Bakery, Smithy)
            SetupMarketBazaar(worldRoot.transform, floorCollider);

            // 7. Stone Terraced Stairs
            SetupStoneStairs(worldRoot.transform, floorCollider);

            // 8. Natural Flora & Geology (Trees, Boulders, Foliage Bushes)
            SetupFoliageAndRocks(worldRoot.transform, floorCollider);

            // 9. Streetlamp Posts & Amber Lanterns
            SetupStreetlamps(worldRoot.transform, floorCollider);

            // 10. Atmospheric Pollen Particles & Town Ambiance Audio
            SetupFlairAndAudio(worldRoot.transform, floorCollider);

            // 11. Grounded Player Hero & Camera
            SetupPlayerAndCamera(floorCollider);

            // 11b. Combat Training Targets (Gargoyles & Dummies for testing spell slinging & Z-targeting)
            SetupCombatTrainingTargets(worldRoot.transform, floorCollider);

            // 12. Complete Player HUD on OverworldHUDCanvas
            SetupOverworldHUD();

            Debug.Log("<color=#76FF03><b>[OverworldSceneGenerator] Overworld Generation Complete! All props raycast-snapped.</b></color>");

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
        }

        public static bool GetGroundHeight(MeshCollider floorCol, float x, float z, out float groundY)
        {
            Physics.SyncTransforms();
            Ray ray = new Ray(new Vector3(x, 100f, z), Vector3.down);
            if (floorCol.Raycast(ray, out RaycastHit hit, 200f))
            {
                groundY = hit.point.y;
                return true;
            }
            groundY = 0f;
            return false;
        }

        public static void SnapObjectToGround(GameObject obj, MeshCollider floorCol, float x, float z)
        {
            if (GetGroundHeight(floorCol, x, z, out float groundY))
            {
                obj.transform.position = Vector3.zero;
                var rend = obj.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    float bottomOffset = rend.bounds.min.y - obj.transform.position.y;
                    obj.transform.position = new Vector3(x, groundY - bottomOffset, z);
                }
                else
                {
                    obj.transform.position = new Vector3(x, groundY, z);
                }
            }
            else
            {
                obj.transform.position = new Vector3(x, 0f, z);
            }
        }

        private static void SetupLightingAndAtmosphere(Transform parent)
        {
            var sunObj = GameObject.Find("Sun_DirectionalLight");
            if (sunObj == null)
            {
                sunObj = new GameObject("Sun_DirectionalLight");
            }

            var light = sunObj.GetComponent<Light>();
            if (light == null) light = sunObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.94f, 0.85f); // Warm fantasy golden hour
            light.intensity = 1.30f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            sunObj.transform.rotation = Quaternion.Euler(44f, -32f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.40f, 0.48f); // Soft fantasy sky fill
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.82f, 0.88f, 0.95f);
        }

        private static void SetupSpireWaypoint(Transform parent, MeshCollider floorCol)
        {
            GameObject spireRoot = new GameObject("Spire_Waypoint_Plaza");
            spireRoot.transform.SetParent(parent, false);

            float x = 0f, z = 0f;
            GetGroundHeight(floorCol, x, z, out float groundY);
            spireRoot.transform.position = new Vector3(x, groundY, z);

            // 1. Pedestal Base
            GameObject dais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dais.name = "Waypoint_Dais";
            dais.transform.SetParent(spireRoot.transform, false);
            dais.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            dais.transform.localScale = new Vector3(3.8f, 0.25f, 3.8f);
            var daisRend = dais.GetComponent<Renderer>();
            daisRend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            daisRend.material.color = new Color(0.18f, 0.20f, 0.24f);

            // 2. Central Floating Crystal
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crystal.name = "Floating_Astral_Crystal";
            crystal.transform.SetParent(spireRoot.transform, false);
            crystal.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            crystal.transform.localScale = new Vector3(0.9f, 2.2f, 0.9f);
            var crystalCol = crystal.GetComponent<Collider>();
            if (crystalCol != null) Object.DestroyImmediate(crystalCol);

            var crystalRend = crystal.GetComponent<Renderer>();
            var crystalMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            crystalMat.color = new Color(0.1f, 0.85f, 1.0f, 0.9f);
            crystalMat.EnableKeyword("_EMISSION");
            crystalMat.SetColor("_EmissionColor", new Color(0.1f, 0.90f, 1.0f) * 2.8f);
            crystalRend.material = crystalMat;

            // 3. Runic Ring
            GameObject runeRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeRing.name = "Runic_Ring";
            runeRing.transform.SetParent(spireRoot.transform, false);
            runeRing.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            runeRing.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
            var ringCol = runeRing.GetComponent<Collider>();
            if (ringCol != null) Object.DestroyImmediate(ringCol);
            var ringRend = runeRing.GetComponent<Renderer>();
            var ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            ringMat.color = new Color(0.85f, 0.72f, 0.25f);
            ringMat.EnableKeyword("_EMISSION");
            ringMat.SetColor("_EmissionColor", new Color(1.0f, 0.82f, 0.2f) * 1.5f);
            ringRend.material = ringMat;

            // 4. Point Light Aura
            GameObject lightObj = new GameObject("Crystal_AuraLight");
            lightObj.transform.SetParent(spireRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            var pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.2f, 0.9f, 1.0f);
            pLight.range = 14f;
            pLight.intensity = 2.4f;

            // 5. Waypoint Controller Component
            var waypointComp = spireRoot.AddComponent<OverworldSpireWaypoint>();
            var so = new SerializedObject(waypointComp);
            so.FindProperty("crystalTransform").objectReferenceValue = crystal.transform;
            so.FindProperty("runeRingTransform").objectReferenceValue = runeRing.transform;
            so.FindProperty("auraLight").objectReferenceValue = pLight;
            so.ApplyModifiedProperties();
        }

        private static void SetupShopBuilding(Transform parent, MeshCollider floorCol)
        {
            var shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Shop_Building_Exterior.prefab");
            if (shopPrefab == null) return;

            GameObject shop = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab, parent);
            shop.name = "Shop_Building_Exterior";
            shop.transform.localScale = Vector3.one * 0.10f; // 10m x 9.2m x 9.7m
            // Rotate to face plaza
            shop.transform.rotation = Quaternion.AngleAxis(135f, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);

            float x = -15.5f, z = 14.0f;
            SnapObjectToGround(shop, floorCol, x, z);

            // Configure Doorway_EntranceTrigger
            var trigger = shop.transform.Find("Doorway_EntranceTrigger");
            if (trigger != null)
            {
                trigger.localScale = Vector3.one * 10f; // Cancels parent 0.10 scale in world space
                trigger.localPosition = new Vector3(0f, 10f, -40f);
                var bc = trigger.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.isTrigger = true;
                    bc.size = new Vector3(2.5f, 3.0f, 2.5f);
                }

                var entrance = trigger.GetComponent<BuildingEntranceTrigger>();
                if (entrance != null)
                {
                    entrance.targetSceneName = "ShopInterior";
                    entrance.targetSpawnMarkerId = "Interior_Doorway";
                    entrance.locationTitle = "Curio & Alchemy Emporium";
                    entrance.triggerOnTouch = false;
                    entrance.interactKey = KeyCode.F;
                }
            }

            // Return Marker from interior
            var markerObj = GameObject.Find("Overworld_ShopDoor");
            if (markerObj == null) markerObj = new GameObject("Overworld_ShopDoor");
            markerObj.transform.SetParent(parent, false);
            GetGroundHeight(floorCol, -15.5f, 10.5f, out float markerY);
            markerObj.transform.position = new Vector3(-15.5f, markerY + 0.05f, 10.5f);
            markerObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var marker = markerObj.GetComponent<SceneEntranceMarker>();
            if (marker == null) marker = markerObj.AddComponent<SceneEntranceMarker>();
            marker.markerId = "Overworld_ShopDoor";
            marker.facingYaw = 180f;

            // Porch Hanging Lantern
            GameObject porchLight = new GameObject("Shop_PorchLantern");
            porchLight.transform.SetParent(shop.transform, false);
            porchLight.transform.localPosition = new Vector3(0f, 25f, -45f);
            var pl = porchLight.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(1.0f, 0.78f, 0.40f);
            pl.range = 7f;
            pl.intensity = 1.9f;
        }

        private static void SetupMarketBazaar(Transform parent, MeshCollider floorCol)
        {
            var boothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Market_Booth.prefab");
            if (boothPrefab == null) return;

            GameObject bazaarRoot = new GameObject("Market_Bazaar_Row");
            bazaarRoot.transform.SetParent(parent, false);

            var stallConfigs = new[]
            {
                new { name = "Market_Booth_Potions", x = 6.0f, z = -3.5f, yaw = -60f, label = "✦ Mystic Potions & Draughts ✦" },
                new { name = "Market_Booth_Bakery",  x = 7.5f, z = 1.8f,  yaw = -90f, label = "✦ Solas Artisan Bakery ✦" },
                new { name = "Market_Booth_Smithy",  x = 6.0f, z = 7.0f,  yaw = -120f, label = "✦ Relic Forge & Blades ✦" },
            };

            foreach (var cfg in stallConfigs)
            {
                GameObject booth = (GameObject)PrefabUtility.InstantiatePrefab(boothPrefab, bazaarRoot.transform);
                booth.name = cfg.name;
                booth.transform.localScale = Vector3.one * 0.04f; // ~4m wide stall
                booth.transform.rotation = Quaternion.AngleAxis(cfg.yaw, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                SnapObjectToGround(booth, floorCol, cfg.x, cfg.z);

                // Add warm stall lantern
                GameObject lantern = new GameObject("StallLantern");
                lantern.transform.SetParent(booth.transform, false);
                lantern.transform.localPosition = new Vector3(0f, 35f, 0f);
                var l = lantern.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.82f, 0.45f);
                l.range = 5.5f;
                l.intensity = 1.5f;
            }
        }

        private static void SetupStoneStairs(Transform parent, MeshCollider floorCol)
        {
            var stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Stone_Stairs.prefab");
            if (stairsPrefab == null) return;

            GameObject stairsRoot = new GameObject("Stone_Stairs_Group");
            stairsRoot.transform.SetParent(parent, false);

            var stairConfigs = new[]
            {
                new { name = "Stairs_Plaza_NorthGorge", x = -1.0f, z = 11.5f, yaw = 0f, scale = 0.045f },
                new { name = "Stairs_Promenade_ShopTerrace", x = -10.0f, z = 9.5f, yaw = 65f, scale = 0.045f },
            };

            foreach (var cfg in stairConfigs)
            {
                GameObject stair = (GameObject)PrefabUtility.InstantiatePrefab(stairsPrefab, stairsRoot.transform);
                stair.name = cfg.name;
                stair.transform.localScale = Vector3.one * cfg.scale;
                stair.transform.rotation = Quaternion.AngleAxis(cfg.yaw, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                SnapObjectToGround(stair, floorCol, cfg.x, cfg.z);
            }
        }

        private static void SetupFoliageAndRocks(Transform parent, MeshCollider floorCol)
        {
            GameObject floraRoot = new GameObject("Organic_Environment_Props");
            floraRoot.transform.SetParent(parent, false);

            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Fantasy_Tree.prefab");
            var boulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Cliff_Boulder.prefab");
            var bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Overworld/Foliage_Bush.prefab");

            // 1. Trees (perimeter groves framing roads and ridges)
            if (treePrefab != null)
            {
                Vector2[] treeCoords = new Vector2[]
                {
                    new Vector2(-22f, 18f), new Vector2(-25f, 8f), new Vector2(-18f, -8f),
                    new Vector2(-22f, -18f), new Vector2(-10f, -22f), new Vector2(16f, 18f),
                    new Vector2(22f, 12f), new Vector2(18f, -6f), new Vector2(22f, -16f),
                    new Vector2(12f, -22f), new Vector2(-28f, 0f), new Vector2(28f, 0f),
                    new Vector2(-12f, 24f), new Vector2(12f, 24f), new Vector2(-4f, 28f),
                    new Vector2(4f, 28f), new Vector2(-10f, 2f), new Vector2(-10f, -8f)
                };

                for (int i = 0; i < treeCoords.Length; i++)
                {
                    var coord = treeCoords[i];
                    GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, floraRoot.transform);
                    tree.name = $"Fantasy_Tree_{i + 1}";
                    float scale = Random.Range(0.075f, 0.105f); // 7.5m - 10.5m tall
                    tree.transform.localScale = Vector3.one * scale;
                    float yaw = Random.Range(0f, 360f);
                    tree.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                    SnapObjectToGround(tree, floorCol, coord.x, coord.y);
                }
            }

            // 2. Cliff Boulders
            if (boulderPrefab != null)
            {
                Vector2[] boulderCoords = new Vector2[]
                {
                    new Vector2(-26f, 22f), new Vector2(24f, 20f), new Vector2(24f, -8f),
                    new Vector2(20f, -24f), new Vector2(-22f, -24f), new Vector2(0f, 26f),
                    new Vector2(-18f, 26f), new Vector2(18f, 26f), new Vector2(28f, 4f),
                    new Vector2(-28f, -6f), new Vector2(16f, -14f), new Vector2(-16f, -14f)
                };

                for (int i = 0; i < boulderCoords.Length; i++)
                {
                    var coord = boulderCoords[i];
                    GameObject boulder = (GameObject)PrefabUtility.InstantiatePrefab(boulderPrefab, floraRoot.transform);
                    boulder.name = $"Cliff_Boulder_{i + 1}";
                    float scale = Random.Range(0.035f, 0.055f); // 3.5m - 5.5m rock
                    boulder.transform.localScale = Vector3.one * scale;
                    float yaw = Random.Range(0f, 360f);
                    boulder.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                    SnapObjectToGround(boulder, floorCol, coord.x, coord.y);
                }
            }

            // 3. Foliage Bushes
            if (bushPrefab != null)
            {
                Vector2[] bushCoords = new Vector2[]
                {
                    new Vector2(-3.5f, -12f), new Vector2(3.5f, -12f), new Vector2(-3.5f, -6f),
                    new Vector2(3.5f, -6f), new Vector2(-7f, -16f), new Vector2(7f, -16f),
                    new Vector2(4.5f, -1.5f), new Vector2(4.5f, 4.5f), new Vector2(4.5f, 9.5f),
                    new Vector2(-8.5f, 12f), new Vector2(-12f, 15f), new Vector2(-18f, 16f),
                    new Vector2(-5f, 5f), new Vector2(5f, 5f), new Vector2(-6f, -2f),
                    new Vector2(-2f, -16f), new Vector2(2f, -16f), new Vector2(-8f, 7f),
                    new Vector2(-14f, 8f), new Vector2(9f, -1f), new Vector2(9f, 5f)
                };

                for (int i = 0; i < bushCoords.Length; i++)
                {
                    var coord = bushCoords[i];
                    GameObject bush = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, floraRoot.transform);
                    bush.name = $"Foliage_Bush_{i + 1}";
                    float scale = Random.Range(0.018f, 0.026f); // 1.8m - 2.6m bush
                    bush.transform.localScale = Vector3.one * scale;
                    float yaw = Random.Range(0f, 360f);
                    bush.transform.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
                    SnapObjectToGround(bush, floorCol, coord.x, coord.y);
                }
            }
        }

        private static void SetupStreetlamps(Transform parent, MeshCollider floorCol)
        {
            GameObject lampsRoot = new GameObject("Streetlamp_Posts");
            lampsRoot.transform.SetParent(parent, false);

            Vector2[] lampCoords = new Vector2[]
            {
                new Vector2(-2.8f, -14.0f),
                new Vector2(2.8f, -14.0f),
                new Vector2(-2.8f, -8.0f),
                new Vector2(2.8f, -8.0f),
                new Vector2(-4.5f, -1.5f),
                new Vector2(4.5f, -1.5f)
            };

            for (int i = 0; i < lampCoords.Length; i++)
            {
                var coord = lampCoords[i];
                GetGroundHeight(floorCol, coord.x, coord.y, out float groundY);

                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Streetlamp_Post_{i + 1}";
                post.transform.SetParent(lampsRoot.transform, false);
                post.transform.position = new Vector3(coord.x, groundY + 1.5f, coord.y);
                post.transform.localScale = new Vector3(0.22f, 1.5f, 0.22f);
                var rend = post.GetComponent<Renderer>();
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                rend.material.color = new Color(0.20f, 0.22f, 0.25f);

                // Lantern Head
                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
                head.name = "Lantern_Head";
                head.transform.SetParent(post.transform, false);
                head.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                head.transform.localScale = new Vector3(2.0f, 0.35f, 2.0f);
                var headCol = head.GetComponent<Collider>();
                if (headCol != null) Object.DestroyImmediate(headCol);
                var headRend = head.GetComponent<Renderer>();
                var headMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                headMat.color = new Color(1.0f, 0.85f, 0.4f);
                headMat.EnableKeyword("_EMISSION");
                headMat.SetColor("_EmissionColor", new Color(1.0f, 0.80f, 0.35f) * 2.5f);
                headRend.material = headMat;

                // Point Light
                GameObject lightObj = new GameObject("AmberLight");
                lightObj.transform.SetParent(post.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                var l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.78f, 0.38f);
                l.range = 8.5f;
                l.intensity = 1.8f;
            }
        }

        private static void SetupFlairAndAudio(Transform parent, MeshCollider floorCol)
        {
            // 1. Floating Golden Pollen Particles
            GameObject pollenObj = new GameObject("Atmospheric_GoldenPollen");
            pollenObj.transform.SetParent(parent, false);
            pollenObj.transform.position = new Vector3(0f, 7.5f, 0f);

            var ps = pollenObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 7f;
            main.startSpeed = 0.45f;
            main.startSize = 0.12f;
            main.startColor = new Color(1.0f, 0.88f, 0.45f, 0.75f);
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(35f, 8f, 35f);

            var velOverLife = ps.velocityOverLifetime;
            velOverLife.enabled = true;
            velOverLife.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velOverLife.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.25f);
            velOverLife.z = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);

            var psRend = pollenObj.GetComponent<ParticleSystemRenderer>();
            psRend.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));

            // 2. Town Ambiance Audio Synthesizer
            GameObject audioObj = GameObject.Find("Town_Ambiance_Audio");
            if (audioObj == null) audioObj = new GameObject("Town_Ambiance_Audio");
            audioObj.transform.SetParent(parent, false);
            audioObj.transform.position = Vector3.zero;
            if (audioObj.GetComponent<OverworldTownAmbiance>() == null)
            {
                audioObj.AddComponent<OverworldTownAmbiance>();
            }
        }

        private static void SetupCombatTrainingTargets(Transform parent, MeshCollider floorCol)
        {
            var combatRoot = CreateOrGetChild(parent, "CombatTraining_Enemies");
            Vector3[] enemyPositions = new Vector3[]
            {
                new Vector3(-4.5f, 0f, 6.0f),
                new Vector3(4.5f, 0f, 7.5f),
                new Vector3(0.0f, 0f, 13.0f)
            };

            string[] enemyNames = new string[]
            {
                "Gargoyle_Vanguard",
                "Spire_ShadowStalker",
                "Obsidian_Golem_Dummy"
            };

            for (int i = 0; i < enemyPositions.Length; i++)
            {
                var ep = enemyPositions[i];
                GetGroundHeight(floorCol, ep.x, ep.z, out float gy);

                GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObj.name = enemyNames[i];
                enemyObj.tag = "Enemy";
                enemyObj.transform.SetParent(combatRoot.transform, false);
                enemyObj.transform.position = new Vector3(ep.x, gy + 1.0f, ep.z);
                enemyObj.transform.localScale = (i == 2) ? new Vector3(1.3f, 1.4f, 1.3f) : new Vector3(1.0f, 1.0f, 1.0f);

                var rend = enemyObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    Color demonColor = (i == 2) ? new Color(0.45f, 0.15f, 0.55f) : new Color(0.75f, 0.20f, 0.15f);
                    mat.color = demonColor;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", demonColor * 2.5f);
                    rend.sharedMaterial = mat;
                }

                // Add SpireEnemy and StatusEffectReceiver
                var spireComp = enemyObj.AddComponent<SpireEnemy>();
                var statusComp = enemyObj.GetComponent<StatusEffectReceiver>();
                if (statusComp == null) statusComp = enemyObj.AddComponent<StatusEffectReceiver>();

                // Add in-world overhead nameplate/healthbar
                var nameplateGo = new GameObject("EnemyNameplate", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI));
                nameplateGo.transform.SetParent(enemyObj.transform, false);
                nameplateGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);

                var c = nameplateGo.GetComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                var rt = nameplateGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(160f, 40f);
                rt.localScale = Vector3.one * 0.012f;

                var tmp = nameplateGo.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<color=#FF5533><b>[LV.38 {enemyNames[i].Replace('_', ' ')}]</b></color>\n<size=75%><color=#76FF03>HP: 450/450</color></size>";
                tmp.fontSize = 18;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void SetupPlayerAndCamera(MeshCollider floorCol)
        {
            var player = GameObject.Find("Player_Hero");
            if (player == null)
            {
                var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player_Hero.prefab");
                if (heroPrefab != null)
                {
                    player = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
                    player.name = "Player_Hero";
                }
                else
                {
                    Debug.LogError("[OverworldSceneGenerator] Player_Hero not found in scene and prefab missing!");
                    return;
                }
            }

            // Ensure modular character appearance is detected and loaded
            var modularApp = player.GetComponent<BattleSummoner.Customization.ModularCharacterAppearance>();
            if (modularApp != null)
            {
                modularApp.AutoDetectRenderers();
                modularApp.ApplyAppearance(BattleSummoner.Customization.ModularPlayerAppearanceData.LoadLocalProfile());
            }

            var customLoader = player.GetComponent<BattleSummoner.Customization.PlayerCustomizationLoader>();
            if (customLoader == null) player.AddComponent<BattleSummoner.Customization.PlayerCustomizationLoader>();

            // Spawn player at South Promenade entrance, facing north toward Plaza Spire
            float spawnX = 0.0f;
            float spawnZ = -15.0f;
            GetGroundHeight(floorCol, spawnX, spawnZ, out float spawnY);

            // Ground precisely + 0.05m to prevent CC clipping
            player.transform.position = new Vector3(spawnX, spawnY + 0.05f, spawnZ);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            // Character Controller settings
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 1.8f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0f, 0.9f, 0f);
            }

            // Ensure PlayerProgression exists and is populated
            var prog = player.GetComponent<PlayerProgression>();
            if (prog == null) prog = player.AddComponent<PlayerProgression>();
            prog.playerName = "Astraea";
            prog.currentLevel = 42;
            prog.currentHp = 1450f;
            prog.maxHp = 1450f;
            prog.currentMp = 320f;
            prog.maxMp = 320f;

            // Link PortraitARPGCamera
            var cam = Camera.main;
            if (cam != null)
            {
                var arpgCam = cam.GetComponent<PortraitARPGCamera>();
                if (arpgCam != null)
                {
                    arpgCam.playerTarget = player.transform;
                    arpgCam.pitchAngle = 24.0f;
                    arpgCam.cameraDistance = 3.2f;
                    arpgCam.targetFocusHeight = 1.35f;
                    arpgCam.yawAngle = 0.0f;
                }
            }
        }

        private static void SetupOverworldHUD()
        {
            var canvasObj = GameObject.Find("OverworldHUDCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("OverworldHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = canvasObj.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            // Ensure OverworldPlayerHUDController component
            var hudController = canvasObj.GetComponent<OverworldPlayerHUDController>();
            if (hudController == null) hudController = canvasObj.AddComponent<OverworldPlayerHUDController>();

            // 1. Persona Battle Side Profile HUD (Top Left)
            var profileHUD = BattleSideProfileHUDController.EnsureInScene(canvasObj.transform);

            // 2. Top-Right Navigation & Vault Deck
            var navDeck = CreateOrGetChild(canvasObj.transform, "TopRight_NavigationDeck");
            var navRect = navDeck.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(1f, 1f);
            navRect.anchorMax = new Vector2(1f, 1f);
            navRect.pivot = new Vector2(1f, 1f);
            navRect.anchoredPosition = new Vector2(-25f, -25f);
            navRect.sizeDelta = new Vector2(460f, 180f);

            // Location Badge
            var locBadge = CreateOrGetChild(navDeck.transform, "LocationBadge");
            var locRect = locBadge.GetComponent<RectTransform>();
            locRect.anchorMin = new Vector2(0f, 0.6f);
            locRect.anchorMax = new Vector2(1f, 1f);
            locRect.sizeDelta = Vector2.zero;
            var locTmp = GetOrAddComponent<TextMeshProUGUI>(locBadge);
            locTmp.text = "✦ SOLAS OVERWORLD — SPIRE PLAZA ✦";
            locTmp.fontSize = 20f;
            locTmp.fontStyle = FontStyles.Bold;
            locTmp.alignment = TextAlignmentOptions.Right;
            locTmp.color = new Color(0.95f, 0.90f, 0.70f);

            // Gold & Gem counters
            var vaultObj = CreateOrGetChild(navDeck.transform, "VaultDisplay");
            var vaultRect = vaultObj.GetComponent<RectTransform>();
            vaultRect.anchorMin = new Vector2(0f, 0.25f);
            vaultRect.anchorMax = new Vector2(1f, 0.6f);
            vaultRect.sizeDelta = Vector2.zero;
            var vaultTmp = GetOrAddComponent<TextMeshProUGUI>(vaultObj);
            vaultTmp.text = "<color=#FFD700>✦ 14,250 Gold</color>   <color=#00E5FF>◆ 180 Gems</color>";
            vaultTmp.fontSize = 18f;
            vaultTmp.alignment = TextAlignmentOptions.Right;

            // Menu Shortcut Buttons (Campfire Map [M], Bag [B], Settings)
            var btnRow = CreateOrGetChild(navDeck.transform, "MenuButtonRow");
            var btnRect = btnRow.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0f, 0f);
            btnRect.anchorMax = new Vector2(1f, 0.25f);
            btnRect.sizeDelta = Vector2.zero;
            var hLayout = GetOrAddComponent<HorizontalLayoutGroup>(btnRow);
            hLayout.childAlignment = TextAnchor.MiddleRight;
            hLayout.spacing = 15f;

            var mapBtn = CreateActionButton(btnRow.transform, "Btn_CampfireMap", "🗺 Campfire [M]", new Color(0.2f, 0.5f, 0.8f));
            var bagBtn = CreateActionButton(btnRow.transform, "Btn_Bag", "🎒 Bag", new Color(0.6f, 0.4f, 0.2f));
            var setBtn = CreateActionButton(btnRow.transform, "Btn_Settings", "⚙ Options", new Color(0.3f, 0.3f, 0.35f));

            // 3. Bottom-Right Combat Action Deck
            var combatDeck = CreateOrGetChild(canvasObj.transform, "BottomRight_CombatDeck");
            var cdRect = combatDeck.GetComponent<RectTransform>();
            cdRect.anchorMin = new Vector2(1f, 0f);
            cdRect.anchorMax = new Vector2(1f, 0f);
            cdRect.pivot = new Vector2(1f, 0f);
            cdRect.anchoredPosition = new Vector2(-30f, 30f);
            cdRect.sizeDelta = new Vector2(440f, 440f);

            // Primary Attack Button (Center large)
            var atkBtnObj = CreateCircularButton(combatDeck.transform, "Btn_Attack", "⚔ ATTACK\n[J]", 130f, new Vector2(300f, 140f), new Color(0.85f, 0.30f, 0.15f));
            var dashBtnObj = CreateCircularButton(combatDeck.transform, "Btn_Dash", "💨 DASH\n[SPACE]", 100f, new Vector2(170f, 70f), new Color(0.15f, 0.65f, 0.85f));

            // Skills (1, 2, 3)
            var skill1Obj = CreateCircularButton(combatDeck.transform, "Btn_Skill1", "🔥 ARC\n[1]", 80f, new Vector2(140f, 190f), new Color(0.95f, 0.55f, 0.10f));
            var skill2Obj = CreateCircularButton(combatDeck.transform, "Btn_Skill2", "🛡 WARD\n[2]", 80f, new Vector2(210f, 290f), new Color(0.30f, 0.80f, 0.30f));
            var skill3Obj = CreateCircularButton(combatDeck.transform, "Btn_Skill3", "⚡ SPIRE\n[3]", 80f, new Vector2(320f, 330f), new Color(0.70f, 0.25f, 0.95f));

            // Potions Quick Belt (Q, E)
            var hpPotObj = CreateCircularButton(combatDeck.transform, "Btn_HpPotion", "❤ HP (5)\n[Q]", 70f, new Vector2(50f, 70f), new Color(0.85f, 0.15f, 0.25f));
            var mpPotObj = CreateCircularButton(combatDeck.transform, "Btn_MpPotion", "🔷 MP (3)\n[E]", 70f, new Vector2(50f, 160f), new Color(0.15f, 0.45f, 0.95f));

            // 4. Center Toast Notification Group
            var toastObj = CreateOrGetChild(canvasObj.transform, "CenterToastGroup");
            var toastRect = toastObj.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.65f);
            toastRect.anchorMax = new Vector2(0.5f, 0.65f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.sizeDelta = new Vector2(720f, 80f);

            var toastImg = GetOrAddComponent<Image>(toastObj);
            toastImg.color = new Color(0.05f, 0.08f, 0.14f, 0.88f);

            var toastGroup = GetOrAddComponent<CanvasGroup>(toastObj);
            toastGroup.alpha = 0f;
            toastObj.SetActive(false);

            var toastTextObj = CreateOrGetChild(toastObj.transform, "ToastText");
            var ttRect = toastTextObj.GetComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.sizeDelta = Vector2.zero;
            var toastTmp = GetOrAddComponent<TextMeshProUGUI>(toastTextObj);
            toastTmp.text = "✦ WELCOME TO SOLAS OVERWORLD ✦";
            toastTmp.fontSize = 24f;
            toastTmp.fontStyle = FontStyles.Bold;
            toastTmp.alignment = TextAlignmentOptions.Center;
            toastTmp.color = new Color(1.0f, 0.90f, 0.50f);

            // Ensure EventSystem
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            // Wire HUD references
            var player = GameObject.Find("Player_Hero");
            var so = new SerializedObject(hudController);
            if (player != null)
            {
                so.FindProperty("playerProgression").objectReferenceValue = player.GetComponent<PlayerProgression>();
                so.FindProperty("playerMovement").objectReferenceValue = player.GetComponent<PlayerMovementController>();
                so.FindProperty("humanoidAnimation").objectReferenceValue = player.GetComponent<BattleSummoner.Combat.HumanoidAnimationController>();
            }
            so.FindProperty("battleSideProfileHUD").objectReferenceValue = profileHUD;
            so.FindProperty("attackButton").objectReferenceValue = atkBtnObj.GetComponent<Button>();
            so.FindProperty("dashButton").objectReferenceValue = dashBtnObj.GetComponent<Button>();
            so.FindProperty("skill1Button").objectReferenceValue = skill1Obj.GetComponent<Button>();
            so.FindProperty("skill2Button").objectReferenceValue = skill2Obj.GetComponent<Button>();
            so.FindProperty("skill3Button").objectReferenceValue = skill3Obj.GetComponent<Button>();
            so.FindProperty("hpPotionButton").objectReferenceValue = hpPotObj.GetComponent<Button>();
            so.FindProperty("mpPotionButton").objectReferenceValue = mpPotObj.GetComponent<Button>();
            so.FindProperty("campfireMapButton").objectReferenceValue = mapBtn.GetComponent<Button>();
            so.FindProperty("bagButton").objectReferenceValue = bagBtn.GetComponent<Button>();
            so.FindProperty("settingsButton").objectReferenceValue = setBtn.GetComponent<Button>();
            so.FindProperty("locationTitleTMP").objectReferenceValue = locTmp;
            so.FindProperty("goldCounterTMP").objectReferenceValue = vaultTmp;
            so.FindProperty("toastGroup").objectReferenceValue = toastGroup;
            so.FindProperty("toastTextTMP").objectReferenceValue = toastTmp;
            so.ApplyModifiedProperties();
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static GameObject CreateOrGetChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child.gameObject;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateActionButton(Transform parent, string name, string label, Color bgColor)
        {
            var btnGo = CreateOrGetChild(parent, name);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(130f, 40f);

            var img = GetOrAddComponent<Image>(btnGo);
            img.color = bgColor;

            var btn = GetOrAddComponent<Button>(btnGo);

            var textGo = CreateOrGetChild(btnGo.transform, "Label");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = GetOrAddComponent<TextMeshProUGUI>(textGo);
            tmp.text = label;
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnGo;
        }

        private static GameObject CreateCircularButton(Transform parent, string name, string label, float diameter, Vector2 pos, Color bgColor)
        {
            var btnGo = CreateOrGetChild(parent, name);
            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(diameter, diameter);

            var img = GetOrAddComponent<Image>(btnGo);
            img.color = bgColor;

            var btn = GetOrAddComponent<Button>(btnGo);

            var textGo = CreateOrGetChild(btnGo.transform, "Label");
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = GetOrAddComponent<TextMeshProUGUI>(textGo);
            tmp.text = label;
            tmp.fontSize = diameter > 90f ? 18f : 14f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnGo;
        }
    }
}
