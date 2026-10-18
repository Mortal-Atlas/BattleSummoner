using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Character.FacialAnimation
{
    public enum FacialEmotion
    {
        Neutral = 0,
        Happy = 1,
        Angry = 2,
        Sad = 3,
        Surprised = 4,
        Smug = 5,
        HeartEyesBlush = 6,
        CrazyOverload = 7,
        PainHit = 8
    }

    public enum MouthViseme
    {
        Closed_Rest = 0,
        Vowel_I_Small = 1,   // Narrow/Teeth ("ee", "ih")
        Vowel_E_Mid = 2,     // Mid-wide ("eh", "ae")
        Vowel_U_Round = 3,   // Pursed small ("oo", "w")
        Vowel_O_Round = 4,   // Round open ("oh", "aw")
        Vowel_A_Wide = 5     // Full open ("ah", "aa")
    }

    [Serializable]
    public struct EmotionSpriteSet
    {
        public FacialEmotion emotion;
        [Tooltip("Combined Eyebrows + Open Eyes graphic for this emotion")]
        public Sprite eyesAndBrowsOpen;
        [Tooltip("Combined Eyebrows + Closed/Blinking Eyes graphic for this emotion")]
        public Sprite eyesAndBrowsBlink;
        [Tooltip("Default Mouth graphic when the character is NOT speaking")]
        public Sprite restingEmotionMouth;
        [Tooltip("Optional UV Atlas Grid Index (X=Col, Y=Row) for 3D Mesh Shader Mode")]
        public Vector2Int uvAtlasOpenCoord;
        public Vector2Int uvAtlasBlinkCoord;
        public Vector2Int uvAtlasRestingMouthCoord;
    }

    [CreateAssetMenu(fileName = "NewFacialExpressionDB", menuName = "BattleSummoner/Character/Facial Expression Database")]
    public class FacialExpressionDatabaseSO : ScriptableObject
    {
        [Header("1. Upper (Eyes+Brows) & Lower (Mouth) Emotion Sets")]
        public List<EmotionSpriteSet> emotionSets = new List<EmotionSpriteSet>();

        [Header("2. Phonetic Lip-Sync Mouth Visemes (Closed, I, E, U, O, A)")]
        [Tooltip("Index 0: Closed, 1: I, 2: E, 3: U, 4: O, 5: A")]
        public Sprite[] phoneticMouthVisemes = new Sprite[6];
        public Vector2Int[] phoneticMouthUVCoords = new Vector2Int[6];

        [Header("3. 3D UV Atlas Grid Dimensions (If using 3D Shader Mode)")]
        public Vector2Int atlasGridCells = new Vector2Int(4, 4);

        private Dictionary<FacialEmotion, EmotionSpriteSet> _lookupCache;

        public EmotionSpriteSet GetEmotionSet(FacialEmotion targetEmotion)
        {
            if (_lookupCache == null || _lookupCache.Count != emotionSets.Count)
            {
                _lookupCache = new Dictionary<FacialEmotion, EmotionSpriteSet>(emotionSets.Count);
                for (int i = 0; i < emotionSets.Count; i++)
                {
                    _lookupCache[emotionSets[i].emotion] = emotionSets[i];
                }
            }

            if (_lookupCache.TryGetValue(targetEmotion, out EmotionSpriteSet result))
            {
                return result;
            }

            return emotionSets.Count > 0 ? emotionSets[0] : default;
        }

        public Sprite GetVisemeSprite(MouthViseme viseme)
        {
            if (phoneticMouthVisemes == null || phoneticMouthVisemes.Length == 0) return null;
            int idx = Mathf.Clamp((int)viseme, 0, phoneticMouthVisemes.Length - 1);
            return phoneticMouthVisemes[idx];
        }

        public Vector4 GetAtlasST(Vector2Int cellCoord)
        {
            float scaleX = 1f / Mathf.Max(1, atlasGridCells.x);
            float scaleY = 1f / Mathf.Max(1, atlasGridCells.y);
            float offsetX = cellCoord.x * scaleX;
            float offsetY = cellCoord.y * scaleY;
            return new Vector4(scaleX, scaleY, offsetX, offsetY);
        }
    }
}
