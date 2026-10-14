using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BattleSummoner.Core
{
    [Serializable]
    public class GemSaveEntry
    {
        public MineralRarity rarity;
        public int count;
    }

    [Serializable]
    public class PlayerDataSave
    {
        public string saveVersion = "5.0-Unified";
        public string timestamp;

        // Player Core Progression
        public string playerName = "Wanderer";
        public int currentLevel = 1;
        public double currentExp = 0;
        public float currentHp = 100f;
        public float maxHp = 100f;
        public float currentMp = 50f;
        public float maxMp = 50f;

        // Currency & Mining Vault
        public int totalManaCores = 250;
        public List<GemSaveEntry> gemInventory = new List<GemSaveEntry>();

        // Uncapped Accessory Buffer
        public List<AccessoryItem> equippedAccessories = new List<AccessoryItem>();

        // Quarantine & Tribunal Flags
        public int quarantineStatus = 0; // 0 = Clean, 1 = Quarantined
        public string quarantineViolationReason = "Thermodynamic Limit Override";

        // Living Weapon & Anvil Strain Metrics
        public int weaponMatrixStrain = 0; // Anvil strike counter
        public string prizedWeaponName = "Voracious Scythe";
        public int prizedWeaponEgoIndex = 3;
        public float prizedWeaponSatiety = 75f;
        public float prizedWeaponMaxSatiety = 100f;
        public float prizedWeaponTemper = 100f;
        public float prizedWeaponMaxTemper = 100f;
        public float prizedWeaponFractureDebt = 0f; // Prized weapon durability debt

        // Bardic Melodies & Grimoires
        public List<string> discoveredMelodies = new List<string>();
    }

    /// <summary>
    /// SaveSystem handles persistent local storage for BattleSummoner: The Hundredfold Spire.
    /// Serializes player accessory buffers, QuarantineStatus flags, weapon matrix strain counters,
    /// and Prized Weapon durability debt to JSON at Application.persistentDataPath.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private const string SAVE_FILENAME = "spire_save_data.json";
        private string saveFilePath;

        [Header("Runtime State")]
        [SerializeField] private PlayerDataSave activeSaveData = new PlayerDataSave();
        [SerializeField] private bool autoSaveOnStateChange = true;

        public event Action<PlayerDataSave> OnGameSaved;
        public event Action<PlayerDataSave> OnGameLoaded;

        public PlayerDataSave ActiveSaveData => activeSaveData;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent == null && Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
                saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (HasSaveData())
            {
                LoadGame();
            }
            else
            {
                CaptureCurrentGameState();
                SaveGame();
            }

            HookAutoSaveEvents();
        }

        private void HookAutoSaveEvents()
        {
            var player = FindAnyObjectByType<PlayerProgression>();
            if (player != null)
            {
                player.OnLevelUpEvent += _ => TriggerAutoSave();
            }

            var equip = FindAnyObjectByType<EquipmentSystem>();
            if (equip != null)
            {
                equip.OnAccessoryEquipped += _ => TriggerAutoSave();
            }

            if (GemologySystem.Instance != null)
            {
                GemologySystem.Instance.OnManaCoresChanged += _ => TriggerAutoSave();
            }
        }

        private void TriggerAutoSave()
        {
            if (autoSaveOnStateChange)
            {
                SaveGame();
            }
        }

        private string GetSaveFilePath()
        {
            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
            }
            return saveFilePath;
        }

        public string SaveFilePath => GetSaveFilePath();

        /// <summary>
        /// Gathers active state from all subsystems and serializes to disk.
        /// </summary>
        public bool SaveGame()
        {
            try
            {
                CaptureCurrentGameState();

                string path = GetSaveFilePath();
                string json = JsonUtility.ToJson(activeSaveData, true);
                string tempPath = path + ".tmp";

                File.WriteAllText(tempPath, json);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);

                Debug.Log($"[SaveSystem] Successfully saved Spire state to {path}");
                OnGameSaved?.Invoke(activeSaveData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save game: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads save file from disk and applies all persisted variables to active subsystems.
        /// </summary>
        public bool LoadGame()
        {
            try
            {
                if (!HasSaveData())
                {
                    Debug.LogWarning("[SaveSystem] No save file found to load.");
                    return false;
                }

                string path = GetSaveFilePath();
                string json = File.ReadAllText(path);
                PlayerDataSave loadedData = JsonUtility.FromJson<PlayerDataSave>(json);

                if (loadedData != null)
                {
                    activeSaveData = loadedData;
                    ApplySaveDataToGameSystems(activeSaveData);
                    Debug.Log($"[SaveSystem] Successfully loaded Spire save (Version: {activeSaveData.saveVersion}).");
                    OnGameLoaded?.Invoke(activeSaveData);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load game: {ex.Message}");
                return false;
            }
        }

        public bool HasSaveData()
        {
            if (string.IsNullOrEmpty(saveFilePath))
            {
                saveFilePath = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
            }
            return File.Exists(saveFilePath);
        }

        public void DeleteSaveData()
        {
            try
            {
                if (HasSaveData())
                {
                    File.Delete(saveFilePath);
                }
                activeSaveData = new PlayerDataSave();
                Debug.Log("[SaveSystem] Save data deleted successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save: {ex.Message}");
            }
        }

        /// <summary>
        /// Gathers metrics from PlayerProgression, EquipmentSystem, LivingGearSystem,
        /// EnchantmentForgeThreeStrike, GemologySystem, and TribunalAppealController.
        /// </summary>
        public void CaptureCurrentGameState()
        {
            activeSaveData.timestamp = DateTime.UtcNow.ToString("o");

            // 1. Player Progression
            var player = FindAnyObjectByType<PlayerProgression>();
            if (player != null)
            {
                activeSaveData.playerName = player.playerName;
                activeSaveData.currentLevel = player.currentLevel;
                activeSaveData.currentExp = player.currentExp;
                activeSaveData.currentHp = player.currentHp;
                activeSaveData.maxHp = player.maxHp;
                activeSaveData.currentMp = player.currentMp;
                activeSaveData.maxMp = player.maxMp;
            }

            // 2. Equipment System (Accessory buffer)
            var equip = FindAnyObjectByType<EquipmentSystem>();
            if (equip != null)
            {
                activeSaveData.equippedAccessories = new List<AccessoryItem>(equip.equippedAccessories);
            }

            // 3. Gemology & Currency
            if (GemologySystem.Instance != null)
            {
                activeSaveData.totalManaCores = GemologySystem.Instance.totalManaCores;
                activeSaveData.gemInventory.Clear();
                foreach (var kvp in GemologySystem.Instance.gemInventory)
                {
                    activeSaveData.gemInventory.Add(new GemSaveEntry { rarity = kvp.Key, count = kvp.Value });
                }
            }

            // 4. Living Gear & Durability Debt
            var livingGear = FindAnyObjectByType<LivingGearSystem>();
            if (livingGear != null)
            {
                activeSaveData.prizedWeaponName = livingGear.weaponName;
                activeSaveData.prizedWeaponEgoIndex = livingGear.egoIndex;
                activeSaveData.prizedWeaponSatiety = livingGear.currentSatiety;
                activeSaveData.prizedWeaponMaxSatiety = livingGear.maxSatiety;
                activeSaveData.prizedWeaponTemper = livingGear.currentTemper;
                activeSaveData.prizedWeaponMaxTemper = livingGear.maxTemper;
                activeSaveData.prizedWeaponFractureDebt = livingGear.fractureDebt;
            }

            // 5. Enchantment Matrix Strain Counter
            var forge = FindAnyObjectByType<EnchantmentForgeThreeStrike>();
            if (forge != null)
            {
                activeSaveData.weaponMatrixStrain = forge.strikeCounter;
            }

            // 6. Quarantine Status Flags
            activeSaveData.quarantineStatus = TribunalAppealController.QuarantineStatus;
            activeSaveData.quarantineViolationReason = TribunalAppealController.ViolationReason;
        }

        /// <summary>
        /// Injects loaded data back into active game systems.
        /// </summary>
        public void ApplySaveDataToGameSystems(PlayerDataSave data)
        {
            if (data == null) return;

            // 1. Player Progression
            var player = FindAnyObjectByType<PlayerProgression>();
            if (player != null)
            {
                player.playerName = data.playerName;
                player.currentLevel = data.currentLevel;
                player.currentExp = data.currentExp;
                player.maxHp = data.maxHp > 0f ? data.maxHp : 100f;
                player.currentHp = data.currentHp > 0f ? data.currentHp : player.maxHp;
                player.maxMp = data.maxMp > 0f ? data.maxMp : 50f;
                player.currentMp = data.currentMp > 0f ? data.currentMp : player.maxMp;
            }

            // 2. Equipment System
            var equip = FindAnyObjectByType<EquipmentSystem>();
            if (equip != null && data.equippedAccessories != null)
            {
                equip.equippedAccessories = new List<AccessoryItem>(data.equippedAccessories);
            }

            // 3. Gemology & Currency
            if (GemologySystem.Instance != null)
            {
                GemologySystem.Instance.totalManaCores = data.totalManaCores;
                if (data.gemInventory != null)
                {
                    foreach (var entry in data.gemInventory)
                    {
                        GemologySystem.Instance.gemInventory[entry.rarity] = entry.count;
                    }
                }
            }

            // 4. Living Gear & Durability Debt
            var livingGear = FindAnyObjectByType<LivingGearSystem>();
            if (livingGear != null)
            {
                livingGear.weaponName = data.prizedWeaponName;
                livingGear.egoIndex = data.prizedWeaponEgoIndex;
                livingGear.currentSatiety = data.prizedWeaponSatiety;
                livingGear.maxSatiety = data.prizedWeaponMaxSatiety;
                livingGear.currentTemper = data.prizedWeaponTemper;
                livingGear.maxTemper = data.prizedWeaponMaxTemper;
                livingGear.fractureDebt = data.prizedWeaponFractureDebt;
            }

            // 5. Enchantment Matrix Strain Counter
            var forge = FindAnyObjectByType<EnchantmentForgeThreeStrike>();
            if (forge != null)
            {
                forge.strikeCounter = data.weaponMatrixStrain;
            }

            // 6. Quarantine Status Flags
            TribunalAppealController.QuarantineStatus = data.quarantineStatus;
            TribunalAppealController.ViolationReason = data.quarantineViolationReason;
        }
    }
}
