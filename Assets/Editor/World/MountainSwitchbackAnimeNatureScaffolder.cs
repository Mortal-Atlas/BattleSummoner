#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.World.Editor
{
    public static class MountainSwitchbackAnimeNatureScaffolder
    {
        [MenuItem("BattleSummoner/MCP World Remediation/Populate Switchback Trail & River Gorge with Anime Nature (276931)")]
        public static void PopulateMountainTrailAndRiverGorge()
        {
            // 1. Discover all authentic Anime Nature (276931) Tree, Bush & Rock prefabs in Assets/
            List<GameObject> pinePrefabs   = new List<GameObject>();
            List<GameObject> bushPrefabs   = new List<GameObject>();
            List<GameObject> rockPrefabs   = new List<GameObject>();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLowerInvariant();

                // Match Anime Nature package paths or stylized nature prefabs
                if (!lower.Contains("anime") && !lower.Contains("nature") && !lower.Contains("stylized") && !lower.Contains("environment") && !lower.Contains("idyllic"))
                    continue;

                GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (pf == null) continue;

                string n = pf.name.ToLowerInvariant();
                if (n.Contains("lamp") || n.Contains("lantern") || n.Contains("house") || n.Contains("door") || n.Contains("prop")) continue;

                if (n.Contains("pine") || n.Contains("spruce") || n.Contains("fir") || n.Contains("tree") || n.Contains("cypress") || n.Contains("conifer"))
                    pinePrefabs.Add(pf);
                else if (n.Contains("bush") || n.Contains("shrub") || n.Contains("fern") || n.Contains("grass"))
                    bushPrefabs.Add(pf);
                else if (n.Contains("rock") || n.Contains("cliff") || n.Contains("boulder") || n.Contains("stone"))
                    rockPrefabs.Add(pf);
            }

            GameObject existingRoot = GameObject.Find("Mountain_AnimeNature_SwitchbackAndGorge_Root");
            if (existingRoot != null) Undo.DestroyObjectImmediate(existingRoot);

            GameObject root = new GameObject("Mountain_AnimeNature_SwitchbackAndGorge_Root");
            Material cliffMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            if (cliffMat == null) cliffMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Metro_StoneBase.mat");

            Random.InitState(20261019);

            // 2. Scatter Cliff Overhangs, Pine Trees & Bushes along BOTH sides of the Switchback Trail
            int trailSamples = 140;
            for (int i = 0; i < trailSamples; i++)
            {
                float t = i / (float)(trailSamples - 1);
                float wz = Mathf.Lerp(560f, 2280f, t);
                float wx = -260f + Mathf.Sin(t * Mathf.PI * 4.5f) * 290f;

                // Tangent vector of switchback spine
                float tNext = Mathf.Min(1f, t + 0.008f);
                float wzNext = Mathf.Lerp(560f, 2280f, tNext);
                float wxNext = -260f + Mathf.Sin(tNext * Mathf.PI * 4.5f) * 290f;
                Vector2 tangent = new Vector2(wxNext - wx, wzNext - wz).normalized;
                Vector2 perp    = new Vector2(-tangent.y, tangent.x);

                // Spawn on Left (-1) and Right (+1) banks outside the 9.0m walkable trail corridor
                for (int side = -1; side <= 1; side += 2)
                {
                    // A. Dramatic Cliff Overhang Ledge (11m to 18m from trail center)
                    if (i % 2 == 0)
                    {
                        float rockDist = Random.Range(11.0f, 18.5f);
                        Vector3 rockOrigin = new Vector3(wx + perp.x * side * rockDist, 600f, wz + perp.y * side * rockDist);
                        if (Physics.Raycast(rockOrigin, Vector3.down, out RaycastHit rHit, 800f))
                        {
                            GameObject overhang = InstantiateOrFallbackRock(rockPrefabs, root.transform, cliffMat);
                            overhang.name = $"Switchback_CliffOverhang_{i:03d}_{(side < 0 ? "L" : "R")}";
                            overhang.transform.position = rHit.point + Vector3.up * Random.Range(2.2f, 5.0f);

                            // Lean 24 degrees inward over the switchback trail
                            Vector3 inwardDir = new Vector3(-perp.x * side, 0f, -perp.y * side);
                            Quaternion lookRot = Quaternion.LookRotation(inwardDir != Vector3.zero ? inwardDir : Vector3.forward, Vector3.up);
                            overhang.transform.rotation = lookRot * Quaternion.Euler(24f, Random.Range(-15f, 15f), 0f);
                            overhang.transform.localScale = new Vector3(Random.Range(6f, 12f), Random.Range(8f, 15f), Random.Range(7f, 13f));
                        }
                    }

                    // B. Flanking Anime Nature Pine Trees (10m to 36m from trail center)
                    for (int p = 0; p < 3; p++)
                    {
                        float treeDist = Random.Range(10.0f, 36.0f);
                        float jitterAlong = Random.Range(-8.0f, 8.0f);
                        Vector3 treeRay = new Vector3(
                            wx + perp.x * side * treeDist + tangent.x * jitterAlong,
                            600f,
                            wz + perp.y * side * treeDist + tangent.y * jitterAlong
                        );

                        if (Physics.Raycast(treeRay, Vector3.down, out RaycastHit tHit, 800f))
                        {
                            if (tHit.point.y < 19.5f) continue;

                            if (pinePrefabs.Count > 0)
                            {
                                GameObject treePf = pinePrefabs[Random.Range(0, pinePrefabs.Count)];
                                GameObject treeInst = (GameObject)PrefabUtility.InstantiatePrefab(treePf, root.transform);
                                treeInst.name = $"AnimeNature_TrailPine_{i:03d}_{p}_{(side < 0 ? "L" : "R")}";
                                treeInst.transform.position = tHit.point;
                                treeInst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                                float s = Random.Range(0.95f, 1.55f);
                                treeInst.transform.localScale = new Vector3(s, s * Random.Range(0.95f, 1.2f), s);
                            }

                            // C. Underbrush Bushes & Ferns at the base of the trees
                            if (bushPrefabs.Count > 0)
                            {
                                GameObject bushPf = bushPrefabs[Random.Range(0, bushPrefabs.Count)];
                                GameObject bushInst = (GameObject)PrefabUtility.InstantiatePrefab(bushPf, root.transform);
                                bushInst.name = $"AnimeNature_TrailBush_{i:03d}_{p}_{(side < 0 ? "L" : "R")}";
                                bushInst.transform.position = tHit.point + new Vector3(Random.Range(-2.2f, 2.2f), 0f, Random.Range(-2.2f, 2.2f));
                                bushInst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                                float bs = Random.Range(0.85f, 1.35f);
                                bushInst.transform.localScale = new Vector3(bs, bs, bs);
                            }
                        }
                    }
                }
            }

            // 3. Scatter River Gorge Boulders & Waterside Anime Nature Foliage along the Meandering River
            int riverSamples = 90;
            for (int rIdx = 0; rIdx < riverSamples; rIdx++)
            {
                float rz = Mathf.Lerp(540f, 2350f, rIdx / (float)(riverSamples - 1));
                float rx = Mathf.Sin(rz * 0.0032f) * 145f + Mathf.Cos(rz * 0.0075f) * 55f;

                for (int side = -1; side <= 1; side += 2)
                {
                    float bankOffset = Random.Range(38.0f, 72.0f);
                    Vector3 bankRay = new Vector3(rx + side * bankOffset, 600f, rz + Random.Range(-10f, 10f));

                    if (Physics.Raycast(bankRay, Vector3.down, out RaycastHit bHit, 800f))
                    {
                        // Gorge Bank Boulder
                        GameObject gorgeRock = InstantiateOrFallbackRock(rockPrefabs, root.transform, cliffMat);
                        gorgeRock.name = $"RiverGorge_BankBoulder_{rIdx:03d}_{(side < 0 ? "L" : "R")}";
                        gorgeRock.transform.position = bHit.point;
                        gorgeRock.transform.rotation = Quaternion.Euler(Random.Range(0f, 25f), Random.Range(0f, 360f), Random.Range(0f, 25f));
                        gorgeRock.transform.localScale = new Vector3(Random.Range(4.5f, 9.5f), Random.Range(3.5f, 7.5f), Random.Range(4.5f, 9.5f));

                        // Waterside Anime Nature Tree & Shrub
                        if (pinePrefabs.Count > 0 && rIdx % 2 == 0)
                        {
                            GameObject rTree = (GameObject)PrefabUtility.InstantiatePrefab(pinePrefabs[Random.Range(0, pinePrefabs.Count)], root.transform);
                            rTree.transform.position = bHit.point + new Vector3(side * 6.5f, 0f, 0f);
                            rTree.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                            float ts = Random.Range(0.9f, 1.4f);
                            rTree.transform.localScale = new Vector3(ts, ts, ts);
                        }
                    }
                }
            }

            SceneView.RepaintAll();
            Debug.Log($"[Antigravity MCP] Populated Mountain Switchbacks & River Gorge using {pinePrefabs.Count} Anime Nature tree variants, {bushPrefabs.Count} bush variants, and {rockPrefabs.Count} rock variants.");
        }

        private static GameObject InstantiateOrFallbackRock(List<GameObject> rockPrefabs, Transform parent, Material fallbackMat)
        {
            if (rockPrefabs != null && rockPrefabs.Count > 0)
            {
                GameObject pf = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
                return (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            }
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.transform.SetParent(parent, false);
            if (fallbackMat != null) rock.GetComponent<Renderer>().sharedMaterial = fallbackMat;
            return rock;
        }
    }
}
#endif
