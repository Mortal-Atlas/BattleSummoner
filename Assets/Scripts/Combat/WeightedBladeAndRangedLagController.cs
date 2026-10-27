using System;
using UnityEngine;
using BattleSummoner.Progression.ThreeMasks;

namespace BattleSummoner.Combat
{
    public class WeightedBladeAndRangedLagController : MonoBehaviour
    {
        [Header("Weapon Mass & Proficiency Telemetry")]
        public float baseSpeed = 24.0f;
        public float strengthScalar = 2.5f;
        public float equippedWeapon4DMass = 35f;
        public int currentWeaponProficiencyLevel = 1;
        public bool isRangedWeapon = false;
        public float dullBladeKnockbackMultiplier = 0.0f; // 0% Dull Blade Blunt Knockback

        [Header("Ranged Aim-Lag & Stamina Sway")]
        public float rangedAimSwayIntensity = 0.35f;
        public float playerStamina01 = 1.0f;

        [Header("Live Finger vs. Blade Tip Tracking")]
        public Vector3 targetFingerWorldPos;
        public Vector3 laggedWeaponTipWorldPos;
        public float currentFollowSpeed { get; private set; }

        private float _adaptationWindowExpiresAt = 0f;
        public bool isTacticalPivotReady => Time.time <= _adaptationWindowExpiresAt;

        public event Action<Vector3> OnHeavyFloorDragSparks;
        public event Action OnTacticalPivotTriggered;

        private void Update()
        {
            float talentLagMod = 1.0f;
            if (ThreeMasksAndWildcardTalentSystem.Instance != null &&
                ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1 != null)
            {
                talentLagMod -= ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1.weaponLagReductionBonus;
            }

            // Exact Mathematical Equation:
            // FollowSpeed = BaseSpeed * ((ProficiencyLevel + StrengthScalar) / WeaponMass_4D) * TalentLagModifier
            float massDenominator = Mathf.Max(0.5f, equippedWeapon4DMass);
            currentFollowSpeed = baseSpeed * ((currentWeaponProficiencyLevel + strengthScalar) / massDenominator) * Mathf.Max(0.1f, talentLagMod);

            Vector3 destinationPos = targetFingerWorldPos;

            // Ranged Aim-Lag and Stamina Sway for low proficiency
            if (isRangedWeapon && currentWeaponProficiencyLevel < 20)
            {
                float swayFactor = (1.0f - (currentWeaponProficiencyLevel / 20f)) * (1.1f - playerStamina01);
                float swayX = Mathf.Sin(Time.time * 3.5f) * rangedAimSwayIntensity * swayFactor;
                float swayY = Mathf.Cos(Time.time * 2.8f) * rangedAimSwayIntensity * swayFactor;
                destinationPos += new Vector3(swayX, swayY, 0f);
            }

            laggedWeaponTipWorldPos = Vector3.Lerp(
                laggedWeaponTipWorldPos,
                destinationPos,
                Time.deltaTime * currentFollowSpeed);

            // Floor Drag-Sparks on heavy/unproficient weapons
            if (!isRangedWeapon && equippedWeapon4DMass >= 40f && Vector3.Distance(laggedWeaponTipWorldPos, targetFingerWorldPos) > 1.1f)
            {
                OnHeavyFloorDragSparks?.Invoke(laggedWeaponTipWorldPos);
                if (ZeroGCMicroVFXDirector.Instance != null)
                {
                    ZeroGCMicroVFXDirector.Instance.PlaySlashSpark(laggedWeaponTipWorldPos, Vector3.up, new Color(1f, 0.65f, 0.2f));
                }
            }
        }

        public void NotifyAttackBlockedOrResisted()
        {
            _adaptationWindowExpiresAt = Time.time + 3.0f; // 3.0s Tactical Pivot Surge window
        }

        public bool TryConsumeTacticalPivotOnWeaponOrElementSwap()
        {
            if (Time.time <= _adaptationWindowExpiresAt)
            {
                _adaptationWindowExpiresAt = 0f;
                OnTacticalPivotTriggered?.Invoke();
                return true;
            }
            return false;
        }
    }
}
