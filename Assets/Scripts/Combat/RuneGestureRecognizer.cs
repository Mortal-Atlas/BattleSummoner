using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    public enum RecognizedRuneShape
    {
        None,
        CartoonHeart,    // Healing Spell
        VWithDash,       // Fire Spell (Flame Slinger)
        LightningBolt,   // Lightning Stream (Continuous Ray with Mana Drain)
        WavyLine,        // Water Torrent (NamuFX Water Stream)
        Square,          // Rock Throw (Blunt Stagger)
        StraightLine,    // Arcane Piercing Bolt
        Circle,          // Astral Summoning Circle
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
        public string description;
    }

    /// <summary>
    /// RuneGestureRecognizer analyzes 2D touch strokes for distinct spellcraft shapes:
    /// 1. Cartoon Heart (♥) -> Healing Spell.
    /// 2. 'V' with a Dash (∀ / chevron) -> Fire Spell (Flame Slinger).
    /// 3. Lightning Bolt / Zigzag (⚡ / Z / N) -> Lightning Ray (Continuous stream, drains mana).
    /// 4. Wavy Line (〰️ / sine) -> Water Torrent (NamuFX Water Stream).
    /// 5. Square (⬛) -> Earth Boulder (Blunt rock).
    /// 6. Straight Slash (🗡️) -> Arcane Piercing Bolt.
    /// 7. Circle (⭕) -> Astral Summoning Circle.
    /// </summary>
    public static class RuneGestureRecognizer
    {
        /// <summary>
        /// Analyzes drawn points to detect which rune shape has been inscribed.
        /// </summary>
        public static RuneRecognitionResult AnalyzeShape(List<Vector2> points, List<List<Vector2>> allStrokes = null)
        {
            var result = new RuneRecognitionResult
            {
                shape = RecognizedRuneShape.None,
                element = SpellElement.Arcane,
                accuracy = 0f,
                sizeRadius = 0f,
                centroid = Vector2.zero,
                displayName = "Uninscribed Gesture",
                description = "Draw a recognizable rune on screen to weave magic."
            };

            if (points == null || points.Count < 5)
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

            if (width < 25f && height < 25f)
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
                result.description = "Charge fireball in hand (up to 1.50x DMG), then sling to launch!";
                return result;
            }

            // 2. Check for Cartoon Heart (Healing Spell)
            if (CheckCartoonHeart(points, minX, maxX, minY, maxY, result.centroid, out float heartAccuracy))
            {
                result.shape = RecognizedRuneShape.CartoonHeart;
                result.element = SpellElement.Healing;
                result.accuracy = heartAccuracy;
                result.displayName = "Healing Harmony (Heart)";
                result.description = "Select ally, then draw continuous circuits to channel healing!";
                return result;
            }

            // 3. Check for Lightning Bolt / Zigzag (Lightning Ray - Continuous Stream with Mana Drain)
            if (CheckLightningBolt(points, minX, maxX, minY, maxY, out float lightningAccuracy))
            {
                result.shape = RecognizedRuneShape.LightningBolt;
                result.element = SpellElement.Lightning;
                result.accuracy = lightningAccuracy;
                result.displayName = "Lightning Ray (Zigzag Stream)";
                result.description = "Channels a constant stream of high-voltage lightning! Drains mana over time.";
                return result;
            }

            // 4. Check for Wavy Line (Water Torrent - NamuFX Water Stream)
            if (CheckWavyLine(points, minX, maxX, minY, maxY, out float waveAccuracy))
            {
                result.shape = RecognizedRuneShape.WavyLine;
                result.element = SpellElement.Water;
                result.accuracy = waveAccuracy;
                result.displayName = "Water Torrent (Wave Surge)";
                result.description = "Unleashes a rushing torrent of pressurized water with explosive impact!";
                return result;
            }

            // 5. Check for Circle Rune (Astral Circle)
            if (CheckCircleRune(points, minX, maxX, minY, maxY, result.centroid, out float circleAccuracy))
            {
                result.shape = RecognizedRuneShape.Circle;
                result.element = SpellElement.Arcane;
                result.accuracy = circleAccuracy;
                result.displayName = "Astral Circle (Magic Sigil)";
                result.description = "Summons an ethereal arcane sigil that resonates with companion spirits.";
                return result;
            }

            // 6. Check for Square (Earth Boulder)
            if (CheckSquareRune(points, minX, maxX, minY, maxY, out float squareAccuracy))
            {
                result.shape = RecognizedRuneShape.Square;
                result.element = SpellElement.Rock;
                result.accuracy = squareAccuracy;
                result.displayName = "Earth Boulder (Square Sigil)";
                result.description = "Hurls a massive dense boulder dealing heavy blunt posture stagger!";
                return result;
            }

            // 7. Check for Straight Slash (Arcane Piercing Bolt)
            if (CheckStraightLine(points, minX, maxX, minY, maxY, out float slashAccuracy))
            {
                result.shape = RecognizedRuneShape.StraightLine;
                result.element = SpellElement.Arcane;
                result.accuracy = slashAccuracy;
                result.displayName = "Arcane Bolt (Piercing Slash)";
                result.description = "A rapid, piercing linear ether javelin piercing through enemies.";
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

        #region Lightning Bolt / Zigzag Recognition
        /// <summary>
        /// Detects a Lightning Bolt / Zigzag pattern ('Z', 'N', or multi-turn acute zigzags).
        /// Characterized by 2 to 4 sharp direction reversals and non-closed path.
        /// </summary>
        public static bool CheckLightningBolt(List<Vector2> pts, float minX, float maxX, float minY, float maxY, out float accuracy)
        {
            accuracy = 0f;
            if (pts == null || pts.Count < 8) return false;

            float width = maxX - minX;
            float height = maxY - minY;
            if (width < 35f && height < 35f) return false;

            // Start and end must NOT be close (not a loop)
            float startEndDist = Vector2.Distance(pts[0], pts[pts.Count - 1]);
            if (startEndDist < (width + height) * 0.25f) return false;

            // Subsample path every 12 pixels to detect macro directional turns
            List<Vector2> simplified = SimplifyPath(pts, 14f);
            if (simplified.Count < 4) return false;

            int sharpTurns = 0;
            float totalAngleChange = 0f;

            for (int i = 1; i < simplified.Count - 1; i++)
            {
                Vector2 dirIn = (simplified[i] - simplified[i - 1]).normalized;
                Vector2 dirOut = (simplified[i + 1] - simplified[i]).normalized;
                float dot = Vector2.Dot(dirIn, dirOut); // -1 = 180 deg reverse, 0 = 90 deg turn

                if (dot < 0.45f) // > 63 degree turn
                {
                    sharpTurns++;
                    totalAngleChange += (1f - dot);
                }
            }

            // A classic Z or lightning bolt has 2 to 4 sharp turns
            if (sharpTurns >= 2 && sharpTurns <= 5)
            {
                accuracy = Mathf.Clamp(0.65f + (sharpTurns * 0.08f), 0.70f, 0.95f);
                return true;
            }

            return false;
        }
        #endregion

        #region Wavy Line (Water Surge) Recognition
        /// <summary>
        /// Detects sinusoidal undulating waves (smooth alternating crests and troughs).
        /// </summary>
        public static bool CheckWavyLine(List<Vector2> pts, float minX, float maxX, float minY, float maxY, out float accuracy)
        {
            accuracy = 0f;
            if (pts == null || pts.Count < 10) return false;

            float width = maxX - minX;
            float height = maxY - minY;
            if (width < 40f || height < 15f) return false;

            // Start and end far apart
            float startEndDist = Vector2.Distance(pts[0], pts[pts.Count - 1]);
            if (startEndDist < width * 0.5f) return false;

            // Count smooth inflections along Y
            int yInflections = 0;
            float lastDy = 0f;

            for (int i = 2; i < pts.Count; i += 2)
            {
                float dy = pts[i].y - pts[i - 2].y;
                if (Mathf.Abs(dy) > 3f)
                {
                    if (lastDy != 0f && Mathf.Sign(dy) != Mathf.Sign(lastDy))
                    {
                        yInflections++;
                    }
                    lastDy = dy;
                }
            }

            if (yInflections >= 3)
            {
                accuracy = Mathf.Clamp(0.60f + (yInflections * 0.08f), 0.68f, 0.94f);
                return true;
            }

            return false;
        }
        #endregion

        #region Circle Rune Recognition
        /// <summary>
        /// Detects a closed or near-closed circular sigil.
        /// </summary>
        public static bool CheckCircleRune(List<Vector2> pts, float minX, float maxX, float minY, float maxY, Vector2 centroid, out float accuracy)
        {
            accuracy = 0f;
            if (pts == null || pts.Count < 12) return false;

            float width = maxX - minX;
            float height = maxY - minY;
            float avgRadius = (width + height) * 0.25f;
            if (avgRadius < 25f) return false;

            // Start and end must be near each other
            float closureDist = Vector2.Distance(pts[0], pts[pts.Count - 1]);
            if (closureDist > avgRadius * 0.85f) return false;

            // Circularity check
            float radialVariance = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                float d = Vector2.Distance(pts[i], centroid);
                radialVariance += Mathf.Abs(d - avgRadius);
            }
            radialVariance /= (pts.Count * avgRadius);

            if (radialVariance < 0.32f)
            {
                accuracy = Mathf.Clamp01(1.0f - radialVariance * 1.8f);
                return accuracy >= 0.55f;
            }

            return false;
        }
        #endregion

        #region Square / Box Recognition
        /// <summary>
        /// Detects a roughly rectangular/square block rune with 3-4 corners.
        /// </summary>
        public static bool CheckSquareRune(List<Vector2> pts, float minX, float maxX, float minY, float maxY, out float accuracy)
        {
            accuracy = 0f;
            if (pts == null || pts.Count < 10) return false;

            float width = maxX - minX;
            float height = maxY - minY;
            if (width < 30f || height < 30f) return false;

            float aspect = width / height;
            if (aspect < 0.70f || aspect > 1.45f) return false;

            List<Vector2> simplified = SimplifyPath(pts, 16f);
            int corners = 0;
            for (int i = 1; i < simplified.Count - 1; i++)
            {
                Vector2 v1 = (simplified[i] - simplified[i - 1]).normalized;
                Vector2 v2 = (simplified[i + 1] - simplified[i]).normalized;
                float dot = Vector2.Dot(v1, v2);
                if (Mathf.Abs(dot) < 0.45f) corners++; // ~90 degree turns
            }

            if (corners >= 3 && corners <= 5)
            {
                accuracy = 0.82f;
                return true;
            }

            return false;
        }
        #endregion

        #region Straight Line / Slash Recognition
        /// <summary>
        /// Detects a rapid straight slash stroke.
        /// </summary>
        public static bool CheckStraightLine(List<Vector2> pts, float minX, float maxX, float minY, float maxY, out float accuracy)
        {
            accuracy = 0f;
            if (pts == null || pts.Count < 4) return false;

            float directDist = Vector2.Distance(pts[0], pts[pts.Count - 1]);
            if (directDist < 40f) return false;

            float pathLen = 0f;
            for (int i = 1; i < pts.Count; i++)
            {
                pathLen += Vector2.Distance(pts[i], pts[i - 1]);
            }

            float linearity = directDist / Mathf.Max(0.01f, pathLen);
            if (linearity >= 0.85f)
            {
                accuracy = Mathf.Clamp01(linearity);
                return true;
            }

            return false;
        }

        private static List<Vector2> SimplifyPath(List<Vector2> pts, float minStep)
        {
            var result = new List<Vector2> { pts[0] };
            Vector2 last = pts[0];
            for (int i = 1; i < pts.Count; i++)
            {
                if (Vector2.Distance(pts[i], last) >= minStep)
                {
                    result.Add(pts[i]);
                    last = pts[i];
                }
            }
            if (result[result.Count - 1] != pts[pts.Count - 1])
            {
                result.Add(pts[pts.Count - 1]);
            }
            return result;
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
