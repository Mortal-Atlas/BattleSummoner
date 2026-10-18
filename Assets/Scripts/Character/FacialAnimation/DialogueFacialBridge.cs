using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleSummoner.Character.FacialAnimation
{
    public class DialogueFacialBridge : MonoBehaviour
    {
        [Header("Pipeline References")]
        public UnfilteredDialoguePipeline dialoguePipeline;
        public BattleSummonerAudioMaster audioMaster;
        public AudioSource primaryVocalAudioSource;

        [Header("Active Speaker Binding")]
        public ModularFaceRigController currentSpeakerFaceRig;

        public async Task ExecuteFullEmotionalDialogueAsync(
            ModularFaceRigController speakerRig,
            string systemPersonaPrompt,
            string playerUtterance,
            string elevenLabsVoiceId)
        {
            currentSpeakerFaceRig = speakerRig;

            string enhancedPrompt = systemPersonaPrompt +
                "\nPrepend your response with one facial emotion tag strictly in the format: " +
                "[EMOTION: Neutral|Happy|Angry|Sad|Surprised|Smug|HeartEyesBlush|CrazyOverload|PainHit]";

            string rawScript = await dialoguePipeline.GenerateDialogueAndCueStreamAsync(enhancedPrompt, playerUtterance);
            if (string.IsNullOrEmpty(rawScript)) return;

            // 1. Extract and apply [EMOTION: ...] tag to the character's 2-node face rig
            string scriptWithoutEmotion = currentSpeakerFaceRig != null
                ? currentSpeakerFaceRig.ParseAndStripEmotionTags(rawScript)
                : rawScript;

            // 2. Extract and schedule [SFX: ...] cues via BattleSummonerAudioMaster
            string cleanSpokenText = audioMaster != null
                ? audioMaster.ParseCuesAndStripText(scriptWithoutEmotion, out List<SfxCue> sfxCues)
                : scriptWithoutEmotion;

            // 3. Bind the vocal AudioSource to the speaker's face rig for real-time PCM vowel lip-sync
            if (currentSpeakerFaceRig != null && primaryVocalAudioSource != null)
            {
                currentSpeakerFaceRig.BindAudioSource(primaryVocalAudioSource);
            }

            // 4. Stream ElevenLabs TTS audio directly into the bound vocal source
            await dialoguePipeline.StreamVoicePlaybackAsync(cleanSpokenText, elevenLabsVoiceId, primaryVocalAudioSource);
        }
    }
}
