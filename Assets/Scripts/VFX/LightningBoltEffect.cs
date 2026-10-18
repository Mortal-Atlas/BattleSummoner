using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.VFX
{
    /// <summary>
    /// Procedural Lightning Bolt Effect (Digital Ruby / 59471 architecture).
    /// Uses fractal midpoint displacement with secondary branching forks,
    /// dual-layer glow lines, high-frequency temporal jitter, and terminal spark bursts.
    /// Used for Lightning Arc, Lightning Ray, Chain Lightning, and Storm Weather strikes.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningBoltEffect : MonoBehaviour
    {
        [Header("Terminal Anchors")]
        public Transform startTransform;
        public Transform endTransform;
        public Vector3 startPosition;
        public Vector3 endPosition;
        public bool useTransformPositions = false;

        [Header("Fractal Bolt Configuration")]
        [Range(1, 8)]
        [SerializeField] private int generations = 5;
        [Range(0.01f, 1.0f)]
        [SerializeField] private float chaosFactor = 0.15f;
        [SerializeField] private float trunkWidth = 0.18f;
        [SerializeField] private float branchProbability = 0.55f;
        [SerializeField] private int maxBranches = 4;

        [Header("Temporal Pulse & Jitter")]
        [SerializeField] private float duration = 0.28f;
        [SerializeField] private float jitterInterval = 0.035f;
        [SerializeField] private Color coreColor = new Color(0.9f, 0.98f, 1.0f, 1.0f);
        [SerializeField] private Color glowColor = new Color(0.15f, 0.75f, 1.0f, 0.85f);

        [Header("Impact & Light")]
        [SerializeField] private bool spawnImpactFlash = true;
        [SerializeField] private float impactLightIntensity = 4.0f;

        private LineRenderer lineRenderer;
        private readonly List<Vector3> mainPoints = new List<Vector3>(64);
        private readonly List<LineRenderer> branchRenderers = new List<LineRenderer>(8);
        private float lifeTimer = 0f;
        private float jitterTimer = 0f;
        private Material boltMaterial;
        private Light impactLight;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, trunkWidth, coreColor);
            CreateImpactLight();
        }

        private void OnEnable()
        {
            lifeTimer = 0f;
            jitterTimer = 0f;
            GenerateBoltGeometry();
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= duration)
            {
                Destroy(gameObject);
                return;
            }

            jitterTimer += Time.deltaTime;
            if (jitterTimer >= jitterInterval)
            {
                jitterTimer = 0f;
                GenerateBoltGeometry();
            }

            // Fade intensity over life
            float alpha = 1.0f - (lifeTimer / duration);
            if (boltMaterial != null && boltMaterial.HasProperty("_BaseColor"))
            {
                var col = glowColor;
                col.a = alpha;
                boltMaterial.SetColor("_BaseColor", col);
            }
        }

        public void TriggerBolt(Vector3 start, Vector3 end, float customDuration = 0.28f, Color? customColor = null)
        {
            startPosition = start;
            endPosition = end;
            useTransformPositions = false;
            duration = customDuration;
            if (customColor.HasValue) glowColor = customColor.Value;

            if (impactLight != null)
            {
                impactLight.transform.position = end;
                impactLight.color = glowColor;
                impactLight.intensity = impactLightIntensity;
            }

            GenerateBoltGeometry();
        }

        private void ConfigureLineRenderer(LineRenderer lr, float width, Color col)
        {
            lr.useWorldSpace = true;
            lr.startWidth = width;
            lr.endWidth = width * 0.35f;

            if (boltMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                Shader.Find("Universal Render Pipeline/Lit") ??
                                Shader.Find("Standard");
                boltMaterial = new Material(shader);
                boltMaterial.SetColor("_BaseColor", glowColor);
                if (boltMaterial.HasProperty("_EmissionColor"))
                {
                    boltMaterial.EnableKeyword("_EMISSION");
                    boltMaterial.SetColor("_EmissionColor", glowColor * 4.0f);
                }
            }
            lr.sharedMaterial = boltMaterial;
        }

        private void CreateImpactLight()
        {
            if (!spawnImpactFlash) return;
            var lightObj = new GameObject("LightningImpactLight");
            lightObj.transform.SetParent(transform, false);
            impactLight = lightObj.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.range = 8.0f;
            impactLight.intensity = impactLightIntensity;
            impactLight.color = glowColor;
        }

        public void GenerateBoltGeometry()
        {
            Vector3 start = useTransformPositions && startTransform != null ? startTransform.position : startPosition;
            Vector3 end = useTransformPositions && endTransform != null ? endTransform.position : endPosition;

            mainPoints.Clear();
            mainPoints.Add(start);
            mainPoints.Add(end);

            float totalLength = Vector3.Distance(start, end);
            Vector3 direction = (end - start).normalized;

            // Multi-generational midpoint displacement
            for (int g = 0; g < generations; g++)
            {
                int count = mainPoints.Count;
                var nextPoints = new List<Vector3>(count * 2);
                float maxOffset = (totalLength / Mathf.Pow(2, g + 1)) * chaosFactor;

                for (int i = 0; i < count - 1; i++)
                {
                    Vector3 p0 = mainPoints[i];
                    Vector3 p1 = mainPoints[i + 1];
                    Vector3 mid = (p0 + p1) * 0.5f;

                    // Perpendicular displacement
                    Vector3 ortho = Vector3.Cross(direction, UnityEngine.Random.onUnitSphere).normalized;
                    mid += ortho * UnityEngine.Random.Range(-maxOffset, maxOffset);

                    nextPoints.Add(p0);
                    nextPoints.Add(mid);
                }
                nextPoints.Add(mainPoints[count - 1]);
                mainPoints.Clear();
                mainPoints.AddRange(nextPoints);
            }

            lineRenderer.positionCount = mainPoints.Count;
            lineRenderer.SetPositions(mainPoints.ToArray());

            GenerateSecondaryBranches(start, end, totalLength);
        }

        private void GenerateSecondaryBranches(Vector3 start, Vector3 end, float length)
        {
            int branchIdx = 0;
            int step = Mathf.Max(2, mainPoints.Count / (maxBranches + 1));

            for (int i = step; i < mainPoints.Count - 2 && branchIdx < maxBranches; i += step)
            {
                if (UnityEngine.Random.value < branchProbability)
                {
                    Vector3 branchOrigin = mainPoints[i];
                    Vector3 forward = (end - start).normalized;
                    Vector3 branchDir = Quaternion.Euler(UnityEngine.Random.Range(-35f, 35f), UnityEngine.Random.Range(-35f, 35f), 0) * forward;
                    float branchLen = length * UnityEngine.Random.Range(0.2f, 0.45f);
                    Vector3 branchEnd = branchOrigin + branchDir * branchLen;

                    LineRenderer branchLr = GetOrCreateBranchRenderer(branchIdx);
                    BuildSingleBranch(branchLr, branchOrigin, branchEnd, trunkWidth * 0.45f);
                    branchIdx++;
                }
            }

            // Disable unused branches
            for (int b = branchIdx; b < branchRenderers.Count; b++)
            {
                branchRenderers[b].gameObject.SetActive(false);
            }
        }

        private LineRenderer GetOrCreateBranchRenderer(int index)
        {
            if (index < branchRenderers.Count)
            {
                branchRenderers[index].gameObject.SetActive(true);
                return branchRenderers[index];
            }

            var child = new GameObject($"Branch_{index}");
            child.transform.SetParent(transform, false);
            var lr = child.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lr, trunkWidth * 0.45f, glowColor);
            branchRenderers.Add(lr);
            return lr;
        }

        private void BuildSingleBranch(LineRenderer lr, Vector3 from, Vector3 to, float width)
        {
            int segs = 6;
            var pts = new Vector3[segs + 1];
            float len = Vector3.Distance(from, to);

            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < segs)
                {
                    p += UnityEngine.Random.insideUnitSphere * (len * 0.12f);
                }
                pts[i] = p;
            }

            lr.positionCount = pts.Length;
            lr.SetPositions(pts);
            lr.startWidth = width;
            lr.endWidth = width * 0.2f;
        }
    }
}
