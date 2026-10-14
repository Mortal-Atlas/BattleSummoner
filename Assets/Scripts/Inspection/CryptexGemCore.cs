using UnityEngine;

/// <summary>
/// CryptexGemCore represents the sacred Astral Gem revealed inside the Spire Cryptex.
/// When tapped or revealed, it plays ethereal ASMR crystal bell resonances and pulses with radiant bloom.
/// </summary>
public class CryptexGemCore : MonoBehaviour, IInspectablePuzzlePiece
{
    [Header("Visual Effects")]
    [SerializeField] private Renderer gemRenderer;
    [SerializeField] private Light corePointLight;
    [SerializeField] private float spinSpeed = 25f;

    private Material gemMaterial;
    private Color baseEmissionColor;
    private float pulseTimer = 0f;
    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
        if (gemRenderer == null) gemRenderer = GetComponent<Renderer>();
        if (gemRenderer != null)
        {
            gemMaterial = gemRenderer.material;
            if (gemMaterial.HasProperty("_EmissionColor"))
            {
                baseEmissionColor = gemMaterial.GetColor("_EmissionColor");
            }
        }

        if (corePointLight == null) corePointLight = GetComponentInChildren<Light>();
    }

    private void Update()
    {
        // Gentle celestial float / spin
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

        // Subtle breathing emission
        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.deltaTime * 2.5f;
            float t = Mathf.Clamp01(pulseTimer);
            if (gemMaterial != null)
            {
                gemMaterial.SetColor("_EmissionColor", baseEmissionColor * (1f + t * 4f));
            }
            if (corePointLight != null)
            {
                corePointLight.intensity = 1.2f + t * 3.5f;
            }
            transform.localScale = initialScale * (1f + t * 0.15f);
        }
    }

    public void OnPuzzleInteracted()
    {
        OnPuzzleTapped();
    }

    public void OnPuzzleTapped()
    {
        pulseTimer = 1.0f;
        RelicASMRSynthesizer.Instance?.PlayCrystalChime(1.0f, UnityEngine.Random.Range(0.98f, 1.06f));
        Debug.Log("[ASTRAL GEM RESONANCE] Pure crystal harmonics resonate through the chamber.");
    }

    public void OnPuzzleSlide(Vector2 screenDelta)
    {
        // Gem can be tapped; gentle tilt on slide
        transform.Rotate(Vector3.forward, screenDelta.x * 0.5f, Space.Self);
    }

    public void OnPuzzleSlideEnd()
    {
    }
}
