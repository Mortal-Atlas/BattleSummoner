using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Combat;

public enum PlayerClassType
{
    Warrior,
    Mage,
    Bard,
    Ranger
}

/// <summary>
/// DevClassLoadoutManager provides instant runtime cycling and testing of all class loadouts:
/// - Numpad 1: Warrior (Spire Sword with directional swiping & raycast distance damage)
/// - Numpad 2: Mage (Astral Staff casting ballistic projectile spells & rune matrices)
/// - Numpad 3: Bard (Celestiphone Instrument playing notes, melody recognition & harmonic shockwaves)
/// - Numpad 4: Ranger (Elven Recurve Bow with ballistic arrows & Over-The-Shoulder Scope ADS)
/// Also supports Alpha1-4, Tab cycling, and an on-screen clickable dev switcher HUD.
/// </summary>
public class DevClassLoadoutManager : MonoBehaviour
{
    private static DevClassLoadoutManager _instance;
    public static DevClassLoadoutManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<DevClassLoadoutManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Current Class")]
    [SerializeField] private PlayerClassType currentClass = PlayerClassType.Warrior;

    [Header("Warrior Combat Parameters")]
    [SerializeField] private float swordAttackDistance = 4.5f;
    [SerializeField] private float swordDamage = 65f;
    [SerializeField] private float swordRaycastRadius = 0.85f;

    [Header("Mage Combat Parameters")]
    [SerializeField] private float spellDamage = 95f;
    [SerializeField] private float spellExplosionRadius = 3.5f;
    [SerializeField] private float spellSpeed = 22f;

    [Header("Bard Combat Parameters")]
    [SerializeField] private float harmonicWaveDamage = 35f;
    [SerializeField] private float harmonicWaveRadius = 6.5f;

    [Header("Ranger Combat Parameters")]
    [SerializeField] private float arrowDamage = 85f;
    [SerializeField] private float arrowSpeed = 28f;
    [SerializeField] private float arrowAdsSpeed = 38f;

    [Header("Subsystem References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerProgression playerProgression;
    [SerializeField] private WeaponTemper weaponTemper;
    [SerializeField] private ModularSpellEngine spellEngine;
    [SerializeField] private InstrumentCatalyst instrumentCatalyst;
    [SerializeField] private PortraitGestureCanvas gestureCanvas;

    // 3D Visual Weapon Sockets
    private GameObject warriorSwordObj;
    private GameObject mageStaffObj;
    private GameObject bardInstrumentObj;
    private GameObject rangerBowObj;
    private Transform staffOrbLight;

    // UI Dev Switcher HUD
    private GameObject devHudRoot;
    private TextMeshProUGUI activeClassToastTMP;
    private Button warriorBtn;
    private Button mageBtn;
    private Button bardBtn;
    private Button rangerBtn;
    private Coroutine toastCoroutine;

    public PlayerClassType CurrentClass => currentClass;
    public bool IsHoldingProjectileWeapon => currentClass == PlayerClassType.Ranger || currentClass == PlayerClassType.Mage;
    public static event Action<PlayerClassType> OnClassChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        ResolveReferences();
        CreateVisualWeapons();
    }

    private void Start()
    {
        CreateDevHUDOverlay();
        // Initialize default loadout
        ApplyClassLoadout(currentClass, true);

        // Attach & configure WeaponArtGestureController
        var weaponArt = GetComponent<WeaponArtGestureController>();
        if (weaponArt == null) weaponArt = gameObject.AddComponent<WeaponArtGestureController>();
        weaponArt.OnDirectionalSlashExecuted += HandleWeaponArtSlash;
        weaponArt.OnComboRecognized += HandleWeaponArtCombo;
        weaponArt.ResolveWeaponReferences();

        // Attach & configure PlayerCombatStanceController
        var stanceCtrl = GetComponent<PlayerCombatStanceController>();
        if (stanceCtrl == null) stanceCtrl = gameObject.AddComponent<PlayerCombatStanceController>();
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnBladeSlash += HandleBladeSlash;
        PortraitGestureCanvas.OnSpellTapCast += HandleSpellTapCast;
        MelodyRecognizer.OnNotePushedToBuffer += HandleBardNotePushed;
        if (WeaponArtGestureController.Instance != null)
        {
            WeaponArtGestureController.Instance.OnDirectionalSlashExecuted += HandleWeaponArtSlash;
            WeaponArtGestureController.Instance.OnComboRecognized += HandleWeaponArtCombo;
        }
        if (RuneShapeCastingEngine.Instance != null)
        {
            RuneShapeCastingEngine.Instance.OnSpellCastDispatched += HandleRuneSpellCastDispatched;
        }
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnBladeSlash -= HandleBladeSlash;
        PortraitGestureCanvas.OnSpellTapCast -= HandleSpellTapCast;
        MelodyRecognizer.OnNotePushedToBuffer -= HandleBardNotePushed;
        if (WeaponArtGestureController.Instance != null)
        {
            WeaponArtGestureController.Instance.OnDirectionalSlashExecuted -= HandleWeaponArtSlash;
            WeaponArtGestureController.Instance.OnComboRecognized -= HandleWeaponArtCombo;
        }
        if (RuneShapeCastingEngine.Instance != null)
        {
            RuneShapeCastingEngine.Instance.OnSpellCastDispatched -= HandleRuneSpellCastDispatched;
        }
    }

    private void Update()
    {
        HandleKeyboardInputs();
    }

    #region Input Handling & Loadout Toggling
    private void HandleKeyboardInputs()
    {
        // Numpad 1 or Alpha 1 -> Warrior
        if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetClassLoadout(PlayerClassType.Warrior);
        }
        // Numpad 2 or Alpha 2 -> Mage
        else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetClassLoadout(PlayerClassType.Mage);
        }
        // Numpad 3 or Alpha 3 -> Bard
        else if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetClassLoadout(PlayerClassType.Bard);
        }
        // Numpad 4 or Alpha 4 -> Ranger
        else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetClassLoadout(PlayerClassType.Ranger);
        }
        // Cycle Next Class: Tab or Numpad 0 or Alpha 0
        else if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
        {
            CycleNextClass();
        }

        // Desktop Dev Action Key (Space / X)
        if (Input.GetKeyDown(KeyCode.X))
        {
            ExecuteQuickDevAction();
        }

        // Desktop Bard note shortcuts (Keypad 4-9 or Q-U)
        if (currentClass == PlayerClassType.Bard)
        {
            if (Input.GetKeyDown(KeyCode.Q)) TriggerBardNote("C4", 261.63f);
            if (Input.GetKeyDown(KeyCode.W)) TriggerBardNote("D4", 293.66f);
            if (Input.GetKeyDown(KeyCode.E)) TriggerBardNote("E4", 329.63f);
            if (Input.GetKeyDown(KeyCode.R)) TriggerBardNote("F4", 349.23f);
            if (Input.GetKeyDown(KeyCode.T)) TriggerBardNote("G4", 392.00f);
            if (Input.GetKeyDown(KeyCode.Y)) TriggerBardNote("A4", 440.00f);
            if (Input.GetKeyDown(KeyCode.U)) TriggerBardNote("B4", 493.88f);
        }
    }

    public void CycleNextClass()
    {
        int next = ((int)currentClass + 1) % 4;
        SetClassLoadout((PlayerClassType)next);
    }

    public void SetClassLoadout(PlayerClassType newClass)
    {
        if (currentClass == newClass) return;
        currentClass = newClass;
        ApplyClassLoadout(currentClass, false);
    }

    private void ApplyClassLoadout(PlayerClassType targetClass, bool isInitial)
    {
        // 1. Play loadout audio chime
        if (!isInitial && BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.85f);
        }

        // 2. Toggle 3D visual weapon models
        if (warriorSwordObj != null) warriorSwordObj.SetActive(targetClass == PlayerClassType.Warrior);
        if (mageStaffObj != null) mageStaffObj.SetActive(targetClass == PlayerClassType.Mage);
        if (bardInstrumentObj != null) bardInstrumentObj.SetActive(targetClass == PlayerClassType.Bard);
        if (rangerBowObj != null) rangerBowObj.SetActive(targetClass == PlayerClassType.Ranger);

        // 3. Resolve interaction mode and instrument UI
        if (gestureCanvas == null) gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();

        switch (targetClass)
        {
            case PlayerClassType.Warrior:
                if (instrumentCatalyst != null) instrumentCatalyst.UnequipInstrument();
                if (PortraitInstrumentUI.Instance != null) PortraitInstrumentUI.Instance.SetVisible(false);
                if (gestureCanvas != null) gestureCanvas.currentMode = InteractionMode.Combat;
                ShowToast("<color=#30FF80>[WARRIOR EQUIPPED]</color>\nSteel Spire Claymore. Swipe to Slash & Raycast Hit enemies in range!");
                Debug.Log("[DEV LOADOUT] Active Class: WARRIOR (1). Sword equipped. Swipe gestures active.");
                break;

            case PlayerClassType.Mage:
                if (instrumentCatalyst != null) instrumentCatalyst.UnequipInstrument();
                if (PortraitInstrumentUI.Instance != null) PortraitInstrumentUI.Instance.SetVisible(false);
                if (gestureCanvas != null) gestureCanvas.currentMode = InteractionMode.Spellcraft;
                ShowToast("<color=#00D8FF>[MAGE EQUIPPED]</color>\nArchmage Astral Staff. Tap / Click to cast Fireball Projectiles, draw Circle for Singularity! Press SCOPE for OTS view.");
                Debug.Log("[DEV LOADOUT] Active Class: MAGE (2). Staff equipped. Spellcraft mode active.");
                break;

            case PlayerClassType.Bard:
                if (instrumentCatalyst != null) instrumentCatalyst.EquipInstrument();
                if (PortraitInstrumentUI.Instance != null) PortraitInstrumentUI.Instance.SetVisible(true);
                if (gestureCanvas != null) gestureCanvas.currentMode = InteractionMode.InstrumentKalimba;
                ShowToast("<color=#FFD700>[BARD EQUIPPED]</color>\nAstral Celestiphone. Kalimba active! Tap notes to radiate Sonic Harmonic shockwaves & play music.");
                Debug.Log("[DEV LOADOUT] Active Class: BARD (3). Celestiphone equipped. Kalimba V-Shape overlay active.");
                break;

            case PlayerClassType.Ranger:
                if (instrumentCatalyst != null) instrumentCatalyst.UnequipInstrument();
                if (PortraitInstrumentUI.Instance != null) PortraitInstrumentUI.Instance.SetVisible(false);
                if (gestureCanvas != null) gestureCanvas.currentMode = InteractionMode.Spellcraft;
                ShowToast("<color=#40FF90>[RANGER EQUIPPED]</color>\nElven Recurve Bow. Tap / Click screen to Shoot Arrows. Tap SCOPE for Over-The-Shoulder laser accuracy!");
                Debug.Log("[DEV LOADOUT] Active Class: RANGER (4). Recurve Bow equipped. Projectile targeting active.");
                break;
        }

        UpdateHUDButtons();
        OnClassChanged?.Invoke(targetClass);
    }
    #endregion

    #region Warrior: Sword Slash & Raycast Hit Detection
    private void HandleBladeSlash(Vector2 swipeDir, float velocity)
    {
        if (currentClass != PlayerClassType.Warrior) return;
        float angleDeg = Mathf.Atan2(swipeDir.y, swipeDir.x) * Mathf.Rad2Deg;
        ExecuteWarriorSwordSwing(swipeDir, velocity, angleDeg);
    }

    private void HandleWeaponArtSlash(Vector2 swipeDir, float velocity)
    {
        if (currentClass != PlayerClassType.Warrior) return;
        float angleDeg = Mathf.Atan2(swipeDir.y, swipeDir.x) * Mathf.Rad2Deg;
        ExecuteWarriorSwordSwing(swipeDir, velocity, angleDeg);
    }

    private void HandleWeaponArtCombo(string comboPattern)
    {
        if (currentClass != PlayerClassType.Warrior) return;
        ExecuteWeaponArtCombo(comboPattern);
    }

    public void ExecuteWarriorSwordSwing(Vector2 swipeDir, float velocity, float? overrideAngleDeg = null)
    {
        // 1. Audio whoosh
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 0.95f);
        }

        // 2. Animate 3D Sword visual swing
        if (warriorSwordObj != null)
        {
            StartCoroutine(AnimateSwordSwingRoutine());
        }

        // 3. Direction projection from 2D swipe to 3D world space
        Vector3 camFwd = Camera.main != null ? Camera.main.transform.forward : transform.forward;
        Vector3 camRight = Camera.main != null ? Camera.main.transform.right : transform.right;
        camFwd.y = 0f; camFwd.Normalize();
        camRight.y = 0f; camRight.Normalize();

        Vector3 slashWorldDir = (camRight * swipeDir.x + camFwd * swipeDir.y).normalized;
        if (slashWorldDir == Vector3.zero || slashWorldDir.sqrMagnitude < 0.01f)
        {
            slashWorldDir = transform.forward;
        }

        float swipeAngleDeg = overrideAngleDeg ?? (Mathf.Atan2(swipeDir.y, swipeDir.x) * Mathf.Rad2Deg);

        // 4. Raycast / SphereCast hit detection:
        // "anything that is caught in the ray cast and is a certain amount of distance from the character take damage"
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
        RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, swordRaycastRadius, slashWorldDir, swordAttackDistance, ~0, QueryTriggerInteraction.Ignore);

        int hitCount = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider col = hit.collider;
            if (col.CompareTag("Player") || col.transform.IsChildOf(transform)) continue;

            float dist = Vector3.Distance(transform.position, hit.point);
            if (dist > swordAttackDistance + 0.5f) continue;

            bool struck = false;
            float effectiveDmg = swordDamage;
            if (BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance != null)
            {
                effectiveDmg *= BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance.GetDamageMultiplier();
            }

            // Check Riposte Critical Boost (3.0x from Two-Finger Parry)
            float riposte = 1f;
            if (PlayerCombatStanceController.Instance != null && PlayerCombatStanceController.Instance.ConsumeRiposteBoost(out riposte))
            {
                effectiveDmg *= riposte;
            }

            if (col.TryGetComponent<SpireEnemy>(out var enemy))
            {
                enemy.TakeDamage(effectiveDmg);
                enemy.Stagger(0.65f, transform.position);
                struck = true;
                CheckDungeonVampiricLeech(effectiveDmg);
            }
            else if (col.TryGetComponent<BossController>(out var boss))
            {
                boss.TakeDamage(effectiveDmg);
                boss.Stagger(0.35f, transform.position);
                struck = true;
                CheckDungeonVampiricLeech(effectiveDmg);
            }
            else if (col.TryGetComponent<CaveMiningNode>(out var node))
            {
                node.ExecuteChiselStrike();
                struck = true;
            }

            if (struck)
            {
                hitCount++;
                WarriorSwordSlashVFX.SpawnHitSpark(hit.point, hit.normal);
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_impact", 0.9f);
                }
                Debug.Log($"[WARRIOR SWORD RAYCAST] Struck {col.name} at distance {dist:F2}m for {swordDamage} damage!");
            }
        }

        // 5. Spawn Visual Slash Arc VFX with directional tilt and propagation
        WarriorSwordSlashVFX.SpawnSlashWave(rayOrigin + slashWorldDir * 0.8f, slashWorldDir, swipeAngleDeg, velocity);

        // If Dual Blades: spawn second simultaneous sword slash wave with distinct golden amber tint!
        if (PlayerCombatStanceController.Instance != null && PlayerCombatStanceController.Instance.IsDualWielding)
        {
            WarriorSwordSlashVFX.SpawnSlashWave(
                rayOrigin + slashWorldDir * 0.8f + camRight * 0.35f,
                slashWorldDir,
                swipeAngleDeg + 25f,
                velocity * 0.95f,
                new Color(1f, 0.75f, 0.20f)
            );
        }

        // 6. Wear on edge
        if (weaponTemper != null)
        {
            weaponTemper.ApplySlashWear(velocity > 100f ? velocity : 1000f);
        }
    }

    public void ExecuteWeaponArtCombo(string comboPattern)
    {
        Debug.Log($"[WEAPON ART COMBO] Unleashing Weapon Art: {comboPattern}!");
        ShowToast($"<color=#FFD700>[WEAPON ART: {comboPattern}]</color>\nDevastating Spire Blade Combo unleashed!");

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("anvil_shatter", 0.9f);
        }

        Vector3 center = transform.position;
        switch (comboPattern)
        {
            case "CIRCLE_COMBO":
                // Spire Whirlwind: 8-way radial slashes
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * 45f * Mathf.Deg2Rad;
                    Vector3 radialDir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                    WarriorSwordSlashVFX.SpawnSlashWave(center + Vector3.up * 1f + radialDir * 0.8f, radialDir, i * 45f, 1800f);
                }
                DamageEnemiesInArea(center, 7.5f, swordDamage * 2.2f, 1.2f);
                break;

            case "X_COMBO":
                // Dual Crossing Cleave (+45 and -45 deg)
                Vector3 fwd = transform.forward;
                WarriorSwordSlashVFX.SpawnSlashWave(center + Vector3.up * 1f + fwd * 0.5f, fwd, 45f, 2200f);
                WarriorSwordSlashVFX.SpawnSlashWave(center + Vector3.up * 1f + fwd * 0.5f, fwd, -45f, 2200f);
                DamageEnemiesInArea(center + fwd * 2f, 4.5f, swordDamage * 2.5f, 1.5f);
                break;

            case "SQUARE_COMBO":
                // Spire Bastion: 4 cardinal slashes
                Vector3[] dirs = { transform.forward, transform.right, -transform.forward, -transform.right };
                for (int i = 0; i < dirs.Length; i++)
                {
                    WarriorSwordSlashVFX.SpawnSlashWave(center + Vector3.up * 1f + dirs[i] * 0.8f, dirs[i], i * 90f, 1500f);
                }
                DamageEnemiesInArea(center, 5.5f, swordDamage * 1.8f, 1.0f);
                break;

            case "STAR_COMBO":
            default:
                // Starfall Cleave: 5 celestial slashes
                for (int i = 0; i < 5; i++)
                {
                    float ang = i * 72f * Mathf.Deg2Rad;
                    Vector3 starDir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                    WarriorSwordSlashVFX.SpawnSlashWave(center + Vector3.up * 1f + starDir * 0.8f, starDir, i * 72f, 2400f);
                }
                DamageEnemiesInArea(center, 8f, swordDamage * 3.0f, 2.0f);
                break;
        }
    }

    private void DamageEnemiesInArea(Vector3 center, float radius, float damage, float staggerDuration)
    {
        if (BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance != null)
        {
            damage *= BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance.GetDamageMultiplier();
        }

        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col.CompareTag("Player") || col.transform.IsChildOf(transform)) continue;

            if (col.TryGetComponent<SpireEnemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
                enemy.Stagger(staggerDuration, center);
                WarriorSwordSlashVFX.SpawnHitSpark(col.transform.position + Vector3.up * 1f, Vector3.up);
                CheckDungeonVampiricLeech(damage);
            }
            else if (col.TryGetComponent<BossController>(out var boss))
            {
                boss.TakeDamage(damage);
                boss.Stagger(staggerDuration * 0.6f, center);
                WarriorSwordSlashVFX.SpawnHitSpark(col.transform.position + Vector3.up * 1f, Vector3.up);
                CheckDungeonVampiricLeech(damage);
            }
        }
    }

    private void CheckDungeonVampiricLeech(float dmg)
    {
        if (BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance != null && playerProgression != null)
        {
            float leech = BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance.GetLifeStealPercent();
            if (leech > 0f)
            {
                playerProgression.currentHp = Mathf.Min(playerProgression.currentHp + dmg * leech, playerProgression.maxHp);
            }
        }
    }

    private void HandleRuneSpellCastDispatched(SpellElement element, Vector2 launchDir)
    {
        if (currentClass != PlayerClassType.Mage) return;
        Vector3 camFwd = Camera.main != null ? Camera.main.transform.forward : transform.forward;
        Vector3 camRight = Camera.main != null ? Camera.main.transform.right : transform.right;
        camFwd.y = 0f; camFwd.Normalize();
        camRight.y = 0f; camRight.Normalize();
        Vector3 castWorldDir = (camRight * launchDir.x + camFwd * launchDir.y).normalized;
        if (castWorldDir.sqrMagnitude < 0.01f) castWorldDir = transform.forward;

        Debug.Log($"[MAGE RUNE SPELL] Launching {element} spell projectile in direction {castWorldDir}");
        CastSpellProjectile(castWorldDir);
    }

    private IEnumerator AnimateSwordSwingRoutine()
    {
        if (warriorSwordObj == null) yield break;

        Quaternion initialRot = warriorSwordObj.transform.localRotation;
        Quaternion swingStartRot = Quaternion.Euler(-45f, -60f, 30f);
        Quaternion swingEndRot = Quaternion.Euler(75f, 80f, -40f);

        float duration = 0.16f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            warriorSwordObj.transform.localRotation = Quaternion.Slerp(swingStartRot, swingEndRot, t);
            yield return null;
        }

        warriorSwordObj.transform.localRotation = initialRot;
    }

    private void SpawnSwordSlashArc(Vector3 position, Vector3 direction)
    {
        WarriorSwordSlashVFX.SpawnSlashWave(position, direction, 0f, 1200f);
    }
    #endregion

    #region Mage & Ranger: Ballistic Projectile & Arrow Firing
    private void HandleSpellTapCast(Vector2 screenPos)
    {
        if (currentClass != PlayerClassType.Mage && currentClass != PlayerClassType.Ranger) return;

        Vector3 targetPoint = Vector3.zero;
        bool hasHitTarget = false;
        bool isADS = WeaponScopeADSController.Instance != null && WeaponScopeADSController.Instance.IsAimingDownSights;

        if (isADS && Camera.main != null)
        {
            Ray centerRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.55f, 0f));
            if (Physics.Raycast(centerRay, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    targetPoint = hit.point;
                    hasHitTarget = true;
                }
            }
            if (!hasHitTarget)
            {
                targetPoint = centerRay.origin + centerRay.direction * 50f;
                hasHitTarget = true;
            }
        }
        else if (Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 60f))
            {
                targetPoint = hit.point;
                hasHitTarget = true;
            }
        }

        Vector3 castDirection = hasHitTarget ? (targetPoint - (transform.position + Vector3.up * 1.3f)) : transform.forward;
        if (!isADS) castDirection.y = 0f;
        if (castDirection.sqrMagnitude < 0.1f) castDirection = transform.forward;
        castDirection.Normalize();

        if (currentClass == PlayerClassType.Ranger)
        {
            ShootRangerArrow(castDirection);
        }
        else if (currentClass == PlayerClassType.Mage)
        {
            CastSpellProjectile(castDirection);
        }
    }

    public void ShootRangerArrow(Vector3 direction)
    {
        bool isADS = WeaponScopeADSController.Instance != null && WeaponScopeADSController.Instance.IsAimingDownSights;

        // Spread modifier: 0 spread in ADS, slight dispersion in hipfire
        if (!isADS)
        {
            Vector2 spread = UnityEngine.Random.insideUnitCircle * 0.05f;
            direction += new Vector3(spread.x, spread.y * 0.5f, 0f);
            direction.Normalize();
        }

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 0.9f);
        }

        Vector3 spawnPos = transform.position + Vector3.up * 1.35f + direction * 0.85f;
        GameObject arrowObj = new GameObject("RangerArrowProjectile");
        arrowObj.transform.position = spawnPos;
        arrowObj.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Visual Shaft (Cylinder laying flat on Z)
        GameObject shaftObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaftObj.name = "ArrowShaft";
        shaftObj.transform.SetParent(arrowObj.transform, false);
        shaftObj.transform.localPosition = Vector3.zero;
        shaftObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Lay cylinder axis along Z
        shaftObj.transform.localScale = new Vector3(0.025f, 0.42f, 0.025f);
        var shaftCol = shaftObj.GetComponent<Collider>();
        if (shaftCol != null) Destroy(shaftCol);

        // Arrowhead (Pyramid/Cube tip at the front)
        GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headObj.name = "ArrowHead";
        headObj.transform.SetParent(arrowObj.transform, false);
        headObj.transform.localPosition = new Vector3(0f, 0f, 0.44f);
        headObj.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        headObj.transform.localScale = new Vector3(0.045f, 0.045f, 0.09f);
        var headCol = headObj.GetComponent<Collider>();
        if (headCol != null) Destroy(headCol);

        // Fletching (Feathers at the tail)
        GameObject fletchObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fletchObj.name = "ArrowFletching";
        fletchObj.transform.SetParent(arrowObj.transform, false);
        fletchObj.transform.localPosition = new Vector3(0f, 0f, -0.38f);
        fletchObj.transform.localRotation = Quaternion.identity;
        fletchObj.transform.localScale = new Vector3(0.015f, 0.07f, 0.12f);
        var fletchCol = fletchObj.GetComponent<Collider>();
        if (fletchCol != null) Destroy(fletchCol);

        // Materials & Glow
        Material arrowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        arrowMat.color = isADS ? new Color(0.2f, 0.95f, 1.0f) : new Color(0.85f, 0.75f, 0.45f);
        arrowMat.EnableKeyword("_EMISSION");
        arrowMat.SetColor("_EmissionColor", isADS ? new Color(0.1f, 0.8f, 1.0f) * 3f : new Color(0.9f, 0.7f, 0.2f) * 1.5f);
        shaftObj.GetComponent<Renderer>().sharedMaterial = arrowMat;
        fletchObj.GetComponent<Renderer>().sharedMaterial = arrowMat;

        Material tipMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        tipMat.color = isADS ? new Color(0.9f, 0.95f, 1.0f) : new Color(0.6f, 0.65f, 0.7f);
        tipMat.EnableKeyword("_EMISSION");
        tipMat.SetColor("_EmissionColor", isADS ? new Color(0.5f, 0.9f, 1.0f) * 2.5f : new Color(0.4f, 0.4f, 0.4f));
        headObj.GetComponent<Renderer>().sharedMaterial = tipMat;

        // Elegant flight trail
        TrailRenderer trail = arrowObj.AddComponent<TrailRenderer>();
        trail.time = 0.28f;
        trail.startWidth = 0.05f;
        trail.endWidth = 0.0f;
        trail.material = arrowMat;

        float effectiveSpeed = isADS ? arrowAdsSpeed : arrowSpeed;
        float effectiveDamage = arrowDamage;

        RangerArrowProjectile proj = arrowObj.AddComponent<RangerArrowProjectile>();
        proj.Initialize(direction, effectiveDamage, effectiveSpeed, isADS);

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        Debug.Log($"🏹 [RANGER SHOT] Fired arrow in direction {direction} (ADS: {isADS}, Speed: {effectiveSpeed}m/s, Damage: {effectiveDamage}).");
    }

    public void CastSpellProjectile(Vector3 direction)
    {
        // 1. Audio
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("spell_fireball_launch", 0.9f);
        }

        // 2. Staff glow pulse animation
        if (staffOrbLight != null)
        {
            StartCoroutine(PulseStaffLightRoutine());
        }

        // 3. Spawn Spell Projectile GameObject
        Vector3 spawnPos = transform.position + Vector3.up * 1.3f + direction * 0.8f;
        GameObject projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projObj.name = "MageAstralProjectile";
        projObj.transform.position = spawnPos;
        projObj.transform.localScale = Vector3.one * 0.45f;

        var rend = projObj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.15f, 0.8f, 1f, 1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1f) * 4.5f);
            rend.sharedMaterial = mat;
        }

        GameObject pLightObj = new GameObject("ProjectilePointLight", typeof(Light));
        pLightObj.transform.SetParent(projObj.transform, false);
        Light l = pLightObj.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 0.85f, 1f);
        l.intensity = 3.5f;
        l.range = 5f;

        // Add projectile controller
        MageSpellProjectile proj = projObj.AddComponent<MageSpellProjectile>();
        proj.Initialize(direction, spellDamage, spellExplosionRadius, spellSpeed);

        // Turn player slightly toward spell direction
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        Debug.Log($"[MAGE SPELLCAST] Fired Astral Ballistic Projectile in direction {direction}.");
    }

    private IEnumerator PulseStaffLightRoutine()
    {
        if (staffOrbLight == null) yield break;
        Light l = staffOrbLight.GetComponent<Light>();
        if (l == null) yield break;

        float startIntensity = l.intensity;
        l.intensity = 6f;
        yield return new WaitForSeconds(0.12f);
        l.intensity = startIntensity;
    }
    #endregion

    #region Bard: Musical Notes & Harmonic Waves
    private void HandleBardNotePushed(string noteName)
    {
        if (currentClass != PlayerClassType.Bard) return;

        EmitHarmonicWave();
    }

    public void TriggerBardNote(string noteName, float frequency)
    {
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayKalimbaNote(noteName, frequency, 1.0f);
        }

        if (MelodyRecognizer.Instance != null)
        {
            MelodyRecognizer.Instance.RecordTappedNote(noteName);
        }

        EmitHarmonicWave();
    }

    public void EmitHarmonicWave()
    {
        // 1. Audio Strum
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.6f);
        }

        // 2. Spawn Expanding 3D Soundwave Disc
        Vector3 spawnPos = transform.position + Vector3.up * 0.15f;
        GameObject waveObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        waveObj.name = "BardHarmonicSoundwave";
        waveObj.transform.position = spawnPos;
        waveObj.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);

        var col = waveObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = waveObj.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.95f, 0.80f, 0.25f, 0.75f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.3f) * 3.5f);
            rend.sharedMaterial = mat;
        }

        BardHarmonicWaveVFX wave = waveObj.AddComponent<BardHarmonicWaveVFX>();
        wave.Initialize(harmonicWaveDamage, harmonicWaveRadius);
    }
    #endregion

    #region Quick Dev Desktop Actions
    private void ExecuteQuickDevAction()
    {
        switch (currentClass)
        {
            case PlayerClassType.Warrior:
                ExecuteWarriorSwordSwing(Vector2.up, 1200f);
                break;
            case PlayerClassType.Mage:
                CastSpellProjectile(transform.forward);
                break;
            case PlayerClassType.Bard:
                TriggerBardNote("C4", 261.63f);
                break;
            case PlayerClassType.Ranger:
                Vector3 shootDir = transform.forward;
                if (WeaponScopeADSController.Instance != null && WeaponScopeADSController.Instance.IsAimingDownSights && Camera.main != null)
                {
                    shootDir = Camera.main.transform.forward;
                }
                ShootRangerArrow(shootDir);
                break;
        }
    }
    #endregion

    #region 3D Visual Weapon Sockets Construction
    private void CreateVisualWeapons()
    {
        // 1. Warrior Sword
        warriorSwordObj = new GameObject("DevWeapon_Sword");
        warriorSwordObj.transform.SetParent(transform, false);
        warriorSwordObj.transform.localPosition = new Vector3(0.42f, 0.20f, 0.45f);
        warriorSwordObj.transform.localRotation = Quaternion.Euler(-25f, 35f, -10f);

        // Blade
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "SwordBlade";
        blade.transform.SetParent(warriorSwordObj.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        blade.transform.localScale = new Vector3(0.08f, 1.25f, 0.20f);
        DestroyImmediate(blade.GetComponent<Collider>());
        var bRend = blade.GetComponent<Renderer>();
        if (bRend != null)
        {
            Material steelMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            steelMat.color = new Color(0.9f, 0.95f, 1.0f);
            steelMat.SetFloat("_Metallic", 0.92f);
            steelMat.SetFloat("_Smoothness", 0.88f);
            steelMat.EnableKeyword("_EMISSION");
            steelMat.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 1.0f) * 1.5f);
            bRend.sharedMaterial = steelMat;
        }

        // Crossguard
        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Crossguard";
        guard.transform.SetParent(warriorSwordObj.transform, false);
        guard.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        guard.transform.localScale = new Vector3(0.12f, 0.08f, 0.52f);
        DestroyImmediate(guard.GetComponent<Collider>());
        var gRend = guard.GetComponent<Renderer>();
        if (gRend != null)
        {
            Material goldMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            goldMat.color = new Color(0.9f, 0.75f, 0.2f);
            goldMat.SetFloat("_Metallic", 0.9f);
            goldMat.SetFloat("_Smoothness", 0.8f);
            gRend.sharedMaterial = goldMat;
        }

        // Hilt
        GameObject hilt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hilt.name = "Grip";
        hilt.transform.SetParent(warriorSwordObj.transform, false);
        hilt.transform.localPosition = new Vector3(0f, -0.22f, 0f);
        hilt.transform.localScale = new Vector3(0.07f, 0.22f, 0.07f);
        DestroyImmediate(hilt.GetComponent<Collider>());

        // 2. Mage Staff
        mageStaffObj = new GameObject("DevWeapon_Staff");
        mageStaffObj.transform.SetParent(transform, false);
        mageStaffObj.transform.localPosition = new Vector3(0.40f, 0.35f, 0.38f);
        mageStaffObj.transform.localRotation = Quaternion.Euler(15f, 15f, 0f);

        // Staff Shaft
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "StaffShaft";
        shaft.transform.SetParent(mageStaffObj.transform, false);
        shaft.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        shaft.transform.localScale = new Vector3(0.06f, 1.35f, 0.06f);
        DestroyImmediate(shaft.GetComponent<Collider>());
        var sRend = shaft.GetComponent<Renderer>();
        if (sRend != null)
        {
            Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            woodMat.color = new Color(0.15f, 0.12f, 0.18f);
            woodMat.SetFloat("_Metallic", 0.4f);
            woodMat.SetFloat("_Smoothness", 0.7f);
            sRend.sharedMaterial = woodMat;
        }

        // Astral Orb Tip
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "AstralOrb";
        orb.transform.SetParent(mageStaffObj.transform, false);
        orb.transform.localPosition = new Vector3(0f, 1.82f, 0f);
        orb.transform.localScale = Vector3.one * 0.32f;
        DestroyImmediate(orb.GetComponent<Collider>());
        var oRend = orb.GetComponent<Renderer>();
        if (oRend != null)
        {
            Material orbMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            orbMat.color = new Color(0.1f, 0.9f, 1f, 0.9f);
            orbMat.EnableKeyword("_EMISSION");
            orbMat.SetColor("_EmissionColor", new Color(0.15f, 0.9f, 1f) * 4.5f);
            oRend.sharedMaterial = orbMat;
        }

        GameObject orbLightObj = new GameObject("OrbRadianceLight", typeof(Light));
        orbLightObj.transform.SetParent(orb.transform, false);
        Light orbL = orbLightObj.GetComponent<Light>();
        orbL.type = LightType.Point;
        orbL.color = new Color(0.15f, 0.9f, 1f);
        orbL.range = 3.5f;
        orbL.intensity = 2.8f;
        staffOrbLight = orbLightObj.transform;

        // 3. Bard Celestiphone
        bardInstrumentObj = new GameObject("DevWeapon_Instrument");
        bardInstrumentObj.transform.SetParent(transform, false);
        bardInstrumentObj.transform.localPosition = new Vector3(0f, 0.20f, 0.45f);
        bardInstrumentObj.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        GameObject kalimbaBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kalimbaBody.name = "KalimbaSoundboard";
        kalimbaBody.transform.SetParent(bardInstrumentObj.transform, false);
        kalimbaBody.transform.localPosition = Vector3.zero;
        kalimbaBody.transform.localScale = new Vector3(0.55f, 0.08f, 0.40f);
        DestroyImmediate(kalimbaBody.GetComponent<Collider>());
        var kRend = kalimbaBody.GetComponent<Renderer>();
        if (kRend != null)
        {
            Material soundboardMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            soundboardMat.color = new Color(0.85f, 0.65f, 0.25f);
            soundboardMat.SetFloat("_Metallic", 0.7f);
            soundboardMat.SetFloat("_Smoothness", 0.85f);
            kRend.sharedMaterial = soundboardMat;
        }

        // Tines Bar
        GameObject tinesBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tinesBar.name = "TinesBridge";
        tinesBar.transform.SetParent(bardInstrumentObj.transform, false);
        tinesBar.transform.localPosition = new Vector3(0f, 0.06f, 0.05f);
        tinesBar.transform.localScale = new Vector3(0.48f, 0.05f, 0.12f);
        DestroyImmediate(tinesBar.GetComponent<Collider>());
        var tRend = tinesBar.GetComponent<Renderer>();
        if (tRend != null)
        {
            Material silverMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            silverMat.color = new Color(0.85f, 0.92f, 1f);
            silverMat.EnableKeyword("_EMISSION");
            silverMat.SetColor("_EmissionColor", new Color(0.4f, 0.8f, 1f) * 2f);
            tRend.sharedMaterial = silverMat;
        }

        // 4. Ranger Recurve Bow
        rangerBowObj = new GameObject("DevWeapon_Bow");
        rangerBowObj.transform.SetParent(transform, false);
        rangerBowObj.transform.localPosition = new Vector3(-0.35f, 0.25f, 0.35f);
        rangerBowObj.transform.localRotation = Quaternion.Euler(15f, -20f, 25f);

        // Grip
        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        grip.name = "BowGrip";
        grip.transform.SetParent(rangerBowObj.transform, false);
        grip.transform.localPosition = Vector3.zero;
        grip.transform.localScale = new Vector3(0.06f, 0.16f, 0.06f);
        DestroyImmediate(grip.GetComponent<Collider>());
        var bgRend = grip.GetComponent<Renderer>();
        if (bgRend != null)
        {
            Material gMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            gMat.color = new Color(0.35f, 0.22f, 0.12f);
            bgRend.sharedMaterial = gMat;
        }

        // Upper Limb
        GameObject upperLimb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        upperLimb.name = "UpperLimb";
        upperLimb.transform.SetParent(rangerBowObj.transform, false);
        upperLimb.transform.localPosition = new Vector3(0f, 0.42f, 0.08f);
        upperLimb.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        upperLimb.transform.localScale = new Vector3(0.05f, 0.65f, 0.035f);
        DestroyImmediate(upperLimb.GetComponent<Collider>());
        var uRend = upperLimb.GetComponent<Renderer>();
        if (uRend != null)
        {
            Material limbMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            limbMat.color = new Color(0.18f, 0.55f, 0.28f);
            limbMat.SetFloat("_Metallic", 0.4f);
            limbMat.SetFloat("_Smoothness", 0.7f);
            uRend.sharedMaterial = limbMat;
        }

        // Lower Limb
        GameObject lowerLimb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lowerLimb.name = "LowerLimb";
        lowerLimb.transform.SetParent(rangerBowObj.transform, false);
        lowerLimb.transform.localPosition = new Vector3(0f, -0.42f, 0.08f);
        lowerLimb.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        lowerLimb.transform.localScale = new Vector3(0.05f, 0.65f, 0.035f);
        DestroyImmediate(lowerLimb.GetComponent<Collider>());
        var lRend = lowerLimb.GetComponent<Renderer>();
        if (lRend != null)
        {
            Material limbMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            limbMat.color = new Color(0.18f, 0.55f, 0.28f);
            limbMat.SetFloat("_Metallic", 0.4f);
            limbMat.SetFloat("_Smoothness", 0.7f);
            lRend.sharedMaterial = limbMat;
        }

        // Bowstring
        GameObject bowstring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bowstring.name = "Bowstring";
        bowstring.transform.SetParent(rangerBowObj.transform, false);
        bowstring.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        bowstring.transform.localScale = new Vector3(0.012f, 0.78f, 0.012f);
        DestroyImmediate(bowstring.GetComponent<Collider>());
        var bsRend = bowstring.GetComponent<Renderer>();
        if (bsRend != null)
        {
            Material sMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            sMat.color = new Color(0.9f, 0.95f, 1.0f);
            sMat.EnableKeyword("_EMISSION");
            sMat.SetColor("_EmissionColor", new Color(0.4f, 0.9f, 1.0f) * 1.5f);
            bsRend.sharedMaterial = sMat;
        }
    }
    #endregion

    #region On-Screen Dev Switcher HUD
    private void CreateDevHUDOverlay()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Transform safeArea = canvas.transform.Find("SafeAreaContainer") ?? canvas.transform;

        devHudRoot = new GameObject("DevClassSwitcherBar", typeof(RectTransform));
        devHudRoot.transform.SetParent(safeArea, false);
        RectTransform rootRt = devHudRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 1.0f);
        rootRt.anchorMax = new Vector2(0.5f, 1.0f);
        rootRt.anchoredPosition = new Vector2(0f, -125f);
        rootRt.sizeDelta = new Vector2(880f, 90f);

        // Header Title / Key hint
        GameObject hintObj = new GameObject("DevKeyHintTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(devHudRoot.transform, false);
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchoredPosition = new Vector2(0f, 32f);
        hintRt.sizeDelta = new Vector2(850f, 30f);
        TextMeshProUGUI hintTmp = hintObj.GetComponent<TextMeshProUGUI>();
        hintTmp.text = "[DEV QUICK LOADOUT] Press 1: Warrior | 2: Mage | 3: Bard | 4: Ranger (or click below)";
        hintTmp.fontSize = 20;
        hintTmp.color = new Color(1f, 0.85f, 0.35f, 0.95f);
        hintTmp.alignment = TextAlignmentOptions.Center;

        // 4 Buttons: Warrior (1), Mage (2), Bard (3), Ranger (4)
        warriorBtn = CreateHUDButton(devHudRoot.transform, "WarriorBtn", "[W] WARRIOR (1)", new Vector2(-315f, -12f), new Color(0.18f, 0.42f, 0.28f, 0.92f), () => SetClassLoadout(PlayerClassType.Warrior));
        mageBtn = CreateHUDButton(devHudRoot.transform, "MageBtn", "[M] MAGE (2)", new Vector2(-105f, -12f), new Color(0.22f, 0.32f, 0.58f, 0.92f), () => SetClassLoadout(PlayerClassType.Mage));
        bardBtn = CreateHUDButton(devHudRoot.transform, "BardBtn", "[B] BARD (3)", new Vector2(105f, -12f), new Color(0.55f, 0.35f, 0.15f, 0.92f), () => SetClassLoadout(PlayerClassType.Bard));
        rangerBtn = CreateHUDButton(devHudRoot.transform, "RangerBtn", "[R] RANGER (4)", new Vector2(315f, -12f), new Color(0.15f, 0.45f, 0.35f, 0.92f), () => SetClassLoadout(PlayerClassType.Ranger));

        // Toast Feedback Text
        GameObject toastObj = new GameObject("ClassToastTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        toastObj.transform.SetParent(safeArea, false);
        RectTransform toastRt = toastObj.GetComponent<RectTransform>();
        toastRt.anchorMin = new Vector2(0.5f, 1.0f);
        toastRt.anchorMax = new Vector2(0.5f, 1.0f);
        toastRt.anchoredPosition = new Vector2(0f, -140f);
        toastRt.sizeDelta = new Vector2(960f, 65f);
        activeClassToastTMP = toastObj.GetComponent<TextMeshProUGUI>();
        activeClassToastTMP.fontSize = 22;
        activeClassToastTMP.alignment = TextAlignmentOptions.Center;
        activeClassToastTMP.color = Color.white;
        activeClassToastTMP.text = "";
        devHudRoot.SetActive(false);
    }

    private Button CreateHUDButton(Transform parent, string name, string label, Vector2 pos, Color baseColor, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200f, 52f);

        Image img = btnObj.GetComponent<Image>();
        img.color = baseColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("LabelTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private void UpdateHUDButtons()
    {
        if (warriorBtn != null)
        {
            warriorBtn.GetComponent<Image>().color = (currentClass == PlayerClassType.Warrior) ? new Color(0.2f, 0.85f, 0.4f, 1f) : new Color(0.18f, 0.42f, 0.28f, 0.75f);
        }
        if (mageBtn != null)
        {
            mageBtn.GetComponent<Image>().color = (currentClass == PlayerClassType.Mage) ? new Color(0.3f, 0.65f, 1f, 1f) : new Color(0.22f, 0.32f, 0.58f, 0.75f);
        }
        if (bardBtn != null)
        {
            bardBtn.GetComponent<Image>().color = (currentClass == PlayerClassType.Bard) ? new Color(1f, 0.75f, 0.2f, 1f) : new Color(0.55f, 0.35f, 0.15f, 0.75f);
        }
        if (rangerBtn != null)
        {
            rangerBtn.GetComponent<Image>().color = (currentClass == PlayerClassType.Ranger) ? new Color(0.2f, 0.95f, 0.6f, 1f) : new Color(0.15f, 0.45f, 0.35f, 0.75f);
        }
    }

    public void ShowToast(string message)
    {
        if (BattleSummoner.Core.QualityOfLifeMasterController.Instance != null)
        {
            BattleSummoner.Core.QualityOfLifeMasterController.Instance.ShowToast(message, 3.5f);
            return;
        }

        if (activeClassToastTMP == null) return;
        activeClassToastTMP.text = message;

        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastFadeRoutine());
    }

    private IEnumerator ToastFadeRoutine()
    {
        if (activeClassToastTMP == null) yield break;
        activeClassToastTMP.alpha = 1f;
        yield return new WaitForSeconds(3.5f);

        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            if (activeClassToastTMP != null)
            {
                activeClassToastTMP.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.6f);
            }
            yield return null;
        }
    }
    #endregion

    private void ResolveReferences()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (playerProgression == null) playerProgression = GetComponent<PlayerProgression>();
        if (weaponTemper == null) weaponTemper = GetComponent<WeaponTemper>();
        if (spellEngine == null) spellEngine = GetComponent<ModularSpellEngine>();
        if (instrumentCatalyst == null) instrumentCatalyst = GetComponent<InstrumentCatalyst>();
        if (gestureCanvas == null) gestureCanvas = FindAnyObjectByType<PortraitGestureCanvas>();
    }
}
