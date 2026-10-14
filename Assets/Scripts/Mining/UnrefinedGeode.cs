using System;
using UnityEngine;

public class UnrefinedGeode : MonoBehaviour, IInspectablePuzzlePiece
{
    [Header("Geode Properties")]
    public MineralRarity innerGemType = MineralRarity.VoidAmethyst;
    [SerializeField] private int maxStrikesBeforeShatter = 3;
    [SerializeField] private int currentStrikes = 0;
    [SerializeField] private bool isCracked = false;
    [SerializeField] private Transform geodeUpperHalf;
    [SerializeField] private Transform geodeLowerHalf;
    [SerializeField] private GameObject innerGemCore;

    public event Action<MineralRarity> OnGeodeHarvested;

    public void OnPuzzleInteracted()
    {
        OnPuzzleTapped();
    }

    public void OnPuzzleTapped()
    {
        if (isCracked) return;

        currentStrikes++;
        Debug.Log($"[GEODE CHISEL] Chiseled along fault line (Strike {currentStrikes}/{maxStrikesBeforeShatter})");

        if (currentStrikes == 1 && geodeUpperHalf != null)
        {
            geodeUpperHalf.localPosition += new Vector3(0.04f, 0.08f, 0f);
        }
        else if (currentStrikes == 2 && geodeLowerHalf != null)
        {
            geodeLowerHalf.localPosition -= new Vector3(0.04f, 0.08f, 0f);
        }
        else if (currentStrikes >= maxStrikesBeforeShatter)
        {
            CrackOpenGeode();
        }
    }

    private void CrackOpenGeode()
    {
        isCracked = true;

        if (innerGemCore != null)
        {
            innerGemCore.SetActive(true);
        }

        if (GemologySystem.Instance != null)
        {
            if (GemologySystem.Instance.gemInventory.ContainsKey(innerGemType))
            {
                GemologySystem.Instance.gemInventory[innerGemType] += 1;
            }
            GemologySystem.Instance.totalManaCores += 150;
        }

        OnGeodeHarvested?.Invoke(innerGemType);
        Debug.Log($"[GEODE CRACKED] Revealing crystalline core: {innerGemType}!");
    }

    public void OnPuzzleSlide(Vector2 screenDelta)
    {
        // Geode uses chisel taps
    }

    public void OnPuzzleSlideEnd()
    {
    }

    public void TriggerVolatileDetonation(PlayerProgression player)
    {
        Debug.LogError("[VOLATILE EXPLOSION] Unrefined volatile geode pressurized and detonated!");
        if (player != null)
        {
            player.TakeDirectDamage(200f);
            SystemBroadcastManager.Instance?.TriggerAchievement("ach_secret_volatile_geode_explosion", player.playerName);
        }
        Destroy(gameObject);
    }
}
