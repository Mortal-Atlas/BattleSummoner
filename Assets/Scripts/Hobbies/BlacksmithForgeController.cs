using System;
using UnityEngine;

namespace BattleSummoner.Hobbies
{
    /// <summary>
    /// Result stats generated when forging and quenching a sword at a campfire or hub anvil.
    /// </summary>
    [Serializable]
    public class ForgedSwordStats
    {
        public string swordName = "Tempered Astral Blade";
        public float baseDamage = 55f;
        public int maxDurability = 180;
        public int maxSockets = 4;
        public float temperHRC = 62.5f;
        public string qualityTier = "Masterwork";

        public float finalBaseDamage => baseDamage;
        public float finalMaxDurability => maxDurability;
        public int maxEnchantmentSockets => maxSockets;
    }

    /// <summary>
    /// BlacksmithForgeController handles interactive anvil hammering, thermal quenching,
    /// and edge tempering at campfires or hub anvils.
    /// Provides QuenchAndFinishSword to overwrite sword stats (baseDamage, maxDurability, maxSockets)
    /// across WeaponTemper, LivingGearSystem, and ItemEnchantmentMatrix upon hammer completion.
    /// </summary>
    public class BlacksmithForgeController : MonoBehaviour
    {
        private static BlacksmithForgeController _instance;
        public static BlacksmithForgeController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<BlacksmithForgeController>();
                }
                return _instance;
            }
        }

        [Header("Margo Blueprint Forge Parameters")]
        public string targetSwordName = "Spire Broadsword";
        public float rawDamage = 80f;
        public float rawDurability = 200f;
        private int requiredHits = 5;
        private int currentHits = 0;
        private float hitAccuracySum = 0f;

        [Header("Anvil / Forge Station Settings")]
        public bool isHubAnvil = true;
        public bool isCampfire = false;
        public float currentMetalHeat = 80f; // 0 to 100

        public event Action<int, int> OnHammerStrike;
        public event Action<ForgedSwordData> OnSwordFinished;
        public event Action<ForgedSwordStats> OnSwordQuenched;

        private void Awake()
        {
            if (_instance == null) _instance = this;
        }

        public void StartForgingSword(string swordName, float baseDmg, float baseDur)
        {
            targetSwordName = swordName;
            rawDamage = baseDmg;
            rawDurability = baseDur;
            currentHits = 0;
            hitAccuracySum = 0f;
            Debug.Log($"🔨 [FORGE] Started forging {targetSwordName} (Base: {rawDamage} DMG, {rawDurability} Durability).");
        }

        public void StrikeAnvilWithHammer(float hitSpotAccuracy01)
        {
            if (currentHits >= requiredHits) return;
            hitAccuracySum += Mathf.Clamp01(hitSpotAccuracy01);
            currentHits++;
            currentMetalHeat = Mathf.Min(100f, currentMetalHeat + 5f);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("slash_heavy", 0.9f);
            }

            Debug.Log($"🔨 [FORGE STRIKE] Hammer hit {currentHits}/{requiredHits} (Accuracy: {hitSpotAccuracy01 * 100:F0}%)");
            OnHammerStrike?.Invoke(currentHits, requiredHits);

            if (currentHits >= requiredHits)
            {
                QuenchAndFinishSword();
            }
        }

        public void PerformHammerStrike(float timingAccuracy = 1.0f) => StrikeAnvilWithHammer(timingAccuracy);

        /// <summary>
        /// Margo Blueprint Quench with ItemEnchantmentMatrix target.
        /// </summary>
        public void QuenchAndFinishSword(ItemEnchantmentMatrix targetMatrix)
        {
            float quality01 = currentHits > 0 ? (hitAccuracySum / requiredHits) : 0.5f;
            ForgedSwordData sword = HobbyMasteryManager.Instance.CreateForgedSword(targetSwordName, rawDamage, rawDurability, quality01);

            if (targetMatrix != null)
            {
                targetMatrix.baseCapacity = sword.maxEnchantmentSockets;
                targetMatrix.currentWeaponDurability = sword.finalMaxDurability;
            }

            // Also overwrite scene WeaponTemper if present
            var weapon = UnityEngine.Object.FindAnyObjectByType<WeaponTemper>();
            if (weapon != null)
            {
                weapon.baseDamage = sword.finalBaseDamage;
                weapon.maxDurability = Mathf.RoundToInt(sword.finalMaxDurability);
                weapon.maxSockets = sword.maxEnchantmentSockets;
            }

            OnSwordFinished?.Invoke(sword);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(
                    $"<color=#FF8C00>[SWORD QUENCHED & FORGED]</color>\n" +
                    $"⚔️ Base DMG: <b>{sword.finalBaseDamage:F0}</b> | Durability: <b>{sword.finalMaxDurability:F0}</b> | Sockets: <b>{sword.maxEnchantmentSockets}</b>"
                );
            }
        }

        /// <summary>
        /// Overload for QuenchAndFinishSword with explicit weapon references.
        /// Overwrites sword baseDamage, maxDurability, and maxSockets based on hammering quality and Blacksmithing mastery.
        /// </summary>
        public static ForgedSwordStats QuenchAndFinishSword(
            WeaponTemper weapon = null,
            LivingGearSystem livingGear = null,
            bool atCampfire = false)
        {
            if (weapon == null)
            {
                weapon = UnityEngine.Object.FindAnyObjectByType<WeaponTemper>();
            }
            if (livingGear == null && weapon != null)
            {
                livingGear = weapon.GetComponent<LivingGearSystem>() ?? UnityEngine.Object.FindAnyObjectByType<LivingGearSystem>();
            }

            var modularManager = ModularEquipmentManager.Instance ?? UnityEngine.Object.FindAnyObjectByType<ModularEquipmentManager>();
            var enchantForge = UnityEngine.Object.FindAnyObjectByType<EnchantmentForge>();
            var threeStrikeForge = UnityEngine.Object.FindAnyObjectByType<EnchantmentForgeThreeStrike>();
            var matrix = ItemEnchantmentMatrix.Instance ?? UnityEngine.Object.FindAnyObjectByType<ItemEnchantmentMatrix>();

            float dmgMultiplier = 1.0f;
            int durabilityBonus = 0;
            int smithLevel = 1;

            if (HobbyMasteryManager.Instance != null)
            {
                dmgMultiplier = HobbyMasteryManager.Instance.GetBlacksmithDamageMultiplier();
                durabilityBonus = HobbyMasteryManager.Instance.GetBlacksmithDurabilityBonus();
                smithLevel = HobbyMasteryManager.Instance.blacksmithLevel;
            }

            float stationMult = atCampfire ? 1.0f : 1.25f;

            float finalBaseDamage = Mathf.Round((45f * stationMult * dmgMultiplier) + (smithLevel * 1.5f));
            int finalMaxDurability = Mathf.RoundToInt((150f * stationMult) + durabilityBonus);

            int minSockets = atCampfire ? 3 : 4;
            int maxSockets = atCampfire ? 4 : 6;
            int rolledSockets = Mathf.Clamp(minSockets + (smithLevel / 10), minSockets, maxSockets);

            string qualityTier = (finalBaseDamage >= 75f) ? "Astral Relic" : (finalBaseDamage >= 60f ? "Masterwork" : "Refined");
            string swordName = livingGear != null ? livingGear.weaponName : "Forged Blade";

            ForgedSwordStats result = new ForgedSwordStats
            {
                swordName = $"{qualityTier} {swordName}",
                baseDamage = finalBaseDamage,
                maxDurability = finalMaxDurability,
                maxSockets = rolledSockets,
                temperHRC = 60f + (smithLevel * 0.25f),
                qualityTier = qualityTier
            };

            if (weapon != null)
            {
                weapon.baseDamage = result.baseDamage;
                weapon.maxDurability = result.maxDurability;
                weapon.maxSockets = result.maxSockets;
                weapon.QuenchCooling(100f);
            }

            if (livingGear != null)
            {
                livingGear.maxTemper = result.maxDurability;
                livingGear.currentTemper = result.maxDurability;
                livingGear.fractureDebt = 0f;
            }

            if (modularManager != null && modularManager.currentMainWeapon != null)
            {
                modularManager.currentMainWeapon.baseDamage = result.baseDamage;
            }

            if (enchantForge != null)
            {
                enchantForge.maxSockets = result.maxSockets;
            }

            if (threeStrikeForge != null)
            {
                threeStrikeForge.safeCapacitySockets = result.maxSockets;
            }

            if (matrix != null)
            {
                matrix.baseCapacity = result.maxSockets;
                matrix.currentWeaponDurability = result.maxDurability;
            }

            float xpGain = atCampfire ? 45f : 85f;
            if (HobbyMasteryManager.Instance != null)
            {
                HobbyMasteryManager.Instance.AddBlacksmithExp(xpGain);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(
                    $"<color=#FF8C00>[SWORD QUENCHED & FORGED]</color>\n" +
                    $"⚔️ Base DMG: <b>{result.baseDamage:F0}</b> | Durability: <b>{result.maxDurability}</b> | Sockets: <b>{result.maxSockets}</b>\n" +
                    $"<color=#FFE082>Blade Grain Sealed & Hardened!</color> (+{xpGain:F0} Smithing EXP)"
                );
            }

            if (Instance != null)
            {
                Instance.currentHits = 0;
                Instance.hitAccuracySum = 0f;
                Instance.OnSwordQuenched?.Invoke(result);
                Instance.OnSwordFinished?.Invoke(new ForgedSwordData
                {
                    swordName = result.swordName,
                    finalBaseDamage = result.baseDamage,
                    finalMaxDurability = result.maxDurability,
                    maxEnchantmentSockets = result.maxSockets,
                    temperHRC = result.temperHRC,
                    qualityTier = result.qualityTier
                });
            }

            Debug.Log($"⚔️ [FORGE COMPLETE] Sword Quenched! Base DMG: {result.baseDamage}, Max Durability: {result.maxDurability}, Max Sockets: {result.maxSockets}.");
            return result;
        }

        public static ForgedSwordStats QuenchAndFinishSword() => QuenchAndFinishSword(null, null, false);
    }
}
