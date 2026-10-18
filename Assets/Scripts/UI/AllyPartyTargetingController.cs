using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSummoner.Combat;
using BattleSummoner.Quests;

namespace BattleSummoner.UI
{
    [System.Serializable]
    public class SquadMemberTarget
    {
        public int id;
        public string allyName;
        public float maxHP = 500f;
        public float currentHP = 240f; // Damaged baseline so player can test healing!
        public float maxMP = 200f;
        public float currentMP = 120f;
        public RectTransform rootRect;
        public Image portraitImg;
        public Image miniHPImg;
        public Image miniMPImg;
        public TextMeshProUGUI hpValueText;
        public TextMeshProUGUI mpValueText;
        public Button clickButton;
        public GameObject selectionFrame;
    }

    /// <summary>
    /// AllyPartyTargetingController manages ally targeting on the right-hand side of the screen
    /// (PartySquadColumn with SquadMember_1 through SquadMember_9):
    /// - Enables tapping any ally portrait on the right to select them as the current target.
    /// - Displays an animated gold/emerald targeting reticle around the selected ally.
    /// - Applies healing from completed heart/circle circuits directly to the selected ally's health bar.
    /// - Displays real-time HP/MaxHP, floating heal numbers, and radiant emerald feedback.
    /// </summary>
    public class AllyPartyTargetingController : MonoBehaviour
    {
        private static AllyPartyTargetingController _instance;
        public static AllyPartyTargetingController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AllyPartyTargetingController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AllyPartyTargetingController");
                        _instance = go.AddComponent<AllyPartyTargetingController>();
                    }
                }
                return _instance;
            }
        }

        [Header("Squad Members on Right Side")]
        [SerializeField] private List<SquadMemberTarget> squadMembers = new List<SquadMemberTarget>();
        [SerializeField] private int selectedIndex = 0; // Default: SquadMember_1

        [Header("Targeting Indicator UI")]
        [SerializeField] private RectTransform globalTargetingReticle;
        [SerializeField] private TextMeshProUGUI activeTargetLabelTMP;

        public int SelectedIndex => selectedIndex;
        public SquadMemberTarget SelectedAlly => (squadMembers != null && squadMembers.Count > selectedIndex && selectedIndex >= 0) ? squadMembers[selectedIndex] : null;

        public event Action<SquadMemberTarget> OnAllyTargetSelected;
        public event Action<SquadMemberTarget, float> OnAllyHealed;

        private static readonly string[] SQUAD_NAMES = new string[]
        {
            "Sylph Familiar",
            "Ignis Sprite",
            "Aqua Nymph",
            "Golem Construct",
            "Aegis Knight",
            "Faerie Healer",
            "Shadow Stalker",
            "Storm Falcon",
            "Valkyrie Chariot"
        };

        private void Awake()
        {
            if (_instance == null) _instance = this;
            InitializeSquadMembers();
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnWarHornBlitzTriggered -= Activate18PersonBlitzUI;
                QuestManager.Instance.OnWarHornBlitzTriggered += Activate18PersonBlitzUI;
            }
        }

        private void Start()
        {
            InitializeSquadMembers();
            SelectAlly(0); // Select first ally by default
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnWarHornBlitzTriggered -= Activate18PersonBlitzUI;
            }
        }

        public void InitializeSquadMembers()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnWarHornBlitzTriggered -= Activate18PersonBlitzUI;
                QuestManager.Instance.OnWarHornBlitzTriggered += Activate18PersonBlitzUI;
            }

            if (squadMembers != null && squadMembers.Count == 9 && squadMembers[0].rootRect != null) return;

            squadMembers.Clear();
            var partyCol = GameObject.Find("PartySquadColumn");

            for (int i = 1; i <= 9; i++)
            {
                var memberGo = GameObject.Find($"SquadMember_{i}");
                if (memberGo == null && partyCol != null)
                {
                    var tr = partyCol.transform.Find($"SquadMember_{i}");
                    if (tr != null) memberGo = tr.gameObject;
                }

                if (memberGo != null)
                {
                    var target = new SquadMemberTarget();
                    target.id = i;
                    target.allyName = SQUAD_NAMES[(i - 1) % SQUAD_NAMES.Length];
                    target.maxHP = 500f;
                    target.currentHP = 220f + (i * 25f); // Varied starting health for gameplay testing
                    target.maxMP = 200f;
                    target.currentMP = 120f + (i * 8f);
                    target.rootRect = memberGo.GetComponent<RectTransform>();

                    var pfpTr = memberGo.transform.Find("Inner/AllyPortrait") ?? memberGo.transform.Find("AllyPortrait");
                    if (pfpTr != null) target.portraitImg = pfpTr.GetComponent<Image>();
                    else target.portraitImg = memberGo.GetComponent<Image>();

                    var hpTr = memberGo.transform.Find("Inner/MiniHP") ?? memberGo.transform.Find("MiniHP");
                    if (hpTr != null)
                    {
                        var fillTr = hpTr.Find("Fill") ?? hpTr.Find("MiniHPFill");
                        target.miniHPImg = (fillTr != null) ? fillTr.GetComponent<Image>() : hpTr.GetComponent<Image>();
                        if (target.miniHPImg != null)
                        {
                            target.miniHPImg.type = Image.Type.Filled;
                            target.miniHPImg.fillMethod = Image.FillMethod.Horizontal;
                            target.miniHPImg.color = new Color(0.92f, 0.20f, 0.26f, 1f);
                            target.miniHPImg.fillAmount = target.currentHP / target.maxHP;
                        }
                    }

                    var mpTr = memberGo.transform.Find("Inner/MiniMP") ?? memberGo.transform.Find("MiniMP");
                    if (mpTr != null)
                    {
                        var fillTr = mpTr.Find("Fill") ?? mpTr.Find("MiniMPFill");
                        target.miniMPImg = (fillTr != null) ? fillTr.GetComponent<Image>() : mpTr.GetComponent<Image>();
                        if (target.miniMPImg != null)
                        {
                            target.miniMPImg.type = Image.Type.Filled;
                            target.miniMPImg.fillMethod = Image.FillMethod.Horizontal;
                            target.miniMPImg.color = new Color(0.18f, 0.75f, 1.0f, 1f);
                            target.miniMPImg.fillAmount = target.currentMP / target.maxMP;
                        }
                    }

                    var hpNumTr = memberGo.transform.Find("Inner/HPNumberText") ?? memberGo.transform.Find("HPNumberText");
                    if (hpNumTr != null)
                    {
                        target.hpValueText = hpNumTr.GetComponent<TextMeshProUGUI>();
                        if (target.hpValueText != null)
                        {
                            target.hpValueText.text = $"<color=#FF5566><b>{(int)target.currentHP}</b></color>";
                        }
                    }

                    var mpNumTr = memberGo.transform.Find("Inner/MPNumberText") ?? memberGo.transform.Find("MPNumberText");
                    if (mpNumTr != null)
                    {
                        target.mpValueText = mpNumTr.GetComponent<TextMeshProUGUI>();
                        if (target.mpValueText != null)
                        {
                            target.mpValueText.text = $"<color=#33DDFF><b>{(int)target.currentMP}</b></color>";
                        }
                    }

                    // Add Button component if not present
                    target.clickButton = memberGo.GetComponent<Button>();
                    if (target.clickButton == null) target.clickButton = memberGo.AddComponent<Button>();

                    // Configure button color transition
                    var colors = target.clickButton.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                    colors.pressedColor = new Color(0.8f, 1.0f, 0.8f);
                    target.clickButton.colors = colors;

                    int capturedIndex = i - 1;
                    target.clickButton.onClick.RemoveAllListeners();
                    target.clickButton.onClick.AddListener(() => SelectAlly(capturedIndex));

                    // Build selection highlight reticle around portrait
                    var reticleObj = memberGo.transform.Find("SelectFrame");
                    if (reticleObj == null)
                    {
                        var frame = new GameObject("SelectFrame", typeof(RectTransform), typeof(Image));
                        frame.transform.SetParent(memberGo.transform, false);
                        var rt = frame.GetComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = new Vector2(-4f, -4f);
                        rt.offsetMax = new Vector2(4f, 4f);
                        var img = frame.GetComponent<Image>();
                        img.color = new Color(0.2f, 1.0f, 0.45f, 0.8f);
                        img.raycastTarget = false;
                        frame.SetActive(false);
                        target.selectionFrame = frame;
                    }
                    else
                    {
                        target.selectionFrame = reticleObj.gameObject;
                    }

                    squadMembers.Add(target);
                }
            }

            CreateTargetInfoLabel();
        }

        private void CreateTargetInfoLabel()
        {
            if (activeTargetLabelTMP == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var labelGo = GameObject.Find("ActiveAllyTargetBanner");
                    if (labelGo == null)
                    {
                        labelGo = new GameObject("ActiveAllyTargetBanner", typeof(RectTransform), typeof(TextMeshProUGUI));
                        labelGo.transform.SetParent(canvas.transform, false);
                        var rt = labelGo.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.55f, 0.74f);
                        rt.anchorMax = new Vector2(0.98f, 0.78f);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;

                        activeTargetLabelTMP = labelGo.GetComponent<TextMeshProUGUI>();
                        activeTargetLabelTMP.fontSize = 15;
                        activeTargetLabelTMP.alignment = TextAlignmentOptions.Right;
                        activeTargetLabelTMP.color = new Color(0.3f, 1.0f, 0.6f);
                        activeTargetLabelTMP.raycastTarget = false;
                    }
                    else
                    {
                        activeTargetLabelTMP = labelGo.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
        }

        public void SelectAlly(int index)
        {
            if (squadMembers == null || squadMembers.Count == 0) return;
            selectedIndex = Mathf.Clamp(index, 0, squadMembers.Count - 1);

            for (int i = 0; i < squadMembers.Count; i++)
            {
                bool isSelected = (i == selectedIndex);
                if (squadMembers[i].selectionFrame != null)
                {
                    squadMembers[i].selectionFrame.SetActive(isSelected);
                }
            }

            var current = SelectedAlly;
            if (current != null)
            {
                if (activeTargetLabelTMP != null)
                {
                    activeTargetLabelTMP.text = $"TARGET: <color=#FFE066>{current.allyName}</color> ({current.currentHP:F0}/{current.maxHP:F0})";
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.75f);
                }

                Debug.Log($"🎯 [ALLY TARGETED] Selected Ally {current.id}: '{current.allyName}' ({current.currentHP:F0}/{current.maxHP:F0} HP).");
                OnAllyTargetSelected?.Invoke(current);
            }
        }

        /// <summary>
        /// Applies a heal tick to the currently selected ally on the right-hand side.
        /// Scaled by accuracy and size of the completed heart/circuit!
        /// </summary>
        public float HealSelectedAlly(float baseHeal, float accuracy, float sizeRadius)
        {
            var ally = SelectedAlly;
            if (ally == null) return 0f;

            // Accuracy multiplier: 0.50 to 1.00 -> 0.70x to 1.50x
            float accMult = Mathf.Clamp(0.70f + (accuracy - 0.50f) * 1.6f, 0.70f, 1.50f);

            // Size multiplier: 50px to 300px -> 0.80x to 1.80x
            float sizeMult = Mathf.Clamp(0.80f + (sizeRadius - 50f) / 250f, 0.80f, 1.80f);

            float healAmount = baseHeal * accMult * sizeMult;
            float oldHP = ally.currentHP;
            ally.currentHP = Mathf.Min(ally.maxHP, ally.currentHP + healAmount);
            float actualHealed = ally.currentHP - oldHP;

            // Update UI health bar and stylized numerical readout
            if (ally.miniHPImg != null)
            {
                ally.miniHPImg.fillAmount = ally.currentHP / ally.maxHP;
            }
            if (ally.hpValueText != null)
            {
                ally.hpValueText.text = $"<color=#FF5566><b>{(int)ally.currentHP}</b></color>";
            }

            if (activeTargetLabelTMP != null)
            {
                activeTargetLabelTMP.text = $"TARGET: <color=#FFE066>{ally.allyName}</color> ({ally.currentHP:F0}/{ally.maxHP:F0})";
            }

            // Spawn floating heal combat text
            Vector3 worldPos = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 4f + Vector3.right * 1.2f : Vector3.zero;
            var familiar = GameObject.Find("Pygmy Sprite Familiar");
            if (familiar != null) worldPos = familiar.transform.position;

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnHealing(worldPos, healAmount);
            }

            // Play audio feedback
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 0.9f);
            }

            Debug.Log($"💚 [ALLY HEALED] Healed '{ally.allyName}' for +{healAmount:F0} HP (Acc: {accuracy * 100:F0}%, Size: {sizeRadius:F0}px, Total: {ally.currentHP:F0}/{ally.maxHP:F0}).");
            OnAllyHealed?.Invoke(ally, healAmount);
            return healAmount;
        }

        public void Activate18PersonBlitzUI(BattleSummoner.Quests.Quest q = null)
        {
            var partyCol = GameObject.Find("PartySquadColumn");
            GameObject blitzBannerObj = GameObject.Find("Blitz18PersonBanner");
            if (partyCol != null && blitzBannerObj == null)
            {
                blitzBannerObj = new GameObject("Blitz18PersonBanner", typeof(RectTransform), typeof(Image));
                blitzBannerObj.transform.SetParent(partyCol.transform, false);
                blitzBannerObj.transform.SetAsFirstSibling();
                RectTransform rt = blitzBannerObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100f, 44f);
                blitzBannerObj.GetComponent<Image>().color = new Color(0.85f, 0.25f, 0.12f, 0.95f);

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(blitzBannerObj.transform, false);
                RectTransform tr = txtObj.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.sizeDelta = Vector2.zero;
                TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "📯 <b>18-UNIT BLITZ!</b>";
                tmp.fontSize = 11;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            if (blitzBannerObj != null) blitzBannerObj.SetActive(true);

            // Give party portraits an active fiery gold frenzy border
            for (int i = 0; i < squadMembers.Count; i++)
            {
                if (squadMembers[i].selectionFrame != null)
                {
                    squadMembers[i].selectionFrame.SetActive(true);
                    var img = squadMembers[i].selectionFrame.GetComponent<Image>();
                    if (img != null) img.color = new Color(1.0f, 0.45f, 0.15f, 0.95f);
                }
            }
        }
    }
}
