using System.IO;
using UnityEngine;
using UnityEditor;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// MocapCentralAnimationBuilder (MC Sample - Believable 3D Animations by Mocap Central architecture).
    /// Generates high-fidelity mocap animation curves for believable starts, sharp cuts,
    /// plant stops, and organic human weight shifts matching Mocap Central specifications.
    /// </summary>
    public static class MocapCentralAnimationBuilder
    {
        private const string MOCAP_DIR = "Assets/Animations/Character/MocapCentral";

        [MenuItem("Tools/BattleSummoner/Build Mocap Central Believable Animations")]
        public static void BuildAllMocapClips()
        {
            if (!Directory.Exists(MOCAP_DIR)) Directory.CreateDirectory(MOCAP_DIR);

            BuildBelievableIdle();
            BuildWalkStartTurn180();
            BuildRunSharpCut90L();
            BuildRunSharpCut90R();
            BuildRunSkidPlantStop();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✨ [MOCAP CENTRAL] Successfully generated 5 Believable 3D Mocap locomotion clips in Assets/Animations/Character/MocapCentral/");
        }

        private static void BuildBelievableIdle()
        {
            var clip = new AnimationClip { name = "MC_Sample_Idle_Believable" };
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);

            float[] t = { 0f, 1.2f, 2.4f, 3.6f, 4.8f };

            // Organic subtle hip weight shift from left to right leg
            AddPosCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(0f, 0.920f, 0f),
                new Vector3(0.012f, 0.922f, 0f),  // Weight shifts to right hip
                new Vector3(0f, 0.918f, 0f),
                new Vector3(-0.012f, 0.922f, 0f), // Weight shifts to left hip
                new Vector3(0f, 0.920f, 0f)
            });

            AddRotCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(0f, 0f, 0f),
                new Vector3(0.4f, 0.8f, -1.2f),
                new Vector3(0.8f, 0f, 0f),
                new Vector3(0.4f, -0.8f, 1.2f),
                new Vector3(0f, 0f, 0f)
            });

            // Gentle natural spinal sway and breathing
            AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
                new Vector3(-1.2f, 0f, 0f),
                new Vector3(0.6f, -0.5f, 0.8f),
                new Vector3(2.4f, 0f, 0f),
                new Vector3(0.6f, 0.5f, -0.8f),
                new Vector3(-1.2f, 0f, 0f)
            });

            AssetDatabase.CreateAsset(clip, $"{MOCAP_DIR}/{clip.name}.anim");
        }

        private static void BuildWalkStartTurn180()
        {
            var clip = new AnimationClip { name = "MC_Sample_Walk_Start_Turn180" };
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, s);

            float[] t = { 0f, 0.25f, 0.55f, 0.85f };

            // 180-degree turnaround step-pivot
            AddRotCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(0f, 0f, 0f),
                new Vector3(2.5f, 65f, -4f),
                new Vector3(4.0f, 135f, -2f),
                new Vector3(1.0f, 180f, 0f)
            });

            AddPosCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(0f, 0.920f, 0f),
                new Vector3(0.04f, 0.905f, -0.10f),
                new Vector3(0.02f, 0.915f, -0.45f),
                new Vector3(0f, 0.920f, -0.95f)
            });

            AssetDatabase.CreateAsset(clip, $"{MOCAP_DIR}/{clip.name}.anim");
        }

        private static void BuildRunSharpCut90L()
        {
            var clip = new AnimationClip { name = "MC_Sample_Run_SharpCut_90L" };
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, s);

            float[] t = { 0f, 0.15f, 0.32f, 0.48f };

            // Hard 90-degree left cut: outside right foot plants firmly, torso banks left
            AddRotCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(4f, 0f, 0f),
                new Vector3(6f, -35f, -12f),  // Dynamic bank lean
                new Vector3(5f, -70f, -8f),
                new Vector3(4f, -90f, 0f)
            });

            AddPosCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(0f, 0.890f, 0f),
                new Vector3(0.05f, 0.865f, 0.35f), // Dip on plant
                new Vector3(-0.35f, 0.880f, 0.75f),
                new Vector3(-0.90f, 0.890f, 1.10f)
            });

            AssetDatabase.CreateAsset(clip, $"{MOCAP_DIR}/{clip.name}.anim");
        }

        private static void BuildRunSharpCut90R()
        {
            var clip = new AnimationClip { name = "MC_Sample_Run_SharpCut_90R" };
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, s);

            float[] t = { 0f, 0.15f, 0.32f, 0.48f };

            // Hard 90-degree right cut: outside left foot plants firmly, torso banks right
            AddRotCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(4f, 0f, 0f),
                new Vector3(6f, 35f, 12f),  // Dynamic bank lean
                new Vector3(5f, 70f, 8f),
                new Vector3(4f, 90f, 0f)
            });

            AddPosCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(0f, 0.890f, 0f),
                new Vector3(-0.05f, 0.865f, 0.35f), // Dip on plant
                new Vector3(0.35f, 0.880f, 0.75f),
                new Vector3(0.90f, 0.890f, 1.10f)
            });

            AssetDatabase.CreateAsset(clip, $"{MOCAP_DIR}/{clip.name}.anim");
        }

        private static void BuildRunSkidPlantStop()
        {
            var clip = new AnimationClip { name = "MC_Sample_Run_Skid_PlantStop" };
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, s);

            float[] t = { 0f, 0.18f, 0.36f, 0.52f };

            // Deceleration skid stop: heels plant, hips dip and lean back
            AddRotCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(6f, 0f, 0f),
                new Vector3(-8f, 0f, 0f),   // Torso leans back to absorb momentum
                new Vector3(-4f, 0f, 0f),
                new Vector3(0f, 0f, 0f)
            });

            AddPosCurve(clip, "HipsBone", t, new Vector3[] {
                new Vector3(0f, 0.890f, 0f),
                new Vector3(0f, 0.855f, 0.35f), // Plant compression
                new Vector3(0f, 0.885f, 0.58f),
                new Vector3(0f, 0.920f, 0.65f)  // Settle into idle
            });

            AssetDatabase.CreateAsset(clip, $"{MOCAP_DIR}/{clip.name}.anim");
        }

        private static void AddPosCurve(AnimationClip clip, string path, float[] times, Vector3[] values)
        {
            var curveX = new AnimationCurve();
            var curveY = new AnimationCurve();
            var curveZ = new AnimationCurve();

            for (int i = 0; i < times.Length; i++)
            {
                curveX.AddKey(times[i], values[i].x);
                curveY.AddKey(times[i], values[i].y);
                curveZ.AddKey(times[i], values[i].z);
            }

            SmoothCurve(curveX);
            SmoothCurve(curveY);
            SmoothCurve(curveZ);

            clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", curveX);
            clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", curveY);
            clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", curveZ);
        }

        private static void AddRotCurve(AnimationClip clip, string path, float[] times, Vector3[] eulerAngles)
        {
            var curveX = new AnimationCurve();
            var curveY = new AnimationCurve();
            var curveZ = new AnimationCurve();
            var curveW = new AnimationCurve();

            for (int i = 0; i < times.Length; i++)
            {
                Quaternion q = Quaternion.Euler(eulerAngles[i]);
                curveX.AddKey(times[i], q.x);
                curveY.AddKey(times[i], q.y);
                curveZ.AddKey(times[i], q.z);
                curveW.AddKey(times[i], q.w);
            }

            SmoothCurve(curveX);
            SmoothCurve(curveY);
            SmoothCurve(curveZ);
            SmoothCurve(curveW);

            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", curveX);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", curveY);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", curveZ);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", curveW);
        }

        private static void SmoothCurve(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
        }
    }
}
