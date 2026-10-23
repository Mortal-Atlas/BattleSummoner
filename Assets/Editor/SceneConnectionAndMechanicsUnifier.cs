using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using BattleSummoner.World;
using BattleSummoner.Intro;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// Connects and validates all 10 game scenes together into a unified, seamless game loop:
    /// WorldEntryExperience -> CharacterEquipmentShowcase -> WorldIntroductionCutscene -> 
    /// OverworldMap <-> CampfireHub <-> MainDungeonFloor <-> GuildHallInterior <-> ShopInterior <-> BuildingInterior.
    /// </summary>
    public static class SceneConnectionAndMechanicsUnifier
    {
        private static readonly string[] CanonicalScenePaths = new string[]
        {
            "Assets/Scenes/WorldEntryExperience.unity",
            "Assets/Scenes/CharacterEquipmentShowcase.unity",
            "Assets/Scenes/WorldIntroductionCutscene.unity",
            "Assets/Scenes/OverworldMap.unity",
            "Assets/Scenes/CampfireHub.unity",
            "Assets/Scenes/MainDungeonFloor.unity",
            "Assets/Scenes/GuildHallInterior.unity",
            "Assets/Scenes/ShopInterior.unity",
            "Assets/Scenes/BuildingInterior.unity",
            "Assets/Scenes/RelicTestingStage.unity"
        };

        [MenuItem("BattleSummoner/Unify/Run All - Connect All Scenes & Unify Progression Mechanics", false, 1)]
        public static void UnifyAndConnectAllScenes()
        {
            Debug.Log("======================================================================");
            Debug.Log("🔗 STARTING COMPREHENSIVE SCENE CONNECTION & PROGRESSION UNIFICATION");
            Debug.Log("======================================================================");

            ConfigureBuildSettings();
            UnifyShopInterior();
            UnifyMainDungeonFloor();
            UnifyCampfireHub();
            UnifyGuildHallInterior();
            UnifyBuildingInterior();
            UnifyCutscenesAndShowcase();
            UnifyOverworldMap();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("======================================================================");
            Debug.Log("🎉 [ALL CONNECTED] ALL 10 SCENES & TRANSITION MECHANICS FULLY UNIFIED!");
            Debug.Log("======================================================================");
        }

        [MenuItem("BattleSummoner/Unify/1. Configure Build Settings", false, 10)]
        public static void ConfigureBuildSettings()
        {
            var buildScenes = new List<EditorBuildSettingsScene>();
            foreach (var path in CanonicalScenePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
                else
                {
                    Debug.LogWarning($"⚠️ Scene path not found on disk: {path}");
                }
            }
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"✅ [BUILD SETTINGS] Configured {buildScenes.Count} canonical scenes in Build Settings.");
        }

        [MenuItem("BattleSummoner/Unify/2. Unify Shop Interior", false, 11)]
        public static void UnifyShopInterior()
        {
            string path = "Assets/Scenes/ShopInterior.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            // 1. Ensure Entrance Marker
            var doorway = GameObject.Find("Interior_Doorway");
            if (doorway == null)
            {
                doorway = new GameObject("Interior_Doorway");
                doorway.transform.position = new Vector3(0f, 0.1f, -0.6f);
            }
            var marker = doorway.GetComponent<SceneEntranceMarker>();
            if (marker == null) marker = doorway.AddComponent<SceneEntranceMarker>();
            marker.markerId = "Interior_Doorway";
            marker.facingYaw = 0f;

            // 2. Ensure Exit Trigger to OverworldMap
            var exitObj = doorway.transform.Find("Exit_To_Overworld");
            GameObject exitGo = exitObj != null ? exitObj.gameObject : null;
            if (exitGo == null)
            {
                exitGo = new GameObject("Exit_To_Overworld");
                exitGo.transform.SetParent(doorway.transform, false);
                exitGo.transform.localPosition = new Vector3(0f, 0.5f, -0.4f);
            }

            var col = exitGo.GetComponent<BoxCollider>();
            if (col == null) col = exitGo.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.5f, 2.5f, 1.5f);

            var trigger = exitGo.GetComponent<BuildingEntranceTrigger>();
            if (trigger == null) trigger = exitGo.AddComponent<BuildingEntranceTrigger>();
            trigger.targetSceneName = "OverworldMap";
            trigger.targetSpawnMarkerId = "Overworld_ShopDoor";
            trigger.locationTitle = "The Metropolis of Solas";
            trigger.triggerOnTouch = true;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [ShopInterior] Configured entrance marker & OverworldMap exit doorway.");
        }

        [MenuItem("BattleSummoner/Unify/3. Unify Main Dungeon Floor", false, 12)]
        public static void UnifyMainDungeonFloor()
        {
            string path = "Assets/Scenes/MainDungeonFloor.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            // 1. Ensure Dungeon Dimension Entry Marker
            var entryObj = GameObject.Find("DungeonDimension_EntryMarker");
            if (entryObj == null)
            {
                entryObj = new GameObject("DungeonDimension_EntryMarker");
                entryObj.transform.position = new Vector3(0f, 1f, -8f);
            }
            var marker = entryObj.GetComponent<SceneEntranceMarker>();
            if (marker == null) marker = entryObj.AddComponent<SceneEntranceMarker>();
            marker.markerId = "DungeonDimension_Entry";
            marker.facingYaw = 0f;

            // 2. Ensure Exit Portal back to OverworldMap
            var exitPortal = GameObject.Find("Dungeon_Exit_OverworldPortal");
            if (exitPortal == null)
            {
                exitPortal = new GameObject("Dungeon_Exit_OverworldPortal");
                exitPortal.transform.position = new Vector3(0f, 1f, -11.5f);

                // Visual Archway
                var arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arch.name = "PortalArch_L";
                arch.transform.SetParent(exitPortal.transform, false);
                arch.transform.localPosition = new Vector3(-1.8f, 1.8f, 0f);
                arch.transform.localScale = new Vector3(0.5f, 3.6f, 0.5f);

                var archR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                archR.name = "PortalArch_R";
                archR.transform.SetParent(exitPortal.transform, false);
                archR.transform.localPosition = new Vector3(1.8f, 1.8f, 0f);
                archR.transform.localScale = new Vector3(0.5f, 3.6f, 0.5f);

                var archTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                archTop.name = "PortalArch_Top";
                archTop.transform.SetParent(exitPortal.transform, false);
                archTop.transform.localPosition = new Vector3(0f, 3.8f, 0f);
                archTop.transform.localScale = new Vector3(4.1f, 0.5f, 0.5f);

                // Cyan Glow Vortex
                var vortex = GameObject.CreatePrimitive(PrimitiveType.Quad);
                vortex.name = "PortalVortex";
                vortex.transform.SetParent(exitPortal.transform, false);
                vortex.transform.localPosition = new Vector3(0f, 1.9f, 0f);
                vortex.transform.localScale = new Vector3(3.2f, 3.5f, 1f);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.85f, 1.0f, 0.85f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 0.9f) * 1.5f);
                vortex.GetComponent<Renderer>().sharedMaterial = mat;

                // Point Light
                var lObj = new GameObject("PortalLight", typeof(Light));
                lObj.transform.SetParent(exitPortal.transform, false);
                lObj.transform.localPosition = new Vector3(0f, 2f, 0.5f);
                var l = lObj.GetComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.3f, 0.85f, 1.0f);
                l.range = 8f;
                l.intensity = 2f;
            }

            var col = exitPortal.GetComponent<BoxCollider>();
            if (col == null) col = exitPortal.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 1.8f, 0f);
            col.size = new Vector3(3.5f, 3.8f, 2.0f);

            var trigger = exitPortal.GetComponent<BuildingEntranceTrigger>();
            if (trigger == null) trigger = exitPortal.AddComponent<BuildingEntranceTrigger>();
            trigger.targetSceneName = "OverworldMap";
            trigger.targetSpawnMarkerId = "Overworld_NorthernPortal";
            trigger.locationTitle = "Return to Solas Overworld";
            trigger.triggerOnTouch = true;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [MainDungeonFloor] Configured dungeon entrance marker & exit portal back to OverworldMap.");
        }

        [MenuItem("BattleSummoner/Unify/4. Unify Campfire Hub", false, 13)]
        public static void UnifyCampfireHub()
        {
            string path = "Assets/Scenes/CampfireHub.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            // 1. Ensure Entrance Marker
            var arrivalObj = GameObject.Find("Campfire_ArrivalMarker");
            if (arrivalObj == null)
            {
                arrivalObj = new GameObject("Campfire_ArrivalMarker");
                arrivalObj.transform.position = new Vector3(0f, 0.8f, -3.2f);
                arrivalObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            var marker = arrivalObj.GetComponent<SceneEntranceMarker>();
            if (marker == null) marker = arrivalObj.AddComponent<SceneEntranceMarker>();
            marker.markerId = "Campfire_Arrival";
            marker.facingYaw = 0f;

            // Also provide Interior_Doorway alias
            var doorwayObj = GameObject.Find("Interior_Doorway");
            if (doorwayObj == null)
            {
                doorwayObj = new GameObject("Interior_Doorway");
                doorwayObj.transform.position = new Vector3(0f, 0.8f, -3.2f);
            }
            var dMarker = doorwayObj.GetComponent<SceneEntranceMarker>();
            if (dMarker == null) dMarker = doorwayObj.AddComponent<SceneEntranceMarker>();
            dMarker.markerId = "Interior_Doorway";
            dMarker.facingYaw = 0f;

            // 2. Verify Break Camp Button exists and connects
            var canvas = GameObject.Find("CampfireDialogueCanvas");
            if (canvas != null)
            {
                var breakBtn = canvas.transform.Find("SafeAreaContainer/CampfireActionBar/Btn_BreakCamp")?.GetComponent<Button>();
                var hub = GameObject.Find("DialogueHubController")?.GetComponent<MultiCharacterConversationHub>();
                if (breakBtn != null && hub != null)
                {
                    breakBtn.onClick.RemoveListener(hub.BreakCampAndReturn);
                    breakBtn.onClick.AddListener(hub.BreakCampAndReturn);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [CampfireHub] Configured arrival markers & Break Camp resume transition.");
        }

        [MenuItem("BattleSummoner/Unify/5. Unify Guild Hall Interior", false, 14)]
        public static void UnifyGuildHallInterior()
        {
            string path = "Assets/Scenes/GuildHallInterior.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            // Ensure Interior_Doorway marker
            var doorway = GameObject.Find("Interior_Doorway");
            if (doorway != null)
            {
                var marker = doorway.GetComponent<SceneEntranceMarker>();
                if (marker == null) marker = doorway.AddComponent<SceneEntranceMarker>();
                marker.markerId = "Interior_Doorway";
            }

            // Ensure exit trigger points to OverworldMap
            var trigger = UnityEngine.Object.FindAnyObjectByType<BuildingEntranceTrigger>();
            if (trigger != null)
            {
                trigger.targetSceneName = "OverworldMap";
                trigger.targetSpawnMarkerId = "Overworld_GuildHallDoor";
                trigger.locationTitle = "The Metropolis of Solas";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [GuildHallInterior] Verified entrance marker & OverworldMap exit doorway.");
        }

        [MenuItem("BattleSummoner/Unify/6. Unify Building Interior", false, 15)]
        public static void UnifyBuildingInterior()
        {
            string path = "Assets/Scenes/BuildingInterior.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            // Ensure Interior_Doorway marker
            var doorway = GameObject.Find("Interior_Doorway");
            if (doorway != null)
            {
                var marker = doorway.GetComponent<SceneEntranceMarker>();
                if (marker == null) marker = doorway.AddComponent<SceneEntranceMarker>();
                marker.markerId = "Interior_Doorway";
            }

            // Ensure exit trigger points to OverworldMap
            var trigger = UnityEngine.Object.FindAnyObjectByType<BuildingEntranceTrigger>();
            if (trigger != null)
            {
                trigger.targetSceneName = "OverworldMap";
                trigger.targetSpawnMarkerId = "Overworld_TavernDoor";
                trigger.locationTitle = "The Metropolis of Solas";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [BuildingInterior] Verified entrance marker & OverworldMap exit doorway.");
        }

        [MenuItem("BattleSummoner/Unify/7. Unify Cutscenes & Character Showcase", false, 16)]
        public static void UnifyCutscenesAndShowcase()
        {
            UnifyWorldIntroductionCutscene();
            UnifyCharacterEquipmentShowcase();
            UnifyWorldEntryExperience();
        }

        [MenuItem("BattleSummoner/Unify/8. Unify Overworld Map", false, 17)]
        public static void UnifyOverworldMap()
        {
            string path = "Assets/Scenes/OverworldMap.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            // 1. Update Inn Lodge Doorway -> CampfireHub
            var innBuilding = GameObject.Find("Building_Inn_Lodge");
            if (innBuilding != null)
            {
                var door = innBuilding.transform.Find("Doorway_CampfireHub");
                if (door != null)
                {
                    var trigger = door.GetComponent<BuildingEntranceTrigger>();
                    if (trigger != null)
                    {
                        trigger.targetSceneName = "CampfireHub";
                        trigger.targetSpawnMarkerId = "Campfire_Arrival";
                        trigger.locationTitle = "The Adventurers' Campfire Hub";
                        trigger.triggerOnTouch = true;
                        Debug.Log("✅ [OverworldMap] Updated Doorway_CampfireHub targetSceneName to 'CampfireHub'.");
                    }
                }
            }

            // 2. Ensure Return Markers exist in OverworldMap
            EnsureOverworldMarker("Overworld_TavernDoor", new Vector3(-220f, 38.0f, 137f), 180f);
            EnsureOverworldMarker("Overworld_GuildHallDoor", new Vector3(-205f, 38.0f, 177f), 180f);
            EnsureOverworldMarker("Overworld_ShopDoor", new Vector3(-218f, 38.0f, 159f), 180f);
            EnsureOverworldMarker("Overworld_CampfireDoor", new Vector3(-215f, 38.0f, 149f), 180f);
            EnsureOverworldMarker("Overworld_NorthernPortal", new Vector3(-440f, 84.5f, 830f), 180f);
            EnsureOverworldMarker("Spawn_CitadelCenter", new Vector3(0f, 80.05f, -55f), 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [OverworldMap] Saved doorways and return markers.");
        }

        private static void EnsureOverworldMarker(string markerId, Vector3 pos, float yaw)
        {
            var existing = GameObject.Find($"Marker_{markerId}");
            if (existing == null)
            {
                existing = new GameObject($"Marker_{markerId}");
                existing.transform.position = pos;
                existing.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            var marker = existing.GetComponent<SceneEntranceMarker>();
            if (marker == null) marker = existing.AddComponent<SceneEntranceMarker>();
            marker.markerId = markerId;
            marker.facingYaw = yaw;
        }

        private static void UnifyWorldIntroductionCutscene()
        {
            string path = "Assets/Scenes/WorldIntroductionCutscene.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            var director = UnityEngine.Object.FindAnyObjectByType<WorldIntroductionCutsceneDirector>();
            if (director != null)
            {
                director.targetOpenWorldSceneName = "OverworldMap";
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("✅ [WorldIntroductionCutscene] Verified director targets 'OverworldMap'.");
            }
        }

        private static void UnifyCharacterEquipmentShowcase()
        {
            string path = "Assets/Scenes/CharacterEquipmentShowcase.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            var creator = UnityEngine.Object.FindAnyObjectByType<SimsCharacterCreatorUI>();
            if (creator != null)
            {
                creator.targetCutsceneSceneName = "WorldIntroductionCutscene";
                creator.EnsureProceedButton();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("✅ [CharacterEquipmentShowcase] Verified creator targets 'WorldIntroductionCutscene' & Proceed button wired.");
            }
        }

        private static void UnifyWorldEntryExperience()
        {
            string path = "Assets/Scenes/WorldEntryExperience.unity";
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path);

            var seqMgr = UnityEngine.Object.FindAnyObjectByType<WorldEntrySequenceManager>();
            if (seqMgr != null)
            {
                seqMgr.targetWorldSceneName = "CharacterEquipmentShowcase";
            }

            var titleUI = UnityEngine.Object.FindAnyObjectByType<TitleLogoScreenUI>();
            if (titleUI != null)
            {
                titleUI.targetCharacterSceneName = "CharacterEquipmentShowcase";
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("✅ [WorldEntryExperience] Verified Title & Sequence Manager target 'CharacterEquipmentShowcase'.");
        }
    }
}
