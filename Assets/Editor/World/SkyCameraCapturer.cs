#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace BattleSummoner.EditorScripts
{
    public static class SkyCameraCapturer
    {
        [MenuItem("BattleSummoner/World/Capture Sky Views")]
        public static void CaptureSky()
        {
            string artifactDir = @"C:\Users\jedij\.gemini\antigravity-ide\brain\eac88545-145c-4283-95b5-caa5fd3c32f6\.tempmediaStorage";
            if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

            // Create temporary camera
            GameObject camGo = new GameObject("Sky_Inspection_Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 4500f;

            // View 1: Standing in Citadel Acropolis plaza East terrace looking up towards the sky archipelago (NE)
            CaptureView(cam, new Vector3(45f, 82f, 0f), Quaternion.Euler(-22f, 55f, 0f), Path.Combine(artifactDir, "SkyView_FromCitadel_NE_Fixed.png"));

            // View 2: Standing in Citadel Acropolis plaza West terrace looking up SW
            CaptureView(cam, new Vector3(-45f, 82f, 0f), Quaternion.Euler(-22f, 235f, 0f), Path.Combine(artifactDir, "SkyView_FromCitadel_SW_Fixed.png"));

            // View 3: High overview looking down/across at the sky islands layer (Y = 650m)
            CaptureView(cam, new Vector3(0f, 650f, -800f), Quaternion.Euler(20f, 0f, 0f), Path.Combine(artifactDir, "SkyView_HighOverview_North_Fixed.png"));

            // View 4: Cinematic close-up framing Grand Celestial Island 01 (pos=(480, 440, 260))
            CaptureView(cam, new Vector3(380f, 475f, 160f), Quaternion.Euler(14f, 45f, 0f), Path.Combine(artifactDir, "SkyView_GrandIsland_CloseUp.png"));

            Object.DestroyImmediate(camGo);
            Debug.Log("Captured 4 refined sky inspection views to artifact directory.");
        }

        private static void CaptureView(Camera cam, Vector3 pos, Quaternion rot, string filePath)
        {
            cam.transform.position = pos;
            cam.transform.rotation = rot;

            RenderTexture rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(filePath, tex.EncodeToPNG());

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
#endif
