using UnityEngine;
using BattleSummoner.World;

namespace BattleSummoner.Animation
{
    public enum NPCAmbientBehavior
    {
        DefaultIdleBreathing = 0,
        BlacksmithCrafting = 1, // Human Crafting: Anvil hammering cadence
        BardDancing = 2,        // Human Dance: Tavern folk jig & joyful hop
        BakerCooking = 3,       // Free Cooking: Stirring & seasoning cadence
        GuardVigilance = 4,     // Sentry scanning & upright watchpost stance
        AlchemistBrewing = 5    // Alembic examination & vial swirling
    }

    /// <summary>
    /// NPCSecondaryIdleAnimator provides lifelike ambient motion (breathing, body sway, crafting, dancing)
    /// for both rigged humanoid NPCs and creature meshes in the overworld.
    /// Operates in both Edit Mode ([ExecuteAlways]) and Play Mode so NPCs are visibly alive from every angle.
    /// Interacts seamlessly with OverworldNPCInteractable to pause activity during player conversation.
    /// </summary>
    [ExecuteAlways]
    public class NPCSecondaryIdleAnimator : MonoBehaviour
    {
        [Header("Ambient Life-Sim Behavior")]
        public NPCAmbientBehavior ambientBehavior = NPCAmbientBehavior.DefaultIdleBreathing;

        [Header("Breathing & Sway Parameters")]
        public float breathingSpeed = 1.4f;
        public float breathingHeightAmplitude = 0f;
        public float breathingChestScaleAmplitude = 0.04f;
        public float idleSwayAngle = 1.5f;
        public float idleSwaySpeed = 0.8f;

        [Header("Animator Linkage")]
        public Animator animator;
        public RuntimeAnimatorController defaultController;

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private Vector3 initialScale = Vector3.one;
        private float randomOffset;
        private OverworldNPCInteractable _interactable;

        // Chest / Breast bone isolation
        private Transform chestBone;
        private Vector3 initialChestScale = Vector3.one;
        private Transform[] chestChildren;
        private Vector3[] initialChestChildrenScales;

        // Animator Hashes
        private static readonly int HashCraftingAction = Animator.StringToHash("CraftingAction");
        private static readonly int HashDanceAction = Animator.StringToHash("DanceAction");
        private static readonly int HashCookingAction = Animator.StringToHash("CookingAction");

        private void Awake()
        {
            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
            initialScale = transform.localScale;
            if (initialScale == Vector3.zero) initialScale = Vector3.one;
            randomOffset = Random.Range(0f, 100f);

            _interactable = GetComponent<OverworldNPCInteractable>();
            AutoDetectBehavior();
            ResolveAnimator();
            FindChestBone();
        }

        private void OnEnable()
        {
            if (initialScale == Vector3.zero) initialScale = transform.localScale;
            if (initialScale == Vector3.zero) initialScale = Vector3.one;
            _interactable = GetComponent<OverworldNPCInteractable>();
            AutoDetectBehavior();
            ResolveAnimator();
            FindChestBone();
        }

        private void OnDisable()
        {
            // Restore exact root transform so editor/play mode transitions never leave NPCs distorted
            transform.localScale = initialScale;
            transform.localPosition = initialLocalPos;
            transform.localRotation = initialLocalRot;

            if (chestBone != null)
            {
                chestBone.localScale = initialChestScale;
                if (chestChildren != null && initialChestChildrenScales != null)
                {
                    for (int i = 0; i < chestChildren.Length; i++)
                    {
                        if (chestChildren[i] != null && i < initialChestChildrenScales.Length)
                        {
                            chestChildren[i].localScale = initialChestChildrenScales[i];
                        }
                    }
                }
            }
        }

        public void FindChestBone()
        {
            if (chestBone != null) return;

            // 1. Try animator humanoid binding
            if (animator != null && animator.isHuman)
            {
                chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (chestBone == null) chestBone = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            }

            // 2. Search hierarchy for Chest or Breast bone
            if (chestBone == null)
            {
                chestBone = FindDeepChild(transform, "Chest") ??
                            FindDeepChild(transform, "breast") ??
                            FindDeepChild(transform, "Spine");
            }

            if (chestBone != null)
            {
                initialChestScale = chestBone.localScale;
                if (initialChestScale == Vector3.zero) initialChestScale = Vector3.one;

                int childCount = chestBone.childCount;
                chestChildren = new Transform[childCount];
                initialChestChildrenScales = new Vector3[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    chestChildren[i] = chestBone.GetChild(i);
                    initialChestChildrenScales[i] = chestChildren[i].localScale;
                    if (initialChestChildrenScales[i] == Vector3.zero) initialChestChildrenScales[i] = Vector3.one;
                }
            }
        }

        private Transform FindDeepChild(Transform parent, string targetName)
        {
            if (parent.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindDeepChild(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        public void AutoDetectBehavior()
        {
            if (ambientBehavior != NPCAmbientBehavior.DefaultIdleBreathing) return;

            string n = gameObject.name.ToLowerInvariant();
            string r = _interactable != null ? _interactable.npcRole.ToLowerInvariant() : "";

            if (n.Contains("blacksmith") || n.Contains("torvald") || r.Contains("forge") || r.Contains("armory"))
            {
                ambientBehavior = NPCAmbientBehavior.BlacksmithCrafting;
            }
            else if (n.Contains("bard") || n.Contains("lyra") || r.Contains("minstrel") || r.Contains("dance"))
            {
                ambientBehavior = NPCAmbientBehavior.BardDancing;
            }
            else if (n.Contains("baker") || n.Contains("pierre") || r.Contains("delicacies") || r.Contains("cook"))
            {
                ambientBehavior = NPCAmbientBehavior.BakerCooking;
            }
            else if (n.Contains("alchemist") || n.Contains("lysandra") || r.Contains("apothecary"))
            {
                ambientBehavior = NPCAmbientBehavior.AlchemistBrewing;
            }
            else if (n.Contains("guard") || n.Contains("sentinel") || n.Contains("scout") || n.Contains("marshal") || r.Contains("bastion") || r.Contains("commander"))
            {
                ambientBehavior = NPCAmbientBehavior.GuardVigilance;
            }
        }

        public void ResolveAnimator()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null && animator.runtimeAnimatorController == null && defaultController != null)
            {
                animator.runtimeAnimatorController = defaultController;
            }

            ApplyAnimatorParameters();
        }

        private void ApplyAnimatorParameters()
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;

            switch (ambientBehavior)
            {
                case NPCAmbientBehavior.BlacksmithCrafting:
                    animator.SetInteger(HashCraftingAction, 1);
                    break;
                case NPCAmbientBehavior.BardDancing:
                    animator.SetInteger(HashDanceAction, 1);
                    break;
                case NPCAmbientBehavior.BakerCooking:
                    animator.SetInteger(HashCookingAction, 1);
                    break;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            float t = (Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup) + randomOffset;
#else
            float t = Time.time + randomOffset;
#endif

            // Check if NPC is actively in dialogue with the player
            bool isTalking = _interactable != null && _interactable.IsCurrentlyTalking;

            if (isTalking)
            {
                // Polite attentive listening: subtle breathing, facing player
                float listenBreath = Mathf.Sin(t * 1.8f);
                transform.localPosition = initialLocalPos + Vector3.up * (listenBreath * 0.008f);
                return;
            }

            // Execute specialized life-sim ambient motions
            switch (ambientBehavior)
            {
                case NPCAmbientBehavior.BlacksmithCrafting:
                    UpdateBlacksmithHammering(t);
                    break;

                case NPCAmbientBehavior.BardDancing:
                    UpdateBardDancing(t);
                    break;

                case NPCAmbientBehavior.BakerCooking:
                    UpdateBakerCooking(t);
                    break;

                case NPCAmbientBehavior.GuardVigilance:
                    UpdateGuardVigilance(t);
                    break;

                case NPCAmbientBehavior.AlchemistBrewing:
                    UpdateAlchemistBrewing(t);
                    break;

                default:
                    UpdateDefaultBreathing(t);
                    break;
            }
        }

        private void UpdateDefaultBreathing(float t)
        {
            // 1. Procedural breathing chest/height pulsation - KEEP ROOT SCALE STATIC!
            float breath = Mathf.Sin(t * breathingSpeed);

            // Keep root local scale strictly constant so the character never grows and shrinks
            transform.localScale = initialScale;

            // Subtle vertical translation if configured (default 0 keeps feet firmly grounded)
            if (breathingHeightAmplitude > 0.0001f)
            {
                Vector3 targetPos = initialLocalPos + Vector3.up * (breath * breathingHeightAmplitude);
                transform.localPosition = targetPos;
            }

            // 2. Procedural idle sway (gentle breathing tilt)
            float sway = Mathf.Sin(t * idleSwaySpeed) * idleSwayAngle;
            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, sway * 0.5f, sway * 0.3f);

            // 3. Isolated chest / breast piece expansion ONLY
            if (chestBone == null) FindChestBone();

            if (chestBone != null)
            {
                // Inhale expands ribcage primarily forward (Z) and laterally (X)
                float expandX = 1f + breath * (breathingChestScaleAmplitude * 0.7f);
                float expandY = 1f + breath * (breathingChestScaleAmplitude * 0.3f);
                float expandZ = 1f + breath * (breathingChestScaleAmplitude * 1.1f);

                chestBone.localScale = new Vector3(
                    initialChestScale.x * expandX,
                    initialChestScale.y * expandY,
                    initialChestScale.z * expandZ
                );

                // Counter-scale children (shoulders, neck, head) so ONLY the breast/chest piece expands and contracts!
                if (chestChildren != null && initialChestChildrenScales != null)
                {
                    for (int i = 0; i < chestChildren.Length; i++)
                    {
                        if (chestChildren[i] != null && i < initialChestChildrenScales.Length)
                        {
                            chestChildren[i].localScale = new Vector3(
                                initialChestChildrenScales[i].x / expandX,
                                initialChestChildrenScales[i].y / expandY,
                                initialChestChildrenScales[i].z / expandZ
                            );
                        }
                    }
                }
            }
        }

        private void UpdateBlacksmithHammering(float t)
        {
            // 1.3s crafting cycle: wind-up -> hammer strike -> anvil hold
            float cycle = (t * 0.77f) % 1.0f; // 0 to 1
            float heightOffset = 0f;
            float pitchAngle = 0f;

            if (cycle < 0.75f)
            {
                // Slow deliberate wind-up
                float p = cycle / 0.75f;
                heightOffset = Mathf.Sin(p * Mathf.PI * 0.5f) * 0.04f;
                pitchAngle = -Mathf.Sin(p * Mathf.PI * 0.5f) * 6.0f; // leaning back
            }
            else
            {
                // Rapid forceful downward hammer strike
                float p = (cycle - 0.75f) / 0.25f;
                heightOffset = Mathf.Lerp(0.04f, -0.025f, Mathf.Pow(p, 2f));
                pitchAngle = Mathf.Lerp(-6.0f, 8.5f, p); // strike down
            }

            transform.localPosition = initialLocalPos + Vector3.up * heightOffset;
            transform.localRotation = initialLocalRot * Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        private void UpdateBardDancing(float t)
        {
            // Upbeat 2.6Hz folk jig: joyful hops, alternating heel drops, and cheerful sway
            float hopSpeed = 2.6f;
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * hopSpeed)) * 0.065f;
            float roll = Mathf.Sin(t * Mathf.PI * hopSpeed * 0.5f) * 5.5f;
            float yawBob = Mathf.Cos(t * Mathf.PI * hopSpeed * 0.5f) * 3.5f;

            transform.localPosition = initialLocalPos + Vector3.up * hop;
            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, yawBob, roll);
        }

        private void UpdateBakerCooking(float t)
        {
            // Gentle circular pot stirring and inspection nod
            float stirSpeed = 1.9f;
            float xOffset = Mathf.Cos(t * stirSpeed) * 0.015f;
            float zOffset = Mathf.Sin(t * stirSpeed) * 0.015f;
            float nod = Mathf.Sin(t * 0.6f) * 4.0f;

            transform.localPosition = initialLocalPos + new Vector3(xOffset, 0f, zOffset);
            transform.localRotation = initialLocalRot * Quaternion.Euler(nod, 0f, 0f);
        }

        private void UpdateGuardVigilance(float t)
        {
            // Disciplined standing watch: slow perimeter scanning (left, forward, right)
            float scanCycle = Mathf.Sin(t * 0.35f);
            float scanYaw = scanCycle * 28.0f;
            float subtleBreath = Mathf.Sin(t * 1.2f) * 0.008f;

            transform.localPosition = initialLocalPos + Vector3.up * subtleBreath;
            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, scanYaw, 0f);
        }

        private void UpdateAlchemistBrewing(float t)
        {
            // Careful vial swirl with periodic magnifying examination
            float swirl = Mathf.Sin(t * 2.2f) * 0.012f;
            float examTilt = Mathf.Sin(t * 0.8f) * 4.5f;

            transform.localPosition = initialLocalPos + Vector3.up * (swirl * 0.5f);
            transform.localRotation = initialLocalRot * Quaternion.Euler(examTilt, 0f, swirl * 50f);
        }
    }
}
