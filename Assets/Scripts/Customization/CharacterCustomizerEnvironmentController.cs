using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Environment;

namespace BattleSummoner.Customization
{
    public enum SkyboxTheme
    {
        AtmosphericDynamic, // Built-in Rayleigh/Mie procedural skybox
        AnimeDayNight,       // Custom Stylized multi-gradient with sun/moon discs and stars
        StarryNebula,        // Deep cosmic violet-indigo cosmos with twinkling stars
        GoldenSunset,        // Fiery golden hour amber/crimson/rose horizon
        MysticAurora,        // Deep twilight indigo with shimmering aurora glow
        BloodEclipse,        // Dark crimson occult sky with eclipse corona
        MinimalStudio        // Neutral sleek studio cyclorama gradient
    }

    public enum TimePreset
    {
        Dawn,    // 06:30 AM
        Noon,    // 12:00 PM
        Sunset,  // 06:30 PM
        Twilight,// 08:30 PM
        Night,   // 12:00 AM
        Eclipse  // Special Occult Eclipse
    }

    /// <summary>
    /// CharacterCustomizerEnvironmentController manages the complete dynamic world ambiance
    /// around the character in the Showcase Customizer:
    /// - 24-hour continuous time-of-day progression or 1-click presets
    /// - Dynamic skybox switching across 7 distinct artistic themes
    /// - Directional Sun/Moon light modulation (pitch, yaw, color, intensity, shadows)
    /// - Studio Rim and Fill light harmonization for anime character cel-shading
    /// - Ambient Trilight (Sky, Equator, Ground) and dynamic GI environmental updates
    /// - Atmospheric distance fog
    /// - Stage braziers/lanterns that ignite warmly at night and dim at noon
    /// - Real-time timelapse day-night cycle animation
    /// </summary>
    [ExecuteAlways]
    public class CharacterCustomizerEnvironmentController : MonoBehaviour
    {
        public static CharacterCustomizerEnvironmentController Instance { get; private set; }

        [Header("Target Lights")]
        public Light sunLight;
        public Light rimLight;
        public Light fillLight;

        [Header("Stage Environment Props")]
        public List<Light> stageLanternLights = new List<Light>();
        public List<MeshRenderer> stageLanternEmissives = new List<MeshRenderer>();
        public ParticleSystem starlightParticles;
        public GameObject exhibitionStage;
        public GameObject fantasyArchway;

        [Header("Skybox Materials")]
        public Material proceduralSkyboxMaterial;
        public Material stylizedAnimeSkyboxMaterial;

        [Header("Current Environment State")]
        [Range(0f, 24f)]
        public float currentTimeOfDay = 12.0f; // 0.0 to 24.0 hours
        public SkyboxTheme currentTheme = SkyboxTheme.AnimeDayNight;
        public bool isTimelapseActive = false;
        public float timelapseSpeed = 1.0f; // Hours per real-second
        public bool manualSunOverride = false;
        public float manualSunPitch = 45f;
        public float manualSunYaw = 160f;
        public float sunIntensityMultiplier = 1.0f;
        public float customFogDensity = 0.012f;
        public bool lanternsEnabled = true;

        [Header("Bioluminescent Night Glow")]
        public Renderer daisRuneCircleRenderer;
        public Light daisUnderLight;
        [Range(0f, 3f)]
        public float nightGlowIntensity = 1.5f;
        public bool runicGlowEnabled = true;

        // Internal cached materials
        private Material runtimeStylizedMat;
        private Material runtimeProceduralMat;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            AutoBindReferences();
            InitializeMaterials();
        }

        private void Start()
        {
            ApplyTimeOfDay(currentTimeOfDay);
        }

        private void Update()
        {
            if (Application.isPlaying && isTimelapseActive)
            {
                currentTimeOfDay = (currentTimeOfDay + Time.deltaTime * timelapseSpeed) % 24f;
                ApplyTimeOfDay(currentTimeOfDay);
            }
        }

        public void AutoBindReferences()
        {
            if (sunLight == null)
            {
                var sunGo = GameObject.Find("Sun_DirectionalLight");
                if (sunGo != null) sunLight = sunGo.GetComponent<Light>();
                if (sunLight == null) sunLight = RenderSettings.sun;
            }
            if (rimLight == null)
            {
                var rimGo = GameObject.Find("Studio_RimLight");
                if (rimGo != null) rimLight = rimGo.GetComponent<Light>();
            }
            if (fillLight == null)
            {
                var fillGo = GameObject.Find("Studio_FillLight");
                if (fillGo != null) fillLight = fillGo.GetComponent<Light>();
            }
            if (starlightParticles == null)
            {
                var psGo = GameObject.Find("Studio_StarlightParticles");
                if (psGo != null) starlightParticles = psGo.GetComponent<ParticleSystem>();
            }
            if (exhibitionStage == null) exhibitionStage = GameObject.Find("Studio_ExhibitionStage");
            if (fantasyArchway == null) fantasyArchway = GameObject.Find("Studio_FantasyArchway");
            if (daisRuneCircleRenderer == null)
            {
                var runeGo = GameObject.Find("Dais_GlowingRuneCircle");
                if (runeGo != null) daisRuneCircleRenderer = runeGo.GetComponent<Renderer>();
            }
            if (daisUnderLight == null)
            {
                var underGo = GameObject.Find("Dais_SummoningUnderLight");
                if (underGo != null) daisUnderLight = underGo.GetComponent<Light>();
            }

            // Bind stage lanterns
            if (stageLanternLights.Count == 0 && exhibitionStage != null)
            {
                var lights = exhibitionStage.GetComponentsInChildren<Light>();
                foreach (var l in lights)
                {
                    if (l != sunLight && l != rimLight && l != fillLight)
                        stageLanternLights.Add(l);
                }
            }
        }

        public void InitializeMaterials()
        {
            var animeShader = Shader.Find("Custom/StylizedAnimeSkybox");
            if (animeShader != null && stylizedAnimeSkyboxMaterial == null)
            {
                stylizedAnimeSkyboxMaterial = new Material(animeShader) { name = "Mat_Runtime_AnimeSkybox" };
            }

            var procShader = Shader.Find("Skybox/Procedural");
            if (procShader != null && proceduralSkyboxMaterial == null)
            {
                proceduralSkyboxMaterial = new Material(procShader) { name = "Mat_Runtime_ProceduralSkybox" };
            }

            if (stylizedAnimeSkyboxMaterial != null)
            {
                runtimeStylizedMat = new Material(stylizedAnimeSkyboxMaterial);
            }
            if (proceduralSkyboxMaterial != null)
            {
                runtimeProceduralMat = new Material(proceduralSkyboxMaterial);
            }
        }

        // --- PRESET APPLICATION ---
        public void ApplyPreset(TimePreset preset)
        {
            manualSunOverride = false;
            switch (preset)
            {
                case TimePreset.Dawn:
                    currentTimeOfDay = 6.5f;
                    currentTheme = SkyboxTheme.AnimeDayNight;
                    break;
                case TimePreset.Noon:
                    currentTimeOfDay = 12.0f;
                    currentTheme = SkyboxTheme.AnimeDayNight;
                    break;
                case TimePreset.Sunset:
                    currentTimeOfDay = 18.5f;
                    currentTheme = SkyboxTheme.GoldenSunset;
                    break;
                case TimePreset.Twilight:
                    currentTimeOfDay = 20.5f;
                    currentTheme = SkyboxTheme.MysticAurora;
                    break;
                case TimePreset.Night:
                    currentTimeOfDay = 0.0f;
                    currentTheme = SkyboxTheme.StarryNebula;
                    break;
                case TimePreset.Eclipse:
                    currentTimeOfDay = 14.0f;
                    currentTheme = SkyboxTheme.BloodEclipse;
                    break;
            }

            ApplyTimeOfDay(currentTimeOfDay);
        }

        public void SetTimeOfDay(float hour)
        {
            manualSunOverride = false;
            currentTimeOfDay = Mathf.Clamp(hour, 0f, 24f);
            ApplyTimeOfDay(currentTimeOfDay);
        }

        public void SetSkyboxTheme(SkyboxTheme theme)
        {
            currentTheme = theme;
            ApplyTimeOfDay(currentTimeOfDay);
        }

        public void SetSunAzimuth(float yaw)
        {
            manualSunOverride = true;
            manualSunYaw = yaw;
            ApplySunRotation();
        }

        public void SetSunElevation(float pitch)
        {
            manualSunOverride = true;
            manualSunPitch = pitch;
            ApplySunRotation();
        }

        public void SetSunIntensity(float intensity)
        {
            sunIntensityMultiplier = Mathf.Max(0f, intensity);
            ApplyLightingColorsAndIntensities(currentTimeOfDay);
        }

        public void SetFogDensity(float density)
        {
            customFogDensity = Mathf.Clamp(density, 0f, 0.05f);
            RenderSettings.fogDensity = customFogDensity;
        }

        public void ToggleTimelapse()
        {
            isTimelapseActive = !isTimelapseActive;
        }

        public void SetTimelapse(bool active)
        {
            isTimelapseActive = active;
        }

        public void SetTimelapseSpeed(float speed)
        {
            timelapseSpeed = Mathf.Max(0.1f, speed);
        }

        public void SetLanternsEnabled(bool enabled)
        {
            lanternsEnabled = enabled;
            UpdateStageLanterns(currentTimeOfDay);
        }

        public void SetNightGlowIntensity(float intensity)
        {
            nightGlowIntensity = Mathf.Clamp(intensity, 0f, 3f);
            UpdateNightBioluminescence(currentTimeOfDay);
        }

        public void ToggleRunicGlow()
        {
            runicGlowEnabled = !runicGlowEnabled;
            UpdateNightBioluminescence(currentTimeOfDay);
        }

        public string GetDigitalClockString()
        {
            int hour = Mathf.FloorToInt(currentTimeOfDay);
            int minute = Mathf.FloorToInt((currentTimeOfDay - hour) * 60f);
            string period = hour >= 12 ? "PM" : "AM";
            int displayHour = hour % 12;
            if (displayHour == 0) displayHour = 12;
            return $"{displayHour:00}:{minute:00} {period}";
        }

        // --- CORE TIME & LIGHTING EVALUATION ---
        public void ApplyTimeOfDay(float hour)
        {
            AutoBindReferences();
            if (runtimeStylizedMat == null || runtimeProceduralMat == null) InitializeMaterials();

            // 1. Calculate and apply Sun / Celestial Light rotation
            ApplySunRotation();

            // 2. Modulate Lights, Ambient Trilight, and Fog
            ApplyLightingColorsAndIntensities(hour);

            // 3. Update Skybox Material based on Theme and Time
            UpdateSkyboxMaterial(hour);

            // 4. Update Stage Lanterns and Archway Sconces
            UpdateStageLanterns(hour);

            // 5. Update Bioluminescent Night Glow (Arcane Dais, Fireflies, Underlight)
            UpdateNightBioluminescence(hour);

            // 6. Trigger Unity Dynamic GI Environmental Lighting refresh
            DynamicGI.UpdateEnvironment();
        }

        private void ApplySunRotation()
        {
            if (sunLight == null) return;

            float pitch;
            float yaw;

            if (manualSunOverride)
            {
                pitch = manualSunPitch;
                yaw = manualSunYaw;
            }
            else if (currentTheme == SkyboxTheme.BloodEclipse)
            {
                pitch = 24f;
                yaw = 190f;
            }
            else
            {
                float hour = currentTimeOfDay;
                if (hour >= 5.5f && hour <= 19.0f)
                {
                    // Day Arc (05:30 -> 19:00): 13.5 hours
                    float t = (hour - 5.5f) / 13.5f; // 0.0 to 1.0
                    // Pitch ascends from 0° at dawn to 68° at noon, down to 0° at dusk
                    pitch = Mathf.Sin(t * Mathf.PI) * 68f;
                    // Yaw travels from east (70°) through south (160°) to west (265°)
                    yaw = Mathf.Lerp(70f, 265f, t);
                }
                else
                {
                    // Night Arc (19:00 -> 05:30): 10.5 hours
                    float nightHour = hour > 19.0f ? hour - 19.0f : hour + 5.0f;
                    float t = nightHour / 10.5f;
                    // Moon pitch ascends from 5° to 48° to 5°
                    pitch = Mathf.Sin(t * Mathf.PI) * 48f + 5f;
                    yaw = Mathf.Lerp(30f, 110f, t);
                }
            }

            sunLight.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            RenderSettings.sun = sunLight;
        }

        private void ApplyLightingColorsAndIntensities(float hour)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            Color sunCol;
            float sunInt;
            Color rimCol;
            float rimInt;
            Color fillCol;
            float fillInt;
            Color skyAmb;
            Color eqAmb;
            Color grAmb;
            Color fogCol;
            float fogDens = customFogDensity;

            if (currentTheme == SkyboxTheme.BloodEclipse)
            {
                // Occult Crimson Eclipse
                sunCol = new Color(0.95f, 0.18f, 0.35f);
                sunInt = 1.25f * sunIntensityMultiplier;
                rimCol = new Color(0.85f, 0.20f, 0.95f);
                rimInt = 1.6f;
                fillCol = new Color(0.40f, 0.12f, 0.28f);
                fillInt = 0.45f;
                skyAmb = new Color(0.35f, 0.08f, 0.22f);
                eqAmb = new Color(0.20f, 0.05f, 0.15f);
                grAmb = new Color(0.08f, 0.02f, 0.06f);
                fogCol = new Color(0.25f, 0.06f, 0.18f);
                fogDens = 0.022f;
            }
            else if (currentTheme == SkyboxTheme.MinimalStudio)
            {
                // High-End Studio Lighting
                sunCol = new Color(1.0f, 0.98f, 0.95f);
                sunInt = 1.20f * sunIntensityMultiplier;
                rimCol = new Color(0.45f, 0.75f, 1.0f);
                rimInt = 1.1f;
                fillCol = new Color(0.85f, 0.90f, 0.95f);
                fillInt = 0.60f;
                skyAmb = new Color(0.30f, 0.35f, 0.42f);
                eqAmb = new Color(0.20f, 0.23f, 0.28f);
                grAmb = new Color(0.12f, 0.14f, 0.18f);
                fogCol = new Color(0.10f, 0.12f, 0.16f);
                fogDens = 0.005f;
            }
            else if (hour >= 5.5f && hour < 8.0f)
            {
                // Dawn / Sunrise (05:30 -> 08:00)
                float t = (hour - 5.5f) / 2.5f;
                sunCol = Color.Lerp(new Color(1.0f, 0.55f, 0.35f), new Color(1.0f, 0.88f, 0.72f), t);
                sunInt = Mathf.Lerp(0.8f, 1.3f, t) * sunIntensityMultiplier;
                rimCol = Color.Lerp(new Color(1.0f, 0.60f, 0.40f), new Color(1.0f, 0.90f, 0.70f), t);
                rimInt = 1.35f;
                fillCol = new Color(0.70f, 0.65f, 0.80f);
                fillInt = 0.45f;
                skyAmb = Color.Lerp(new Color(0.28f, 0.25f, 0.45f), new Color(0.45f, 0.55f, 0.75f), t);
                eqAmb = new Color(0.85f, 0.55f, 0.42f) * 0.4f;
                grAmb = new Color(0.15f, 0.12f, 0.18f);
                fogCol = Color.Lerp(new Color(0.85f, 0.55f, 0.45f), new Color(0.80f, 0.85f, 0.95f), t);
                fogDens = Mathf.Lerp(0.018f, 0.008f, t);
            }
            else if (hour >= 8.0f && hour < 16.5f)
            {
                // Midday / Noon (08:00 -> 16:30)
                sunCol = new Color(1.0f, 0.97f, 0.92f);
                sunInt = 1.45f * sunIntensityMultiplier;
                rimCol = new Color(0.75f, 0.90f, 1.0f);
                rimInt = 0.9f;
                fillCol = new Color(0.85f, 0.92f, 1.0f);
                fillInt = 0.55f;
                skyAmb = new Color(0.40f, 0.55f, 0.80f);
                eqAmb = new Color(0.35f, 0.45f, 0.60f);
                grAmb = new Color(0.18f, 0.22f, 0.28f);
                fogCol = new Color(0.75f, 0.85f, 0.95f);
                fogDens = 0.006f;
            }
            else if (hour >= 16.5f && hour < 19.5f)
            {
                // Golden Hour / Sunset (16:30 -> 19:30)
                float t = (hour - 16.5f) / 3.0f;
                sunCol = Color.Lerp(new Color(1.0f, 0.85f, 0.50f), new Color(1.0f, 0.40f, 0.15f), t);
                sunInt = Mathf.Lerp(1.4f, 1.1f, t) * sunIntensityMultiplier;
                rimCol = Color.Lerp(new Color(1.0f, 0.70f, 0.30f), new Color(1.0f, 0.35f, 0.15f), t);
                rimInt = 1.7f;
                fillCol = new Color(0.65f, 0.50f, 0.75f);
                fillInt = 0.40f;
                skyAmb = Color.Lerp(new Color(0.40f, 0.45f, 0.70f), new Color(0.30f, 0.20f, 0.45f), t);
                eqAmb = Color.Lerp(new Color(0.95f, 0.55f, 0.30f) * 0.5f, new Color(0.85f, 0.35f, 0.20f) * 0.4f, t);
                grAmb = new Color(0.14f, 0.10f, 0.16f);
                fogCol = Color.Lerp(new Color(0.95f, 0.65f, 0.45f), new Color(0.70f, 0.30f, 0.35f), t);
                fogDens = Mathf.Lerp(0.008f, 0.020f, t);
            }
            else if (hour >= 19.5f && hour < 21.5f)
            {
                // Twilight / Dusk (19:30 -> 21:30)
                float t = (hour - 19.5f) / 2.0f;
                sunCol = Color.Lerp(new Color(0.60f, 0.25f, 0.50f), new Color(0.30f, 0.40f, 0.75f), t);
                sunInt = Mathf.Lerp(0.7f, 0.45f, t) * sunIntensityMultiplier;
                rimCol = Color.Lerp(new Color(0.90f, 0.45f, 0.70f), new Color(0.40f, 0.75f, 1.0f), t);
                rimInt = 1.25f;
                fillCol = new Color(0.35f, 0.40f, 0.65f);
                fillInt = 0.35f;
                skyAmb = Color.Lerp(new Color(0.25f, 0.18f, 0.45f), new Color(0.12f, 0.15f, 0.30f), t);
                eqAmb = new Color(0.35f, 0.20f, 0.45f) * 0.35f;
                grAmb = new Color(0.06f, 0.06f, 0.10f);
                fogCol = Color.Lerp(new Color(0.45f, 0.25f, 0.45f), new Color(0.15f, 0.18f, 0.30f), t);
                fogDens = Mathf.Lerp(0.020f, 0.015f, t);
            }
            else
            {
                // Midnight / Night (21:30 -> 05:30) - Moonlight
                sunCol = new Color(0.65f, 0.78f, 0.98f);
                sunInt = 0.55f * sunIntensityMultiplier;
                rimCol = new Color(0.55f, 0.80f, 1.0f);
                rimInt = 1.05f;
                fillCol = new Color(0.20f, 0.28f, 0.45f);
                fillInt = 0.30f;
                skyAmb = new Color(0.06f, 0.09f, 0.18f);
                eqAmb = new Color(0.08f, 0.12f, 0.22f);
                grAmb = new Color(0.02f, 0.03f, 0.06f);
                fogCol = new Color(0.05f, 0.08f, 0.14f);
                fogDens = 0.012f;
            }

            if (sunLight != null)
            {
                sunLight.color = sunCol;
                sunLight.intensity = sunInt;
            }
            if (rimLight != null)
            {
                rimLight.color = rimCol;
                rimLight.intensity = rimInt;
            }
            if (fillLight != null)
            {
                fillLight.color = fillCol;
                fillLight.intensity = fillInt;
            }

            RenderSettings.ambientSkyColor = skyAmb;
            RenderSettings.ambientEquatorColor = eqAmb;
            RenderSettings.ambientGroundColor = grAmb;
            RenderSettings.fogColor = fogCol;
            RenderSettings.fogDensity = fogDens;
        }

        private void UpdateSkyboxMaterial(float hour)
        {
            if (currentTheme == SkyboxTheme.AtmosphericDynamic && runtimeProceduralMat != null)
            {
                // Unity Procedural Skybox
                RenderSettings.skybox = runtimeProceduralMat;

                if (hour >= 5.5f && hour < 8.0f)
                {
                    // Dawn
                    runtimeProceduralMat.SetColor("_SkyTint", new Color(0.90f, 0.62f, 0.50f));
                    runtimeProceduralMat.SetColor("_GroundColor", new Color(0.18f, 0.12f, 0.10f));
                    runtimeProceduralMat.SetFloat("_AtmosphereThickness", 1.45f);
                    runtimeProceduralMat.SetFloat("_Exposure", 1.05f);
                }
                else if (hour >= 8.0f && hour < 16.5f)
                {
                    // Noon
                    runtimeProceduralMat.SetColor("_SkyTint", new Color(0.48f, 0.68f, 0.95f));
                    runtimeProceduralMat.SetColor("_GroundColor", new Color(0.20f, 0.22f, 0.25f));
                    runtimeProceduralMat.SetFloat("_AtmosphereThickness", 1.00f);
                    runtimeProceduralMat.SetFloat("_Exposure", 1.25f);
                }
                else if (hour >= 16.5f && hour < 19.5f)
                {
                    // Sunset
                    runtimeProceduralMat.SetColor("_SkyTint", new Color(0.98f, 0.42f, 0.20f));
                    runtimeProceduralMat.SetColor("_GroundColor", new Color(0.15f, 0.08f, 0.08f));
                    runtimeProceduralMat.SetFloat("_AtmosphereThickness", 1.85f);
                    runtimeProceduralMat.SetFloat("_Exposure", 1.15f);
                }
                else
                {
                    // Night
                    runtimeProceduralMat.SetColor("_SkyTint", new Color(0.04f, 0.06f, 0.15f));
                    runtimeProceduralMat.SetColor("_GroundColor", new Color(0.01f, 0.02f, 0.04f));
                    runtimeProceduralMat.SetFloat("_AtmosphereThickness", 0.40f);
                    runtimeProceduralMat.SetFloat("_Exposure", 0.50f);
                }
            }
            else if (runtimeStylizedMat != null)
            {
                RenderSettings.skybox = runtimeStylizedMat;
                runtimeStylizedMat.SetFloat("_EclipseMode", 0f);

                switch (currentTheme)
                {
                    case SkyboxTheme.StarryNebula:
                        runtimeStylizedMat.SetColor("_ZenithColor", new Color(0.04f, 0.05f, 0.16f));
                        runtimeStylizedMat.SetColor("_HorizonColor", new Color(0.15f, 0.18f, 0.38f));
                        runtimeStylizedMat.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.05f));
                        runtimeStylizedMat.SetColor("_SunColor", new Color(0.60f, 0.75f, 1.0f));
                        runtimeStylizedMat.SetColor("_MoonColor", new Color(0.90f, 0.95f, 1.0f));
                        runtimeStylizedMat.SetFloat("_StarIntensity", 2.2f);
                        runtimeStylizedMat.SetFloat("_StarDensity", 160f);
                        runtimeStylizedMat.SetFloat("_HorizonExponent", 2.0f);
                        runtimeStylizedMat.SetFloat("_Exposure", 1.1f);
                        break;

                    case SkyboxTheme.GoldenSunset:
                        runtimeStylizedMat.SetColor("_ZenithColor", new Color(0.18f, 0.15f, 0.45f));
                        runtimeStylizedMat.SetColor("_HorizonColor", new Color(0.98f, 0.48f, 0.18f));
                        runtimeStylizedMat.SetColor("_GroundColor", new Color(0.12f, 0.06f, 0.08f));
                        runtimeStylizedMat.SetColor("_SunColor", new Color(1.0f, 0.85f, 0.40f));
                        runtimeStylizedMat.SetFloat("_StarIntensity", 0.15f);
                        runtimeStylizedMat.SetFloat("_HorizonExponent", 2.8f);
                        runtimeStylizedMat.SetFloat("_Exposure", 1.25f);
                        break;

                    case SkyboxTheme.MysticAurora:
                        runtimeStylizedMat.SetColor("_ZenithColor", new Color(0.08f, 0.06f, 0.25f));
                        runtimeStylizedMat.SetColor("_HorizonColor", new Color(0.15f, 0.75f, 0.65f)); // Aurora teal/emerald
                        runtimeStylizedMat.SetColor("_GroundColor", new Color(0.03f, 0.04f, 0.08f));
                        runtimeStylizedMat.SetColor("_MoonColor", new Color(0.80f, 0.95f, 1.0f));
                        runtimeStylizedMat.SetFloat("_StarIntensity", 1.6f);
                        runtimeStylizedMat.SetFloat("_StarDensity", 120f);
                        runtimeStylizedMat.SetFloat("_HorizonExponent", 3.2f);
                        runtimeStylizedMat.SetFloat("_Exposure", 1.15f);
                        break;

                    case SkyboxTheme.BloodEclipse:
                        runtimeStylizedMat.SetFloat("_EclipseMode", 1.0f);
                        runtimeStylizedMat.SetColor("_ZenithColor", new Color(0.12f, 0.02f, 0.08f));
                        runtimeStylizedMat.SetColor("_HorizonColor", new Color(0.55f, 0.08f, 0.20f));
                        runtimeStylizedMat.SetColor("_GroundColor", new Color(0.04f, 0.01f, 0.02f));
                        runtimeStylizedMat.SetColor("_CoronaColor", new Color(1.0f, 0.15f, 0.35f));
                        runtimeStylizedMat.SetFloat("_SunSize", 0.035f);
                        runtimeStylizedMat.SetFloat("_StarIntensity", 0.9f);
                        runtimeStylizedMat.SetFloat("_HorizonExponent", 2.2f);
                        runtimeStylizedMat.SetFloat("_Exposure", 1.30f);
                        break;

                    case SkyboxTheme.MinimalStudio:
                        runtimeStylizedMat.SetColor("_ZenithColor", new Color(0.08f, 0.12f, 0.18f));
                        runtimeStylizedMat.SetColor("_HorizonColor", new Color(0.20f, 0.26f, 0.35f));
                        runtimeStylizedMat.SetColor("_GroundColor", new Color(0.05f, 0.07f, 0.10f));
                        runtimeStylizedMat.SetColor("_SunColor", new Color(0.95f, 0.98f, 1.0f));
                        runtimeStylizedMat.SetFloat("_StarIntensity", 0.0f);
                        runtimeStylizedMat.SetFloat("_HorizonExponent", 1.8f);
                        runtimeStylizedMat.SetFloat("_Exposure", 1.0f);
                        break;

                    case SkyboxTheme.AnimeDayNight:
                    default:
                        // Dynamic Anime Interpolation based on 24h cycle
                        if (hour >= 5.5f && hour < 8.0f)
                        {
                            // Dawn
                            float t = (hour - 5.5f) / 2.5f;
                            runtimeStylizedMat.SetColor("_ZenithColor", Color.Lerp(new Color(0.10f, 0.14f, 0.35f), new Color(0.22f, 0.45f, 0.75f), t));
                            runtimeStylizedMat.SetColor("_HorizonColor", Color.Lerp(new Color(0.95f, 0.55f, 0.45f), new Color(0.98f, 0.80f, 0.65f), t));
                            runtimeStylizedMat.SetColor("_GroundColor", new Color(0.12f, 0.10f, 0.15f));
                            runtimeStylizedMat.SetColor("_SunColor", new Color(1.0f, 0.88f, 0.65f));
                            runtimeStylizedMat.SetFloat("_StarIntensity", Mathf.Lerp(0.8f, 0.0f, t));
                            runtimeStylizedMat.SetFloat("_Exposure", 1.15f);
                        }
                        else if (hour >= 8.0f && hour < 16.5f)
                        {
                            // Crisp Day
                            runtimeStylizedMat.SetColor("_ZenithColor", new Color(0.20f, 0.48f, 0.88f));
                            runtimeStylizedMat.SetColor("_HorizonColor", new Color(0.70f, 0.85f, 0.98f));
                            runtimeStylizedMat.SetColor("_GroundColor", new Color(0.18f, 0.22f, 0.28f));
                            runtimeStylizedMat.SetColor("_SunColor", new Color(1.0f, 0.98f, 0.90f));
                            runtimeStylizedMat.SetFloat("_StarIntensity", 0.0f);
                            runtimeStylizedMat.SetFloat("_Exposure", 1.25f);
                        }
                        else if (hour >= 16.5f && hour < 19.5f)
                        {
                            // Sunset
                            float t = (hour - 16.5f) / 3.0f;
                            runtimeStylizedMat.SetColor("_ZenithColor", Color.Lerp(new Color(0.20f, 0.35f, 0.70f), new Color(0.18f, 0.12f, 0.40f), t));
                            runtimeStylizedMat.SetColor("_HorizonColor", Color.Lerp(new Color(0.98f, 0.70f, 0.30f), new Color(0.95f, 0.42f, 0.18f), t));
                            runtimeStylizedMat.SetColor("_GroundColor", new Color(0.12f, 0.08f, 0.10f));
                            runtimeStylizedMat.SetColor("_SunColor", new Color(1.0f, 0.85f, 0.45f));
                            runtimeStylizedMat.SetFloat("_StarIntensity", Mathf.Lerp(0.0f, 0.5f, t));
                            runtimeStylizedMat.SetFloat("_Exposure", 1.20f);
                        }
                        else if (hour >= 19.5f && hour < 21.5f)
                        {
                            // Twilight
                            float t = (hour - 19.5f) / 2.0f;
                            runtimeStylizedMat.SetColor("_ZenithColor", Color.Lerp(new Color(0.15f, 0.10f, 0.38f), new Color(0.06f, 0.08f, 0.22f), t));
                            runtimeStylizedMat.SetColor("_HorizonColor", Color.Lerp(new Color(0.85f, 0.35f, 0.45f), new Color(0.30f, 0.35f, 0.65f), t));
                            runtimeStylizedMat.SetColor("_GroundColor", new Color(0.06f, 0.05f, 0.08f));
                            runtimeStylizedMat.SetColor("_MoonColor", new Color(0.85f, 0.92f, 1.0f));
                            runtimeStylizedMat.SetFloat("_StarIntensity", Mathf.Lerp(0.5f, 1.4f, t));
                            runtimeStylizedMat.SetFloat("_Exposure", 1.10f);
                        }
                        else
                        {
                            // Night
                            runtimeStylizedMat.SetColor("_ZenithColor", new Color(0.04f, 0.05f, 0.15f));
                            runtimeStylizedMat.SetColor("_HorizonColor", new Color(0.10f, 0.15f, 0.30f));
                            runtimeStylizedMat.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.05f));
                            runtimeStylizedMat.SetColor("_MoonColor", new Color(0.90f, 0.95f, 1.0f));
                            runtimeStylizedMat.SetFloat("_StarIntensity", 1.6f);
                            runtimeStylizedMat.SetFloat("_Exposure", 1.00f);
                        }
                        break;
                }
            }
        }

        private void UpdateStageLanterns(float hour)
        {
            bool isDark = (hour >= 18.5f || hour <= 6.5f) || (currentTheme == SkyboxTheme.BloodEclipse);
            float targetLanternIntensity = (!lanternsEnabled) ? 0f : (isDark ? 2.8f : 0.25f);
            Color lanternColor = currentTheme == SkyboxTheme.BloodEclipse 
                ? new Color(1.0f, 0.25f, 0.40f) 
                : new Color(1.0f, 0.65f, 0.28f);

            foreach (var l in stageLanternLights)
            {
                if (l != null)
                {
                    l.intensity = targetLanternIntensity;
                    l.color = lanternColor;
                    l.enabled = lanternsEnabled && targetLanternIntensity > 0.05f;
                }
            }

            // Update emissives
            foreach (var r in stageLanternEmissives)
            {
                if (r != null && r.sharedMaterial != null)
                {
                    float emissiveMult = targetLanternIntensity / 2.8f;
                    r.sharedMaterial.SetColor("_EmissionColor", lanternColor * emissiveMult * 2.5f);
                }
            }

            // Update Starlight Particles
            if (starlightParticles != null)
            {
                var main = starlightParticles.main;
                if (currentTheme == SkyboxTheme.BloodEclipse)
                {
                    main.startColor = new Color(0.95f, 0.20f, 0.35f, 0.75f);
                }
                else if (hour >= 18.0f || hour <= 6.5f)
                {
                    main.startColor = new Color(0.55f, 0.85f, 1.0f, 0.75f);
                }
                else
                {
                    main.startColor = new Color(1.0f, 0.95f, 0.70f, 0.35f);
                }
            }
        }

        private void UpdateNightBioluminescence(float hour)
        {
            float darkness = 0f;
            if (currentTheme == SkyboxTheme.BloodEclipse)
            {
                darkness = 1.0f;
            }
            else if (hour >= 5.0f && hour <= 7.0f)
            {
                // Dawn: fades from 1.0 to 0.0
                darkness = Mathf.Clamp01(1.0f - (hour - 5.0f) / 2.0f);
            }
            else if (hour > 7.0f && hour < 17.5f)
            {
                // Day: dormant
                darkness = 0.0f;
            }
            else if (hour >= 17.5f && hour <= 20.5f)
            {
                // Sunset to Dusk: ramps from 0.0 to 1.0
                darkness = Mathf.Clamp01((hour - 17.5f) / 3.0f);
            }
            else
            {
                // Night (20:30 to 05:00): full radiant darkness glow
                darkness = 1.0f;
            }

            Color runeColor;
            if (currentTheme == SkyboxTheme.BloodEclipse)
                runeColor = new Color(1.0f, 0.15f, 0.35f);
            else if (currentTheme == SkyboxTheme.GoldenSunset || (hour >= 17.0f && hour < 19.5f))
                runeColor = new Color(1.0f, 0.70f, 0.30f);
            else if (currentTheme == SkyboxTheme.MysticAurora || (hour >= 19.5f && hour < 22.0f))
                runeColor = new Color(0.20f, 0.85f, 0.75f);
            else
                runeColor = new Color(0.25f, 0.85f, 1.0f);

            float activeRuneIntensity = runicGlowEnabled ? (darkness * nightGlowIntensity * 2.2f) : 0f;

            if (daisRuneCircleRenderer != null && daisRuneCircleRenderer.sharedMaterial != null)
            {
                daisRuneCircleRenderer.sharedMaterial.SetColor("_GlowColor", runeColor);
                daisRuneCircleRenderer.sharedMaterial.SetFloat("_GlowIntensity", activeRuneIntensity);
                daisRuneCircleRenderer.sharedMaterial.SetFloat("_MasterAlpha", runicGlowEnabled ? Mathf.Clamp01(darkness * 1.4f + 0.05f) : 0f);
            }

            if (daisUnderLight != null)
            {
                daisUnderLight.color = runeColor;
                daisUnderLight.intensity = runicGlowEnabled ? (darkness * nightGlowIntensity * 0.95f) : 0f;
                daisUnderLight.enabled = runicGlowEnabled && darkness > 0.05f;
            }

            // Modulate fireflies emission based on darkness
            if (starlightParticles != null)
            {
                var emission = starlightParticles.emission;
                emission.rateOverTime = (runicGlowEnabled ? darkness : 0.1f) * 28f;
                var main = starlightParticles.main;
                main.startColor = runeColor * 1.25f;
            }
        }
    }
}
