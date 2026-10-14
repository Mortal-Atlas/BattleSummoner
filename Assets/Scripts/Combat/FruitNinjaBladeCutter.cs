using System;
using System.Collections;
using UnityEngine;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// ISliceable interface for entities and destructibles susceptible to gesture blade slashes.
    /// </summary>
    public interface ISliceable
    {
        void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage);
    }

    /// <summary>
    /// FruitNinjaBladeCutter pipes high-speed swipe velocity slashes through the
    /// UncappedAccessorySystem stack aggregation matrix to evaluate aggregate kinetic strike damage,
    /// triggering hit-stop impulses and cutting ISliceable targets.
    /// </summary>
    public class FruitNinjaBladeCutter : MonoBehaviour
    {
        public static FruitNinjaBladeCutter Instance { get; private set; }

        [Header("Kinetic Slash Tuning")]
        [SerializeField] private float baseSlashPower = 35f;
        [SerializeField] private float velocityScalingFactor = 0.08f;
        [SerializeField] private float hitStopDurationSec = 0.045f;
        [SerializeField] private LayerMask sliceableLayers = ~0;

        [Header("Audio & Feedback")]
        [SerializeField] private string hitSFXPreset = "blade_hit";

        public event Action<Vector3, float> OnSliceExecuted;

        private bool inHitStop = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            PortraitGestureCanvas.OnBladeSlash += HandleBladeSlash;
        }

        private void OnDisable()
        {
            PortraitGestureCanvas.OnBladeSlash -= HandleBladeSlash;
        }

        private void HandleBladeSlash(Vector2 direction, float velocity)
        {
            // 1. Calculate raw velocity damage from gesture stroke
            float rawDamage = baseSlashPower + (velocity * velocityScalingFactor);

            // 2. Pipe raw swipe velocity damage through UncappedAccessorySystem aggregation matrix
            float finalKineticDamage = rawDamage;
            if (UncappedAccessorySystem.Instance != null)
            {
                finalKineticDamage = UncappedAccessorySystem.Instance.CalculateTotalStrikeOutput(rawDamage);
            }

            // 3. Perform raycast sweep in world space along swipe vector
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 worldSlashDir = new Vector3(direction.x, 0f, direction.y).normalized;
            if (worldSlashDir.sqrMagnitude < 0.01f) worldSlashDir = cam.transform.forward;

            // Detect targets in front of player
            Collider[] colliders = Physics.OverlapSphere(cam.transform.position + cam.transform.forward * 2.5f, 3.5f, sliceableLayers);
            bool struckAny = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col.CompareTag("Player")) continue;

                Vector3 contactPoint = col.ClosestPoint(cam.transform.position);

                // Check ISliceable
                if (col.TryGetComponent<ISliceable>(out var sliceable))
                {
                    sliceable.OnSlice(contactPoint, worldSlashDir, finalKineticDamage);
                    struckAny = true;
                }
                else if (col.TryGetComponent<IDamageReceiver>(out var receiver))
                {
                    var payload = new DamagePayload(finalKineticDamage, DamageType.PhysicalSlash, contactPoint, worldSlashDir, gameObject);
                    receiver.TakeAdvancedDamage(payload);
                    struckAny = true;
                }
                else if (col.TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.TakeDamage((int)finalKineticDamage);
                    struckAny = true;
                }
            }

            if (struckAny)
            {
                // Hit-stop effect
                if (!inHitStop && gameObject.activeInHierarchy)
                {
                    StartCoroutine(ExecuteHitStopRoutine(hitStopDurationSec));
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX(hitSFXPreset, 0.9f);
                }

                OnSliceExecuted?.Invoke(cam.transform.position + cam.transform.forward * 2.5f, finalKineticDamage);
            }
        }

        private IEnumerator ExecuteHitStopRoutine(float duration)
        {
            inHitStop = true;
            float prevTimeScale = Time.timeScale;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = prevTimeScale;
            inHitStop = false;
        }

        /// <summary>
        /// Explicit API to slice a target directly through the uncapped matrix.
        /// </summary>
        public float ProcessSliceTarget(ISliceable target, Vector3 point, Vector3 direction, float rawDamage)
        {
            float finalKineticDamage = rawDamage;
            if (UncappedAccessorySystem.Instance != null)
            {
                finalKineticDamage = UncappedAccessorySystem.Instance.CalculateTotalStrikeOutput(rawDamage);
            }

            target?.OnSlice(point, direction, finalKineticDamage);
            return finalKineticDamage;
        }
    }
}
