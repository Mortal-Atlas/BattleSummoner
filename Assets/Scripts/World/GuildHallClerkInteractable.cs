using System;
using UnityEngine;
using BattleSummoner.Progression.ThreeMasks;

namespace BattleSummoner.World
{
    /// <summary>
    /// GuildHallClerkInteractable attaches to Catherine.glb stationed inside the Guild Hall.
    /// Rules of Mask 3: Classes can ONLY be changed inside the Guild Hall via Catherine.
    /// Unlocks Infinite Class Evolution at Lv.25+ ([Affinity] + [Base Class]).
    /// </summary>
    public class GuildHallClerkInteractable : MonoBehaviour
    {
        [Header("Clerk Identity")]
        public string clerkName = "Catherine";
        public string clerkRole = "Guild Hall Clerk";
        public float interactionRadius = 3.0f;

        private Transform _playerTransform;
        private bool _isPlayerNear = false;

        private void Start()
        {
            var inter = GetComponent<OverworldNPCInteractable>();
            if (inter == null)
            {
                inter = gameObject.AddComponent<OverworldNPCInteractable>();
                inter.npcName = clerkName;
                inter.npcRole = clerkRole;
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                var hero = GameObject.Find("Player_Hero") ?? GameObject.FindGameObjectWithTag("Player");
                if (hero != null) _playerTransform = hero.transform;
            }

            if (_playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                bool near = dist <= interactionRadius;
                if (near != _isPlayerNear)
                {
                    _isPlayerNear = near;
                    if (ThreeMasksAndWildcardTalentSystem.Instance != null)
                    {
                        ThreeMasksAndWildcardTalentSystem.Instance.isInsideGuildHall = _isPlayerNear;
                    }
                }
            }
        }

        /// <summary>
        /// Switches the active Guild Class (Tank, Warrior, Ranger, Mage, Assassin, Summoner).
        /// Restricted strictly to the Guild Hall.
        /// </summary>
        public bool RequestClassChange(BaseGuildClass newClass)
        {
            if (ThreeMasksAndWildcardTalentSystem.Instance == null) return false;

            ThreeMasksAndWildcardTalentSystem.Instance.isInsideGuildHall = true;
            ThreeMasksAndWildcardTalentSystem.Instance.activeGuildClass = newClass;
            Debug.Log($"<color=#38BDF8><b>[GUILD HALL] Catherine updated active class to: {newClass}</b></color>");
            return true;
        }

        /// <summary>
        /// Unlocks Infinite Class Evolution at Level 25+ ([Affinity] + [Base Class] = Blood Tank, Void Ranger, etc.)
        /// </summary>
        public bool RequestInfiniteClassEvolution(AffinityElement catalystAffinity, string customPrefix = "")
        {
            if (ThreeMasksAndWildcardTalentSystem.Instance == null) return false;

            ThreeMasksAndWildcardTalentSystem.Instance.isInsideGuildHall = true;
            bool success = ThreeMasksAndWildcardTalentSystem.Instance.TryEvolveInfiniteSubClassAtGuildHall(catalystAffinity, customPrefix);
            if (success)
            {
                Debug.Log($"<color=#A855F7><b>✦ [INFINITE CLASS EVOLUTION] Catherine consecrated evolution: {ThreeMasksAndWildcardTalentSystem.Instance.evolvedSubClassTitle}!</b></color>");
            }
            return success;
        }

        private void OnDisable()
        {
            if (ThreeMasksAndWildcardTalentSystem.Instance != null)
            {
                ThreeMasksAndWildcardTalentSystem.Instance.isInsideGuildHall = false;
            }
        }
    }
}
