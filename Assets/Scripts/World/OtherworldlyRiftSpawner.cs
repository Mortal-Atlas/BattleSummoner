using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.World
{
    /// <summary>
    /// OtherworldlyRiftSpawner manages the periodic emergence of otherworldly portals
    /// and their accompanying enemy forward bases across the overworld:
    /// - Spawns active rifts on a dynamic interval.
    /// - Dispatches global broadcast alerts across the Spire and Overworld.
    /// - Positions enemy defensive barricades, void pylons, and outposts.
    /// </summary>
    public class OtherworldlyRiftSpawner : MonoBehaviour
    {
        private static OtherworldlyRiftSpawner _instance;
        public static OtherworldlyRiftSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<OtherworldlyRiftSpawner>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Spawn Interval Settings")]
        [SerializeField] private float minIntervalSeconds = 120f;  // 2 minutes
        [SerializeField] private float maxIntervalSeconds = 300f;  // 5 minutes
        [SerializeField] private bool autoSpawnOnStart = true;

        [Header("Spawn Waypoints (Overworld Coordinates)")]
        [SerializeField] private List<Vector3> potentialRiftLocations = new List<Vector3>
        {
            new Vector3(28f, 0f, -5f),   // Mistwood Forest edge
            new Vector3(-32f, 0f, 15f),  // Western Crag plateau
            new Vector3(38f, 0f, 25f),   // Ancient Shrine clearing
            new Vector3(-13f, 0f, 28f)   // Spire Approach valley
        };

        [Header("Active Outpost")]
        [SerializeField] private OtherworldlyPortalOutpost activeOutpost;

        private float nextSpawnTime = 0f;
        private bool isSpawningRoutineActive = false;

        public event Action<Vector3> OnRiftEmerged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            if (autoSpawnOnStart)
            {
                // Schedule first spawn shortly after start (e.g. 15s for instant demo experience)
                nextSpawnTime = Time.time + 15f;
                StartCoroutine(RiftSpawnLoop());
            }
        }

        private IEnumerator RiftSpawnLoop()
        {
            isSpawningRoutineActive = true;
            while (isSpawningRoutineActive)
            {
                yield return new WaitForSeconds(5.0f);

                if (Time.time >= nextSpawnTime)
                {
                    SpawnRiftOutpost();
                    float delay = UnityEngine.Random.Range(minIntervalSeconds, maxIntervalSeconds);
                    nextSpawnTime = Time.time + delay;
                }
            }
        }

        public void SpawnRiftOutpost(int waypointIndex = -1)
        {
            Vector3 targetPos;
            if (waypointIndex >= 0 && waypointIndex < potentialRiftLocations.Count)
            {
                targetPos = potentialRiftLocations[waypointIndex];
            }
            else
            {
                int idx = UnityEngine.Random.Range(0, potentialRiftLocations.Count);
                targetPos = potentialRiftLocations[idx];
            }

            if (activeOutpost == null)
            {
                GameObject baseGo = ConstructOutpostBaseGameObject(targetPos);
                activeOutpost = baseGo.GetComponent<OtherworldlyPortalOutpost>();
            }
            else
            {
                activeOutpost.transform.position = targetPos;
                activeOutpost.gameObject.SetActive(true);
                activeOutpost.SpawnOutpostDefenders();
            }

            // Global Broadcast Notification
            string alert = "🌌 <color=#D040FF>[DIMENSIONAL INVASION]</color> An <b>Otherworldly Portal</b> has torn open in the Overworld! Enemy forces have fortified the rift!";
            Debug.Log(alert);

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("An Otherworldly Rift has opened! Enemy forces are fortifying the rift!", true);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("ror_tier_up", 1.0f);
            }

            OnRiftEmerged?.Invoke(targetPos);
        }

        private GameObject ConstructOutpostBaseGameObject(Vector3 position)
        {
            GameObject outpostRoot = new GameObject("Otherworldly_Portal_Outpost", typeof(OtherworldlyPortalOutpost));
            outpostRoot.transform.position = position;

            // 1. Portal Gate Archway
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "Arch_Top";
            gate.transform.SetParent(outpostRoot.transform, false);
            gate.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            gate.transform.localScale = new Vector3(5.5f, 0.8f, 1.2f);
            gate.GetComponent<Renderer>().material.color = new Color(0.12f, 0.08f, 0.20f);

            GameObject pillarL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarL.name = "Pillar_Left";
            pillarL.transform.SetParent(outpostRoot.transform, false);
            pillarL.transform.localPosition = new Vector3(-2.2f, 2.2f, 0f);
            pillarL.transform.localScale = new Vector3(0.9f, 4.5f, 0.9f);
            pillarL.GetComponent<Renderer>().material.color = new Color(0.12f, 0.08f, 0.20f);

            GameObject pillarR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillarR.name = "Pillar_Right";
            pillarR.transform.SetParent(outpostRoot.transform, false);
            pillarR.transform.localPosition = new Vector3(2.2f, 2.2f, 0f);
            pillarR.transform.localScale = new Vector3(0.9f, 4.5f, 0.9f);
            pillarR.GetComponent<Renderer>().material.color = new Color(0.12f, 0.08f, 0.20f);

            // 2. Swirling Vortex Quad
            GameObject vortex = GameObject.CreatePrimitive(PrimitiveType.Quad);
            vortex.name = "PortalVortexDisc";
            vortex.transform.SetParent(outpostRoot.transform, false);
            vortex.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            vortex.transform.localScale = new Vector3(3.6f, 4.2f, 1f);
            var ren = vortex.GetComponent<Renderer>();
            ren.material.color = new Color(0.70f, 0.15f, 0.95f, 0.9f);

            // 3. Portal Light
            GameObject lightObj = new GameObject("PortalAuraLight", typeof(Light));
            lightObj.transform.SetParent(outpostRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0.5f);
            Light l = lightObj.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.85f, 0.20f, 1.0f);
            l.range = 16f;
            l.intensity = 3.2f;

            // 4. Portal Trigger Collider
            BoxCollider col = outpostRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 2.0f, 0f);
            col.size = new Vector3(3.8f, 4.0f, 2.5f);
            col.isTrigger = true;

            // 5. Fortified Barricades (Spikes & Sandbags)
            Vector3[] barricadeOffsets = {
                new Vector3(-4.0f, 0.6f, 3.0f),
                new Vector3(4.0f, 0.6f, 3.0f),
                new Vector3(-5.5f, 0.6f, -1.0f),
                new Vector3(5.5f, 0.6f, -1.0f)
            };
            for (int i = 0; i < barricadeOffsets.Length; i++)
            {
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"Barricade_{i + 1}";
                bar.transform.SetParent(outpostRoot.transform, false);
                bar.transform.localPosition = barricadeOffsets[i];
                bar.transform.localScale = new Vector3(2.5f, 1.2f, 0.6f);
                bar.GetComponent<Renderer>().material.color = new Color(0.28f, 0.18f, 0.12f);
            }

            // 6. Void Warding Pylons with glowing crystals
            Vector3[] pylonOffsets = {
                new Vector3(-3.5f, 0f, -4.0f),
                new Vector3(3.5f, 0f, -4.0f)
            };
            for (int p = 0; p < pylonOffsets.Length; p++)
            {
                GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pylon.name = $"VoidPylon_{p + 1}";
                pylon.transform.SetParent(outpostRoot.transform, false);
                pylon.transform.localPosition = pylonOffsets[p] + new Vector3(0f, 1.5f, 0f);
                pylon.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
                pylon.GetComponent<Renderer>().material.color = new Color(0.15f, 0.12f, 0.25f);

                // Crystal on top
                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crystal.name = "PylonCrystal";
                crystal.transform.SetParent(pylon.transform, false);
                crystal.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                crystal.transform.localScale = new Vector3(1.2f, 1.6f, 1.2f);
                crystal.GetComponent<Renderer>().material.color = new Color(0.90f, 0.20f, 0.95f);
            }

            // Wire fields on OtherworldlyPortalOutpost
            var outpostComp = outpostRoot.GetComponent<OtherworldlyPortalOutpost>();
            var fVortex = typeof(OtherworldlyPortalOutpost).GetField("portalVortexTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fVortex != null) fVortex.SetValue(outpostComp, vortex.transform);

            var fLight = typeof(OtherworldlyPortalOutpost).GetField("portalAuraLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fLight != null) fLight.SetValue(outpostComp, l);

            var fCol = typeof(OtherworldlyPortalOutpost).GetField("portalTriggerCollider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fCol != null) fCol.SetValue(outpostComp, col);

            return outpostRoot;
        }
    }
}
