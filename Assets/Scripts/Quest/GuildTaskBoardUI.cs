using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Quests
{
    /// <summary>
    /// GuildTaskBoardUI manages the full-screen interactive job bulletin board.
    /// Displays parchment flyers for Delivery, Slay, Escort, Bounty, Gather, and Investigation jobs.
    /// Fully mobile portrait 9:16 responsive with category filters and instant claim actions.
    /// </summary>
    public class GuildTaskBoardUI : MonoBehaviour
    {
        public static GuildTaskBoardUI Instance { get; private set; }

        [Header("Root Canvas & Panel")]
        [SerializeField] private Canvas taskBoardCanvas;
        [SerializeField] private CanvasGroup boardCanvasGroup;
        [SerializeField] private GameObject boardRootPanel;

        [Header("Header Elements")]
        [SerializeField] private TextMeshProUGUI guildTitleTMP;
        [SerializeField] private TextMeshProUGUI goldDisplayTMP;
        [SerializeField] private TextMeshProUGUI activeCountTMP;

        [Header("Filter Tabs")]
        [SerializeField] private Transform filterTabContainer;
        private QuestType? currentFilterType = null;

        [Header("Scroll List Container")]
        [SerializeField] private Transform jobCardsContainer;
        [SerializeField] private ScrollRect jobScrollRect;

        [Header("Status & Audio")]
        [SerializeField] private string openAudio = "relic_chime";
        [SerializeField] private string acceptAudio = "relic_chime";
        [SerializeField] private string claimAudio = "harp_resonance";

        private bool isOpen = false;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            BuildUIHierarchyIfMissing();
            CloseBoard(instant: true);
        }

        private void Start()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestListChanged += RefreshAllJobCards;
                QuestManager.Instance.OnGoldChanged += UpdateHeaderStats;
            }
            UpdateHeaderStats(QuestManager.Instance != null ? QuestManager.Instance.PlayerGold : 0);
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestListChanged -= RefreshAllJobCards;
                QuestManager.Instance.OnGoldChanged -= UpdateHeaderStats;
            }
        }

        private void Update()
        {
            if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
            {
                CloseBoard();
            }
        }

        public void ToggleTaskBoard()
        {
            if (isOpen) CloseBoard();
            else OpenBoard();
        }

        public void OpenBoard()
        {
            isOpen = true;
            BuildUIHierarchyIfMissing();

            if (taskBoardCanvas != null && taskBoardCanvas.worldCamera == null)
            {
                taskBoardCanvas.worldCamera = Camera.main;
            }

            if (boardRootPanel != null) boardRootPanel.SetActive(true);
            if (boardCanvasGroup != null)
            {
                boardCanvasGroup.alpha = 1f;
                boardCanvasGroup.blocksRaycasts = true;
                boardCanvasGroup.interactable = true;
            }

            RefreshAllJobCards();
            UpdateHeaderStats(QuestManager.Instance != null ? QuestManager.Instance.PlayerGold : 0);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX(openAudio, 0.9f);
            }
        }

        public void CloseBoard(bool instant = false)
        {
            isOpen = false;
            if (boardCanvasGroup != null)
            {
                boardCanvasGroup.alpha = 0f;
                boardCanvasGroup.blocksRaycasts = false;
                boardCanvasGroup.interactable = false;
            }
            if (boardRootPanel != null)
            {
                boardRootPanel.SetActive(false);
            }
        }

        private void UpdateHeaderStats(int gold)
        {
            if (goldDisplayTMP != null)
            {
                goldDisplayTMP.text = $"💰 <b>{gold}</b> G";
            }
            if (activeCountTMP != null && QuestManager.Instance != null)
            {
                activeCountTMP.text = $"ACTIVE JOBS: <b>{QuestManager.Instance.GetActiveQuestCount()}</b>";
            }
        }

        public void SetCategoryFilter(QuestType? filter)
        {
            currentFilterType = filter;
            RefreshAllJobCards();
        }

        public void RefreshAllJobCards()
        {
            if (jobCardsContainer == null || QuestManager.Instance == null) return;

            // Clear old cards
            for (int i = jobCardsContainer.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(jobCardsContainer.GetChild(i).gameObject);
                else
                    DestroyImmediate(jobCardsContainer.GetChild(i).gameObject);
            }

            List<Quest> filtered = QuestManager.Instance.GetQuests(currentFilterType);

            for (int i = 0; i < filtered.Count; i++)
            {
                CreateJobNoticeCard(jobCardsContainer, filtered[i]);
            }

            UpdateHeaderStats(QuestManager.Instance.PlayerGold);
        }

        private void CreateJobNoticeCard(Transform parent, Quest q)
        {
            GameObject cardObj = new GameObject($"JobCard_{q.questId}", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(920f, 215f);

            // Card background styling (parchment card with subtle border)
            Image bgImg = cardObj.GetComponent<Image>();
            bgImg.color = new Color(0.12f, 0.13f, 0.18f, 0.95f);

            // Vertical Layout
            VerticalLayoutGroup vLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 14, 14);
            vLayout.spacing = 6f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            // TOP ROW: Type Tag + Rank + Title
            GameObject topRow = new GameObject("TopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            topRow.transform.SetParent(cardObj.transform, false);
            RectTransform trRt = topRow.GetComponent<RectTransform>();
            trRt.sizeDelta = new Vector2(880f, 34f);
            HorizontalLayoutGroup trH = topRow.GetComponent<HorizontalLayoutGroup>();
            trH.spacing = 10f;
            trH.childControlWidth = false;
            trH.childControlHeight = true;

            // Quest Type Pill Badge
            GameObject typeBadge = new GameObject("TypeBadge", typeof(RectTransform), typeof(Image));
            typeBadge.transform.SetParent(topRow.transform, false);
            typeBadge.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 32f);
            Image typeImg = typeBadge.GetComponent<Image>();
            typeImg.color = q.GetTypeColor();

            GameObject typeTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            typeTxtObj.transform.SetParent(typeBadge.transform, false);
            RectTransform typeTxtRt = typeTxtObj.GetComponent<RectTransform>();
            typeTxtRt.anchorMin = Vector2.zero;
            typeTxtRt.anchorMax = Vector2.one;
            typeTxtRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI typeTxt = typeTxtObj.GetComponent<TextMeshProUGUI>();
            typeTxt.text = $"<b>{q.questType.ToString().ToUpper()}</b>";
            typeTxt.fontSize = 14;
            typeTxt.alignment = TextAlignmentOptions.Center;
            typeTxt.color = Color.white;

            // Rank Badge
            GameObject rankBadge = new GameObject("RankBadge", typeof(RectTransform), typeof(Image));
            rankBadge.transform.SetParent(topRow.transform, false);
            rankBadge.GetComponent<RectTransform>().sizeDelta = new Vector2(90f, 32f);
            Image rankImg = rankBadge.GetComponent<Image>();
            rankImg.color = new Color(0.25f, 0.25f, 0.35f);

            GameObject rankTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            rankTxtObj.transform.SetParent(rankBadge.transform, false);
            RectTransform rankTxtRt = rankTxtObj.GetComponent<RectTransform>();
            rankTxtRt.anchorMin = Vector2.zero;
            rankTxtRt.anchorMax = Vector2.one;
            rankTxtRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI rankTxt = rankTxtObj.GetComponent<TextMeshProUGUI>();
            rankTxt.text = $"<b>{q.GetFormattedRank()}</b>";
            rankTxt.fontSize = 13;
            rankTxt.alignment = TextAlignmentOptions.Center;
            rankTxt.color = new Color(1f, 0.85f, 0.3f);

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(topRow.transform, false);
            titleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 32f);
            TextMeshProUGUI titleTxt = titleObj.GetComponent<TextMeshProUGUI>();
            titleTxt.text = $"<b>{q.title}</b>";
            titleTxt.fontSize = 17;
            titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
            titleTxt.color = Color.white;

            // MIDDLE ROW: Client, Location & Lore summary
            GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(cardObj.transform, false);
            descObj.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 48f);
            TextMeshProUGUI descTxt = descObj.GetComponent<TextMeshProUGUI>();
            descTxt.text = $"<color=#8FA0B8>Client: <b>{q.clientName}</b>  •  Location: <b>{q.locationName}</b></color>\n<size=88%><color=#D0D8E4>{q.description}</color></size>";
            descTxt.fontSize = 13;
            descTxt.maxVisibleLines = 2;
            descTxt.overflowMode = TextOverflowModes.Ellipsis;

            // OBJECTIVE ROW: Progress & Target
            GameObject objRow = new GameObject("ObjectiveRow", typeof(RectTransform), typeof(TextMeshProUGUI));
            objRow.transform.SetParent(cardObj.transform, false);
            objRow.GetComponent<RectTransform>().sizeDelta = new Vector2(880f, 26f);
            TextMeshProUGUI objTxt = objRow.GetComponent<TextMeshProUGUI>();
            string progressHighlight = q.state == QuestState.Active ? $"<color=#50E080><b>[{q.GetProgressString()}]</b></color> " : "";
            objTxt.text = $"🎯 <color=#FFD700>Objective:</color> {progressHighlight}{q.objectiveText}";
            objTxt.fontSize = 14;

            // BOTTOM ROW: Rewards on left, Action Button on right
            GameObject bottomRow = new GameObject("BottomRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bottomRow.transform.SetParent(cardObj.transform, false);
            RectTransform brRt = bottomRow.GetComponent<RectTransform>();
            brRt.sizeDelta = new Vector2(880f, 46f);
            HorizontalLayoutGroup brH = bottomRow.GetComponent<HorizontalLayoutGroup>();
            brH.childControlWidth = false;
            brH.childControlHeight = true;
            brH.spacing = 15f;

            // Rewards string
            GameObject rewObj = new GameObject("RewardsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            rewObj.transform.SetParent(bottomRow.transform, false);
            rewObj.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 44f);
            TextMeshProUGUI rewTxt = rewObj.GetComponent<TextMeshProUGUI>();
            string itemStr = !string.IsNullOrEmpty(q.itemRewardName) ? $"  🎁 <color=#70D0FF>{q.itemRewardName}</color>" : "";
            rewTxt.text = $"💰 <color=#FFD700><b>{q.goldReward}G</b></color>   ⭐ <color=#C080FF><b>{q.expReward} EXP</b></color>{itemStr}";
            rewTxt.fontSize = 15;
            rewTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Action Button
            GameObject actionBtnObj = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            actionBtnObj.transform.SetParent(bottomRow.transform, false);
            RectTransform btnRt = actionBtnObj.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(230f, 44f);
            Image btnImg = actionBtnObj.GetComponent<Image>();
            Button btn = actionBtnObj.GetComponent<Button>();

            GameObject btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(actionBtnObj.transform, false);
            RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.sizeDelta = Vector2.zero;
            btnTxtRt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI btnTxt = btnTxtObj.GetComponent<TextMeshProUGUI>();
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.fontSize = 15;

            // Configure button state
            switch (q.state)
            {
                case QuestState.Available:
                    btnImg.color = new Color(0.18f, 0.58f, 0.32f); // Forest Green
                    btnTxt.text = "<b>ACCEPT JOB</b>";
                    btnTxt.color = Color.white;
                    btn.onClick.AddListener(() =>
                    {
                        QuestManager.Instance.AcceptQuest(q.questId);
                    });
                    break;

                case QuestState.Active:
                    btnImg.color = new Color(0.20f, 0.45f, 0.75f); // Cobalt Blue
                    btnTxt.text = $"<b>IN PROGRESS ({q.currentCount}/{q.targetCount})</b>";
                    btnTxt.color = Color.white;
                    // Clicking active button allows quick progress test or abandon
                    btn.onClick.AddListener(() =>
                    {
                        // Simulate objective progress for ease of testing!
                        QuestManager.Instance.ProgressQuest(q.questId, 1);
                    });
                    break;

                case QuestState.Completed:
                    btnImg.color = new Color(0.85f, 0.65f, 0.15f); // Radiant Gold
                    btnTxt.text = "<b>CLAIM REWARD! ★</b>";
                    btnTxt.color = new Color(0.10f, 0.10f, 0.10f);
                    btn.onClick.AddListener(() =>
                    {
                        QuestManager.Instance.ClaimQuestReward(q.questId);
                    });
                    break;

                case QuestState.Claimed:
                    btnImg.color = new Color(0.20f, 0.22f, 0.26f); // Slate Muted
                    btnTxt.text = "<b>COMPLETED ✓</b>";
                    btnTxt.color = new Color(0.6f, 0.65f, 0.7f);
                    btn.interactable = false;
                    break;
            }
        }

        #region Procedural UI Hierarchy Builder
        public void BuildUIHierarchyIfMissing()
        {
            if (taskBoardCanvas != null && boardRootPanel != null) return;

            // Find or create Canvas
            GameObject canvasGo = GameObject.Find("GuildTaskBoardCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("GuildTaskBoardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
                canvasGo.transform.SetParent(transform, false);
            }

            taskBoardCanvas = canvasGo.GetComponent<Canvas>();
            taskBoardCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            taskBoardCanvas.worldCamera = Camera.main;
            taskBoardCanvas.planeDistance = 1.0f;
            taskBoardCanvas.sortingOrder = 950; // Above gameplay, below system toasts

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            boardCanvasGroup = canvasGo.GetComponent<CanvasGroup>();

            // Fullscreen backdrop overlay
            GameObject overlayObj = new GameObject("DarkBackdropOverlay", typeof(RectTransform), typeof(Image));
            overlayObj.transform.SetParent(canvasGo.transform, false);
            RectTransform overRt = overlayObj.GetComponent<RectTransform>();
            overRt.anchorMin = Vector2.zero;
            overRt.anchorMax = Vector2.one;
            overRt.sizeDelta = Vector2.zero;
            Image overImg = overlayObj.GetComponent<Image>();
            overImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

            // Main Board Root Panel (Centered 980 x 1760 portrait frame)
            boardRootPanel = new GameObject("TaskBoardFrame", typeof(RectTransform), typeof(Image));
            boardRootPanel.transform.SetParent(canvasGo.transform, false);
            RectTransform boardRt = boardRootPanel.GetComponent<RectTransform>();
            boardRt.anchorMin = new Vector2(0.5f, 0.5f);
            boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.sizeDelta = new Vector2(980f, 1740f);
            boardRt.anchoredPosition = Vector2.zero;

            Image boardImg = boardRootPanel.GetComponent<Image>();
            boardImg.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);

            // HEADER PANEL (Top 180px)
            GameObject headerObj = new GameObject("HeaderPanel", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(boardRootPanel.transform, false);
            RectTransform hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(0f, 180f);
            hRt.anchoredPosition = Vector2.zero;
            Image hImg = headerObj.GetComponent<Image>();
            hImg.color = new Color(0.12f, 0.14f, 0.20f, 1f);

            // Title Text
            GameObject titleObj = new GameObject("GuildTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.05f, 0.40f);
            tRt.anchorMax = new Vector2(0.75f, 0.95f);
            tRt.sizeDelta = Vector2.zero;
            tRt.anchoredPosition = Vector2.zero;
            guildTitleTMP = titleObj.GetComponent<TextMeshProUGUI>();
            guildTitleTMP.text = "<color=#FFD700><b>SOLAS ADVENTURER'S GUILD</b></color>\n<size=70%><color=#A0B4CE>JOB DISPATCH & BOUNTY TASK BOARD</color></size>";
            guildTitleTMP.fontSize = 24;
            guildTitleTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Stats row (Gold & Active)
            GameObject statsObj = new GameObject("StatsRow", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(headerObj.transform, false);
            RectTransform sRt = statsObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.05f, 0.05f);
            sRt.anchorMax = new Vector2(0.75f, 0.40f);
            sRt.sizeDelta = Vector2.zero;
            sRt.anchoredPosition = Vector2.zero;
            goldDisplayTMP = statsObj.GetComponent<TextMeshProUGUI>();
            goldDisplayTMP.text = "💰 <b>500</b> G  •  ACTIVE: <b>0</b>";
            goldDisplayTMP.fontSize = 17;
            goldDisplayTMP.color = new Color(1f, 0.9f, 0.6f);
            goldDisplayTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Close Button (Top-Right of Header)
            GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 0.5f);
            cRt.anchorMax = new Vector2(1f, 0.5f);
            cRt.pivot = new Vector2(1f, 0.5f);
            cRt.sizeDelta = new Vector2(100f, 90f);
            cRt.anchoredPosition = new Vector2(-25f, 0f);
            Image cImg = closeBtnObj.GetComponent<Image>();
            cImg.color = new Color(0.85f, 0.22f, 0.22f);
            Button cBtn = closeBtnObj.GetComponent<Button>();
            cBtn.onClick.AddListener(() => CloseBoard());

            GameObject cTxtObj = new GameObject("CloseTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cTxtRt = cTxtObj.GetComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;
            cTxtRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI cTxt = cTxtObj.GetComponent<TextMeshProUGUI>();
            cTxt.text = "<b>✕</b>";
            cTxt.fontSize = 32;
            cTxt.alignment = TextAlignmentOptions.Center;
            cTxt.color = Color.white;

            // FILTER TABS BAR (Height 70px)
            GameObject tabsObj = new GameObject("FilterTabsBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsObj.transform.SetParent(boardRootPanel.transform, false);
            RectTransform tabRt = tabsObj.GetComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0f, 1f);
            tabRt.anchorMax = new Vector2(1f, 1f);
            tabRt.pivot = new Vector2(0.5f, 1f);
            tabRt.sizeDelta = new Vector2(0f, 70f);
            tabRt.anchoredPosition = new Vector2(0f, -180f);
            filterTabContainer = tabRt;

            HorizontalLayoutGroup tabH = tabsObj.GetComponent<HorizontalLayoutGroup>();
            tabH.padding = new RectOffset(20, 20, 10, 10);
            tabH.spacing = 10f;
            tabH.childControlWidth = true;
            tabH.childControlHeight = true;

            // Create Tabs: ALL, BOUNTY, SLAY, DELIVERY, ESCORT, GATHER
            CreateFilterTab(filterTabContainer, "ALL", null);
            CreateFilterTab(filterTabContainer, "BOUNTY", QuestType.Bounty);
            CreateFilterTab(filterTabContainer, "SLAY", QuestType.Slay);
            CreateFilterTab(filterTabContainer, "DELIVERY", QuestType.Delivery);
            CreateFilterTab(filterTabContainer, "ESCORT", QuestType.Escort);
            CreateFilterTab(filterTabContainer, "GATHER", QuestType.Gather);

            // SCROLL VIEW FOR JOB CARDS
            GameObject scrollGo = new GameObject("JobNoticeScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(boardRootPanel.transform, false);
            RectTransform sViewRt = scrollGo.GetComponent<RectTransform>();
            sViewRt.anchorMin = new Vector2(0.02f, 0.07f);
            sViewRt.anchorMax = new Vector2(0.98f, 1f);
            sViewRt.anchoredPosition = new Vector2(0f, -135f);
            sViewRt.sizeDelta = new Vector2(0f, -270f);

            Image scrollMaskImg = scrollGo.GetComponent<Image>();
            scrollMaskImg.color = new Color(0, 0, 0, 0.01f);

            jobScrollRect = scrollGo.GetComponent<ScrollRect>();
            jobScrollRect.horizontal = false;
            jobScrollRect.vertical = true;
            jobScrollRect.scrollSensitivity = 25f;

            // Content Container
            GameObject contentGo = new GameObject("JobCardsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(scrollGo.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup cV = contentGo.GetComponent<VerticalLayoutGroup>();
            cV.padding = new RectOffset(10, 10, 15, 20);
            cV.spacing = 14f;
            cV.childControlWidth = true;
            cV.childControlHeight = false;

            ContentSizeFitter csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            jobCardsContainer = contentGo.transform;
            jobScrollRect.content = contentRt;

            // BOTTOM BAR: Quick Sim / Progress & Close Hint
            GameObject bottomBar = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bottomBar.transform.SetParent(boardRootPanel.transform, false);
            RectTransform bbRt = bottomBar.GetComponent<RectTransform>();
            bbRt.anchorMin = Vector2.zero;
            bbRt.anchorMax = new Vector2(1f, 0.065f);
            bbRt.sizeDelta = Vector2.zero;
            bbRt.anchoredPosition = Vector2.zero;
            Image bbImg = bottomBar.GetComponent<Image>();
            bbImg.color = new Color(0.10f, 0.12f, 0.16f, 1f);

            GameObject hintObj = new GameObject("BottomHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintObj.transform.SetParent(bottomBar.transform, false);
            RectTransform hintRt = hintObj.GetComponent<RectTransform>();
            hintRt.anchorMin = Vector2.zero;
            hintRt.anchorMax = Vector2.one;
            hintRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI hintTmp = hintObj.GetComponent<TextMeshProUGUI>();
            hintTmp.text = "<color=#90A0B5>Tip: Tap <b>[IN PROGRESS]</b> on an active job to simulate objective completion!</color>";
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.fontSize = 14;
        }

        private void CreateFilterTab(Transform parent, string label, QuestType? type)
        {
            GameObject tabObj = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            tabObj.transform.SetParent(parent, false);

            Image img = tabObj.GetComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.30f);

            Button btn = tabObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                SetCategoryFilter(type);
            });

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(tabObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
        #endregion
    }
}
