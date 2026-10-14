using System;
using System.Collections.Generic;
using UnityEngine;

public enum ScalingCurveType
{
    LinearAdditive,      // Store buffs (+0.25x per stack)
    HyperbolicAsymptote, // Crit/Evasion/Proc rates (caps asymptotically at 100%)
    ExponentialCompound  // High-floor 3.0x strike relics
}

[Serializable]
public class AccessoryDefinition
{
    public string accessoryId;
    public string accessoryName;
    [TextArea] public string loreDescription;
    public Sprite iconSprite;
    public bool isPremiumStoreTier;
    public ScalingCurveType scalingType;
    public float baseModifier = 0.25f; // e.g., 0.25 for +25%, or 3.0 for 3x strike
    public int marketValueInManaCores = 150;
}

[Serializable]
public class AccessoryStackNode
{
    public AccessoryDefinition definition;
    public int stackCount;

    public float GetEffectiveValue()
    {
        if (stackCount <= 0 || definition == null) return 0f;

        switch (definition.scalingType)
        {
            case ScalingCurveType.LinearAdditive:
                return stackCount * definition.baseModifier;

            case ScalingCurveType.HyperbolicAsymptote:
                return 1.0f - (1.0f / (1.0f + (stackCount * definition.baseModifier)));

            case ScalingCurveType.ExponentialCompound:
                float dampedExponent = Mathf.Pow(stackCount, 0.65f);
                return Mathf.Pow(definition.baseModifier, dampedExponent);

            default:
                return 1.0f;
        }
    }
}

public class UncappedAccessorySystem : MonoBehaviour
{
    private static UncappedAccessorySystem instance;
    public static UncappedAccessorySystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<UncappedAccessorySystem>();
                if (instance != null) instance.SyncRegistry();
            }
            return instance;
        }
        private set { instance = value; }
    }

    [Header("Live Accessory Buffer")]
    [SerializeField] private List<AccessoryStackNode> serializedBuffer = new List<AccessoryStackNode>();
    private Dictionary<string, AccessoryStackNode> stackRegistry = new Dictionary<string, AccessoryStackNode>();

    public event Action<AccessoryStackNode> OnAccessoryBufferMutated;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        SyncRegistry();
    }

    public void SyncRegistry()
    {
        if (stackRegistry == null) stackRegistry = new Dictionary<string, AccessoryStackNode>();
        stackRegistry.Clear();
        foreach (var node in serializedBuffer)
        {
            if (node != null && node.definition != null && !string.IsNullOrEmpty(node.definition.accessoryId))
            {
                stackRegistry[node.definition.accessoryId] = node;
            }
        }
    }

    /// <summary>
    /// Injects an accessory into the uncapped player buffer. Increments stack if ID already exists.
    /// </summary>
    public void AcquireAccessory(AccessoryDefinition itemDef, int quantity = 1)
    {
        if (itemDef == null || quantity <= 0) return;

        if (stackRegistry.TryGetValue(itemDef.accessoryId, out AccessoryStackNode existingNode))
        {
            existingNode.stackCount += quantity;
            OnAccessoryBufferMutated?.Invoke(existingNode);
        }
        else
        {
            AccessoryStackNode newNode = new AccessoryStackNode
            {
                definition = itemDef,
                stackCount = quantity
            };
            stackRegistry.Add(itemDef.accessoryId, newNode);
            serializedBuffer.Add(newNode);
            OnAccessoryBufferMutated?.Invoke(newNode);
        }
    }

    /// <summary>
    /// Removes a specific quantity of an accessory (used when selling to merchants or players).
    /// </summary>
    public bool RemoveAccessory(string accessoryId, int quantity = 1)
    {
        if (!stackRegistry.TryGetValue(accessoryId, out AccessoryStackNode node)) return false;
        if (node.stackCount < quantity) return false;

        node.stackCount -= quantity;
        if (node.stackCount <= 0)
        {
            stackRegistry.Remove(accessoryId);
            serializedBuffer.Remove(node);
        }

        OnAccessoryBufferMutated?.Invoke(node);
        return true;
    }

    /// <summary>
    /// Evaluates aggregate strike damage across all linear and compound stacks.
    /// Total Kinetic Output = Base Damage * (1.0 + Sum(n_i * m_linear,i)) * Prod(m_compound,j ^ (n_j ^ 0.65))
    /// </summary>
    public float CalculateTotalStrikeOutput(float baseSwipeDamage)
    {
        float additiveSum = 0f;
        float compoundProduct = 1.0f;

        foreach (var kvp in stackRegistry)
        {
            AccessoryStackNode node = kvp.Value;
            if (node == null || node.definition == null) continue;

            if (node.definition.scalingType == ScalingCurveType.LinearAdditive)
            {
                additiveSum += node.GetEffectiveValue();
            }
            else if (node.definition.scalingType == ScalingCurveType.ExponentialCompound)
            {
                compoundProduct *= node.GetEffectiveValue();
            }
        }

        return (baseSwipeDamage * (1.0f + additiveSum)) * compoundProduct;
    }

    public IReadOnlyList<AccessoryStackNode> GetActiveBuffer() => serializedBuffer;

    public AccessoryStackNode GetStackNode(string accessoryId)
    {
        if (stackRegistry.TryGetValue(accessoryId, out var node)) return node;
        return null;
    }

    public int GetStackCount(string accessoryId)
    {
        if (stackRegistry.TryGetValue(accessoryId, out var node)) return node.stackCount;
        return 0;
    }

    public void ClearBuffer()
    {
        stackRegistry.Clear();
        serializedBuffer.Clear();
    }
}
