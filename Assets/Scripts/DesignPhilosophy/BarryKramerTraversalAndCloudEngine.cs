using System;
using UnityEngine;

namespace BattleSummoner.DesignPhilosophy
{
    // =========================================================================
    // 6. BARRY KRAMER (MARIO GALAXY, DEATH STRANDING & SKYWARD SWORD'S SKY):
    //    SLOPE DASH, EXPRESSIVE MOVEMENT, ASYNC VINE LADDERS & CLOUD FEEL
    // =========================================================================
    public class BarryKramerTraversalAndCloudEngine : MonoBehaviour
    {
        [Header("Skyward Sword Sky Fix: Azure Altitude Lerp & 2s Clinging Cloud Fluff")]
        public Color groundWarmFogColor = new Color(0.82f, 0.88f, 0.78f, 1f);
        public Color highAltitudeVibrantAzureColor = new Color(0.08f, 0.48f, 0.96f, 1f);
        public Color nightAladdinMoonlitSkyColor = new Color(0.04f, 0.11f, 0.28f, 1f);
        public ParticleSystem cloudEntryPoofBurstPrefab;
        public ParticleSystem cloudExitFwooshBurstPrefab;
        public ParticleSystem playerClingingCloudBitsEmitter;
        public AudioSource diveWindAudioSource;
        public AudioSource vocalJubilationWhoopSource;
        public AudioClip[] diveWhoopShouts;

        private float _clingingCloudFluffTimer = 0f;

        private void Update()
        {
            // 1. Dynamic Altitude Sky Palette (No Beige Smog!) + 24/7 Moonlit Night Flight
            float altT = Mathf.InverseLerp(180f, 480f, transform.position.y);
            bool isNight = SequelitisAndHowAboutMasterEngine.Instance != null && SequelitisAndHowAboutMasterEngine.Instance.isServerNighttime;
            Color targetSky = isNight ? nightAladdinMoonlitSkyColor : highAltitudeVibrantAzureColor;
            RenderSettings.fogColor = Color.Lerp(groundWarmFogColor, targetSky, altT);

            // 2. 2.0-Second Clinging Cloud Fluff Decay after exiting a cloud (Steve Swink Game Feel)
            if (_clingingCloudFluffTimer > 0f)
            {
                _clingingCloudFluffTimer -= Time.deltaTime;
                if (_clingingCloudFluffTimer <= 0f && playerClingingCloudBitsEmitter != null)
                {
                    playerClingingCloudBitsEmitter.Stop();
                }
            }
        }

        /// <summary>
        /// Mega Man X + Mario Galaxy Fix: Projects dash/shield-surf velocity parallel to any slope normal
        /// so ascending the 4-tiered Magic City ramps conserves 100% speed without stair clipping.
        /// </summary>
        public Vector3 ProjectSlopeConservingDash(Vector3 desiredVelocity, Vector3 groundNormal)
        {
            return Vector3.ProjectOnPlane(desiredVelocity, groundNormal).normalized * desiredVelocity.magnitude;
        }

        public void OnEnterCloudVolume(Vector3 pos)
        {
            if (cloudEntryPoofBurstPrefab != null) Instantiate(cloudEntryPoofBurstPrefab, pos, Quaternion.identity);
        }

        public void OnExitCloudVolume(Vector3 pos, Vector3 exitVelocity)
        {
            if (cloudExitFwooshBurstPrefab != null && exitVelocity.sqrMagnitude > 0.01f)
                Instantiate(cloudExitFwooshBurstPrefab, pos, Quaternion.LookRotation(exitVelocity.normalized));

            _clingingCloudFluffTimer = 2.0f;
            if (playerClingingCloudBitsEmitter != null && !playerClingingCloudBitsEmitter.isPlaying)
                playerClingingCloudBitsEmitter.Play();

            if (exitVelocity.y < -20f && vocalJubilationWhoopSource != null && diveWhoopShouts != null && diveWhoopShouts.Length > 0)
            {
                vocalJubilationWhoopSource.PlayOneShot(diveWhoopShouts[UnityEngine.Random.Range(0, diveWhoopShouts.Length)]);
            }
        }
    }
}
