using System;
using UnityEngine;

namespace BattleSummoner.Customization
{
    public enum SexOption
    {
        Female,
        Male,
        NonBinary,
        Custom
    }

    public enum PronounOption
    {
        SheHer,       // She / Her / Hers
        HeHim,        // He / Him / His
        TheyThem,     // They / Them / Theirs
        ItIts,        // It / Its / Itself
        Custom        // Custom user-defined string
    }

    public enum BodyTypePreset
    {
        Athletic,
        Feminine,
        Masculine,
        Slender,
        Heavy,
        Stocky
    }

    public enum FaceShapePreset
    {
        Oval,
        Angular,
        Chiseled,
        SoftRound,
        Heart,
        Sharp
    }

    public enum EyeShapePreset
    {
        IntenseAlmond,
        WideCurious,
        StoicNarrow,
        DemonicSlit,
        SoftGentle
    }

    public enum NoseShapePreset
    {
        Straight,
        Aquiline,
        ButtonPetite,
        SharpPointed,
        Broad
    }

    public enum MouthShapePreset
    {
        DeterminedNeutral,
        ConfidentSmirk,
        GentleSmile,
        PoutingFull,
        FangedGrin
    }

    public enum HairStyleOption
    {
        SpikyAnime,
        LongPonytail,
        FlowingBraids,
        ShortCrop,
        ElegantBob,
        ShaggyLocks,
        GothicTwintails,
        BaldBuzzed
    }

    [System.Serializable]
    public class CharacterCustomizationData
    {
        // 1. Identity & Social Presentation
        public string characterName = "Summoner";
        public SexOption sex = SexOption.Female;
        public PronounOption pronouns = PronounOption.SheHer;
        public string customPronounSubject = "Ze";
        public string customPronounObject = "Zir";
        public string customPronounPossessive = "Zirs";

        // 2. Body Type & Presets
        public BodyTypePreset bodyType = BodyTypePreset.Athletic;

        // 3. Head & Facial Morphology
        public FaceShapePreset faceShape = FaceShapePreset.Oval;
        public int skinToneIndex = 1;

        public EyeShapePreset eyeShape = EyeShapePreset.IntenseAlmond;
        public int eyeColorIndex = 2; // Cerulean Blue
        public bool heterochromia = false;
        public int rightEyeColorIndex = 4; // Crimson Ruby

        public NoseShapePreset noseShape = NoseShapePreset.Straight;
        public float noseScale = 1.0f; // [0.70, 1.30]

        public MouthShapePreset mouthShape = MouthShapePreset.DeterminedNeutral;
        public int lipColorIndex = 0;

        // 4. Hair & Styling
        public HairStyleOption hairStyle = HairStyleOption.SpikyAnime;
        public int hairColorIndex = 0; // Obsidian Black
        public int highlightColorIndex = 2; // Golden highlight

        // 5. Body Proportions & Morphology Sliders ([0.70, 1.40], default 1.0)
        [Range(0.70f, 1.40f)] public float neckLength = 1.0f;
        [Range(0.70f, 1.40f)] public float armLength = 1.0f;
        [Range(0.70f, 1.40f)] public float torsoScale = 1.0f;
        [Range(0.70f, 1.40f)] public float hipsScale = 1.0f;
        [Range(0.70f, 1.40f)] public float legsLength = 1.0f;
        [Range(0.70f, 1.40f)] public float feetScale = 1.0f;

        // 6. Secondary Aesthetic Scaling
        [Range(0.85f, 1.15f)] public float heightScale = 1.0f;
        [Range(0.0f, 1.0f)] public float muscleDefinition = 0.5f;
        public int outfitPrimaryColor = 0;
        public int outfitSecondaryColor = 1;

        #region Color Palette Standards
        public static readonly Color[] SkinTones = new Color[]
        {
            new Color(0.99f, 0.92f, 0.87f), // Fair Porcelain
            new Color(0.89f, 0.71f, 0.55f), // Sun-Kissed Tanned
            new Color(0.61f, 0.42f, 0.27f), // Deep Bronze
            new Color(0.29f, 0.18f, 0.11f), // Rich Obsidian
            new Color(0.95f, 0.94f, 1.00f), // Celestial Alabaster
            new Color(0.48f, 0.47f, 0.52f), // Ash Demonic
            new Color(0.71f, 0.85f, 0.93f), // Frost Azure
            new Color(0.83f, 0.64f, 0.35f)  // Royal Gold
        };

        public static readonly Color[] HairColors = new Color[]
        {
            new Color(0.10f, 0.10f, 0.12f), // Obsidian Black
            new Color(0.88f, 0.90f, 0.95f), // Platinum Silver
            new Color(0.98f, 0.82f, 0.28f), // Radiant Gold
            new Color(0.85f, 0.18f, 0.22f), // Crimson Ruby
            new Color(0.18f, 0.52f, 0.92f), // Cobalt Blue
            new Color(0.15f, 0.75f, 0.45f), // Forest Emerald
            new Color(0.92f, 0.25f, 0.75f), // Neon Magenta
            new Color(0.45f, 0.26f, 0.16f)  // Chestnut Brown
        };

        public static readonly Color[] EyeColors = new Color[]
        {
            new Color(0.98f, 0.72f, 0.15f), // Amber Gold
            new Color(0.22f, 0.65f, 0.95f), // Cerulean Blue
            new Color(0.20f, 0.82f, 0.48f), // Emerald Green
            new Color(0.68f, 0.35f, 0.92f), // Amethyst Violet
            new Color(0.90f, 0.15f, 0.22f), // Crimson Ruby
            new Color(1.00f, 0.90f, 0.40f), // Molten Gold
            new Color(0.75f, 0.80f, 0.88f), // Silver Mist
            new Color(0.25f, 0.15f, 0.35f)  // Void Purple
        };

        public static readonly Color[] OutfitColors = new Color[]
        {
            new Color(0.08f, 0.10f, 0.15f), // Midnight Navy
            new Color(0.95f, 0.80f, 0.30f), // Gilded Gold
            new Color(0.78f, 0.15f, 0.20f), // Royal Crimson
            new Color(0.12f, 0.55f, 0.40f), // Verdant Jade
            new Color(0.85f, 0.88f, 0.92f), // Platinum White
            new Color(0.42f, 0.18f, 0.65f), // Astral Violet
            new Color(0.85f, 0.45f, 0.15f), // Amber Bronze
            new Color(0.15f, 0.15f, 0.18f)  // Charcoal Obsidian
        };
        #endregion

        #region Pronoun Helpers
        public string GetSubject(bool capitalize = false)
        {
            string s = pronouns switch
            {
                PronounOption.SheHer => "she",
                PronounOption.HeHim => "he",
                PronounOption.TheyThem => "they",
                PronounOption.ItIts => "it",
                PronounOption.Custom => string.IsNullOrEmpty(customPronounSubject) ? "they" : customPronounSubject.ToLower(),
                _ => "they"
            };
            if (capitalize && s.Length > 0)
                return char.ToUpper(s[0]) + s.Substring(1);
            return s;
        }

        public string GetObject(bool capitalize = false)
        {
            string s = pronouns switch
            {
                PronounOption.SheHer => "her",
                PronounOption.HeHim => "him",
                PronounOption.TheyThem => "them",
                PronounOption.ItIts => "it",
                PronounOption.Custom => string.IsNullOrEmpty(customPronounObject) ? "them" : customPronounObject.ToLower(),
                _ => "them"
            };
            if (capitalize && s.Length > 0)
                return char.ToUpper(s[0]) + s.Substring(1);
            return s;
        }

        public string GetPossessive(bool capitalize = false)
        {
            string s = pronouns switch
            {
                PronounOption.SheHer => "her",
                PronounOption.HeHim => "his",
                PronounOption.TheyThem => "their",
                PronounOption.ItIts => "its",
                PronounOption.Custom => string.IsNullOrEmpty(customPronounPossessive) ? "their" : customPronounPossessive.ToLower(),
                _ => "their"
            };
            if (capitalize && s.Length > 0)
                return char.ToUpper(s[0]) + s.Substring(1);
            return s;
        }

        public string GetPronounsSummary()
        {
            return pronouns switch
            {
                PronounOption.SheHer => "She / Her",
                PronounOption.HeHim => "He / Him",
                PronounOption.TheyThem => "They / Them",
                PronounOption.ItIts => "It / Its",
                PronounOption.Custom => $"{customPronounSubject} / {customPronounObject}",
                _ => "They / Them"
            };
        }

        public string GetSampleSentence()
        {
            string subj = GetSubject(true);
            string poss = GetPossessive(false);
            return $"\"{subj} commands familiars with {poss} grimoire.\"";
        }
        #endregion

        #region Presets and Randomization
        public void ApplyBodyTypePreset(BodyTypePreset preset)
        {
            bodyType = preset;
            switch (preset)
            {
                case BodyTypePreset.Athletic:
                    neckLength = 1.0f;
                    armLength = 1.0f;
                    torsoScale = 1.05f;
                    hipsScale = 1.0f;
                    legsLength = 1.05f;
                    feetScale = 1.0f;
                    muscleDefinition = 0.65f;
                    heightScale = 1.0f;
                    break;
                case BodyTypePreset.Feminine:
                    neckLength = 1.05f;
                    armLength = 0.95f;
                    torsoScale = 0.92f;
                    hipsScale = 1.15f;
                    legsLength = 1.08f;
                    feetScale = 0.92f;
                    muscleDefinition = 0.35f;
                    heightScale = 0.96f;
                    break;
                case BodyTypePreset.Masculine:
                    neckLength = 0.98f;
                    armLength = 1.08f;
                    torsoScale = 1.18f;
                    hipsScale = 0.95f;
                    legsLength = 1.05f;
                    feetScale = 1.10f;
                    muscleDefinition = 0.80f;
                    heightScale = 1.05f;
                    break;
                case BodyTypePreset.Slender:
                    neckLength = 1.12f;
                    armLength = 1.05f;
                    torsoScale = 0.88f;
                    hipsScale = 0.90f;
                    legsLength = 1.15f;
                    feetScale = 0.90f;
                    muscleDefinition = 0.20f;
                    heightScale = 1.02f;
                    break;
                case BodyTypePreset.Heavy:
                    neckLength = 0.90f;
                    armLength = 1.12f;
                    torsoScale = 1.30f;
                    hipsScale = 1.25f;
                    legsLength = 0.95f;
                    feetScale = 1.20f;
                    muscleDefinition = 0.90f;
                    heightScale = 1.08f;
                    break;
                case BodyTypePreset.Stocky:
                    neckLength = 0.85f;
                    armLength = 0.95f;
                    torsoScale = 1.25f;
                    hipsScale = 1.18f;
                    legsLength = 0.88f;
                    feetScale = 1.15f;
                    muscleDefinition = 0.75f;
                    heightScale = 0.90f;
                    break;
            }
        }

        public void Randomize()
        {
            var rnd = new System.Random();
            sex = (SexOption)rnd.Next(0, 4);
            pronouns = (PronounOption)rnd.Next(0, 4);
            bodyType = (BodyTypePreset)rnd.Next(0, 6);
            ApplyBodyTypePreset(bodyType);

            // Jitter morphology slightly
            neckLength = Mathf.Clamp(neckLength + (float)(rnd.NextDouble() * 0.2 - 0.1), 0.70f, 1.40f);
            armLength = Mathf.Clamp(armLength + (float)(rnd.NextDouble() * 0.2 - 0.1), 0.70f, 1.40f);
            torsoScale = Mathf.Clamp(torsoScale + (float)(rnd.NextDouble() * 0.2 - 0.1), 0.70f, 1.40f);
            hipsScale = Mathf.Clamp(hipsScale + (float)(rnd.NextDouble() * 0.2 - 0.1), 0.70f, 1.40f);
            legsLength = Mathf.Clamp(legsLength + (float)(rnd.NextDouble() * 0.2 - 0.1), 0.70f, 1.40f);
            feetScale = Mathf.Clamp(feetScale + (float)(rnd.NextDouble() * 0.2 - 0.1), 0.70f, 1.40f);

            faceShape = (FaceShapePreset)rnd.Next(0, 6);
            skinToneIndex = rnd.Next(0, SkinTones.Length);
            eyeShape = (EyeShapePreset)rnd.Next(0, 5);
            eyeColorIndex = rnd.Next(0, EyeColors.Length);
            heterochromia = (rnd.Next(0, 5) == 0); // 20% chance
            rightEyeColorIndex = rnd.Next(0, EyeColors.Length);

            noseShape = (NoseShapePreset)rnd.Next(0, 5);
            noseScale = Mathf.Clamp(0.85f + (float)rnd.NextDouble() * 0.3f, 0.70f, 1.30f);

            mouthShape = (MouthShapePreset)rnd.Next(0, 5);
            lipColorIndex = rnd.Next(0, 4);

            hairStyle = (HairStyleOption)rnd.Next(0, 8);
            hairColorIndex = rnd.Next(0, HairColors.Length);
            highlightColorIndex = rnd.Next(0, HairColors.Length);

            outfitPrimaryColor = rnd.Next(0, OutfitColors.Length);
            outfitSecondaryColor = rnd.Next(0, OutfitColors.Length);
        }

        public CharacterCustomizationData Clone()
        {
            string json = JsonUtility.ToJson(this);
            return JsonUtility.FromJson<CharacterCustomizationData>(json);
        }
        #endregion

        #region Persistence (PlayerPrefs)
        private const string PREF_KEY = "BS_CharacterCustomization_v1";

        public void SaveToPrefs(string customKey = null)
        {
            string key = string.IsNullOrEmpty(customKey) ? PREF_KEY : customKey;
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public static CharacterCustomizationData LoadFromPrefs(string customKey = null)
        {
            string key = string.IsNullOrEmpty(customKey) ? PREF_KEY : customKey;
            if (PlayerPrefs.HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                try
                {
                    var data = JsonUtility.FromJson<CharacterCustomizationData>(json);
                    if (data != null) return data;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to deserialize character data: {ex.Message}");
                }
            }
            var def = new CharacterCustomizationData();
            def.ApplyBodyTypePreset(BodyTypePreset.Athletic);
            return def;
        }
        #endregion
    }
}
