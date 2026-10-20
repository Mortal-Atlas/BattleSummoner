using System;
using UnityEngine;

namespace BattleSummoner.Customization
{
    public enum ModularBodyTypePreset
    {
        HyperFemininePetite = 0,         // Shortest female, petite narrow waist, modest frame
        PetiteSveltFeminine = 1,         // Slim, graceful, medium-petite height, balanced curves
        VoluptuousFeminine = 2,          // Curvaceous, fuller bust, wider hips, hourglass waist
        LeanAndrogynousFeminine = 3,     // Taller, athletic, leaner hips/bust, androgynous torso
        PetiteAndrogynousMasculine = 4,  // Shorter male, lean androgynous athletic build
        LeanAndrogynousMasculine = 5,    // Average height, defined torso, lean musculature
        SveltMasculine = 6,              // Tall, broad shoulders, athletic tapered V-torso
        HyperMasculineTall = 7           // Tallest, colossal muscular build, massive shoulders & quads
    }

    /// <summary>
    /// ModularPlayerAppearanceData represents the serializable multiplayer data contract
    /// for a player's complete custom character: base skin, colors, equipped armor pieces, and morphs.
    /// Can be serialized to/from JSON or transmitted across network packets.
    /// </summary>
    [Serializable]
    public class ModularPlayerAppearanceData
    {
        public string playerName = "Astraea";
        public int gender = 1; // 0=Male, 1=Female
        public int bodyTypeIndex = 1; // 0 to 7 matching the 8 Base Body Types
        public Color skinColor = new Color(1.0f, 0.91f, 0.83f, 1.0f);
        public Color eyeColor = new Color(0.12f, 0.65f, 0.95f, 1.0f);
        public Color hairColor = new Color(0.15f, 0.15f, 0.18f, 1.0f);
        public int hairStyleIndex = 0;
        public Color armorDyeColor = new Color(0.25f, 0.42f, 0.82f, 1.0f);

        // Modular Equipment Slot Toggles (replaces underlying body parts)
        public bool equipTorso = false;      // Replaces Body_Torso with Armor_Torso
        public bool equipGauntlets = true;   // Replaces Body_Hands (wrist/hand) with Armor_Gauntlets
        public bool equipLeggings = true;    // Replaces Body_Legs with Armor_Leggings
        public bool equipBoots = false;      // Replaces Body_Feet with Armor_Boots
        public bool equipHelmet = false;     // Toggles Armor_Helmet
        public string weaponId = "sword";

        // Proportions / Bone Scaling
        public float heightScale = 1.0f;
        public float headScale = 1.0f;
        public float shoulderWidth = 1.0f;
        public float chestScale = 1.0f;
        public float waistScale = 1.0f;
        public float hipScale = 1.0f;
        public float armThickness = 1.0f;
        public float legThickness = 1.0f;

        public static readonly string[] BodyTypeNames = new string[]
        {
            "Hyper-feminine, Petite",
            "Petite Svelt Feminine",
            "Voluptuous Feminine",
            "Lean/Androgynous Feminine",
            "Petite/Androgynous Masculine",
            "Lean/Androgynous Masculine",
            "Svelt Masculine",
            "Hyper-masculine, Tall"
        };

        public void ApplyBodyTypePreset(int index)
        {
            bodyTypeIndex = Mathf.Clamp(index, 0, 7);
            gender = bodyTypeIndex < 4 ? 1 : 0;
            switch (bodyTypeIndex)
            {
                case 0: // Hyper-feminine, Petite
                    heightScale = 0.82f; headScale = 1.06f; shoulderWidth = 0.80f; chestScale = 0.85f;
                    waistScale = 0.75f; hipScale = 0.88f; armThickness = 0.78f; legThickness = 0.80f;
                    break;
                case 1: // Petite Svelt Feminine
                    heightScale = 0.90f; headScale = 1.00f; shoulderWidth = 0.85f; chestScale = 0.95f;
                    waistScale = 0.80f; hipScale = 0.95f; armThickness = 0.85f; legThickness = 0.88f;
                    break;
                case 2: // Voluptuous Feminine
                    heightScale = 0.98f; headScale = 0.98f; shoulderWidth = 0.92f; chestScale = 1.28f;
                    waistScale = 0.80f; hipScale = 1.32f; armThickness = 0.95f; legThickness = 1.12f;
                    break;
                case 3: // Lean/Androgynous Feminine
                    heightScale = 1.03f; headScale = 0.96f; shoulderWidth = 0.98f; chestScale = 0.98f;
                    waistScale = 0.88f; hipScale = 0.98f; armThickness = 0.92f; legThickness = 0.98f;
                    break;
                case 4: // Petite/Androgynous Masculine
                    heightScale = 0.92f; headScale = 1.00f; shoulderWidth = 0.92f; chestScale = 0.92f;
                    waistScale = 0.84f; hipScale = 0.88f; armThickness = 0.88f; legThickness = 0.90f;
                    break;
                case 5: // Lean/Androgynous Masculine
                    heightScale = 1.02f; headScale = 0.96f; shoulderWidth = 1.02f; chestScale = 1.00f;
                    waistScale = 0.88f; hipScale = 0.92f; armThickness = 0.96f; legThickness = 1.00f;
                    break;
                case 6: // Svelt Masculine
                    heightScale = 1.12f; headScale = 0.94f; shoulderWidth = 1.15f; chestScale = 1.16f;
                    waistScale = 0.92f; hipScale = 0.96f; armThickness = 1.08f; legThickness = 1.08f;
                    break;
                case 7: // Hyper-masculine, Tall
                    heightScale = 1.24f; headScale = 0.90f; shoulderWidth = 1.32f; chestScale = 1.38f;
                    waistScale = 0.98f; hipScale = 1.06f; armThickness = 1.30f; legThickness = 1.28f;
                    break;
            }
        }

        private const string PrefsKey = "Modular_Player_Appearance_Data";

        public string ToJson() => JsonUtility.ToJson(this);

        public static ModularPlayerAppearanceData FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<ModularPlayerAppearanceData>(json);
            }
            catch
            {
                return new ModularPlayerAppearanceData();
            }
        }

        public void SaveLocalProfile()
        {
            PlayerPrefs.SetString(PrefsKey, ToJson());
            PlayerPrefs.Save();
        }

        public static ModularPlayerAppearanceData LoadLocalProfile()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                string json = PlayerPrefs.GetString(PrefsKey);
                if (!string.IsNullOrEmpty(json))
                {
                    return FromJson(json);
                }
            }

            // Fallback: check legacy PlayerPrefs
            var data = new ModularPlayerAppearanceData();
            if (PlayerPrefs.HasKey("Morph_Height"))
            {
                data.heightScale = PlayerPrefs.GetFloat("Morph_Height", 1.0f);
                data.headScale = PlayerPrefs.GetFloat("Morph_Head", 1.0f);
                data.shoulderWidth = PlayerPrefs.GetFloat("Morph_Shoulders", 1.0f);
                data.chestScale = PlayerPrefs.GetFloat("Morph_Chest", 1.0f);
                data.waistScale = PlayerPrefs.GetFloat("Morph_Waist", 1.0f);
                data.hipScale = PlayerPrefs.GetFloat("Morph_Hips", 1.0f);
                data.armThickness = PlayerPrefs.GetFloat("Morph_Arms", 1.0f);
                data.legThickness = PlayerPrefs.GetFloat("Morph_Legs", 1.0f);
            }
            if (PlayerPrefs.HasKey("Equip_Torso")) data.equipTorso = PlayerPrefs.GetInt("Equip_Torso", 1) == 1;
            if (PlayerPrefs.HasKey("Equip_Gauntlets")) data.equipGauntlets = PlayerPrefs.GetInt("Equip_Gauntlets", 1) == 1;
            if (PlayerPrefs.HasKey("Equip_Leggings")) data.equipLeggings = PlayerPrefs.GetInt("Equip_Leggings", 1) == 1;
            if (PlayerPrefs.HasKey("Equip_Shoes")) data.equipBoots = PlayerPrefs.GetInt("Equip_Shoes", 1) == 1;
            if (PlayerPrefs.HasKey("Equip_Helmet")) data.equipHelmet = PlayerPrefs.GetInt("Equip_Helmet", 0) == 1;
            if (PlayerPrefs.HasKey("Equip_Weapon")) data.weaponId = PlayerPrefs.GetString("Equip_Weapon", "sword");
            if (PlayerPrefs.HasKey("Hair_Style")) data.hairStyleIndex = PlayerPrefs.GetInt("Hair_Style", 0);
            if (PlayerPrefs.HasKey("Hair_Color"))
            {
                if (ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("Hair_Color"), out Color c))
                    data.hairColor = c;
            }

            return data;
        }
    }
}
