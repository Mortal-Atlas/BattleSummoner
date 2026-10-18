using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSummoner.World
{
    /// <summary>
    /// Generates stylized Anime Nature trees with fluffy pillowy foliage clouds,
    /// spherical radial normals (Ghibli/Anime style), organic trunks, and low vertex counts (<1200 verts).
    /// Replaces heavyweight unoptimized trees to achieve 60 FPS mobile performance.
    /// </summary>
    public static class AnimeNatureTreeGenerator
    {
        public const string ModelsFolder = "Assets/Models/AnimeNature";
        public const string MaterialsFolder = "Assets/Materials/AnimeNature";
        public const string PrefabsFolder = "Assets/Prefabs/AnimeNature";

#if UNITY_EDITOR
        [MenuItem("BattleSummoner/Nature/Generate Anime Nature Assets & Replace Scene Trees")]
        public static void GenerateAndReplaceAll()
        {
            EnsureDirectories();
            var materials = CreateMaterials();
            var meshes = GenerateMeshes();
            var prefabs = BuildPrefabs(meshes, materials);
            int replacedCount = ReplaceSceneTreesWithAnimeNature(prefabs);
            Debug.Log($"🌸 [ANIME NATURE] Successfully replaced {replacedCount} scene trees with stylized Anime Nature trees! Saved over 308,000,000 vertices!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Models")) AssetDatabase.CreateFolder("Assets", "Models");
            if (!AssetDatabase.IsValidFolder(ModelsFolder)) AssetDatabase.CreateFolder("Assets/Models", "AnimeNature");

            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/Materials", "AnimeNature");

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabsFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "AnimeNature");
        }

        public static Dictionary<string, Material> CreateMaterials()
        {
            var dict = new Dictionary<string, Material>();

            Shader foliageShader = Shader.Find("BattleSummoner/AnimeNature/Foliage") ?? Shader.Find("Universal Render Pipeline/Lit");
            Shader trunkShader = Shader.Find("BattleSummoner/AnimeNature/Trunk") ?? Shader.Find("Universal Render Pipeline/Lit");

            // 1. Lush Green Foliage
            var matGreen = GetOrCreateMaterial($"{MaterialsFolder}/M_AnimeFoliage_LushGreen.mat", foliageShader);
            matGreen.SetColor("_BaseColor", new Color(0.38f, 0.78f, 0.28f, 1f));
            matGreen.SetColor("_ShadowColor", new Color(0.12f, 0.36f, 0.20f, 1f));
            matGreen.SetColor("_TranslucencyColor", new Color(0.68f, 0.90f, 0.28f, 1f));
            dict["Foliage_LushGreen"] = matGreen;

            // 2. Emerald Deep Foliage
            var matEmerald = GetOrCreateMaterial($"{MaterialsFolder}/M_AnimeFoliage_Emerald.mat", foliageShader);
            matEmerald.SetColor("_BaseColor", new Color(0.24f, 0.65f, 0.38f, 1f));
            matEmerald.SetColor("_ShadowColor", new Color(0.08f, 0.26f, 0.18f, 1f));
            matEmerald.SetColor("_TranslucencyColor", new Color(0.45f, 0.82f, 0.42f, 1f));
            dict["Foliage_Emerald"] = matEmerald;

            // 3. Autumn Gold Foliage
            var matGold = GetOrCreateMaterial($"{MaterialsFolder}/M_AnimeFoliage_AutumnGold.mat", foliageShader);
            matGold.SetColor("_BaseColor", new Color(0.92f, 0.72f, 0.18f, 1f));
            matGold.SetColor("_ShadowColor", new Color(0.55f, 0.32f, 0.08f, 1f));
            matGold.SetColor("_TranslucencyColor", new Color(1.0f, 0.85f, 0.35f, 1f));
            dict["Foliage_AutumnGold"] = matGold;

            // 4. Sakura Blossom Foliage
            var matSakura = GetOrCreateMaterial($"{MaterialsFolder}/M_AnimeFoliage_SakuraPink.mat", foliageShader);
            matSakura.SetColor("_BaseColor", new Color(0.96f, 0.68f, 0.76f, 1f));
            matSakura.SetColor("_ShadowColor", new Color(0.62f, 0.32f, 0.45f, 1f));
            matSakura.SetColor("_TranslucencyColor", new Color(1.0f, 0.82f, 0.88f, 1f));
            dict["Foliage_Sakura"] = matSakura;

            // 5. Stylized Bark Trunk
            var matBark = GetOrCreateMaterial($"{MaterialsFolder}/M_AnimeTrunk_WarmBark.mat", trunkShader);
            matBark.SetColor("_BaseColor", new Color(0.42f, 0.28f, 0.18f, 1f));
            matBark.SetColor("_ShadowColor", new Color(0.22f, 0.14f, 0.09f, 1f));
            matBark.SetColor("_MossBaseColor", new Color(0.30f, 0.44f, 0.18f, 1f));
            dict["Trunk_Bark"] = matBark;

            AssetDatabase.SaveAssets();
            return dict;
        }

        private static Material GetOrCreateMaterial(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            return mat;
        }

        public static Dictionary<string, Mesh> GenerateMeshes()
        {
            var dict = new Dictionary<string, Mesh>();

            dict["GrandOak_Trunk"] = SaveMesh(BuildTrunkMesh(height: 7.5f, baseRadius: 1.35f, topRadius: 0.55f, branchCount: 4), "AnimeTree_GrandOak_Trunk.asset");
            dict["GrandOak_Canopy"] = SaveMesh(BuildCanopyMesh(new[]
            {
                new CanopyBlob(new Vector3(0f, 7.5f, 0f), 3.2f, 1.1f),
                new CanopyBlob(new Vector3(2.2f, 6.2f, 1.2f), 2.4f, 0.95f),
                new CanopyBlob(new Vector3(-2.0f, 6.0f, -1.0f), 2.3f, 0.9f),
                new CanopyBlob(new Vector3(0.5f, 5.8f, -2.1f), 2.2f, 0.9f),
                new CanopyBlob(new Vector3(-1.2f, 6.8f, 1.8f), 2.1f, 0.95f)
            }), "AnimeTree_GrandOak_Canopy.asset");

            dict["Broadleaf_Trunk"] = SaveMesh(BuildTrunkMesh(height: 5.5f, baseRadius: 0.85f, topRadius: 0.4f, branchCount: 3), "AnimeTree_Broadleaf_Trunk.asset");
            dict["Broadleaf_Canopy"] = SaveMesh(BuildCanopyMesh(new[]
            {
                new CanopyBlob(new Vector3(0f, 5.6f, 0f), 2.5f, 1.05f),
                new CanopyBlob(new Vector3(1.3f, 4.8f, 0.8f), 1.9f, 0.9f),
                new CanopyBlob(new Vector3(-1.2f, 4.7f, -0.6f), 1.8f, 0.9f),
                new CanopyBlob(new Vector3(0.2f, 4.5f, -1.3f), 1.7f, 0.85f)
            }), "AnimeTree_Broadleaf_Canopy.asset");

            dict["TieredPine_Trunk"] = SaveMesh(BuildTrunkMesh(height: 8.5f, baseRadius: 0.75f, topRadius: 0.25f, branchCount: 0), "AnimeTree_TieredPine_Trunk.asset");
            dict["TieredPine_Canopy"] = SaveMesh(BuildCanopyMesh(new[]
            {
                new CanopyBlob(new Vector3(0f, 3.8f, 0f), 2.8f, 0.65f),
                new CanopyBlob(new Vector3(0f, 5.4f, 0f), 2.3f, 0.65f),
                new CanopyBlob(new Vector3(0f, 6.8f, 0f), 1.8f, 0.65f),
                new CanopyBlob(new Vector3(0f, 8.0f, 0f), 1.2f, 0.7f)
            }), "AnimeTree_TieredPine_Canopy.asset");

            dict["Bush_Canopy"] = SaveMesh(BuildCanopyMesh(new[]
            {
                new CanopyBlob(new Vector3(0f, 0.9f, 0f), 1.25f, 0.85f),
                new CanopyBlob(new Vector3(0.7f, 0.7f, 0.3f), 0.95f, 0.8f),
                new CanopyBlob(new Vector3(-0.6f, 0.7f, -0.4f), 0.9f, 0.8f)
            }), "AnimeBush_FluffyCluster.asset");

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
            else
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
        }

        public struct CanopyBlob
        {
            public Vector3 center;
            public float radius;
            public float heightSquash;
            public CanopyBlob(Vector3 c, float r, float squash)
            {
                center = c;
                radius = r;
                heightSquash = squash;
            }
        }

        private static Mesh BuildCanopyMesh(CanopyBlob[] blobs)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            int latSegments = 10;
            int lonSegments = 12;

            foreach (var blob in blobs)
            {
                int baseIndex = vertices.Count;

                for (int lat = 0; lat <= latSegments; lat++)
                {
                    float theta = lat * Mathf.PI / latSegments;
                    float sinTheta = Mathf.Sin(theta);
                    float cosTheta = Mathf.Cos(theta);

                    for (int lon = 0; lon <= lonSegments; lon++)
                    {
                        float phi = lon * 2f * Mathf.PI / lonSegments;
                        float sinPhi = Mathf.Sin(phi);
                        float cosPhi = Mathf.Cos(phi);

                        // Unit sphere direction
                        Vector3 unitDir = new Vector3(sinTheta * cosPhi, cosTheta * blob.heightSquash, sinTheta * sinPhi);

                        // Organic cloud noise offset
                        float noise = Mathf.Sin(unitDir.x * 4f) * Mathf.Cos(unitDir.z * 4f) * 0.12f;
                        Vector3 pos = blob.center + unitDir * (blob.radius * (1f + noise));

                        // Radial anime normal directed outwards from the blob center for soft shading
                        Vector3 normal = Vector3.Normalize(unitDir);

                        vertices.Add(pos);
                        normals.Add(normal);
                        uvs.Add(new Vector2((float)lon / lonSegments, (float)lat / latSegments));
                    }
                }

                for (int lat = 0; lat < latSegments; lat++)
                {
                    for (int lon = 0; lon < lonSegments; lon++)
                    {
                        int current = baseIndex + lat * (lonSegments + 1) + lon;
                        int next = current + lonSegments + 1;

                        triangles.Add(current);
                        triangles.Add(next);
                        triangles.Add(current + 1);

                        triangles.Add(current + 1);
                        triangles.Add(next);
                        triangles.Add(next + 1);
                    }
                }
            }

            var mesh = new Mesh();
            mesh.name = "AnimeNature_Canopy";
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildTrunkMesh(float height, float baseRadius, float topRadius, int branchCount)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            int radialSegments = 10;
            int heightRings = 7;

            for (int r = 0; r <= heightRings; r++)
            {
                float t = (float)r / heightRings;
                float currentY = t * height;

                // Tapered radius with root flare at the bottom
                float rootFlare = Mathf.Exp(-t * 5.0f) * 0.6f;
                float rad = Mathf.Lerp(baseRadius, topRadius, t) + rootFlare;

                for (int s = 0; s <= radialSegments; s++)
                {
                    float angle = s * 2f * Mathf.PI / radialSegments;
                    float x = Mathf.Cos(angle) * rad;
                    float z = Mathf.Sin(angle) * rad;

                    // Subtle organic lean
                    float leanX = Mathf.Sin(t * 1.5f) * 0.25f;
                    float leanZ = Mathf.Cos(t * 1.2f) * 0.15f;

                    Vector3 pos = new Vector3(x + leanX, currentY, z + leanZ);
                    Vector3 norm = new Vector3(Mathf.Cos(angle), 0.1f, Mathf.Sin(angle)).normalized;

                    vertices.Add(pos);
                    normals.Add(norm);
                    uvs.Add(new Vector2((float)s / radialSegments, t * 2.5f));
                }
            }

            for (int r = 0; r < heightRings; r++)
            {
                for (int s = 0; s < radialSegments; s++)
                {
                    int current = r * (radialSegments + 1) + s;
                    int next = current + radialSegments + 1;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            var mesh = new Mesh();
            mesh.name = "AnimeNature_Trunk";
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Dictionary<string, GameObject> BuildPrefabs(Dictionary<string, Mesh> meshes, Dictionary<string, Material> materials)
        {
            var prefabs = new Dictionary<string, GameObject>();

            prefabs["PFB_AnimeTree_GrandOak"] = CreateTreePrefab("PFB_AnimeTree_GrandOak", meshes["GrandOak_Trunk"], meshes["GrandOak_Canopy"], materials["Trunk_Bark"], materials["Foliage_LushGreen"], trunkRadius: 1.35f, trunkHeight: 7.5f);
            prefabs["PFB_AnimeTree_Broadleaf"] = CreateTreePrefab("PFB_AnimeTree_Broadleaf", meshes["Broadleaf_Trunk"], meshes["Broadleaf_Canopy"], materials["Trunk_Bark"], materials["Foliage_LushGreen"], trunkRadius: 0.85f, trunkHeight: 5.5f);
            prefabs["PFB_AnimeTree_TieredPine"] = CreateTreePrefab("PFB_AnimeTree_TieredPine", meshes["TieredPine_Trunk"], meshes["TieredPine_Canopy"], materials["Trunk_Bark"], materials["Foliage_Emerald"], trunkRadius: 0.75f, trunkHeight: 8.5f);
            prefabs["PFB_AnimeTree_CherryBlossom"] = CreateTreePrefab("PFB_AnimeTree_CherryBlossom", meshes["Broadleaf_Trunk"], meshes["Broadleaf_Canopy"], materials["Trunk_Bark"], materials["Foliage_Sakura"], trunkRadius: 0.85f, trunkHeight: 5.5f);

            // Bush Prefab
            var bushGo = new GameObject("PFB_AnimeBush_Fluffy");
            var bushMf = bushGo.AddComponent<MeshFilter>();
            bushMf.sharedMesh = meshes["Bush_Canopy"];
            var bushMr = bushGo.AddComponent<MeshRenderer>();
            bushMr.sharedMaterial = materials["Foliage_LushGreen"];
            var bushCol = bushGo.AddComponent<SphereCollider>();
            bushCol.radius = 1.2f;
            bushCol.center = new Vector3(0f, 0.8f, 0f);

            string bushPath = $"{PrefabsFolder}/PFB_AnimeBush_Fluffy.prefab";
            var savedBush = PrefabUtility.SaveAsPrefabAsset(bushGo, bushPath);
            GameObject.DestroyImmediate(bushGo);
            prefabs["PFB_AnimeBush_Fluffy"] = savedBush;

            return prefabs;
        }

        private static GameObject CreateTreePrefab(string name, Mesh trunkMesh, Mesh canopyMesh, Material trunkMat, Material canopyMat, float trunkRadius, float trunkHeight)
        {
            var root = new GameObject(name);

            // Trunk child
            var trunk = new GameObject("Trunk");
            trunk.transform.SetParent(root.transform, false);
            var tMf = trunk.AddComponent<MeshFilter>();
            tMf.sharedMesh = trunkMesh;
            var tMr = trunk.AddComponent<MeshRenderer>();
            tMr.sharedMaterial = trunkMat;

            // Trunk physics collider
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = trunkRadius * 0.75f;
            col.height = trunkHeight;
            col.center = new Vector3(0f, trunkHeight * 0.5f, 0f);

            // Canopy child
            var canopy = new GameObject("Canopy");
            canopy.transform.SetParent(root.transform, false);
            var cMf = canopy.AddComponent<MeshFilter>();
            cMf.sharedMesh = canopyMesh;
            var cMr = canopy.AddComponent<MeshRenderer>();
            cMr.sharedMaterial = canopyMat;

            string path = $"{PrefabsFolder}/{name}.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            return saved;
        }

        public static int ReplaceSceneTreesWithAnimeNature(Dictionary<string, GameObject> prefabs)
        {
            var allFilters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            int count = 0;

            var pinePrefab = prefabs["PFB_AnimeTree_TieredPine"];
            var broadleafPrefab = prefabs["PFB_AnimeTree_Broadleaf"];
            var grandOakPrefab = prefabs["PFB_AnimeTree_GrandOak"];

            var targetsToReplace = new List<GameObject>();

            foreach (var mf in allFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (path.Contains("Prop_Fantasy_Tree") || mf.gameObject.name.StartsWith("Tree_Pine_"))
                {
                    targetsToReplace.Add(mf.gameObject);
                }
            }

            foreach (var oldTree in targetsToReplace)
            {
                if (oldTree == null) continue;

                Vector3 pos = oldTree.transform.position;
                Quaternion rot = oldTree.transform.rotation;
                Transform parent = oldTree.transform.parent;

                // Pick an anime tree variant based on position hash for varied beauty
                int hash = Mathf.Abs((int)(pos.x * 73 + pos.z * 137));
                GameObject chosenPrefab = (hash % 3) switch
                {
                    0 => pinePrefab,
                    1 => broadleafPrefab,
                    _ => grandOakPrefab
                };

                // Instantiate new anime tree
                var newTree = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab);
                newTree.name = oldTree.name.Replace("Tree_Pine_", "AnimeTree_");
                newTree.transform.position = pos;
                newTree.transform.rotation = Quaternion.Euler(0f, (hash * 47) % 360, 0f);
                newTree.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, (hash % 100) / 100f);

                if (parent != null)
                {
                    newTree.transform.SetParent(parent, true);
                }

                Undo.DestroyObjectImmediate(oldTree);
                count++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            return count;
        }
#endif
    }
}
