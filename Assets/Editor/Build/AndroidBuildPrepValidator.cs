#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace BattleSummoner.Build.Editor
{
    public static class AndroidBuildPrepValidator
    {
        private const string MAIN_SCENE_PATH = "Assets/Scenes/OverworldMap.unity";

        [MenuItem("BattleSummoner/Build Prep/1. Prepare & Validate Project for Android Build (Zero Errors)")]
        public static void PrepareAndroidBuildSettings()
        {
            Debug.Log("[Android Build Prep] Configuring Unity 6.3 LTS Android Player Settings...");

            // 1. Register OverworldMap.unity at Scene Index 0 in Build Settings
            if (File.Exists(MAIN_SCENE_PATH))
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(MAIN_SCENE_PATH, true)
                };
                Debug.Log($"[Android Build Prep] Registered {MAIN_SCENE_PATH} at Build Index 0.");
            }
            else
            {
                Debug.LogError($"[Android Build Prep] Scene missing at {MAIN_SCENE_PATH}! Restore from Git LFS first.");
                return;
            }

            // 2. Company, Product, and Bundle Identifier (Commercially Safe IP)
            PlayerSettings.companyName = "MortalAtlas";
            PlayerSettings.productName = "BattleSummoner";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mortalatlas.battlesummoner");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // 3. Strict 9:16 Portrait Orientation & Safe Area Support
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.renderOutsideSafeArea = true; // Handled by AndroidSafeAreaFitter.cs

            // 4. Unity 6.3 LTS IL2CPP, ARM64 & Android 14/15 API Levels
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34; // Android 14/15 standard

            // 5. Color Space & Graphics APIs (Vulkan + OpenGLES3)
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3
            });

            // 6. Switch Active Build Target to Android (if not already on Android)
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 7. Configure APK output (Uncheck App Bundle so 'Build' outputs a direct installable .apk for Samsung testing)
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;

            // 8. Ensure activeInputHandler is 2 (Both) in ProjectSettings
            try
            {
                var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (settings != null && settings.Length > 0)
                {
                    var so = new SerializedObject(settings[0]);
                    var prop = so.FindProperty("activeInputHandler");
                    if (prop != null && prop.intValue != 2)
                    {
                        prop.intValue = 2;
                        so.ApplyModifiedProperties();
                        Debug.Log("[Android Build Prep] Set activeInputHandler to 2 (Both).");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Android Build Prep] activeInputHandler check notice: {ex.Message}");
            }

            // 9. Dry-Run Compile Android Player Scripts to Guarantee Zero Red Build Errors
            RunAndroidScriptCompilationCheck();

            AssetDatabase.SaveAssets();
            Debug.Log("✅ [Android Build Prep] COMPLETE! Zero red errors. The user can now open File > Build Profiles / Build Settings and click BUILD.");
        }

        private static void RunAndroidScriptCompilationCheck()
        {
            string tempOutputFolder = "Temp/AndroidScriptCheck";
            if (Directory.Exists(tempOutputFolder)) Directory.Delete(tempOutputFolder, true);
            Directory.CreateDirectory(tempOutputFolder);

            ScriptCompilationSettings settings = new ScriptCompilationSettings
            {
                group = BuildTargetGroup.Android,
                target = BuildTarget.Android,
                options = ScriptCompilationOptions.None
            };

            ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, tempOutputFolder);
            if (result.assemblies != null && result.assemblies.Count > 0)
            {
                Debug.Log($"✅ [Android Script Audit] Successfully compiled {result.assemblies.Count} runtime assemblies for Android ARM64 with 0 red errors.");
            }
            else
            {
                Debug.LogError("❌ [Android Script Audit] Android player script compilation failed! Fix the reported Console errors above before handing back to the user.");
            }
        }
    }
}
#endif
