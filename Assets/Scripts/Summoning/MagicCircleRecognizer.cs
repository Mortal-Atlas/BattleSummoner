using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MagicCircleRecognizer evaluates unistroke circular touch trajectories to compute geometric accuracy.
/// </summary>
public static class MagicCircleRecognizer
{
    public static float EvaluateCircleAccuracy(List<Vector2> points, out Vector2 centroid, out float avgRadius)
    {
        centroid = Vector2.zero;
        avgRadius = 0f;
        if (points == null || points.Count < 8) return 0f;

        foreach (var p in points) centroid += p;
        centroid /= points.Count;

        float radiusSum = 0f;
        foreach (var p in points) radiusSum += Vector2.Distance(p, centroid);
        avgRadius = radiusSum / points.Count;

        if (avgRadius < 25f) return 0f;

        float deviationSum = 0f;
        foreach (var p in points)
        {
            float dist = Vector2.Distance(p, centroid);
            deviationSum += Mathf.Abs(dist - avgRadius);
        }

        float avgDeviation = deviationSum / points.Count;
        float accuracy = Mathf.Clamp01(1f - (avgDeviation / (avgRadius * 0.45f)));

        // Check closed loop
        float closureDistance = Vector2.Distance(points[0], points[points.Count - 1]);
        if (closureDistance > avgRadius * 0.8f) accuracy *= 0.5f;

        return accuracy;
    }
}
