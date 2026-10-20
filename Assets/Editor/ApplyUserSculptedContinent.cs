using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BattleSummoner.EditorTools
{
    [InitializeOnLoad]
    public static class ApplyUserSculptedContinent
    {
        private const string TriggerFile = "Temp/ApplyUserSculpt_Done.txt";

        static ApplyUserSculptedContinent()
        {
            EditorApplication.delayCall += ExecuteIfNeeded;
        }

        [MenuItem("BattleSummoner/Apply User Sculpted Continent Now")]
        public static void ForceApply()
        {
            if (File.Exists(TriggerFile)) File.Delete(TriggerFile);
            Execute();
        }

        private static void ExecuteIfNeeded()
        {
            if (File.Exists(TriggerFile)) return;
            Execute();
        }

        public static void Execute()
        {
            string report = "=== APPLY USER SCULPTED CONTINENT REPORT ===\n";
            report += $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";

            try
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.path != "Assets/Scenes/OverworldMap.unity")
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity", OpenSceneMode.Single);
                }
                report += $"Active Scene: {scene.path}\n";

                // 1. Force re-import the FBX models exported from Continent_Map_Sculptjz.blend
                string fbxPath = "Assets/Models/Environments/CircularContinent_MacroLevel1.fbx";
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);

                string fbxJzPath = "Assets/Models/Environments/Continent_Map_Sculptjz.fbx";
                AssetDatabase.ImportAsset(fbxJzPath, ImportAssetOptions.ForceUpdate);

                // Force re-import textures so GPU cache updates
                AssetDatabase.ImportAsset("Assets/Textures/Environments/Continent/T_Continent_MacroAlbedo.png", ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset("Assets/Textures/Environments/Continent/T_Continent_MacroNormal.png", ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset("Assets/Textures/Environments/Continent/T_Continent_BiomeMask.png", ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset("Assets/Textures/Environments/Continent/T_Continent_WaterMask.png", ImportAssetOptions.ForceUpdate);

                // Load mesh from FBX
                var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                Mesh terrainMesh = null;
                foreach (var obj in assets)
                {
                    if (obj is Mesh m && m.name.Contains("Floor01_MacroContinent_Terrain"))
                    {
                        terrainMesh = m;
                        break;
                    }
                }

                if (terrainMesh == null)
                {
                    // Fallback to Continent_Map_Sculptjz.fbx
                    var jzAssets = AssetDatabase.LoadAllAssetsAtPath(fbxJzPath);
                    foreach (var obj in jzAssets)
                    {
                        if (obj is Mesh m && m.name.Contains("Floor01_MacroContinent_Terrain"))
                        {
                            terrainMesh = m;
                            break;
                        }
                    }
                }

                if (terrainMesh == null)
                {
                    report += "ERROR: Could not find Floor01_MacroContinent_Terrain mesh in FBX assets!\n";
                    File.WriteAllText("Assets/Screenshots/UserContinentVerificationReport.txt", report);
                    return;
                }

                // Analyze mesh vertices and verify peak orientation
                var verts = terrainMesh.vertices;
                var normals = terrainMesh.normals;
                Vector3 peak = Vector3.zero;
                Vector3 lowest = Vector3.zero;
                float maxY = float.MinValue;
                float minY = float.MaxValue;
                float avgNormY = 0f;
                int upNormCount = 0;

                for (int i = 0; i < verts.Length; i++)
                {
                    if (verts[i].y > maxY) { maxY = verts[i].y; peak = verts[i]; }
                    if (verts[i].y < minY) { minY = verts[i].y; lowest = verts[i]; }
                    if (normals != null && i < normals.Length)
                    {
                        avgNormY += normals[i].y;
                        if (normals[i].y > 0) upNormCount++;
                    }
                }
                if (normals != null && normals.Length > 0) avgNormY /= normals.Length;

                report += $"Terrain Mesh: '{terrainMesh.name}' ({verts.Length} vertices, {terrainMesh.triangles.Length / 3} triangles)\n";
                report += $"  Bounds: Center={terrainMesh.bounds.center}, Extents={terrainMesh.bounds.extents}\n";
                report += $"  Peak: Height Y={peak.y:F2}m at (X={peak.x:F2}m, Z={peak.z:F2}m)\n";
                report += $"  Lowest: Depth Y={lowest.y:F2}m at (X={lowest.x:F2}m, Z={lowest.z:F2}m)\n";
                report += $"  Normals: Avg Y={avgNormY:F4}, Upward Faces={upNormCount}/{normals.Length} ({(float)upNormCount / normals.Length * 100:F1}%)\n";

                // Verify Orientation
                bool isNorthwest = peak.x < 0 && peak.z > 0;
                bool is1192m = Math.Abs(peak.y - 1192.36f) < 2.0f;
                report += $"  ORIENTATION CHECK: Northwest (X<0, Z>0)? {isNorthwest} | Height ~1192m? {is1192m}\n";

                // 2. Assign to GameObject CircularContinent_MacroLevel1
                var continentGo = GameObject.Find("CircularContinent_MacroLevel1");
                if (continentGo == null)
                {
                    report += "ERROR: GameObject 'CircularContinent_MacroLevel1' not found in scene!\n";
                    File.WriteAllText("Assets/Screenshots/UserContinentVerificationReport.txt", report);
                    return;
                }

                Transform tTr = continentGo.transform.Find("Floor01_MacroContinent_Terrain");
                if (tTr == null) tTr = continentGo.transform;

                var mf = tTr.GetComponent<MeshFilter>();
                if (mf == null) mf = tTr.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = terrainMesh;

                var mc = tTr.GetComponent<MeshCollider>();
                if (mc == null) mc = tTr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = null;
                mc.sharedMesh = terrainMesh;

                // 3. Material & Textures Setup
                string matPath = "Assets/Materials/M_CircularContinent_MacroTerrain.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat != null)
                {
                    // Check and reassign shader if needed
                    var shader = Shader.Find("BattleSummoner/Environment/URP_CircularContinent_MacroTerrain");
                    if (shader != null && mat.shader != shader)
                    {
                        mat.shader = shader;
                        report += $"  Material shader reassigned to {shader.name}\n";
                    }

                    // Assign textures
                    var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environments/Continent/T_Continent_MacroAlbedo.png");
                    var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environments/Continent/T_Continent_MacroNormal.png");
                    var splatTex  = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environments/Continent/T_Continent_BiomeMask.png");

                    if (albedoTex != null) mat.SetTexture("_MacroAlbedoTex", albedoTex);
                    if (normalTex != null) mat.SetTexture("_MacroNormalTex", normalTex);
                    if (splatTex != null)  mat.SetTexture("_MacroSplatTex", splatTex);

                    mat.SetFloat("_MacroBlend", 0.92f);
                    mat.SetFloat("_MacroNormalStrength", 1.0f);
                    mat.SetFloat("_BrushScale", 18.0f);
                    mat.SetFloat("_BrushDepth", 0.25f);
                    mat.SetFloat("_CelBands", 4.0f);
                    mat.SetFloat("_CliffSlopeThreshold", 0.55f);
                    mat.SetFloat("_CliffBlendSmoothness", 0.15f);
                    mat.SetFloat("_Smoothness", 0.08f);

                    EditorUtility.SetDirty(mat);

                    var mr = tTr.GetComponent<MeshRenderer>();
                    if (mr == null) mr = tTr.gameObject.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;

                    report += $"  Material configured with Albedo='{albedoTex?.name}', Normal='{normalTex?.name}', Splat='{splatTex?.name}'\n";
                }

                // 4. Capture Verification Screenshots
                CaptureVerificationCameras(report);

                // 5. Save Scene and Assets
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                report += $"Scene Saved: {saved}\n";
                report += "=== ALL VERIFICATION TASKS COMPLETED SUCCESSFULLY ===\n";

                File.WriteAllText(TriggerFile, "DONE");
                File.WriteAllText("Assets/Screenshots/UserContinentVerificationReport.txt", report);
                Debug.Log(report);
            }
            catch (Exception ex)
            {
                report += $"\nFATAL EXCEPTION: {ex}\n";
                File.WriteAllText("Assets/Screenshots/UserContinentVerificationReport.txt", report);
                Debug.LogError(report);
            }
        }

        private static void CaptureVerificationCameras(string currentReport)
        {
            // Camera shots to take:
            // 1. Overview looking from South-Southeast across the entire circular continent to the NW Spire
            TakeShot("Assets/Screenshots/Continent_Overview_Verification.png",
                new Vector3(400f, 2600f, -2800f),
                new Vector3(-400f, 200f, 600f), 60f);

            // 2. High Northwest Peak Close-Up (showing 1192m sculpted peak, volcanic caldera, and rock faces)
            TakeShot("Assets/Screenshots/Continent_Northwest_Peak_Verification.png",
                new Vector3(-1138f + 600f, 1500f, 1357f - 700f),
                new Vector3(-1138f, 1192f, 1357f), 65f);

            // 3. Central Metropolis Terraces, Moat & Bridges meeting the terrain
            TakeShot("Assets/Screenshots/Continent_Metropolis_Moat_Verification.png",
                new Vector3(0f, 650f, -850f),
                new Vector3(0f, 50f, 0f), 55f);
        }

        private static void TakeShot(string outputPath, Vector3 camPos, Vector3 lookTarget, float fov)
        {
            GameObject camGo = new GameObject("TempVerificationCam");
            try
            {
                Camera cam = camGo.AddComponent<Camera>();
                cam.transform.position = camPos;
                cam.transform.LookAt(lookTarget);
                cam.fieldOfView = fov;
                cam.nearClipPlane = 1.0f;
                cam.farClipPlane = 15000f;
                cam.clearFlags = CameraClearFlags.Skybox;

                int width = 1920;
                int height = 1080;
                RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(outputPath, bytes);

                RenderTexture.active = null;
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }
    }
}
