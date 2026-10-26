using System;
using UnityEngine;
using BattleSummoner.Progression.ThreeMasks;

namespace BattleSummoner.Combat
{
    public class WeightedBladeAndRangedLagController : MonoBehaviour
    {
        [Header("Weapon Mass & Proficiency Telemetry")]
        public float equippedWeapon4DMass = 35f;
        public int currentWeaponProficiencyLevel = 1;
        public bool isRangedWeapon = false;

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
            float proficiencyScalar = Mathf.Lerp(1.0f, 8.5f, currentWeaponProficiencyLevel / 100f);
            float massResistance = Mathf.Max(1.0f, equippedWeapon4DMass / 12f);

            float talentLagMod = 1.0f;
            if (ThreeMasksAndWildcardTalentSystem.Instance != null &&
                ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1 != null)
            {
                talentLagMod -= ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1.weaponLagReductionBonus;
            }

            currentFollowSpeed = (18f * (proficiencyScalar / massResistance)) / Mathf.Max(0.3f, talentLagMod);

            laggedWeaponTipWorldPos = Vector3.Lerp(
                laggedWeaponTipWorldPos,
                targetFingerWorldPos,
                Time.deltaTime * currentFollowSpeed);

            if (!isRangedWeapon && equippedWeapon4DMass >= 45f && Vector3.Distance(laggedWeaponTipWorldPos, targetFingerWorldPos) > 1.2f)
            {
                OnHeavyFloorDragSparks?.Invoke(laggedWeaponTipWorldPos);
            }
        }

        public void NotifyAttackBlockedOrResisted()
        {
            _adaptationWindowExpiresAt = Time.time + 3.0f;
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
