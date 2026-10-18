using System.Collections;
using UnityEngine;

namespace BattleSummoner.World
{
    /// <summary>
    /// OverworldSpireWaypoint controls the central astral waypoint crystal at the Solas Plaza:
    /// - Smooth levitating and rotating celestial crystal with pulsating aura light.
    /// - Counter-rotating runic resonance ring.
    /// - Proximity recovery trigger: tunes resonance and restores HP & MP to full.
    /// </summary>
    public class OverworldSpireWaypoint : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Transform crystalTransform;
        [SerializeField] private Transform runeRingTransform;
        [SerializeField] private Light auraLight;
        [SerializeField] private ParticleSystem runeParticles;

        [Header("Animation Parameters")]
        [SerializeField] private float floatSpeed = 1.4f;
        [SerializeField] private float floatHeight = 0.35f;
        [SerializeField] private float crystalRotateSpeed = 22f;
        [SerializeField] private float ringRotateSpeed = -36f;
        [SerializeField] private float lightPulseSpeed = 1.8f;
        [SerializeField] private float baseLightIntensity = 2.2f;

        [Header("Resonance Tuning")]
        [SerializeField] private float resonanceRadius = 4.0f;
        [SerializeField] private float resonanceCooldown = 6.0f;

        private Vector3 initialCrystalPos;
        private float lastResonanceTime = -100f;
        private Transform playerTransform;

        private void Start()
        {
            if (crystalTransform != null)
            {
                initialCrystalPos = crystalTransform.localPosition;
            }

            var player = GameObject.Find("Player_Hero");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void Update()
        {
            float time = Time.time;

            // 1. Crystal Floating & Yaw Rotation
            if (crystalTransform != null)
            {
                float yOffset = Mathf.Sin(time * floatSpeed) * floatHeight;
                crystalTransform.localPosition = initialCrystalPos + new Vector3(0f, yOffset, 0f);
                crystalTransform.Rotate(Vector3.up, crystalRotateSpeed * Time.deltaTime, Space.World);
            }

            // 2. Runic Ring Counter-Rotation
            if (runeRingTransform != null)
            {
                runeRingTransform.Rotate(Vector3.up, ringRotateSpeed * Time.deltaTime, Space.World);
            }

            // 3. Pulsating Aura Light
            if (auraLight != null)
            {
                auraLight.intensity = baseLightIntensity + Mathf.Sin(time * lightPulseSpeed) * 0.5f;
            }

            // 4. Proximity Check for Player Resonance
            CheckPlayerProximity();
        }

        private void CheckPlayerProximity()
        {
            if (playerTransform == null)
            {
                var player = GameObject.Find("Player_Hero");
                if (player != null) playerTransform = player.transform;
                if (playerTransform == null) return;
            }

            float distSq = (playerTransform.position - transform.position).sqrMagnitude;
            if (distSq <= resonanceRadius * resonanceRadius)
            {
                if (Time.time - lastResonanceTime > resonanceCooldown)
                {
                    TuneAstralResonance();
                }
            }
        }

        public void TuneAstralResonance()
        {
            lastResonanceTime = Time.time;

            if (playerTransform != null)
            {
                var progression = playerTransform.GetComponent<PlayerProgression>();
                if (progression != null)
                {
                    progression.currentHp = progression.maxHp;
                    progression.currentMp = progression.maxMp;
                }
            }

            if (runeParticles != null)
            {
                runeParticles.Emit(25);
            }

            // Show HUD prompt
            var hud = FindAnyObjectByType<BattleSummoner.UI.OverworldPlayerHUDController>();
            if (hud != null)
            {
                hud.ShowToast("✦ ASTRAL WAYPOINT TUNED — VITALITY & MANA RESTORED ✦");
            }
        }
    }
}
