using System;
using UnityEngine;

/// <summary>
/// ItemEnchantmentMatrix handles 3-strike anvil forging and gem resonance matrices.
/// </summary>
public class ItemEnchantmentMatrix : MonoBehaviour
{
    public static ItemEnchantmentMatrix Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public float CalculateEnchantmentYield(int strikePrecision, float gemPurity)
    {
        return Mathf.Clamp01(strikePrecision / 100f) * gemPurity * 1.5f;
    }
}
