using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Optimization
{
    /// <summary>
    /// High-performance camera frustum, distance culling, and resource throttling system.
    /// Unloads props, row houses, foliage, rocks, and street decorations outside camera view.
    /// Also throttles real-time point lights and chimney particle emitters to achieve 60+ FPS.
    /// </summary>
    [ExecuteAlways]
    public class CameraFrustumPropUnloader : MonoBehaviour
    {
        public static CameraFrustumPropUnloader Instance { get; private set; }

        [Header("Target Camera")]
        [Tooltip("Camera used to evaluate visibility frustum. Defaults to Camera.main.")]
        public Camera targetCamera;

        [Header("Distance Thresholds (Meters)")]
        [Tooltip("Metropolis multi-story row houses cull distance.")]
        public float metroHouseCullDistance = 380f;

        [Tooltip("Metropolis street props (benches, lamp posts, furnishings) cull distance.")]
        public float metroStreetPropsCullDistance = 150f;

        [Tooltip("Metropolis flower planters and micro-decorations cull distance.")]
        public float metroPlantersCullDistance = 100f;

        [Tooltip("Forest and tree chunks cull distance.")]
        public float forestCullDistance = 450f;

        [Tooltip("Grass and wildflower chunks cull distance.")]
        public float grassCullDistance = 80f;

        [Tooltip("Continental rock formations cull distance.")]
        public float rockCullDistance = 300f;

        [Tooltip("Mountain switchback and gorge scenery cull distance.")]
        public float mountainCullDistance = 350f;

        [Tooltip("World foliage cull distance.")]
        public float worldFoliageCullDistance = 350f;

        [Tooltip("Seaward port town props and buildings cull distance.")]
        public float portTownCullDistance = 300f;

        [Tooltip("Lively metropolis citizens and NPCs cull distance.")]
        public float npcCullDistance = 120f;

        [Tooltip("General props (crates, barrels, market goods) cull distance.")]
        public float propCullDistance = 160f;

        [Tooltip("Perimeter defense towers cull distance.")]
        public float towerCullDistance = 600f;

        [Tooltip("Point lights hard distance cull threshold (meters). Beyond this, point light component is disabled.")]
        public float lightCullDistance = 45f;

        [Tooltip("Chimney hearth smoke particle simulation cull threshold (meters).")]
        public float chimneySmokeCullDistance = 140f;

        [Tooltip("Proximity radius around camera/player where chunks are ALWAYS kept loaded regardless of angle.")]
        public float nearPreserveRadius = 25f;

        [Header("Performance")]
        [Range(1, 4)]
        [Tooltip("Number of slices to divide evaluations across frames to maintain smooth 60+ FPS.")]
        public int frameSlices = 2;

        public struct CullChunk
        {
            public GameObject gameObject;
            public Bounds worldBounds;
            public float maxDistance;
            public bool isLoaded;
        }

        public struct CullLight
        {
            public Light light;
            public Vector3 position;
            public bool isEnabled;
        }

        public struct CullParticle
        {
            public ParticleSystem particleSystem;
            public Vector3 position;
            public bool isPlaying;
        }

        private readonly List<CullChunk> _chunks = new List<CullChunk>(12000);
        private readonly List<CullLight> _lights = new List<CullLight>(2000);
        private readonly List<CullParticle> _chimneyParticles = new List<CullParticle>(800);

        private Plane[] _frustumPlanes = new Plane[6];
        private int _currentSlice = 0;
        public int RegisteredChunkCount => _chunks.Count;
        public int RegisteredLightCount => _lights.Count;
        public int RegisteredParticleCount => _chimneyParticles.Count;
        public int CurrentSlice => _currentSlice;

        private void OnEnable()
        {
            Instance = this;
            if (targetCamera == null) targetCamera = Camera.main;
            RefreshPropRegistry();
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            RestoreAll();
        }

        /// <summary>
        /// Comprehensive scene scan that registers all major hierarchies into the spatial culling database.
        /// </summary>
        [ContextMenu("Refresh Prop Registry")]
        public void RefreshPropRegistry()
        {
            _chunks.Clear();
            _lights.Clear();
            _chimneyParticles.Clear();

            // 1. --- ANIME NATURE & VEGETATION --- (508 chunks)
            RegisterHierarchyChildren("--- ANIME NATURE & VEGETATION ---", forestCullDistance);

            // 2. --- DYNAMIC STYLIZED GRASS & WILDFLOWERS --- (920 chunks)
            RegisterHierarchyChildren("--- DYNAMIC STYLIZED GRASS & WILDFLOWERS ---", grassCullDistance);

            // 3. --- SCATTERED CONTINENTAL ROCK FORMATIONS ---
            RegisterHierarchyChildren("--- SCATTERED CONTINENTAL ROCK FORMATIONS ---", rockCullDistance);

            // 4. 8Vector_Metropolis_Root
            RegisterHierarchyChildren("8Vector_Metropolis_Root/01_Tier_Stepped_RowHouses", metroHouseCullDistance);
            RegisterHierarchyChildren("8Vector_Metropolis_Root/02_Street_Props_Benches_Lamps", metroStreetPropsCullDistance);
            RegisterHierarchyChildren("8Vector_Metropolis_Root/03_Street_Flower_Planters", metroPlantersCullDistance);
            RegisterHierarchyChildren("8Vector_Metropolis_Root/04_Market_Bazaars_And_Barrels", propCullDistance);
            RegisterHierarchyChildren("8Vector_Metropolis_Root/Metropolis_Street_Infrastructure_Root", 300f);
            RegisterHierarchyChildren("8Vector_Metropolis_Root/06_Metropolis_Beauty_Enrichment", metroStreetPropsCullDistance);

            // 5. ENVIRONMENT_ROOT / METROPOLIS_INFRASTRUCTURE
            RegisterHierarchyChildren("ENVIRONMENT_ROOT/METROPOLIS_INFRASTRUCTURE/07_Detailed_Street_Scenes/02_Intersection_Plazas", 250f);
            RegisterHierarchyChildren("ENVIRONMENT_ROOT/METROPOLIS_INFRASTRUCTURE/07_Detailed_Street_Scenes/03_District_Street_Life", 160f);
            RegisterHierarchyChildren("ENVIRONMENT_ROOT/METROPOLIS_INFRASTRUCTURE/07_Detailed_Street_Scenes/04_Overhead_Festoon_Lanterns", 180f);
            RegisterHierarchyChildren("ENVIRONMENT_ROOT/METROPOLIS_INFRASTRUCTURE/07_Detailed_Street_Scenes/05_Curb_Foliage_And_Moss", 100f);
            RegisterHierarchyChildren("ENVIRONMENT_ROOT/METROPOLIS_INFRASTRUCTURE/01_Master_Road_Network/04_Raised_Flagstone_Sidewalks", 250f);
            RegisterHierarchyChildren("ENVIRONMENT_ROOT/METROPOLIS_INFRASTRUCTURE/01_Master_Road_Network/05_Curbs_Gutters_And_DrainGrates", 150f);
            RegisterHierarchyChildren("ENVIRONMENT_ROOT/METROPOLIS_INFRASTRUCTURE/01_Master_Road_Network/06_Roadside_Wayfinding_And_Furnishings", 180f);

            // 6. Mountain Gorge & Trails
            RegisterHierarchyChildren("Mountain_AnimeNature_SwitchbackAndGorge_Root", mountainCullDistance);

            // 7. World Foliage & Landmarks
            RegisterHierarchyChildren("World_AnimeNature_Foliage", worldFoliageCullDistance);
            RegisterHierarchyChildren("Seaward_Port_Town_Lively", portTownCullDistance);
            RegisterHierarchyChildren("LIVELY_METROPOLIS_NPCS", npcCullDistance);
            RegisterHierarchyChildren("--- TOWN BEAUTIFICATION & ROADSIDE ---", propCullDistance);
            RegisterHierarchyChildren("--- ENEMY OUTPOSTS & DUNGEON PORTAL ---", 250f);
            RegisterHierarchyChildren("--- BEAUTIFIED SCENIC CITY PARKS ---", 180f);
            RegisterHierarchyChildren("City_Perimeter_16Towers", towerCullDistance);
            RegisterHierarchyChildren("Arena_Destructible_Pillars_Root", 200f);
            RegisterSingleObject("Courtyard_PracticeDummy_Armored", 150f);

            // 8. Register All Real-Time Point Lights for Distance Culling
            int lanternMask = LayerMask.GetMask("Default", "Props_Details");
            if (lanternMask == 0) lanternMask = 1; // Default layer fallback

            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < allLights.Length; i++)
            {
                var l = allLights[i];
                if (l == null || l.type == LightType.Directional) continue;

                // Optimize culling mask to avoid running against grass/foliage/water
                if (l.cullingMask == -1)
                {
                    l.cullingMask = lanternMask;
                }

                _lights.Add(new CullLight
                {
                    light = l,
                    position = l.transform.position,
                    isEnabled = l.enabled
                });
            }

            // 9. Register All Chimney Hearth Smoke Emitters
            var allParticles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < allParticles.Length; i++)
            {
                var ps = allParticles[i];
                if (ps == null) continue;
                if (ps.name.Contains("Chimney") || ps.name.Contains("Smoke") || ps.name.Contains("LeafParticle"))
                {
                    _chimneyParticles.Add(new CullParticle
                    {
                        particleSystem = ps,
                        position = ps.transform.position,
                        isPlaying = ps.isPlaying
                    });
                }
            }

            Debug.Log($"⚡ [CameraFrustumPropUnloader] Registered {_chunks.Count} world chunks, {_lights.Count} point lights, {_chimneyParticles.Count} particles.");
        }

        private void RegisterHierarchyChildren(string path, float maxDist)
        {
            var root = GameObject.Find(path);
            if (root == null) return;

            int childCount = root.transform.childCount;
            if (childCount == 0)
            {
                RegisterChunk(root, maxDist);
                return;
            }

            for (int i = 0; i < childCount; i++)
            {
                RegisterChunk(root.transform.GetChild(i).gameObject, maxDist);
            }
        }

        private void RegisterSingleObject(string name, float maxDist)
        {
            var go = GameObject.Find(name);
            if (go != null) RegisterChunk(go, maxDist);
        }

        private void RegisterChunk(GameObject go, float maxDist)
        {
            if (go == null) return;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            Bounds b;

            if (renderers.Length > 0)
            {
                b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) b.Encapsulate(renderers[i].bounds);
                }
            }
            else
            {
                // Fallback for empty groups or particle-only hierarchies
                b = new Bounds(go.transform.position, Vector3.one * 12.0f);
            }

            // Expand bounds by 6m to eliminate any visual popping at screen edges
            b.Expand(6.0f);

            _chunks.Add(new CullChunk
            {
                gameObject = go,
                worldBounds = b,
                maxDistance = maxDist,
                isLoaded = go.activeSelf
            });
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            // 1. Calculate camera view frustum planes
            GeometryUtility.CalculateFrustumPlanes(targetCamera, _frustumPlanes);
            Vector3 camPos = targetCamera.transform.position;
            float nearPreserveSq = nearPreserveRadius * nearPreserveRadius;
            float lightCullSq = lightCullDistance * lightCullDistance;
            float chimneyCullSq = chimneySmokeCullDistance * chimneySmokeCullDistance;

            // 2. Sliced Chunk Evaluations
            int chunkCount = _chunks.Count;
            if (chunkCount > 0)
            {
                int sliceSize = Mathf.CeilToInt((float)chunkCount / frameSlices);
                int startIdx = _currentSlice * sliceSize;
                int endIdx = Mathf.Min(startIdx + sliceSize, chunkCount);

                for (int i = startIdx; i < endIdx; i++)
                {
                    var chunk = _chunks[i];
                    if (chunk.gameObject == null) continue;

                    // Proximity check: always keep nearby props loaded
                    float sqrDist = (chunk.worldBounds.ClosestPoint(camPos) - camPos).sqrMagnitude;
                    if (sqrDist <= nearPreserveSq)
                    {
                        if (!chunk.isLoaded)
                        {
                            chunk.gameObject.SetActive(true);
                            chunk.isLoaded = true;
                            _chunks[i] = chunk;
                        }
                        continue;
                    }

                    // Maximum distance check
                    float maxDistSq = chunk.maxDistance * chunk.maxDistance;
                    if (sqrDist > maxDistSq)
                    {
                        if (chunk.isLoaded)
                        {
                            chunk.gameObject.SetActive(false);
                            chunk.isLoaded = false;
                            _chunks[i] = chunk;
                        }
                        continue;
                    }

                    // Frustum intersection test
                    bool inView = GeometryUtility.TestPlanesAABB(_frustumPlanes, chunk.worldBounds);
                    if (inView != chunk.isLoaded)
                    {
                        chunk.gameObject.SetActive(inView);
                        chunk.isLoaded = inView;
                        _chunks[i] = chunk;
                    }
                }
            }

            // 3. Sliced Point Light Distance Culling
            int lightCount = _lights.Count;
            if (lightCount > 0)
            {
                int lSliceSize = Mathf.CeilToInt((float)lightCount / frameSlices);
                int lStart = _currentSlice * lSliceSize;
                int lEnd = Mathf.Min(lStart + lSliceSize, lightCount);

                for (int i = lStart; i < lEnd; i++)
                {
                    var cl = _lights[i];
                    if (cl.light == null) continue;
                    if (!cl.light.gameObject.activeInHierarchy) continue;

                    float distSq = (cl.position - camPos).sqrMagnitude;
                    bool shouldBeOn = distSq <= lightCullSq;

                    if (shouldBeOn != cl.isEnabled)
                    {
                        cl.light.enabled = shouldBeOn;
                        cl.isEnabled = shouldBeOn;
                        _lights[i] = cl;
                    }
                }
            }

            // 4. Sliced Chimney / Tree Particle Simulation Culling
            int partCount = _chimneyParticles.Count;
            if (partCount > 0)
            {
                int pSliceSize = Mathf.CeilToInt((float)partCount / frameSlices);
                int pStart = _currentSlice * pSliceSize;
                int pEnd = Mathf.Min(pStart + pSliceSize, partCount);

                for (int i = pStart; i < pEnd; i++)
                {
                    var cp = _chimneyParticles[i];
                    if (cp.particleSystem == null) continue;
                    if (!cp.particleSystem.gameObject.activeInHierarchy) continue;

                    float distSq = (cp.position - camPos).sqrMagnitude;
                    bool shouldPlay = distSq <= chimneyCullSq;

                    if (shouldPlay != cp.isPlaying)
                    {
                        if (shouldPlay) cp.particleSystem.Play(true);
                        else cp.particleSystem.Pause(true);

                        cp.isPlaying = shouldPlay;
                        _chimneyParticles[i] = cp;
                    }
                }
            }

            _currentSlice = (_currentSlice + 1) % frameSlices;
        }

        /// <summary>
        /// Restores all managed entities to their active state (used when exiting play mode or disabling).
        /// </summary>
        private void RestoreAll()
        {
            for (int i = 0; i < _chunks.Count; i++)
            {
                var chunk = _chunks[i];
                if (chunk.gameObject != null && !chunk.gameObject.activeSelf)
                {
                    chunk.gameObject.SetActive(true);
                    chunk.isLoaded = true;
                    _chunks[i] = chunk;
                }
            }

            for (int i = 0; i < _lights.Count; i++)
            {
                var cl = _lights[i];
                if (cl.light != null && !cl.light.enabled)
                {
                    cl.light.enabled = true;
                    cl.isEnabled = true;
                    _lights[i] = cl;
                }
            }

            for (int i = 0; i < _chimneyParticles.Count; i++)
            {
                var cp = _chimneyParticles[i];
                if (cp.particleSystem != null && !cp.particleSystem.isPlaying)
                {
                    cp.particleSystem.Play(true);
                    cp.isPlaying = true;
                    _chimneyParticles[i] = cp;
                }
            }
        }
    }
}
