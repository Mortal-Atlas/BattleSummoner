using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Customization
{
    /// <summary>
    /// CustomizablePlayerAvatar procedural anatomical rig and real-time morph engine.
    /// Manages 3D body bones, facial features, swappable hair styles, and morphology scaling:
    /// - Neck Length, Arm Length, Torso Scale, Hips Scale, Legs Length, Feet Scale.
    /// - Face Shape, Skin Tone, Eyes (with Heterochromia), Nose, Mouth, and Hair.
    /// Works seamlessly on Player_Hero in-world and on UI 3D preview turntables.
    /// </summary>
    [ExecuteAlways]
    public class CustomizablePlayerAvatar : MonoBehaviour
    {
        public static CustomizablePlayerAvatar Instance { get; private set; }

        [Header("Customization Data")]
        [SerializeField] private CharacterCustomizationData activeData;
        public CharacterCustomizationData CurrentData => activeData;

        [Header("Procedural Rig Roots")]
        [SerializeField] private Transform avatarRoot;
        [SerializeField] private Transform hipsBone;
        [SerializeField] private Transform torsoBone;
        [SerializeField] private Transform chestBone;
        [SerializeField] private Transform neckBone;
        [SerializeField] private Transform headBone;

        [Header("Facial Bones")]
        [SerializeField] private Transform faceShapeRoot;
        [SerializeField] private Transform leftEyeBone;
        [SerializeField] private Transform rightEyeBone;
        [SerializeField] private Transform noseBone;
        [SerializeField] private Transform mouthBone;
        [SerializeField] private Transform hairRoot;

        [Header("Limbs Roots")]
        [SerializeField] private Transform leftUpperArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;

        [SerializeField] private Transform leftThigh;
        [SerializeField] private Transform leftShin;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightThigh;
        [SerializeField] private Transform rightShin;
        [SerializeField] private Transform rightFoot;

        [Header("Hair Style GameObjects")]
        private readonly Dictionary<HairStyleOption, GameObject> hairStyleObjects = new Dictionary<HairStyleOption, GameObject>();

        [Header("Dynamic Materials")]
        private Material skinMaterial;
        private Material hairMaterial;
        private Material eyeLeftMaterial;
        private Material eyeRightMaterial;
        private Material outfitPrimaryMaterial;
        private Material outfitSecondaryMaterial;

        // Procedural Idle Breathing
        private float breathTimer = 0f;
        private Vector3 initialChestScale = Vector3.one;

        private void Awake()
        {
            if (Instance == null && gameObject.name.Contains("Player_Hero"))
            {
                Instance = this;
            }
        }

        private void Start()
        {
            if (activeData == null)
            {
                activeData = CharacterCustomizationData.LoadFromPrefs();
            }
            EnsureRigConstructed();
            ApplyCustomization(activeData);
        }

        private void Update()
        {
            // Subtle living breathing animation
            if (chestBone != null)
            {
                breathTimer += Time.deltaTime * 2.2f;
                float breathPulse = Mathf.Sin(breathTimer) * 0.025f;
                chestBone.localScale = new Vector3(
                    initialChestScale.x * (1f + breathPulse * 0.5f),
                    initialChestScale.y * (1f + breathPulse),
                    initialChestScale.z * (1f + breathPulse * 0.7f)
                );
            }
        }

        #region Rig Generation
        public void EnsureRigConstructed()
        {
            if (avatarRoot == null)
            {
                var existing = transform.Find("AvatarRigRoot");
                if (existing != null)
                {
                    avatarRoot = existing;
                }
                else
                {
                    var rootGo = new GameObject("AvatarRigRoot");
                    rootGo.transform.SetParent(transform, false);
                    rootGo.transform.localPosition = Vector3.zero;
                    rootGo.transform.localRotation = Quaternion.identity;
                    avatarRoot = rootGo.transform;
                }
            }

            // Hide legacy basic capsule mesh if present on the parent
            var parentRenderer = GetComponent<MeshRenderer>();
            if (parentRenderer != null && avatarRoot != null)
            {
                parentRenderer.enabled = false;
            }

            InitMaterials();
            BuildBonesHierarchy();
            BuildHairStyles();
        }

        private void InitMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (skinMaterial == null)
            {
                skinMaterial = new Material(litShader) { name = "Avatar_Skin_Runtime" };
                skinMaterial.SetFloat("_Smoothness", 0.35f);
            }
            if (hairMaterial == null)
            {
                hairMaterial = new Material(litShader) { name = "Avatar_Hair_Runtime" };
                hairMaterial.SetFloat("_Smoothness", 0.65f);
            }
            if (eyeLeftMaterial == null)
            {
                eyeLeftMaterial = new Material(litShader) { name = "Avatar_EyeL_Runtime" };
                eyeLeftMaterial.SetFloat("_Smoothness", 0.90f);
            }
            if (eyeRightMaterial == null)
            {
                eyeRightMaterial = new Material(litShader) { name = "Avatar_EyeR_Runtime" };
                eyeRightMaterial.SetFloat("_Smoothness", 0.90f);
            }
            if (outfitPrimaryMaterial == null)
            {
                outfitPrimaryMaterial = new Material(litShader) { name = "Avatar_OutfitPri_Runtime" };
                outfitPrimaryMaterial.SetFloat("_Smoothness", 0.45f);
            }
            if (outfitSecondaryMaterial == null)
            {
                outfitSecondaryMaterial = new Material(litShader) { name = "Avatar_OutfitSec_Runtime" };
                outfitSecondaryMaterial.SetFloat("_Smoothness", 0.70f);
                outfitSecondaryMaterial.SetFloat("_Metallic", 0.60f);
            }
        }

        private void BuildBonesHierarchy()
        {
            // 1. Hips (Pelvis)
            if (hipsBone == null) hipsBone = GetOrCreateBone(avatarRoot, "HipsBone", new Vector3(0, 0.92f, 0));
            CreatePartMesh(hipsBone, "HipsMesh", PrimitiveType.Sphere, new Vector3(0, 0, 0), new Vector3(0.38f, 0.22f, 0.28f), outfitPrimaryMaterial);

            // 2. Torso (Spine / Abdomen)
            if (torsoBone == null) torsoBone = GetOrCreateBone(hipsBone, "TorsoBone", new Vector3(0, 0.16f, 0));
            CreatePartMesh(torsoBone, "AbdomenMesh", PrimitiveType.Cylinder, new Vector3(0, 0.10f, 0), new Vector3(0.30f, 0.10f, 0.22f), outfitPrimaryMaterial);

            // 3. Chest (Ribcage / Shoulders)
            if (chestBone == null) chestBone = GetOrCreateBone(torsoBone, "ChestBone", new Vector3(0, 0.22f, 0));
            CreatePartMesh(chestBone, "ChestArmorMesh", PrimitiveType.Cube, new Vector3(0, 0.10f, 0), new Vector3(0.40f, 0.24f, 0.24f), outfitSecondaryMaterial);

            // 4. Neck
            if (neckBone == null) neckBone = GetOrCreateBone(chestBone, "NeckBone", new Vector3(0, 0.22f, 0));
            CreatePartMesh(neckBone, "NeckMesh", PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(0.12f, 0.06f, 0.12f), skinMaterial);

            // 5. Head
            if (headBone == null) headBone = GetOrCreateBone(neckBone, "HeadBone", new Vector3(0, 0.12f, 0));
            if (faceShapeRoot == null) faceShapeRoot = GetOrCreateBone(headBone, "FaceShapeRoot", Vector3.zero);
            CreatePartMesh(faceShapeRoot, "HeadCranium", PrimitiveType.Sphere, new Vector3(0, 0.04f, -0.01f), new Vector3(0.24f, 0.28f, 0.25f), skinMaterial);
            CreatePartMesh(faceShapeRoot, "JawChin", PrimitiveType.Cube, new Vector3(0, -0.06f, 0.05f), new Vector3(0.15f, 0.10f, 0.12f), skinMaterial);

            // 6. Facial Features
            // Eyes
            if (leftEyeBone == null) leftEyeBone = GetOrCreateBone(headBone, "LeftEyeBone", new Vector3(-0.062f, 0.035f, 0.11f));
            CreatePartMesh(leftEyeBone, "EyeIrisL", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.040f, 0.035f, 0.025f), eyeLeftMaterial);

            if (rightEyeBone == null) rightEyeBone = GetOrCreateBone(headBone, "RightEyeBone", new Vector3(0.062f, 0.035f, 0.11f));
            CreatePartMesh(rightEyeBone, "EyeIrisR", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.040f, 0.035f, 0.025f), eyeRightMaterial);

            // Nose
            if (noseBone == null) noseBone = GetOrCreateBone(headBone, "NoseBone", new Vector3(0, -0.005f, 0.125f));
            CreatePartMesh(noseBone, "NoseMesh", PrimitiveType.Cube, Vector3.zero, new Vector3(0.025f, 0.050f, 0.045f), skinMaterial);

            // Mouth
            if (mouthBone == null) mouthBone = GetOrCreateBone(headBone, "MouthBone", new Vector3(0, -0.065f, 0.105f));
            CreatePartMesh(mouthBone, "MouthMesh", PrimitiveType.Cube, Vector3.zero, new Vector3(0.055f, 0.018f, 0.020f), outfitSecondaryMaterial);

            // Hair Root
            if (hairRoot == null) hairRoot = GetOrCreateBone(headBone, "HairRoot", Vector3.zero);

            // 7. Arms
            // Left Arm
            if (leftUpperArm == null) leftUpperArm = GetOrCreateBone(chestBone, "LeftUpperArm", new Vector3(-0.25f, 0.14f, 0));
            CreatePartMesh(leftUpperArm, "UpperArmMeshL", PrimitiveType.Cylinder, new Vector3(0, -0.14f, 0), new Vector3(0.09f, 0.14f, 0.09f), outfitPrimaryMaterial);

            if (leftForearm == null) leftForearm = GetOrCreateBone(leftUpperArm, "LeftForearm", new Vector3(0, -0.28f, 0));
            CreatePartMesh(leftForearm, "ForearmMeshL", PrimitiveType.Cylinder, new Vector3(0, -0.13f, 0), new Vector3(0.08f, 0.13f, 0.08f), skinMaterial);

            if (leftHand == null) leftHand = GetOrCreateBone(leftForearm, "LeftHand", new Vector3(0, -0.26f, 0));
            CreatePartMesh(leftHand, "HandMeshL", PrimitiveType.Cube, new Vector3(0, -0.04f, 0), new Vector3(0.06f, 0.08f, 0.05f), outfitSecondaryMaterial);

            // Right Arm
            if (rightUpperArm == null) rightUpperArm = GetOrCreateBone(chestBone, "RightUpperArm", new Vector3(0.25f, 0.14f, 0));
            CreatePartMesh(rightUpperArm, "UpperArmMeshR", PrimitiveType.Cylinder, new Vector3(0, -0.14f, 0), new Vector3(0.09f, 0.14f, 0.09f), outfitPrimaryMaterial);

            if (rightForearm == null) rightForearm = GetOrCreateBone(rightUpperArm, "RightForearm", new Vector3(0, -0.28f, 0));
            CreatePartMesh(rightForearm, "ForearmMeshR", PrimitiveType.Cylinder, new Vector3(0, -0.13f, 0), new Vector3(0.08f, 0.13f, 0.08f), skinMaterial);

            if (rightHand == null) rightHand = GetOrCreateBone(rightForearm, "RightHand", new Vector3(0, -0.26f, 0));
            CreatePartMesh(rightHand, "HandMeshR", PrimitiveType.Cube, new Vector3(0, -0.04f, 0), new Vector3(0.06f, 0.08f, 0.05f), outfitSecondaryMaterial);

            // 8. Legs
            // Left Leg
            if (leftThigh == null) leftThigh = GetOrCreateBone(hipsBone, "LeftThigh", new Vector3(-0.13f, -0.08f, 0));
            CreatePartMesh(leftThigh, "ThighMeshL", PrimitiveType.Cylinder, new Vector3(0, -0.19f, 0), new Vector3(0.12f, 0.19f, 0.12f), outfitPrimaryMaterial);

            if (leftShin == null) leftShin = GetOrCreateBone(leftThigh, "LeftShin", new Vector3(0, -0.38f, 0));
            CreatePartMesh(leftShin, "ShinMeshL", PrimitiveType.Cylinder, new Vector3(0, -0.18f, 0), new Vector3(0.10f, 0.18f, 0.10f), outfitPrimaryMaterial);

            if (leftFoot == null) leftFoot = GetOrCreateBone(leftShin, "LeftFoot", new Vector3(0, -0.36f, 0.04f));
            CreatePartMesh(leftFoot, "FootMeshL", PrimitiveType.Cube, Vector3.zero, new Vector3(0.11f, 0.07f, 0.22f), outfitSecondaryMaterial);

            // Right Leg
            if (rightThigh == null) rightThigh = GetOrCreateBone(hipsBone, "RightThigh", new Vector3(0.13f, -0.08f, 0));
            CreatePartMesh(rightThigh, "ThighMeshR", PrimitiveType.Cylinder, new Vector3(0, -0.19f, 0), new Vector3(0.12f, 0.19f, 0.12f), outfitPrimaryMaterial);

            if (rightShin == null) rightShin = GetOrCreateBone(rightThigh, "RightShin", new Vector3(0, -0.38f, 0));
            CreatePartMesh(rightShin, "ShinMeshR", PrimitiveType.Cylinder, new Vector3(0, -0.18f, 0), new Vector3(0.10f, 0.18f, 0.10f), outfitPrimaryMaterial);

            if (rightFoot == null) rightFoot = GetOrCreateBone(rightShin, "RightFoot", new Vector3(0, -0.36f, 0.04f));
            CreatePartMesh(rightFoot, "FootMeshR", PrimitiveType.Cube, Vector3.zero, new Vector3(0.11f, 0.07f, 0.22f), outfitSecondaryMaterial);

            initialChestScale = chestBone.localScale;
        }

        private void BuildHairStyles()
        {
            if (hairRoot == null) return;
            hairStyleObjects.Clear();

            // 1. Spiky Anime
            var spiky = GetOrCreateChild(hairRoot, "Hair_SpikyAnime");
            CreatePartMesh(spiky.transform, "Crown", PrimitiveType.Sphere, new Vector3(0, 0.08f, -0.02f), new Vector3(0.26f, 0.18f, 0.26f), hairMaterial);
            CreatePartMesh(spiky.transform, "Spike1", PrimitiveType.Cylinder, new Vector3(0, 0.20f, 0.02f), new Vector3(0.06f, 0.12f, 0.06f), hairMaterial);
            CreatePartMesh(spiky.transform, "Spike2", PrimitiveType.Cylinder, new Vector3(-0.08f, 0.18f, -0.04f), new Vector3(0.05f, 0.10f, 0.05f), hairMaterial);
            CreatePartMesh(spiky.transform, "Spike3", PrimitiveType.Cylinder, new Vector3(0.08f, 0.18f, -0.04f), new Vector3(0.05f, 0.10f, 0.05f), hairMaterial);
            CreatePartMesh(spiky.transform, "Bangs", PrimitiveType.Cube, new Vector3(0, 0.08f, 0.13f), new Vector3(0.20f, 0.06f, 0.04f), hairMaterial);
            hairStyleObjects[HairStyleOption.SpikyAnime] = spiky;

            // 2. Long Ponytail
            var ponytail = GetOrCreateChild(hairRoot, "Hair_LongPonytail");
            CreatePartMesh(ponytail.transform, "Crown", PrimitiveType.Sphere, new Vector3(0, 0.07f, -0.02f), new Vector3(0.26f, 0.17f, 0.26f), hairMaterial);
            CreatePartMesh(ponytail.transform, "Tie", PrimitiveType.Cylinder, new Vector3(0, 0.09f, -0.15f), new Vector3(0.06f, 0.03f, 0.06f), outfitSecondaryMaterial);
            CreatePartMesh(ponytail.transform, "TailUpper", PrimitiveType.Cylinder, new Vector3(0, -0.05f, -0.22f), new Vector3(0.07f, 0.16f, 0.07f), hairMaterial);
            CreatePartMesh(ponytail.transform, "TailLower", PrimitiveType.Cylinder, new Vector3(0, -0.25f, -0.25f), new Vector3(0.05f, 0.14f, 0.05f), hairMaterial);
            hairStyleObjects[HairStyleOption.LongPonytail] = ponytail;

            // 3. Flowing Braids
            var braids = GetOrCreateChild(hairRoot, "Hair_FlowingBraids");
            CreatePartMesh(braids.transform, "Crown", PrimitiveType.Sphere, new Vector3(0, 0.07f, -0.02f), new Vector3(0.26f, 0.17f, 0.26f), hairMaterial);
            CreatePartMesh(braids.transform, "BraidL", PrimitiveType.Cylinder, new Vector3(-0.14f, -0.18f, 0.02f), new Vector3(0.045f, 0.22f, 0.045f), hairMaterial);
            CreatePartMesh(braids.transform, "BraidR", PrimitiveType.Cylinder, new Vector3(0.14f, -0.18f, 0.02f), new Vector3(0.045f, 0.22f, 0.045f), hairMaterial);
            hairStyleObjects[HairStyleOption.FlowingBraids] = braids;

            // 4. Short Crop
            var crop = GetOrCreateChild(hairRoot, "Hair_ShortCrop");
            CreatePartMesh(crop.transform, "Crown", PrimitiveType.Sphere, new Vector3(0, 0.06f, -0.01f), new Vector3(0.255f, 0.15f, 0.255f), hairMaterial);
            CreatePartMesh(crop.transform, "SidesL", PrimitiveType.Cube, new Vector3(-0.13f, 0.02f, 0), new Vector3(0.03f, 0.08f, 0.16f), hairMaterial);
            CreatePartMesh(crop.transform, "SidesR", PrimitiveType.Cube, new Vector3(0.13f, 0.02f, 0), new Vector3(0.03f, 0.08f, 0.16f), hairMaterial);
            hairStyleObjects[HairStyleOption.ShortCrop] = crop;

            // 5. Elegant Bob
            var bob = GetOrCreateChild(hairRoot, "Hair_ElegantBob");
            CreatePartMesh(bob.transform, "Crown", PrimitiveType.Sphere, new Vector3(0, 0.07f, -0.01f), new Vector3(0.26f, 0.18f, 0.26f), hairMaterial);
            CreatePartMesh(bob.transform, "FlankL", PrimitiveType.Cube, new Vector3(-0.14f, -0.05f, 0.02f), new Vector3(0.04f, 0.15f, 0.18f), hairMaterial);
            CreatePartMesh(bob.transform, "FlankR", PrimitiveType.Cube, new Vector3(0.14f, -0.05f, 0.02f), new Vector3(0.04f, 0.15f, 0.18f), hairMaterial);
            CreatePartMesh(bob.transform, "Back", PrimitiveType.Cube, new Vector3(0, -0.06f, -0.12f), new Vector3(0.26f, 0.14f, 0.04f), hairMaterial);
            hairStyleObjects[HairStyleOption.ElegantBob] = bob;

            // 6. Shaggy Locks
            var shaggy = GetOrCreateChild(hairRoot, "Hair_ShaggyLocks");
            CreatePartMesh(shaggy.transform, "Crown", PrimitiveType.Sphere, new Vector3(0, 0.07f, 0), new Vector3(0.27f, 0.19f, 0.27f), hairMaterial);
            CreatePartMesh(shaggy.transform, "LockFrontL", PrimitiveType.Cylinder, new Vector3(-0.11f, -0.04f, 0.12f), new Vector3(0.04f, 0.10f, 0.04f), hairMaterial);
            CreatePartMesh(shaggy.transform, "LockFrontR", PrimitiveType.Cylinder, new Vector3(0.11f, -0.04f, 0.12f), new Vector3(0.04f, 0.10f, 0.04f), hairMaterial);
            CreatePartMesh(shaggy.transform, "LockBack", PrimitiveType.Cylinder, new Vector3(0, -0.08f, -0.14f), new Vector3(0.06f, 0.12f, 0.06f), hairMaterial);
            hairStyleObjects[HairStyleOption.ShaggyLocks] = shaggy;

            // 7. Gothic Twintails
            var twintails = GetOrCreateChild(hairRoot, "Hair_GothicTwintails");
            CreatePartMesh(twintails.transform, "Crown", PrimitiveType.Sphere, new Vector3(0, 0.07f, -0.01f), new Vector3(0.26f, 0.17f, 0.26f), hairMaterial);
            CreatePartMesh(twintails.transform, "TwinL", PrimitiveType.Cylinder, new Vector3(-0.20f, -0.14f, -0.06f), new Vector3(0.07f, 0.22f, 0.07f), hairMaterial);
            CreatePartMesh(twintails.transform, "TwinR", PrimitiveType.Cylinder, new Vector3(0.20f, -0.14f, -0.06f), new Vector3(0.07f, 0.22f, 0.07f), hairMaterial);
            hairStyleObjects[HairStyleOption.GothicTwintails] = twintails;

            // 8. Bald / Buzzed
            var bald = GetOrCreateChild(hairRoot, "Hair_BaldBuzzed");
            // Minimal stubble shadow cap
            CreatePartMesh(bald.transform, "StubbleCap", PrimitiveType.Sphere, new Vector3(0, 0.04f, 0), new Vector3(0.245f, 0.14f, 0.25f), hairMaterial);
            hairStyleObjects[HairStyleOption.BaldBuzzed] = bald;
        }

        private Transform GetOrCreateBone(Transform parent, string boneName, Vector3 localPos)
        {
            var found = parent.Find(boneName);
            if (found != null) return found;
            var go = new GameObject(boneName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private GameObject GetOrCreateChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private void CreatePartMesh(Transform parent, string name, PrimitiveType prim, Vector3 localPos, Vector3 scale, Material mat)
        {
            var found = parent.Find(name);
            GameObject go;
            if (found != null)
            {
                go = found.gameObject;
            }
            else
            {
                go = GameObject.CreatePrimitive(prim);
                go.name = name;
                go.transform.SetParent(parent, false);
                // Remove primitive colliders to prevent interference with player CharacterController
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
            }
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
            }
        }
        #endregion

        #region Customization Application
        public void ApplyCustomization(CharacterCustomizationData data)
        {
            if (data == null) return;
            activeData = data;
            EnsureRigConstructed();

            // 1. Color Palettes
            Color skinCol = CharacterCustomizationData.SkinTones[Mathf.Clamp(data.skinToneIndex, 0, CharacterCustomizationData.SkinTones.Length - 1)];
            Color hairCol = CharacterCustomizationData.HairColors[Mathf.Clamp(data.hairColorIndex, 0, CharacterCustomizationData.HairColors.Length - 1)];
            Color eyeLCol = CharacterCustomizationData.EyeColors[Mathf.Clamp(data.eyeColorIndex, 0, CharacterCustomizationData.EyeColors.Length - 1)];
            Color eyeRCol = data.heterochromia
                ? CharacterCustomizationData.EyeColors[Mathf.Clamp(data.rightEyeColorIndex, 0, CharacterCustomizationData.EyeColors.Length - 1)]
                : eyeLCol;
            Color outfitPri = CharacterCustomizationData.OutfitColors[Mathf.Clamp(data.outfitPrimaryColor, 0, CharacterCustomizationData.OutfitColors.Length - 1)];
            Color outfitSec = CharacterCustomizationData.OutfitColors[Mathf.Clamp(data.outfitSecondaryColor, 0, CharacterCustomizationData.OutfitColors.Length - 1)];

            if (skinMaterial != null) skinMaterial.color = skinCol;
            if (hairMaterial != null) hairMaterial.color = hairCol;
            if (eyeLeftMaterial != null) eyeLeftMaterial.color = eyeLCol;
            if (eyeRightMaterial != null) eyeRightMaterial.color = eyeRCol;
            if (outfitPrimaryMaterial != null) outfitPrimaryMaterial.color = outfitPri;
            if (outfitSecondaryMaterial != null) outfitSecondaryMaterial.color = outfitSec;

            // 2. Neck Length (Scales Y, translates Head up/down)
            float clampedNeck = Mathf.Clamp(data.neckLength, 0.70f, 1.40f);
            if (neckBone != null)
            {
                neckBone.localScale = new Vector3(1f, clampedNeck, 1f);
                if (headBone != null)
                {
                    headBone.localPosition = new Vector3(0, 0.08f + 0.08f * clampedNeck, 0);
                }
            }

            // 3. Arm Length (Scales Y, shifts forearms and hands)
            float clampedArm = Mathf.Clamp(data.armLength, 0.70f, 1.40f);
            if (leftUpperArm != null)
            {
                leftUpperArm.localScale = new Vector3(1f, clampedArm, 1f);
                if (leftForearm != null) leftForearm.localScale = new Vector3(1f, clampedArm, 1f);
            }
            if (rightUpperArm != null)
            {
                rightUpperArm.localScale = new Vector3(1f, clampedArm, 1f);
                if (rightForearm != null) rightForearm.localScale = new Vector3(1f, clampedArm, 1f);
            }

            // 4. Torso & Hips Scaling
            float clampedTorso = Mathf.Clamp(data.torsoScale, 0.70f, 1.40f);
            float clampedHips = Mathf.Clamp(data.hipsScale, 0.70f, 1.40f);
            if (torsoBone != null)
            {
                torsoBone.localScale = new Vector3(clampedTorso, clampedTorso, clampedTorso * 0.95f);
            }
            if (hipsBone != null)
            {
                hipsBone.localScale = new Vector3(clampedHips, 1.0f, clampedHips * 1.05f);
            }

            // 5. Legs Length (Scales Y, adjusts stance height)
            float clampedLegs = Mathf.Clamp(data.legsLength, 0.70f, 1.40f);
            if (leftThigh != null) leftThigh.localScale = new Vector3(1f, clampedLegs, 1f);
            if (rightThigh != null) rightThigh.localScale = new Vector3(1f, clampedLegs, 1f);
            if (leftShin != null) leftShin.localScale = new Vector3(1f, clampedLegs, 1f);
            if (rightShin != null) rightShin.localScale = new Vector3(1f, clampedLegs, 1f);

            // Shift Hips height slightly based on leg length
            if (hipsBone != null)
            {
                hipsBone.localPosition = new Vector3(0, 0.92f * clampedLegs, 0);
            }

            // 6. Feet Scale
            float clampedFeet = Mathf.Clamp(data.feetScale, 0.70f, 1.40f);
            if (leftFoot != null) leftFoot.localScale = new Vector3(clampedFeet, clampedFeet, clampedFeet);
            if (rightFoot != null) rightFoot.localScale = new Vector3(clampedFeet, clampedFeet, clampedFeet);

            // 7. Facial Shapes & Scales
            ApplyFaceShape(data.faceShape);
            ApplyNoseShape(data.noseShape, data.noseScale);
            ApplyMouthShape(data.mouthShape);
            ApplyEyeShape(data.eyeShape);

            // 8. Hair Style Activation
            foreach (var kvp in hairStyleObjects)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(kvp.Key == data.hairStyle);
                }
            }

            // 9. Overall Height Scale
            float clampedHeight = Mathf.Clamp(data.heightScale, 0.85f, 1.15f);
            if (avatarRoot != null)
            {
                avatarRoot.localScale = Vector3.one * clampedHeight;
            }

            // Adjust character controller if on Player_Hero
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.height = 1.95f * clampedHeight * (0.5f + 0.5f * clampedLegs);
                cc.center = new Vector3(0, cc.height * 0.5f, 0);
            }

            // Sync with PlayerProgression
            var prog = GetComponent<PlayerProgression>();
            if (prog != null && !string.IsNullOrEmpty(data.characterName))
            {
                prog.playerName = data.characterName;
            }
        }

        private void ApplyFaceShape(FaceShapePreset preset)
        {
            if (faceShapeRoot == null) return;
            switch (preset)
            {
                case FaceShapePreset.Oval:
                    faceShapeRoot.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case FaceShapePreset.Angular:
                    faceShapeRoot.localScale = new Vector3(0.92f, 1.05f, 0.94f);
                    break;
                case FaceShapePreset.Chiseled:
                    faceShapeRoot.localScale = new Vector3(1.10f, 0.96f, 1.08f);
                    break;
                case FaceShapePreset.SoftRound:
                    faceShapeRoot.localScale = new Vector3(1.08f, 0.92f, 1.02f);
                    break;
                case FaceShapePreset.Heart:
                    faceShapeRoot.localScale = new Vector3(1.05f, 1.02f, 0.90f);
                    break;
                case FaceShapePreset.Sharp:
                    faceShapeRoot.localScale = new Vector3(0.88f, 1.08f, 0.96f);
                    break;
            }
        }

        private void ApplyNoseShape(NoseShapePreset preset, float scale)
        {
            if (noseBone == null) return;
            float s = Mathf.Clamp(scale, 0.70f, 1.30f);
            switch (preset)
            {
                case NoseShapePreset.Straight:
                    noseBone.localScale = new Vector3(s, s, s);
                    break;
                case NoseShapePreset.Aquiline:
                    noseBone.localScale = new Vector3(s * 0.9f, s * 1.25f, s * 1.3f);
                    break;
                case NoseShapePreset.ButtonPetite:
                    noseBone.localScale = new Vector3(s * 0.8f, s * 0.75f, s * 0.75f);
                    break;
                case NoseShapePreset.SharpPointed:
                    noseBone.localScale = new Vector3(s * 0.75f, s * 1.15f, s * 1.4f);
                    break;
                case NoseShapePreset.Broad:
                    noseBone.localScale = new Vector3(s * 1.35f, s * 0.9f, s * 0.95f);
                    break;
            }
        }

        private void ApplyMouthShape(MouthShapePreset preset)
        {
            if (mouthBone == null) return;
            switch (preset)
            {
                case MouthShapePreset.DeterminedNeutral:
                    mouthBone.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case MouthShapePreset.ConfidentSmirk:
                    mouthBone.localScale = new Vector3(1.15f, 0.9f, 1.0f);
                    mouthBone.localRotation = Quaternion.Euler(0, 0, 4f);
                    break;
                case MouthShapePreset.GentleSmile:
                    mouthBone.localScale = new Vector3(1.20f, 1.1f, 1.0f);
                    mouthBone.localRotation = Quaternion.identity;
                    break;
                case MouthShapePreset.PoutingFull:
                    mouthBone.localScale = new Vector3(0.85f, 1.4f, 1.3f);
                    mouthBone.localRotation = Quaternion.identity;
                    break;
                case MouthShapePreset.FangedGrin:
                    mouthBone.localScale = new Vector3(1.25f, 1.2f, 1.1f);
                    mouthBone.localRotation = Quaternion.identity;
                    break;
            }
        }

        private void ApplyEyeShape(EyeShapePreset preset)
        {
            if (leftEyeBone == null || rightEyeBone == null) return;
            Vector3 eyeScale = preset switch
            {
                EyeShapePreset.IntenseAlmond => new Vector3(1.0f, 0.9f, 1.0f),
                EyeShapePreset.WideCurious => new Vector3(1.25f, 1.3f, 1.0f),
                EyeShapePreset.StoicNarrow => new Vector3(0.95f, 0.65f, 1.0f),
                EyeShapePreset.DemonicSlit => new Vector3(0.65f, 1.35f, 1.0f),
                EyeShapePreset.SoftGentle => new Vector3(1.15f, 1.1f, 1.0f),
                _ => Vector3.one
            };
            leftEyeBone.localScale = eyeScale;
            rightEyeBone.localScale = eyeScale;
        }
        #endregion
    }
}
