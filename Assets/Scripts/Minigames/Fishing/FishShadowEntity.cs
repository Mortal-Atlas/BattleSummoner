using System;
using System.Collections;
using UnityEngine;

namespace BattleSummoner.Minigames.Fishing
{
    public enum FishAIState
    {
        IdleWander,
        NoticingBobber,
        ApproachingBobber,
        Nibbling,
        Biting,
        Fleeing,
        Caught
    }

    /// <summary>
    /// FishShadowEntity models an authentic Animal Crossing style fish shadow swimming in water:
    /// - Smooth wander paths with tail oscillation
    /// - Scent/Sight detection of nearby cast fishing bobbers
    /// - 1-4 tension-building nibbles (bloop ripples, bobber dips)
    /// - Sudden dramatic BITE with "PLOP!" splash, '!' alert icon, and reaction window
    /// - Startle/flee behavior if player hooks early or runs nearby
    /// </summary>
    public class FishShadowEntity : MonoBehaviour
    {
        [Header("Fish Properties")]
        public FishShadowSize shadowSize = FishShadowSize.Medium;
        public float moveSpeed = 1.2f;
        public float turnSpeed = 4.0f;
        public Vector2 wanderRadius = new Vector2(10f, 10f);

        [Header("State")]
        [SerializeField] private FishAIState state = FishAIState.IdleWander;
        public FishAIState State => state;

        [Header("Visual Shadow Mesh")]
        [SerializeField] private Transform shadowMesh;
        [SerializeField] private GameObject alertExclamation;

        private Vector3 spawnOrigin;
        private Vector3 targetWanderPos;
        private float wanderTimer = 0f;
        private float swimWavePhase = 0f;

        // Fishing Interaction
        private FishingBobber targetBobber;
        private int remainingNibbles = 0;
        private Coroutine fishingInteractionCoroutine;

        public event Action<FishShadowEntity> OnBiteStarted;
        public event Action<FishShadowEntity> OnFishFled;
        public event Action<FishShadowEntity> OnFishCaught;

        private void Awake()
        {
            spawnOrigin = transform.position;
            BuildShadowMesh();
            PickNewWanderPoint();
        }

        public void BuildShadowMesh()
        {
            if (shadowMesh != null || transform.Find("ShadowVisual") != null) return;
            {
                var shadowGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shadowGO.name = "ShadowVisual";
                var col = shadowGO.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                shadowGO.transform.SetParent(transform, false);
                shadowMesh = shadowGO.transform;

                // Adjust shadow scale based on size
                float length = shadowSize switch
                {
                    FishShadowSize.Tiny => 0.5f,
                    FishShadowSize.Small => 0.8f,
                    FishShadowSize.Medium => 1.2f,
                    FishShadowSize.Large => 1.8f,
                    FishShadowSize.Huge => 2.8f,
                    FishShadowSize.Finned => 2.2f,
                    _ => 1.2f
                };
                shadowMesh.localScale = new Vector3(length * 0.28f, 0.05f, length);

                // Dark water silhouette material
                var mr = shadowGO.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                mat.SetColor("_BaseColor", new Color(0.04f, 0.08f, 0.12f, 0.85f));
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3050;
                mr.sharedMaterial = mat;

                // Add fin if finned
                if (shadowSize == FishShadowSize.Finned)
                {
                    var fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var fcol = fin.GetComponent<Collider>();
                    if (fcol != null) DestroyImmediate(fcol);
                    fin.transform.SetParent(shadowMesh, false);
                    fin.transform.localPosition = new Vector3(0, 0.6f, -0.2f);
                    fin.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
                    fin.transform.localScale = new Vector3(0.15f, 1.2f, 0.4f);
                    fin.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }

            // Exclamation '!' Alert Icon for bite
            if (alertExclamation == null)
            {
                alertExclamation = new GameObject("AlertExclamation");
                alertExclamation.transform.SetParent(transform, false);
                alertExclamation.transform.localPosition = new Vector3(0, 1.2f, 0);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(alertExclamation.transform, false);
                quad.transform.localScale = new Vector3(0.5f, 0.8f, 1f);

                var qmr = quad.GetComponent<MeshRenderer>();
                var qmat = new Material(Shader.Find("Sprites/Default"));
                qmat.color = new Color(1f, 0.15f, 0.15f, 1f);
                qmr.sharedMaterial = qmat;

                alertExclamation.SetActive(false);
            }
        }

        private void Update()
        {
            switch (state)
            {
                case FishAIState.IdleWander:
                    UpdateWander();
                    CheckForBobberDetection();
                    break;
                case FishAIState.Fleeing:
                    UpdateFlee();
                    break;
            }

            // Subtle body oscillation for swimming
            if (shadowMesh != null && (state == FishAIState.IdleWander || state == FishAIState.ApproachingBobber || state == FishAIState.Fleeing))
            {
                swimWavePhase += Time.deltaTime * (state == FishAIState.Fleeing ? 18f : 5f);
                float angle = Mathf.Sin(swimWavePhase) * (state == FishAIState.Fleeing ? 14f : 6f);
                shadowMesh.localRotation = Quaternion.Euler(0, angle, 0);
            }

            // Keep '!' billboard facing camera
            if (alertExclamation != null && alertExclamation.activeSelf && Camera.main != null)
            {
                alertExclamation.transform.forward = Camera.main.transform.forward;
            }
        }

        private void UpdateWander()
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f || Vector3.Distance(transform.position, targetWanderPos) < 0.6f)
            {
                PickNewWanderPoint();
            }

            Vector3 dir = (targetWanderPos - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
                transform.position += transform.forward * (moveSpeed * 0.65f * Time.deltaTime);

                // Moat canal radial clamp: keep within canal bed [304m, 341m]
                float curDist = new Vector2(transform.position.x, transform.position.z).magnitude;
                float originDist = new Vector2(spawnOrigin.x, spawnOrigin.z).magnitude;
                if (originDist >= 295f && originDist <= 350f)
                {
                    if (curDist < 304f || curDist > 341f)
                    {
                        Vector2 clamped = new Vector2(transform.position.x, transform.position.z).normalized * Mathf.Clamp(curDist, 305f, 340f);
                        transform.position = new Vector3(clamped.x, transform.position.y, clamped.y);
                        PickNewWanderPoint();
                    }
                }
            }
        }

        private void PickNewWanderPoint()
        {
            wanderTimer = UnityEngine.Random.Range(4.0f, 8.5f);
            float rx = UnityEngine.Random.Range(-wanderRadius.x, wanderRadius.x);
            float rz = UnityEngine.Random.Range(-wanderRadius.y, wanderRadius.y);
            targetWanderPos = spawnOrigin + new Vector3(rx, 0, rz);

            // Clamp moat target to canal radius
            float originDist = new Vector2(spawnOrigin.x, spawnOrigin.z).magnitude;
            if (originDist >= 295f && originDist <= 350f)
            {
                Vector2 planar = new Vector2(targetWanderPos.x, targetWanderPos.z);
                float dist = planar.magnitude;
                if (dist > 0.001f)
                {
                    planar = planar.normalized * Mathf.Clamp(dist, 306f, 339f);
                    targetWanderPos.x = planar.x;
                    targetWanderPos.z = planar.y;
                }
            }
        }

        private void CheckForBobberDetection()
        {
            if (FishingRodTool.ActiveBobber == null || !FishingRodTool.ActiveBobber.IsInWater) return;

            var bobber = FishingRodTool.ActiveBobber;
            Vector3 toBobber = bobber.transform.position - transform.position;
            toBobber.y = 0;
            float dist = toBobber.magnitude;

            // Scent/Sight cone: within 4 meters, within 75 degrees forward
            if (dist < 4.2f)
            {
                float angle = Vector3.Angle(transform.forward, toBobber);
                if (angle < 75f)
                {
                    StartFishingInteraction(bobber);
                }
            }
        }

        public void StartFishingInteraction(FishingBobber bobber)
        {
            targetBobber = bobber;
            targetBobber.AssignedFish = this;
            state = FishAIState.NoticingBobber;
            if (fishingInteractionCoroutine != null) StopCoroutine(fishingInteractionCoroutine);
            fishingInteractionCoroutine = StartCoroutine(FishingInteractionRoutine());
        }

        private IEnumerator FishingInteractionRoutine()
        {
            // 1. Notice Bobber: Pause and turn towards it
            state = FishAIState.NoticingBobber;
            float turnElapsed = 0f;
            Vector3 toBobber = (targetBobber.transform.position - transform.position);
            toBobber.y = 0;
            Quaternion targetRot = Quaternion.LookRotation(toBobber);

            while (turnElapsed < 0.8f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
                turnElapsed += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            // 2. Approach Bobber
            state = FishAIState.ApproachingBobber;
            while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                   new Vector3(targetBobber.transform.position.x, 0, targetBobber.transform.position.z)) > 0.45f)
            {
                Vector3 moveDir = (targetBobber.transform.position - transform.position);
                moveDir.y = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 5f);
                transform.position += transform.forward * (moveSpeed * 0.8f * Time.deltaTime);
                yield return null;
            }

            // 3. Animal Crossing Nibble Cycle: 1 to 4 nibbles
            remainingNibbles = UnityEngine.Random.Range(1, 4);

            for (int i = 0; i < remainingNibbles; i++)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.8f));
                if (state != FishAIState.ApproachingBobber && state != FishAIState.Nibbling) yield break;

                state = FishAIState.Nibbling;
                // Dart forward slightly to tap the bobber
                Vector3 preNibblePos = transform.position;
                transform.position += transform.forward * 0.18f;
                targetBobber.TriggerNibbleRipple();

                yield return new WaitForSeconds(0.12f);
                // Back off slightly
                transform.position = preNibblePos;
                yield return new WaitForSeconds(0.2f);
            }

            // 4. DRAMATIC BITE!
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.9f, 2.0f));
            if (state != FishAIState.Nibbling && state != FishAIState.ApproachingBobber) yield break;

            state = FishAIState.Biting;
            if (alertExclamation != null) alertExclamation.SetActive(true);

            // Plunge bobber underwater with splash
            targetBobber.TriggerBiteSplash();
            OnBiteStarted?.Invoke(this);

            // Reaction window: 0.85s
            float reactionTimer = 0.85f;
            while (reactionTimer > 0f)
            {
                reactionTimer -= Time.deltaTime;
                if (state == FishAIState.Caught) yield break; // Player reeled in!
                yield return null;
            }

            // If time expires without reeling: Fish flees!
            if (state == FishAIState.Biting)
            {
                StartleAndFlee();
            }
        }

        public void HookFish()
        {
            if (state == FishAIState.Biting)
            {
                state = FishAIState.Caught;
                if (alertExclamation != null) alertExclamation.SetActive(false);
                if (fishingInteractionCoroutine != null) StopCoroutine(fishingInteractionCoroutine);
                OnFishCaught?.Invoke(this);
            }
            else if (state == FishAIState.Nibbling || state == FishAIState.ApproachingBobber)
            {
                // Hooked too early!
                StartleAndFlee();
            }
        }

        public void StartleAndFlee()
        {
            if (state == FishAIState.Fleeing || state == FishAIState.Caught) return;

            state = FishAIState.Fleeing;
            if (alertExclamation != null) alertExclamation.SetActive(false);
            if (fishingInteractionCoroutine != null) StopCoroutine(fishingInteractionCoroutine);

            if (targetBobber != null)
            {
                targetBobber.TriggerFleeRipple();
                targetBobber.AssignedFish = null;
                targetBobber = null;
            }

            OnFishFled?.Invoke(this);

            // Turn opposite and zoom away, then despawn
            Vector3 fleeDir = -transform.forward + (UnityEngine.Random.insideUnitSphere * 0.4f);
            fleeDir.y = 0;
            transform.rotation = Quaternion.LookRotation(fleeDir.normalized);
            StartCoroutine(FleeAndDespawnRoutine());
        }

        private IEnumerator FleeAndDespawnRoutine()
        {
            float elapsed = 0f;
            while (elapsed < 1.4f)
            {
                transform.position += transform.forward * (moveSpeed * 3.5f * Time.deltaTime);
                if (shadowMesh != null)
                {
                    shadowMesh.localScale = Vector3.Lerp(shadowMesh.localScale, Vector3.zero, Time.deltaTime * 2.5f);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        private void UpdateFlee()
        {
            transform.position += transform.forward * (moveSpeed * 3.5f * Time.deltaTime);
        }
    }
}
