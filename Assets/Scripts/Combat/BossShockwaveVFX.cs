using UnityEngine;

public class BossShockwaveVFX : MonoBehaviour
{
    [Header("Shockwave Expansion")]
    [SerializeField] private float expansionSpeed = 14f;
    [SerializeField] private float maxRadius = 16f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float lifetime = 1.5f;

    private float currentRadius = 0.5f;
    private float timer = 0f;
    private bool hasHitPlayer = false;

    private void Start()
    {
        transform.localScale = new Vector3(currentRadius, 0.1f, currentRadius);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        currentRadius += expansionSpeed * Time.deltaTime;
        transform.localScale = new Vector3(currentRadius, 0.1f, currentRadius);

        if (!hasHitPlayer)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius * 0.5f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Player") && hits[i].TryGetComponent<PlayerProgression>(out var player))
                {
                    player.TakeDirectDamage(damage);
                    hasHitPlayer = true;
                    Debug.Log($"[BOSS SHOCKWAVE] Player struck by kinetic shockwave! ({damage} damage)");
                    break;
                }
            }
        }

        if (timer >= lifetime || currentRadius >= maxRadius)
        {
            Destroy(gameObject);
        }
    }
}
