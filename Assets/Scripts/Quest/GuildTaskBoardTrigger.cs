using UnityEngine;
using TMPro;

namespace BattleSummoner.Quests
{
    /// <summary>
    /// GuildTaskBoardTrigger is placed on the physical 3D Task Board in the Adventurer's Guild Hall.
    /// When the player approaches the board, it displays an interactive prompt.
    /// Pressing [E] or tapping the board opens the Guild Task Board UI.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class GuildTaskBoardTrigger : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float triggerDistance = 3.5f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("3D Floating Prompt")]
        [SerializeField] private GameObject promptCanvasObj;
        [SerializeField] private TextMeshProUGUI promptTMP;

        private bool playerInRange = false;

        private void Awake()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
                col.size = Vector3.one * triggerDistance;
            }

            EnsurePromptCanvas();
        }

        private void EnsurePromptCanvas()
        {
            if (promptCanvasObj != null) return;

            promptCanvasObj = new GameObject("TaskBoardPromptCanvas", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI));
            promptCanvasObj.transform.SetParent(transform, false);
            promptCanvasObj.transform.localPosition = new Vector3(0f, 2.8f, 0f);

            Canvas c = promptCanvasObj.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;

            RectTransform rt = promptCanvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320f, 70f);
            rt.localScale = Vector3.one * 0.008f;
            promptCanvasObj.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);

            promptTMP = promptCanvasObj.GetComponent<TextMeshProUGUI>();
            promptTMP.text = "<color=#FFD700><b>[GUILD TASK BOARD]</b></color>\n<size=80%><color=#FFFFFF>Tap or Press [E] to View Jobs</color></size>";
            promptTMP.alignment = TextAlignmentOptions.Center;
            promptTMP.fontSize = 20;
            promptTMP.textWrappingMode = TextWrappingModes.NoWrap;

            promptCanvasObj.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;

            playerInRange = true;
            if (promptCanvasObj != null) promptCanvasObj.SetActive(true);

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast("<color=#FFD700><b>[GUILD TASK BOARD]</b></color>\nPress [E] or tap to inspect job notices.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;

            playerInRange = false;
            if (promptCanvasObj != null) promptCanvasObj.SetActive(false);
        }

        private void Update()
        {
            if (!playerInRange) return;

            // Desktop shortcut or mouse click
            if (Input.GetKeyDown(interactKey))
            {
                InteractWithBoard();
            }

            // Billboard prompt towards camera
            if (promptCanvasObj != null && promptCanvasObj.activeSelf && Camera.main != null)
            {
                promptCanvasObj.transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, 0f);
            }
        }

        private void OnMouseDown()
        {
            if (playerInRange)
            {
                InteractWithBoard();
            }
        }

        public void InteractWithBoard()
        {
            if (GuildTaskBoardUI.Instance != null)
            {
                GuildTaskBoardUI.Instance.ToggleTaskBoard();
            }
            else
            {
                Debug.LogWarning("⚠️ [TASK BOARD] GuildTaskBoardUI instance not found in scene!");
            }
        }

        private bool IsPlayer(Collider other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;
            if (other.GetComponent<PlayerMovementController>() != null) return true;
            if (other.name.Contains("Player")) return true;
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.75f, 0.15f, 0.35f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
    }
}
