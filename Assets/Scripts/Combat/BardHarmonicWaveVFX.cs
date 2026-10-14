using UnityEngine;

/// <summary>
/// BardHarmonicWaveVFX creates an expanding acoustic soundwave ring that radiates
/// outward from the player when playing notes or strumming melodic chords.
/// Any enemies caught within harmonic distance take resonance damage and are staggered.
/// </summary>
public class BardHarmonicWaveVFX : MonoBehaviour
{
    [Header("Wave Expansion")]
    [SerializeField] private float expansionSpeed = 12f;
    [SerializeField] private float maxRadius = 6.5f;
    [SerializeField] private float damage = 35f;
    [SerializeField] private float lifetime = 0.65f;

    private float currentRadius = 0.5f;
    private float timer = 0f;
    private readonly System.Collections.Generic.HashSet<Collider> alreadyHit = new System.Collections.Generic.HashSet<Collider>();

    public void Initialize(float waveDamage, float maxDist = 6.5f)
    {
        damage = waveDamage;
        maxRadius = maxDist;
    }

    private void Start()
    {
        transform.localScale = new Vector3(currentRadius, 0.08f, currentRadius);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        currentRadius += expansionSpeed * Time.deltaTime;
        transform.localScale = new Vector3(currentRadius, 0.08f, currentRadius);

        // Acoustic Overlap Sphere check
        float effectiveRadius = Mathf.Max(currentRadius, transform.localScale.x * 0.5f);
        Collider[] hits = Physics.OverlapSphere(transform.position, effectiveRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col.CompareTag("Player") || alreadyHit.Contains(col)) continue;

            if (col.TryGetComponent<SpireEnemy>(out var enemy))
            {
                alreadyHit.Add(col);
                enemy.TakeDamage(damage);
                enemy.Stagger(0.6f, transform.position);
                Debug.Log($"[BARD HARMONIC RESONANCE] Acoustic shockwave struck {col.name} for {damage} resonance damage!");
            }
            else if (col.TryGetComponent<BossController>(out var boss))
            {
                alreadyHit.Add(col);
                boss.TakeDamage(damage);
                boss.Stagger(0.3f, transform.position);
                Debug.Log($"[BARD HARMONIC RESONANCE] Acoustic shockwave struck Boss {col.name} for {damage} resonance damage!");
            }
        }

        if (timer >= lifetime || currentRadius >= maxRadius)
        {
            Destroy(gameObject);
        }
    }
}
