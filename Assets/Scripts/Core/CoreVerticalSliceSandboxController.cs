using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BattleSummoner.Combat;
using BattleSummoner.Hobbies;
using BattleSummoner.World;
using BattleSummoner.DungeonDimension;

namespace BattleSummoner.Core
{
    [Serializable]
    public class PartyMemberRuntimeData
    {
        public string allyId;
        public string allyName;
        public Sprite personaSideProfileSprite;
        public float currentHp = 100f;
        public float maxHp = 100f;
        public float currentMp = 50f;
        public float maxMp = 50f;
        public Transform overworldInstance;
        [HideInInspector] public Vector3 assignedFormationOffset;
    }

    /// <summary>
    /// Interactive Practice Dummy in the Church Exterior Courtyard.
    /// Implements ISliceable for FruitNinjaBladeCutter kinetic slices and takes elemental spells.
    /// Supports weak-spot seam checking and directional test damage feedback.
    /// </summary>
    public class PracticeDummyTarget : MonoBehaviour, ISliceable
    {
        public string dummyName = "Training Dummy";
        public float dummyHp = 99999f;
        public Renderer dummyMeshRenderer;
        public bool hasWeakSpotSeam = false;
        public Vector2 weakSpotSeamAngle = Vector2.up;
        public SpellAffinity elementalWeakness = SpellAffinity.None;

        public void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage)
        {
            ApplyTestHit(finalDamage, $"Slash ({sliceDirection.normalized})");
        }

        public void OnSliced(Vector2 sliceVelocity, float appliedDamage)
        {
            ApplyTestHit(appliedDamage, $"Slash ({sliceVelocity.normalized})");
        }

        public void OnSpellHit(SpellAffinity affinity, float spellDamage)
        {
            ApplyTestHit(spellDamage, $"Spell [{affinity}]");
        }

        private void ApplyTestHit(float dmg, string sourceType)
        {
            Debug.Log($"[Practice Dummy '{dummyName}'] Hit by {sourceType} for {dmg:F1} damage!");
            if (dummyMeshRenderer != null)
            {
                dummyMeshRenderer.material.color = Color.red;
                CancelInvoke(nameof(ResetDummyColor));
                Invoke(nameof(ResetDummyColor), 0.12f);
            }
        }

        private void ResetDummyColor()
        {
            if (dummyMeshRenderer != null) dummyMeshRenderer.material.color = Color.white;
        }
    }

    /// <summary>
    /// CoreVerticalSliceSandboxController unifies the end-to-end playable vertical slice:
    /// 1. Character Creation & Guardian Contract Signature -> Spawn in Central Church.
    /// 2. Courtyard Practice Dummies & Action Buttons (Z-Target, Swap, Dash, Jump).
    /// 3. Dynamic -5.5deg Right-Edge Ally Column & Staggered Anti-Clipping Boids Follow.
    /// 4. Test Dungeon Portal -> Test Boss (Kaelen) with Anvil-Hide + Clutch Surge & Subterranean Mining.
    /// 5. Return to Town -> Mount Traversal (2.25x speed on wide avenues).
    /// 6. Crafting District (Blacksmith, Cooking Potluck, 3-Strike Anvil, Tomes).
    /// 7. Bazaar Trading with NPC Merchant Bank Caps and 5% Deflationary Burn Fees.
    /// 8. Quick-Access Whetstone, Uncapped Accessories, 3D Relic Inspector & AI Auto.
    /// </summary>
    public class CoreVerticalSliceSandboxController : MonoBehaviour
    {
        public static CoreVerticalSliceSandboxController Instance { get; private set; }

        [Header("1. Onboarding, Town & Test Dungeon Anchors")]
        public Transform churchInteriorSpawnPoint;
        public Transform exteriorTestDungeonPortalPoint;
        public Transform practiceDummyYardPoint;
        public Transform testDungeonBossArenaSpawnPoint;
        public Transform townBazaarAndCraftingPlazaPoint;
        public Transform playerTransform;
        public CharacterController playerController;

        [Header("2. Core Combat & Movement Action Parameters")]
        public float baseWalkSpeed = 6.0f;
        public float mountedMoveSpeed = 13.5f; // 2.25x faster traversal on wide town streets
        public bool isMounted = false;
        public GameObject activeMountVisualPrefab;
        public float dashSpeed = 16f;
        public float dashDuration = 0.22f;
        public float jumpForce = 7.5f;
        public float gravity = -20f;
        public float zTargetRange = 18f;
        public LayerMask targetableEnemyOrDummyLayer = ~0;

        private Vector3 playerVelocity;
        private bool isDashing = false;
        private float dashTimer = 0f;
        public Transform currentZTarget { get; private set; }
        private int zTargetCycleIndex = 0;
        public bool isSecondaryLoadoutActive = false;
        public bool isAiAutopilotEnabled = false;

        [Header("3. Dynamic Right-Edge Party UI & Staggered Overworld Follow")]
        [SerializeField] private RectTransform rightEdgePartyColumnContainer;
        [SerializeField] private GameObject allyPersonaPortraitPrefab;
        public GameObject allyOverworldPrefab;
        public List<PartyMemberRuntimeData> activePartyMembers = new List<PartyMemberRuntimeData>();
        private Dictionary<string, GameObject> spawnedPortraitUIObjects = new Dictionary<string, GameObject>();

        [Header("4. Anti-Clipping & Non-Linear Formation Settings")]
        [Tooltip("Prevents allies from walking in a single straight line.")]
        public float rowSpacingBack = 1.85f;
        public float lateralSpreadWidth = 1.65f;
        [Tooltip("Minimum personal space radius so party members never clip into each other.")]
        public float minSeparationRadius = 1.35f;
        public float separationPushStrength = 6.5f;
        public float followMoveSpeed = 6.0f;

        [Header("5. Vertical Slice Sandbox Inventory & Economy Telemetry")]
        public int playerManaCores = 500;
        public int minedIronOreCount = 0;
        public int minedPrismaticGemsCount = 0;
        public int harvestedBossMeatCount = 0;
        public int npcBazaarMerchantBankBalance = 1200; // Enforces NPC bank cap during Bazaar testing
        public bool hasDefeatedTestBoss = false;
        public bool hasAcquiredBossRelic = false;

        public event Action<Transform> OnZTargetChanged;
        public event Action<bool> OnLoadoutSwapped;
        public event Action<bool> OnMountToggled;
        public event Action<bool> OnAiAutopilotToggled;
        public event Action OnTestBossDefeatedEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolvePlayerReferences();
        }

        private void ResolvePlayerReferences()
        {
            if (playerTransform == null)
            {
                var playerObj = GameObject.Find("Player_Hero");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                    playerController = playerObj.GetComponent<CharacterController>();
                }
            }
        }

        /// <summary>
        /// Step 1 -> Step 2: Called automatically when GuardianSignatureBinder.OnAvatarBoundToWorld fires.
        /// Spawns the newly bound character directly inside the Central Church.
        /// </summary>
        public void CompleteCreationAndSpawnInChurch(GuardianContractData pactData)
        {
            TeleportPlayerTo(churchInteriorSpawnPoint);

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    $"⛪ [CHURCH OF REVIVAL] {pactData.avatarName} has awakened under Guardian {pactData.guardianPrintedName}!", false);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }

            Debug.Log($"[Vertical Slice] '{pactData.avatarName}' bound to Guardian '{pactData.guardianPrintedName}'. Spawned inside Central Church!");
        }

        public void TeleportPlayerTo(Transform targetAnchor)
        {
            if (playerTransform == null || targetAnchor == null) return;
            if (playerController != null) playerController.enabled = false;
            playerTransform.position = targetAnchor.position;
            playerTransform.rotation = targetAnchor.rotation;
            if (playerController != null) playerController.enabled = true;
        }

        private void Update()
        {
            UpdatePlayerVerticalAndDashPhysics();
            MaintainZTargetCameraOrientation();
            UpdatePartyOverworldStaggeredFollowAndAntiClipping();
        }

        // =========================================================================
        // CORE ACTION BUTTONS: Z-TARGETING, WEAPON SWAP, DASH, JUMP, MOUNT, AI AUTO
        // =========================================================================

        public void OnZTargetButtonPressed()
        {
            if (playerTransform == null) return;
            Collider[] hits = Physics.OverlapSphere(playerTransform.position, zTargetRange, targetableEnemyOrDummyLayer);
            if (hits.Length == 0)
            {
                currentZTarget = null;
                OnZTargetChanged?.Invoke(null);
                Debug.Log("[Z-Target] No Practice Dummies or enemies in range. Camera unlocked.");
                return;
            }

            zTargetCycleIndex = (zTargetCycleIndex + 1) % hits.Length;
            currentZTarget = hits[zTargetCycleIndex].transform;
            OnZTargetChanged?.Invoke(currentZTarget);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.6f);
            }

            Debug.Log($"[Z-Target] Locked onto: {currentZTarget.name}");
        }

        public void ClearZTarget()
        {
            currentZTarget = null;
            OnZTargetChanged?.Invoke(null);
        }

        private void MaintainZTargetCameraOrientation()
        {
            if (currentZTarget == null || playerTransform == null) return;
            Vector3 lookDir = currentZTarget.position - playerTransform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                playerTransform.rotation = Quaternion.Slerp(
                    playerTransform.rotation,
                    Quaternion.LookRotation(lookDir),
                    Time.deltaTime * 12f);
            }
        }

        public void OnSwapWeaponButtonPressed()
        {
            isSecondaryLoadoutActive = !isSecondaryLoadoutActive;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.8f);
            }

            OnLoadoutSwapped?.Invoke(isSecondaryLoadoutActive);
            Debug.Log($"[Swap Button] Swapped active combat stance -> {(isSecondaryLoadoutActive ? "Secondary Loadout (Staff/Dual)" : "Primary Loadout (1H Blade + Shield)")}");
        }

        public void OnDashButtonPressed(Vector2 inputDirection)
        {
            if (isDashing || playerTransform == null) return;
            isDashing = true;
            dashTimer = dashDuration;

            Vector3 dashDir = inputDirection.sqrMagnitude > 0.01f
                ? new Vector3(inputDirection.x, 0f, inputDirection.y).normalized
                : playerTransform.forward;

            playerVelocity.x = dashDir.x * dashSpeed;
            playerVelocity.z = dashDir.z * dashSpeed;

            if (HumanoidAnimationController.Instance != null)
            {
                HumanoidAnimationController.Instance.PlayDashAnimation();
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 0.7f);
            }

            Debug.Log("[Dash Button] Executed Evasive Dash with Invulnerability Frames!");
        }

        public void OnJumpButtonPressed()
        {
            if (playerController == null) return;
            if (playerController.isGrounded)
            {
                playerVelocity.y = jumpForce;

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.5f);
                }

                Debug.Log("[Jump Button] Executed Vertical Jump for Aerial Slashes!");
            }
        }

        /// <summary>
        /// Step 6: Mount toggle for fast traversal across the circular town's wide streets.
        /// </summary>
        public void OnToggleMountButtonPressed()
        {
            isMounted = !isMounted;
            if (activeMountVisualPrefab != null) activeMountVisualPrefab.SetActive(isMounted);
            followMoveSpeed = isMounted ? mountedMoveSpeed : baseWalkSpeed;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.9f);
            }

            OnMountToggled?.Invoke(isMounted);
            Debug.Log($"[Mount System] Mount {(isMounted ? "Summoned (Speed: " + mountedMoveSpeed + " m/s)" : "Dismissed (Speed: " + baseWalkSpeed + " m/s)")}. Wide avenues clear!");
        }

        public void OnToggleAiAutopilotButtonPressed()
        {
            isAiAutopilotEnabled = !isAiAutopilotEnabled;

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    $"🤖 [AI AUTOPILOT] Autonomous System Takeover: {(isAiAutopilotEnabled ? "ENGAGED" : "MANUAL CONTROL")}", false);
            }

            OnAiAutopilotToggled?.Invoke(isAiAutopilotEnabled);
            Debug.Log($"[AI Auto Button] Autonomous AI Takeover: {(isAiAutopilotEnabled ? "ENGAGED" : "MANUAL CONTROL")}");
        }

        private void UpdatePlayerVerticalAndDashPhysics()
        {
            if (playerController == null || !playerController.enabled) return;

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                {
                    isDashing = false;
                    playerVelocity.x = 0f;
                    playerVelocity.z = 0f;
                }
            }

            if (playerController.isGrounded && playerVelocity.y < 0f)
            {
                playerVelocity.y = -2f;
            }

            playerVelocity.y += gravity * Time.deltaTime;
            playerController.Move(playerVelocity * Time.deltaTime);
        }

        // =========================================================================
        // TEST BOSS, MINING MATERIALS, RETURN TO TOWN, CRAFTING & BAZAAR HOOKS
        // =========================================================================

        /// <summary>
        /// Step 5A: Simulates defeating Test Dungeon Boss (Kaelen, The Unforged),
        /// awarding boss crafting materials, 3.0x Strike Relic, and Level-Up Clutch Surge (+45% HP, +60% MP).
        /// </summary>
        public void OnTestBossDefeated()
        {
            hasDefeatedTestBoss = true;
            hasAcquiredBossRelic = true;
            harvestedBossMeatCount += 3;
            minedIronOreCount += 5;
            playerManaCores += 350;

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast(
                    "👑 [TEST BOSS SUBJUGATED] Kaelen, The Unforged Defeated! Level-Up Clutch Surge Restores 45% HP & 60% MP!", true);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            if (HumanoidAnimationController.Instance != null)
            {
                HumanoidAnimationController.Instance.PlayLevelUpAnimation();
                HumanoidAnimationController.Instance.PlayVictoryAnimation();
            }

            OnTestBossDefeatedEvent?.Invoke();
            Debug.Log("[Test Boss Cleared!] Defeated Kaelen, The Unforged! Looted: 3x Boss Cutlets, 5x Star-Iron Ore, 350 Mana Cores & 3.0x Strike Relic!");
        }

        /// <summary>
        /// Step 5B: Called when the player chisels a CaveMiningNode inside the Test Dungeon.
        /// </summary>
        public void OnTestMiningNodeExtracted(int oresGained, int gemsGained)
        {
            minedIronOreCount += oresGained;
            minedPrismaticGemsCount += gemsGained;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.9f);
            }

            Debug.Log($"[Mining Test] Extracted +{oresGained} Iron Ore and +{gemsGained} Prismatic Gems! (Totals: {minedIronOreCount} Ore, {minedPrismaticGemsCount} Gems)");
        }

        /// <summary>
        /// Step 6: Exits the Test Dungeon, resets Dimensional Instability, and returns the squad to Town.
        /// </summary>
        public void ReturnFromTestDungeonToTown()
        {
            if (DungeonInstabilityManager.Instance != null)
            {
                DungeonInstabilityManager.Instance.ExitDungeonDimension();
            }

            TeleportPlayerTo(exteriorTestDungeonPortalPoint);
            Debug.Log("[Vertical Slice] Returned to Town Courtyard from Test Dungeon. Dimensional Instability reset to zero. Ready to mount up!");
        }

        /// <summary>
        /// Step 8: Verifies Bazaar Buying & Selling respecting NPC Bank Caps and System-Fixed Player Pricing.
        /// </summary>
        public bool TrySellItemAtBazaar(string itemName, int priceInManaCores, bool sellingToNpcMerchant)
        {
            if (sellingToNpcMerchant)
            {
                if (npcBazaarMerchantBankBalance < priceInManaCores)
                {
                    Debug.LogWarning($"[Bazaar Test] Trade Denied! NPC Merchant only has {npcBazaarMerchantBankBalance} Mana Cores in their bank account (Price: {priceInManaCores}).");
                    return false;
                }
                npcBazaarMerchantBankBalance -= priceInManaCores;
                playerManaCores += priceInManaCores;

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.8f);
                }

                Debug.Log($"[Bazaar Test] Sold '{itemName}' to NPC Merchant for {priceInManaCores} Mana Cores! (Merchant Remaining Bank: {npcBazaarMerchantBankBalance})");
                return true;
            }
            else
            {
                int burnFee = Mathf.RoundToInt(priceInManaCores * 0.05f); // 5% deflationary burn fee
                int netPayout = priceInManaCores - burnFee;
                playerManaCores += netPayout;

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
                }

                Debug.Log($"[Bazaar Test] Sold '{itemName}' on Player Bazaar for fixed price {priceInManaCores} Cores (-{burnFee} 5% tax = +{netPayout} net Cores)!");
                return true;
            }
        }

        public bool TryBuyItemAtBazaar(string itemName, int costInManaCores)
        {
            if (playerManaCores < costInManaCores)
            {
                Debug.LogWarning($"[Bazaar Test] Not enough Mana Cores to buy '{itemName}' ({playerManaCores}/{costInManaCores}).");
                return false;
            }
            playerManaCores -= costInManaCores;
            npcBazaarMerchantBankBalance += costInManaCores;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.8f);
            }

            Debug.Log($"[Bazaar Test] Purchased '{itemName}' at Bazaar for {costInManaCores} Mana Cores!");
            return true;
        }

        // =========================================================================
        // DYNAMIC PARTY PORTRAITS & STAGGERED NON-CLIPPING OVERWORLD FOLLOW
        // =========================================================================

        public void AddAllyToParty(PartyMemberRuntimeData newAlly)
        {
            if (newAlly == null || activePartyMembers.Count >= 9) return;
            if (activePartyMembers.Exists(a => a.allyId == newAlly.allyId)) return;

            activePartyMembers.Add(newAlly);
            RecalculateStaggeredFormationOffsets();
            SpawnAllyPortraitInRightColumn(newAlly);

            if (newAlly.overworldInstance == null)
            {
                SpawnOverworldAllyModel(newAlly);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.8f);
            }

            Debug.Log($"[Party System] Added '{newAlly.allyName}' to Party ({activePartyMembers.Count}/9). Slanted -5.5deg portrait mounted & overworld follow active.");
        }

        public void RemoveAllyFromParty(string allyId)
        {
            var ally = activePartyMembers.Find(a => a.allyId == allyId);
            if (ally == null) return;

            activePartyMembers.Remove(ally);
            if (spawnedPortraitUIObjects.TryGetValue(allyId, out GameObject uiNode))
            {
                if (Application.isPlaying) Destroy(uiNode);
                else DestroyImmediate(uiNode);
                spawnedPortraitUIObjects.Remove(allyId);
            }

            if (ally.overworldInstance != null)
            {
                if (Application.isPlaying) Destroy(ally.overworldInstance.gameObject);
                else DestroyImmediate(ally.overworldInstance.gameObject);
                ally.overworldInstance = null;
            }

            RecalculateStaggeredFormationOffsets();
            Debug.Log($"[Party System] Removed '{ally.allyName}' from Party ({activePartyMembers.Count}/9). Portrait removed.");
        }

        private void SpawnOverworldAllyModel(PartyMemberRuntimeData ally)
        {
            Vector3 spawnPos = playerTransform != null
                ? playerTransform.TransformPoint(ally.assignedFormationOffset)
                : transform.position;

            GameObject allyObj = null;
            if (allyOverworldPrefab != null)
            {
                allyObj = Instantiate(allyOverworldPrefab, spawnPos, playerTransform != null ? playerTransform.rotation : Quaternion.identity);
            }
            else
            {
#if UNITY_EDITOR
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RPG Tiny Hero Duo/Prefab/FemaleCharacterPBR.prefab");
                if (prefab != null)
                {
                    allyObj = Instantiate(prefab, spawnPos, playerTransform != null ? playerTransform.rotation : Quaternion.identity);
                }
#endif
                if (allyObj == null)
                {
                    allyObj = new GameObject($"Ally_{ally.allyName}");
                    allyObj.transform.position = spawnPos;
                }
            }

            allyObj.name = $"Ally_{ally.allyId}_{ally.allyName}";

            // Ensure CharacterController for smooth overworld movement and collision avoidance
            if (!allyObj.TryGetComponent<CharacterController>(out var cc))
            {
                cc = allyObj.AddComponent<CharacterController>();
                cc.height = 1.6f;
                cc.center = new Vector3(0f, 0.8f, 0f);
                cc.radius = 0.4f;
            }

            // Ensure Animator is running PlayerHeroAnimator with seamless blend trees
            var anim = allyObj.GetComponent<Animator>() ?? allyObj.GetComponentInChildren<Animator>();
            if (anim != null)
            {
#if UNITY_EDITOR
                var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animators/PlayerHeroAnimator.controller");
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
#endif
            }

            ally.overworldInstance = allyObj.transform;
        }

        private void SpawnAllyPortraitInRightColumn(PartyMemberRuntimeData ally)
        {
            if (rightEdgePartyColumnContainer == null) return;

            GameObject uiInstance;
            if (allyPersonaPortraitPrefab != null)
            {
                uiInstance = Instantiate(allyPersonaPortraitPrefab, rightEdgePartyColumnContainer);
            }
            else
            {
                uiInstance = new GameObject($"PartyPortrait_{ally.allyId}", typeof(RectTransform), typeof(Image));
                uiInstance.transform.SetParent(rightEdgePartyColumnContainer, false);
                var rt = uiInstance.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(90f, 90f);
            }

            uiInstance.name = $"PartyPortrait_{ally.allyId}";

            Image portraitImg = uiInstance.GetComponentInChildren<Image>();
            if (portraitImg != null)
            {
                if (ally.personaSideProfileSprite != null) portraitImg.sprite = ally.personaSideProfileSprite;
                portraitImg.preserveAspect = true;
                portraitImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, -5.5f); // Slanted -5.5 deg Persona cut-in style
            }

            spawnedPortraitUIObjects[ally.allyId] = uiInstance;
        }

        private void RecalculateStaggeredFormationOffsets()
        {
            for (int i = 0; i < activePartyMembers.Count; i++)
            {
                int row = (i / 2) + 1;
                int side = (i % 2 == 0) ? -1 : 1;
                float asymmetricJitter = ((i % 3) - 1) * 0.35f;

                float offsetX = side * (row * 0.55f + 0.65f) * lateralSpreadWidth;
                float offsetZ = -(row * rowSpacingBack) + asymmetricJitter;

                activePartyMembers[i].assignedFormationOffset = new Vector3(offsetX, 0f, offsetZ);
            }
        }

        private void UpdatePartyOverworldStaggeredFollowAndAntiClipping()
        {
            if (playerTransform == null || activePartyMembers.Count == 0) return;

            for (int i = 0; i < activePartyMembers.Count; i++)
            {
                Transform allyTf = activePartyMembers[i].overworldInstance;
                if (allyTf == null) continue;

                Vector3 desiredSlotWorldPos = playerTransform.TransformPoint(activePartyMembers[i].assignedFormationOffset);
                desiredSlotWorldPos.y = allyTf.position.y;

                Vector3 moveVector = Vector3.zero;
                Vector3 toSlot = desiredSlotWorldPos - allyTf.position;
                if (toSlot.magnitude > 0.35f)
                {
                    moveVector += toSlot.normalized * followMoveSpeed;
                }

                Vector3 separationForce = ComputePersonalSpaceRepulsion(allyTf, i);
                Vector3 finalVelocity = moveVector + separationForce;
                finalVelocity.y = 0f;

                float speedNorm = 0f;
                if (finalVelocity.sqrMagnitude > 0.02f)
                {
                    speedNorm = Mathf.Clamp(finalVelocity.magnitude / Mathf.Max(0.1f, followMoveSpeed), 0f, 1.25f);
                    if (allyTf.TryGetComponent<CharacterController>(out var cc) && cc.enabled)
                    {
                        cc.SimpleMove(finalVelocity);
                    }
                    else
                    {
                        allyTf.position += finalVelocity * Time.deltaTime;
                    }

                    Quaternion targetRot = Quaternion.LookRotation(finalVelocity.normalized);
                    allyTf.rotation = Quaternion.Slerp(allyTf.rotation, targetRot, Time.deltaTime * 10f);
                }

                // Update Animator with smooth blend dampening so walk/run fits seamlessly into idle
                var allyAnim = allyTf.GetComponent<Animator>() ?? allyTf.GetComponentInChildren<Animator>();
                if (allyAnim != null && allyAnim.isInitialized && allyAnim.runtimeAnimatorController != null)
                {
                    allyAnim.SetFloat("Speed", speedNorm, 0.15f, Time.deltaTime);
                    allyAnim.SetBool("IsGrounded", true);
                }
            }
        }

        private Vector3 ComputePersonalSpaceRepulsion(Transform currentAlly, int currentIndex)
        {
            Vector3 repulsion = Vector3.zero;

            Vector3 awayFromPlayer = currentAlly.position - playerTransform.position;
            awayFromPlayer.y = 0f;
            float distToPlayer = awayFromPlayer.magnitude;
            if (distToPlayer < minSeparationRadius && distToPlayer > 0.001f)
            {
                repulsion += (awayFromPlayer / distToPlayer) * ((minSeparationRadius - distToPlayer) * separationPushStrength);
            }

            for (int j = 0; j < activePartyMembers.Count; j++)
            {
                if (j == currentIndex) continue;
                Transform otherAlly = activePartyMembers[j].overworldInstance;
                if (otherAlly == null) continue;

                Vector3 diff = currentAlly.position - otherAlly.position;
                diff.y = 0f;
                float dist = diff.magnitude;

                if (dist < minSeparationRadius && dist > 0.001f)
                {
                    float overlapRatio = (minSeparationRadius - dist) / minSeparationRadius;
                    repulsion += (diff / dist) * (overlapRatio * separationPushStrength);
                }
            }

            return repulsion;
        }
    }

    /// <summary>
    /// DemoVerticalSliceBootstrapper coordinates the first 15-minute playable demo loop:
    /// Step 1: Tier 4 Central Cathedral Spawn, Guardian Contract signing, pew jump, rope slice & door open.
    /// Step 2: 3 Courtyard Practice Dummies, Progressive HUD unlocks (Vitals, Ally Column), Margo Party Join & V-formation follow.
    /// Step 3: Town Loop with Mount Whistle, 4 Crafting Stations (Forge, Cooking, Alchemy, Anvil), and Tier 1 Bazaar.
    /// Step 4: Test Dungeon, 3 Geode Mining Nodes, Destructible Pillars, Mana Larvae, Boss Kaelen with Whetstone sharpening & Level-Up Clutch Surge.
    /// </summary>
    public class DemoVerticalSliceBootstrapper : MonoBehaviour
    {
        public static DemoVerticalSliceBootstrapper Instance { get; private set; }

        [Header("Step 1: Cathedral Spawn & Altar Contract")]
        public Transform playerHero;
        public Vector3 cathedralSpawnPos = new Vector3(0f, 80.1f, 16f);
        public Quaternion cathedralSpawnRot = Quaternion.Euler(0f, 0f, 0f);
        public GameObject floatingContractParchment;
        public GameObject fallenChurchPewObstacle;
        public GameObject cathedralDoubleDoors;
        public CathedralDoorRopeBarricade doorRopeBarricade;
        public bool isContractSigned = false;

        [Header("Step 2: Courtyard Practice Dummies & Party")]
        public DemoPracticeDummy dummy1_VelocityCutting;
        public DemoClockworkStriker dummy2_ClockworkStriker;
        public DemoBramblesRuneArchway dummy3_BramblesArchway;
        public Transform margoCourtyardNpc;
        public bool hasUnlockedHUD1_Vitals = false;
        public bool hasUnlockedHUD2_AllyColumn = false;
        public bool isMargoInParty = false;

        [Header("Step 3: Town Traversal & Crafting")]
        public GameObject mountWhistleStableNode;
        public GameObject blacksmithForgeStation;
        public GameObject cookingMamaStation;
        public GameObject alchemyTomeDeskStation;
        public GameObject enchantingAnvilStation;
        public GameObject tier1BazaarMerchantStall;
        public Transform catherineGuildHallNpc;

        [Header("Step 4: Test Dungeon & Boss Kaelen")]
        public GameObject testDungeonPortalEntrance;
        public CaveMiningNode[] subterraneanGeodeNodes;
        public BattleSummoner.DesignPhilosophy.ArenaDestructiblePillar[] arenaDestructiblePillars;
        public BattleSummoner.DesignPhilosophy.PreBossLarvaeCluster preBossManaLarvae;
        public DemoBossKaelen testBossKaelen;
        public bool hasUnlockedHUD3_Whetstone = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeStep1CathedralSpawn();
        }

        public void InitializeStep1CathedralSpawn()
        {
            if (playerHero == null)
            {
                var p = GameObject.Find("Player_Hero");
                if (p != null) playerHero = p.transform;
            }

            if (playerHero != null)
            {
                var cc = playerHero.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerHero.position = cathedralSpawnPos;
                playerHero.rotation = cathedralSpawnRot;
                if (cc != null) cc.enabled = true;
            }

            if (BattleViewportRebalancer.Instance != null)
            {
                BattleViewportRebalancer.Instance.HideInitialHUDForTutorial();
            }

            var gesture = FindAnyObjectByType<PortraitGestureCanvas>();
            if (gesture != null)
            {
                gesture.currentMode = InteractionMode.GuardianSignature;
            }

            if (floatingContractParchment != null)
            {
                floatingContractParchment.SetActive(true);
            }
        }

        public void OnGuardianContractSigned(string signerName)
        {
            if (isContractSigned) return;
            isContractSigned = true;

            if (floatingContractParchment != null)
            {
                floatingContractParchment.SetActive(false);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            var gesture = FindAnyObjectByType<PortraitGestureCanvas>();
            if (gesture != null)
            {
                gesture.currentMode = InteractionMode.CombatSlash;
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast($"📜 [GUARDIAN CONTRACT SIGNED] Welcome, {signerName}! Approach the aisle exit.", false);
            }

            Debug.Log($"[DemoVerticalSlice] Contract signed by {signerName}. Movement unlocked!");
        }

        public void OnCathedralDoorsOpened()
        {
            if (cathedralDoubleDoors != null)
            {
                StartCoroutine(SwingDoorsOpenRoutine(cathedralDoubleDoors.transform));
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.7f);
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("⛪ [CATHEDRAL DOORS OPEN] Sunlight floods the courtyard. Test dummies await!", false);
            }
        }

        private IEnumerator SwingDoorsOpenRoutine(Transform doors)
        {
            float elapsed = 0f;
            Quaternion startRot = doors.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(0f, 85f, 0f);
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                doors.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / 1.5f);
                yield return null;
            }
        }

        public void OnDummy1Sliced()
        {
            if (hasUnlockedHUD1_Vitals) return;
            hasUnlockedHUD1_Vitals = true;

            if (BattleViewportRebalancer.Instance != null)
            {
                BattleViewportRebalancer.Instance.UnlockPlayerVitalsHUD();
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 1.0f);
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.8f);
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("⚔️ [HUD UNLOCKED] Persona Vitals, EXP, HP/MP & Item Belt unlocked!", true);
            }
        }

        public void OnDummy2Parried()
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 1.0f);
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("🛡️ [PERFECT PARRY] Clockwork arm shattered! Two-finger parry mastered!", false);
            }
        }

        public void OnDummy3BramblesBurned()
        {
            if (isMargoInParty) return;
            isMargoInParty = true;
            hasUnlockedHUD2_AllyColumn = true;

            if (BattleViewportRebalancer.Instance != null)
            {
                BattleViewportRebalancer.Instance.UnlockAllyColumnHUD();
            }

            if (CoreVerticalSliceSandboxController.Instance != null)
            {
                Sprite margoSprite = null;
                var tex = Resources.Load<Texture2D>("Portrait_Margo");
                if (tex != null)
                {
                    margoSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }

                var margoData = new PartyMemberRuntimeData
                {
                    allyId = "npc_margo",
                    allyName = "Margo",
                    personaSideProfileSprite = margoSprite,
                    currentHp = 100f,
                    maxHp = 100f,
                    currentMp = 75f,
                    maxMp = 75f,
                    overworldInstance = margoCourtyardNpc
                };

                CoreVerticalSliceSandboxController.Instance.AddAllyToParty(margoData);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("✨ [PARTY RECRUIT] Margo joined the party! Right-Edge Ally Column unlocked.", true);
            }
        }

        public void OnKaelenAnvilBladeStruck()
        {
            if (hasUnlockedHUD3_Whetstone) return;
            hasUnlockedHUD3_Whetstone = true;

            if (BattleViewportRebalancer.Instance != null)
            {
                BattleViewportRebalancer.Instance.UnlockWhetstoneHUD();
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 1.0f);
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.8f);
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("💎 [WHETSTONE UNLOCKED] Blade edge chipped against anvil! Swipe Top-Right Whetstone to hone!", true);
            }
        }
    }

    public class CathedralDoorRopeBarricade : MonoBehaviour, ISliceable
    {
        public bool isSliced = false;

        public void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage)
        {
            if (isSliced) return;
            isSliced = true;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_whoosh", 1.0f);
            }

            gameObject.SetActive(false);
            DemoVerticalSliceBootstrapper.Instance?.OnCathedralDoorsOpened();
        }
    }

    public class DemoPracticeDummy : MonoBehaviour, ISliceable
    {
        public int dummyIndex = 1;
        public float sliceVelocityThreshold = 650f;
        public Renderer dummyRenderer;

        public void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage)
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 1.0f);
            }
            if (dummyRenderer != null) dummyRenderer.material.color = Color.green;

            DemoVerticalSliceBootstrapper.Instance?.OnDummy1Sliced();
        }

        private void OnMouseDown()
        {
            if (CoreVerticalSliceSandboxController.Instance != null)
            {
                CoreVerticalSliceSandboxController.Instance.OnZTargetButtonPressed();
            }
        }
    }

    public class DemoClockworkStriker : MonoBehaviour
    {
        public bool isArmShattered = false;
        public GameObject clockworkArm;

        private void OnEnable()
        {
            if (FruitNinjaBladeCutter.Instance != null)
            {
                FruitNinjaBladeCutter.Instance.OnTwoFingerParryExecuted += HandleParry;
            }
        }

        private void OnDisable()
        {
            if (FruitNinjaBladeCutter.Instance != null)
            {
                FruitNinjaBladeCutter.Instance.OnTwoFingerParryExecuted -= HandleParry;
            }
        }

        private void HandleParry()
        {
            if (isArmShattered) return;
            isArmShattered = true;
            if (clockworkArm != null) clockworkArm.SetActive(false);

            DemoVerticalSliceBootstrapper.Instance?.OnDummy2Parried();
        }
    }

    public class DemoBramblesRuneArchway : MonoBehaviour
    {
        public bool isBurned = false;
        public GameObject bramblesVisual;
        public GameObject stoneMuralVisual;

        public void BurnBrambles()
        {
            if (isBurned) return;
            isBurned = true;
            if (bramblesVisual != null) bramblesVisual.SetActive(false);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.9f);
            }

            DemoVerticalSliceBootstrapper.Instance?.OnDummy3BramblesBurned();
        }
    }

    public class DemoBossKaelen : MonoBehaviour, ISliceable
    {
        public bool isAwakened = false;
        public GameObject impaledSwordVisual;
        public float bossHp = 500f;
        public bool isQuenched = false;

        public void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage)
        {
            if (!isAwakened)
            {
                if (sliceDirection.y < -0.3f || slicePoint.y < transform.position.y + 1.2f)
                {
                    AwakenBoss();
                }
                return;
            }

            DemoVerticalSliceBootstrapper.Instance?.OnKaelenAnvilBladeStruck();

            bossHp -= finalDamage;
            if (bossHp <= 0f)
            {
                DefeatBoss();
            }
        }

        public void AwakenBoss()
        {
            isAwakened = true;
            if (impaledSwordVisual != null) impaledSwordVisual.SetActive(false);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("apex_celestial_roar", 1.0f);
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("🔥 [TEST BOSS AWAKENED] Kaelen, The Unforged rises! Sever his weak point!", true);
            }
        }

        public void QuenchArmor()
        {
            isQuenched = true;
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }
            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("❄️ [ARMOR QUENCHED] Kaelen's molten armor cooled! Weak point exposed!", false);
            }
        }

        public void DefeatBoss()
        {
            if (CoreVerticalSliceSandboxController.Instance != null)
            {
                CoreVerticalSliceSandboxController.Instance.OnTestBossDefeated();
            }
            gameObject.SetActive(false);
        }
    }
}
