using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Intro
{
    /// <summary>
    /// SAOHexagonPillarTunnel generates and animates a dense honeycomb lattice of
    /// tessellating hexagonal light pillars that shoot backwards past the camera at hyperspeed,
    /// reproducing the iconic Sword Art Online "Link Start" dive aesthetic.
    /// </summary>
    public class SAOHexagonPillarTunnel : MonoBehaviour
    {
        [Header("Honeycomb Lattice Parameters")]
        [Tooltip("Circumradius of each regular hexagonal pillar.")]
        public float hexRadius = 0.55f;

        [Tooltip("Minimum distance from center axis (leaves clear flight path for camera).")]
        public float innerClearanceRadius = 1.35f;

        [Tooltip("Maximum distance from center axis for outer perimeter pillars.")]
        public float outerRadius = 7.2f;

        [Tooltip("Total length of the dive tunnel loop.")]
        public float tunnelLength = 85.0f;

        [Header("Pillar Dimensions & Variation")]
        public float minPillarLength = 8.0f;
        public float maxPillarLength = 26.0f;
        public int gridSpan = 6; // Axial coordinate span [-gridSpan, gridSpan]

        [Header("Material & Visuals")]
        public Material pillarMaterial;

        [Header("Motion & Velocity")]
        public float currentSpeed = 55.0f;

        private struct PillarData
        {
            public Transform transform;
            public float baseZ;
            public float length;
            public Vector2 latticeXY;
        }

        private List<PillarData> activePillars = new List<PillarData>();
        private Mesh cachedHexMesh;
        private static readonly int SpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int FlashId = Shader.PropertyToID("_FlashIntensity");

        private void Awake()
        {
            EnsureHexMesh();
            BuildHoneycombTunnel();
        }

        /// <summary>
        /// Generates an elongated 6-sided hexagonal prism mesh with outward normals and circumference UVs.
        /// </summary>
        public Mesh EnsureHexMesh()
        {
            if (cachedHexMesh != null) return cachedHexMesh;

            cachedHexMesh = new Mesh();
            cachedHexMesh.name = "Proc_Hexagonal_Prism_1x1";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            float r = 1.0f; // Unit radius in XY
            float halfLen = 0.5f; // Unit length in Z (-0.5 to +0.5)

            // Compute 6 corner points in XY (pointy-topped hexagon)
            Vector2[] hexCorners = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
                hexCorners[i] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
            }

            // 1. Build 6 Longitudinal Side Faces
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Vector2 c0 = hexCorners[i];
                Vector2 c1 = hexCorners[next];

                // Face normal
                float midAngle = (i * 60f + 60f) * Mathf.Deg2Rad;
                Vector3 n = new Vector3(Mathf.Cos(midAngle), Mathf.Sin(midAngle), 0f);

                int vIdx = vertices.Count;

                // 4 vertices per quad (Back-Bottom, Back-Top, Front-Top, Front-Bottom)
                vertices.Add(new Vector3(c0.x, c0.y, -halfLen)); // 0
                vertices.Add(new Vector3(c1.x, c1.y, -halfLen)); // 1
                vertices.Add(new Vector3(c1.x, c1.y, +halfLen)); // 2
                vertices.Add(new Vector3(c0.x, c0.y, +halfLen)); // 3

                for (int k = 0; k < 4; k++) normals.Add(n);

                float u0 = (float)i / 6.0f;
                float u1 = (float)(i + 1) / 6.0f;
                uvs.Add(new Vector2(u0, 0f));
                uvs.Add(new Vector2(u1, 0f));
                uvs.Add(new Vector2(u1, 1f));
                uvs.Add(new Vector2(u0, 1f));

                // Two triangles
                triangles.Add(vIdx + 0);
                triangles.Add(vIdx + 2);
                triangles.Add(vIdx + 1);

                triangles.Add(vIdx + 0);
                triangles.Add(vIdx + 3);
                triangles.Add(vIdx + 2);
            }

            // 2. Build Front Hexagonal Cap (facing camera at z = -halfLen)
            int centerIdx = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, -halfLen));
            normals.Add(new Vector3(0f, 0f, -1f));
            uvs.Add(new Vector2(0.5f, 0.0f));

            int cornerStartIdx = vertices.Count;
            for (int i = 0; i < 6; i++)
            {
                vertices.Add(new Vector3(hexCorners[i].x, hexCorners[i].y, -halfLen));
                normals.Add(new Vector3(0f, 0f, -1f));
                uvs.Add(new Vector2(0.5f, 0.0f));
            }

            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                triangles.Add(centerIdx);
                triangles.Add(cornerStartIdx + next);
                triangles.Add(cornerStartIdx + i);
            }

            // Pure white vertex colors for luminous white hexagon interiors
            List<Color> vertexColors = new List<Color>();
            for (int k = 0; k < vertices.Count; k++)
            {
                vertexColors.Add(Color.white);
            }

            cachedHexMesh.SetVertices(vertices);
            cachedHexMesh.SetNormals(normals);
            cachedHexMesh.SetUVs(0, uvs);
            cachedHexMesh.SetColors(vertexColors);
            cachedHexMesh.SetTriangles(triangles, 0);
            cachedHexMesh.RecalculateBounds();

            return cachedHexMesh;
        }

        /// <summary>
        /// Builds the tessellating honeycomb lattice using axial coordinate math.
        /// </summary>
        public void BuildHoneycombTunnel()
        {
            // Clear any prior children
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("HexPillar_"))
                {
                    Destroy(child.gameObject);
                }
            }

            activePillars.Clear();

            if (pillarMaterial == null)
            {
                var s = Shader.Find("BattleSummoner/SAO_TessellatingHexagonPillars");
                if (s != null) pillarMaterial = new Material(s);
            }

            float sqrt3 = Mathf.Sqrt(3.0f);
            int pillarIndex = 0;

            // Generate axial coordinate grid (q, r)
            for (int q = -gridSpan; q <= gridSpan; q++)
            {
                for (int r = -gridSpan; r <= gridSpan; r++)
                {
                    // Pointy-topped hexagon lattice coordinates:
                    // x = R * sqrt(3) * (q + r/2)
                    // y = R * 1.5 * r
                    float posX = hexRadius * sqrt3 * (q + r * 0.5f);
                    float posY = hexRadius * 1.5f * r;
                    float dist = Mathf.Sqrt(posX * posX + posY * posY);

                    // Filter out inner corridor (for camera flight path) and outer bounds
                    if (dist < innerClearanceRadius || dist > outerRadius)
                        continue;

                    // Stagger initial Z depth along tunnel
                    float startZ = (pillarIndex * 1.73f) % tunnelLength;
                    float length = Mathf.Lerp(minPillarLength, maxPillarLength, Mathf.PingPong(q * 3.7f + r * 2.1f, 1f));

                    // Create Pillar GameObject
                    var pGo = new GameObject($"HexPillar_{pillarIndex}_{q}_{r}");
                    pGo.transform.SetParent(transform, false);
                    pGo.transform.localPosition = new Vector3(posX, posY, startZ);
                    // Scale: X, Y by hexRadius * 0.78 for crisp hexagonal silhouette, Z by length
                    pGo.transform.localScale = new Vector3(hexRadius * 0.78f, hexRadius * 0.78f, length);

                    var mf = pGo.AddComponent<MeshFilter>();
                    mf.sharedMesh = cachedHexMesh;

                    var mr = pGo.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = pillarMaterial;

                    // Pure white core with crisp silver-ice edges for iconic SAO Link Start
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_CoreColor", Color.white);
                    mpb.SetColor("_EdgeColor", new Color(0.85f, 0.95f, 1.00f, 1.0f));
                    mr.SetPropertyBlock(mpb);

                    activePillars.Add(new PillarData
                    {
                        transform = pGo.transform,
                        baseZ = startZ,
                        length = length,
                        latticeXY = new Vector2(posX, posY)
                    });

                    pillarIndex++;
                }
            }

            Debug.Log($"[SAO Hexagon Tunnel] Successfully generated {activePillars.Count} tessellating hexagonal light pillars!");
        }

        private void Update()
        {
            if (activePillars.Count == 0) return;

            float dt = Time.deltaTime;
            float movement = currentSpeed * dt;

            for (int i = 0; i < activePillars.Count; i++)
            {
                var p = activePillars[i];
                if (p.transform == null) continue;

                Vector3 pos = p.transform.localPosition;
                pos.z -= movement;

                // When pillar has completely shot behind the camera, wrap it to the far front
                if (pos.z + (p.length * 0.5f) < -12.0f)
                {
                    pos.z += tunnelLength;
                }

                p.transform.localPosition = pos;
            }
        }

        /// <summary>
        /// Updates the dive warp velocity of all tessellating hexagonal pillars.
        /// </summary>
        public void SetWarpSpeed(float speed)
        {
            currentSpeed = speed;
            if (pillarMaterial != null)
            {
                pillarMaterial.SetFloat(SpeedId, Mathf.Max(speed * 0.4f, 12f));
            }
        }

        /// <summary>
        /// Triggers blinding white dive arrival flash.
        /// </summary>
        public void SetFlashIntensity(float flash)
        {
            if (pillarMaterial != null)
            {
                pillarMaterial.SetFloat(FlashId, flash);
            }
        }
    }
}
