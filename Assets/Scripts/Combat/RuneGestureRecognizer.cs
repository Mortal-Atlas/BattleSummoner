using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    public enum RecognizedRuneShape
    {
        None,
        CartoonHeart,    // Healing Spell
        VWithDash,       // Fire Spell (V shape with horizontal crossbar)
        ContinuousLoop   // Continuous circuit while locked in healing
    }

    public struct RuneRecognitionResult
    {
        public RecognizedRuneShape shape;
        public SpellElement element;
        public float accuracy;       // 0.0 to 1.0 (or percentage 0 to 100)
        public float sizeRadius;      // Effective pixel radius
        public Vector2 centroid;
        public string displayName;
    }

    /// <summary>
    /// RuneGestureRecognizer analyzes 2D touch strokes for distinct spellcraft shapes:
    /// 1. Cartoon Heart (♥) -> Healing Spell.
    ///    Features top central cleft, two upper rounded lobes, and tapering bottom apex.
    /// 2. 'V' with a Dash Through It (∀ / chevron with crossbar) -> Fire Spell.
    ///    Features two angled arms meeting at a sharp acute bottom apex, cut by a horizontal dash.
    /// 3. Continuous Heart / Circuit Loop Tracking:
    ///    Evaluates closed circuits in real-time during locked healing mode.
    /// </summary>
    public static class RuneGestureRecognizer
    {
        /// <summary>
        /// Analyzes a set of drawn points to detect if it matches a Cartoon Heart (Healing)
        /// or a 'V' with Dash (Fire).
        /// </summary>
        public static RuneRecognitionResult AnalyzeShape(List<Vector2> points, List<List<Vector2>> allStrokes = null)
        {
            var result = new RuneRecognitionResult
            {
                shape = RecognizedRuneShape.None,
                element = SpellElement.Fire,
                accuracy = 0f,
                sizeRadius = 0f,
                centroid = Vector2.zero,
                displayName = "Unknown"
            };

            if (points == null || points.Count < 6)
            {
                return result;
            }

            // Calculate bounding box and centroid
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            Vector2 sum = Vector2.zero;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
                sum += p;
            }

            float width = maxX - minX;
            float height = maxY - minY;
            result.centroid = sum / points.Count;
            result.sizeRadius = (width + height) * 0.25f;

            if (width < 35f || height < 35f)
            {
                return result;
            }

            // 1. Check for 'V' with a Dash through it (Fire Spell)
            if (CheckVWithDash(points, allStrokes, minX, maxX, minY, maxY, out float vAccuracy))
            {
                result.shape = RecognizedRuneShape.VWithDash;
                result.element = SpellElement.Fire;
                result.accuracy = vAccuracy;
                result.displayName = "Flame Slinger (V-Dash)";
                return result;
            }

            // 2. Check for Cartoon Heart (Healing Spell)
            if (CheckCartoonHeart(points, minX, maxX, minY, maxY, result.centroid, out float heartAccuracy))
            {
                result.shape = RecognizedRuneShape.CartoonHeart;
                result.element = SpellElement.Healing;
                result.accuracy = heartAccuracy;
                result.displayName = "Healing Harmony (Heart)";
                return result;
            }

            return result;
        }

        #region Cartoon Heart Recognition
        /// <summary>
        /// A Cartoon Heart shape has:
        /// - Two upper lobes: left peak (x < midX) and right peak (x > midX).
        /// - A central cleft/dip at the top center between the two lobes.
        /// - Both sides taper down to a sharp bottom tip near the center line.
        /// - Start and end are close or close near top cleft / bottom tip.
        /// </summary>
        public static bool CheckCartoonHeart(
            List<Vector2> points,
            float minX, float maxX, float minY, float maxY,
            Vector2 centroid,
            out float accuracy)
        {
            accuracy = 0f;
            float width = maxX - minX;
            float height = maxY - minY;
            float midX = (minX + maxX) * 0.5f;
            float midY = (minY + maxY) * 0.5f;

            // Aspect ratio check: hearts are roughly square or slightly taller than wide
            float aspect = width / height;
            if (aspect < 0.55f || aspect > 1.65f) return false;

            // Divide points into left and right relative to midX
            float maxLeftY = float.MinValue;
            float maxRightY = float.MinValue;
            float minCenterY = float.MaxValue;
            Vector2 lowestPoint = points[0];

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.y < lowestPoint.y) lowestPoint = p;

                // Left lobe peak
                if (p.x < midX - width * 0.12f)
                {
                    if (p.y > maxLeftY) maxLeftY = p.y;
                }
                // Right lobe peak
                else if (p.x > midX + width * 0.12f)
                {
                    if (p.y > maxRightY) maxRightY = p.y;
                }
                // Central cleft region
                else
                {
                    if (p.y > midY && p.y < minCenterY) minCenterY = p.y;
                }
            }

            // Both left and right upper lobes must exist in the upper half
            bool hasLeftLobe = maxLeftY > midY + height * 0.15f;
            bool hasRightLobe = maxRightY > midY + height * 0.15f;
            if (!hasLeftLobe || !hasRightLobe) return false;

            // The central cleft must dip lower than the average lobe peaks
            float avgLobePeak = (maxLeftY + maxRightY) * 0.5f;
            bool hasTopCleft = minCenterY < avgLobePeak - height * 0.08f || minCenterY == float.MaxValue;

            // The bottom-most tip should be reasonably centered (within 35% of width from midX)
            bool hasBottomTipCentered = Mathf.Abs(lowestPoint.x - midX) < width * 0.35f;

            // Bottom points should be tapered (not a flat bottom or wide base)
            int bottomCount = 0;
            float bottomThreshold = minY + height * 0.18f;
            float bottomMinX = float.MaxValue, bottomMaxX = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].y < bottomThreshold)
                {
                    bottomCount++;
                    bottomMinX = Mathf.Min(bottomMinX, points[i].x);
                    bottomMaxX = Mathf.Max(bottomMaxX, points[i].x);
                }
            }

            bool hasTaperedTip = (bottomMaxX - bottomMinX) < width * 0.60f;

            if (hasLeftLobe && hasRightLobe && (hasTopCleft || hasTaperedTip) && hasBottomTipCentered)
            {
                // Score accuracy:
                float lobeSymmetry = 1f - Mathf.Abs(maxLeftY - maxRightY) / (height * 0.5f);
                float cleftDepthScore = hasTopCleft ? Mathf.Clamp01((avgLobePeak - minCenterY) / (height * 0.3f)) : 0.6f;
                float tipCentering = 1f - Mathf.Clamp01(Mathf.Abs(lowestPoint.x - midX) / (width * 0.35f));

                accuracy = Mathf.Clamp01(0.35f * lobeSymmetry + 0.35f * cleftDepthScore + 0.30f * tipCentering);
                return accuracy >= 0.45f;
            }

            return false;
        }
        #endregion

        #region 'V' with Dash (Fire Spell) Recognition
        /// <summary>
        /// Recognizes a 'V' shape cut by a horizontal dash.
        /// Handles:
        /// 1. Multi-stroke: Stroke 1 is 'V' (down-right to vertex, up-right to peak), Stroke 2 is horizontal dash '-'.
        /// 2. Single continuous stroke: Draws 'V', then loops across to cross it.
        /// </summary>
        public static bool CheckVWithDash(
            List<Vector2> points,
            List<List<Vector2>> allStrokes,
            float minX, float maxX, float minY, float maxY,
            out float accuracy)
        {
            accuracy = 0f;
            float width = maxX - minX;
            float height = maxY - minY;
            float midX = (minX + maxX) * 0.5f;
            float midY = (minY + maxY) * 0.5f;

            // Case A: Multi-stroke gesture (Stroke 1 = V, Stroke 2 = Dash)
            if (allStrokes != null && allStrokes.Count >= 2)
            {
                var stroke1 = allStrokes[0];
                var stroke2 = allStrokes[1];

                bool s1IsV = IsVeeShape(stroke1, out float vScore);
                bool s2IsDash = IsHorizontalDash(stroke2, minX, maxX, minY, maxY, out float dashScore);

                if (s1IsV && s2IsDash)
                {
                    accuracy = Mathf.Clamp01(0.6f * vScore + 0.4f * dashScore);
                    return accuracy >= 0.5f;
                }

                // Or reverse stroke order (dash first, then V)
                if (IsHorizontalDash(stroke1, minX, maxX, minY, maxY, out dashScore) && IsVeeShape(stroke2, out vScore))
                {
                    accuracy = Mathf.Clamp01(0.6f * vScore + 0.4f * dashScore);
                    return accuracy >= 0.5f;
                }
            }

            // Case B: Single continuous stroke
            // Stroke starts top left, goes down to bottom apex, goes up to top right,
            // then cuts back across the middle horizontally.
            if (points.Count >= 10)
            {
                // Find index of lowest point (apex of V)
                int apexIdx = 0;
                float lowestY = float.MaxValue;
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i].y < lowestY)
                    {
                        lowestY = points[i].y;
                        apexIdx = i;
                    }
                }

                // Apex should not be right at the start or end
                if (apexIdx > 2 && apexIdx < points.Count - 4)
                {
                    Vector2 start = points[0];
                    Vector2 apex = points[apexIdx];

                    // Segment 1 (start -> apex): general downward trend
                    bool leg1Down = start.y > apex.y + height * 0.4f && start.x < apex.x + width * 0.2f;

                    // After apex, goes up to peak
                    int rightPeakIdx = apexIdx;
                    float highestAfterApex = float.MinValue;
                    for (int i = apexIdx; i < points.Count; i++)
                    {
                        if (points[i].y > highestAfterApex)
                        {
                            highestAfterApex = points[i].y;
                            rightPeakIdx = i;
                        }
                    }

                    bool leg2Up = highestAfterApex > apex.y + height * 0.4f && points[rightPeakIdx].x > apex.x;

                    // After right peak, check if there is a horizontal cut across the middle
                    if (leg1Down && leg2Up && rightPeakIdx < points.Count - 2)
                    {
                        float cutMinX = float.MaxValue, cutMaxX = float.MinValue;
                        float cutMinY = float.MaxValue, cutMaxY = float.MinValue;
                        for (int i = rightPeakIdx; i < points.Count; i++)
                        {
                            var p = points[i];
                            if (p.x < cutMinX) cutMinX = p.x;
                            if (p.x > cutMaxX) cutMaxX = p.x;
                            if (p.y < cutMinY) cutMinY = p.y;
                            if (p.y > cutMaxY) cutMaxY = p.y;
                        }

                        float cutWidth = cutMaxX - cutMinX;
                        float cutHeight = cutMaxY - cutMinY;

                        // Horizontal crossbar check
                        if (cutWidth > width * 0.35f && cutHeight < height * 0.5f && (cutMinY + cutMaxY) * 0.5f > minY + height * 0.2f)
                        {
                            accuracy = 0.85f;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsVeeShape(List<Vector2> pts, out float score)
        {
            score = 0f;
            if (pts == null || pts.Count < 5) return false;

            int lowestIdx = 0;
            float lowestY = float.MaxValue;
            for (int i = 0; i < pts.Count; i++)
            {
                if (pts[i].y < lowestY)
                {
                    lowestY = pts[i].y;
                    lowestIdx = i;
                }
            }

            if (lowestIdx <= 1 || lowestIdx >= pts.Count - 2) return false;

            Vector2 pStart = pts[0];
            Vector2 pApex = pts[lowestIdx];
            Vector2 pEnd = pts[pts.Count - 1];

            // Starts high on left, dips to apex, rises high on right
            bool startHigh = pStart.y > pApex.y + 30f;
            bool endHigh = pEnd.y > pApex.y + 30f;
            bool leftToRight = pStart.x < pApex.x && pApex.x < pEnd.x;

            if (startHigh && endHigh && leftToRight)
            {
                // Measure angle between leg1 and leg2
                Vector2 leg1 = (pApex - pStart).normalized;
                Vector2 leg2 = (pEnd - pApex).normalized;
                float dot = Vector2.Dot(-leg1, leg2); // 1 = straight line, 0 = 90 deg, < 0 = acute V
                float acuteScore = Mathf.Clamp01(1f - (dot + 1f) * 0.5f);
                score = Mathf.Clamp01(0.7f + 0.3f * acuteScore);
                return true;
            }

            return false;
        }

        private static bool IsHorizontalDash(List<Vector2> pts, float bMinX, float bMaxX, float bMinY, float bMaxY, out float score)
        {
            score = 0f;
            if (pts == null || pts.Count < 2) return false;

            Vector2 pStart = pts[0];
            Vector2 pEnd = pts[pts.Count - 1];

            float dx = Mathf.Abs(pEnd.x - pStart.x);
            float dy = Mathf.Abs(pEnd.y - pStart.y);

            // Dash should be predominantly horizontal
            if (dx > dy * 1.5f && dx > 25f)
            {
                float midY = (pStart.y + pEnd.y) * 0.5f;
                // Dash should pass across the vertical middle of the V
                if (midY > bMinY && midY < bMaxY)
                {
                    score = Mathf.Clamp01(dx / Mathf.Max(1f, dy * 3f));
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Continuous Circuit / Loop Detection
        /// <summary>
        /// During locked healing mode, checks if the running stroke buffer has completed
        /// a closed loop/circuit. Returns true when a circuit is closed, outputting
        /// the circuit points, accuracy, and size for immediate healing tick dispatch.
        /// </summary>
        public static bool CheckClosedCircuit(
            List<Vector2> points,
            int minPointsForLoop,
            out List<Vector2> closedCircuit,
            out float accuracy,
            out float radius)
        {
            closedCircuit = null;
            accuracy = 0f;
            radius = 0f;

            if (points == null || points.Count < minPointsForLoop) return false;

            Vector2 head = points[points.Count - 1];

            // Look backwards for a point near the current head that isn't immediately previous
            for (int i = 0; i < points.Count - minPointsForLoop; i++)
            {
                float dist = Vector2.Distance(points[i], head);
                if (dist < 45f)
                {
                    // A loop is formed from index i to points.Count - 1!
                    int loopCount = points.Count - i;
                    closedCircuit = points.GetRange(i, loopCount);

                    // Evaluate size & accuracy
                    float minX = float.MaxValue, maxX = float.MinValue;
                    float minY = float.MaxValue, maxY = float.MinValue;
                    Vector2 sum = Vector2.zero;
                    for (int j = 0; j < closedCircuit.Count; j++)
                    {
                        var p = closedCircuit[j];
                        minX = Mathf.Min(minX, p.x);
                        maxX = Mathf.Max(maxX, p.x);
                        minY = Mathf.Min(minY, p.y);
                        maxY = Mathf.Max(maxY, p.y);
                        sum += p;
                    }

                    Vector2 centroid = sum / closedCircuit.Count;
                    radius = (maxX - minX + (maxY - minY)) * 0.25f;

                    // If it's a heart shape, give high heart accuracy
                    if (CheckCartoonHeart(closedCircuit, minX, maxX, minY, maxY, centroid, out float heartAcc))
                    {
                        accuracy = Mathf.Clamp(heartAcc * 1.35f, 0.75f, 1.0f); // Bonus for heart
                    }
                    else
                    {
                        // Fallback circularity evaluation: "the caster can continue to draw circles to keep on casting"
                        float avgRadDiff = 0f;
                        for (int j = 0; j < closedCircuit.Count; j++)
                        {
                            avgRadDiff += Mathf.Abs(Vector2.Distance(closedCircuit[j], centroid) - radius);
                        }
                        avgRadDiff /= closedCircuit.Count;
                        float circularity = Mathf.Clamp01(1f - (avgRadDiff / Mathf.Max(1f, radius)));
                        accuracy = Mathf.Clamp(circularity, 0.55f, 0.90f);
                    }

                    return radius >= 30f;
                }
            }

            return false;
        }
        #endregion
    }
}
