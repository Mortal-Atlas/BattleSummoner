using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using BattleSummoner.Combat;

public static class HumanoidAnimationBuilder
{
    private const string ANIM_DIR = "Assets/Animations/Character";
    private const string CONTROLLER_PATH = "Assets/Animators/PlayerHeroAnimator.controller";
    private const string AVATAR_ASSET_PATH = "Assets/Animations/Character/PlayerHumanoidAvatar.asset";

    [MenuItem("Tools/BattleSummoner/Build Humanoid Rig, Animations & Controller")]
    public static void BuildAll()
    {
        EnsureDirectories();

        // 1. Build all animation clips
        var clips = GenerateAllClips();

        // 2. Build AnimatorController with Locomotion BlendTree and state machine
        var controller = BuildAnimatorController(clips);

        // 3. Build Humanoid Avatar and configure Player_Hero in scene
        ConfigureSceneHero(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✨ [HUMANOID RIG] Successfully built Humanoid Rigs, 10 Animation Clips, Blend Tree Controller, and wired Player_Hero!");
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists("Assets/Animators")) Directory.CreateDirectory("Assets/Animators");
        if (!Directory.Exists("Assets/Animations")) Directory.CreateDirectory("Assets/Animations");
        if (!Directory.Exists(ANIM_DIR)) Directory.CreateDirectory(ANIM_DIR);
    }

    #region Clip Generation
    public struct CharacterClips
    {
        public AnimationClip idle;
        public AnimationClip walk;
        public AnimationClip run;
        public AnimationClip sprint;
        public AnimationClip dodgeRoll;
        public AnimationClip jumpTakeoff;
        public AnimationClip jumpFall;
        public AnimationClip jumpLand;
        public AnimationClip meleeSlash;
        public AnimationClip spellCast;
    }

    private static CharacterClips GenerateAllClips()
    {
        var result = new CharacterClips();

        result.idle = CreateIdleClip();
        result.walk = CreateWalkClip();
        result.run = CreateRunClip();
        result.sprint = CreateSprintClip();
        result.dodgeRoll = CreateDodgeRollClip();
        result.jumpTakeoff = CreateJumpTakeoffClip();
        result.jumpFall = CreateJumpFallClip();
        result.jumpLand = CreateJumpLandClip();
        result.meleeSlash = CreateMeleeSlashClip();
        result.spellCast = CreateSpellCastClip();

        return result;
    }

    // Helper to add a quaternion rotation curve
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

    private static void AddPosCurve(AnimationClip clip, string path, float[] times, Vector3[] positions)
    {
        var curveX = new AnimationCurve();
        var curveY = new AnimationCurve();
        var curveZ = new AnimationCurve();

        for (int i = 0; i < times.Length; i++)
        {
            curveX.AddKey(times[i], positions[i].x);
            curveY.AddKey(times[i], positions[i].y);
            curveZ.AddKey(times[i], positions[i].z);
        }

        SmoothCurve(curveX);
        SmoothCurve(curveY);
        SmoothCurve(curveZ);

        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", curveX);
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", curveY);
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", curveZ);
    }

    private static void SmoothCurve(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
    }

    private static AnimationClip CreateIdleClip()
    {
        var clip = new AnimationClip { name = "Player_Idle" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.5f, 1.0f, 1.5f, 2.0f };

        // Hips breathing and gentle weight shift
        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.920f, 0f),
            new Vector3(0.005f, 0.923f, 0f),
            new Vector3(0f, 0.916f, 0f),
            new Vector3(-0.005f, 0.923f, 0f),
            new Vector3(0f, 0.920f, 0f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(0.5f, 0f, -0.8f),
            new Vector3(1.0f, 0f, 0f),
            new Vector3(0.5f, 0f, 0.8f),
            new Vector3(0f, 0f, 0f)
        });

        // Spine & Chest rhythmic breathing
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
            new Vector3(-1.0f, 0f, 0f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(2.2f, 0f, 0f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-1.0f, 0f, 0f)
        });

        // Head counter-stabilization
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/NeckBone/HeadBone", t, new Vector3[] {
            new Vector3(1.0f, 0f, 0f),
            new Vector3(-0.3f, 0.5f, 0f),
            new Vector3(-1.8f, 0f, 0f),
            new Vector3(-0.3f, -0.5f, 0f),
            new Vector3(1.0f, 0f, 0f)
        });

        // Relaxed arms in ready stance
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(8f, 0f, -10f),
            new Vector3(10f, 0f, -9f),
            new Vector3(12f, 0f, -8f),
            new Vector3(10f, 0f, -9f),
            new Vector3(8f, 0f, -10f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm/LeftForearm", t, new Vector3[] {
            new Vector3(-18f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-22f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-18f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm", t, new Vector3[] {
            new Vector3(10f, 0f, 10f),
            new Vector3(12f, 0f, 9f),
            new Vector3(14f, 0f, 8f),
            new Vector3(12f, 0f, 9f),
            new Vector3(10f, 0f, 10f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm", t, new Vector3[] {
            new Vector3(-22f, 0f, 0f),
            new Vector3(-24f, 0f, 0f),
            new Vector3(-26f, 0f, 0f),
            new Vector3(-24f, 0f, 0f),
            new Vector3(-22f, 0f, 0f)
        });

        // Legs natural upright combat stance
        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(-4f, 0f, -2f),
            new Vector3(-4f, 0f, -2f),
            new Vector3(-4f, 0f, -2f),
            new Vector3(-4f, 0f, -2f),
            new Vector3(-4f, 0f, -2f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin/LeftFoot", t, new Vector3[] {
            new Vector3(-2f, 0f, 2f),
            new Vector3(-2f, 0f, 2f),
            new Vector3(-2f, 0f, 2f),
            new Vector3(-2f, 0f, 2f),
            new Vector3(-2f, 0f, 2f)
        });

        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(-4f, 0f, 2f),
            new Vector3(-4f, 0f, 2f),
            new Vector3(-4f, 0f, 2f),
            new Vector3(-4f, 0f, 2f),
            new Vector3(-4f, 0f, 2f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f),
            new Vector3(6f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin/RightFoot", t, new Vector3[] {
            new Vector3(-2f, 0f, -2f),
            new Vector3(-2f, 0f, -2f),
            new Vector3(-2f, 0f, -2f),
            new Vector3(-2f, 0f, -2f),
            new Vector3(-2f, 0f, -2f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_Idle.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateWalkClip()
    {
        var clip = new AnimationClip { name = "Player_Walk" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.25f, 0.50f, 0.75f, 1.0f };

        // Hips vertical bounce and yaw/roll
        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.908f, 0f),
            new Vector3(0.012f, 0.932f, 0f),
            new Vector3(0f, 0.908f, 0f),
            new Vector3(-0.012f, 0.932f, 0f),
            new Vector3(0f, 0.908f, 0f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(2.5f, 6.0f, 0f),
            new Vector3(1.0f, 0f, 2.5f),
            new Vector3(2.5f, -6.0f, 0f),
            new Vector3(1.0f, 0f, -2.5f),
            new Vector3(2.5f, 6.0f, 0f)
        });

        // Spine counter-rotation
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
            new Vector3(2.0f, -5.0f, 0f),
            new Vector3(1.5f, 0f, -1.5f),
            new Vector3(2.0f, 5.0f, 0f),
            new Vector3(1.5f, 0f, 1.5f),
            new Vector3(2.0f, -5.0f, 0f)
        });

        // Left Leg stride
        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(24f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(8f, 0f, 0f),
            new Vector3(24f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(-12f, 0f, 0f),
            new Vector3(-6f, 0f, 0f),
            new Vector3(-42f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin/LeftFoot", t, new Vector3[] {
            new Vector3(14f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(-22f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(14f, 0f, 0f)
        });

        // Right Leg stride (opposite phase)
        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(-20f, 0f, 0f),
            new Vector3(8f, 0f, 0f),
            new Vector3(24f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(-20f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(-6f, 0f, 0f),
            new Vector3(-42f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(-12f, 0f, 0f),
            new Vector3(-6f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin/RightFoot", t, new Vector3[] {
            new Vector3(-22f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(14f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(-22f, 0f, 0f)
        });

        // Natural counter-balancing Arm Swings
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(-18f, 0f, -8f),
            new Vector3(0f, 0f, -10f),
            new Vector3(22f, 0f, -8f),
            new Vector3(0f, 0f, -10f),
            new Vector3(-18f, 0f, -8f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm/LeftForearm", t, new Vector3[] {
            new Vector3(-16f, 0f, 0f),
            new Vector3(-28f, 0f, 0f),
            new Vector3(-35f, 0f, 0f),
            new Vector3(-24f, 0f, 0f),
            new Vector3(-16f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm", t, new Vector3[] {
            new Vector3(22f, 0f, 8f),
            new Vector3(0f, 0f, 10f),
            new Vector3(-18f, 0f, 8f),
            new Vector3(0f, 0f, 10f),
            new Vector3(22f, 0f, 8f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm", t, new Vector3[] {
            new Vector3(-35f, 0f, 0f),
            new Vector3(-24f, 0f, 0f),
            new Vector3(-16f, 0f, 0f),
            new Vector3(-28f, 0f, 0f),
            new Vector3(-35f, 0f, 0f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_Walk.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateRunClip()
    {
        var clip = new AnimationClip { name = "Player_Run" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.175f, 0.35f, 0.525f, 0.70f };

        // Athletic forward lean & bouncy hips
        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.895f, 0f),
            new Vector3(0.015f, 0.948f, 0.02f),
            new Vector3(0f, 0.895f, 0f),
            new Vector3(-0.015f, 0.948f, 0.02f),
            new Vector3(0f, 0.895f, 0f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(8.0f, 8.0f, 0f),
            new Vector3(6.0f, 0f, 3.5f),
            new Vector3(8.0f, -8.0f, 0f),
            new Vector3(6.0f, 0f, -3.5f),
            new Vector3(8.0f, 8.0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
            new Vector3(8.0f, -7.0f, 0f),
            new Vector3(7.0f, 0f, -2.0f),
            new Vector3(8.0f, 7.0f, 0f),
            new Vector3(7.0f, 0f, 2.0f),
            new Vector3(8.0f, -7.0f, 0f)
        });

        // High knee drive and trailing kick
        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(48f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(-34f, 0f, 0f),
            new Vector3(16f, 0f, 0f),
            new Vector3(48f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(-8f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-10f, 0f, 0f),
            new Vector3(-72f, 0f, 0f),
            new Vector3(-8f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(-34f, 0f, 0f),
            new Vector3(16f, 0f, 0f),
            new Vector3(48f, 0f, 0f),
            new Vector3(10f, 0f, 0f),
            new Vector3(-34f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(-10f, 0f, 0f),
            new Vector3(-72f, 0f, 0f),
            new Vector3(-8f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-10f, 0f, 0f)
        });

        // Energetic bent-elbow Arm Drive
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(-35f, 0f, -10f),
            new Vector3(0f, 0f, -12f),
            new Vector3(42f, 0f, -10f),
            new Vector3(0f, 0f, -12f),
            new Vector3(-35f, 0f, -10f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm/LeftForearm", t, new Vector3[] {
            new Vector3(-45f, 0f, 0f),
            new Vector3(-65f, 0f, 0f),
            new Vector3(-80f, 0f, 0f),
            new Vector3(-65f, 0f, 0f),
            new Vector3(-45f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm", t, new Vector3[] {
            new Vector3(42f, 0f, 10f),
            new Vector3(0f, 0f, 12f),
            new Vector3(-35f, 0f, 10f),
            new Vector3(0f, 0f, 12f),
            new Vector3(42f, 0f, 10f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm", t, new Vector3[] {
            new Vector3(-80f, 0f, 0f),
            new Vector3(-65f, 0f, 0f),
            new Vector3(-45f, 0f, 0f),
            new Vector3(-65f, 0f, 0f),
            new Vector3(-80f, 0f, 0f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_Run.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateSprintClip()
    {
        var clip = new AnimationClip { name = "Player_Sprint" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.125f, 0.25f, 0.375f, 0.50f };

        // Low profile forward lean and aggressive drive
        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.875f, 0.04f),
            new Vector3(0.018f, 0.940f, 0.06f),
            new Vector3(0f, 0.875f, 0.04f),
            new Vector3(-0.018f, 0.940f, 0.06f),
            new Vector3(0f, 0.875f, 0.04f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(16f, 10f, 0f),
            new Vector3(12f, 0f, 4f),
            new Vector3(16f, -10f, 0f),
            new Vector3(12f, 0f, -4f),
            new Vector3(16f, 10f, 0f)
        });

        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
            new Vector3(18f, -9f, 0f),
            new Vector3(16f, 0f, -3f),
            new Vector3(18f, 9f, 0f),
            new Vector3(16f, 0f, 3f),
            new Vector3(18f, -9f, 0f)
        });

        // Maximum stride extension
        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(64f, 0f, 0f),
            new Vector3(15f, 0f, 0f),
            new Vector3(-42f, 0f, 0f),
            new Vector3(25f, 0f, 0f),
            new Vector3(64f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(-12f, 0f, 0f),
            new Vector3(-30f, 0f, 0f),
            new Vector3(-12f, 0f, 0f),
            new Vector3(-88f, 0f, 0f),
            new Vector3(-12f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(-42f, 0f, 0f),
            new Vector3(25f, 0f, 0f),
            new Vector3(64f, 0f, 0f),
            new Vector3(15f, 0f, 0f),
            new Vector3(-42f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(-12f, 0f, 0f),
            new Vector3(-88f, 0f, 0f),
            new Vector3(-12f, 0f, 0f),
            new Vector3(-30f, 0f, 0f),
            new Vector3(-12f, 0f, 0f)
        });

        // High-velocity Arm Drive
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(-50f, 0f, -12f),
            new Vector3(0f, 0f, -14f),
            new Vector3(55f, 0f, -12f),
            new Vector3(0f, 0f, -14f),
            new Vector3(-50f, 0f, -12f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm/LeftForearm", t, new Vector3[] {
            new Vector3(-60f, 0f, 0f),
            new Vector3(-80f, 0f, 0f),
            new Vector3(-92f, 0f, 0f),
            new Vector3(-80f, 0f, 0f),
            new Vector3(-60f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm", t, new Vector3[] {
            new Vector3(55f, 0f, 12f),
            new Vector3(0f, 0f, 14f),
            new Vector3(-50f, 0f, 12f),
            new Vector3(0f, 0f, 14f),
            new Vector3(55f, 0f, 12f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm", t, new Vector3[] {
            new Vector3(-92f, 0f, 0f),
            new Vector3(-80f, 0f, 0f),
            new Vector3(-60f, 0f, 0f),
            new Vector3(-80f, 0f, 0f),
            new Vector3(-92f, 0f, 0f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_Sprint.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateDodgeRollClip()
    {
        var clip = new AnimationClip { name = "Player_DodgeRoll" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.10f, 0.22f, 0.34f, 0.45f };

        // 360-degree pitch tumble roll
        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.92f, 0f),
            new Vector3(0f, 0.42f, 0.2f),
            new Vector3(0f, 0.38f, 0.4f),
            new Vector3(0f, 0.65f, 0.6f),
            new Vector3(0f, 0.92f, 0.8f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(100f, 0f, 0f),
            new Vector3(210f, 0f, 0f),
            new Vector3(310f, 0f, 0f),
            new Vector3(360f, 0f, 0f)
        });

        // Curled spine tuck during roll
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(45f, 0f, 0f),
            new Vector3(55f, 0f, 0f),
            new Vector3(25f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        // Tucked limbs
        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(80f, 0f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(35f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(-95f, 0f, 0f),
            new Vector3(-105f, 0f, 0f),
            new Vector3(-45f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(80f, 0f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(35f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(-95f, 0f, 0f),
            new Vector3(-105f, 0f, 0f),
            new Vector3(-45f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_DodgeRoll.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateJumpTakeoffClip()
    {
        var clip = new AnimationClip { name = "Player_Jump_Takeoff" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.08f, 0.25f };

        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.92f, 0f),
            new Vector3(0f, 0.72f, 0f),
            new Vector3(0f, 0.96f, 0f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(15f, 0f, 0f),
            new Vector3(-8f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(35f, 0f, 0f),
            new Vector3(-15f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(-55f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(35f, 0f, 0f),
            new Vector3(-15f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(-55f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        // Arms swing up
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(0f, 0f, -10f),
            new Vector3(-35f, 0f, -12f),
            new Vector3(45f, 0f, -15f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm", t, new Vector3[] {
            new Vector3(0f, 0f, 10f),
            new Vector3(-35f, 0f, 12f),
            new Vector3(45f, 0f, 15f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_Jump_Takeoff.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateJumpFallClip()
    {
        var clip = new AnimationClip { name = "Player_Jump_Fall" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.3f, 0.6f };

        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.92f, 0f),
            new Vector3(0f, 0.93f, 0f),
            new Vector3(0f, 0.92f, 0f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(-5f, 0f, 0f),
            new Vector3(-4f, 0f, 0f),
            new Vector3(-5f, 0f, 0f)
        });

        // Trailing legs in air
        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(-18f, 0f, -4f),
            new Vector3(-15f, 0f, -4f),
            new Vector3(-18f, 0f, -4f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(-25f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-25f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(-12f, 0f, 4f),
            new Vector3(-10f, 0f, 4f),
            new Vector3(-12f, 0f, 4f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(-35f, 0f, 0f),
            new Vector3(-30f, 0f, 0f),
            new Vector3(-35f, 0f, 0f)
        });

        // Stabilizing arms
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(15f, 0f, -35f),
            new Vector3(18f, 0f, -32f),
            new Vector3(15f, 0f, -35f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm", t, new Vector3[] {
            new Vector3(15f, 0f, 35f),
            new Vector3(18f, 0f, 32f),
            new Vector3(15f, 0f, 35f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_Jump_Fall.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateJumpLandClip()
    {
        var clip = new AnimationClip { name = "Player_Jump_Land" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.12f, 0.28f };

        AddPosCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(0f, 0.90f, 0f),
            new Vector3(0f, 0.68f, 0f),
            new Vector3(0f, 0.92f, 0f)
        });

        AddRotCurve(clip, "HipsBone", t, new Vector3[] {
            new Vector3(5f, 0f, 0f),
            new Vector3(18f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        // Knee bend cushion
        AddRotCurve(clip, "HipsBone/LeftThigh", t, new Vector3[] {
            new Vector3(10f, 0f, 0f),
            new Vector3(45f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/LeftThigh/LeftShin", t, new Vector3[] {
            new Vector3(-15f, 0f, 0f),
            new Vector3(-62f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        AddRotCurve(clip, "HipsBone/RightThigh", t, new Vector3[] {
            new Vector3(10f, 0f, 0f),
            new Vector3(45f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });
        AddRotCurve(clip, "HipsBone/RightThigh/RightShin", t, new Vector3[] {
            new Vector3(-15f, 0f, 0f),
            new Vector3(-62f, 0f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_Jump_Land.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateMeleeSlashClip()
    {
        var clip = new AnimationClip { name = "Player_MeleeSlash" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.10f, 0.22f, 0.35f, 0.45f };

        // Dramatic torso torque and slash
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
            new Vector3(5f, 0f, 0f),
            new Vector3(10f, -38f, 0f),  // Wind-up to right
            new Vector3(-5f, 42f, 0f),   // Explosive forward cleave
            new Vector3(0f, 25f, 0f),    // Decelerate
            new Vector3(0f, 0f, 0f)      // Return to guard
        });

        // Right Arm cleave arc
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm", t, new Vector3[] {
            new Vector3(15f, 0f, 10f),
            new Vector3(55f, -30f, 45f), // High back wind-up
            new Vector3(-25f, 65f, -15f),// Powerful cross-slash
            new Vector3(-10f, 40f, 0f),
            new Vector3(10f, 0f, 10f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm", t, new Vector3[] {
            new Vector3(-25f, 0f, 0f),
            new Vector3(-85f, 0f, 0f),
            new Vector3(-20f, 0f, 0f),
            new Vector3(-35f, 0f, 0f),
            new Vector3(-22f, 0f, 0f)
        });

        // Left Arm counter-balance
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(10f, 0f, -10f),
            new Vector3(-25f, 0f, -25f),
            new Vector3(15f, 0f, -35f),
            new Vector3(10f, 0f, -20f),
            new Vector3(10f, 0f, -10f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_MeleeSlash.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimationClip CreateSpellCastClip()
    {
        var clip = new AnimationClip { name = "Player_SpellCast" };
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        float[] t = { 0f, 0.15f, 0.32f, 0.45f };

        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone", t, new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(-8f, 15f, 0f),
            new Vector3(4f, -10f, 0f),
            new Vector3(0f, 0f, 0f)
        });

        // Left Arm extends forward in gesture channel
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm", t, new Vector3[] {
            new Vector3(10f, 0f, -10f),
            new Vector3(65f, 20f, -15f),
            new Vector3(75f, 15f, -12f),
            new Vector3(10f, 0f, -10f)
        });
        AddRotCurve(clip, "HipsBone/TorsoBone/ChestBone/LeftUpperArm/LeftForearm", t, new Vector3[] {
            new Vector3(-20f, 0f, 0f),
            new Vector3(-15f, 0f, 0f),
            new Vector3(-10f, 0f, 0f),
            new Vector3(-20f, 0f, 0f)
        });

        string path = Path.Combine(ANIM_DIR, "Player_SpellCast.anim").Replace('\\', '/');
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
    #endregion

    #region AnimatorController & BlendTree Builder
    private static AnimatorController BuildAnimatorController(CharacterClips clips)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);

        // Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsMountRiding", AnimatorControllerParameterType.Bool);
        controller.AddParameter("SlashTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("CastTrigger", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DashTrigger", AnimatorControllerParameterType.Trigger);

        var rootStateMachine = controller.layers[0].stateMachine;

        // 1. Locomotion State with 1D BlendTree
        BlendTree blendTree;
        var locomotionState = controller.CreateBlendTreeInController("Locomotion", out blendTree);
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Speed";
        blendTree.useAutomaticThresholds = false;
        blendTree.children = new ChildMotion[]
        {
            new ChildMotion { motion = clips.idle, threshold = 0.0f, timeScale = 1.0f },
            new ChildMotion { motion = clips.walk, threshold = 0.35f, timeScale = 1.0f },
            new ChildMotion { motion = clips.run, threshold = 0.80f, timeScale = 1.0f },
            new ChildMotion { motion = clips.sprint, threshold = 1.25f, timeScale = 1.0f }
        };

        rootStateMachine.defaultState = locomotionState;

        // 2. Dash / Dodge Roll State
        var dashState = rootStateMachine.AddState("Dash");
        dashState.motion = clips.dodgeRoll;

        var anyToDash = rootStateMachine.AddAnyStateTransition(dashState);
        anyToDash.AddCondition(AnimatorConditionMode.If, 0, "DashTrigger");
        anyToDash.duration = 0.06f;
        anyToDash.hasExitTime = false;
        anyToDash.canTransitionToSelf = false;

        var dashToLoco = dashState.AddTransition(locomotionState);
        dashToLoco.hasExitTime = true;
        dashToLoco.exitTime = 0.88f;
        dashToLoco.duration = 0.12f;

        // 3. Jump Takeoff State
        var jumpTakeoffState = rootStateMachine.AddState("Jump_Takeoff");
        jumpTakeoffState.motion = clips.jumpTakeoff;

        var locoToJump = locomotionState.AddTransition(jumpTakeoffState);
        locoToJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        locoToJump.AddCondition(AnimatorConditionMode.Greater, 0.5f, "VerticalSpeed");
        locoToJump.duration = 0.06f;
        locoToJump.hasExitTime = false;

        // 4. Jump Fall State
        var jumpFallState = rootStateMachine.AddState("Jump_Fall");
        jumpFallState.motion = clips.jumpFall;

        var takeoffToFall = jumpTakeoffState.AddTransition(jumpFallState);
        takeoffToFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalSpeed");
        takeoffToFall.duration = 0.12f;
        takeoffToFall.hasExitTime = false;

        var locoToFall = locomotionState.AddTransition(jumpFallState);
        locoToFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        locoToFall.AddCondition(AnimatorConditionMode.Less, -1.5f, "VerticalSpeed");
        locoToFall.duration = 0.10f;
        locoToFall.hasExitTime = false;

        // 5. Jump Land State
        var jumpLandState = rootStateMachine.AddState("Jump_Land");
        jumpLandState.motion = clips.jumpLand;

        var fallToLand = jumpFallState.AddTransition(jumpLandState);
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        fallToLand.duration = 0.06f;
        fallToLand.hasExitTime = false;

        var landToLoco = jumpLandState.AddTransition(locomotionState);
        landToLoco.hasExitTime = true;
        landToLoco.exitTime = 0.85f;
        landToLoco.duration = 0.12f;

        // 6. Melee Slash State
        var slashState = rootStateMachine.AddState("MeleeSlash");
        slashState.motion = clips.meleeSlash;

        var anyToSlash = rootStateMachine.AddAnyStateTransition(slashState);
        anyToSlash.AddCondition(AnimatorConditionMode.If, 0, "SlashTrigger");
        anyToSlash.duration = 0.05f;
        anyToSlash.hasExitTime = false;
        anyToSlash.canTransitionToSelf = false;

        var slashToLoco = slashState.AddTransition(locomotionState);
        slashToLoco.hasExitTime = true;
        slashToLoco.exitTime = 0.82f;
        slashToLoco.duration = 0.12f;

        // 7. Spell Cast State
        var castState = rootStateMachine.AddState("SpellCast");
        castState.motion = clips.spellCast;

        var anyToCast = rootStateMachine.AddAnyStateTransition(castState);
        anyToCast.AddCondition(AnimatorConditionMode.If, 0, "CastTrigger");
        anyToCast.duration = 0.08f;
        anyToCast.hasExitTime = false;
        anyToCast.canTransitionToSelf = false;

        var castToLoco = castState.AddTransition(locomotionState);
        castToLoco.hasExitTime = true;
        castToLoco.exitTime = 0.85f;
        castToLoco.duration = 0.12f;

        return controller;
    }
    #endregion

    #region Scene Configuration
    public static void ConfigureSceneHero(AnimatorController controller)
    {
        var player = GameObject.Find("Player_Hero");
        if (player == null)
        {
            Debug.LogError("No Player_Hero found in active scene!");
            return;
        }

        // Hide old capsule placeholder mesh
        var mr = player.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Ensure AvatarRigRoot
        var avatarRoot = player.transform.Find("AvatarRigRoot");
        if (avatarRoot == null)
        {
            Debug.LogError("Player_Hero does not have AvatarRigRoot!");
            return;
        }

        // Reset skeleton to clean upright T-pose before avatar construction
        var hipsBone = avatarRoot.Find("HipsBone");
        if (hipsBone != null)
        {
            hipsBone.localPosition = new Vector3(0f, 0.95f, 0f);
            hipsBone.localRotation = Quaternion.identity;
        }

        var leftThigh = avatarRoot.Find("HipsBone/LeftThigh"); if (leftThigh != null) leftThigh.localRotation = Quaternion.identity;
        var leftShin = avatarRoot.Find("HipsBone/LeftThigh/LeftShin"); if (leftShin != null) leftShin.localRotation = Quaternion.identity;
        var leftFoot = avatarRoot.Find("HipsBone/LeftThigh/LeftShin/LeftFoot"); if (leftFoot != null) leftFoot.localRotation = Quaternion.identity;
        var rightThigh = avatarRoot.Find("HipsBone/RightThigh"); if (rightThigh != null) rightThigh.localRotation = Quaternion.identity;
        var rightShin = avatarRoot.Find("HipsBone/RightThigh/RightShin"); if (rightShin != null) rightShin.localRotation = Quaternion.identity;
        var rightFoot = avatarRoot.Find("HipsBone/RightThigh/RightShin/RightFoot"); if (rightFoot != null) rightFoot.localRotation = Quaternion.identity;
        var torso = avatarRoot.Find("HipsBone/TorsoBone"); if (torso != null) torso.localRotation = Quaternion.identity;
        var chest = avatarRoot.Find("HipsBone/TorsoBone/ChestBone"); if (chest != null) chest.localRotation = Quaternion.identity;
        var neck = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/NeckBone"); if (neck != null) neck.localRotation = Quaternion.identity;
        var head = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/NeckBone/HeadBone"); if (head != null) head.localRotation = Quaternion.identity;
        var lArm = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/LeftUpperArm"); if (lArm != null) lArm.localRotation = Quaternion.identity;
        var lForearm = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/LeftUpperArm/LeftForearm"); if (lForearm != null) lForearm.localRotation = Quaternion.identity;
        var rArm = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/RightUpperArm"); if (rArm != null) rArm.localRotation = Quaternion.identity;
        var rForearm = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm"); if (rForearm != null) rForearm.localRotation = Quaternion.identity;

        // Build Humanoid Avatar
        Avatar avatar = BuildHumanoidAvatar(avatarRoot.gameObject);
        if (avatar != null)
        {
            AssetDatabase.CreateAsset(avatar, AVATAR_ASSET_PATH);
        }

        // Attach and configure Animator on AvatarRigRoot
        var animator = avatarRoot.GetComponent<Animator>();
        if (animator == null) animator = avatarRoot.gameObject.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        if (avatar != null) animator.avatar = avatar;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Fit hair crowns cleanly over head cranium
        var hairRoot = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/NeckBone/HeadBone/HairRoot");
        if (hairRoot != null)
        {
            for (int i = 0; i < hairRoot.childCount; i++)
            {
                var hairChild = hairRoot.GetChild(i);
                var crown = hairChild.Find("Crown");
                if (crown != null)
                {
                    crown.localPosition = new Vector3(0f, 0.08f, -0.01f);
                    crown.localScale = new Vector3(0.27f, 0.28f, 0.27f);
                }
            }
        }

        // Equip stylized Rune Blade to RightHand if not already equipped
        var rightHand = avatarRoot.Find("HipsBone/TorsoBone/ChestBone/RightUpperArm/RightForearm/RightHand");
        if (rightHand != null)
        {
            var existingWeapon = rightHand.Find("Weapon_RuneBlade");
            if (existingWeapon == null)
            {
                var weaponRoot = new GameObject("Weapon_RuneBlade");
                weaponRoot.transform.SetParent(rightHand, false);
                weaponRoot.transform.localPosition = new Vector3(0f, -0.05f, 0.04f);
                weaponRoot.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

                var goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Relic_BrushedBrass.mat");
                var ironMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Anvil_Iron.mat");
                var bladeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Relic_AstralGem.mat");

                // Hilt grip
                var hilt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hilt.name = "Hilt";
                hilt.transform.SetParent(weaponRoot.transform, false);
                hilt.transform.localPosition = new Vector3(0f, -0.08f, 0f);
                hilt.transform.localScale = new Vector3(0.035f, 0.09f, 0.035f);
                if (ironMat != null) hilt.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
                var coll = hilt.GetComponent<Collider>(); if (coll != null) UnityEngine.Object.DestroyImmediate(coll);

                // Pommel
                var pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pommel.name = "Pommel";
                pommel.transform.SetParent(weaponRoot.transform, false);
                pommel.transform.localPosition = new Vector3(0f, 0.01f, 0f);
                pommel.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
                if (goldMat != null) pommel.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
                coll = pommel.GetComponent<Collider>(); if (coll != null) UnityEngine.Object.DestroyImmediate(coll);

                // Crossguard
                var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                guard.name = "Crossguard";
                guard.transform.SetParent(weaponRoot.transform, false);
                guard.transform.localPosition = new Vector3(0f, -0.17f, 0f);
                guard.transform.localScale = new Vector3(0.24f, 0.035f, 0.06f);
                if (goldMat != null) guard.GetComponent<MeshRenderer>().sharedMaterial = goldMat;
                coll = guard.GetComponent<Collider>(); if (coll != null) UnityEngine.Object.DestroyImmediate(coll);

                // Blade
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Blade";
                blade.transform.SetParent(weaponRoot.transform, false);
                blade.transform.localPosition = new Vector3(0f, -0.58f, 0f);
                blade.transform.localScale = new Vector3(0.07f, 0.78f, 0.015f);
                if (bladeMat != null) blade.GetComponent<MeshRenderer>().sharedMaterial = bladeMat;
                coll = blade.GetComponent<Collider>(); if (coll != null) UnityEngine.Object.DestroyImmediate(coll);
            }
        }

        // Attach HumanoidAnimationController to Player_Hero
        var animController = player.GetComponent<HumanoidAnimationController>();
        if (animController == null) animController = player.AddComponent<HumanoidAnimationController>();
        animController.ResolveAnimator();
        EditorUtility.SetDirty(animController);
        EditorUtility.SetDirty(player);

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }

    public static Avatar BuildHumanoidAvatar(GameObject rigRoot)
    {
        var humanBones = new List<HumanBone>();
        Action<string, string> mapBone = (humanName, boneName) => {
            var hb = new HumanBone();
            hb.humanName = humanName;
            hb.boneName = boneName;
            hb.limit.useDefaultValues = true;
            humanBones.Add(hb);
        };

        mapBone("Hips", "HipsBone");
        mapBone("Spine", "TorsoBone");
        mapBone("Chest", "ChestBone");
        mapBone("Neck", "NeckBone");
        mapBone("Head", "HeadBone");
        mapBone("LeftUpperArm", "LeftUpperArm");
        mapBone("LeftLowerArm", "LeftForearm");
        mapBone("LeftHand", "LeftHand");
        mapBone("RightUpperArm", "RightUpperArm");
        mapBone("RightLowerArm", "RightForearm");
        mapBone("RightHand", "RightHand");
        mapBone("LeftUpperLeg", "LeftThigh");
        mapBone("LeftLowerLeg", "LeftShin");
        mapBone("LeftFoot", "LeftFoot");
        mapBone("RightUpperLeg", "RightThigh");
        mapBone("RightLowerLeg", "RightShin");
        mapBone("RightFoot", "RightFoot");

        var skeletonBones = new List<SkeletonBone>();
        Action<Transform> addSkel = null;
        addSkel = (t) => {
            var sb = new SkeletonBone();
            sb.name = t.name;
            sb.position = t.localPosition;
            sb.rotation = t.localRotation;
            sb.scale = t.localScale;
            skeletonBones.Add(sb);
            for (int i = 0; i < t.childCount; i++) addSkel(t.GetChild(i));
        };

        addSkel(rigRoot.transform);

        var desc = new HumanDescription();
        desc.human = humanBones.ToArray();
        desc.skeleton = skeletonBones.ToArray();
        desc.armStretch = 0.05f;
        desc.legStretch = 0.05f;
        desc.upperArmTwist = 0.5f;
        desc.lowerArmTwist = 0.5f;
        desc.upperLegTwist = 0.5f;
        desc.lowerLegTwist = 0.5f;
        desc.feetSpacing = 0f;

        return AvatarBuilder.BuildHumanAvatar(rigRoot, desc);
    }
    #endregion
}
