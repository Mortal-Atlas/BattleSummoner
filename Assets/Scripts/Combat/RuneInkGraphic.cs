using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSummoner.Combat
{
    /// <summary>
    /// RuneInkGraphic renders smooth glowing ink strokes on the UI Canvas:
    /// "Like drawing on a piece of paper. You see markings."
    /// Uses dynamic quad ribbons with rounded joints and glowing arcane vertex colors.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class RuneInkGraphic : MaskableGraphic
    {
        [Header("Ink Ribbon Styling")]
        [SerializeField] private float strokeWidth = 14f;
        [SerializeField] private Color inkCoreColor = new Color(1.0f, 0.92f, 0.65f, 0.95f);   // Radiant gold ink
        [SerializeField] private Color inkEdgeColor = new Color(0.25f, 0.85f, 1.0f, 0.35f);  // Arcane cyan aura
        [SerializeField] private bool useDualColor = true;

        [Header("Dual Blade Styling")]
        [SerializeField] private bool isDualBladeMode = false;
        [SerializeField] private Vector2 dualBladeOffset = new Vector2(-22f, 18f);
        [SerializeField] private Color secondaryCoreColor = new Color(1.0f, 0.95f, 0.70f, 0.98f);
        [SerializeField] private Color secondaryEdgeColor = new Color(1.0f, 0.65f, 0.15f, 0.60f); // Fiery Golden/Amber Blade

        private readonly List<List<Vector2>> strokes = new List<List<Vector2>>();
        private List<Vector2> activeStroke = null;
        private List<Vector2> secondaryActiveStroke = null;
        private float inkAlpha = 1.0f;

        // Ni No Kuni Cast Echo Animation Parameters
        private float echoScale = 1.0f;
        private float echoAlpha = 0f;
        private Color echoColor = Color.white;
        private Vector2 strokesCentroid = Vector2.zero;
        private Coroutine niNoKuniCoroutine = null;

        public float StrokeWidth
        {
            get => strokeWidth;
            set { strokeWidth = value; SetVerticesDirty(); }
        }

        public void SetInkCoreColor(Color color)
        {
            inkCoreColor = color;
            SetVerticesDirty();
        }

        public void SetInkEdgeColor(Color color)
        {
            inkEdgeColor = color;
            SetVerticesDirty();
        }

        public void SetGlobalAlpha(float alpha)
        {
            inkAlpha = Mathf.Clamp01(alpha);
            SetVerticesDirty();
        }

        public bool IsDualBladeMode
        {
            get => isDualBladeMode;
            set { isDualBladeMode = value; }
        }

        public void SetDualBladeMode(bool enable, Vector2? offset = null, Color? secCore = null, Color? secEdge = null)
        {
            isDualBladeMode = enable;
            if (offset.HasValue) dualBladeOffset = offset.Value;
            if (secCore.HasValue) secondaryCoreColor = secCore.Value;
            if (secEdge.HasValue) secondaryEdgeColor = secEdge.Value;
        }

        public void BeginStroke(Vector2 localPoint)
        {
            activeStroke = new List<Vector2> { localPoint };
            strokes.Add(activeStroke);
            if (isDualBladeMode)
            {
                secondaryActiveStroke = new List<Vector2> { localPoint + dualBladeOffset };
                strokes.Add(secondaryActiveStroke);
            }
            inkAlpha = 1.0f;
            SetVerticesDirty();
        }

        public void AddPoint(Vector2 localPoint)
        {
            if (activeStroke == null)
            {
                BeginStroke(localPoint);
                return;
            }

            if (activeStroke.Count == 0 || Vector2.Distance(activeStroke[activeStroke.Count - 1], localPoint) >= 3.5f)
            {
                activeStroke.Add(localPoint);
                if (isDualBladeMode && secondaryActiveStroke != null)
                {
                    secondaryActiveStroke.Add(localPoint + dualBladeOffset);
                }
                SetVerticesDirty();
            }
        }

        public void TrimActiveStroke(int maxPoints)
        {
            if (activeStroke != null && activeStroke.Count > maxPoints)
            {
                activeStroke.RemoveRange(0, activeStroke.Count - maxPoints);
                if (secondaryActiveStroke != null && secondaryActiveStroke.Count > maxPoints)
                {
                    secondaryActiveStroke.RemoveRange(0, secondaryActiveStroke.Count - maxPoints);
                }
                SetVerticesDirty();
            }
        }

        public void EndStroke()
        {
            activeStroke = null;
            secondaryActiveStroke = null;
            SetVerticesDirty();
        }

        public void ClearAll()
        {
            strokes.Clear();
            activeStroke = null;
            secondaryActiveStroke = null;
            inkAlpha = 1.0f;
            echoAlpha = 0f;
            echoScale = 1.0f;
            SetVerticesDirty();
        }

        public int StrokeCount => strokes.Count;
        public IReadOnlyList<List<Vector2>> AllStrokes => strokes;

        public List<Vector2> GetAllPointsFlattened()
        {
            var flat = new List<Vector2>();
            for (int i = 0; i < strokes.Count; i++)
            {
                flat.AddRange(strokes[i]);
            }
            return flat;
        }

        public Vector2 CalculateCentroid()
        {
            var flat = GetAllPointsFlattened();
            if (flat.Count == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < flat.Count; i++) sum += flat[i];
            return sum / flat.Count;
        }

        /// <summary>
        /// Ni No Kuni Spell Cast Animation (refer to 3:44-3:47):
        /// 1. Glowing surge: color intensifies to brilliant emissive white core.
        /// 2. Bolds: stroke width expands and thickens into bold arcane lines.
        /// 3. Echo effect: radiant ghost duplicate ripples outward like an arcane shockwave.
        /// 4. Disappears: smoothly dissolves into sparkles and clears.
        /// </summary>
        public void PlayNiNoKuniCastSequence(Color elementColor, System.Action onComplete)
        {
            if (niNoKuniCoroutine != null) StopCoroutine(niNoKuniCoroutine);
            niNoKuniCoroutine = StartCoroutine(RoutineNiNoKuniCast(elementColor, onComplete));
        }

        private System.Collections.IEnumerator RoutineNiNoKuniCast(Color elemColor, System.Action onComplete)
        {
            strokesCentroid = CalculateCentroid();
            float baseWidth = 14f;
            float boldWidth = 28f;

            // Audio confirmation
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("apex_celestial_roar", 0.95f);
            }

            // -------------------------------------------------------------
            // PHASE 1: GLOW & BOLD (0.16s)
            // -------------------------------------------------------------
            float elapsed = 0f;
            float durationBold = 0.16f;
            Color origCore = inkCoreColor;
            Color origEdge = inkEdgeColor;

            while (elapsed < durationBold)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationBold);
                // Bold expansion
                strokeWidth = Mathf.Lerp(baseWidth, boldWidth, Mathf.SmoothStep(0f, 1f, t));
                // Glowing flash
                inkCoreColor = Color.Lerp(origCore, Color.white, t);
                inkEdgeColor = Color.Lerp(origEdge, elemColor, t);
                inkAlpha = 1.0f;
                SetVerticesDirty();
                yield return null;
            }

            // -------------------------------------------------------------
            // PHASE 2: ECHO EFFECT (0.28s)
            // -------------------------------------------------------------
            elapsed = 0f;
            float durationEcho = 0.28f;

            while (elapsed < durationEcho)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationEcho);
                // Echo ripples outward
                echoScale = Mathf.Lerp(1.0f, 1.42f, Mathf.Sin(t * Mathf.PI * 0.5f));
                echoAlpha = Mathf.Lerp(0.85f, 0f, t * t);
                echoColor = new Color(elemColor.r, elemColor.g, elemColor.b, echoAlpha);

                // Main strokes hold bold
                strokeWidth = boldWidth;
                SetVerticesDirty();
                yield return null;
            }
            echoAlpha = 0f;

            // -------------------------------------------------------------
            // PHASE 3: DISSOLVE & DISAPPEAR (0.22s)
            // -------------------------------------------------------------
            elapsed = 0f;
            float durationDissolve = 0.22f;

            while (elapsed < durationDissolve)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationDissolve);
                inkAlpha = Mathf.Lerp(1.0f, 0f, t * t);
                strokeWidth = Mathf.Lerp(boldWidth, baseWidth * 0.5f, t);
                SetVerticesDirty();
                yield return null;
            }

            // Reset and complete
            ClearAll();
            strokeWidth = baseWidth;
            inkCoreColor = origCore;
            inkEdgeColor = origEdge;
            inkAlpha = 1.0f;
            niNoKuniCoroutine = null;
            SetVerticesDirty();

            onComplete?.Invoke();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (strokes == null || strokes.Count == 0) return;

            // 1. Draw Echo Copy if active (Ni No Kuni echo wave)
            if (echoAlpha > 0.01f)
            {
                Color eColor = echoColor;
                float echoWidth = strokeWidth * 0.75f;
                for (int s = 0; s < strokes.Count; s++)
                {
                    var stroke = strokes[s];
                    if (stroke.Count < 2) continue;

                    // Build echo points scaled from centroid
                    var echoStroke = new List<Vector2>(stroke.Count);
                    for (int p = 0; p < stroke.Count; p++)
                    {
                        Vector2 offset = stroke[p] - strokesCentroid;
                        echoStroke.Add(strokesCentroid + offset * echoScale);
                    }
                    DrawRibbon(vh, echoStroke, echoWidth, eColor, new Color(eColor.r, eColor.g, eColor.b, eColor.a * 0.3f));
                }

                // Arcane echo ring pulse
                AddCircularRing(vh, strokesCentroid, 110f * echoScale, 6f, eColor);
            }

            // 2. Draw Main Strokes
            if (inkAlpha > 0.001f)
            {
                Color coreC = inkCoreColor;
                coreC.a *= inkAlpha;

                Color edgeC = useDualColor ? inkEdgeColor : inkCoreColor;
                edgeC.a *= inkAlpha;

                for (int s = 0; s < strokes.Count; s++)
                {
                    Color cC = coreC;
                    Color eC = edgeC;
                    if (isDualBladeMode && s % 2 == 1)
                    {
                        cC = secondaryCoreColor;
                        cC.a *= inkAlpha;
                        eC = secondaryEdgeColor;
                        eC.a *= inkAlpha;
                    }
                    DrawRibbon(vh, strokes[s], strokeWidth, cC, eC);
                }
            }
        }

        private void DrawRibbon(VertexHelper vh, List<Vector2> stroke, float width, Color coreC, Color edgeC)
        {
            if (stroke == null || stroke.Count == 0) return;
            float halfWidth = width * 0.5f;

            if (stroke.Count == 1)
            {
                AddCircularCap(vh, stroke[0], halfWidth * 1.2f, coreC, edgeC);
                return;
            }

            for (int i = 0; i < stroke.Count - 1; i++)
            {
                Vector2 p0 = stroke[i];
                Vector2 p1 = stroke[i + 1];
                Vector2 dir = (p1 - p0).normalized;
                if (dir.sqrMagnitude < 0.001f) continue;

                Vector2 normal = new Vector2(-dir.y, dir.x);

                Vector2 v0_L = p0 - normal * halfWidth;
                Vector2 v0_C = p0;
                Vector2 v0_R = p0 + normal * halfWidth;

                Vector2 v1_L = p1 - normal * halfWidth;
                Vector2 v1_C = p1;
                Vector2 v1_R = p1 + normal * halfWidth;

                int startIndex = vh.currentVertCount;

                // Left quad
                vh.AddVert(v0_L, edgeC, new Vector2(0f, 0f));
                vh.AddVert(v0_C, coreC, new Vector2(0.5f, 0f));
                vh.AddVert(v1_C, coreC, new Vector2(0.5f, 1f));
                vh.AddVert(v1_L, edgeC, new Vector2(0f, 1f));

                vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
                vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);

                // Right quad
                int rStart = vh.currentVertCount;
                vh.AddVert(v0_C, coreC, new Vector2(0.5f, 0f));
                vh.AddVert(v0_R, edgeC, new Vector2(1f, 0f));
                vh.AddVert(v1_R, edgeC, new Vector2(1f, 1f));
                vh.AddVert(v1_C, coreC, new Vector2(0.5f, 1f));

                vh.AddTriangle(rStart + 0, rStart + 1, rStart + 2);
                vh.AddTriangle(rStart + 2, rStart + 3, rStart + 0);

                // Cap joints
                if (i == 0) AddCircularCap(vh, p0, halfWidth, coreC, edgeC);
                if (i == stroke.Count - 2) AddCircularCap(vh, p1, halfWidth, coreC, edgeC);
            }
        }

        private void AddCircularRing(VertexHelper vh, Vector2 center, float radius, float thickness, Color color)
        {
            int segments = 24;
            float rInner = radius - thickness * 0.5f;
            float rOuter = radius + thickness * 0.5f;
            int startIdx = vh.currentVertCount;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(center + dir * rInner, new Color(color.r, color.g, color.b, color.a * 0.2f), Vector2.zero);
                vh.AddVert(center + dir * rOuter, color, Vector2.one);
            }

            for (int i = 0; i < segments; i++)
            {
                int i0 = startIdx + i * 2;
                int o0 = startIdx + i * 2 + 1;
                int i1 = startIdx + (i + 1) * 2;
                int o1 = startIdx + (i + 1) * 2 + 1;

                vh.AddTriangle(i0, o0, o1);
                vh.AddTriangle(o1, i1, i0);
            }
        }

        private void AddCircularCap(VertexHelper vh, Vector2 center, float radius, Color coreC, Color edgeC)
        {
            int centerIdx = vh.currentVertCount;
            vh.AddVert(center, coreC, new Vector2(0.5f, 0.5f));

            int segments = 8;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(pos, edgeC, new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f));
            }

            for (int i = 1; i <= segments; i++)
            {
                vh.AddTriangle(centerIdx, centerIdx + i, centerIdx + i + 1);
            }
        }
    }
}
