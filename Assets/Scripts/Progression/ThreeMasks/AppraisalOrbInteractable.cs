using System;
using UnityEngine;
using TMPro;

namespace BattleSummoner.Progression.ThreeMasks
{
    /// <summary>
    /// AppraisalOrbInteractable represents the monumental awakening artifact in the
    /// Central Cathedral / Awakening Hub. Touching the orb assigns the player's Mask 1 Talent
    /// with 3 distinct pathways: [Accept Appraisal], [Choose Manually], or [Randomize Fate].
    /// </summary>
    public class AppraisalOrbInteractable : MonoBehaviour
    {
        [Header("Orb Visuals")]
        [SerializeField] private float glowPulseSpeed = 2.0f;
        [SerializeField] private Color orbAuraColor = new Color(0.2f, 0.85f, 1.0f, 1.0f);
        [SerializeField] private float interactionRadius = 3.5f;

        [Header("Appraisal State")]
        public bool isAppraisalCompleted = false;
        public DoubleEdgedTalentDefinition appraisedTalent;

        public event Action<DoubleEdgedTalentDefinition> OnTalentChosen;

        private Transform _playerTransform;
        private bool _isPlayerNear = false;
        private Light _pointLight;

        private void Awake()
        {
            _pointLight = GetComponentInChildren<Light>();
            if (_pointLight == null)
            {
                var lightObj = new GameObject("Orb_AuraLight");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = Vector3.zero;
                _pointLight = lightObj.AddComponent<Light>();
                _pointLight.type = LightType.Point;
                _pointLight.color = orbAuraColor;
                _pointLight.range = 7.0f;
                _pointLight.intensity = 2.5f;
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                var hero = GameObject.Find("Player_Hero") ?? GameObject.FindGameObjectWithTag("Player");
                if (hero != null) _playerTransform = hero.transform;
            }

            // Pulsing astral glow
            if (_pointLight != null)
            {
                float pulse = 1.8f + Mathf.Sin(Time.time * glowPulseSpeed) * 0.7f;
                _pointLight.intensity = pulse;
            }

            if (_playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                _isPlayerNear = dist <= interactionRadius;

                if (_isPlayerNear && Input.GetKeyDown(KeyCode.E))
                {
                    TriggerAppraisalDialog();
                }
            }
        }

        public void TriggerAppraisalDialog()
        {
            if (ThreeMasksAndWildcardTalentSystem.Instance == null) return;

            // Generate initial appraisal suggestion based on character profile
            appraisedTalent = ThreeMasksAndWildcardTalentSystem.Instance.AppraiseFromQuestionnaire("Traveler", "Mastery", "Metropolis");
            Debug.Log($"<color=#38BDF8><b>✦ [APPRAISAL ORB] Resonating with player soul... Current Appraisal: {appraisedTalent?.displayWord}</b></color>");
        }

        /// <summary>
        /// Pathway 1: Player accepts the divine appraisal generated from their awakening.
        /// </summary>
        public void ActionAcceptAppraisal()
        {
            if (appraisedTalent == null && ThreeMasksAndWildcardTalentSystem.Instance != null)
            {
                appraisedTalent = ThreeMasksAndWildcardTalentSystem.Instance.masterTalentRegistry.Find(t => t.wordId == TalentWordID.Chef);
            }

            if (ThreeMasksAndWildcardTalentSystem.Instance != null && appraisedTalent != null)
            {
                ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1 = appraisedTalent;
            }
            isAppraisalCompleted = true;
            OnTalentChosen?.Invoke(appraisedTalent);
            Debug.Log($"<color=#10B981><b>✓ [APPRAISAL ACCEPTED] Bound to Talent: {appraisedTalent?.displayWord}</b></color>");
        }

        /// <summary>
        /// Pathway 2: Player selects their desired talent manually from the master catalog.
        /// </summary>
        public void ActionChooseManually(TalentWordID chosenWordId)
        {
            if (ThreeMasksAndWildcardTalentSystem.Instance == null) return;

            var talent = ThreeMasksAndWildcardTalentSystem.Instance.masterTalentRegistry.Find(t => t.wordId == chosenWordId);
            if (talent != null)
            {
                ThreeMasksAndWildcardTalentSystem.Instance.innateTalentMask1 = talent;
                appraisedTalent = talent;
                isAppraisalCompleted = true;
                OnTalentChosen?.Invoke(talent);
                Debug.Log($"<color=#F59E0B><b>✓ [MANUAL SELECTION] Bound to Talent: {talent.displayWord}</b></color>");
            }
        }

        /// <summary>
        /// Pathway 3: Player embraces destiny and rolls a random wildcard from the 32 One-Word pool!
        /// </summary>
        public void ActionRandomizeFate()
        {
            if (ThreeMasksAndWildcardTalentSystem.Instance == null) return;

            appraisedTalent = ThreeMasksAndWildcardTalentSystem.Instance.RollRandomWildcardTalentAtOrb();
            isAppraisalCompleted = true;
            OnTalentChosen?.Invoke(appraisedTalent);
            Debug.Log($"<color=#EC4899><b>🎲 [FATE RANDOMIZED] Wildcard Gift & Flaw: {appraisedTalent?.displayWord}</b></color>");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = orbAuraColor;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
