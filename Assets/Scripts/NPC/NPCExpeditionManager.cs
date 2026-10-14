using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ActiveExpedition
{
    public BespokeNPCObject dispatchedNPC;
    public string targetRouteName;
    public float durationSeconds;
    public float elapsedSeconds;
    public int floorDangerLevel;
    public bool isComplete;
}

public class NPCExpeditionManager : MonoBehaviour
{
    public static NPCExpeditionManager Instance { get; private set; }

    [Header("Strict Roster Limits")]
    public const int MAX_COMBAT_COMPANIONS = 1; // Strict 1 companion in real-time raids!
    public const int MAX_DISPATCHED_EXPEDITIONS = 3; // Max 3 on off-screen passive safari

    [Header("Active States")]
    public BespokeNPCObject activeRaidCompanion;
    public List<ActiveExpedition> currentExpeditions = new List<ActiveExpedition>();

    // Events
    public static event Action<string, int, MineralRarity> OnExpeditionReturned;
    public static event Action<BespokeNPCObject> OnRaidCompanionChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        for (int i = currentExpeditions.Count - 1; i >= 0; i--)
        {
            var exp = currentExpeditions[i];
            if (!exp.isComplete)
            {
                exp.elapsedSeconds += Time.deltaTime;
                if (exp.elapsedSeconds >= exp.durationSeconds)
                {
                    exp.isComplete = true;
                    ResolveExpeditionRewards(exp);
                    currentExpeditions.RemoveAt(i);
                }
            }
        }
    }

    public bool AssignActiveRaidCompanion(BespokeNPCObject npc)
    {
        if (npc == null)
        {
            DismissRaidCompanion();
            return true;
        }

        if (IsNPCBusy(npc))
        {
            Debug.LogWarning($"[ROSTER] {npc.displayName} is currently out on an expedition!");
            return false;
        }

        activeRaidCompanion = npc;
        OnRaidCompanionChanged?.Invoke(activeRaidCompanion);
        Debug.Log($"[ROSTER] {npc.displayName} assigned as sole active combat companion.");
        return true;
    }

    public void DismissRaidCompanion()
    {
        activeRaidCompanion = null;
        OnRaidCompanionChanged?.Invoke(null);
    }

    public bool DispatchOnExpedition(BespokeNPCObject npc, string routeName, float durationSeconds, int floorLevel)
    {
        if (currentExpeditions.Count >= MAX_DISPATCHED_EXPEDITIONS)
        {
            Debug.LogWarning("[EXPEDITION] Max expedition squads (3) already deployed!");
            return false;
        }

        if (activeRaidCompanion == npc)
        {
            Debug.LogWarning($"[EXPEDITION] {npc.displayName} is in your active raid squad! Dismiss them first.");
            return false;
        }

        if (IsNPCBusy(npc))
        {
            Debug.LogWarning($"[EXPEDITION] {npc.displayName} is already on another deployment!");
            return false;
        }

        ActiveExpedition newExp = new ActiveExpedition
        {
            dispatchedNPC = npc,
            targetRouteName = routeName,
            durationSeconds = durationSeconds * (1.0f / Mathf.Max(0.1f, npc.expeditionSpeedMultiplier)),
            elapsedSeconds = 0f,
            floorDangerLevel = floorLevel,
            isComplete = false
        };

        currentExpeditions.Add(newExp);
        Debug.Log($"[EXPEDITION DEPLOYED] {npc.displayName} sent to '{routeName}' for {newExp.durationSeconds}s.");
        return true;
    }

    private void ResolveExpeditionRewards(ActiveExpedition exp)
    {
        // Base passive payout in Mana Cores
        int baseManaCores = exp.floorDangerLevel * 120;
        MineralRarity rewardGem = MineralRarity.AzuriteCore;

        if (exp.dispatchedNPC.quirk == NPCQuirk.HoarderOfGems)
        {
            baseManaCores = Mathf.RoundToInt(baseManaCores * 1.30f);
            rewardGem = (UnityEngine.Random.value < 0.35f) ? MineralRarity.CinnabarRuby : MineralRarity.VoidAmethyst;
        }
        else if (exp.dispatchedNPC.quirk == NPCQuirk.CautiousScout)
        {
            rewardGem = MineralRarity.GildedTopaz;
        }

        // Apex gem roll
        if (exp.floorDangerLevel >= 50 && UnityEngine.Random.value < 0.05f)
        {
            rewardGem = MineralRarity.PrismaticHeart;
        }

        // Deliver rewards to player economy
        if (GemologySystem.Instance != null)
        {
            GemologySystem.Instance.totalManaCores += baseManaCores;
            if (GemologySystem.Instance.gemInventory.ContainsKey(rewardGem))
            {
                GemologySystem.Instance.gemInventory[rewardGem] += 2;
            }
        }

        Debug.Log($"[EXPEDITION COMPLETE] {exp.dispatchedNPC.displayName} returned from {exp.targetRouteName}! Yielded: {baseManaCores} Mana Cores & 2x {rewardGem}.");
        OnExpeditionReturned?.Invoke(exp.dispatchedNPC.displayName, baseManaCores, rewardGem);
    }

    public bool IsNPCBusy(BespokeNPCObject npc)
    {
        if (activeRaidCompanion == npc) return true;
        for (int i = 0; i < currentExpeditions.Count; i++)
        {
            if (currentExpeditions[i].dispatchedNPC == npc) return true;
        }
        return false;
    }
}
