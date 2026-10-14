using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.UI
{
    public enum ProfileGenderBase
    {
        Male,
        Female
    }

    public enum ProfileHairstyle
    {
        ShaggyProtagonistBangs,
        PartedScholarCut,
        SpikyAdventurerTufts,
        ModernUndercut,
        SleekBobWithBangs,
        LongTwinTails,
        HighPonytail,
        ElegantSideBraid
    }

    public enum ProfileEyeExpression
    {
        DeterminedFocused,
        CalmObservant,
        ConfusedSurprised,
        IntenseBattleGlare,
        ConfidentGaze,
        GentleSmiling
    }

    public enum ProfileFacialFeature
    {
        None,
        CheekBattleScar,
        ArcaneGlyphTattoo,
        NoseBridgeBandage,
        BeautyMarkUnderEye,
        SoftAnimeBlush
    }

    public enum ProfileGlasses
    {
        None,
        RoundWireframeSpectacles,
        ModernHalfRimFrames,
        OrnateMonocleWithChain,
        CatEyeFrames
    }

    public enum ProfileAccessory
    {
        None,
        SilverStudEarring,
        RibbonDropEarring,
        VelvetHairRibbonBow,
        AlchemicalCrossEarring
    }

    public enum ProfileClothingCollar
    {
        AcademyBlazerWithTie,
        ScholarFrockCoatWithAscot,
        VelvetDressWithLaceCollar,
        RangerHoodedCapelet,
        SmithWorkwearWithApron,
        OpenCollarShirtWithRibbon
    }

    [System.Serializable]
    public class ModularProfileConfiguration
    {
        public string characterId = "hero";
        public string characterName = "Protagonist";
        public ProfileGenderBase gender = ProfileGenderBase.Male;
        public ProfileHairstyle hairstyle = ProfileHairstyle.ShaggyProtagonistBangs;
        public ProfileEyeExpression eyeExpression = ProfileEyeExpression.DeterminedFocused;
        public ProfileFacialFeature facialFeature = ProfileFacialFeature.None;
        public ProfileGlasses glasses = ProfileGlasses.None;
        public ProfileAccessory accessory = ProfileAccessory.None;
        public ProfileClothingCollar clothing = ProfileClothingCollar.AcademyBlazerWithTie;

        public Color skinToneColor = new Color(0.98f, 0.88f, 0.80f);
        public Color hairColor = new Color(0.12f, 0.14f, 0.18f);
        public Color eyeColor = new Color(0.15f, 0.45f, 0.65f);
        public Color clothingPrimaryColor = new Color(0.08f, 0.10f, 0.16f);
        public Color clothingAccentColor = new Color(0.75f, 0.12f, 0.15f);

        public static ModularProfileConfiguration CreateHeroPreset()
        {
            return new ModularProfileConfiguration
            {
                characterId = "hero",
                characterName = "Protagonist",
                gender = ProfileGenderBase.Male,
                hairstyle = ProfileHairstyle.ShaggyProtagonistBangs,
                eyeExpression = ProfileEyeExpression.DeterminedFocused,
                facialFeature = ProfileFacialFeature.None,
                glasses = ProfileGlasses.None,
                accessory = ProfileAccessory.None,
                clothing = ProfileClothingCollar.AcademyBlazerWithTie,
                hairColor = new Color(0.12f, 0.14f, 0.20f),
                eyeColor = new Color(0.15f, 0.45f, 0.65f),
                clothingPrimaryColor = new Color(0.08f, 0.10f, 0.16f), // Gekkoukan midnight navy
                clothingAccentColor = new Color(0.75f, 0.12f, 0.15f)   // Persona crimson tie
            };
        }

        public static ModularProfileConfiguration CreateMargoPreset()
        {
            return new ModularProfileConfiguration
            {
                characterId = "margo",
                characterName = "Margo",
                gender = ProfileGenderBase.Female,
                hairstyle = ProfileHairstyle.SleekBobWithBangs,
                eyeExpression = ProfileEyeExpression.CalmObservant,
                facialFeature = ProfileFacialFeature.BeautyMarkUnderEye,
                glasses = ProfileGlasses.None,
                accessory = ProfileAccessory.RibbonDropEarring,
                clothing = ProfileClothingCollar.VelvetDressWithLaceCollar,
                hairColor = new Color(0.10f, 0.08f, 0.14f), // Dark plum black
                eyeColor = new Color(0.55f, 0.45f, 0.65f),  // Occult violet
                clothingPrimaryColor = new Color(0.15f, 0.05f, 0.12f), // Royal velvet plum
                clothingAccentColor = new Color(0.88f, 0.75f, 0.35f)   // Antique gold filigree
            };
        }

        public static ModularProfileConfiguration CreateMayorBarnabyPreset()
        {
            return new ModularProfileConfiguration
            {
                characterId = "barnaby",
                characterName = "Mayor Barnaby",
                gender = ProfileGenderBase.Male,
                hairstyle = ProfileHairstyle.PartedScholarCut,
                eyeExpression = ProfileEyeExpression.CalmObservant,
                facialFeature = ProfileFacialFeature.None,
                glasses = ProfileGlasses.RoundWireframeSpectacles,
                accessory = ProfileAccessory.None,
                clothing = ProfileClothingCollar.ScholarFrockCoatWithAscot,
                hairColor = new Color(0.78f, 0.79f, 0.82f), // Distinguished silver
                eyeColor = new Color(0.35f, 0.45f, 0.55f),
                clothingPrimaryColor = new Color(0.24f, 0.10f, 0.12f), // Burgundy scholar coat
                clothingAccentColor = new Color(0.85f, 0.70f, 0.30f)   // Gold pocket watch chain
            };
        }

        public static ModularProfileConfiguration CreateCherylPreset()
        {
            return new ModularProfileConfiguration
            {
                characterId = "cheryl",
                characterName = "Cheryl",
                gender = ProfileGenderBase.Female,
                hairstyle = ProfileHairstyle.HighPonytail,
                eyeExpression = ProfileEyeExpression.GentleSmiling,
                facialFeature = ProfileFacialFeature.SoftAnimeBlush,
                glasses = ProfileGlasses.None,
                accessory = ProfileAccessory.VelvetHairRibbonBow,
                clothing = ProfileClothingCollar.OpenCollarShirtWithRibbon,
                hairColor = new Color(0.38f, 0.22f, 0.14f), // Chestnut brown
                eyeColor = new Color(0.25f, 0.55f, 0.35f),  // Emerald green
                clothingPrimaryColor = new Color(0.08f, 0.32f, 0.18f),
                clothingAccentColor = new Color(0.95f, 0.95f, 0.96f)
            };
        }

        public static ModularProfileConfiguration CreateZephyrPreset()
        {
            return new ModularProfileConfiguration
            {
                characterId = "zephyr",
                characterName = "Zephyr",
                gender = ProfileGenderBase.Male,
                hairstyle = ProfileHairstyle.SpikyAdventurerTufts,
                eyeExpression = ProfileEyeExpression.IntenseBattleGlare,
                facialFeature = ProfileFacialFeature.NoseBridgeBandage,
                glasses = ProfileGlasses.None,
                accessory = ProfileAccessory.SilverStudEarring,
                clothing = ProfileClothingCollar.RangerHoodedCapelet,
                hairColor = new Color(0.30f, 0.20f, 0.14f),
                eyeColor = new Color(0.30f, 0.55f, 0.55f),
                clothingPrimaryColor = new Color(0.18f, 0.28f, 0.16f), // Forest ranger green
                clothingAccentColor = new Color(0.65f, 0.50f, 0.25f)
            };
        }

        public static ModularProfileConfiguration CreateKeithPreset()
        {
            return new ModularProfileConfiguration
            {
                characterId = "keith",
                characterName = "Keith",
                gender = ProfileGenderBase.Male,
                hairstyle = ProfileHairstyle.ModernUndercut,
                eyeExpression = ProfileEyeExpression.DeterminedFocused,
                facialFeature = ProfileFacialFeature.CheekBattleScar,
                glasses = ProfileGlasses.None,
                accessory = ProfileAccessory.SilverStudEarring,
                clothing = ProfileClothingCollar.SmithWorkwearWithApron,
                hairColor = new Color(0.22f, 0.16f, 0.12f),
                eyeColor = new Color(0.65f, 0.45f, 0.15f), // Warm amber
                clothingPrimaryColor = new Color(0.28f, 0.18f, 0.12f), // Leather apron
                clothingAccentColor = new Color(0.78f, 0.62f, 0.25f)
            };
        }
    }

    /// <summary>
    /// ModularProfilePortrait renders layered anime side-view character profile portraits.
    /// Supports paper-doll style swapping of Base Head, Hair, Eyes, Glasses, Accessories, and Clothing.
    /// </summary>
    public class ModularProfilePortrait : MonoBehaviour
    {
        [Header("Layered Sprite Renderers (Canvas UI)")]
        [SerializeField] private Image backgroundCardImage;
        [SerializeField] private Image baseHeadImage;
        [SerializeField] private Image clothingImage;
        [SerializeField] private Image facialFeatureImage;
        [SerializeField] private Image eyesImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image glassesImage;
        [SerializeField] private Image accessoryImage;
        [SerializeField] private Image circularFrameImage;
        [SerializeField] private Image classBadgeImage;
        [SerializeField] private TextMeshProUGUI characterNameTMP;

        [Header("Current Configuration")]
        [SerializeField] private ModularProfileConfiguration configuration = new ModularProfileConfiguration();

        [Header("Pre-Rendered Fallback Portraits")]
        [SerializeField] private Sprite preRenderedPortrait;

        public ModularProfileConfiguration Config => configuration;

        public void ApplyConfiguration(ModularProfileConfiguration config)
        {
            if (config == null) return;
            configuration = config;
            RefreshDisplay();
        }

        public void SetPreRenderedPortrait(Sprite portrait, string name = null)
        {
            preRenderedPortrait = portrait;
            if (baseHeadImage != null && portrait != null)
            {
                baseHeadImage.sprite = portrait;
                baseHeadImage.color = Color.white;
                baseHeadImage.enabled = true;

                if (clothingImage != null) clothingImage.enabled = false;
                if (facialFeatureImage != null) facialFeatureImage.enabled = false;
                if (eyesImage != null) eyesImage.enabled = false;
                if (hairImage != null) hairImage.enabled = false;
                if (glassesImage != null) glassesImage.enabled = false;
                if (accessoryImage != null) accessoryImage.enabled = false;
            }

            if (characterNameTMP != null && !string.IsNullOrEmpty(name))
            {
                characterNameTMP.text = name;
            }
        }

        public void RefreshDisplay()
        {
            if (preRenderedPortrait != null)
            {
                SetPreRenderedPortrait(preRenderedPortrait, configuration.characterName);
                return;
            }

            if (characterNameTMP != null)
            {
                characterNameTMP.text = configuration.characterName;
            }

            if (baseHeadImage != null)
            {
                baseHeadImage.color = configuration.skinToneColor;
                baseHeadImage.enabled = true;
            }

            if (hairImage != null)
            {
                hairImage.color = configuration.hairColor;
                hairImage.enabled = true;
            }

            if (eyesImage != null)
            {
                eyesImage.color = configuration.eyeColor;
                eyesImage.enabled = true;
            }

            if (clothingImage != null)
            {
                clothingImage.color = configuration.clothingPrimaryColor;
                clothingImage.enabled = true;
            }

            if (glassesImage != null)
            {
                glassesImage.enabled = (configuration.glasses != ProfileGlasses.None);
            }

            if (accessoryImage != null)
            {
                accessoryImage.enabled = (configuration.accessory != ProfileAccessory.None);
            }

            if (facialFeatureImage != null)
            {
                facialFeatureImage.enabled = (configuration.facialFeature != ProfileFacialFeature.None);
            }
        }

        private void OnValidate()
        {
            RefreshDisplay();
        }
    }
}
