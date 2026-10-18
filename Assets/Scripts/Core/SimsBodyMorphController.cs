using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BodyProportionProfile
{
    [Range(0.8f, 1.25f)] public float heightScale = 1.0f;
    [Range(0.8f, 1.3f)] public float headScale = 1.0f;
    [Range(0.7f, 1.4f)] public float shoulderWidth = 1.0f;
    [Range(0.7f, 1.4f)] public float chestScale = 1.0f;
    [Range(0.7f, 1.4f)] public float waistWidth = 1.0f;
    [Range(0.7f, 1.4f)] public float hipWidth = 1.0f;
    [Range(0.8f, 1.3f)] public float armThickness = 1.0f;
    [Range(0.8f, 1.3f)] public float legThickness = 1.0f;
    [Range(10f, 60f)] public float muscleMass_4D = 25f; // Bridges into 12D Inertial Mass
}

/// <summary>
/// SimsBodyMorphController handles real-time dynamic body proportion customization
/// on standard humanoid skeleton rigs (Head, Shoulders, Chest, Waist, Hips, Limbs).
/// </summary>
public class SimsBodyMorphController : MonoBehaviour
{
    [Header("Current Proportions")]
    public BodyProportionProfile proportions = new BodyProportionProfile();

    [Header("Skeleton Bone References")]
    public Transform headBone;
    public Transform chestBone;
    public Transform spineBone;
    public Transform hipsBone;
    public Transform leftShoulderBone;
    public Transform rightShoulderBone;
    public Transform leftArmBone;
    public Transform rightArmBone;
    public Transform leftLegBone;
    public Transform rightLegBone;

    [Header("12D Telemetry Coupling")]
    public DimensionalArtifact boundDimensionalArtifact;

    public event Action<BodyProportionProfile> OnProportionsChanged;

    private void Awake()
    {
        AutoDetectBones();
    }

    private void Start()
    {
        ApplyProportions();
    }

    public void AutoDetectBones()
    {
        var allBones = GetComponentsInChildren<Transform>(true);
        foreach (var b in allBones)
        {
            string n = b.name.ToLower();
            if (n.Contains("socket")) continue; // Skip sockets

            if (b.name.Equals("Head", StringComparison.OrdinalIgnoreCase)) headBone = b;
            else if (b.name.Equals("Chest", StringComparison.OrdinalIgnoreCase) || b.name.Equals("UpperChest", StringComparison.OrdinalIgnoreCase)) { if (chestBone == null) chestBone = b; }
            else if (b.name.Equals("Spine", StringComparison.OrdinalIgnoreCase)) spineBone = b;
            else if (b.name.Equals("Hips", StringComparison.OrdinalIgnoreCase)) hipsBone = b;
            else if (b.name.Equals("LeftShoulder", StringComparison.OrdinalIgnoreCase)) leftShoulderBone = b;
            else if (b.name.Equals("RightShoulder", StringComparison.OrdinalIgnoreCase)) rightShoulderBone = b;
            else if (b.name.Equals("LeftArm", StringComparison.OrdinalIgnoreCase) || b.name.Equals("LeftUpperArm", StringComparison.OrdinalIgnoreCase)) leftArmBone = b;
            else if (b.name.Equals("RightArm", StringComparison.OrdinalIgnoreCase) || b.name.Equals("RightUpperArm", StringComparison.OrdinalIgnoreCase)) rightArmBone = b;
            else if (b.name.Equals("LeftUpLeg", StringComparison.OrdinalIgnoreCase) || b.name.Equals("LeftUpperLeg", StringComparison.OrdinalIgnoreCase)) leftLegBone = b;
            else if (b.name.Equals("RightUpLeg", StringComparison.OrdinalIgnoreCase) || b.name.Equals("RightUpperLeg", StringComparison.OrdinalIgnoreCase)) rightLegBone = b;
        }
    }

    public void SetHeight(float val) { proportions.heightScale = Mathf.Clamp(val, 0.8f, 1.25f); ApplyProportions(); }
    public void SetHeadScale(float val) { proportions.headScale = Mathf.Clamp(val, 0.8f, 1.3f); ApplyProportions(); }
    public void SetShoulderWidth(float val) { proportions.shoulderWidth = Mathf.Clamp(val, 0.7f, 1.4f); ApplyProportions(); }
    public void SetChestScale(float val) { proportions.chestScale = Mathf.Clamp(val, 0.7f, 1.4f); ApplyProportions(); }
    public void SetWaistWidth(float val) { proportions.waistWidth = Mathf.Clamp(val, 0.7f, 1.4f); ApplyProportions(); }
    public void SetHipWidth(float val) { proportions.hipWidth = Mathf.Clamp(val, 0.7f, 1.4f); ApplyProportions(); }
    public void SetArmThickness(float val) { proportions.armThickness = Mathf.Clamp(val, 0.8f, 1.3f); ApplyProportions(); }
    public void SetLegThickness(float val) { proportions.legThickness = Mathf.Clamp(val, 0.8f, 1.3f); ApplyProportions(); }
    public void SetMuscleMass(float val) { proportions.muscleMass_4D = Mathf.Clamp(val, 10f, 60f); ApplyProportions(); }

    public void ApplyProportions()
    {
        // 1. Overall Height
        transform.localScale = Vector3.one * proportions.heightScale;

        // 2. Head
        if (headBone != null)
        {
            headBone.localScale = Vector3.one * proportions.headScale;
        }

        // 3. Chest
        if (chestBone != null)
        {
            chestBone.localScale = new Vector3(proportions.chestScale, 1.0f, proportions.chestScale);
        }

        // 4. Waist / Spine
        if (spineBone != null)
        {
            spineBone.localScale = new Vector3(proportions.waistWidth, 1.0f, proportions.waistWidth);
        }

        // 5. Hips / Pelvis
        if (hipsBone != null)
        {
            hipsBone.localScale = new Vector3(proportions.hipWidth, 1.0f, proportions.hipWidth);
        }

        // 6. Shoulders Width
        if (leftShoulderBone != null) leftShoulderBone.localScale = new Vector3(proportions.shoulderWidth, 1.0f, 1.0f);
        if (rightShoulderBone != null) rightShoulderBone.localScale = new Vector3(proportions.shoulderWidth, 1.0f, 1.0f);

        // 7. Arms & Legs Thickness
        if (leftArmBone != null) leftArmBone.localScale = new Vector3(proportions.armThickness, 1.0f, proportions.armThickness);
        if (rightArmBone != null) rightArmBone.localScale = new Vector3(proportions.armThickness, 1.0f, proportions.armThickness);
        if (leftLegBone != null) leftLegBone.localScale = new Vector3(proportions.legThickness, 1.0f, proportions.legThickness);
        if (rightLegBone != null) rightLegBone.localScale = new Vector3(proportions.legThickness, 1.0f, proportions.legThickness);

        // 8. 12D Telemetry Bridge: Update 4D Inertial Mass & 5D Density Matrix
        if (boundDimensionalArtifact != null)
        {
            boundDimensionalArtifact.inertialMass_4D = proportions.muscleMass_4D * proportions.heightScale;
            float volume = Mathf.Max(0.001f, proportions.heightScale * proportions.chestScale * proportions.waistWidth);
            boundDimensionalArtifact.densityMatrix_5D = boundDimensionalArtifact.inertialMass_4D / volume;
        }

        OnProportionsChanged?.Invoke(proportions);
    }
}
