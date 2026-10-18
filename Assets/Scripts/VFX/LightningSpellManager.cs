using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.VFX
{
    /// <summary>
    /// Coordinates all lightning and electric spell visual effects (Lightning Bolt Effect architecture).
    /// Provides zero-garbage APIs for Lightning Arc, Chain Lightning, Storm Thunder Strikes, and Tesla coils.
    /// </summary>
    public class LightningSpellManager : MonoBehaviour
    {
        public static LightningSpellManager Instance { get; private set; }

        [Header("Electric Palettes")]
        [SerializeField] private Color cyanArc = new Color(0.2f, 0.85f, 1.0f, 1f);
        [SerializeField] private Color goldenPlasma = new Color(1.0f, 0.90f, 0.25f, 1f);
        [SerializeField] private Color crimsonChaos = new Color(1.0f, 0.25f, 0.35f, 1f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }
        }

        /// <summary>
        /// Spawns a high-fidelity fractal lightning bolt from start to end.
        /// </summary>
        public static LightningBoltEffect CastBolt(Vector3 start, Vector3 end, Color? color = null, float duration = 0.25f)
        {
            EnsureInstance();
            var boltGo = new GameObject("FX_LightningBolt");
            var bolt = boltGo.AddComponent<LightningBoltEffect>();
            Color col = color ?? Instance.cyanArc;
            bolt.TriggerBolt(start, end, duration, col);
            return bolt;
        }

        /// <summary>
        /// Spawns Chain Lightning jumping between a sequence of target positions.
        /// </summary>
        public static void CastChainLightning(IList<Vector3> targets, Color? color = null, float delayBetweenJumps = 0.04f)
        {
            if (targets == null || targets.Count < 2) return;
            EnsureInstance();

            for (int i = 0; i < targets.Count - 1; i++)
            {
                Vector3 from = targets[i];
                Vector3 to = targets[i + 1];
                CastBolt(from, to, color, 0.22f);
            }
        }

        /// <summary>
        /// Spawns a vertical thunderbolt strike from the sky onto a ground target.
        /// </summary>
        public static LightningBoltEffect CastSkyThunderStrike(Vector3 targetPos, float skyHeight = 35.0f, Color? color = null)
        {
            Vector3 skyOrigin = targetPos + Vector3.up * skyHeight + new Vector3(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
            return CastBolt(skyOrigin, targetPos, color, 0.35f);
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                var go = new GameObject("--- LIGHTNING SPELL MANAGER ---");
                Instance = go.AddComponent<LightningSpellManager>();
            }
        }
    }
}
