using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Core;
using BattleSummoner.Combat;

namespace BattleSummoner.World
{
    [Serializable]
    public class PatentedPlayerInvention
    {
        public string patentId;
        public string inventionTitle;
        public string sharedSkillOrRecipeId;
        public string inventorAvatarName;
        public string inventorGuardianSignature;
        public int learnOrCraftFeeCores = 150;
        public int totalTimesLearnedByOtherPlayers = 0;
        public int totalRoyaltiesEarned = 0;
    }

    public enum EnvironmentalHazardType
    {
        HangingCeilingBoulder,
        ParalyzingVinePillar,
        ExplosiveOilBarrel,
        FlashbugSwarmNode,
        GeyserSteamVent
    }

    [Serializable]
    public class MonsterHunterHazardNode : MonoBehaviour
    {
        public string hazardName = "Suspended Cavern Boulder";
        public EnvironmentalHazardType hazardType = EnvironmentalHazardType.HangingCeilingBoulder;
        public float baseEnvironmentalDamage = 850f;
        public float knockdownStunDuration = 6.5f;
        public bool isTriggered = false;

        public void TriggerHazard(string triggeredByMethod, out string ingenuityLog)
        {
            if (isTriggered)
            {
                ingenuityLog = $"{hazardName} was already triggered.";
                return;
            }

            isTriggered = true;
            ingenuityLog = $"[MONSTER HUNTER HAZARD!] Triggered '{hazardName}' via {triggeredByMethod}! Dealt {baseEnvironmentalDamage:F0} True Environmental Damage + {knockdownStunDuration:F1}s Boss Knockdown (+25% Gourmet Part Drop Bonus)!";
            Debug.Log(ingenuityLog);
        }
    }

    public enum ChestOpenMethod
    {
        Inspect3D,
        TactilePicklock,
        Spell_FireMeltLock,
        Spell_AcidDissolveLock,
        Spell_IceBrittleShatter,
        Spell_ArcaneUnlockRune,
        BruteForceSmashOpen
    }

    [Serializable]
    public class ChestLootItem
    {
        public string itemName;
        public bool isFragileGlassOrRelic;
        public int manaCoreValue;
        public bool wasDamagedDuringSmash;
    }

    /// <summary>
    /// Section 11: Creativity & Ingenuity Rewards: Royal Patent Office (Skill/Blueprint Sharing),
    /// Monster Hunter Environmental Hazards & Multi-Solution Locked Chests.
    /// </summary>
    public class PlayerIngenuityAndInteractiveWorldSystem : MonoBehaviour
    {
        public static PlayerIngenuityAndInteractiveWorldSystem Instance { get; private set; }

        [Header("1. Global Patent & Skill-Sharing Codex")]
        public List<PatentedPlayerInvention> globalPatentedSkillsAndBlueprints = new List<PatentedPlayerInvention>();
        [Range(0.05f, 0.30f)] public float inventorRoyaltyShare = 0.15f;

        public event Action<PatentedPlayerInvention, int> OnPatentRoyaltyPaidToInventor;
        public event Action<string, List<ChestLootItem>> OnLockedChestOpened;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public PatentedPlayerInvention RegisterNewPlayerPatent(
            string inventionTitle,
            string skillOrRecipeId,
            string inventorName,
            string guardianSignature,
            int feeInCores = 150)
        {
            var existing = globalPatentedSkillsAndBlueprints.Find(p => p.sharedSkillOrRecipeId == skillOrRecipeId);
            if (existing != null)
            {
                Debug.LogWarning($"[Patent Office] '{inventionTitle}' is already patented by {existing.inventorAvatarName}!");
                return existing;
            }

            var newPatent = new PatentedPlayerInvention
            {
                patentId = $"PATENT_{globalPatentedSkillsAndBlueprints.Count + 101}",
                inventionTitle = inventionTitle,
                sharedSkillOrRecipeId = skillOrRecipeId,
                inventorAvatarName = inventorName,
                inventorGuardianSignature = guardianSignature,
                learnOrCraftFeeCores = feeInCores
            };

            globalPatentedSkillsAndBlueprints.Add(newPatent);
            SystemBroadcastManager.Instance?.DispatchBroadcast(
                $"[ROYAL PATENT REGISTERED] {inventorName} patented [{inventionTitle}]! All players can now learn this skill at the Guild Codex!", true);

            return newPatent;
        }

        public bool LearnSharedSkillFromPatentCodex(string patentId, string learnerPlayerName, ref int learnerManaCores, ref int inventorBankCores)
        {
            var patent = globalPatentedSkillsAndBlueprints.Find(p => p.patentId == patentId);
            if (patent == null || learnerManaCores < patent.learnOrCraftFeeCores) return false;

            learnerManaCores -= patent.learnOrCraftFeeCores;
            int royaltyPayout = Mathf.Max(1, Mathf.RoundToInt(patent.learnOrCraftFeeCores * inventorRoyaltyShare));
            inventorBankCores += royaltyPayout;

            patent.totalTimesLearnedByOtherPlayers++;
            patent.totalRoyaltiesEarned += royaltyPayout;

            MonsterResearchAndInfiniteWeaponSystem.Instance?.UnlockSkillOrPerformActionOnTheFly(
                patent.sharedSkillOrRecipeId,
                patent.inventionTitle,
                25f);

            OnPatentRoyaltyPaidToInventor?.Invoke(patent, royaltyPayout);
            Debug.Log($"[Skill Shared!] '{learnerPlayerName}' learned [{patent.inventionTitle}] created by '{patent.inventorAvatarName}'! Inventor received +{royaltyPayout} Mana Cores in royalties!");
            return true;
        }

        public List<ChestLootItem> InteractWithLockedChestPrompt(
            string chestName,
            ChestOpenMethod chosenMethod,
            List<ChestLootItem> chestContents,
            int playerDexterity = 15)
        {
            switch (chosenMethod)
            {
                case ChestOpenMethod.Inspect3D:
                    Debug.Log($"[Locked Chest '{chestName}'] Opened in 3D Relic Inspector! Player can rotate 360° to check for Mimic teeth, traps, or lock weak-points.");
                    return null;

                case ChestOpenMethod.TactilePicklock:
                    Debug.Log($"[Locked Chest '{chestName}'] Picklocked cleanly (DEX {playerDexterity})! 100% of items recovered in pristine condition!");
                    break;

                case ChestOpenMethod.Spell_FireMeltLock:
                    Debug.Log($"[Locked Chest '{chestName}'] Creative Spell Cast: FIRE! Melted the iron padlock into slag and swung the lid open!");
                    break;

                case ChestOpenMethod.Spell_AcidDissolveLock:
                    Debug.Log($"[Locked Chest '{chestName}'] Creative Spell Cast: ACID! Dissolved the brass lock and hinges silently with zero impact shock!");
                    break;

                case ChestOpenMethod.Spell_IceBrittleShatter:
                    Debug.Log($"[Locked Chest '{chestName}'] Creative Spell Cast: ICE! Froze the lock cylinder brittle and snapped it clean off!");
                    break;

                case ChestOpenMethod.Spell_ArcaneUnlockRune:
                    Debug.Log($"[Locked Chest '{chestName}'] Creative Spell Cast: UNLOCK RUNE! Unsealed the arcane tumbler effortlessly!");
                    break;

                case ChestOpenMethod.BruteForceSmashOpen:
                    Debug.LogWarning($"[Locked Chest '{chestName}'] SMASHED OPEN with brute force! Checking if fragile goods inside survived the impact...");
                    foreach (var item in chestContents)
                    {
                        if (item.isFragileGlassOrRelic && UnityEngine.Random.value < 0.40f)
                        {
                            item.wasDamagedDuringSmash = true;
                            item.itemName = $"Damaged/Cracked {item.itemName}";
                            item.manaCoreValue = Mathf.RoundToInt(item.manaCoreValue * 0.35f);
                            Debug.LogWarning($" -> [Loot Damaged!] '{item.itemName}' was partially crushed by smashing the chest! (Value reduced to {item.manaCoreValue} Cores)");
                        }
                    }
                    break;
            }

            OnLockedChestOpened?.Invoke(chestName, chestContents);
            return chestContents;
        }
    }
}
