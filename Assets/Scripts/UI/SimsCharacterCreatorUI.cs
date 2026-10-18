using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.UI;

/// <summary>
/// SimsCharacterCreatorUI provides a full-featured character studio HUD:
/// dynamic Sims-like body morph sliders, modular wardrobe & arsenal equipping,
/// turntable camera preset focus buttons, and real-time 12-Dimensional telemetry monitoring.
/// </summary>
[ExecuteAlways]
public class SimsCharacterCreatorUI : MonoBehaviour
{
    public static SimsCharacterCreatorUI Instance { get; private set; }

    [Header("Target Character Archetypes")]
    public GameObject maleCharacterInstance;
    public GameObject femaleCharacterInstance;
    public SimsBodyMorphController currentMorphController;
    public ModularEquipmentManager currentEquipmentManager;
    public BattleSummoner.Customization.ModularCharacterAppearance currentModularAppearance;

    [Header("Skin Tone & Armor Dye Palettes")]
    public static readonly Color[] SkinTonePresets = new Color[]
    {
        new Color(1.0f, 0.91f, 0.83f, 1.0f), // Anime Fair / Porcelain
        new Color(0.98f, 0.84f, 0.73f, 1.0f), // Light Peach
        new Color(0.91f, 0.74f, 0.60f, 1.0f), // Warm Beige
        new Color(0.80f, 0.60f, 0.45f, 1.0f), // Tan / Sun-kissed
        new Color(0.55f, 0.38f, 0.28f, 1.0f), // Deep Bronze
        new Color(0.35f, 0.22f, 0.16f, 1.0f)  // Dark Espresso
    };

    public static readonly Color[] ArmorDyePresets = new Color[]
    {
        new Color(0.25f, 0.42f, 0.82f, 1.0f), // Royal Solas Blue
        new Color(0.78f, 0.22f, 0.22f, 1.0f), // Crimson Vanguard
        new Color(0.20f, 0.60f, 0.38f, 1.0f), // Emerald Warden
        new Color(0.85f, 0.68f, 0.18f, 1.0f), // Astral Gold
        new Color(0.48f, 0.25f, 0.68f, 1.0f), // Void Amethyst
        new Color(0.20f, 0.22f, 0.26f, 1.0f)  // Obsidian Knight
    };

    public int selectedSkinIndex = 0;
    public int selectedArmorDyeIndex = 0;

    [Header("Active State")]
    public int activeViewMode = 2; // 0=Male Only, 1=Female Only, 2=Dual View
    public bool isFemaleTarget = false;

    [Header("Camera Turntable")]
    public ShowcaseTurntableController turntableController;

    [Header("Environment Controller")]
    public BattleSummoner.Customization.CharacterCustomizerEnvironmentController environmentController;

    [Header("UI Tabs")]
    public GameObject morphTabPanel;
    public GameObject wardrobeTabPanel;
    public GameObject portraitTabPanel;
    public GameObject telemetryTabPanel;
    public GameObject environmentTabPanel;

    [Header("Battle Side Profile Portrait")]
    public BattleSideProfilePortrait sideProfilePortrait;

    [Header("Environment Tab UI")]
    public Slider timeOfDaySlider;
    public TextMeshProUGUI timeOfDayValueText;
    public Slider sunAzimuthSlider;
    public TextMeshProUGUI sunAzimuthValueText;
    public Slider sunElevationSlider;
    public TextMeshProUGUI sunElevationValueText;
    public Slider sunIntensitySlider;
    public TextMeshProUGUI sunIntensityValueText;
    public Slider fogDensitySlider;
    public TextMeshProUGUI fogDensityValueText;
    public Button timelapseButton;
    public TextMeshProUGUI timelapseButtonText;
    public Button lanternsToggleButton;
    public TextMeshProUGUI lanternsToggleText;
    public Slider nightGlowSlider;
    public TextMeshProUGUI nightGlowValueText;
    public Button runicGlowToggleButton;
    public TextMeshProUGUI runicGlowToggleText;
    public TextMeshProUGUI activeThemeText;

    [Header("Proportion Sliders")]
    public Slider heightSlider;
    public Slider headSlider;
    public Slider shouldersSlider;
    public Slider chestSlider;
    public Slider waistSlider;
    public Slider hipsSlider;
    public Slider armsSlider;
    public Slider legsSlider;
    public Slider muscleSlider;

    [Header("Value Labels")]
    public TextMeshProUGUI heightValueText;
    public TextMeshProUGUI headValueText;
    public TextMeshProUGUI shouldersValueText;
    public TextMeshProUGUI chestValueText;
    public TextMeshProUGUI waistValueText;
    public TextMeshProUGUI hipsValueText;
    public TextMeshProUGUI armsValueText;
    public TextMeshProUGUI legsValueText;
    public TextMeshProUGUI muscleValueText;

    [Header("12D Telemetry UI")]
    public TextMeshProUGUI telemetryDisplayText;

    [Header("Equipment Prefabs")]
    public GameObject helmetPrefab;
    public GameObject torsoPrefab;
    public GameObject leggingsPrefab;
    public GameObject shoesPrefab;
    public GameObject battleAxePrefab;
    public GameObject swordPrefab;
    public GameObject staffPrefab;
    public GameObject wandPrefab;

    [Header("Confirmation & Enter World")]
    public Button enterWorldButton;
    public TMP_InputField monikerInput;
    public string targetCutsceneSceneName = "WorldIntroductionCutscene";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        AutoBindReferences();
    }

    private void Start()
    {
        InitializeSliders();
        SetViewMode(2); // Start in Dual View so user sees both base bodies immediately
        EquipStarterClothingOnBoth(); // Overlay clothing & armor by default on both models
        EnsureProceedButton();
    }

    private void Update()
    {
        Update12DTelemetryDisplay();
        if (environmentController != null && environmentController.isTimelapseActive)
        {
            if (timeOfDaySlider != null)
                timeOfDaySlider.SetValueWithoutNotify(environmentController.currentTimeOfDay);
            if (timeOfDayValueText != null)
                timeOfDayValueText.text = environmentController.GetDigitalClockString();
        }

        // Shortcut: Return/Enter key confirms character creation and proceeds
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            EnterWorldWithCharacter();
        }
    }

    public void AutoBindReferences()
    {
        if (maleCharacterInstance == null) maleCharacterInstance = GameObject.Find("Showcase_Male_Base_Naked");
        if (femaleCharacterInstance == null) femaleCharacterInstance = GameObject.Find("Showcase_Female_Base_Naked");
        if (turntableController == null) turntableController = Camera.main?.GetComponent<ShowcaseTurntableController>();
        if (environmentController == null)
        {
            environmentController = UnityEngine.Object.FindAnyObjectByType<BattleSummoner.Customization.CharacterCustomizerEnvironmentController>();
            if (environmentController == null)
            {
                var stage = GameObject.Find("Studio_ExhibitionStage");
                if (stage != null) environmentController = stage.GetComponent<BattleSummoner.Customization.CharacterCustomizerEnvironmentController>();
            }
        }

        // Auto-load prefabs if not assigned
        if (helmetPrefab == null) helmetPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Helmet");
        if (torsoPrefab == null) torsoPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Torso");
        if (leggingsPrefab == null) leggingsPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Leggings");
        if (shoesPrefab == null) shoesPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Shoes");
        if (swordPrefab == null) swordPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_Sword");
        if (battleAxePrefab == null) battleAxePrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_TwoHanded_BattleAxe");
        if (staffPrefab == null) staffPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_Staff");
        if (wandPrefab == null) wandPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_Wand");

        if (sideProfilePortrait == null)
        {
            sideProfilePortrait = UnityEngine.Object.FindAnyObjectByType<BattleSideProfilePortrait>();
        }

        EnsureProceedButton();
    }

    public void InitializeSliders()
    {
        ConfigureSlider(heightSlider, 0.80f, 1.25f, 1.0f, OnHeightChanged);
        ConfigureSlider(headSlider, 0.80f, 1.30f, 1.0f, OnHeadChanged);
        ConfigureSlider(shouldersSlider, 0.70f, 1.40f, 1.0f, OnShouldersChanged);
        ConfigureSlider(chestSlider, 0.70f, 1.40f, 1.0f, OnChestChanged);
        ConfigureSlider(waistSlider, 0.70f, 1.40f, 1.0f, OnWaistChanged);
        ConfigureSlider(hipsSlider, 0.70f, 1.40f, 1.0f, OnHipsChanged);
        ConfigureSlider(armsSlider, 0.80f, 1.30f, 1.0f, OnArmsChanged);
        ConfigureSlider(legsSlider, 0.80f, 1.30f, 1.0f, OnLegsChanged);
        ConfigureSlider(muscleSlider, 10f, 60f, 25f, OnMuscleChanged);

        ConfigureSlider(timeOfDaySlider, 0f, 24f, 12f, OnTimeOfDayChanged);
        ConfigureSlider(sunAzimuthSlider, 0f, 360f, 160f, OnSunAzimuthChanged);
        ConfigureSlider(sunElevationSlider, -10f, 90f, 45f, OnSunElevationChanged);
        ConfigureSlider(sunIntensitySlider, 0f, 2.5f, 1.0f, OnSunIntensityChanged);
        ConfigureSlider(fogDensitySlider, 0f, 0.04f, 0.012f, OnFogDensityChanged);
        ConfigureSlider(nightGlowSlider, 0f, 3.0f, 1.5f, OnNightGlowChanged);
        SyncEnvironmentUIValues();
    }

    private void ConfigureSlider(Slider s, float min, float max, float def, UnityEngine.Events.UnityAction<float> callback)
    {
        if (s == null) return;
        s.minValue = min;
        s.maxValue = max;
        s.value = def;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(callback);
    }

    public void SetViewMode(int mode)
    {
        activeViewMode = mode;
        if (maleCharacterInstance == null || femaleCharacterInstance == null) return;

        switch (mode)
        {
            case 0: // Male Only Center
                isFemaleTarget = false;
                maleCharacterInstance.SetActive(true);
                femaleCharacterInstance.SetActive(false);
                maleCharacterInstance.transform.position = new Vector3(0, 0, 0);
                if (turntableController != null) turntableController.FocusFullBody();
                SelectTargetCharacter(maleCharacterInstance);
                if (sideProfilePortrait != null) sideProfilePortrait.SetGender(BattlePortraitGender.Masc);
                break;

            case 1: // Female Only Center
                isFemaleTarget = true;
                maleCharacterInstance.SetActive(false);
                femaleCharacterInstance.SetActive(true);
                femaleCharacterInstance.transform.position = new Vector3(0, 0, 0);
                if (turntableController != null) turntableController.FocusFullBody();
                SelectTargetCharacter(femaleCharacterInstance);
                if (sideProfilePortrait != null) sideProfilePortrait.SetGender(BattlePortraitGender.Feme);
                break;

            case 2: // Dual View Side-by-Side
            default:
                maleCharacterInstance.SetActive(true);
                femaleCharacterInstance.SetActive(true);
                maleCharacterInstance.transform.position = new Vector3(-0.55f, 0, 0);
                femaleCharacterInstance.transform.position = new Vector3(0.55f, 0, 0);
                if (turntableController != null)
                {
                    turntableController.focalPoint = new Vector3(-0.15f, 0.95f, 0f);
                    turntableController.distance = 3.3f;
                }
                SelectTargetCharacter(isFemaleTarget ? femaleCharacterInstance : maleCharacterInstance);
                if (sideProfilePortrait != null) sideProfilePortrait.SetGender(isFemaleTarget ? BattlePortraitGender.Feme : BattlePortraitGender.Masc);
                break;
        }
    }

    public void SelectTargetCharacter(GameObject charObj)
    {
        if (charObj == null) return;
        currentMorphController = charObj.GetComponent<SimsBodyMorphController>();
        currentEquipmentManager = charObj.GetComponent<ModularEquipmentManager>();
        currentModularAppearance = charObj.GetComponent<BattleSummoner.Customization.ModularCharacterAppearance>() ?? charObj.GetComponentInChildren<BattleSummoner.Customization.ModularCharacterAppearance>();

        if (currentMorphController != null)
        {
            var p = currentMorphController.proportions;
            if (heightSlider) heightSlider.SetValueWithoutNotify(p.heightScale);
            if (headSlider) headSlider.SetValueWithoutNotify(p.headScale);
            if (shouldersSlider) shouldersSlider.SetValueWithoutNotify(p.shoulderWidth);
            if (chestSlider) chestSlider.SetValueWithoutNotify(p.chestScale);
            if (waistSlider) waistSlider.SetValueWithoutNotify(p.waistWidth);
            if (hipsSlider) hipsSlider.SetValueWithoutNotify(p.hipWidth);
            if (armsSlider) armsSlider.SetValueWithoutNotify(p.armThickness);
            if (legsSlider) legsSlider.SetValueWithoutNotify(p.legThickness);
            if (muscleSlider) muscleSlider.SetValueWithoutNotify(p.muscleMass_4D);

            UpdateLabels();
        }
    }

    public void ToggleTargetGender()
    {
        isFemaleTarget = !isFemaleTarget;
        if (sideProfilePortrait != null)
        {
            sideProfilePortrait.SetGender(isFemaleTarget ? BattlePortraitGender.Feme : BattlePortraitGender.Masc);
        }
        if (activeViewMode == 2)
        {
            SelectTargetCharacter(isFemaleTarget ? femaleCharacterInstance : maleCharacterInstance);
        }
        else
        {
            SetViewMode(isFemaleTarget ? 1 : 0);
        }
    }

    // --- Tab Switching ---
    public void SwitchTab(int tabIndex)
    {
        if (morphTabPanel) morphTabPanel.SetActive(tabIndex == 0);
        if (wardrobeTabPanel) wardrobeTabPanel.SetActive(tabIndex == 1);
        if (portraitTabPanel) portraitTabPanel.SetActive(tabIndex == 2);
        if (telemetryTabPanel) telemetryTabPanel.SetActive(tabIndex == 3);
        if (environmentTabPanel) environmentTabPanel.SetActive(tabIndex == 4);
        if (tabIndex == 3) Update12DTelemetryDisplay();
        if (tabIndex == 4) SyncEnvironmentUIValues();
    }

    // --- Morph Slider Callbacks ---
    public void OnHeightChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetHeight(val);
        if (heightValueText != null) heightValueText.text = $"{val:F2}x";
    }

    public void OnHeadChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetHeadScale(val);
        if (headValueText != null) headValueText.text = $"{val:F2}x";
    }

    public void OnShouldersChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetShoulderWidth(val);
        if (shouldersValueText != null) shouldersValueText.text = $"{val:F2}x";
    }

    public void OnChestChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetChestScale(val);
        if (chestValueText != null) chestValueText.text = $"{val:F2}x";
    }

    public void OnWaistChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetWaistWidth(val);
        if (waistValueText != null) waistValueText.text = $"{val:F2}x";
    }

    public void OnHipsChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetHipWidth(val);
        if (hipsValueText != null) hipsValueText.text = $"{val:F2}x";
    }

    public void OnArmsChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetArmThickness(val);
        if (armsValueText != null) armsValueText.text = $"{val:F2}x";
    }

    public void OnLegsChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetLegThickness(val);
        if (legsValueText != null) legsValueText.text = $"{val:F2}x";
    }

    public void OnMuscleChanged(float val)
    {
        if (currentMorphController != null) currentMorphController.SetMuscleMass(val);
        if (muscleValueText != null) muscleValueText.text = $"{val:F0} kg";
    }

    private void UpdateLabels()
    {
        if (currentMorphController == null) return;
        var p = currentMorphController.proportions;
        if (heightValueText) heightValueText.text = $"{p.heightScale:F2}x";
        if (headValueText) headValueText.text = $"{p.headScale:F2}x";
        if (shouldersValueText) shouldersValueText.text = $"{p.shoulderWidth:F2}x";
        if (chestValueText) chestValueText.text = $"{p.chestScale:F2}x";
        if (waistValueText) waistValueText.text = $"{p.waistWidth:F2}x";
        if (hipsValueText) hipsValueText.text = $"{p.hipWidth:F2}x";
        if (armsValueText) armsValueText.text = $"{p.armThickness:F2}x";
        if (legsValueText) legsValueText.text = $"{p.legThickness:F2}x";
        if (muscleValueText) muscleValueText.text = $"{p.muscleMass_4D:F0} kg";
    }

    public void ResetProportions()
    {
        if (heightSlider) heightSlider.value = 1.0f;
        if (headSlider) headSlider.value = 1.0f;
        if (shouldersSlider) shouldersSlider.value = 1.0f;
        if (chestSlider) chestSlider.value = 1.0f;
        if (waistSlider) waistSlider.value = 1.0f;
        if (hipsSlider) hipsSlider.value = 1.0f;
        if (armsSlider) armsSlider.value = 1.0f;
        if (legsSlider) legsSlider.value = 1.0f;
        if (muscleSlider) muscleSlider.value = isFemaleTarget ? 20f : 30f;
    }

    public void RandomizeProportions()
    {
        if (heightSlider) heightSlider.value = UnityEngine.Random.Range(0.88f, 1.18f);
        if (headSlider) headSlider.value = UnityEngine.Random.Range(0.90f, 1.15f);
        if (shouldersSlider) shouldersSlider.value = UnityEngine.Random.Range(0.85f, 1.30f);
        if (chestSlider) chestSlider.value = UnityEngine.Random.Range(0.85f, 1.30f);
        if (waistSlider) waistSlider.value = UnityEngine.Random.Range(0.80f, 1.20f);
        if (hipsSlider) hipsSlider.value = UnityEngine.Random.Range(0.85f, 1.30f);
        if (armsSlider) armsSlider.value = UnityEngine.Random.Range(0.85f, 1.25f);
        if (legsSlider) legsSlider.value = UnityEngine.Random.Range(0.85f, 1.25f);
        if (muscleSlider) muscleSlider.value = UnityEngine.Random.Range(15f, 55f);
    }

    // --- Wardrobe Equipment Toggles (Modular Body Part Customization) ---
    public void ToggleHelmet(bool equip)
    {
        if (currentModularAppearance != null) currentModularAppearance.SetHelmetArmor(equip);
        if (currentEquipmentManager != null)
        {
            if (equip && helmetPrefab != null)
                currentEquipmentManager.Equip(new EquipmentItemDefinition { itemId = "helmet", itemName = "Knight Helmet", slot = EquipmentSlot.Helmet, prefab = helmetPrefab });
            else
                currentEquipmentManager.Unequip(EquipmentSlot.Helmet);
        }
    }

    public void ToggleTorso(bool equip)
    {
        if (currentModularAppearance != null) currentModularAppearance.SetTorsoArmor(equip);
        if (currentEquipmentManager != null)
        {
            if (equip && torsoPrefab != null)
                currentEquipmentManager.Equip(new EquipmentItemDefinition { itemId = "torso", itemName = "Adventurer Cuirass", slot = EquipmentSlot.Torso, prefab = torsoPrefab });
            else
                currentEquipmentManager.Unequip(EquipmentSlot.Torso);
        }
    }

    public void ToggleGauntlets(bool equip)
    {
        if (currentModularAppearance != null) currentModularAppearance.SetGauntletsArmor(equip);
        if (currentEquipmentManager != null)
        {
            if (equip)
                currentEquipmentManager.Equip(new EquipmentItemDefinition { itemId = "gauntlets", itemName = "Armored Gauntlets", slot = EquipmentSlot.Gauntlets, prefab = null });
            else
                currentEquipmentManager.Unequip(EquipmentSlot.Gauntlets);
        }
    }

    public void ToggleLeggings(bool equip)
    {
        if (currentModularAppearance != null) currentModularAppearance.SetLeggingsArmor(equip);
        if (currentEquipmentManager != null)
        {
            if (equip && leggingsPrefab != null)
                currentEquipmentManager.Equip(new EquipmentItemDefinition { itemId = "leggings", itemName = "Adventurer Leggings", slot = EquipmentSlot.Leggings, prefab = leggingsPrefab });
            else
                currentEquipmentManager.Unequip(EquipmentSlot.Leggings);
        }
    }

    public void ToggleShoes(bool equip)
    {
        if (currentModularAppearance != null) currentModularAppearance.SetBootsArmor(equip);
        if (currentEquipmentManager != null)
        {
            if (equip && shoesPrefab != null)
                currentEquipmentManager.Equip(new EquipmentItemDefinition { itemId = "shoes", itemName = "Armored Boots", slot = EquipmentSlot.Shoes, prefab = shoesPrefab });
            else
                currentEquipmentManager.Unequip(EquipmentSlot.Shoes);
        }
    }

    public void SetSkinTone(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= SkinTonePresets.Length) return;
        selectedSkinIndex = presetIndex;
        Color c = SkinTonePresets[presetIndex];
        if (currentModularAppearance != null)
        {
            currentModularAppearance.ApplySkinTint(c);
            if (currentModularAppearance.CurrentData != null) currentModularAppearance.CurrentData.skinColor = c;
        }
    }

    public void SetArmorDye(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= ArmorDyePresets.Length) return;
        selectedArmorDyeIndex = presetIndex;
        Color c = ArmorDyePresets[presetIndex];
        if (currentModularAppearance != null)
        {
            currentModularAppearance.ApplyArmorDye(c);
            if (currentModularAppearance.CurrentData != null) currentModularAppearance.CurrentData.armorDyeColor = c;
        }
    }

    public void EquipWeapon(string weaponKey)
    {
        if (currentModularAppearance != null) currentModularAppearance.EquipWeapon(weaponKey);

        if (currentEquipmentManager == null) return;
        GameObject prefab = weaponKey switch
        {
            "axe" => battleAxePrefab,
            "sword" => swordPrefab,
            "staff" => staffPrefab,
            "wand" => wandPrefab,
            _ => null
        };

        if (prefab != null)
        {
            bool is2H = weaponKey == "axe" || weaponKey == "staff";
            currentEquipmentManager.Equip(new EquipmentItemDefinition
            {
                itemId = weaponKey,
                itemName = weaponKey.ToUpper(),
                slot = EquipmentSlot.MainHandWeapon,
                prefab = prefab,
                isTwoHanded = is2H
            });
        }
        else
        {
            currentEquipmentManager.Unequip(EquipmentSlot.MainHandWeapon);
        }
    }

    public void EquipFullSet()
    {
        ToggleHelmet(true);
        ToggleTorso(true);
        ToggleGauntlets(true);
        ToggleLeggings(true);
        ToggleShoes(true);
        EquipWeapon(isFemaleTarget ? "axe" : "sword");
    }

    public void StripAllGear()
    {
        ToggleHelmet(false);
        ToggleTorso(false);
        ToggleGauntlets(false);
        ToggleLeggings(false);
        ToggleShoes(false);
        EquipWeapon("none");
    }

    // --- Camera Presets ---
    public void OnFocusFullBody() { turntableController?.FocusFullBody(); }
    public void OnFocusPortrait() { turntableController?.FocusHead(); }
    public void OnFocusHands() { turntableController?.FocusHands(); }
    public void OnFocusFeet() { turntableController?.FocusFeet(); }
    public void OnToggleTurntable() { turntableController?.ToggleAutoRotate(); }

    // --- 12-Dimensional Telemetry Real-time Calculation ---
    public void Update12DTelemetryDisplay()
    {
        if (telemetryDisplayText == null || currentMorphController == null) return;

        var p = currentMorphController.proportions;
        float h = p.heightScale * (isFemaleTarget ? 1.65f : 1.80f);
        float s = p.shoulderWidth;
        float c = p.chestScale;
        float m = p.muscleMass_4D;

        // Metric Volume approx (m³)
        float metricVolume = 0.065f * h * s * c * p.hipWidth;
        // Total Mass (kg)
        float totalMass = 45f * h + m;
        // Density (kg/m³)
        float density = totalMass / Mathf.Max(0.01f, metricVolume);
        // Resonance Frequency (Hz)
        float resonance = 137.5f * (1.0f / Mathf.Sqrt(metricVolume));
        // Entropy S_0D
        float entropy = Mathf.Log(density * resonance) * 0.142f;

        string archetype = isFemaleTarget ? "FEMALE_GRACEFUL_MAGE" : "MALE_HEROIC_ATHLETE";
        string hash = $"{archetype.GetHashCode():X8}_{(int)(totalMass * 100):X4}";

        var sb = new StringBuilder();
        sb.AppendLine("<b><color=#38BDF8>✦ 12-DIMENSIONAL CRAFTING TELEMETRY ✦</color></b>");
        sb.AppendLine($"<color=#94A3B8>0D Entropy (S₀):</color>  <b>{entropy:F3} bits</b>");
        sb.AppendLine($"<color=#94A3B8>1D Resonance (f₁):</color> <b>{resonance:F1} Hz</b>");
        sb.AppendLine($"<color=#94A3B8>2D Planar Flux:</color>    <b>{(s * c):F3} Φ</b>");
        sb.AppendLine($"<color=#94A3B8>3D Metric Volume:</color>  <b>{metricVolume:F4} m³</b>");
        sb.AppendLine($"<color=#94A3B8>4D Inertial Mass:</color>  <b><color=#4ADE80>{totalMass:F1} kg</color></b> (Muscle: {m:F0}kg)");
        sb.AppendLine($"<color=#94A3B8>5D Density Matrix:</color> <b>{density:F1} kg/m³</b>");
        sb.AppendLine($"<color=#94A3B8>6D Curvature (R₆):</color>  <b>{(1f / h):F2} m⁻¹</b>");
        sb.AppendLine($"<color=#94A3B8>7D Carrier Wave:</color>   <b>60.00 FPS</b>");
        sb.AppendLine($"<color=#94A3B8>8D Base Prefab:</color>    <b>Character_Base_{ (isFemaleTarget ? "Female" : "Male") }</b>");
        sb.AppendLine($"<color=#94A3B8>9D Semantic Class:</color> <b>{archetype}</b>");
        sb.AppendLine($"<color=#94A3B8>10D Obfuscated Hash:</color> <b>#{hash}</b>");
        sb.AppendLine($"<color=#94A3B8>11D Parity Matrix:</color>   <b><color=#22C55E>VALIDATED (0x00)</color></b>");

        telemetryDisplayText.text = sb.ToString();
    }

    /// <summary>
    /// Overlays clothing and weapons on both male and female models so neither starts naked.
    /// </summary>
    public void EquipStarterClothingOnBoth()
    {
        if (maleCharacterInstance != null)
        {
            EquipStarterAttire(maleCharacterInstance, isFemale: false);
        }
        if (femaleCharacterInstance != null)
        {
            EquipStarterAttire(femaleCharacterInstance, isFemale: true);
        }
    }

    private void EquipStarterAttire(GameObject charObj, bool isFemale)
    {
        if (charObj == null) return;
        var equipMgr = charObj.GetComponent<ModularEquipmentManager>();
        if (equipMgr == null) equipMgr = charObj.AddComponent<ModularEquipmentManager>();
        equipMgr.AutoDetectSockets();

        if (torsoPrefab != null)
            equipMgr.Equip(new EquipmentItemDefinition { itemId = "torso", itemName = "Adventurer Cuirass", slot = EquipmentSlot.Torso, prefab = torsoPrefab });
        if (leggingsPrefab != null)
            equipMgr.Equip(new EquipmentItemDefinition { itemId = "leggings", itemName = "Adventurer Leggings", slot = EquipmentSlot.Leggings, prefab = leggingsPrefab });
        if (shoesPrefab != null)
            equipMgr.Equip(new EquipmentItemDefinition { itemId = "shoes", itemName = "Armored Boots", slot = EquipmentSlot.Shoes, prefab = shoesPrefab });

        GameObject wPrefab = isFemale ? (wandPrefab != null ? wandPrefab : swordPrefab) : swordPrefab;
        if (wPrefab != null)
        {
            equipMgr.Equip(new EquipmentItemDefinition
            {
                itemId = isFemale ? "wand" : "sword",
                itemName = isFemale ? "Apprentice Wand" : "Hero Sword",
                slot = EquipmentSlot.MainHandWeapon,
                prefab = wPrefab
            });
        }
    }

    public void EnsureProceedButton()
    {
        if (enterWorldButton == null)
        {
            var allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns)
            {
                string bName = b.name.ToLower();
                if (bName.Contains("enter_world") || bName.Contains("proceed") || bName.Contains("confirm"))
                {
                    enterWorldButton = b;
                    break;
                }
            }
        }

        if (enterWorldButton != null)
        {
            enterWorldButton.gameObject.SetActive(true);
            enterWorldButton.interactable = true;
            enterWorldButton.onClick.RemoveListener(EnterWorldWithCharacter);
            enterWorldButton.onClick.AddListener(EnterWorldWithCharacter);

            var img = enterWorldButton.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.95f, 0.70f, 0.15f, 0.98f);
                img.raycastTarget = true;
            }

            var txt = enterWorldButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = "<b>CREATE CHARACTER & PROCEED  ➔</b>";
                txt.fontSize = 14f;
                txt.color = new Color(0.04f, 0.07f, 0.14f);
            }
        }
    }

    public void SelectBodyType(int index)
    {
        index = Mathf.Clamp(index, 0, 7);
        isFemaleTarget = index < 4;
        SetViewMode(isFemaleTarget ? 1 : 0);

        var targetObj = isFemaleTarget ? femaleCharacterInstance : maleCharacterInstance;
        var modular = targetObj != null ? targetObj.GetComponentInChildren<BattleSummoner.Customization.ModularCharacterAppearance>() : currentModularAppearance;
        if (modular != null)
        {
            modular.ApplyBodyType(index);
        }
        var morph = targetObj != null ? targetObj.GetComponent<SimsBodyMorphController>() : currentMorphController;
        if (morph != null && modular != null && modular.CurrentData != null)
        {
            var d = modular.CurrentData;
            morph.proportions.heightScale = d.heightScale;
            morph.proportions.headScale = d.headScale;
            morph.proportions.shoulderWidth = d.shoulderWidth;
            morph.proportions.chestScale = d.chestScale;
            morph.proportions.waistWidth = d.waistScale;
            morph.proportions.hipWidth = d.hipScale;
            morph.proportions.armThickness = d.armThickness;
            morph.proportions.legThickness = d.legThickness;
            morph.ApplyProportions();
        }
        SelectTargetCharacter(targetObj);
        Debug.Log($"[CHARACTER CREATOR] Selected Body Type {index}: {BattleSummoner.Customization.ModularPlayerAppearanceData.BodyTypeNames[index]}");
    }

    /// <summary>
    /// Saves the customized character data (archetype, morph proportions, and equipment)
    /// and drops the player into the world by triggering the World Introduction Cutscene.
    /// </summary>
    public void EnterWorldWithCharacter()
    {
        string archetype = isFemaleTarget ? "Female" : "Male";
        PlayerPrefs.SetString("SummonerArchetype", archetype);

        string name = monikerInput != null && !string.IsNullOrEmpty(monikerInput.text)
            ? monikerInput.text
            : PlayerPrefs.GetString("SummonerName", isFemaleTarget ? "Asuna" : "Kirito");
        PlayerPrefs.SetString("SummonerName", name);

        // Save body morphs
        var activeMorph = isFemaleTarget && femaleCharacterInstance != null
            ? femaleCharacterInstance.GetComponent<SimsBodyMorphController>()
            : (maleCharacterInstance != null ? maleCharacterInstance.GetComponent<SimsBodyMorphController>() : currentMorphController);

        if (activeMorph != null)
        {
            var p = activeMorph.proportions;
            PlayerPrefs.SetFloat("Morph_Height", p.heightScale);
            PlayerPrefs.SetFloat("Morph_Head", p.headScale);
            PlayerPrefs.SetFloat("Morph_Shoulders", p.shoulderWidth);
            PlayerPrefs.SetFloat("Morph_Chest", p.chestScale);
            PlayerPrefs.SetFloat("Morph_Waist", p.waistWidth);
            PlayerPrefs.SetFloat("Morph_Hips", p.hipWidth);
            PlayerPrefs.SetFloat("Morph_Arms", p.armThickness);
            PlayerPrefs.SetFloat("Morph_Legs", p.legThickness);
            PlayerPrefs.SetFloat("Morph_Muscle", p.muscleMass_4D);
        }

        // Save modular appearance data (multiplayer and cross-scene serializable profile)
        var modData = new BattleSummoner.Customization.ModularPlayerAppearanceData();
        modData.playerName = name;
        modData.gender = isFemaleTarget ? 1 : 0;
        modData.skinColor = SkinTonePresets[Mathf.Clamp(selectedSkinIndex, 0, SkinTonePresets.Length - 1)];
        modData.armorDyeColor = ArmorDyePresets[Mathf.Clamp(selectedArmorDyeIndex, 0, ArmorDyePresets.Length - 1)];

        var activeModular = isFemaleTarget && femaleCharacterInstance != null
            ? femaleCharacterInstance.GetComponentInChildren<BattleSummoner.Customization.ModularCharacterAppearance>()
            : (maleCharacterInstance != null ? maleCharacterInstance.GetComponentInChildren<BattleSummoner.Customization.ModularCharacterAppearance>() : currentModularAppearance);

        if (activeModular != null && activeModular.CurrentData != null)
        {
            modData.bodyTypeIndex = activeModular.CurrentData.bodyTypeIndex;
        }

        var activeEquip = isFemaleTarget && femaleCharacterInstance != null
            ? femaleCharacterInstance.GetComponent<ModularEquipmentManager>()
            : (maleCharacterInstance != null ? maleCharacterInstance.GetComponent<ModularEquipmentManager>() : currentEquipmentManager);

        if (activeModular != null && activeModular.CurrentData != null)
        {
            modData.equipTorso = activeModular.CurrentData.equipTorso;
            modData.equipGauntlets = activeModular.CurrentData.equipGauntlets;
            modData.equipLeggings = activeModular.CurrentData.equipLeggings;
            modData.equipBoots = activeModular.CurrentData.equipBoots;
            modData.equipHelmet = activeModular.CurrentData.equipHelmet;
            modData.weaponId = !string.IsNullOrEmpty(activeModular.CurrentData.weaponId) ? activeModular.CurrentData.weaponId : (isFemaleTarget ? "wand" : "sword");
        }
        else if (activeEquip != null)
        {
            modData.equipHelmet = activeEquip.currentHelmet != null;
            modData.equipTorso = activeEquip.currentTorso != null;
            modData.equipGauntlets = activeEquip.currentGauntlets != null;
            modData.equipLeggings = activeEquip.currentLeggings != null;
            modData.equipBoots = activeEquip.currentShoes != null;
            modData.weaponId = activeEquip.currentMainWeapon?.itemId ?? (isFemaleTarget ? "wand" : "sword");
        }

        if (activeMorph != null)
        {
            var p = activeMorph.proportions;
            modData.heightScale = p.heightScale;
            modData.headScale = p.headScale;
            modData.shoulderWidth = p.shoulderWidth;
            modData.chestScale = p.chestScale;
            modData.waistScale = p.waistWidth;
            modData.hipScale = p.hipWidth;
            modData.armThickness = p.armThickness;
            modData.legThickness = p.legThickness;
        }

        modData.SaveLocalProfile();

        PlayerPrefs.SetInt("Equip_Helmet", modData.equipHelmet ? 1 : 0);
        PlayerPrefs.SetInt("Equip_Torso", modData.equipTorso ? 1 : 0);
        PlayerPrefs.SetInt("Equip_Gauntlets", modData.equipGauntlets ? 1 : 0);
        PlayerPrefs.SetInt("Equip_Leggings", modData.equipLeggings ? 1 : 0);
        PlayerPrefs.SetInt("Equip_Shoes", modData.equipBoots ? 1 : 0);
        PlayerPrefs.SetString("Equip_Weapon", modData.weaponId);

        PlayerPrefs.SetInt("CharacterCreationCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log($"[CHARACTER CREATOR] Modular character saved ({name} / {archetype}, gauntlets={modData.equipGauntlets}, torso={modData.equipTorso}). Transitioning to World Introduction Cutscene...");

        if (BattleSummoner.Audio.LoginAudioSynthesizer.Instance != null)
        {
            BattleSummoner.Audio.LoginAudioSynthesizer.Instance.PlayInductionFanfare();
            BattleSummoner.Audio.LoginAudioSynthesizer.Instance.PlayUIClick();
        }

        StartCoroutine(TransitionToCutsceneRoutine());
    }

    private IEnumerator TransitionToCutsceneRoutine()
    {
        // Smooth fade out
        var faderObj = GameObject.Find("SAO_ScreenFadeOverlay");
        Image fadeImg = null;
        if (faderObj != null) fadeImg = faderObj.GetComponent<Image>();

        if (fadeImg != null)
        {
            fadeImg.gameObject.SetActive(true);
            float duration = 0.50f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeImg.color = new Color(0.02f, 0.04f, 0.08f, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.35f);
        }

        string nextScene = !string.IsNullOrEmpty(targetCutsceneSceneName) ? targetCutsceneSceneName : "WorldIntroductionCutscene";
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }

    // --- Dynamic World & Environment Controls ---
    public void ApplyTimePreset(int presetIndex)
    {
        if (environmentController == null) return;
        environmentController.ApplyPreset((BattleSummoner.Customization.TimePreset)presetIndex);
        SyncEnvironmentUIValues();
    }

    public void SetSkyboxTheme(int themeIndex)
    {
        if (environmentController == null) return;
        environmentController.SetSkyboxTheme((BattleSummoner.Customization.SkyboxTheme)themeIndex);
        if (activeThemeText != null) activeThemeText.text = environmentController.currentTheme.ToString();
    }

    public void OnTimeOfDayChanged(float val)
    {
        if (environmentController == null) return;
        environmentController.SetTimeOfDay(val);
        if (timeOfDayValueText != null) timeOfDayValueText.text = environmentController.GetDigitalClockString();
        SyncSunSliderValues();
    }

    public void OnSunAzimuthChanged(float val)
    {
        if (environmentController == null) return;
        environmentController.SetSunAzimuth(val);
        if (sunAzimuthValueText != null) sunAzimuthValueText.text = $"{val:F0}°";
    }

    public void OnSunElevationChanged(float val)
    {
        if (environmentController == null) return;
        environmentController.SetSunElevation(val);
        if (sunElevationValueText != null) sunElevationValueText.text = $"{val:F0}°";
    }

    public void OnSunIntensityChanged(float val)
    {
        if (environmentController == null) return;
        environmentController.SetSunIntensity(val);
        if (sunIntensityValueText != null) sunIntensityValueText.text = $"{val:F2}x";
    }

    public void OnFogDensityChanged(float val)
    {
        if (environmentController == null) return;
        environmentController.SetFogDensity(val);
        if (fogDensityValueText != null) fogDensityValueText.text = $"{val * 1000f:F1}‰";
    }

    public void OnToggleTimelapse()
    {
        if (environmentController == null) return;
        environmentController.ToggleTimelapse();
        if (timelapseButtonText != null)
            timelapseButtonText.text = environmentController.isTimelapseActive ? "⏸ PAUSE CYCLE" : "▶ AUTO CYCLE";
    }

    public void OnToggleLanterns()
    {
        if (environmentController == null) return;
        bool newState = !environmentController.lanternsEnabled;
        environmentController.SetLanternsEnabled(newState);
        if (lanternsToggleText != null)
            lanternsToggleText.text = newState ? "LANTERNS: ON" : "LANTERNS: OFF";
    }

    public void OnNightGlowChanged(float val)
    {
        if (environmentController == null) return;
        environmentController.SetNightGlowIntensity(val);
        if (nightGlowValueText != null) nightGlowValueText.text = $"{val:F1}x";
    }

    public void OnToggleRunicGlow()
    {
        if (environmentController == null) return;
        environmentController.ToggleRunicGlow();
        if (runicGlowToggleText != null)
            runicGlowToggleText.text = environmentController.runicGlowEnabled ? "RUNIC GLOW: ON" : "RUNIC GLOW: OFF";
    }

    public void SyncEnvironmentUIValues()
    {
        if (environmentController == null) return;
        if (timeOfDaySlider != null) timeOfDaySlider.SetValueWithoutNotify(environmentController.currentTimeOfDay);
        if (timeOfDayValueText != null) timeOfDayValueText.text = environmentController.GetDigitalClockString();
        if (sunIntensitySlider != null) sunIntensitySlider.SetValueWithoutNotify(environmentController.sunIntensityMultiplier);
        if (sunIntensityValueText != null) sunIntensityValueText.text = $"{environmentController.sunIntensityMultiplier:F2}x";
        if (fogDensitySlider != null) fogDensitySlider.SetValueWithoutNotify(environmentController.customFogDensity);
        if (fogDensityValueText != null) fogDensityValueText.text = $"{environmentController.customFogDensity * 1000f:F1}‰";
        if (nightGlowSlider != null) nightGlowSlider.SetValueWithoutNotify(environmentController.nightGlowIntensity);
        if (nightGlowValueText != null) nightGlowValueText.text = $"{environmentController.nightGlowIntensity:F1}x";
        if (activeThemeText != null) activeThemeText.text = environmentController.currentTheme.ToString();
        if (timelapseButtonText != null)
            timelapseButtonText.text = environmentController.isTimelapseActive ? "⏸ PAUSE CYCLE" : "▶ AUTO CYCLE";
        if (lanternsToggleText != null)
            lanternsToggleText.text = environmentController.lanternsEnabled ? "LANTERNS: ON" : "LANTERNS: OFF";
        if (runicGlowToggleText != null)
            runicGlowToggleText.text = environmentController.runicGlowEnabled ? "RUNIC GLOW: ON" : "RUNIC GLOW: OFF";
        SyncSunSliderValues();
    }

    private void SyncSunSliderValues()
    {
        if (environmentController == null || environmentController.sunLight == null) return;
        var angles = environmentController.sunLight.transform.eulerAngles;
        float pitch = angles.x > 180f ? angles.x - 360f : angles.x;
        float yaw = angles.y;
        if (sunElevationSlider != null) sunElevationSlider.SetValueWithoutNotify(pitch);
        if (sunElevationValueText != null) sunElevationValueText.text = $"{pitch:F0}°";
        if (sunAzimuthSlider != null) sunAzimuthSlider.SetValueWithoutNotify(yaw);
        if (sunAzimuthValueText != null) sunAzimuthValueText.text = $"{yaw:F0}°";
    }

    // --- Battle Side Profile Portrait Helpers ---
    public void SetPortraitGender(bool isFemale)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetGender(isFemale ? BattlePortraitGender.Feme : BattlePortraitGender.Masc);
    }

    public void SetPortraitHair(int hairIndex)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetHair((BattlePortraitHair)hairIndex);
    }

    public void SetPortraitFace(int faceIndex)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetFace((BattlePortraitFace)faceIndex);
    }

    public void SetPortraitEyeColor(int eyeIndex)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetEyeColor((BattlePortraitEyeColor)eyeIndex);
    }

    public void SetPortraitClothing(int clothingIndex)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetClothing((BattlePortraitClothing)clothingIndex);
    }

    public void SetPortraitAccessory(int accessoryIndex)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetAccessory((BattlePortraitAccessory)accessoryIndex);
    }

    public void SetPortraitDirectTemplateMode(bool direct)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetUseTemplateDirectMode(direct);
    }

    public void SetPortraitHp(float hp)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetVitals(hp, 100f, 50f, 50f);
    }

    public void SetPortraitMp(float mp)
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.SetVitals(100f, 100f, mp, 50f);
    }

    public void TriggerPortraitDamageFlash()
    {
        if (sideProfilePortrait != null)
            sideProfilePortrait.TriggerDamageFlash();
    }
}

