using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Audio;

namespace BattleSummoner.Intro
{
    /// <summary>
    /// AccountInductionUI handles the first-time user account creation and character setup:
    /// - Summoner Moniker input field.
    /// - Gender Archetype selection (Male Heroic Athlete vs Female Graceful Mage).
    /// - Dimensional Affinity selection (Solar Dawn, Lunar Mist, Astral Spire, Void Spark).
    /// - Live 3D Avatar holographic turntable preview.
    /// - Real-time 12-Dimensional Telemetry computation.
    /// - "LINK START / ENTER WORLD" induction execution.
    /// </summary>
    public class AccountInductionUI : MonoBehaviour
    {
        [Header("UI Controls")]
        public TMP_InputField monikerInput;
        public Button btnSelectMale;
        public Button btnSelectFemale;
        public Button[] affinityButtons;
        public TextMeshProUGUI telemetryPreviewText;
        public Button btnLinkStart;
        public TextMeshProUGUI archetypeDescText;

        [Header("3D Avatar Hologram References")]
        public GameObject maleHologramInstance;
        public GameObject femaleHologramInstance;
        public Transform avatarStageRoot;
        public float avatarRotateSpeed = 20f;

        [Header("State")]
        public string summonerName = "Kirito";
        public bool isFemale = false;
        public string selectedAffinity = "SOLAR_DAWN";

        private void Start()
        {
            if (monikerInput != null)
            {
                monikerInput.text = PlayerPrefs.GetString("SummonerName", "Asuna");
                monikerInput.onValueChanged.AddListener(OnMonikerChanged);
            }

            if (btnSelectMale != null) btnSelectMale.onClick.AddListener(() => SetArchetype(false));
            if (btnSelectFemale != null) btnSelectFemale.onClick.AddListener(() => SetArchetype(true));

            if (btnLinkStart != null) btnLinkStart.onClick.AddListener(OnLinkStartClicked);

            SetArchetype(isFemale);
            UpdateTelemetry();
        }

        private void Update()
        {
            // Gentle continuous holographic rotation of avatar
            if (avatarStageRoot != null)
            {
                avatarStageRoot.Rotate(Vector3.up, avatarRotateSpeed * Time.deltaTime, Space.World);
            }
        }

        public void SetArchetype(bool female)
        {
            isFemale = female;

            if (maleHologramInstance != null) maleHologramInstance.SetActive(!isFemale);
            if (femaleHologramInstance != null) femaleHologramInstance.SetActive(isFemale);

            if (archetypeDescText != null)
            {
                archetypeDescText.text = isFemale
                    ? "<color=#F472B6>GRACEFUL MAGE</color>\n<i>High Ether Channeling, Astral Resonant Agility, Celestial Precision.</i>"
                    : "<color=#38BDF8>HEROIC ATHLETE</color>\n<i>High Physical Inertia, Pectoral & Core Kinetic Force, Swordmaster Stance.</i>";
            }

            // Update button visual states
            if (btnSelectMale != null)
            {
                var cb = btnSelectMale.colors;
                cb.normalColor = !isFemale ? new Color(0.18f, 0.45f, 0.70f) : new Color(0.10f, 0.15f, 0.22f);
                btnSelectMale.colors = cb;
            }
            if (btnSelectFemale != null)
            {
                var cb = btnSelectFemale.colors;
                cb.normalColor = isFemale ? new Color(0.70f, 0.22f, 0.45f) : new Color(0.10f, 0.15f, 0.22f);
                btnSelectFemale.colors = cb;
            }

            LoginAudioSynthesizer.Instance?.PlayUIClick();
            UpdateTelemetry();
        }

        public void SetAffinity(string affinity)
        {
            selectedAffinity = affinity;
            LoginAudioSynthesizer.Instance?.PlayUIClick();
            UpdateTelemetry();
        }

        private void OnMonikerChanged(string val)
        {
            summonerName = string.IsNullOrEmpty(val) ? "Summoner" : val;
            UpdateTelemetry();
        }

        private void UpdateTelemetry()
        {
            if (telemetryPreviewText == null) return;

            string nameStr = string.IsNullOrEmpty(summonerName) ? "Summoner" : summonerName;
            int nameHash = nameStr.GetHashCode();
            float baseMass = isFemale ? 56.5f : 78.0f;
            float mass = baseMass + (Mathf.Abs(nameHash % 1500) / 100f);
            float volume = isFemale ? 0.058f : 0.082f;
            float density = mass / volume;
            float resonance = 137.5f + (Mathf.Abs(nameHash % 400) / 10f);

            var sb = new StringBuilder();
            sb.AppendLine("<b><color=#38BDF8>✦ INDUCTION TELEMETRY ✦</color></b>");
            sb.AppendLine($"<color=#94A3B8>Summoner ID:</color> <b><color=#FACC15>{nameStr.ToUpper()}</color></b>");
            sb.AppendLine($"<color=#94A3B8>Soul Archetype:</color> <b>{(isFemale ? "FEMALE_GRACEFUL_MAGE" : "MALE_HEROIC_ATHLETE")}</b>");
            sb.AppendLine($"<color=#94A3B8>Dimensional Affinity:</color> <b><color=#4ADE80>{selectedAffinity}</color></b>");
            sb.AppendLine($"<color=#94A3B8>4D Inertial Mass:</color> <b>{mass:F1} kg</b>");
            sb.AppendLine($"<color=#94A3B8>5D Soul Density:</color> <b>{density:F1} kg/m³</b>");
            sb.AppendLine($"<color=#94A3B8>10D Resonance Hash:</color> <b>#{nameHash:X8}</b>");
            sb.AppendLine($"<color=#94A3B8>11D Security Handshake:</color> <b><color=#22C55E>AUTHENTICATED</color></b>");

            telemetryPreviewText.text = sb.ToString();
        }

        private void OnLinkStartClicked()
        {
            LoginAudioSynthesizer.Instance?.PlayUIClick();
            LoginAudioSynthesizer.Instance?.PlayInductionFanfare();

            // Save player account & archetype induction
            PlayerPrefs.SetString("SummonerName", summonerName);
            PlayerPrefs.SetString("SummonerArchetype", isFemale ? "Female" : "Male");
            PlayerPrefs.SetString("SummonerAffinity", selectedAffinity);
            PlayerPrefs.SetInt("FirstTimeUserCompleted", 1);
            PlayerPrefs.Save();

            WorldEntrySequenceManager.Instance?.OnAccountInductionComplete();
        }
    }
}
