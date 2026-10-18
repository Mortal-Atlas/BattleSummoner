using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Hobbies
{
    [Serializable]
    public struct MonsterIngredient
    {
        public string ingredientName;
        public string monsterOriginSpecies;
        public float baseHealContribution;
        public float baseManaRegenContribution;
        public bool isGourmetCut; // True if harvested cleanly via Weak-Spot or High Dexterity
    }

    [Serializable]
    public struct CookedMealOutput
    {
        public string dishName;
        public float totalHealthRestore;
        public float manaRegenPerSecond;
        public float buffDurationSeconds;
        public string realWorldFoodTriviaTitle;
        [TextArea] public string realWorldFoodTriviaBody;
    }

    [Serializable]
    public struct ForgedSwordOutput
    {
        public string swordName;
        public float finalBaseDamage;
        public float finalMaxDurability;
        public int maxEnchantmentSockets;
        public string makerGuardianSignature;
    }

    /// <summary>
    /// Hobby Mastery: Cooking Mama Minigame, Delicious in Dungeon Potluck & Blacksmithing.
    /// Higher Cooking -> Greater health/mana regen and God-Tier feast buffs with real-world culinary trivia.
    /// Higher Blacksmithing -> Higher base damage, durability, and +1 safe enchantment slot every 25 levels
    /// (delaying the 3-Strike Explosion) without musical rhythm tracks—pure physical hammering and quenching.
    /// </summary>
    public class HobbyMasteryAndPotluckEngine : MonoBehaviour
    {
        public static HobbyMasteryAndPotluckEngine Instance { get; private set; }

        [Header("Hobby Mastery Levels")]
        public int cookingLevel = 1;
        public float cookingExp = 0f;
        public int blacksmithLevel = 1;
        public float blacksmithExp = 0f;

        [Header("Co-Op Campfire Potluck Cauldron")]
        public List<MonsterIngredient> activeCauldronIngredients = new List<MonsterIngredient>();

        public event Action<CookedMealOutput> OnPotluckDishServedWithTrivia;
        public event Action<ForgedSwordOutput> OnSwordForged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public float GetRequiredExp(int level) => 50f * Mathf.Pow(level, 1.8f);

        // --- Co-Op Monster Potluck & Cooking Mama Compiler ---
        public void AddPlayerChosenIngredientToPot(MonsterIngredient chosenIngredient)
        {
            activeCauldronIngredients.Add(chosenIngredient);
            Debug.Log($"🍲 [POTLUCK CAULDRON] Added {chosenIngredient.ingredientName} ({chosenIngredient.monsterOriginSpecies}). Cauldron size: {activeCauldronIngredients.Count}");
        }

        public CookedMealOutput FinishCookingPotluck(string recipeTitle, float cookingMamaMinigameScore01, string triviaTitle, string triviaFact)
        {
            float baseHealSum = 80f;
            float baseManaSum = 4f;
            int gourmetCount = 0;

            foreach (var ing in activeCauldronIngredients)
            {
                baseHealSum += ing.baseHealContribution;
                baseManaSum += ing.baseManaRegenContribution;
                if (ing.isGourmetCut) gourmetCount++;
            }

            // God-Tier Feast Synergy if everyone coordinated good monster ingredients
            bool isGodTier = activeCauldronIngredients.Count >= 3 && gourmetCount == activeCauldronIngredients.Count;
            float teamSynergyBonus = isGodTier ? 1.75f : 1.0f;

            float skillMult = 1.0f + (cookingLevel * 0.05f); // +5% Heal & Mana Regen per Cooking Level
            float minigameMult = Mathf.Lerp(0.5f, 1.5f, Mathf.Clamp01(cookingMamaMinigameScore01));
            float totalMult = skillMult * minigameMult * teamSynergyBonus;

            string finalName = isGodTier ? $"God-Tier Feast: {recipeTitle}" : $"{recipeTitle} (Lv.{cookingLevel})";

            CookedMealOutput dish = new CookedMealOutput
            {
                dishName = finalName,
                totalHealthRestore = baseHealSum * totalMult,
                manaRegenPerSecond = baseManaSum * totalMult,
                buffDurationSeconds = 90f * skillMult * teamSynergyBonus,
                realWorldFoodTriviaTitle = triviaTitle,
                realWorldFoodTriviaBody = triviaFact
            };

            activeCauldronIngredients.Clear();
            GainCookingExp(30f * minigameMult * teamSynergyBonus);

            if (isGodTier && SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    $"🌟 [CAMPFIRE POTLUCK] Squad achieved God-Tier Feast: {dish.dishName}! +75% Healing & Morale Buff!", true);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.95f);
            }

            OnPotluckDishServedWithTrivia?.Invoke(dish);
            Debug.Log($"🍽️ [POTLUCK SERVED] {dish.dishName}: +{dish.totalHealthRestore:F0} HP, +{dish.manaRegenPerSecond:F1} MP/s for {dish.buffDurationSeconds:F0}s.");
            return dish;
        }

        private void GainCookingExp(float amt)
        {
            cookingExp += amt;
            while (cookingExp >= GetRequiredExp(cookingLevel))
            {
                cookingExp -= GetRequiredExp(cookingLevel);
                cookingLevel++;
                Debug.Log($"🍳 [COOKING MASTERY UP!] Cooking reached Level {cookingLevel}!");
            }
        }

        // --- Blacksmithing Mastery Compiler (No Rhythm Track Required - Pure Touch & Quench) ---
        public ForgedSwordOutput ForgeSwordOnAnvil(string baseSwordName, float rawDmg, float rawDur, float hammerAndQuenchAccuracy01)
        {
            float dmgMult = 1.0f + (blacksmithLevel * 0.04f); // +4% Damage per Blacksmith Level
            float durMult = 1.0f + (blacksmithLevel * 0.03f); // +3% Durability per Blacksmith Level
            float qualityMult = Mathf.Lerp(0.8f, 1.35f, Mathf.Clamp01(hammerAndQuenchAccuracy01));
            int bonusSockets = blacksmithLevel / 25; // +1 Safe Socket every 25 levels delaying 3-Strike explosion

            string forgedName = $"{baseSwordName} +{blacksmithLevel}";
            string makerSignature = "";

            // Stamp Guardian Signature Maker's Mark if bound
            var guardian = BattleSummoner.Core.GuardianSignatureBinder.Instance;
            if (guardian != null && guardian.HasBoundGuardian())
            {
                makerSignature = guardian.activeContract.guardianPrintedName;
                forgedName = guardian.StampSignatureOnForgedWeapon(forgedName);
            }

            ForgedSwordOutput sword = new ForgedSwordOutput
            {
                swordName = forgedName,
                finalBaseDamage = rawDmg * dmgMult * qualityMult,
                finalMaxDurability = rawDur * durMult * qualityMult,
                maxEnchantmentSockets = 3 + bonusSockets,
                makerGuardianSignature = makerSignature
            };

            // Link into WeaponTemper if present in scene
            var weaponTemper = FindAnyObjectByType<WeaponTemper>();
            if (weaponTemper != null)
            {
                weaponTemper.baseDamage = sword.finalBaseDamage;
                weaponTemper.maxDurability = Mathf.RoundToInt(sword.finalMaxDurability);
                weaponTemper.maxSockets = sword.maxEnchantmentSockets;
            }

            // Link into EnchantmentForgeThreeStrike if present
            var forgeThreeStrike = FindAnyObjectByType<EnchantmentForgeThreeStrike>();
            if (forgeThreeStrike != null)
            {
                forgeThreeStrike.safeCapacitySockets = sword.maxEnchantmentSockets;
            }

            blacksmithExp += 35f * qualityMult;
            while (blacksmithExp >= GetRequiredExp(blacksmithLevel))
            {
                blacksmithExp -= GetRequiredExp(blacksmithLevel);
                blacksmithLevel++;
                Debug.Log($"⚒️ [BLACKSMITH MASTERY UP!] Blacksmithing reached Level {blacksmithLevel}! Safe sockets: {3 + (blacksmithLevel / 25)}");
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 0.9f);
            }

            OnSwordForged?.Invoke(sword);
            Debug.Log($"⚔️ [FORGE COMPLETE] {sword.swordName} | Dmg: {sword.finalBaseDamage:F1} | Dur: {sword.finalMaxDurability:F0} | Sockets: {sword.maxEnchantmentSockets}");
            return sword;
        }
    }
}
