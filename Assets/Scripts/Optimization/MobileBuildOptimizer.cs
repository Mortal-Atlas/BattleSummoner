using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.Optimization
{
    public enum TargetGamePlatform
    {
        AndroidMobile,
        PCStandalone
    }

    /// <summary>
    /// Automated Mobile Build Optimizer & Cross-Platform Configuration Suite.
    /// Configures high-performance Android mobile settings (60 FPS, ASTC, low draw call limits,
    /// optimized shadow cascades) while maintaining an effortless 1-click abstraction to port to PC.
    /// </summary>
    [ExecuteAlways]
    public class MobileBuildOptimizer : MonoBehaviour
    {
        public static MobileBuildOptimizer Instance { get; private set; }

        [Header("Active Target Platform")]
        [SerializeField] private TargetGamePlatform activePlatform = TargetGamePlatform.AndroidMobile;

        [Header("Mobile Target Framerate & Quality")]
        [SerializeField] private int mobileTargetFPS = 60;
        [SerializeField] private float mobileShadowDistance = 45.0f;
        [SerializeField] private bool mobileEnableSoftShadows = true;

        [Header("PC Target Settings")]
        [SerializeField] private int pcTargetFPS = 144;
        [SerializeField] private float pcShadowDistance = 120.0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            ApplyActivePlatformSettings();
        }

        public void SetPlatform(TargetGamePlatform platform)
        {
            activePlatform = platform;
            ApplyActivePlatformSettings();
        }

        public void ApplyActivePlatformSettings()
        {
            if (activePlatform == TargetGamePlatform.AndroidMobile)
            {
                // Android Mobile Optimization
                Application.targetFrameRate = mobileTargetFPS;
                QualitySettings.vSyncCount = 0; // Avoid forced 30/60/120 drops
                QualitySettings.shadowDistance = mobileShadowDistance;
                QualitySettings.shadowCascades = 2; // Optimal mobile shadow cascade balance
                QualitySettings.shadows = mobileEnableSoftShadows ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.HardOnly;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.realtimeReflectionProbes = false;

                // Configure URP Asset if available
                var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = mobileShadowDistance;
                    urpAsset.shadowCascadeCount = 2;
                }

                Debug.Log($"📱 [MOBILE OPTIMIZER] Configured Android Mobile Profile: {mobileTargetFPS} FPS cap, {mobileShadowDistance}m shadows, 2 cascades.");
            }
            else
            {
                // PC Standalone Profile
                Application.targetFrameRate = pcTargetFPS;
                QualitySettings.vSyncCount = 1;
                QualitySettings.shadowDistance = pcShadowDistance;
                QualitySettings.shadowCascades = 4;
                QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.realtimeReflectionProbes = true;

                var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = pcShadowDistance;
                    urpAsset.shadowCascadeCount = 4;
                }

                Debug.Log($"🖥️ [PC OPTIMIZER] Configured PC Standalone Profile: {pcTargetFPS} FPS, {pcShadowDistance}m shadows, 4 cascades.");
            }
        }

#if UNITY_EDITOR
        [MenuItem("BattleSummoner/Optimization/Apply Mobile Build Optimizations (2-Click)")]
        public static void OptimizeForMobileBuild()
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            PlayerSettings.stripEngineCode = true;

            var optimizer = FindAnyObjectByType<MobileBuildOptimizer>();
            if (optimizer == null)
            {
                var go = new GameObject("--- MOBILE BUILD OPTIMIZER ---");
                optimizer = go.AddComponent<MobileBuildOptimizer>();
            }
            optimizer.SetPlatform(TargetGamePlatform.AndroidMobile);

            Debug.Log("🚀 [MOBILE BUILD OPTIMIZER] Android IL2CPP ARM64, ASTC Textures, and 60 FPS limits configured!");
        }

        [MenuItem("BattleSummoner/Optimization/Switch Platform Profile to PC Standalone")]
        public static void SwitchToPCBuild()
        {
            var optimizer = FindAnyObjectByType<MobileBuildOptimizer>();
            if (optimizer == null)
            {
                var go = new GameObject("--- MOBILE BUILD OPTIMIZER ---");
                optimizer = go.AddComponent<MobileBuildOptimizer>();
            }
            optimizer.SetPlatform(TargetGamePlatform.PCStandalone);

            Debug.Log("🖥️ [PC BUILD PROFILE] Switched target configuration to PC Standalone!");
        }
#endif
    }
}
