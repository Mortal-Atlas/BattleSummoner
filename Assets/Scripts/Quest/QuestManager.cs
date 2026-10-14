using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Quests
{
    /// <summary>
    /// QuestManager is a persistent singleton controlling all adventurer job notices,
    /// tracking active quests, evaluating progress, and dispensing rewards.
    /// Supports Delivery, Slay, Escort, Bounty, Gather, and Investigation jobs.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        private static QuestManager _instance;
        public static QuestManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<QuestManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("QuestManager");
                        _instance = go.AddComponent<QuestManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Player Economy Status")]
        [SerializeField] private int playerGold = 500;
        public int PlayerGold => playerGold;

        [Header("Quest Database")]
        [SerializeField] private List<Quest> quests = new List<Quest>();
        public IReadOnlyList<Quest> AllQuests => quests;

        public event Action<Quest> OnQuestAccepted;
        public event Action<Quest> OnQuestUpdated;
        public event Action<Quest> OnQuestCompleted;
        public event Action<Quest> OnQuestClaimed;
        public event Action OnQuestListChanged;
        public event Action<int> OnGoldChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                if (transform.parent == null && Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
                EnsureDefaultQuests();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void AddGold(int amount)
        {
            playerGold = Mathf.Max(0, playerGold + amount);
            OnGoldChanged?.Invoke(playerGold);
            Debug.Log($"💰 [QUEST ECONOMY] Gold updated: {playerGold} (+{amount})");
        }

        public void EnsureDefaultQuests()
        {
            if (quests != null && quests.Count > 0) return;

            quests = new List<Quest>
            {
                // 1. BOUNTY: Bloodfang
                new Quest
                {
                    questId = "bounty_bloodfang",
                    title = "Bounty: Bloodfang the Dire Wolf",
                    clientName = "Mayor Barnaby",
                    locationName = "Mistwood Forest Outskirts",
                    questType = QuestType.Bounty,
                    difficulty = QuestDifficulty.Rank_B,
                    description = "A massive dire wolf corrupted by Spire miasma has terrorized supply caravans along the western highway. Hunt down Bloodfang and return with its pelt.",
                    objectiveText = "Hunt and defeat Bloodfang the Alpha Dire Wolf",
                    targetEntityOrNpc = "Bloodfang",
                    currentCount = 0,
                    targetCount = 1,
                    goldReward = 850,
                    expReward = 500,
                    itemRewardName = "Dire Wolf Fang Ring",
                    itemRewardQuantity = 1,
                    state = QuestState.Available
                },

                // 2. SLAY: Mistwood Arachnids
                new Quest
                {
                    questId = "slay_mistwood_spiders",
                    title = "Cull the Mistwood Arachnids",
                    clientName = "Ranger Zephyr",
                    locationName = "Deep Mistwood Trail",
                    questType = QuestType.Slay,
                    difficulty = QuestDifficulty.Rank_E,
                    description = "Venomous shadow spiders have woven thick webs across the logging roads. Exterminate 5 arachnids so woodsmen can safely gather timber.",
                    objectiveText = "Slay Mistwood Shadow Spiders",
                    targetEntityOrNpc = "Mistwood Spider",
                    currentCount = 0,
                    targetCount = 5,
                    goldReward = 320,
                    expReward = 220,
                    itemRewardName = "Vial of Spider Venom",
                    itemRewardQuantity = 2,
                    state = QuestState.Available
                },

                // 3. DELIVERY: Titan-Iron Tempering Ingot
                new Quest
                {
                    questId = "delivery_keith_ingot",
                    title = "Delivery: Tempered Titan-Iron Ingot",
                    clientName = "Master Smith Keith",
                    locationName = "Spire Gate Fortress",
                    questType = QuestType.Delivery,
                    difficulty = QuestDifficulty.Rank_D,
                    description = "I forged an enchanted titan-iron plate to reinforce the barrier at the Spire portal. Deliver this heavy crate directly to Spire Warden Gregory.",
                    objectiveText = "Deliver the Tempering Ingot to Spire Warden Gregory",
                    targetEntityOrNpc = "Spire Warden Gregory",
                    currentCount = 0,
                    targetCount = 1,
                    goldReward = 280,
                    expReward = 160,
                    itemRewardName = "Forgemaster's Whetstone",
                    itemRewardQuantity = 1,
                    state = QuestState.Available
                },

                // 4. ESCORT: Scholar Dennis's Expedition
                new Quest
                {
                    questId = "escort_scholar_dennis",
                    title = "Escort: The Forgotten Rune Shrine",
                    clientName = "Scholar Dennis",
                    locationName = "Mistwood Ancient Shrine",
                    questType = QuestType.Escort,
                    difficulty = QuestDifficulty.Rank_C,
                    description = "Ancient summoner relics have resonated within the stone ruins east of town. Guard Scholar Dennis as he studies the monolith carvings without monster harassment.",
                    objectiveText = "Safely escort Scholar Dennis to the Mistwood Shrine",
                    targetEntityOrNpc = "Scholar Dennis",
                    currentCount = 0,
                    targetCount = 1,
                    goldReward = 550,
                    expReward = 380,
                    itemRewardName = "Tome of Archaic Glyphs",
                    itemRewardQuantity = 1,
                    state = QuestState.Available
                },

                // 5. GATHER: Luminous Spire Moss
                new Quest
                {
                    questId = "gather_luminous_moss",
                    title = "Gather: Luminous Spire Flora",
                    clientName = "Apothecary Cheryl",
                    locationName = "Spire Outer Plateau",
                    questType = QuestType.Gather,
                    difficulty = QuestDifficulty.Rank_E,
                    description = "Our potion reserves are running low. Gather 4 clusters of bioluminescent moss that cling to the humid lower stone walls of the Spire entrance.",
                    objectiveText = "Collect Luminous Spire Moss clusters",
                    targetEntityOrNpc = "Luminous Moss",
                    currentCount = 0,
                    targetCount = 4,
                    goldReward = 240,
                    expReward = 150,
                    itemRewardName = "Grand Healing Elixir",
                    itemRewardQuantity = 2,
                    state = QuestState.Available
                },

                // 6. BOUNTY: The Crimson Reaver
                new Quest
                {
                    questId = "bounty_crimson_reaver",
                    title = "Bounty: The Crimson Reaver",
                    clientName = "Guildmaster Vance",
                    locationName = "Spire Lower Courtyard",
                    questType = QuestType.Bounty,
                    difficulty = QuestDifficulty.Rank_A,
                    description = "A renegade summoner wielding forbidden blood contracts was sighted near the broken obelisks. Subdue this high-threat rogue before innocent guild scouts are harmed.",
                    objectiveText = "Track down and eliminate The Crimson Reaver",
                    targetEntityOrNpc = "Crimson Reaver",
                    currentCount = 0,
                    targetCount = 1,
                    goldReward = 1400,
                    expReward = 850,
                    itemRewardName = "Crest of the Outlaw",
                    itemRewardQuantity = 1,
                    state = QuestState.Available
                },

                // 7. DELIVERY: Starfall Restorative Salve
                new Quest
                {
                    questId = "delivery_apothecary_salve",
                    title = "Delivery: Urgent Restorative Salve",
                    clientName = "Apothecary Cheryl",
                    locationName = "The Wandering Wyvern Tavern",
                    questType = QuestType.Delivery,
                    difficulty = QuestDifficulty.Rank_F,
                    description = "Adventurer Lyra returned from the woods bruised and fatigued. Run this soothing Starfall restorative salve over to her at the tavern immediately.",
                    objectiveText = "Deliver Restorative Salve to Adventurer Lyra at the Tavern",
                    targetEntityOrNpc = "Adventurer Lyra",
                    currentCount = 0,
                    targetCount = 1,
                    goldReward = 180,
                    expReward = 100,
                    itemRewardName = "Herbal Tea Pouch",
                    itemRewardQuantity = 1,
                    state = QuestState.Available
                },

                // 8. SLAY: Corrupted Spire Imps
                new Quest
                {
                    questId = "slay_spire_imps",
                    title = "Exterminate Voidfire Imps",
                    clientName = "Margo the Gothic Guide",
                    locationName = "Spire Dungeon Floor 1",
                    questType = QuestType.Slay,
                    difficulty = QuestDifficulty.Rank_D,
                    description = "Noxious fire imps are congregating around the summoner conduits in the spire. Eliminate 6 voidfire imps to stabilize the chamber's arcane frequency.",
                    objectiveText = "Slay Voidfire Imps in the Spire",
                    targetEntityOrNpc = "Voidfire Imp",
                    currentCount = 0,
                    targetCount = 6,
                    goldReward = 450,
                    expReward = 320,
                    itemRewardName = "Smoldering Core",
                    itemRewardQuantity = 3,
                    state = QuestState.Available
                },

                // 9. INVESTIGATION: Whispering Monolith Echoes
                new Quest
                {
                    questId = "investigate_spire_monoliths",
                    title = "Investigation: Whispering Monoliths",
                    clientName = "Guildmaster Vance",
                    locationName = "Spire Portal Ruins",
                    questType = QuestType.Investigation,
                    difficulty = QuestDifficulty.Rank_C,
                    description = "Scouts reported low resonant hums emanating from the four shattered gothic pillars flanking the portal. Inspect the runic monoliths and record any glyph distortions.",
                    objectiveText = "Survey and analyze the Whispering Monoliths",
                    targetEntityOrNpc = "Ancient Monolith",
                    currentCount = 0,
                    targetCount = 1,
                    goldReward = 420,
                    expReward = 260,
                    itemRewardName = "Monolith Rubbing Scroll",
                    itemRewardQuantity = 1,
                    state = QuestState.Available
                },

                // 10. ESCORT: Wandering Merchant Gidley
                new Quest
                {
                    questId = "escort_merchant_gidley",
                    title = "Escort: Stranded Silk Trader",
                    clientName = "Merchant Gidley",
                    locationName = "Mistwood Eastern Ridge",
                    questType = QuestType.Escort,
                    difficulty = QuestDifficulty.Rank_B,
                    description = "My pack-mule cart broke an axle near the goblin crossroads. Provide armed escort as we transport my rare dye crates into Solas Town Plaza.",
                    objectiveText = "Escort Merchant Gidley safely to Solas Town Plaza",
                    targetEntityOrNpc = "Merchant Gidley",
                    currentCount = 0,
                    targetCount = 1,
                    goldReward = 720,
                    expReward = 450,
                    itemRewardName = "Enchanted Silk Sash",
                    itemRewardQuantity = 1,
                    state = QuestState.Available
                }
            };
        }

        public Quest FindQuest(string questId)
        {
            if (quests == null) return null;
            return quests.Find(q => q.questId == questId);
        }

        public bool AcceptQuest(string questId)
        {
            Quest q = FindQuest(questId);
            if (q == null)
            {
                Debug.LogWarning($"⚠️ [QUEST] Cannot accept unknown quest ID: {questId}");
                return false;
            }

            if (q.state != QuestState.Available)
            {
                Debug.LogWarning($"⚠️ [QUEST] Quest '{q.title}' is not available (current: {q.state})");
                return false;
            }

            q.state = QuestState.Active;
            q.currentCount = 0;

            PlayQuestAudio("relic_chime");
            ShowQuestToast($"<color=#FFD700><b>[JOB ACCEPTED]</b></color>\n{q.title}");

            OnQuestAccepted?.Invoke(q);
            OnQuestListChanged?.Invoke();
            Debug.Log($"📜 [QUEST ACCEPTED] '{q.title}' ({q.questType}) - Objective: {q.objectiveText}");
            return true;
        }

        public bool AbandonQuest(string questId)
        {
            Quest q = FindQuest(questId);
            if (q == null || q.state != QuestState.Active) return false;

            q.state = QuestState.Available;
            q.currentCount = 0;

            PlayQuestAudio("relic_chime");
            ShowQuestToast($"<color=#E05050><b>[JOB ABANDONED]</b></color>\n{q.title}");

            OnQuestListChanged?.Invoke();
            Debug.Log($"📜 [QUEST ABANDONED] '{q.title}'");
            return true;
        }

        public void ProgressQuest(string questId, int amount = 1)
        {
            Quest q = FindQuest(questId);
            if (q == null || q.state != QuestState.Active) return;

            q.currentCount = Mathf.Min(q.targetCount, q.currentCount + amount);
            Debug.Log($"🎯 [QUEST PROGRESS] '{q.title}': {q.currentCount}/{q.targetCount}");

            OnQuestUpdated?.Invoke(q);

            if (q.IsCompleted)
            {
                q.state = QuestState.Completed;
                PlayQuestAudio("harp_resonance");
                ShowQuestToast($"<color=#40FF70><b>[OBJECTIVE COMPLETE!]</b></color>\n{q.title}\nReturn to Guild Task Board to claim reward.");
                OnQuestCompleted?.Invoke(q);
            }
            OnQuestListChanged?.Invoke();
        }

        public void ProgressQuestsByType(QuestType type, int amount = 1, string targetEntity = null)
        {
            if (quests == null) return;
            for (int i = 0; i < quests.Count; i++)
            {
                Quest q = quests[i];
                if (q.state == QuestState.Active && q.questType == type)
                {
                    if (string.IsNullOrEmpty(targetEntity) || 
                        string.IsNullOrEmpty(q.targetEntityOrNpc) || 
                        q.targetEntityOrNpc.Equals(targetEntity, StringComparison.OrdinalIgnoreCase))
                    {
                        ProgressQuest(q.questId, amount);
                    }
                }
            }
        }

        public bool ClaimQuestReward(string questId)
        {
            Quest q = FindQuest(questId);
            if (q == null) return false;

            if (q.state != QuestState.Completed)
            {
                Debug.LogWarning($"⚠️ [QUEST] Quest '{q.title}' is not ready to claim (current state: {q.state})");
                return false;
            }

            q.state = QuestState.Claimed;

            // Grant Gold
            AddGold(q.goldReward);

            // Grant Exp
            PlayerProgression prog = UnityEngine.Object.FindAnyObjectByType<PlayerProgression>();
            if (prog != null)
            {
                prog.AddExperience(q.expReward, 1);
            }

            PlayQuestAudio("harp_resonance");
            ShowQuestToast($"<color=#FFD700><b>[REWARD CLAIMED!]</b></color>\n+{q.goldReward} Gold  +{q.expReward} EXP\n{q.title}");

            OnQuestClaimed?.Invoke(q);
            OnQuestListChanged?.Invoke();
            Debug.Log($"🎉 [QUEST REWARD] Claimed '{q.title}': {q.goldReward}G, {q.expReward}EXP, Item: '{q.itemRewardName}'");
            return true;
        }

        public List<Quest> GetQuests(QuestType? filterType = null, QuestState? filterState = null)
        {
            EnsureDefaultQuests();
            List<Quest> result = new List<Quest>();
            for (int i = 0; i < quests.Count; i++)
            {
                Quest q = quests[i];
                if (filterType.HasValue && q.questType != filterType.Value) continue;
                if (filterState.HasValue && q.state != filterState.Value) continue;
                result.Add(q);
            }
            return result;
        }

        public int GetActiveQuestCount()
        {
            int count = 0;
            if (quests != null)
            {
                for (int i = 0; i < quests.Count; i++)
                {
                    if (quests[i].state == QuestState.Active) count++;
                }
            }
            return count;
        }

        private void PlayQuestAudio(string soundName)
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(soundName, 0.9f);
            }
        }

        private void ShowQuestToast(string message)
        {
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(message);
            }
        }
    }
}
