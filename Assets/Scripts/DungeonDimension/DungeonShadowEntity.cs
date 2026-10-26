using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;

namespace BattleSummoner.DungeonDimension
{
    public enum ShadowAwarenessState
    {
        Patrolling,
        Suspicious,
        Chasing
    }

    public class DungeonShadowEntity : MonoBehaviour
    {
        [Header("Shadow Attributes")]
        [SerializeField] private string shadowName = "Void Shade";
        [SerializeField] private bool isElite = false;
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private float detectionRange = 11f;
        [SerializeField] private float fieldOfViewAngle = 105f;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float chaseSpeed = 3.8f;

        [Header("Runtime State")]
        [SerializeField] private ShadowAwarenessState state = ShadowAwarenessState.Patrolling;

        private Vector3 spawnOrigin;
        private Vector3 targetPatrolPoint;
        private float nextPatrolTime = 0f;
        private Transform playerTransform;
        private SpireEnemy spireEnemy;
        private Light shadowAuraLight;
        private bool hasInitiatedCombat = false;

        public ShadowAwarenessState State => state;

        public void Initialize(bool elite, Vector3 roomCenter, float roomRadius)
        {
            isElite = elite;
            spawnOrigin = roomCenter;
            patrolRadius = Mathf.Max(4f, roomRadius * 0.45f);
            spireEnemy = GetComponent<SpireEnemy>();

            SetupAura();
            PickNewPatrolTarget();
        }

        private void Start()
        {
            if (spireEnemy == null) spireEnemy = GetComponent<SpireEnemy>();
            FindPlayer();
            SetupAura();
        }

        private void SetupAura()
        {
            var lightObj = transform.Find("ShadowAuraLight");
            if (lightObj != null)
            {
                shadowAuraLight = lightObj.GetComponent<Light>();
            }
            else
            {
                var go = new GameObject("ShadowAuraLight", typeof(Light));
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                shadowAuraLight = go.GetComponent<Light>();
                shadowAuraLight.type = LightType.Point;
                shadowAuraLight.range = 5f;
            }

            UpdateAuraColor();
        }

        private void UpdateAuraColor()
        {
            if (shadowAuraLight == null) return;

            if (isElite)
            {
                shadowAuraLight.color = new Color(0.95f, 0.15f, 0.85f);
                shadowAuraLight.intensity = 2.4f;
            }
            else if (state == ShadowAwarenessState.Chasing)
            {
                shadowAuraLight.color = new Color(1.0f, 0.2f, 0.2f);
                shadowAuraLight.intensity = 2.0f;
            }
            else
            {
                shadowAuraLight.color = new Color(0.35f, 0.2f, 0.6f);
                shadowAuraLight.intensity = 1.2f;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            switch (state)
            {
                case ShadowAwarenessState.Patrolling:
                    HandlePatrol();
                    CheckPlayerDetection(distToPlayer);
                    break;

                case ShadowAwarenessState.Suspicious:
                    HandleSuspicious();
                    CheckPlayerDetection(distToPlayer);
                    break;

                case ShadowAwarenessState.Chasing:
                    HandleChase(distToPlayer);
                    break;
            }
        }

        private void HandlePatrol()
        {
            if (Time.time >= nextPatrolTime)
            {
                PickNewPatrolTarget();
            }

            MoveToward(targetPatrolPoint, walkSpeed);
            if (Vector3.Distance(transform.position, targetPatrolPoint) < 1.2f)
            {
                nextPatrolTime = Time.time + Random.Range(2f, 4.5f);
            }
        }

        private void PickNewPatrolTarget()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            targetPatrolPoint = spawnOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);
            nextPatrolTime = Time.time + Random.Range(4f, 8f);
        }

        private void CheckPlayerDetection(float distToPlayer)
        {
            if (distToPlayer > detectionRange) return;

            Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);

            // Vision cone check or very close proximity
            if (angle < fieldOfViewAngle * 0.5f || distToPlayer < 2.8f)
            {
                EnterChaseState();
            }
        }

        private void HandleSuspicious()
        {
            // Face toward player
            Vector3 dir = (playerTransform.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3f);
            }
        }

        private void HandleChase(float distToPlayer)
        {
            MoveToward(playerTransform.position, chaseSpeed);

            // Ambush check: If shadow hits player from behind
            if (distToPlayer < 1.6f && !hasInitiatedCombat)
            {
                CheckAmbushCollision();
            }
        }

        public void EnterChaseState()
        {
            if (state == ShadowAwarenessState.Chasing) return;
            state = ShadowAwarenessState.Chasing;
            UpdateAuraColor();

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("glitch", 0.6f);
            }
        }

        private void CheckAmbushCollision()
        {
            hasInitiatedCombat = true;
            float dot = Vector3.Dot(playerTransform.forward, (transform.position - playerTransform.position).normalized);

            // If shadow is behind player (dot < -0.3f), it's an AMBUSH!
            if (dot < -0.3f)
            {
                TriggerAmbush();
            }
        }

        private void TriggerAmbush()
        {
            Debug.LogWarning("⚠️ [AMBUSH!] Shadow caught the Summoner unaware!");
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#FF2233><b>[AMBUSHED!]</b></color>\n<size=85%>Caught from behind! Staggered!</size>");
            }
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("heavy_impact", 1.0f);
            }

            var playerProg = playerTransform.GetComponent<PlayerProgression>();
            if (playerProg != null)
            {
                playerProg.TakeDirectDamage(18f);
            }
        }

        /// <summary>
        /// Invoked when the player strikes the shadow in 3rd person.
        /// Evaluates whether the strike qualified as an Advantage Strike (Persona / Metaphor style).
        /// </summary>
        public void EvaluatePlayerStrike(Transform attacker)
        {
            if (attacker == null) return;

            // Advantage criteria: Attacker is behind the shadow OR shadow was unalerted
            Vector3 shadowForward = transform.forward;
            Vector3 attackerForward = attacker.forward;
            float alignment = Vector3.Dot(shadowForward, attackerForward);

            bool isBackstrike = alignment > 0.25f; // Facing same direction = striking shadow's back
            bool isPreemptive = state != ShadowAwarenessState.Chasing;

            if (isBackstrike || isPreemptive)
            {
                TriggerAdvantageStrike();
            }
            else
            {
                EnterChaseState();
            }
        }

        private void TriggerAdvantageStrike()
        {
            Debug.Log("💥 [ADVANTAGE STRIKE!] Pre-emptive strike on Void Shadow!");

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#40EBFF><b>[ADVANTAGE STRIKE!]</b></color>\n<color=#FFD700><size=90%>Pre-emptive Burst! Enemies Stunned!</size></color>");
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 1.0f);
            }

            // Stun and damage this shadow and nearby room shadows
            Collider[] roomHits = Physics.OverlapSphere(transform.position, patrolRadius + 4f);
            for (int i = 0; i < roomHits.Length; i++)
            {
                if (roomHits[i].TryGetComponent<SpireEnemy>(out var enemy))
                {
                    enemy.Stagger(3.8f, transform.position);
                    enemy.TakeDamage(45f);
                }
            }
        }

        private void MoveToward(Vector3 target, float speed)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.05f) return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null) playerTransform = prog.transform;
            }
        }
    }
}
