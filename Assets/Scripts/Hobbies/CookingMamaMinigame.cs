using System;
using System.Collections;
using UnityEngine;

namespace BattleSummoner.Hobbies
{
    public enum CookingStep { Idle, Chopping, Stirring, PanFlip, Done }

    /// <summary>
    /// Data payload for a prepared or cooked meal, consumed from the quick-access belt carousel
    /// to instantly restore health and apply ticking mana regeneration over duration.
    /// </summary>
    [Serializable]
    public class CookedMealData
    {
        public string mealId = "meal_campfire_stew";
        public string mealName = "Hearty Campfire Stew";
        public string description = "Warm and fragrant stew simmered over crackling embers.";
        public float totalHealthRestore = 220f;
        public float manaRegenPerSecond = 12f;
        public float buffDurationSeconds = 20f;
        public Sprite mealIcon;
        public int qualityRating = 3; // 1 to 5 stars
        public string qualityTierName = "Delicious";
        public Color themeColor = new Color(1.0f, 0.65f, 0.20f);
    }

    /// <summary>
    /// CookingMamaMinigame provides an interactive, rhythmic culinary minigame
    /// featuring chopping, heat regulation / skillet stirring, and seasoning / pan flipping.
    /// Outputs CookedMealData items directly to the player's belt carousel
    /// and awards Cooking XP to HobbyMasteryManager.
    /// </summary>
    public class CookingMamaMinigame : MonoBehaviour
    {
        public static CookingMamaMinigame Instance { get; private set; }

        [Header("Margo Blueprint Step Flow")]
        public CookingStep currentStep = CookingStep.Idle;
        public string recipeName = "Spire Monster Stew";
        public float baseHeal = 100f;
        public float baseManaRegen = 5f;

        private int targetCuts = 5;
        private int currentCuts = 0;
        private float chopScore = 0f;
        private float stirProgress = 0f;
        private float stirScore = 0f;
        private float flipScore = 0f;

        [Header("Legacy Stage Aliasing")]
        public float sessionProgress = 0f;
        public float currentHeat = 50f;
        public float optimalHeatMin = 40f;
        public float optimalHeatMax = 70f;
        public int perfectStrikes = 0;
        public int totalSteps = 0;

        [SerializeField] private CookedMealData activeRecipeTemplate;

        public event Action<CookingStep> OnStepChanged;
        public event Action<CookedMealData> OnCookingCompleted;

        private Coroutine activeMinigameRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void StartCooking(string recipe, float healBase, float manaBase)
        {
            recipeName = recipe;
            baseHeal = healBase;
            baseManaRegen = manaBase;
            currentCuts = 0;
            chopScore = 0f;
            stirProgress = 0f;
            stirScore = 0f;
            flipScore = 0f;
            currentStep = CookingStep.Chopping;

            activeRecipeTemplate = CreatePresetMeal($"meal_{recipe.ToLower().Replace(' ', '_')}", recipe, healBase, manaBase, 60f);
            OnStepChanged?.Invoke(currentStep);
            Debug.Log($"🍳 [COOKING MAMA] Starting preparation for '{recipeName}'!");
        }

        public void StartCooking(CookedMealData recipeTemplate = null)
        {
            activeRecipeTemplate = recipeTemplate ?? CreatePresetMeal("meal_campfire_stew", "Hearty Campfire Stew", 240f, 15f, 25f);
            StartCooking(activeRecipeTemplate.mealName, activeRecipeTemplate.totalHealthRestore, activeRecipeTemplate.manaRegenPerSecond);
        }

        public void RegisterIngredientChop(float cutAccuracy01)
        {
            if (currentStep != CookingStep.Chopping) return;
            chopScore += Mathf.Clamp01(cutAccuracy01) / targetCuts;
            currentCuts++;
            totalSteps++;
            perfectStrikes++;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("slash_heavy", 0.7f);
            }

            if (currentCuts >= targetCuts)
            {
                currentStep = CookingStep.Stirring;
                OnStepChanged?.Invoke(currentStep);
            }
        }

        public void RegisterSkilletStir(float dragDelta, float smoothness01)
        {
            if (currentStep != CookingStep.Stirring) return;
            stirProgress += dragDelta;
            stirScore = Mathf.Lerp(stirScore, Mathf.Clamp01(smoothness01), 0.2f);
            totalSteps++;

            if (stirProgress >= 100f)
            {
                currentStep = CookingStep.PanFlip;
                OnStepChanged?.Invoke(currentStep);
            }
        }

        public void RegisterPanFlip(float flipTimingAccuracy01)
        {
            if (currentStep != CookingStep.PanFlip) return;
            flipScore = Mathf.Clamp01(flipTimingAccuracy01);
            currentStep = CookingStep.Done;
            OnStepChanged?.Invoke(currentStep);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.8f);
            }

            float finalScore = (chopScore + stirScore + flipScore) / 3f;
            var meal = HobbyMasteryManager.Instance.CreateCookedFood(recipeName, baseHeal, baseManaRegen, finalScore);

            // Add directly to QuickItemCarouselController
            if (QuickItemCarouselController.Instance != null)
            {
                QuickItemCarouselController.Instance.AddCookedMeal(meal, 2);
            }

            OnCookingCompleted?.Invoke(meal);
        }

        public void OnPlayerActionTap()
        {
            switch (currentStep)
            {
                case CookingStep.Chopping:
                    RegisterIngredientChop(1.0f);
                    break;
                case CookingStep.Stirring:
                    RegisterSkilletStir(25f, 1.0f);
                    break;
                case CookingStep.PanFlip:
                    RegisterPanFlip(1.0f);
                    break;
            }
        }

        public CookedMealData FinalizeCookedMeal()
        {
            if (activeRecipeTemplate == null)
            {
                activeRecipeTemplate = CreatePresetMeal("meal_campfire_stew", "Hearty Campfire Stew", 240f, 15f, 25f);
            }

            currentStep = CookingStep.Done;
            float performanceRatio = (totalSteps > 0) ? Mathf.Clamp01((float)perfectStrikes / Mathf.Max(1, totalSteps)) : 0.85f;
            float cookingMastery = HobbyMasteryManager.Instance.GetCookingMasteryMultiplier();

            int stars = Mathf.Clamp(Mathf.RoundToInt(performanceRatio * 5f), 1, 5);
            string tierName = stars switch
            {
                5 => "Masterpiece",
                4 => "Delicious",
                3 => "Hearty",
                2 => "Standard",
                _ => "Crispy"
            };

            float healthRestore = (activeRecipeTemplate.totalHealthRestore * (0.7f + 0.3f * performanceRatio)) * cookingMastery;
            float manaRegen = (activeRecipeTemplate.manaRegenPerSecond * (0.8f + 0.2f * performanceRatio)) * cookingMastery;
            float duration = activeRecipeTemplate.buffDurationSeconds * (0.85f + 0.15f * performanceRatio) * cookingMastery;

            CookedMealData finalMeal = new CookedMealData
            {
                mealId = activeRecipeTemplate.mealId,
                mealName = $"{tierName} {activeRecipeTemplate.mealName}",
                description = $"{activeRecipeTemplate.description} (Quality: {stars}★ {tierName})",
                totalHealthRestore = Mathf.Round(healthRestore),
                manaRegenPerSecond = Mathf.Round(manaRegen * 10f) / 10f,
                buffDurationSeconds = Mathf.Round(duration),
                mealIcon = activeRecipeTemplate.mealIcon,
                qualityRating = stars,
                qualityTierName = tierName,
                themeColor = activeRecipeTemplate.themeColor
            };

            float xpGain = 35f * stars;
            HobbyMasteryManager.Instance.AddCookingExp(xpGain);

            if (QuickItemCarouselController.Instance != null)
            {
                QuickItemCarouselController.Instance.AddCookedMeal(finalMeal, 2);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(
                    $"<color=#FFA500>[DISH SERVED: {finalMeal.mealName.ToUpper()}]</color>\n" +
                    $"❤️ +{finalMeal.totalHealthRestore:F0} HP | 💧 +{finalMeal.manaRegenPerSecond:F1} MP/s for {finalMeal.buffDurationSeconds:F0}s\n" +
                    $"Added to Belt Carousel! (+{xpGain:F0} Cooking EXP)"
                );
            }

            Debug.Log($"🍲 [COOKING COMPLETE] {finalMeal.mealName} prepared! +{finalMeal.totalHealthRestore} HP, +{finalMeal.manaRegenPerSecond} MP/s x{finalMeal.buffDurationSeconds}s.");
            OnCookingCompleted?.Invoke(finalMeal);
            return finalMeal;
        }

        public static CookedMealData CreatePresetMeal(string id, string name, float hpRestore, float mpRegenPerSec, float durationSec)
        {
            return new CookedMealData
            {
                mealId = id,
                mealName = name,
                description = $"Restores +{hpRestore:F0} HP and +{mpRegenPerSec:F1} MP/sec for {durationSec:F0}s.",
                totalHealthRestore = hpRestore,
                manaRegenPerSecond = mpRegenPerSec,
                buffDurationSeconds = durationSec,
                qualityRating = 4,
                qualityTierName = "Delicious",
                themeColor = new Color(1.0f, 0.68f, 0.22f)
            };
        }
    }
}
