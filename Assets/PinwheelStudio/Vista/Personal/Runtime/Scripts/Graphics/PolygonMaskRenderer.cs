#if VISTA
using UnityEngine;

namespace Pinwheel.Vista.Graphics
{
    /// <summary>
    /// Renders polygon masks with optional falloff into a render texture.
    /// </summary>
    public static class PolygonMaskRenderer
    {
        /// <summary>
        /// Parameters used to render a polygon mask.
        /// </summary>
        public class Configs
        {
            /// <summary>
            /// Base polygon vertices in normalized mask space.
            /// </summary>
            public Vector2[] vertices { get; set; }
            /// <summary>
            /// Outer or inner falloff polygon corresponding by index to <see cref="vertices"/>.
            /// </summary>
            public Vector2[] falloffVertices { get; set; }
            /// <summary>
            /// Optional falloff remap texture sampled by the falloff pass.
            /// </summary>
            public Texture falloffTexture { get; set; }
            /// <summary>
            /// Direction in which the falloff gradient should fade.
            /// </summary>
            public FalloffDirection falloffDirection { get; set; }
        }

        private static readonly string COMPUTE_SHADER_NAME = "Vista/Shaders/Graph/PolygonMask";
        private static readonly int MASK_TEX = Shader.PropertyToID("_MaskTex");
        private static readonly int MASK_TEX_0 = Shader.PropertyToID("_MaskTex0");
        private static readonly int MASK_TEX_1 = Shader.PropertyToID("_MaskTex1");
        private static readonly int MASK_TEX_2 = Shader.PropertyToID("_MaskTex2");
        private static readonly int MASK_RESOLUTION = Shader.PropertyToID("_MaskResolution");
        private static readonly int SEGMENTS = Shader.PropertyToID("_Segments");
        private static readonly int SEGMENT_COUNT = Shader.PropertyToID("_SegmentCount");
        private static readonly int TARGET_MASK = Shader.PropertyToID("_TargetMask");
        private const int KERNEL_INDEX = 0;
        private const int MRT_KERNEL_INDEX = 1;
        private const float MRT_FULL_INTENSITY_OVERLAP_PIXELS = 1f;

        private static ComputeShader s_computeShader;
        private static ComputeShader computeShader
        {
            get
            {
                if (s_computeShader == null)
                    s_computeShader = Resources.Load<ComputeShader>(COMPUTE_SHADER_NAME);
                return s_computeShader;
            }
        }

        private static readonly string FALLOFF_SHADER_NAME = "Hidden/Vista/Graph/PolygonMaskFalloff";
        private static readonly int FALLOFF_MAP = Shader.PropertyToID("_FalloffMap");
        private static readonly int HAS_FALLOFF_MAP = Shader.PropertyToID("_HasFalloffMap");
        private static readonly int PASS = 0;

        private static Material s_falloffMaterial;
        private static Material falloffMaterial
        {
            get
            {
                if (s_falloffMaterial == null)
                {
                    s_falloffMaterial = new Material(ShaderUtilities.Find(FALLOFF_SHADER_NAME));
                    s_falloffMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_falloffMaterial;
            }
        }

        /// <summary>
        /// Renders a polygon mask into a target render texture.
        /// </summary>
        /// <param name="targetRt">
        /// Render target that receives the base mask and optional falloff gradient.
        /// </param>
        /// <param name="configs">
        /// Polygon and falloff settings used for the render.
        /// </param>
        /// <remarks>
        /// The method first clears the target, fills the polygon interior through the compute-shader
        /// base pass, then optionally draws the falloff band with a material-based quad strip pass.
        /// </remarks>
        public static void Render(RenderTexture targetRt, Configs configs)
        {
            GraphicsUtils.ClearWithZeros(targetRt);
            DrawBaseMask(targetRt, configs);            
            DrawFalloffMask(targetRt, configs); // Hard-edged areas pass identical base and falloff vertices; DrawFalloffMask detects that and draws nothing.
        }

        /// <summary>
        /// Max-accumulates one polygon mask into any combination of three scalar render targets.
        /// The targets must already be created, have matching dimensions, and be cleared by the caller.
        /// </summary>
        public static void Accumulate(
            RenderTexture target0,
            RenderTexture target1,
            RenderTexture target2,
            bool writeTarget0,
            bool writeTarget1,
            bool writeTarget2,
            Configs configs)
        {
            ValidateMrtTargets(target0, target1, target2);
            if (!writeTarget0 && !writeTarget1 && !writeTarget2)
                return;

            Vector4 targetMask = new Vector4(
                writeTarget0 ? 1 : 0,
                writeTarget1 ? 1 : 0,
                writeTarget2 ? 1 : 0,
                0);
            DrawBaseMask(target0, target1, target2, targetMask, configs);
            DrawFalloffMask(target0, target1, target2, targetMask, configs);
        }

        private static void DrawBaseMask(RenderTexture targetRt, Configs configs)
        {
            Vector4[] segments = CreateBaseSegments(configs, false, Vector2.zero, out int segmentCount);

            computeShader.SetTexture(KERNEL_INDEX, MASK_TEX, targetRt);
            SetBaseMaskParameters(computeShader, targetRt, segments, segmentCount);
            Dispatch(computeShader, KERNEL_INDEX, targetRt);
        }

        private static void DrawBaseMask(
            RenderTexture target0,
            RenderTexture target1,
            RenderTexture target2,
            Vector4 targetMask,
            Configs configs)
        {
            Vector2 pixelScale = new Vector2(target0.width - 1, target0.height - 1);
            Vector4[] segments = CreateBaseSegments(configs, true, pixelScale, out int segmentCount);

            computeShader.SetTexture(MRT_KERNEL_INDEX, MASK_TEX_0, target0);
            computeShader.SetTexture(MRT_KERNEL_INDEX, MASK_TEX_1, target1);
            computeShader.SetTexture(MRT_KERNEL_INDEX, MASK_TEX_2, target2);
            computeShader.SetVector(TARGET_MASK, targetMask);
            SetBaseMaskParameters(computeShader, target0, segments, segmentCount);
            Dispatch(computeShader, MRT_KERNEL_INDEX, target0);
        }

        private static Vector4[] CreateBaseSegments(
            Configs configs,
            bool useMrtCoverageOverlap,
            Vector2 pixelScale,
            out int segmentCount)
        {
            const int MAX_VERTEX = 999;
            Vector2[] vertices = configs.vertices;
            Vector2[] falloffVertices = configs.falloffVertices;
            Vector4[] segments = new Vector4[vertices.Length + 1];
            for (int i = 0; i < vertices.Length; ++i)
            {
                if (useMrtCoverageOverlap)
                {
                    // The compute and raster passes use slightly different pixel coverage rules. Extend the
                    // full-intensity region one texel into the falloff band so no pixel can be missed by both.
                    // MRT max blending keeps this overlap at full intensity without lowering existing masks.
                    Vector2 fullIntensityVertex = configs.falloffDirection == FalloffDirection.Inner
                        ? falloffVertices[i]
                        : vertices[i];
                    Vector2 fadingVertex = configs.falloffDirection == FalloffDirection.Inner
                        ? vertices[i]
                        : falloffVertices[i];
                    Vector2 distanceInPixels = Vector2.Scale(
                        fadingVertex - fullIntensityVertex,
                        pixelScale);
                    float overlap = distanceInPixels.sqrMagnitude > 0
                        ? Mathf.Min(1, MRT_FULL_INTENSITY_OVERLAP_PIXELS / distanceInPixels.magnitude)
                        : 0;
                    segments[i] = Vector2.Lerp(fullIntensityVertex, fadingVertex, overlap);
                }
                else
                {
                    // Preserve the established single-target renderer exactly. Its falloff pass overwrites this
                    // midpoint fill rather than max-accumulating it.
                    segments[i] = (vertices[i] + falloffVertices[i]) * 0.5f;
                }
            }
            segments[segments.Length - 1] = segments[0];
            segmentCount = vertices.Length;

            if (vertices.Length > MAX_VERTEX)
            {
                Debug.LogWarning("VISTA: Too many vertices in a polygon mask may lead to incorrect result. Try reducing vertex count.");
            }

            return segments;
        }

        private static void SetBaseMaskParameters(
            ComputeShader computeShader,
            RenderTexture target,
            Vector4[] segments,
            int segmentCount)
        {
            computeShader.SetVector(MASK_RESOLUTION, new Vector4(target.width, target.height, 0, 0));
            computeShader.SetVectorArray(SEGMENTS, segments);
            computeShader.SetInt(SEGMENT_COUNT, segmentCount);
        }

        private static void Dispatch(ComputeShader computeShader, int kernel, RenderTexture target)
        {
            int threadX = (target.width + 7) / 8;
            int threadY = (target.height + 7) / 8;
            int threadZ = 1;
            computeShader.Dispatch(kernel, threadX, threadY, threadZ);
        }

        private static void DrawFalloffMask(RenderTexture targetRt, Configs configs)
        {
            if (IsFalloffDistanceZero(configs))
                return;

            Vector2[] vertices = configs.vertices;
            Vector2[] falloffVertices = configs.falloffVertices;
            Vector4[] segments = new Vector4[vertices.Length + 1];
            Vector4[] falloffSegments = new Vector4[vertices.Length + 1];
            for (int i = 0; i < vertices.Length; ++i)
            {
                segments[i] = vertices[i];
                falloffSegments[i] = falloffVertices[i];
            }
            segments[segments.Length - 1] = vertices[0];
            falloffSegments[falloffSegments.Length - 1] = falloffVertices[0];

            Material material = ConfigureFalloffMaterial(configs);

            RenderTexture previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = targetRt;
                DrawFalloffQuads(material, PASS, segments, falloffSegments, configs.falloffDirection);
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private static void DrawFalloffMask(
            RenderTexture target0,
            RenderTexture target1,
            RenderTexture target2,
            Vector4 targetMask,
            Configs configs)
        {
            if (IsFalloffDistanceZero(configs))
                return;

            CreateFalloffSegments(configs, out Vector4[] segments, out Vector4[] falloffSegments);
            Material material = ConfigureFalloffMaterial(configs);
            material.SetVector(TARGET_MASK, targetMask);

            RenderTexture previousActive = RenderTexture.active;
            RenderBuffer[] colorBuffers =
            {
                target0.colorBuffer,
                target1.colorBuffer,
                target2.colorBuffer
            };
            try
            {
                UnityEngine.Graphics.SetRenderTarget(colorBuffers, target0.depthBuffer);
                DrawFalloffQuads(material, 1, segments, falloffSegments, configs.falloffDirection);
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private static void CreateFalloffSegments(
            Configs configs,
            out Vector4[] segments,
            out Vector4[] falloffSegments)
        {
            Vector2[] vertices = configs.vertices;
            Vector2[] falloffVertices = configs.falloffVertices;
            segments = new Vector4[vertices.Length + 1];
            falloffSegments = new Vector4[vertices.Length + 1];
            for (int i = 0; i < vertices.Length; ++i)
            {
                segments[i] = vertices[i];
                falloffSegments[i] = falloffVertices[i];
            }
            segments[segments.Length - 1] = vertices[0];
            falloffSegments[falloffSegments.Length - 1] = falloffVertices[0];
        }

        private static Material ConfigureFalloffMaterial(Configs configs)
        {
            Material material = falloffMaterial;
            if (configs.falloffTexture != null)
            {
                material.SetTexture(FALLOFF_MAP, configs.falloffTexture);
                material.SetInteger(HAS_FALLOFF_MAP, 1);
            }
            else
            {
                material.SetTexture(FALLOFF_MAP, null);
                material.SetInteger(HAS_FALLOFF_MAP, 0);
            }
            return material;
        }

        private static void DrawFalloffQuads(
            Material material,
            int pass,
            Vector4[] segments,
            Vector4[] falloffSegments,
            FalloffDirection falloffDirection)
        {
            GL.PushMatrix();
            material.SetPass(pass);
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);

            for (int i = 0; i < segments.Length - 1; ++i)
            {
                GL.Color(falloffDirection == FalloffDirection.Outer ? Color.white : Color.black);
                GL.Vertex3(segments[i].x, segments[i].y, 0);
                GL.Color(falloffDirection == FalloffDirection.Outer ? Color.white : Color.black);
                GL.Vertex3(segments[i + 1].x, segments[i + 1].y, 0);
                GL.Color(falloffDirection == FalloffDirection.Outer ? Color.black : Color.white);
                GL.Vertex3(falloffSegments[i + 1].x, falloffSegments[i + 1].y, 0);
                GL.Color(falloffDirection == FalloffDirection.Outer ? Color.black : Color.white);
                GL.Vertex3(falloffSegments[i].x, falloffSegments[i].y, 0);
            }

            GL.End();
            GL.PopMatrix();
        }

        private static void ValidateMrtTargets(
            RenderTexture target0,
            RenderTexture target1,
            RenderTexture target2)
        {
            if (SystemInfo.supportedRenderTargetCount < 3)
                throw new System.NotSupportedException("Polygon mask MRT rendering requires three simultaneous render targets.");
            if (!SystemInfo.SupportsBlendingOnRenderTextureFormat(RenderTextureFormat.RFloat))
                throw new System.NotSupportedException("Polygon mask MRT rendering requires blending support for RFloat render textures.");

            RenderTexture[] targets = { target0, target1, target2 };
            for (int i = 0; i < targets.Length; ++i)
            {
                if (targets[i] == null || !targets[i].IsCreated())
                    throw new System.ArgumentException($"Polygon mask target {i} must be created before rendering.");
                if (targets[i].width != target0.width || targets[i].height != target0.height)
                    throw new System.ArgumentException("Polygon mask MRT targets must have matching dimensions.");
            }
        }

        private static bool IsFalloffDistanceZero(Configs configs)
        {
            Vector2[] vertices = configs.vertices;
            Vector2[] falloffVertices = configs.falloffVertices;

            for (int i = 0; i < vertices.Length; ++i)
            {
                if (vertices[i] != falloffVertices[i])
                    return false;
            }

            return true;
        }
    }
}
#endif


