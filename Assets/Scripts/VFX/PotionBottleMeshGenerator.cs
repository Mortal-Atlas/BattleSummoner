using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.VFX
{
    public enum PotionBottleType
    {
        VitalityHP,     // Ruby Red
        AstralMP,       // Sapphire Blue
        SwiftnessHaste, // Emerald Green
        CausticAcid,    // Acid Lime
        MoltenFire,     // Fiery Orange
        CelestialElixir // Golden Radiant
    }

    /// <summary>
    /// Generates stylized potion bottles, glass flasks, and liquid materials
    /// (VFX Particles Bottles & Spells architecture).
    /// </summary>
    public static class PotionBottleMeshGenerator
    {
        public const string ModelsFolder = "Assets/Models/VFX";
        public const string MaterialsFolder = "Assets/Materials/VFX";
        public const string PrefabsFolder = "Assets/Prefabs/VFX";

#if UNITY_EDITOR
        [MenuItem("BattleSummoner/VFX/Generate Potion Bottle Assets")]
        public static void GenerateAllBottleAssets()
        {
            EnsureDirectories();
            var mesh = GenerateFlaskMesh();
            var corkMesh = GenerateCorkMesh();
            var glassMat = CreateGlassMaterial();
            var liquidMats = CreateLiquidMaterials();
            BuildPrefabs(mesh, corkMesh, glassMat, liquidMats);
            Debug.Log("🧪 [BOTTLE VFX] Successfully generated Stylized Potion Flasks, Glass shaders, and Liquid Prefabs!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets", "Models");
            if (!AssetDatabase.IsValidFolder(ModelsFolder)) AssetDatabase.CreateFolder("Assets/Models", "VFX");

            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/Materials", "VFX");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabsFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "VFX");
        }

        public static Mesh GenerateFlaskMesh()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            int radialSegs = 14;
            int heightSteps = 12;

            // Profile: Sphere bulb base -> narrow neck -> flared rim
            for (int h = 0; h <= heightSteps; h++)
            {
                float t = (float)h / heightSteps;
                float y = t * 0.45f;
                float rad;

                if (t < 0.65f)
                {
                    // Spherical bulb
                    float sphereT = (t / 0.65f) * Mathf.PI;
                    rad = Mathf.Sin(sphereT) * 0.16f + 0.05f;
                }
                else if (t < 0.90f)
                {
                    // Neck
                    rad = 0.065f;
                }
                else
                {
                    // Flared rim
                    rad = Mathf.Lerp(0.065f, 0.09f, (t - 0.90f) / 0.10f);
                }

                for (int r = 0; r <= radialSegs; r++)
                {
                    float angle = r * 2f * Mathf.PI / radialSegs;
                    float x = Mathf.Cos(angle) * rad;
                    float z = Mathf.Sin(angle) * rad;

                    vertices.Add(new Vector3(x, y, z));
                    normals.Add(new Vector3(x, 0.1f, z).normalized);
                    uvs.Add(new Vector2((float)r / radialSegs, t));
                }
            }

            for (int h = 0; h < heightSteps; h++)
            {
                for (int r = 0; r < radialSegs; r++)
                {
                    int cur = h * (radialSegs + 1) + r;
                    int next = cur + radialSegs + 1;

                    triangles.Add(cur); triangles.Add(next); triangles.Add(cur + 1);
                    triangles.Add(cur + 1); triangles.Add(next); triangles.Add(next + 1);
                }
            }

            var mesh = new Mesh { name = "PotionFlask_Stylized" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            string path = $"{ModelsFolder}/PotionFlask_Stylized.asset";
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

        public static Mesh GenerateCorkMesh()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            int radialSegs = 10;
            float topY = 0.49f;
            float botY = 0.44f;
            float topRad = 0.075f;
            float botRad = 0.058f;

            for (int r = 0; r <= radialSegs; r++)
            {
                float ang = r * 2f * Mathf.PI / radialSegs;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);

                vertices.Add(new Vector3(cos * botRad, botY, sin * botRad));
                normals.Add(new Vector3(cos, 0f, sin));
                vertices.Add(new Vector3(cos * topRad, topY, sin * topRad));
                normals.Add(new Vector3(cos, 0f, sin));
            }

            for (int r = 0; r < radialSegs; r++)
            {
                int b = r * 2;
                triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 2);
                triangles.Add(b + 1); triangles.Add(b + 3); triangles.Add(b + 2);
            }

            var mesh = new Mesh { name = "PotionCork_Stylized" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            string path = $"{ModelsFolder}/PotionCork_Stylized.asset";
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

        public static Material CreateGlassMaterial()
        {
            string path = $"{MaterialsFolder}/M_PotionGlass_Transparent.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha
                mat.SetColor("_BaseColor", new Color(0.85f, 0.95f, 1.0f, 0.45f));
                mat.SetFloat("_Smoothness", 0.92f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        public static Dictionary<PotionBottleType, Material> CreateLiquidMaterials()
        {
            var dict = new Dictionary<PotionBottleType, Material>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");

            void AddLiquid(PotionBottleType type, Color col, Color glow, string name)
            {
                string path = $"{MaterialsFolder}/M_Liquid_{name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader);
                    mat.SetColor("_BaseColor", col);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", glow * 2.5f);
                    mat.SetFloat("_Smoothness", 0.85f);
                    AssetDatabase.CreateAsset(mat, path);
                }
                dict[type] = mat;
            }

            AddLiquid(PotionBottleType.VitalityHP, new Color(0.92f, 0.15f, 0.25f, 1f), new Color(0.95f, 0.1f, 0.2f), "VitalityHP");
            AddLiquid(PotionBottleType.AstralMP, new Color(0.18f, 0.45f, 1.0f, 1f), new Color(0.15f, 0.5f, 1.0f), "AstralMP");
            AddLiquid(PotionBottleType.SwiftnessHaste, new Color(0.25f, 0.92f, 0.45f, 1f), new Color(0.2f, 0.88f, 0.35f), "SwiftnessHaste");
            AddLiquid(PotionBottleType.CausticAcid, new Color(0.65f, 1.0f, 0.15f, 1f), new Color(0.6f, 0.95f, 0.1f), "CausticAcid");
            AddLiquid(PotionBottleType.MoltenFire, new Color(1.0f, 0.45f, 0.08f, 1f), new Color(1.0f, 0.38f, 0.05f), "MoltenFire");
            AddLiquid(PotionBottleType.CelestialElixir, new Color(1.0f, 0.90f, 0.35f, 1f), new Color(1.0f, 0.85f, 0.25f), "CelestialElixir");

            AssetDatabase.SaveAssets();
            return dict;
        }

        public static void BuildPrefabs(Mesh flaskMesh, Mesh corkMesh, Material glassMat, Dictionary<PotionBottleType, Material> liquidMats)
        {
            var corkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            corkMat.SetColor("_BaseColor", new Color(0.55f, 0.38f, 0.22f, 1f));

            foreach (var kvp in liquidMats)
            {
                var root = new GameObject($"PFB_PotionFlask_{kvp.Key}");

                // Glass Flask outer shell
                var flask = new GameObject("GlassFlask");
                flask.transform.SetParent(root.transform, false);
                var fMf = flask.AddComponent<MeshFilter>();
                fMf.sharedMesh = flaskMesh;
                var fMr = flask.AddComponent<MeshRenderer>();
                fMr.sharedMaterial = glassMat;

                // Inner liquid volume
                var liquid = new GameObject("InnerLiquid");
                liquid.transform.SetParent(root.transform, false);
                liquid.transform.localScale = Vector3.one * 0.93f;
                var lMf = liquid.AddComponent<MeshFilter>();
                lMf.sharedMesh = flaskMesh;
                var lMr = liquid.AddComponent<MeshRenderer>();
                lMr.sharedMaterial = kvp.Value;

                // Cork stopper
                var cork = new GameObject("Cork");
                cork.transform.SetParent(root.transform, false);
                var cMf = cork.AddComponent<MeshFilter>();
                cMf.sharedMesh = corkMesh;
                var cMr = cork.AddComponent<MeshRenderer>();
                cMr.sharedMaterial = corkMat;

                // Add collider & rigid dynamic support
                var col = root.AddComponent<SphereCollider>();
                col.radius = 0.18f;
                col.center = new Vector3(0f, 0.18f, 0f);

                string path = $"{PrefabsFolder}/PFB_PotionFlask_{kvp.Key}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                GameObject.DestroyImmediate(root);
            }
        }
#endif
    }
}
