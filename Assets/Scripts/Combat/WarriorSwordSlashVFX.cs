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

    private static Mesh cachedCrescentMesh = null;

    public void Initialize(Vector3 forwardDir, float swipeAngleDeg, float speed)
    {
        moveDirection = forwardDir.normalized;
        if (moveDirection.sqrMagnitude < 0.001f) moveDirection = Vector3.forward;
        propagationSpeed = speed;

        // Orient along travel direction (local +Z), then roll along travel axis by swipe angle
        Vector3 upRef = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(moveDirection, Vector3.up)) > 0.95f) upRef = Vector3.forward;
        Quaternion lookRot = Quaternion.LookRotation(moveDirection, upRef);
        transform.rotation = lookRot * Quaternion.Euler(0f, 0f, -swipeAngleDeg);

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

        // Expand slash arc outwards along width and forward
        transform.localScale += new Vector3(expansionRate * 1.2f, expansionRate * 0.1f, expansionRate * 0.9f) * Time.deltaTime;

        // Fade out smoothly
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

    public static GameObject SpawnSlashWave(Vector3 spawnOrigin, Vector3 slashDir, float swipeAngleDeg, float velocity, Color? waveColor = null)
    {
        GameObject arc = new GameObject("WarriorZeldaCrescentSlashArc");
        arc.transform.position = spawnOrigin;
        arc.transform.localScale = Vector3.one;

        // Add MeshFilter and MeshRenderer with Procedural Zelda Crescent Arch
        var mf = arc.AddComponent<MeshFilter>();
        if (cachedCrescentMesh == null)
        {
            cachedCrescentMesh = CreateCrescentArchMesh();
        }
        mf.sharedMesh = cachedCrescentMesh;

        var rend = arc.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");

        Color emission = waveColor ?? new Color(0.25f, 0.85f, 1f);
        Material mat = new Material(shader);
        mat.name = "ZeldaSlashWave_Mat";
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", emission);
        mat.color = emission;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission * 4.8f);
        }
        rend.sharedMaterial = mat;

        // Subtle blade wave light
        GameObject lightObj = new GameObject("SlashLight", typeof(Light));
        lightObj.transform.SetParent(arc.transform, false);
        Light l = lightObj.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = emission;
        l.range = 3.8f;
        l.intensity = 2.4f;

        float speed = Mathf.Clamp(velocity * 0.014f, 12f, 28f);
        WarriorSwordSlashVFX vfx = arc.AddComponent<WarriorSwordSlashVFX>();
        vfx.Initialize(slashDir, swipeAngleDeg, speed);

        return arc;
    }

    /// <summary>
    /// Generates a tapered, curved Legend of Zelda Master Sword beam crescent arch.
    /// The convex curved edge leads forward in +Z, with needle-sharp tapered tips at both ends.
    /// </summary>
    public static Mesh CreateCrescentArchMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ZeldaCrescentArchMesh";

        int segments = 36;
        float arcSpanDeg = 115f;
        float radius = 2.1f;
        float maxThickness = 0.52f;

        int vertCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Color[] colors = new Color[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];

        float halfSpanRad = (arcSpanDeg * 0.5f) * Mathf.Deg2Rad;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float phi = Mathf.Lerp(-halfSpanRad, halfSpanRad, t);

            // Curve spine: center at (0, 0, 0), wings sweep back in -Z as phi moves away from 0
            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);

            Vector3 spinePt = new Vector3(radius * sinPhi, 0f, radius * (cosPhi - 1.0f));

            // Outward normal in the XZ plane pointing in the convex direction
            Vector3 curveNormal = new Vector3(sinPhi, 0f, cosPhi).normalized;

            // Taper thickness: 0 at tips (t=0, t=1), max at center (t=0.5)
            float taper = Mathf.Sin(t * Mathf.PI);
            float width = maxThickness * taper;

            // Outer leading edge (convex forward)
            Vector3 outerPt = spinePt + curveNormal * (width * 0.65f);
            // Inner trailing edge (concave backward)
            Vector3 innerPt = spinePt - curveNormal * (width * 0.35f);

            int outerIdx = i * 2;
            int innerIdx = i * 2 + 1;

            vertices[outerIdx] = outerPt;
            vertices[innerIdx] = innerPt;

            normals[outerIdx] = Vector3.up;
            normals[innerIdx] = Vector3.up;

            uvs[outerIdx] = new Vector2(t, 1f);
            uvs[innerIdx] = new Vector2(t, 0f);

            // Outer edge: intense white-hot emissive core with tapered tip alpha
            float tipFade = Mathf.Clamp01(taper * 2.5f);
            colors[outerIdx] = new Color(1f, 1f, 1f, 1f * tipFade);
            // Inner edge: soft electric cyan fading out
            colors[innerIdx] = new Color(0.2f, 0.85f, 1f, 0.08f * tipFade);
        }

        // Build double-sided triangles so the crescent is visible from any angle
        int quadCount = segments;
        int triCount = quadCount * 12; // 2 triangles front + 2 triangles back per quad
        int[] triangles = new int[triCount];

        int tIdx = 0;
        for (int i = 0; i < segments; i++)
        {
            int o0 = i * 2;
            int i0 = i * 2 + 1;
            int o1 = (i + 1) * 2;
            int i1 = (i + 1) * 2 + 1;

            // Front face (winding clockwise seen from above)
            triangles[tIdx++] = o0;
            triangles[tIdx++] = o1;
            triangles[tIdx++] = i0;

            triangles[tIdx++] = o1;
            triangles[tIdx++] = i1;
            triangles[tIdx++] = i0;

            // Back face (winding counter-clockwise seen from below)
            triangles[tIdx++] = o0;
            triangles[tIdx++] = i0;
            triangles[tIdx++] = o1;

            triangles[tIdx++] = o1;
            triangles[tIdx++] = i0;
            triangles[tIdx++] = i1;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    public static void SpawnHitSpark(Vector3 point, Vector3 normal)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.name = "SwordHitSpark";
        spark.transform.position = point;
        spark.transform.localScale = Vector3.one * 0.22f;

        var col = spark.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying) Object.Destroy(col);
            else Object.DestroyImmediate(col);
        }

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
