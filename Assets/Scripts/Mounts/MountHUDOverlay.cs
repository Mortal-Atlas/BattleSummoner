using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Mounts
{
    /// <summary>
    /// MountHUDOverlay renders the mounted combat interface,
    /// showing real-time speed, kinetic momentum bar for Ramming Overdrive,
    /// and controls for charging, mounting, and dismounting.
    /// </summary>
    public class MountHUDOverlay : MonoBehaviour
    {
        public static MountHUDOverlay Instance { get; private set; }

        [Header("UI References")]
        private GameObject hudCanvasObj;
        private RectTransform panelRoot;
        private TextMeshProUGUI mountTitleText;
        private TextMeshProUGUI speedText;
        private Image momentumFillBar;
        private TextMeshProUGUI momentumLabel;
        private GameObject mountPromptObj;
        private TextMeshProUGUI mountPromptText;

        private PlayerMountController mountController;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            BuildProceduralHUD();
        }

        private void Start()
        {
            mountController = PlayerMountController.Instance;
        }

        private void Update()
        {
            if (mountController == null)
            {
                mountController = PlayerMountController.Instance;
                if (mountController == null) return;
            }

            if (mountController.IsMounted && mountController.CurrentMount != null)
            {
                var mount = mountController.CurrentMount;
                panelRoot.gameObject.SetActive(true);
                if (mountPromptObj != null) mountPromptObj.SetActive(false);

                // Update text
                if (mountTitleText != null)
                {
                    mountTitleText.text = $"<color=#FFD700><b>{mount.Stats.mountName.ToUpper()}</b></color>";
                }

                if (speedText != null)
                {
                    string chargeTag = mount.IsRammingCharge ? "<color=#FF4400> [RAMMING CHARGE!]</color>" : "";
                    speedText.text = $"SPEED: <b>{mount.CurrentSpeed:F1} m/s</b>{chargeTag}";
                }

                if (momentumFillBar != null)
                {
                    momentumFillBar.fillAmount = mount.KineticMomentum;
                    momentumFillBar.color = Color.Lerp(new Color(0.2f, 0.7f, 1f), new Color(1f, 0.4f, 0.1f), mount.KineticMomentum);
                }

                if (momentumLabel != null)
                {
                    if (mount.KineticMomentum >= 0.75f)
                    {
                        momentumLabel.text = "<color=#FFAA00><b>⚡ KINETIC OVERDRIVE ACTIVE! ⚡</b></color>";
                    }
                    else if (mount.IsRammingCharge)
                    {
                        momentumLabel.text = "<color=#FFFF00>BUILDING RAMMING MOMENTUM...</color>";
                    }
                    else
                    {
                        momentumLabel.text = "<color=#CCCCCC>HOLD [SHIFT] FOR RAMMING OVERDRIVE</color>";
                    }
                }
            }
            else
            {
                panelRoot.gameObject.SetActive(false);

                // Check if near an unmounted mount
                var nearMount = mountController.FindNearestMount(3.8f);
                if (nearMount != null && mountPromptObj != null && mountPromptText != null)
                {
                    mountPromptObj.SetActive(true);
                    mountPromptText.text = $"[F] RIDE <b>{nearMount.Stats.mountName.ToUpper()}</b>\n<size=80%>[M] Cycle Constructs</size>";
                }
                else if (mountPromptObj != null)
                {
                    mountPromptObj.SetActive(false);
                }
            }
        }

        private void BuildProceduralHUD()
        {
            hudCanvasObj = new GameObject("MountHUD_Canvas");
            hudCanvasObj.transform.SetParent(transform, false);

            var canvas = hudCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;

            var scaler = hudCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Portrait baseline

            hudCanvasObj.AddComponent<GraphicRaycaster>();

            // 1. Mounted Top-Center Combat Panel
            var panel = new GameObject("MountedCombatPanel");
            panel.transform.SetParent(hudCanvasObj.transform, false);
            panelRoot = panel.AddComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.88f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.88f);
            panelRoot.sizeDelta = new Vector2(500f, 110f);

            var panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);

            // Title
            var titleGo = new GameObject("MountTitle");
            titleGo.transform.SetParent(panel.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.65f);
            titleRt.anchorMax = new Vector2(0.95f, 0.95f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            mountTitleText = titleGo.AddComponent<TextMeshProUGUI>();
            mountTitleText.alignment = TextAlignmentOptions.Center;
            mountTitleText.fontSize = 22;
            mountTitleText.fontStyle = FontStyles.Bold;

            // Speed
            var speedGo = new GameObject("SpeedText");
            speedGo.transform.SetParent(panel.transform, false);
            var speedRt = speedGo.AddComponent<RectTransform>();
            speedRt.anchorMin = new Vector2(0.05f, 0.38f);
            speedRt.anchorMax = new Vector2(0.95f, 0.65f);
            speedRt.offsetMin = Vector2.zero;
            speedRt.offsetMax = Vector2.zero;
            speedText = speedGo.AddComponent<TextMeshProUGUI>();
            speedText.alignment = TextAlignmentOptions.Center;
            speedText.fontSize = 17;

            // Momentum Bar BG
            var barBgGo = new GameObject("MomentumBarBG");
            barBgGo.transform.SetParent(panel.transform, false);
            var barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.06f, 0.12f);
            barBgRt.anchorMax = new Vector2(0.94f, 0.32f);
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.22f, 0.26f, 0.9f);

            // Momentum Bar Fill
            var barFillGo = new GameObject("MomentumBarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRt = barFillGo.AddComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
            momentumFillBar = barFillGo.AddComponent<Image>();
            momentumFillBar.type = Image.Type.Filled;
            momentumFillBar.fillMethod = Image.FillMethod.Horizontal;
            momentumFillBar.fillAmount = 0f;
            momentumFillBar.color = new Color(1f, 0.6f, 0.1f);

            // Momentum Label
            var labelGo = new GameObject("MomentumLabel");
            labelGo.transform.SetParent(barBgGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            momentumLabel = labelGo.AddComponent<TextMeshProUGUI>();
            momentumLabel.alignment = TextAlignmentOptions.Center;
            momentumLabel.fontSize = 13;
            momentumLabel.fontStyle = FontStyles.Bold;

            panel.SetActive(false);

            // 2. Unmounted Interaction Prompt (Bottom Center)
            mountPromptObj = new GameObject("MountInteractionPrompt");
            mountPromptObj.transform.SetParent(hudCanvasObj.transform, false);
            var promptRt = mountPromptObj.AddComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.5f, 0.26f);
            promptRt.anchorMax = new Vector2(0.5f, 0.26f);
            promptRt.sizeDelta = new Vector2(360f, 65f);

            var promptBg = mountPromptObj.AddComponent<Image>();
            promptBg.color = new Color(0.1f, 0.12f, 0.18f, 0.85f);

            var promptTextGo = new GameObject("PromptText");
            promptTextGo.transform.SetParent(mountPromptObj.transform, false);
            var textRt = promptTextGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            mountPromptText = promptTextGo.AddComponent<TextMeshProUGUI>();
            mountPromptText.alignment = TextAlignmentOptions.Center;
            mountPromptText.fontSize = 17;
            mountPromptText.color = Color.white;

            mountPromptObj.SetActive(false);
        }
    }
}
