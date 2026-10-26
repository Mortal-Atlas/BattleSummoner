using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Progression.ThreeMasks
{
    public enum TalentWordID
    {
        Chef, SeaLover, Blacksmith, TimeWeaver, BloodVessel, VoidWalker,
        Ferryman, Gambler, Juggernaut, Plantmancy, Summoning, Futurist, VesselOfKnowledge, TaxCollector,
        Trike, Breaker, Spoon, Lizard, Fan, Fool, Machine, Lantern, Showman, Wonka,
        Salaryman, Pinpoint, Sponge, Magnet, Balloon, Cactus, Boomerang, Wax, Static, Crab, Zipper, Doll, Salt, Pendulum
    }

    public enum AffinityElement
    {
        None, SlicingCulinary, Water, Wind, Fire, Rock, Lightning, Blood, Void, Time, Plant, Healing, Protection, ArcaneKnowledge, ChaosLuck
    }

    public enum BaseGuildClass
    {
        Classless, Tank, Warrior, Ranger, Mage, Assassin, Summoner
    }

    [Serializable]
    public class DoubleEdgedTalentDefinition
    {
        public TalentWordID wordId;
        public string displayWord;
        public bool isWildcardRandomPool;
        public List<AffinityElement> grantedAffinities = new List<AffinityElement>();

        [Header("THE GIFT (Positive Buffs & EXP Boosts)")]
        [TextArea] public string giftDescription;
        public float favoredExpMultiplier = 1.30f;
        public float weaponLagReductionBonus = 0f;
        public float critOrSpecialMultiplier = 1.0f;
        public float spellCraftStabilityDiscount = 0f;

        [Header("THE FLAW (Natural Friction — Never Blocks Gameplay)")]
        [TextArea] public string flawDescription;
        public AffinityElement unproficientArea = AffinityElement.None;
        public float unproficientExpMultiplier = 0.70f;
        public float unproficientWeaponLagPenalty = 0.25f;
    }

    [Serializable]
    public class DormantNurtureTalent
    {
        public string hiddenTalentId;
        public string revealedName;
        public string unlockConditionDescription;
        public int currentProgressCount;
        public int requiredThreshold;
        public bool isAwakened;
        public string passiveEffectSummary;
    }

    [Serializable]
    public class StackablePrestigeTitle
    {
        public string titleId;
        public string titleDisplayName;
        [TextArea] public string earnedFeatDescription;
        public float permanentAllStatBonus = 0.05f;
        public string conditionalTriggerTag;
        public float conditionalEffectBonus = 0.20f;
    }

    [Serializable]
    public class ClassSkillTreeNode
    {
        public string nodeId;
        public string nodeName;
        public int spCost = 1;
        public bool unlocked = false;
        public int bonusMaxComboSwipes = 1;
    }

    public class ThreeMasksAndWildcardTalentSystem : MonoBehaviour
    {
        public static ThreeMasksAndWildcardTalentSystem Instance { get; private set; }

        [Header("MASK 1: Nature (Appraisal Orb Innate Talent)")]
        public DoubleEdgedTalentDefinition innateTalentMask1;
        public List<DoubleEdgedTalentDefinition> masterTalentRegistry = new List<DoubleEdgedTalentDefinition>();

        [Header("MASK 2: Nurture (Dormant Hidden Talents '???')")]
        public List<DormantNurtureTalent> nurtureTalentsMask2 = new List<DormantNurtureTalent>();

        [Header("MASK 3: Persona (Guild Hall Class & Infinite Evolution)")]
        public bool isInsideGuildHall = false;
        public BaseGuildClass activeGuildClass = BaseGuildClass.Warrior;
        public int classProficiencyLevel = 1;
        public float classProficiencyExp = 0f;
        public int availableClassSkillPoints = 0;
        public bool hasEvolvedSubClass = false;
        public string evolvedSubClassTitle = "";
        public List<ClassSkillTreeNode> activeClassSkillTree = new List<ClassSkillTreeNode>();

        [Header("Anti-Spam Swipe Combo Gate")]
        public int baseStartingMaxComboSwipes = 2;
        public float comboRecoveryCooldownSeconds = 0.45f;
        private int _currentSwipeCount = 0;
        private float _lastSwipeTimestamp = 0f;
        private float _comboCooldownUntil = 0f;

        [Header("THE CROWN: Uncapped Stackable Titles")]
        public string equippedDisplayTitle = "";
        public List<StackablePrestigeTitle> earnedStackedTitles = new List<StackablePrestigeTitle>();

        public event Action<DoubleEdgedTalentDefinition> OnOrbTalentAppraised;
        public event Action<DormantNurtureTalent> OnHiddenNurtureTalentAwakened;
        public event Action<string> OnClassOrEvolutionUpdated;
        public event Action<StackablePrestigeTitle> OnPrestigeTitleEarned;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SeedTalentRegistry();
            SeedDormantNurtureTalents();
            SeedDefaultComboSkillTree();
        }

        private void SeedTalentRegistry()
        {
            if (masterTalentRegistry.Count > 0) return;

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Chef, displayWord = "[ CHEF ]", isWildcardRandomPool = false,
                grantedAffinities = new List<AffinityElement> { AffinityElement.SlicingCulinary },
                giftDescription = "+20% Slicing Damage, zero lag with kitchen/cleaver blades, +35% Cooking EXP.",
                favoredExpMultiplier = 1.35f, weaponLagReductionBonus = -0.25f,
                flawDescription = "Heavy 2H blunt weapons feel clumsy (+25% lag).",
                unproficientArea = AffinityElement.Rock, unproficientWeaponLagPenalty = 0.25f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.TimeWeaver, displayWord = "[ TIME ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time },
                giftDescription = "-30% Mana & Instability when crafting 7D Time-Slow Bubbles, Limb-Drag, or Fast-Forward Decay spells.",
                favoredExpMultiplier = 1.30f, spellCraftStabilityDiscount = 0.30f,
                flawDescription = "Cannot ever freeze time (0x); raw physical shield-tanking drains stamina 25% faster.",
                unproficientArea = AffinityElement.Rock, unproficientWeaponLagPenalty = 0.20f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Gambler, displayWord = "[ GAMBLER ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ChaosLuck },
                giftDescription = "No normal hits: every attack or craft is either a 0-damage graze or a 300% Critical Jackpot!",
                critOrSpecialMultiplier = 3.0f,
                flawDescription = "Incapable of steady, predictable baseline damage or safe crafting grades."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.VesselOfKnowledge, displayWord = "[ VESSEL OF KNOWLEDGE ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ArcaneKnowledge },
                giftDescription = "Total Recall: Any spell witnessed or cast once is permanently memorized and swappable at any Inn.",
                favoredExpMultiplier = 1.40f, spellCraftStabilityDiscount = 0.20f,
                flawDescription = "Overcrowded Mind: Heavy physical weapons suffer +40% blade lag behind your finger.",
                unproficientWeaponLagPenalty = 0.40f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Spoon, displayWord = "[ SPOON ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.SlicingCulinary, AffinityElement.Protection },
                giftDescription = "Curved props, ladles, woks & shields scoop up enemy projectiles/puddles mid-swipe and fling them back!",
                flawDescription = "-30% straight piercing/thrust damage with rapiers and spears."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Salaryman, displayWord = "[ SALARYMAN ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time },
                giftDescription = "-35% stamina/mana cost for the first 60s of combat; can 'Clock Out' to drop a 65% Time-Slow commuter bubble.",
                flawDescription = "Unpaid Overtime: Fights lasting longer than 3 minutes inflict +25% fatigue blade lag unless you drink coffee/tea."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Pinpoint, displayWord = "[ PINPOINT ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time },
                giftDescription = "Striking an exact armor seam blooms a micro Time-Slow bubble on that limb and deals 2.5x Piercing Crit.",
                critOrSpecialMultiplier = 2.5f, weaponLagReductionBonus = -0.30f,
                flawDescription = "Wide horizontal AoE crowd-cleaves lose 40% damage and knockback."
            });
        }

        private void SeedDormantNurtureTalents()
        {
            nurtureTalentsMask2.Add(new DormantNurtureTalent {
                hiddenTalentId = "nurture_lightning_rod", revealedName = "Human Lightning Rod",
                unlockConditionDescription = "Survive being struck by lightning 3 times.",
                requiredThreshold = 3, isAwakened = false,
                passiveEffectSummary = "Absorbs 50% of incoming Lightning damage to overcharge next weapon swipe."
            });
            nurtureTalentsMask2.Add(new DormantNurtureTalent {
                hiddenTalentId = "nurture_iron_stomach", revealedName = "Iron Stomach",
                unlockConditionDescription = "Consume 50 questionable or toxic monster dishes.",
                requiredThreshold = 50, isAwakened = false,
                passiveEffectSummary = "Converts Poison debuffs into passive Mana Regeneration."
            });
        }

        private void SeedDefaultComboSkillTree()
        {
            activeClassSkillTree = new List<ClassSkillTreeNode>
            {
                new ClassSkillTreeNode { nodeId = "combo_3", nodeName = "3-Strike Flow", spCost = 1, bonusMaxComboSwipes = 1 },
                new ClassSkillTreeNode { nodeId = "combo_4", nodeName = "4-Strike Cross-Weave", spCost = 2, bonusMaxComboSwipes = 1 },
                new ClassSkillTreeNode { nodeId = "combo_5", nodeName = "5-Strike Relentless", spCost = 3, bonusMaxComboSwipes = 1 },
                new ClassSkillTreeNode { nodeId = "combo_6", nodeName = "6-Strike Apex Mastery", spCost = 4, bonusMaxComboSwipes = 1 }
            };
        }

        public DoubleEdgedTalentDefinition AppraiseFromQuestionnaire(string dreamJob, string desiredPower, string environment)
        {
            string combined = $"{dreamJob} {desiredPower} {environment}".ToLowerInvariant();
            if (combined.Contains("time") || combined.Contains("clock"))
                innateTalentMask1 = masterTalentRegistry.Find(t => t.wordId == TalentWordID.TimeWeaver);
            else if (combined.Contains("chef") || combined.Contains("cook"))
                innateTalentMask1 = masterTalentRegistry.Find(t => t.wordId == TalentWordID.Chef);
            else
                innateTalentMask1 = masterTalentRegistry[0];

            OnOrbTalentAppraised?.Invoke(innateTalentMask1);
            return innateTalentMask1;
        }

        public DoubleEdgedTalentDefinition RollRandomWildcardTalentAtOrb()
        {
            var wildcards = masterTalentRegistry.FindAll(t => t.isWildcardRandomPool);
            innateTalentMask1 = wildcards[UnityEngine.Random.Range(0, wildcards.Count)];
            OnOrbTalentAppraised?.Invoke(innateTalentMask1);
            return innateTalentMask1;
        }

        public void RecordWorldNurtureEvent(string hiddenTalentId, int increment = 1)
        {
            var dormant = nurtureTalentsMask2.Find(t => t.hiddenTalentId == hiddenTalentId);
            if (dormant == null || dormant.isAwakened) return;

            dormant.currentProgressCount += increment;
            if (dormant.currentProgressCount >= dormant.requiredThreshold)
            {
                dormant.isAwakened = true;
                OnHiddenNurtureTalentAwakened?.Invoke(dormant);
            }
        }

        public int GetMaxAllowedComboSwipes()
        {
            int max = baseStartingMaxComboSwipes;
            foreach (var node in activeClassSkillTree)
                if (node.unlocked) max += node.bonusMaxComboSwipes;
            return max;
        }

        public bool ValidateAndConsumeComboSwipe()
        {
            if (Time.time < _comboCooldownUntil) return false;
            if (Time.time - _lastSwipeTimestamp > 0.85f) _currentSwipeCount = 0;

            _currentSwipeCount++;
            _lastSwipeTimestamp = Time.time;

            if (_currentSwipeCount >= GetMaxAllowedComboSwipes())
            {
                _comboCooldownUntil = Time.time + comboRecoveryCooldownSeconds;
                _currentSwipeCount = 0;
            }
            return true;
        }

        public bool TryEvolveInfiniteSubClassAtGuildHall(AffinityElement catalystAffinity, string customPrefix = "")
        {
            if (!isInsideGuildHall || classProficiencyLevel < 25) return false;
            hasEvolvedSubClass = true;
            string prefix = !string.IsNullOrEmpty(customPrefix) ? customPrefix : catalystAffinity.ToString();
            evolvedSubClassTitle = $"{prefix} {activeGuildClass}";
            OnClassOrEvolutionUpdated?.Invoke(evolvedSubClassTitle);
            return true;
        }

        public void AwardPrestigeTitle(StackablePrestigeTitle newTitle)
        {
            if (earnedStackedTitles.Exists(t => t.titleId == newTitle.titleId)) return;
            earnedStackedTitles.Add(newTitle);
            if (string.IsNullOrEmpty(equippedDisplayTitle)) equippedDisplayTitle = newTitle.titleDisplayName;
            OnPrestigeTitleEarned?.Invoke(newTitle);
        }

        public float GetTotalStackedTitleStatMultiplier(string activeConditionTag = "")
        {
            float mult = 1.0f;
            foreach (var title in earnedStackedTitles)
            {
                mult += title.permanentAllStatBonus;
                if (!string.IsNullOrEmpty(activeConditionTag) && title.conditionalTriggerTag == activeConditionTag)
                    mult += title.conditionalEffectBonus;
            }
            return mult;
        }
    }
}
