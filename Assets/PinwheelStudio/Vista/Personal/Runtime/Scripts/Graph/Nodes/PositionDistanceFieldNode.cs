#if VISTA
using Pinwheel.Vista.Graphics;
using System.Collections;
using UnityEngine;

namespace Pinwheel.Vista.Graph
{
    [NodeMetadata(
        title = "Position SDF",
        path = "General/Position SDF",
        keywords = "sdf, signed distance, point distance, polygon distance, path distance",
        description = "Calculate a world-space distance field from valid positions. Point mode treats positions independently; Path mode connects valid positions in buffer order and skips invalid samples.")]
    public class PositionDistanceFieldNode : ImageNodeBase
    {
        public enum GeometryMode
        {
            Points,
            Path
        }

        public enum RangeMode
        {
            Auto,
            Manual
        }

        public readonly BufferSlot positionSlot = new BufferSlot("Positions", SlotDirection.Input, 0);
        public readonly MaskSlot normalizedOutputSlot = new MaskSlot("Normalized", SlotDirection.Output, 100);

        [SerializeField]
        private GeometryMode m_geometry;
        public GeometryMode geometry
        {
            get { return m_geometry; }
            set { m_geometry = value; }
        }

        [SerializeField]
        private bool m_closed;
        public bool closed
        {
            get { return m_closed; }
            set { m_closed = value; }
        }

        [SerializeField]
        private VistaLib.PositionDistanceFieldProfile m_profile;
        public VistaLib.PositionDistanceFieldProfile profile
        {
            get { return m_profile; }
            set { m_profile = value; }
        }

        [SerializeField]
        private float m_range;
        public float range
        {
            get { return m_range; }
            set { m_range = Mathf.Max(0.01f, value); }
        }

        [SerializeField]
        private RangeMode m_rangeMode;
        public RangeMode rangeMode
        {
            get { return m_rangeMode; }
            set { m_rangeMode = value; }
        }

        private const string TEMP_RAW_OUTPUT_NAME = "~PositionSdfRaw";

        public PositionDistanceFieldNode()
        {
            m_geometry = GeometryMode.Path;
            m_closed = true;
            m_profile = VistaLib.PositionDistanceFieldProfile.InsideOnly;
            m_range = 100;
            m_rangeMode = RangeMode.Auto;
        }

        public override IEnumerator Execute(GraphContext context)
        {
            ExecuteImmediate(context);
            yield return null;
        }

        public override void ExecuteImmediate(GraphContext context)
        {
            int baseResolution = context.GetArg(Args.RESOLUTION).intValue;
            int resolution = this.CalculateResolution(baseResolution, baseResolution);
            DataPool.RtDescriptor desc = DataPool.RtDescriptor.Create(resolution, resolution);
            SlotRef normalizedOutputRef = new SlotRef(m_id, normalizedOutputSlot.id);

            SlotRef inputRef = context.GetInputLink(m_id, positionSlot.id);
            ComputeBuffer positions = context.GetBuffer(inputRef);
            if (positions == null)
            {
                CreateEmptyOutput(context, desc, normalizedOutputRef);
                return;
            }

            if (positions.count % PositionSample.SIZE != 0)
            {
                Debug.LogWarning($"Cannot parse position buffer, node id {m_id}");
                CreateEmptyOutput(context, desc, normalizedOutputRef);
                context.ReleaseReference(inputRef);
                return;
            }

            RenderTexture normalizedTarget = context.CreateRenderTarget(desc, normalizedOutputRef);
            string tempRawName = $"{TEMP_RAW_OUTPUT_NAME}_{m_id}";
            RenderTexture rawTarget = context.CreateTemporaryRT(desc, tempRawName);

            Vector4 worldBounds = context.GetArg(Args.WORLD_BOUNDS).vectorValue;
            VistaLib.PositionDistanceFieldSettings settings = new VistaLib.PositionDistanceFieldSettings
            {
                worldSize = new Vector2(worldBounds.z, worldBounds.w),
                connectPositions = m_geometry == GeometryMode.Path,
                closed = m_closed,
                profile = m_profile,
                autoRange = m_rangeMode == RangeMode.Auto,
                range = m_range
            };
            VistaLib.GeneratePositionDistanceField(
                normalizedTarget,
                rawTarget,
                positions,
                settings);

            context.ReleaseTemporary(tempRawName);
            context.ReleaseReference(inputRef);
        }

        private static void CreateEmptyOutput(
            GraphContext context,
            DataPool.RtDescriptor desc,
            SlotRef normalizedOutputRef)
        {
            RenderTexture normalizedTarget = context.CreateRenderTarget(desc, normalizedOutputRef);
            GraphicsUtils.ClearWithZeros(normalizedTarget);
        }
    }
}
#endif
