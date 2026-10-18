using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Hobbies;

namespace BattleSummoner.Combat
{
    public enum ImprovisedPropCategory
    {
        Furniture_StoolOrChair,
        ChefTool_WokOrSkillet,
        Cleaning_BroomOrMop,
        StandardBlade,
        FirearmOrMechanism,
        PoleOrStaff,
        GauntletOrClaw
    }

    [Serializable]
    public class DynamicPlayerSkillNode
    {
        public string skillId;
        public string skillDisplayName;
        public int skillLevel = 1;
        public float currentExp = 0f;
        public float bonusDamageMultiplier = 1.0f;

        public float GetRequiredExp() => 25f * Mathf.Pow(skillLevel, 1.55f);
    }

    [Serializable]
    public class WorldWeaponOrPropItem
    {
        public string itemId;
        public string displayName;
        public ImprovisedPropCategory category;
        public float baseDamage = 25f;
        public float throwDamageMultiplier = 1.4f;
        public bool isSharpnessEnchanted = false;
        public List<string> activeEnchantments = new List<string>();
        public GameObject visualPrefab;
    }

    [Serializable]
    public class CapturedMonsterSpecimen
    {
        public string speciesId;
        public string monsterName;
        public int monsterLevel;
        public float researchProgressPercent = 0f;
        public bool isFullyResearched = false;
        public string unlockedTraitOrSkillReward;
    }

    [Serializable]
    public class RefinedWorkbenchRecipe
    {
        public string recipeId;
        public ImprovisedPropCategory inputCategoryA;
        public ImprovisedPropCategory inputCategoryB;
        public string refinedOutputName;
        public float combinedBaseDamage;
        public string specialDualMechanicDescription;
        public GameObject refinedMasterworkPrefab;
    }

    /// <summary>
    /// Section 10: Live Monster Capture & Research, "Anything Is A Weapon" Infinite Skills,
    /// and Refined Artisan Weapon Fusion Workbench (Dead Rising Concept, Masterwork Fantasy Execution).
    /// </summary>
    public class MonsterResearchAndInfiniteWeaponSystem : MonoBehaviour
    {
        public static MonsterResearchAndInfiniteWeaponSystem Instance { get; private set; }

        [Header("1. Live Monster Capture & Town Research Lab")]
        public List<CapturedMonsterSpecimen> researchSanctuarySpecimens = new List<CapturedMonsterSpecimen>();

        [Header("2. Infinite On-The-Fly Dynamic Skill Registry")]
        public Dictionary<string, DynamicPlayerSkillNode> dynamicSkills = new Dictionary<string, DynamicPlayerSkillNode>();
        public WorldWeaponOrPropItem currentlyHeldPropOrWeapon;

        [Tooltip("Incredibly minor Cooking EXP granted per hit when bashing enemies with a Wok or Chef equipment.")]
        public float minorCookingExpPerWokBash = 0.05f;

        [Header("3. Refined Artisan Workbench Recipes (Dead Rising Concept, Sleek Execution)")]
        public List<RefinedWorkbenchRecipe> workbenchFusionCatalog = new List<RefinedWorkbenchRecipe>
        {
            new RefinedWorkbenchRecipe
            {
                recipeId = "Refined_Gunblade",
                inputCategoryA = ImprovisedPropCategory.StandardBlade,
                inputCategoryB = ImprovisedPropCategory.FirearmOrMechanism,
                refinedOutputName = "Sovereign Artisan Gunblade",
                combinedBaseDamage = 145f,
                specialDualMechanicDescription = "Tap during a blade slice to detonate a chambered mana-shell along the edge."
            },
            new RefinedWorkbenchRecipe
            {
                recipeId = "Refined_ShotgunAxe",
                inputCategoryA = ImprovisedPropCategory.PoleOrStaff,
                inputCategoryB = ImprovisedPropCategory.FirearmOrMechanism,
                refinedOutputName = "Highborn Scattergun-Halberd Axe",
                combinedBaseDamage = 165f,
                specialDualMechanicDescription = "Heavy axe cleaves release a close-range concussive buckshot blast."
            },
            new RefinedWorkbenchRecipe
            {
                recipeId = "Refined_WarGlaive",
                inputCategoryA = ImprovisedPropCategory.PoleOrStaff,
                inputCategoryB = ImprovisedPropCategory.StandardBlade,
                refinedOutputName = "Crescent Spire Glaive-Spear",
                combinedBaseDamage = 135f,
                specialDualMechanicDescription = "Wide sweeping polearm arc with armor-piercing thrust tips."
            },
            new RefinedWorkbenchRecipe
            {
                recipeId = "Refined_BearClaws",
                inputCategoryA = ImprovisedPropCategory.GauntletOrClaw,
                inputCategoryB = ImprovisedPropCategory.StandardBlade,
                refinedOutputName = "Artisan Ursine Rend-Claws",
                combinedBaseDamage = 125f,
                specialDualMechanicDescription = "Unlocks 2-finger dual-wield feral tearing combos with stacking bleed."
            },
            new RefinedWorkbenchRecipe
            {
                recipeId = "Refined_RazorWok",
                inputCategoryA = ImprovisedPropCategory.ChefTool_WokOrSkillet,
                inputCategoryB = ImprovisedPropCategory.StandardBlade,
                refinedOutputName = "Gilded Bladed Battle-Wok",
                combinedBaseDamage = 120f,
                specialDualMechanicDescription = "Deflects spells, severs limbs, and grants minor Cooking EXP on every strike!"
            }
        };

        public event Action<DynamicPlayerSkillNode> OnNewSkillCreatedOnTheFly;
        public event Action<CapturedMonsterSpecimen> OnMonsterResearchCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public bool TryCaptureMonsterAlive(
            string speciesId,
            string monsterName,
            int level,
            float currentHp,
            float maxHp,
            bool isAsleepOrParalyzed,
            string unlockedTraitName)
        {
            float hpRatio = currentHp / Mathf.Max(1f, maxHp);
            if (hpRatio > 0.15f && !isAsleepOrParalyzed)
            {
                Debug.LogWarning($"[Monster Capture] '{monsterName}' is too strong ({hpRatio * 100f:F0}% HP)! Weaken below 15% HP or inflict Sleep/Paralyze first.");
                return false;
            }

            CapturedMonsterSpecimen specimen = new CapturedMonsterSpecimen
            {
                speciesId = speciesId,
                monsterName = monsterName,
                monsterLevel = level,
                researchProgressPercent = 0f,
                isFullyResearched = false,
                unlockedTraitOrSkillReward = unlockedTraitName
            };

            researchSanctuarySpecimens.Add(specimen);
            Debug.Log($"[Monster Captured!] Captured live '{monsterName}' (Lv.{level}) and transported to the Town Research Lab!");
            return true;
        }

        public void TickTownMonsterResearch(float researchPointsAdded)
        {
            foreach (var specimen in researchSanctuarySpecimens)
            {
                if (specimen.isFullyResearched) continue;

                specimen.researchProgressPercent = Mathf.Clamp(specimen.researchProgressPercent + researchPointsAdded, 0f, 100f);
                CombatMasteryAndEvolutionSystem.Instance?.RecordBestiaryProgress(specimen.speciesId, researchPointsAdded);

                if (specimen.researchProgressPercent >= 100f)
                {
                    specimen.isFullyResearched = true;
                    UnlockSkillOrPerformActionOnTheFly(
                        $"Research_{specimen.speciesId}",
                        $"Bio-Mastery: {specimen.unlockedTraitOrSkillReward}",
                        50f);

                    OnMonsterResearchCompleted?.Invoke(specimen);
                    Debug.Log($"[Research Complete!] 100% Research on '{specimen.monsterName}'! Unlocked '{specimen.unlockedTraitOrSkillReward}' & Tower Defense Spawner Card!");
                }
            }
        }

        public void PickUpWorldObjectAsWeapon(WorldWeaponOrPropItem prop)
        {
            currentlyHeldPropOrWeapon = prop;
            string autoSkillId = GetSkillIdForPropCategory(prop.category, prop.displayName);
            string autoSkillName = GetSkillDisplayNameForPropCategory(prop.category, prop.displayName);

            EnsureDynamicSkillExists(autoSkillId, autoSkillName);
            Debug.Log($"[Anything Is A Weapon] Picked up '{prop.displayName}'! Active Skill: [{autoSkillName}]");
        }

        public float ExecuteImprovisedWeaponStrikeOrThrow(bool isThrown, out string combatLog)
        {
            if (currentlyHeldPropOrWeapon == null)
            {
                combatLog = "Unarmed strike!";
                return 10f;
            }

            string skillId = GetSkillIdForPropCategory(currentlyHeldPropOrWeapon.category, currentlyHeldPropOrWeapon.displayName);
            string skillName = GetSkillDisplayNameForPropCategory(currentlyHeldPropOrWeapon.category, currentlyHeldPropOrWeapon.displayName);

            DynamicPlayerSkillNode skill = UnlockSkillOrPerformActionOnTheFly(skillId, skillName, 8.5f);

            float damage = currentlyHeldPropOrWeapon.baseDamage * skill.bonusDamageMultiplier;
            if (isThrown) damage *= currentlyHeldPropOrWeapon.throwDamageMultiplier;
            if (currentlyHeldPropOrWeapon.isSharpnessEnchanted) damage *= 1.45f;

            if (currentlyHeldPropOrWeapon.category == ImprovisedPropCategory.ChefTool_WokOrSkillet)
            {
                if (HobbyMasteryAndPotluckEngine.Instance != null)
                {
                    HobbyMasteryAndPotluckEngine.Instance.GainCookingExp(minorCookingExpPerWokBash);
                }
                combatLog = $"Bashed enemy with '{currentlyHeldPropOrWeapon.displayName}' for {damage:F1} dmg! ([{skill.skillDisplayName} Lv.{skill.skillLevel}] +{minorCookingExpPerWokBash} Minor Cooking EXP!)";
            }
            else
            {
                combatLog = $"{(isThrown ? "Threw" : "Swung")} '{currentlyHeldPropOrWeapon.displayName}' for {damage:F1} dmg! ([{skill.skillDisplayName} Lv.{skill.skillLevel}])";
            }

            Debug.Log($"[Improvised Combat] {combatLog}");
            return damage;
        }

        public void EnchantImprovisedObject(WorldWeaponOrPropItem targetProp, string enchantmentTitle, bool grantsRazorSharpness)
        {
            if (targetProp == null) return;
            targetProp.activeEnchantments.Add(enchantmentTitle);
            if (grantsRazorSharpness) targetProp.isSharpnessEnchanted = true;
            targetProp.displayName = $"Enchanted {enchantmentTitle} {targetProp.displayName}";

            Debug.Log($"[Goofy Creative Enchanting] Created '{targetProp.displayName}'! Anything really IS a weapon!");
        }

        public DynamicPlayerSkillNode UnlockSkillOrPerformActionOnTheFly(string actionId, string customSkillTitle, float expGained)
        {
            DynamicPlayerSkillNode node = EnsureDynamicSkillExists(actionId, customSkillTitle);
            node.currentExp += expGained;

            while (node.currentExp >= node.GetRequiredExp())
            {
                node.currentExp -= node.GetRequiredExp();
                node.skillLevel++;
                node.bonusDamageMultiplier = 1.0f + (node.skillLevel * 0.06f);
                Debug.Log($"[Infinite Skill Level Up!] '{node.skillDisplayName}' reached Level {node.skillLevel} (Multiplier: {node.bonusDamageMultiplier:F2}x)!");
            }

            return node;
        }

        private DynamicPlayerSkillNode EnsureDynamicSkillExists(string skillId, string skillTitle)
        {
            if (!dynamicSkills.TryGetValue(skillId, out var node))
            {
                node = new DynamicPlayerSkillNode
                {
                    skillId = skillId,
                    skillDisplayName = skillTitle,
                    skillLevel = 1,
                    currentExp = 0f,
                    bonusDamageMultiplier = 1.0f
                };
                dynamicSkills[skillId] = node;
                OnNewSkillCreatedOnTheFly?.Invoke(node);
                Debug.Log($"[NEW SKILL CREATED ON THE FLY!] Learned Skill: [{skillTitle}] (Lv.1)!");
            }
            return node;
        }

        private string GetSkillIdForPropCategory(ImprovisedPropCategory cat, string itemName)
        {
            return cat switch
            {
                ImprovisedPropCategory.Furniture_StoolOrChair => "Skill_FurnitureCombat",
                ImprovisedPropCategory.ChefTool_WokOrSkillet => "Skill_WokAndChefCombat",
                ImprovisedPropCategory.Cleaning_BroomOrMop => "Skill_SweepingBroomArts",
                _ => $"Skill_{itemName.Replace(" ", "")}Mastery"
            };
        }

        private string GetSkillDisplayNameForPropCategory(ImprovisedPropCategory cat, string itemName)
        {
            return cat switch
            {
                ImprovisedPropCategory.Furniture_StoolOrChair => "Tavern Furniture Combat",
                ImprovisedPropCategory.ChefTool_WokOrSkillet => "Iron Wok & Culinary Bashing",
                ImprovisedPropCategory.Cleaning_BroomOrMop => "Arcane Janitor Broom-Sweeping",
                _ => $"{itemName} Combat Mastery"
            };
        }

        public WorldWeaponOrPropItem CraftRefinedFusionWeaponAtWorkbench(WorldWeaponOrPropItem itemA, WorldWeaponOrPropItem itemB)
        {
            if (itemA == null || itemB == null) return null;

            RefinedWorkbenchRecipe matchedRecipe = workbenchFusionCatalog.Find(r =>
                (r.inputCategoryA == itemA.category && r.inputCategoryB == itemB.category) ||
                (r.inputCategoryA == itemB.category && r.inputCategoryB == itemA.category));

            WorldWeaponOrPropItem craftedMasterwork;

            if (matchedRecipe != null)
            {
                craftedMasterwork = new WorldWeaponOrPropItem
                {
                    itemId = matchedRecipe.recipeId,
                    displayName = matchedRecipe.refinedOutputName,
                    category = itemA.category == ImprovisedPropCategory.ChefTool_WokOrSkillet || itemB.category == ImprovisedPropCategory.ChefTool_WokOrSkillet
                        ? ImprovisedPropCategory.ChefTool_WokOrSkillet
                        : ImprovisedPropCategory.StandardBlade,
                    baseDamage = matchedRecipe.combinedBaseDamage,
                    isSharpnessEnchanted = itemA.isSharpnessEnchanted || itemB.isSharpnessEnchanted,
                    visualPrefab = matchedRecipe.refinedMasterworkPrefab
                };
                Debug.Log($"[Artisan Fusion Workbench] Engineered sleek masterwork: '{craftedMasterwork.displayName}' ({matchedRecipe.specialDualMechanicDescription})!");
            }
            else
            {
                craftedMasterwork = new WorldWeaponOrPropItem
                {
                    itemId = $"Artisan_{itemA.itemId}_{itemB.itemId}",
                    displayName = $"Artisan {itemA.displayName}-{itemB.displayName} Hybrid",
                    category = itemA.category,
                    baseDamage = (itemA.baseDamage + itemB.baseDamage) * 1.15f,
                    isSharpnessEnchanted = true
                };
                Debug.Log($"[Artisan Fusion Workbench] Custom-forged sleek hybrid: '{craftedMasterwork.displayName}' (Base Dmg: {craftedMasterwork.baseDamage:F0})!");
            }

            UnlockSkillOrPerformActionOnTheFly($"Skill_{craftedMasterwork.itemId}", $"{craftedMasterwork.displayName} Mastery", 15f);
            return craftedMasterwork;
        }
    }
}
