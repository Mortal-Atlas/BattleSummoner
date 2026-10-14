using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum RitualStep
{
    Closed,
    Step1_NomenclatureSeed,
    Step2_LLMInquisition,
    Step3_AstralGeometry,
    Step4_StatSynthesisComplete
}

[System.Serializable]
public class InquisitionQuestion
{
    public string prompt;
    public string optionA;
    public string optionB;
    public string optionC;
    public string affinityA;
    public string affinityB;
    public string affinityC;
}

[System.Serializable]
public class SummonInquisitionQuiz
{
    public string seedConcept;
    public List<InquisitionQuestion> questions = new List<InquisitionQuestion>();
}

/// <summary>
/// SummoningRitualManager executes the multi-stage Patronus Engine sequence:
/// Step 1: Nomenclature Seed (Input text field)
/// Step 2: The LLM Inquisition (3 esoteric quiz questions determining Affinity, Temperament, Quirk)
/// Step 3: The Astral Geometry (Drawing magic circle with finger, Unistroke geometric variance calculation)
/// Step 4: Stat Synthesis: Base Stats = Baseline Level * (Accuracy Score / 100)
/// </summary>
public class SummoningRitualManager : MonoBehaviour
{
    public static SummoningRitualManager Instance { get; private set; }

    [Header("Modal & Stage Containers")]
    [SerializeField] private GameObject ritualModalPanel;
    [SerializeField] private GameObject step1SeedContainer;
    [SerializeField] private GameObject step2InquisitionContainer;
    [SerializeField] private GameObject step3GeometryContainer;
    [SerializeField] private GameObject step4SynthesisContainer;

    [Header("Step 1: Nomenclature Seed UI")]
    [SerializeField] private TMP_InputField seedInputField;
    [SerializeField] private Button submitSeedButton;

    [Header("Step 2: LLM Inquisition UI")]
    [SerializeField] private TextMeshProUGUI inquisitionQuestionText;
    [SerializeField] private Button choiceButtonA;
    [SerializeField] private Button choiceButtonB;
    [SerializeField] private Button choiceButtonC;
    [SerializeField] private TextMeshProUGUI choiceTextA;
    [SerializeField] private TextMeshProUGUI choiceTextB;
    [SerializeField] private TextMeshProUGUI choiceTextC;

    [Header("Step 3: Geometry & Result UI")]
    [SerializeField] private TextMeshProUGUI geometryInstructionText;
    [SerializeField] private TextMeshProUGUI accuracyResultText;
    [SerializeField] private Button finalizeRitualButton;
    [SerializeField] private Button closeRitualModalButton;

    [Header("Prefab & Spawning")]
    [SerializeField] private GameObject summonEntityPrefab;

    public static event Action<SummonCompanionEntity> OnSummonRitualFinished;

    private RitualStep currentStep = RitualStep.Closed;
    public RitualStep CurrentStep => currentStep;
    private string activeSeedConcept = "Aegis";
    private int currentQuestionIndex = 0;
    private SummonInquisitionQuiz activeQuiz;

    // Accumulated quiz affinities
    private SummonAffinity calculatedAffinity = SummonAffinity.Celestial;
    private SummonTemperament calculatedTemperament = SummonTemperament.Loyal;
    private SummonQuirk calculatedQuirk = SummonQuirk.StaggerMaster;
    private float measuredAccuracyScore = 100f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            BindUIListeners();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnSummoningCircleDrawn += HandleSummoningCircleDrawn;
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnSummoningCircleDrawn -= HandleSummoningCircleDrawn;
    }

    private void BindUIListeners()
    {
        if (submitSeedButton != null)
        {
            submitSeedButton.onClick.AddListener(OnSubmitSeedClicked);
        }

        if (choiceButtonA != null) choiceButtonA.onClick.AddListener(() => OnChoiceSelected(0));
        if (choiceButtonB != null) choiceButtonB.onClick.AddListener(() => OnChoiceSelected(1));
        if (choiceButtonC != null) choiceButtonC.onClick.AddListener(() => OnChoiceSelected(2));

        if (finalizeRitualButton != null)
        {
            finalizeRitualButton.onClick.AddListener(CloseRitualModal);
        }

        if (closeRitualModalButton != null)
        {
            closeRitualModalButton.onClick.AddListener(CloseRitualModal);
        }
    }

    public void OpenSummoningRitualModal()
    {
        if (ritualModalPanel != null) ritualModalPanel.SetActive(true);
        SwitchStep(RitualStep.Step1_NomenclatureSeed);
    }

    public void CloseRitualModal()
    {
        if (ritualModalPanel != null) ritualModalPanel.SetActive(false);
        SwitchStep(RitualStep.Closed);

        PortraitGestureCanvas canvas = FindAnyObjectByType<PortraitGestureCanvas>();
        if (canvas != null && canvas.currentMode == InteractionMode.SummoningCircle)
        {
            canvas.currentMode = InteractionMode.Combat;
        }
    }

    private void SwitchStep(RitualStep step)
    {
        currentStep = step;

        if (step1SeedContainer != null) step1SeedContainer.SetActive(step == RitualStep.Step1_NomenclatureSeed);
        if (step2InquisitionContainer != null) step2InquisitionContainer.SetActive(step == RitualStep.Step2_LLMInquisition);
        if (step3GeometryContainer != null) step3GeometryContainer.SetActive(step == RitualStep.Step3_AstralGeometry);
        if (step4SynthesisContainer != null) step4SynthesisContainer.SetActive(step == RitualStep.Step4_StatSynthesisComplete);
    }

    #region Step 1: Nomenclature Seed
    private void OnSubmitSeedClicked()
    {
        string input = seedInputField != null ? seedInputField.text.Trim() : "Aegis";
        if (string.IsNullOrEmpty(input)) input = "Aegis";

        activeSeedConcept = input;
        Debug.Log($"[SUMMONING RITUAL] Step 1 Seed received: '{activeSeedConcept}'. Contacting LLM Inquisition...");

        // Transition to Step 2
        SwitchStep(RitualStep.Step2_LLMInquisition);
        currentQuestionIndex = 0;

        if (UnfilteredDialoguePipeline.Instance != null)
        {
            UnfilteredDialoguePipeline.Instance.GenerateSummoningInquisition(activeSeedConcept, HandleInquisitionQuizGenerated);
        }
        else
        {
            HandleInquisitionQuizGenerated(BuildFallbackQuiz(activeSeedConcept));
        }
    }
    #endregion

    #region Step 2: The LLM Inquisition
    private void HandleInquisitionQuizGenerated(SummonInquisitionQuiz quiz)
    {
        activeQuiz = quiz ?? BuildFallbackQuiz(activeSeedConcept);
        currentQuestionIndex = 0;
        DisplayCurrentInquisitionQuestion();
    }

    private void DisplayCurrentInquisitionQuestion()
    {
        if (activeQuiz == null || activeQuiz.questions == null || currentQuestionIndex >= activeQuiz.questions.Count)
        {
            // Inquisition completed, transition to Astral Geometry
            TransitionToAstralGeometry();
            return;
        }

        var q = activeQuiz.questions[currentQuestionIndex];
        if (inquisitionQuestionText != null)
        {
            inquisitionQuestionText.text = $"[INQUISITION {currentQuestionIndex + 1}/3 for '{activeSeedConcept}']\n{q.prompt}";
        }
        if (choiceTextA != null) choiceTextA.text = $"A) {q.optionA}";
        if (choiceTextB != null) choiceTextB.text = $"B) {q.optionB}";
        if (choiceTextC != null) choiceTextC.text = $"C) {q.optionC}";
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        if (activeQuiz != null && currentQuestionIndex < activeQuiz.questions.Count)
        {
            var q = activeQuiz.questions[currentQuestionIndex];
            string chosenAffinity = (choiceIndex == 0) ? q.affinityA : ((choiceIndex == 1) ? q.affinityB : q.affinityC);

            ApplyQuizChoiceArchetype(currentQuestionIndex, chosenAffinity);
        }

        currentQuestionIndex++;
        if (currentQuestionIndex < 3 && activeQuiz != null && currentQuestionIndex < activeQuiz.questions.Count)
        {
            DisplayCurrentInquisitionQuestion();
        }
        else
        {
            TransitionToAstralGeometry();
        }
    }

    private void ApplyQuizChoiceArchetype(int qIndex, string archetypeTag)
    {
        switch (qIndex)
        {
            case 0:
                // Question 1 dictates Elemental Affinity
                calculatedAffinity = archetypeTag?.ToLowerInvariant() switch
                {
                    "fire" => SummonAffinity.Fire,
                    "void" => SummonAffinity.Void,
                    "earth" => SummonAffinity.Earth,
                    "wind" => SummonAffinity.Wind,
                    "blood" => SummonAffinity.Blood,
                    _ => SummonAffinity.Celestial
                };
                break;

            case 1:
                // Question 2 dictates Ego / Temperament
                calculatedTemperament = archetypeTag?.ToLowerInvariant() switch
                {
                    "aggressive" => SummonTemperament.Aggressive,
                    "stoic" => SummonTemperament.Stoic,
                    "chaotic" => SummonTemperament.Chaotic,
                    "gluttonous" => SummonTemperament.Gluttonous,
                    _ => SummonTemperament.Loyal
                };
                break;

            case 2:
                // Question 3 dictates Behavior Quirk
                calculatedQuirk = archetypeTag?.ToLowerInvariant() switch
                {
                    "spellvamp" => SummonQuirk.SpellVamp,
                    "kineticreflect" => SummonQuirk.KineticReflect,
                    "gemfinder" => SummonQuirk.GemFinder,
                    "potatolimp" => SummonQuirk.PotatoLimp,
                    _ => SummonQuirk.StaggerMaster
                };
                break;
        }
    }
    #endregion

    #region Step 3: The Astral Geometry
    private void TransitionToAstralGeometry()
    {
        SwitchStep(RitualStep.Step3_AstralGeometry);

        if (geometryInstructionText != null)
        {
            geometryInstructionText.text = $"★ STEP 3: THE ASTRAL GEOMETRY ★\nDraw a closed magic circle on screen for '{activeSeedConcept}'!\n(Geometric precision dictates stats: 98%+ = Apex, <50% = Squashed Potato)";
        }

        PortraitGestureCanvas canvas = FindAnyObjectByType<PortraitGestureCanvas>();
        if (canvas != null)
        {
            canvas.currentMode = InteractionMode.SummoningCircle;
        }
    }

    private void HandleSummoningCircleDrawn(float accuracyScore, Vector2 center, float radius)
    {
        if (currentStep != RitualStep.Step3_AstralGeometry) return;

        measuredAccuracyScore = accuracyScore;
        Debug.Log($"[ASTRAL GEOMETRY] Circle drawn with Accuracy Score: {measuredAccuracyScore:F1}%. Moving to Step 4 Stat Synthesis.");

        ExecuteStep4StatSynthesis();
    }
    #endregion

    #region Step 4: Stat Synthesis
    private void ExecuteStep4StatSynthesis()
    {
        SwitchStep(RitualStep.Step4_StatSynthesisComplete);

        int baselineLevel = 14;
        PlayerProgression player = FindAnyObjectByType<PlayerProgression>();
        if (player != null)
        {
            baselineLevel = player.currentLevel;
        }

        // Spawn Summon Entity in 3D world
        Vector3 spawnPosition = Vector3.zero;
        if (player != null)
        {
            spawnPosition = player.transform.position + player.transform.forward * 2.0f + player.transform.right * 1.5f;
            spawnPosition.y = 1f;
        }

        GameObject entityObj = null;
        if (summonEntityPrefab != null)
        {
            entityObj = Instantiate(summonEntityPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            entityObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            entityObj.name = $"Summon_{activeSeedConcept}";
            entityObj.transform.position = spawnPosition;
        }

        SummonCompanionEntity summonComp = entityObj.GetComponent<SummonCompanionEntity>();
        if (summonComp == null) summonComp = entityObj.AddComponent<SummonCompanionEntity>();

        summonComp.InitializeSynthesizedStats(activeSeedConcept, calculatedAffinity, calculatedTemperament, calculatedQuirk, measuredAccuracyScore, baselineLevel);

        // Update Step 4 Result Text
        if (accuracyResultText != null)
        {
            string qualityTitle = summonComp.isApexSummon 
                ? "★ [APEX CELESTIAL SOVEREIGN (98%+)]" 
                : (summonComp.isMalformedPotato ? "[MALFORMED SQUASHED POTATO (<50%)]" : "[ASTRAL COMBAT GOLEM]");

            accuracyResultText.text = $"{qualityTitle}\n" +
                $"Name: {summonComp.summonName}\n" +
                $"Affinity: {summonComp.affinity} | Temperament: {summonComp.temperament}\n" +
                $"Quirk: {summonComp.quirk}\n" +
                $"Circle Accuracy: {measuredAccuracyScore:F1}%\n" +
                $"Max HP: {summonComp.maxHp:F0} | ATK: {summonComp.attackPower:F1} | DEF: {summonComp.defense:F1}";
        }

        // Trigger Broadcast if Apex
        if (summonComp.isApexSummon && SystemBroadcastManager.Instance != null)
        {
            SystemBroadcastManager.Instance.TriggerAchievement("ach_relic_3x_drop", activeSeedConcept);
        }

        OnSummonRitualFinished?.Invoke(summonComp);
    }
    #endregion

    private SummonInquisitionQuiz BuildFallbackQuiz(string seed)
    {
        return new SummonInquisitionQuiz
        {
            seedConcept = seed,
            questions = new List<InquisitionQuestion>
            {
                new InquisitionQuestion
                {
                    prompt = $"When the cosmic spire collapses under entropic weight, where does the essence of {seed} retreat?",
                    optionA = "Into the roaring abyssal flames of ruin.",
                    optionB = "Into the silent void between celestial stars.",
                    optionC = "Into the unyielding stone bedrock of the earth.",
                    affinityA = "Fire",
                    affinityB = "Void",
                    affinityC = "Earth"
                },
                new InquisitionQuestion
                {
                    prompt = $"How does {seed} face a sovereign Spire sentinel towering over it?",
                    optionA = "With ferocious, unbridled kinetic assault.",
                    optionB = "With cold, calculated stoic defiance.",
                    optionC = "With unpredictable chaotic deception.",
                    affinityA = "Aggressive",
                    affinityB = "Stoic",
                    affinityC = "Chaotic"
                },
                new InquisitionQuestion
                {
                    prompt = $"What boon does {seed} siphon from the shattered geodes of delvers?",
                    optionA = "Siphons astral blood to restore its own vitality (SpellVamp).",
                    optionB = "Deflects kinetic shockwaves back upon adversaries (KineticReflect).",
                    optionC = "Cracks bedrock to uncover ancestral mana cores (GemFinder).",
                    affinityA = "SpellVamp",
                    affinityB = "KineticReflect",
                    affinityC = "GemFinder"
                }
            }
        };
    }
}
