using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    public static class ShowcaseCameraRig
    {
        private const string ARTIFACT_DIR = @"C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.tempmediaStorage";

        public static string RenderShot(string shotName, Vector3 camPos, Vector3 lookAtPos, float fov = 50f)
        {
            var camObj = GameObject.Find("Screenshot_Camera_Overhaul");
            if (camObj == null) camObj = new GameObject("Screenshot_Camera_Overhaul");
            var cam = camObj.GetComponent<Camera>();
            if (cam == null) cam = camObj.AddComponent<Camera>();

            cam.transform.position = camPos;
            cam.transform.LookAt(lookAtPos);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1500f;

            // Warm up particles if nearby
            var motes = GameObject.Find("Ambient_Glowing_Motes_System");
            if (motes != null)
            {
                var ps = motes.GetComponent<ParticleSystem>();
                if (ps != null) ps.Simulate(3.0f, true, true);
            }

            RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string outPath = Path.Combine(ARTIFACT_DIR, shotName + ".png");
            File.WriteAllBytes(outPath, bytes);

            return $"Rendered {shotName}: {bytes.Length} bytes to {outPath}";
        }
    }
}
