using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Customization
{
    public enum ExpressionPreset
    {
        Neutral,
        Joy,
        Angry,
        Surprised,
        Determined,
        Sad,
        Smug,
        Thinking,
        Skeptical
    }

    public enum VisemeVowel
    {
        Sil, // Closed / silence
        A,   // Aa (wide open jaw)
        I,   // Ee (wide horizontal smile)
        U,   // Oo (pursed rounded lips)
        E,   // Eh (medium relaxed open)
        O    // Oh (tall vertical oval)
    }

    /// <summary>
    /// FacialExpressionController handles expressive facial animation on anime character models:
    /// - One-click expression presets (Joy, Angry, Surprised, Determined, Sad, Smug, Thinking)
    /// - On-the-fly eyebrow manipulation (height, furrow/anger tilt, sadness slant, arch)
    /// - On-the-fly mouth shapes (smile, frown, open, pout, smirk)
    /// - 5-vowel anime viseme lip-sync engine (A, I, U, E, O) with procedural dialogue speech timing
    /// - Autonomous natural micro-blinking
    /// - Supports both SkinnedMeshRenderer blendshapes and physical facial bones
    /// </summary>
    [ExecuteAlways]
    public class FacialExpressionController : MonoBehaviour
    {
        [Header("Mesh & Target Renderer")]
        public SkinnedMeshRenderer targetSkinnedMesh;
        public Renderer mouthCavityRenderer;

        [Header("Physical Mouth & Facial Rig Bones (Optional)")]
        public Transform jawBone;
        public Transform lipUpperBone;
        public Transform lipLowerBone;
        public Transform mouthCornerLeftBone;
        public Transform mouthCornerRightBone;
        public Transform browLeftBone;
        public Transform browRightBone;

        [Header("Active Expression Preset")]
        [SerializeField] private ExpressionPreset currentPreset = ExpressionPreset.Neutral;

        [Header("On-The-Fly Eyebrow Controls (-1 to 1)")]
        [Range(-1f, 1f)] public float browHeightL = 0f;
        [Range(-1f, 1f)] public float browHeightR = 0f;
        [Range(-1f, 1f)] public float browAngleL = 0f; // >0 = Angry/Furrow, <0 = Sad/Worry
        [Range(-1f, 1f)] public float browAngleR = 0f;

        [Header("On-The-Fly Eye Controls (0 to 1)")]
        [Range(0f, 1f)] public float eyeBlinkL = 0f;
        [Range(0f, 1f)] public float eyeBlinkR = 0f;
        [Range(0f, 1f)] public float eyeWide = 0f;
        public bool enableAutoBlink = true;

        [Header("On-The-Fly Mouth Shapes (-1 to 1)")]
        [Range(-1f, 1f)] public float mouthSmile = 0f; // >0 = Smile, <0 = Frown
        [Range(0f, 1f)] public float mouthOpen = 0f;
        [Range(0f, 1f)] public float mouthPout = 0f;
        [Range(0f, 1f)] public float smirkL = 0f;
        [Range(0f, 1f)] public float smirkR = 0f;

        [Header("Vowel Visemes (0 to 100)")]
        [Range(0f, 100f)] public float vowelA = 0f;
        [Range(0f, 100f)] public float vowelI = 0f;
        [Range(0f, 100f)] public float vowelU = 0f;
        [Range(0f, 100f)] public float vowelE = 0f;
        [Range(0f, 100f)] public float vowelO = 0f;

        [Header("Blender Baked Animation Clips")]
        public AnimationClip vowelSpeechCycleClip;
        public AnimationClip dialogueIntroClip;
        public AnimationClip dialogueThinkingClip;
        public AnimationClip dialogueDeterminedClip;
        public AnimationClip expressionJoyClip;

        [Header("Speech Audio / Blip Source (Optional)")]
        public AudioSource speechAudioSource;
        public AudioClip[] speechChimes;

        // Internal BlendShape index caching
        private readonly Dictionary<string, int> shapeIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private MaterialPropertyBlock propBlock;

        // Transition Coroutine
        private Coroutine activeExpressionRoutine;
        private Coroutine activeSpeechRoutine;

        // Auto Blink Timing
        private float nextBlinkTime;
        private float blinkTimer;
        private bool isBlinking;

        // Initial Bone Transforms
        private Vector3 initJawPos, initLipUpperPos, initLipLowerPos, initCornerLPos, initCornerRPos, initBrowLPos, initBrowRPos;
        private Quaternion initJawRot;

        public ExpressionPreset CurrentPreset => currentPreset;
        public bool IsSpeaking => activeSpeechRoutine != null;

        private void Awake()
        {
            InitializeRenderer();
            AutoDetectFacialBones();
            ScheduleNextBlink();
        }

        private void Start()
        {
            InitializeRenderer();
            AutoDetectFacialBones();
        }

        public void InitializeRenderer()
        {
            if (targetSkinnedMesh == null)
            {
                targetSkinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            }

            shapeIndices.Clear();
            if (targetSkinnedMesh != null && targetSkinnedMesh.sharedMesh != null)
            {
                var mesh = targetSkinnedMesh.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    shapeIndices[mesh.GetBlendShapeName(i)] = i;
                }
            }
        }

        public void AutoDetectFacialBones()
        {
            var allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                string n = t.name.ToLower();
                if (jawBone == null && (n.Contains("jaw_master") || n.Contains("jaw_bone") || n == "jaw")) jawBone = t;
                else if (lipUpperBone == null && (n.Contains("lip_upper_mid") || n.Contains("lip_upper"))) lipUpperBone = t;
                else if (lipLowerBone == null && (n.Contains("lip_lower_mid") || n.Contains("lip_lower"))) lipLowerBone = t;
                else if (mouthCornerLeftBone == null && (n.Contains("mouth_corner_left") || n.Contains("mouth_corner_l"))) mouthCornerLeftBone = t;
                else if (mouthCornerRightBone == null && (n.Contains("mouth_corner_right") || n.Contains("mouth_corner_r"))) mouthCornerRightBone = t;
                else if (browLeftBone == null && (n.Contains("brow_left") || n.Contains("brow_bone_left"))) browLeftBone = t;
                else if (browRightBone == null && (n.Contains("brow_right") || n.Contains("brow_bone_right"))) browRightBone = t;
            }
            CacheBoneTransforms();
        }

        private void CacheBoneTransforms()
        {
            if (jawBone != null) { initJawPos = jawBone.localPosition; initJawRot = jawBone.localRotation; }
            if (lipUpperBone != null) initLipUpperPos = lipUpperBone.localPosition;
            if (lipLowerBone != null) initLipLowerPos = lipLowerBone.localPosition;
            if (mouthCornerLeftBone != null) initCornerLPos = mouthCornerLeftBone.localPosition;
            if (mouthCornerRightBone != null) initCornerRPos = mouthCornerRightBone.localPosition;
            if (browLeftBone != null) initBrowLPos = browLeftBone.localPosition;
            if (browRightBone != null) initBrowRPos = browRightBone.localPosition;
        }

        private void Update()
        {
            if (Application.isPlaying && enableAutoBlink && !isBlinking)
            {
                if (Time.time >= nextBlinkTime)
                {
                    StartCoroutine(AutoBlinkRoutine());
                }
            }

            ApplyFacialState();
        }

        private void LateUpdate()
        {
            ApplyBoneRigging();
        }

        #region BlendShape & State Application

        public void ApplyFacialState()
        {
            if (targetSkinnedMesh == null) return;

            // Vowels
            SetBlendShape("Mouth_A", vowelA + (mouthOpen * 70f));
            SetBlendShape("Mouth_I", vowelI);
            SetBlendShape("Mouth_U", vowelU);
            SetBlendShape("Mouth_E", vowelE);
            SetBlendShape("Mouth_O", vowelO);

            // Mouth Shapes
            SetBlendShape("Mouth_Smile", Mathf.Max(0f, mouthSmile) * 100f);
            SetBlendShape("Mouth_Frown", Mathf.Max(0f, -mouthSmile) * 100f);
            SetBlendShape("Mouth_Open", mouthOpen * 100f);
            SetBlendShape("Mouth_Pout", mouthPout * 100f);
            SetBlendShape("Mouth_Smirk_L", smirkL * 100f);
            SetBlendShape("Mouth_Smirk_R", smirkR * 100f);

            // Eyebrows
            SetBlendShape("Brow_Raise_L", Mathf.Max(0f, browHeightL) * 100f);
            SetBlendShape("Brow_Raise_R", Mathf.Max(0f, browHeightR) * 100f);
            SetBlendShape("Brow_Lower_L", Mathf.Max(0f, -browHeightL) * 100f);
            SetBlendShape("Brow_Lower_R", Mathf.Max(0f, -browHeightR) * 100f);

            SetBlendShape("Brow_Angry_L", Mathf.Max(0f, browAngleL) * 100f);
            SetBlendShape("Brow_Angry_R", Mathf.Max(0f, browAngleR) * 100f);
            SetBlendShape("Brow_Sad_L", Mathf.Max(0f, -browAngleL) * 100f);
            SetBlendShape("Brow_Sad_R", Mathf.Max(0f, -browAngleR) * 100f);

            // Eyes
            SetBlendShape("Eye_Blink_L", eyeBlinkL * 100f);
            SetBlendShape("Eye_Blink_R", eyeBlinkR * 100f);
            SetBlendShape("Eye_Wide_L", eyeWide * 100f);
            SetBlendShape("Eye_Wide_R", eyeWide * 100f);

            // Drive Stylized Anime Mouth Cavity Shader (if attached)
            if (mouthCavityRenderer != null)
            {
                if (propBlock == null) propBlock = new MaterialPropertyBlock();
                mouthCavityRenderer.GetPropertyBlock(propBlock);
                propBlock.SetFloat("_VowelA", vowelA * 0.01f);
                propBlock.SetFloat("_VowelI", vowelI * 0.01f);
                propBlock.SetFloat("_VowelU", vowelU * 0.01f);
                propBlock.SetFloat("_VowelE", vowelE * 0.01f);
                propBlock.SetFloat("_VowelO", vowelO * 0.01f);
                propBlock.SetFloat("_Smile", mouthSmile);
                propBlock.SetFloat("_Open", mouthOpen);
                mouthCavityRenderer.SetPropertyBlock(propBlock);
            }
        }

        private void SetBlendShape(string shapeName, float weight)
        {
            if (targetSkinnedMesh == null) return;
            float clampedWeight = Mathf.Clamp(weight, 0f, 100f);

            if (shapeIndices.TryGetValue(shapeName, out int index))
            {
                targetSkinnedMesh.SetBlendShapeWeight(index, clampedWeight);
                return;
            }

            // Check Blender naming fallback (e.g., Mouth_A -> Vowel_A, Mouth_Smile -> Mouth_Smile, etc.)
            if (shapeName.StartsWith("Mouth_") && shapeName.Length == 7) // e.g. Mouth_A
            {
                string vowelName = "Vowel_" + shapeName.Substring(6);
                if (shapeIndices.TryGetValue(vowelName, out index))
                {
                    targetSkinnedMesh.SetBlendShapeWeight(index, clampedWeight);
                    return;
                }
            }

            string alt = shapeName.StartsWith("Mouth_") ? shapeName.Substring(6) : "Mouth_" + shapeName;
            if (shapeIndices.TryGetValue(alt, out index))
            {
                targetSkinnedMesh.SetBlendShapeWeight(index, clampedWeight);
            }
        }

        /// <summary>
        /// Plays a baked Blender animation action clip directly on the character.
        /// </summary>
        public void PlayBlenderAction(string clipName)
        {
            var anim = GetComponent<UnityEngine.Animation>();
            if (anim == null) anim = gameObject.AddComponent<UnityEngine.Animation>();

            AnimationClip clipToPlay = null;
            string lower = clipName.ToLower();
            if (lower.Contains("vowel") || lower.Contains("cycle")) clipToPlay = vowelSpeechCycleClip;
            else if (lower.Contains("intro") || lower.Contains("where")) clipToPlay = dialogueIntroClip;
            else if (lower.Contains("thinking") || lower.Contains("spire")) clipToPlay = dialogueThinkingClip;
            else if (lower.Contains("determined") || lower.Contains("conquer")) clipToPlay = dialogueDeterminedClip;
            else if (lower.Contains("joy") || lower.Contains("smile")) clipToPlay = expressionJoyClip;

            if (clipToPlay != null)
            {
                if (anim.GetClip(clipToPlay.name) == null)
                {
                    anim.AddClip(clipToPlay, clipToPlay.name);
                }
                anim.Play(clipToPlay.name);
            }
        }

        private void ApplyBoneRigging()
        {
            // Drive physical bones if present in character armature
            if (jawBone != null)
            {
                float totalOpen = Mathf.Clamp01((vowelA * 0.01f) + mouthOpen + (vowelO * 0.008f));
                jawBone.localRotation = initJawRot * Quaternion.Euler(totalOpen * 14f, 0f, 0f);
                jawBone.localPosition = initJawPos + new Vector3(0f, -totalOpen * 0.015f, totalOpen * 0.005f);
            }

            if (mouthCornerLeftBone != null)
            {
                float stretchX = (vowelI * 0.0001f) + (mouthSmile * 0.004f) - (vowelU * 0.0001f);
                float offsetY = (mouthSmile * 0.006f) + (smirkL * 0.008f);
                mouthCornerLeftBone.localPosition = initCornerLPos + new Vector3(-stretchX, offsetY, 0f);
            }

            if (mouthCornerRightBone != null)
            {
                float stretchX = (vowelI * 0.0001f) + (mouthSmile * 0.004f) - (vowelU * 0.0001f);
                float offsetY = (mouthSmile * 0.006f) + (smirkR * 0.008f);
                mouthCornerRightBone.localPosition = initCornerRPos + new Vector3(stretchX, offsetY, 0f);
            }

            if (browLeftBone != null)
            {
                browLeftBone.localPosition = initBrowLPos + new Vector3(0f, browHeightL * 0.015f, 0f);
                browLeftBone.localRotation = Quaternion.Euler(0f, 0f, browAngleL * 12f);
            }

            if (browRightBone != null)
            {
                browRightBone.localPosition = initBrowRPos + new Vector3(0f, browHeightR * 0.015f, 0f);
                browRightBone.localRotation = Quaternion.Euler(0f, 0f, -browAngleR * 12f);
            }
        }

        #endregion

        #region Expression Preset API

        /// <summary>
        /// Instantly sets or smoothly transitions to a defined emotional expression preset.
        /// </summary>
        public void SetExpression(ExpressionPreset preset, float duration = 0.25f)
        {
            currentPreset = preset;
            GetPresetValues(preset, out float tBrowH_L, out float tBrowH_R, out float tBrowA_L, out float tBrowA_R,
                out float tSmile, out float tOpen, out float tWide, out float tSmirkL, out float tSmirkR);

            if (duration <= 0f || !Application.isPlaying)
            {
                browHeightL = tBrowH_L;
                browHeightR = tBrowH_R;
                browAngleL = tBrowA_L;
                browAngleR = tBrowA_R;
                mouthSmile = tSmile;
                mouthOpen = tOpen;
                eyeWide = tWide;
                smirkL = tSmirkL;
                smirkR = tSmirkR;
                ApplyFacialState();
                return;
            }

            if (activeExpressionRoutine != null) StopCoroutine(activeExpressionRoutine);
            activeExpressionRoutine = StartCoroutine(ExpressionTransitionRoutine(
                tBrowH_L, tBrowH_R, tBrowA_L, tBrowA_R, tSmile, tOpen, tWide, tSmirkL, tSmirkR, duration));
        }

        private IEnumerator ExpressionTransitionRoutine(
            float tBrowH_L, float tBrowH_R, float tBrowA_L, float tBrowA_R,
            float tSmile, float tOpen, float tWide, float tSmirkL, float tSmirkR, float duration)
        {
            float sBrowH_L = browHeightL, sBrowH_R = browHeightR;
            float sBrowA_L = browAngleL, sBrowA_R = browAngleR;
            float sSmile = mouthSmile, sOpen = mouthOpen, sWide = eyeWide;
            float sSmirkL = smirkL, sSmirkR = smirkR;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                browHeightL = Mathf.Lerp(sBrowH_L, tBrowH_L, t);
                browHeightR = Mathf.Lerp(sBrowH_R, tBrowH_R, t);
                browAngleL = Mathf.Lerp(sBrowA_L, tBrowA_L, t);
                browAngleR = Mathf.Lerp(sBrowA_R, tBrowA_R, t);
                mouthSmile = Mathf.Lerp(sSmile, tSmile, t);
                mouthOpen = Mathf.Lerp(sOpen, tOpen, t);
                eyeWide = Mathf.Lerp(sWide, tWide, t);
                smirkL = Mathf.Lerp(sSmirkL, tSmirkL, t);
                smirkR = Mathf.Lerp(sSmirkR, tSmirkR, t);

                yield return null;
            }

            browHeightL = tBrowH_L;
            browHeightR = tBrowH_R;
            browAngleL = tBrowA_L;
            browAngleR = tBrowA_R;
            mouthSmile = tSmile;
            mouthOpen = tOpen;
            eyeWide = tWide;
            smirkL = tSmirkL;
            smirkR = tSmirkR;
            activeExpressionRoutine = null;
        }

        public static void GetPresetValues(ExpressionPreset preset,
            out float browHL, out float browHR, out float browAL, out float browAR,
            out float smile, out float open, out float wide, out float smirkL, out float smirkR)
        {
            browHL = browHR = browAL = browAR = smile = open = wide = smirkL = smirkR = 0f;

            switch (preset)
            {
                case ExpressionPreset.Neutral:
                    break;
                case ExpressionPreset.Joy:
                    browHL = browHR = 0.35f;
                    browAL = browAR = -0.15f;
                    smile = 0.85f;
                    open = 0.15f;
                    break;
                case ExpressionPreset.Angry:
                    browHL = browHR = -0.40f;
                    browAL = browAR = 0.85f;
                    smile = -0.55f;
                    open = 0.20f;
                    break;
                case ExpressionPreset.Surprised:
                    browHL = browHR = 0.80f;
                    browAL = browAR = -0.20f;
                    wide = 0.90f;
                    open = 0.55f;
                    break;
                case ExpressionPreset.Determined:
                    browHL = browHR = -0.20f;
                    browAL = browAR = 0.50f;
                    smile = 0.15f;
                    break;
                case ExpressionPreset.Sad:
                    browHL = browHR = -0.15f;
                    browAL = browAR = -0.75f;
                    smile = -0.65f;
                    open = 0.08f;
                    break;
                case ExpressionPreset.Smug:
                    browHL = 0.40f; browHR = -0.10f;
                    browAL = 0.10f; browAR = 0.30f;
                    smirkR = 0.75f;
                    smile = 0.35f;
                    break;
                case ExpressionPreset.Thinking:
                    browHL = 0.50f; browHR = -0.30f;
                    browAL = -0.30f; browAR = 0.40f;
                    smile = -0.10f;
                    open = 0.05f;
                    break;
                case ExpressionPreset.Skeptical:
                    browHL = 0.65f; browHR = -0.35f;
                    browAL = -0.20f; browAR = 0.60f;
                    smirkL = 0.50f;
                    smile = -0.20f;
                    break;
            }
        }

        #endregion

        #region Vowel Viseme & Speech Lip-Flap Engine

        /// <summary>
        /// Explicitly set a specific vowel viseme weight (0 to 100).
        /// </summary>
        public void SetVowel(VisemeVowel vowel, float weight)
        {
            vowelA = vowel == VisemeVowel.A ? weight : 0f;
            vowelI = vowel == VisemeVowel.I ? weight : 0f;
            vowelU = vowel == VisemeVowel.U ? weight : 0f;
            vowelE = vowel == VisemeVowel.E ? weight : 0f;
            vowelO = vowel == VisemeVowel.O ? weight : 0f;
        }

        /// <summary>
        /// Clears all vowel mouth openings to neutral resting position.
        /// </summary>
        public void ClearVowels()
        {
            vowelA = vowelI = vowelU = vowelE = vowelO = 0f;
        }

        /// <summary>
        /// Plays animated phonetic vowel lip-sync for a given dialogue sentence over a specified duration.
        /// </summary>
        public void SpeakDialogue(string dialogueText, float duration, Action onComplete = null)
        {
            if (activeSpeechRoutine != null) StopCoroutine(activeSpeechRoutine);
            activeSpeechRoutine = StartCoroutine(SpeechLipSyncRoutine(dialogueText, duration, onComplete));
        }

        public void StopSpeaking()
        {
            if (activeSpeechRoutine != null)
            {
                StopCoroutine(activeSpeechRoutine);
                activeSpeechRoutine = null;
            }
            ClearVowels();
        }

        private IEnumerator SpeechLipSyncRoutine(string dialogueText, float totalDuration, Action onComplete)
        {
            if (string.IsNullOrEmpty(dialogueText) || totalDuration <= 0f)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Extract vowel sequence from dialogue
            var vowels = ParseTextToVowels(dialogueText);
            if (vowels.Count == 0)
            {
                vowels = new List<VisemeVowel> { VisemeVowel.A, VisemeVowel.E, VisemeVowel.I, VisemeVowel.O, VisemeVowel.A };
            }

            float stepTime = Mathf.Clamp(totalDuration / (vowels.Count + 1), 0.08f, 0.22f);
            int vowelIndex = 0;
            float elapsed = 0f;

            VisemeVowel currentVowel = VisemeVowel.Sil;
            VisemeVowel targetVowel = vowels[0];

            float vowelTimer = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                vowelTimer += Time.deltaTime;

                if (vowelTimer >= stepTime)
                {
                    vowelTimer = 0f;
                    vowelIndex++;
                    currentVowel = targetVowel;

                    if (vowelIndex < vowels.Count)
                    {
                        targetVowel = vowels[vowelIndex];
                        PlaySpeechBlip();
                    }
                    else
                    {
                        targetVowel = VisemeVowel.Sil;
                    }
                }

                float t = Mathf.Clamp01(vowelTimer / stepTime);
                float flap = Mathf.Sin(t * Mathf.PI); // Natural open-close arch

                // Lerp vowel shapes
                vowelA = Mathf.Lerp(GetVowelWeight(currentVowel, VisemeVowel.A), GetVowelWeight(targetVowel, VisemeVowel.A), t) * flap;
                vowelI = Mathf.Lerp(GetVowelWeight(currentVowel, VisemeVowel.I), GetVowelWeight(targetVowel, VisemeVowel.I), t) * flap;
                vowelU = Mathf.Lerp(GetVowelWeight(currentVowel, VisemeVowel.U), GetVowelWeight(targetVowel, VisemeVowel.U), t) * flap;
                vowelE = Mathf.Lerp(GetVowelWeight(currentVowel, VisemeVowel.E), GetVowelWeight(targetVowel, VisemeVowel.E), t) * flap;
                vowelO = Mathf.Lerp(GetVowelWeight(currentVowel, VisemeVowel.O), GetVowelWeight(targetVowel, VisemeVowel.O), t) * flap;

                yield return null;
            }

            // Smooth close
            float closeElapsed = 0f;
            float closeDur = 0.12f;
            float curA = vowelA, curI = vowelI, curU = vowelU, curE = vowelE, curO = vowelO;
            while (closeElapsed < closeDur)
            {
                closeElapsed += Time.deltaTime;
                float ct = 1f - (closeElapsed / closeDur);
                vowelA = curA * ct;
                vowelI = curI * ct;
                vowelU = curU * ct;
                vowelE = curE * ct;
                vowelO = curO * ct;
                yield return null;
            }

            ClearVowels();
            activeSpeechRoutine = null;
            onComplete?.Invoke();
        }

        private float GetVowelWeight(VisemeVowel v, VisemeVowel match)
        {
            if (v == match)
            {
                return match == VisemeVowel.A ? 85f :
                       match == VisemeVowel.I ? 75f :
                       match == VisemeVowel.U ? 65f :
                       match == VisemeVowel.E ? 70f : 80f;
            }
            return 0f;
        }

        private List<VisemeVowel> ParseTextToVowels(string text)
        {
            var list = new List<VisemeVowel>();
            foreach (char c in text.ToLowerInvariant())
            {
                switch (c)
                {
                    case 'a': list.Add(VisemeVowel.A); break;
                    case 'e': list.Add(VisemeVowel.E); break;
                    case 'i': case 'y': list.Add(VisemeVowel.I); break;
                    case 'o': list.Add(VisemeVowel.O); break;
                    case 'u': case 'w': list.Add(VisemeVowel.U); break;
                    case ' ':
                        if (list.Count > 0 && list[list.Count - 1] != VisemeVowel.Sil)
                            list.Add(VisemeVowel.Sil);
                        break;
                }
            }
            return list;
        }

        private void PlaySpeechBlip()
        {
            if (speechAudioSource != null && speechChimes != null && speechChimes.Length > 0)
            {
                var clip = speechChimes[UnityEngine.Random.Range(0, speechChimes.Length)];
                speechAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                speechAudioSource.PlayOneShot(clip, 0.4f);
            }
        }

        #endregion

        #region Auto Blinking

        private void ScheduleNextBlink()
        {
            nextBlinkTime = Time.time + UnityEngine.Random.Range(2.8f, 5.2f);
        }

        private IEnumerator AutoBlinkRoutine()
        {
            isBlinking = true;
            float blinkDuration = 0.14f;
            float halfDuration = blinkDuration * 0.5f;

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                eyeBlinkL = eyeBlinkR = Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                eyeBlinkL = eyeBlinkR = Mathf.SmoothStep(1f, 0f, t);
                yield return null;
            }

            eyeBlinkL = eyeBlinkR = 0f;
            isBlinking = false;
            ScheduleNextBlink();
        }

        #endregion
    }
}
