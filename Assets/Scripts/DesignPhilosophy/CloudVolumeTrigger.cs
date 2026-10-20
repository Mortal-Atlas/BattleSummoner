using UnityEngine;

namespace BattleSummoner.DesignPhilosophy
{
    /// <summary>
    /// Attached to each of the 85 Wispy & Poofy Slalom Clouds in the Sky Archipelago.
    /// Routes entry/exit events directly into BarryKramerTraversalAndCloudEngine
    /// to trigger Wind Waker poofs, fwooshes, and 2.0s clinging cloud fluff on the player.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CloudVolumeTrigger : MonoBehaviour
    {
        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.name.Contains("Player"))
            {
                var cloudEngine = other.GetComponentInChildren<BarryKramerTraversalAndCloudEngine>();
                if (cloudEngine == null)
                    cloudEngine = Object.FindAnyObjectByType<BarryKramerTraversalAndCloudEngine>();

                if (cloudEngine != null)
                {
                    cloudEngine.OnEnterCloudVolume(transform.position);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.name.Contains("Player"))
            {
                var cloudEngine = other.GetComponentInChildren<BarryKramerTraversalAndCloudEngine>();
                if (cloudEngine == null)
                    cloudEngine = Object.FindAnyObjectByType<BarryKramerTraversalAndCloudEngine>();

                if (cloudEngine != null)
                {
                    Vector3 exitVel = Vector3.down * 22f; // default high dive velocity
                    var rb = other.GetComponent<Rigidbody>();
                    if (rb != null) exitVel = rb.linearVelocity;
                    else
                    {
                        var cc = other.GetComponent<CharacterController>();
                        if (cc != null) exitVel = cc.velocity;
                    }

                    cloudEngine.OnExitCloudVolume(transform.position, exitVel);
                }
            }
        }
    }
}
