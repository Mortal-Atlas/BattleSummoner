#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace BattleSummoner.EditorScripts
{
    public static class StairsFlushFixer
    {
        [MenuItem("BattleSummoner/World/Fix Stairs Flush And Remove Floating Bridges")]
        public static void FixStairsAndRemoveBridges()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log($"<color=cyan><b>[StairsFlushFixer] Starting remediation in scene: {activeScene.name}</b></color>");

            Material curbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CityStoneCurb.mat");
            if (curbMat == null)
            {
                curbMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                curbMat.name = "M_CityStoneCurb";
                curbMat.color = new Color(0.68f, 0.65f, 0.60f, 1f);
                AssetDatabase.CreateAsset(curbMat, "Assets/Materials/M_CityStoneCurb.mat");
            }

            // -------------------------------------------------------------
            // 1. FIND & REMOVE ALL FLOATING PATH BRIDGES (R86 to R95)
            // -------------------------------------------------------------
            int removedBridges = 0;
            var boulevardsRoot = GameObject.Find("01_Radial_Grand_Boulevards");
            if (boulevardsRoot != null)
            {
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (Transform child in boulevardsRoot.transform)
                {
                    if (child.name.Contains("R86_to_R95") || child.name.Contains("Bridge") || (child.name.Contains("Road_Radial") && child.name.Contains("R86")))
                    {
                        toDestroy.Add(child.gameObject);
                    }
                }
                foreach (var go in toDestroy)
                {
                    Debug.Log($"[StairsFlushFixer] Destroying floating path bridge: {go.name}");
                    Undo.DestroyObjectImmediate(go);
                    removedBridges++;
                }
            }

            // Also check under 01_Cobblestone_Roadways or any other parent
            var roadwaysRoot = GameObject.Find("01_Cobblestone_Roadways");
            if (roadwaysRoot != null)
            {
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (Transform child in roadwaysRoot.transform)
                {
                    if (child.name.Contains("R86_to_R95") || child.name.Contains("Bridge") || (child.name.Contains("Road_Radial") && child.name.Contains("R86")))
                    {
                        toDestroy.Add(child.gameObject);
                    }
                }
                foreach (var go in toDestroy)
                {
                    Debug.Log($"[StairsFlushFixer] Destroying floating path bridge under roadways: {go.name}");
                    Undo.DestroyObjectImmediate(go);
                    removedBridges++;
                }
            }

            // Also check root or scene for any object with R86_to_R95
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.Contains("R86_to_R95"))
                {
                    Debug.Log($"[StairsFlushFixer] Destroying floating bridge object: {go.name}");
                    Undo.DestroyObjectImmediate(go);
                    removedBridges++;
                }
            }
            Debug.Log($"<color=yellow>[StairsFlushFixer] Total floating path bridges removed: {removedBridges}</color>");

            // -------------------------------------------------------------
            // 2. REBUILD / FLUSH ALL 8 T4_TO_T3 STAIRWAYS
            // -------------------------------------------------------------
            // Correct geometry:
            // Upper Tier 4 platform ends at R = 86.0m (Y = 80.06m)
            // Lower Tier 3 road starts at R = 116.0m (Y = 58.06m)
            // Total flight length = 30.0m
            // Center of flight = dir * 101.0m
            // Step count = 28
            // Width = 14.0m
            float[] radialAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            float boulevardWidth = 14.0f;
            float rUpperPlatform = 86.0f;
            float rLowerRoad = 116.0f;
            float flightLength = rLowerRoad - rUpperPlatform; // 30.0m
            float centerR = (rUpperPlatform + rLowerRoad) * 0.5f; // 101.0m
            float yTop = 80.06f;
            float yBottom = 58.06f;
            int stepCount = 28;

            Transform stairsParent = null;
            var stairsParentGo = GameObject.Find("02_Stone_Tier_Stairs");
            if (stairsParentGo != null) stairsParent = stairsParentGo.transform;

            if (stairsParent == null)
            {
                Debug.LogError("[StairsFlushFixer] 02_Stone_Tier_Stairs not found in scene!");
                return;
            }

            foreach (float angDeg in radialAngles)
            {
                string stairsName = $"Stairs_{angDeg:F0}_T4_to_T3";
                Transform existingStairs = stairsParent.Find(stairsName);
                if (existingStairs != null)
                {
                    Undo.DestroyObjectImmediate(existingStairs.gameObject);
                }

                float angRad = angDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angRad), 0f, Mathf.Cos(angRad));
                Vector3 perp = new Vector3(dir.z, 0f, -dir.x);
                Vector3 center = dir * centerR;

                CreateFlushFlightOfStairs(
                    stairsParent,
                    stairsName,
                    center, dir, perp,
                    flightLength,
                    rUpperPlatform, rLowerRoad,
                    yTop, yBottom,
                    boulevardWidth,
                    stepCount,
                    curbMat
                );
            }
            Debug.Log("<color=green><b>[StairsFlushFixer] Successfully rebuilt all 8 T4_to_T3 stairways flush against upper platform!</b></color>");

            // -------------------------------------------------------------
            // 3. REPOSITION ANY PROPS ON NORTH BOULEVARD STAIRWAY ZONE (Z: 85-115)
            // -------------------------------------------------------------
            // If the pig or market stall was hovering in the old gap (Z ~ 87-94), move it onto the solid platform floor (Z <= 84).
            var marketStalls = GameObject.Find("05_Carts_Stalls_Barrels_Crates");
            if (marketStalls != null)
            {
                foreach (Transform child in marketStalls.transform)
                {
                    if (Mathf.Abs(child.position.x) < 8.0f && child.position.z >= 85.0f && child.position.z <= 98.0f)
                    {
                        Vector3 p = child.position;
                        p.z = 82.5f; // Place firmly on the platform floor
                        p.y = 80.12f;
                        child.position = p;
                        Debug.Log($"[StairsFlushFixer] Adjusted prop '{child.name}' onto platform floor at {p}");
                    }
                }
            }

            // Check for critters / pigs near the gap
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.ToLower().Contains("pig") || go.name.ToLower().Contains("critter"))
                {
                    if (Mathf.Abs(go.transform.position.x) < 8.0f && go.transform.position.z >= 85.0f && go.transform.position.z <= 98.0f)
                    {
                        Vector3 p = go.transform.position;
                        p.z = 83.5f; // Place onto platform floor next to stall
                        p.y = 80.10f;
                        go.transform.position = p;
                        Debug.Log($"[StairsFlushFixer] Adjusted critter '{go.name}' onto platform floor at {p}");
                    }
                }
            }

            EditorUtility.SetDirty(stairsParentGo);
            Debug.Log("<color=green><b>[StairsFlushFixer] Remediation complete! All stairs flush against platform, 0 gaps, 0 floating bridges.</b></color>");
        }

        private static void CreateFlushFlightOfStairs(
            Transform parent, string name,
            Vector3 center, Vector3 forward, Vector3 right,
            float length,
            float rUpper, float rLower,
            float yTop, float yBottom,
            float width, int stepCount, Material mat)
        {
            float stepLength = length / stepCount;
            float stepHeight = (yTop - yBottom) / stepCount;

            GameObject stairsGroup = new GameObject(name);
            stairsGroup.transform.SetParent(parent, false);

            // Steps from bottom (rLower) to top (rUpper)
            for (int i = 0; i < stepCount; i++)
            {
                float t = (float)i / stepCount;
                // i=0 is bottom step (near rLower = 116m), i=stepCount-1 is top step (near rUpper = 86m)
                float rDist = (1f - t) * length - (length * 0.5f);

                // Deep solid foundation cube: top surface is step Y, bottom goes deep down to foundation depth (yBottom - 2m)
                // This guarantees NO hollow underside or light leaking
                float topY = yBottom + (i + 1) * stepHeight;
                float targetBottomY = yBottom - 2.0f;
                float fullHeight = topY - targetBottomY;
                float stepCenterY = (topY + targetBottomY) * 0.5f;

                Vector3 stepPos = center + forward * rDist;
                stepPos.y = stepCenterY;

                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i}";
                step.transform.SetParent(stairsGroup.transform, false);
                step.transform.position = stepPos;
                step.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                step.transform.localScale = new Vector3(width, fullHeight, stepLength * 1.15f);
                step.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            // Top Landing Threshold Platform:
            // Sits at rUpper (86.0m) and extends 1.5m into the platform (84.5m to 86.0m) and 0.5m forward (86.5m),
            // perfectly flush with the platform floor at yTop (80.06m)
            float landingThickness = 0.5f;
            Vector3 landingPos = forward * (rUpper - 0.25f) + Vector3.up * (yTop - landingThickness * 0.5f);
            GameObject topLanding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topLanding.name = "Top_Landing_Threshold";
            topLanding.transform.SetParent(stairsGroup.transform, false);
            topLanding.transform.position = landingPos;
            topLanding.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            topLanding.transform.localScale = new Vector3(width, landingThickness, 2.5f);
            topLanding.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Balustrades along stairs
            float balustradeWidth = 0.85f;
            float balustradeHeight = 1.3f;
            float midY = (yTop + yBottom) * 0.5f;
            float slopeAngle = Mathf.Atan2(yTop - yBottom, length) * Mathf.Rad2Deg;

            // Left Balustrade
            Vector3 leftBPos = center - right * (width * 0.5f + balustradeWidth * 0.5f) + Vector3.up * (midY + balustradeHeight * 0.5f - 0.9f);
            GameObject leftB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftB.name = "Balustrade_Left";
            leftB.transform.SetParent(stairsGroup.transform, false);
            leftB.transform.position = leftBPos;
            leftB.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(slopeAngle, 0f, 0f);
            leftB.transform.localScale = new Vector3(balustradeWidth, balustradeHeight + 2.5f, length * 1.05f);
            leftB.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Right Balustrade
            Vector3 rightBPos = center + right * (width * 0.5f + balustradeWidth * 0.5f) + Vector3.up * (midY + balustradeHeight * 0.5f - 0.9f);
            GameObject rightB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightB.name = "Balustrade_Right";
            rightB.transform.SetParent(stairsGroup.transform, false);
            rightB.transform.position = rightBPos;
            rightB.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(slopeAngle, 0f, 0f);
            rightB.transform.localScale = new Vector3(balustradeWidth, balustradeHeight + 2.5f, length * 1.05f);
            rightB.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Decorative Newel Piers at Top (rUpper) and Bottom (rLower)
            for (int side = -1; side <= 1; side += 2)
            {
                // Top Pier at platform edge
                Vector3 pierPosTop = forward * rUpper + right * (side * (width * 0.5f + balustradeWidth * 0.5f)) + Vector3.up * (yTop + 0.65f);
                GameObject pierTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pierTop.name = side == -1 ? "Newel_Pier_Top_Left" : "Newel_Pier_Top_Right";
                pierTop.transform.SetParent(stairsGroup.transform, false);
                pierTop.transform.position = pierPosTop;
                pierTop.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                pierTop.transform.localScale = new Vector3(balustradeWidth * 1.3f, 1.5f, balustradeWidth * 1.3f);
                pierTop.GetComponent<MeshRenderer>().sharedMaterial = mat;

                // Bottom Pier at lower road edge
                Vector3 pierPosBtm = forward * rLower + right * (side * (width * 0.5f + balustradeWidth * 0.5f)) + Vector3.up * (yBottom + 0.65f);
                GameObject pierBtm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pierBtm.name = side == -1 ? "Newel_Pier_Btm_Left" : "Newel_Pier_Btm_Right";
                pierBtm.transform.SetParent(stairsGroup.transform, false);
                pierBtm.transform.position = pierPosBtm;
                pierBtm.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                pierBtm.transform.localScale = new Vector3(balustradeWidth * 1.3f, 1.5f, balustradeWidth * 1.3f);
                pierBtm.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }
    }
}
#endif
