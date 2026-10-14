using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// SfxCueParser extracts timed audio triggers [SFX: preset | at: Xs | vol: Y%] from dialogue scripts.
/// </summary>
public static class SfxCueParser
{
    private static readonly Regex sfxRegex = new Regex(@"\[SFX:\s*([a-zA-Z0-9_\-]+)(?:\s*\|\s*at:\s*([0-9.]+)s?)?(?:\s*\|\s*vol:\s*([0-9.]+)%?)?\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string ParseCues(string rawScript, out List<SfxCue> cues)
    {
        cues = new List<SfxCue>();
        if (string.IsNullOrEmpty(rawScript)) return "";

        foreach (Match match in sfxRegex.Matches(rawScript))
        {
            float timestamp = match.Groups[2].Success && float.TryParse(match.Groups[2].Value, out float t) ? t : 0f;
            cues.Add(new SfxCue { preset = match.Groups[1].Value.ToLowerInvariant(), timestamp = timestamp });
        }

        return sfxRegex.Replace(rawScript, "").Trim();
    }
}
