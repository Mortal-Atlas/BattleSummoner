using System;
using UnityEngine;

/// <summary>
/// RangerArrowProjectile handles ballistic trajectory, hit detection, and damage
/// for arrows fired by the Ranger class (and any bow weapon).
/// </summary>
public class RangerArrowProjectile : MonoBehaviour
{
    [Header("Flight Dynamics")]
    [SerializeField] private float speed = 28f;
    [SerializeField] private float damage = 85f;
    [SerializeField] private float maxLifetime = 3.5f;
    [SerializeField] private float gravityFactor = 2.0f;
    [SerializeField] private LayerMask hitLayers = ~0;

    private Vector3 velocity = Vector3.forward;
    private float lifeTimer = 0f;
    private bool hasImpacted = false;
    private bool isADSShot = false;

    public void Initialize(Vector3 direction, float arrowDamage, float arrowSpeed, bool isADS)
    {
        damage = arrowDamage;
        speed = arrowSpeed;
        isADSShot = isADS;

        velocity = direction.normalized * speed;
        transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);

        if (isADSShot)
        {
            damage *= 1.30f; // Critical precision multiplier in ADS
        }
    }

    private void Update()
    {
        if (hasImpacted) return;

        lifeTimer += Time.deltaTime;

        // Apply subtle ballistic drop unless high-velocity ADS laser shot
        if (!isADSShot)
        {
            velocity.y -= gravityFactor * Time.deltaTime;
        }

        Vector3 moveStep = velocity * Time.deltaTime;
        float stepDist = moveStep.magnitude;
        Vector3 moveDir = moveStep.normalized;

        if (Physics.Raycast(transform.position, moveDir, out RaycastHit hit, stepDist, hitLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                transform.position = hit.point;
                Impact(hit.collider, hit.point, hit.normal);
                return;
            }
        }

        transform.position += moveStep;
        if (velocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
        }

        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasImpacted || other.CompareTag("Player")) return;
        Impact(other, transform.position, -transform.forward);
    }

    private void Impact(Collider col, Vector3 point, Vector3 normal)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        bool struckTarget = false;

        if (col.TryGetComponent<SpireEnemy>(out var enemy))
        {
            enemy.TakeDamage(damage);
            enemy.Stagger(0.65f, transform.position);
            struckTarget = true;
        }
        else if (col.TryGetComponent<BossController>(out var boss))
        {
            boss.TakeDamage(damage);
            boss.Stagger(0.35f, transform.position);
            struckTarget = true;
        }
        else if (col.TryGetComponent<CaveMiningNode>(out var node))
        {
            node.ExecuteChiselStrike();
            struckTarget = true;
        }

        // Visual Spark VFX
        WarriorSwordSlashVFX.SpawnHitSpark(point, normal);

        // SFX
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("sword_impact", 0.85f);
        }

        Debug.Log($"🏹 [RANGER ARROW IMPACT] Arrow struck {col.name} (target: {struckTarget}) for {damage:F1} damage! (ADS: {isADSShot})");
        Destroy(gameObject);
    }
}
