#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace BattleSummoner.EditorScripts
{
    public static class SkyInspectorAndFixer
    {
        [MenuItem("BattleSummoner/World/Inspect Sky Objects")]
        public static void InspectSky()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log($"=== DETAILED SKY INSPECTION IN {activeScene.name} ===");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== DETAILED SKY INSPECTION REPORT ({activeScene.name}) ===");

            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            
            // Collect all terrain colliders or terrain objects to test grounding
            var terrainGo = GameObject.Find("Floor01_MacroContinent_Terrain") 
                ?? GameObject.Find("CircularContinent_MacroLevel1")
                ?? GameObject.Find("Terrain");

            Collider terrainCol = terrainGo != null ? terrainGo.GetComponentInChildren<Collider>() : null;

            // Also find all island mesh colliders
            var skyArch = GameObject.Find("Grand_Sky_Archipelago_System");
            var skyClouds = GameObject.Find("Sky_Archipelago_And_Clouds_Root");
            var skyAetherion = GameObject.Find("Sky_Layer_Aetherion");
            var worldSkyPOIs = GameObject.Find("World_And_Sky_Activities_POIs_Model");

            List<Collider> islandCols = new List<Collider>();
            if (skyArch != null) islandCols.AddRange(skyArch.GetComponentsInChildren<Collider>());
            if (skyClouds != null) islandCols.AddRange(skyClouds.GetComponentsInChildren<Collider>());
            if (skyAetherion != null) islandCols.AddRange(skyAetherion.GetComponentsInChildren<Collider>());

            sb.AppendLine($"Found terrain: {(terrainCol != null ? terrainCol.name : "null")}");
            sb.AppendLine($"Found island colliders: {islandCols.Count}");

            // Group high-altitude renderers by their root or immediate major category
            var skyItems = new List<(Renderer r, float altitude, float distToGround, string category, string hierarchy)>();

            foreach (var r in allRenderers)
            {
                if (r == null || !r.enabled) continue;
                Vector3 pos = r.transform.position;
                if (pos.y < 85.0f) continue; // Only high altitude items

                // Calculate distance to terrain or island below
                float groundY = -999f;
                Ray ray = new Ray(pos + Vector3.up * 1f, Vector3.down);
                if (terrainCol != null && terrainCol.Raycast(ray, out RaycastHit hitTerrain, 2000f))
                {
                    groundY = Mathf.Max(groundY, hitTerrain.point.y);
                }
                foreach (var col in islandCols)
                {
                    if (col != null && col.Raycast(ray, out RaycastHit hitIsland, 2000f))
                    {
                        groundY = Mathf.Max(groundY, hitIsland.point.y);
                    }
                }

                float distToGround = groundY > -900f ? (pos.y - groundY) : pos.y;

                // Build hierarchy path
                string path = r.gameObject.name;
                Transform curr = r.transform.parent;
                while (curr != null)
                {
                    path = curr.name + "/" + path;
                    curr = curr.parent;
                }

                string rootName = r.transform.root.name;
                skyItems.Add((r, pos.y, distToGround, rootName, path));
            }

            sb.AppendLine($"\nTotal renderers with Y >= 85m: {skyItems.Count}");

            // Filter for items that are floating in air (distToGround > 5m) and not legitimate terrain vegetation
            var floatingInAir = new List<(Renderer r, float altitude, float distToGround, string category, string hierarchy)>();
            foreach (var item in skyItems)
            {
                // If it's more than 5m above any ground / island mesh, it is floating!
                if (item.distToGround > 5.0f)
                {
                    floatingInAir.Add(item);
                }
            }

            sb.AppendLine($"\n=== FLOATING ITEMS IN AIR (distToGround > 5m): {floatingInAir.Count} ===");
            // Group floating items by root
            Dictionary<string, List<string>> floatingGroups = new Dictionary<string, List<string>>();
            foreach (var f in floatingInAir)
            {
                if (!floatingGroups.ContainsKey(f.category)) floatingGroups[f.category] = new List<string>();
                floatingGroups[f.category].Add($"'{f.r.name}' pos={f.r.transform.position:F1} | distGround={f.distToGround:F1}m | path={f.hierarchy}");
            }

            foreach (var kvp in floatingGroups)
            {
                sb.AppendLine($"\n--- Root: {kvp.Key} (count={kvp.Value.Count}) ---");
                int limit = Mathf.Min(kvp.Value.Count, 50);
                for (int i = 0; i < limit; i++)
                {
                    sb.AppendLine($"   {kvp.Value[i]}");
                }
                if (kvp.Value.Count > 50)
                {
                    sb.AppendLine($"   ... and {kvp.Value.Count - 50} more");
                }
            }

            string outPath = "Assets/Editor/World/SkyReport.txt";
            System.IO.File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[SkyInspector] Wrote detailed report to {outPath} (floating in air: {floatingInAir.Count})");
        }
    }
}
#endif
