#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Core;
using BattleSummoner.Combat;
using BattleSummoner.Character;
using BattleSummoner.Optimization;
using BattleSummoner.DesignPhilosophy;

namespace BattleSummoner.EditorScripts
{
    public static class DemoVerticalSliceSetup
    {
        [MenuItem("BattleSummoner/Demo Build/Wire Complete Vertical Slice Demo in OverworldMap", false, 0)]
        public static void WireCompleteVerticalSliceDemo()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/OverworldMap.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            Debug.Log("[Demo Build] Starting complete Vertical Slice wiring for OverworldMap...");

            // 1. Root Manager
            var rootGo = GameObject.Find("--- DEMO_VERTICAL_SLICE_SYSTEM ---");
            if (rootGo == null) rootGo = new GameObject("--- DEMO_VERTICAL_SLICE_SYSTEM ---");
            Undo.RegisterCreatedObjectUndo(rootGo, "Create Demo Slice System");

            var bootstrapper = rootGo.GetComponent<DemoVerticalSliceBootstrapper>();
            if (bootstrapper == null) bootstrapper = rootGo.AddComponent<DemoVerticalSliceBootstrapper>();

            // 2. Optimization Root & SpireSpatialCullingManager
            var optRoot = GameObject.Find("--- OPTIMIZATION_SYSTEMS ---");
            if (optRoot == null) optRoot = new GameObject("--- OPTIMIZATION_SYSTEMS ---");
            var culler = optRoot.GetComponent<SpireSpatialCullingManager>();
            if (culler == null) optRoot.AddComponent<SpireSpatialCullingManager>();

            // 3. Player_Hero Setup & Model Binding
            var player = GameObject.Find("Player_Hero");
            if (player == null)
            {
                player = new GameObject("Player_Hero");
                player.transform.position = new Vector3(0f, 80.1f, 16f);
            }
            bootstrapper.playerHero = player.transform;

            // CharacterController
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.radius = 0.45f;
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Animator
            var anim = player.GetComponent<Animator>();
            if (anim == null) anim = player.AddComponent<Animator>();
            var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animators/PlayerHeroAnimator.controller");
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

            // Model Selector (.GLB bindings)
            var modelSel = player.GetComponent<PlayerHeroModelSelector>();
            if (modelSel == null) modelSel = player.AddComponent<PlayerHeroModelSelector>();

            modelSel.maleStarterGlbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/MaleStarter.glb")
                                         ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/MaleStarter.glb");
            modelSel.femaleStarterGlbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/femalestarter.glb")
                                           ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/femalestarter.glb");
            modelSel.manBaseGlbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/manbase.glb")
                                     ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/manbase.glb");
            modelSel.womanBaseGlbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/womanbase.glb")
                                       ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/womanbase.glb");

            modelSel.selectedSex = CharacterSex.Male;
            modelSel.isWearingStarterArmor = true;
            modelSel.ApplySelectedCharacterModel(CharacterSex.Male, true);

            // Core Combat & Traversal Components
            if (!player.GetComponent<PlayerMovementController>()) player.AddComponent<PlayerMovementController>();
            if (!player.GetComponent<PlayerCombatStanceController>()) player.AddComponent<PlayerCombatStanceController>();
            if (!player.GetComponent<FruitNinjaBladeCutter>()) player.AddComponent<FruitNinjaBladeCutter>();
            if (!player.GetComponent<RuneShapeCastingEngine>()) player.AddComponent<RuneShapeCastingEngine>();
            if (!player.GetComponent<WizardSpellSlingerController>()) player.AddComponent<WizardSpellSlingerController>();
            if (!player.GetComponent<MultiTouchCombatAndRuneDispatcher>()) player.AddComponent<MultiTouchCombatAndRuneDispatcher>();

            // 4. Core NPCs (Catherine & Margo)
            // Catherine in Tier 2 Guild Hall (Y=38.05m)
            var catherineGo = GameObject.Find("NPC_Catherine");
            if (catherineGo == null)
            {
                catherineGo = new GameObject("NPC_Catherine");
            }
            catherineGo.transform.position = new Vector3(-60f, 38.05f, 20f);
            catherineGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var catherineGlb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Catherine.glb");
            if (catherineGlb != null && catherineGo.transform.childCount == 0)
            {
                var cathVisual = (GameObject)PrefabUtility.InstantiatePrefab(catherineGlb, catherineGo.transform);
                cathVisual.transform.localPosition = Vector3.zero;
            }
            bootstrapper.catherineGuildHallNpc = catherineGo.transform;

            // Margo in Tier 4 Cathedral Courtyard (Y=80.08m)
            var margoGo = GameObject.Find("NPC_Margo_Courtyard");
            if (margoGo == null)
            {
                margoGo = new GameObject("NPC_Margo_Courtyard");
            }
            margoGo.transform.position = new Vector3(15f, 80.08f, -30f);
            margoGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var margoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Margo.prefab")
                           ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Starter/femalestarter.glb")
                           ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/femalestarter.glb");
            if (margoPrefab != null && margoGo.transform.childCount == 0)
            {
                var margoVisual = (GameObject)PrefabUtility.InstantiatePrefab(margoPrefab, margoGo.transform);
                margoVisual.transform.localPosition = Vector3.zero;
            }
            bootstrapper.margoCourtyardNpc = margoGo.transform;

            // 5. Step 1: Cathedral Spawn & Affordance Lessons
            var step1Folder = GetOrCreateChild(rootGo.transform, "Step1_CathedralIntro");

            // Altar Parchment
            var parchment = GameObject.Find("Floating_Contract_Parchment");
            if (parchment == null)
            {
                parchment = GameObject.CreatePrimitive(PrimitiveType.Quad);
                parchment.name = "Floating_Contract_Parchment";
                parchment.transform.SetParent(step1Folder, false);
                parchment.transform.position = new Vector3(0f, 81.2f, 23f);
                parchment.transform.rotation = Quaternion.Euler(30f, 180f, 0f);
                parchment.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
                var r = parchment.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(1f, 0.85f, 0.4f, 0.9f);
            }
            bootstrapper.floatingContractParchment = parchment;

            // Fallen Pew Obstacle
            var pew = GameObject.Find("Fallen_Church_Pew_Obstacle");
            if (pew == null)
            {
                pew = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pew.name = "Fallen_Church_Pew_Obstacle";
                pew.transform.SetParent(step1Folder, false);
                pew.transform.position = new Vector3(0f, 80.35f, 2f);
                pew.transform.localScale = new Vector3(5f, 0.7f, 0.8f);
                var r = pew.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.4f, 0.25f, 0.15f);
            }
            bootstrapper.fallenChurchPewObstacle = pew;

            // Cathedral Doors & Rope Barricade
            var doors = GameObject.Find("Cathedral_Double_Doors");
            if (doors == null)
            {
                doors = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doors.name = "Cathedral_Double_Doors";
                doors.transform.SetParent(step1Folder, false);
                doors.transform.position = new Vector3(0f, 82.5f, -14f);
                doors.transform.localScale = new Vector3(4.5f, 5f, 0.5f);
            }
            bootstrapper.cathedralDoubleDoors = doors;

            var rope = GameObject.Find("Cathedral_Door_Rope_Barricade");
            if (rope == null)
            {
                rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rope.name = "Cathedral_Door_Rope_Barricade";
                rope.transform.SetParent(step1Folder, false);
                rope.transform.position = new Vector3(0f, 81.5f, -13.5f);
                rope.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                rope.transform.localScale = new Vector3(0.12f, 2.5f, 0.12f);
                var r = rope.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(1f, 0.8f, 0.2f);
            }
            var ropeBarricade = rope.GetComponent<CathedralDoorRopeBarricade>() ?? rope.AddComponent<CathedralDoorRopeBarricade>();
            bootstrapper.doorRopeBarricade = ropeBarricade;

            // 6. Step 2: Practice Dummies & Progressive HUD
            var step2Folder = GetOrCreateChild(rootGo.transform, "Step2_CourtyardPractice");

            // Dummy 1: Velocity Cutting & Z-Targeting
            var d1 = GameObject.Find("Practice_Dummy_1_VelocityCut");
            if (d1 == null)
            {
                d1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                d1.name = "Practice_Dummy_1_VelocityCut";
                d1.transform.SetParent(step2Folder, false);
                d1.transform.position = new Vector3(-4f, 80.9f, -26f);
                d1.transform.localScale = new Vector3(0.8f, 1.8f, 0.8f);
                var r = d1.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.8f, 0.6f, 0.4f);
            }
            var dummy1Comp = d1.GetComponent<DemoPracticeDummy>() ?? d1.AddComponent<DemoPracticeDummy>();
            dummy1Comp.dummyIndex = 1;
            dummy1Comp.dummyRenderer = d1.GetComponent<Renderer>();
            bootstrapper.dummy1_VelocityCutting = dummy1Comp;

            // Dummy 2: Clockwork Striker (Dash & Parry)
            var d2 = GameObject.Find("Practice_Dummy_2_ClockworkStriker");
            if (d2 == null)
            {
                d2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                d2.name = "Practice_Dummy_2_ClockworkStriker";
                d2.transform.SetParent(step2Folder, false);
                d2.transform.position = new Vector3(0f, 80.9f, -28f);
                d2.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);
                var r = d2.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.5f, 0.5f, 0.6f);

                // Arm
                var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Clockwork_Arm";
                arm.transform.SetParent(d2.transform, false);
                arm.transform.localPosition = new Vector3(0.6f, 0.2f, 0f);
                arm.transform.localScale = new Vector3(1.2f, 0.25f, 0.25f);
            }
            var dummy2Comp = d2.GetComponent<DemoClockworkStriker>() ?? d2.AddComponent<DemoClockworkStriker>();
            var armTf = d2.transform.Find("Clockwork_Arm");
            if (armTf != null) dummy2Comp.clockworkArm = armTf.gameObject;
            bootstrapper.dummy2_ClockworkStriker = dummy2Comp;

            // Dummy 3: Brambles Archway (Fireball Rune)
            var d3 = GameObject.Find("Practice_Dummy_3_BramblesArchway");
            if (d3 == null)
            {
                d3 = new GameObject("Practice_Dummy_3_BramblesArchway");
                d3.transform.SetParent(step2Folder, false);
                d3.transform.position = new Vector3(18f, 80.08f, -32f);

                var brambles = GameObject.CreatePrimitive(PrimitiveType.Cube);
                brambles.name = "Brambles_DryWood";
                brambles.transform.SetParent(d3.transform, false);
                brambles.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                brambles.transform.localScale = new Vector3(3.5f, 3.5f, 0.6f);
                var brRenderer = brambles.GetComponent<Renderer>();
                if (brRenderer != null) brRenderer.material.color = new Color(0.35f, 0.2f, 0.1f);

                var mural = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mural.name = "Stone_Mural_RuneGuide";
                mural.transform.SetParent(d3.transform, false);
                mural.transform.localPosition = new Vector3(-3.2f, 2.0f, 0f);
                mural.transform.localScale = new Vector3(2.0f, 2.5f, 1f);
                var mRenderer = mural.GetComponent<Renderer>();
                if (mRenderer != null) mRenderer.material.color = new Color(0.9f, 0.4f, 0.1f);
            }
            var dummy3Comp = d3.GetComponent<DemoBramblesRuneArchway>() ?? d3.AddComponent<DemoBramblesRuneArchway>();
            var brTf = d3.transform.Find("Brambles_DryWood");
            if (brTf != null) dummy3Comp.bramblesVisual = brTf.gameObject;
            var muralTf = d3.transform.Find("Stone_Mural_RuneGuide");
            if (muralTf != null) dummy3Comp.stoneMuralVisual = muralTf.gameObject;
            bootstrapper.dummy3_BramblesArchway = dummy3Comp;

            // 7. Step 3: Town Traversal, Mount & Crafting
            var step3Folder = GetOrCreateChild(rootGo.transform, "Step3_TownAndCrafting");

            // Mount Stable Node
            var mountNode = GameObject.Find("Mount_Whistle_Stable_Node");
            if (mountNode == null)
            {
                mountNode = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mountNode.name = "Mount_Whistle_Stable_Node";
                mountNode.transform.SetParent(step3Folder, false);
                mountNode.transform.position = new Vector3(-12f, 80.08f, -42f);
                mountNode.transform.localScale = new Vector3(2.5f, 0.2f, 2.5f);
                var r = mountNode.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.2f, 0.6f, 0.9f, 0.7f);
            }
            if (!mountNode.GetComponent<DemoMountStableNode>()) mountNode.AddComponent<DemoMountStableNode>();
            bootstrapper.mountWhistleStableNode = mountNode;

            // 4 Crafting Stations in Tier 2
            bootstrapper.blacksmithForgeStation = GetOrCreateStation(step3Folder, "Station_Blacksmith_Forge", new Vector3(-45f, 38.05f, -10f), new Color(0.9f, 0.3f, 0.1f));
            if (!bootstrapper.blacksmithForgeStation.GetComponent<DemoBlacksmithForgeNode>()) bootstrapper.blacksmithForgeStation.AddComponent<DemoBlacksmithForgeNode>();

            bootstrapper.cookingMamaStation = GetOrCreateStation(step3Folder, "Station_Cooking_Potluck", new Vector3(-55f, 38.05f, -15f), new Color(0.4f, 0.8f, 0.2f));
            if (!bootstrapper.cookingMamaStation.GetComponent<DemoCookingStationNode>()) bootstrapper.cookingMamaStation.AddComponent<DemoCookingStationNode>();

            bootstrapper.alchemyTomeDeskStation = GetOrCreateStation(step3Folder, "Station_Alchemy_TomeDesk", new Vector3(-35f, 38.05f, -20f), new Color(0.6f, 0.2f, 0.8f));
            if (!bootstrapper.alchemyTomeDeskStation.GetComponent<DemoAlchemyDeskNode>()) bootstrapper.alchemyTomeDeskStation.AddComponent<DemoAlchemyDeskNode>();

            bootstrapper.enchantingAnvilStation = GetOrCreateStation(step3Folder, "Station_3Strike_EnchantingAnvil", new Vector3(-40f, 38.05f, -5f), new Color(0.2f, 0.7f, 0.9f));
            if (!bootstrapper.enchantingAnvilStation.GetComponent<DemoEnchantingAnvilNode>()) bootstrapper.enchantingAnvilStation.AddComponent<DemoEnchantingAnvilNode>();

            // Tier 1 Bazaar Merchant Stall
            var bazaarStall = GameObject.Find("Bazaar_Merchant_Stall");
            if (bazaarStall == null)
            {
                bazaarStall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bazaarStall.name = "Bazaar_Merchant_Stall";
                bazaarStall.transform.SetParent(step3Folder, false);
                bazaarStall.transform.position = new Vector3(-80f, 22.05f, 40f);
                bazaarStall.transform.localScale = new Vector3(3f, 2.5f, 2f);
                var r = bazaarStall.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.7f, 0.5f, 0.3f);
            }
            if (!bazaarStall.GetComponent<DemoBazaarStallNode>()) bazaarStall.AddComponent<DemoBazaarStallNode>();
            bootstrapper.tier1BazaarMerchantStall = bazaarStall;

            // Catherine in Guild Hall node component
            if (catherineGo != null && !catherineGo.GetComponent<DemoCatherineGuildHallNode>())
            {
                catherineGo.AddComponent<DemoCatherineGuildHallNode>();
            }

            // 8. Step 4: Test Dungeon, Mining & Boss Kaelen
            var step4Folder = GetOrCreateChild(rootGo.transform, "Step4_TestDungeonAndBoss");

            // Portal
            var portal = GameObject.Find("Test_Dungeon_Portal_Archway");
            if (portal == null)
            {
                portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                portal.name = "Test_Dungeon_Portal_Archway";
                portal.transform.SetParent(step4Folder, false);
                portal.transform.position = new Vector3(22f, 80.08f, -34f);
                portal.transform.localScale = new Vector3(3f, 0.1f, 3f);
                var r = portal.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.5f, 0f, 0.8f, 0.8f);
            }
            bootstrapper.testDungeonPortalEntrance = portal;

            // 3 Geode Mining Nodes
            var miningNodes = new List<CaveMiningNode>();
            for (int i = 1; i <= 3; i++)
            {
                var mNodeGo = GameObject.Find($"Subterranean_Geode_Node_{i}");
                if (mNodeGo == null)
                {
                    mNodeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    mNodeGo.name = $"Subterranean_Geode_Node_{i}";
                    mNodeGo.transform.SetParent(step4Folder, false);
                    mNodeGo.transform.position = new Vector3(30f + (i * 2.5f), 80.08f, -36f + (i * 1.5f));
                    mNodeGo.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    var r = mNodeGo.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(0.1f, 0.8f, 0.9f);
                }
                var cmn = mNodeGo.GetComponent<CaveMiningNode>() ?? mNodeGo.AddComponent<CaveMiningNode>();
                cmn.mineralType = (MineralRarity)(i - 1);
                miningNodes.Add(cmn);
            }
            bootstrapper.subterraneanGeodeNodes = miningNodes.ToArray();

            // Arena Destructible Pillars
            var pillars = new List<ArenaDestructiblePillar>();
            for (int p = 1; p <= 2; p++)
            {
                var pillarGo = GameObject.Find($"Arena_Destructible_Pillar_{p}");
                if (pillarGo == null)
                {
                    pillarGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillarGo.name = $"Arena_Destructible_Pillar_{p}";
                    pillarGo.transform.SetParent(step4Folder, false);
                    pillarGo.transform.position = new Vector3(45f + (p * 5f), 82.5f, -40f);
                    pillarGo.transform.localScale = new Vector3(1.5f, 5f, 1.5f);
                    var r = pillarGo.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(0.6f, 0.6f, 0.65f);
                }
                var adp = pillarGo.GetComponent<ArenaDestructiblePillar>() ?? pillarGo.AddComponent<ArenaDestructiblePillar>();
                pillars.Add(adp);
            }
            bootstrapper.arenaDestructiblePillars = pillars.ToArray();

            // Pre-Boss Mana Larvae
            var larvaeGo = GameObject.Find("PreBoss_Mana_Larvae_Cluster");
            if (larvaeGo == null)
            {
                larvaeGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                larvaeGo.name = "PreBoss_Mana_Larvae_Cluster";
                larvaeGo.transform.SetParent(step4Folder, false);
                larvaeGo.transform.position = new Vector3(42f, 80.08f, -38f);
                larvaeGo.transform.localScale = new Vector3(1.5f, 0.8f, 1.5f);
                var r = larvaeGo.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.4f, 1f, 0.7f);
            }
            var larvae = larvaeGo.GetComponent<PreBossLarvaeCluster>() ?? larvaeGo.AddComponent<PreBossLarvaeCluster>();
            bootstrapper.preBossManaLarvae = larvae;

            // Test Boss Kaelen
            var kaelenGo = GameObject.Find("Boss_Kaelen_The_Unforged");
            if (kaelenGo == null)
            {
                kaelenGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                kaelenGo.name = "Boss_Kaelen_The_Unforged";
                kaelenGo.transform.SetParent(step4Folder, false);
                kaelenGo.transform.position = new Vector3(55f, 81.8f, -42f);
                kaelenGo.transform.localScale = new Vector3(2.2f, 3.5f, 2.2f);
                var r = kaelenGo.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.2f, 0.2f, 0.25f);

                // Impaled sword
                var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sword.name = "Impaled_Sword";
                sword.transform.SetParent(kaelenGo.transform, false);
                sword.transform.localPosition = new Vector3(0f, 0.2f, 0.7f);
                sword.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                sword.transform.localScale = new Vector3(0.2f, 1.8f, 0.05f);
                var sr = sword.GetComponent<Renderer>();
                if (sr != null) sr.material.color = new Color(0.2f, 0.8f, 1f);
            }
            var kaelenComp = kaelenGo.GetComponent<DemoBossKaelen>() ?? kaelenGo.AddComponent<DemoBossKaelen>();
            var swordTf = kaelenGo.transform.Find("Impaled_Sword");
            if (swordTf != null) kaelenComp.impaledSwordVisual = swordTf.gameObject;
            bootstrapper.testBossKaelen = kaelenComp;

            // 9. Build Settings Configuration (Scene 0 & Portrait 9:16)
            ConfigureBuildSettings();

            // 10. Split HUD Canvases
            if (BattleViewportRebalancer.Instance != null)
            {
                BattleViewportRebalancer.Instance.EnforceCanvasSeparation();
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("✅ [Demo Build] Successfully wired complete 15-Minute Vertical Slice Demo loop in OverworldMap.unity!");
        }

        private static GameObject GetOrCreateStation(Transform parent, string name, Vector3 pos, Color color)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(2f, 1.2f, 2f);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = color;
            }
            return go;
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            var t = parent.Find(childName);
            if (t != null) return t;
            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static void ConfigureBuildSettings()
        {
            // Ensure OverworldMap is Scene 0
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int overworldIdx = scenes.FindIndex(s => s.path == "Assets/Scenes/OverworldMap.unity");
            if (overworldIdx > 0)
            {
                var ovScene = scenes[overworldIdx];
                scenes.RemoveAt(overworldIdx);
                scenes.Insert(0, ovScene);
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            else if (overworldIdx < 0)
            {
                scenes.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/OverworldMap.unity", true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            // Configure Portrait 9:16 Orientation for Mobile and Standalone
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Set Standalone Resolution (540x960 / 1080x1920 resizable window)
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            Debug.Log("[Demo Build] Build Settings locked: OverworldMap is Scene 0, Portrait 9:16 orientation enforced.");
        }
    }
}
#endif
