#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.Editor
{
    public static class SceneDiagnosticInspector
    {
        [MenuItem("BattleSummoner/Diagnose Magic City Scene", false, 99)]
        public static void Diagnose()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== MAGIC CITY SCENE DIAGNOSTICS ===");

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            sb.AppendLine($"Active Scene: {activeScene.name} (path: {activeScene.path})");
            var roots = activeScene.GetRootGameObjects();
            sb.AppendLine($"Total Root GameObjects: {roots.Length}");
            for (int r = 0; r < roots.Length; r++)
            {
                sb.AppendLine($"  Root[{r}]: '{roots[r].name}', active: {roots[r].activeSelf}, children: {roots[r].transform.childCount}");
            }

            var rowHouses = GameObject.Find("01_Tier_Stepped_RowHouses");
            if (rowHouses != null)
            {
                sb.AppendLine($"Found 01_Tier_Stepped_RowHouses: childCount = {rowHouses.transform.childCount}");
                for (int i = 0; i < Mathf.Min(10, rowHouses.transform.childCount); i++)
                {
                    var c = rowHouses.transform.GetChild(i);
                    var mf = c.GetComponent<MeshFilter>();
                    var mr = c.GetComponent<MeshRenderer>();
                    string meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "NULL";
                    int matCount = mr != null && mr.sharedMaterials != null ? mr.sharedMaterials.Length : 0;
                    string shaderName = (matCount > 0 && mr.sharedMaterials[0] != null) ? mr.sharedMaterials[0].shader.name : "NULL";
                    sb.AppendLine($"  Child[{i}]: '{c.name}', Mesh: '{meshName}', Mats: {matCount}, Shader[0]: '{shaderName}', Active: {c.gameObject.activeSelf}");
                    
                    // Check sub-children of this house
                    for (int j = 0; j < c.childCount; j++)
                    {
                        var sc = c.GetChild(j);
                        sb.AppendLine($"    SubChild[{j}]: '{sc.name}', Active: {sc.gameObject.activeSelf}");
                    }
                }
            }
            else
            {
                sb.AppendLine("01_Tier_Stepped_RowHouses NOT FOUND!");
            }

            // Check floating windows / lanterns
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int windowCount = 0;
            int lanternCount = 0;
            foreach (var go in allObjects)
            {
                if (go.name.ToLowerInvariant().Contains("window")) windowCount++;
                if (go.name.ToLowerInvariant().Contains("lantern")) lanternCount++;
            }
            sb.AppendLine($"Total GameObjects in scene with 'window': {windowCount}, with 'lantern': {lanternCount}");

            File.WriteAllText("SceneDiagnosticsReport.txt", sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
#endif
