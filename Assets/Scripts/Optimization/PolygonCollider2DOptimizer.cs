using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Optimization
{
    /// <summary>
    /// PolygonCollider2D Optimizer (PolygonCollider2D Optimizer architecture).
    /// Simplifies complex polygon colliders, minimap radar polygons, and 2D trigger boundaries
    /// using the Ramer-Douglas-Peucker algorithm to eliminate redundant vertices while preserving shape contours.
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public class PolygonCollider2DOptimizer : MonoBehaviour
    {
        [Header("Optimization Settings")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float tolerance = 0.08f;

        [ContextMenu("Optimize Polygon Points")]
        public void OptimizeCollider()
        {
            var poly = GetComponent<PolygonCollider2D>();
            if (poly == null) return;

            int originalPointCount = 0;
            int newPointCount = 0;

            for (int p = 0; p < poly.pathCount; p++)
            {
                var path = poly.GetPath(p);
                originalPointCount += path.Length;

                var simplified = SimplifyPath(path, tolerance);
                newPointCount += simplified.Count;

                poly.SetPath(p, simplified.ToArray());
            }

            Debug.Log($"📐 [POLYGON OPTIMIZER] Optimized {gameObject.name}: {originalPointCount} points reduced to {newPointCount} points (Tolerance: {tolerance}).");
        }

        private List<Vector2> SimplifyPath(Vector2[] points, float epsilon)
        {
            if (points == null || points.Length < 3) return new List<Vector2>(points);

            var pointList = new List<Vector2>(points);
            return DouglasPeucker(pointList, 0, pointList.Count - 1, epsilon);
        }

        private List<Vector2> DouglasPeucker(List<Vector2> points, int firstIndex, int lastIndex, float epsilon)
        {
            float maxDistance = 0f;
            int farthestIndex = 0;

            for (int i = firstIndex + 1; i < lastIndex; i++)
            {
                float distance = PerpendicularDistance(points[i], points[firstIndex], points[lastIndex]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthestIndex = i;
                }
            }

            var result = new List<Vector2>();

            if (maxDistance > epsilon)
            {
                var firstHalf = DouglasPeucker(points, firstIndex, farthestIndex, epsilon);
                var secondHalf = DouglasPeucker(points, farthestIndex, lastIndex, epsilon);

                result.AddRange(firstHalf);
                result.RemoveAt(result.Count - 1);
                result.AddRange(secondHalf);
            }
            else
            {
                result.Add(points[firstIndex]);
                result.Add(points[lastIndex]);
            }

            return result;
        }

        private float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            float lineLength = Vector2.Distance(lineStart, lineEnd);
            if (lineLength < 0.0001f) return Vector2.Distance(point, lineStart);

            float area = Mathf.Abs(0.5f * (lineStart.x * (lineEnd.y - point.y) + lineEnd.x * (point.y - lineStart.y) + point.x * (lineStart.y - lineEnd.y)));
            return (2f * area) / lineLength;
        }
    }
}
