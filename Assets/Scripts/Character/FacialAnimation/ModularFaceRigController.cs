using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BattleSummoner.Character.FacialAnimation
{
    [DisallowMultipleComponent]
    public class ModularFaceRigController : MonoBehaviour
    {
        public enum FaceRenderMode
        {
            SpriteRendererStack2D,
            MeshUVAtlas3D
        }

        [Header("Core Configuration")]
        public FacialExpressionDatabaseSO expressionDB;
        public FaceRenderMode renderMode = FaceRenderMode.SpriteRendererStack2D;
        public FacialEmotion currentEmotion = FacialEmotion.Neutral;

        [Header("2D/2.5D SpriteRenderer Nodes (Head Bone Children)")]
        public Transform headAnchorTransform;
        public SortingGroup faceSortingGroup;
        public SpriteRenderer eyesAndBrowsRenderer;
        public SpriteRenderer mouthRenderer;

        [Header("Spatial Calibration (Normalized to Featureless Base Head)")]
        public Vector3 upperFaceLocalOffset = new Vector3(0f, 0.045f, -0.005f);
        public Vector3 lowerFaceLocalOffset = new Vector3(0f, -0.115f, -0.005f);
        public Vector3 faceNodesLocalScale = Vector3.one;

        [Header("3D Head Mesh Renderer (Optional UV Atlas Mode)")]
        public Renderer headMeshRenderer3D;
        public int headMaterialIndex = 0;

        [Header("HUD Persona Cut-In Sync (BattleViewportRebalancer)")]
        public Image hudPersonaEyesBrowsImage;
        public Image hudPersonaMouthImage;

        [Header("Autonomous Blink Settings")]
        public Vector2 blinkIntervalSeconds = new Vector2(2.6f, 5.4f);
        public float blinkClosedDuration = 0.085f;

        [Header("ElevenLabs Real-Time Lip-Sync Settings")]
        public AudioSource characterVocalSource;
        public float voiceGateRmsThreshold = 0.0075f;
        public float visemeHoldCadenceSeconds = 0.045f; // ~22 FPS anime mouth cadence

        // Pre-allocated 256-sample PCM buffer for zero-GC audio evaluation
        private readonly float[] _pcmSamples = new float[256];
        private MaterialPropertyBlock _mpb;
        private Coroutine _blinkRoutine;
        private bool _isBlinking;
        private bool _isSpeaking;
        private float _lipSyncTimer;

        private static readonly int UpperFaceSTProp = Shader.PropertyToID("_UpperFaceST");
        private static readonly int LowerFaceSTProp = Shader.PropertyToID("_LowerFaceST");
        private static readonly Regex EmotionTagRegex = new Regex(@"\[EMOTION:\s*([a-zA-Z0-9_]+)\]", RegexOptions.IgnoreCase);

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            ApplySpatialAnchors();
        }

        private void OnEnable()
        {
            ApplyEmotionVisuals(currentEmotion);
            _blinkRoutine = StartCoroutine(StochasticBlinkCoroutine());
        }

        private void OnDisable()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
        }

        private void Update()
        {
            EvaluateLipSyncStream();
        }

        [ContextMenu("Snap Face Nodes to Calibrated Head Offsets")]
        public void ApplySpatialAnchors()
        {
            if (eyesAndBrowsRenderer != null)
            {
                eyesAndBrowsRenderer.transform.localPosition = upperFaceLocalOffset;
                eyesAndBrowsRenderer.transform.localScale = faceNodesLocalScale;
            }

            if (mouthRenderer != null)
            {
                mouthRenderer.transform.localPosition = lowerFaceLocalOffset;
                mouthRenderer.transform.localScale = faceNodesLocalScale;
            }
        }

        public void BindAudioSource(AudioSource vocalSource)
        {
            characterVocalSource = vocalSource;
        }

        public void SetEmotion(FacialEmotion newEmotion)
        {
            currentEmotion = newEmotion;
            ApplyEmotionVisuals(currentEmotion);
        }

        /// <summary>
        /// Parses inline [EMOTION: Angry] tags from UnfilteredDialoguePipeline, updates the face state,
        /// and returns clean text for ElevenLabs TTS streaming.
        /// </summary>
        public string ParseAndStripEmotionTags(string rawDialogue)
        {
            if (string.IsNullOrEmpty(rawDialogue)) return rawDialogue;

            Match match = EmotionTagRegex.Match(rawDialogue);
            if (match.Success)
            {
                string tagValue = match.Groups[1].Value;
                if (Enum.TryParse(tagValue, true, out FacialEmotion parsedEmotion))
                {
                    SetEmotion(parsedEmotion);
                }
                return EmotionTagRegex.Replace(rawDialogue, "").Trim();
            }
            return rawDialogue;
        }

        private void ApplyEmotionVisuals(FacialEmotion emotion)
        {
            if (expressionDB == null) return;
            EmotionSpriteSet set = expressionDB.GetEmotionSet(emotion);

            Sprite upperSprite = (_isBlinking && set.eyesAndBrowsBlink != null)
                ? set.eyesAndBrowsBlink
                : set.eyesAndBrowsOpen;

            Vector2Int upperUV = _isBlinking ? set.uvAtlasBlinkCoord : set.uvAtlasOpenCoord;
            SetUpperFaceChannel(upperSprite, upperUV);

            if (!_isSpeaking)
            {
                SetLowerFaceChannel(set.restingEmotionMouth, set.uvAtlasRestingMouthCoord);
            }
        }

        private IEnumerator StochasticBlinkCoroutine()
        {
            while (true)
            {
                float wait = UnityEngine.Random.Range(blinkIntervalSeconds.x, blinkIntervalSeconds.y);
                yield return new WaitForSeconds(wait);

                if (expressionDB == null) continue;
                EmotionSpriteSet set = expressionDB.GetEmotionSet(currentEmotion);

                if (set.eyesAndBrowsBlink != null)
                {
                    _isBlinking = true;
                    SetUpperFaceChannel(set.eyesAndBrowsBlink, set.uvAtlasBlinkCoord);
                    yield return new WaitForSeconds(blinkClosedDuration);
                    _isBlinking = false;
                    SetUpperFaceChannel(set.eyesAndBrowsOpen, set.uvAtlasOpenCoord);
                }
            }
        }

        private void EvaluateLipSyncStream()
        {
            if (characterVocalSource == null || !characterVocalSource.isPlaying)
            {
                if (_isSpeaking)
                {
                    _isSpeaking = false;
                    ApplyEmotionVisuals(currentEmotion);
                }
                return;
            }

            _isSpeaking = true;
            _lipSyncTimer += Time.deltaTime;
            if (_lipSyncTimer < visemeHoldCadenceSeconds) return;
            _lipSyncTimer = 0f;

            characterVocalSource.GetOutputData(_pcmSamples, 0);

            float sumSquares = 0f;
            int zeroCrossings = 0;

            for (int i = 0; i < _pcmSamples.Length; i++)
            {
                float s = _pcmSamples[i];
                sumSquares += s * s;
                if (i > 0 && ((_pcmSamples[i - 1] >= 0f && s < 0f) || (_pcmSamples[i - 1] < 0f && s >= 0f)))
                {
                    zeroCrossings++;
                }
            }

            float rms = Mathf.Sqrt(sumSquares / _pcmSamples.Length);

            if (rms < voiceGateRmsThreshold)
            {
                ApplyViseme(MouthViseme.Closed_Rest);
                return;
            }

            MouthViseme selectedViseme;
            if (rms > 0.14f)
            {
                selectedViseme = zeroCrossings > 34 ? MouthViseme.Vowel_E_Mid : MouthViseme.Vowel_A_Wide;
            }
            else if (rms > 0.06f)
            {
                selectedViseme = zeroCrossings > 38 ? MouthViseme.Vowel_I_Small : MouthViseme.Vowel_O_Round;
            }
            else
            {
                selectedViseme = MouthViseme.Vowel_U_Round;
            }

            ApplyViseme(selectedViseme);
        }

        private void ApplyViseme(MouthViseme viseme)
        {
            if (expressionDB == null) return;
            Sprite visemeSprite = expressionDB.GetVisemeSprite(viseme);
            int idx = Mathf.Clamp((int)viseme, 0, expressionDB.phoneticMouthUVCoords.Length - 1);
            Vector2Int uvCoord = expressionDB.phoneticMouthUVCoords.Length > 0
                ? expressionDB.phoneticMouthUVCoords[idx]
                : Vector2Int.zero;

            SetLowerFaceChannel(visemeSprite, uvCoord);
        }

        private void SetUpperFaceChannel(Sprite sprite, Vector2Int uvCoord)
        {
            if (renderMode == FaceRenderMode.SpriteRendererStack2D && eyesAndBrowsRenderer != null && sprite != null)
            {
                eyesAndBrowsRenderer.sprite = sprite;
            }
            else if (renderMode == FaceRenderMode.MeshUVAtlas3D && headMeshRenderer3D != null && expressionDB != null)
            {
                headMeshRenderer3D.GetPropertyBlock(_mpb, headMaterialIndex);
                _mpb.SetVector(UpperFaceSTProp, expressionDB.GetAtlasST(uvCoord));
                headMeshRenderer3D.SetPropertyBlock(_mpb, headMaterialIndex);
            }

            if (hudPersonaEyesBrowsImage != null && sprite != null)
            {
                hudPersonaEyesBrowsImage.sprite = sprite;
            }
        }

        private void SetLowerFaceChannel(Sprite sprite, Vector2Int uvCoord)
        {
            if (renderMode == FaceRenderMode.SpriteRendererStack2D && mouthRenderer != null && sprite != null)
            {
                mouthRenderer.sprite = sprite;
            }
            else if (renderMode == FaceRenderMode.MeshUVAtlas3D && headMeshRenderer3D != null && expressionDB != null)
            {
                headMeshRenderer3D.GetPropertyBlock(_mpb, headMaterialIndex);
                _mpb.SetVector(LowerFaceSTProp, expressionDB.GetAtlasST(uvCoord));
                headMeshRenderer3D.SetPropertyBlock(_mpb, headMaterialIndex);
            }

            if (hudPersonaMouthImage != null && sprite != null)
            {
                hudPersonaMouthImage.sprite = sprite;
            }
        }
    }
}
