using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    public static class CaptureWorldRefinementShowcases
    {
        private const string ARTIFACT_DIR = @"C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.tempmediaStorage";
        private const string ASSETS_DIR = @"Assets/Screenshots";

        [MenuItem("BattleSummoner/Capture World Refinement Showcases", false, 20)]
        public static void CaptureAllShowcases()
        {
            if (!Directory.Exists(ARTIFACT_DIR))
                Directory.CreateDirectory(ARTIFACT_DIR);
            if (!Directory.Exists(ASSETS_DIR))
                Directory.CreateDirectory(ASSETS_DIR);

            GameObject camObj = GameObject.Find("Screenshot_Camera_Overhaul");
            if (camObj == null)
            {
                camObj = new GameObject("Screenshot_Camera_Overhaul");
            }

            Camera cam = camObj.GetComponent<Camera>();
            if (cam == null) cam = camObj.AddComponent<Camera>();

            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1200f;
            cam.fieldOfView = 55f;

            // Make sure particle motes are simulated and visible
            var motes = GameObject.Find("Ambient_Glowing_Motes_System");
            ParticleSystem ps = null;
            if (motes != null)
            {
                ps = motes.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Simulate(5.0f, true, true);
                }
            }

            // Define Shot setups
            var shots = new (string name, Vector3 pos, Vector3 rot, float fov, Vector3? playerMovePos)[]
            {
                // 1. Central Meshy Gothic Cathedral & Plaza
                (
                    "Showcase_01_MeshyCathedral_And_KingdomPlaza",
                    new Vector3(0f, 42.0f, -48.0f),
                    new Vector3(18f, 0f, 0f),
                    58f,
                    new Vector3(0f, 16.2f, -22f)
                ),
                // 2. Lively Commercial Town District (Shops, Props, NPCs)
                (
                    "Showcase_02_LivelyTown_Shops_And_NPCs",
                    new Vector3(24f, 21.0f, -40.0f),
                    new Vector3(14f, 42f, 0f),
                    52f,
                    new Vector3(32f, 16.2f, -22f)
                ),
                // 3. Flora Overhaul (Anime Trees, Wind Grass, Flower Clusters)
                (
                    "Showcase_03_Flora_AnimeTrees_And_Flowers",
                    new Vector3(120f, 22.0f, 100f),
                    new Vector3(12f, 45f, 0f),
                    55f,
                    new Vector3(135f, 18.0f, 115f)
                ),
                // 4. Floating Yellow-White Glowing Particles around Player
                (
                    "Showcase_04_FloatingGlowingMotes_AroundHero",
                    new Vector3(2.5f, 17.8f, -27.5f),
                    new Vector3(8f, -165f, 0f),
                    48f,
                    new Vector3(0f, 16.2f, -22f)
                ),
                // 5. Volcano Strict Barren Exclusion Overlook
                (
                    "Showcase_05_MagioVolcano_BarrenExclusion",
                    new Vector3(-160f, 75.0f, 150f),
                    new Vector3(18f, -48f, 0f),
                    60f,
                    new Vector3(-160f, 70f, 150f)
                ),
                // 6. Fixed Bridges (Citadel Viaducts & Seaward Dockcity High Arch)
                (
                    "Showcase_06_FixedBridges_HighArchAndViaducts",
                    new Vector3(180f, 110.0f, -90f),
                    new Vector3(30f, -45f, 0f),
                    58f,
                    new Vector3(0f, 16.2f, 0f)
                )
            };

            var player = GameObject.Find("Player_Hero");

            foreach (var s in shots)
            {
                if (s.playerMovePos.HasValue && player != null)
                {
                    player.transform.position = s.playerMovePos.Value;
                    if (motes != null && ps != null)
                    {
                        motes.transform.position = s.playerMovePos.Value + Vector3.up * 1.5f;
                        ps.Simulate(3.0f, true, true);
                    }
                }

                cam.transform.position = s.pos;
                cam.transform.eulerAngles = s.rot;
                cam.fieldOfView = s.fov;

                RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                Texture2D screenShot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

                cam.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                screenShot.Apply();

                cam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);

                byte[] bytes = screenShot.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(screenShot);

                string artifactPath = Path.Combine(ARTIFACT_DIR, s.name + ".png");
                string assetPath = Path.Combine(ASSETS_DIR, s.name + ".png");

                File.WriteAllBytes(artifactPath, bytes);
                File.WriteAllBytes(assetPath, bytes);

                Debug.Log($"Captured shot: {s.name}");
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=green>All 6 showcase screenshots rendered and saved!</color>");
        }
    }
}
