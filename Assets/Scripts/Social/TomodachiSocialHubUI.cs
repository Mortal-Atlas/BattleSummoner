using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.Social
{
    /// <summary>
    /// TomodachiSocialHubUI controls the Famigotchi Familiar Sanctuary & Social Hub:
    /// - Comprehensive Tamagotchi Pet Familiar Care (Ni no Kuni feeding + Pokémon Amie bonding)
    /// - Pet Care Actions: Feed elemental treats, Give spring water, Bathe with bubble sponge, Give presents, Direct petting
    /// - 4 Need Bars: Fullness (Hunger), Hydration, Cleanliness (Bath), Joy (Happiness)
    /// - Ni no Kuni permanent stat growths: ATK (Fire), MAG (Water), DEF (Earth), SPD (Wind)
    /// - Pokémon Affection Hearts (1-5) and active combat bond perks (e.g. Endure lethal hits with 1 HP)
    /// - Dynamic interactive thought/speech bubble over the familiar responding to care and vitals
    /// - Also supports Persona-style VN dialogue, satchel gifts, and sparring for ally NPCs & summons
    /// </summary>
    public class TomodachiSocialHubUI : MonoBehaviour
    {
        private static TomodachiSocialHubUI _instance;
        public static TomodachiSocialHubUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<TomodachiSocialHubUI>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Overlay Root & Header")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI headerTitleTMP;

        [Header("Category Filter Tabs")]
        [SerializeField] private Button tabAllBtn;
        [SerializeField] private Button tabNpcsBtn;
        [SerializeField] private Button tabFamiliarsBtn;
        [SerializeField] private Button tabSummonsBtn;
        [SerializeField] private Button tabConstructsBtn;

        [Header("Roster Scroll Area")]
        [SerializeField] private Transform rosterContainer;

        [Header("Inspector Showcase")]
        [SerializeField] private Image characterPortraitImg;
        [SerializeField] private Image arcanaCardBackingImg;
        [SerializeField] private TextMeshProUGUI nameTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI arcanaRankTMP;
        [SerializeField] private TextMeshProUGUI moodBadgeTMP;
        [SerializeField] private Slider affinityBar;
        [SerializeField] private Image affinityBarFill;
        [SerializeField] private TextMeshProUGUI affinityValueTMP;
        [SerializeField] private TextMeshProUGUI tierTagTMP;
        [SerializeField] private TextMeshProUGUI loreTMP;

        [Header("Famigotchi Pet Care Sanctuary Fields")]
        [SerializeField] private GameObject speechBubbleObj;
        [SerializeField] private TextMeshProUGUI speechBubbleTMP;
        [SerializeField] private Button petClickAreaBtn;

        [Header("Tamagotchi Need Bars")]
        [SerializeField] private GameObject petCareVitalsContainer;
        [SerializeField] private Slider fullnessBar;
        [SerializeField] private TextMeshProUGUI fullnessValTMP;
        [SerializeField] private Slider hydrationBar;
        [SerializeField] private TextMeshProUGUI hydrationValTMP;
        [SerializeField] private Slider cleanlinessBar;
        [SerializeField] private TextMeshProUGUI cleanlinessValTMP;
        [SerializeField] private Slider happinessBar;
        [SerializeField] private TextMeshProUGUI happinessValTMP;

        [Header("Pokémon Affection & Bond Perk")]
        [SerializeField] private TextMeshProUGUI affectionHeartsTMP;
        [SerializeField] private TextMeshProUGUI bondPerkTMP;

        [Header("Ni no Kuni Stat Boosts")]
        [SerializeField] private GameObject niNoKuniStatsContainer;
        [SerializeField] private TextMeshProUGUI statAtkTMP;
        [SerializeField] private TextMeshProUGUI statDefTMP;
        [SerializeField] private TextMeshProUGUI statMagTMP;
        [SerializeField] private TextMeshProUGUI statSpdTMP;

        [Header("Pet Care Action Deck")]
        [SerializeField] private GameObject petActionDeckObj;
        [SerializeField] private Button feedTreatBtn;
        [SerializeField] private Button giveWaterBtn;
        [SerializeField] private Button bathePetBtn;
        [SerializeField] private Button givePresentBtn;
        [SerializeField] private Button petCuddleBtn;

        [Header("NPC / Ally Action Deck")]
        [SerializeField] private GameObject npcActionDeckObj;
        [SerializeField] private Button giftBtn;
        [SerializeField] private Button talkBtn;
        [SerializeField] private Button sparBtn;

        [Header("Active Effect Card")]
        [SerializeField] private GameObject activeEffectCardObj;
        [SerializeField] private Image activeEffectBorderImg;
        [SerializeField] private TextMeshProUGUI activeEffectTitleTMP;
        [SerializeField] private TextMeshProUGUI activeEffectDescTMP;

        [Header("Treat Drawer (Ni no Kuni)")]
        [SerializeField] private GameObject treatDrawerRoot;
        [SerializeField] private Transform treatGridContainer;
        [SerializeField] private Button closeTreatDrawerBtn;

        [Header("Present Drawer (Pokémon-Amie)")]
        [SerializeField] private GameObject presentDrawerRoot;
        [SerializeField] private Transform presentGridContainer;
        [SerializeField] private Button closePresentDrawerBtn;

        [Header("General Gift Selection Drawer (Satchel Items)")]
        [SerializeField] private GameObject giftDrawerRoot;
        [SerializeField] private Transform giftGridContainer;
        [SerializeField] private Button closeGiftDrawerBtn;

        [Header("Floating Hearts FX")]
        [SerializeField] private RectTransform heartsFxContainer;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

        private SocialEntityType activeCategory = SocialEntityType.Familiar;
        private SocialEntityProfile selectedEntity;
        private readonly List<GameObject> activeRosterCardObjects = new List<GameObject>();
        private readonly List<GameObject> activeGiftSlotObjects = new List<GameObject>();
        private readonly List<GameObject> activeTreatSlotObjects = new List<GameObject>();
        private readonly List<GameObject> activePresentSlotObjects = new List<GameObject>();
        private Coroutine portraitBreathingRoutine;
        private Coroutine speechResetRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            if (overlayRoot == null) overlayRoot = gameObject;
            BindButtons();
            SetVisible(false);
            if (giftDrawerRoot != null) giftDrawerRoot.SetActive(false);
            if (treatDrawerRoot != null) treatDrawerRoot.SetActive(false);
            if (presentDrawerRoot != null) presentDrawerRoot.SetActive(false);
        }

        private void Update()
        {
            // Desktop hotkey 'T' toggles Famigotchi Sanctuary
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleSocialHub();
            }
        }

        private void OnEnable()
        {
            TomodachiSocialLinkManager.OnAffinityChanged += HandleAffinityChanged;
            TomodachiSocialLinkManager.OnMoodChanged += HandleMoodChanged;
            TomodachiSocialLinkManager.OnRankUp += HandleRankUp;
            TomodachiSocialLinkManager.OnPetFed += HandlePetFed;
            TomodachiSocialLinkManager.OnPetWatered += HandlePetWatered;
            TomodachiSocialLinkManager.OnPetBathed += HandlePetBathed;
            TomodachiSocialLinkManager.OnPetPresentGiven += HandlePetPresentGiven;
            TomodachiSocialLinkManager.OnPetPetted += HandlePetPetted;
        }

        private void OnDisable()
        {
            TomodachiSocialLinkManager.OnAffinityChanged -= HandleAffinityChanged;
            TomodachiSocialLinkManager.OnMoodChanged -= HandleMoodChanged;
            TomodachiSocialLinkManager.OnRankUp -= HandleRankUp;
            TomodachiSocialLinkManager.OnPetFed -= HandlePetFed;
            TomodachiSocialLinkManager.OnPetWatered -= HandlePetWatered;
            TomodachiSocialLinkManager.OnPetBathed -= HandlePetBathed;
            TomodachiSocialLinkManager.OnPetPresentGiven -= HandlePetPresentGiven;
            TomodachiSocialLinkManager.OnPetPetted -= HandlePetPetted;
        }

        public void ToggleSocialHub()
        {
            SetVisible(!IsOpen);
        }

        public void SetVisible(bool visible)
        {
            if (overlayRoot != null) overlayRoot.SetActive(visible);

            // Toggle HUD visibility for safe viewport display
            Transform parentTrans = transform.parent;
            if (parentTrans != null)
            {
                Transform hud = parentTrans.Find("MockupMasterHUD");
                if (hud != null) hud.gameObject.SetActive(!visible);

                Transform safeArea = parentTrans.Find("SafeAreaContainer");
                if (safeArea != null) safeArea.gameObject.SetActive(!visible);
            }
            else
            {
                GameObject hudGo = GameObject.Find("MockupMasterHUD");
                if (hudGo != null) hudGo.SetActive(!visible);
                GameObject safeGo = GameObject.Find("SafeAreaContainer");
                if (safeGo != null) safeGo.SetActive(!visible);
            }

            if (visible)
            {
                CloseAllDrawers();
                activeCategory = SocialEntityType.Familiar;
                RefreshRosterList();
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.9f);
                }

                if (portraitBreathingRoutine != null) StopCoroutine(portraitBreathingRoutine);
                portraitBreathingRoutine = StartCoroutine(AnimatePortraitBreathing());
            }
            else
            {
                if (portraitBreathingRoutine != null)
                {
                    StopCoroutine(portraitBreathingRoutine);
                    portraitBreathingRoutine = null;
                }
                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_close", 0.8f);
                }
            }
        }

        public void CloseAllDrawers()
        {
            if (giftDrawerRoot != null) giftDrawerRoot.SetActive(false);
            if (treatDrawerRoot != null) treatDrawerRoot.SetActive(false);
            if (presentDrawerRoot != null) presentDrawerRoot.SetActive(false);
        }

        private void BindButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => SetVisible(false));
            }

            if (tabAllBtn != null) tabAllBtn.onClick.AddListener(() => SetCategory(SocialEntityType.All));
            if (tabNpcsBtn != null) tabNpcsBtn.onClick.AddListener(() => SetCategory(SocialEntityType.NPC));
            if (tabFamiliarsBtn != null) tabFamiliarsBtn.onClick.AddListener(() => SetCategory(SocialEntityType.Familiar));
            if (tabSummonsBtn != null) tabSummonsBtn.onClick.AddListener(() => SetCategory(SocialEntityType.Summon));
            if (tabConstructsBtn != null) tabConstructsBtn.onClick.AddListener(() => SetCategory(SocialEntityType.Construct));

            // Pet Care Action Buttons
            if (feedTreatBtn != null) feedTreatBtn.onClick.AddListener(OnFeedTreatButtonClicked);
            if (giveWaterBtn != null) giveWaterBtn.onClick.AddListener(OnGiveWaterButtonClicked);
            if (bathePetBtn != null) bathePetBtn.onClick.AddListener(OnBathePetButtonClicked);
            if (givePresentBtn != null) givePresentBtn.onClick.AddListener(OnGivePresentButtonClicked);
            if (petCuddleBtn != null) petCuddleBtn.onClick.AddListener(OnPetCuddleButtonClicked);
            if (petClickAreaBtn != null) petClickAreaBtn.onClick.AddListener(OnPetCuddleButtonClicked);

            // NPC Action Buttons
            if (giftBtn != null) giftBtn.onClick.AddListener(OnGiftButtonClicked);
            if (talkBtn != null) talkBtn.onClick.AddListener(OnTalkButtonClicked);
            if (sparBtn != null) sparBtn.onClick.AddListener(OnSparButtonClicked);

            // Drawer Close Buttons
            if (closeGiftDrawerBtn != null) closeGiftDrawerBtn.onClick.AddListener(CloseAllDrawers);
            if (closeTreatDrawerBtn != null) closeTreatDrawerBtn.onClick.AddListener(CloseAllDrawers);
            if (closePresentDrawerBtn != null) closePresentDrawerBtn.onClick.AddListener(CloseAllDrawers);
        }

        public void SetCategory(SocialEntityType type)
        {
            activeCategory = type;
            RefreshRosterList();
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("loadout_switch", 0.6f);
            }
        }

        public void RefreshRosterList()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < activeRosterCardObjects.Count; i++)
                {
                    if (activeRosterCardObjects[i] != null) Destroy(activeRosterCardObjects[i]);
                }
            }
            else
            {
                if (rosterContainer != null)
                {
                    for (int i = rosterContainer.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(rosterContainer.GetChild(i).gameObject);
                    }
                }
            }
            activeRosterCardObjects.Clear();

            var list = TomodachiSocialLinkManager.Instance.GetEntitiesByType(activeCategory);

            if (rosterContainer != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var entity = list[i];
                    GameObject card = CreateRosterCard(entity, i);
                    activeRosterCardObjects.Add(card);
                }
            }

            if (list.Count > 0)
            {
                if (selectedEntity == null || !list.Contains(selectedEntity))
                {
                    SelectEntity(list[0]);
                }
                else
                {
                    SelectEntity(selectedEntity);
                }
            }
            else
            {
                ClearInspector();
            }
        }

        private GameObject CreateRosterCard(SocialEntityProfile entity, int index)
        {
            GameObject cardObj = new GameObject($"RosterCard_{index}_{entity.entityId}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardObj.transform.SetParent(rosterContainer, false);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280f, 68f);

            Image bg = cardObj.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.20f, 0.95f);

            Button btn = cardObj.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectEntity(entity));

            // Thumbnail Icon
            GameObject iconObj = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.03f, 0.12f);
            irt.anchorMax = new Vector2(0.22f, 0.88f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.GetComponent<Image>();
            Sprite s = LoadSprite(entity.portraitSpriteName);
            if (s != null) iconImg.sprite = s;
            iconImg.preserveAspect = true;

            // Name & Arcana / Hearts Label
            GameObject nameObj = new GameObject("NameLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nrt = nameObj.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.26f, 0.52f);
            nrt.anchorMax = new Vector2(0.96f, 0.95f);
            nrt.offsetMin = Vector2.zero;
            nrt.offsetMax = Vector2.zero;
            TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();

            if (entity.entityType == SocialEntityType.Familiar && entity.careState != null)
            {
                nameTmp.text = $"<b>{entity.displayName}</b> <size=85%>{entity.careState.GetAffectionHeartsString()}</size>";
            }
            else
            {
                nameTmp.text = $"<b>{entity.displayName}</b> <size=80%><color=#FFD700>[RK {entity.socialLinkRank}]</color></size>";
            }
            nameTmp.fontSize = 13;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.color = Color.white;

            // Mood & Need/Tier label
            GameObject subObj = new GameObject("SubLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(cardObj.transform, false);
            RectTransform srt = subObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.26f, 0.05f);
            srt.anchorMax = new Vector2(0.96f, 0.48f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            TextMeshProUGUI subTmp = subObj.GetComponent<TextMeshProUGUI>();

            if (entity.entityType == SocialEntityType.Familiar && entity.careState != null)
            {
                subTmp.text = $"🍖 {entity.careState.hunger:F0}% | 💧 {entity.careState.hydration:F0}% | 🧼 {entity.careState.cleanliness:F0}% | ✨ {entity.careState.happiness:F0}%";
            }
            else
            {
                string tierHex = ColorUtility.ToHtmlStringRGB(entity.GetTierColor());
                subTmp.text = $"{entity.GetMoodEmoji()} // <color=#{tierHex}>{entity.GetRelationshipTier()}</color>";
            }
            subTmp.fontSize = 10;
            subTmp.alignment = TextAlignmentOptions.MidlineLeft;

            return cardObj;
        }

        public void SelectEntity(SocialEntityProfile entity)
        {
            selectedEntity = entity;
            if (entity == null)
            {
                ClearInspector();
                return;
            }

            if (nameTMP != null) nameTMP.text = $"<b>{entity.displayName.ToUpper()}</b>";
            if (subtitleTMP != null) subtitleTMP.text = entity.subtitle.ToUpper();
            if (arcanaRankTMP != null) arcanaRankTMP.text = $"<color=#FFD700><b>{entity.arcanaTitle}</b></color> // <color=#40EBFF>RANK {entity.socialLinkRank}</color>";

            if (moodBadgeTMP != null)
            {
                moodBadgeTMP.text = entity.GetMoodEmoji();
            }

            // Affinity Bar & Values (-100 to 100 mapped to 0 to 1)
            float normAffinity = (entity.currentAffinity + 100f) / 200f;
            if (affinityBar != null) affinityBar.value = normAffinity;
            if (affinityBarFill != null) affinityBarFill.color = entity.GetTierColor();

            if (affinityValueTMP != null)
            {
                string sign = (entity.currentAffinity > 0) ? $"+{entity.currentAffinity}" : $"{entity.currentAffinity}";
                affinityValueTMP.text = $"{sign} / 100";
            }

            if (tierTagTMP != null)
            {
                string tierHex = ColorUtility.ToHtmlStringRGB(entity.GetTierColor());
                tierTagTMP.text = $"<color=#{tierHex}><b>{entity.GetRelationshipTier().ToString().ToUpper()}</b></color>";
            }

            if (loreTMP != null) loreTMP.text = entity.loreDescription;

            if (characterPortraitImg != null)
            {
                Sprite s = LoadSprite(entity.portraitSpriteName);
                if (s != null) characterPortraitImg.sprite = s;
                characterPortraitImg.preserveAspect = true;
            }

            if (arcanaCardBackingImg != null)
            {
                Sprite cardSprite = LoadSprite("Mockup_SocialArcanaCard.png");
                if (cardSprite != null) arcanaCardBackingImg.sprite = cardSprite;
            }

            // Differentiate between Familiar (Tamagotchi Pet Care) and NPC/Summon
            bool isFamiliar = (entity.entityType == SocialEntityType.Familiar && entity.careState != null);

            if (petActionDeckObj != null) petActionDeckObj.SetActive(isFamiliar);
            if (petCareVitalsContainer != null) petCareVitalsContainer.SetActive(isFamiliar);
            if (niNoKuniStatsContainer != null) niNoKuniStatsContainer.SetActive(isFamiliar);
            if (speechBubbleObj != null) speechBubbleObj.SetActive(isFamiliar);

            if (npcActionDeckObj != null) npcActionDeckObj.SetActive(!isFamiliar);

            if (isFamiliar)
            {
                UpdateFamiliarCareUI(entity);
            }
            else
            {
                UpdateActiveEffectCard(entity);
            }
        }

        private void UpdateFamiliarCareUI(SocialEntityProfile entity)
        {
            var care = entity.careState;
            if (care == null) return;

            // 4 Need Bars (Tamagotchi Vitals)
            if (fullnessBar != null) fullnessBar.value = care.hunger / 100f;
            if (fullnessValTMP != null) fullnessValTMP.text = $"{care.hunger:F0}%";

            if (hydrationBar != null) hydrationBar.value = care.hydration / 100f;
            if (hydrationValTMP != null) hydrationValTMP.text = $"{care.hydration:F0}%";

            if (cleanlinessBar != null) cleanlinessBar.value = care.cleanliness / 100f;
            if (cleanlinessValTMP != null) cleanlinessValTMP.text = $"{care.cleanliness:F0}%";

            if (happinessBar != null) happinessBar.value = care.happiness / 100f;
            if (happinessValTMP != null) happinessValTMP.text = $"{care.happiness:F0}%";

            // Pokémon Affection Hearts (1 to 5)
            if (affectionHeartsTMP != null)
            {
                affectionHeartsTMP.text = $"<color=#FFD700>BOND:</color> {care.GetAffectionHeartsString()} <size=85%>(LV {care.affectionHearts}/5)</size>";
            }

            if (bondPerkTMP != null)
            {
                bondPerkTMP.text = care.GetPerkDescription();
            }

            // Ni no Kuni Permanent Stat Boosts
            if (statAtkTMP != null) statAtkTMP.text = $"<color=#FF5533>⚔ ATK</color> <b>+{care.bonusAttack}</b>";
            if (statDefTMP != null) statDefTMP.text = $"<color=#EEAA22>🛡 DEF</color> <b>+{care.bonusDefense}</b>";
            if (statMagTMP != null) statMagTMP.text = $"<color=#33BBFF>🔮 MAG</color> <b>+{care.bonusMagic}</b>";
            if (statSpdTMP != null) statSpdTMP.text = $"<color=#44EE88>⚡ SPD</color> <b>+{care.bonusSpeed}</b>";

            // Speech Bubble Dialogue
            UpdateFamiliarSpeechBubble(entity);

            if (activeEffectCardObj != null) activeEffectCardObj.SetActive(false);
        }

        private void UpdateFamiliarSpeechBubble(SocialEntityProfile entity, string customMessage = null)
        {
            if (speechBubbleObj == null || speechBubbleTMP == null) return;

            speechBubbleObj.SetActive(true);

            if (!string.IsNullOrEmpty(customMessage))
            {
                speechBubbleTMP.text = customMessage;
                if (speechResetRoutine != null) StopCoroutine(speechResetRoutine);
                speechResetRoutine = StartCoroutine(ResetSpeechBubbleAfterDelay(entity, 4.0f));
                return;
            }

            var care = entity.careState;
            if (care == null) return;

            if (care.hunger <= 40f)
            {
                speechBubbleTMP.text = $"*Pats tummy* <i>\"{entity.displayName}'s tummy is rumbling... Could I have a tasty {care.favoriteFood}?\"</i>";
            }
            else if (care.hydration <= 40f)
            {
                speechBubbleTMP.text = $"*Licks lips* <i>\"Thirsty... A bowl of crystal spring dew would be heavenly!\"</i>";
            }
            else if (care.cleanliness <= 40f)
            {
                speechBubbleTMP.text = $"*Shakes off dust* <i>\"Feathers and fur feel dusty from dungeon crawling... bubble bath time?\"</i>";
            }
            else if (care.happiness >= 85f)
            {
                speechBubbleTMP.text = $"*Radiates sparkles and purrs!* <i>\"I feel so happy and loved! Master is the best summoner in the whole Spire! ✨\"</i>";
            }
            else
            {
                speechBubbleTMP.text = $"*Snuggles close* <i>\"Ready to explore together, Master! Don't forget my treats!\"</i>";
            }
        }

        private IEnumerator ResetSpeechBubbleAfterDelay(SocialEntityProfile entity, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (selectedEntity != null && selectedEntity.entityId == entity.entityId)
            {
                UpdateFamiliarSpeechBubble(entity, null);
            }
        }

        private void UpdateActiveEffectCard(SocialEntityProfile entity)
        {
            var effect = entity.GetActiveEffect();
            if (effect != null)
            {
                if (activeEffectCardObj != null) activeEffectCardObj.SetActive(true);
                if (activeEffectBorderImg != null)
                {
                    activeEffectBorderImg.color = effect.isBuff 
                        ? new Color(0.20f, 0.90f, 0.45f, 1f) 
                        : new Color(0.95f, 0.20f, 0.25f, 1f);
                }

                string tag = effect.isBuff ? "<color=#30FF80>[ACTIVE LINK BUFF]</color>" : "<color=#FF3040>[ACTIVE LINK DEBUFF]</color>";
                if (activeEffectTitleTMP != null) activeEffectTitleTMP.text = $"{tag} <b>{effect.title.ToUpper()}</b>";
                if (activeEffectDescTMP != null) activeEffectDescTMP.text = effect.description;
            }
            else
            {
                if (activeEffectCardObj != null) activeEffectCardObj.SetActive(true);
                if (activeEffectBorderImg != null) activeEffectBorderImg.color = new Color(0.40f, 0.45f, 0.55f, 0.8f);
                if (activeEffectTitleTMP != null) activeEffectTitleTMP.text = "<color=#A0A5B5>[LINK DORMANT]</color> <b>NEUTRAL ALIGNMENT</b>";
                if (activeEffectDescTMP != null) activeEffectDescTMP.text = "Feed, bathe, and give presents to deepen affection hearts and unlock combat perks!";
            }
        }

        private void ClearInspector()
        {
            selectedEntity = null;
            if (nameTMP != null) nameTMP.text = "SELECT AN ENTITY";
            if (subtitleTMP != null) subtitleTMP.text = "";
            if (arcanaRankTMP != null) arcanaRankTMP.text = "";
            if (moodBadgeTMP != null) moodBadgeTMP.text = "";
            if (affinityValueTMP != null) affinityValueTMP.text = "";
            if (tierTagTMP != null) tierTagTMP.text = "";
            if (loreTMP != null) loreTMP.text = "Select a familiar companion, ally NPC, or summon to care and bond.";
            if (activeEffectCardObj != null) activeEffectCardObj.SetActive(false);
            if (petCareVitalsContainer != null) petCareVitalsContainer.SetActive(false);
            if (petActionDeckObj != null) petActionDeckObj.SetActive(false);
            if (speechBubbleObj != null) speechBubbleObj.SetActive(false);
        }

        #region Pet Care Action Handlers
        private void OnFeedTreatButtonClicked()
        {
            if (selectedEntity == null) return;
            OpenTreatDrawer();
        }

        private void OnGiveWaterButtonClicked()
        {
            if (selectedEntity == null) return;
            string feedback;
            bool success = TomodachiSocialLinkManager.Instance.GiveWaterToFamiliar(selectedEntity.entityId, out feedback);
            UpdateFamiliarSpeechBubble(selectedEntity, feedback);
            SpawnHeartFX();
            SelectEntity(selectedEntity);
        }

        private void OnBathePetButtonClicked()
        {
            if (selectedEntity == null) return;
            string feedback;
            bool success = TomodachiSocialLinkManager.Instance.BatheFamiliar(selectedEntity.entityId, out feedback);
            UpdateFamiliarSpeechBubble(selectedEntity, feedback);
            SpawnHeartFX();
            SelectEntity(selectedEntity);
        }

        private void OnGivePresentButtonClicked()
        {
            if (selectedEntity == null) return;
            OpenPresentDrawer();
        }

        private void OnPetCuddleButtonClicked()
        {
            if (selectedEntity == null) return;
            string feedback;
            bool success = TomodachiSocialLinkManager.Instance.PetFamiliar(selectedEntity.entityId, out feedback);
            UpdateFamiliarSpeechBubble(selectedEntity, feedback);
            SpawnHeartFX();
            SelectEntity(selectedEntity);
        }
        #endregion

        #region NPC Action Handlers
        private void OnGiftButtonClicked()
        {
            if (selectedEntity == null) return;
            OpenGiftDrawer();
        }

        private void OnTalkButtonClicked()
        {
            if (selectedEntity == null) return;
            TomodachiSocialLinkManager.Instance.TalkWithEntity(selectedEntity);
        }

        private void OnSparButtonClicked()
        {
            if (selectedEntity == null) return;
            TomodachiSocialLinkManager.Instance.SparWithEntity(selectedEntity);
        }
        #endregion

        #region Treat Drawer (Ni no Kuni)
        public void OpenTreatDrawer()
        {
            CloseAllDrawers();
            if (treatDrawerRoot != null) treatDrawerRoot.SetActive(true);
            RefreshTreatDrawer();
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.8f);
            }
        }

        private void RefreshTreatDrawer()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < activeTreatSlotObjects.Count; i++)
                {
                    if (activeTreatSlotObjects[i] != null) Destroy(activeTreatSlotObjects[i]);
                }
            }
            else
            {
                if (treatGridContainer != null)
                {
                    for (int i = treatGridContainer.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(treatGridContainer.GetChild(i).gameObject);
                    }
                }
            }
            activeTreatSlotObjects.Clear();

            if (treatGridContainer == null) return;

            var treats = TomodachiSocialLinkManager.Instance.GetAvailableTreats();
            for (int i = 0; i < treats.Count; i++)
            {
                var treat = treats[i];
                GameObject slot = CreateTreatSlot(treat, i);
                activeTreatSlotObjects.Add(slot);
            }
        }

        private GameObject CreateTreatSlot(PetTreatItem treat, int index)
        {
            GameObject slotObj = new GameObject($"TreatSlot_{index}_{treat.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotObj.transform.SetParent(treatGridContainer, false);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250f, 68f);

            Image bg = slotObj.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

            Button btn = slotObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (selectedEntity != null)
                {
                    string feedback;
                    bool success = TomodachiSocialLinkManager.Instance.FeedFamiliar(selectedEntity.entityId, treat, out feedback);
                    UpdateFamiliarSpeechBubble(selectedEntity, feedback);
                    SpawnHeartFX();
                    if (treatDrawerRoot != null) treatDrawerRoot.SetActive(false);
                    SelectEntity(selectedEntity);
                    RefreshRosterList();
                }
            });

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.04f, 0.12f);
            irt.anchorMax = new Vector2(0.24f, 0.88f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.GetComponent<Image>();
            Sprite s = LoadSprite(treat.iconSpriteName);
            if (s != null) iconImg.sprite = s;
            iconImg.preserveAspect = true;

            // Details
            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(slotObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.28f, 0.05f);
            trt.anchorMax = new Vector2(0.96f, 0.95f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            string colHex = ColorUtility.ToHtmlStringRGB(treat.themeColor);
            tmp.text = $"<color=#{colHex}><b>{treat.name}</b></color> <size=80%>(+{treat.statGain} {treat.statBoostType})</size>\n<size=75%><color=#D0D5E5>{treat.description}</color></size>";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return slotObj;
        }
        #endregion

        #region Present Drawer (Pokémon-Amie)
        public void OpenPresentDrawer()
        {
            CloseAllDrawers();
            if (presentDrawerRoot != null) presentDrawerRoot.SetActive(true);
            RefreshPresentDrawer();
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.8f);
            }
        }

        private void RefreshPresentDrawer()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < activePresentSlotObjects.Count; i++)
                {
                    if (activePresentSlotObjects[i] != null) Destroy(activePresentSlotObjects[i]);
                }
            }
            else
            {
                if (presentGridContainer != null)
                {
                    for (int i = presentGridContainer.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(presentGridContainer.GetChild(i).gameObject);
                    }
                }
            }
            activePresentSlotObjects.Clear();

            if (presentGridContainer == null) return;

            var presents = TomodachiSocialLinkManager.Instance.GetAvailablePresents();
            for (int i = 0; i < presents.Count; i++)
            {
                var present = presents[i];
                GameObject slot = CreatePresentSlot(present, i);
                activePresentSlotObjects.Add(slot);
            }
        }

        private GameObject CreatePresentSlot(PetPresentItem present, int index)
        {
            GameObject slotObj = new GameObject($"PresentSlot_{index}_{present.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotObj.transform.SetParent(presentGridContainer, false);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250f, 68f);

            Image bg = slotObj.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

            Button btn = slotObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (selectedEntity != null)
                {
                    string feedback;
                    bool success = TomodachiSocialLinkManager.Instance.GivePresentToFamiliar(selectedEntity.entityId, present, out feedback);
                    UpdateFamiliarSpeechBubble(selectedEntity, feedback);
                    SpawnHeartFX();
                    if (presentDrawerRoot != null) presentDrawerRoot.SetActive(false);
                    SelectEntity(selectedEntity);
                    RefreshRosterList();
                }
            });

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.04f, 0.12f);
            irt.anchorMax = new Vector2(0.24f, 0.88f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.GetComponent<Image>();
            Sprite s = LoadSprite(present.iconSpriteName);
            if (s != null) iconImg.sprite = s;
            iconImg.preserveAspect = true;

            // Details
            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(slotObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.28f, 0.05f);
            trt.anchorMax = new Vector2(0.96f, 0.95f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            string colHex = ColorUtility.ToHtmlStringRGB(present.themeColor);
            tmp.text = $"<color=#{colHex}><b>{present.name}</b></color> <size=80%>(+{present.affectionPoints} Affection)</size>\n<size=75%><color=#D0D5E5>{present.description}</color></size>";
            tmp.fontSize = 11;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return slotObj;
        }
        #endregion

        #region Satchel Gift Selection Drawer
        public void OpenGiftDrawer()
        {
            CloseAllDrawers();
            if (giftDrawerRoot != null) giftDrawerRoot.SetActive(true);
            RefreshGiftDrawer();
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.8f);
            }
        }

        private void RefreshGiftDrawer()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < activeGiftSlotObjects.Count; i++)
                {
                    if (activeGiftSlotObjects[i] != null) Destroy(activeGiftSlotObjects[i]);
                }
            }
            else
            {
                if (giftGridContainer != null)
                {
                    for (int i = giftGridContainer.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(giftGridContainer.GetChild(i).gameObject);
                    }
                }
            }
            activeGiftSlotObjects.Clear();

            if (giftGridContainer == null) return;

            // Load real items from InventoryMenuUI
            IReadOnlyList<InventoryItemEntry> invItems = (InventoryMenuUI.Instance != null)
                ? InventoryMenuUI.Instance.Items
                : new List<InventoryItemEntry>();

            for (int i = 0; i < invItems.Count; i++)
            {
                var item = invItems[i];
                GameObject slot = CreateGiftDrawerSlot(item, i);
                activeGiftSlotObjects.Add(slot);
            }
        }

        private GameObject CreateGiftDrawerSlot(InventoryItemEntry item, int index)
        {
            GameObject slotObj = new GameObject($"GiftSlot_{index}_{item.itemId}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotObj.transform.SetParent(giftGridContainer, false);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240f, 60f);

            Image bg = slotObj.GetComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);

            Button btn = slotObj.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (selectedEntity != null)
                {
                    TomodachiSocialLinkManager.Instance.GiveGift(selectedEntity.entityId, item.itemId);
                    if (giftDrawerRoot != null) giftDrawerRoot.SetActive(false);
                    SelectEntity(selectedEntity);
                    RefreshRosterList();
                }
            });

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.04f, 0.15f);
            irt.anchorMax = new Vector2(0.24f, 0.85f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            Image iconImg = iconObj.GetComponent<Image>();
            Sprite s = LoadSprite(item.iconTextureName);
            if (s != null) iconImg.sprite = s;
            iconImg.color = item.GetRarityColor();

            // Name & Count
            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(slotObj.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.28f, 0f);
            trt.anchorMax = new Vector2(0.96f, 1f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            string hex = ColorUtility.ToHtmlStringRGB(item.GetRarityColor());
            tmp.text = $"<color=#{hex}><b>{item.displayName}</b></color>\n<size=80%><color=#FFE070>x{item.quantity} available [GIFT]</color></size>";
            tmp.fontSize = 12;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return slotObj;
        }
        #endregion

        #region FX
        private void SpawnHeartFX()
        {
            if (heartsFxContainer == null) return;
            StartCoroutine(AnimateFloatingHeart());
        }

        private IEnumerator AnimateFloatingHeart()
        {
            GameObject heartObj = new GameObject("HeartFX", typeof(RectTransform), typeof(TextMeshProUGUI));
            heartObj.transform.SetParent(heartsFxContainer, false);
            RectTransform rt = heartObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-20f, 20f));

            TextMeshProUGUI tmp = heartObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "<color=#FF3366>♥</color>";
            tmp.fontSize = UnityEngine.Random.Range(28, 42);
            tmp.alignment = TextAlignmentOptions.Center;

            float elapsed = 0f;
            float duration = 1.0f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = startPos + new Vector2(Mathf.Sin(t * 8f) * 15f, t * 80f);
                tmp.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }

            Destroy(heartObj);
        }

        private IEnumerator AnimatePortraitBreathing()
        {
            if (characterPortraitImg == null) yield break;
            RectTransform rt = characterPortraitImg.GetComponent<RectTransform>();
            Vector3 baseScale = Vector3.one;

            while (true)
            {
                float breathe = 1.0f + 0.025f * Mathf.Sin(Time.time * 2.5f);
                if (rt != null) rt.localScale = new Vector3(baseScale.x * breathe, baseScale.y * (2f - breathe), 1f);
                yield return null;
            }
        }
        #endregion

        #region Callbacks
        private void HandleAffinityChanged(SocialEntityProfile profile, int newAffinity)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
        }

        private void HandleMoodChanged(SocialEntityProfile profile, TomodachiMood newMood)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
        }

        private void HandleRankUp(SocialEntityProfile profile, int newRank)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
            RefreshRosterList();
        }

        private void HandlePetFed(SocialEntityProfile profile, PetTreatItem treat)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
            RefreshRosterList();
        }

        private void HandlePetWatered(SocialEntityProfile profile)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
            RefreshRosterList();
        }

        private void HandlePetBathed(SocialEntityProfile profile)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
            RefreshRosterList();
        }

        private void HandlePetPresentGiven(SocialEntityProfile profile, PetPresentItem present)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
            RefreshRosterList();
        }

        private void HandlePetPetted(SocialEntityProfile profile)
        {
            if (selectedEntity != null && selectedEntity.entityId == profile.entityId)
            {
                SelectEntity(profile);
            }
            RefreshRosterList();
        }
        #endregion

        private Sprite LoadSprite(string textureName)
        {
#if UNITY_EDITOR
            string path = "Assets/Textures/UI/" + textureName;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            return null;
#endif
        }
    }
}
