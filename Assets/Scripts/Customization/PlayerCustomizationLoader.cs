using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSummoner.Customization
{
    /// <summary>
    /// PlayerCustomizationLoader loads the player's saved character archetype,
    /// morph proportions, and equipment into the active player avatar when entering game scenes.
    /// </summary>
    public class PlayerCustomizationLoader : MonoBehaviour
    {
        [Header("Equipment Prefabs")]
        public GameObject helmetPrefab;
        public GameObject torsoPrefab;
        public GameObject leggingsPrefab;
        public GameObject shoesPrefab;
        public GameObject swordPrefab;
        public GameObject battleAxePrefab;
        public GameObject staffPrefab;
        public GameObject wandPrefab;

        private void Awake()
        {
            EnsurePrefabs();
        }

        private void Start()
        {
            ApplyCustomization();
            StartCoroutine(ArrivalFadeInRoutine());
        }

        public void EnsurePrefabs()
        {
            if (helmetPrefab == null) helmetPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Helmet");
            if (torsoPrefab == null) torsoPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Torso");
            if (leggingsPrefab == null) leggingsPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Leggings");
            if (shoesPrefab == null) shoesPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Equipment_Shoes");
            if (swordPrefab == null) swordPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_Sword");
            if (battleAxePrefab == null) battleAxePrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_TwoHanded_BattleAxe");
            if (staffPrefab == null) staffPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_Staff");
            if (wandPrefab == null) wandPrefab = Resources.Load<GameObject>("Prefabs/Equipment/Weapon_Wand");
        }

        public void ApplyCustomization()
        {
            var modularApp = GetComponent<ModularCharacterAppearance>() ?? GetComponentInChildren<ModularCharacterAppearance>();
            if (modularApp != null)
            {
                var profile = ModularPlayerAppearanceData.LoadLocalProfile();
                modularApp.ApplyAppearance(profile);

                var prog = GetComponent<PlayerProgression>();
                if (prog != null && !string.IsNullOrEmpty(profile.playerName))
                {
                    prog.playerName = profile.playerName;
                }
                Debug.Log($"[PLAYER LOADER] Applied ModularPlayerAppearanceData to {gameObject.name}: (Torso={profile.equipTorso}, Gauntlets={profile.equipGauntlets}, Leggings={profile.equipLeggings}, Boots={profile.equipBoots}, Weapon={profile.weaponId})");
                return;
            }

            var equipMgr = GetComponentInChildren<ModularEquipmentManager>();
            if (equipMgr == null)
            {
                equipMgr = gameObject.AddComponent<ModularEquipmentManager>();
            }
            equipMgr.AutoDetectSockets();

            // Apply saved equipment
            if (PlayerPrefs.GetInt("Equip_Torso", 1) == 1 && torsoPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "torso", itemName = "Adventurer Cuirass", slot = EquipmentSlot.Torso, prefab = torsoPrefab });
            }
            if (PlayerPrefs.GetInt("Equip_Leggings", 1) == 1 && leggingsPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "leggings", itemName = "Adventurer Leggings", slot = EquipmentSlot.Leggings, prefab = leggingsPrefab });
            }
            if (PlayerPrefs.GetInt("Equip_Shoes", 1) == 1 && shoesPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "shoes", itemName = "Armored Boots", slot = EquipmentSlot.Shoes, prefab = shoesPrefab });
            }
            if (PlayerPrefs.GetInt("Equip_Helmet", 0) == 1 && helmetPrefab != null)
            {
                equipMgr.Equip(new EquipmentItemDefinition { itemId = "helmet", itemName = "Knight Helmet", slot = EquipmentSlot.Helmet, prefab = helmetPrefab });
            }

            string weapon = PlayerPrefs.GetString("Equip_Weapon", "sword");
            GameObject wPrefab = weapon switch
            {
                "axe" => battleAxePrefab,
                "sword" => swordPrefab,
                "staff" => staffPrefab,
                "wand" => wandPrefab,
                _ => swordPrefab
            };
            if (wPrefab != null)
            {
                bool is2H = weapon == "axe" || weapon == "staff";
                equipMgr.Equip(new EquipmentItemDefinition
                {
                    itemId = weapon,
                    itemName = weapon.ToUpper(),
                    slot = EquipmentSlot.MainHandWeapon,
                    prefab = wPrefab,
                    isTwoHanded = is2H
                });
            }

            // Apply body morph proportions if controller is attached
            var morph = GetComponentInChildren<SimsBodyMorphController>();
            if (morph != null && PlayerPrefs.HasKey("Morph_Height"))
            {
                morph.SetHeight(PlayerPrefs.GetFloat("Morph_Height", 1.0f));
                morph.SetHeadScale(PlayerPrefs.GetFloat("Morph_Head", 1.0f));
                morph.SetShoulderWidth(PlayerPrefs.GetFloat("Morph_Shoulders", 1.0f));
                morph.SetChestScale(PlayerPrefs.GetFloat("Morph_Chest", 1.0f));
                morph.SetWaistWidth(PlayerPrefs.GetFloat("Morph_Waist", 1.0f));
                morph.SetHipWidth(PlayerPrefs.GetFloat("Morph_Hips", 1.0f));
                morph.SetArmThickness(PlayerPrefs.GetFloat("Morph_Arms", 1.0f));
                morph.SetLegThickness(PlayerPrefs.GetFloat("Morph_Legs", 1.0f));
                morph.SetMuscleMass(PlayerPrefs.GetFloat("Morph_Muscle", 25.0f));
            }
        }

        private IEnumerator ArrivalFadeInRoutine()
        {
            var faderObj = GameObject.Find("SAO_ScreenFadeOverlay");
            if (faderObj != null)
            {
                var fadeImg = faderObj.GetComponent<Image>();
                if (fadeImg != null)
                {
                    fadeImg.gameObject.SetActive(true);
                    float duration = 0.80f;
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float alpha = 1.0f - Mathf.SmoothStep(0f, 1f, elapsed / duration);
                        fadeImg.color = new Color(0.02f, 0.04f, 0.08f, alpha);
                        yield return null;
                    }
                    fadeImg.gameObject.SetActive(false);
                }
            }
        }
    }
}
