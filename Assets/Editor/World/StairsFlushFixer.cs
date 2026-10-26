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
            Debug.Log($"<color=cyan><b>[StairsFlushFixer] Starting comprehensive stairs flush remediation in: {activeScene.name}</b></color>");

            // Load Materials
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_TimberWood.mat");
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CityStoneCurb.mat");
            if (stoneMat == null) stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Metropolis/M_Metro_StoneBase.mat");
            if (woodMat == null) woodMat = stoneMat;

            Material roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CityCobbleRoad.mat");
            if (roadMat == null) roadMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CityCobbleRoad_Detailed.mat");

            // -----------------------------------------------------------------
            // 1. DESTROY ALL FLOATING PATH BRIDGES (Road_Radial_*_R86_to_R95)
            // -----------------------------------------------------------------
            int destroyedCount = 0;
            var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go.name.Contains("R86_to_R95") || go.name.Contains("FloatingBridge") || (go.name.Contains("Road_Radial") && go.name.Contains("R86")))
                {
                    Debug.Log($"[StairsFlushFixer] Destroying floating bridge: {go.name}");
                    Undo.DestroyObjectImmediate(go);
                    destroyedCount++;
                }
            }
            Debug.Log($"<color=yellow>[StairsFlushFixer] Removed {destroyedCount} floating path bridges.</color>");

            // -----------------------------------------------------------------
            // 2. EXTEND PLATFORM ROADBED (R = 86m to 95m) ON TIER 4 SO FLOOR REACHES PLATFORM EDGE
            // -----------------------------------------------------------------
            // This ensures the platform floor itself extends fully to R = 95.0m,
            // meeting the retaining wall and stairs landing flush with solid cobblestone!
            float[] radialAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            float boulevardWidth = 14.0f;
            float curbWidth = 1.5f;

            var roadwaysParent = GameObject.Find("01_Cobblestone_Roadways")?.transform;
            if (roadwaysParent == null)
            {
                var metroRoot = GameObject.Find("8Vector_Metropolis_Root") ?? GameObject.Find("Metropolis_Street_Infrastructure_Root");
                if (metroRoot != null)
                {
                    roadwaysParent = new GameObject("01_Cobblestone_Roadways").transform;
                    roadwaysParent.SetParent(metroRoot.transform, false);
                }
            }

            // -----------------------------------------------------------------
            // 3. REBUILD ALL 8 T4_TO_T3 STAIRWAYS COMPLETELY FLUSH TO PLATFORM (R = 95m)
            // -----------------------------------------------------------------
            // Geometry:
            // Platform edge: R = 95.0m, Y = 80.06m
            // Lower road: R = 125.0m, Y = 58.06m
            // Total length: 30.0m
            // Center: dir * 110.0m
            // Step count: 28 steps
            // Balustrade horizontal span: exactly 30.0m (slanted length = 30 / cos(36.25°) = 37.2m)
            // Solid side walls & foundations extending deep into lower ground
            var stairsParentGo = GameObject.Find("02_Stone_Tier_Stairs");
            Transform stairsParent = stairsParentGo != null ? stairsParentGo.transform : null;
            if (stairsParent == null)
            {
                Debug.LogError("[StairsFlushFixer] 02_Stone_Tier_Stairs container not found!");
                return;
            }

            foreach (float angDeg in radialAngles)
            {
                float angRad = angDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angRad), 0f, Mathf.Cos(angRad));
                Vector3 perp = new Vector3(dir.z, 0f, -dir.x);

                // Rebuild / ensure platform road extension from R = 86m to 95m
                if (roadwaysParent != null && roadMat != null)
                {
                    string extName = $"Platform_Floor_Extension_{angDeg:F0}_R86_to_R95";
                    var existingExt = roadwaysParent.Find(extName);
                    if (existingExt != null) Undo.DestroyObjectImmediate(existingExt.gameObject);

                    Vector3 centerExt = dir * 90.5f + Vector3.up * 80.06f;
                    CreateSolidPlatformRoadbed(
                        roadwaysParent,
                        extName,
                        centerExt, dir, perp,
                        9.0f, boulevardWidth,
                        roadMat, stoneMat
                    );
                }

                // Rebuild Stairs flush
                string stairsName = $"Stairs_{angDeg:F0}_T4_to_T3";
                var oldStairs = stairsParent.Find(stairsName);
                if (oldStairs != null) Undo.DestroyObjectImmediate(oldStairs.gameObject);

                CreateGrandFlushStairs(
                    stairsParent,
                    stairsName,
                    dir, perp,
                    95.0f, 125.0f, // rTop, rBottom
                    80.06f, 58.06f, // yTop, yBottom
                    boulevardWidth,
                    28,
                    woodMat
                );
            }

            EditorUtility.SetDirty(stairsParentGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=green><b>[StairsFlushFixer] Complete! All 8 stairways are flush against the platform, solid floor extends to R=95m, and 0 floating bridges exist.</b></color>");
        }

        private static void CreateSolidPlatformRoadbed(
            Transform parent, string name,
            Vector3 center, Vector3 forward, Vector3 right,
            float length, float width,
            Material roadMat, Material curbMat)
        {
            // Create a solid platform slab (not a paper-thin quad!) with thickness 0.6m
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetParent(parent, false);
            slab.transform.position = center - Vector3.up * 0.25f;
            slab.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            slab.transform.localScale = new Vector3(width, 0.5f, length);
            slab.GetComponent<MeshRenderer>().sharedMaterial = roadMat;

            // Stone Curbs on left and right
            float curbWidth = 1.2f;
            float curbHeight = 0.35f;
            float sideOffset = (width * 0.5f) - (curbWidth * 0.5f);

            GameObject curbL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbL.name = "StoneCurb_Left";
            curbL.transform.SetParent(slab.transform, false);
            curbL.transform.localPosition = new Vector3(-sideOffset / width * width, 0.25f, 0f);
            curbL.transform.localScale = new Vector3(curbWidth / width, (curbHeight + 0.5f) / 0.5f, 1f);
            curbL.GetComponent<MeshRenderer>().sharedMaterial = curbMat;

            GameObject curbR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curbR.name = "StoneCurb_Right";
            curbR.transform.SetParent(slab.transform, false);
            curbR.transform.localPosition = new Vector3(sideOffset / width * width, 0.25f, 0f);
            curbR.transform.localScale = new Vector3(curbWidth / width, (curbHeight + 0.5f) / 0.5f, 1f);
            curbR.GetComponent<MeshRenderer>().sharedMaterial = curbMat;
        }

        private static void CreateGrandFlushStairs(
            Transform parent, string name,
            Vector3 forward, Vector3 right,
            float rTop, float rBottom,
            float yTop, float yBottom,
            float width, int stepCount, Material mat)
        {
            float length = rBottom - rTop; // 30.0f
            float stepLength = length / stepCount; // 1.071f
            float stepHeight = (yTop - yBottom) / stepCount; // 0.7857f
            float slopeAngle = Mathf.Atan2(yTop - yBottom, length) * Mathf.Rad2Deg; // 36.25 deg

            GameObject stairsGroup = new GameObject(name);
            stairsGroup.transform.SetParent(parent, false);

            // 1. Build all 28 solid steps from bottom (rBottom) to top (rTop)
            for (int i = 0; i < stepCount; i++)
            {
                // i = 0 is bottom step (near rBottom), i = stepCount - 1 is top step (meeting rTop)
                float t = (float)i / stepCount;
                float r = Mathf.Lerp(rBottom, rTop, t + (0.5f / stepCount));
                float stepTopY = yBottom + (i + 1) * stepHeight;

                // Foundation reaches all the way down to lower floor foundation (yBottom - 2.0m)
                float foundationBottomY = yBottom - 2.0f;
                float fullHeight = stepTopY - foundationBottomY;
                float centerY = (stepTopY + foundationBottomY) * 0.5f;

                Vector3 stepPos = forward * r;
                stepPos.y = centerY;

                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i}";
                step.transform.SetParent(stairsGroup.transform, false);
                step.transform.position = stepPos;
                step.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                step.transform.localScale = new Vector3(width, fullHeight, stepLength * 1.15f);
                step.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            // 2. Solid Top Landing Platform (Flush at rTop = 95.0m, embeds 1.5m into platform to r = 93.5m)
            // This ensures 100% zero-gap seamless transition between stairs and platform floor
            float landingThickness = 0.6f;
            Vector3 landingPos = forward * (rTop - 0.75f) + Vector3.up * (yTop - landingThickness * 0.5f);
            GameObject topLanding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topLanding.name = "Top_Landing_Threshold";
            topLanding.transform.SetParent(stairsGroup.transform, false);
            topLanding.transform.position = landingPos;
            topLanding.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            topLanding.transform.localScale = new Vector3(width, landingThickness, 2.5f);
            topLanding.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 3. Solid Foundation Wedge underneath the Top Landing
            // Fills any gap between the top steps and the retaining wall
            Vector3 wedgePos = forward * (rTop - 0.5f) + Vector3.up * ((yTop + yBottom) * 0.5f);
            GameObject wedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wedge.name = "Top_Foundation_Abutment";
            wedge.transform.SetParent(stairsGroup.transform, false);
            wedge.transform.position = wedgePos;
            wedge.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            wedge.transform.localScale = new Vector3(width, (yTop - yBottom) + 2.0f, 2.5f);
            wedge.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 4. Balustrades along Left and Right
            // Slanted length exactly spans from rBottom to rTop
            float cosAngle = Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
            float slantedLength = (length / cosAngle) + 1.2f;
            float balustradeWidth = 0.85f;
            float balustradeHeight = 1.4f;

            float midR = (rTop + rBottom) * 0.5f;
            float midY = (yTop + yBottom) * 0.5f;
            Vector3 center = forward * midR;

            // Left Balustrade
            Vector3 leftBPos = center - right * (width * 0.5f + balustradeWidth * 0.5f) + Vector3.up * (midY + balustradeHeight * 0.5f - 0.5f);
            GameObject leftB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftB.name = "Balustrade_Left";
            leftB.transform.SetParent(stairsGroup.transform, false);
            leftB.transform.position = leftBPos;
            leftB.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(slopeAngle, 0f, 0f);
            leftB.transform.localScale = new Vector3(balustradeWidth, balustradeHeight + 3.0f, slantedLength);
            leftB.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Right Balustrade
            Vector3 rightBPos = center + right * (width * 0.5f + balustradeWidth * 0.5f) + Vector3.up * (midY + balustradeHeight * 0.5f - 0.5f);
            GameObject rightB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightB.name = "Balustrade_Right";
            rightB.transform.SetParent(stairsGroup.transform, false);
            rightB.transform.position = rightBPos;
            rightB.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(slopeAngle, 0f, 0f);
            rightB.transform.localScale = new Vector3(balustradeWidth, balustradeHeight + 3.0f, slantedLength);
            rightB.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 5. Decorative Newel Piers at Top (rTop) and Bottom (rBottom)
            for (int side = -1; side <= 1; side += 2)
            {
                // Top Pier at platform edge
                Vector3 pierPosTop = forward * rTop + right * (side * (width * 0.5f + balustradeWidth * 0.5f)) + Vector3.up * (yTop + 0.75f);
                GameObject pierTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pierTop.name = side == -1 ? "Newel_Pier_Top_Left" : "Newel_Pier_Top_Right";
                pierTop.transform.SetParent(stairsGroup.transform, false);
                pierTop.transform.position = pierPosTop;
                pierTop.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                pierTop.transform.localScale = new Vector3(balustradeWidth * 1.4f, 1.8f, balustradeWidth * 1.4f);
                pierTop.GetComponent<MeshRenderer>().sharedMaterial = mat;

                // Bottom Pier at lower road edge
                Vector3 pierPosBtm = forward * rBottom + right * (side * (width * 0.5f + balustradeWidth * 0.5f)) + Vector3.up * (yBottom + 0.75f);
                GameObject pierBtm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pierBtm.name = side == -1 ? "Newel_Pier_Btm_Left" : "Newel_Pier_Btm_Right";
                pierBtm.transform.SetParent(stairsGroup.transform, false);
                pierBtm.transform.position = pierPosBtm;
                pierBtm.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                pierBtm.transform.localScale = new Vector3(balustradeWidth * 1.4f, 1.8f, balustradeWidth * 1.4f);
                pierBtm.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        [MenuItem("BattleSummoner/World/Capture Stairs Verification Screenshot")]
        public static void CaptureStairsView()
        {
            var cameraGo = new GameObject("TempVerificationCamera");
            var cam = cameraGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;

            // Frame Stairs_0_T4_to_T3 from elevated side perspective matching user view
            cameraGo.transform.position = new Vector3(-35f, 95f, 135f);
            cameraGo.transform.LookAt(new Vector3(0f, 75f, 95f));

            int width = 1280;
            int height = 800;
            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            string shotPath = "Assets/Screenshots/Stairs_Flush_Platform_Verification.png";
            System.IO.File.WriteAllBytes(shotPath, bytes);

            string brainPath = "C:/Users/jedij/.gemini/antigravity-ide/brain/eac88545-145c-4283-95b5-caa5fd3c32f6/.tempmediaStorage/Stairs_Flush_Platform_Verification.png";
            System.IO.File.WriteAllBytes(brainPath, bytes);

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(cameraGo);

            Debug.Log($"<color=green><b>[StairsFlushFixer] Captured verification screenshot to {shotPath} and {brainPath}</b></color>");
        }
    }
}
#endif
