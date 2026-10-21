#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.VisualPolish.Editor
{
    /// <summary>
    /// Automatically normalizes and scales newly imported Meshy AI 3D props.
    /// Meshy models frequently import at 10,000 units (centimeters vs meters scale).
    /// This postprocessor immediately detects oversized props and clamps them to realistic game dimensions.
    /// </summary>
    public class MeshyPropImportScaler : AssetPostprocessor
    {
        private static readonly Dictionary<string, float> KnownPropTargetSizes = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Prop_SakuraStonePlanterRing", 5.0f },
            { "Prop_MarketMerchantSet_CratesAndBaskets", 2.5f },
            { "Prop_Mondstadt_WroughtIronStreetLamp", 3.8f },
            { "Prop_AlchemistCauldron_Stylized", 1.8f },
            { "Prop_PlazaFlowerBox_And_Bench", 2.4f },
            { "Prop_Lantern", 1.2f },
            { "Prop_Crate", 1.0f },
            { "Prop_Barrel", 1.2f },
            { "Prop_Bench", 2.2f },
            { "Prop_Table", 1.8f },
            { "Prop_Chair", 1.0f },
            { "Prop_Cart", 2.8f },
            { "Prop_Chest", 1.1f },
            { "Prop_Signpost", 2.5f }
        };

        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Meshy") && !assetPath.Contains("Props")) return;

            ModelImporter importer = (ModelImporter)assetImporter;
            if (importer == null) return;

            // Ensure imported models have normal generation and readable meshes
            importer.isReadable = true;
            importer.importNormals = ModelImporterNormals.Import;
        }

        public static void NormalizeModelScale(string assetPath, float targetMaxDimensionMeters = 2.5f)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (importer == null || prefab == null) return;

            Bounds bounds = CalculateHierarchyBounds(prefab);
            float currentMaxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

            if (currentMaxDim > 0.001f && Mathf.Abs(currentMaxDim - targetMaxDimensionMeters) > 0.1f)
            {
                float ratio = targetMaxDimensionMeters / currentMaxDim;
                importer.globalScale *= ratio;
                importer.SaveAndReimport();
                Debug.Log($"[MeshyPropImportScaler] Scaled '{Path.GetFileName(assetPath)}' from {currentMaxDim:F2}m to target {targetMaxDimensionMeters:F2}m (globalScale: {importer.globalScale:F5}).");
            }
        }

        public static void AutoScaleAllMeshyProps(string folderPath = "Assets/Models/MeshyPainterlyProps")
        {
            if (!Directory.Exists(folderPath)) return;

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileNameNoExt = Path.GetFileNameWithoutExtension(path);

                float targetSize = 2.2f; // Default sensible prop size
                foreach (var kvp in KnownPropTargetSizes)
                {
                    if (fileNameNoExt.IndexOf(kvp.Key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        targetSize = kvp.Value;
                        break;
                    }
                }

                NormalizeModelScale(path, targetSize);
            }
        }

        private static Bounds CalculateHierarchyBounds(GameObject root)
        {
            Bounds b = new Bounds();
            bool hasBounds = false;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                if (!hasBounds)
                {
                    b = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        [MenuItem("BattleSummoner/Visual Polish/Auto-Scale All Meshy Props To Real World Dimensions")]
        public static void MenuScaleAllProps()
        {
            AutoScaleAllMeshyProps();
        }
    }
}
#endif
