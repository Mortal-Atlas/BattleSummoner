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
            if (Instance != null && Instance != this && Application.isPlaying) { Destroy(gameObject); return; }
            Instance = this;
            InitializeForEditorOrPlay();
        }

        public void InitializeForEditorOrPlay()
        {
            SeedTalentRegistry();
            SeedDormantNurtureTalents();
            SeedDefaultComboSkillTree();
        }

        private void SeedTalentRegistry()
        {
            if (masterTalentRegistry.Count > 0) return;

            // BASELINE TALENTS
            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Chef, displayWord = "[ CHEF ]", isWildcardRandomPool = false,
                grantedAffinities = new List<AffinityElement> { AffinityElement.SlicingCulinary },
                giftDescription = "+20% Slicing Damage, zero lag with kitchen/cleaver blades, +35% Cooking EXP.",
                favoredExpMultiplier = 1.35f, weaponLagReductionBonus = -0.25f,
                flawDescription = "Heavy 2H blunt weapons feel clumsy (+25% lag).",
                unproficientArea = AffinityElement.Rock, unproficientWeaponLagPenalty = 0.25f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.SeaLover, displayWord = "[ SEA LOVER ]", isWildcardRandomPool = false,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Water },
                giftDescription = "+40% swim speed, water exploration stamina drain halved, aquatic harvest yield doubled.",
                favoredExpMultiplier = 1.30f,
                flawDescription = "Arid desert environments drain stamina 20% faster; -15% Fire resistance.",
                unproficientArea = AffinityElement.Fire, unproficientWeaponLagPenalty = 0.15f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Blacksmith, displayWord = "[ BLACKSMITH ]", isWildcardRandomPool = false,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Rock, AffinityElement.Fire },
                giftDescription = "+30% weapon durability, forge reinforcement grants extra sub-slot, +30% Mining EXP.",
                favoredExpMultiplier = 1.35f, weaponLagReductionBonus = -0.20f,
                flawDescription = "Delicate ranged instruments and bows feel uncoordinated (+20% aim lag).",
                unproficientArea = AffinityElement.Wind, unproficientWeaponLagPenalty = 0.20f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.TimeWeaver, displayWord = "[ TIME ]", isWildcardRandomPool = false,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time },
                giftDescription = "-30% Mana & Instability when crafting 7D Time-Slow Bubbles, Limb-Drag, or Fast-Forward Decay spells.",
                favoredExpMultiplier = 1.30f, spellCraftStabilityDiscount = 0.30f,
                flawDescription = "Golden Law of 7D Time: Cannot freeze time (0x blocked); physical tanking drains stamina 25% faster.",
                unproficientArea = AffinityElement.Rock, unproficientWeaponLagPenalty = 0.20f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.BloodVessel, displayWord = "[ BLOOD VESSEL ]", isWildcardRandomPool = false,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Blood },
                giftDescription = "Siphon vitality on heavy lacerations; bleed ticks heal the player for 25% of bleed damage.",
                favoredExpMultiplier = 1.30f, critOrSpecialMultiplier = 1.4f,
                flawDescription = "Purification draughts and holy healing potions are 25% less potent.",
                unproficientArea = AffinityElement.Healing, unproficientWeaponLagPenalty = 0.15f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.VoidWalker, displayWord = "[ VOID WALKER ]", isWildcardRandomPool = false,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Void },
                giftDescription = "Dashing phases through enemy collision; gravity lag reduced by 40% during aerial combat.",
                favoredExpMultiplier = 1.35f,
                flawDescription = "Holy light and radiant shrine sanctuaries cause 20% increased vulnerability.",
                unproficientArea = AffinityElement.Protection, unproficientWeaponLagPenalty = 0.20f
            });

            // 32 ONE-WORD WILDCARD TALENTS (Option A Appraisal Orb Pool)
            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Ferryman, displayWord = "[ FERRYMAN ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Water },
                giftDescription = "Effortlessly glide over ethereal and aquatic surfaces (+40% traversal speed near water).",
                favoredExpMultiplier = 1.25f, flawDescription = "Movement speed reduced by 15% across dry desert terrain."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Gambler, displayWord = "[ GAMBLER ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ChaosLuck },
                giftDescription = "All attacks and crafts bypass baseline grades: either a 0-damage graze or a 300% Critical Jackpot!",
                critOrSpecialMultiplier = 3.0f, flawDescription = "Incapable of steady baseline damage or safe crafting outcomes."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Juggernaut, displayWord = "[ JUGGERNAUT ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Rock, AffinityElement.Protection },
                giftDescription = "Unstoppable poise during heavy strikes; cannot be staggered by light monster attacks.",
                favoredExpMultiplier = 1.25f, flawDescription = "+30% blade lag on high-speed directional recovery."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Plantmancy, displayWord = "[ PLANTMANCY ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Plant },
                giftDescription = "Strikes sprout grasping brambles that root enemies for 1.8s; +40% forage yield.",
                favoredExpMultiplier = 1.30f, flawDescription = "-25% fire resistance; burns tick for double duration."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Summoning, displayWord = "[ SUMMONING ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ArcaneKnowledge },
                giftDescription = "Summoned familiars and spire companions deal +35% damage and hold 50% more aggro.",
                favoredExpMultiplier = 1.35f, flawDescription = "Player direct blade kinetic damage reduced by 15%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Futurist, displayWord = "[ FUTURIST ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time, AffinityElement.ArcaneKnowledge },
                giftDescription = "Spellcraft workbench reveals exact formula outcome previews; -20% spell instability.",
                spellCraftStabilityDiscount = 0.20f, flawDescription = "-20% EXP gained from ancient non-mechanical relics."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.VesselOfKnowledge, displayWord = "[ VESSEL OF KNOWLEDGE ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ArcaneKnowledge },
                giftDescription = "Total Recall: Any spell or rune witnessed once is permanently memorized and swappable at any Inn.",
                favoredExpMultiplier = 1.40f, spellCraftStabilityDiscount = 0.20f,
                flawDescription = "Overcrowded Mind: Heavy physical weapons suffer +40% blade lag behind your finger.",
                unproficientWeaponLagPenalty = 0.40f
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.TaxCollector, displayWord = "[ TAX COLLECTOR ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ChaosLuck },
                giftDescription = "Defeated foes drop +45% mana cores and gold coins; bounties pay 30% higher rewards.",
                favoredExpMultiplier = 1.25f, flawDescription = "Shopkeeper prices in foreign settlements increase by 15%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Trike, displayWord = "[ TRIKE ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Wind },
                giftDescription = "Mounted riding and sprint acceleration increased by +35%; ramming impact deals kinetic knockback.",
                favoredExpMultiplier = 1.20f, flawDescription = "Cornering turning circle during high-speed sprint widened by 25%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Breaker, displayWord = "[ BREAKER ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Rock },
                giftDescription = "+50% shield-break and posture impact against guarded or armored opponents.",
                critOrSpecialMultiplier = 1.5f, flawDescription = "Weapon edge degrades 20% faster when clashing with heavy stone."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Spoon, displayWord = "[ SPOON ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.SlicingCulinary, AffinityElement.Protection },
                giftDescription = "Curved props, ladles, woks & shields scoop up enemy projectiles/puddles mid-swipe and fling them back!",
                critOrSpecialMultiplier = 1.4f, flawDescription = "-30% straight piercing/thrust damage with rapiers and spears."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Lizard, displayWord = "[ LIZARD ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Healing },
                giftDescription = "Basking in direct sunlight restores 2% max HP and Stamina every 3 seconds while resting.",
                favoredExpMultiplier = 1.20f, flawDescription = "Movement speed and attack cadence slowed by 30% in freezing snow biomes."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Fan, displayWord = "[ FAN ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Wind },
                giftDescription = "Blade swipes kick up swirling miniature typhoons that draw surrounding mob clusters inward.",
                favoredExpMultiplier = 1.25f, flawDescription = "Single-target focus damage reduced by 20%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Fool, displayWord = "[ FOOL ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ChaosLuck },
                giftDescription = "Attacks randomly trigger bizarre spectacle debuffs (confuse, tickle, spin) that stun foes for 2s.",
                critOrSpecialMultiplier = 1.6f, flawDescription = "Base magical ward integrity weakened by 15%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Machine, displayWord = "[ MACHINE ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ArcaneKnowledge },
                giftDescription = "Clockwork Precision: 1:1 fingertip blade tracking with zero latency jitter.",
                weaponLagReductionBonus = -0.35f, flawDescription = "Mana regeneration halts for 3.0s following any spell cast."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Lantern, displayWord = "[ LANTERN ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Protection },
                giftDescription = "Radiates an astral golden aura that dispels darkness, reveals camouflaged monsters and secret doors.",
                favoredExpMultiplier = 1.25f, flawDescription = "Night stealth detection radius expanded by 30%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Showman, displayWord = "[ SHOWMAN ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Wind },
                giftDescription = "Landing continuous 4+ hit combos builds up crowd adrenaline, boosting damage up to +40%.",
                critOrSpecialMultiplier = 1.4f, flawDescription = "Whiffed or missed swipes drain 15% extra stamina."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Wonka, displayWord = "[ WONKA ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ChaosLuck },
                giftDescription = "Crafting confections and potions yields double batch output with whimsical buff surges.",
                favoredExpMultiplier = 1.30f, flawDescription = "Savory life-sim cooking recipes suffer a 20% potency penalty."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Salaryman, displayWord = "[ SALARYMAN ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time },
                giftDescription = "-35% stamina/mana cost for the first 60s of combat; can 'Clock Out' to drop a 65% Time-Slow commuter bubble.",
                spellCraftStabilityDiscount = 0.15f, flawDescription = "Unpaid Overtime: Fights lasting longer than 3 minutes inflict +25% fatigue blade lag unless you drink coffee/tea."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Pinpoint, displayWord = "[ PINPOINT ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time },
                giftDescription = "Striking an exact armor seam blooms a micro Time-Slow bubble on that limb and deals 2.5x Piercing Crit.",
                critOrSpecialMultiplier = 2.5f, weaponLagReductionBonus = -0.30f, flawDescription = "Wide horizontal AoE crowd-cleaves lose 40% damage and knockback."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Sponge, displayWord = "[ SPONGE ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Water },
                giftDescription = "Absorbs 30% of incoming elemental projectile impact damage, converting it into instant mana.",
                favoredExpMultiplier = 1.25f, flawDescription = "Physical blunt kinetic impact damage taken increased by 15%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Magnet, displayWord = "[ MAGNET ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Lightning },
                giftDescription = "Dropped metallic loot, coins, and ore shards within a 12m radius magnetically drift toward player.",
                favoredExpMultiplier = 1.25f, flawDescription = "Incoming Lightning damage increased by 20%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Balloon, displayWord = "[ BALLOON ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Wind },
                giftDescription = "Fall velocity halved; enables extended jump apex hang-time and aerial dive maneuvers.",
                favoredExpMultiplier = 1.20f, flawDescription = "Strong wind blasts and boss knockback effects push player 40% farther."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Cactus, displayWord = "[ CACTUS ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Plant },
                giftDescription = "Direct melee attackers suffer 25% reflected piercing thorn retaliation damage.",
                favoredExpMultiplier = 1.25f, flawDescription = "Hydration and drink stamina consumption doubled in desert biomes."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Boomerang, displayWord = "[ BOOMERANG ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Wind },
                giftDescription = "Thrown blades and projectiles loop backward, automatically striking enemy rear flank weak spots.",
                critOrSpecialMultiplier = 1.5f, flawDescription = "Initial linear forward projectile speed reduced by 25%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Wax, displayWord = "[ WAX ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Protection },
                giftDescription = "Forms a hardened candle glaze over player armor that completely negates the first lethal blow.",
                favoredExpMultiplier = 1.30f, flawDescription = "Fire attacks melt the glaze, inflicting 20% vulnerability for 4 seconds."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Static, displayWord = "[ STATIC ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Lightning },
                giftDescription = "Continuous movement builds up kinetic sparks that discharge 150% shock damage on next strike.",
                critOrSpecialMultiplier = 1.5f, flawDescription = "Standing still for longer than 2.0 seconds dissipates stored charge."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Crab, displayWord = "[ CRAB ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Water, AffinityElement.Protection },
                giftDescription = "Lateral strafe speed increased by 50%; side armor plating deflects 35% incoming damage.",
                favoredExpMultiplier = 1.25f, flawDescription = "Forward sprint startup acceleration reduced by 15%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Zipper, displayWord = "[ ZIPPER ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Void },
                giftDescription = "Instantaneous weapon and grimoire switching with zero recovery delay.",
                weaponLagReductionBonus = -0.20f, flawDescription = "Equipment durability drains 10% faster during frequent stance swaps."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Doll, displayWord = "[ DOLL ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.ArcaneKnowledge },
                giftDescription = "Substitutes an animated cloth puppet decoy on fatal damage, avoiding death once per day.",
                favoredExpMultiplier = 1.30f, flawDescription = "Max Health permanently reduced by a flat 10%."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Salt, displayWord = "[ SALT ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Protection },
                giftDescription = "Purifies cursed terrain; deals 2.0x Pure/Holy damage against ethereal specters and undead.",
                critOrSpecialMultiplier = 2.0f, flawDescription = "Consuming acidic potions or poisons inflicts accelerated corrosion."
            });

            masterTalentRegistry.Add(new DoubleEdgedTalentDefinition {
                wordId = TalentWordID.Pendulum, displayWord = "[ PENDULUM ]", isWildcardRandomPool = true,
                grantedAffinities = new List<AffinityElement> { AffinityElement.Time },
                giftDescription = "Rhythmic combat cadence oscillates between +30% Attack Power and +30% Damage Mitigation every 4s.",
                critOrSpecialMultiplier = 1.3f, flawDescription = "Strikes landing off-beat outside the rhythmic pulse deal -15% damage."
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
