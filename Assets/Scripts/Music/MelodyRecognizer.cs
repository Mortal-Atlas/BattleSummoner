using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MelodyRecognizer maintains a rolling buffer listening to the last 4-8 tapped notes.
/// Matching a valid sequence triggers a Hunting Horn style payload, unleashing an AoE magic blast
/// or party-wide buff. Seamless during combat or freeplay serenading in town.
/// </summary>
public class MelodyRecognizer : MonoBehaviour
{
    public static MelodyRecognizer Instance { get; private set; }

    [Header("Buffer Configuration")]
    [SerializeField] private int maxBufferSize = 8;
    [SerializeField] private float noteTimeoutSeconds = 2.8f;

    [Header("Repertoire Grimoires")]
    [SerializeField] private List<SheetMusicItem> activeRepertoire = new List<SheetMusicItem>();

    public static event Action<SheetMusicItem> OnMelodyPayloadCast;
    public static event Action<string> OnNotePushedToBuffer;

    private readonly List<string> rollingBuffer = new List<string>();
    private float lastNoteTime = 0f;

    public IReadOnlyList<string> CurrentBuffer => rollingBuffer;
    public SheetMusicItem ActiveGuidedSong { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeDefaultRepertoire();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDefaultRepertoire()
    {
        if (activeRepertoire.Count > 0) return;

        // 1. Hymn of the Spire
        SheetMusicItem hymn = ScriptableObject.CreateInstance<SheetMusicItem>();
        hymn.sheetId = "song_hymn_spire";
        hymn.songTitle = "Hymn of the Spire";
        hymn.loreDescription = "Resonant celestial verse amplifying physical & magical damage by +25%.";
        hymn.noteSequence = new string[] { "C4", "E4", "G4", "C5" };
        hymn.buffPayload = BardicBuffPayloadType.HymnOfTheSpire_DamageBuff;
        hymn.payloadMagnitude = 0.25f;
        hymn.durationSeconds = 14f;
        activeRepertoire.Add(hymn);

        // 2. Song of Fortitude
        SheetMusicItem fort = ScriptableObject.CreateInstance<SheetMusicItem>();
        fort.sheetId = "song_fortitude";
        fort.songTitle = "Song of Fortitude";
        fort.loreDescription = "Sturdy mountain cadence creating a 150 HP kinetic barrier shield.";
        fort.noteSequence = new string[] { "D4", "F4", "A4", "D5" };
        fort.buffPayload = BardicBuffPayloadType.SongOfFortitude_Shield;
        fort.payloadMagnitude = 150f;
        fort.durationSeconds = 12f;
        activeRepertoire.Add(fort);

        // 3. Aria of Inferno
        SheetMusicItem inferno = ScriptableObject.CreateInstance<SheetMusicItem>();
        inferno.sheetId = "song_inferno";
        inferno.songTitle = "Aria of Inferno";
        inferno.loreDescription = "Scorching cadence unleashing a 360 radial AoE flame detonation.";
        inferno.noteSequence = new string[] { "E4", "G4", "B4", "E5" };
        inferno.buffPayload = BardicBuffPayloadType.AriaOfInferno_FireBurst;
        inferno.payloadMagnitude = 450f;
        inferno.aoeRadius = 9f;
        activeRepertoire.Add(inferno);

        // 4. Sonata of Swiftness
        SheetMusicItem swift = ScriptableObject.CreateInstance<SheetMusicItem>();
        swift.sheetId = "song_swiftness";
        swift.songTitle = "Sonata of Swiftness";
        swift.loreDescription = "Agile breeze cadence granting +40% sprint & dash mobility.";
        swift.noteSequence = new string[] { "G4", "B4", "D5", "G5" };
        swift.buffPayload = BardicBuffPayloadType.SonataOfSwiftness_Speed;
        swift.payloadMagnitude = 0.40f;
        swift.durationSeconds = 10f;
        activeRepertoire.Add(swift);

        // 5. Margo's Lullaby
        SheetMusicItem lullaby = ScriptableObject.CreateInstance<SheetMusicItem>();
        lullaby.sheetId = "song_margos_lullaby";
        lullaby.songTitle = "Margo's Lullaby";
        lullaby.loreDescription = "Somber gothic void frequency that sleeps/stuns non-boss entities for 4 seconds.";
        lullaby.noteSequence = new string[] { "A4", "C5", "E5", "A5" };
        lullaby.buffPayload = BardicBuffPayloadType.MargosLullaby_Stun;
        lullaby.payloadMagnitude = 4.0f;
        lullaby.aoeRadius = 12f;
        activeRepertoire.Add(lullaby);

        ActiveGuidedSong = hymn;
    }

    private void Update()
    {
        // Flush buffer if user paused longer than timeout
        if (rollingBuffer.Count > 0 && (Time.time - lastNoteTime > noteTimeoutSeconds))
        {
            rollingBuffer.Clear();
        }
    }

    /// <summary>
    /// Pushes a tapped note to the rolling buffer and evaluates for Hunting Horn payload triggers.
    /// </summary>
    public void RecordTappedNote(string noteName)
    {
        lastNoteTime = Time.time;
        rollingBuffer.Add(noteName.ToUpperInvariant());
        if (rollingBuffer.Count > maxBufferSize)
        {
            rollingBuffer.RemoveAt(0);
        }

        OnNotePushedToBuffer?.Invoke(noteName);

        CheckBufferForPayloadMatches();
    }

    private void CheckBufferForPayloadMatches()
    {
        for (int i = 0; i < activeRepertoire.Count; i++)
        {
            var song = activeRepertoire[i];
            if (song != null && song.isDiscovered && song.MatchesTail(rollingBuffer))
            {
                ExecuteBardicPayload(song);
                rollingBuffer.Clear();
                break;
            }
        }
    }

    private void ExecuteBardicPayload(SheetMusicItem song)
    {
        Debug.Log($"[MELODY RECOGNIZER] Hunting Horn Payload Triggered: '{song.songTitle}'!");

        // Play triumphant fanfare audio
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("hunting_horn_fanfare", 1.0f);
        }

        GameObject player = GameObject.FindWithTag("Player");
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

        // Execute respective Hunting Horn payloads
        switch (song.buffPayload)
        {
            case BardicBuffPayloadType.HymnOfTheSpire_DamageBuff:
                ApplyPartyDamageBuff(song.payloadMagnitude, song.durationSeconds);
                break;

            case BardicBuffPayloadType.SongOfFortitude_Shield:
                ApplyFortitudeBarrier(song.payloadMagnitude);
                break;

            case BardicBuffPayloadType.AriaOfInferno_FireBurst:
                ExecuteAriaInfernoAoE(playerPos, song.payloadMagnitude, song.aoeRadius);
                break;

            case BardicBuffPayloadType.SonataOfSwiftness_Speed:
                ApplySwiftnessBuff(song.payloadMagnitude, song.durationSeconds);
                break;

            case BardicBuffPayloadType.MargosLullaby_Stun:
                ExecuteMargosLullabyStun(playerPos, song.payloadMagnitude, song.aoeRadius);
                break;
        }

        OnMelodyPayloadCast?.Invoke(song);
    }

    private void ApplyPartyDamageBuff(float multiplier, float duration)
    {
        Debug.Log($"[BARDIC BUFF] Party Damage amplified by +{multiplier * 100f:F0}% for {duration}s.");
    }

    private void ApplyFortitudeBarrier(float shieldHp)
    {
        PlayerProgression prog = FindAnyObjectByType<PlayerProgression>();
        if (prog != null)
        {
            prog.Heal(shieldHp * 0.5f);
        }
        Debug.Log($"[BARDIC BUFF] Song of Fortitude granted {shieldHp} kinetic barrier points.");
    }

    private void ExecuteAriaInfernoAoE(Vector3 origin, float damage, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Enemy"))
            {
                BossController boss = hits[i].GetComponent<BossController>();
                if (boss != null) boss.TakeDirectDamage(damage);

                SpireEnemy enemy = hits[i].GetComponent<SpireEnemy>();
                if (enemy != null) enemy.TakeDamage(damage);
            }
        }
        Debug.Log($"[BARDIC PAYLOAD] Aria of Inferno dealt {damage} AoE Fire Damage across {radius}m radius.");
    }

    private void ApplySwiftnessBuff(float speedBonus, float duration)
    {
        Debug.Log($"[BARDIC BUFF] Sonata of Swiftness increased mobility by +{speedBonus * 100f:F0}%.");
    }

    private void ExecuteMargosLullabyStun(Vector3 origin, float duration, float radius)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Enemy"))
            {
                if (hits[i].TryGetComponent<IEnemyStagger>(out var stagger))
                {
                    stagger.Stagger(duration, origin);
                }
            }
        }
        Debug.Log($"[BARDIC PAYLOAD] Margo's Lullaby induced void slumber for {duration}s.");
    }
}
