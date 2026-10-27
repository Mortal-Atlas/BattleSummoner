using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Progression.ThreeMasks
{
    /// <summary>
    /// UncappedTitleSystem manages the player's Prestige Titles.
    /// The Crown rule: Equips 1 cosmetic display title while permanently stacking flat stats
    /// and conditional passives from ALL earned titles infinitely.
    /// </summary>
    public class UncappedTitleSystem : MonoBehaviour
    {
        public static UncappedTitleSystem Instance { get; private set; }

        [Header("Active Display Title")]
        [SerializeField] private string activeDisplayTitle = "Spire Novice";

        [Header("Infinite Stacked Title Ledger")]
        [SerializeField] private List<StackablePrestigeTitle> stackedTitles = new List<StackablePrestigeTitle>();

        public event Action<string> OnTitleEquipped;
        public event Action<StackablePrestigeTitle> OnNewTitleEarned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SeedDefaultTitles();
        }

        private void SeedDefaultTitles()
        {
            if (stackedTitles.Count > 0) return;

            AwardTitle(new StackablePrestigeTitle
            {
                titleId = "title_spire_novice",
                titleDisplayName = "Spire Novice",
                earnedFeatDescription = "Crossed the Threshold of the Hundredfold Spire.",
                permanentAllStatBonus = 0.02f,
                conditionalTriggerTag = "Overworld",
                conditionalEffectBonus = 0.05f
            });

            AwardTitle(new StackablePrestigeTitle
            {
                titleId = "title_hundredfold_challenger",
                titleDisplayName = "Hundredfold Challenger",
                earnedFeatDescription = "Commenced ascension toward the upper terraces of the Spire Metropolis.",
                permanentAllStatBonus = 0.03f,
                conditionalTriggerTag = "SpireAscent",
                conditionalEffectBonus = 0.10f
            });
        }

        public void AwardTitle(StackablePrestigeTitle title)
        {
            if (title == null || string.IsNullOrEmpty(title.titleId)) return;
            if (stackedTitles.Exists(t => t.titleId == title.titleId)) return;

            stackedTitles.Add(title);
            if (ThreeMasksAndWildcardTalentSystem.Instance != null)
            {
                ThreeMasksAndWildcardTalentSystem.Instance.AwardPrestigeTitle(title);
            }

            OnNewTitleEarned?.Invoke(title);
        }

        public void EquipDisplayTitle(string titleDisplayName)
        {
            activeDisplayTitle = titleDisplayName;
            if (ThreeMasksAndWildcardTalentSystem.Instance != null)
            {
                ThreeMasksAndWildcardTalentSystem.Instance.equippedDisplayTitle = titleDisplayName;
            }
            OnTitleEquipped?.Invoke(activeDisplayTitle);
        }

        public string GetEquippedDisplayTitle() => activeDisplayTitle;

        public IReadOnlyList<StackablePrestigeTitle> GetAllTitles() => stackedTitles;

        public float CalculateTotalStatBonus(string activeConditionTag = "")
        {
            float total = 1.0f;
            foreach (var t in stackedTitles)
            {
                total += t.permanentAllStatBonus;
                if (!string.IsNullOrEmpty(activeConditionTag) && t.conditionalTriggerTag == activeConditionTag)
                {
                    total += t.conditionalEffectBonus;
                }
            }
            return total;
        }
    }
}
