using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BattleSummoner.Optimization;
using BattleSummoner.Animation;

namespace BattleSummoner.World
{
    /// <summary>
    /// OverworldNPCInteractable powers land NPCs across the city streets.
    /// Provides overhead nameplate, proximity detection, player look-at,
    /// voice chimes, and full integration with PersonaDialogueUI.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Collider))]
    public class OverworldNPCInteractable : MonoBehaviour
    {
        [Header("NPC Identity & Role")]
        public string npcName = "Town Citizen";
        public string npcRole = "Resident";
        public Sprite portraitSprite;

        [Header("Persona Dialogue Lines")]
        [TextArea(2, 4)]
        public string[] dialogueLines = new string[]
        {
            "Welcome to Aethelburg! The Hundredfold Spire looms high above us.",
            "Summoners from all corners of the world gather here to challenge the Spire.",
            "Watch out for the outer rifts! The incursions have grown fiercer by the day."
        };

        [Header("Interaction Settings")]
        public float interactionRadius = 3.8f;
        public KeyCode interactKey = KeyCode.E;
        public string greetingAudioSfx = "margo_chime";

        [Header("Overhead Visuals")]
        public float overheadBadgeHeight = 2.1f;
        public Color nameplateColor = new Color(0.2f, 0.95f, 1.0f);

        private Transform _playerTransform;
        private GameObject _overheadCanvasObj;
        private TextMeshPro _nameplateTMP;
        private TextMeshPro _promptTMP;
        private bool _isPlayerInRange = false;
        private bool _isCurrentlyTalking = false;
        public bool IsCurrentlyTalking => _isCurrentlyTalking;
        private Quaternion _originalRotation;

        private void OnEnable()
        {
            CreateOverheadPromptUI();
        }

        private void Start()
        {
            _originalRotation = transform.rotation;
            FindPlayerReference();
            CreateOverheadPromptUI();

            // Ensure collider is configured
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Auto-attach secondary idle breathing
            if (GetComponent<NPCSecondaryIdleAnimator>() == null)
            {
                gameObject.AddComponent<NPCSecondaryIdleAnimator>();
            }

            // Auto-attach CullableWorldProp for zero-loss proximity optimization
            if (GetComponent<CullableWorldProp>() == null)
            {
                var prop = gameObject.AddComponent<CullableWorldProp>();
                prop.boundingRadius = 3.0f;
                prop.isDynamicMover = false;
            }
        }

        private void FindPlayerReference()
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) _playerTransform = pObj.transform;
        }

        private void CreateOverheadPromptUI()
        {
            if (_overheadCanvasObj != null) return;

            var existing = transform.Find("Overhead_Prompt_Billboard");
            if (existing != null)
            {
                _overheadCanvasObj = existing.gameObject;
                var nObj = _overheadCanvasObj.transform.Find("Nameplate_Text");
                if (nObj != null) _nameplateTMP = nObj.GetComponent<TextMeshPro>();
                var pObj = _overheadCanvasObj.transform.Find("Prompt_Text");
                if (pObj != null) _promptTMP = pObj.GetComponent<TextMeshPro>();
                return;
            }

            _overheadCanvasObj = new GameObject("Overhead_Prompt_Billboard");
            _overheadCanvasObj.transform.SetParent(transform, false);
            _overheadCanvasObj.transform.localPosition = new Vector3(0f, overheadBadgeHeight, 0f);

            // Nameplate text
            var nameObj = new GameObject("Nameplate_Text");
            nameObj.transform.SetParent(_overheadCanvasObj.transform, false);
            nameObj.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            _nameplateTMP = nameObj.AddComponent<TextMeshPro>();
            _nameplateTMP.text = $"<color=#{ColorUtility.ToHtmlStringRGB(nameplateColor)}><b>{npcName}</b></color>\n<size=70%><color=#FFE070>〈 {npcRole} 〉</color></size>";
            _nameplateTMP.fontSize = 3.6f;
            _nameplateTMP.alignment = TextAlignmentOptions.Center;
            _nameplateTMP.sortingOrder = 50;

            // Interaction Prompt Badge
            var promptObj = new GameObject("Prompt_Text");
            promptObj.transform.SetParent(_overheadCanvasObj.transform, false);
            promptObj.transform.localPosition = new Vector3(0f, -0.22f, 0f);
            _promptTMP = promptObj.AddComponent<TextMeshPro>();
            _promptTMP.text = "<color=#FFFF55><b>[E] TALK</b></color>";
            _promptTMP.fontSize = 3.0f;
            _promptTMP.alignment = TextAlignmentOptions.Center;
            _promptTMP.sortingOrder = 51;
            _promptTMP.gameObject.SetActive(false);
        }

        private void Update()
        {
            // Billboard overhead prompt toward Camera
            if (_overheadCanvasObj != null && Camera.main != null)
            {
                _overheadCanvasObj.transform.rotation = Camera.main.transform.rotation;
            }

            if (_playerTransform == null)
            {
                FindPlayerReference();
                if (_playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            bool inRange = dist <= interactionRadius;

            if (inRange != _isPlayerInRange)
            {
                _isPlayerInRange = inRange;
                if (_promptTMP != null) _promptTMP.gameObject.SetActive(inRange && !_isCurrentlyTalking);
            }

            // Keyboard interact trigger
            if (_isPlayerInRange && !_isCurrentlyTalking)
            {
                if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.F))
                {
                    TriggerInteraction();
                }
            }

            // Smooth face toward player when interacting
            if (_isCurrentlyTalking && _playerTransform != null)
            {
                Vector3 lookDir = (_playerTransform.position - transform.position).normalized;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
                }
            }
            else if (!_isCurrentlyTalking && transform.rotation != _originalRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _originalRotation, Time.deltaTime * 3f);
            }
        }

        private void OnMouseDown()
        {
            // Touch / mouse click interaction
            if (_isPlayerInRange && !_isCurrentlyTalking)
            {
                TriggerInteraction();
            }
        }

        [ContextMenu("Trigger Dialogue Interaction")]
        public void TriggerInteraction()
        {
            if (_isCurrentlyTalking) return;
            _isCurrentlyTalking = true;
            if (_promptTMP != null) _promptTMP.gameObject.SetActive(false);

            if (BattleSummonerAudioMaster.Instance != null && !string.IsNullOrEmpty(greetingAudioSfx))
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(greetingAudioSfx, 0.85f);
            }

            if (PersonaDialogueUI.Instance != null && dialogueLines != null && dialogueLines.Length > 0)
            {
                PersonaDialogueUI.Instance.ShowSimpleDialogue(npcName, npcRole, portraitSprite, dialogueLines, OnDialogueFinished);
            }
            else
            {
                Debug.Log($"[NPC Conversation] {npcName} ({npcRole}): \"{string.Join(" ", dialogueLines)}\"");
                // Auto finish in 4 seconds if PersonaDialogueUI is absent
                Invoke(nameof(OnDialogueFinished), 3.5f);
            }
        }

        private void OnDialogueFinished()
        {
            _isCurrentlyTalking = false;
            if (_promptTMP != null && _isPlayerInRange)
            {
                _promptTMP.gameObject.SetActive(true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
