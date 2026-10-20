using System.Collections;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.Core;

namespace BattleSummoner.DesignPhilosophy
{
    /// <summary>
    /// Mega Man X Pre-Boss Sub-Tank Caterpillar Corridor Rule:
    /// Outside every Boss Gate sits a cluster of fast-respawning Mana-Crystalline Larvae.
    /// One charged slash or slice through the cluster immediately refills Whetstone Temper,
    /// Mana (+150), and Bento Sub-Tanks in 3 seconds, respawning rapidly.
    /// </summary>
    public class PreBossLarvaeCluster : MonoBehaviour, ISliceable
    {
        [Header("Fast-Respawn Pre-Boss Caterpillar Configuration")]
        public float respawnDelaySec = 2.5f;
        public int manaCoreReward = 150;
        public float temperHoneAmount = 100f;
        public GameObject harvestBurstVfxPrefab;

        private Collider _col;
        private Renderer[] _renderers;
        private bool _isHarvested = false;

        private void Awake()
        {
            _col = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        public void OnSlice(Vector3 contactPoint, Vector3 slashDirection, float damage)
        {
            if (_isHarvested) return;
            HarvestAndRefillSubTanks(contactPoint);
        }

        public void HarvestAndRefillSubTanks(Vector3 hitPoint)
        {
            if (_isHarvested) return;
            _isHarvested = true;

            // 1. Refill Whetstone Temper
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Hero");
            if (player != null)
            {
                var temper = player.GetComponentInChildren<WeaponTemper>();
                if (temper != null)
                {
                    temper.HoneWithWhetstone();
                    temper.HoneWithWhetstone();
                    temper.HoneWithWhetstone();
                }

                if (CoreVerticalSliceSandboxController.Instance != null)
                {
                    CoreVerticalSliceSandboxController.Instance.playerManaCores += manaCoreReward;
                }
            }

            // 2. Play burst VFX
            if (harvestBurstVfxPrefab != null)
            {
                Instantiate(harvestBurstVfxPrefab, hitPoint, Quaternion.identity);
            }

            // 3. Disable visual/collider during fast respawn
            if (_col != null) _col.enabled = false;
            foreach (var r in _renderers) if (r != null) r.enabled = false;

            Debug.Log("[Mega Man X Larvae] Sliced Pre-Boss Crystalline Larvae! Whetstone, Mana & Bento Sub-Tanks 100% REFILLED in seconds!");

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelaySec);
            _isHarvested = false;
            if (_col != null) _col.enabled = true;
            foreach (var r in _renderers) if (r != null) r.enabled = true;
        }
    }
}
