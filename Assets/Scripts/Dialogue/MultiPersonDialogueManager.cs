using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.UI;

namespace BattleSummoner.Dialogue
{
    /// <summary>
    /// MultiPersonDialogueManager coordinates multi-character conversations and field party banter
    /// across ALL scenes and environments (Overworld, Boss Dungeons, Interiors, and CampHub).
    /// Supports Persona-style visual novel cutscene dialogue with alternating character portraits,
    /// reactive companion chime-ins, field banter triggers, and full hotkey integration.
    /// </summary>
    public class MultiPersonDialogueManager : MonoBehaviour
    {
        private static MultiPersonDialogueManager _instance;
        public static MultiPersonDialogueManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<MultiPersonDialogueManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MultiPersonDialogueManager");
                        _instance = go.AddComponent<MultiPersonDialogueManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureManagerRuntime()
        {
            if (_instance == null)
            {
                _ = Instance;
            }
        }

        [Header("Global Hotkeys")]
        [SerializeField] private KeyCode banterHotkey = KeyCode.B;
        [SerializeField] private KeyCode chatHubHotkey = KeyCode.C;
        [SerializeField] private bool allowFieldBanterAnywhere = true;

        [Header("Banter Timing")]
        [SerializeField] private float autoBanterIntervalSeconds = 180f; // 3 minutes
        private float lastBanterTime = 0f;
        private int currentBanterIndex = 0;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                lastBanterTime = Time.time;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(banterHotkey))
            {
                TriggerRandomFieldPartyBanter();
            }

            if (Input.GetKeyDown(chatHubHotkey))
            {
                ToggleMultiPersonChatHub();
            }

            // Optional ambient field banter during long journeys
            if (allowFieldBanterAnywhere && Time.time - lastBanterTime > autoBanterIntervalSeconds)
            {
                if (PersonaDialogueUI.Instance != null && !PersonaDialogueUI.Instance.IsDialogueActive)
                {
                    TriggerRandomFieldPartyBanter();
                }
                lastBanterTime = Time.time;
            }
        }

        /// <summary>
        /// Plays a multi-person conversation anywhere in the game world with sequential speakers,
        /// dynamic portrait updates, voice chimes, and choice callbacks.
        /// </summary>
        public void PlayConversation(IEnumerable<PersonaDialogueLine> lines, Action onComplete = null)
        {
            if (PersonaDialogueUI.Instance != null)
            {
                PersonaDialogueUI.Instance.ShowMultiPersonDialogue(lines, onComplete);
            }
            else
            {
                Debug.Log("[MultiPersonDialogueManager] PersonaDialogueUI not found in scene. Playing conversation via log fallback.");
                foreach (var l in lines)
                {
                    Debug.Log($"[{l.speakerName.ToUpper()}] ({l.speakerTitle}): \"{l.text}\"");
                }
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Triggers multi-person party banter between companions anywhere (Overworld, Dungeon, Town).
        /// </summary>
        public void TriggerRandomFieldPartyBanter(Action onComplete = null)
        {
            if (PersonaDialogueUI.Instance != null && PersonaDialogueUI.Instance.IsDialogueActive) return;

            var arcs = GetAuthoredFieldBanters();
            if (arcs == null || arcs.Count == 0) return;

            var chosenArc = arcs[currentBanterIndex % arcs.Count];
            currentBanterIndex++;
            lastBanterTime = Time.time;

            PlayConversation(chosenArc, onComplete);
        }

        public void ToggleMultiPersonChatHub()
        {
            if (MultiPersonDialogueChatHub.Instance != null)
            {
                bool active = MultiPersonDialogueChatHub.Instance.IsOverlayActive;
                MultiPersonDialogueChatHub.Instance.SetVisible(!active);
            }
            else
            {
                Debug.Log("[MultiPersonDialogueManager] MultiPersonDialogueChatHub overlay not active in current scene.");
            }
        }

        #region Authored Multi-Person Banter Arcs
        public List<List<PersonaDialogueLine>> GetAuthoredFieldBanters()
        {
            var list = new List<List<PersonaDialogueLine>>();

            // Arc 1: Spire Ascent & Combat Tactics (Aria, Leo, Margo)
            list.Add(new List<PersonaDialogueLine>
            {
                new PersonaDialogueLine
                {
                    speakerName = "Aria",
                    speakerTitle = "Vanguard Cleric",
                    text = "The spire's energy pulses stronger as we advance. Keep your balance, everyone!",
                    audioSfx = "bell_chime"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Leo",
                    speakerTitle = "Blade Sentinel",
                    text = "My shield arm is ready. Whatever fiends crawl out of the rifts, I'll draw their aggression first.",
                    audioSfx = "sword_unsheath"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Margo",
                    speakerTitle = "Occultist Alchemist",
                    text = "Don't rush in blindly, Leo. The rift constructs absorb physical impacts unless we shatter their crystalline cores first. (¬_¬)",
                    audioSfx = "margo_chime"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Player",
                    speakerTitle = "Grand Summoner",
                    text = "Understood. Aria provides recovery, Leo draws aggro, and Margo and I crack the armor.",
                    audioSfx = "relic_chime"
                }
            });

            // Arc 2: Grand Avenue & Market Banter (Barnaby, Margo, Zephyr)
            list.Add(new List<PersonaDialogueLine>
            {
                new PersonaDialogueLine
                {
                    speakerName = "Barnaby",
                    speakerTitle = "Master Anvil Artisan",
                    text = "Hah! Look at those imperial terraces! Every archway braced with tempered Mithril. That's real craft!",
                    audioSfx = "forge_strike"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Margo",
                    speakerTitle = "Goth Occultist",
                    text = "Mithril is fine for doorstops, Barnaby. But it takes Void Amethyst resonance to seal interdimensional cracks.",
                    audioSfx = "margo_chime"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Zephyr",
                    speakerTitle = "Wandering Minstrel",
                    text = "♪ 'One swings the hammer, one brews the tea, together they squabble in grand harmony!' ♪",
                    audioSfx = "harp_flourish"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Barnaby",
                    speakerTitle = "Master Anvil Artisan",
                    text = "Keep strumming that lute, Zephyr, and I'll forge you steel strings that won't snap when monsters roar!",
                    audioSfx = "forge_strike"
                }
            });

            // Arc 3: Faith and Steel (Cheryl, Keith, Player)
            list.Add(new List<PersonaDialogueLine>
            {
                new PersonaDialogueLine
                {
                    speakerName = "Sister Cheryl",
                    speakerTitle = "Church of Revival",
                    text = "The blessings of the Soul-Binder crystal protect all who step beyond the gates.",
                    audioSfx = "bell_chime"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Captain Keith",
                    speakerTitle = "Imperial Vanguard",
                    text = "Blessings are welcomed, Sister, but discipline and iron keep the perimeter from falling.",
                    audioSfx = "sword_unsheath"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Player",
                    speakerTitle = "Grand Summoner",
                    text = "Both faith and steel will see the Hundredfold Spire reclaimed.",
                    audioSfx = "relic_chime"
                }
            });

            // Arc 4: Camp & Journey Recollections (Alex, Elena, Margo)
            list.Add(new List<PersonaDialogueLine>
            {
                new PersonaDialogueLine
                {
                    speakerName = "Alex",
                    speakerTitle = "Scout Delver",
                    text = "The journey has been intense, but setting up camp by the hearth always clears the fatigue.",
                    audioSfx = "relic_chime"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Elena",
                    speakerTitle = "Arcane Mage",
                    text = "I agree. Hot herbal stew and a quiet night under the stars prepare us for Floor 50.",
                    audioSfx = "margo_chime"
                },
                new PersonaDialogueLine
                {
                    speakerName = "Margo",
                    speakerTitle = "Occultist Alchemist",
                    text = "Just don't let Barnaby cook the stew. Last time he stirred it with a forging poker. (¬_¬)",
                    audioSfx = "margo_chime"
                }
            });

            return list;
        }
        #endregion
    }
}
