using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Customization
{
    /// <summary>
    /// ModularCharacterAppearance implements the industry-standard modular body part
    /// customization architecture for both local Player_Hero and networked multiplayer peers:
    /// - Body is segmented into discrete SkinnedMeshRenderers (Head, Torso, Arms, Hands, Legs, Feet).
    /// - When an armor piece is equipped (e.g. Gauntlets), the underlying skin part (Hands/Wrists)
    ///   is completely disabled and replaced by the Gauntlet mesh. Zero clipping, perfect seams.
    /// - Applies skin tones and armor dye colors via MaterialPropertyBlocks.
    /// - Synchronizes bone scaling for height, shoulders, waist, and limbs.
    /// </summary>
    [ExecuteAlways]
    public class ModularCharacterAppearance : MonoBehaviour
    {
        public static ModularCharacterAppearance LocalInstance { get; private set; }

        [Header("Modular Body Skin Parts")]
        public SkinnedMeshRenderer bodyHead;
        public SkinnedMeshRenderer bodyTorso;
        public SkinnedMeshRenderer bodyArms;
        public SkinnedMeshRenderer bodyHands;
        public SkinnedMeshRenderer bodyLegs;
        public SkinnedMeshRenderer bodyFeet;

        [Header("Modular Armor Parts")]
        public SkinnedMeshRenderer armorTorso;
        public SkinnedMeshRenderer armorGauntlets;
        public SkinnedMeshRenderer armorLeggings;
        public SkinnedMeshRenderer armorBoots;
        public SkinnedMeshRenderer armorHelmet;

        [Header("Weapon Sockets")]
        public Transform rightHandSocket;
        public Transform leftHandSocket;
        public Transform backSheathSocket;
        private GameObject activeWeaponInstance;

        [Header("Current Appearance State")]
        [SerializeField] private ModularPlayerAppearanceData currentData;
        public ModularPlayerAppearanceData CurrentData => currentData;

        public event Action<ModularPlayerAppearanceData> OnAppearanceChanged;

        private MaterialPropertyBlock skinPropertyBlock;
        private MaterialPropertyBlock armorPropertyBlock;

        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (gameObject.name.Contains("Player_Hero"))
            {
                LocalInstance = this;
            }
            skinPropertyBlock = new MaterialPropertyBlock();
            armorPropertyBlock = new MaterialPropertyBlock();
            AutoDetectRenderers();
        }

        private void OnEnable()
        {
            AutoDetectRenderers();
            if (currentData == null)
            {
                currentData = ModularPlayerAppearanceData.LoadLocalProfile();
            }
            ApplyAppearance(currentData);
        }

        private void Start()
        {
            if (currentData == null)
            {
                currentData = ModularPlayerAppearanceData.LoadLocalProfile();
            }
            ApplyAppearance(currentData);
        }

        private void OnValidate()
        {
            AutoDetectRenderers();
            if (currentData != null)
            {
                ApplyAppearance(currentData);
            }
        }

        public void AutoDetectRenderers()
        {
            var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                string n = smr.name;
                if (n.Contains("Modular_Body_Head") || n.Contains("Head")) bodyHead = smr;
                else if (n.Contains("Modular_Body_Torso")) bodyTorso = smr;
                else if (n.Contains("Modular_Body_Arms")) bodyArms = smr;
                else if (n.Contains("Modular_Body_Hands")) bodyHands = smr;
                else if (n.Contains("Modular_Body_Legs")) bodyLegs = smr;
                else if (n.Contains("Modular_Body_Feet")) bodyFeet = smr;
                else if (n.Contains("Modular_Armor_Torso") || n.Contains("Equipment_Torso")) armorTorso = smr;
                else if (n.Contains("Modular_Armor_Gauntlets") || n.Contains("Gauntlet")) armorGauntlets = smr;
                else if (n.Contains("Modular_Armor_Leggings") || n.Contains("Equipment_Leggings")) armorLeggings = smr;
                else if (n.Contains("Modular_Armor_Boots") || n.Contains("Equipment_Shoes")) armorBoots = smr;
                else if (n.Contains("Modular_Armor_Helmet") || n.Contains("Equipment_Helmet")) armorHelmet = smr;
            }

            // Auto-detect weapon socket on RightHand bone
            if (!rightHandSocket)
            {
                var rHand = FindChildRecursive(transform, "RightHand");
                if (rHand != null)
                {
                    var existingSocket = rHand.Find("Socket_Weapon_Right");
                    if (existingSocket == null)
                    {
                        var socketGo = new GameObject("Socket_Weapon_Right");
                        socketGo.transform.SetParent(rHand, false);
                        socketGo.transform.localPosition = new Vector3(0f, 0.08f, 0.02f);
                        socketGo.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                        rightHandSocket = socketGo.transform;
                    }
                    else
                    {
                        rightHandSocket = existingSocket;
                    }
                }
            }
        }

        public void ApplyAppearance(ModularPlayerAppearanceData data)
        {
            if (data == null) data = new ModularPlayerAppearanceData();
            if (data.bodyTypeIndex >= 0 && data.bodyTypeIndex < 8 && Mathf.Approximately(data.heightScale, 1.0f) && Mathf.Approximately(data.shoulderWidth, 1.0f))
            {
                data.ApplyBodyTypePreset(data.bodyTypeIndex);
            }
            currentData = data;

            AutoDetectRenderers();

            // 1. Modular Body vs Armor Swapping:
            // Torso: when armor is equipped, naked torso and arms/shoulders covered by armor are hidden
            if (armorTorso != null) armorTorso.gameObject.SetActive(data.equipTorso);
            if (bodyTorso != null) bodyTorso.gameObject.SetActive(!data.equipTorso);
            if (bodyArms != null) bodyArms.gameObject.SetActive(!data.equipTorso);

            // Gauntlets: when equipped, get rid of hand/wrist and replace with gauntlets!
            if (armorGauntlets != null) armorGauntlets.gameObject.SetActive(data.equipGauntlets);
            if (bodyHands != null) bodyHands.gameObject.SetActive(!data.equipGauntlets);

            // Leggings: when equipped, get rid of legs and replace with leggings!
            if (armorLeggings != null) armorLeggings.gameObject.SetActive(data.equipLeggings);
            if (bodyLegs != null) bodyLegs.gameObject.SetActive(!data.equipLeggings);

            // Boots: when equipped, get rid of bare feet and replace with boots!
            if (armorBoots != null) armorBoots.gameObject.SetActive(data.equipBoots);
            if (bodyFeet != null) bodyFeet.gameObject.SetActive(!data.equipBoots);

            // Helmet: when equipped, head is tucked/hidden under helmet
            if (armorHelmet != null) armorHelmet.gameObject.SetActive(data.equipHelmet);
            if (bodyHead != null) bodyHead.gameObject.SetActive(!data.equipHelmet);

            // 2. Apply Dynamic Color Tinting
            ApplySkinTint(data.skinColor);
            ApplyArmorDye(data.armorDyeColor);

            // 3. Apply Weapon Attachment
            EquipWeapon(data.weaponId);

            // 4. Apply Bone Scale Morphs
            ApplyBoneMorphs(data);

            OnAppearanceChanged?.Invoke(data);
        }

        public void SetTorsoArmor(bool equipped)
        {
            if (currentData == null) currentData = new ModularPlayerAppearanceData();
            currentData.equipTorso = equipped;
            if (armorTorso != null) armorTorso.gameObject.SetActive(equipped);
            if (bodyTorso != null) bodyTorso.gameObject.SetActive(!equipped);
            if (bodyArms != null) bodyArms.gameObject.SetActive(!equipped);
        }

        public void SetGauntletsArmor(bool equipped)
        {
            if (currentData == null) currentData = new ModularPlayerAppearanceData();
            currentData.equipGauntlets = equipped;
            if (armorGauntlets != null) armorGauntlets.gameObject.SetActive(equipped);
            if (bodyHands != null) bodyHands.gameObject.SetActive(!equipped);
        }

        public void SetLeggingsArmor(bool equipped)
        {
            if (currentData == null) currentData = new ModularPlayerAppearanceData();
            currentData.equipLeggings = equipped;
            if (armorLeggings != null) armorLeggings.gameObject.SetActive(equipped);
            if (bodyLegs != null) bodyLegs.gameObject.SetActive(!equipped);
        }

        public void SetBootsArmor(bool equipped)
        {
            if (currentData == null) currentData = new ModularPlayerAppearanceData();
            currentData.equipBoots = equipped;
            if (armorBoots != null) armorBoots.gameObject.SetActive(equipped);
            if (bodyFeet != null) bodyFeet.gameObject.SetActive(!equipped);
        }

        public void SetHelmetArmor(bool equipped)
        {
            if (currentData == null) currentData = new ModularPlayerAppearanceData();
            currentData.equipHelmet = equipped;
            if (armorHelmet != null) armorHelmet.gameObject.SetActive(equipped);
            if (bodyHead != null) bodyHead.gameObject.SetActive(!equipped);
        }

        public void ApplySkinTint(Color color)
        {
            if (skinPropertyBlock == null) skinPropertyBlock = new MaterialPropertyBlock();
            skinPropertyBlock.SetColor(BaseColorProp, color);
            skinPropertyBlock.SetColor(ColorProp, color);

            SetBlockOnRenderer(bodyHead, skinPropertyBlock);
            SetBlockOnRenderer(bodyTorso, skinPropertyBlock);
            SetBlockOnRenderer(bodyArms, skinPropertyBlock);
            SetBlockOnRenderer(bodyHands, skinPropertyBlock);
            SetBlockOnRenderer(bodyLegs, skinPropertyBlock);
            SetBlockOnRenderer(bodyFeet, skinPropertyBlock);
        }

        public void ApplyArmorDye(Color color)
        {
            if (armorPropertyBlock == null) armorPropertyBlock = new MaterialPropertyBlock();
            armorPropertyBlock.SetColor(BaseColorProp, color);
            armorPropertyBlock.SetColor(ColorProp, color);

            SetBlockOnRenderer(armorTorso, armorPropertyBlock);
            SetBlockOnRenderer(armorGauntlets, armorPropertyBlock);
            SetBlockOnRenderer(armorLeggings, armorPropertyBlock);
            SetBlockOnRenderer(armorBoots, armorPropertyBlock);
            SetBlockOnRenderer(armorHelmet, armorPropertyBlock);
        }

        public void EquipWeapon(string weaponId)
        {
            if (activeWeaponInstance != null)
            {
                Destroy(activeWeaponInstance);
                activeWeaponInstance = null;
            }

            if (!rightHandSocket) AutoDetectRenderers();
            if (!rightHandSocket) return;
            if (string.IsNullOrEmpty(weaponId) || weaponId == "none") return;

            string prefabPath = weaponId.ToLower() switch
            {
                "axe" => "Assets/Models/Equipment/Weapon_TwoHanded_BattleAxe.fbx",
                "staff" => "Assets/Models/Equipment/Weapon_Staff.fbx",
                "wand" => "Assets/Models/Equipment/Weapon_Wand.fbx",
                _ => "Assets/Models/Equipment/Weapon_Sword.fbx"
            };

            var wPrefab = Resources.Load<GameObject>(prefabPath);
#if UNITY_EDITOR
            if (wPrefab == null)
            {
                wPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
#endif
            if (wPrefab != null)
            {
                activeWeaponInstance = Instantiate(wPrefab, rightHandSocket);
                activeWeaponInstance.name = $"Equipped_{weaponId}";
                activeWeaponInstance.transform.localPosition = Vector3.zero;
                activeWeaponInstance.transform.localRotation = Quaternion.identity;
                activeWeaponInstance.transform.localScale = Vector3.one;

                // Bind weapon material if needed
                var rend = activeWeaponInstance.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    string matPath = weaponId.ToLower() switch
                    {
                        "axe" => "Assets/Models/Equipment/Materials/M_Weapon_TwoHanded_BattleAxe.mat",
                        "staff" => "Assets/Models/Equipment/Materials/M_Weapon_Staff.mat",
                        "wand" => "Assets/Models/Equipment/Materials/M_Weapon_Wand.mat",
                        _ => "Assets/Models/Equipment/Materials/M_Weapon_Sword.mat"
                    };
#if UNITY_EDITOR
                    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat != null) rend.sharedMaterial = mat;
#endif
                }
            }
        }

        public void ApplyBodyType(int presetIndex)
        {
            if (currentData == null) currentData = new ModularPlayerAppearanceData();
            currentData.ApplyBodyTypePreset(presetIndex);
            ApplyAppearance(currentData);
        }

        private void ApplyBoneMorphs(ModularPlayerAppearanceData data)
        {
            transform.localScale = Vector3.one * Mathf.Clamp(data.heightScale, 0.70f, 1.35f);

            var headBone = FindChildRecursive(transform, "Head");
            if (headBone != null) headBone.localScale = Vector3.one * Mathf.Clamp(data.headScale, 0.75f, 1.30f);

            var chestBone = FindChildRecursive(transform, "Chest");
            if (chestBone != null)
            {
                chestBone.localScale = new Vector3(
                    Mathf.Clamp(data.shoulderWidth, 0.75f, 1.40f),
                    1.0f,
                    Mathf.Clamp(data.chestScale, 0.75f, 1.45f)
                );
            }

            var spineBone = FindChildRecursive(transform, "Spine");
            if (spineBone != null)
            {
                spineBone.localScale = new Vector3(
                    Mathf.Clamp(data.waistScale, 0.70f, 1.30f),
                    1.0f,
                    Mathf.Clamp(data.waistScale, 0.70f, 1.30f)
                );
            }

            var hipsBone = FindChildRecursive(transform, "Hips");
            if (hipsBone != null)
            {
                hipsBone.localScale = new Vector3(
                    Mathf.Clamp(data.hipScale, 0.75f, 1.40f),
                    1.0f,
                    Mathf.Clamp(data.hipScale, 0.75f, 1.40f)
                );
            }

            // Limb Musculature / Thickness (Arms & Legs)
            float armScale = Mathf.Clamp(data.armThickness, 0.70f, 1.40f);
            var leftArm = FindChildRecursive(transform, "LeftArm");
            if (leftArm != null) leftArm.localScale = new Vector3(armScale, 1.0f, armScale);
            var rightArm = FindChildRecursive(transform, "RightArm");
            if (rightArm != null) rightArm.localScale = new Vector3(armScale, 1.0f, armScale);
            var leftForeArm = FindChildRecursive(transform, "LeftForeArm");
            if (leftForeArm != null) leftForeArm.localScale = new Vector3(armScale, 1.0f, armScale);
            var rightForeArm = FindChildRecursive(transform, "RightForeArm");
            if (rightForeArm != null) rightForeArm.localScale = new Vector3(armScale, 1.0f, armScale);

            float legScale = Mathf.Clamp(data.legThickness, 0.70f, 1.40f);
            var leftUpLeg = FindChildRecursive(transform, "LeftUpLeg");
            if (leftUpLeg != null) leftUpLeg.localScale = new Vector3(legScale, 1.0f, legScale);
            var rightUpLeg = FindChildRecursive(transform, "RightUpLeg");
            if (rightUpLeg != null) rightUpLeg.localScale = new Vector3(legScale, 1.0f, legScale);
            var leftLeg = FindChildRecursive(transform, "LeftLeg");
            if (leftLeg != null) leftLeg.localScale = new Vector3(legScale, 1.0f, legScale);
            var rightLeg = FindChildRecursive(transform, "RightLeg");
            if (rightLeg != null) rightLeg.localScale = new Vector3(legScale, 1.0f, legScale);
        }

        private static void SetBlockOnRenderer(Renderer rend, MaterialPropertyBlock block)
        {
            if (rend != null && rend.gameObject.activeInHierarchy)
            {
                rend.SetPropertyBlock(block);
            }
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
