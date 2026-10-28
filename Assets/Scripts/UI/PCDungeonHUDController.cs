using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.UI
{
    [DefaultExecutionOrder(100)]
    public class PCDungeonHUDController : MonoBehaviour
    {
        public static PCDungeonHUDController Instance { get; private set; }

        [Header("Canvas & Display Root")]
        public Canvas targetCanvas;
        public CanvasGroup hudCanvasGroup;

        [Header("Top Left: Player Status Frame")]
        public TextMeshProUGUI playerNameTMP;
        public Slider playerHpSlider;
        public TextMeshProUGUI playerHpText;
        public Slider playerMpSlider;
        public TextMeshProUGUI playerMpText;
        public TextMeshProUGUI activeHeroModelTMP;

        [Header("Bottom Left: Weapon & Keypad Deck")]
        public TextMeshProUGUI currentWeaponTMP;
        public TextMeshProUGUI weaponArtStatusTMP;
        public TextMeshProUGUI keypadWeaponListTMP;

        [Header("Bottom Right: Keybind Legend")]
        public TextMeshProUGUI controlsLegendTMP;

        [Header("Top Center: Boss Telemetry")]
        public GameObject bossBarContainer;
        public Slider bossHpSlider;
        public TextMeshProUGUI bossNameTMP;
        public TextMeshProUGUI bossLimbStatusTMP;

        [Header("Center Reticle")]
        public Image crosshairImage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureHUDComponents();
        }

        public void SetHUDVisible(bool visible)
        {
            if (hudCanvasGroup != null)
            {
                hudCanvasGroup.alpha = visible ? 1f : 0f;
                hudCanvasGroup.interactable = visible;
                hudCanvasGroup.blocksRaycasts = visible;
            }
            else if (targetCanvas != null)
            {
                targetCanvas.gameObject.SetActive(visible);
            }
        }

        public void UpdatePlayerHealth(float current, float max)
        {
            if (playerHpSlider != null) playerHpSlider.value = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (playerHpText != null) playerHpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)} HP";
        }

        public void UpdatePlayerMana(float current, float max)
        {
            if (playerMpSlider != null) playerMpSlider.value = Mathf.Clamp01(current / Mathf.Max(1f, max));
            if (playerMpText != null) playerMpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)} MP";
        }

        public void UpdateActiveHeroModel(string heroName, int heroIndex)
        {
            if (activeHeroModelTMP != null)
            {
                activeHeroModelTMP.text = $"<color=#FFD700>ACTIVE HERO:</color> {heroName} <color=#AAAAAA>[Key {heroIndex + 1}]</color>";
            }
        }

        public void UpdateEquippedWeapon(string weaponTitle, string stanceTag, int weaponIndex)
        {
            if (currentWeaponTMP != null)
            {
                currentWeaponTMP.text = $"<color=#64D2FF>WEAPON [{weaponIndex}]:</color> {weaponTitle}\n<size=12><color=#AAAAAA>Stance: {stanceTag}</color></size>";
            }
        }

        public void UpdateWeaponArtCharge(bool isReady, float chargeRatio)
        {
            if (weaponArtStatusTMP != null)
            {
                if (isReady)
                {
                    weaponArtStatusTMP.text = "<color=#FF453A><b>[CHARGED HEAVY ART READY!]</b></color>";
                }
                else if (chargeRatio > 0.05f)
                {
                    weaponArtStatusTMP.text = $"<color=#FFD60A>CHARGING ART... {Mathf.RoundToInt(chargeRatio * 100)}%</color>";
                }
                else
                {
                    weaponArtStatusTMP.text = "<color=#8E8E93>[Heavy Cleave: Hold L-Click > 0.45s]</color>";
                }
            }
        }

        public void UpdateBossTelemetry(string bossName, float currentHp, float maxHp, string limbName, bool isLimbSevered)
        {
            if (bossBarContainer != null) bossBarContainer.SetActive(currentHp > 0f);
            if (bossHpSlider != null) bossHpSlider.value = Mathf.Clamp01(currentHp / Mathf.Max(1f, maxHp));
            if (bossNameTMP != null) bossNameTMP.text = $"{bossName} ({Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)})";
            if (bossLimbStatusTMP != null)
            {
                bossLimbStatusTMP.text = isLimbSevered
                    ? $"<color=#30D158>SEVERED:</color> {limbName} [Disabled!]"
                    : $"<color=#FF9F0A>WEAK PART:</color> {limbName} [Intact]";
            }
        }

        private void EnsureHUDComponents()
        {
            if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
            if (targetCanvas == null) targetCanvas = gameObject.AddComponent<Canvas>();
            if (Camera.main != null)
            {
                targetCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                targetCanvas.worldCamera = Camera.main;
                targetCanvas.planeDistance = 1.0f;
            }
            else
            {
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            targetCanvas.sortingOrder = 50;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            if (hudCanvasGroup == null) hudCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            // Setup Keypad list text default
            if (keypadWeaponListTMP != null && string.IsNullOrEmpty(keypadWeaponListTMP.text))
            {
                keypadWeaponListTMP.text =
                    "<b>KEYPAD WEAPONS:</b>\n" +
                    "[1] Bronze Sword & Shield  [2] Knight Steel Longsword\n" +
                    "[3] Bronze War Axe        [4] Dungeon Battleaxe\n" +
                    "[5] Dual Rogue Daggers    [6] War Pickaxe\n" +
                    "[7] Torch & Shield        [8] Dual Broadswords\n" +
                    "[9] Bare Fists Unarmed    [ / ] Prev / Next Weapon";
            }

            if (controlsLegendTMP != null && string.IsNullOrEmpty(controlsLegendTMP.text))
            {
                controlsLegendTMP.text =
                    "<b>PC CONTROLS:</b>\n" +
                    "<color=#FFD60A>WASD:</color> Move (Camera-Relative)\n" +
                    "<color=#FFD60A>Left Shift:</color> Sprint / Dodge Tap\n" +
                    "<color=#FFD60A>Space:</color> Jump\n" +
                    "<color=#FFD60A>L-Click Drag:</color> 1:1 Directional Slash\n" +
                    "<color=#FFD60A>R-Click:</color> Parry / Shield Guard\n" +
                    "<color=#FFD60A>R-Mouse Drag:</color> Orbit Camera\n" +
                    "<color=#FFD60A>Scroll Wheel:</color> Zoom Camera\n" +
                    "<color=#FFD60A>E / Mid-Click:</color> Cast Rune Spell\n" +
                    "<color=#FFD60A>Keys 1-4:</color> Swap Hero Model\n" +
                    "<color=#FFD60A>Numpad 1-9:</color> Swap Weapon\n" +
                    "<color=#FFD60A>F1:</color> Toggle PC / Mobile Mode";
            }
        }
    }
}
