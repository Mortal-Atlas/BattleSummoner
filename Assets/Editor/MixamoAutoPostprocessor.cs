using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BattleSummoner.EditorTools
{
    public class MixamoAutoPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("Assets/Animations/Mixamo") || !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                return;

            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null) return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
        }

        private void OnPreprocessAnimation()
        {
            string normPath = assetPath.Replace('\\', '/');
            if (!normPath.Contains("Assets/Animations/Mixamo") || !normPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                return;

            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null) return;

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            string lower = fileName.ToLowerInvariant();
            bool isInPlace = normPath.Contains("/InPlace/");

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.clipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                foreach (var clip in clips)
                {
                    if (clip.name.Equals("mixamo.com", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(clip.name))
                    {
                        clip.name = fileName;
                    }

                    if (lower.Contains("idle") || lower.Contains("run") || lower.Contains("walk") || lower.Contains("sprint") || lower.Contains("strafe"))
                    {
                        clip.loopTime = true;
                    }

                    clip.lockRootHeightY = true;
                    clip.keepOriginalPositionY = true;

                    if (isInPlace)
                    {
                        clip.lockRootRotation = true;
                        clip.lockRootPositionXZ = true;
                        clip.keepOriginalOrientation = true;
                        clip.keepOriginalPositionXZ = true;
                    }
                }
                importer.clipAnimations = clips;
            }
        }
    }
}

