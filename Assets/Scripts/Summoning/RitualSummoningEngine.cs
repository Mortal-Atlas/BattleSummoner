using System;
using UnityEngine;

[Serializable]
public struct SummonResult
{
    public string entityName;
    public float adjustedHp;
    public float adjustedDamage;
}

public class RitualSummoningEngine : MonoBehaviour
{
    public static RitualSummoningEngine Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public SummonResult ProcessSummon(string summonedName, float circleAccuracyPercent)
    {
        float statSeedMultiplier = Mathf.Clamp(circleAccuracyPercent, 0.25f, 2.0f);
        return new SummonResult
        {
            entityName = summonedName,
            adjustedHp = 500f * statSeedMultiplier,
            adjustedDamage = 85f * statSeedMultiplier
        };
    }
}
