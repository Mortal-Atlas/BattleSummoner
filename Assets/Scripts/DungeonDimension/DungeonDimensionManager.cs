using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public enum DungeonDimensionState
    {
        Inactive,
        InRoom,
        RoomCleared,
        RunCompleted,
        Defeated
    }

    /// <summary>
    /// DungeonDimensionManager orchestrates the multi-area roguelike dungeon dive:
    /// - Tracks current Area / Room progression (1 to 4+).
    /// - Spawns enemy waves dynamically scaled by the Risk of Rain difficulty clock.
    /// - Detects room clear conditions and triggers the temporary Dungeon Boon reward screen.
    /// - Spawns the Dimensional Rift Gateway to teleport to the next area.
    /// - Enforces temporary run buff persistence (buffs last only for that dungeon run).
    /// </summary>
    public class DungeonDimensionManager : MonoBehaviour
    {
        private static DungeonDimensionManager _instance;
        public static DungeonDimensionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DungeonDimensionManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DungeonDimensionManager");
                        _instance = go.AddComponent<DungeonDimensionManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Run State")]
        [SerializeField] private DungeonDimensionState runState = DungeonDimensionState.Inactive;
        [SerializeField] private int currentAreaIndex = 1;
        [SerializeField] private string currentAreaName = "Threshold of the Void";
        [SerializeField] private int enemiesRemaining = 0;

        [Header("Spawn Settings")]
        [SerializeField] private Transform arenaCenterTransform;
        [SerializeField] private float arenaSpawnRadius = 14f;
        [SerializeField] private GameObject exitGatewayObj;

        private readonly List<GameObject> activeEnemies = new List<GameObject>();
        private bool isSpawningWave = false;

        public DungeonDimensionState RunState => runState;
        public int CurrentAreaIndex => currentAreaIndex;
        public string CurrentAreaName => currentAreaName;
        public int EnemiesRemaining => enemiesRemaining;

        public event Action<int, string> OnAreaStarted;
        public event Action<int> OnAreaCleared;
        public event Action<int> OnEnemyCountChanged;
        public event Action OnRunEnded;

        private static readonly string[] AreaTitles = {
            "Threshold of the Void",
            "Abyssal Sanctum",
            "Cataclysmic Core",
            "Nexus of the Ancients"
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // Auto-detect arena center
            if (arenaCenterTransform == null)
            {
                var floor = GameObject.Find("DungeonFloorArena");
                if (floor != null) arenaCenterTransform = floor.transform;
            }

            // If we are in MainDungeonFloor and loaded with intention of a dimensional run, auto-start
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainDungeonFloor")
            {
                // Ready for run
            }
        }

        public void StartDungeonDimensionRun()
        {
            Debug.Log("🌀 [DUNGEON DIMENSION] Commencing Otherworldly Dungeon Dive!");

            // 1. Reset Risk of Rain Clock
            var clock = RiskOfRainDifficultyClock.Instance;
            if (clock != null)
            {
                clock.StartRun();
            }

            // 2. Clear previous temporary buffs
            var buffs = DungeonRunBuffSystem.Instance;
            if (buffs != null)
            {
                buffs.ResetRunBuffs();
            }

            // 3. Setup Area 1
            currentAreaIndex = 1;
            runState = DungeonDimensionState.InRoom;
            if (exitGatewayObj != null) exitGatewayObj.SetActive(false);

            StartCoroutine(PrepareAndSpawnArea(currentAreaIndex));
        }

        private IEnumerator PrepareAndSpawnArea(int areaIndex)
        {
            isSpawningWave = true;
            int titleIdx = Mathf.Clamp(areaIndex - 1, 0, AreaTitles.Length - 1);
            currentAreaName = AreaTitles[titleIdx];

            if (exitGatewayObj != null) exitGatewayObj.SetActive(false);

            OnAreaStarted?.Invoke(areaIndex, currentAreaName);

            // Announce Area Title Toast
            string areaToast = $"⚔ <color=#40EBFF>[AREA {areaIndex}]</color> <b>{currentAreaName.ToUpper()}</b>";
            if (Combat.CombatFeedbackManager.Instance != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                Vector3 toastPos = player != null ? player.transform.position + Vector3.up * 2.8f : Vector3.up * 3f;
                // Toast
            }

            yield return new WaitForSeconds(1.2f);

            ClearActiveEnemies();
            SpawnAreaEnemies(areaIndex);
            isSpawningWave = false;
        }

        private void SpawnAreaEnemies(int areaIndex)
        {
            float coeff = (RiskOfRainDifficultyClock.Instance != null) 
                ? RiskOfRainDifficultyClock.Instance.GetDifficultyCoefficient() 
                : 1.0f;

            int waveCount = 3 + (areaIndex - 1) * 2;
            enemiesRemaining = waveCount;
            OnEnemyCountChanged?.Invoke(enemiesRemaining);

            Vector3 center = (arenaCenterTransform != null) ? arenaCenterTransform.position : Vector3.zero;

            for (int i = 0; i < waveCount; i++)
            {
                float angle = (i / (float)waveCount) * Mathf.PI * 2f;
                float r = UnityEngine.Random.Range(8f, arenaSpawnRadius);
                Vector3 spawnPos = center + new Vector3(Mathf.Cos(angle) * r, 0.8f, Mathf.Sin(angle) * r);

                bool isElite = (i == waveCount - 1 && areaIndex >= 2) || (UnityEngine.Random.value < 0.25f * coeff);

                GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyObj.name = isElite ? $"VoidElite_Area{areaIndex}_{i + 1}" : $"VoidMob_Area{areaIndex}_{i + 1}";
                enemyObj.tag = "Enemy";
                enemyObj.transform.position = spawnPos;

                float scale = isElite ? 1.35f : 1.0f;
                enemyObj.transform.localScale = Vector3.one * scale;

                var ren = enemyObj.GetComponent<Renderer>();
                if (ren != null)
                {
                    ren.material.color = isElite 
                        ? new Color(0.95f, 0.15f, 0.80f) // Magenta Elite
                        : new Color(0.35f, 0.15f, 0.55f); // Abyssal Purple
                }

                // Add SpireEnemy with scaled stats
                var enemyComp = enemyObj.AddComponent<SpireEnemy>();
                ScaleEnemyStats(enemyComp, isElite, areaIndex, coeff);

                // Add Death Listener Monitor
                var deathWatcher = enemyObj.AddComponent<EnemyDeathWatcher>();
                deathWatcher.Initialize(this);

                activeEnemies.Add(enemyObj);
            }

            Debug.Log($"👾 [DUNGEON DIMENSION] Spawned Area {areaIndex} wave ({waveCount} enemies) at Risk of Rain Coeff {coeff:F2}x.");
        }

        private void ScaleEnemyStats(SpireEnemy enemy, bool isElite, int areaIndex, float diffCoeff)
        {
            // Base: HP 150, Damage 12, Speed 2.2
            float baseHp = isElite ? 380f : 150f;
            float baseDmg = isElite ? 24f : 12f;
            float baseSpd = 2.2f;

            float scaledHp = baseHp * diffCoeff * (1f + (areaIndex - 1) * 0.25f);
            float scaledDmg = baseDmg * (1f + (diffCoeff - 1f) * 0.55f);
            float scaledSpd = baseSpd * Mathf.Min(1.65f, 1f + (diffCoeff - 1f) * 0.08f);

            var t = typeof(SpireEnemy);
            var fMaxHp = t.GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fCurHp = t.GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fDmg = t.GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fSpd = t.GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var fTier = t.GetField("mobTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fMaxHp != null) fMaxHp.SetValue(enemy, scaledHp);
            if (fCurHp != null) fCurHp.SetValue(enemy, scaledHp);
            if (fDmg != null) fDmg.SetValue(enemy, scaledDmg);
            if (fSpd != null) fSpd.SetValue(enemy, scaledSpd);
            if (fTier != null) fTier.SetValue(enemy, isElite ? 3 : 1);
        }

        public void NotifyEnemyDefeated(GameObject enemy)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
            }

            // Check if player has Cull the Weak boon (kinetic explosion on kill)
            if (DungeonRunBuffSystem.Instance != null && DungeonRunBuffSystem.Instance.HasSlainExplosion(out float explodePct))
            {
                TriggerSlainKineticExplosion(enemy.transform.position, explodePct);
            }

            enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
            OnEnemyCountChanged?.Invoke(enemiesRemaining);

            if (enemiesRemaining <= 0 && runState == DungeonDimensionState.InRoom && !isSpawningWave)
            {
                HandleRoomCleared();
            }
        }

        private void TriggerSlainKineticExplosion(Vector3 pos, float damagePercent)
        {
            Collider[] hits = Physics.OverlapSphere(pos, 5.5f);
            float explodeDamage = 60f * damagePercent;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Enemy") && hits[i].TryGetComponent<SpireEnemy>(out var nearbyEnemy))
                {
                    nearbyEnemy.TakeDamage(explodeDamage);
                }
            }
        }

        private void HandleRoomCleared()
        {
            runState = DungeonDimensionState.RoomCleared;
            Debug.Log($"🎉 [DUNGEON DIMENSION] Area {currentAreaIndex} ({currentAreaName}) CLEARED! Presenting temporary dungeon boons.");

            // Pause difficulty clock during boon selection
            if (RiskOfRainDifficultyClock.Instance != null)
            {
                RiskOfRainDifficultyClock.Instance.PauseClock();
            }

            // Spawn Gateway to Next Area
            SpawnExitGateway();

            // Trigger UI & Rewards
            OnAreaCleared?.Invoke(currentAreaIndex);

            if (DungeonBoonRewardUI.Instance != null)
            {
                DungeonBoonRewardUI.Instance.OpenRewardScreen(currentAreaIndex, () =>
                {
                    // Resumes after boon picked
                    if (RiskOfRainDifficultyClock.Instance != null)
                    {
                        RiskOfRainDifficultyClock.Instance.ResumeClock();
                    }
                });
            }
        }

        private void SpawnExitGateway()
        {
            Vector3 gatePos = (arenaCenterTransform != null) 
                ? arenaCenterTransform.position + new Vector3(0f, 0f, 10f) 
                : new Vector3(0f, 0f, 10f);

            if (exitGatewayObj == null)
            {
                exitGatewayObj = new GameObject("Dimensional_Rift_Gateway");
                exitGatewayObj.transform.position = gatePos;

                // Arch & Vortex
                GameObject vortex = GameObject.CreatePrimitive(PrimitiveType.Quad);
                vortex.name = "GatewayVortex";
                vortex.transform.SetParent(exitGatewayObj.transform, false);
                vortex.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                vortex.transform.localScale = new Vector3(3.5f, 4.5f, 1f);
                vortex.GetComponent<Renderer>().material.color = new Color(0.20f, 0.90f, 1.00f, 0.9f);

                GameObject lightObj = new GameObject("GatewayLight", typeof(Light));
                lightObj.transform.SetParent(exitGatewayObj.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 2.5f, 0.5f);
                Light l = lightObj.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.30f, 0.90f, 1.00f);
                l.range = 14f;
                l.intensity = 3.0f;

                BoxCollider col = exitGatewayObj.AddComponent<BoxCollider>();
                col.size = new Vector3(3.5f, 4f, 2f);
                col.center = new Vector3(0f, 2f, 0f);
                col.isTrigger = true;

                exitGatewayObj.AddComponent<DimensionalGatewayTrigger>();
            }
            else
            {
                exitGatewayObj.transform.position = gatePos;
                exitGatewayObj.SetActive(true);
            }
        }

        public void AdvanceToNextArea()
        {
            if (currentAreaIndex >= AreaTitles.Length)
            {
                // Run complete!
                CompleteRun();
                return;
            }

            currentAreaIndex++;
            runState = DungeonDimensionState.InRoom;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("portal_warp", 1.0f);
            }

            StartCoroutine(PrepareAndSpawnArea(currentAreaIndex));
        }

        public void CompleteRun()
        {
            runState = DungeonDimensionState.RunCompleted;
            Debug.Log("🏆 [DUNGEON DIMENSION] Dimensional Nexus cleared! Run Victory!");

            if (RiskOfRainDifficultyClock.Instance != null)
            {
                RiskOfRainDifficultyClock.Instance.PauseClock();
            }

            // Clear temporary buffs upon returning to reality
            if (DungeonRunBuffSystem.Instance != null)
            {
                DungeonRunBuffSystem.Instance.ResetRunBuffs();
            }

            OnRunEnded?.Invoke();

            // Smooth exit back to Overworld
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene("OverworldMap", "Overworld_TavernDoor", "Solas Town Overworld");
            }
        }

        public void EndRunOnDefeat()
        {
            runState = DungeonDimensionState.Defeated;
            Debug.Log("💀 [DUNGEON DIMENSION] Dimensional collapse. The summoner was banished back to the Overworld.");

            if (RiskOfRainDifficultyClock.Instance != null)
            {
                RiskOfRainDifficultyClock.Instance.StopAndReset();
            }

            // Clear temporary buffs
            if (DungeonRunBuffSystem.Instance != null)
            {
                DungeonRunBuffSystem.Instance.ResetRunBuffs();
            }

            OnRunEnded?.Invoke();

            // Return to Overworld
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene("OverworldMap", "Overworld_TavernDoor", "Solas Town - Revived");
            }
        }

        private void ClearActiveEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    Destroy(activeEnemies[i]);
                }
            }
            activeEnemies.Clear();
        }
    }

    /// <summary>
    /// Helper component attached to enemies to notify the manager upon death.
    /// </summary>
    public class EnemyDeathWatcher : MonoBehaviour
    {
        private DungeonDimensionManager manager;

        public void Initialize(DungeonDimensionManager mgr)
        {
            manager = mgr;
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.NotifyEnemyDefeated(gameObject);
            }
        }
    }

    /// <summary>
    /// Trigger component for the exit gateway between areas.
    /// </summary>
    public class DimensionalGatewayTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (DungeonDimensionManager.Instance != null)
                {
                    DungeonDimensionManager.Instance.AdvanceToNextArea();
                }
            }
        }
    }
}
