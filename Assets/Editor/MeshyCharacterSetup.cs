using System.IO;
using UnityEditor;
using UnityEngine;

public static class MeshyCharacterSetup
{
    [MenuItem("BattleSummoner/Setup Meshy Rigged Character")]
    public static void SetupRiggedCharacter()
    {
        string fbxPath = "Assets/Models/Meshy/Character_MeshyHero_Rigged.fbx";
        string texPath = "Assets/Models/Meshy/Image_0.png";
        string matPath = "Assets/Models/Meshy/M_MeshyHero.mat";
        string prefabDir = "Assets/Prefabs/Characters";
        string prefabPath = $"{prefabDir}/Hero_MeshySummoner.prefab";
        string artifactDir = "Assets/Data/Artifacts";
        string artifactPath = $"{artifactDir}/Artifact_HeroSummoner.asset";

        if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);
        if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

        // 1. Configure ModelImporter
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.SaveAndReimport();
            Debug.Log("[MESHY SETUP] ModelImporter configured with Humanoid Avatar.");
        }

        // 2. Create / Setup URP Material
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            mat = new Material(urpLit);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        Texture2D albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (albedoTex != null)
        {
            mat.SetTexture("_BaseMap", albedoTex);
            mat.SetTexture("_MainTex", albedoTex);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
        }

        // 3. Create 12D DimensionalArtifact ScriptableObject
        DimensionalArtifact artifact = AssetDatabase.LoadAssetAtPath<DimensionalArtifact>(artifactPath);
        if (artifact == null)
        {
            artifact = ScriptableObject.CreateInstance<DimensionalArtifact>();
            AssetDatabase.CreateAsset(artifact, artifactPath);
        }

        artifact.baseClassName_8D = "Hero_MeshySummoner";
        artifact.semanticDescription_9D = "An immovable scholar of the Spire, wielding spite and heavy kinetic inertia through runic alchemy.";
        artifact.uiIcon_8D = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/DarkAcademia/summoner_hero_alchemist_portrait.jpg");
        artifact.Compile9DMetadata();
        EditorUtility.SetDirty(artifact);

        // 4. Instantiate and build Prefab
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            instance.name = "Hero_MeshySummoner";

            // Apply Material to renderers
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // Ensure AudioSource
            AudioSource audio = instance.GetComponent<AudioSource>();
            if (audio == null) audio = instance.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;

            // Ensure DimensionalRuntimeExecutor
            DimensionalRuntimeExecutor executor = instance.GetComponent<DimensionalRuntimeExecutor>();
            if (executor == null) executor = instance.AddComponent<DimensionalRuntimeExecutor>();
            executor.artifactData = artifact;
            executor.isSummonedFamiliar = true;

            // Ensure CapsuleCollider
            CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
            if (col == null) col = instance.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.95f, 0);
            col.height = 1.9f;
            col.radius = 0.4f;

            artifact.visualPrefab_8D = fbxAsset;
            EditorUtility.SetDirty(artifact);

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"[MESHY SETUP] Successfully created Rigged Character Prefab at '{prefabPath}' with 12D Artifact binding!");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
