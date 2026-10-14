using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSummoner.DungeonDimension
{
    /// <summary>
    /// DungeonBoonRewardUI presents the 3-choice roguelike boon selection screen
    /// upon clearing each room / area in the Dungeon Dimension:
    /// - Holographic card layout (Common, Rare, Epic, Legendary).
    /// - Displays temporary stat growth, icon, and lore.
    /// - Equips chosen boon into DungeonRunBuffSystem for the remainder of this run.
    /// </summary>
    public class DungeonBoonRewardUI : MonoBehaviour
    {
        private static DungeonBoonRewardUI _instance;
        public static DungeonBoonRewardUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DungeonBoonRewardUI>(FindObjectsInactive.Include);
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Modal Overlay")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI headerTitleTMP;
        [SerializeField] private TextMeshProUGUI headerSubtitleTMP;

        [Header("3 Boon Cards")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private List<GameObject> boonCardSlots = new List<GameObject>();

        private Action onBoonSelectedCallback;
        private List<DungeonBoon> currentChoices = new List<DungeonBoon>();

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (modalRoot != null) modalRoot.SetActive(false);
        }

        public void OpenRewardScreen(int clearedAreaIndex, Action onComplete)
        {
            onBoonSelectedCallback = onComplete;
            if (modalRoot != null) modalRoot.SetActive(true);

            if (headerTitleTMP != null)
            {
                headerTitleTMP.text = $"<color=#FFD700><b>AREA {clearedAreaIndex} CLEARED!</b></color>";
            }

            if (headerSubtitleTMP != null)
            {
                headerSubtitleTMP.text = "<color=#40EBFF>SELECT A TEMPORARY DUNGEON BOON</color> <size=80%>(Persists for this run)</size>";
            }

            // Roll 3 boons
            currentChoices = DungeonRunBuffSystem.Instance.RollBoonChoices(3);
            PopulateCards(currentChoices);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("boon_card_reveal", 0.9f);
            }
        }

        private void PopulateCards(List<DungeonBoon> boons)
        {
            // If card slots exist, populate them
            for (int i = 0; i < boonCardSlots.Count; i++)
            {
                if (i < boons.Count)
                {
                    boonCardSlots[i].SetActive(true);
                    BindCardSlot(boonCardSlots[i], boons[i]);
                }
                else
                {
                    boonCardSlots[i].SetActive(false);
                }
            }
        }

        private void BindCardSlot(GameObject slotObj, DungeonBoon boon)
        {
            var titleTMP = slotObj.transform.Find("TitleTMP")?.GetComponent<TextMeshProUGUI>();
            var rarityTMP = slotObj.transform.Find("RarityTMP")?.GetComponent<TextMeshProUGUI>();
            var descTMP = slotObj.transform.Find("DescTMP")?.GetComponent<TextMeshProUGUI>();
            var iconImg = slotObj.transform.Find("IconFrame/IconImg")?.GetComponent<Image>();
            var borderImg = slotObj.transform.Find("BorderImg")?.GetComponent<Image>();
            var selectBtn = slotObj.transform.Find("SelectBtn")?.GetComponent<Button>();

            Color rColor = boon.GetRarityColor();

            if (titleTMP != null)
            {
                titleTMP.text = $"<b>{boon.name.ToUpper()}</b>";
                titleTMP.color = new Color(0.18f, 0.12f, 0.08f, 1f);
            }

            if (rarityTMP != null)
            {
                rarityTMP.text = boon.GetRarityTag();
            }

            if (descTMP != null)
            {
                descTMP.text = boon.description;
                descTMP.color = new Color(0.24f, 0.18f, 0.12f, 1f);
            }

            if (borderImg != null)
            {
                borderImg.color = Color.clear;
            }

            if (iconImg != null && !string.IsNullOrEmpty(boon.iconSpriteName))
            {
                Sprite sp = LoadSprite(boon.iconSpriteName);
                if (sp != null)
                {
                    iconImg.sprite = sp;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = rColor;
                }
            }

            if (selectBtn != null)
            {
                selectBtn.onClick.RemoveAllListeners();
                selectBtn.onClick.AddListener(() => OnCardSelected(boon));
            }
        }

        private void OnCardSelected(DungeonBoon boon)
        {
            Debug.Log($"✨ [BOON REWARD] Summoner selected {boon.name}!");
            DungeonRunBuffSystem.Instance.ApplyBoon(boon);

            if (modalRoot != null) modalRoot.SetActive(false);

            onBoonSelectedCallback?.Invoke();
            onBoonSelectedCallback = null;
        }

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
