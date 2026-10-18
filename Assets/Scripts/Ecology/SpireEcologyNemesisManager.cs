using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpeciesStratumLedger
{
    public string speciesId;
    public int floorNumber;
    public string droppedMaterialId;
    public int maxPopulation = 100;
    public int currentPopulation = 100;
    public bool nemesisActive = false;
    public Dictionary<string, float> damageHistogram = new Dictionary<string, float>();
}

public class SpireEcologyNemesisManager : MonoBehaviour
{
    public static SpireEcologyNemesisManager Instance { get; private set; }

    [SerializeField] private float extinctionThresholdRatio = 0.15f;
    [SerializeField] private List<SpeciesStratumLedger> floorLedgers = new List<SpeciesStratumLedger>();

    public event Action<ApexNemesisEntity> OnApexNemesisSpawned;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    public void RecordMonsterKill(int floor, string speciesId, string primaryDamageType, float damageDealt)
    {
        var ledger = floorLedgers.Find(l => l.floorNumber == floor && l.speciesId == speciesId);
        if (ledger == null)
        {
            ledger = new SpeciesStratumLedger
            {
                speciesId = speciesId,
                floorNumber = floor,
                droppedMaterialId = $"{speciesId}_Husk"
            };
            floorLedgers.Add(ledger);
        }
        if (ledger.nemesisActive) return;

        ledger.currentPopulation = Mathf.Max(0, ledger.currentPopulation - 1);

        if (!ledger.damageHistogram.ContainsKey(primaryDamageType))
            ledger.damageHistogram[primaryDamageType] = 0f;
        ledger.damageHistogram[primaryDamageType] += damageDealt;

        float popRatio = (float)ledger.currentPopulation / ledger.maxPopulation;
        if (popRatio <= extinctionThresholdRatio)
        {
            SpawnApexNemesis(ledger);
        }
    }

    private void SpawnApexNemesis(SpeciesStratumLedger ledger)
    {
        ledger.nemesisActive = true;
        string dominantDamageType = GetDominantDamageType(ledger.damageHistogram, out float dominanceRatio);

        // Determine adaptive counter-mutation based on >= 50% damage source
        ApexMutationTrait mutation = ApexMutationTrait.None;
        if (dominanceRatio >= 0.50f || dominantDamageType.Contains("Fire"))
        {
            if (dominantDamageType.Contains("Fire"))
                mutation = ApexMutationTrait.ObsidianThermalCarapace;
            else if (dominantDamageType.Contains("Slash") || dominantDamageType.Contains("Physical"))
                mutation = ApexMutationTrait.ReactiveSerratedScales;
            else if (dominantDamageType.Contains("Lightning") || dominantDamageType.Contains("Water") || dominantDamageType.Contains("Electric"))
                mutation = ApexMutationTrait.AetherGroundingOrgans;
        }

        if (mutation == ApexMutationTrait.None)
        {
            mutation = ApexMutationTrait.ObsidianThermalCarapace; // Standard default apex carapace
        }

        // Create or configure Apex Nemesis GameObject
        GameObject nemesisObj = new GameObject($"Apex_Nemesis_{ledger.speciesId}");
        var player = GameObject.Find("Player_Hero");
        Vector3 spawnPos = player != null ? player.transform.position + player.transform.forward * 12f : Vector3.zero;
        nemesisObj.transform.position = spawnPos;

        var nemesisComp = nemesisObj.AddComponent<ApexNemesisEntity>();
        nemesisComp.speciesId = ledger.speciesId;
        nemesisComp.floorNumber = ledger.floorNumber;
        nemesisComp.huntedMaterialId = ledger.droppedMaterialId;
        nemesisComp.mutationTrait = mutation;

        // Kin-Scent Targeting: locate player with highest inventory scent
        Transform primeTarget = SelectKinScentTarget(ledger.droppedMaterialId);
        nemesisComp.kinScentTarget = primeTarget != null ? primeTarget : (player != null ? player.transform : null);

        // Dispatch Top Marquee Banner via SystemBroadcastManager
        string broadcastMsg = $"[ECOLOGICAL MUTATION] Over-hunting on Floor {ledger.floorNumber} has birthed an Apex {ledger.speciesId} ({mutation}) adapted against {dominantDamageType}!";
        if (SystemBroadcastManager.Instance != null)
        {
            SystemBroadcastManager.Instance.DispatchBroadcast(broadcastMsg, true);
        }

        Debug.LogWarning(broadcastMsg);
        OnApexNemesisSpawned?.Invoke(nemesisComp);
    }

    private string GetDominantDamageType(Dictionary<string, float> histogram, out float dominanceRatio)
    {
        string topType = "PhysicalSlash";
        float maxVal = -1f;
        float totalVal = 0f;

        foreach (var kvp in histogram)
        {
            totalVal += kvp.Value;
            if (kvp.Value > maxVal)
            {
                maxVal = kvp.Value;
                topType = kvp.Key;
            }
        }

        dominanceRatio = totalVal > 0f ? (maxVal / totalVal) : 1.0f;
        return topType;
    }

    /// <summary>
    /// Evaluates which player on the floor holds the most materials harvested from the Nemesis's kin.
    /// </summary>
    public Transform SelectKinScentTarget(string huntedMaterialId)
    {
        var playerObj = GameObject.Find("Player_Hero");
        if (playerObj == null) return null;

        // In BattleSummoner, single player with companion party is tracked
        return playerObj.transform;
    }

    /// <summary>
    /// Overload for multiplayer / party dictionaries.
    /// </summary>
    public Transform SelectKinScentTarget(string huntedMaterialId, Dictionary<Transform, Dictionary<string, int>> playerInventories)
    {
        Transform primeTarget = null;
        int highestMatCount = 0;

        foreach (var playerEntry in playerInventories)
        {
            if (playerEntry.Value.TryGetValue(huntedMaterialId, out int count) && count > highestMatCount)
            {
                highestMatCount = count;
                primeTarget = playerEntry.Key;
            }
        }
        return primeTarget;
    }
}
