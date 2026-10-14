using System;
using UnityEngine;

namespace BattleSummoner.Mounts
{
    public enum MountType
    {
        RammingWarChariot,   // Wheeled construct with front iron plow & dual side cleaving scythes
        FamiliarBeast,       // Quadruped beast familiar (e.g. Celestial Lion, Spectral Stag)
        ConstructGolem,      // Colossal runic stone / iron construct juggernaut
        PygmySprite          // Tiny familiar (too small to fit riders - validates size rejection)
    }

    public enum MountSizeTier
    {
        Tiny,       // 0.4m - 0.7m (e.g. Sprite, imp). Max carry: 15kg. CANNOT fit human riders!
        Small,      // 0.8m - 1.2m (e.g. hound, small raptor). Max carry: 45kg.
        Medium,     // 1.3m - 2.0m (e.g. War wolf, lion, stag). Max carry: 140kg. Fits 1 rider.
        Large,      // 2.1m - 3.5m (e.g. War chariot, stone golem, drake). Max carry: 450kg.
        Colossal    // 3.6m+ (e.g. Siege titan). Max carry: 2500kg. Multi-rider construct.
    }

    [System.Serializable]
    public class MountStats
    {
        [Header("Identity")]
        public string mountName = "Valkyrie Ramming Chariot";
        public MountType mountType = MountType.RammingWarChariot;
        public MountSizeTier sizeTier = MountSizeTier.Large;
        public float maxCarryWeightKg = 450f;
        public float minRiderHeight = 1.0f;
        public float maxRiderHeight = 2.6f;

        [Header("Locomotion")]
        public float cruiseSpeed = 9.5f;
        public float sprintSpeed = 14.5f;
        public float rammingTopSpeed = 22.0f;
        public float acceleration = 16.0f;
        public float turnSpeedDeg = 130f;
        public float jumpForce = 9.0f;

        [Header("Ramming Chariot Combat")]
        public float baseRamDamage = 180f;
        public float bossRamBonusDamage = 250f;
        public float ramHitRadius = 2.4f;
        public float trampleKnockbackForce = 14.0f;
        public float scytheCleaveDamage = 110f;
        public float scytheReach = 2.0f;

        [Header("Visual Dimensions")]
        public Vector3 chassisScale = new Vector3(2.2f, 1.4f, 3.2f);
        public Vector3 saddleLocalOffset = new Vector3(0f, 1.25f, -0.4f);
    }

    public static class MountFittingEvaluator
    {
        /// <summary>
        /// Evaluates if a given rider's dimensions and weight can physically fit on the mount.
        /// Fulfills the requirement: "People can ride familiar, summons and other constructs. If it can fit them that is."
        /// </summary>
        public static bool CanRiderFit(MountStats mount, float riderHeightMeters, float riderWeightKg, out string rejectionReason)
        {
            rejectionReason = string.Empty;

            // 1. Size Tier Incompatibility Check
            if (mount.sizeTier == MountSizeTier.Tiny)
            {
                rejectionReason = $"<color=#FF5555>[MOUNT REJECTED]</color>\n<b>{mount.mountName}</b> is a <b>Tiny familiar</b> (Tier 1)!\nIt is far too small to bear your weight. Requires Medium or larger construct.";
                return false;
            }

            if (mount.sizeTier == MountSizeTier.Small && riderHeightMeters > 1.25f)
            {
                rejectionReason = $"<color=#FF5555>[MOUNT REJECTED]</color>\n<b>{mount.mountName}</b> is a <b>Small construct</b>.\nYour height ({riderHeightMeters:F2}m) exceeds its saddle clearance (1.25m max).";
                return false;
            }

            // 2. Weight Capacity Check
            if (riderWeightKg > mount.maxCarryWeightKg)
            {
                rejectionReason = $"<color=#FF5555>[MOUNT REJECTED]</color>\nExceeds max carry capacity!\nRider weight ({riderWeightKg:F0}kg) exceeds {mount.mountName}'s limit ({mount.maxCarryWeightKg:F0}kg).";
                return false;
            }

            // 3. Height Bounds Check
            if (riderHeightMeters < mount.minRiderHeight)
            {
                rejectionReason = $"<color=#FF5555>[MOUNT REJECTED]</color>\nRider is too short to reach {mount.mountName}'s saddle and reins!";
                return false;
            }

            if (riderHeightMeters > mount.maxRiderHeight)
            {
                rejectionReason = $"<color=#FF5555>[MOUNT REJECTED]</color>\nRider is too tall for {mount.mountName}'s cockpit clearance!";
                return false;
            }

            return true;
        }
    }
}
