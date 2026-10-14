using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SummoningRitualUI provides an arcane summoning altar interface connecting to SummoningRitualManager:
/// - Step 1: Nomenclature Seed & Essence Catalyst selection (Celestial, Infernal, Abyssal, Verdant)
/// - Step 2: The LLM Inquisition (Persona/Megaten-style demonic quiz testing morality, temperament, quirk)
/// - Step 3: Astral Geometry Altar (Drawing the summoning circle / patronus geometry on touch canvas)
/// - Step 4: Patronus Synthesis (Reveals summoned companion entity with stats, element, and quirk, then spawns it)
/// </summary>
public class SummoningRitualUI : MonoBehaviour
{
    private static SummoningRitualUI _instance;
    public static SummoningRitualUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SummoningRitualUI>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Root & Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button closeButton;

    [Header("Stage Panels")]
    [SerializeField] private GameObject step1SeedPanel;
    [SerializeField] private GameObject step2InquisitionPanel;
    [SerializeField] private GameObject step3GeometryPanel;
    [SerializeField] private GameObject step4SynthesisPanel;

    [Header("Step 1 Elements")]
    [SerializeField] private TMP_InputField seedNameInputField;
    [SerializeField] private Button catalystCelestialBtn;
    [SerializeField] private Button catalystInfernalBtn;
    [SerializeField] private Button catalystAbyssalBtn;
    [SerializeField] private Button catalystVerdantBtn;
    [SerializeField] private TextMeshProUGUI selectedCatalystTMP;
    [SerializeField] private Button beginInquisitionBtn;

    [Header("Step 2 Inquisition Elements")]
    [SerializeField] private TextMeshProUGUI questionTitleTMP;
    [SerializeField] private TextMeshProUGUI questionBodyTMP;
    [SerializeField] private Button inquisitionChoiceABtn;
    [SerializeField] private Button inquisitionChoiceBBtn;
    [SerializeField] private Button inquisitionChoiceCBtn;
    [SerializeField] private TextMeshProUGUI inquisitionChoiceATMP;
    [SerializeField] private TextMeshProUGUI inquisitionChoiceBTMP;
    [SerializeField] private TextMeshProUGUI inquisitionChoiceCTMP;

    [Header("Step 3 Astral Geometry Elements")]
    [SerializeField] private TextMeshProUGUI geometryInstructionTMP;
    [SerializeField] private TextMeshProUGUI circleAccuracyResultTMP;
    [SerializeField] private Button drawSimulateCircleBtn;
    [SerializeField] private Button proceedToSynthesisBtn;

    [Header("Step 4 Synthesis Elements")]
    [SerializeField] private Image synthesizedPortraitImg;
    [SerializeField] private TextMeshProUGUI synthesizedNameTMP;
    [SerializeField] private TextMeshProUGUI synthesizedAffinityTMP;
    [SerializeField] private TextMeshProUGUI synthesizedStatsTMP;
    [SerializeField] private Button summonIntoWorldButton;

    [Header("State")]
    private string activeSeedName = "Aegis";
    private SummonAffinity selectedAffinity = SummonAffinity.Celestial;
    private SummonTemperament selectedTemperament = SummonTemperament.Loyal;
    private SummonQuirk selectedQuirk = SummonQuirk.StaggerMaster;
    private float ritualAccuracyScore = 94.5f;

    public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (overlayRoot == null) overlayRoot = gameObject;
        BindButtonListeners();
        SetVisible(false);
    }

    private void Update()
    {
        // Desktop shortcut: 'R' toggles Summoning Ritual Altar
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRitualUI();
        }
    }

    public void ToggleRitualUI()
    {
        SetVisible(!IsOpen);
    }

    public void SetVisible(bool visible)
    {
        if (overlayRoot != null) overlayRoot.SetActive(visible);

        Transform parentTrans = transform.parent;
        if (parentTrans != null)
        {
            Transform hud = parentTrans.Find("MockupMasterHUD");
            if (hud != null) hud.gameObject.SetActive(!visible);

            Transform safeArea = parentTrans.Find("SafeAreaContainer");
            if (safeArea != null) safeArea.gameObject.SetActive(!visible);
        }
        else
        {
            GameObject hudGo = GameObject.Find("MockupMasterHUD");
            if (hudGo != null) hudGo.SetActive(!visible);
            GameObject safeGo = GameObject.Find("SafeAreaContainer");
            if (safeGo != null) safeGo.SetActive(!visible);
        }

        if (visible)
        {
            ShowStep(1);
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.9f);
            }
        }
        else
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.8f);
            }
        }
    }

    private void BindButtonListeners()
    {
        if (closeButton != null) closeButton.onClick.AddListener(() => SetVisible(false));

        // Step 1
        if (catalystCelestialBtn != null) catalystCelestialBtn.onClick.AddListener(() => SelectCatalyst(SummonAffinity.Celestial, "Celestial Starlight"));
        if (catalystInfernalBtn != null) catalystInfernalBtn.onClick.AddListener(() => SelectCatalyst(SummonAffinity.Fire, "Infernal Pyre"));
        if (catalystAbyssalBtn != null) catalystAbyssalBtn.onClick.AddListener(() => SelectCatalyst(SummonAffinity.Void, "Abyssal Void"));
        if (catalystVerdantBtn != null) catalystVerdantBtn.onClick.AddListener(() => SelectCatalyst(SummonAffinity.Earth, "Verdant Growth"));
        if (beginInquisitionBtn != null) beginInquisitionBtn.onClick.AddListener(StartInquisitionStep);

        // Step 2
        if (inquisitionChoiceABtn != null) inquisitionChoiceABtn.onClick.AddListener(() => AnswerQuestion(0));
        if (inquisitionChoiceBBtn != null) inquisitionChoiceBBtn.onClick.AddListener(() => AnswerQuestion(1));
        if (inquisitionChoiceCBtn != null) inquisitionChoiceCBtn.onClick.AddListener(() => AnswerQuestion(2));

        // Step 3
        if (drawSimulateCircleBtn != null) drawSimulateCircleBtn.onClick.AddListener(SimulateMagicCircleDraw);
        if (proceedToSynthesisBtn != null) proceedToSynthesisBtn.onClick.AddListener(StartSynthesisStep);

        // Step 4
        if (summonIntoWorldButton != null) summonIntoWorldButton.onClick.AddListener(SpawnSummonIntoWorld);
    }

    public void ShowStep(int step)
    {
        if (step1SeedPanel != null) step1SeedPanel.SetActive(step == 1);
        if (step2InquisitionPanel != null) step2InquisitionPanel.SetActive(step == 2);
        if (step3GeometryPanel != null) step3GeometryPanel.SetActive(step == 3);
        if (step4SynthesisPanel != null) step4SynthesisPanel.SetActive(step == 4);

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
        }
    }

    #region Step 1: Nomenclature Seed & Catalyst
    private void SelectCatalyst(SummonAffinity affinity, string catalystName)
    {
        selectedAffinity = affinity;
        if (selectedCatalystTMP != null)
        {
            selectedCatalystTMP.text = $"CATALYST: <color=#FFD700><b>{catalystName.ToUpper()}</b></color>";
        }
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.8f);
        }
    }

    private void StartInquisitionStep()
    {
        if (seedNameInputField != null && !string.IsNullOrEmpty(seedNameInputField.text))
        {
            activeSeedName = seedNameInputField.text.Trim();
        }
        else
        {
            activeSeedName = "Aegis";
        }

        // Setup Step 2 Persona/Megaten style demonic quiz
        if (questionTitleTMP != null) questionTitleTMP.text = "THE LLM INQUISITION // TRIAL OF INTENT";
        if (questionBodyTMP != null)
        {
            questionBodyTMP.text = $"\"You dare evoke the moniker of '{activeSeedName.ToUpper()}'?\nWhen the Spire's floor crumbles beneath your feet, where does your loyalty lie?\"";
        }

        if (inquisitionChoiceATMP != null) inquisitionChoiceATMP.text = "[A] In shielding those who cannot defend themselves.";
        if (inquisitionChoiceBTMP != null) inquisitionChoiceBTMP.text = "[B] In burning away any obstacle standing in our path.";
        if (inquisitionChoiceCTMP != null) inquisitionChoiceCTMP.text = "[C] In deciphering the ancient celestial knowledge.";

        ShowStep(2);
    }
    #endregion

    #region Step 2: The LLM Inquisition
    private void AnswerQuestion(int choiceIndex)
    {
        switch (choiceIndex)
        {
            case 0:
                selectedTemperament = SummonTemperament.Loyal;
                selectedQuirk = SummonQuirk.StaggerMaster;
                break;
            case 1:
                selectedTemperament = SummonTemperament.Aggressive;
                selectedQuirk = SummonQuirk.KineticReflect;
                break;
            case 2:
            default:
                selectedTemperament = SummonTemperament.Stoic;
                selectedQuirk = SummonQuirk.SpellVamp;
                break;
        }

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
        }

        // Setup Step 3 Geometry
        if (geometryInstructionTMP != null)
        {
            geometryInstructionTMP.text = "Trace the Astral Circle with your finger or press SIMULATE to inscribe the geometry.";
        }
        if (circleAccuracyResultTMP != null)
        {
            circleAccuracyResultTMP.text = "Awaiting sacred stroke...";
        }
        if (proceedToSynthesisBtn != null)
        {
            proceedToSynthesisBtn.interactable = false;
        }

        ShowStep(3);
    }
    #endregion

    #region Step 3: Astral Geometry
    public void SimulateMagicCircleDraw()
    {
        ritualAccuracyScore = UnityEngine.Random.Range(88f, 98.5f);
        if (circleAccuracyResultTMP != null)
        {
            circleAccuracyResultTMP.text = $"<color=#30FF80><b>RITUAL ACCURACY: {ritualAccuracyScore:F1}%</b></color>\nCelestial resonance attained!";
        }
        if (proceedToSynthesisBtn != null)
        {
            proceedToSynthesisBtn.interactable = true;
        }
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("spell_fireball_launch", 0.9f);
        }
    }

    private void StartSynthesisStep()
    {
        ShowStep(4);

        if (synthesizedNameTMP != null)
        {
            synthesizedNameTMP.text = $"<color=#FFD700><b>{activeSeedName.ToUpper()}</b></color>";
        }

        if (synthesizedAffinityTMP != null)
        {
            synthesizedAffinityTMP.text = $"AFFINITY: {selectedAffinity} | TEMPERAMENT: {selectedTemperament}";
        }

        float hp = 850f * (ritualAccuracyScore / 100f);
        float atk = 68f * (ritualAccuracyScore / 100f);

        if (synthesizedStatsTMP != null)
        {
            synthesizedStatsTMP.text = $"<color=#30FF80><b>HP: {hp:F0}</b></color> | <color=#FF6060><b>ATK: {atk:F0}</b></color>\n<color=#40EBFF><b>QUIRK:</b> {selectedQuirk}</color>";
        }

        if (synthesizedPortraitImg != null)
        {
            Sprite demonSprite = LoadSprite("Mockup_PersonaPortrait_Demon.png");
            if (demonSprite != null) synthesizedPortraitImg.sprite = demonSprite;
        }

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
        }
    }
    #endregion

    #region Step 4: Summoning Into World
    private void SpawnSummonIntoWorld()
    {
        SetVisible(false);

        // Notify user via Toast and Persona Dialogue!
        if (DevClassLoadoutManager.Instance != null)
        {
            DevClassLoadoutManager.Instance.ShowToast($"<color=#FFD700>[SUMMONING COMPLETE]</color>\n{activeSeedName} materialized into the battlefield!");
        }

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("apex_celestial_roar", 1.0f);
        }

        // Persona Dialogue introduction with summoned entity
        if (PersonaDialogueUI.Instance != null)
        {
            Sprite demonSprite = LoadSprite("Mockup_PersonaPortrait_Demon.png");
            List<PersonaDialogueLine> lines = new List<PersonaDialogueLine>
            {
                new PersonaDialogueLine
                {
                    speakerName = activeSeedName,
                    speakerTitle = $"{selectedAffinity} Patronus Entity",
                    portraitSprite = demonSprite,
                    text = $"I awaken to your sacred inquisition, Summoner. The bond between soul and starlight is sealed."
                },
                new PersonaDialogueLine
                {
                    speakerName = activeSeedName,
                    speakerTitle = $"{selectedAffinity} Patronus Entity",
                    portraitSprite = demonSprite,
                    text = $"Command me into the Spire's depths, and my ether shall cleave all darkness before you!"
                }
            };
            PersonaDialogueUI.Instance.StartDialogueSequence(lines);
        }
    }
    #endregion

    private Sprite LoadSprite(string textureName)
    {
#if UNITY_EDITOR
        string path = "Assets/Textures/UI/" + textureName;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }
}
