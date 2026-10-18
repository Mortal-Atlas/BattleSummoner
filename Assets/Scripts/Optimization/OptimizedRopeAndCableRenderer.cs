using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Optimization
{
    /// <summary>
    /// Optimized Ropes & Cables Tool (Optimized Ropes and Cables Tool architecture).
    /// Generates high-efficiency catenary curve suspension cables, hanging lantern lines,
    /// and guild banner cords with procedural wind sway physics and zero PhysX joint overhead.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [ExecuteAlways]
    public class OptimizedRopeAndCableRenderer : MonoBehaviour
    {
        [Header("Anchor Points")]
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;

        [Header("Cable Curve Settings")]
        [Range(4, 32)]
        [SerializeField] private int segmentCount = 12;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float sagAmount = 0.65f;
        [SerializeField] private float cableWidth = 0.05f;

        [Header("Dynamic Wind Sway")]
        [SerializeField] private bool enableWindSway = true;
        [SerializeField] private float windSpeed = 1.8f;
        [SerializeField] private float windAmplitude = 0.06f;

        private LineRenderer lineRenderer;
        private Vector3[] positions;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        private void OnEnable()
        {
            positions = new Vector3[segmentCount + 1];
            UpdateCableGeometry();
        }

        private void Update()
        {
            if (enableWindSway || !Application.isPlaying)
            {
                UpdateCableGeometry();
            }
        }

        public void SetAnchors(Transform start, Transform end)
        {
            startPoint = start;
            endPoint = end;
            UpdateCableGeometry();
        }

        private void ConfigureLineRenderer()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = cableWidth;
            lineRenderer.endWidth = cableWidth;
            lineRenderer.positionCount = segmentCount + 1;
        }

        public void UpdateCableGeometry()
        {
            if (startPoint == null || endPoint == null) return;
            if (positions == null || positions.Length != (segmentCount + 1))
            {
                positions = new Vector3[segmentCount + 1];
                if (lineRenderer != null) lineRenderer.positionCount = segmentCount + 1;
            }

            Vector3 p0 = startPoint.position;
            Vector3 p1 = endPoint.position;

            float time = Time.time * windSpeed;

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                // Linear interpolation
                Vector3 point = Vector3.Lerp(p0, p1, t);

                // Catenary sag parabola: 4 * sag * t * (1 - t)
                float sag = 4.0f * sagAmount * t * (1.0f - t);
                point.y -= sag;

                // Procedural wind sway perpendicular to rope
                if (enableWindSway)
                {
                    Vector3 forward = (p1 - p0).normalized;
                    Vector3 side = Vector3.Cross(forward, Vector3.up).normalized;
                    float sway = Mathf.Sin(time + t * 3.14f) * windAmplitude * sag;
                    point += side * sway;
                }

                positions[i] = point;
            }

            if (lineRenderer != null)
            {
                lineRenderer.SetPositions(positions);
            }
        }
    }
}
