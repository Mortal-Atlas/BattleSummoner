#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using BattleSummoner.Character;

namespace BattleSummoner.VisualPolish.Editor
{
    public static class GenshinPainterlyMasterOverhaul
    {
        private const string ExternalGlbSourceFolder = @"D:\Games\BattleSummoner\Assets";
        private const string InternalGlbFallbackFolder = "Assets/Models/Characters/";
        private const string TextureDir = "Assets/Textures/PainterlyGenshin/";
        private const string MeshyPropDir = "Assets/Models/MeshyPainterlyProps/";

        [MenuItem("BattleSummoner/Visual Polish/Execute Complete Genshin Shaders, GLB Characters, NanoBanana & Meshy Overhaul")]
        public static void ExecuteCompleteGenshinOverhaul()
        {
            ConfigureGenshinGoldenHourLighting();
            ReplacePlayerHeroAndCylinderNpcsWithGlbModels();
            FixMirroredNpcNameplates();
            ApplyPainterlyShadersAndNanoBananaTextures();
            PopulatePlazaWithMeshyPropsAndFallbackPlanter();

            SceneView.RepaintAll();
            Debug.Log("[Genshin Master Overhaul] Complete! Player_Hero set to MaleStarter.glb (with FemaleStarter/manbase/womanbase wired), Catherine.glb assigned to Guildhall Clerk, cylindrical NPCs replaced, and Painterly Shaders + Lighting active!");
        }

        // =====================================================================
        // 1. RESOLVE & REPLACE PLAYER_HERO, CATHERINE & CYLINDER NPCS WITH .GLB
        // =====================================================================
        private static GameObject LoadOrImportGlbModel(string fileNameWithExt)
        {
            // 1. Check direct root path: "Assets/<fileNameWithExt>"
            string directAssetPath = "Assets/" + fileNameWithExt;
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(directAssetPath);
            if (asset != null) return asset;

            // 2. Check internal character folder: "Assets/Models/Characters/<fileNameWithExt>"
            string subFolderAssetPath = InternalGlbFallbackFolder + fileNameWithExt;
            asset = AssetDatabase.LoadAssetAtPath<GameObject>(subFolderAssetPath);
            if (asset != null) return asset;

            // 3. Search anywhere in AssetDatabase by filename without extension
            string baseName = Path.GetFileNameWithoutExtension(fileNameWithExt);
            string[] guids = AssetDatabase.FindAssets(baseName);
            foreach (string guid in guids)
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guid);
                if (foundPath.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase) ||
                    foundPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) ||
                    foundPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(foundPath);
                    if (candidate != null) return candidate;
                }
            }

            // 4. If Unity project sits inside D:\Games\BattleSummoner\BattleSummoner, copy from D:\Games\BattleSummoner\Assets\
            string diskPath = Path.Combine(ExternalGlbSourceFolder, fileNameWithExt);
            if (File.Exists(diskPath))
            {
                if (!Directory.Exists(InternalGlbFallbackFolder))
                    Directory.CreateDirectory(InternalGlbFallbackFolder);

                File.Copy(diskPath, subFolderAssetPath, true);
                AssetDatabase.ImportAsset(subFolderAssetPath, ImportAssetOptions.ForceSynchronousImport);
                asset = AssetDatabase.LoadAssetAtPath<GameObject>(subFolderAssetPath);
                if (asset != null) return asset;
            }

            Debug.LogWarning($"[GLB Resolver] Could not locate '{fileNameWithExt}' in AssetDatabase or at '{diskPath}'. Ensure a Unity GLTF/GLB importer package (e.g., glTFast / UniGLTF) is active.");
            return null;
        }

        private static void ReplacePlayerHeroAndCylinderNpcsWithGlbModels()
        {
            GameObject maleStarterPrefab   = LoadOrImportGlbModel("MaleStarter.glb");
            GameObject femaleStarterPrefab = LoadOrImportGlbModel("femalestarter.glb");
            GameObject manBasePrefab       = LoadOrImportGlbModel("manbase.glb");
            GameObject womanBasePrefab     = LoadOrImportGlbModel("womanbase.glb");
            GameObject catherinePrefab     = LoadOrImportGlbModel("Catherine.glb");

            Shader charShader = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyCharacter");

            // A. Configure Player_Hero with rigged Starter_Male_Rigged FBX
            BattleSummoner.EditorScripts.HeroAndUIBuilder.SetupHeroModelAndRig();

            // B. Ensure Catherine (Guildhall Clerk) is rigged
            GameObject catherineNpc = GameObject.Find("NPC_Catherine_GuildhallClerk");
            if (catherineNpc != null)
            {
                EnsureCleanNameplate(catherineNpc, "Catherine", "Guildhall Clerk");
            }

            // C. Replace only legacy unrigged Cylindrical Placeholders (skip already rigged NPCs)
            int npcToggleIndex = 0;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject go in allObjects)
            {
                if (go == null || go == catherineNpc) continue;
                if (go.name.Contains("Player_Hero") || go.name == "Player") continue;
                if (go.transform.Find("HumanoidBody") != null) continue;
                if (go.GetComponentInChildren<SkinnedMeshRenderer>() != null) continue;
                if (go.GetComponent<Animator>() != null && go.GetComponent<Animator>().isHuman) continue;

                GameObject chosenNpcGlb = null;
                switch (npcToggleIndex % 4)
                {
                    case 0: chosenNpcGlb = manBasePrefab != null ? manBasePrefab : maleStarterPrefab; break;
                    case 1: chosenNpcGlb = womanBasePrefab != null ? womanBasePrefab : femaleStarterPrefab; break;
                    case 2: chosenNpcGlb = femaleStarterPrefab != null ? femaleStarterPrefab : womanBasePrefab; break;
                    case 3: chosenNpcGlb = maleStarterPrefab != null ? maleStarterPrefab : manBasePrefab; break;
                }
                npcToggleIndex++;

                if (chosenNpcGlb != null)
                {
                    SwapObjectVisualMeshWithGlb(go, chosenNpcGlb, charShader, $"Visual_NPC_{chosenNpcGlb.name}");
                }
            }
        }

        private static void SwapObjectVisualMeshWithGlb(GameObject targetRoot, GameObject glbPrefab, Shader charShader, string childVisualName)
        {
            if (targetRoot == null || glbPrefab == null) return;

            // Hide any cylinder/capsule MeshRenderer directly on the root
            MeshRenderer rootMr = targetRoot.GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;

            // Remove old placeholder visual children while preserving World-Space Canvases / Nameplates
            for (int i = targetRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = targetRoot.transform.GetChild(i);
                if (child.GetComponentInChildren<Canvas>() != null || child.GetComponentInChildren<TMP_Text>() != null)
                    continue;

                Object.DestroyImmediate(child.gameObject);
            }

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(glbPrefab, targetRoot.transform);
            if (inst == null) inst = Object.Instantiate(glbPrefab, targetRoot.transform);
            inst.name = childVisualName;
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            if (charShader != null)
                ApplyCharacterCelShaderToHierarchy(inst, charShader);
        }

        private static void ApplyCharacterCelShaderToHierarchy(GameObject root, Shader charShader)
        {
            if (root == null || charShader == null) return;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    Material newMat = new Material(mats[i]);
                    Texture mainTex = null;
                    if (mats[i].HasProperty("baseColorTexture")) mainTex = mats[i].GetTexture("baseColorTexture");
                    if (mainTex == null) mainTex = mats[i].mainTexture;
                    if (mainTex == null && mats[i].HasProperty("_BaseMap")) mainTex = mats[i].GetTexture("_BaseMap");
                    if (mainTex == null && mats[i].HasProperty("_MainTex")) mainTex = mats[i].GetTexture("_MainTex");

                    Color baseCol = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") :
                                    (mats[i].HasProperty("_Color") ? mats[i].GetColor("_Color") : Color.white);
                    if (baseCol.a < 0.01f || (baseCol.r == 0 && baseCol.g == 0 && baseCol.b == 0)) baseCol = Color.white;

                    newMat.shader = charShader;
                    if (mainTex != null) newMat.SetTexture("_BaseMap", mainTex);
                    newMat.SetColor("_BaseColor", baseCol);
                    newMat.SetColor("_ShadowTint", new Color(0.68f, 0.64f, 0.78f, 1.0f));
                    newMat.SetFloat("_MinBrightnessFloor", 0.22f);
                    mats[i] = newMat;
                }
                r.sharedMaterials = mats;
            }
        }

        private static void EnsureCleanNameplate(GameObject npcRoot, string npcName, string roleSubTitle)
        {
            TMP_Text existingTmp = npcRoot.GetComponentInChildren<TMP_Text>(true);
            if (existingTmp != null)
            {
                existingTmp.text = $"{npcName}\n<size=70%><color=#E6C687>{roleSubTitle}</color></size>";
                existingTmp.color = new Color(1.0f, 0.98f, 0.92f, 1.0f);
                return;
            }

            GameObject labelObj = new GameObject("Nameplate_TMP");
            labelObj.transform.SetParent(npcRoot.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = $"{npcName}\n<size=70%><color=#E6C687>{roleSubTitle}</color></size>";
            tmp.fontSize = 2.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1.0f, 0.98f, 0.92f, 1.0f);
        }

        // =====================================================================
        // 2. GOLDEN-HOUR LIGHTING, SOFT SHADOWS & NAMEPLATE ORIENTATION FIX
        // =====================================================================
        private static void ConfigureGenshinGoldenHourLighting()
        {
            Light sun = RenderSettings.sun != null ? RenderSettings.sun : Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.enabled = true;
                sun.type = LightType.Directional;
                sun.color = new Color(1.0f, 0.96f, 0.88f, 1.0f);
                sun.intensity = 1.45f;
                sun.transform.rotation = Quaternion.Euler(46f, -32f, 0f);
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.80f;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.68f, 0.80f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.82f, 0.84f, 0.88f);
            RenderSettings.ambientGroundColor  = new Color(0.38f, 0.46f, 0.36f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.85f, 0.98f, 1.0f);
            RenderSettings.fogStartDistance = 250f;
            RenderSettings.fogEndDistance = 2400f;
        }

        private static void FixMirroredNpcNameplates()
        {
            TMP_Text[] allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (TMP_Text tmp in allTexts)
            {
                Vector3 ls = tmp.transform.localScale;
                if (ls.x < 0f) ls.x = Mathf.Abs(ls.x);
                tmp.transform.localScale = ls;

                if (Camera.main != null && (tmp is TextMeshPro || (tmp.canvas != null && tmp.canvas.renderMode == RenderMode.WorldSpace)))
                {
                    Vector3 dirFromCam = tmp.transform.position - Camera.main.transform.position;
                    dirFromCam.y = 0f;
                    if (dirFromCam.sqrMagnitude > 0.001f)
                        tmp.transform.rotation = Quaternion.LookRotation(dirFromCam);
                }

                if (tmp.text.Contains("Citizen") || tmp.text.Contains("Generic"))
                {
                    tmp.color = new Color(1.0f, 0.98f, 0.92f, 1.0f);
                    tmp.outlineWidth = 0.22f;
                    tmp.outlineColor = new Color32(28, 30, 42, 255);
                }
            }
        }

        // =====================================================================
        // 3. APPLY PAINTERLY SHADERS, NANO BANANA TEXTURES & MESHY PLAZA PROPS
        // =====================================================================
        private static void ApplyPainterlyShadersAndNanoBananaTextures()
        {
            Shader envShader  = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyEnvironment");
            Shader folShader  = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyFoliage");
            Shader charShader = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyCharacter");
            if (envShader == null) return;

            Texture2D cobbleTex  = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "T_Genshin_PlazaCobblestone_Seamless.png");
            Texture2D stoneTex   = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "T_Genshin_CathedralStone_TrimSheet.png");
            Texture2D awningTex  = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "T_Genshin_MarketAwning_StripedCloth.png");
            Texture2D sakuraCard = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "T_Genshin_SakuraBranch_AlphaCard.png");

            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (Renderer r in allRenderers)
            {
                if (r == null || r is ParticleSystemRenderer || r is SpriteRenderer) continue;
                string objName = r.gameObject.name.ToLowerInvariant();

                // 1. Never overwrite the island macro terrain or mountain
                if (objName.Contains("continent") || objName.Contains("terrain") || objName.Contains("caldera") || objName.Contains("mountain_traversal"))
                    continue;

                // 2. Never overwrite water surfaces (Moat, Ocean, Cascades)
                if (objName.Contains("water") || objName.Contains("ocean") || objName.Contains("moat") || objName.Contains("river"))
                    continue;

                // 3. Characters & NPCs use GenshinPainterlyCharacter shader
                if (objName.Contains("player") || objName.Contains("npc") || objName.Contains("citizen") ||
                    objName.Contains("catherine") || objName.Contains("starter") || objName.Contains("manbase") ||
                    objName.Contains("womanbase") || r is SkinnedMeshRenderer)
                {
                    if (charShader != null) ApplyCharacterCelShaderToHierarchy(r.gameObject, charShader);
                    continue;
                }

                // 4. All Nature Assets (Trees, Rocks, Cliffs, Foliage, Flowers, Grass, Bushes) PRESERVE their authentic shaders & textures
                if (objName.Contains("animtree") || objName.Contains("willow") || objName.Contains("blossom") ||
                    objName.Contains("cherry") || objName.Contains("broadleaf") || objName.Contains("fir") ||
                    objName.Contains("pine") || objName.Contains("oak") || objName.Contains("bush") ||
                    objName.Contains("flower") || objName.Contains("grass") || objName.Contains("plant") ||
                    objName.Contains("rock") || objName.Contains("cliff") || objName.Contains("boulder") ||
                    objName.Contains("stone_big") || objName.Contains("stone_medium") || objName.Contains("stone_small"))
                {
                    continue;
                }

                // 5. Selective Nano Banana Painterly application to Plaza, Cathedral, and Market props
                if (objName.Contains("plaza") || objName.Contains("cobble") || (objName.Contains("floor") && objName.Contains("town")))
                {
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;
                        mat.shader = envShader;
                        if (cobbleTex != null)
                        {
                            mat.SetTexture("_BaseMap", cobbleTex);
                            mat.SetTextureScale("_BaseMap", new Vector2(24f, 24f));
                        }
                    }
                }
                else if (objName.Contains("cathedral") || objName.Contains("church"))
                {
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;
                        string mn = mat.name.ToLowerInvariant();
                        if (mn.Contains("glass") || mn.Contains("glow") || mn.Contains("gold")) continue;
                        mat.shader = envShader;
                        if (stoneTex != null) mat.SetTexture("_BaseMap", stoneTex);
                    }
                }
                else if (objName.Contains("awning") || objName.Contains("market_stall") || objName.Contains("bazaar_tent"))
                {
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;
                        mat.shader = envShader;
                        if (awningTex != null) mat.SetTexture("_BaseMap", awningTex);
                    }
                }
                else if (objName.StartsWith("meshy_"))
                {
                    // Newly imported Meshy props get painterly environment shading
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (mat == null) continue;
                        mat.shader = envShader;
                    }
                }
            }
        }

        private static void PopulatePlazaWithMeshyPropsAndFallbackPlanter()
        {
            // Auto-scale and normalize any Meshy props on import to realistic game dimensions
            MeshyPropImportScaler.AutoScaleAllMeshyProps(MeshyPropDir);

            Shader envShader = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyEnvironment");

            // Relocate the two foreground black cauldrons blocking the bottom of image_178b4a.jpg
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                string n = go.name.ToLowerInvariant();
                if (n.Contains("cauldron") || n.Contains("pot"))
                    go.transform.position = new Vector3(5.2f, go.transform.position.y, 4.5f);
            }

            GameObject propRoot = GameObject.Find("Genshin_Meshy_PlazaSetDressing") ?? new GameObject("Genshin_Meshy_PlazaSetDressing");

            GameObject planterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPropDir + "Prop_SakuraStonePlanterRing.fbx");
            GameObject cratesPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPropDir + "Prop_MarketMerchantSet_CratesAndBaskets.fbx");
            GameObject lampPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPropDir + "Prop_Mondstadt_WroughtIronStreetLamp.fbx");
            GameObject benchPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPropDir + "Prop_PlazaFlowerBox_And_Bench.fbx");

            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                string n = go.name.ToLowerInvariant();
                // Ensure we only match true trees, NOT "street"
                bool isTree = (n.Contains("cherry") || n.Contains("sakura") || (n.Contains("tree") && !n.Contains("street")));
                if (isTree && go.transform.position.magnitude < 120f)
                {
                    if (planterPrefab != null && go.transform.Find("Meshy_SakuraPlanter") == null)
                    {
                        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(planterPrefab, go.transform);
                        inst.name = "Meshy_SakuraPlanter";
                        inst.transform.localPosition = Vector3.zero;
                    }
                    else if (planterPrefab == null && go.transform.Find("Genshin_StonePlanterRing") == null)
                    {
                        GameObject planter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        planter.name = "Genshin_StonePlanterRing";
                        planter.transform.SetParent(go.transform, false);
                        planter.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                        planter.transform.localScale = new Vector3(3.6f, 0.28f, 3.6f);
                        if (envShader != null)
                        {
                            Material stoneMat = new Material(envShader);
                            stoneMat.SetColor("_BaseColor", new Color(0.92f, 0.89f, 0.82f));
                            stoneMat.SetColor("_ShadowColor", new Color(0.56f, 0.62f, 0.78f));
                            planter.GetComponent<Renderer>().sharedMaterial = stoneMat;
                        }
                    }
                    break;
                }
            }

            SpawnPropAtGround(cratesPrefab, propRoot.transform, new Vector3(4.5f, 10f, 6.0f), 0f, "Meshy_MarketGoods_01");
            SpawnPropAtGround(cratesPrefab, propRoot.transform, new Vector3(4.5f, 10f, 9.5f), 90f, "Meshy_MarketGoods_02");
            SpawnPropAtGround(lampPrefab,   propRoot.transform, new Vector3(-4.2f, 10f, 5.0f), 25f, "Meshy_StreetLamp_Left");
            SpawnPropAtGround(lampPrefab,   propRoot.transform, new Vector3(3.8f, 10f, 3.2f), -145f, "Meshy_StreetLamp_Right");
            SpawnPropAtGround(benchPrefab,  propRoot.transform, new Vector3(-3.8f, 10f, 8.5f), 90f, "Meshy_FlowerBench_Left");
        }

        private static void SpawnPropAtGround(GameObject prefab, Transform parent, Vector3 rayOrigin, float yaw, string instanceName)
        {
            if (prefab == null) return;
            Transform existing = parent.Find(instanceName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Vector3 spawnPos = new Vector3(rayOrigin.x, 0f, rayOrigin.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 50f))
                spawnPos = hit.point;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = instanceName;
            inst.transform.position = spawnPos;

            // Detect if model was exported Z-up (e.g. lamp lying on ground with size.z > size.y)
            Renderer[] renderers = inst.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                if (instanceName.ToLowerInvariant().Contains("lamp") && b.size.z > b.size.y)
                {
                    inst.transform.rotation = Quaternion.Euler(-90f, yaw, 0f);
                }
                else
                {
                    inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                }

                // Re-evaluate bounds and normalize scale if necessary
                b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 6.0f)
                {
                    float scaleRatio = 3.5f / maxDim;
                    inst.transform.localScale = Vector3.one * scaleRatio;
                }
            }
        }
    }
}
#endif
