using UnityEngine;

public class LivingGearSystem : MonoBehaviour
{
    [Header("Living Weapon Identity")]
    public string weaponName = "Voracious Scythe";
    public int egoIndex = 3;
    public float currentSatiety = 75f;
    public float maxSatiety = 100f;

    [Header("Temper / Durability")]
    public float currentTemper = 100f;
    public float maxTemper = 100f;
    public float fractureDebt = 0f;

    [SerializeField] private PlayerProgression player;

    private void Awake()
    {
        if (player == null)
        {
            player = GetComponentInParent<PlayerProgression>();
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerProgression>();
            }
        }
    }

    private void Update()
    {
        // Satiety decays slowly over active combat time
        currentSatiety = Mathf.Max(0f, currentSatiety - Time.deltaTime * 0.4f);

        // Parasitic Soul Syphon if player drops below 20% HP and weapon is starving
        if (currentSatiety < 10f && player != null)
        {
            if (player.currentHp < (player.maxHp * 0.20f))
            {
                player.TakeDirectDamage(5f * Time.deltaTime);
            }
        }
    }

    public float CalculateKineticDamage(float baseDamage)
    {
        float temperEfficiency = Mathf.Max(0.20f, currentTemper / Mathf.Max(1f, maxTemper - fractureDebt));
        return baseDamage * temperEfficiency;
    }

    public void OnMobSlain(int mobTier)
    {
        // Feeding restores durability and satiety
        currentSatiety = Mathf.Min(maxSatiety, currentSatiety + mobTier * 5f);
        currentTemper = Mathf.Min(maxTemper - fractureDebt, currentTemper + mobTier * 3.5f);
    }

    public void RestoreFractureDebtAtForge()
    {
        fractureDebt = 0f;
        currentTemper = maxTemper;
    }

    /// <summary>
    /// Performs Anima Core Re-Anchoring to fix broken maximum durability caps
    /// caused by fracture debt or forge detonations.
    /// </summary>
    public bool PerformAnimaCoreReAnchoring(int animaCoreCost = 1)
    {
        fractureDebt = 0f;
        currentTemper = maxTemper;
        currentSatiety = maxSatiety;
        Debug.Log($"[LivingGearSystem] Anima Core Re-Anchoring successful on {weaponName}. Maximum durability cap restored.");
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
        }
        return true;
    }
}
