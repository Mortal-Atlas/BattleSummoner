#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.EditorScripts
{
    public static class StairsGapInspector
    {
        [MenuItem("BattleSummoner/Debug/Inspect Stairs and Bridges")]
        public static void InspectStairs()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== STAIRS & GAP REPORT ===");

            var stairs = GameObject.Find("02_Stone_Tier_Stairs");
            if (stairs != null)
            {
                sb.AppendLine($"\n--- 02_Stone_Tier_Stairs ({stairs.transform.childCount} children) ---");
                foreach (Transform t in stairs.transform)
                {
                    Bounds b = new Bounds();
                    bool hasB = false;
                    foreach (Transform c in t)
                    {
                        var mr = c.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {
                            if (!hasB) { b = mr.bounds; hasB = true; }
                            else b.Encapsulate(mr.bounds);
                        }
                    }
                    sb.AppendLine($"  {t.name}: boundsCenter={b.center:F2}, boundsSize={b.size:F2}, min={b.min:F2}, max={b.max:F2}");
                }
            }

            var bvd = GameObject.Find("01_Radial_Grand_Boulevards");
            if (bvd != null)
            {
                sb.AppendLine($"\n--- 01_Radial_Grand_Boulevards ({bvd.transform.childCount} children) ---");
                foreach (Transform t in bvd.transform)
                {
                    var mr = t.GetComponent<MeshRenderer>();
                    sb.AppendLine($"  {t.name} (childCount={t.childCount}): bounds={mr?.bounds}");
                }
            }

            var northBvd = GameObject.Find("Boulevard_North_0");
            if (northBvd != null)
            {
                sb.AppendLine($"\n--- Boulevard_North_0 ({northBvd.transform.childCount} children) ---");
                foreach (Transform t in northBvd.transform)
                {
                    var mr = t.GetComponent<MeshRenderer>();
                    sb.AppendLine($"  {t.name}: bounds={mr?.bounds}");
                }
            }

            var roadways = GameObject.Find("01_Cobblestone_Roadways");
            if (roadways != null)
            {
                sb.AppendLine($"\n--- 01_Cobblestone_Roadways ({roadways.transform.childCount} children) ---");
                foreach (Transform t in roadways.transform)
                {
                    if (t.name.Contains("0") || t.name.Contains("North") || t.name.Contains("Plaza"))
                    {
                        var mr = t.GetComponent<MeshRenderer>();
                        sb.AppendLine($"  {t.name}: bounds={mr?.bounds}");
                    }
                }
            }

            string outPath = "Assets/Editor/World/StairsReport.txt";
            System.IO.File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Wrote fast stairs report to {outPath}");
        }
    }
}
#endif
