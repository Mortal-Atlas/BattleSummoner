using System;
using UnityEngine;

public enum MineralRarity
{
    AzuriteCore,      // Basic mana conduit gem
    CinnabarRuby,     // Fire thermodynamic amplifier
    GildedTopaz,      // Kinetic strike accelerator
    VoidAmethyst,     // Gravitational stabilizer
    PrismaticHeart    // Legendary apex gem (broadcastable!)
}

public class CaveMiningNode : MonoBehaviour
{
    [Header("Mineral Vein Settings")]
    public MineralRarity mineralType = MineralRarity.AzuriteCore;
    [SerializeField] private int health = 3;
    [SerializeField] private string gemDropId = "PrismaticHeart";
    [SerializeField] private int maxStrikesRequired = 4;
    [SerializeField] private int currentStrikes = 0;
    [SerializeField] private int gemYield = 2;
    [SerializeField] private int manaCoreValue = 85;

    [Header("Audio & Visual Feedback")]
    [SerializeField] private AudioSource nodeAudioSource;
    [SerializeField] private AudioClip strikeChimeClip;
    [SerializeField] private AudioClip shatterYieldClip;
    [SerializeField] private ParticleSystem impactSparksVFX;
    [SerializeField] private ParticleSystem gemExplosionVFX;

    [Header("Loot Dropper")]
    public GameObject crackedGeodeInspectionPrefab;

    public static event Action<MineralRarity, int, int> OnMineralMined;

    private void Awake()
    {
        if (nodeAudioSource == null)
        {
            nodeAudioSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnMiningChiselTap += HandleChiselHit;
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnMiningChiselTap -= HandleChiselHit;
    }

    private void HandleChiselHit(Vector2 screenTapPosition)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenTapPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == this.gameObject || hit.transform.IsChildOf(this.transform))
            {
                ExecuteChiselStrike();
            }
        }
    }

    public void OnChiselStrike(int strikePower)
    {
        health -= strikePower;
        float pitchRamp = Mathf.Min(1.48f, 1.0f + (0.15f * (3 - health)));
        if (nodeAudioSource != null)
        {
            nodeAudioSource.pitch = pitchRamp;
            if (strikeChimeClip != null) nodeAudioSource.PlayOneShot(strikeChimeClip);
            else nodeAudioSource.Play();
        }

        if (health <= 0)
        {
            if (GemologySystem.Instance != null)
            {
                GemologySystem.Instance.AddGemToInventory(gemDropId);
            }
            if (gemDropId == "PrismaticHeart" && SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("A rare Prismatic Heart has been extracted from the subterranean Spire depths!", true);
            }
            Destroy(gameObject);
        }
    }

    public void ExecuteChiselStrike()
    {
        OnChiselStrike(1);
        currentStrikes++;

        if (impactSparksVFX != null) impactSparksVFX.Play();
        if (nodeAudioSource != null && strikeChimeClip != null)
        {
            nodeAudioSource.pitch = 1.0f + (currentStrikes * 0.12f);
            nodeAudioSource.PlayOneShot(strikeChimeClip);
        }

        // Slight micro-scale compression impact
        transform.localScale = Vector3.one * 0.92f;
        Invoke(nameof(ResetScale), 0.08f);

        if (currentStrikes >= maxStrikesRequired)
        {
            ShatterVein();
        }
    }

    private void ResetScale()
    {
        transform.localScale = Vector3.one;
    }

    private void ShatterVein()
    {
        if (gemExplosionVFX != null)
        {
            gemExplosionVFX.transform.SetParent(null);
            gemExplosionVFX.Play();
            Destroy(gemExplosionVFX.gameObject, 2.0f);
        }

        if (nodeAudioSource != null && shatterYieldClip != null)
        {
            AudioSource.PlayClipAtPoint(shatterYieldClip, transform.position);
        }

        OnMineralMined?.Invoke(mineralType, gemYield, manaCoreValue);

        if (mineralType == MineralRarity.PrismaticHeart)
        {
            SystemBroadcastManager.Instance?.TriggerAchievement("ach_gem_prismatic_unearth", "Delver");
        }

        // Record world rumor discovery for offline campfire propagation
        if (CampfireRumorPropagationService.Instance != null)
        {
            CampfireRumorPropagationService.Instance.RecordDiscovery("Player_Hero", 34, transform.position, $"{mineralType} Crystal Vein");
        }

        if (crackedGeodeInspectionPrefab != null)
        {
            Instantiate(crackedGeodeInspectionPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}
