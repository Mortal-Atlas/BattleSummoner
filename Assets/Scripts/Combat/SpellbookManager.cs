using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpellbookManager allows spellcasters (Mage) and Bards to equip multiple books/grimoires
/// simultaneously and summon their pages at any time for spellcasting and performance assistance.
/// </summary>
public class SpellbookManager : MonoBehaviour
{
    private static SpellbookManager _instance;
    public static SpellbookManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SpellbookManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Equipped Books")]
    [SerializeField] private List<SpellbookItem> equippedBooks = new List<SpellbookItem>();
    [SerializeField] private int activeBookIndex = 0;

    [Header("UI Reference")]
    [SerializeField] private GrimoireAssistantUI grimoireUI;

    public static event Action<SpellbookItem> OnBookEquipped;
    public static event Action<SpellbookItem> OnBookUnequipped;
    public static event Action<int> OnActiveBookChanged;

    public IReadOnlyList<SpellbookItem> EquippedBooks
    {
        get
        {
            if (equippedBooks.Count == 0)
            {
                InitializeDefaultSpellbooks();
            }
            return equippedBooks;
        }
    }
    public int ActiveBookIndex => activeBookIndex;
    public SpellbookItem ActiveBook => (EquippedBooks.Count > 0 && activeBookIndex >= 0 && activeBookIndex < EquippedBooks.Count) 
        ? EquippedBooks[activeBookIndex] : null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeDefaultSpellbooks();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (grimoireUI == null)
        {
            grimoireUI = FindAnyObjectByType<GrimoireAssistantUI>();
        }
    }

    private void Update()
    {
        // Desktop shortcut: 'B' toggles Grimoire Assistant
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleGrimoire();
        }
    }

    public void InitializeDefaultSpellbooks()
    {
        if (equippedBooks.Count > 0) return;

        // 1. Ignis Compendium (Mage - Pyromancy)
        SpellbookItem ignis = ScriptableObject.CreateInstance<SpellbookItem>();
        ignis.bookId = "book_ignis";
        ignis.bookTitle = "Ignis Compendium";
        ignis.authorLore = "Grand Magus Ignis, Lord of Cinders";
        ignis.description = "Bound in dragon scale, this grimoire contains explosive fire runes capable of incinerating dungeon denizens.";
        ignis.themeColor = new Color(1.0f, 0.40f, 0.15f);
        ignis.allowedClasses = new PlayerClassType[] { PlayerClassType.Mage };

        ignis.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "ignis_fireball",
            spellName = "Fireball",
            element = SpellElement.Fire,
            manaCost = 25,
            loreDescription = "Conjures a concentrated sphere of blazing fire that explodes upon striking the target.",
            gestureShapeName = "Triangle",
            gestureInstructions = "Draw a Triangle starting from top apex, down right, across left, and back to apex. Then tap spinning CAST.",
            guideStrokePoints = new Vector2[] { new Vector2(0f, 1f), new Vector2(0.866f, -0.5f), new Vector2(-0.866f, -0.5f), new Vector2(0f, 1f) }
        });

        ignis.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "ignis_singularity",
            spellName = "Inferno Singularity",
            element = SpellElement.Fire,
            manaCost = 50,
            loreDescription = "A gravitational flame vortex that pulls nearby monsters into an infernal vortex.",
            gestureShapeName = "Circle + Triangle",
            gestureInstructions = "Draw an outer Circle, then inscribe a Triangle inside. Tap CAST when primed.",
            guideStrokePoints = new Vector2[] { new Vector2(0f, 1f), new Vector2(1f, 0f), new Vector2(0f, -1f), new Vector2(-1f, 0f), new Vector2(0f, 1f) }
        });

        equippedBooks.Add(ignis);

        // 2. Glacies Codex (Mage - Cryomancy)
        SpellbookItem glacies = ScriptableObject.CreateInstance<SpellbookItem>();
        glacies.bookId = "book_glacies";
        glacies.bookTitle = "Glacies Codex";
        glacies.authorLore = "Arch-Sorceress Lysandra of the Frost Peaks";
        glacies.description = "Chilled parchment that radiates sub-zero ether. Teaches precision frost projection and freezing barriers.";
        glacies.themeColor = new Color(0.20f, 0.80f, 1.0f);
        glacies.allowedClasses = new PlayerClassType[] { PlayerClassType.Mage };

        glacies.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "glacies_ice_lance",
            spellName = "Ice Lance",
            element = SpellElement.Water,
            manaCost = 28,
            loreDescription = "Fires a razor-sharp spear of permafrost that pierces enemy defenses and inflicts deep chill.",
            gestureShapeName = "Wavy Lines",
            gestureInstructions = "Draw horizontal undulating wave lines left-to-right. Tap CAST when primed.",
            guideStrokePoints = new Vector2[] { new Vector2(-1f, 0f), new Vector2(-0.5f, 0.6f), new Vector2(0f, -0.6f), new Vector2(0.5f, 0.6f), new Vector2(1f, 0f) }
        });

        glacies.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "glacies_frost_ward",
            spellName = "Frost Barrier",
            element = SpellElement.Rock,
            manaCost = 35,
            loreDescription = "Erects a protective crystalline aegis that deflects physical impacts.",
            gestureShapeName = "Square",
            gestureInstructions = "Draw a 4-corner Square starting from top-left. Tap CAST to deploy barrier.",
            guideStrokePoints = new Vector2[] { new Vector2(-0.8f, 0.8f), new Vector2(0.8f, 0.8f), new Vector2(0.8f, -0.8f), new Vector2(-0.8f, -0.8f), new Vector2(-0.8f, 0.8f) }
        });

        equippedBooks.Add(glacies);

        // 3. Astral Vitae Tome (Mage & Bard - Restoration & Arcane)
        SpellbookItem vitae = ScriptableObject.CreateInstance<SpellbookItem>();
        vitae.bookId = "book_vitae";
        vitae.bookTitle = "Astral Vitae Tome";
        vitae.authorLore = "High Priestess Elowen of the Celestial Sanctum";
        vitae.description = "Illuminated celestial scripture that channels cosmic radiance to heal wounds and mend armor integrity.";
        vitae.themeColor = new Color(0.35f, 1.0f, 0.60f);
        vitae.allowedClasses = new PlayerClassType[] { PlayerClassType.Mage, PlayerClassType.Bard };

        vitae.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "vitae_divine_mend",
            spellName = "Divine Mend",
            element = SpellElement.Healing,
            manaCost = 30,
            loreDescription = "Restores 250 HP to the caster and clears active bleed and poison debuffs.",
            gestureShapeName = "Heart",
            gestureInstructions = "Draw a Heart shape starting from bottom dip to top curves. Tap CAST to heal.",
            guideStrokePoints = new Vector2[] { new Vector2(0f, -0.8f), new Vector2(-0.8f, 0.2f), new Vector2(-0.4f, 0.8f), new Vector2(0f, 0.4f), new Vector2(0.4f, 0.8f), new Vector2(0.8f, 0.2f), new Vector2(0f, -0.8f) }
        });

        equippedBooks.Add(vitae);

        // 4. Canticles of the Spire (Bard - Hymnal Vol. I)
        SpellbookItem canticles = ScriptableObject.CreateInstance<SpellbookItem>();
        canticles.bookId = "book_canticles";
        canticles.bookTitle = "Canticles of the Spire";
        canticles.authorLore = "Maestro Vane, First Bard of the Spire";
        canticles.description = "A golden musical hymnal bound in crushed velvet. Inscribes celestial melodies that harmonize with the Celestiphone.";
        canticles.themeColor = new Color(1.0f, 0.85f, 0.25f);
        canticles.allowedClasses = new PlayerClassType[] { PlayerClassType.Bard };

        canticles.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "song_hymn_spire",
            spellName = "Hymn of the Spire",
            element = SpellElement.Fire,
            manaCost = 20,
            loreDescription = "Resonant celestial melody amplifying party physical & magical damage by +25% for 14 seconds.",
            gestureShapeName = "Melody: C4-E4-G4-C5",
            gestureInstructions = "Play Kalimba notes: C4 -> E4 -> G4 -> C5. Auto-triggers harmonic damage aura.",
            noteSequence = new string[] { "C4", "E4", "G4", "C5" },
            buffPayload = BardicBuffPayloadType.HymnOfTheSpire_DamageBuff,
            payloadMagnitude = 0.25f,
            durationSeconds = 14f
        });

        canticles.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "song_inferno_aria",
            spellName = "Aria of Inferno",
            element = SpellElement.Fire,
            manaCost = 35,
            loreDescription = "Fiery allegro cadence unleashing a 360-degree radial blast dealing 450 fire damage.",
            gestureShapeName = "Melody: E4-G4-B4-E5",
            gestureInstructions = "Play Kalimba notes: E4 -> G4 -> B4 -> E5. Auto-unleashes radial shockwave.",
            noteSequence = new string[] { "E4", "G4", "B4", "E5" },
            buffPayload = BardicBuffPayloadType.AriaOfInferno_FireBurst,
            payloadMagnitude = 450f,
            durationSeconds = 1f
        });

        equippedBooks.Add(canticles);

        // 5. Ballads of Fortitude & Wind (Bard - Hymnal Vol. II)
        SpellbookItem ballads = ScriptableObject.CreateInstance<SpellbookItem>();
        ballads.bookId = "book_ballads";
        ballads.bookTitle = "Ballads of Fortitude";
        ballads.authorLore = "Skald Bran of the Iron Crags";
        ballads.description = "Sturdy leatherbound songbook filled with protective cadences and swift wind sonatas.";
        ballads.themeColor = new Color(0.95f, 0.60f, 0.30f);
        ballads.allowedClasses = new PlayerClassType[] { PlayerClassType.Bard };

        ballads.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "song_fortitude",
            spellName = "Song of Fortitude",
            element = SpellElement.Rock,
            manaCost = 25,
            loreDescription = "Steady subterranean cadence enveloping party in a 150 HP kinetic barrier.",
            gestureShapeName = "Melody: D4-F4-A4-D5",
            gestureInstructions = "Play Kalimba notes: D4 -> F4 -> A4 -> D5. Generates kinetic shield.",
            noteSequence = new string[] { "D4", "F4", "A4", "D5" },
            buffPayload = BardicBuffPayloadType.SongOfFortitude_Shield,
            payloadMagnitude = 150f,
            durationSeconds = 12f
        });

        ballads.inscribedSpells.Add(new GrimoireSpellEntry
        {
            spellId = "song_swiftness",
            spellName = "Sonata of Swiftness",
            element = SpellElement.Water,
            manaCost = 20,
            loreDescription = "Nimble zephyr rhythm granting +40% sprint & dash mobility for 10 seconds.",
            gestureShapeName = "Melody: G4-B4-D5-G5",
            gestureInstructions = "Play Kalimba notes: G4 -> B4 -> D5 -> G5. Enhances movement speed.",
            noteSequence = new string[] { "G4", "B4", "D5", "G5" },
            buffPayload = BardicBuffPayloadType.SonataOfSwiftness_Speed,
            payloadMagnitude = 0.40f,
            durationSeconds = 10f
        });

        equippedBooks.Add(ballads);
    }

    public void EquipBook(SpellbookItem book)
    {
        if (book == null || equippedBooks.Contains(book)) return;
        equippedBooks.Add(book);
        OnBookEquipped?.Invoke(book);
    }

    public void UnequipBook(SpellbookItem book)
    {
        if (book == null || !equippedBooks.Contains(book)) return;
        equippedBooks.Remove(book);
        if (activeBookIndex >= equippedBooks.Count)
        {
            activeBookIndex = Mathf.Max(0, equippedBooks.Count - 1);
        }
        OnBookUnequipped?.Invoke(book);
    }

    public void SelectBook(int index)
    {
        if (index >= 0 && index < equippedBooks.Count)
        {
            activeBookIndex = index;
            OnActiveBookChanged?.Invoke(activeBookIndex);
            if (grimoireUI != null)
            {
                grimoireUI.RefreshCurrentBook();
            }
        }
    }

    public void ToggleGrimoire()
    {
        if (grimoireUI == null) grimoireUI = FindAnyObjectByType<GrimoireAssistantUI>();
        if (grimoireUI != null)
        {
            grimoireUI.ToggleVisibility();
        }
    }

    /// <summary>
    /// Casting Assistant Action: When the player presses "PRIME SPELL" in the grimoire,
    /// directly prime the spell into SpellCastingFlowController, close the book, and
    /// bring up the spinning CAST disc in the center of the screen!
    /// </summary>
    public void PrimeSpellAssisted(GrimoireSpellEntry spell)
    {
        if (spell == null) return;

        Debug.Log($"📖 [GRIMOIRE ASSISTANT] Priming '{spell.spellName}' ({spell.element}) from active spellbook!");

        // If Bard song, play melody notes through melody recognizer
        if (spell.noteSequence != null && spell.noteSequence.Length > 0 && MelodyRecognizer.Instance != null)
        {
            for (int i = 0; i < spell.noteSequence.Length; i++)
            {
                MelodyRecognizer.Instance.RecordTappedNote(spell.noteSequence[i]);
            }
        }

        // Prime the elemental spell into SpellCastingFlowController
        if (SpellCastingFlowController.Instance != null)
        {
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            SpellCastingFlowController.Instance.PrimeSpell(spell.element, screenCenter, 140f, spell.spellName);
        }

        // Close the Grimoire so the player can immediately hit the spinning CAST button!
        if (grimoireUI != null)
        {
            grimoireUI.SetVisible(false);
        }

        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
        }
    }
}
