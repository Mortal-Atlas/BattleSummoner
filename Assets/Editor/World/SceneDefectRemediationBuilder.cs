#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.WorldGen
{
    public static class SceneDefectRemediationBuilder
    {
        [MenuItem("BattleSummoner/World/Fix Screenshot Defects (Moat, Retaining Walls, Foliage & Sky Islands)")]
        public static void ExecuteFullSceneRemediation()
        {
            BuildMoatAndTierRetainingWalls();
            BuildPerimeterWallAnd16Towers();
            PopulateAnimeNatureFoliage();
            SpawnSkyIslandsLayer();
            SceneView.RepaintAll();
            Debug.Log("[Antigravity Remediation] Moat, Retaining Walls, 16 Towers, Anime Nature Foliage, and Sky Islands generated.");
        }

        public static void BuildMoatAndTierRetainingWalls()
        {
            GameObject root = GameObject.Find("City_Structural_Masonry");
            if (root != null) Undo.DestroyObjectImmediate(root);
            root = new GameObject("City_Structural_Masonry");

            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            if (stoneMat == null) stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Metro_StoneBase.mat");

            // Moat water is unified with Outerwilds_Ocean_Water at Y = 18.0m to prevent duplicate surface Z-fighting
            var existingRing = root.transform.Find("Water_ConcentricMoat_Ring");
            if (existingRing != null) Undo.DestroyObjectImmediate(existingRing.gameObject);

            var oceanObj = GameObject.Find("Outerwilds_Ocean_Water");
            if (oceanObj != null)
            {
                oceanObj.transform.position = new Vector3(0f, 18.0f, 0f);
            }

            // 2. Cut-Stone Retaining Wall Collars + Cobblestone Plazas covering raw dirt tiers
            float[] radii = { 450f, 310f, 195f, 95f };
            float[] heights = { 22f, 38f, 58f, 80f };

            for (int i = 0; i < 4; i++)
            {
                GameObject tierDrum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tierDrum.name = $"City_Tier{i + 1}_MasonryRetainingDrum";
                tierDrum.transform.SetParent(root.transform, false);
                float h = heights[i];
                tierDrum.transform.position = new Vector3(0f, h * 0.5f, 0f);
                tierDrum.transform.localScale = new Vector3(radii[i] * 2f, h * 0.5f, radii[i] * 2f);
                if (stoneMat != null) tierDrum.GetComponent<Renderer>().sharedMaterial = stoneMat;
            }
        }

        public static void BuildPerimeterWallAnd16Towers()
        {
            GameObject wallRoot = GameObject.Find("City_Perimeter_16Towers");
            if (wallRoot != null) Undo.DestroyObjectImmediate(wallRoot);
            wallRoot = new GameObject("City_Perimeter_16Towers");

            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            if (stoneMat == null) stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Metro_StoneBase.mat");

            float wallRadius = 446f;
            float baseHeight = 22f;

            for (int i = 0; i < 16; i++)
            {
                float angleRad = i * (Mathf.PI * 2f / 16f);
                Vector3 pos = new Vector3(Mathf.Cos(angleRad) * wallRadius, baseHeight + 12f, Mathf.Sin(angleRad) * wallRadius);

                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = (i % 2 == 0) ? $"Gatehouse_Bastion_{i}" : $"ManaShield_Tower_{i}";
                tower.transform.SetParent(wallRoot.transform, false);
                tower.transform.position = pos;
                tower.transform.localScale = new Vector3(18f, 14f, 18f);
                if (stoneMat != null) tower.GetComponent<Renderer>().sharedMaterial = stoneMat;

                // Curtain wall segment connecting to next tower
                float nextAngle = (i + 1) * (Mathf.PI * 2f / 16f);
                Vector3 nextPos = new Vector3(Mathf.Cos(nextAngle) * wallRadius, baseHeight + 12f, Mathf.Sin(nextAngle) * wallRadius);
                Vector3 midPoint = (pos + nextPos) * 0.5f;
                float segLen = Vector3.Distance(pos, nextPos);

                GameObject wallSeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallSeg.name = $"CurtainWall_Segment_{i}";
                wallSeg.transform.SetParent(wallRoot.transform, false);
                wallSeg.transform.position = new Vector3(midPoint.x, baseHeight + 7f, midPoint.z);
                wallSeg.transform.rotation = Quaternion.LookRotation(nextPos - pos);
                wallSeg.transform.localScale = new Vector3(4.5f, 14f, segLen);
                if (stoneMat != null) wallSeg.GetComponent<Renderer>().sharedMaterial = stoneMat;
            }
        }

        public static void PopulateAnimeNatureFoliage()
        {
            List<GameObject> treePrefabs = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).ToLowerInvariant();
                if ((path.Contains("anime") || path.Contains("nature") || path.Contains("tree")) &&
                    !path.Contains("lamp") && !path.Contains("house") && !path.Contains("prop"))
                {
                    GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (pf != null && (pf.name.Contains("Tree") || pf.name.Contains("Pine") || pf.name.Contains("Conifer")))
                        treePrefabs.Add(pf);
                }
            }

            if (treePrefabs.Count == 0) return;

            GameObject foliageRoot = GameObject.Find("World_AnimeNature_Foliage");
            if (foliageRoot != null) Undo.DestroyObjectImmediate(foliageRoot);
            foliageRoot = new GameObject("World_AnimeNature_Foliage");

            Random.InitState(42);
            int spawned = 0;
            for (int i = 0; i < 1800; i++)
            {
                float r = Random.Range(565f, 2450f); // Strictly outside Moat (R > 565m)
                float theta = Random.Range(0f, Mathf.PI * 2f);
                Vector3 candidate = new Vector3(Mathf.Cos(theta) * r, 400f, Mathf.Sin(theta) * r);

                if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 500f))
                {
                    if (hit.point.y < 19f || Vector3.Angle(hit.normal, Vector3.up) > 32f) continue;
                    GameObject sampleTree = treePrefabs[Random.Range(0, treePrefabs.Count)];
                    GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(sampleTree, foliageRoot.transform);
                    inst.transform.position = hit.point;
                    inst.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    float s = Random.Range(0.85f, 1.35f);
                    inst.transform.localScale = new Vector3(s, s, s);
                    spawned++;
                }
            }
            Debug.Log($"[Antigravity Remediation] Spawned {spawned} trees across outer biomes.");
        }

        public static void SpawnSkyIslandsLayer()
        {
            GameObject skyRoot = GameObject.Find("Sky_Layer_Aetherion");
            if (skyRoot != null) Undo.DestroyObjectImmediate(skyRoot);
            skyRoot = new GameObject("Sky_Layer_Aetherion");

            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            if (stoneMat == null) stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Metro_StoneBase.mat");

            for (int i = 0; i < 12; i++)
            {
                float angle = i * (Mathf.PI * 2f / 12f);
                float dist = Mathf.Lerp(280f, 1150f, i / 11f);
                float alt = Mathf.Lerp(380f, 760f, i / 11f);

                GameObject island = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                island.name = $"SkyIsland_SteppingStone_{i + 1:D2}";
                island.transform.SetParent(skyRoot.transform, false);
                island.transform.position = new Vector3(Mathf.Cos(angle) * dist, alt, Mathf.Sin(angle) * dist);
                island.transform.localScale = new Vector3(95f, 28f, 95f);
                if (stoneMat != null) island.GetComponent<Renderer>().sharedMaterial = stoneMat;
            }
        }
    }
}
#endif
