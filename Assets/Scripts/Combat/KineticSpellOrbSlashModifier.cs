using UnityEngine;

namespace BattleSummoner.Combat
{
    public enum KineticSpellBallisticForm
    {
        StandardOrbProjectile,
        RazorChakramDisc,      // Created by slicing horizontally through the floating spell orb
        ShotgunShrapnelSpread, // Created by X-slashing (or Dual-Wield crossing) through the spell orb
        PiercingRailBeam       // Created by tap-thrusting the spell orb
    }

    [RequireComponent(typeof(SphereCollider))]
    public class KineticSpellOrbSlashModifier : MonoBehaviour
    {
        public string spellElementAffinity = "Fire";
        public float baseSpellDamage = 110f;
        public float launchSpeed = 24f;
        public KineticSpellBallisticForm currentForm = KineticSpellBallisticForm.StandardOrbProjectile;

        private bool _hasBeenSlashedOnce = false;
        private float _firstSlashAngle = 0f;
        private Vector3 _launchDirection = Vector3.forward;
        private bool _isLaunched = false;

        private void Update()
        {
            if (_isLaunched)
            {
                transform.position += _launchDirection * (launchSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Called by FruitNinjaBladeCutter when the player's finger-blade raycast slices through this floating spell orb!
        /// </summary>
        public void OnSlicedByPlayerBlade(float swipeAngleDeg, Vector3 aimDirection, bool isDualWieldCrossSlash)
        {
            _launchDirection = aimDirection.normalized;

            if (isDualWieldCrossSlash || (_hasBeenSlashedOnce && Mathf.Abs(Mathf.DeltaAngle(swipeAngleDeg, _firstSlashAngle)) > 55f))
            {
                // X-Slash -> Shatter into High-Spread Shotgun Shrapnel!
                currentForm = KineticSpellBallisticForm.ShotgunShrapnelSpread;
                transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
                baseSpellDamage *= 1.45f;
                launchSpeed = 32f;
                _isLaunched = true;
                Debug.Log($"[Kinetic Spell-Slash!] X-Slashed {spellElementAffinity} Orb -> Mutated into SHOTGUN SHRAPNEL BURST!");
                return;
            }

            if (!_hasBeenSlashedOnce)
            {
                _hasBeenSlashedOnce = true;
                _firstSlashAngle = swipeAngleDeg;

                // Flatten the orb into a high-RPM spinning Razor Chakram Disc aligned with the blade angle!
                currentForm = KineticSpellBallisticForm.RazorChakramDisc;
                transform.localScale = new Vector3(1.8f, 0.14f, 1.8f);
                transform.rotation = Quaternion.Euler(0f, 0f, swipeAngleDeg);
                baseSpellDamage *= 1.30f;
                launchSpeed = 36f;
                _isLaunched = true;
                Debug.Log($"[Kinetic Spell-Slash!] Sliced {spellElementAffinity} Orb at {swipeAngleDeg:F0}Â° -> Compressed into 4200 RPM RAZOR CHAKRAM!");
            }
        }
    }
}

