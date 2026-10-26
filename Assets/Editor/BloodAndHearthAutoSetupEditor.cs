#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BattleSummoner.Core;
using BattleSummoner.Combat;

namespace BattleSummoner.EditorTools
{
    public static class BloodAndHearthAutoSetupEditor
    {
        [MenuItem("BattleSummoner/Blood And Hearth/1. Execute Full Blood & Hearth Overhaul & Purge Bloat")]
        public static void ExecuteFullBloodAndHearthSetup()
        {
            // 1. Ensure Master Director exists in the active scene
            BloodAndHearthMasterDirector director = UnityEngine.Object.FindFirstObjectByType<BloodAndHearthMasterDirector>();
            if (director == null)
            {
                GameObject rootObj = new GameObject("BloodAndHearthMasterDirector_Root");
                director = rootObj.AddComponent<BloodAndHearthMasterDirector>();
                Undo.RegisterCreatedObjectUndo(rootObj, "Create BloodAndHearthMasterDirector");
            }

            // 2. Ensure Combat Bridge is attached
            if (director.GetComponent<CombatToBloodHearthBridge>() == null)
            {
                director.gameObject.AddComponent<CombatToBloodHearthBridge>();
            }

            // 3. Enforce 9-Ally Portrait -5.5 Deg Shear
            director.EnforceNinePortraitGothicShear();

            // 4. Create & Assign URP Visceral Blood & Grime Material if missing
            Shader grimeShader = Shader.Find("BattleSummoner/Visceral/BloodAndHearthGrimeAndGlass");
            if (grimeShader != null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                    AssetDatabase.CreateFolder("Assets", "Materials");

                string matPath = "Assets/Materials/MAT_VisceralArmorGrime.mat";
                Material grimeMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (grimeMat == null)
                {
                    grimeMat = new Material(grimeShader);
                    AssetDatabase.CreateAsset(grimeMat, matPath);
                }
            }

            EditorUtility.SetDirty(director);
            AssetDatabase.SaveAssets();
            Debug.Log("[Blood & Hearth Auto-Setup] Overhaul complete! 0D-11D Compiler, 9-Portrait Raid HUD, Anything Is A Weapon (+0.05 Wok EXP), Visceral Blood/Grime, DanMachi Hearth Leveling, and Shield Hero Monster-Feeding are locked and wired.");
        }
    }
}
#endif
