using UnityEngine;

/// <summary>
/// WarriorSwordSlashVFX creates a visual crescent slash arc in 3D world space
/// that sweeps and propagates outward along the exact 2D swipe angle of the player's gesture.
/// </summary>
public class WarriorSwordSlashVFX : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.28f;
    [SerializeField] private float expansionRate = 6.5f;
    [SerializeField] private float propagationSpeed = 16f;

    private Vector3 moveDirection = Vector3.forward;
    private float timer = 0f;
    private Material slashMaterial;
    private Color initialColor;

    public void Initialize(Vector3 forwardDir, float swipeAngleDeg, float speed)
    {
        moveDirection = forwardDir.normalized;
        propagationSpeed = speed;

        // Orient along travel direction, then roll around the travel axis by the swipe angle
        Quaternion lookRot = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = lookRot * Quaternion.Euler(0f, 0f, -swipeAngleDeg + 90f);

        var rend = GetComponent<Renderer>();
        if (rend != null)
        {
            slashMaterial = rend.material;
            initialColor = slashMaterial.color;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        // Propagate outward in the slash world direction
        transform.position += moveDirection * (propagationSpeed * Time.deltaTime);

        // Expand slash arc outwards
        transform.localScale += new Vector3(expansionRate * 1.5f, expansionRate * 0.4f, expansionRate) * Time.deltaTime;

        // Fade out
        if (slashMaterial != null)
        {
            Color c = initialColor;
            c.a = Mathf.Lerp(initialColor.a, 0f, progress * progress);
            slashMaterial.color = c;
            if (slashMaterial.HasProperty("_EmissionColor"))
            {
                Color em = slashMaterial.GetColor("_EmissionColor");
                slashMaterial.SetColor("_EmissionColor", em * (1f - progress));
            }
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public static GameObject SpawnSlashWave(Vector3 spawnOrigin, Vector3 slashDir, float swipeAngleDeg, float velocity)
    {
        GameObject arc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        arc.name = "WarriorDirectionalSlashArc";
        arc.transform.position = spawnOrigin;
        arc.transform.localScale = new Vector3(2.4f, 1.4f, 1f);

        var col = arc.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var rend = arc.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.9f, 0.95f, 1f, 0.85f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.3f, 0.85f, 1f) * 4.5f);
            rend.sharedMaterial = mat;
        }

        float speed = Mathf.Clamp(velocity * 0.012f, 10f, 26f);
        WarriorSwordSlashVFX vfx = arc.AddComponent<WarriorSwordSlashVFX>();
        vfx.Initialize(slashDir, swipeAngleDeg, speed);

        return arc;
    }

    public static void SpawnHitSpark(Vector3 point, Vector3 normal)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.name = "SwordHitSpark";
        spark.transform.position = point;
        spark.transform.localScale = Vector3.one * 0.22f;

        var col = spark.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var rend = spark.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.85f, 0.3f, 1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f) * 5f);
            rend.sharedMaterial = mat;
        }

        GameObject lightObj = new GameObject("SparkLight", typeof(Light));
        lightObj.transform.SetParent(spark.transform, false);
        Light l = lightObj.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.85f, 0.3f);
        l.range = 3.5f;
        l.intensity = 3.5f;

        spark.AddComponent<TemporaryBurstFader>();
    }
}
