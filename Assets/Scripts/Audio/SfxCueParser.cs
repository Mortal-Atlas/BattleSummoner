using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BattleSummoner.Audio
{
    [System.Serializable]
    public class SFXCue
    {
        public string id;
        public string rawName;
        public string preset;      // e.g. "cinematic_boom", "thunder", "whoosh", "riser", "glitch", "rain", "heartbeat", "chime", "laser", "drone", "camera_click", "typing"
        public float timestamp;   // Exact delay in seconds from speech start (T0)
        public float volume;      // Normalized volume (0.0 to 1.0)
        public float duration;    // Target duration in seconds
        public float pitch;       // Playback pitch multiplier (0.5 to 2.0)
    }

    /// <summary>
    /// SFXCueParser matches the exact cue syntax from VoxCraft AI:
    /// [SFX: preset_name | at: 0.0s | vol: 30% | len: 3s | pitch: 1.0]
    /// 
    /// Features:
    /// 1. High-precision regex parsing with flexible spacing and optional parameters.
    /// 2. Automatic contextual speech-rate timestamping (~2.3 words/sec) when 'at: Xs' is omitted.
    /// 3. Tag stripping to provide clean text for ElevenLabs TTS and UI captions.
    /// 4. Fuzzy preset matching mapping colloquial sound terms to engine audio presets.
    /// </summary>
    public static class SFXCueParser
    {
        // Regex pattern from VoxCraft AI
        private static readonly Regex SfxRegex = new Regex(
            @"\[SFX:\s*([a-zA-Z0-9_\-\s]+?)(?:\s*\|\s*at:\s*([0-9.]+)\s*s?)?(?:\s*\|\s*vol:\s*([0-9.]+)%?)?(?:\s*\|\s*len:\s*([0-9.]+)\s*s?)?(?:\s*\|\s*pitch:\s*([0-9.]+))?\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        // Average speech pacing: 140 words per minute ≈ 2.33 words/sec => ~0.43s per word
        public const float WordsPerSecond = 2.33f;
        public const float SecondsPerWord = 1f / WordsPerSecond;

        /// <summary>
        /// Strips all [SFX: ...] tags from the text, returning pure clean dialogue
        /// for ElevenLabs TTS synthesis and subtitle UI.
        /// </summary>
        public static string StripSfxTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string clean = SfxRegex.Replace(text, "");
            // Clean up double spaces created by tag removals
            clean = Regex.Replace(clean, @"\s+", " ").Trim();
            return clean;
        }

        /// <summary>
        /// Parses all [SFX: ...] tags from the raw text into structured SFXCue items.
        /// If an SFX tag is embedded within a sentence without an explicit 'at: Xs',
        /// its timestamp is calculated based on its position in the word stream.
        /// </summary>
        public static List<SFXCue> ParseSfxCues(string rawText)
        {
            var cues = new List<SFXCue>();
            if (string.IsNullOrEmpty(rawText)) return cues;

            MatchCollection matches = SfxRegex.Matches(rawText);
            if (matches.Count == 0) return cues;

            // To calculate auto timestamps, count words before each match
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                string rawPreset = match.Groups[1].Value.Trim().ToLowerInvariant().Replace(' ', '_');
                string matchedPreset = MatchClosestPreset(rawPreset);

                // Check explicit timestamp
                float timestamp = -1f;
                if (match.Groups[2].Success && float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedAt))
                {
                    timestamp = parsedAt;
                }
                else
                {
                    // Calculate contextual timestamp based on preceding words in clean text
                    string precedingText = rawText.Substring(0, match.Index);
                    string cleanPreceding = StripSfxTags(precedingText);
                    int wordCountBefore = string.IsNullOrWhiteSpace(cleanPreceding) 
                        ? 0 
                        : cleanPreceding.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    timestamp = wordCountBefore * SecondsPerWord;
                }

                // Volume (default 0.70 / 70% or parsed)
                float volume = 0.70f;
                if (match.Groups[3].Success && float.TryParse(match.Groups[3].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedVol))
                {
                    // Can be either 0-100 or 0-1.0
                    volume = parsedVol > 1.0f ? Mathf.Clamp01(parsedVol / 100f) : Mathf.Clamp01(parsedVol);
                }

                // Duration (default 2.5s)
                float duration = 2.5f;
                if (match.Groups[4].Success && float.TryParse(match.Groups[4].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedLen))
                {
                    duration = Mathf.Clamp(parsedLen, 0.2f, 15f);
                }

                // Pitch (default 1.0)
                float pitch = 1.0f;
                if (match.Groups[5].Success && float.TryParse(match.Groups[5].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedPitch))
                {
                    pitch = Mathf.Clamp(parsedPitch, 0.5f, 2.0f);
                }

                cues.Add(new SFXCue
                {
                    id = "sfx_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    rawName = match.Groups[1].Value.Trim(),
                    preset = matchedPreset,
                    timestamp = Mathf.Max(0f, timestamp),
                    volume = volume,
                    duration = duration,
                    pitch = pitch
                });
            }

            // Sort cues chronologically by timestamp
            cues.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            return cues;
        }

        /// <summary>
        /// Fuzzy match colloquial sound names to official VoxCraft & BattleSummoner presets.
        /// </summary>
        public static string MatchClosestPreset(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("boom") || n.Contains("impact") || n.Contains("explosion") || n.Contains("bass")) return "cinematic_boom";
            if (n.Contains("thunder") || n.Contains("lightning") || n.Contains("storm")) return "thunder";
            if (n.Contains("whoosh") || n.Contains("swoosh") || n.Contains("wind") || n.Contains("sweep")) return "whoosh";
            if (n.Contains("riser") || n.Contains("build") || n.Contains("tension")) return "riser";
            if (n.Contains("glitch") || n.Contains("cyber") || n.Contains("digital") || n.Contains("static")) return "glitch";
            if (n.Contains("rain") || n.Contains("water") || n.Contains("drizzle")) return "rain";
            if (n.Contains("heart") || n.Contains("pulse") || n.Contains("thump")) return "heartbeat";
            if (n.Contains("chime") || n.Contains("bell") || n.Contains("sparkle") || n.Contains("crystal")) return "chime";
            if (n.Contains("laser") || n.Contains("zap") || n.Contains("beam") || n.Contains("ray")) return "laser";
            if (n.Contains("drone") || n.Contains("hum") || n.Contains("ambient") || n.Contains("synth")) return "drone";
            if (n.Contains("camera") || n.Contains("shutter") || n.Contains("snap")) return "camera_click";
            if (n.Contains("type") || n.Contains("keyboard") || n.Contains("key") || n.Contains("click")) return "typing";
            
            // BattleSummoner Spire specific presets
            if (n.Contains("anvil") || n.Contains("forge") || n.Contains("hammer") || n.Contains("shatter")) return "anvil_shatter";
            if (n.Contains("singularity") || n.Contains("vortex") || n.Contains("blackhole") || n.Contains("whir")) return "singularity_whir";
            if (n.Contains("relic")) return "relic_chime";
            if (n.Contains("needle") || n.Contains("trap")) return "needle_trap";
            if (n.Contains("fanfare") || n.Contains("horn")) return "hunting_horn_fanfare";

            return "cinematic_boom";
        }
    }
}
