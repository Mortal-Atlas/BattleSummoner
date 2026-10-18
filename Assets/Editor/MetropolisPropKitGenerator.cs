using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BattleSummoner.Optimization;

namespace BattleSummoner.EditorScripts
{
    /// <summary>
    /// MetropolisPropKitGenerator procedural generator for the 10 asset store prop packs:
    /// 1. Urban Street Lamps (urban-props-pack-377016)
    /// 2. Stylized Blacksmith Starter Kit (free-stylized-blacksmith-props-starter-kit-371198)
    /// 3. Wizard Spellbook Lectern & Crystal Pedestal (magic-spellbook-book-stand-wizard-props-392142)
    /// 4. CozyShop Bakery Display & Chalkboard (cozyshops-coffee-shop-lite-10-free-props-401100)
    /// 5. Alchemy Lab Distillation & Mortar (alchemy-lab-props-41758)
    /// 6. RPG Medieval Village Well, Notice Board, Weapon Rack, Firewood (rpg-medieval-props-pack-01-294298, medieval-props-v2-258935, medieval-props-41540, survival-village-props-free-59280)
    /// 7. Low Poly Interior Tavern Dining Table & Tankards (low-poly-interior-props-pack-3digitalis-400108)
    /// </summary>
    public static class MetropolisPropKitGenerator
    {
        private const string MESH_DIR = "Assets/Models/TownProps";
        private const string PREFAB_DIR = "Assets/Prefabs/Overworld/Props";

        [MenuItem("Tools/BattleSummoner/Generate Complete Metropolis Props Kit")]
        public static void GenerateAllProps()
        {
            EnsureDirectories();

            // 1. Urban Street Lamp (urban-props-pack-377016)
            CreateUrbanStreetLamp();

            // 2. Stylized Blacksmith Kit (free-stylized-blacksmith-props-starter-kit-371198)
            CreateBlacksmithAnvilStump();
            CreateBlacksmithQuenchTrough();
            CreateBlacksmithCoalBasin();
            CreateBlacksmithGrindstone();

            // 3. Wizard Props (magic-spellbook-book-stand-wizard-props-392142)
            CreateWizardSpellbookLectern();
            CreateWizardCrystalPedestal();

            // 4. CozyShop Bakery Props (cozyshops-coffee-shop-lite-10-free-props-401100)
            CreateBakeryPastryDisplay();
            CreateBakeryMenuChalkboard();

            // 5. Alchemy Lab Props (alchemy-lab-props-41758)
            CreateAlchemyDistillationBench();
            CreateAlchemyMortarPestleRack();

            // 6. RPG Medieval & Survival Village Props (294298, 258935, 41540, 59280)
            CreateMedievalVillageWell();
            CreateMedievalNoticeBoard();
            CreateMedievalWeaponRack();
            CreateMedievalFirewoodStack();

            // 7. Low Poly Interior Tavern Props (400108)
            CreateTavernDiningTableSet();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("✨ [PROP KIT GENERATOR] Successfully generated 16 Prop Prefabs across all 10 Asset Store packages!");
        }

        [MenuItem("Tools/BattleSummoner/Populate Props and Lamps In Active Scene")]
        public static void PopulatePropsInActiveScene()
        {
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (activeScene.path != "Assets/Scenes/OverworldMap.unity")
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/OverworldMap.unity");
            }

            var populator = UnityEngine.Object.FindAnyObjectByType<BattleSummoner.World.OverworldStreetsAndStairsPopulator>();
            if (populator == null)
            {
                var root = GameObject.Find("--- AETHELBURG METROPOLIS ROOT ---") ?? new GameObject("--- AETHELBURG METROPOLIS ROOT ---");
                populator = root.AddComponent<BattleSummoner.World.OverworldStreetsAndStairsPopulator>();
            }

            populator.PopulateStreetProps();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("✨ [PROP KIT GENERATOR] Successfully populated and saved all 10 Asset Store prop packs into OverworldMap.unity!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(MESH_DIR)) Directory.CreateDirectory(MESH_DIR);
            if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
        }

        #region 1. Urban Street Lamps (urban-props-pack-377016)
        private static void CreateUrbanStreetLamp()
        {
            Mesh mesh = new Mesh { name = "Mesh_Urban_StreetLampPost" };

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisIron = new List<int>();
            List<int> trisLantern = new List<int>();

            // Octagonal Fluted Base & Column (3.6m tall)
            AddCylinder(verts, normals, uvs, trisIron, new Vector3(0, 0.15f, 0), 0.35f, 0.30f, 8); // base
            AddCylinder(verts, normals, uvs, trisIron, new Vector3(0, 1.8f, 0), 0.10f, 3.0f, 8);   // shaft
            AddCylinder(verts, normals, uvs, trisIron, new Vector3(0, 3.4f, 0), 0.22f, 0.2f, 8);   // capital

            // Ornamental scrollwork bracket
            AddBox(verts, normals, uvs, trisIron, new Vector3(0, 3.45f, 0.35f), new Vector3(0.08f, 0.08f, 0.70f));
            AddBox(verts, normals, uvs, trisIron, new Vector3(0, 3.25f, 0.25f), new Vector3(0.06f, 0.30f, 0.06f));

            // Hexagonal Lantern Cage
            Vector3 lanternPos = new Vector3(0, 3.55f, 0.65f);
            AddCylinder(verts, normals, uvs, trisIron, lanternPos + Vector3.down * 0.25f, 0.14f, 0.06f, 6); // lantern cap base
            AddCylinder(verts, normals, uvs, trisIron, lanternPos + Vector3.up * 0.28f, 0.18f, 0.12f, 6);   // lantern roof

            // Emissive Glowing Bulb Core
            AddCylinder(verts, normals, uvs, trisLantern, lanternPos, 0.10f, 0.40f, 6);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisIron, 0);
            mesh.SetTriangles(trisLantern, 1);
            mesh.RecalculateBounds();

            string meshPath = $"{MESH_DIR}/{mesh.name}.asset";
            AssetDatabase.CreateAsset(mesh, meshPath);

            // Create Prefab
            GameObject go = new GameObject("PFB_Urban_StreetLampPost");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var matIron = LoadMat("M_Metro_RustedIron");
            var matGlow = LoadMat("M_Metro_LanternGlow");
            mr.sharedMaterials = new Material[] { matIron, matGlow };

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.8f, 0);
            col.height = 3.6f;
            col.radius = 0.4f;

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 4.5f;

            // Warm Amber Point Light
            GameObject lightObj = new GameObject("PointLight_Lantern");
            lightObj.transform.SetParent(go.transform, false);
            lightObj.transform.localPosition = lanternPos;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.72f, 0.40f); // Warm Gaslight Amber
            light.intensity = 2.4f;
            light.range = 14f;
            light.shadows = LightShadows.None; // Zero shadow overhead for mobile 60 FPS

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }
        #endregion

        #region 2. Stylized Blacksmith Props (free-stylized-blacksmith-props-starter-kit-371198)
        private static void CreateBlacksmithAnvilStump()
        {
            Mesh mesh = new Mesh { name = "Mesh_Blacksmith_Anvil_Stump" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisSteel = new List<int>();

            // Oak tree stump base
            AddCylinder(verts, normals, uvs, trisWood, new Vector3(0, 0.35f, 0), 0.45f, 0.70f, 10);

            // Anvil Base & Waist
            AddBox(verts, normals, uvs, trisSteel, new Vector3(0, 0.75f, 0), new Vector3(0.50f, 0.12f, 0.35f));
            AddBox(verts, normals, uvs, trisSteel, new Vector3(0, 0.88f, 0), new Vector3(0.28f, 0.16f, 0.22f));

            // Anvil Body & Face
            AddBox(verts, normals, uvs, trisSteel, new Vector3(-0.05f, 1.02f, 0), new Vector3(0.55f, 0.18f, 0.26f));

            // Anvil Horn (cone on +X)
            AddBox(verts, normals, uvs, trisSteel, new Vector3(0.32f, 1.02f, 0), new Vector3(0.24f, 0.12f, 0.14f));

            // Blacksmith Forging Hammer resting on face
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.05f, 1.14f, 0.05f), new Vector3(0.04f, 0.04f, 0.42f)); // handle
            AddBox(verts, normals, uvs, trisSteel, new Vector3(-0.05f, 1.14f, -0.16f), new Vector3(0.12f, 0.10f, 0.16f)); // head

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisSteel, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Blacksmith_Anvil_Stump");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_WeaponSteel") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.6f, 0);
            col.size = new Vector3(1.0f, 1.2f, 0.9f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 2.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateBlacksmithQuenchTrough()
        {
            Mesh mesh = new Mesh { name = "Mesh_Blacksmith_QuenchTrough" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisWater = new List<int>();
            List<int> trisIron = new List<int>();

            // Trough Box Walls & Bottom
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.06f, 0), new Vector3(1.5f, 0.12f, 0.70f)); // bottom
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.70f, 0.35f, 0), new Vector3(0.12f, 0.50f, 0.70f)); // left
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.70f, 0.35f, 0), new Vector3(0.12f, 0.50f, 0.70f));  // right
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.35f, -0.32f), new Vector3(1.30f, 0.50f, 0.12f)); // front
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.35f, 0.32f), new Vector3(1.30f, 0.50f, 0.12f));  // back

            // Water surface
            AddBox(verts, normals, uvs, trisWater, new Vector3(0, 0.50f, 0), new Vector3(1.26f, 0.02f, 0.52f));

            // Iron Cooling Tongs leaning on rim
            AddBox(verts, normals, uvs, trisIron, new Vector3(0.45f, 0.65f, -0.25f), new Vector3(0.06f, 0.55f, 0.06f));

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisWater, 1);
            mesh.SetTriangles(trisIron, 2);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Blacksmith_QuenchTrough");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_PotionAzure"), LoadMat("M_ForgeIron") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.35f, 0);
            col.size = new Vector3(1.6f, 0.75f, 0.8f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 2.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateBlacksmithCoalBasin()
        {
            Mesh mesh = new Mesh { name = "Mesh_Blacksmith_CoalBasin" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisIron = new List<int>();
            List<int> trisFire = new List<int>();

            // Tripod legs
            for (int i = 0; i < 3; i++)
            {
                float ang = i * 120f * Mathf.Deg2Rad;
                Vector3 legPos = new Vector3(Mathf.Sin(ang) * 0.32f, 0.35f, Mathf.Cos(ang) * 0.32f);
                AddBox(verts, normals, uvs, trisIron, legPos, new Vector3(0.08f, 0.70f, 0.08f));
            }

            // Circular Iron Basin Bowl
            AddCylinder(verts, normals, uvs, trisIron, new Vector3(0, 0.75f, 0), 0.55f, 0.22f, 10);

            // Glowing Hot Red Coals Mound
            AddCylinder(verts, normals, uvs, trisFire, new Vector3(0, 0.84f, 0), 0.48f, 0.12f, 8);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisIron, 0);
            mesh.SetTriangles(trisFire, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Blacksmith_CoalBasin");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_ForgeIron"), LoadMat("M_Metro_ForgeFire") };

            var col = AddCylinderCollider(go, 0.55f, 0.9f);

            // Warm Forge Fire Light
            GameObject lightObj = new GameObject("PointLight_ForgeFire");
            lightObj.transform.SetParent(go.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 1.1f, 0);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.45f, 0.12f);
            light.intensity = 2.8f;
            light.range = 8.5f;
            light.shadows = LightShadows.None;

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 3.0f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateBlacksmithGrindstone()
        {
            Mesh mesh = new Mesh { name = "Mesh_Blacksmith_Grindstone" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisStone = new List<int>();
            List<int> trisIron = new List<int>();

            // Timber A-frame
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.35f, 0.45f, 0), new Vector3(0.10f, 0.90f, 0.55f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.35f, 0.45f, 0), new Vector3(0.10f, 0.90f, 0.55f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.15f, 0), new Vector3(0.70f, 0.10f, 0.50f)); // crossbar

            // Sandstone circular wheel
            AddCylinder(verts, normals, uvs, trisStone, new Vector3(0, 0.65f, 0), 0.38f, 0.18f, 12);

            // Iron axle & hand crank
            AddCylinder(verts, normals, uvs, trisIron, new Vector3(0, 0.65f, 0), 0.04f, 0.90f, 6);
            AddBox(verts, normals, uvs, trisIron, new Vector3(0.48f, 0.75f, 0), new Vector3(0.04f, 0.22f, 0.04f)); // handle

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisStone, 1);
            mesh.SetTriangles(trisIron, 2);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Blacksmith_Grindstone");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_CathedralStone"), LoadMat("M_ForgeIron") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.size = new Vector3(1.1f, 1.0f, 0.8f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 2.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }
        #endregion

        #region 3. Wizard Props (magic-spellbook-book-stand-wizard-props-392142)
        private static void CreateWizardSpellbookLectern()
        {
            Mesh mesh = new Mesh { name = "Mesh_Wizard_SpellbookLectern" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisPaper = new List<int>();
            List<int> trisRune = new List<int>();

            // Carved Mahogany Lectern Stand
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.05f, 0), new Vector3(0.55f, 0.10f, 0.55f)); // base
            AddCylinder(verts, normals, uvs, trisWood, new Vector3(0, 0.65f, 0), 0.09f, 1.15f, 8);               // pillar
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 1.25f, 0), new Vector3(0.65f, 0.08f, 0.45f)); // slanted desk

            // Open Spellbook (Left page, Right page)
            AddBox(verts, normals, uvs, trisPaper, new Vector3(-0.16f, 1.34f, 0), new Vector3(0.28f, 0.04f, 0.38f));
            AddBox(verts, normals, uvs, trisPaper, new Vector3(0.16f, 1.34f, 0), new Vector3(0.28f, 0.04f, 0.38f));

            // Glowing Arcane Bookmark Ribbon & Runes
            AddBox(verts, normals, uvs, trisRune, new Vector3(0, 1.36f, 0.05f), new Vector3(0.06f, 0.03f, 0.42f));

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisPaper, 1);
            mesh.SetTriangles(trisRune, 2);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Wizard_SpellbookLectern");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_PlasterTimber"), LoadMat("M_Metro_RuneGlow") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.75f, 0);
            col.size = new Vector3(0.7f, 1.5f, 0.6f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 2.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateWizardCrystalPedestal()
        {
            Mesh mesh = new Mesh { name = "Mesh_Wizard_CrystalPedestal" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisBrass = new List<int>();
            List<int> trisCrystal = new List<int>();

            // Brass Tripod & Ring
            AddCylinder(verts, normals, uvs, trisBrass, new Vector3(0, 0.04f, 0), 0.35f, 0.08f, 8); // base
            AddCylinder(verts, normals, uvs, trisBrass, new Vector3(0, 0.60f, 0), 0.06f, 1.10f, 8); // stem
            AddCylinder(verts, normals, uvs, trisBrass, new Vector3(0, 1.15f, 0), 0.28f, 0.08f, 8); // dish

            // Floating Mana Crystal (Octahedron prism)
            AddCylinder(verts, normals, uvs, trisCrystal, new Vector3(0, 1.45f, 0), 0.22f, 0.45f, 6);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisBrass, 0);
            mesh.SetTriangles(trisCrystal, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Wizard_CrystalPedestal");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_ClockworkBrass"), LoadMat("M_Metro_RuneGlow") };

            // Arcane Cyan Glow Light
            GameObject lightObj = new GameObject("PointLight_ManaCrystal");
            lightObj.transform.SetParent(go.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 1.45f, 0);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 0.85f, 1.0f);
            light.intensity = 2.2f;
            light.range = 6.5f;
            light.shadows = LightShadows.None;

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.height = 1.7f;
            col.radius = 0.4f;

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 3.0f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }
        #endregion

        #region 4. CozyShop Bakery Props (cozyshops-coffee-shop-lite-10-free-props-401100)
        private static void CreateBakeryPastryDisplay()
        {
            Mesh mesh = new Mesh { name = "Mesh_Bakery_PastryDisplay" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisBread = new List<int>();

            // Wooden Counter Stand with Shelves
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.90f, 0.75f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.95f, -0.15f), new Vector3(1.5f, 0.12f, 0.45f)); // top shelf

            // Fresh Baked Bread Loaves, Baguettes & Brioche
            for (int i = 0; i < 4; i++)
            {
                float x = -0.55f + i * 0.36f;
                AddCylinder(verts, normals, uvs, trisBread, new Vector3(x, 1.05f, -0.15f), 0.11f, 0.32f, 8); // bread loaves
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisBread, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Bakery_PastryDisplay");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_BreadGold") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.55f, 0);
            col.size = new Vector3(1.7f, 1.15f, 0.85f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 3.0f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateBakeryMenuChalkboard()
        {
            Mesh mesh = new Mesh { name = "Mesh_Bakery_MenuChalkboard" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisSlate = new List<int>();

            // A-frame wooden legs
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.35f, 0.60f, 0), new Vector3(0.06f, 1.20f, 0.06f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.35f, 0.60f, 0), new Vector3(0.06f, 1.20f, 0.06f));

            // Slate chalkboard panel
            AddBox(verts, normals, uvs, trisSlate, new Vector3(0, 0.65f, 0), new Vector3(0.65f, 0.85f, 0.04f));

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisSlate, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Bakery_MenuChalkboard");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_RoofSlate") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.6f, 0);
            col.size = new Vector3(0.85f, 1.25f, 0.4f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 2.0f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }
        #endregion

        #region 5. Alchemy Lab Props (alchemy-lab-props-41758)
        private static void CreateAlchemyDistillationBench()
        {
            Mesh mesh = new Mesh { name = "Mesh_Alchemy_DistillationBench" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisGlass = new List<int>();
            List<int> trisLiquid = new List<int>();

            // Heavy Timber Bench Table
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.80f, 0), new Vector3(1.8f, 0.12f, 0.85f));   // tabletop
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.80f, 0.38f, 0), new Vector3(0.12f, 0.76f, 0.75f)); // legs
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.80f, 0.38f, 0), new Vector3(0.12f, 0.76f, 0.75f));

            // Glass Retort Flasks & Distillation Tubing
            AddCylinder(verts, normals, uvs, trisGlass, new Vector3(-0.45f, 1.08f, 0), 0.16f, 0.35f, 8); // alembic flask
            AddCylinder(verts, normals, uvs, trisGlass, new Vector3(0.45f, 1.08f, 0), 0.14f, 0.32f, 8);  // receiving flask
            AddBox(verts, normals, uvs, trisGlass, new Vector3(0, 1.20f, 0), new Vector3(0.75f, 0.05f, 0.05f)); // condenser tube

            // Glowing bubbling elixir liquids (Ruby & Azure)
            AddCylinder(verts, normals, uvs, trisLiquid, new Vector3(-0.45f, 0.98f, 0), 0.14f, 0.18f, 8);
            AddCylinder(verts, normals, uvs, trisLiquid, new Vector3(0.45f, 0.98f, 0), 0.12f, 0.16f, 8);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisGlass, 1);
            mesh.SetTriangles(trisLiquid, 2);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Alchemy_DistillationBench");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_RoseGlass"), LoadMat("M_PotionRuby") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.75f, 0);
            col.size = new Vector3(1.9f, 1.4f, 0.95f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 3.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateAlchemyMortarPestleRack()
        {
            Mesh mesh = new Mesh { name = "Mesh_Alchemy_MortarPestleRack" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisStone = new List<int>();
            List<int> trisWood = new List<int>();
            List<int> trisVials = new List<int>();

            // Stone Grinding Mortar Bowl
            AddCylinder(verts, normals, uvs, trisStone, new Vector3(0, 0.15f, 0), 0.25f, 0.25f, 8);

            // Wooden Pestle & Reagent Rack
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.05f, 0.28f, 0), new Vector3(0.06f, 0.35f, 0.06f)); // pestle
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.45f, 0.35f), new Vector3(0.60f, 0.30f, 0.15f)); // vial shelf

            // Specimen reagent bottles
            for (int i = 0; i < 3; i++)
            {
                float x = -0.18f + i * 0.18f;
                AddCylinder(verts, normals, uvs, trisVials, new Vector3(x, 0.68f, 0.35f), 0.05f, 0.16f, 6);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trisStone, 0);
            mesh.SetTriangles(trisWood, 1);
            mesh.SetTriangles(trisVials, 2);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Alchemy_MortarPestleRack");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_CathedralStone"), LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_RuneGlow") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.4f, 0.15f);
            col.size = new Vector3(0.7f, 0.85f, 0.65f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 2.0f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }
        #endregion

        #region 6. RPG Medieval & Survival Village Props (294298, 258935, 41540, 59280)
        private static void CreateMedievalVillageWell()
        {
            Mesh mesh = new Mesh { name = "Mesh_Medieval_VillageWell" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisStone = new List<int>();
            List<int> trisWood = new List<int>();
            List<int> trisRoof = new List<int>();

            // Cobblestone Circular Well Base (radius 1.0m, height 0.9m)
            AddCylinder(verts, normals, uvs, trisStone, new Vector3(0, 0.45f, 0), 1.05f, 0.90f, 12);

            // Two Timber Canopy Upright Pillars
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.95f, 1.8f, 0), new Vector3(0.14f, 1.9f, 0.14f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.95f, 1.8f, 0), new Vector3(0.14f, 1.9f, 0.14f));
            AddCylinder(verts, normals, uvs, trisWood, new Vector3(0, 2.0f, 0), 0.08f, 1.9f, 6); // crank drum

            // Pitched Shingled Canopy Roof
            AddGableRoof(verts, normals, uvs, trisRoof, new Vector3(0, 3.0f, 0), 2.4f, 1.6f, 0.65f);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(trisStone, 0);
            mesh.SetTriangles(trisWood, 1);
            mesh.SetTriangles(trisRoof, 2);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Medieval_VillageWell");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_StoneBase"), LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_RoofSlate") };

            var col = AddCylinderCollider(go, 1.1f, 1.8f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 4.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateMedievalNoticeBoard()
        {
            Mesh mesh = new Mesh { name = "Mesh_Medieval_NoticeBoard" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisParchment = new List<int>();

            // Timber upright posts & rooflet
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.9f, 1.25f, 0), new Vector3(0.12f, 2.5f, 0.12f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.9f, 1.25f, 0), new Vector3(0.12f, 2.5f, 0.12f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 2.55f, 0), new Vector3(2.1f, 0.18f, 0.35f)); // roof overhang
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 1.45f, 0), new Vector3(1.7f, 1.4f, 0.08f));   // board backing

            // Pinned Parchment Bounty Posters
            AddBox(verts, normals, uvs, trisParchment, new Vector3(-0.45f, 1.6f, 0.05f), new Vector3(0.35f, 0.45f, 0.02f));
            AddBox(verts, normals, uvs, trisParchment, new Vector3(0.35f, 1.5f, 0.05f), new Vector3(0.40f, 0.50f, 0.02f));
            AddBox(verts, normals, uvs, trisParchment, new Vector3(-0.1f, 1.25f, 0.05f), new Vector3(0.30f, 0.38f, 0.02f));

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisParchment, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Medieval_NoticeBoard");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_PlasterTimber") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.3f, 0);
            col.size = new Vector3(2.2f, 2.6f, 0.5f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 3.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateMedievalWeaponRack()
        {
            Mesh mesh = new Mesh { name = "Mesh_Medieval_WeaponRack" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisSteel = new List<int>();

            // Timber Weapon Rack Frame
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.75f, 0.85f, 0), new Vector3(0.10f, 1.7f, 0.45f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.75f, 0.85f, 0), new Vector3(0.10f, 1.7f, 0.45f));
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.35f, 0), new Vector3(1.5f, 0.10f, 0.35f)); // base shelf
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 1.25f, 0), new Vector3(1.5f, 0.10f, 0.25f)); // upper slotted rail

            // Upright Swords & Spears
            for (int i = 0; i < 4; i++)
            {
                float x = -0.45f + i * 0.30f;
                AddBox(verts, normals, uvs, trisSteel, new Vector3(x, 1.0f, 0), new Vector3(0.06f, 1.25f, 0.02f)); // blade
                AddBox(verts, normals, uvs, trisWood, new Vector3(x, 0.50f, 0), new Vector3(0.14f, 0.04f, 0.04f));  // crossguard
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisSteel, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Medieval_WeaponRack");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_WeaponSteel") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.size = new Vector3(1.8f, 1.8f, 0.6f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 3.0f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }

        private static void CreateMedievalFirewoodStack()
        {
            Mesh mesh = new Mesh { name = "Mesh_Medieval_FirewoodStack" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisBark = new List<int>();

            // Neatly stacked logs
            for (int row = 0; row < 3; row++)
            {
                int countInRow = 4 - row;
                for (int c = 0; c < countInRow; c++)
                {
                    float x = (-0.5f + c * 0.32f) + (row * 0.16f);
                    float y = 0.18f + row * 0.28f;
                    AddCylinder(verts, normals, uvs, trisWood, new Vector3(x, y, 0), 0.14f, 0.95f, 8);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 1;
            mesh.SetTriangles(trisWood, 0);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Medieval_FirewoodStack");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.45f, 0);
            col.size = new Vector3(1.5f, 0.95f, 1.1f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 2.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }
        #endregion

        #region 7. Low Poly Interior Tavern Props (low-poly-interior-props-pack-3digitalis-400108)
        private static void CreateTavernDiningTableSet()
        {
            Mesh mesh = new Mesh { name = "Mesh_Tavern_DiningTableSet" };
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trisWood = new List<int>();
            List<int> trisCandle = new List<int>();

            // Tavern Table (2.2m x 1.0m)
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.72f, 0), new Vector3(2.2f, 0.10f, 1.0f)); // tabletop
            AddBox(verts, normals, uvs, trisWood, new Vector3(-0.9f, 0.35f, 0), new Vector3(0.12f, 0.70f, 0.8f)); // leg L
            AddBox(verts, normals, uvs, trisWood, new Vector3(0.9f, 0.35f, 0), new Vector3(0.12f, 0.70f, 0.8f));  // leg R

            // Two Long Benches
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.42f, -0.85f), new Vector3(2.0f, 0.08f, 0.35f)); // bench front
            AddBox(verts, normals, uvs, trisWood, new Vector3(0, 0.42f, 0.85f), new Vector3(2.0f, 0.08f, 0.35f));  // bench back

            // Wooden Beer Tankards
            AddCylinder(verts, normals, uvs, trisWood, new Vector3(-0.5f, 0.88f, -0.2f), 0.08f, 0.18f, 6);
            AddCylinder(verts, normals, uvs, trisWood, new Vector3(0.5f, 0.88f, 0.2f), 0.08f, 0.18f, 6);

            // Candle Wax & Flame
            AddCylinder(verts, normals, uvs, trisCandle, new Vector3(0, 0.88f, 0), 0.05f, 0.18f, 6);

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trisWood, 0);
            mesh.SetTriangles(trisCandle, 1);
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, $"{MESH_DIR}/{mesh.name}.asset");

            GameObject go = new GameObject("PFB_Tavern_DiningTableSet");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { LoadMat("M_Metro_TimberWood"), LoadMat("M_Metro_LanternGlow") };

            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.45f, 0);
            col.size = new Vector3(2.3f, 0.95f, 2.1f);

            var prop = go.AddComponent<CullableWorldProp>();
            prop.boundingRadius = 3.5f;

            SavePrefab(go, $"{PREFAB_DIR}/{go.name}.prefab");
        }
        #endregion

        #region Geometry Helpers
        private static void AddBox(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            int startIndex = verts.Count;

            // 6 faces: +Y, -Y, +Z, -Z, +X, -X
            Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            Vector3[][] faceVerts = {
                new Vector3[] { new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z) },
                new Vector3[] { new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z) },
                new Vector3[] { new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z) },
                new Vector3[] { new Vector3(h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z), new Vector3(h.x, h.y, -h.z) },
                new Vector3[] { new Vector3(h.x, -h.y, h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(h.x, h.y, h.z) },
                new Vector3[] { new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, h.y, -h.z) }
            };

            for (int f = 0; f < 6; f++)
            {
                int baseIdx = verts.Count;
                for (int v = 0; v < 4; v++)
                {
                    verts.Add(center + faceVerts[f][v]);
                    normals.Add(faceNormals[f]);
                }
                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));

                tris.Add(baseIdx);
                tris.Add(baseIdx + 1);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx);
                tris.Add(baseIdx + 2);
                tris.Add(baseIdx + 3);
            }
        }

        private static void AddCylinder(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 center, float radius, float height, int segments)
        {
            float halfH = height * 0.5f;
            int baseIdx = verts.Count;

            // Generate side vertices
            for (int i = 0; i <= segments; i++)
            {
                float ang = (i % segments) * (Mathf.PI * 2f / segments);
                float x = Mathf.Sin(ang) * radius;
                float z = Mathf.Cos(ang) * radius;
                Vector3 n = new Vector3(Mathf.Sin(ang), 0, Mathf.Cos(ang));

                // Bottom vertex
                verts.Add(center + new Vector3(x, -halfH, z));
                normals.Add(n);
                uvs.Add(new Vector2((float)i / segments, 0));

                // Top vertex
                verts.Add(center + new Vector3(x, halfH, z));
                normals.Add(n);
                uvs.Add(new Vector2((float)i / segments, 1));
            }

            // Side triangles
            for (int i = 0; i < segments; i++)
            {
                int b0 = baseIdx + i * 2;
                int t0 = b0 + 1;
                int b1 = b0 + 2;
                int t1 = b0 + 3;

                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);
            }

            // Top Cap (pointing UP)
            int topCenterIdx = verts.Count;
            verts.Add(center + Vector3.up * halfH);
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < segments; i++)
            {
                float ang = i * (Mathf.PI * 2f / segments);
                float nextAng = ((i + 1) % segments) * (Mathf.PI * 2f / segments);

                int v1 = verts.Count;
                verts.Add(center + new Vector3(Mathf.Sin(ang) * radius, halfH, Mathf.Cos(ang) * radius));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f + Mathf.Sin(ang) * 0.5f, 0.5f + Mathf.Cos(ang) * 0.5f));

                int v2 = verts.Count;
                verts.Add(center + new Vector3(Mathf.Sin(nextAng) * radius, halfH, Mathf.Cos(nextAng) * radius));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(0.5f + Mathf.Sin(nextAng) * 0.5f, 0.5f + Mathf.Cos(nextAng) * 0.5f));

                tris.Add(topCenterIdx);
                tris.Add(v1);
                tris.Add(v2);
            }
        }

        private static void AddGableRoof(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 peakPos, float width, float depth, float height)
        {
            float hw = width * 0.5f;
            float hd = depth * 0.5f;

            // Slopes outward and upward: Side 1 (+X) and Side 2 (-X)
            // Left Slope (-X)
            int b0 = verts.Count;
            verts.Add(peakPos + new Vector3(-hw, -height, -hd));
            verts.Add(peakPos + new Vector3(0, 0, -hd));
            verts.Add(peakPos + new Vector3(0, 0, hd));
            verts.Add(peakPos + new Vector3(-hw, -height, hd));

            Vector3 nLeft = new Vector3(-height, hw, 0).normalized;
            for (int i = 0; i < 4; i++) { normals.Add(nLeft); uvs.Add(new Vector2(i % 2, i / 2)); }

            tris.Add(b0); tris.Add(b0 + 1); tris.Add(b0 + 2);
            tris.Add(b0); tris.Add(b0 + 2); tris.Add(b0 + 3);

            // Right Slope (+X)
            int b1 = verts.Count;
            verts.Add(peakPos + new Vector3(0, 0, -hd));
            verts.Add(peakPos + new Vector3(hw, -height, -hd));
            verts.Add(peakPos + new Vector3(hw, -height, hd));
            verts.Add(peakPos + new Vector3(0, 0, hd));

            Vector3 nRight = new Vector3(height, hw, 0).normalized;
            for (int i = 0; i < 4; i++) { normals.Add(nRight); uvs.Add(new Vector2(i % 2, i / 2)); }

            tris.Add(b1); tris.Add(b1 + 1); tris.Add(b1 + 2);
            tris.Add(b1); tris.Add(b1 + 2); tris.Add(b1 + 3);
        }

        private static Material LoadMat(string matName)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Metropolis/{matName}.mat");
            if (mat != null) return mat;
            return new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        private static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static Collider AddCylinderCollider(GameObject go, float radius, float height)
        {
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, height * 0.5f, 0);
            col.height = height;
            col.radius = radius;
            return col;
        }
        #endregion
    }
}
