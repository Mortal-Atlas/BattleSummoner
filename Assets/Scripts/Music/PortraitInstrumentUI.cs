using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class KalimbaTineConfig
{
    public string noteName;
    public float frequency;
    public float relativeThumbOffset; // -1 (far left) to 0 (center) to +1 (far right)
    public float heightMultiplier;
}

/// <summary>
/// PortraitInstrumentUI implements a curved, staggered V-shape Kalimba layout
/// anchored strictly to the bottom third of the screen. Allows dual-thumb reaching
/// for 2 to 3 octaves of dotted note tines without obscuring the 3D dungeon above.
/// </summary>
public class PortraitInstrumentUI : MonoBehaviour
{
    public static PortraitInstrumentUI Instance { get; private set; }

    [Header("UI Root & Container")]
    [SerializeField] private RectTransform tinesContainer;
    [SerializeField] private CanvasGroup instrumentCanvasGroup;
    [SerializeField] private TextMeshProUGUI activeSongPromptText;
    [SerializeField] private TextMeshProUGUI recentNotesBufferText;

    [Header("Tine Configuration (17-Tine Dual-Thumb V-Arc)")]
    private readonly KalimbaTineConfig[] standardTines = new KalimbaTineConfig[]
    {
        // Alternating Kalimba order: Left (8 tines), Center (C4), Right (8 tines)
        new KalimbaTineConfig { noteName = "D6", frequency = 1174.66f, relativeThumbOffset = -0.90f, heightMultiplier = 0.52f },
        new KalimbaTineConfig { noteName = "B5", frequency = 987.77f,  relativeThumbOffset = -0.78f, heightMultiplier = 0.58f },
        new KalimbaTineConfig { noteName = "G5", frequency = 783.99f,  relativeThumbOffset = -0.66f, heightMultiplier = 0.64f },
        new KalimbaTineConfig { noteName = "E5", frequency = 659.25f,  relativeThumbOffset = -0.54f, heightMultiplier = 0.70f },
        new KalimbaTineConfig { noteName = "C5", frequency = 523.25f,  relativeThumbOffset = -0.42f, heightMultiplier = 0.76f },
        new KalimbaTineConfig { noteName = "A4", frequency = 440.00f,  relativeThumbOffset = -0.30f, heightMultiplier = 0.82f },
        new KalimbaTineConfig { noteName = "F4", frequency = 349.23f,  relativeThumbOffset = -0.19f, heightMultiplier = 0.88f },
        new KalimbaTineConfig { noteName = "D4", frequency = 293.66f,  relativeThumbOffset = -0.09f, heightMultiplier = 0.94f },

        // Center Root Tine (Lowest & Longest)
        new KalimbaTineConfig { noteName = "C4", frequency = 261.63f,  relativeThumbOffset = 0.00f,  heightMultiplier = 1.00f },

        // Right Hand Tines
        new KalimbaTineConfig { noteName = "E4", frequency = 329.63f,  relativeThumbOffset = 0.09f,  heightMultiplier = 0.94f },
        new KalimbaTineConfig { noteName = "G4", frequency = 392.00f,  relativeThumbOffset = 0.19f,  heightMultiplier = 0.88f },
        new KalimbaTineConfig { noteName = "B4", frequency = 493.88f,  relativeThumbOffset = 0.30f,  heightMultiplier = 0.82f },
        new KalimbaTineConfig { noteName = "D5", frequency = 587.33f,  relativeThumbOffset = 0.42f,  heightMultiplier = 0.76f },
        new KalimbaTineConfig { noteName = "F5", frequency = 698.46f,  relativeThumbOffset = 0.54f,  heightMultiplier = 0.70f },
        new KalimbaTineConfig { noteName = "A5", frequency = 880.00f,  relativeThumbOffset = 0.66f,  heightMultiplier = 0.64f },
        new KalimbaTineConfig { noteName = "C6", frequency = 1046.50f, relativeThumbOffset = 0.78f, heightMultiplier = 0.58f },
        new KalimbaTineConfig { noteName = "E6", frequency = 1318.51f, relativeThumbOffset = 0.90f, heightMultiplier = 0.52f }
    };

    private readonly List<GameObject> spawnedTineObjects = new List<GameObject>();
    private readonly Dictionary<string, Image> tineGlowDots = new Dictionary<string, Image>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        BuildKalimbaVLayout();
    }

    private void OnEnable()
    {
        MelodyRecognizer.OnNotePushedToBuffer += HandleNotePushed;
        MelodyRecognizer.OnMelodyPayloadCast += HandleMelodyCast;
        UpdateActiveSongGuideDisplay();
    }

    private void OnDisable()
    {
        MelodyRecognizer.OnNotePushedToBuffer -= HandleNotePushed;
        MelodyRecognizer.OnMelodyPayloadCast -= HandleMelodyCast;
    }

    /// <summary>
    /// Constructs the curved staggered V-shape layout procedurally if not pre-baked in prefab.
    /// </summary>
    public void BuildKalimbaVLayout()
    {
        if (tinesContainer == null)
        {
            tinesContainer = GetComponent<RectTransform>();
        }

        // Clear existing generated items if any
        for (int i = spawnedTineObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedTineObjects[i] != null) Destroy(spawnedTineObjects[i]);
        }
        spawnedTineObjects.Clear();
        tineGlowDots.Clear();

        float containerWidth = 1040f;
        float baseTineHeight = 440f;
        float tineWidth = 52f;

        for (int i = 0; i < standardTines.Length; i++)
        {
            var cfg = standardTines[i];

            // V-shaped thumb curve offset: Y dips down in the center and slopes slightly upward towards edges
            float xPos = cfg.relativeThumbOffset * (containerWidth * 0.48f);
            float yArc = Mathf.Abs(cfg.relativeThumbOffset) * 75f; // Curved ergonomic thumb sweep
            float currentHeight = baseTineHeight * cfg.heightMultiplier;

            GameObject tineObj = new GameObject($"Tine_{cfg.noteName}", typeof(RectTransform), typeof(Image), typeof(Button));
            tineObj.transform.SetParent(tinesContainer, false);
            spawnedTineObjects.Add(tineObj);

            RectTransform rt = tineObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(tineWidth, currentHeight);
            rt.anchoredPosition = new Vector2(xPos, yArc + 20f);

            // Metallic tine body visual
            Image img = tineObj.GetComponent<Image>();
            img.color = new Color(0.85f, 0.88f, 0.95f, 0.88f);

            // Dotted note tip at the tine top
            GameObject dotObj = new GameObject("DottedTip", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(tineObj.transform, false);
            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0.5f, 1f);
            dotRt.anchorMax = new Vector2(0.5f, 1f);
            dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.sizeDelta = new Vector2(28f, 28f);
            dotRt.anchoredPosition = new Vector2(0f, -16f);

            Image dotImg = dotObj.GetComponent<Image>();
            dotImg.color = new Color(0.2f, 0.7f, 1f, 0.95f);
            tineGlowDots[cfg.noteName] = dotImg;

            // Note label
            GameObject labelObj = new GameObject("LabelTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(tineObj.transform, false);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.sizeDelta = new Vector2(tineWidth, 36f);
            labelRt.anchoredPosition = new Vector2(0f, -20f);

            TextMeshProUGUI labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
            labelTmp.text = cfg.noteName;
            labelTmp.fontSize = 20;
            labelTmp.color = new Color(0.12f, 0.14f, 0.2f, 1f);
            labelTmp.alignment = TextAlignmentOptions.Center;

            // Zero-latency tap listener
            Button btn = tineObj.GetComponent<Button>();
            string capturedNote = cfg.noteName;
            float capturedFreq = cfg.frequency;
            btn.onClick.AddListener(() => OnTineTapped(capturedNote, capturedFreq, tineObj));
        }

        UpdateActiveSongGuideDisplay();
    }

    private void OnTineTapped(string noteName, float frequency, GameObject tineObj)
    {
        // 1. Zero-latency procedural audio playback
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayKalimbaNote(noteName, frequency, 1.0f);
        }

        // 2. Multiplayer RPC sync broadcast
        if (InstrumentFreeplaySync.Instance != null)
        {
            InstrumentFreeplaySync.Instance.BroadcastNoteTapped(noteName, frequency, 1.0f);
        }

        // 3. Push to Hunting Horn melody recognizer
        if (MelodyRecognizer.Instance != null)
        {
            MelodyRecognizer.Instance.RecordTappedNote(noteName);
        }

        // 4. Visual ripple bounce on tine
        LeanTineBounce(tineObj);
    }

    private void LeanTineBounce(GameObject tine)
    {
        if (tine == null) return;
        tine.transform.localScale = new Vector3(0.92f, 0.95f, 1f);
        Invoke(nameof(ResetAllTineScales), 0.07f);
    }

    private void ResetAllTineScales()
    {
        for (int i = 0; i < spawnedTineObjects.Count; i++)
        {
            if (spawnedTineObjects[i] != null)
            {
                spawnedTineObjects[i].transform.localScale = Vector3.one;
            }
        }
    }

    private void HandleNotePushed(string note)
    {
        if (recentNotesBufferText != null && MelodyRecognizer.Instance != null)
        {
            recentNotesBufferText.text = $"[MELODY BUFFER]: " + string.Join(" - ", MelodyRecognizer.Instance.CurrentBuffer);
        }
        HighlightGuidingNotes();
    }

    private void HandleMelodyCast(SheetMusicItem item)
    {
        if (activeSongPromptText != null)
        {
            activeSongPromptText.text = $"✨ [PAYLOAD ACTIVATED]: {item.songTitle}!";
        }
    }

    public void UpdateActiveSongGuideDisplay()
    {
        if (MelodyRecognizer.Instance != null && MelodyRecognizer.Instance.ActiveGuidedSong != null)
        {
            var song = MelodyRecognizer.Instance.ActiveGuidedSong;
            if (activeSongPromptText != null)
            {
                activeSongPromptText.text = $"[SONG] Repertoire: {song.songTitle} ({string.Join(" • ", song.noteSequence)})";
            }
        }
        HighlightGuidingNotes();
    }

    private void HighlightGuidingNotes()
    {
        if (MelodyRecognizer.Instance == null || MelodyRecognizer.Instance.ActiveGuidedSong == null) return;

        var song = MelodyRecognizer.Instance.ActiveGuidedSong;
        int currentBufferLen = MelodyRecognizer.Instance.CurrentBuffer.Count;

        string nextGuidingNote = "";
        if (currentBufferLen < song.noteSequence.Length)
        {
            nextGuidingNote = song.noteSequence[currentBufferLen];
        }

        foreach (var kvp in tineGlowDots)
        {
            if (string.Equals(kvp.Key, nextGuidingNote, StringComparison.OrdinalIgnoreCase))
            {
                kvp.Value.color = new Color(1f, 0.85f, 0.2f, 1f); // Vibrant gold highlight for thumb guidance
                kvp.Value.transform.localScale = Vector3.one * 1.35f;
            }
            else
            {
                kvp.Value.color = new Color(0.2f, 0.7f, 1f, 0.85f);
                kvp.Value.transform.localScale = Vector3.one;
            }
        }
    }

    public void SetVisible(bool visible)
    {
        if (instrumentCanvasGroup != null)
        {
            instrumentCanvasGroup.alpha = visible ? 1f : 0f;
            instrumentCanvasGroup.interactable = visible;
            instrumentCanvasGroup.blocksRaycasts = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
}
