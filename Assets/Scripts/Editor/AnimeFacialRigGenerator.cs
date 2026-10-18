using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Customization;

namespace BattleSummoner.EditorScripts
{
    public static class AnimeFacialRigGenerator
    {
        [MenuItem("BattleSummoner/Rig Character Faces (Mouth & Expressions)", priority = 110)]
        public static void GenerateAllFacialRigs()
        {
            RigCharacterModel(
                "Assets/Prefabs/Characters/Character_Base_Male_Genshin.prefab",
                "Assets/Models/Characters/Character_Mesh_Male_FacialRigged.asset",
                isFemale: false
            );

            RigCharacterModel(
                "Assets/Prefabs/Characters/Character_Base_Female_Genshin.prefab",
                "Assets/Models/Characters/Character_Mesh_Female_FacialRigged.asset",
                isFemale: true
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#38BDF8>✦ Successfully generated anime mouth rigs and facial expression controllers for both characters! ✦</color>");
        }

        public static void RigCharacterModel(string prefabPath, string outputMeshPath, bool isFemale)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"Could not load prefab at {prefabPath}");
                return;
            }

            var smr = prefabRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr == null)
            {
                Debug.LogError($"No SkinnedMeshRenderer found on {prefabPath}");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            // Clone base mesh to create the facial rigged mesh
            var baseMesh = smr.sharedMesh;
            var facialMesh = UnityEngine.Object.Instantiate(baseMesh);
            facialMesh.name = isFemale ? "Character_Mesh_Female_FacialRigged" : "Character_Mesh_Male_FacialRigged";

            // Define facial landmarks
            Vector3 mouthCenter = isFemale ? new Vector3(0.00f, 1.50f, 0.10f) : new Vector3(0.00f, 1.54f, 0.08f);
            Vector3 browLCenter = isFemale ? new Vector3(-0.04f, 1.60f, 0.09f) : new Vector3(-0.04f, 1.65f, 0.11f);
            Vector3 browRCenter = isFemale ? new Vector3(0.04f, 1.60f, 0.09f) : new Vector3(0.04f, 1.65f, 0.11f);
            Vector3 eyeLCenter = isFemale ? new Vector3(-0.042f, 1.55f, 0.09f) : new Vector3(-0.045f, 1.60f, 0.10f);
            Vector3 eyeRCenter = isFemale ? new Vector3(0.042f, 1.55f, 0.09f) : new Vector3(0.045f, 1.60f, 0.10f);

            float mouthR = 0.055f;
            float browR = 0.042f;
            float eyeR = 0.038f;

            var verts = facialMesh.vertices;
            int n = verts.Length;

            // Allocate delta arrays
            var d_A = new Vector3[n];
            var d_I = new Vector3[n];
            var d_U = new Vector3[n];
            var d_E = new Vector3[n];
            var d_O = new Vector3[n];
            var d_Smile = new Vector3[n];
            var d_Frown = new Vector3[n];
            var d_Open = new Vector3[n];
            var d_Pout = new Vector3[n];
            var d_SmirkL = new Vector3[n];
            var d_SmirkR = new Vector3[n];

            var d_BrowRaiseL = new Vector3[n];
            var d_BrowRaiseR = new Vector3[n];
            var d_BrowLowerL = new Vector3[n];
            var d_BrowLowerR = new Vector3[n];
            var d_BrowAngryL = new Vector3[n];
            var d_BrowAngryR = new Vector3[n];
            var d_BrowSadL = new Vector3[n];
            var d_BrowSadR = new Vector3[n];

            var d_EyeBlinkL = new Vector3[n];
            var d_EyeBlinkR = new Vector3[n];
            var d_EyeWideL = new Vector3[n];
            var d_EyeWideR = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                var v = verts[i];

                // 1. Mouth calculations
                float dm = Vector3.Distance(v, mouthCenter);
                if (dm < mouthR)
                {
                    float w = 0.5f * (1f + Mathf.Cos(Mathf.PI * dm / mouthR));
                    float relY = v.y - mouthCenter.y;
                    float relX = v.x - mouthCenter.x;
                    float signX = Mathf.Sign(relX);

                    // Vowel A (Subtle jaw relaxation)
                    if (relY <= 0.005f) d_A[i] = new Vector3(0f, -0.005f * w, -0.002f * w);
                    else d_A[i] = new Vector3(0f, 0.002f * w, -0.001f * w);

                    // Vowel I (Wide horizontal smile)
                    d_I[i] = new Vector3(signX * 0.006f * w, 0.002f * w, 0f);

                    // Vowel U (Pursed lips forward, narrow X)
                    d_U[i] = new Vector3(-signX * 0.004f * w, -0.002f * w, 0.004f * w);

                    // Vowel E (Medium opening, relaxed wide corners)
                    d_E[i] = new Vector3(signX * 0.004f * w, -0.003f * w, 0f);

                    // Vowel O (Tall vertical oval, narrow X)
                    d_O[i] = new Vector3(-signX * 0.003f * w, -0.005f * w, 0.002f * w);

                    // Mouth Smile (Corners up and out)
                    if (Mathf.Abs(relX) > 0.012f)
                        d_Smile[i] = new Vector3(signX * 0.005f * w, 0.004f * w, 0.001f * w);
                    else if (relY > 0f)
                        d_Smile[i] = new Vector3(0f, 0.002f * w, 0f);

                    // Mouth Frown (Corners down)
                    if (Mathf.Abs(relX) > 0.012f)
                        d_Frown[i] = new Vector3(0f, -0.004f * w, 0f);

                    // Mouth Open (Pure jaw drop)
                    if (relY <= 0.005f) d_Open[i] = new Vector3(0f, -0.006f * w, -0.002f * w);

                    // Mouth Pout (Lower lip up and out)
                    if (relY <= 0.005f) d_Pout[i] = new Vector3(0f, 0.010f * w, 0.016f * w);

                    // Smirks
                    if (relX < -0.012f) d_SmirkL[i] = new Vector3(-0.008f * w, 0.016f * w, 0f);
                    if (relX > 0.012f) d_SmirkR[i] = new Vector3(0.008f * w, 0.016f * w, 0f);
                }

                // 2. Eyebrow Left
                float dbl = Vector3.Distance(v, browLCenter);
                if (dbl < browR)
                {
                    float w = 0.5f * (1f + Mathf.Cos(Mathf.PI * dbl / browR));
                    float innerWeight = Mathf.Clamp01(1f - (Mathf.Abs(v.x) - 0.015f) / 0.035f);
                    float outerWeight = 1f - innerWeight;

                    d_BrowRaiseL[i] = new Vector3(0f, 0.022f * w, 0.004f * w);
                    d_BrowLowerL[i] = new Vector3(0f, -0.016f * w, -0.002f * w);
                    d_BrowAngryL[i] = new Vector3(0.010f * w * innerWeight, -0.020f * w * innerWeight, 0f);
                    d_BrowSadL[i] = new Vector3(0f, (0.020f * innerWeight - 0.014f * outerWeight) * w, 0f);
                }

                // 3. Eyebrow Right
                float dbr = Vector3.Distance(v, browRCenter);
                if (dbr < browR)
                {
                    float w = 0.5f * (1f + Mathf.Cos(Mathf.PI * dbr / browR));
                    float innerWeight = Mathf.Clamp01(1f - (Mathf.Abs(v.x) - 0.015f) / 0.035f);
                    float outerWeight = 1f - innerWeight;

                    d_BrowRaiseR[i] = new Vector3(0f, 0.022f * w, 0.004f * w);
                    d_BrowLowerR[i] = new Vector3(0f, -0.016f * w, -0.002f * w);
                    d_BrowAngryR[i] = new Vector3(-0.010f * w * innerWeight, -0.020f * w * innerWeight, 0f);
                    d_BrowSadR[i] = new Vector3(0f, (0.020f * innerWeight - 0.014f * outerWeight) * w, 0f);
                }

                // 4. Eye Left
                float del = Vector3.Distance(v, eyeLCenter);
                if (del < eyeR)
                {
                    float w = 0.5f * (1f + Mathf.Cos(Mathf.PI * del / eyeR));
                    if (v.y >= eyeLCenter.y - 0.005f)
                    {
                        d_EyeBlinkL[i] = new Vector3(0f, -0.018f * w, -0.002f * w);
                        d_EyeWideL[i] = new Vector3(0f, 0.008f * w, 0.002f * w);
                    }
                }

                // 5. Eye Right
                float der = Vector3.Distance(v, eyeRCenter);
                if (der < eyeR)
                {
                    float w = 0.5f * (1f + Mathf.Cos(Mathf.PI * der / eyeR));
                    if (v.y >= eyeRCenter.y - 0.005f)
                    {
                        d_EyeBlinkR[i] = new Vector3(0f, -0.018f * w, -0.002f * w);
                        d_EyeWideR[i] = new Vector3(0f, 0.008f * w, 0.002f * w);
                    }
                }
            }

            // Register BlendShape channels
            facialMesh.ClearBlendShapes();
            facialMesh.AddBlendShapeFrame("Mouth_A", 100f, d_A, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_I", 100f, d_I, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_U", 100f, d_U, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_E", 100f, d_E, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_O", 100f, d_O, null, null);

            facialMesh.AddBlendShapeFrame("Mouth_Smile", 100f, d_Smile, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_Frown", 100f, d_Frown, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_Open", 100f, d_Open, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_Pout", 100f, d_Pout, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_Smirk_L", 100f, d_SmirkL, null, null);
            facialMesh.AddBlendShapeFrame("Mouth_Smirk_R", 100f, d_SmirkR, null, null);

            facialMesh.AddBlendShapeFrame("Brow_Raise_L", 100f, d_BrowRaiseL, null, null);
            facialMesh.AddBlendShapeFrame("Brow_Raise_R", 100f, d_BrowRaiseR, null, null);
            facialMesh.AddBlendShapeFrame("Brow_Lower_L", 100f, d_BrowLowerL, null, null);
            facialMesh.AddBlendShapeFrame("Brow_Lower_R", 100f, d_BrowLowerR, null, null);
            facialMesh.AddBlendShapeFrame("Brow_Angry_L", 100f, d_BrowAngryL, null, null);
            facialMesh.AddBlendShapeFrame("Brow_Angry_R", 100f, d_BrowAngryR, null, null);
            facialMesh.AddBlendShapeFrame("Brow_Sad_L", 100f, d_BrowSadL, null, null);
            facialMesh.AddBlendShapeFrame("Brow_Sad_R", 100f, d_BrowSadR, null, null);

            facialMesh.AddBlendShapeFrame("Eye_Blink_L", 100f, d_EyeBlinkL, null, null);
            facialMesh.AddBlendShapeFrame("Eye_Blink_R", 100f, d_EyeBlinkR, null, null);
            facialMesh.AddBlendShapeFrame("Eye_Wide_L", 100f, d_EyeWideL, null, null);
            facialMesh.AddBlendShapeFrame("Eye_Wide_R", 100f, d_EyeWideR, null, null);

            // Save mesh asset
            AssetDatabase.CreateAsset(facialMesh, outputMeshPath);
            smr.sharedMesh = facialMesh;

            // Build physical mouth and facial bones under Head
            var headTransform = prefabRoot.transform.Find("Character_Armature/Hips/Spine/Chest/UpperChest/Neck/Head");
            Transform mouthRoot = null, jaw = null, lipUp = null, lipLow = null, cornerL = null, cornerR = null, browBoneL = null, browBoneR = null;

            if (headTransform != null)
            {
                var existingRoot = headTransform.Find("Mouth_Rig_Root");
                if (existingRoot != null) UnityEngine.Object.DestroyImmediate(existingRoot.gameObject);

                var rootGo = new GameObject("Mouth_Rig_Root");
                rootGo.transform.SetParent(headTransform, false);
                rootGo.transform.localPosition = new Vector3(0f, 0.05f, 0.08f);
                mouthRoot = rootGo.transform;

                jaw = CreateBone(mouthRoot, "Jaw_Bone", new Vector3(0f, -0.06f, 0.02f));
                lipUp = CreateBone(mouthRoot, "Lip_Upper", new Vector3(0f, -0.04f, 0.04f));
                lipLow = CreateBone(jaw, "Lip_Lower", new Vector3(0f, 0.015f, 0.02f));
                cornerL = CreateBone(mouthRoot, "Mouth_Corner_Left", new Vector3(-0.025f, -0.05f, 0.035f));
                cornerR = CreateBone(mouthRoot, "Mouth_Corner_Right", new Vector3(0.025f, -0.05f, 0.035f));
                browBoneL = CreateBone(headTransform, "Brow_Bone_Left", new Vector3(-0.04f, 0.12f, 0.09f));
                browBoneR = CreateBone(headTransform, "Brow_Bone_Right", new Vector3(0.04f, 0.12f, 0.09f));

                // Create Anime Mouth Cavity Quad
                var existingCavity = headTransform.Find("Anime_Mouth_Cavity");
                if (existingCavity != null) UnityEngine.Object.DestroyImmediate(existingCavity.gameObject);

                var mouthGo = new GameObject("Anime_Mouth_Cavity");
                mouthGo.transform.SetParent(headTransform, false);
                mouthGo.transform.localPosition = isFemale ? new Vector3(0f, 0.003f, 0.098f) : new Vector3(0f, 0.007f, 0.102f);
                mouthGo.transform.localRotation = Quaternion.identity;
                mouthGo.transform.localScale = isFemale ? new Vector3(0.038f, 0.028f, 1f) : new Vector3(0.042f, 0.032f, 1f);

                var mf = mouthGo.AddComponent<MeshFilter>();
                var mr = mouthGo.AddComponent<MeshRenderer>();

                var qm = new Mesh();
                qm.name = "AnimeMouthQuad";
                qm.vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                };
                qm.uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                };
                qm.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
                qm.RecalculateNormals();
                mf.sharedMesh = qm;

                var shader = Shader.Find("BattleSummoner/Anime_MouthCavity");
                var mouthMat = new Material(shader);
                if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
                string matPath = isFemale ? "Assets/Materials/M_Anime_MouthCavity_Female.mat" : "Assets/Materials/M_Anime_MouthCavity_Male.mat";
                AssetDatabase.CreateAsset(mouthMat, matPath);
                mr.sharedMaterial = mouthMat;
            }

            // Attach & link FacialExpressionController
            var faceController = prefabRoot.GetComponent<FacialExpressionController>();
            if (faceController == null) faceController = prefabRoot.AddComponent<FacialExpressionController>();

            faceController.targetSkinnedMesh = smr;
            var cavityTransform = headTransform != null ? headTransform.Find("Anime_Mouth_Cavity") : null;
            faceController.mouthCavityRenderer = cavityTransform != null ? cavityTransform.GetComponent<MeshRenderer>() : null;
            faceController.jawBone = jaw;
            faceController.lipUpperBone = lipUp;
            faceController.lipLowerBone = lipLow;
            faceController.mouthCornerLeftBone = cornerL;
            faceController.mouthCornerRightBone = cornerR;
            faceController.browLeftBone = browBoneL;
            faceController.browRightBone = browBoneR;
            faceController.InitializeRenderer();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"[FACIAL RIG] Successfully configured {prefabPath} with {facialMesh.blendShapeCount} blendshapes and facial rig bones.");
        }

        private static Transform CreateBone(Transform parent, string name, Vector3 localPos)
        {
            var existing = parent.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var boneGo = new GameObject(name);
            boneGo.transform.SetParent(parent, false);
            boneGo.transform.localPosition = localPos;
            return boneGo.transform;
        }
    }
}
