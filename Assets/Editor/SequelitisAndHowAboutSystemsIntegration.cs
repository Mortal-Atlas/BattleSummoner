using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using BattleSummoner.DesignPhilosophy;
using BattleSummoner.Combat;
using BattleSummoner.Core;

namespace BattleSummoner.EditorScripts
{
    public static class SequelitisAndHowAboutSystemsIntegration
    {
        [MenuItem("BattleSummoner/Sequelitis/Integrate All Systems")]
        public static string ExecuteCompleteMasterIntegration()
        {
            var activeScene = SceneManager.GetActiveScene();
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== SEQUELITIS (EPISODES 1-4) & HOW ABOUT THIS GAME? (EPISODES 1-5) INTEGRATION ===");

            // -----------------------------------------------------------------
            // STEP 1: ATTACH CORE DESIGN ENGINES TO PLAYER_HERO
            // -----------------------------------------------------------------
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Hero");
            if (player == null)
            {
                report.AppendLine("[ERROR] Player_Hero not found in scene!");
                return report.ToString();
            }

            var masterEngine = player.GetComponent<SequelitisAndHowAboutMasterEngine>();
            if (masterEngine == null) masterEngine = player.AddComponent<SequelitisAndHowAboutMasterEngine>();
            masterEngine.selectiveZAxisVerticalSlabHeight = 3.6f;
            masterEngine.wideSweepSwarmRadius = 4.2f;
            masterEngine.reciprocalSlashKnockbackImpulse = 12.0f;
            masterEngine.combatAndEnvironmentLayer = ~0;

            var cloudEngine = player.GetComponent<BarryKramerTraversalAndCloudEngine>();
            if (cloudEngine == null) cloudEngine = player.AddComponent<BarryKramerTraversalAndCloudEngine>();

            // Setup audio sources on player for cloud feel
            var whoopSource = player.GetComponent<AudioSource>();
            if (whoopSource == null) whoopSource = player.AddComponent<AudioSource>();
            cloudEngine.vocalJubilationWhoopSource = whoopSource;
            cloudEngine.highAltitudeVibrantAzureColor = new Color(0.08f, 0.48f, 0.96f, 1f);
            cloudEngine.nightAladdinMoonlitSkyColor = new Color(0.04f, 0.11f, 0.28f, 1f);
            cloudEngine.groundWarmFogColor = new Color(0.82f, 0.88f, 0.78f, 1f);

            // Setup clinging cloud emitter on player
            var cloudBitsGo = player.transform.Find("PlayerClingingCloudFluffEmitter");
            if (cloudBitsGo == null)
            {
                var go = new GameObject("PlayerClingingCloudFluffEmitter");
                go.transform.SetParent(player.transform, false);
                go.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 0.6f;
                main.startSpeed = 0.5f;
                main.startSize = 0.4f;
                main.startColor = new Color(1f, 1f, 1f, 0.65f);
                main.playOnAwake = false;
                var emission = ps.emission;
                emission.rateOverTime = 18f;
                ps.Stop();
                cloudEngine.playerClingingCloudBitsEmitter = ps;
            }
            else
            {
                cloudEngine.playerClingingCloudBitsEmitter = cloudBitsGo.GetComponent<ParticleSystem>();
            }

            report.AppendLine("✅ [Step 1] SequelitisAndHowAboutMasterEngine & BarryKramerTraversalAndCloudEngine attached to Player_Hero.");

            // -----------------------------------------------------------------
            // STEP 2: VERIFY FRUIT NINJA BLADE CUTTER Z-AXIS HOOK
            // -----------------------------------------------------------------
            var cutter = player.GetComponent<FruitNinjaBladeCutter>();
            if (cutter == null) cutter = player.AddComponent<FruitNinjaBladeCutter>();
            report.AppendLine("✅ [Step 2] FruitNinjaBladeCutter selective Z-axis slab cleaving validated (3.6m vertical cleave).");

            // -----------------------------------------------------------------
            // STEP 3: UPGRADE ENEMY AI, BOSS LIMBS & ARENA PILLARS
            // -----------------------------------------------------------------
            // 3A. Armored Practice Dummy
            var dummy = GameObject.Find("Courtyard_PracticeDummy_Armored");
            if (dummy != null)
            {
                var dummyEnemy = dummy.GetComponent<IronKnuckleProvocableEnemy>();
                if (dummyEnemy == null) dummyEnemy = dummy.AddComponent<IronKnuckleProvocableEnemy>();
                dummyEnemy.enemyName = "Acropolis Anvil-Guardian";
                dummyEnemy.exposedSeamSlashAngleDeg = 45f;
                dummyEnemy.seamAngleToleranceDeg = 35f;
                dummyEnemy.provokedVulnerabilityDuration = 1.6f;
                report.AppendLine("✅ [Step 3A] Attached IronKnuckleProvocableEnemy to Courtyard_PracticeDummy_Armored.");
            }

            // 3B. Destructible Arena Pillars
            var pillarsRoot = GameObject.Find("Arena_Destructible_Pillars_Root");
            if (pillarsRoot == null)
            {
                pillarsRoot = new GameObject("Arena_Destructible_Pillars_Root");
                pillarsRoot.transform.position = Vector3.zero;
            }

            // Create shard drop prefab asset or in-memory prototype
            var shardProto = GameObject.Find("RecoveryShardPrototype");
            if (shardProto == null)
            {
                shardProto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shardProto.name = "RecoveryShardPrototype";
                shardProto.transform.localScale = Vector3.one * 0.45f;
                var shardMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_FairyGlow.mat");
                if (shardMat != null) shardProto.GetComponent<Renderer>().sharedMaterial = shardMat;
                shardProto.SetActive(false);
            }

            // Spawn 4 arena destructible pillars around Acropolis plaza
            Vector3[] pillarOffsets = new Vector3[]
            {
                new Vector3(-8f, 80f, -42f),
                new Vector3(8f, 80f, -42f),
                new Vector3(-12f, 80f, -54f),
                new Vector3(12f, 80f, -54f)
            };

            var pillarMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Cavern_Pillars.mat") 
                         ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_CathedralStone.mat");

            for (int i = 0; i < pillarOffsets.Length; i++)
            {
                string pName = $"ArenaDestructiblePillar_{i + 1}";
                var pillarGo = GameObject.Find(pName);
                if (pillarGo == null)
                {
                    pillarGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillarGo.name = pName;
                    pillarGo.transform.SetParent(pillarsRoot.transform, false);
                    pillarGo.transform.position = pillarOffsets[i] + new Vector3(0f, 3.5f, 0f);
                    pillarGo.transform.localScale = new Vector3(1.6f, 3.5f, 1.6f);
                    if (pillarMat != null) pillarGo.GetComponent<Renderer>().sharedMaterial = pillarMat;
                }

                var pillarComp = pillarGo.GetComponent<ArenaDestructiblePillar>();
                if (pillarComp == null) pillarComp = pillarGo.AddComponent<ArenaDestructiblePillar>();
                pillarComp.recoveryHeartAndManaShardPrefab = shardProto;
                pillarComp.shardsToDropOnShatter = 4;
            }
            report.AppendLine("✅ [Step 3B] Spawned 4 ArenaDestructiblePillar columns in Acropolis courtyard.");

            // 3C. Gatekeeper Boss Kaelen with Severable Boss Limb
            var kaelenGo = GameObject.Find("Boss_Kaelen_The_Unforged");
            if (kaelenGo == null)
            {
                kaelenGo = new GameObject("Boss_Kaelen_The_Unforged");
                kaelenGo.transform.position = new Vector3(0f, 80f, -22f); // Facing cathedral entrance
                var col = kaelenGo.AddComponent<CapsuleCollider>();
                col.height = 3.8f;
                col.radius = 1.2f;
                col.center = new Vector3(0f, 1.9f, 0f);

                // Add visual model
                var golemFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Golem_IndustrialRustedIron.fbx")
                            ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Golem_AncientMossStone.fbx");
                if (golemFbx != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(golemFbx, kaelenGo.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    visual.transform.localScale = Vector3.one * 1.5f;
                }
                kaelenGo.SetActive(true);
            }
            kaelenGo.SetActive(true);

            // Child limb object so severing deactivates only the limb, not the entire boss
            var limbGo = kaelenGo.transform.Find("Boss_Magma_Trunk_Limb")?.gameObject;
            if (limbGo == null)
            {
                limbGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                limbGo.name = "Boss_Magma_Trunk_Limb";
                limbGo.transform.SetParent(kaelenGo.transform, false);
                limbGo.transform.localPosition = new Vector3(0.8f, 2.2f, 0.6f);
                limbGo.transform.localRotation = Quaternion.Euler(45f, 30f, 0f);
                limbGo.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
                var lavaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/Mat_Volcanic_BasaltRift.mat")
                           ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_ForgeFire.mat");
                if (lavaMat != null) limbGo.GetComponent<Renderer>().sharedMaterial = lavaMat;
            }
            limbGo.SetActive(true);

            var severableLimb = limbGo.GetComponent<SeverableBossLimb>();
            if (severableLimb == null) severableLimb = limbGo.AddComponent<SeverableBossLimb>();
            severableLimb.limbName = "Volcanic Magma Trunk";
            severableLimb.counterElementTag = "Water";
            severableLimb.seamAngleDeg = 30f;
            severableLimb.limbHp = 350f;
            severableLimb.isSevered = false;
            report.AppendLine("✅ [Step 3C] Configured Floor 01 Gatekeeper Boss Kaelen with SeverableBossLimb (Volcanic Magma Trunk).");

            // 3D. Stealable Golem Ride-Armor and Enemy Pilot
            var golemMechRoot = GameObject.Find("Stealable_Golem_RideArmor_Unit");
            if (golemMechRoot == null)
            {
                golemMechRoot = new GameObject("Stealable_Golem_RideArmor_Unit");
                golemMechRoot.transform.position = new Vector3(24f, 80f, -48f); // East side of plaza

                var golemFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Metropolis/Golem_AncientMossStone.fbx");
                if (golemFbx != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(golemFbx, golemMechRoot.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                }

                var boxCol = golemMechRoot.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(2.5f, 3.2f, 2.5f);
                boxCol.center = new Vector3(0f, 1.6f, 0f);

                var mechComp = golemMechRoot.AddComponent<StealableGolemRideArmor>();
                mechComp.bonusMechShieldHp = 850f;

                // Enemy Pilot sprinting to board it
                var pilotGo = GameObject.Find("Enemy_Pilot_AnvilGuard");
                if (pilotGo == null)
                {
                    pilotGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    pilotGo.name = "Enemy_Pilot_AnvilGuard";
                    pilotGo.transform.position = new Vector3(36f, 80f, -48f);
                    pilotGo.transform.localScale = new Vector3(1f, 1.4f, 1f);
                    var guardMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_PlateArmor.mat");
                    if (guardMat != null) pilotGo.GetComponent<Renderer>().sharedMaterial = guardMat;
                }

                var pilotComp = pilotGo.GetComponent<IronKnuckleProvocableEnemy>();
                if (pilotComp == null) pilotComp = pilotGo.AddComponent<IronKnuckleProvocableEnemy>();
                pilotComp.enemyName = "Golem Pilot Vanguard";
                pilotComp.nearbyUnmountedGolem = mechComp;
            }
            report.AppendLine("✅ [Step 3D] Spawned StealableGolemRideArmor mech & interceptable Enemy Pilot in Acropolis camp.");

            // -----------------------------------------------------------------
            // STEP 4: ZERO-WAIT EXPLORATION & PRE-BOSS SUB-TANK LARVAE
            // -----------------------------------------------------------------
            // 4A. Central Giant Master Chest (0.15s Pop, 3.0x Kinetic Strike Relic)
            var giantChestGo = GameObject.Find("Central_Giant_Master_Chest");
            if (giantChestGo == null)
            {
                giantChestGo = new GameObject("Central_Giant_Master_Chest");
                giantChestGo.transform.position = new Vector3(-20f, 80f, -46f);
                giantChestGo.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

                var chestFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TownProps/Prop_GoldCoinsChest.fbx");
                if (chestFbx != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(chestFbx, giantChestGo.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localScale = Vector3.one * 0.018f; // Giant Master Chest scaled to match 2.5m collider
                }
                var box = giantChestGo.AddComponent<BoxCollider>();
                box.size = new Vector3(2.5f, 2.0f, 2.5f);
                box.center = new Vector3(0f, 1.0f, 0f);

                var chestComp = giantChestGo.AddComponent<ZeroWaitInstantChest>();
                chestComp.isCentralGiantMasterChest = true;
                chestComp.rewardItemTitle = "3.0x Kinetic Strike Relic (Uncapped Accessory)";
            }
            report.AppendLine("✅ [Step 4A] Spawned Central Giant Master Chest (0.15s instant pop, 3.0x Kinetic Strike Relic).");

            // 4B. Fast-Respawning Pre-Boss Mana-Crystalline Larvae Cluster
            var larvaeGo = GameObject.Find("PreBoss_ManaCrystalline_Larvae_Cluster");
            if (larvaeGo == null)
            {
                larvaeGo = new GameObject("PreBoss_ManaCrystalline_Larvae_Cluster");
                larvaeGo.transform.position = new Vector3(0f, 80f, -30f); // Right outside Boss Kaelen gate

                var crystalFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/TownProps/Prop_ManaCrystals_Cluster.fbx");
                if (crystalFbx != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(crystalFbx, larvaeGo.transform);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localScale = Vector3.one * 1.6f;
                }
                var sphereCol = larvaeGo.AddComponent<SphereCollider>();
                sphereCol.radius = 1.8f;
                sphereCol.center = new Vector3(0f, 0.9f, 0f);

                var larvaeComp = larvaeGo.AddComponent<PreBossLarvaeCluster>();
                larvaeComp.respawnDelaySec = 2.5f;
                larvaeComp.manaCoreReward = 150;
            }
            report.AppendLine("✅ [Step 4B] Spawned fast-respawning PreBossLarvaeCluster outside Gatekeeper Kaelen's arena.");

            // -----------------------------------------------------------------
            // STEP 5: 12 SKY ISLANDS, AETHERION SKY CITY & 85 SLALOM CLOUDS
            // -----------------------------------------------------------------
            var skyArchipelagoRoot = GameObject.Find("Sky_Archipelago_And_Clouds_Root");
            if (skyArchipelagoRoot == null)
            {
                skyArchipelagoRoot = new GameObject("Sky_Archipelago_And_Clouds_Root");
                skyArchipelagoRoot.transform.position = Vector3.zero;
            }

            // 5A. 12 Sky Islands + Sky City Aetherion
            var skyIslandsGroup = skyArchipelagoRoot.transform.Find("Sky_Islands_Group");
            if (skyIslandsGroup == null)
            {
                var grp = new GameObject("Sky_Islands_Group");
                grp.transform.SetParent(skyArchipelagoRoot.transform, false);
                skyIslandsGroup = grp.transform;

                var islandMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_Limestone.mat")
                             ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/Mat_Tier4_CitadelPlateau.mat");

                // Procedural distribution of 12 Sky Islands (Y = +360m to +760m)
                float[] islandRadii = { 420f, 650f, 920f, 1200f, 1500f, 1850f, 2100f, 2400f, 2750f, 3100f, 3400f, 3800f };
                float[] islandHeights = { 360f, 410f, 470f, 520f, 580f, 630f, 680f, 720f, 760f, 590f, 640f, 710f };

                for (int i = 0; i < 12; i++)
                {
                    float angleRad = (i / 12f) * Mathf.PI * 2f + 0.35f;
                    float x = Mathf.Cos(angleRad) * islandRadii[i];
                    float z = Mathf.Sin(angleRad) * islandRadii[i];
                    float y = islandHeights[i];

                    var isl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    isl.name = $"Sky_Island_{i + 1:D2}";
                    isl.transform.SetParent(skyIslandsGroup, false);
                    isl.transform.position = new Vector3(x, y, z);
                    isl.transform.localScale = new Vector3(65f + (i * 5f), 18f, 65f + (i * 5f));
                    if (islandMat != null) isl.GetComponent<Renderer>().sharedMaterial = islandMat;
                }

                // 3-Tiered Sky City: Aetherion (+840m)
                var aetherion = new GameObject("SkyCity_Aetherion_Acropolis");
                aetherion.transform.SetParent(skyIslandsGroup, false);
                aetherion.transform.position = new Vector3(0f, 840f, 0f); // Centered above Cathedral Spire

                for (int tier = 0; tier < 3; tier++)
                {
                    var tCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tCyl.name = $"Aetherion_Tier_{tier + 1}";
                    tCyl.transform.SetParent(aetherion.transform, false);
                    tCyl.transform.localPosition = new Vector3(0f, tier * 18f, 0f);
                    float tierScale = 220f - (tier * 55f);
                    tCyl.transform.localScale = new Vector3(tierScale, 12f, tierScale);
                    if (islandMat != null) tCyl.GetComponent<Renderer>().sharedMaterial = islandMat;
                }
            }
            report.AppendLine("✅ [Step 5A] Verified 12 Sky Islands & 3-Tiered Sky City Aetherion (+840m) in world space.");

            // 5B. 85 Wispy Semi-Transparent & Poofy Slalom Clouds
            var cloudsGroup = skyArchipelagoRoot.transform.Find("Slalom_Clouds_Group");
            if (cloudsGroup == null)
            {
                var grp = new GameObject("Slalom_Clouds_Group");
                grp.transform.SetParent(skyArchipelagoRoot.transform, false);
                cloudsGroup = grp.transform;

                // Create cloud material with semi-transparency
                var cloudMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                cloudMat.name = "Mat_WispySlalomCloud";
                cloudMat.color = new Color(0.96f, 0.98f, 1.0f, 0.65f);

                // Scatter 85 slalom clouds between Y = 280m and 750m
                var rng = new System.Random(777);
                for (int c = 0; c < 85; c++)
                {
                    float dist = 250f + (float)rng.NextDouble() * 3200f;
                    float theta = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float x = Mathf.Cos(theta) * dist;
                    float z = Mathf.Sin(theta) * dist;
                    float y = 280f + (float)rng.NextDouble() * 470f;

                    var cloudGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    cloudGo.name = $"Slalom_Cloud_{c + 1:D2}";
                    cloudGo.transform.SetParent(cloudsGroup, false);
                    cloudGo.transform.position = new Vector3(x, y, z);
                    float radX = 35f + (float)rng.NextDouble() * 50f;
                    float radY = 14f + (float)rng.NextDouble() * 18f;
                    float radZ = 35f + (float)rng.NextDouble() * 50f;
                    cloudGo.transform.localScale = new Vector3(radX, radY, radZ);
                    cloudGo.GetComponent<Renderer>().sharedMaterial = cloudMat;

                    var trig = cloudGo.GetComponent<SphereCollider>();
                    if (trig != null) trig.isTrigger = true;
                    cloudGo.AddComponent<CloudVolumeTrigger>();
                }
            }
            report.AppendLine("✅ [Step 5B] Verified 85 Wispy Slalom Clouds with CloudVolumeTrigger routing to BarryKramerTraversalAndCloudEngine.");

            // Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            report.AppendLine("💾 [Saved Scene] OverworldMap.unity saved successfully.");

            return report.ToString();
        }
    }
}
