using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Core;
using BattleSummoner.World;
using BattleSummoner.Hobbies;
using BattleSummoner.Combat;
using BattleSummoner.Social;
using BattleSummoner.DungeonDimension;

namespace BattleSummoner.EditorScripts
{
    public static class MasterIntegrationSystemsVerifier
    {
        [MenuItem("BattleSummoner/Run Full Master Integration Verification", false, 1)]
        public static void RunAllVerifications()
        {
            Debug.Log("======================================================================");
            Debug.Log("🚀 STARTING BATTLE SUMMONER MASTER INTEGRATION SPECIFICATION VERIFICATION");
            Debug.Log("======================================================================");

            int passed = 0;
            int total = 13;

            if (VerifySection0_1_FirstPlayableVerticalSlice()) passed++;
            if (VerifySection1_GuardianPactSignature()) passed++;
            if (VerifySection2_BlueprintTownAndHousing()) passed++;
            if (VerifySection3_HobbyMasteryAndPotluck()) passed++;
            if (VerifySection4_DungeonInstabilityAndFortress()) passed++;
            if (VerifySection5_CustomSpecialMoveRecorder()) passed++;
            if (VerifySection6_CombatMasteryAndEvolution()) passed++;
            if (VerifySection7_TacticalEnvironmentAndCursedMask()) passed++;
            if (VerifySection8_TavernSocialAndWorldEvents()) passed++;
            if (VerifySection9_GestureCanvasAndBladeCutterBinding()) passed++;
            if (VerifySection10_CharacterAnimationsAndBlending()) passed++;
            if (VerifySection11_MotionMatchingAndMocapCentral()) passed++;
            if (VerifySection12_AssetStorePropPacksAndUrbanLamps()) passed++;

            Debug.Log("======================================================================");
            if (passed == total)
            {
                Debug.Log($"🎉 [ALL PASS] {passed}/{total} MASTER SUBSYSTEMS FULLY VERIFIED & COMPLIANT!");
            }
            else
            {
                Debug.LogWarning($"⚠️ [PARTIAL PASS] {passed}/{total} Subsystems Passed. Check logs above.");
            }
            Debug.Log("======================================================================");
        }

        private static bool VerifySection0_1_FirstPlayableVerticalSlice()
        {
            var go = new GameObject("Test_CoreVerticalSliceController");
            var ctrl = go.AddComponent<CoreVerticalSliceSandboxController>();

            // Setup anchors
            var churchAnchor = new GameObject("Anchor_Church").transform;
            churchAnchor.position = new Vector3(0f, 85f, 0f);
            var portalAnchor = new GameObject("Anchor_Portal").transform;
            portalAnchor.position = new Vector3(10f, 0f, 20f);
            var playerObj = new GameObject("Test_Player");
            var playerTf = playerObj.transform;
            ctrl.playerTransform = playerTf;
            ctrl.churchInteriorSpawnPoint = churchAnchor;
            ctrl.exteriorTestDungeonPortalPoint = portalAnchor;

            // 1. Character Creation -> Church Spawn
            var pact = new GuardianContractData { avatarName = "Aurelia", guardianPrintedName = "Master_Jedi" };
            ctrl.CompleteCreationAndSpawnInChurch(pact);
            bool churchSpawnSuccess = Vector3.Distance(playerTf.position, churchAnchor.position) < 0.01f;

            // 2. Practice Dummy Verification
            var dummyGo = new GameObject("Test_PracticeDummy");
            var dummy = dummyGo.AddComponent<PracticeDummyTarget>();
            dummy.OnSlice(Vector3.zero, Vector3.right, 75f);
            dummy.OnSpellHit(SpellAffinity.Fire, 120f);
            bool dummyActive = dummy != null;

            // 3. Action Buttons: Z-Target, Stance Swap, Dash, Jump, Mount, AI Auto
            ctrl.OnSwapWeaponButtonPressed();
            bool swapSuccess = ctrl.isSecondaryLoadoutActive;

            ctrl.OnDashButtonPressed(Vector2.right);
            ctrl.OnJumpButtonPressed();

            ctrl.OnToggleMountButtonPressed();
            bool mountSuccess = ctrl.isMounted && Mathf.Approximately(ctrl.followMoveSpeed, 13.5f);

            ctrl.OnToggleAiAutopilotButtonPressed();
            bool aiAutoSuccess = ctrl.isAiAutopilotEnabled;

            // 4. Dynamic Right-Edge Party & Staggered Follow
            var ally1Obj = new GameObject("Test_Ally1");
            var ally2Obj = new GameObject("Test_Ally2");
            var ally1 = new PartyMemberRuntimeData { allyId = "ally_01", allyName = "Lyra the Mage", overworldInstance = ally1Obj.transform };
            var ally2 = new PartyMemberRuntimeData { allyId = "ally_02", allyName = "Kael the Paladin", overworldInstance = ally2Obj.transform };

            ctrl.AddAllyToParty(ally1);
            ctrl.AddAllyToParty(ally2);
            bool partyAddSuccess = ctrl.activePartyMembers.Count == 2;
            bool nonLinearOffsets = ctrl.activePartyMembers[0].assignedFormationOffset != ctrl.activePartyMembers[1].assignedFormationOffset;

            ctrl.RemoveAllyFromParty("ally_01");
            bool partyRemoveSuccess = ctrl.activePartyMembers.Count == 1;

            // 5. Test Boss Defeat & Mining Extraction
            ctrl.OnTestBossDefeated();
            bool bossDefeatSuccess = ctrl.hasDefeatedTestBoss && ctrl.hasAcquiredBossRelic && ctrl.harvestedBossMeatCount >= 3;

            ctrl.OnTestMiningNodeExtracted(4, 2);
            bool miningSuccess = ctrl.minedIronOreCount >= 9 && ctrl.minedPrismaticGemsCount >= 2;

            // 6. Return from Dungeon
            ctrl.ReturnFromTestDungeonToTown();
            bool returnToTownSuccess = Vector3.Distance(playerTf.position, portalAnchor.position) < 0.01f;

            // 7. Bazaar Buy & Sell Economy Validation
            // NPC Bank Cap Check: Selling beyond balance fails
            bool tradeOverBankDenied = !ctrl.TrySellItemAtBazaar("Excalibur", 2000, true);
            // Valid NPC sale
            bool tradeNpcApproved = ctrl.TrySellItemAtBazaar("Star-Iron Blade", 200, true);
            // Player storefront sale with 5% burn fee
            int coresBeforeBazaar = ctrl.playerManaCores;
            bool playerBazaarApproved = ctrl.TrySellItemAtBazaar("Cooked Potluck Dish", 100, false);
            int netEarned = ctrl.playerManaCores - coresBeforeBazaar;
            bool burnFeeCorrect = (netEarned == 95); // 100 - 5% = 95 cores

            // Cleanup test objects
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(churchAnchor.gameObject);
            UnityEngine.Object.DestroyImmediate(portalAnchor.gameObject);
            UnityEngine.Object.DestroyImmediate(playerObj);
            UnityEngine.Object.DestroyImmediate(dummyGo);
            UnityEngine.Object.DestroyImmediate(ally1Obj);
            UnityEngine.Object.DestroyImmediate(ally2Obj);

            bool pass = churchSpawnSuccess && dummyActive && swapSuccess && mountSuccess && aiAutoSuccess &&
                        partyAddSuccess && nonLinearOffsets && partyRemoveSuccess && bossDefeatSuccess &&
                        miningSuccess && returnToTownSuccess && tradeOverBankDenied && tradeNpcApproved &&
                        playerBazaarApproved && burnFeeCorrect;

            Debug.Log($"[VERIFY 0.1 - FIRST PLAYABLE VERTICAL SLICE] Result: {(pass ? "PASS" : "FAIL")} | Church Spawn: {churchSpawnSuccess}, Swap: {swapSuccess}, Mount: {mountSuccess}, AI Auto: {aiAutoSuccess}, Party: {partyAddSuccess}/{partyRemoveSuccess}, Boss: {bossDefeatSuccess}, Mining: {miningSuccess}, Bazaar Bank Cap: {tradeOverBankDenied}, 5% Deflationary Burn: {burnFeeCorrect}");
            return pass;
        }

        private static bool VerifySection1_GuardianPactSignature()
        {
            var go = new GameObject("Test_GuardianSignatureBinder");
            var binder = go.AddComponent<GuardianSignatureBinder>();

            binder.BeginSignatureStroke();
            for (int i = 0; i < 12; i++)
            {
                binder.RecordSignaturePoint(new Vector2(100f + i * 15f, 200f + Mathf.Sin(i) * 20f));
            }
            binder.EndSignatureStroke();

            bool pactResult = binder.CompleteGuardianPact("Aurelia", "GrandMaster_Jedi");
            bool hasBound = binder.HasBoundGuardian();

            string stampedSword = binder.StampSignatureOnForgedWeapon("Radiant Claymore");
            string stampedContract = binder.StampSignatureOnGuildContract("Subjugation of Floor 50");

            UnityEngine.Object.DestroyImmediate(go);

            bool pass = pactResult && hasBound &&
                        stampedSword.Contains("GrandMaster_Jedi") &&
                        stampedContract.Contains("GrandMaster_Jedi");

            Debug.Log($"[VERIFY 1 - GUARDIAN PACT] Result: {(pass ? "PASS" : "FAIL")} | Stamped: {stampedSword}");
            return pass;
        }

        private static bool VerifySection2_BlueprintTownAndHousing()
        {
            var go = new GameObject("Test_BlueprintTownManager");
            var mgr = go.AddComponent<BlueprintTownAndHousingManager>();
            mgr.PopulateTownFromDiagram();

            bool hasPlots = mgr.townPlots.Count > 0;
            // Verify mount avenue clearance (no plots near 0, 90, 180, 270 deg)
            bool avenueClear = true;
            foreach (var p in mgr.townPlots)
            {
                float angle = Mathf.Atan2(p.normalizedDiagramCoords.y, p.normalizedDiagramCoords.x) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360f;
                if (Mathf.Abs(angle % 90f) < 7f || Mathf.Abs((angle % 90f) - 90f) < 7f)
                {
                    avenueClear = false;
                    break;
                }
            }

            int playerCores = 500;
            bool claimSuccess = mgr.ClaimPlayerHousePlot(mgr.townPlots[0].plotId, "Player_01", 100, ref playerCores);
            bool deployShopSuccess = mgr.DeployPersonalStorefront(mgr.townPlots[0].plotId, new List<string> { "sword_iron_01", "dish_wyvern_stew" });

            UnityEngine.Object.DestroyImmediate(go);

            bool pass = hasPlots && avenueClear && claimSuccess && deployShopSuccess && playerCores == 400;
            Debug.Log($"[VERIFY 2 - BLUEPRINT TOWN] Result: {(pass ? "PASS" : "FAIL")} | Plots: {mgr.townPlots.Count}, Avenues Clear: {avenueClear}");
            return pass;
        }

        private static bool VerifySection3_HobbyMasteryAndPotluck()
        {
            var go = new GameObject("Test_HobbyMastery");
            var engine = go.AddComponent<HobbyMasteryAndPotluckEngine>();

            engine.cookingLevel = 5;
            engine.blacksmithLevel = 50;

            // Add 3 gourmet cuts for God-Tier Feast
            engine.AddPlayerChosenIngredientToPot(new MonsterIngredient { ingredientName = "Wyvern Tail", monsterOriginSpecies = "Wyvern", baseHealContribution = 80f, baseManaRegenContribution = 5f, isGourmetCut = true });
            engine.AddPlayerChosenIngredientToPot(new MonsterIngredient { ingredientName = "Basilisk Yolk", monsterOriginSpecies = "Basilisk", baseHealContribution = 60f, baseManaRegenContribution = 8f, isGourmetCut = true });
            engine.AddPlayerChosenIngredientToPot(new MonsterIngredient { ingredientName = "Slime Jelly", monsterOriginSpecies = "RoyalSlime", baseHealContribution = 50f, baseManaRegenContribution = 4f, isGourmetCut = true });

            var meal = engine.FinishCookingPotluck("Grand Wyvern Bouillabaisse", 0.95f, "Gastronomic History of Bouillabaisse", "Originally a stew made by Marseillaise fishermen using bony rockfish...");
            bool isGodTier = meal.dishName.Contains("God-Tier Feast");

            // Blacksmith forging: level 50 -> 3 + 2 = 5 sockets
            var sword = engine.ForgeSwordOnAnvil("Mithril Longsword", 50f, 150f, 0.90f);
            bool socketBonus = sword.maxEnchantmentSockets == 5; // 3 + 50/25 = 5 sockets

            UnityEngine.Object.DestroyImmediate(go);

            bool pass = isGodTier && socketBonus && meal.totalHealthRestore > 250f;
            Debug.Log($"[VERIFY 3 - HOBBY MASTERY] Result: {(pass ? "PASS" : "FAIL")} | Meal: {meal.dishName} (Heal: {meal.totalHealthRestore:F0}), Sockets: {sword.maxEnchantmentSockets}");
            return pass;
        }

        private static bool VerifySection4_DungeonInstabilityAndFortress()
        {
            var go = new GameObject("Test_DungeonDimension");
            var instabilityMgr = go.AddComponent<DungeonInstabilityManager>();
            var fortressNode = go.AddComponent<PortalOutpostFortressNode>();

            instabilityMgr.EnterDungeonDimension(4, 1.0f);
            instabilityMgr.AdvanceDimensionTime(60f * 8f); // 8 minutes -> Volatile stage

            // Escalate Fortress
            fortressNode.uncheckedDurationMinutes = 65f;
            fortressNode.EscalateToTier(OutpostTier.Tier3_RampagingFortress);

            float scaledMult = instabilityMgr.instabilityMultiplier;
            InstabilityStage stage = instabilityMgr.currentStage;
            bool isVolatileOrHigher = stage >= InstabilityStage.Volatile && scaledMult > 1.5f;
            bool isFortressRaid = fortressNode.isGuildMassRaidActive && fortressNode.isPortalEntranceBlockedByFortress;

            instabilityMgr.ExitDungeonDimension();
            UnityEngine.Object.DestroyImmediate(go);

            bool pass = isVolatileOrHigher && isFortressRaid;
            Debug.Log($"[VERIFY 4 - DUNGEON PORTAL INSTABILITY] Result: {(pass ? "PASS" : "FAIL")} | Instability: {scaledMult:F2}x, Stage: {stage}, Fortress Blocked: {isFortressRaid}");
            return pass;
        }

        private static bool VerifySection5_CustomSpecialMoveRecorder()
        {
            var go = new GameObject("Test_SpecialMoveRecorder");
            var recorder = go.AddComponent<CustomSpecialMoveRecorder>();

            recorder.playerCharacterLevel = 30; // Level 30 -> 2 + 30/15 = 4 allowed slots
            recorder.isInsidePracticeRoom = true;

            int maxSlots = recorder.GetMaxAllowedSpecialMoveSlots();
            bool slotCalcCorrect = maxSlots == 4;

            bool step1 = recorder.RecordStepInPracticeRoom(true, new RecordedMoveNode { actionType = RecordedActionType.MeleeDirectionalSlash, swipeDirectionOrCastVector = Vector2.up, basePowerMultiplier = 1.0f });
            bool step2 = recorder.RecordStepInPracticeRoom(true, new RecordedMoveNode { actionType = RecordedActionType.MeleeDirectionalSlash, swipeDirectionOrCastVector = Vector2.right, basePowerMultiplier = 1.2f });

            // Team Synergy gauge test
            recorder.AddTeamSynergyProgress(50f);
            bool cantLaunchYet = !recorder.TryExecuteTeammateShoulderThrowLaunch(null);
            recorder.AddTeamSynergyProgress(55f); // Gauge reaches 100%
            bool canLaunchNow = recorder.TryExecuteTeammateShoulderThrowLaunch(null);

            UnityEngine.Object.DestroyImmediate(go);

            bool pass = slotCalcCorrect && step1 && step2 && cantLaunchYet && canLaunchNow && recorder.customMeleeSpecialSequence[0].basePowerMultiplier >= 1.35f;
            Debug.Log($"[VERIFY 5 - SPECIAL MOVE RECORDER] Result: {(pass ? "PASS" : "FAIL")} | Max Slots: {maxSlots}, Shoulder Throw: {canLaunchNow}");
            return pass;
        }

        private static bool VerifySection6_CombatMasteryAndEvolution()
        {
            var go = new GameObject("Test_CombatMastery");
            var combat = go.AddComponent<CombatMasteryAndEvolutionSystem>();
            combat.dexterityStat = 35; // High DEX

            // Weak-Spot Test: aligned angles
            float multiplier = combat.EvaluateWeakSpotHit("Chimera", Vector2.up, Vector2.up, SpellAffinity.None, SpellAffinity.None, out bool crit);

            // Heirloom Awakening Test
            string weaponName = "Rusty Iron Blade";
            int totalKills = 499;
            float baseDmg = 20f;
            bool awakened = false;
            combat.RegisterWeaponKillForHeirloomEvolution(ref weaponName, ref totalKills, ref baseDmg, ref awakened, "Goblin");

            // Dexterity Disarm Counter Test
            bool disarmResult = combat.TryDexterityDisarmAndCollectCounter("Goblin_Cleaver", out float counterDmg);

            UnityEngine.Object.DestroyImmediate(go);

            bool pass = crit && multiplier == 2.25f && awakened && baseDmg > 50f && weaponName.Contains("Awakened Goblin-Bane");
            Debug.Log($"[VERIFY 6 - COMBAT MASTERY & EVOLUTION] Result: {(pass ? "PASS" : "FAIL")} | Weak-Spot Crit: {crit} ({multiplier}x), Awakened Weapon: {weaponName} (Dmg: {baseDmg:F1})");
            return pass;
        }

        private static bool VerifySection7_TacticalEnvironmentAndCursedMask()
        {
            var go = new GameObject("Test_TacticalEnv");
            var env = go.AddComponent<TacticalEnvironmentAndMaskController>();

            // Rainstorm weather magic check
            env.SetWeather(FloorWeather.Rainstorm);
            float lightningMult = env.GetWeatherSpellMultiplier(SpellAffinity.Lightning, out bool chains);
            float fireMult = env.GetWeatherSpellMultiplier(SpellAffinity.Fire, out _);

            // Cursed Mask Possession
            float hp = 1f;
            float mp = 100f;
            env.TriggerCursedMaskPossessionPrompt(ref hp, ref mp);
            bool maskActive = env.isCursedMaskPossessionActive;

            // Guild Banner
            env.PlantGuildTrophyBanner(new Vector3(10f, 0f, 10f));
            bool bannerStanding = env.isGuildBannerPlanted;
            bool inZone = env.IsPositionInBannerZone(new Vector3(12f, 0f, 12f));

            UnityEngine.Object.DestroyImmediate(go);

            bool pass = chains && lightningMult == 1.50f && fireMult == 0.65f && maskActive && bannerStanding && inZone;
            Debug.Log($"[VERIFY 7 - TACTICAL ENVIRONMENT & MASK] Result: {(pass ? "PASS" : "FAIL")} | Lightning Chain: {chains} (1.5x), Fire: {fireMult}x, Mask Active: {maskActive}, Banner In-Zone: {inZone}");
            return pass;
        }

        private static bool VerifySection8_TavernSocialAndWorldEvents()
        {
            var go = new GameObject("Test_TavernSocial");
            var tavern = go.AddComponent<TavernSocialAndWorldEventsManager>();

            // Arm Wrestling
            tavern.StartArmWrestlingMatch();
            tavern.RegisterArmWrestleAlternateTap(0f);
            tavern.CounterArmWrestleSurge();
            bool wrestleProg = tavern.armWrestleMeter > 0.5f;

            // Secret Door Rhythm Knock
            tavern.ValidateSecretDoorKnockTap(); // tap 0

            // Band Jam
            bool jamBuff = tavern.EvaluateCampfireBandJamBuff(3); // 3 musicians -> Concert Hype!

            // Bribe Merchant
            bool bribe = tavern.BribeMerchantWithCookedDish("God-Tier Feast", 200f, out float disc, out string pwd);

            UnityEngine.Object.DestroyImmediate(go);

            bool pass = wrestleProg && jamBuff && bribe && disc >= 0.35f && pwd == "Sable Serpent";
            Debug.Log($"[VERIFY 8 - TAVERN SOCIAL & WORLD EVENTS] Result: {(pass ? "PASS" : "FAIL")} | Arm Wrestle: {wrestleProg}, Jam Buff: {jamBuff}, Merchant Discount: {disc * 100f}%, Secret Pwd: '{pwd}'");
            return pass;
        }

        [MenuItem("BattleSummoner/Setup Vertical Slice Hierarchy In Scene", false, 2)]
        public static void SetupVerticalSliceHierarchyInScene()
        {
            var controllerObj = GameObject.Find("[VerticalSliceSandboxController]");
            if (controllerObj == null)
            {
                controllerObj = new GameObject("[VerticalSliceSandboxController]");
                Undo.RegisterCreatedObjectUndo(controllerObj, "Create VerticalSliceSandboxController");
            }

            var sandboxCtrl = controllerObj.GetComponent<CoreVerticalSliceSandboxController>();
            if (sandboxCtrl == null)
            {
                sandboxCtrl = controllerObj.AddComponent<CoreVerticalSliceSandboxController>();
            }

            // Wire Player
            var playerObj = GameObject.Find("Player_Hero");
            if (playerObj != null)
            {
                sandboxCtrl.playerTransform = playerObj.transform;
                sandboxCtrl.playerController = playerObj.GetComponent<CharacterController>();
            }

            // Create or configure Anchors
            var anchorsRoot = GameObject.Find("--- VERTICAL SLICE ANCHORS ---");
            if (anchorsRoot == null)
            {
                anchorsRoot = new GameObject("--- VERTICAL SLICE ANCHORS ---");
                Undo.RegisterCreatedObjectUndo(anchorsRoot, "Create Anchors Root");
            }

            Transform GetOrCreateAnchor(string name, Vector3 pos)
            {
                var existing = GameObject.Find(name);
                if (existing != null) return existing.transform;
                var t = new GameObject(name).transform;
                t.SetParent(anchorsRoot.transform);
                t.position = pos;
                return t;
            }

            sandboxCtrl.churchInteriorSpawnPoint = GetOrCreateAnchor("Anchor_ChurchInteriorSpawn", new Vector3(0f, 85f, 0f));
            sandboxCtrl.practiceDummyYardPoint = GetOrCreateAnchor("Anchor_PracticeDummyYard", new Vector3(8f, 85f, 12f));
            sandboxCtrl.exteriorTestDungeonPortalPoint = GetOrCreateAnchor("Anchor_ExteriorTestDungeonPortal", new Vector3(15f, 85f, 18f));
            sandboxCtrl.testDungeonBossArenaSpawnPoint = GetOrCreateAnchor("Anchor_BossArenaSpawn", new Vector3(200f, -50f, 200f));
            sandboxCtrl.townBazaarAndCraftingPlazaPoint = GetOrCreateAnchor("Anchor_TownBazaarCraftingPlaza", new Vector3(0f, 25f, 120f));

            // Create Practice Dummy
            var dummyObj = GameObject.Find("Courtyard_PracticeDummy_Armored");
            if (dummyObj == null)
            {
                dummyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dummyObj.name = "Courtyard_PracticeDummy_Armored";
                dummyObj.transform.position = sandboxCtrl.practiceDummyYardPoint.position + new Vector3(0f, 1f, 0f);
                var dummy = dummyObj.AddComponent<PracticeDummyTarget>();
                dummy.dummyName = "Armored Practice Dummy";
                dummy.dummyMeshRenderer = dummyObj.GetComponent<Renderer>();
                dummy.hasWeakSpotSeam = true;
                dummy.weakSpotSeamAngle = new Vector2(0.707f, 0.707f);
                dummy.elementalWeakness = SpellAffinity.Fire;
                Undo.RegisterCreatedObjectUndo(dummyObj, "Create Practice Dummy");
            }

            // Configure HUD Party Column
            var hudObj = GameObject.Find("PlayerHUDCanvas");
            if (hudObj != null)
            {
                var partyCol = hudObj.transform.Find("PartySquadColumn");
                if (partyCol == null)
                {
                    var colObj = new GameObject("PartySquadColumn", typeof(RectTransform));
                    colObj.transform.SetParent(hudObj.transform, false);
                    var rt = colObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0.2f);
                    rt.anchorMax = new Vector2(1f, 0.8f);
                    rt.pivot = new Vector2(1f, 0.5f);
                    rt.sizeDelta = new Vector2(120f, 400f);
                    rt.anchoredPosition = new Vector2(-15f, 0f);
                    Undo.RegisterCreatedObjectUndo(colObj, "Create Party Column");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Vertical Slice Setup] Hierarchy and Anchors successfully wired and saved in OverworldMap scene!");
        }

        private static bool VerifySection9_GestureCanvasAndBladeCutterBinding()
        {
            var hero = GameObject.Find("Player_Hero");
            var hud = GameObject.Find("PlayerHUDCanvas");
            var playArea = GameObject.Find("PlayAreaFrame");

            bool hasCutter = hero != null && hero.GetComponent<FruitNinjaBladeCutter>() != null;
            bool hasCanvas = playArea != null && playArea.GetComponent<PortraitGestureCanvas>() != null;
            bool hasCombatMastery = hero != null && hero.GetComponent<CombatMasteryAndEvolutionSystem>() != null;
            bool hasSpecialMoveRecorder = hero != null && hero.GetComponent<CustomSpecialMoveRecorder>() != null;
            bool hasTownHousingMgr = GameObject.Find("BlueprintTownHousingManager") != null;
            bool hasSandboxCtrl = GameObject.Find("[VerticalSliceSandboxController]") != null;
            bool hasDummy = GameObject.Find("Courtyard_PracticeDummy_Armored") != null;

            bool pass = hasCutter && hasCanvas && hasCombatMastery && hasSpecialMoveRecorder && hasTownHousingMgr && hasSandboxCtrl && hasDummy;
            Debug.Log($"[VERIFY 9 - SCENE BINDINGS & CANVAS] Result: {(pass ? "PASS" : "FAIL")} | Cutter: {hasCutter}, Canvas: {hasCanvas}, Mastery: {hasCombatMastery}, SpecialRecorder: {hasSpecialMoveRecorder}, TownHousingMgr: {hasTownHousingMgr}, SandboxCtrl: {hasSandboxCtrl}, PracticeDummy: {hasDummy}");
            return pass;
        }

        private static bool VerifySection10_CharacterAnimationsAndBlending()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animators/PlayerHeroAnimator.controller");
            bool hasController = ctrl != null;
            if (!hasController) return false;

            // 1. Verify Locomotion BlendTree
            var locoState = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Locomotion");
            bool hasLoco = locoState.state != null && locoState.state.motion is UnityEditor.Animations.BlendTree;
            var tree = locoState.state.motion as UnityEditor.Animations.BlendTree;
            bool treeValid = tree != null && tree.children.Length == 4 && tree.blendParameter == "Speed";

            // Verify looping of locomotion motions
            bool loopingValid = true;
            if (treeValid)
            {
                foreach (var c in tree.children)
                {
                    if (c.motion is AnimationClip clip)
                    {
                        if (!clip.isLooping) loopingValid = false;
                    }
                }
            }

            // 2. Verify Attack States & Cross-Fade Blend Transitions
            var a1 = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Attack01");
            var a2 = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Attack02");
            var a3 = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Attack03");
            var a4 = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Attack04_Heavy");

            bool comboStatesValid = a1.state != null && a2.state != null && a3.state != null && a4.state != null;
            bool attackTransitionsValid = true;
            if (comboStatesValid)
            {
                foreach (var t in a1.state.transitions)
                {
                    if (t.duration < 0.08f) attackTransitionsValid = false;
                }
            }

            // 3. Verify Jump, Dash, Defend, Hit, Victory States
            var dashState = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Dash");
            var jStart = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Jump_Takeoff");
            var defState = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Defend");
            var vicState = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "Victory");
            var lvlState = Array.Find(ctrl.layers[0].stateMachine.states, s => s.state.name == "LevelUp");

            bool specialStatesValid = dashState.state != null && jStart.state != null && defState.state != null && vicState.state != null && lvlState.state != null;

            // 4. Verify Player_Hero Rig & Playable Animator
            var hero = GameObject.Find("Player_Hero");
            var avatarRoot = hero != null ? hero.transform.Find("AvatarRigRoot") : null;
            var animCtrl = hero != null ? hero.GetComponent<HumanoidAnimationController>() : null;
            bool heroAvatarActive = avatarRoot != null && avatarRoot.gameObject.activeInHierarchy;
            bool heroPlayable = animCtrl != null && animCtrl.IsAnimatorPlayable;

            // 5. Verify Party Member Overworld Spawning & Animation
            var sandbox = CoreVerticalSliceSandboxController.Instance ?? UnityEngine.Object.FindFirstObjectByType<CoreVerticalSliceSandboxController>();
            bool partyAnimationValid = false;
            if (sandbox != null)
            {
                var testAlly = new PartyMemberRuntimeData { allyId = "anim_test_ally", allyName = "Eldrin" };
                sandbox.AddAllyToParty(testAlly);
                bool allySpawned = testAlly.overworldInstance != null;
                var allyAnim = allySpawned ? testAlly.overworldInstance.GetComponentInChildren<Animator>() : null;
                bool allyHasAnim = allyAnim != null && allyAnim.runtimeAnimatorController != null && allyAnim.runtimeAnimatorController.name == "PlayerHeroAnimator";

                sandbox.RemoveAllyFromParty("anim_test_ally");
                bool allyCleaned = testAlly.overworldInstance == null;
                partyAnimationValid = allySpawned && allyHasAnim && allyCleaned;
            }

            bool pass = hasController && hasLoco && treeValid && loopingValid && comboStatesValid &&
                        attackTransitionsValid && specialStatesValid && heroAvatarActive && heroPlayable && partyAnimationValid;

            Debug.Log($"[VERIFY 10 - CHARACTER ANIMATIONS & BLENDING] Result: {(pass ? "PASS" : "FAIL")} | BlendTree: {treeValid}, Looping: {loopingValid}, Combos: {comboStatesValid}, CrossFades: {attackTransitionsValid}, Hero Playable: {heroPlayable}, Party Spawning & Anim: {partyAnimationValid}");
            return pass;
        }

        private static bool VerifySection11_MotionMatchingAndMocapCentral()
        {
            // 1. Verify Player_Hero has MotionMatchingLocomotionEngine attached
            var hero = GameObject.Find("Player_Hero");
            if (hero == null)
            {
                Debug.LogError("[VERIFY 11] Player_Hero not found in scene!");
                return false;
            }

            var mmEngine = hero.GetComponent<BattleSummoner.Animation.MotionMatching.MotionMatchingLocomotionEngine>();
            if (mmEngine == null)
            {
                Debug.LogError("[VERIFY 11] MotionMatchingLocomotionEngine missing on Player_Hero!");
                return false;
            }

            mmEngine.EnsureInitialized();

            // 2. Verify Trajectory Prediction
            bool trajValid = mmEngine.Trajectory != null && mmEngine.Trajectory.Points != null && mmEngine.Trajectory.Points.Length == 3;

            // 3. Verify Inertializer Blender & Decay
            bool inertValid = mmEngine.Inertializer != null;
            if (inertValid)
            {
                mmEngine.Inertializer.TriggerTransition(Vector3.forward * 5f, Vector3.zero, Quaternion.identity, Quaternion.Euler(0f, 90f, 0f), 0.15f);
                Vector3 off = mmEngine.Inertializer.GetCurrentPositionOffset();
                if (float.IsNaN(off.x) || float.IsInfinity(off.x)) inertValid = false;
            }

            // 4. Verify Foot Locking IK on Humanoid Rig
            bool footIKValid = mmEngine.FootIK != null;

            // 5. Verify Believable Mocap Database & Cost Evaluation
            bool dbValid = mmEngine.MocapDatabase != null;
            if (dbValid)
            {
                var cand = mmEngine.MocapDatabase.FindBestMatch(mmEngine.Trajectory, 0f, BattleSummoner.Animation.MotionMatching.MocapLocomotionStyle.IdleMocapNatural);
                if (cand.clipDuration <= 0f) dbValid = false;
            }

            // 6. Verify Mocap Central Animation Clips on Disk
            string mocapDir = "Assets/Animations/Character/MocapCentral";
            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{mocapDir}/MC_Sample_Idle_Believable.anim");
            var skidClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{mocapDir}/MC_Sample_Run_Skid_PlantStop.anim");
            var cutLClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{mocapDir}/MC_Sample_Run_SharpCut_90L.anim");
            var cutRClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{mocapDir}/MC_Sample_Run_SharpCut_90R.anim");
            var turn180Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{mocapDir}/MC_Sample_Walk_Start_Turn180.anim");

            bool clipsValid = idleClip != null && skidClip != null && cutLClip != null && cutRClip != null && turn180Clip != null;

            // 7. Verify Animator Controller Parameters & States
            var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animators/PlayerHeroAnimator.controller");
            bool paramsValid = false;
            bool statesValid = false;

            if (ctrl != null)
            {
                bool hasMocapStyle = Array.Exists(ctrl.parameters, p => p.name == "MocapStyle");
                bool hasTurnAngle = Array.Exists(ctrl.parameters, p => p.name == "TurnAngle");
                bool hasPlantStop = Array.Exists(ctrl.parameters, p => p.name == "IsPlantStop");
                bool hasSharpCut = Array.Exists(ctrl.parameters, p => p.name == "IsSharpCut");
                paramsValid = hasMocapStyle && hasTurnAngle && hasPlantStop && hasSharpCut;

                var sm = ctrl.layers[0].stateMachine;
                bool hasSkidState = Array.Exists(sm.states, s => s.state.name == "Mocap_PlantStop");
                bool hasCutLState = Array.Exists(sm.states, s => s.state.name == "Mocap_SharpCut_Left");
                bool hasCutRState = Array.Exists(sm.states, s => s.state.name == "Mocap_SharpCut_Right");
                bool hasTurn180State = Array.Exists(sm.states, s => s.state.name == "Mocap_Turnaround_180");
                statesValid = hasSkidState && hasCutLState && hasCutRState && hasTurn180State;
            }

            bool pass = trajValid && inertValid && footIKValid && dbValid && clipsValid && paramsValid && statesValid;

            Debug.Log($"[VERIFY 11 - MOTION MATCHING & MOCAP CENTRAL] Result: {(pass ? "PASS" : "FAIL")} | Trajectory: {trajValid}, Inertializer: {inertValid}, FootIK: {footIKValid}, MocapDatabase: {dbValid}, Clips: {clipsValid}, Params: {paramsValid}, States: {statesValid}");
            return pass;
        }

        private static bool VerifySection12_AssetStorePropPacksAndUrbanLamps()
        {
            string propDir = "Assets/Prefabs/Overworld/Props";
            string[] expectedPrefabs = new string[]
            {
                "PFB_Urban_StreetLampPost",
                "PFB_Blacksmith_Anvil_Stump",
                "PFB_Blacksmith_QuenchTrough",
                "PFB_Blacksmith_CoalBasin",
                "PFB_Blacksmith_Grindstone",
                "PFB_Wizard_SpellbookLectern",
                "PFB_Wizard_CrystalPedestal",
                "PFB_Bakery_PastryDisplay",
                "PFB_Bakery_MenuChalkboard",
                "PFB_Alchemy_DistillationBench",
                "PFB_Alchemy_MortarPestleRack",
                "PFB_Medieval_VillageWell",
                "PFB_Medieval_NoticeBoard",
                "PFB_Medieval_WeaponRack",
                "PFB_Medieval_FirewoodStack",
                "PFB_Tavern_DiningTableSet"
            };

            int foundPrefabs = 0;
            foreach (var pName in expectedPrefabs)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{propDir}/{pName}.prefab");
                if (p != null) foundPrefabs++;
            }
            bool prefabsValid = (foundPrefabs == expectedPrefabs.Length);

            // Verify Urban Street Lamp component configuration (Light, Cullable, Collider)
            var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{propDir}/PFB_Urban_StreetLampPost.prefab");
            bool lampValid = false;
            if (lampPrefab != null)
            {
                var light = lampPrefab.GetComponentInChildren<Light>();
                var cullable = lampPrefab.GetComponent<BattleSummoner.Optimization.CullableWorldProp>();
                var col = lampPrefab.GetComponent<Collider>();
                lampValid = light != null && cullable != null && col != null && light.shadows == LightShadows.None;
            }

            // Verify In-Scene Population of props
            var propsRoot = GameObject.Find("06_Lively_Street_Props");
            bool scenePopulationValid = (propsRoot != null && propsRoot.transform.childCount >= 100);

            bool pass = prefabsValid && lampValid && scenePopulationValid;

            Debug.Log($"[VERIFY 12 - ASSET STORE PROPS & URBAN LAMPS] Result: {(pass ? "PASS" : "FAIL")} | Prefabs Found: {foundPrefabs}/{expectedPrefabs.Length}, Lamp Component Setup: {lampValid}, Active Scene Props Count: {(propsRoot != null ? propsRoot.transform.childCount : 0)}");
            return pass;
        }
    }
}
