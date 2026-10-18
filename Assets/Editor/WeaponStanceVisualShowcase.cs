using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Combat;
using BattleSummoner.UI;
using BattleSummoner.Quests;

namespace BattleSummoner.Editor
{
    public static class WeaponStanceVisualShowcase
    {
        private static void CleanAndFrame(PlayerCombatStanceController stance)
        {
            // 1. Hide modal task boards
            var boards = UnityEngine.Object.FindObjectsByType<GuildTaskBoardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < boards.Length; i++)
            {
                boards[i].gameObject.SetActive(false);
            }

            // 2. Clean temporary VFX / floating text objects
            var cfm = UnityEngine.Object.FindAnyObjectByType<CombatFeedbackManager>(FindObjectsInactive.Include);
            if (cfm != null)
            {
                for (int i = cfm.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(cfm.transform.GetChild(i).gameObject);
                }
            }

            var allTMP = UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTMP)
            {
                if (t != null && (t.name.Contains("DamageText") || t.text.Contains("PARRY") || t.text.Contains("BLOCK") || t.text.Contains("Riposte")))
                {
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }

            var objs = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in objs)
            {
                if (go != null && (go.name.StartsWith("Showcase_") || 
                                   go.name.StartsWith("ShieldBlock") || 
                                   go.name.StartsWith("PerfectParry") || 
                                   go.name.StartsWith("ParryStance") ||
                                   go.name == "WarriorZeldaCrescentSlashArc" ||
                                   go.name == "SwordHitSpark"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // 3. Attach Anime Starter Male model to Player_Hero and disable green capsule renderer
            var hero = stance.gameObject;
            var capsuleRend = hero.GetComponent<MeshRenderer>();
            if (capsuleRend != null) capsuleRend.enabled = false;

            Transform visualChild = hero.transform.Find("VisualModel_AnimeStarter");
            if (visualChild == null)
            {
                GameObject starterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Starter_Male.prefab");
                if (starterPrefab == null)
                {
                    starterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Character_Base_Male_Genshin.prefab");
                }

                if (starterPrefab != null)
                {
                    GameObject model = UnityEngine.Object.Instantiate(starterPrefab, hero.transform);
                    model.name = "VisualModel_AnimeStarter";
                    model.transform.localPosition = new Vector3(0, 0.45f, 0);
                    model.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Face camera
                    model.transform.localScale = Vector3.one;
                }
            }
            else
            {
                visualChild.localPosition = new Vector3(0, 0.45f, 0);
                visualChild.localRotation = Quaternion.Euler(0, 180f, 0); // Face camera
            }

            // 4. Smoothly frame camera in front of Player_Hero
            Camera cam = Camera.main;
            if (cam != null)
            {
                var arpgCam = cam.GetComponent<PortraitARPGCamera>();
                if (arpgCam != null)
                {
                    arpgCam.yawAngle = 180f; // View character from the front!
                    arpgCam.pitchAngle = 12f;
                    arpgCam.cameraDistance = 4.4f;
                    arpgCam.targetFocusHeight = 0.95f;
                }
                else
                {
                    Vector3 playerPos = stance.transform.position;
                    cam.transform.position = playerPos + new Vector3(0f, 1.45f, 3.8f);
                    cam.transform.LookAt(playerPos + Vector3.up * 0.95f);
                }
            }
        }

        private static PlayerCombatStanceController GetOrCreateStanceController()
        {
            var hero = GameObject.Find("Player_Hero");
            if (hero == null)
            {
                hero = GameObject.FindWithTag("Player");
            }
            if (hero == null)
            {
                hero = new GameObject("Player_Hero");
                hero.tag = "Player";
                hero.transform.position = new Vector3(0, 0, 0);
            }

            var stance = hero.GetComponent<PlayerCombatStanceController>();
            if (stance == null)
            {
                stance = hero.AddComponent<PlayerCombatStanceController>();
            }

            return stance;
        }

        [MenuItem("Tools/BattleSummoner/Showcase Single Sword Stance")]
        public static void ShowcaseSingleSword()
        {
            var stance = GetOrCreateStanceController();
            CleanAndFrame(stance);
            stance.SetStance(WeaponCombatStance.SingleSword);

            Vector3 centerPos = stance.transform.position + Vector3.forward * 0.8f + Vector3.up * 1.05f;
            Vector3 camDir = (Camera.main.transform.position - stance.transform.position).normalized;

            // Single swipe finger blade: 1 clean cyan crescent wave
            WarriorSwordSlashVFX.SpawnSlashWave(
                centerPos,
                camDir,
                -15f,
                1300f,
                new Color(0.2f, 0.9f, 1.0f)
            );
            WarriorSwordSlashVFX.SpawnHitSpark(centerPos, camDir);

            Debug.Log("📸 [SHOWCASE] Single Sword stance ready (1 swipe finger blade).");
        }

        [MenuItem("Tools/BattleSummoner/Showcase Dual Blades Stance")]
        public static void ShowcaseDualBlades()
        {
            var stance = GetOrCreateStanceController();
            CleanAndFrame(stance);
            stance.SetStance(WeaponCombatStance.DualBlades);

            // Spawn twin simultaneous slash waves in front of player
            Vector3 centerPos = stance.transform.position + Vector3.forward * 0.85f + Vector3.up * 1.1f;
            Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            Vector3 camDir = (Camera.main.transform.position - stance.transform.position).normalized;

            // 1. Primary Blade Slash (Cyan Katana Arc)
            WarriorSwordSlashVFX.SpawnSlashWave(
                centerPos - camRight * 0.35f,
                camDir,
                -25f,
                1450f,
                new Color(0.15f, 0.90f, 1.0f)
            );

            // 2. Secondary Blade Slash (Amber Gold Arc)
            WarriorSwordSlashVFX.SpawnSlashWave(
                centerPos + camRight * 0.35f,
                camDir,
                30f,
                1400f,
                new Color(1.0f, 0.70f, 0.15f)
            );

            WarriorSwordSlashVFX.SpawnHitSpark(centerPos, camDir);
            Debug.Log("📸 [SHOWCASE] Dual Blades stance ready (2 simultaneous slashes: Cyan & Amber).");
        }

        [MenuItem("Tools/BattleSummoner/Showcase Sword & Shield Passive Block")]
        public static void ShowcaseSwordAndShield()
        {
            var stance = GetOrCreateStanceController();
            CleanAndFrame(stance);
            stance.SetStance(WeaponCombatStance.SwordAndShield);

            Vector3 centerPos = stance.transform.position + Vector3.forward * 0.75f + Vector3.up * 1.05f;
            Vector3 camDir = (Camera.main.transform.position - stance.transform.position).normalized;

            // Spawn glowing hexagonal barrier quad in front of shield
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Quad);
            barrier.name = "Showcase_ShieldBarrier";
            barrier.transform.position = centerPos;
            barrier.transform.rotation = Quaternion.LookRotation(camDir, Vector3.up);
            barrier.transform.localScale = Vector3.one * 1.7f;
            UnityEngine.Object.DestroyImmediate(barrier.GetComponent<Collider>());

            var rend = barrier.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.25f, 0.70f, 1.0f, 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.20f, 0.65f, 1.0f) * 4.5f);
                rend.sharedMaterial = mat;
            }

            WarriorSwordSlashVFX.SpawnHitSpark(centerPos, camDir);
            Debug.Log("📸 [SHOWCASE] Sword & Shield Passive Block ready (-65% frontal barrier).");
        }

        [MenuItem("Tools/BattleSummoner/Showcase Two-Finger Parry")]
        public static void ShowcaseTwoFingerParry()
        {
            var stance = GetOrCreateStanceController();
            CleanAndFrame(stance);
            stance.ExecuteParry();

            Vector3 centerPos = stance.transform.position + Vector3.forward * 0.8f + Vector3.up * 1.15f;
            Vector3 camDir = (Camera.main.transform.position - stance.transform.position).normalized;

            // Golden starburst deflection
            GameObject starburst = new GameObject("Showcase_ParryStarburst");
            starburst.transform.position = centerPos;

            for (int i = 0; i < 6; i++)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = $"Showcase_Spoke_{i}";
                beam.transform.SetParent(starburst.transform, false);
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, i * 30f);
                beam.transform.localScale = new Vector3(0.07f, 2.4f, 0.07f);
                UnityEngine.Object.DestroyImmediate(beam.GetComponent<Collider>());

                var rend = beam.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = new Color(1.0f, 0.95f, 0.55f, 0.95f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.82f, 0.18f) * 6.0f);
                    rend.sharedMaterial = mat;
                }
            }

            // Radial golden deflection ring on floor
            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.name = "Showcase_ParryRing";
            ringObj.transform.position = stance.transform.position + Vector3.up * 0.05f;
            ringObj.transform.localScale = new Vector3(2.8f, 0.02f, 2.8f);
            UnityEngine.Object.DestroyImmediate(ringObj.GetComponent<Collider>());
            var rRend = ringObj.GetComponent<Renderer>();
            if (rRend != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(1.0f, 0.85f, 0.25f, 0.70f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.80f, 0.20f) * 4.0f);
                rRend.sharedMaterial = mat;
            }

            WarriorSwordSlashVFX.SpawnHitSpark(centerPos, camDir);
            Debug.Log("📸 [SHOWCASE] Two-Finger Parry ready (Golden Starburst & Deflection Ring).");
        }

        [MenuItem("Tools/BattleSummoner/Showcase Wand & Shield Protected Casting")]
        public static void ShowcaseWandAndShieldProtectedCasting()
        {
            var stance = GetOrCreateStanceController();
            CleanAndFrame(stance);

            var loadout = stance.GetComponent<SpireLoadoutAndTomeSystem>() ?? SpireLoadoutAndTomeSystem.Instance;
            if (loadout != null)
            {
                loadout.EquipPreset("WandAndShield");
            }

            Vector3 toCam = Camera.main != null ? (Camera.main.transform.position - stance.transform.position).normalized : Vector3.back;
            toCam.y = 0f;
            toCam.Normalize();

            Vector3 centerPos = stance.transform.position + toCam * 0.75f + Vector3.up * 1.05f;
            Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;

            // 1. Passive Shield Barrier Quad on off-hand side
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Quad);
            barrier.name = "Showcase_ShieldBarrier";
            barrier.transform.position = centerPos - camRight * 0.45f;
            barrier.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
            barrier.transform.localScale = Vector3.one * 1.6f;
            UnityEngine.Object.DestroyImmediate(barrier.GetComponent<Collider>());

            var bRend = barrier.GetComponent<Renderer>();
            if (bRend != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.20f, 0.65f, 1.0f, 0.85f);
                bRend.sharedMaterial = mat;
            }

            // 2. Arcane Rune Circle on main-hand wand side
            GameObject runeRing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            runeRing.name = "Showcase_RuneRing";
            runeRing.transform.position = centerPos + camRight * 0.45f + Vector3.up * 0.1f;
            runeRing.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
            runeRing.transform.localScale = Vector3.one * 1.3f;
            UnityEngine.Object.DestroyImmediate(runeRing.GetComponent<Collider>());

            var rRend = runeRing.GetComponent<Renderer>();
            if (rRend != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.35f, 0.85f, 1.0f, 0.80f);
                rRend.sharedMaterial = mat;
            }

            // 3. Floating 3D Text
            GameObject txtGo = new GameObject("Showcase_Text");
            txtGo.transform.position = stance.transform.position + Vector3.up * 2.25f + toCam * 0.35f;
            txtGo.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
            txtGo.transform.localScale = Vector3.one;
            var tmp = txtGo.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "<color=#60A5FA><b>[PROTECTED CASTING]</b></color>\n<size=75%><color=#93C5FD>1H Wand (1-Finger Rune) + Passive Shield Block (-65%)</color></size>";
            tmp.fontSize = 2.4f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            Debug.Log("📸 [SHOWCASE] Wand & Shield Protected Casting ready.");
        }

        [MenuItem("Tools/BattleSummoner/Showcase Protective Shield Rune Cast")]
        public static void ShowcaseProtectiveShieldRuneCast()
        {
            var stance = GetOrCreateStanceController();
            CleanAndFrame(stance);

            var loadout = stance.GetComponent<SpireLoadoutAndTomeSystem>() ?? SpireLoadoutAndTomeSystem.Instance;
            if (loadout != null)
            {
                loadout.EquipPreset("BastionLexicon");
            }

            Vector3 toCam = Camera.main != null ? (Camera.main.transform.position - stance.transform.position).normalized : Vector3.back;
            toCam.y = 0f;
            toCam.Normalize();

            Vector3 centerPos = stance.transform.position + toCam * 0.8f + Vector3.up * 1.15f;
            Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;

            // 1. Shimmering Outer Magic Containment Ring Quad
            GameObject outerCircle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            outerCircle.name = "Showcase_ShieldRing";
            outerCircle.transform.position = centerPos;
            outerCircle.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
            outerCircle.transform.localScale = Vector3.one * 1.8f;
            UnityEngine.Object.DestroyImmediate(outerCircle.GetComponent<Collider>());

            var ocRend = outerCircle.GetComponent<Renderer>();
            if (ocRend != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.25f, 0.65f, 1.0f, 0.60f);
                ocRend.sharedMaterial = mat;
            }

            // 2. Shield Crest Rune Visual Quad
            GameObject crestQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            crestQuad.name = "Showcase_ShieldCrest";
            crestQuad.transform.position = centerPos + toCam * 0.05f;
            crestQuad.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
            crestQuad.transform.localScale = new Vector3(1.1f, 1.25f, 1.0f);
            UnityEngine.Object.DestroyImmediate(crestQuad.GetComponent<Collider>());

            var cRend = crestQuad.GetComponent<Renderer>();
            if (cRend != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = new Color(0.55f, 0.90f, 1.0f, 0.95f);
                cRend.sharedMaterial = mat;
            }

            // 3. Floating 3D Text
            GameObject txtGo = new GameObject("Showcase_Text");
            txtGo.transform.position = stance.transform.position + Vector3.up * 2.30f + toCam * 0.35f;
            txtGo.transform.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
            txtGo.transform.localScale = Vector3.one;
            var tmp = txtGo.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "<color=#59BFFF><b>[PROTECTIVE WARD CAST]</b></color>\n<size=75%><color=#BAE6FD>Bastion Lexicon +15% Boost | Targeted to Ally #1 (+127 HP)</color></size>";
            tmp.fontSize = 2.3f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            Debug.Log("📸 [SHOWCASE] Protective Shield Rune Cast & Bastion Lexicon targeting ready.");
        }
    }
}
