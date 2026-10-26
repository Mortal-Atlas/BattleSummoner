using System;
using UnityEngine;

namespace BattleSummoner.WorldActivities
{
    /// <summary>
    /// Smooth continuous rotation for Farmhouse & Sky-Island Windmills.
    /// Rotates the rotor sails continuously to serve as visual weenie landmarks and live wind indicators.
    /// </summary>
    public class WindmillRotorSpinner : MonoBehaviour
    {
        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 38.0f;
        public Vector3 rotationAxis = Vector3.forward;

        private void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Trigger component on Sky Island Inversion Rune Rings.
    /// Flips player gravity when passing through the ring onto the underside cavern.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SkyIslandGravityRingTrigger : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.name.Contains("Hero") || other.name.Contains("Player"))
            {
                if (WorldAndSkyActivityDirector.Instance != null)
                {
                    WorldAndSkyActivityDirector.Instance.OnSkyIslandGravityRingTriggered(other.gameObject);
                }
            }
        }
    }
}
