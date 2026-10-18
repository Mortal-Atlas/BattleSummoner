using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.Environment
{
    /// <summary>
    /// Stylized Rock & Boulder Generator (Free Pack Rocks Stylized architecture).
    /// Generates faceted low-poly stylized boulders, mossy riverbank rocks, and cliff stones
    /// with anime cel ramp shading and minimal vertex overhead for mobile environments.
    /// </summary>
    public static class StylizedRockGenerator
    {
        public const string ModelsFolder = "Assets/Models/StylizedRocks";
        public const string MaterialsFolder = "Assets/Materials/StylizedRocks";
        public const string PrefabsFolder = "Assets/Prefabs/StylizedRocks";

#if UNITY_EDITOR
        [MenuItem("BattleSummoner/Nature/Generate Stylized Rocks")]
        public static void GenerateRockAssets()
        {
            EnsureDirectories();
            var mat = CreateRockMaterial();
            var meshes = GenerateMeshes();
            BuildPrefabs(meshes, mat);
            Debug.Log("🪨 [STYLIZED ROCKS] Successfully generated 3 faceted stylized rock variants and prefabs!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets", "Models");
            if (!AssetDatabase.IsValidFolder(ModelsFolder)) AssetDatabase.CreateFolder("Assets/Models", "StylizedRocks");

            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/Materials", "StylizedRocks");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabsFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "StylizedRocks");
        }

        public static Material CreateRockMaterial()
        {
            string path = $"{MaterialsFolder}/M_StylizedRock_MossySlate.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(0.48f, 0.52f, 0.55f, 1f));
                mat.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        public static Dictionary<string, Mesh> GenerateMeshes()
        {
            var dict = new Dictionary<string, Mesh>();
            dict["Rock_BoulderLarge"] = SaveMesh(BuildFacetedRockMesh(2.5f, 1.8f, 1.6f, 0), "Rock_BoulderLarge.asset");
            dict["Rock_RiverStoneMedium"] = SaveMesh(BuildFacetedRockMesh(1.4f, 0.9f, 1.1f, 1), "Rock_RiverStoneMedium.asset");
            dict["Rock_CliffSpire"] = SaveMesh(BuildFacetedRockMesh(1.2f, 3.2f, 1.0f, 2), "Rock_CliffSpire.asset");
            AssetDatabase.SaveAssets();
            return dict;
        }

        private static Mesh SaveMesh(Mesh mesh, string filename)
        {
            string path = $"{ModelsFolder}/{filename}";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                AssetDatabase.SaveAssets();
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh BuildFacetedRockMesh(float sizeX, float sizeY, float sizeZ, int seed)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            // Generate an icosphere and randomize vertex offsets for faceted boulder shape
            int latSegs = 4;
            int lonSegs = 6;
            var points = new List<Vector3>();

            for (int lat = 0; lat <= latSegs; lat++)
            {
                float theta = lat * Mathf.PI / latSegs;
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    float phi = lon * 2f * Mathf.PI / lonSegs;
                    Vector3 dir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));

                    // Random facet perturbation
                    float noise = Mathf.Sin(dir.x * 6f + seed) * Mathf.Cos(dir.z * 5f + seed) * 0.25f;
                    Vector3 pos = new Vector3(dir.x * sizeX, dir.y * sizeY, dir.z * sizeZ) * (1f + noise);
                    if (pos.y < 0) pos.y *= 0.5f; // Flatten base for sitting cleanly on ground
                    points.Add(pos);
                }
            }

            // Build flat-shaded faceted triangles (independent vertex per face)
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int nextLon = (lon + 1) % lonSegs;
                    int i0 = lat * lonSegs + lon;
                    int i1 = (lat + 1) * lonSegs + lon;
                    int i2 = lat * lonSegs + nextLon;
                    int i3 = (lat + 1) * lonSegs + nextLon;

                    if (i0 < points.Count && i1 < points.Count && i2 < points.Count)
                    {
                        AddFlatTriangle(points[i0], points[i1], points[i2], vertices, normals, triangles);
                    }
                    if (i2 < points.Count && i1 < points.Count && i3 < points.Count)
                    {
                        AddFlatTriangle(points[i2], points[i1], points[i3], vertices, normals, triangles);
                    }
                }
            }

            var mesh = new Mesh();
            mesh.name = "StylizedRock";
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFlatTriangle(Vector3 v0, Vector3 v1, Vector3 v2, List<Vector3> verts, List<Vector3> norms, List<int> tris)
        {
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            int baseIdx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2);
            norms.Add(normal); norms.Add(normal); norms.Add(normal);
            tris.Add(baseIdx); tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
        }

        public static void BuildPrefabs(Dictionary<string, Mesh> meshes, Material mat)
        {
            foreach (var kvp in meshes)
            {
                var go = new GameObject(kvp.Key);
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = kvp.Value;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                var col = go.AddComponent<MeshCollider>();
                col.sharedMesh = kvp.Value;

                string path = $"{PrefabsFolder}/{kvp.Key}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, path);
                GameObject.DestroyImmediate(go);
            }
        }
#endif
    }
}
