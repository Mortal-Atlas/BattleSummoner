using System.Collections;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public class DungeonStaircaseEntity : MonoBehaviour
    {
        [Header("Staircase Elements")]
        [SerializeField] private Light stairPortalLight;
        [SerializeField] private float interactRadius = 4.2f;

        private bool isUnlocked = false;
        private bool isPlayerInRange = false;
        private Transform playerTransform;
        private GameObject promptCanvasObj;
        private TMPro.TextMeshProUGUI promptTMP;

        public bool IsUnlocked => isUnlocked;

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            UpdateVisuals();
            UpdatePromptText();
        }

        private void Start()
        {
            SetupPromptUI();
            FindPlayer();
            UpdateVisuals();
        }

        private void SetupPromptUI()
        {
            GameObject canvasObj = new GameObject("StaircasePromptCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 90f);
            canvasObj.transform.localScale = Vector3.one * 0.009f;

            GameObject textObj = new GameObject("Text_Prompt");
            textObj.transform.SetParent(canvasObj.transform, false);
            promptTMP = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            promptTMP.alignment = TMPro.TextAlignmentOptions.Center;
            promptTMP.fontSize = 22f;
            promptTMP.fontStyle = TMPro.FontStyles.Bold;

            UpdatePromptText();
            canvasObj.SetActive(false);
            promptCanvasObj = canvasObj;
        }

        private void UpdatePromptText()
        {
            if (promptTMP == null) return;

            if (isUnlocked)
            {
                promptTMP.text = "<color=#FFEE44><b>[E]</b></color> // <color=#40EBFF>DESCEND TO NEXT FLOOR</color>\n<color=#FFEE44><b>[F]</b></color> // <color=#FFAA33>RETURN TO SURFACE (BANK LOOT)</color>";
            }
            else
            {
                promptTMP.text = "<color=#FF4444><b>[SEALED GATEWAY]</b></color>\n<size=75%><color=#D0A0FF>Defeat the Floor Guardian to Unlock Descent</color></size>";
            }
        }

        private void UpdateVisuals()
        {
            if (stairPortalLight != null)
            {
                stairPortalLight.color = isUnlocked ? new Color(0.2f, 0.9f, 1.0f) : new Color(0.9f, 0.2f, 0.3f);
                stairPortalLight.intensity = isUnlocked ? 3.5f : 1.5f;
            }
        }

        private void Update()
        {
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
                if (Camera.main != null && promptCanvasObj != null)
                {
                    promptCanvasObj.transform.rotation = Quaternion.LookRotation(promptCanvasObj.transform.position - Camera.main.transform.position);
                }

                if (isUnlocked)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        DescendToNextFloor();
                    }
                    else if (Input.GetKeyDown(KeyCode.F))
                    {
                        EvacuateToSurface();
                    }
                }
            }
        }

        private void DescendToNextFloor()
        {
            if (promptCanvasObj != null) promptCanvasObj.SetActive(false);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("portal_warp", 1.0f);
            }

            var floorMaster = DungeonCrawlerFloorMaster.Instance;
            if (floorMaster != null)
            {
                floorMaster.AdvanceToNextFloor();
            }
            else
            {
                // Fallback to generator directly
                var gen = ProceduralDungeonGenerator.Instance;
                if (gen != null)
                {
                    gen.GenerateUniqueProceduralDungeon();
                }
            }
        }

        private void EvacuateToSurface()
        {
            if (promptCanvasObj != null) promptCanvasObj.SetActive(false);

            var floorMaster = DungeonCrawlerFloorMaster.Instance;
            if (floorMaster != null)
            {
                floorMaster.EvacuateDungeon();
            }
            else
            {
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.TransitionToScene("OverworldMap", "Overworld_TavernDoor", "Solas Town Overworld");
                }
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
