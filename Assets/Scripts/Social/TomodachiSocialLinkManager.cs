using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Social
{
    /// <summary>
    /// Central manager for the Tomodachi Life / Divinatus / Megulink social link ecosystem:
    /// - Manages entity roster across NPCs, Familiars, Summoned Creatures, and Constructs
    /// - Tracks dynamic affinity (-100 to +100), Social Link Ranks (0 to 10), and Tomodachi Moods
    /// - Evaluates gifts from the player's Messenger Bag and delivers Persona-style VN reactions
    /// - Computes and supplies tangible Combat, Economy, and Companion buffs/debuffs
    /// </summary>
    public class TomodachiSocialLinkManager : MonoBehaviour
    {
        private static TomodachiSocialLinkManager _instance;
        public static TomodachiSocialLinkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<TomodachiSocialLinkManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TomodachiSocialLinkManager");
                        _instance = go.AddComponent<TomodachiSocialLinkManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Entity Roster")]
        [SerializeField] private List<SocialEntityProfile> roster = new List<SocialEntityProfile>();

        public IReadOnlyList<SocialEntityProfile> Roster => roster;

        public static event Action<SocialEntityProfile, int, GiftReaction> OnGiftGiven;
        public static event Action<SocialEntityProfile, int> OnAffinityChanged;
        public static event Action<SocialEntityProfile, TomodachiMood> OnMoodChanged;
        public static event Action<SocialEntityProfile, int> OnRankUp;

        // Tamagotchi Pet Care Events
        public static event Action<SocialEntityProfile, PetTreatItem> OnPetFed;
        public static event Action<SocialEntityProfile> OnPetWatered;
        public static event Action<SocialEntityProfile> OnPetBathed;
        public static event Action<SocialEntityProfile, PetPresentItem> OnPetPresentGiven;
        public static event Action<SocialEntityProfile> OnPetPetted;

        private readonly List<PetTreatItem> availableTreats = new List<PetTreatItem>
        {
            new PetTreatItem
            {
                id = "treat_choco",
                name = "Choco Drop",
                iconSpriteName = "Mockup_PetFood_ChocoDrop.png",
                element = "Fire",
                statBoostType = "ATK",
                statGain = 5,
                hungerGain = 25f,
                happinessGain = 15f,
                description = "Rich volcanic cacao treat. Familiars gain spicy courage and permanent ATK power!",
                themeColor = new Color(0.95f, 0.40f, 0.15f)
            },
            new PetTreatItem
            {
                id = "treat_flan",
                name = "Star Flan",
                iconSpriteName = "Mockup_PetFood_StarFlan.png",
                element = "Water",
                statBoostType = "MAG",
                statGain = 5,
                hungerGain = 25f,
                happinessGain = 15f,
                description = "Silky mana custard infused with astral essence. Elevates Magic potency!",
                themeColor = new Color(0.20f, 0.75f, 1.0f)
            },
            new PetTreatItem
            {
                id = "treat_honey",
                name = "Honey Puff",
                iconSpriteName = "Mockup_PetFood_HoneyPuff.png",
                element = "Earth",
                statBoostType = "DEF",
                statGain = 5,
                hungerGain = 25f,
                happinessGain = 15f,
                description = "Crispy amber honeycomb puff baked with mountain wheat. Fortifies Defense!",
                themeColor = new Color(0.95f, 0.80f, 0.20f)
            },
            new PetTreatItem
            {
                id = "treat_berry",
                name = "Berry Poke-Puff",
                iconSpriteName = "Mockup_PetFood_BerryPuff.png",
                element = "Wind",
                statBoostType = "SPD",
                statGain = 5,
                hungerGain = 25f,
                happinessGain = 15f,
                description = "Fluffy whipped pastry topped with fresh Luminescent Berries. Boosts Agility!",
                themeColor = new Color(0.35f, 0.95f, 0.55f)
            }
        };

        private readonly List<PetPresentItem> availablePresents = new List<PetPresentItem>
        {
            new PetPresentItem
            {
                id = "toy_bell",
                name = "Jingle Bell Collar",
                iconSpriteName = "Mockup_PetPresent_ToyBell.png",
                happinessGain = 35f,
                affectionPoints = 15,
                description = "A golden chime bell collar that tinkles merrily as your familiar bounces!",
                themeColor = new Color(1.0f, 0.85f, 0.20f)
            },
            new PetPresentItem
            {
                id = "toy_ribbon",
                name = "Celestial Ribbon",
                iconSpriteName = "Mockup_Acc_Ribbon.png",
                happinessGain = 35f,
                affectionPoints = 15,
                description = "Silken starry ribbon that flutters with astral luminescence.",
                themeColor = new Color(0.95f, 0.45f, 0.75f)
            },
            new PetPresentItem
            {
                id = "toy_gem",
                name = "Gleaming Gem Toy",
                iconSpriteName = "Mockup_Acc_Orb.png",
                happinessGain = 45f,
                affectionPoints = 20,
                description = "A smooth polished crystal sphere that reflects rainbow sparkles.",
                themeColor = new Color(0.40f, 0.90f, 1.0f)
            },
            new PetPresentItem
            {
                id = "toy_plushie",
                name = "Familiar Plushie",
                iconSpriteName = "Mockup_PetPresent_GiftBox.png",
                happinessGain = 50f,
                affectionPoints = 25,
                description = "A soft hand-stitched companion plush doll. Maximum cuddly comfort!",
                themeColor = new Color(1.0f, 0.50f, 0.30f)
            }
        };

        private void Awake()
        {
            if (_instance == null) _instance = this;
            if (roster == null || roster.Count == 0 || GetEntity("familiar_pygmy") == null || GetEntity("familiar_aqua") == null)
            {
                InitializeDefaultRoster();
            }
        }

        public void InitializeDefaultRoster()
        {
            roster.Clear();

            // 0. Familiar: Pygmy Sprite Companion (The Star)
            var pygmy = new SocialEntityProfile
            {
                entityId = "familiar_pygmy",
                displayName = "Pygmy Sprite",
                subtitle = "Gossamer Fairy Companion",
                entityType = SocialEntityType.Familiar,
                arcana = TarotArcana.TheStar,
                arcanaTitle = "THE STAR",
                portraitSpriteName = "Mockup_SocialPortrait_Pygmy.png",
                loreDescription = "Cheerful tiny sprite with iridescent fairy wings and boundless curiosity. Loves sweet berry poke-puffs, warm bubble baths, and shiny ribbons.",
                currentAffinity = 50,
                socialLinkRank = 4,
                currentMood = TomodachiMood.Ecstatic,
                careState = new FamiliarCareState
                {
                    hunger = 75f,
                    hydration = 80f,
                    cleanliness = 70f,
                    happiness = 90f,
                    affectionHearts = 3,
                    bonusAttack = 15,
                    bonusDefense = 10,
                    bonusMagic = 20,
                    bonusSpeed = 25,
                    favoriteFood = "Berry Poke-Puff",
                    favoriteToy = "Celestial Ribbon",
                    speciesDietDesc = "Adores fresh Luminescent Berries, whipped pastry, and crystal dew."
                },
                lovedGiftIds = new List<string> { "treat_berry", "toy_ribbon", "potion_elixir" },
                likedGiftIds = new List<string> { "treat_honey", "toy_bell" },
                dislikedGiftIds = new List<string> { "blunt_rock" },
                hatedGiftIds = new List<string> { "slimy_ooze" },
                lovedGiftLine = "*Giggles and twirls with sparkling fairy dust!* My absolute favorite! Master is the best in the whole wide Spire!",
                likedGiftLine = "*Flutters gossamer wings happily* Ooh, what a lovely treat! Thank you, Master!",
                neutralGiftLine = "*Sniffs curiously* A sweet curiosity! Pygmy will keep it safe in our nest!",
                dislikedGiftLine = "*Pouts with crossed arms* Too heavy and dull! Sprites like sparkly things!",
                hatedGiftLine = "*Hides behind your shoulder shivering* Eeeeek! Keep that slimy thing away!",
                rankUpLine = "Our hearts pulse with pure starlight! Pygmy grants thee the legendary Endure blessing!",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_pygmy",
                    title = "Gossamer Ward",
                    isBuff = true,
                    description = "+20% Familiar Attack Power & Survives lethal hits with 1 HP out of love (Pokémon Amie Sturdy).",
                    summonAttackMultiplier = 1.20f,
                    autoShieldThreshold = 0.20f
                }
            };
            roster.Add(pygmy);

            // 1. NPC: Margo the Archivist (The High Priestess)
            var margo = new SocialEntityProfile
            {
                entityId = "npc_margo",
                displayName = "Margo",
                subtitle = "Royal Spire Archivist",
                entityType = SocialEntityType.NPC,
                arcana = TarotArcana.TheHighPriestess,
                arcanaTitle = "THE HIGH PRIESTESS",
                portraitSpriteName = "Mockup_PersonaPortrait_Margo.png",
                loreDescription = "Keeper of ancient Spire chronologies and arcane runes. Values scholarly texts, contemplative silence, and restorative elixirs.",
                currentAffinity = 25,
                socialLinkRank = 3,
                currentMood = TomodachiMood.Happy,
                lovedGiftIds = new List<string> { "relic_whetstone", "potion_elixir", "cryptex" },
                likedGiftIds = new List<string> { "potion_amber", "mat_ruby" },
                dislikedGiftIds = new List<string> { "weapon_bow", "potion_crimson" },
                hatedGiftIds = new List<string> { "slimy_ooze", "cursed_tome" },
                lovedGiftLine = "Incredible... this artifact bears inscriptions from the First Era! I shall decipher its sacred wisdom immediately.",
                likedGiftLine = "Thank you, Wanderer. A thoughtful draught to illuminate the long hours of archival cataloging.",
                neutralGiftLine = "I accept this gesture. Every artifact in the Spire carries historical merit.",
                dislikedGiftLine = "Instruments of wanton bloodshed hold little allure for me, Wanderer. Please refrain.",
                hatedGiftLine = "Contaminated filth! Keep such blasphemous defilement far from my sacred library!",
                rankUpLine = "Thou art become the mirror of my deepest truth... The High Priestess casts her sacred benediction upon thy path.",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_margo",
                    title = "Archivist Insight",
                    isBuff = true,
                    description = "+25% EXP Surge & Reveals Hidden Rune Weaknesses.",
                    xpMultiplier = 1.25f,
                    playerDefenseMultiplier = 1.10f
                },
                negativeDebuff = new SocialLinkModifier
                {
                    id = "debuff_margo",
                    title = "Archivist Scorn",
                    isBuff = false,
                    description = "-20% EXP Penalty & Minimap Fog of Ignorance.",
                    xpMultiplier = 0.80f,
                    playerDefenseMultiplier = 0.90f
                }
            };
            margo.chatBranches.Add(new PersonaChatBranch
            {
                promptDialogue = "Wanderer... do you believe the Spire was constructed as a prison, or an instrument of transcendence?",
                choiceOptionA = "A temple of transcendence, unlocked through understanding.",
                reactionDialogueA = "Indeed. You possess an archivist's rare philosophical depth.",
                affinityDeltaA = 12,
                moodA = TomodachiMood.Ecstatic,
                choiceOptionB = "Just a blood-soaked dungeon to conquer for loot.",
                reactionDialogueB = "How utterly boorish. Your lack of wonder grieves me.",
                affinityDeltaB = -10,
                moodB = TomodachiMood.Annoyed
            });
            roster.Add(margo);

            // 2. NPC: Balthazar the Shadow Broker (The Devil)
            var balthazar = new SocialEntityProfile
            {
                entityId = "npc_balthazar",
                displayName = "Balthazar",
                subtitle = "Underground Curio Merchant",
                entityType = SocialEntityType.NPC,
                arcana = TarotArcana.TheDevil,
                arcanaTitle = "THE DEVIL",
                portraitSpriteName = "Mockup_PersonaPortrait_Merchant.png",
                loreDescription = "Cunning shadow trader lurking in the subterranean vaults. Driven purely by profit, gems, and priceless relics.",
                currentAffinity = 10,
                socialLinkRank = 2,
                currentMood = TomodachiMood.Content,
                lovedGiftIds = new List<string> { "mat_ruby", "potion_elixir", "spire_gold_sack" },
                likedGiftIds = new List<string> { "potion_amber", "weapon_staff" },
                dislikedGiftIds = new List<string> { "relic_whetstone" },
                hatedGiftIds = new List<string> { "counterfeit_coin", "worthless_slag" },
                lovedGiftLine = "Heheheh! Ah, now THAT's the glittering sparkle of true currency! You've got an eye for exquisite value, friend!",
                likedGiftLine = "A fine offering. I reckon I can flip this on the black market for a tidy margin.",
                neutralGiftLine = "Fair enough. All goods carry a price in the subterranean markets.",
                dislikedGiftLine = "Too crude, too rustic! What am I supposed to do with this utilitarian junk?",
                hatedGiftLine = "You dare present me counterfeit trash?! The nerve! My wares just doubled in price for you, ingrate!",
                rankUpLine = "A blood pact written in golden ink... The Devil smiles upon our mutually profitable conspiracy!",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_balthazar",
                    title = "Shadow Consignment",
                    isBuff = true,
                    description = "20% Shop Discount & +15% Sell Payout Bonus.",
                    shopBuyMultiplier = 0.80f,
                    shopSellMultiplier = 1.15f
                },
                negativeDebuff = new SocialLinkModifier
                {
                    id = "debuff_balthazar",
                    title = "Merchant Extortion",
                    isBuff = false,
                    description = "+35% Shop Price Gouging & -50% Sell Payout Penalty.",
                    shopBuyMultiplier = 1.35f,
                    shopSellMultiplier = 0.50f
                }
            };
            balthazar.chatBranches.Add(new PersonaChatBranch
            {
                promptDialogue = "Tell me true, traveler: when you gaze into the abyss of the Spire, what do you crave most?",
                choiceOptionA = "Wealth and forbidden relics beyond mortal reckoning.",
                reactionDialogueA = "Kwahaha! Spoken like a true soul of ambition! We shall get along swimmingly!",
                affinityDeltaA = 15,
                moodA = TomodachiMood.Ecstatic,
                choiceOptionB = "Righteous honor and selfless protection of the weak.",
                reactionDialogueB = "Pah! Naive sentimentality won't buy bread in the Under-spire.",
                affinityDeltaB = -8,
                moodB = TomodachiMood.Bored
            });
            roster.Add(balthazar);

            // 3. Familiar: Corvus the Arcane Raven (The Magician)
            var corvus = new SocialEntityProfile
            {
                entityId = "familiar_corvus",
                displayName = "Corvus",
                subtitle = "Scrying Arcane Raven",
                entityType = SocialEntityType.Familiar,
                arcana = TarotArcana.TheMagician,
                arcanaTitle = "THE MAGICIAN",
                portraitSpriteName = "Mockup_SocialPortrait_Corvus.png",
                loreDescription = "Inquisitive mystical bird linked to the Astral Weave. Obsessed with glittering gemstones, shiny trinkets, and arcane drafts.",
                currentAffinity = 40,
                socialLinkRank = 4,
                currentMood = TomodachiMood.Ecstatic,
                lovedGiftIds = new List<string> { "mat_ruby", "potion_amber", "astral_shard" },
                likedGiftIds = new List<string> { "potion_crimson", "potion_elixir" },
                dislikedGiftIds = new List<string> { "iron_ingot" },
                hatedGiftIds = new List<string> { "blunt_rock", "scarecrow_straw" },
                lovedGiftLine = "*Caw-caw!* Sparkly ruby! Precious shiny! My nest shall gleam like the Astral Heavens!",
                likedGiftLine = "*Flutters wings* Sweet ether draught! Corvus tastes the currents of magic!",
                neutralGiftLine = "*Tilts head with curiosity* A modest trinket. Corvus shall inspect it with keen eyes.",
                dislikedGiftLine = "*Pecks angrily* Heavy! Drab! Corvus wants glitter, not dull lead weights!",
                hatedGiftLine = "*Furious shrieking* Begone! Put that hideous thing away before I claw your hair!",
                rankUpLine = "The wings of fate beat with arcane resonance! The Magician grants thee uncanny critical precision!",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_corvus",
                    title = "Arcane Reconnaissance",
                    isBuff = true,
                    description = "+15% Critical Strike Chance & +15% Movement Agility.",
                    playerMoveSpeedMultiplier = 1.15f
                },
                negativeDebuff = new SocialLinkModifier
                {
                    id = "debuff_corvus",
                    title = "Corvid Treachery",
                    isBuff = false,
                    description = "-15% Attack Accuracy & Obnoxious Distracting Screeches.",
                    playerMoveSpeedMultiplier = 0.90f
                },
                careState = new FamiliarCareState
                {
                    hunger = 70f,
                    hydration = 65f,
                    cleanliness = 80f,
                    happiness = 85f,
                    affectionHearts = 3,
                    bonusAttack = 10,
                    bonusDefense = 5,
                    bonusMagic = 25,
                    bonusSpeed = 30,
                    favoriteFood = "Star Flan",
                    favoriteToy = "Jingle Bell Collar",
                    speciesDietDesc = "Feasts on astral custard, star flan, and glittering crystal baubles."
                }
            };
            corvus.chatBranches.Add(new PersonaChatBranch
            {
                promptDialogue = "*Caw!* Master, which shiny treasure in the Spire should Corvus snatch first?",
                choiceOptionA = "The dazzling astral rubies lodged in the ceiling vaults!",
                reactionDialogueA = "*Flaps enthusiastically* Yes! Shiny! Master knows Corvus's heart!",
                affinityDeltaA = 12,
                moodA = TomodachiMood.Ecstatic,
                choiceOptionB = "Forget shinies, keep a lookout for lethal spike traps.",
                reactionDialogueB = "*Sighs and ruffles feathers* Boring... always duty, never glitter...",
                affinityDeltaB = -5,
                moodB = TomodachiMood.Annoyed
            });
            roster.Add(corvus);

            // 4. Familiar: Ignis the Ember Salamander (The Sun)
            var ignis = new SocialEntityProfile
            {
                entityId = "familiar_ignis",
                displayName = "Ignis",
                subtitle = "Ember Forge Sprite",
                entityType = SocialEntityType.Familiar,
                arcana = TarotArcana.TheSun,
                arcanaTitle = "THE SUN",
                portraitSpriteName = "Mockup_SocialPortrait_Ignis.png",
                loreDescription = "Playful volcanic drake sprite born from elemental magma vents. Loves spicy crimson concoctions, choco drops, and warm ember baths.",
                currentAffinity = 15,
                socialLinkRank = 2,
                currentMood = TomodachiMood.Happy,
                lovedGiftIds = new List<string> { "treat_choco", "potion_crimson", "potion_elixir", "fire_essence" },
                likedGiftIds = new List<string> { "mat_ruby", "toy_bell" },
                dislikedGiftIds = new List<string> { "weapon_bow", "relic_whetstone" },
                hatedGiftIds = new List<string> { "ice_crystal", "vial_stagnant_water" },
                lovedGiftLine = "*Gurgles happily* Sss-spicy! Hot crimson flame burns inside my belly! I love you, Master!",
                likedGiftLine = "*Sniffs warmly* Warm red stone! Good for resting my belly against!",
                neutralGiftLine = "*Blows small smoke ring* Ignis investigates this curiosity with a gentle sniff.",
                dislikedGiftLine = "*Hisses and recoils* Cold sharp metal... Ignis prefers glowing embers.",
                hatedGiftLine = "*Terrified screech* FREEZING WATER! IT DAMPENS MY HEARTH! GET IT AWAY!",
                rankUpLine = "A blazing supernova erupts within our shared soul! The Sun radiates boundless warmth and restorative vigor!",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_ignis",
                    title = "Solar Hearth",
                    isBuff = true,
                    description = "+20% Fire/Combat Damage & Continuous +5 HP/sec Regen.",
                    playerDefenseMultiplier = 1.10f
                },
                negativeDebuff = new SocialLinkModifier
                {
                    id = "debuff_ignis",
                    title = "Solar Spite",
                    isBuff = false,
                    description = "Chilling Vulnerability & -10% Fire Damage Resistance.",
                    playerDefenseMultiplier = 0.90f
                },
                careState = new FamiliarCareState
                {
                    hunger = 50f,
                    hydration = 45f,
                    cleanliness = 65f,
                    happiness = 75f,
                    affectionHearts = 2,
                    bonusAttack = 30,
                    bonusDefense = 15,
                    bonusMagic = 10,
                    bonusSpeed = 10,
                    favoriteFood = "Choco Drop",
                    favoriteToy = "Gleaming Gem Toy",
                    speciesDietDesc = "Craves spicy volcanic cacao drops and warm thermal pumice."
                }
            };
            roster.Add(ignis);

            // 5. Familiar: Aqua the Bubble Nymph (The Moon)
            var aqua = new SocialEntityProfile
            {
                entityId = "familiar_aqua",
                displayName = "Aqua",
                subtitle = "Bubble Nymph Familiar",
                entityType = SocialEntityType.Familiar,
                arcana = TarotArcana.TheMoon,
                arcanaTitle = "THE MOON",
                portraitSpriteName = "Mockup_SocialPortrait_Aqua.png",
                loreDescription = "Gentle water-nymph familiar encased in shimmering celestial foam. Loves warm bubble baths, silky Star Flan, and shiny crystal spheres.",
                currentAffinity = 45,
                socialLinkRank = 3,
                currentMood = TomodachiMood.Happy,
                lovedGiftIds = new List<string> { "treat_flan", "toy_gem", "potion_elixir" },
                likedGiftIds = new List<string> { "toy_ribbon", "mat_ruby" },
                dislikedGiftIds = new List<string> { "blunt_rock" },
                hatedGiftIds = new List<string> { "fire_essence", "slimy_ooze" },
                lovedGiftLine = "*Giggles as sparkling bubbles swirl around her* Oh wonderful! The purest mana essence! Aqua adores you!",
                likedGiftLine = "*Splashes gently* So pretty and clear! Thank you, kind Summoner!",
                neutralGiftLine = "*Floating inside a bubble* A curious curio. Aqua shall wash it carefully.",
                dislikedGiftLine = "*Puffs cheeks* Too dry and dusty! Aqua prefers things that ripple and gleam!",
                hatedGiftLine = "*Hides in a hardened tidal shell* Scalding flames! Please do not burn my streams!",
                rankUpLine = "The tide of destiny flows through our souls! The Moon bestows restorative mist upon thy journey!",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_aqua",
                    title = "Tidal Benediction",
                    isBuff = true,
                    description = "+25% Mana Recovery Speed & Generates protective tidal bubble barriers.",
                    summonHpMultiplier = 1.25f,
                    autoShieldThreshold = 0.25f
                },
                negativeDebuff = new SocialLinkModifier
                {
                    id = "debuff_aqua",
                    title = "Tidal Sorrow",
                    isBuff = false,
                    description = "-20% Mana Recovery Speed & Soggy Movement Lag.",
                    playerMoveSpeedMultiplier = 0.88f
                },
                careState = new FamiliarCareState
                {
                    hunger = 65f,
                    hydration = 95f,
                    cleanliness = 95f,
                    happiness = 85f,
                    affectionHearts = 3,
                    bonusAttack = 10,
                    bonusDefense = 20,
                    bonusMagic = 35,
                    bonusSpeed = 15,
                    favoriteFood = "Star Flan",
                    favoriteToy = "Gleaming Gem Toy",
                    speciesDietDesc = "Thrives on crystal spring water, mana flan, and scented bubble baths."
                }
            };
            roster.Add(aqua);

            // 6. Summon: Aegis the Celestial Patronus (The Chariot)
            var aegis = new SocialEntityProfile
            {
                entityId = "summon_aegis",
                displayName = "Aegis",
                subtitle = "Celestial Guardian Patronus",
                entityType = SocialEntityType.Summon,
                arcana = TarotArcana.TheChariot,
                arcanaTitle = "THE CHARIOT",
                portraitSpriteName = "Mockup_SocialPortrait_Aegis.png",
                loreDescription = "Synthesized celestial warrior manifested from pure starlight and ritual geometry. Sworn to guard the summoner.",
                currentAffinity = 65,
                socialLinkRank = 5,
                currentMood = TomodachiMood.Ecstatic,
                lovedGiftIds = new List<string> { "potion_elixir", "relic_whetstone", "celestial_feather" },
                likedGiftIds = new List<string> { "potion_crimson", "weapon_bow" },
                dislikedGiftIds = new List<string> { "potion_amber" },
                hatedGiftIds = new List<string> { "void_flesh", "cursed_skull" },
                lovedGiftLine = "By the luminous heavens! This offering restores celestial radiance to my blade and bulwark!",
                likedGiftLine = "A warrior's tribute. I accept with honor, my summoner.",
                neutralGiftLine = "Acknowledged. My vigil remains unbroken.",
                dislikedGiftLine = "This item dulls my martial focus. Let us maintain discipline.",
                hatedGiftLine = "Abomination! Foul void taint will not be tolerated in the presence of the Celestial Chariot!",
                rankUpLine = "No blade may shatter what the stars have bound! The Chariot charges forth, an impenetrable bastion for thy life!",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_aegis",
                    title = "Divine Aegis Bulwark",
                    isBuff = true,
                    description = "Summon +35% ATK, +50% HP & Auto-Shields Player when HP < 30%.",
                    summonAttackMultiplier = 1.35f,
                    summonHpMultiplier = 1.50f,
                    autoShieldThreshold = 0.30f
                },
                negativeDebuff = new SocialLinkModifier
                {
                    id = "debuff_aegis",
                    title = "Rebellious Will",
                    isBuff = false,
                    description = "Summon -30% ATK & Slothful Insubordination (50% Slower Attacks).",
                    summonAttackMultiplier = 0.70f,
                    summonCooldownMultiplier = 1.50f
                }
            };
            roster.Add(aegis);

            // 6. Construct: Talos-04 (The Emperor)
            var talos = new SocialEntityProfile
            {
                entityId = "construct_talos",
                displayName = "Talos-04",
                subtitle = "Clockwork Adamantine Sentinel",
                entityType = SocialEntityType.Construct,
                arcana = TarotArcana.TheEmperor,
                arcanaTitle = "THE EMPEROR",
                portraitSpriteName = "Mockup_SocialPortrait_Talos.png",
                loreDescription = "Self-perpetuating clockwork automaton engineered with reinforced brass cogs and ancient pneumatic power cells.",
                currentAffinity = 30,
                socialLinkRank = 3,
                currentMood = TomodachiMood.Content,
                lovedGiftIds = new List<string> { "relic_whetstone", "mat_ruby", "refined_oil" },
                likedGiftIds = new List<string> { "weapon_staff", "potion_amber" },
                dislikedGiftIds = new List<string> { "potion_crimson" },
                hatedGiftIds = new List<string> { "acid_flask", "rust_powder" },
                lovedGiftLine = "[SYSTEM AUDIO]: OPTIMAL FRICTION COEFFICIENT RESTORED. ADAMANTINE GEARS COMMENCE HYPER-SYNCHRONIZATION. GRATITUDE REGISTERED.",
                likedGiftLine = "[SYSTEM AUDIO]: THERMAL ENERGY COMPATIBILITY: 94.2%. ACCEPTABLE RECHARGING MEDIUM.",
                neutralGiftLine = "[SYSTEM AUDIO]: OBJECT CATALOGED IN STORAGE BUFFER.",
                dislikedGiftLine = "[WARNING]: VISCOUS RESIDUE DETECTED. COG CLOGGING PROBABILITY: ELEVATED.",
                hatedGiftLine = "[CRITICAL ALERT]: CORROSIVE COMPOUND IDENTIFIED. THREAT LEVEL: MAXIMUM. INITIATING DEFENSIVE OVERDRIVE!",
                rankUpLine = "[DIRECTIVE 01 CONFIRMED]: SOVEREIGN BOND ESTABLISHED. The Emperor ordains impenetrable adamantine fortification upon thy frame.",
                positiveBuff = new SocialLinkModifier
                {
                    id = "buff_talos",
                    title = "Adamantine Bulwark",
                    isBuff = true,
                    description = "Player +25% Defense & Complete Knockback Resistance.",
                    playerDefenseMultiplier = 1.25f
                },
                negativeDebuff = new SocialLinkModifier
                {
                    id = "debuff_talos",
                    title = "Clockwork Jam",
                    isBuff = false,
                    description = "-15% Player Move Speed & Gear Friction Drag.",
                    playerMoveSpeedMultiplier = 0.85f
                }
            };
            roster.Add(talos);
        }

        public SocialEntityProfile GetEntity(string entityId)
        {
            return roster.Find(x => x.entityId.Equals(entityId, StringComparison.OrdinalIgnoreCase));
        }

        public List<SocialEntityProfile> GetEntitiesByType(SocialEntityType type)
        {
            if (type == SocialEntityType.All) return new List<SocialEntityProfile>(roster);
            return roster.FindAll(x => x.entityType == type);
        }

        /// <summary>
        /// Gifts an item to an entity. Calculates affinity changes, triggers Persona VN reaction,
        /// adjusts mood, evaluates Social Link Rank Up, and deducts the item from inventory.
        /// </summary>
        public bool GiveGift(string entityId, string itemId)
        {
            var entity = GetEntity(entityId);
            if (entity == null) return false;

            // Evaluate gift reaction
            GiftReaction reaction = entity.EvaluateGift(itemId);

            int baseDelta = reaction switch
            {
                GiftReaction.Loved => 25,
                GiftReaction.Liked => 12,
                GiftReaction.Neutral => 4,
                GiftReaction.Disliked => -12,
                GiftReaction.Hated => -28,
                _ => 2
            };

            // Mood modifier
            float moodMult = entity.currentMood switch
            {
                TomodachiMood.Ecstatic => (baseDelta > 0) ? 1.5f : 0.8f,
                TomodachiMood.Happy => (baseDelta > 0) ? 1.2f : 1.0f,
                TomodachiMood.Annoyed => (baseDelta > 0) ? 0.75f : 1.3f,
                TomodachiMood.Enraged => (baseDelta > 0) ? 0.5f : 1.6f,
                TomodachiMood.Bored => 1.0f,
                _ => 1.0f
            };

            int finalDelta = Mathf.RoundToInt(baseDelta * moodMult);
            entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + finalDelta, -100, 100);

            // Shift mood based on reaction
            switch (reaction)
            {
                case GiftReaction.Loved:
                    entity.currentMood = TomodachiMood.Ecstatic;
                    break;
                case GiftReaction.Liked:
                    entity.currentMood = (entity.currentMood == TomodachiMood.Ecstatic) ? TomodachiMood.Ecstatic : TomodachiMood.Happy;
                    break;
                case GiftReaction.Disliked:
                    entity.currentMood = TomodachiMood.Annoyed;
                    break;
                case GiftReaction.Hated:
                    entity.currentMood = TomodachiMood.Enraged;
                    break;
            }

            // Deduct item from inventory if available
            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.DeductItem(itemId, 1);
            }

            // SFX & Toast
            PlayGiftReactionAudio(reaction);
            ShowGiftReactionToast(entity, reaction, finalDelta);

            // Check rank up
            CheckSocialLinkRankUp(entity);

            // Play Persona Visual Novel Dialogue
            PlayGiftDialogueCutscene(entity, reaction);

            OnGiftGiven?.Invoke(entity, finalDelta, reaction);
            OnAffinityChanged?.Invoke(entity, entity.currentAffinity);
            OnMoodChanged?.Invoke(entity, entity.currentMood);

            return true;
        }

        private void CheckSocialLinkRankUp(SocialEntityProfile entity)
        {
            if (entity.currentAffinity > 0)
            {
                // Each 15-20 positive affinity permits a higher rank up to 10
                int targetRank = Mathf.Clamp(entity.currentAffinity / 15, 0, 10);
                if (targetRank > entity.socialLinkRank)
                {
                    entity.socialLinkRank = targetRank;
                    TriggerRankUpSequence(entity);
                }
            }
        }

        private void TriggerRankUpSequence(SocialEntityProfile entity)
        {
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("level_up", 1.0f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(
                    $"<color=#FFD700><b>★ SOCIAL LINK RANK UP! ★</b></color>\n" +
                    $"<b>{entity.displayName.ToUpper()}</b> // {entity.arcanaTitle}\n" +
                    $"Reached <b>RANK {entity.socialLinkRank}</b>!"
                );
            }

            OnRankUp?.Invoke(entity, entity.socialLinkRank);

            // Trigger Persona Cutscene announcement
            if (PersonaDialogueUI.Instance != null)
            {
                Sprite port = LoadSprite(entity.portraitSpriteName);
                string[] lines = new string[]
                {
                    $"<i>\"Thou hast turned a vow into a bloodstained chain of fate...\"</i>",
                    $"The bond with <b>{entity.displayName.ToUpper()}</b> ({entity.arcanaTitle}) has reached <b>RANK {entity.socialLinkRank}</b>!",
                    entity.rankUpLine
                };
                PersonaDialogueUI.Instance.ShowSimpleDialogue(entity.displayName, entity.arcanaTitle, port, lines);
            }
        }

        public void TalkWithEntity(SocialEntityProfile entity)
        {
            if (entity == null) return;

            Sprite port = LoadSprite(entity.portraitSpriteName);

            if (entity.chatBranches != null && entity.chatBranches.Count > 0)
            {
                PersonaChatBranch branch = entity.chatBranches[0];

                if (PersonaDialogueUI.Instance != null)
                {
                    PersonaChoiceOption choiceA = new PersonaChoiceOption(branch.choiceOptionA, () =>
                    {
                        ApplyChatChoice(entity, branch.affinityDeltaA, branch.moodA, branch.reactionDialogueA, port);
                    });

                    PersonaChoiceOption choiceB = new PersonaChoiceOption(branch.choiceOptionB, () =>
                    {
                        ApplyChatChoice(entity, branch.affinityDeltaB, branch.moodB, branch.reactionDialogueB, port);
                    });

                    PersonaDialogueLine promptLine = new PersonaDialogueLine
                    {
                        speakerName = entity.displayName,
                        speakerTitle = entity.subtitle,
                        portraitSprite = port,
                        text = branch.promptDialogue
                    };

                    PersonaDialogueUI.Instance.StartDialogueSequence(new List<PersonaDialogueLine> { promptLine }, () =>
                    {
                        PersonaDialogueUI.Instance.ShowChoices(new PersonaChoiceOption[] { choiceA, choiceB });
                    });
                }
            }
            else
            {
                string greeting = entity.GetRelationshipTier() switch
                {
                    RelationshipTier.Soulbound => $"My soul is forever aligned with thine, Wanderer. Speak, and I shall heed.",
                    RelationshipTier.Friendly => $"Good to see you again! The Spire feels a little brighter when you are near.",
                    RelationshipTier.Distrusted => $"State your business quickly. I have little patience for idle prattle.",
                    RelationshipTier.Nemesis => $"You dare approach me? Turn back before you regret your insolence.",
                    _ => $"Greetings, traveler. How fares your ascent through the floors?"
                };

                if (PersonaDialogueUI.Instance != null)
                {
                    PersonaDialogueUI.Instance.ShowSimpleDialogue(entity.displayName, entity.arcanaTitle, port, new string[] { greeting });
                }
            }
        }

        private void ApplyChatChoice(SocialEntityProfile entity, int delta, TomodachiMood newMood, string reactionLine, Sprite port)
        {
            entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + delta, -100, 100);
            entity.currentMood = newMood;

            CheckSocialLinkRankUp(entity);

            OnAffinityChanged?.Invoke(entity, entity.currentAffinity);
            OnMoodChanged?.Invoke(entity, entity.currentMood);

            if (PersonaDialogueUI.Instance != null)
            {
                PersonaDialogueUI.Instance.ShowSimpleDialogue(entity.displayName, entity.arcanaTitle, port, new string[] { reactionLine });
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                string sign = (delta >= 0) ? $"+{delta}" : $"{delta}";
                string col = (delta >= 0) ? "#30FF80" : "#FF4050";
                DevClassLoadoutManager.Instance.ShowToast($"<color={col}>[SOCIAL LINK {sign}]</color>\n{entity.displayName} is now {entity.GetMoodEmoji()}");
            }
        }

        public void SparWithEntity(SocialEntityProfile entity)
        {
            if (entity == null) return;

            int delta = 8;
            entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + delta, -100, 100);
            entity.currentMood = TomodachiMood.Happy;

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("heavy_impact", 0.9f);
            }

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(
                    $"<color=#30FF80><b>[MARTIAL SPARRING COMPLETED]</b></color>\n" +
                    $"Bonds forged in combat with {entity.displayName} (+{delta} Affinity)!\n" +
                    $"{entity.GetMoodEmoji()}"
                );
            }

            CheckSocialLinkRankUp(entity);
            OnAffinityChanged?.Invoke(entity, entity.currentAffinity);
            OnMoodChanged?.Invoke(entity, entity.currentMood);

            Sprite port = LoadSprite(entity.portraitSpriteName);
            string sparLine = entity.entityType switch
            {
                SocialEntityType.Summon => "A ferocious clash! Our spirits synchronize perfectly upon the battlefield.",
                SocialEntityType.Construct => "[SYSTEM AUDIO]: COMBAT CALIBRATION COMPLETE. WEAR-PATTERNS REINFORCED.",
                _ => "*Flutters fiercely* A worthy sparring session! You cannot catch my aerial strikes!"
            };

            if (PersonaDialogueUI.Instance != null)
            {
                PersonaDialogueUI.Instance.ShowSimpleDialogue(entity.displayName, entity.arcanaTitle, port, new string[] { sparLine });
            }
        }

        private void PlayGiftDialogueCutscene(SocialEntityProfile entity, GiftReaction reaction)
        {
            if (PersonaDialogueUI.Instance == null) return;

            Sprite port = LoadSprite(entity.portraitSpriteName);
            string line = reaction switch
            {
                GiftReaction.Loved => entity.lovedGiftLine,
                GiftReaction.Liked => entity.likedGiftLine,
                GiftReaction.Disliked => entity.dislikedGiftLine,
                GiftReaction.Hated => entity.hatedGiftLine,
                _ => entity.neutralGiftLine
            };

            if (string.IsNullOrEmpty(line)) line = "Thank you for the gift, Wanderer.";
            PersonaDialogueUI.Instance.ShowSimpleDialogue(entity.displayName, entity.arcanaTitle, port, new string[] { line });
        }

        private void PlayGiftReactionAudio(GiftReaction reaction)
        {
            if (BattleSummonerAudioMaster.Instance == null) return;

            switch (reaction)
            {
                case GiftReaction.Loved:
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("level_up", 0.9f);
                    break;
                case GiftReaction.Liked:
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.8f);
                    break;
                case GiftReaction.Disliked:
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.7f);
                    break;
                case GiftReaction.Hated:
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("enemy_boss_defeat", 0.9f);
                    break;
                default:
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
                    break;
            }
        }

        private void ShowGiftReactionToast(SocialEntityProfile entity, GiftReaction reaction, int delta)
        {
            if (DevClassLoadoutManager.Instance == null) return;

            string sign = (delta >= 0) ? $"+{delta}" : $"{delta}";
            string reactTag = reaction switch
            {
                GiftReaction.Loved => "<color=#FF4090><b>♥ LOVED GIFT! ♥</b></color>",
                GiftReaction.Liked => "<color=#30FF80><b>★ LIKED GIFT! ★</b></color>",
                GiftReaction.Neutral => "<color=#A0A0A0><b>ACCEPTABLE GIFT</b></color>",
                GiftReaction.Disliked => "<color=#FF9020><b>▼ DISLIKED GIFT ▼</b></color>",
                GiftReaction.Hated => "<color=#FF2030><b>☠ HATED GIFT! ☠</b></color>",
                _ => ""
            };

            DevClassLoadoutManager.Instance.ShowToast(
                $"{reactTag}\n" +
                $"<b>{entity.displayName}</b> ({entity.arcanaTitle})\n" +
                $"Affinity: {sign} ({entity.currentAffinity}/100) // {entity.GetMoodEmoji()}"
            );
        }

        #region Active Modifiers & API
        public float GetShopBuyPriceMultiplier()
        {
            var balthazar = GetEntity("npc_balthazar");
            if (balthazar != null)
            {
                var mod = balthazar.GetActiveEffect();
                if (mod != null) return mod.shopBuyMultiplier;
            }
            return 1.0f;
        }

        public float GetShopSellPriceMultiplier()
        {
            var balthazar = GetEntity("npc_balthazar");
            if (balthazar != null)
            {
                var mod = balthazar.GetActiveEffect();
                if (mod != null) return mod.shopSellMultiplier;
            }
            return 1.0f;
        }

        public float GetPlayerXpMultiplier()
        {
            var margo = GetEntity("npc_margo");
            if (margo != null)
            {
                var mod = margo.GetActiveEffect();
                if (mod != null) return mod.xpMultiplier;
            }
            return 1.0f;
        }

        public float GetPlayerDefenseMultiplier()
        {
            float mult = 1.0f;
            var talos = GetEntity("construct_talos");
            if (talos != null)
            {
                var mod = talos.GetActiveEffect();
                if (mod != null) mult *= mod.playerDefenseMultiplier;
            }
            return mult;
        }

        public float GetPlayerMoveSpeedMultiplier()
        {
            float mult = 1.0f;
            var corvus = GetEntity("familiar_corvus");
            if (corvus != null)
            {
                var mod = corvus.GetActiveEffect();
                if (mod != null) mult *= mod.playerMoveSpeedMultiplier;
            }
            var talos = GetEntity("construct_talos");
            if (talos != null)
            {
                var mod = talos.GetActiveEffect();
                if (mod != null) mult *= mod.playerMoveSpeedMultiplier;
            }
            return mult;
        }

        public void GetSummonStatsMultiplier(out float atkMult, out float hpMult, out float cdMult, out float autoShieldThreshold)
        {
            atkMult = 1.0f;
            hpMult = 1.0f;
            cdMult = 1.0f;
            autoShieldThreshold = 0f;

            var aegis = GetEntity("summon_aegis");
            if (aegis != null)
            {
                var mod = aegis.GetActiveEffect();
                if (mod != null)
                {
                    atkMult *= mod.summonAttackMultiplier;
                    hpMult *= mod.summonHpMultiplier;
                    cdMult *= mod.summonCooldownMultiplier;
                    autoShieldThreshold = mod.autoShieldThreshold;
                }
            }

            // Ni no Kuni & Pokémon Affection bonuses from all bonded familiars
            for (int i = 0; i < roster.Count; i++)
            {
                var f = roster[i];
                if (f.entityType == SocialEntityType.Familiar && f.careState != null)
                {
                    atkMult += f.careState.bonusAttack * 0.008f;
                    hpMult += f.careState.bonusDefense * 0.008f;
                    if (f.careState.affectionHearts >= 4)
                    {
                        atkMult *= 1.10f;
                    }
                    if (f.careState.affectionHearts >= 5)
                    {
                        autoShieldThreshold = Mathf.Max(autoShieldThreshold, 0.20f);
                    }
                }
            }
        }
        #endregion

        #region Tamagotchi Pet Familiar Care Interactions
        public List<PetTreatItem> GetAvailableTreats() => availableTreats;
        public List<PetPresentItem> GetAvailablePresents() => availablePresents;

        public bool FeedFamiliar(string entityId, PetTreatItem treat, out string feedback)
        {
            var entity = GetEntity(entityId);
            if (entity == null)
            {
                feedback = "Familiar not found.";
                return false;
            }

            bool success = entity.careState.Feed(treat, out feedback);
            if (success)
            {
                entity.currentMood = entity.careState.happiness >= 80f ? TomodachiMood.Ecstatic : TomodachiMood.Happy;
                entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + 5, -100, 100);
                OnPetFed?.Invoke(entity, treat);
                OnAffinityChanged?.Invoke(entity, entity.currentAffinity);
                OnMoodChanged?.Invoke(entity, entity.currentMood);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("pet_munch", 1.0f);
                }
            }
            return success;
        }

        public bool GiveWaterToFamiliar(string entityId, out string feedback)
        {
            var entity = GetEntity(entityId);
            if (entity == null)
            {
                feedback = "Familiar not found.";
                return false;
            }

            bool success = entity.careState.GiveWater(out feedback);
            if (success)
            {
                entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + 3, -100, 100);
                OnPetWatered?.Invoke(entity);
                OnAffinityChanged?.Invoke(entity, entity.currentAffinity);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("pet_slurp", 1.0f);
                }
            }
            return success;
        }

        public bool BatheFamiliar(string entityId, out string feedback)
        {
            var entity = GetEntity(entityId);
            if (entity == null)
            {
                feedback = "Familiar not found.";
                return false;
            }

            bool success = entity.careState.Bathe(out feedback);
            if (success)
            {
                entity.currentMood = TomodachiMood.Ecstatic;
                entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + 8, -100, 100);
                OnPetBathed?.Invoke(entity);
                OnAffinityChanged?.Invoke(entity, entity.currentAffinity);
                OnMoodChanged?.Invoke(entity, entity.currentMood);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("pet_bubble_bath", 1.0f);
                }
            }
            return success;
        }

        public bool GivePresentToFamiliar(string entityId, PetPresentItem present, out string feedback)
        {
            var entity = GetEntity(entityId);
            if (entity == null)
            {
                feedback = "Familiar not found.";
                return false;
            }

            bool success = entity.careState.GivePresent(present, out feedback);
            if (success)
            {
                entity.currentMood = TomodachiMood.Ecstatic;
                entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + present.affectionPoints, -100, 100);
                OnPetPresentGiven?.Invoke(entity, present);
                OnAffinityChanged?.Invoke(entity, entity.currentAffinity);
                OnMoodChanged?.Invoke(entity, entity.currentMood);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("pet_present_fanfare", 1.0f);
                }
            }
            return success;
        }

        public bool PetFamiliar(string entityId, out string feedback)
        {
            var entity = GetEntity(entityId);
            if (entity == null)
            {
                feedback = "Familiar not found.";
                return false;
            }

            bool success = entity.careState.Pet(out feedback);
            if (success)
            {
                entity.currentAffinity = Mathf.Clamp(entity.currentAffinity + 2, -100, 100);
                OnPetPetted?.Invoke(entity);
                OnAffinityChanged?.Invoke(entity, entity.currentAffinity);

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("pet_happy_chirp", 1.0f);
                }
            }
            return success;
        }
        #endregion

        private Sprite LoadSprite(string textureName)
        {
#if UNITY_EDITOR
            string path = "Assets/Textures/UI/" + textureName;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}
