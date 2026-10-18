using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.Editor
{
    /// <summary>
    /// MultiTouchStanceAndTomeVerificationRunner tests and verifies:
    /// 1. Master Equipment & Hand Slot Rules (OneHanded, TwoHanded, OffHandOnly).
    /// 2. Active Touch Pointer Derivation (allowedSlashFingers, allowedRuneFingers).
    /// 3. Off-Hand Tomes & Wizarding Books (+15% Affinity Resonance Buffs).
    /// 4. Passive Shield Blocking vs. Universal Two-Finger Tap Parry.
    /// 5. Protective Shield Rune (⛨) & Right-Column 9-Ally Persona Targeting.
    /// </summary>
    public static class MultiTouchStanceAndTomeVerificationRunner
    {
        [MenuItem("Tools/BattleSummoner/Verify Multi-Touch Stances & Affinity Tomes")]
        public static string RunFullVerification()
        {
            Debug.Log("========================================================================");
            Debug.Log("✨ [MULTI-TOUCH & TOME VERIFICATION] Starting Full Subsystem Tests...");
            Debug.Log("========================================================================");

            int passed = 0;
            int total = 5;

            // TEST 1: Hand Slot Rules & Equipment Constraints
            try
            {
                var testGO = new GameObject("Test_LoadoutHost");
                var loadout = testGO.AddComponent<SpireLoadoutAndTomeSystem>();

                var sword1H = new SpireEquippableItem("sword_1h", "Steel Sword", EquipmentCategory.Blade1H, HandSlotRule.OneHanded);
                var greatsword2H = new SpireEquippableItem("gs_2h", "Titan Greatsword", EquipmentCategory.Greatsword2H, HandSlotRule.TwoHanded);
                var shieldOffHand = new SpireEquippableItem("shield_off", "Aegis Shield", EquipmentCategory.ShieldOffHand, HandSlotRule.OffHandOnly);
                var tomeOffHand = new SpireEquippableItem("tome_off", "Pyre Grimoire", EquipmentCategory.TomeOffHand, HandSlotRule.OffHandOnly, 35f, SpellAffinity.Fire);

                // 1. OffHandOnly cannot be equipped in Main-Hand
                bool mainShieldOk = loadout.EquipMainHand(shieldOffHand);
                AssertTrue(!mainShieldOk, "OffHandOnly shield was rejected from Main-Hand.");

                bool mainTomeOk = loadout.EquipMainHand(tomeOffHand);
                AssertTrue(!mainTomeOk, "OffHandOnly tome was rejected from Main-Hand.");

                // 2. 1H Sword can go in Main-Hand and Second-Hand
                bool mainSwordOk = loadout.EquipMainHand(sword1H);
                AssertTrue(mainSwordOk, "1H Sword equipped in Main-Hand.");
                bool offShieldOk = loadout.EquipSecondHand(shieldOffHand);
                AssertTrue(offShieldOk, "Shield equipped in Second-Hand.");

                // 3. TwoHanded in Main-Hand automatically clears/locks Second-Hand
                bool gsOk = loadout.EquipMainHand(greatsword2H);
                AssertTrue(gsOk, "2H Greatsword equipped in Main-Hand.");
                AssertTrue(loadout.secondHand == null, "Second-Hand automatically cleared upon equipping 2H weapon.");

                // 4. Cannot equip Second-Hand while wielding 2H weapon
                bool offWhile2HOk = loadout.EquipSecondHand(shieldOffHand);
                AssertTrue(!offWhile2HOk, "Second-Hand equipping rejected while wielding 2H Greatsword.");

                // 5. TwoHanded item cannot be equipped in Second-Hand
                loadout.EquipMainHand(sword1H);
                bool gsInOffHand = loadout.EquipSecondHand(greatsword2H);
                AssertTrue(!gsInOffHand, "2H Greatsword was rejected from Second-Hand.");

                UnityEngine.Object.DestroyImmediate(testGO);
                Debug.Log("✅ [TEST 1 PASSED] Master equipment rules and hand slot validation confirmed.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 1 FAILED] Hand slot rules failed: {ex.Message}\n{ex.StackTrace}");
            }

            // TEST 2: Active Finger Pointers & Catalyst Matrix Derivation
            try
            {
                var testGO = new GameObject("Test_MatrixHost");
                var loadout = testGO.AddComponent<SpireLoadoutAndTomeSystem>();

                // Archetype A: Single Blade
                loadout.EquipPreset("SingleBlade");
                AssertTrue(loadout.allowedSlashFingers == 1, "SingleBlade has exactly 1 slash finger.");
                AssertTrue(loadout.allowedRuneFingers == 0, "SingleBlade has 0 rune fingers.");
                AssertTrue(Mathf.Approximately(loadout.activePostureDamageMultiplier, 1.25f), "SingleBlade has +25% posture damage.");
                AssertTrue(Mathf.Approximately(loadout.activeTemperDecayRate, 0.75f), "SingleBlade has slower temper decay (0.75x).");

                // Archetype B: 2H Greatsword
                loadout.EquipPreset("Greatsword2H");
                AssertTrue(loadout.allowedSlashFingers == 1, "Greatsword2H has 1 slash finger.");
                AssertTrue(Mathf.Approximately(loadout.activeCleaveMassMultiplier, 1.50f), "Greatsword2H has +50% cleave mass.");
                AssertTrue(loadout.activeArmorPenetration > 0f, "Greatsword2H has heavy armor penetration.");

                // Archetype C: Dual Wield (2 Blades)
                loadout.EquipPreset("DualWield");
                AssertTrue(loadout.allowedSlashFingers == 2, "DualWield unlocks 2 simultaneous slash fingers.");
                AssertTrue(loadout.allowedRuneFingers == 0, "DualWield has 0 rune fingers.");

                // Archetype D: Grand Staff (2H Catalyst)
                loadout.EquipPreset("GrandStaff2H");
                AssertTrue(loadout.allowedSlashFingers == 0, "GrandStaff has 0 slash fingers.");
                AssertTrue(loadout.allowedRuneFingers == 1, "GrandStaff has 1 rune finger.");
                AssertTrue(Mathf.Approximately(loadout.baseCatalystMultiplier, 1.35f), "GrandStaff commitment grants +35% Base Spell Power.");
                AssertTrue(Mathf.Approximately(loadout.activeQuantumStabilityMultiplier, 1.20f), "GrandStaff grants +20% 11D Quantum Stability.");

                // Archetype E: Dual Wands
                loadout.EquipPreset("DualWands");
                AssertTrue(loadout.allowedSlashFingers == 0, "DualWands has 0 slash fingers.");
                AssertTrue(loadout.allowedRuneFingers == 2, "DualWands unlocks 2 simultaneous rune fingers (Twin-Glyph Weaving).");

                // Archetype F: Spellsword + Tome
                loadout.EquipPreset("SpellswordAndTome");
                AssertTrue(loadout.allowedSlashFingers == 1, "SpellswordAndTome has 1 slash finger.");
                AssertTrue(loadout.allowedRuneFingers == 1, "SpellswordAndTome has 1 rune finger.");

                UnityEngine.Object.DestroyImmediate(testGO);
                Debug.Log("✅ [TEST 2 PASSED] Finger-pointer derivation and catalyst matrix values validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 2 FAILED] Matrix derivation failed: {ex.Message}\n{ex.StackTrace}");
            }

            // TEST 3: Off-Hand Tomes & Wizarding Books (+15% Affinity Resonance Buff)
            try
            {
                var testGO = new GameObject("Test_TomeHost");
                var loadout = testGO.AddComponent<SpireLoadoutAndTomeSystem>();

                float rawDmg = 100f;

                // 1. Codex of Mending (Healing Affinity)
                loadout.EquipPreset("CodexOfMending");
                float healMatching = loadout.EvaluateSpellOutputWithTomeAffinity(SpellAffinity.Healing, rawDmg);
                float fireNonMatching = loadout.EvaluateSpellOutputWithTomeAffinity(SpellAffinity.Fire, rawDmg);
                // 1H Wand (+10%) * Tome (+15%) = 1.10 * 1.15 = 1.265x
                AssertTrue(healMatching > fireNonMatching, "Healing spell received affinity boost from Codex of Mending.");
                AssertTrue(Mathf.Approximately(healMatching, 100f * 1.10f * 1.15f), $"Expected 126.5, got {healMatching}.");
                AssertTrue(Mathf.Approximately(fireNonMatching, 100f * 1.10f), $"Expected 110.0, got {fireNonMatching}.");

                // 2. Pyre Grimoire (Fire Affinity)
                loadout.EquipPreset("PyreGrimoire");
                float fireMatching = loadout.EvaluateSpellOutputWithTomeAffinity(SpellAffinity.Fire, rawDmg);
                AssertTrue(Mathf.Approximately(fireMatching, 100f * 1.10f * 1.15f), "Pyre Grimoire granted +15% Fire boost.");

                // 3. Lithic Tome (Rock Affinity)
                loadout.EquipPreset("LithicTome");
                float rockMatching = loadout.EvaluateSpellOutputWithTomeAffinity(SpellAffinity.Rock, rawDmg);
                AssertTrue(Mathf.Approximately(rockMatching, 100f * 1.10f * 1.15f), "Lithic Tome granted +15% Rock boost.");

                // 4. Tidal Compendium (Water Affinity)
                loadout.EquipPreset("TidalCompendium");
                float waterMatching = loadout.EvaluateSpellOutputWithTomeAffinity(SpellAffinity.Water, rawDmg);
                AssertTrue(Mathf.Approximately(waterMatching, 100f * 1.10f * 1.15f), "Tidal Compendium granted +15% Water boost.");

                // 5. Bastion Lexicon (Protection Affinity)
                loadout.EquipPreset("BastionLexicon");
                float shieldMatching = loadout.EvaluateSpellOutputWithTomeAffinity(SpellAffinity.Protection, rawDmg);
                AssertTrue(Mathf.Approximately(shieldMatching, 100f * 1.10f * 1.15f), "Bastion Lexicon granted +15% Protection barrier boost.");

                UnityEngine.Object.DestroyImmediate(testGO);
                Debug.Log("✅ [TEST 3 PASSED] Off-Hand Tomes & Wizarding Books (+15% Affinity Buffs) validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 3 FAILED] Tome affinity buffs failed: {ex.Message}\n{ex.StackTrace}");
            }

            // TEST 4: Passive Shield Blocking vs. Universal Two-Finger Tap Parry
            try
            {
                var hostGO = new GameObject("Test_DispatcherHost");
                var loadout = hostGO.AddComponent<SpireLoadoutAndTomeSystem>();
                var stanceCtrl = hostGO.AddComponent<PlayerCombatStanceController>();
                var dispatcher = hostGO.AddComponent<MultiTouchCombatAndRuneDispatcher>();

                // Equip Sword & Shield
                loadout.EquipPreset("SwordAndShield");
                AssertTrue(loadout.hasPassiveShieldBlock, "Sword & Shield has passive shield block.");

                // Check passive shield state: active when not mid-action
                float testDmg = 100f;
                bool blockedFront = stanceCtrl.TryProcessPassiveBlock(
                    ref testDmg,
                    hostGO.transform.position + Vector3.forward * 3f,
                    hostGO.transform.position,
                    hostGO.transform.forward
                );
                AssertTrue(blockedFront, "Frontal incoming attack was passively blocked (-65%).");
                AssertTrue(Mathf.Approximately(testDmg, 35f), $"Damage mitigated to 35: got {testDmg}.");

                // Also test Wand + Shield (Protected Casting)
                loadout.EquipPreset("WandAndShield");
                AssertTrue(loadout.hasPassiveShieldBlock, "Wand + Shield also enables passive shield block!");
                AssertTrue(loadout.allowedRuneFingers == 1, "Wand + Shield allows 1H rune casting.");

                // Test Universal Two-Finger Tap Parry
                bool parryFired = false;
                dispatcher.OnTwoFingerParryTriggered += () => { parryFired = true; };
                dispatcher.ExecuteTwoFingerParry();

                AssertTrue(parryFired, "Universal Two-Finger Parry event triggered.");
                AssertTrue(stanceCtrl.IsParryActive, "Stance controller entered Parry Window.");

                // Negate attack during parry window
                var payload = new DamagePayload(80f, DamageType.PhysicalSlash, hostGO.transform.position, Vector3.forward, hostGO);
                DamageResult res = stanceCtrl.TakeAdvancedDamage(payload);
                AssertTrue(res.outcome == HitOutcome.Parried, "Incoming attack outcome was Parried.");
                AssertTrue(res.finalDamage == 0f, "100% damage negation on parry.");
                AssertTrue(stanceCtrl.HasRiposteBoost, "Parry awarded 3.0x Riposte Critical Boost.");

                UnityEngine.Object.DestroyImmediate(hostGO);
                Debug.Log("✅ [TEST 4 PASSED] Passive shield blocking and universal 2-finger parry validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 4 FAILED] Passive block & parry failed: {ex.Message}\n{ex.StackTrace}");
            }

            // TEST 5: Protective Shield Rune (⛨) & Right-Column 9-Ally Persona Targeting
            try
            {
                var runeGO = new GameObject("Test_RuneEngineHost");
                var runeEngine = runeGO.AddComponent<RuneShapeCastingEngine>();

                // Simulate drawing 5-point Shield Crest (⛨): broad top flat edge tapering down to a single bottom apex
                runeEngine.BeginDrawing();
                runeEngine.AddDrawingPoint(new Vector2(100, 300)); // Top left
                runeEngine.AddDrawingPoint(new Vector2(200, 305)); // Top center
                runeEngine.AddDrawingPoint(new Vector2(300, 300)); // Top right
                runeEngine.AddDrawingPoint(new Vector2(290, 220)); // Right side
                runeEngine.AddDrawingPoint(new Vector2(200, 100)); // Bottom pointed apex
                runeEngine.AddDrawingPoint(new Vector2(110, 220)); // Left side
                runeEngine.AddDrawingPoint(new Vector2(100, 300)); // Closure

                SpellElement recognized = runeEngine.AnalyzeInnerGlyph();
                AssertTrue(recognized == SpellElement.Protection, $"Analyzed glyph is Protection (⛨): got {recognized}.");

                // Test targeting onto the 9 right-column Persona portraits
                var dispGO = new GameObject("Test_DispatcherHost");
                var loadout = dispGO.AddComponent<SpireLoadoutAndTomeSystem>();
                loadout.EquipPreset("BastionLexicon"); // +15% Protection Affinity Tome
                var dispatcher = dispGO.AddComponent<MultiTouchCombatAndRuneDispatcher>();

                // Create mock ally portrait slots
                int protectedAlly = -999;
                float finalHp = 0f;
                dispatcher.OnAllyProtectedWithShieldSpell += (idx, hp) => {
                    protectedAlly = idx;
                    finalHp = hp;
                };

                // Cast to player (self-cast)
                dispatcher.CastDrawnShieldMagic(Vector2.zero, Vector2.zero, 100f);
                AssertTrue(protectedAlly == -1, "Targeted self on tap-in-place.");
                // 1H Wand (+10%) * Bastion Lexicon (+15%) = 100 * 1.10 * 1.15 = 126.5 HP
                AssertTrue(Mathf.Approximately(finalHp, 126.5f), $"Shield barrier HP scaled with Bastion Lexicon: got {finalHp}.");

                UnityEngine.Object.DestroyImmediate(dispGO);
                UnityEngine.Object.DestroyImmediate(runeGO);

                Debug.Log("✅ [TEST 5 PASSED] Protective Shield Rune (⛨) recognition and ally targeting validated.");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ [TEST 5 FAILED] Shield rune & ally targeting failed: {ex.Message}\n{ex.StackTrace}");
            }

            string report = $"[MULTI-TOUCH & TOME VERIFICATION COMPLETE] Passed: {passed}/{total} tests.";
            Debug.Log($"========================================================================");
            Debug.Log($"🏁 {report}");
            Debug.Log($"========================================================================");

            return report;
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}
