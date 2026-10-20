using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.DesignPhilosophy
{
    // =========================================================================
    // 1. ZELDA & CASTLEVANIA IV: SELECTIVE Z-AXIS CLEAVE, DIRECTIONAL GUARD
    //    SPECIALIZATION, IRON KNUCKLE PROVOCATION & BRIDGE SUMO KNOCKBACK
    // =========================================================================
    public class SequelitisAndHowAboutMasterEngine : MonoBehaviour
    {
        private static SequelitisAndHowAboutMasterEngine _instance;
        public static SequelitisAndHowAboutMasterEngine Instance
        {
            get
            {
                if (_instance == null)
                    _instance = UnityEngine.Object.FindAnyObjectByType<SequelitisAndHowAboutMasterEngine>(FindObjectsInactive.Include);
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("1. Selective Z-Axis Swarm Cleave (Fixes 3D Bat / Ant Slingshot Problem)")]
        [Tooltip("Vertical slab height (m) so wide horizontal sword swipes hit flying bats & low crawlers without Z-Targeting.")]
        public float selectiveZAxisVerticalSlabHeight = 3.6f;
        public float wideSweepSwarmRadius = 4.2f;
        public LayerMask combatAndEnvironmentLayer = ~0; // default to all layers

        [Header("2. Bridge & Sky-Ledge Sumo Knockback Physics")]
        public float reciprocalSlashKnockbackImpulse = 12.0f;

        [Header("3. Castlevania II Fix: Seamless Real-Time Day/Night (Zero Screen Freeze)")]
        public bool isServerNighttime;
        public event Action<bool> OnSeamlessDayNightChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            // Real-time server clock check without EVER freezing the screen for a text box!
            bool nightNow = (DateTime.Now.Hour >= 20 || DateTime.Now.Hour < 6);
            if (nightNow != isServerNighttime)
            {
                isServerNighttime = nightNow;
                OnSeamlessDayNightChanged?.Invoke(isServerNighttime);
            }
        }

        /// <summary>
        /// Hook into FruitNinjaBladeCutter.cs: Projects a Selective Z-Axis Slab on every swipe so flying bats
        /// and trash swarms are cleaved mid-stride without locking the camera into a disconnected Z-Target mode.
        /// </summary>
        public void ExecuteSelectiveZAxisSwipeCleave(
            Vector3 playerOrigin, Vector3 swipeDirectionWS, float swipeAngleDeg, float swipeSpeed, float baseDamage, string elementTag)
        {
            if (swipeDirectionWS.sqrMagnitude < 0.001f) swipeDirectionWS = transform.forward;
            Vector3 boxCenter = playerOrigin + swipeDirectionWS.normalized * 2.1f;
            Vector3 halfExtents = new Vector3(wideSweepSwarmRadius * 0.5f, selectiveZAxisVerticalSlabHeight * 0.5f, 2.2f);

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.LookRotation(swipeDirectionWS.normalized), combatAndEnvironmentLayer);
            foreach (Collider col in hits)
            {
                if (col == null) continue;
                if (col.TryGetComponent<IronKnuckleProvocableEnemy>(out var enemy))
                {
                    enemy.OnStruckOrProvokedByPlayer(swipeDirectionWS, swipeAngleDeg, damage: baseDamage, elementTag, transform);
                }
                if (col.TryGetComponent<SeverableBossLimb>(out var limb))
                {
                    limb.TrySeverLimb(baseDamage, swipeAngleDeg, elementTag);
                }
                if (col.TryGetComponent<ArenaDestructiblePillar>(out var pillar))
                {
                    pillar.OnHitByWeapon(baseDamage * 0.4f);
                }
                if (col.TryGetComponent<ZeroWaitInstantChest>(out var chest))
                {
                    chest.PopOpenInstantly(gameObject);
                }
            }
        }
    }

    // =========================================================================
    // 2. ZELDA SEQUELITIS: IRON KNUCKLE PROVOCATION AI, DIRECTIONAL SEAMS &
    //    MEGA MAN X STEALABLE GOLEM RIDE-ARMOR PILOTS
    // =========================================================================
    public class IronKnuckleProvocableEnemy : MonoBehaviour
    {
        public string enemyName = "Spire Anvil-Guard";
        public float currentHp = 450f;
        public float maxHp = 450f;

        [Header("Zero-Wait Provocation & Directional Guard (Super Castlevania IV Fix)")]
        public bool isGuardRaised = true;
        [Tooltip("Required slash angle (deg) to slice the exposed armor seam cleanly.")]
        public float exposedSeamSlashAngleDeg = 45f;
        public float seamAngleToleranceDeg = 35f;
        public float provokedVulnerabilityDuration = 1.4f;

        [Header("Mega Man X Stealable Golem Ride-Armor")]
        public StealableGolemRideArmor nearbyUnmountedGolem;
        public bool isMountedInGolem = false;

        private float _vulnerableTimer = 0f;
        private Rigidbody _rb;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        private void Update()
        {
            if (_vulnerableTimer > 0f)
            {
                _vulnerableTimer -= Time.deltaTime;
                if (_vulnerableTimer <= 0f) isGuardRaised = true;
            }

            // Sprint toward parked Golem Ride-Armor unless the player kills us first!
            if (!isMountedInGolem && nearbyUnmountedGolem != null && !nearbyUnmountedGolem.isOccupied)
            {
                transform.position = Vector3.MoveTowards(transform.position, nearbyUnmountedGolem.transform.position, 4.5f * Time.deltaTime);
                if (Vector3.Distance(transform.position, nearbyUnmountedGolem.transform.position) < 1.8f)
                {
                    nearbyUnmountedGolem.BoardGolemAsEnemy(this);
                }
            }
        }

        public void OnStruckOrProvokedByPlayer(
            Vector3 slashDirWS, float slashAngleDeg, float damage, string elementTag, Transform playerTransform)
        {
            if (isGuardRaised)
            {
                // ZERO WAITING: Player aggression immediately provokes the enemy into a heavy swing,
                // opening its armor seam AND smashing any nearby ArenaDestructiblePillars!
                isGuardRaised = false;
                _vulnerableTimer = provokedVulnerabilityDuration;
                ExecuteHeavyPillarSmashingSwing();
                Debug.Log($"[Iron Knuckle Pacing] Player provoked {enemyName}! Guard open for {provokedVulnerabilityDuration:F1}s.");
                return;
            }

            // Check directional seam precision (Super Castlevania IV anti-button-mash rule)
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(slashAngleDeg, exposedSeamSlashAngleDeg));
            float precisionMult = (angleDiff <= seamAngleToleranceDeg) ? 1.75f : 1.0f;

            currentHp -= damage * precisionMult;

            // Reciprocal Sumo Knockback on Bridges & Sky Island Ledges (Zelda Sequelitis)
            if (_rb != null && SequelitisAndHowAboutMasterEngine.Instance != null)
            {
                _rb.AddForce(slashDirWS.normalized * SequelitisAndHowAboutMasterEngine.Instance.reciprocalSlashKnockbackImpulse, ForceMode.Impulse);
            }

            if (currentHp <= 0f) Destroy(gameObject);
        }

        private void ExecuteHeavyPillarSmashingSwing()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 2.4f, 3.0f);
            foreach (var h in hits)
            {
                if (h.TryGetComponent<ArenaDestructiblePillar>(out var pillar))
                {
                    pillar.ShatterByEnemySwing();
                }
            }
        }
    }

    // =========================================================================
    // 3. ZELDA SEQUELITIS: DESTRUCTIBLE ARENA PILLARS (COMBAT + WORLD MERGER)
    // =========================================================================
    public class ArenaDestructiblePillar : MonoBehaviour
    {
        public GameObject recoveryHeartAndManaShardPrefab;
        public int shardsToDropOnShatter = 3;

        public void OnHitByWeapon(float dmg) { }

        public void ShatterByEnemySwing()
        {
            if (recoveryHeartAndManaShardPrefab != null)
            {
                for (int i = 0; i < shardsToDropOnShatter; i++)
                {
                    Vector3 pos = transform.position + UnityEngine.Random.insideUnitSphere * 1.4f;
                    pos.y = transform.position.y + 0.5f;
                    Instantiate(recoveryHeartAndManaShardPrefab, pos, Quaternion.identity);
                }
            }
            Debug.Log($"[Combat + World Merged] Enemy shattered pillar {name} -> Dropped {shardsToDropOnShatter} Recovery Shards!");
            Destroy(gameObject);
        }
    }

    // =========================================================================
    // 4. MEGA MAN X SEQUELITIS: STEALABLE GOLEM RIDE-ARMORS & BOSS PART SEVER
    // =========================================================================
    public class StealableGolemRideArmor : MonoBehaviour
    {
        public bool isOccupied = false;
        public bool isPilotedByPlayer = false;
        public float bonusMechShieldHp = 850f;

        public void BoardGolemAsEnemy(IronKnuckleProvocableEnemy pilot)
        {
            if (isOccupied) return;
            isOccupied = true;
            pilot.isMountedInGolem = true;
            pilot.transform.SetParent(transform);
            pilot.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        }

        public bool TryHijackByPlayer(GameObject player)
        {
            if (isOccupied) return false;
            isOccupied = true;
            isPilotedByPlayer = true;
            player.transform.SetParent(transform);
            player.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            Debug.Log("[Mega Man X Hijack] Killed enemy pilot early and stole the Golem Ride-Armor!");
            return true;
        }
    }

    public class SeverableBossLimb : MonoBehaviour
    {
        public string limbName = "Volcanic Magma Trunk";
        public string counterElementTag = "Water";
        public float seamAngleDeg = 30f;
        public float limbHp = 200f;
        public bool isSevered = false;
        public GameObject severedCatalystDropPrefab;

        public bool TrySeverLimb(float damage, float slashAngleDeg, string attackElement)
        {
            if (isSevered) return false;
            float elemMult = string.Equals(attackElement, counterElementTag, StringComparison.OrdinalIgnoreCase) ? 2.5f : 1.0f;
            float angleMult = (Mathf.Abs(Mathf.DeltaAngle(slashAngleDeg, seamAngleDeg)) <= 35f) ? 1.5f : 0.75f;

            limbHp -= damage * elemMult * angleMult;
            if (limbHp <= 0f)
            {
                isSevered = true;
                if (severedCatalystDropPrefab != null)
                {
                    Instantiate(severedCatalystDropPrefab, transform.position, Quaternion.identity);
                }
                Debug.Log($"[Boomer Kuwanger Sever!] Severed Boss Limb [{limbName}]! Signature boss attack disabled!");
                gameObject.SetActive(false);
                return true;
            }
            return false;
        }
    }

    // =========================================================================
    // 5. ZELDA SEQUELITIS: 0.15S ZERO-WAIT CHESTS, GIANT BIG-KEY CHEST &
    //    ACTIVE MINERAL-EATING COMPANION (GORON FIX)
    // =========================================================================
    public class ZeroWaitInstantChest : MonoBehaviour
    {
        public bool isCentralGiantMasterChest = false;
        public string rewardItemTitle = "3.0x Kinetic Strike Relic";
        public Transform chestLid;
        private bool _opened = false;

        public void PopOpenInstantly(GameObject opener, bool hasMasterKeystone = true)
        {
            if (_opened || (isCentralGiantMasterChest && !hasMasterKeystone)) return;
            _opened = true;
            if (chestLid != null) chestLid.localRotation = Quaternion.Euler(-115f, 0f, 0f);
            Debug.Log($"[0.15s Zero-Wait Chest] Instantly popped open -> Granted [{rewardItemTitle}] with zero cutscene delay!");
        }
    }

    public class ActiveMineralEatingCompanion : MonoBehaviour
    {
        public string companionName = "Bram, Iron-Peak Vanguard";
        public float currentEnergy = 100f;
        public float burnRatePerSecond = 1.5f;

        private void Update()
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - burnRatePerSecond * Time.deltaTime);
        }

        public void FeedDungeonOreOrBento(float amount)
        {
            currentEnergy = Mathf.Min(100f, currentEnergy + amount);
            Debug.Log($"[Zelda Sequelitis Goron Fix] Fed {companionName} mid-dungeon! Energy at {currentEnergy:F0}%.");
        }
    }
}

