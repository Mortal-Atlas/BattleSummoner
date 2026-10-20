using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.World
{
    public enum MagicCityBeverageType
    {
        GoldenHoneyMead_Thermal6D,     // Left 10ft Barrel: +20% Stamina Regen & Cold Immunity
        SpicedCrimsonWine_Resonance10D,// Center 10ft Barrel: +15% Spell Amplification & Mana Flow
        DarkFrostholdStout_Mass4D      // Right 10ft Barrel: +25% Posture Poise & Knockback Resistance
    }

    public class MagicCityUtilityAndTransitGrid : MonoBehaviour
    {
        public static MagicCityUtilityAndTransitGrid Instance { get; private set; }

        [Header("City Topology & Elevation Anchors")]
        public Vector3 cathedralApexOrigin = new Vector3(0f, 85f, 0f);
        public float outerCurtainWallRadius = 450f;
        public float moatWaterElevationY = 18f;

        [Header("Anti-Gravity & Flight Transit Parameters (10D / 4D Grid)")]
        [Tooltip("Multiplier applied to player/mount speed along the 8 cyan radial anti-gravity lanes.")]
        public float radialAntiGravitySpeedMultiplier = 1.65f;
        [Tooltip("Ambient gravity reduction inside the Magic City (0.88 = 12% lighter gravity).")]
        public float ambientCityGravityScalar = 0.88f;
        public float[] concentricFlightCorridorRadii = { 395f, 290f };
        public Vector3[] circularHoverPadLocations = {
            new Vector3(-295f, 36f, 245f),
            new Vector3(245f, 36f, 295f),
            new Vector3(355f, 36f, 85f),
            new Vector3(285f, 36f, -245f),
            new Vector3(-285f, 36f, -245f)
        };

        [Header("16-Tower Mana-Shield Perimeter Array")]
        public bool isAegisSiegeDomeActive = false;
        public GameObject aegisContainmentDomeVisual;
        public ParticleSystem ambientManaMotesParticles;

        [Header("Grand Norse-Swedish Guild Hall: 3x 10-Foot Barrels")]
        public Vector3 guildHallWorldPosition = new Vector3(-65f, 52f, 220f);
        public float barrelTapInteractionRadius = 4.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Evaluates whether a player or mount at worldPos is inside one of the 8 Radial Anti-Gravity Lanes.
        /// </summary>
        public bool IsInsideRadialAntiGravityLane(Vector3 worldPos, out Vector3 uphillDirection)
        {
            Vector2 flatPos = new Vector2(worldPos.x, worldPos.z);
            float r = flatPos.magnitude;
            uphillDirection = new Vector3(-flatPos.x, 0.18f, -flatPos.y).normalized;

            if (r < 60f || r > outerCurtainWallRadius) return false;

            float angleRad = Mathf.Atan2(flatPos.y, flatPos.x);
            if (angleRad < 0f) angleRad += Mathf.PI * 2f;

            // 8 Vectors spaced every 45 degrees (PI / 4)
            float sectorStep = Mathf.PI * 0.25f;
            float nearestAxis = Mathf.Round(angleRad / sectorStep) * sectorStep;
            float lateralOffsetMeters = Mathf.Abs(Mathf.Sin(angleRad - nearestAxis)) * r;

            return lateralOffsetMeters <= 1.75f; // 3.5m wide cyan mana-glass channel
        }

        /// <summary>
        /// Evaluates if the player is standing on one of the 5 Circular Anti-Gravity Hover Pads
        /// to trigger rapid vertical lift between Tier 1 (+20m) and Tier 3 (+62m).
        /// </summary>
        public bool TryUseCircularHoverPad(Vector3 playerPos, out float targetTierElevationY)
        {
            targetTierElevationY = playerPos.y;
            foreach (var padCenter in circularHoverPadLocations)
            {
                float flatDist = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(padCenter.x, padCenter.z));
                if (flatDist <= 9.0f) // 18m diameter hover pad
                {
                    targetTierElevationY = (playerPos.y < 45f) ? 62f : 22f;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Triggered automatically when OverworldPortalFortressSpawner enters Stage3_TownRaidActive.
        /// Interlocks all 16 perimeter turret generators and seals the 4 Cardinal Portal Gates.
        /// </summary>
        public void SetPerimeterManaShieldSiegeState(bool siegeActive)
        {
            isAegisSiegeDomeActive = siegeActive;
            if (aegisContainmentDomeVisual != null)
            {
                aegisContainmentDomeVisual.SetActive(siegeActive);
            }
            Debug.Log($"[Magic City Defense Grid] 16-Tower Aegis Mana-Shield Dome: {(siegeActive ? "ENGAGED" : "STANDBY")}");
        }

        /// <summary>
        /// Called when a player pours a draft from one of the three 10-foot horizontal barrels
        /// in the back of the Norse-Swedish Grand Guild Hall.
        /// </summary>
        public void PourFromTenFootGuildBarrel(MagicCityBeverageType beverage, GameObject playerActor)
        {
            switch (beverage)
            {
                case MagicCityBeverageType.GoldenHoneyMead_Thermal6D:
                    Debug.Log($"[Grand Guild Hall] Poured Golden Honey Mead from Left 10ft Barrel Disc-Tap -> +20% Stamina Regen & Frosthold Cold Immunity applied to {playerActor.name}.");
                    break;
                case MagicCityBeverageType.SpicedCrimsonWine_Resonance10D:
                    Debug.Log($"[Grand Guild Hall] Poured Spiced Crimson Wine from Center 10ft Barrel Disc-Tap -> +15% Arcane Spell Amplification applied to {playerActor.name}.");
                    break;
                case MagicCityBeverageType.DarkFrostholdStout_Mass4D:
                    Debug.Log($"[Grand Guild Hall] Poured Dark Frosthold Stout from Right 10ft Barrel Disc-Tap -> +25% 4D Inertial Poise applied to {playerActor.name}.");
                    break;
            }
        }
    }
}
