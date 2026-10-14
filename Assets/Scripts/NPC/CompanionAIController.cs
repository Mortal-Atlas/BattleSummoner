using UnityEngine;

public class CompanionAIController : MonoBehaviour
{
    [Header("Profile Link")]
    public BespokeNPCObject companionProfile;

    [Header("Locomotion & Formation")]
    [SerializeField] private float followDistance = 2.4f;
    [SerializeField] private float moveSpeed = 5.2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Combat Support")]
    [SerializeField] private float attackRange = 3.0f;
    [SerializeField] private float supportCooldown = 6.0f;

    private Transform playerTransform;
    private float lastSupportTime = 0f;

    private void Start()
    {
        PlayerProgression player = FindAnyObjectByType<PlayerProgression>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (companionProfile != null)
        {
            supportCooldown = companionProfile.companionSupportCooldown;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer > followDistance)
        {
            Vector3 targetPos = playerTransform.position - (playerTransform.forward * 1.5f) + (playerTransform.right * 1.2f);
            targetPos.y = transform.position.y;
            Vector3 moveDir = (targetPos - transform.position).normalized;

            transform.position += moveDir * (moveSpeed * Time.deltaTime);
            if (moveDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }

        // Support attack nearest enemy
        if (Time.time - lastSupportTime >= supportCooldown)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange + 4f);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].CompareTag("Enemy"))
                {
                    if (hits[i].TryGetComponent<SpireEnemy>(out var enemy))
                    {
                        enemy.TakeDamage(companionProfile != null ? companionProfile.combatAttackPower : 35f);
                        lastSupportTime = Time.time;
                        Debug.Log($"[COMPANION STRIKE] {name} assists player with kinetic support strike!");
                        break;
                    }
                    else if (hits[i].TryGetComponent<BossController>(out var boss))
                    {
                        boss.TakeDamage(companionProfile != null ? companionProfile.combatAttackPower : 35f);
                        lastSupportTime = Time.time;
                        Debug.Log($"[COMPANION STRIKE] {name} strikes boss armor!");
                        break;
                    }
                }
            }
        }
    }
}
