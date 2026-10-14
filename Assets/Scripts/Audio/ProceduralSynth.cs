using UnityEngine;

/// <summary>
/// ProceduralSynth provides real-time procedural waveform generation for tactile ASMR and UI feedback.
/// </summary>
public static class ProceduralSynth
{
    public static AudioClip GenerateSineTone(string clipName, float frequency, float duration, int sampleRate = 44100)
    {
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-4f * (t / duration));
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        AudioClip clip = AudioClip.Create(clipName, numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
