using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Hobbies
{
    /// <summary>
    /// HobbyType defines the life-sim and crafting disciplines available in BattleSummoner.
    /// </summary>
    public enum HobbyType
    {
        Cooking,
        Blacksmithing,
        Fishing,
        Alchemy,
        Gardening
    }

    /// <summary>
    /// Tracks experience, levels, titles, and gameplay bonuses for a specific hobby.
    /// </summary>
    [Serializable]
    public class HobbyProgress
    {
        public HobbyType hobby;
        public int currentLevel = 1;
        public float currentExp = 0f;
        public float maxExp = 100f;
        public string masteryTitle = "Novice";

        public float ExpRatio => maxExp > 0f ? Mathf.Clamp01(currentExp / maxExp) : 0f;

        public void AddExp(float amount, Action<HobbyType, int, string> onLevelUpCallback)
        {
            currentExp += amount;
            while (currentExp >= maxExp)
            {
                currentExp -= maxExp;
                currentLevel++;
                maxExp = CalculateMaxExp(currentLevel);
                masteryTitle = GetTitleForLevel(hobby, currentLevel);
                onLevelUpCallback?.Invoke(hobby, currentLevel, masteryTitle);
            }
        }

        private static float CalculateMaxExp(int lvl)
        {
            return 50f * Mathf.Pow(lvl, 1.8f);
        }

        public static string GetTitleForLevel(HobbyType type, int lvl)
        {
            if (lvl >= 50) return $"Grandmaster {type}";
            if (lvl >= 30) return $"Master {type}";
            if (lvl >= 20) return $"Artisan {type}";
            if (lvl >= 10) return $"Journeyman {type}";
            if (lvl >= 5)  return $"Apprentice {type}";
            return $"Novice {type}";
        }
    }

    [Serializable]
    public struct ForgedSwordData
    {
        public string swordName;
        public float finalBaseDamage;
        public float finalMaxDurability;
        public int maxEnchantmentSockets;

        public float baseDamage => finalBaseDamage;
        public int maxDurability => Mathf.RoundToInt(finalMaxDurability);
        public int maxSockets => maxEnchantmentSockets;
        public float temperHRC;
        public string qualityTier;
    }

    /// <summary>
    /// HobbyMasteryManager maintains masteries, levels, experience, and perk bonuses
    /// across Cooking, Blacksmithing, Fishing, Alchemy, and Gardening.
    /// </summary>
    public class HobbyMasteryManager : MonoBehaviour
    {
        private static HobbyMasteryManager _instance;
        public static HobbyMasteryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<HobbyMasteryManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("HobbyMasteryManager");
                        _instance = go.AddComponent<HobbyMasteryManager>();
                        if (Application.isPlaying)
                        {
                            UnityEngine.Object.DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        [Header("Hobby Masteries")]
        [SerializeField] private List<HobbyProgress> masteries = new List<HobbyProgress>();

        // Margo Blueprint Direct Properties
        public int cookingLevel
        {
            get => GetLevel(HobbyType.Cooking);
            set => GetProgress(HobbyType.Cooking).currentLevel = value;
        }

        public float cookingExp
        {
            get => GetExp(HobbyType.Cooking);
            set => GetProgress(HobbyType.Cooking).currentExp = value;
        }

        public int blacksmithLevel
        {
            get => GetLevel(HobbyType.Blacksmithing);
            set => GetProgress(HobbyType.Blacksmithing).currentLevel = value;
        }

        public float blacksmithExp
        {
            get => GetExp(HobbyType.Blacksmithing);
            set => GetProgress(HobbyType.Blacksmithing).currentExp = value;
        }

        public event Action<int> OnCookingLevelUp;
        public event Action<int> OnBlacksmithLevelUp;
        public event Action<HobbyType, int, float, float> OnHobbyExpGained;
        public event Action<HobbyType, int, string> OnHobbyLevelUp;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                if (transform.parent == null && Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }

            InitializeMasteriesIfEmpty();
        }

        private void InitializeMasteriesIfEmpty()
        {
            if (masteries == null || masteries.Count == 0)
            {
                masteries = new List<HobbyProgress>
                {
                    new HobbyProgress { hobby = HobbyType.Cooking, currentLevel = 1, maxExp = 100f, masteryTitle = "Novice Cook" },
                    new HobbyProgress { hobby = HobbyType.Blacksmithing, currentLevel = 1, maxExp = 100f, masteryTitle = "Novice Smith" },
                    new HobbyProgress { hobby = HobbyType.Fishing, currentLevel = 1, maxExp = 100f, masteryTitle = "Novice Angler" },
                    new HobbyProgress { hobby = HobbyType.Alchemy, currentLevel = 1, maxExp = 100f, masteryTitle = "Novice Alchemist" },
                    new HobbyProgress { hobby = HobbyType.Gardening, currentLevel = 1, maxExp = 100f, masteryTitle = "Novice Botanist" }
                };
            }
        }

        public HobbyProgress GetProgress(HobbyType hobby)
        {
            InitializeMasteriesIfEmpty();
            var prog = masteries.Find(m => m.hobby == hobby);
            if (prog == null)
            {
                prog = new HobbyProgress
                {
                    hobby = hobby,
                    currentLevel = 1,
                    maxExp = 100f,
                    masteryTitle = HobbyProgress.GetTitleForLevel(hobby, 1)
                };
                masteries.Add(prog);
            }
            return prog;
        }

        public int GetLevel(HobbyType hobby) => GetProgress(hobby).currentLevel;
        public float GetExp(HobbyType hobby) => GetProgress(hobby).currentExp;
        public float GetMaxExp(HobbyType hobby) => GetProgress(hobby).maxExp;
        public float GetRequiredExp(int level) => 50f * Mathf.Pow(level, 1.8f);

        public void AddCookingExp(float amount) => AddExp(HobbyType.Cooking, amount);
        public void AddBlacksmithExp(float amount) => AddExp(HobbyType.Blacksmithing, amount);

        /// <summary>
        /// Awards experience to a specific hobby and triggers level-up telemetry and broadcasts.
        /// </summary>
        public void AddExp(HobbyType hobby, float amount)
        {
            if (amount <= 0f) return;

            var prog = GetProgress(hobby);
            prog.AddExp(amount, (h, lvl, title) =>
            {
                HandleLevelUp(h, lvl, title);
            });

            OnHobbyExpGained?.Invoke(hobby, prog.currentLevel, prog.currentExp, prog.maxExp);
            Debug.Log($"🌟 [HOBBY MASTERY] +{amount:F0} EXP to {hobby}! Level: {prog.currentLevel} ({prog.currentExp:F0}/{prog.maxExp:F0})");
        }

        private void HandleLevelUp(HobbyType hobby, int newLevel, string title)
        {
            Debug.Log($"🎉 [HOBBY LEVEL UP] {hobby} attained Level {newLevel}! Title: {title}");
            OnHobbyLevelUp?.Invoke(hobby, newLevel, title);

            if (hobby == HobbyType.Cooking) OnCookingLevelUp?.Invoke(newLevel);
            if (hobby == HobbyType.Blacksmithing) OnBlacksmithLevelUp?.Invoke(newLevel);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#FFD700>[HOBBY LEVEL UP!]</color>\n{hobby} reached Level {newLevel}!\n<color=#A0E0FF>{title}</color>");
            }

            if (newLevel % 5 == 0 && SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast($"Player unlocked {title} milestone (Level {newLevel})!", true);
            }
        }

        public CookedMealData CreateCookedFood(string baseRecipeName, float baseHeal, float baseManaRegen, float minigameScore01)
        {
            float skillMult = 1.0f + (cookingLevel * 0.05f); // +5% healing & mana regen per Cooking Level
            float perfMult = Mathf.Lerp(0.5f, 1.5f, Mathf.Clamp01(minigameScore01));
            float totalMult = skillMult * perfMult;

            CookedMealData meal = new CookedMealData
            {
                mealId = $"meal_{baseRecipeName.ToLower().Replace(' ', '_')}",
                mealName = $"{baseRecipeName} (Lv.{cookingLevel})",
                description = $"Hearty food prepared with Level {cookingLevel} culinary mastery.",
                totalHealthRestore = Mathf.Round(baseHeal * totalMult),
                manaRegenPerSecond = Mathf.Round(baseManaRegen * totalMult * 10f) / 10f,
                buffDurationSeconds = Mathf.Round(60f * skillMult),
                qualityRating = Mathf.Clamp(Mathf.RoundToInt(perfMult * 3.33f), 1, 5),
                qualityTierName = perfMult >= 1.25f ? "Masterpiece" : (perfMult >= 1.0f ? "Delicious" : "Hearty"),
                themeColor = new Color(1.0f, 0.65f, 0.20f)
            };

            AddCookingExp(25f * perfMult);
            return meal;
        }

        public ForgedSwordData CreateForgedSword(string baseSwordName, float rawDamage, float rawDurability, float forgeQuality01)
        {
            float dmgSkillMult = 1.0f + (blacksmithLevel * 0.04f); // +4% base damage per Blacksmith Level
            float durSkillMult = 1.0f + (blacksmithLevel * 0.03f); // +3% max durability per Blacksmith Level
            float qualityMult = Mathf.Lerp(0.8f, 1.35f, Mathf.Clamp01(forgeQuality01));
            int bonusSockets = blacksmithLevel / 25; // +1 safe enchantment slot every 25 levels

            ForgedSwordData sword = new ForgedSwordData
            {
                swordName = $"{baseSwordName} +{blacksmithLevel}",
                finalBaseDamage = Mathf.Round(rawDamage * dmgSkillMult * qualityMult),
                finalMaxDurability = Mathf.Round(rawDurability * durSkillMult * qualityMult),
                maxEnchantmentSockets = 3 + bonusSockets,
                temperHRC = 60f + (blacksmithLevel * 0.25f),
                qualityTier = qualityMult >= 1.2f ? "Masterwork" : "Refined"
            };

            AddBlacksmithExp(35f * qualityMult);
            return sword;
        }

        public float GetCookingMasteryMultiplier()
        {
            return 1.0f + (cookingLevel - 1) * 0.025f;
        }

        public float GetBlacksmithDamageMultiplier()
        {
            return 1.0f + (blacksmithLevel - 1) * 0.02f;
        }

        public int GetBlacksmithDurabilityBonus()
        {
            return (blacksmithLevel - 1) * 5;
        }
    }
}
