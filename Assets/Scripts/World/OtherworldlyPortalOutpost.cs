using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace BattleSummoner.World
{
    /// <summary>
    /// OtherworldlyPortalOutpost represents an active dimensional tear in the overworld
    /// fortified by hostile forward-base enemy forces:
    /// - Dimensional vortex with swirling cosmic rift shader/particles.
    /// - Fortified outpost: defensive barricades, void warding pylons, torchlight brazier.
    /// - Guarded by void defenders and an Outpost Commander elite.
    /// - Interactive proximity trigger transporting the summoner into the Dungeon Dimension.
    /// </summary>
    public class OtherworldlyPortalOutpost : MonoBehaviour
    {
        [Header("Portal Core References")]
        [SerializeField] private Transform portalVortexTransform;
        [SerializeField] private Light portalAuraLight;
        [SerializeField] private AudioSource portalAudioSource;
        [SerializeField] private BoxCollider portalTriggerCollider;

        [Header("Outpost Base Elements")]
        [SerializeField] private Transform outpostStructuresRoot;
        [SerializeField] private Transform defenderSpawnPointsRoot;
        [SerializeField] private List<GameObject> activeDefenders = new List<GameObject>();

        [Header("Target Dimension")]
        [SerializeField] private string targetDungeonSceneName = "MainDungeonFloor";
        [SerializeField] private string targetMarkerId = "DungeonDimension_Entry";
        [SerializeField] private string portalZoneTitle = "The Dungeon Dimension";

        [Header("In-World Prompt")]
        [SerializeField] private GameObject promptCanvasObj;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private float interactRadius = 4.2f;

        private Transform playerTransform;
        private bool isPlayerInRange = false;
        private bool isWarping = false;
        private float vortexRotationSpeed = 45f;

        public bool IsActivePortal => gameObject.activeSelf;

        private void Awake()
        {
            if (promptCanvasObj != null) promptCanvasObj.SetActive(false);
            SetupAudio();
        }

        private void Start()
        {
            FindPlayer();
            SpawnOutpostDefenders();
        }

        private void Update()
        {
            // Spin vortex
            if (portalVortexTransform != null)
            {
                portalVortexTransform.Rotate(Vector3.forward, vortexRotationSpeed * Time.deltaTime, Space.Self);
            }

            // Pulse aura light
            if (portalAuraLight != null)
            {
                portalAuraLight.intensity = 2.5f + Mathf.Sin(Time.time * 3.5f) * 0.8f;
            }

            CheckPlayerProximity();

            // Interact key [F] or enter
            if (isPlayerInRange && !isWarping)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    EnterDungeonDimension();
                }
            }
        }

        private void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null) playerTransform = prog.transform;
            }
        }

        private void CheckPlayerProximity()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= interactRadius;

            if (inRange != isPlayerInRange)
            {
                isPlayerInRange = inRange;
                if (promptCanvasObj != null)
                {
                    promptCanvasObj.SetActive(isPlayerInRange);
                    if (promptTMP != null)
                    {
                        promptTMP.text = "<color=#FFEE44><b>[F] / TAP</b></color> // <color=#40EBFF>ENTER DUNGEON DIMENSION</color>\n<size=75%><color=#D0A0FF>Risk of Rain Scaler Active • Area Boons Await</color></size>";
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isWarping)
            {
                EnterDungeonDimension();
            }
        }

        public void EnterDungeonDimension()
        {
            if (isWarping) return;
            isWarping = true;

            Debug.Log($"🌌 [OTHERWORLDLY PORTAL] Stepping through reality tear into {portalZoneTitle}!");

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("portal_warp", 1.0f);
            }

            // If we are currently in MainDungeonFloor, start the dimensional run directly
            var dimMgr = BattleSummoner.DungeonDimension.DungeonDimensionManager.Instance;
            if (dimMgr != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == targetDungeonSceneName)
            {
                dimMgr.StartDungeonDimensionRun();
                isWarping = false;
                return;
            }

            // Otherwise, smooth fade transition into the dungeon scene
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene(targetDungeonSceneName, targetMarkerId, portalZoneTitle);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetDungeonSceneName);
            }
        }

        private void SetupAudio()
        {
            if (portalAudioSource == null)
            {
                portalAudioSource = gameObject.AddComponent<AudioSource>();
                portalAudioSource.spatialBlend = 0.85f;
                portalAudioSource.minDistance = 3f;
                portalAudioSource.maxDistance = 25f;
                portalAudioSource.loop = true;
                portalAudioSource.volume = 0.55f;
                portalAudioSource.playOnAwake = true;

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    var hum = BattleSummonerAudioMaster.Instance.GetProceduralClip("portal_ambient_hum");
                    if (hum != null)
                    {
                        portalAudioSource.clip = hum;
                        portalAudioSource.Play();
                    }
                }
            }
        }

        public void SpawnOutpostDefenders()
        {
            // Spawn 3 outpost sentries if not already spawned
            if (activeDefenders.Count > 0) return;

            Vector3 basePos = transform.position;
            Vector3[] offsets = {
                new Vector3(-4.5f, 0f, -2.5f),
                new Vector3(4.5f, 0f, -2.5f),
                new Vector3(0f, 0f, -5.5f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject defender = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                defender.name = (i == 2) ? "Outpost_RiftCommander_Elite" : $"Outpost_VoidSentry_{i + 1}";
                defender.tag = "Enemy";
                defender.transform.position = basePos + offsets[i];
                defender.transform.localScale = (i == 2) ? new Vector3(1.2f, 1.4f, 1.2f) : new Vector3(0.9f, 1.0f, 0.9f);

                // Material color
                var ren = defender.GetComponent<Renderer>();
                if (ren != null)
                {
                    ren.material.color = (i == 2) ? new Color(0.85f, 0.20f, 0.90f) : new Color(0.40f, 0.15f, 0.60f);
                }

                // Add SpireEnemy component
                var enemyComp = defender.AddComponent<SpireEnemy>();
                activeDefenders.Add(defender);
            }
        }
    }
}
