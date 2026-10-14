#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class AndroidBuildPipeline
{
    [MenuItem("Build/Export Samsung Android APK (Unity 6)")]
    public static void PerformBuild()
    {
        string[] scenes = { "Assets/Scenes/MainDungeonFloor.unity" };
        string outputPath = "Builds/Android/TowerOfGlyphs_Release.apk";

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        EditorUserBuildSettings.buildAppBundle = false; // Set to true for Google Play Store .aab bundle

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BUILD COMPLETE] Unity 6 APK generated at: {outputPath}");
        }
        else
        {
            Debug.LogError($"[BUILD FAILED] Errors: {report.summary.totalErrors}");
        }
    }

    [MenuItem("Build/Export Google Play AAB Bundle (Unity 6)")]
    public static void PerformBuildAAB()
    {
        string[] scenes = { "Assets/Scenes/MainDungeonFloor.unity" };
        string outputPath = "Builds/Android/TowerOfGlyphs_Release.aab";

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        EditorUserBuildSettings.buildAppBundle = true;

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BUILD COMPLETE] Unity 6 AAB generated at: {outputPath}");
        }
        else
        {
            Debug.LogError($"[BUILD FAILED] Errors: {report.summary.totalErrors}");
        }
    }
}
#endif
