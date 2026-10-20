using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentSlot
{
    Helmet,
    Torso,
    Leggings,
    Shoes,
    MainHandWeapon,
    OffHandWeapon,
    Gauntlets
}

public enum WeaponType
{
    Sword,
    TwoHandedBattleAxe,
    Staff,
    Wand
}

[System.Serializable]
public class EquipmentItemDefinition
{
    public string itemId;
    public string itemName;
    public EquipmentSlot slot;
    public WeaponType weaponType;
    public GameObject prefab;
    public Sprite icon;
    public float baseDamage = 25f;
    public float armorRating = 10f;
    public float inertialMass_4D = 10f;
    public float carrierFrequency_10D = 60f;
    public bool isTwoHanded = false;
}

/// <summary>
/// ModularEquipmentManager manages runtime equipping, unequipping, and visual socket attachment
/// for modular armor (Helmet, Torso, Leggings, Shoes, Gauntlets) and weapons.
/// When ModularCharacterAppearance is present, it directly swaps the rigged body parts (e.g. replacing hands with gauntlets)
/// to eliminate mesh clipping.
/// </summary>
public class ModularEquipmentManager : MonoBehaviour
{
    public static ModularEquipmentManager Instance { get; private set; }

    [Header("Modular Skinned Mesh System Integration")]
    public BattleSummoner.Customization.ModularCharacterAppearance modularAppearance;

    [Header("Bone Sockets (Transform Anchors)")]
    public Transform headSocket;
    public Transform chestSocket;
    public Transform hipsSocket;
    public Transform leftFootSocket;
    public Transform rightFootSocket;
    public Transform rightHandSocket;
    public Transform leftHandSocket;

    [Header("Equipped Items Visual Instances")]
    [SerializeField] private GameObject activeHelmetInstance;
    [SerializeField] private GameObject activeTorsoInstance;
    [SerializeField] private GameObject activeLeggingsInstance;
    [SerializeField] private GameObject activeShoesInstance;
    [SerializeField] private GameObject activeShoesRightInstance;
    [SerializeField] private GameObject activeGauntletsInstance;
    [SerializeField] private GameObject activeMainWeaponInstance;
    [SerializeField] private GameObject activeOffWeaponInstance;

    [Header("Current Equipment Definitions")]
    public EquipmentItemDefinition currentHelmet;
    public EquipmentItemDefinition currentTorso;
    public EquipmentItemDefinition currentGauntlets;
    public EquipmentItemDefinition currentLeggings;
    public EquipmentItemDefinition currentShoes;
    public EquipmentItemDefinition currentMainWeapon;
    public EquipmentItemDefinition currentOffWeapon;

    public event Action<EquipmentSlot, EquipmentItemDefinition> OnEquipmentChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        AutoDetectSockets();
    }

    /// <summary>
    /// Automatically searches child transforms for standard humanoid bone sockets.
    /// </summary>
    public void AutoDetectSockets()
    {
        if (modularAppearance == null)
            modularAppearance = GetComponent<BattleSummoner.Customization.ModularCharacterAppearance>() ?? GetComponentInChildren<BattleSummoner.Customization.ModularCharacterAppearance>();

        var allChildren = GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren)
        {
            string n = t.name.ToLower();
            if (headSocket == null && (n.Contains("socket_head") || n.Contains("head"))) headSocket = t;
            else if (chestSocket == null && (n.Contains("socket_chest") || n.Contains("chest"))) chestSocket = t;
            else if (hipsSocket == null && (n.Contains("socket_legs") || n.Contains("hips"))) hipsSocket = t;
            else if (rightHandSocket == null && (n.Contains("socket_weapon_r") || n.Contains("righthand"))) rightHandSocket = t;
            else if (leftHandSocket == null && (n.Contains("socket_weapon_l") || n.Contains("lefthand"))) leftHandSocket = t;
            else if (leftFootSocket == null && (n.Contains("socket_feet_shoes_l") || n.Contains("leftfoot"))) leftFootSocket = t;
            else if (rightFootSocket == null && (n.Contains("socket_feet_shoes_r") || n.Contains("rightfoot"))) rightFootSocket = t;
        }
    }

    public void Equip(EquipmentItemDefinition item)
    {
        if (item == null) return;

        // If ModularCharacterAppearance is present, use true modular body part substitution!
        if (modularAppearance != null)
        {
            switch (item.slot)
            {
                case EquipmentSlot.Helmet:
                    modularAppearance.SetHelmetArmor(true);
                    currentHelmet = item;
                    break;
                case EquipmentSlot.Torso:
                    modularAppearance.SetTorsoArmor(true);
                    currentTorso = item;
                    break;
                case EquipmentSlot.Gauntlets:
                    modularAppearance.SetGauntletsArmor(true);
                    currentGauntlets = item;
                    break;
                case EquipmentSlot.Leggings:
                    modularAppearance.SetLeggingsArmor(true);
                    currentLeggings = item;
                    break;
                case EquipmentSlot.Shoes:
                    modularAppearance.SetBootsArmor(true);
                    currentShoes = item;
                    break;
                case EquipmentSlot.MainHandWeapon:
                    modularAppearance.EquipWeapon(item.itemId);
                    currentMainWeapon = item;
                    break;
            }

            OnEquipmentChanged?.Invoke(item.slot, item);
            Debug.Log($"[MODULAR EQUIPMENT] Equipped '{item.itemName}' via ModularCharacterAppearance into slot [{item.slot}].");
            return;
        }

        switch (item.slot)
        {
            case EquipmentSlot.Helmet:
                AttachEquipment(ref activeHelmetInstance, item.prefab, headSocket != null ? headSocket : transform.Find("Character_Armature/Hips/Spine/Chest/UpperChest/Neck/Head"), Vector3.zero, Quaternion.identity);
                currentHelmet = item;
                break;

            case EquipmentSlot.Torso:
                AttachEquipment(ref activeTorsoInstance, item.prefab, chestSocket != null ? chestSocket : transform.Find("Character_Armature/Hips/Spine/Chest"), Vector3.zero, Quaternion.identity);
                currentTorso = item;
                break;

            case EquipmentSlot.Leggings:
                AttachEquipment(ref activeLeggingsInstance, item.prefab, hipsSocket != null ? hipsSocket : transform.Find("Character_Armature/Hips"), Vector3.zero, Quaternion.identity);
                currentLeggings = item;
                break;

            case EquipmentSlot.Shoes:
                AttachEquipment(ref activeShoesInstance, item.prefab, transform, new Vector3(0f, 0f, 0.17f), Quaternion.Euler(90f, 180f, 0f));
                currentShoes = item;
                break;

            case EquipmentSlot.MainHandWeapon:
                if (item.isTwoHanded && activeOffWeaponInstance != null)
                {
                    Unequip(EquipmentSlot.OffHandWeapon);
                }
                var rHand = transform.Find("Character_Armature/Hips/Spine/Chest/UpperChest/RightShoulder/RightArm/RightForeArm/RightHand");
                Vector3 wOffset = Vector3.zero;
                Quaternion wRot = Quaternion.identity;
                if (!string.IsNullOrEmpty(item.itemId) && item.itemId.ToLower().Contains("sword"))
                {
                    wOffset = new Vector3(-0.015f, -0.64f, -0.01f);
                }
                AttachEquipment(ref activeMainWeaponInstance, item.prefab, rightHandSocket != null ? rightHandSocket : rHand, wOffset, wRot);
                currentMainWeapon = item;
                break;

            case EquipmentSlot.OffHandWeapon:
                if (currentMainWeapon != null && currentMainWeapon.isTwoHanded)
                {
                    Unequip(EquipmentSlot.MainHandWeapon);
                }
                var lHand = transform.Find("Character_Armature/Hips/Spine/Chest/UpperChest/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
                AttachEquipment(ref activeOffWeaponInstance, item.prefab, leftHandSocket != null ? leftHandSocket : lHand, Vector3.zero, Quaternion.identity);
                currentOffWeapon = item;
                break;
        }

        OnEquipmentChanged?.Invoke(item.slot, item);
        Debug.Log($"[EQUIPMENT] Equipped '{item.itemName}' into slot [{item.slot}].");
    }

    public void Unequip(EquipmentSlot slot)
    {
        if (modularAppearance != null)
        {
            switch (slot)
            {
                case EquipmentSlot.Helmet:
                    modularAppearance.SetHelmetArmor(false);
                    currentHelmet = null;
                    break;
                case EquipmentSlot.Torso:
                    modularAppearance.SetTorsoArmor(false);
                    currentTorso = null;
                    break;
                case EquipmentSlot.Gauntlets:
                    modularAppearance.SetGauntletsArmor(false);
                    currentGauntlets = null;
                    break;
                case EquipmentSlot.Leggings:
                    modularAppearance.SetLeggingsArmor(false);
                    currentLeggings = null;
                    break;
                case EquipmentSlot.Shoes:
                    modularAppearance.SetBootsArmor(false);
                    currentShoes = null;
                    break;
                case EquipmentSlot.MainHandWeapon:
                    modularAppearance.EquipWeapon("none");
                    currentMainWeapon = null;
                    break;
            }

            OnEquipmentChanged?.Invoke(slot, null);
            return;
        }

        switch (slot)
        {
            case EquipmentSlot.Helmet:
                if (activeHelmetInstance != null) Destroy(activeHelmetInstance);
                currentHelmet = null;
                break;
            case EquipmentSlot.Torso:
                if (activeTorsoInstance != null) Destroy(activeTorsoInstance);
                currentTorso = null;
                break;
            case EquipmentSlot.Gauntlets:
                if (activeGauntletsInstance != null) Destroy(activeGauntletsInstance);
                currentGauntlets = null;
                break;
            case EquipmentSlot.Leggings:
                if (activeLeggingsInstance != null) Destroy(activeLeggingsInstance);
                currentLeggings = null;
                break;
            case EquipmentSlot.Shoes:
                if (activeShoesInstance != null) Destroy(activeShoesInstance);
                if (activeShoesRightInstance != null) Destroy(activeShoesRightInstance);
                currentShoes = null;
                break;
            case EquipmentSlot.MainHandWeapon:
                if (activeMainWeaponInstance != null) Destroy(activeMainWeaponInstance);
                currentMainWeapon = null;
                break;
            case EquipmentSlot.OffHandWeapon:
                if (activeOffWeaponInstance != null) Destroy(activeOffWeaponInstance);
                currentOffWeapon = null;
                break;
        }

        OnEquipmentChanged?.Invoke(slot, null);
    }

    private void AttachEquipment(ref GameObject currentInstance, GameObject prefab, Transform parentSocket, Vector3 localPos, Quaternion localRot)
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            currentInstance = null;
        }

        if (prefab == null) return;

        Transform targetParent = parentSocket != null ? parentSocket : transform;
        currentInstance = Instantiate(prefab, targetParent);
        currentInstance.name = prefab.name;
        currentInstance.transform.localPosition = localPos;
        currentInstance.transform.localRotation = localRot;
        currentInstance.transform.localScale = prefab.transform.localScale;

        // Strip extraneous colliders from cosmetic gear
        foreach (var col in currentInstance.GetComponentsInChildren<Collider>())
        {
            if (!col.isTrigger) Destroy(col);
        }
    }
}
