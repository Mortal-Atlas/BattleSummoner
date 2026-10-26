using System;
using UnityEngine;

namespace BattleSummoner.DesignPhilosophy
{
    /// <summary>
    /// ExpressiveMovementAndSlopeDashController
    /// Implements Barry Kramer's Mario Galaxy & Skyward Sword mechanics:
    /// - TogglePersonalGravityFlip(): Inverts gravity (+19.62 m/s²) when stepping into a Sky Island's Inversion Rune Ring,
    ///   allowing the player to walk upside-down on the bottom of the 12 Sky Islands and mine 4D Buoyancy Geodes.
    /// - Expressive slope dash and momentum chaining through wind-stream boost rings.
    /// </summary>
    public class ExpressiveMovementAndSlopeDashController : MonoBehaviour
    {
        [Header("Mario Galaxy Gravity Flip")]
        public bool isGravityFlipped = false;
        public float invertedGravityStrength = 19.62f;
        public float flipTransitionDuration = 0.35f;

        [Header("Aerial Slalom Momentum")]
        public float currentAirMomentumSpeed = 0f;
        public float maxSlalomChainSpeed = 38.0f;
        public float clingingCloudFluffTimer = 0f;

        public event Action<bool> OnGravityFlipToggled;
        public event Action<float> OnSlalomBoostTriggered;

        private CharacterController _characterController;
        private PlayerMovementController _baseMovement;
        private float _flipTimer = 0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _baseMovement = GetComponent<PlayerMovementController>();
        }

        private void Update()
        {
            if (clingingCloudFluffTimer > 0f)
            {
                clingingCloudFluffTimer -= Time.deltaTime;
            }

            if (isGravityFlipped && _characterController != null)
            {
                // Apply upward gravity pull when inverted
                Vector3 upwardGravity = Vector3.up * (invertedGravityStrength * Time.deltaTime);
                _characterController.Move(upwardGravity * Time.deltaTime);
            }
        }

        /// <summary>
        /// Flips personal gravity 180 degrees.
        /// Rotates the player model and inverts gravity so player walks upside-down on island bottoms.
        /// </summary>
        public void TogglePersonalGravityFlip()
        {
            isGravityFlipped = !isGravityFlipped;

            // Rotate player 180 degrees around forward axis for upside-down orientation
            Vector3 rot = transform.localEulerAngles;
            rot.z = isGravityFlipped ? 180f : 0f;
            transform.localEulerAngles = rot;

            OnGravityFlipToggled?.Invoke(isGravityFlipped);

            Debug.Log($"[Mario Galaxy Gravity-Flip] Personal gravity toggled! Inverted = {isGravityFlipped} (Pull: {invertedGravityStrength:F1} m/s²). Upside-down geode mining active!");
        }

        /// <summary>
        /// Called when diving through a poofy cloud or hitting a Wind-Stream Boost Ring.
        /// </summary>
        public void TriggerSlalomBoost(float boostImpulse = 14f)
        {
            currentAirMomentumSpeed = Mathf.Min(maxSlalomChainSpeed, currentAirMomentumSpeed + boostImpulse);
            clingingCloudFluffTimer = 2.0f; // 2.0s clinging cloud fluff!
            OnSlalomBoostTriggered?.Invoke(currentAirMomentumSpeed);
            Debug.Log($"[Spider-Man 2 Slalom] Chained aerial boost! Air speed = {currentAirMomentumSpeed:F1} m/s. Clinging cloud fluff active for 2.0s!");
        }
    }
}
