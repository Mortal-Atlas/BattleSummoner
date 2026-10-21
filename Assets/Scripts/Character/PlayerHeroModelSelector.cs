using UnityEngine;

namespace BattleSummoner.Character
{
    public enum CharacterSex
    {
        Male = 0,
        Female = 1
    }

    public class PlayerHeroModelSelector : MonoBehaviour
    {
        public const string PrefsKeySelectedSex = "SelectedCharacterSex";

        [Header("Testing & Character Creation Sex Selection")]
        [Tooltip("Defaults to Male (MaleStarter.glb) for testing purposes.")]
        public CharacterSex selectedSex = CharacterSex.Male;
        public bool isWearingStarterArmor = true;

        [Header("Pre-Assigned .GLB Model Prefabs (Auto-Wired by Editor Script)")]
        public GameObject maleStarterGlbPrefab;   // D:\Games\BattleSummoner\Assets\MaleStarter.glb
        public GameObject femaleStarterGlbPrefab; // D:\Games\BattleSummoner\Assets\femalestarter.glb
        public GameObject manBaseGlbPrefab;       // D:\Games\BattleSummoner\Assets\manbase.glb (Armorless)
        public GameObject womanBaseGlbPrefab;     // D:\Games\BattleSummoner\Assets\womanbase.glb (Armorless)

        [Header("Active Visual Root")]
        public GameObject activeVisualInstance;

        private void Awake()
        {
            // Read from Character Creation choice if saved; otherwise default to Male (0) for testing
            if (PlayerPrefs.HasKey(PrefsKeySelectedSex))
            {
                selectedSex = (CharacterSex)PlayerPrefs.GetInt(PrefsKeySelectedSex, (int)CharacterSex.Male);
            }
            else
            {
                selectedSex = CharacterSex.Male;
            }

            ApplySelectedCharacterModel(selectedSex, isWearingStarterArmor);
        }

        /// <summary>
        /// Called by Character Creation UI when the player selects Male or Female,
        /// or when toggling starter armor vs. armorless base mesh.
        /// </summary>
        public void SetCharacterCreationSex(CharacterSex chosenSex, bool saveToPrefs = true)
        {
            selectedSex = chosenSex;
            if (saveToPrefs)
            {
                PlayerPrefs.SetInt(PrefsKeySelectedSex, (int)chosenSex);
                PlayerPrefs.Save();
            }
            ApplySelectedCharacterModel(selectedSex, isWearingStarterArmor);
        }

        public void ApplySelectedCharacterModel(CharacterSex sex, bool armored)
        {
            GameObject targetPrefab = null;
            if (sex == CharacterSex.Male)
            {
                targetPrefab = armored ? maleStarterGlbPrefab : manBaseGlbPrefab;
                if (targetPrefab == null) targetPrefab = maleStarterGlbPrefab;
            }
            else
            {
                targetPrefab = armored ? femaleStarterGlbPrefab : womanBaseGlbPrefab;
                if (targetPrefab == null) targetPrefab = femaleStarterGlbPrefab;
            }

            if (targetPrefab == null)
            {
                Debug.LogWarning("[PlayerHeroModelSelector] Target .glb prefab is not assigned yet! Run the Editor Master Overhaul menu item.");
                return;
            }

            // Disable or remove any legacy cylindrical placeholders or old non-GLB child meshes
            PurgeLegacyPlaceholderVisuals();

            if (activeVisualInstance != null)
            {
                if (Application.isPlaying) Destroy(activeVisualInstance);
                else DestroyImmediate(activeVisualInstance);
            }

            activeVisualInstance = Instantiate(targetPrefab, transform);
            activeVisualInstance.name = $"Visual_{targetPrefab.name}";
            activeVisualInstance.transform.localPosition = Vector3.zero;
            activeVisualInstance.transform.localRotation = Quaternion.identity;
            activeVisualInstance.transform.localScale = Vector3.one;

            // Apply character cel shader and preserve the 2048x2048 GLB texture
            Shader charShader = Shader.Find("BattleSummoner/Painterly/GenshinPainterlyCharacter");
            Texture prefabTex = null;
            var pm = targetPrefab.GetComponentInChildren<Renderer>()?.sharedMaterial;
            if (pm != null)
            {
                if (pm.HasProperty("baseColorTexture")) prefabTex = pm.GetTexture("baseColorTexture");
                if (prefabTex == null) prefabTex = pm.mainTexture;
                if (prefabTex == null && pm.HasProperty("_BaseMap")) prefabTex = pm.GetTexture("_BaseMap");
                if (prefabTex == null && pm.HasProperty("_MainTex")) prefabTex = pm.GetTexture("_MainTex");
            }

            if (charShader != null && prefabTex != null)
            {
                foreach (var r in activeVisualInstance.GetComponentsInChildren<Renderer>(true))
                {
                    Material m = new Material(charShader);
                    m.SetTexture("_BaseMap", prefabTex);
                    m.SetColor("_BaseColor", Color.white);
                    m.SetColor("_ShadowTint", new Color(0.68f, 0.64f, 0.78f, 1.0f));
                    m.SetFloat("_MinBrightnessFloor", 0.22f);
                    r.sharedMaterial = m;
                }
            }

            // Rebind Animator avatar if main Animator sits on Player_Hero root
            Animator rootAnim = GetComponent<Animator>();
            Animator childAnim = activeVisualInstance.GetComponentInChildren<Animator>();
            if (rootAnim != null && childAnim != null && childAnim != rootAnim && childAnim.avatar != null)
            {
                rootAnim.avatar = childAnim.avatar;
            }
        }

        private void PurgeLegacyPlaceholderVisuals()
        {
            // Remove primitive cylinder/capsule/cube renderers sitting on the root or legacy child nodes
            MeshRenderer rootMr = GetComponent<MeshRenderer>();
            MeshFilter rootMf = GetComponent<MeshFilter>();
            if (rootMr != null) rootMr.enabled = false;
            if (rootMf != null && rootMf.sharedMesh != null &&
                (rootMf.sharedMesh.name.Contains("Cylinder") || rootMf.sharedMesh.name.Contains("Capsule")))
            {
                rootMr.enabled = false;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                string cn = child.name.ToLowerInvariant();
                if (child.gameObject == activeVisualInstance) continue;

                // Keep camera, UI, audio, or weapon mount anchors intact; remove old meshes/cylinders
                if (cn.Contains("cylinder") || cn.Contains("placeholder") || cn.Contains("capsule") ||
                    cn.Contains("old") || cn.Contains("visual_") || cn.Contains("armature") || cn.Contains("mesh"))
                {
                    if (!cn.Contains("camera") && !cn.Contains("canvas") && !cn.Contains("hud"))
                    {
                        if (Application.isPlaying) Destroy(child.gameObject);
                        else DestroyImmediate(child.gameObject);
                    }
                }
            }
        }
    }
}
