#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace BattleSummoner.Character.FacialAnimation.Editor
{
    public static class EditorModularFaceRigBuilder
    {
        [MenuItem("BattleSummoner/Character/1. Auto-Rig 2-Channel Face Nodes on Selected Character")]
        public static void BuildFaceRigOnSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[FaceRig Builder] Please select a character root or Head GameObject in the Hierarchy.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(selected, "Auto-Rig 2-Channel Face");

            Transform headAnchor = FindHeadTransform(selected.transform);

            // 1. Create or locate FaceRig_SortingGroup container
            Transform groupTransform = headAnchor.Find("FaceRig_SortingGroup");
            GameObject groupObj;
            if (groupTransform == null)
            {
                groupObj = new GameObject("FaceRig_SortingGroup");
                groupObj.transform.SetParent(headAnchor, false);
                // Normalized head-top offset for full-body 2D A-pose base sprites (0.865 Y normalized height)
                // If attached directly to a 3D Head bone, localPosition is close to head front
                bool isHeadBone = headAnchor != selected.transform;
                groupObj.transform.localPosition = isHeadBone 
                    ? new Vector3(0f, 0f, 0.088f) 
                    : new Vector3(0f, 1.48f, 0f);
            }
            else
            {
                groupObj = groupTransform.gameObject;
            }

            SortingGroup sortingGroup = groupObj.GetComponent<SortingGroup>();
            if (sortingGroup == null) sortingGroup = groupObj.AddComponent<SortingGroup>();
            sortingGroup.sortingOrder = 10;

            // 2. Create Channel 1: UpperFace_EyesBrows
            SpriteRenderer upperRenderer = GetOrCreateChildSpriteRenderer(
                groupObj.transform,
                "UpperFace_EyesBrows",
                new Vector3(0f, 0.142f, 0f),
                10);
            upperRenderer.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

            // 3. Create Channel 2: LowerFace_Mouth
            SpriteRenderer lowerRenderer = GetOrCreateChildSpriteRenderer(
                groupObj.transform,
                "LowerFace_Mouth",
                new Vector3(0f, 0.075f, 0.001f),
                10);
            lowerRenderer.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);

            // 4. Attach & wire ModularFaceRigController
            ModularFaceRigController controller = selected.GetComponent<ModularFaceRigController>();
            if (controller == null) controller = selected.AddComponent<ModularFaceRigController>();

            controller.renderMode = ModularFaceRigController.FaceRenderMode.SpriteRendererStack2D;
            controller.headAnchorTransform = groupObj.transform;
            controller.faceSortingGroup = sortingGroup;
            controller.eyesAndBrowsRenderer = upperRenderer;
            controller.mouthRenderer = lowerRenderer;

            // Auto-bind default Expression DB if available
            var defaultDB = AssetDatabase.LoadAssetAtPath<FacialExpressionDatabaseSO>("Assets/Data/FacialAnimation/DefaultFacialExpressionDB.asset");
            if (defaultDB != null) controller.expressionDB = defaultDB;

            controller.ApplySpatialAnchors();

            EditorUtility.SetDirty(selected);
            Debug.Log($"[FaceRig Builder] Successfully rigged 2-channel face (Eyes/Brows + Mouth) on '{selected.name}'.");
        }

        [MenuItem("BattleSummoner/Character/2. Generate Single-Draw-Call CharacterFaceAtlas")]
        public static void GenerateCharacterFaceSpriteAtlas()
        {
            const string folderPath = "Assets/Sprites/CharacterFaces";
            const string atlasPath = "Assets/Sprites/CharacterFaces/CharacterFaceAtlas.spriteatlas";

            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
                AssetDatabase.CreateFolder("Assets", "Sprites");
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets/Sprites", "CharacterFaces");

            SpriteAtlas atlas = new SpriteAtlas();
            SpriteAtlasPackingSettings packSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 4
            };
            atlas.SetPackingSettings(packSettings);

            Object faceFolderObj = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (faceFolderObj != null)
            {
                atlas.Add(new[] { faceFolderObj });
            }

            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FaceRig Builder] Created single-draw-call SpriteAtlas at '{atlasPath}'. Place all Eye/Brow and Mouth sprites inside '{folderPath}'.");
        }

        private static Transform FindHeadTransform(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n == "head" || n.EndsWith("_head") || n.Contains("head_anchor") || n.Contains("face_anchor"))
                    return t;
            }
            return root;
        }

        private static SpriteRenderer GetOrCreateChildSpriteRenderer(Transform parent, string nodeName, Vector3 localOffset, int sortingOrder)
        {
            Transform child = parent.Find(nodeName);
            GameObject obj;
            if (child == null)
            {
                obj = new GameObject(nodeName);
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = child.gameObject;
            }

            obj.transform.localPosition = localOffset;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
#endif
