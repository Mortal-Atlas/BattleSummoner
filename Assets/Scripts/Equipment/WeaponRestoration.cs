using System;
using UnityEngine;

/// <summary>
/// WeaponRestoration handles repairing shattered weapon temper debt from thermodynamic backfires.
/// </summary>
public class WeaponRestoration : MonoBehaviour
{
    public static WeaponRestoration Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public bool RestoreFracturedTemper(WeaponTemper weapon, int manaCorePayment)
    {
        if (weapon == null) return false;
        weapon.RepairWeapon(100f);
        Debug.Log($"[Restoration] Repaired weapon temper via {manaCorePayment} Mana Cores.");
        return true;
    }
}
