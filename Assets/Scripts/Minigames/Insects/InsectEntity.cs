using System;
using System.Collections;
using UnityEngine;

namespace BattleSummoner.Minigames.Insects
{
    public enum InsectState
    {
        Idle,
        Fluttering,
        StartledFleeing,
        Caught
    }

    /// <summary>
    /// InsectEntity represents a wild insect in the world:
    /// - Animated fluttering wings (butterflies) or tree-perching (beetles)
    /// - Stealth awareness AI: runs away if player sprints nearby, allowing slow stalking
    /// - Net collision detection for successful catches
    /// </summary>
    public class InsectEntity : MonoBehaviour
    {
        public string speciesId = "common_butterfly";
        public InsectType insectType = InsectType.Butterfly;
        public float wanderRadius = 6.0f;
        public float flySpeed = 1.6f;

        [Header("State")]
        [SerializeField] private InsectState state = InsectState.Fluttering;
        public InsectState State => state;

        [Header("Wing Animation Transforms")]
        [SerializeField] private Transform leftWing;
        [SerializeField] private Transform rightWing;
        [SerializeField] private Light fireflyGlow;

        private Vector3 spawnOrigin;
        private Vector3 targetWanderPos;
        private float wingPhase = 0f;
        private float wanderTimer = 0f;
        [System.NonSerialized] private InsectSpeciesData speciesData;

        public InsectSpeciesData Data => InsectDatabase.GetById(speciesId);

        private void Awake()
        {
            spawnOrigin = transform.position;
            speciesData = InsectDatabase.GetById(speciesId);
            BuildVisuals();
            PickNewWanderPoint();
        }

        public void BuildVisuals()
        {
            if (transform.Find("Body") != null) return;
            var data = Data;

            // Insect Body (Capsule)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            DestroyImmediate(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
            body.transform.localRotation = Quaternion.Euler(75f, 0, 0);

            var bmr = body.GetComponent<MeshRenderer>();
            var bmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            bmat.color = data.primaryColor;
            bmat.SetColor("_BaseColor", data.primaryColor);
            bmr.sharedMaterial = bmat;

            // Wings
            if (data.type == InsectType.Butterfly)
            {
                var wmat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                wmat.color = data.wingColor;
                wmat.SetColor("_BaseColor", data.wingColor);

                var lw = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(lw.GetComponent<Collider>());
                lw.name = "Wing_L";
                lw.transform.SetParent(transform, false);
                lw.transform.localPosition = new Vector3(-0.12f, 0.05f, 0);
                lw.transform.localScale = new Vector3(0.24f, 0.22f, 1f);
                lw.GetComponent<MeshRenderer>().sharedMaterial = wmat;
                leftWing = lw.transform;

                var rw = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(rw.GetComponent<Collider>());
                rw.name = "Wing_R";
                rw.transform.SetParent(transform, false);
                rw.transform.localPosition = new Vector3(0.12f, 0.05f, 0);
                rw.transform.localScale = new Vector3(0.24f, 0.22f, 1f);
                rw.GetComponent<MeshRenderer>().sharedMaterial = wmat;
                rightWing = rw.transform;
            }
            else if (data.type == InsectType.Firefly)
            {
                var lightGO = new GameObject("FireflyGlow");
                lightGO.transform.SetParent(transform, false);
                fireflyGlow = lightGO.AddComponent<Light>();
                fireflyGlow.type = LightType.Point;
                fireflyGlow.color = data.wingColor;
                fireflyGlow.range = 3.5f;
                fireflyGlow.intensity = 1.8f;
            }

            // Trigger Collider for Net detection
            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;
        }

        private void Update()
        {
            switch (state)
            {
                case InsectState.Fluttering:
                case InsectState.Idle:
                    UpdateFlutterWander();
                    CheckPlayerProximity();
                    break;
                case InsectState.StartledFleeing:
                    UpdateFlee();
                    break;
            }

            // Wing flutter animation
            if (leftWing != null && rightWing != null)
            {
                wingPhase += Time.deltaTime * (state == InsectState.StartledFleeing ? 35f : 16f);
                float flap = Mathf.Sin(wingPhase) * 45f;
                leftWing.localRotation = Quaternion.Euler(0, flap, 0);
                rightWing.localRotation = Quaternion.Euler(0, -flap, 0);
            }

            // Firefly glow pulse
            if (fireflyGlow != null)
            {
                fireflyGlow.intensity = 1.2f + Mathf.PingPong(Time.time * 2.5f, 1.0f);
            }
        }

        private void UpdateFlutterWander()
        {
            if (insectType == InsectType.Beetle) return; // Beetles stay perched on tree/ground

            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f || Vector3.Distance(transform.position, targetWanderPos) < 0.5f)
            {
                PickNewWanderPoint();
            }

            Vector3 dir = (targetWanderPos - transform.position);
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 3.5f);
                transform.position += transform.forward * (flySpeed * Time.deltaTime);
                // Sinusoidal bobbing in air
                transform.position += Vector3.up * (Mathf.Sin(Time.time * 4f) * 0.01f);
            }
        }

        private void PickNewWanderPoint()
        {
            wanderTimer = UnityEngine.Random.Range(3.5f, 7.0f);
            Vector3 offset = UnityEngine.Random.insideUnitSphere * wanderRadius;
            offset.y = Mathf.Abs(offset.y * 0.4f); // hover between 0.8m and 2.5m above ground
            targetWanderPos = spawnOrigin + offset;
            if (targetWanderPos.y < 0.6f) targetWanderPos.y = 0.8f;
        }

        private void CheckPlayerProximity()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero == null) return;

            float dist = Vector3.Distance(transform.position, hero.transform.position);
            if (dist < 4.8f)
            {
                // Check if player is running/sprinting or swinging recklessly
                var cc = hero.GetComponent<CharacterController>();
                float speed = cc != null ? cc.velocity.magnitude : 0f;

                // If player is running (speed > 3.0 m/s), startle the insect!
                if (speed > 3.2f)
                {
                    StartleAndFlee();
                }
            }
        }

        public void StartleAndFlee()
        {
            if (state == InsectState.StartledFleeing || state == InsectState.Caught) return;
            state = InsectState.StartledFleeing;
            StartCoroutine(FleeAndDespawnRoutine());
        }

        private IEnumerator FleeAndDespawnRoutine()
        {
            Vector3 fleeDir = (Vector3.up * 1.5f + UnityEngine.Random.insideUnitSphere).normalized;
            transform.rotation = Quaternion.LookRotation(fleeDir);

            float elapsed = 0f;
            while (elapsed < 2.0f)
            {
                transform.position += fleeDir * (flySpeed * 3.5f * Time.deltaTime);
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 2.0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        private void UpdateFlee()
        {
            transform.position += transform.forward * (flySpeed * 3.5f * Time.deltaTime);
        }

        public void Catch()
        {
            if (state == InsectState.Caught) return;
            state = InsectState.Caught;
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}
