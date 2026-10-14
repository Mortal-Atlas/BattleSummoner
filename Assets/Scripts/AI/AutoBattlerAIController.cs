using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// AutoBattlerAIController provides autonomous ARPG combat automation:
/// - Target acquisition: scans for nearest SpireEnemy or BossController
/// - Navigation & positioning: moves into weapon attack distance
/// - Weapon arts & combos: executes directional slashes and combos for Warrior, spells for Mage
/// - Survival clutch: automatically uses health potions when HP drops below 35%
/// </summary>
public class AutoBattlerAIController : MonoBehaviour
{
    public static AutoBattlerAIController Instance { get; private set; }

    [Header("Auto Battler Status")]
    [SerializeField] private bool isAutoBattlerActive = false;
    [SerializeField] private float scanRadius = 18f;
    [SerializeField] private float attackRange = 3.8f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float actionCooldown = 0.55f;

    [Header("Clutch Potions")]
    [SerializeField] private float autoHealHpThresholdPercent = 0.38f;
    [SerializeField] private float potionCooldown = 4.0f;
    private float lastPotionTime = -10f;

    private CharacterController characterController;
    private PlayerMovementController movementController;
    private PlayerProgression playerProgression;
    private DevClassLoadoutManager devLoadout;
    private WeaponArtGestureController weaponArt;
    private Transform currentTarget;
    private float nextActionTime = 0f;
    private int comboCounter = 0;

    public bool IsActive => isAutoBattlerActive;
    public event Action<bool> OnAutoBattlerToggled;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        ResolveReferences();
    }

    public void ResolveReferences()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (movementController == null) movementController = GetComponent<PlayerMovementController>();
        if (playerProgression == null) playerProgression = GetComponent<PlayerProgression>();
        if (devLoadout == null) devLoadout = GetComponent<DevClassLoadoutManager>();
        if (weaponArt == null) weaponArt = GetComponent<WeaponArtGestureController>();
    }

    public void ToggleAutoBattler()
    {
        SetAutoBattlerActive(!isAutoBattlerActive);
    }

    public void SetAutoBattlerActive(bool active)
    {
        isAutoBattlerActive = active;
        OnAutoBattlerToggled?.Invoke(isAutoBattlerActive);

        if (isAutoBattlerActive)
        {
            Debug.Log("🤖⚔️ [AUTO BATTLER AI] Engaged! Autonomous combat routines active.");
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
            }
            if (devLoadout != null)
            {
                devLoadout.ShowToast("<color=#30FF80>[AUTO-BATTLER AI ACTIVE]</color>\nAutonomous combat routine engaged!");
            }
        }
        else
        {
            Debug.Log("🤖🛑 [AUTO BATTLER AI] Disengaged. Manual control restored.");
            currentTarget = null;
            if (devLoadout != null)
            {
                devLoadout.ShowToast("<color=#FFD700>[AUTO-BATTLER AI OFF]</color>\nManual touch control restored.");
            }
        }
    }

    private void Update()
    {
        if (!isAutoBattlerActive) return;

        CheckEmergencyPotions();
        UpdateCombatRoutine();
    }

    private void CheckEmergencyPotions()
    {
        if (playerProgression == null) return;

        if (Time.time - lastPotionTime >= potionCooldown)
        {
            float hpRatio = playerProgression.currentHp / Mathf.Max(1f, playerProgression.maxHp);
            if (hpRatio < autoHealHpThresholdPercent)
            {
                lastPotionTime = Time.time;
                playerProgression.Heal(220f);
                Debug.Log($"🤖🧪 [AUTO BATTLER] Health clutch triggered! Consumed Health Potion (+220 HP).");
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
                }
            }
        }
    }

    private void UpdateCombatRoutine()
    {
        // 1. Acquire or validate target
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = FindNearestTarget();
        }

        if (currentTarget == null)
        {
            // Idle or slow orbit around player origin
            return;
        }

        // 2. Compute distance and vector to target
        Vector3 targetPos = currentTarget.position;
        targetPos.y = transform.position.y;
        Vector3 toTarget = targetPos - transform.position;
        float distance = toTarget.magnitude;

        // 3. Face target
        if (toTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        }

        // 4. Movement: approach target if beyond attack range
        if (distance > attackRange)
        {
            Vector3 moveDir = toTarget.normalized * 4.8f;
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveDir * Time.deltaTime + Vector3.down * 9.8f * Time.deltaTime);
            }
        }
        else
        {
            // Within strike distance! Execute combat actions
            if (Time.time >= nextActionTime)
            {
                nextActionTime = Time.time + actionCooldown;
                ExecuteAutonomousAttack(toTarget.normalized);
            }
        }
    }

    private void ExecuteAutonomousAttack(Vector3 aimDirection)
    {
        if (devLoadout == null) return;

        comboCounter++;

        switch (devLoadout.CurrentClass)
        {
            case PlayerClassType.Warrior:
                // Alternate directional slash angles: 45 deg, -45 deg, 90 deg, then unleash combo finisher!
                if (comboCounter % 4 == 0)
                {
                    // Unleash combo finisher
                    string[] combos = { "CIRCLE_COMBO", "X_COMBO", "SQUARE_COMBO", "STAR_COMBO" };
                    string chosenCombo = combos[(comboCounter / 4) % combos.Length];
                    devLoadout.ExecuteWeaponArtCombo(chosenCombo);
                }
                else
                {
                    float[] angles = { 45f, -45f, 90f, -90f };
                    float chosenAngle = angles[comboCounter % angles.Length];
                    Vector2 swipe2D = new Vector2(Mathf.Cos(chosenAngle * Mathf.Deg2Rad), Mathf.Sin(chosenAngle * Mathf.Deg2Rad));
                    devLoadout.ExecuteWarriorSwordSwing(swipe2D, 1450f, chosenAngle);
                }
                break;

            case PlayerClassType.Mage:
                devLoadout.CastSpellProjectile(aimDirection);
                break;

            case PlayerClassType.Bard:
                string[] notes = { "C4", "E4", "G4", "B4" };
                float[] freqs = { 261.63f, 329.63f, 392.00f, 493.88f };
                int noteIdx = comboCounter % notes.Length;
                devLoadout.TriggerBardNote(notes[noteIdx], freqs[noteIdx]);
                break;
        }
    }

    private Transform FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, ~0, QueryTriggerInteraction.Ignore);
        Transform closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col.CompareTag("Player") || col.transform.IsChildOf(transform)) continue;

            bool isEnemy = col.GetComponent<SpireEnemy>() != null || col.GetComponent<BossController>() != null;
            if (isEnemy)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.transform;
                }
            }
        }

        return closest;
    }
}
