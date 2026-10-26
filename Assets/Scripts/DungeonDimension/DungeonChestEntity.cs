using System.Collections;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.DungeonDimension
{
    public enum ChestTier { CommonWood, RareGilded, ShadowMimic }

    public class DungeonChestEntity : MonoBehaviour
    {
        [Header("Chest Configuration")]
        [SerializeField] private ChestTier tier = ChestTier.CommonWood;
        [SerializeField] private Transform lidTransform;
        [SerializeField] private Light chestGlowLight;
        [SerializeField] private float interactRadius = 3.2f;

        [Header("State")]
        private bool isOpened = false;
        private bool isPlayerInRange = false;
        private Transform playerTransform;
        private GameObject promptCanvasObj;
        private TMPro.TextMeshProUGUI promptTMP;

        public ChestTier Tier => tier;
        public bool IsOpened => isOpened;

        public void Initialize(ChestTier chestTier)
        {
            tier = chestTier;
            AutoDetectLid();
            SetupPromptUI();

            if (chestGlowLight != null)
            {
                chestGlowLight.color = tier == ChestTier.RareGilded ? new Color(1f, 0.85f, 0.2f) :
                                       tier == ChestTier.ShadowMimic ? new Color(0.9f, 0.1f, 0.4f) :
                                       new Color(0.9f, 0.6f, 0.3f);
                chestGlowLight.intensity = 1.2f;
            }
        }

        private void Start()
        {
            if (lidTransform == null) AutoDetectLid();
            if (promptCanvasObj == null) SetupPromptUI();
            FindPlayer();
        }

        private void AutoDetectLid()
        {
            // Find child that has "Lid", "Door", "Top", or second child
            foreach (Transform child in transform)
            {
                string lower = child.name.ToLower();
                if (lower.Contains("lid") || lower.Contains("door") || lower.Contains("top"))
                {
                    lidTransform = child;
                    break;
                }
            }
            if (lidTransform == null && transform.childCount > 0)
            {
                lidTransform = transform.GetChild(0);
            }
        }

        private void SetupPromptUI()
        {
            var existing = transform.Find("ChestPromptCanvas");
            if (existing != null)
            {
                promptCanvasObj = existing.gameObject;
                promptTMP = promptCanvasObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                promptCanvasObj.SetActive(false);
                return;
            }

            GameObject canvasObj = new GameObject("ChestPromptCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 60f);
            canvasObj.transform.localScale = Vector3.one * 0.009f;

            GameObject textObj = new GameObject("Text_Prompt");
            textObj.transform.SetParent(canvasObj.transform, false);
            promptTMP = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            promptTMP.alignment = TMPro.TextAlignmentOptions.Center;
            promptTMP.fontSize = 24f;
            promptTMP.fontStyle = TMPro.FontStyles.Bold;

            string tierLabel = tier == ChestTier.RareGilded ? "<color=#FFD700>Gilded Relic Chest</color>" :
                               tier == ChestTier.ShadowMimic ? "<color=#FF4488>Cursed Shadow Cache</color>" :
                               "<color=#FFAA44>Treasure Chest</color>";

            promptTMP.text = $"<color=#FFEE44><b>[E]</b></color> // OPEN {tierLabel}";
            canvasObj.SetActive(false);
            promptCanvasObj = canvasObj;
        }

        private void Update()
        {
            if (isOpened) return;

            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= interactRadius;

            if (inRange != isPlayerInRange)
            {
                isPlayerInRange = inRange;
                if (promptCanvasObj != null) promptCanvasObj.SetActive(isPlayerInRange);
            }

            if (isPlayerInRange)
            {
                // Face prompt toward camera
                if (Camera.main != null && promptCanvasObj != null)
                {
                    promptCanvasObj.transform.rotation = Quaternion.LookRotation(promptCanvasObj.transform.position - Camera.main.transform.position);
                }

                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
                {
                    OpenChest();
                }
            }
        }

        public void OpenChest()
        {
            if (isOpened) return;
            isOpened = true;

            if (promptCanvasObj != null) promptCanvasObj.SetActive(false);

            StartCoroutine(AnimateOpenChestRoutine());
        }

        private IEnumerator AnimateOpenChestRoutine()
        {
            // Sound
            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("relic_chime", 1.0f);
            }

            // Smooth lid swinging open
            if (lidTransform != null)
            {
                Quaternion startRot = lidTransform.localRotation;
                Quaternion targetRot = startRot * Quaternion.Euler(-95f, 0f, 0f);
                float elapsed = 0f;
                while (elapsed < 0.45f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.45f);
                    lidTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
            }

            // Burst light
            if (chestGlowLight != null)
            {
                chestGlowLight.intensity = 3.5f;
                chestGlowLight.range = 7f;
            }

            // Distribute Rewards
            DistributeLoot();
        }

        private void DistributeLoot()
        {
            int goldReward = tier == ChestTier.RareGilded ? Random.Range(350, 750) : Random.Range(65, 180);
            double expReward = tier == ChestTier.RareGilded ? 240.0 : 85.0;

            if (InventoryMenuUI.Instance != null)
            {
                InventoryMenuUI.Instance.AddGold(goldReward);
            }

            var playerProg = FindAnyObjectByType<PlayerProgression>();
            if (playerProg != null)
            {
                playerProg.AddExperience(expReward, 1);
            }

            string lootToast = tier == ChestTier.RareGilded 
                ? $"<color=#FFD700><b>[GILDED TREASURE]</b></color>\nAcquired +{goldReward} Gold & Ancient Arcana Relic!"
                : $"<color=#FFAA33><b>[TREASURE CHEST]</b></color>\nAcquired +{goldReward} Gold & Void Shards!";

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(lootToast);
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.SpawnEnemyDamage(transform.position + Vector3.up * 1.2f, goldReward, tier == ChestTier.RareGilded);
            }
        }

        private void FindPlayer()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else
            {
                var prog = FindAnyObjectByType<PlayerProgression>();
                if (prog != null) playerTransform = prog.transform;
            }
        }
    }
}
