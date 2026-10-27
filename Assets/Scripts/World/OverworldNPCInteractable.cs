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

        [Header("Multi-Person Conversation (Anywhere)")]
        [Tooltip("If true, interacting triggers a multi-person conversation with multiple alternating speakers!")]
        public bool isMultiPersonConversation = false;
        public List<PersonaDialogueLine> multiPersonDialogue = new List<PersonaDialogueLine>();
        [Tooltip("If true, nearby companions or citizen NPCs dynamically chime into the conversation!")]
        public bool enableProximityCompanionChimeIn = true;

        [Header("Interaction Settings")]
        public float interactionRadius = 3.8f;
        public KeyCode interactKey = KeyCode.E;
        public string greetingAudioSfx = "margo_chime";

        [Header("Overhead Visuals")]
        public float overheadBadgeHeight = 2.1f;
        public Color nameplateColor = new Color(1.0f, 0.94f, 0.82f, 1.0f); // Warm ivory gold (replaces neon cyan)

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
            _nameplateTMP.text = $"<color=#F8FAFC><b>{npcName}</b></color>\n<size=75%><color=#E6C88A>〈 {npcRole} 〉</color></size>";
            _nameplateTMP.fontSize = 1.6f;
            _nameplateTMP.alignment = TextAlignmentOptions.Center;
            _nameplateTMP.sortingOrder = 50;
            _nameplateTMP.outlineColor = new Color32(14, 18, 28, 255);
            _nameplateTMP.outlineWidth = 0.22f;

            // Interaction Prompt Badge
            var promptObj = new GameObject("Prompt_Text");
            promptObj.transform.SetParent(_overheadCanvasObj.transform, false);
            promptObj.transform.localPosition = new Vector3(0f, -0.22f, 0f);
            _promptTMP = promptObj.AddComponent<TextMeshPro>();
            _promptTMP.text = "<color=#FFDF6D><b>[E]</b></color> <color=#FFFFFF>Talk</color>";
            _promptTMP.fontSize = 1.4f;
            _promptTMP.alignment = TextAlignmentOptions.Center;
            _promptTMP.sortingOrder = 51;
            _promptTMP.outlineColor = new Color32(14, 18, 28, 255);
            _promptTMP.outlineWidth = 0.18f;
            _promptTMP.gameObject.SetActive(false);

            // Ensure scale is positive (never mirrored)
            Vector3 canvasScale = _overheadCanvasObj.transform.localScale;
            if (canvasScale.x < 0f) canvasScale.x = Mathf.Abs(canvasScale.x);
            _overheadCanvasObj.transform.localScale = canvasScale;

            // Hide by default until player approaches
            _overheadCanvasObj.SetActive(false);
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                FindPlayerReference();
                if (_playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, _playerTransform.position);
            bool inRange = dist <= interactionRadius;
            bool nearPlayer = dist <= Mathf.Max(6.5f, interactionRadius * 1.5f);

            // Distance culling: hide overhead billboard entirely when player is far away
            if (_overheadCanvasObj != null)
            {
                if (_overheadCanvasObj.activeSelf != nearPlayer)
                {
                    _overheadCanvasObj.SetActive(nearPlayer);
                }

                // Billboard overhead prompt toward Camera with proper forward billboarding (never backwards)
                if (nearPlayer && Camera.main != null)
                {
                    Vector3 s = _overheadCanvasObj.transform.localScale;
                    if (s.x < 0f) _overheadCanvasObj.transform.localScale = new Vector3(Mathf.Abs(s.x), s.y, s.z);
                    _overheadCanvasObj.transform.rotation = Camera.main.transform.rotation;
                }
            }

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

            // Case A: Pre-authored multi-person dialogue sequence
            if (isMultiPersonConversation || (multiPersonDialogue != null && multiPersonDialogue.Count > 0))
            {
                if (PersonaDialogueUI.Instance != null)
                {
                    PersonaDialogueUI.Instance.ShowMultiPersonDialogue(multiPersonDialogue, OnDialogueFinished);
                    return;
                }
            }

            // Case B: Standard lines, with dynamic proximity chime-in if other NPCs or companions are nearby
            List<PersonaDialogueLine> finalLines = new List<PersonaDialogueLine>();
            if (dialogueLines != null)
            {
                for (int i = 0; i < dialogueLines.Length; i++)
                {
                    finalLines.Add(new PersonaDialogueLine
                    {
                        speakerName = npcName,
                        speakerTitle = npcRole,
                        portraitSprite = portraitSprite,
                        text = dialogueLines[i],
                        audioSfx = greetingAudioSfx
                    });
                }
            }

            if (enableProximityCompanionChimeIn)
            {
                AppendNearbyNPCChimeIns(finalLines);
            }

            if (PersonaDialogueUI.Instance != null && finalLines.Count > 0)
            {
                PersonaDialogueUI.Instance.ShowMultiPersonDialogue(finalLines, OnDialogueFinished);
            }
            else
            {
                Debug.Log($"[NPC Multi-Person Conversation] {npcName} ({npcRole}): \"{string.Join(" ", dialogueLines)}\"");
                // Auto finish in 4 seconds if PersonaDialogueUI is absent
                Invoke(nameof(OnDialogueFinished), 3.5f);
            }
        }

        private void AppendNearbyNPCChimeIns(List<PersonaDialogueLine> lines)
        {
            // Search for other NPCs within chime-in radius
            float searchRadius = interactionRadius * 2.2f;
            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].gameObject == gameObject) continue;
                var otherNpc = hits[i].GetComponent<OverworldNPCInteractable>();
                if (otherNpc != null && !string.IsNullOrEmpty(otherNpc.npcName))
                {
                    string chimeText = GetContextualChimeIn(otherNpc.npcName, npcName);
                    lines.Add(new PersonaDialogueLine
                    {
                        speakerName = otherNpc.npcName,
                        speakerTitle = otherNpc.npcRole,
                        portraitSprite = otherNpc.portraitSprite,
                        text = chimeText,
                        audioSfx = otherNpc.greetingAudioSfx
                    });
                    break; // Keep to 1 chime-in companion per interaction to maintain punchy pacing
                }
            }
        }

        private string GetContextualChimeIn(string chimeInName, string originalSpeaker)
        {
            string lowerChime = chimeInName.ToLowerInvariant();
            string lowerOriginal = originalSpeaker.ToLowerInvariant();

            if (lowerChime.Contains("barnaby") && lowerOriginal.Contains("margo"))
                return "Hah! Don't let Margo spook you with her gloomy alembics, Summoner. Good honest steel and fire keep the kingdom standing!";
            if (lowerChime.Contains("margo") && lowerOriginal.Contains("barnaby"))
                return "Ignore his hammer banging, Summoner. Without alchemical catalysts, his 'honest steel' would snap like dry twigs against spire fiends. (¬_¬)";
            if (lowerChime.Contains("keith") && lowerOriginal.Contains("cheryl"))
                return "Sister Cheryl speaks truth. The Church keeps our spirits resolute, but keep your stance wide when the rifts flare!";
            if (lowerChime.Contains("cheryl") && lowerOriginal.Contains("keith"))
                return "Captain Keith's shield never wavers. May the light of revival preserve all summoners who march into the breach.";
            if (lowerChime.Contains("zephyr"))
                return "♪ 'Steel in the hand, stars in the sky, summoners never say goodbye...' ♪ An epic tale in the making!";

            return $"I stand with {originalSpeaker}. The Hundredfold Spire will test every summoner's mettle today!";
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
