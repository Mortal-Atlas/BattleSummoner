using System;
using UnityEngine;

namespace BattleSummoner.Quests
{
    public enum QuestType
    {
        Bounty,
        Slay,
        Delivery,
        Escort,
        Gather,
        Investigation
    }

    public enum QuestDifficulty
    {
        Rank_F,
        Rank_E,
        Rank_D,
        Rank_C,
        Rank_B,
        Rank_A,
        Rank_S
    }

    public enum QuestState
    {
        Available,
        Active,
        Completed,
        Claimed
    }

    [Serializable]
    public class Quest
    {
        [Header("Identity")]
        public string questId;
        public string title;
        public string clientName;
        public string locationName;
        public QuestType questType;
        public QuestDifficulty difficulty;

        [Header("Narrative & Objective")]
        [TextArea(2, 4)] public string description;
        public string objectiveText;
        public string targetEntityOrNpc;
        public int currentCount;
        public int targetCount = 1;

        [Header("Rewards")]
        public int goldReward = 100;
        public int expReward = 50;
        public string itemRewardName = "";
        public int itemRewardQuantity = 0;

        [Header("State")]
        public QuestState state = QuestState.Available;

        public bool IsCompleted => currentCount >= targetCount;
        public float ProgressNormalized => targetCount > 0 ? Mathf.Clamp01((float)currentCount / targetCount) : 0f;

        public string GetProgressString()
        {
            if (targetCount <= 1)
            {
                return IsCompleted ? "Done" : "Pending";
            }
            return $"{currentCount} / {targetCount}";
        }

        public string GetFormattedRank()
        {
            switch (difficulty)
            {
                case QuestDifficulty.Rank_F: return "RANK F";
                case QuestDifficulty.Rank_E: return "RANK E";
                case QuestDifficulty.Rank_D: return "RANK D";
                case QuestDifficulty.Rank_C: return "RANK C";
                case QuestDifficulty.Rank_B: return "RANK B";
                case QuestDifficulty.Rank_A: return "RANK A";
                case QuestDifficulty.Rank_S: return "RANK S";
                default: return "RANK C";
            }
        }

        public Color GetTypeColor()
        {
            switch (questType)
            {
                case QuestType.Bounty:
                    return new Color(0.95f, 0.22f, 0.22f); // Fiery Crimson Red
                case QuestType.Slay:
                    return new Color(0.92f, 0.42f, 0.15f); // Burning Amber Orange
                case QuestType.Delivery:
                    return new Color(0.18f, 0.78f, 0.45f); // Jade Emerald Green
                case QuestType.Escort:
                    return new Color(0.20f, 0.58f, 0.95f); // Cerulean Blue
                case QuestType.Gather:
                    return new Color(0.85f, 0.72f, 0.15f); // Golden Yellow
                case QuestType.Investigation:
                    return new Color(0.68f, 0.32f, 0.92f); // Arcane Violet
                default:
                    return new Color(0.80f, 0.80f, 0.80f);
            }
        }

        public string GetTypeBadgeHex()
        {
            return "#" + ColorUtility.ToHtmlStringRGB(GetTypeColor());
        }

        public Quest Clone()
        {
            return (Quest)this.MemberwiseClone();
        }
    }
}
