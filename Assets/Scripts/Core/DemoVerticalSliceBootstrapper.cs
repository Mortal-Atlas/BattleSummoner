using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.DesignPhilosophy;

namespace BattleSummoner.Core
{
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

            EnsureOfflineContinuitySystems();
        }

        private void EnsureOfflineContinuitySystems()
        {
            if (OfflineFirstContinuityManager.Instance == null)
            {
                var go = new GameObject("OfflineFirstContinuityManager", typeof(OfflineFirstContinuityManager));
            }
            if (BattleSummoner.Social.AsynchronousClimberInfrastructureManager.Instance == null)
            {
                var go = new GameObject("AsynchronousClimberInfrastructureManager", typeof(BattleSummoner.Social.AsynchronousClimberInfrastructureManager));
            }
            if (OfflineAutopilotEngine.Instance == null)
            {
                var go = new GameObject("OfflineAutopilotEngine", typeof(OfflineAutopilotEngine));
            }
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
        public float sliceVelocityThreshold = 550f;
        public Renderer dummyRenderer;

        public void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage)
        {
            if (finalDamage < 45f)
            {
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("whoosh", 0.7f);
                }
                if (SystemBroadcastManager.Instance != null)
                {
                    SystemBroadcastManager.Instance.DispatchBroadcast("🛡️ [DULL THUD] Slow swipe bounced off! Fast swipe (velocity > threshold) required to slice!", false);
                }
                return;
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("blade_hit", 1.0f);
            }
            if (dummyRenderer != null) dummyRenderer.material.color = Color.green;

            DemoVerticalSliceBootstrapper.Instance?.OnDummy1Sliced();
        }

        private void OnMouseDown()
        {
            if (QuickZTargetingController.Instance != null)
            {
                QuickZTargetingController.Instance.LockTarget(transform);
            }
            else if (CoreVerticalSliceSandboxController.Instance != null)
            {
                CoreVerticalSliceSandboxController.Instance.OnZTargetButtonPressed();
            }
        }
    }

    public class DemoClockworkStriker : MonoBehaviour
    {
        public bool isArmShattered = false;
        public GameObject clockworkArm;
        public Renderer armRenderer;
        private Coroutine telegraphRoutine;

        private void Start()
        {
            if (armRenderer == null && clockworkArm != null)
            {
                armRenderer = clockworkArm.GetComponent<Renderer>();
            }
            telegraphRoutine = StartCoroutine(TelegraphLoopRoutine());
        }

        private IEnumerator TelegraphLoopRoutine()
        {
            while (!isArmShattered)
            {
                yield return new WaitForSeconds(3.0f);
                if (isArmShattered) break;

                // 1. Wide horizontal sweep telegraph (flick-dash warning)
                if (clockworkArm != null)
                {
                    clockworkArm.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                }
                if (armRenderer != null) armRenderer.material.color = Color.yellow;
                yield return new WaitForSeconds(1.0f);

                if (isArmShattered) break;

                // 2. Red-glint overhead slam telegraph (two-finger tap parry window)
                if (clockworkArm != null)
                {
                    clockworkArm.transform.localRotation = Quaternion.Euler(60f, 0f, 0f);
                }
                if (armRenderer != null) armRenderer.material.color = Color.red;
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.7f);
                }

                yield return new WaitForSeconds(1.2f);

                // Reset
                if (clockworkArm != null)
                {
                    clockworkArm.transform.localRotation = Quaternion.identity;
                }
                if (armRenderer != null) armRenderer.material.color = new Color(0.5f, 0.5f, 0.6f);
            }
        }

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
            if (telegraphRoutine != null)
            {
                StopCoroutine(telegraphRoutine);
            }
        }

        public void HandleParry()
        {
            if (isArmShattered) return;
            isArmShattered = true;
            if (clockworkArm != null) clockworkArm.SetActive(false);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("anvil_shatter", 1.0f);
            }

            DemoVerticalSliceBootstrapper.Instance?.OnDummy2Parried();
        }

        private void Update()
        {
            // Fallback PC input for parry test: Spacebar or F or Right Click
            if (!isArmShattered && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F)))
            {
                HandleParry();
            }
        }
    }

    public class DemoBramblesRuneArchway : MonoBehaviour, ISliceable
    {
        public bool isBurned = false;
        public GameObject bramblesVisual;
        public GameObject stoneMuralVisual;

        private void OnEnable()
        {
            if (RuneShapeCastingEngine.Instance != null)
            {
                RuneShapeCastingEngine.Instance.OnSpellCastDispatched += HandleSpellDispatched;
            }
        }

        private void OnDisable()
        {
            if (RuneShapeCastingEngine.Instance != null)
            {
                RuneShapeCastingEngine.Instance.OnSpellCastDispatched -= HandleSpellDispatched;
            }
        }

        private void HandleSpellDispatched(SpellElement element, Vector2 direction)
        {
            if (element == SpellElement.Fire && !isBurned)
            {
                BurnBrambles();
            }
        }

        public void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage)
        {
            BurnBrambles();
        }

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

        private void OnEnable()
        {
            if (RuneShapeCastingEngine.Instance != null)
            {
                RuneShapeCastingEngine.Instance.OnSpellCastDispatched += HandleSpellDispatched;
            }
        }

        private void OnDisable()
        {
            if (RuneShapeCastingEngine.Instance != null)
            {
                RuneShapeCastingEngine.Instance.OnSpellCastDispatched -= HandleSpellDispatched;
            }
        }

        private void HandleSpellDispatched(SpellElement element, Vector2 direction)
        {
            if (element == SpellElement.Water && isAwakened && !isQuenched)
            {
                QuenchArmor();
            }
        }

        public void OnSlice(Vector3 slicePoint, Vector3 sliceDirection, float finalDamage)
        {
            if (!isAwakened)
            {
                AwakenBoss();
                return;
            }

            DemoVerticalSliceBootstrapper.Instance?.OnKaelenAnvilBladeStruck();

            float damage = isQuenched ? finalDamage * 2.5f : finalDamage;
            bossHp -= damage;

            if (bossHp <= 0f)
            {
                DefeatBoss();
            }
        }

        private void OnMouseDown()
        {
            if (!isAwakened)
            {
                AwakenBoss();
            }
        }

        public void AwakenBoss()
        {
            if (isAwakened) return;
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
            var progression = FindAnyObjectByType<PlayerProgression>();
            if (progression != null)
            {
                progression.AddExperience(progression.GetRequiredExp(progression.currentLevel) + 50.0, 1);
            }

            if (CoreVerticalSliceSandboxController.Instance != null)
            {
                CoreVerticalSliceSandboxController.Instance.OnTestBossDefeated();
            }
            gameObject.SetActive(false);
        }
    }

    public class DemoMountStableNode : MonoBehaviour
    {
        private void OnMouseDown() => Interact();
        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player") || other.name.Contains("Player")) Interact(); }
        public void Interact()
        {
            if (CoreVerticalSliceSandboxController.Instance != null)
            {
                CoreVerticalSliceSandboxController.Instance.OnToggleMountButtonPressed();
            }
            else if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 0.9f);
            }
            SystemBroadcastManager.Instance?.DispatchBroadcast("🐎 [MOUNT WHISTLE] Steed summoned! Ride rapidly down the wide avenues.", false);
        }
    }

    public class DemoBlacksmithForgeNode : MonoBehaviour
    {
        private void OnMouseDown() => Interact();
        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player") || other.name.Contains("Player")) Interact(); }
        public void Interact()
        {
            BattleSummonerAudioMaster.Instance?.PlayPresetSFX("anvil_shatter", 0.85f);
            SystemBroadcastManager.Instance?.DispatchBroadcast("⚒️ [BLACKSMITH FORGE] Rhythmic hammer strikes on heated Damascus steel! Edge temper enhanced.", false);
        }
    }

    public class DemoCookingStationNode : MonoBehaviour
    {
        private void OnMouseDown() => Interact();
        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player") || other.name.Contains("Player")) Interact(); }
        public void Interact()
        {
            BattleSummonerAudioMaster.Instance?.PlayPresetSFX("crystal_resonate", 0.85f);
            SystemBroadcastManager.Instance?.DispatchBroadcast("🍳 [COOKING MAMA PREP] Slicing ingredients! Food Trivia Buff: +15% HP & MP recovery.", false);
        }
    }

    public class DemoAlchemyDeskNode : MonoBehaviour
    {
        private void OnMouseDown() => Interact();
        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player") || other.name.Contains("Player")) Interact(); }
        public void Interact()
        {
            BattleSummonerAudioMaster.Instance?.PlayPresetSFX("relic_chime", 0.9f);
            SystemBroadcastManager.Instance?.DispatchBroadcast("🧪 [ALCHEMY & TOME DESK] Potion brewing and second-hand spell tome assembly active.", false);
        }
    }

    public class DemoEnchantingAnvilNode : MonoBehaviour
    {
        private void OnMouseDown() => Interact();
        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player") || other.name.Contains("Player")) Interact(); }
        public void Interact()
        {
            BattleSummonerAudioMaster.Instance?.PlayPresetSFX("anvil_shatter", 0.95f);
            SystemBroadcastManager.Instance?.DispatchBroadcast("⚡ [3-STRIKE ENCHANTING ANVIL] High-risk gear tempering! Strike twice for temper; third strike risks fracture debt!", false);
        }
    }

    public class DemoBazaarStallNode : MonoBehaviour
    {
        private void OnMouseDown() => Interact();
        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player") || other.name.Contains("Player")) Interact(); }
        public void Interact()
        {
            if (CoreVerticalSliceSandboxController.Instance != null)
            {
                CoreVerticalSliceSandboxController.Instance.TrySellItemAtBazaar("Iron Ore", 50, true);
            }
            else
            {
                BattleSummonerAudioMaster.Instance?.PlayPresetSFX("relic_chime", 0.85f);
                SystemBroadcastManager.Instance?.DispatchBroadcast("🪙 [TIER 1 BAZAAR] Merchant Stall: Buying & Selling active. 5% Market Tax & NPC Bank Cap enforced.", false);
            }
        }
    }

    public class DemoCatherineGuildHallNode : MonoBehaviour
    {
        private void OnMouseDown() => Interact();
        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player") || other.name.Contains("Player")) Interact(); }
        public void Interact()
        {
            BattleSummonerAudioMaster.Instance?.PlayPresetSFX("menu_open", 1.0f);
            SystemBroadcastManager.Instance?.DispatchBroadcast("🛡️ [GUILD HALL CLERK] Catherine: 'Welcome, delver. Register your party contracts with the guild.'", false);
        }
    }
}
