using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.Editor
{
    /// <summary>
    /// DialogueAndLootVerificationRunner executes automated end-to-end verification
    /// for both the Multi-Person Dialogue & Text Chat system and the Differentiated
    /// Loot system (Instant Minion drops vs Monster Hunter Boss Carving & Shiny Drops).
    /// </summary>
    public static class DialogueAndLootVerificationRunner
    {
        [MenuItem("Tools/BattleSummoner/Verify Multi-Person Dialogue & Monster Hunter Loot")]
        public static void RunFullVerification()
        {
            Debug.Log("========================================================================");
            Debug.Log("🚀 [VERIFICATION RUNNER] Starting Multi-Person Dialogue & Differentiated Loot Verification...");
            Debug.Log("========================================================================");

            int passed = 0;
            int total = 7;

            // TEST 1: Multi-Person Dialogue Overlay Presence & Wiring
            try
            {
                var canvas = GameObject.Find("PortraitHUDCanvas");
                if (canvas == null)
                {
                    Debug.LogWarning("[TEST 1 WARN] PortraitHUDCanvas not in current open scene, loading MainDungeonFloor.unity...");
                    EditorSceneManager.OpenScene("Assets/Scenes/MainDungeonFloor.unity");
                    canvas = GameObject.Find("PortraitHUDCanvas");
                }

                AssertNotNull(canvas, "PortraitHUDCanvas exists in scene.");

                Transform overlayTransform = canvas.transform.Find("MultiPersonDialogueChatOverlay");
                if (overlayTransform == null)
                {
                    Debug.Log("[TEST 1] Building MultiPersonDialogueChatOverlay via PersonaAndMenusHierarchyBuilder...");
                    PersonaAndMenusHierarchyBuilder.BuildMultiPersonDialogueChatMenu();
                    overlayTransform = canvas.transform.Find("MultiPersonDialogueChatOverlay");
                }

                AssertNotNull(overlayTransform, "MultiPersonDialogueChatOverlay exists in scene hierarchy.");
                var hub = overlayTransform.GetComponent<MultiPersonDialogueChatHub>();
                AssertNotNull(hub, "MultiPersonDialogueChatHub component attached to overlay.");

                Debug.Log("✅ [TEST 1 PASSED] Multi-Person Dialogue Chat Overlay & Hub validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 1 FAILED] {ex.Message}");
            }

            // TEST 2: ElevenLabsTTSBridge Voice Mappings (Alex, Elena, Player)
            try
            {
                var ttsBridgeGo = new GameObject("Test_TTSBridge");
                var tts = ttsBridgeGo.AddComponent<ElevenLabsTTSBridge>();

                string alexVoice = tts.GetVoiceIdForNPC("alex");
                string elenaVoice = tts.GetVoiceIdForNPC("elena");
                string playerVoice = tts.GetVoiceIdForNPC("player");

                AssertTrue(!string.IsNullOrEmpty(alexVoice), $"Alex voice mapped: {alexVoice}");
                AssertTrue(!string.IsNullOrEmpty(elenaVoice), $"Elena voice mapped: {elenaVoice}");
                AssertTrue(!string.IsNullOrEmpty(playerVoice), $"Player voice mapped: {playerVoice}");

                // Test procedural formant synthesis offline
                AudioClip alexClip = tts.SynthesizeProceduralVocalClip("The energy in this town feels restless.", "alex");
                AudioClip elenaClip = tts.SynthesizeProceduralVocalClip("I feel it too. Let's keep our distance.", "elena");
                AudioClip playerClip = tts.SynthesizeProceduralVocalClip("Let's check the spires together.", "player");

                AssertNotNull(alexClip, "Alex procedural voice clip synthesized.");
                AssertNotNull(elenaClip, "Elena procedural voice clip synthesized.");
                AssertNotNull(playerClip, "Player procedural voice clip synthesized.");

                UnityEngine.Object.DestroyImmediate(ttsBridgeGo);
                Debug.Log("✅ [TEST 2 PASSED] ElevenLabsTTSBridge Divinatus read-aloud voice mappings & offline synthesis validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 2 FAILED] {ex.Message}");
            }

            // TEST 3: Audio Master Procedural Loot & Carve Buffers
            try
            {
                var audioMaster = BattleSummonerAudioMaster.Instance;
                if (audioMaster == null)
                {
                    var aGo = new GameObject("Test_AudioMaster");
                    audioMaster = aGo.AddComponent<BattleSummonerAudioMaster>();
                }

                AudioClip lootChime = audioMaster.GetProceduralClip("loot_pickup");
                AudioClip bladeCarve = audioMaster.GetProceduralClip("blade_carve");
                AudioClip carveFanfare = audioMaster.GetProceduralClip("carve_fanfare");

                AssertNotNull(lootChime, "loot_pickup procedural audio buffer generated.");
                AssertNotNull(bladeCarve, "blade_carve procedural audio buffer generated.");
                AssertNotNull(carveFanfare, "carve_fanfare procedural audio buffer generated.");

                Debug.Log("✅ [TEST 3 PASSED] Procedural audio synthesis for loot chimes, blade carve, and fanfare validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 3 FAILED] {ex.Message}");
            }

            // TEST 4: Small Enemy Instant Loot Mechanism
            try
            {
                // Ensure InventoryMenuUI exists
                if (InventoryMenuUI.Instance == null)
                {
                    var invGo = new GameObject("Test_InventoryMenuUI");
                    invGo.AddComponent<InventoryMenuUI>();
                }

                var inv = InventoryMenuUI.Instance;
                inv.InitializeDefaultInventory();
                int initialGold = inv.PlayerGold;

                // Spawn a small minion enemy
                var enemyGo = new GameObject("Test_SmallMinion");
                var enemy = enemyGo.AddComponent<SpireEnemy>();

                // Invoke private GrantInstantMinionLoot via reflection
                var method = typeof(SpireEnemy).GetMethod("GrantInstantMinionLoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                AssertNotNull(method, "GrantInstantMinionLoot method exists on SpireEnemy.");

                method.Invoke(enemy, null);

                AssertTrue(inv.PlayerGold > initialGold, $"Instant minion loot deposited gold immediately: {inv.PlayerGold} > {initialGold}");

                UnityEngine.Object.DestroyImmediate(enemyGo);
                Debug.Log("✅ [TEST 4 PASSED] Small enemy instant auto-vacuum loot deposit validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 4 FAILED] {ex.Message}");
            }

            // TEST 5: Monster Hunter Boss Carcass & Carving System
            try
            {
                Vector3 bossDeathPos = new Vector3(0f, 1f, 10f);
                ApexBossCarcass carcass = ApexBossCarcass.SpawnCarcass(bossDeathPos, Quaternion.identity, "Aethelgard, Core Sentinel");

                AssertNotNull(carcass, "ApexBossCarcass successfully spawned.");
                AssertTrue(carcass.carvesRemaining == 3, $"Initial carves remaining is 3, actual: {carcass.carvesRemaining}");

                // Simulate Carve 1
                var completeCarveMethod = typeof(ApexBossCarcass).GetMethod("CompleteCarve", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                AssertNotNull(completeCarveMethod, "CompleteCarve method exists on ApexBossCarcass.");

                completeCarveMethod.Invoke(carcass, null);
                AssertTrue(carcass.carvesRemaining == 2, $"Carve 1 decremented remaining carves to 2, actual: {carcass.carvesRemaining}");

                // Simulate Carve 2
                completeCarveMethod.Invoke(carcass, null);
                AssertTrue(carcass.carvesRemaining == 1, $"Carve 2 decremented remaining carves to 1, actual: {carcass.carvesRemaining}");

                // Simulate Carve 3
                completeCarveMethod.Invoke(carcass, null);
                AssertTrue(carcass.carvesRemaining == 0, $"Carve 3 decremented remaining carves to 0, actual: {carcass.carvesRemaining}");

                UnityEngine.Object.DestroyImmediate(carcass.gameObject);
                Debug.Log("✅ [TEST 5 PASSED] Monster Hunter 3-carve sequential carcass harvesting validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 5 FAILED] {ex.Message}");
            }

            // TEST 6: Monster Hunter Scattered Shiny Drops
            try
            {
                var shinyGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shinyGo.name = "Test_ShinyDrop";
                var shiny = shinyGo.AddComponent<ApexShinyLootDrop>();
                shiny.bonusGold = 100;
                shiny.displayName = "Test Prismatic Tear";

                int preGold = InventoryMenuUI.Instance != null ? InventoryMenuUI.Instance.PlayerGold : 0;
                shiny.CollectShiny();

                if (InventoryMenuUI.Instance != null)
                {
                    AssertTrue(InventoryMenuUI.Instance.PlayerGold >= preGold + 100, "Shiny drop collected gold bonus.");
                }

                Debug.Log("✅ [TEST 6 PASSED] Monster Hunter scattered shiny drop collection validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 6 FAILED] {ex.Message}");
            }

            // TEST 7: BossController Die() Spawns Carcass
            try
            {
                var bossGo = new GameObject("Test_BossController");
                var boss = bossGo.AddComponent<BossController>();
                boss.bossName = "Gargantuan Spire Arbiter";

                var dieMethod = typeof(BossController).GetMethod("Die", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                AssertNotNull(dieMethod, "BossController.Die method exists.");

                dieMethod.Invoke(boss, null);

                // Check that ApexBossCarcass active instance was spawned
                AssertNotNull(ApexBossCarcass.ActiveCarcass, "BossController.Die spawned active ApexBossCarcass.");

                if (ApexBossCarcass.ActiveCarcass != null)
                {
                    UnityEngine.Object.DestroyImmediate(ApexBossCarcass.ActiveCarcass.gameObject);
                }

                Debug.Log("✅ [TEST 7 PASSED] BossController death transition into Monster Hunter carveable carcass validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 7 FAILED] {ex.Message}");
            }

            Debug.Log("========================================================================");
            Debug.Log($"🎉 [VERIFICATION COMPLETE] {passed}/{total} Tests Passed!");
            Debug.Log("========================================================================");
        }

        private static void AssertNotNull(object obj, string message)
        {
            if (obj == null) throw new Exception($"Assertion Failed: Object was null. Detail: {message}");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new Exception($"Assertion Failed: Expected True. Detail: {message}");
        }
    }
}
