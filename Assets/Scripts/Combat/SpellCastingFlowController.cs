using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SpellCastingFlowController orchestrates the interactive spellcraft sequence:
/// 1. Listens for when a spell finishes being drawn (via PortraitGestureCanvas, RuneShapeCastingEngine, or WeaponArtGestureController).
/// 2. Summons the CAST icon into the exact center of the screen and spins it continuously.
/// 3. If the player presses the spinning CAST icon, the spell is cast.
/// 4. Displays a DISPEL button on the bottom-left of the screen above the movement controls,
///    allowing the player to cancel and dispel the spell casting.
/// </summary>
public class SpellCastingFlowController : MonoBehaviour
{
    private static SpellCastingFlowController _instance;
    public static SpellCastingFlowController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindAnyObjectByType<SpellCastingFlowController>();
            }
            return _instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private RectTransform castButtonRect;
    [SerializeField] private Button castButtonComponent;
    [SerializeField] private RectTransform runeCircleRect;
    [SerializeField] private RectTransform dispelButtonRect;
    [SerializeField] private Button dispelButtonComponent;
    [SerializeField] private TextMeshProUGUI castLabelTMP;
    [SerializeField] private TextMeshProUGUI dispelLabelTMP;

    [Header("Spin Configuration")]
    [SerializeField] private float spinSpeed = -240f; // degrees per second
    [SerializeField] private float pulseFrequency = 6f;
    [SerializeField] private float pulseMagnitude = 0.08f;

    [Header("Behavior")]
    [SerializeField] private bool hideCastButtonWhenNotPrimed = true;

    [Header("State")]
    [SerializeField] private bool isSpellPrimed = false;
    private SpellElement primedElement = SpellElement.Fire;
    private Vector2 primedCentroid = Vector2.zero;
    private float primedRadius = 100f;
    private string primedSpellName = "Elemental Rune";

    // Anchors for Center Spinning State
    private readonly Vector2 CENTER_CAST_MIN = new Vector2(0.38f, 0.44f);
    private readonly Vector2 CENTER_CAST_MAX = new Vector2(0.62f, 0.56f);
    private readonly Vector2 CENTER_RUNE_MIN = new Vector2(0.30f, 0.38f);
    private readonly Vector2 CENTER_RUNE_MAX = new Vector2(0.70f, 0.62f);

    // Anchors for Resting State
    private readonly Vector2 REST_CAST_MIN = new Vector2(0.60f, 0.25f);
    private readonly Vector2 REST_CAST_MAX = new Vector2(0.75f, 0.32f);
    private readonly Vector2 REST_RUNE_MIN = new Vector2(0.53f, 0.05f);
    private readonly Vector2 REST_RUNE_MAX = new Vector2(0.82f, 0.24f);

    // Anchors for Dispel Button: Bottom-Left directly above movement controls
    private readonly Vector2 DISPEL_MIN = new Vector2(0.04f, 0.24f);
    private readonly Vector2 DISPEL_MAX = new Vector2(0.36f, 0.31f);

    public bool IsSpellPrimed => isSpellPrimed;
    public SpellElement PrimedElement => primedElement;

    public event Action<SpellElement> OnSpellCastTriggered;
    public event Action OnSpellDispelled;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        if (!isSpellPrimed)
        {
            SetRestingLayout();
        }
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnCircleRuneDrawn += HandleCircleRuneDrawn;
        PortraitGestureCanvas.OnSummoningCircleDrawn += HandleSummoningCircleDrawn;
        if (WeaponArtGestureController.Instance != null)
        {
            WeaponArtGestureController.Instance.OnComboRecognized += HandleWeaponArtCombo;
        }
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnCircleRuneDrawn -= HandleCircleRuneDrawn;
        PortraitGestureCanvas.OnSummoningCircleDrawn -= HandleSummoningCircleDrawn;
        if (WeaponArtGestureController.Instance != null)
        {
            WeaponArtGestureController.Instance.OnComboRecognized -= HandleWeaponArtCombo;
        }
    }

    public void ResolveReferences()
    {
        if (castButtonRect == null)
        {
            var go = GameObject.Find("CastActionButton");
            if (go != null)
            {
                castButtonRect = go.GetComponent<RectTransform>();
                castButtonComponent = go.GetComponent<Button>();
                castLabelTMP = go.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (runeCircleRect == null)
        {
            var go = GameObject.Find("MockupRuneCircleVisual");
            if (go != null) runeCircleRect = go.GetComponent<RectTransform>();
        }

        if (dispelButtonRect == null)
        {
            var go = GameObject.Find("DispelSpellButton");
            if (go != null)
            {
                dispelButtonRect = go.GetComponent<RectTransform>();
                dispelButtonComponent = go.GetComponent<Button>();
                dispelLabelTMP = go.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (castButtonComponent != null)
        {
            castButtonComponent.onClick.RemoveListener(OnCastButtonPressed);
            castButtonComponent.onClick.AddListener(OnCastButtonPressed);
        }

        if (dispelButtonComponent != null)
        {
            dispelButtonComponent.onClick.RemoveListener(OnDispelButtonPressed);
            dispelButtonComponent.onClick.AddListener(OnDispelButtonPressed);
        }
    }

    public void BindReferences(
        RectTransform castBtn,
        Button castComp,
        RectTransform runeCircle,
        RectTransform dispelBtn,
        Button dispelComp,
        TextMeshProUGUI castLabel,
        TextMeshProUGUI dispelLabel)
    {
        castButtonRect = castBtn;
        castButtonComponent = castComp;
        runeCircleRect = runeCircle;
        dispelButtonRect = dispelBtn;
        dispelButtonComponent = dispelComp;
        castLabelTMP = castLabel;
        dispelLabelTMP = dispelLabel;

        if (castButtonComponent != null)
        {
            castButtonComponent.onClick.RemoveListener(OnCastButtonPressed);
            castButtonComponent.onClick.AddListener(OnCastButtonPressed);
        }
        if (dispelButtonComponent != null)
        {
            dispelButtonComponent.onClick.RemoveListener(OnDispelButtonPressed);
            dispelButtonComponent.onClick.AddListener(OnDispelButtonPressed);
        }

        if (!isSpellPrimed) SetRestingLayout();
    }

    private void Update()
    {
        if (isSpellPrimed)
        {
            UpdateSpinningCastIcon();
        }

        HandleDebugShortcuts();
    }

    private void UpdateSpinningCastIcon()
    {
        if (castButtonRect != null)
        {
            // Spin the cast icon continuously
            castButtonRect.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);

            // Pulsing breathing scale for arcane impact
            float pulse = 1f + pulseMagnitude * Mathf.Sin(Time.time * pulseFrequency);
            castButtonRect.localScale = new Vector3(pulse, pulse, 1f);
        }

        if (runeCircleRect != null)
        {
            // Rotate the magic circle underneath at counter-speed
            runeCircleRect.Rotate(Vector3.forward, -spinSpeed * 0.35f * Time.deltaTime, Space.Self);
        }
    }

    private void HandleDebugShortcuts()
    {
        // 'C' simulates spell drawing finished
        if (Input.GetKeyDown(KeyCode.C))
        {
            SimulateSpellDrawn();
        }

        // Space or Return executes primed spell
        if (isSpellPrimed && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            OnCastButtonPressed();
        }

        // Escape or 'X' dispels primed spell
        if (isSpellPrimed && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X)))
        {
            OnDispelButtonPressed();
        }
    }

    #region Spell Drawing Callbacks
    private void HandleCircleRuneDrawn(Vector2 centroid, float radius)
    {
        PrimeSpell(SpellElement.Fire, centroid, radius, "Circle Rune");
    }

    private void HandleSummoningCircleDrawn(float accuracy, Vector2 centroid, float radius)
    {
        SpellElement el = accuracy >= 80f ? SpellElement.Healing : SpellElement.Water;
        PrimeSpell(el, centroid, radius, $"Summoning Circle ({accuracy:F0}%)");
    }

    private void HandleWeaponArtCombo(string comboPattern)
    {
        if (comboPattern == "CIRCLE_COMBO" || comboPattern == "SQUARE_COMBO" || comboPattern == "X_COMBO")
        {
            PrimeSpell(SpellElement.Rock, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), 120f, comboPattern);
        }
    }
    #endregion

    /// <summary>
    /// When the spell is finished being drawn:
    /// Summons the CAST icon into the exact middle of the screen and starts spinning.
    /// Activates the DISPEL button on the bottom left above the movement controls.
    /// </summary>
    public void PrimeSpell(SpellElement element, Vector2 centroid, float radius, string spellName)
    {
        isSpellPrimed = true;
        primedElement = element;
        primedCentroid = centroid;
        primedRadius = radius;
        primedSpellName = spellName;

        Debug.Log($"✨ [SPELL PRIMED] '{spellName}' ({element}) finished drawing! Summoning spinning CAST icon to screen center.");

        // 1. Move CAST icon to center of screen
        if (castButtonRect != null)
        {
            castButtonRect.gameObject.SetActive(true);
            castButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
            castButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
            castButtonRect.pivot = new Vector2(0.5f, 0.5f);
            castButtonRect.sizeDelta = new Vector2(250f, 250f);
            castButtonRect.anchoredPosition = Vector2.zero;
            castButtonRect.localRotation = Quaternion.identity;
            castButtonRect.localScale = Vector3.one;

            if (castLabelTMP != null)
            {
                castLabelTMP.text = "CAST";
                castLabelTMP.color = new Color(1f, 0.94f, 0.65f);
            }
        }

        // 2. Center Magic Circle behind the spinning CAST icon
        if (runeCircleRect != null)
        {
            runeCircleRect.gameObject.SetActive(true);
            runeCircleRect.anchorMin = new Vector2(0.5f, 0.5f);
            runeCircleRect.anchorMax = new Vector2(0.5f, 0.5f);
            runeCircleRect.pivot = new Vector2(0.5f, 0.5f);
            runeCircleRect.sizeDelta = new Vector2(400f, 400f);
            runeCircleRect.anchoredPosition = Vector2.zero;
        }

        // 3. Activate Dispel Button on bottom-left above controls
        if (dispelButtonRect != null)
        {
            dispelButtonRect.gameObject.SetActive(true);
            dispelButtonRect.anchorMin = DISPEL_MIN;
            dispelButtonRect.anchorMax = DISPEL_MAX;
            dispelButtonRect.offsetMin = Vector2.zero;
            dispelButtonRect.offsetMax = Vector2.zero;
            dispelButtonRect.anchoredPosition = Vector2.zero;
        }

        // Play primed audio feedback
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
        }
    }

    /// <summary>
    /// Player pressed the spinning CAST icon: cast the primed spell!
    /// </summary>
    public void OnCastButtonPressed()
    {
        if (!isSpellPrimed) return;

        Debug.Log($"🔥 [SPELL CAST] Player pressed spinning CAST icon! Executing '{primedSpellName}' ({primedElement}).");

        // 1. Dispatch spellcast to subsystems
        if (RuneShapeCastingEngine.Instance != null)
        {
            RuneShapeCastingEngine.Instance.FinalizeCastButton(Vector2.up);
        }

        if (ModularSpellEngine.Instance != null)
        {
            SpellCatalyst cat = primedElement switch
            {
                SpellElement.Healing => SpellCatalyst.Wind,
                SpellElement.Fire => SpellCatalyst.Fire,
                SpellElement.Rock => SpellCatalyst.Earth,
                SpellElement.Water => SpellCatalyst.Wind,
                _ => SpellCatalyst.Fire
            };
            ModularSpellEngine.Instance.CastModularSpell(cat, SpellGeometry.Singularity, SpellBehavior.ChainDetonation);
        }

        OnSpellCastTriggered?.Invoke(primedElement);

        // 2. Play Cast Impact Audio Feedback
        if (BattleSummonerAudioMaster.Instance != null)
        {
            string sfx = primedElement switch
            {
                SpellElement.Healing => "relic_chime",
                SpellElement.Fire => "spell_fireball_launch",
                SpellElement.Rock => "cinematic_boom",
                _ => "apex_celestial_roar"
            };
            BattleSummonerAudioMaster.Instance.PlayPresetSFX(sfx, 1.0f);
        }

        // 3. Hide spinning CAST icon and Rune Circle, but keep DISPEL button active for locked spell state!
        isSpellPrimed = false;
        if (castButtonRect != null) castButtonRect.gameObject.SetActive(false);
        if (runeCircleRect != null) runeCircleRect.gameObject.SetActive(false);
        if (dispelButtonRect != null) dispelButtonRect.gameObject.SetActive(true);
    }

    /// <summary>
    /// Player pressed the DISPEL button: dispels the spell casting / locked spell, restoring neutral stance.
    /// </summary>
    public void OnDispelButtonPressed()
    {
        bool wasLocked = (BattleSummoner.Combat.ActiveSpellLockManager.Instance != null && BattleSummoner.Combat.ActiveSpellLockManager.Instance.IsLockedInSpell);
        if (!isSpellPrimed && !wasLocked) return;

        Debug.Log($"💨 [SPELL DISPELLED] Player pressed DISPEL button. Dispelling active spell lock and clearing spellcasting state.");

        if (BattleSummoner.Combat.ActiveSpellLockManager.Instance != null)
        {
            BattleSummoner.Combat.ActiveSpellLockManager.Instance.DispelActiveSpell();
        }

        // 1. Clear active drawn points in casting engine
        if (RuneShapeCastingEngine.Instance != null)
        {
            RuneShapeCastingEngine.Instance.DispelDrawnRune();
        }

        // 2. Play dispel fizzle audio feedback
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("glitch", 0.7f);
        }

        OnSpellDispelled?.Invoke();

        // 3. Reset state & return to resting layout
        isSpellPrimed = false;
        SetRestingLayout();
    }

    /// <summary>
    /// Restores CAST button and Magic Circle to resting layout, and hides Dispel button.
    /// </summary>
    public void SetRestingLayout()
    {
        if (castButtonRect != null)
        {
            if (hideCastButtonWhenNotPrimed)
            {
                castButtonRect.gameObject.SetActive(false);
            }
            else
            {
                castButtonRect.gameObject.SetActive(true);
                castButtonRect.anchorMin = REST_CAST_MIN;
                castButtonRect.anchorMax = REST_CAST_MAX;
                castButtonRect.pivot = new Vector2(0.5f, 0.5f);
                castButtonRect.sizeDelta = Vector2.zero;
                castButtonRect.anchoredPosition = Vector2.zero;
                castButtonRect.localRotation = Quaternion.identity;
                castButtonRect.localScale = Vector3.one;
            }
        }

        if (runeCircleRect != null)
        {
            if (hideCastButtonWhenNotPrimed)
            {
                runeCircleRect.gameObject.SetActive(false);
            }
            else
            {
                runeCircleRect.gameObject.SetActive(true);
                runeCircleRect.anchorMin = REST_RUNE_MIN;
                runeCircleRect.anchorMax = REST_RUNE_MAX;
                runeCircleRect.pivot = new Vector2(0.5f, 0.5f);
                runeCircleRect.sizeDelta = Vector2.zero;
                runeCircleRect.anchoredPosition = Vector2.zero;
                runeCircleRect.localRotation = Quaternion.identity;
                runeCircleRect.localScale = Vector3.one;
            }
        }

        if (dispelButtonRect != null)
        {
            dispelButtonRect.gameObject.SetActive(false);
        }
    }

    [ContextMenu("Simulate Spell Drawn (Test)")]
    public void SimulateSpellDrawn()
    {
        PrimeSpell(SpellElement.Fire, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), 120f, "Simulated Fireball");
    }
}
