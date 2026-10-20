#if VISTA
using System;
using UnityEngine;

namespace Pinwheel.Vista
{
    /// <summary>
    /// Represents a centimetre-quantized rectangular world region on the XZ plane.
    /// One Unity world unit is converted to 100 stored coordinate units.
    /// </summary>
    [Serializable]
    public struct RectXZ : IEquatable<RectXZ>
    {
        public const double WORLD_UNIT_TO_CM = 100.0;

        [SerializeField]
        private long m_xMinCM;
        public long xMinCM => m_xMinCM;

        [SerializeField]
        private long m_zMinCM;
        public long zMinCM => m_zMinCM;

        [SerializeField]
        private long m_xSizeCM;
        public long xSizeCM => m_xSizeCM;

        [SerializeField]
        private long m_zSizeCM;
        public long zSizeCM => m_zSizeCM;

        public RectXZ(long xMinCM, long zMinCM, long xSizeCM, long zSizeCM)
        {
            if (xSizeCM <= 0)
                throw new ArgumentOutOfRangeException(nameof(xSizeCM));
            if (zSizeCM <= 0)
                throw new ArgumentOutOfRangeException(nameof(zSizeCM));

            m_xMinCM = xMinCM;
            m_zMinCM = zMinCM;
            m_xSizeCM = xSizeCM;
            m_zSizeCM = zSizeCM;
        }

        public static RectXZ FromBounds(Bounds bounds)
        {
            return new RectXZ(
                Quantize(bounds.min.x),
                Quantize(bounds.min.z),
                Quantize(bounds.size.x),
                Quantize(bounds.size.z));
        }

        private static long Quantize(float value)
        {
            return checked((long)Math.Round(
                value * WORLD_UNIT_TO_CM,
                MidpointRounding.AwayFromZero));
        }

        public bool Equals(RectXZ other)
        {
            return m_xMinCM == other.m_xMinCM &&
                m_zMinCM == other.m_zMinCM &&
                m_xSizeCM == other.m_xSizeCM &&
                m_zSizeCM == other.m_zSizeCM;
        }

        public override bool Equals(object obj)
        {
            return obj is RectXZ other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + m_xMinCM.GetHashCode();
                hash = hash * 31 + m_zMinCM.GetHashCode();
                hash = hash * 31 + m_xSizeCM.GetHashCode();
                hash = hash * 31 + m_zSizeCM.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({m_xMinCM}, {m_zMinCM}, {m_xSizeCM}, {m_zSizeCM})";
        }
    }
}
#endif
