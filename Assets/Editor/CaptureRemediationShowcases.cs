#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    public static class CaptureRemediationShowcases
    {
        private const string ARTIFACT_DIR = @"C:\Users\jedij\.gemini\antigravity-ide\brain\ae46c5bb-f24e-4f24-b773-3b21ea6f55c0\.tempmediaStorage";
        private const string ASSETS_DIR = @"Assets/Screenshots";

        [MenuItem("BattleSummoner/Capture Remediation Showcases", false, 25)]
        public static void CaptureAll()
        {
            if (!Directory.Exists(ARTIFACT_DIR)) Directory.CreateDirectory(ARTIFACT_DIR);
            if (!Directory.Exists(ASSETS_DIR)) Directory.CreateDirectory(ASSETS_DIR);

            GameObject camObj = GameObject.Find("Screenshot_Camera_Remediation");
            if (camObj == null) camObj = new GameObject("Screenshot_Camera_Remediation");

            Camera cam = camObj.GetComponent<Camera>();
            if (cam == null) cam = camObj.AddComponent<Camera>();

            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 12000f;

            // Pre-capture scene polish: disable legacy placeholder primitives and ensure cathedral / hero alignment
            var citadel = GameObject.Find("Concentric_Citadel_Root");
            if (citadel != null)
            {
                for (int i = 0; i < citadel.transform.childCount; i++)
                {
                    var ch = citadel.transform.GetChild(i);
                    if (ch.name.StartsWith("Citadel_House") ||
                        ch.name.Contains("Keep") ||
                        ch.name.Contains("Spire") ||
                        ch.name.StartsWith("Citadel_Gatehouse") ||
                        ch.name.StartsWith("Citadel_Viaduct") ||
                        ch.name.StartsWith("Viaduct_Pillar") ||
                        ch.name == "Citadel_Inner_Wall" ||
                        ch.name == "Citadel_Outer_Wall")
                    {
                        ch.gameObject.SetActive(false);
                    }
                }
            }

            var combat = GameObject.Find("Combat_Encounters_Root");
            if (combat != null) combat.SetActive(false);

            foreach (var g in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (g != null && (g.name.Contains("Genshin") || g.name.Contains("PracticeDummy")))
                {
                    g.SetActive(false);
                }
            }

            var cath = GameObject.Find("Landmark_CathedralGothic_OpenDoors");
            if (cath != null)
            {
                cath.transform.position = new Vector3(0f, 80f, 0f);
                cath.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cath.SetActive(true);
            }

            var hero = GameObject.Find("Player_Hero");
            if (hero != null)
            {
                hero.transform.position = new Vector3(2f, 80.5f, 25f);
                hero.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            var motes = GameObject.Find("Ambient_Glowing_Motes_System");
            if (motes != null) motes.transform.position = new Vector3(2f, 82.0f, 25f);

            var shots = new (string name, Vector3 pos, Vector3 target, float fov)[]
            {
                // 1. High Aerial Macro-Island & Proportional Magic City (Fix #1, image_f3dc0b.jpg)
                (
                    "Remediation_01_MacroIsland_Proportional_Metropolis",
                    new Vector3(0f, 2100f, -2700f),
                    new Vector3(0f, 50f, 100f),
                    64f
                ),
                // 2. Stepped Tiers 1-4 & De-Clipped Lower City (Fix #2, image_f3dc0b.jpg)
                (
                    "Remediation_02_SteppedTiers_UnburiedLowerCity",
                    new Vector3(250f, 88f, 320f),
                    new Vector3(0f, 45f, 0f),
                    52f
                ),
                // 3. Street Lamp Posts & Warm Anime Stylized Buildings (Fix #3 & #5, image_f42e25.jpg)
                (
                    "Remediation_03_StreetLamps_And_WarmAnimeBuildings",
                    new Vector3(210f, 24.2f, 330f),
                    new Vector3(150f, 23.5f, 385f),
                    50f
                ),
                // 4. Seamless Anime Nature (276931) Flora & Blended Grass (Fix #4)
                (
                    "Remediation_04_AnimeNature_ContinuousMeadow",
                    new Vector3(720f, 46f, -720f),
                    new Vector3(300f, 35f, -300f),
                    56f
                ),
                // 5. Tier 4 Cathedral Acropolis Plaza & Player Hero
                (
                    "Remediation_05_Tier4_Acropolis_Cathedral_Hero",
                    new Vector3(5.5f, 82.2f, 32.5f),
                    new Vector3(0f, 88.0f, -5f),
                    56f
                )
            };

            foreach (var s in shots)
            {
                cam.transform.position = s.pos;
                cam.transform.LookAt(s.target);
                cam.fieldOfView = s.fov;

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

                string artifactPath = Path.Combine(ARTIFACT_DIR, s.name + ".png");
                string assetPath = Path.Combine(ASSETS_DIR, s.name + ".png");

                File.WriteAllBytes(artifactPath, bytes);
                File.WriteAllBytes(assetPath, bytes);

                Debug.Log($"[Remediation Camera] Rendered and saved: {s.name}.png");
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Remediation Camera] All 5 showcase renders successfully captured!</color>");
        }
    }
}
#endif
