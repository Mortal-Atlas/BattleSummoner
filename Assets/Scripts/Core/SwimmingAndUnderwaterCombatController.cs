using System;
using UnityEngine;
using BattleSummoner.Combat;

namespace BattleSummoner.Core
{
    public enum SwimState
    {
        DryGrounded,
        WadingShallow,
        SurfaceSwimming,
        UnderwaterDiving,
        SeabedHeavyWalking
    }

    /// <summary>
    /// Attach to any River, Lake, Ocean, or Flooded Dungeon Chamber BoxCollider/MeshCollider (isTrigger = true).
    /// </summary>
    public class WaterVolumeZone : MonoBehaviour
    {
        public string waterBodyName = "Subterranean Flooded Cavern";
        public float waterSurfaceElevationY = 0f;
        public Vector3 waterCurrentFlowVelocity = Vector3.zero;
        public GameObject frozenIcePlatformPrefab;

        private void Reset()
        {
            waterSurfaceElevationY = transform.position.y + (transform.localScale.y * 0.5f);
        }
    }

    /// <summary>
    /// Section 12: 3D Swimming, Underwater Diving, Breath/Buoyancy Physics & Aquatic Combat.
    /// Manages water volume detection, 3D buoyancy, oxygen/breath management,
    /// non-clipping ally squad swimming, ice-platform freezing, and underwater combat modifiers.
    /// </summary>
    public class SwimmingAndUnderwaterCombatController : MonoBehaviour
    {
        public static SwimmingAndUnderwaterCombatController Instance { get; private set; }

        [Header("1. Swimming State & References")]
        public SwimState currentSwimState = SwimState.DryGrounded;
        public CharacterController playerController;
        public WaterVolumeZone activeWaterZone;
        public Camera mainCamera;

        [Header("2. Swim Speed & Buoyancy Physics (4D Mass Integration)")]
        public float baseSurfaceSwimSpeed = 4.8f;
        public float baseUnderwaterSwimSpeed = 4.2f;
        public float verticalDiveAscendSpeed = 3.6f;
        public float dolphinBreachJumpImpulse = 9.0f;
        [Tooltip("If total equipment mass exceeds this threshold, player sinks to walk on the seabed.")]
        public float heavyArmorSinkMassThreshold = 45f;
        public float currentEquipmentMass4D = 12f;

        [Header("3. Breath / Oxygen Meter & Aquatic Buffs")]
        public float maxBreathSeconds = 35f;
        public float currentBreathSeconds = 35f;
        public float drowningDamagePerSecond = 12f;
        public bool hasWaterBreathingFoodOrSpellBuff = false;

        [Header("4. Underwater Visual & Audio Filters")]
        public GameObject underwaterPostProcessVolumeObj;
        public AudioLowPassFilter underwaterAudioFilter;

        public event Action<SwimState> OnSwimStateChanged;
        public event Action<float, float> OnBreathMeterUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            currentBreathSeconds = maxBreathSeconds;

            if (playerController == null) playerController = GetComponent<CharacterController>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<WaterVolumeZone>(out var zone))
            {
                activeWaterZone = zone;
                EvaluateWaterSubmersionState();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<WaterVolumeZone>(out var zone) && activeWaterZone == zone)
            {
                activeWaterZone = null;
                TransitionToSwimState(SwimState.DryGrounded);
            }
        }

        private void Update()
        {
            if (activeWaterZone == null) return;

            EvaluateWaterSubmersionState();
            TickBreathAndOxygenSystem();
        }

        public void EvaluateWaterSubmersionState()
        {
            if (activeWaterZone == null) return;

            float surfaceY = activeWaterZone.waterSurfaceElevationY;
            float playerFeetY = transform.position.y;
            float submersionDepth = surfaceY - playerFeetY;

            // Automatically dismount non-aquatic land mounts in deep water
            if (submersionDepth > 1.35f && CoreVerticalSliceSandboxController.Instance != null && CoreVerticalSliceSandboxController.Instance.isMounted)
            {
                CoreVerticalSliceSandboxController.Instance.OnToggleMountButtonPressed();
            }

            if (submersionDepth <= 0.45f)
            {
                TransitionToSwimState(SwimState.DryGrounded);
            }
            else if (submersionDepth <= 1.25f && currentSwimState != SwimState.UnderwaterDiving)
            {
                TransitionToSwimState(SwimState.WadingShallow);
            }
            else if (currentSwimState != SwimState.UnderwaterDiving && currentSwimState != SwimState.SeabedHeavyWalking)
            {
                if (currentEquipmentMass4D >= heavyArmorSinkMassThreshold)
                {
                    TransitionToSwimState(SwimState.SeabedHeavyWalking);
                }
                else
                {
                    TransitionToSwimState(SwimState.SurfaceSwimming);
                }
            }
        }

        /// <summary>
        /// Called by Portrait Touch Movement Zone & Dive/Ascend buttons while in water.
        /// </summary>
        public void ExecuteSwimMovement(Vector2 horizontalTouchInput, float verticalDiveInput)
        {
            if (activeWaterZone == null || playerController == null) return;

            // Dynamically level up the On-The-Fly Swimming Skill!
            float swimSkillMult = 1.0f;
            if (MonsterResearchAndInfiniteWeaponSystem.Instance != null && horizontalTouchInput.sqrMagnitude > 0.05f)
            {
                var swimSkill = MonsterResearchAndInfiniteWeaponSystem.Instance.UnlockSkillOrPerformActionOnTheFly(
                    "Skill_AquaticSwimming",
                    "Aquatic Swimming & Deep Diving",
                    Time.deltaTime * 2.5f);
                swimSkillMult = 1.0f + (swimSkill.skillLevel * 0.04f);
            }

            Vector3 camForward = mainCamera != null ? mainCamera.transform.forward : transform.forward;
            Vector3 camRight = mainCamera != null ? mainCamera.transform.right : transform.right;

            Vector3 moveDir = Vector3.zero;
            if (currentSwimState == SwimState.SurfaceSwimming)
            {
                camForward.y = 0f;
                camRight.y = 0f;
                moveDir = (camForward.normalized * horizontalTouchInput.y + camRight.normalized * horizontalTouchInput.x);

                // Keep swimmer buoyant at chest height on the water surface unless diving
                float targetY = activeWaterZone.waterSurfaceElevationY - 1.15f;
                float buoyancyY = (targetY - transform.position.y) * 5.0f;

                if (verticalDiveInput < -0.2f)
                {
                    TransitionToSwimState(SwimState.UnderwaterDiving);
                }

                Vector3 velocity = (moveDir * baseSurfaceSwimSpeed * swimSkillMult) + activeWaterZone.waterCurrentFlowVelocity;
                velocity.y = buoyancyY;
                playerController.Move(velocity * Time.deltaTime);
            }
            else if (currentSwimState == SwimState.UnderwaterDiving || currentSwimState == SwimState.SeabedHeavyWalking)
            {
                // Full 3D Pitch + Yaw underwater navigation
                moveDir = (camForward * horizontalTouchInput.y + camRight * horizontalTouchInput.x);
                float verticalVel = verticalDiveInput * verticalDiveAscendSpeed * swimSkillMult;

                if (currentSwimState == SwimState.SeabedHeavyWalking && verticalDiveInput <= 0f)
                {
                    verticalVel = -6.0f; // Heavy 4D Armor Mass anchors player to the seabed floor
                }

                Vector3 velocity = (moveDir * baseUnderwaterSwimSpeed * swimSkillMult) + activeWaterZone.waterCurrentFlowVelocity;
                velocity.y += verticalVel;
                playerController.Move(velocity * Time.deltaTime);

                // Check for Dolphin Breach when ascending past the water surface!
                if (transform.position.y >= activeWaterZone.waterSurfaceElevationY - 1.05f && verticalDiveInput > 0.2f)
                {
                    TransitionToSwimState(SwimState.SurfaceSwimming);
                    Debug.Log("[Swimming] Executed Dolphin Surface Breach!");
                }
            }

            if (moveDir.sqrMagnitude > 0.01f)
            {
                Vector3 rotDir = moveDir;
                rotDir.y = 0f;
                if (rotDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(rotDir.normalized), Time.deltaTime * 10f);
                }
            }
        }

        public void OnDiveButtonPressed()
        {
            if (activeWaterZone == null) return;
            TransitionToSwimState(SwimState.UnderwaterDiving);
        }

        public void OnSurfaceOrBreachButtonPressed()
        {
            if (activeWaterZone == null) return;
            ExecuteSwimMovement(Vector2.zero, 1.0f);
        }

        public void RefillBreathFromUnderwaterBubbleVent(float refillAmount = 20f)
        {
            currentBreathSeconds = Mathf.Min(maxBreathSeconds, currentBreathSeconds + refillAmount);
            OnBreathMeterUpdated?.Invoke(currentBreathSeconds, maxBreathSeconds);
            Debug.Log("[Underwater Bubble Vent] Inhaled subterranean air pocket! Breath restored.");
        }

        private void TickBreathAndOxygenSystem()
        {
            bool isSubmerged = (currentSwimState == SwimState.UnderwaterDiving || currentSwimState == SwimState.SeabedHeavyWalking);

            if (isSubmerged && !hasWaterBreathingFoodOrSpellBuff)
            {
                currentBreathSeconds = Mathf.Max(0f, currentBreathSeconds - Time.deltaTime);
                OnBreathMeterUpdated?.Invoke(currentBreathSeconds, maxBreathSeconds);

                if (currentBreathSeconds <= 0f)
                {
                    Debug.LogWarning($"[Drowning Warning!] Out of Breath underwater! Taking {drowningDamagePerSecond * Time.deltaTime:F1} HP damage!");
                }
            }
            else if (!isSubmerged && currentBreathSeconds < maxBreathSeconds)
            {
                currentBreathSeconds = Mathf.Min(maxBreathSeconds, currentBreathSeconds + (Time.deltaTime * 10f));
                OnBreathMeterUpdated?.Invoke(currentBreathSeconds, maxBreathSeconds);
            }
        }

        /// <summary>
        /// Evaluates hydrodynamic combat modifiers when fighting underwater:
        /// Spears/Glaives/Thrusts hit at 1.25x damage with zero drag; wide slashes suffer 0.65x water drag.
        /// Lightning spells chain-conduct for 1.75x damage; Ice freezes a surface platform!
        /// </summary>
        public float EvaluateAquaticCombatModifier(
            bool isPiercingThrustOrSpear,
            SpellAffinity castElement,
            Vector3 castWorldPos,
            out string aquaticCombatNote)
        {
            if (currentSwimState != SwimState.UnderwaterDiving &&
                currentSwimState != SwimState.SurfaceSwimming &&
                currentSwimState != SwimState.SeabedHeavyWalking)
            {
                aquaticCombatNote = "Dry Land Standard";
                return 1.0f;
            }

            // 1. Spell Interactions in Water
            if (castElement == SpellAffinity.Lightning)
            {
                aquaticCombatNote = "[WATER CONDUCTIVITY!] Lightning spell electrified the entire water volume (1.75x AoE Chain Shock)!";
                return 1.75f;
            }
            if (castElement == SpellAffinity.Fire)
            {
                aquaticCombatNote = "[THERMAL BOIL!] Fire spell boiled the surrounding water into a scalding Steam Burst (0.85x Dmg + Blind)!";
                return 0.85f;
            }
            if (castElement == SpellAffinity.Water)
            {
                aquaticCombatNote = "[TIDAL RESONANCE!] Water magic amplified by surrounding water body (1.40x Potency)!";
                return 1.40f;
            }

            // 2. Melee Weapon Hydrodynamics (Thrusts/Spears vs. Wide Slashes)
            if (isPiercingThrustOrSpear)
            {
                aquaticCombatNote = "[HYDRODYNAMIC THRUST!] Piercing/Spear strike cut cleanly through the water (1.25x Underwater Crit)!";
                return 1.25f;
            }

            aquaticCombatNote = "[WATER DRAG] Wide blade slash slowed by underwater resistance (0.65x Dmg). Use Thrusts, Spears, or Lightning!";
            return 0.65f;
        }

        /// <summary>
        /// Allows casting Ice on the water surface to freeze a solid Ice Platform that players and allies can walk on.
        /// </summary>
        public GameObject TryFreezeWaterSurfaceIntoIcePlatform(Vector3 targetWorldXZ)
        {
            if (activeWaterZone == null || activeWaterZone.frozenIcePlatformPrefab == null) return null;

            Vector3 spawnPos = new Vector3(targetWorldXZ.x, activeWaterZone.waterSurfaceElevationY, targetWorldXZ.z);
            GameObject icePlatform = Instantiate(activeWaterZone.frozenIcePlatformPrefab, spawnPos, Quaternion.identity);
            Destroy(icePlatform, 20f); // Melts after 20 seconds
            Debug.Log($"[Cryo Water Mastery] Froze a solid Ice Platform on the surface of '{activeWaterZone.waterBodyName}' for 20s!");
            return icePlatform;
        }

        public void TransitionToSwimState(SwimState newState)
        {
            if (currentSwimState == newState) return;
            currentSwimState = newState;

            bool isUnderwater = (newState == SwimState.UnderwaterDiving || newState == SwimState.SeabedHeavyWalking);
            if (underwaterPostProcessVolumeObj != null) underwaterPostProcessVolumeObj.SetActive(isUnderwater);
            if (underwaterAudioFilter != null) underwaterAudioFilter.enabled = isUnderwater;

            OnSwimStateChanged?.Invoke(newState);
            Debug.Log($"[Swimming System] State changed -> {newState}");
        }
    }
}
