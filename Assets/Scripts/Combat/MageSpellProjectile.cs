using UnityEngine;

/// <summary>
/// MageSpellProjectile handles ballistic flight, collision detection, and detonation
/// for spells cast in Mage loadout mode.
/// </summary>
public class MageSpellProjectile : MonoBehaviour
{
    [Header("Projectile Physics")]
    [SerializeField] private float speed = 22f;
    [SerializeField] private float explosionRadius = 3.5f;
    [SerializeField] private float damage = 95f;
    [SerializeField] private float maxLifetime = 3.0f;
    [SerializeField] private LayerMask hitLayers = ~0;

    private Vector3 moveDirection = Vector3.forward;
    private float lifeTimer = 0f;
    private bool hasDetonated = false;

    public void Initialize(Vector3 direction, float spellDamage, float radius = 3.5f)
    {
        moveDirection = direction.normalized;
        damage = spellDamage;
        explosionRadius = radius;
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
    }

    private void Update()
    {
        if (hasDetonated) return;

        lifeTimer += Time.deltaTime;
        float moveDistance = speed * Time.deltaTime;

        // Forward Raycast to prevent tunneling through enemies at high speeds
        if (Physics.Raycast(transform.position, moveDirection, out RaycastHit hit, moveDistance, hitLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                transform.position = hit.point;
                Detonate();
                return;
            }
        }

        transform.position += moveDirection * moveDistance;

        if (lifeTimer >= maxLifetime)
        {
            Detonate();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasDetonated || other.CompareTag("Player")) return;
        Detonate();
    }

    private void Detonate()
    {
        if (hasDetonated) return;
        hasDetonated = true;

        // Radial Explosion Damage & Stagger Check
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, hitLayers, QueryTriggerInteraction.Ignore);
        int struckCount = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col.CompareTag("Player")) continue;

            if (col.TryGetComponent<SpireEnemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
                enemy.Stagger(0.85f, transform.position);
                struckCount++;
            }
            else if (col.TryGetComponent<BossController>(out var boss))
            {
                boss.TakeDamage(damage);
                boss.Stagger(0.5f, transform.position);
                struckCount++;
            }
            else if (col.TryGetComponent<CaveMiningNode>(out var node))
            {
                node.ExecuteChiselStrike();
                struckCount++;
            }
        }

        // SFX
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 0.75f);
        }

        // Visual Explosion Shockwave Burst
        SpawnExplosionVFX(transform.position);

        Debug.Log($"[MAGE SPELL DETONATION] Astral blast struck {struckCount} targets for {damage} damage at {transform.position}.");
        Destroy(gameObject);
    }

    private void SpawnExplosionVFX(Vector3 pos)
    {
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.name = "SpellExplosionBurst";
        burst.transform.position = pos;
        burst.transform.localScale = Vector3.one * 0.4f;

        var col = burst.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var rend = burst.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.1f, 0.85f, 1.0f, 0.9f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 1f) * 4.5f);
            rend.sharedMaterial = mat;
        }

        GameObject lightObj = new GameObject("BurstLight", typeof(Light));
        lightObj.transform.SetParent(burst.transform, false);
        Light l = lightObj.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 0.9f, 1f);
        l.range = 7f;
        l.intensity = 4f;

        burst.AddComponent<TemporaryBurstFader>();
    }
}

public class TemporaryBurstFader : MonoBehaviour
{
    private float timer = 0f;
    private void Update()
    {
        timer += Time.deltaTime;
        transform.localScale += Vector3.one * (8f * Time.deltaTime);
        if (timer >= 0.25f)
        {
            Destroy(gameObject);
        }
    }
}
