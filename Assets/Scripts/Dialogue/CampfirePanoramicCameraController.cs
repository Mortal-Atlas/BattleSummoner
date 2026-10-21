using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace BattleSummoner.Dialogue
{
    /// <summary>
    /// CampfirePanoramicCameraController provides smooth horizontal camera panning
    /// around the campfire circle:
    /// - Mouse drag, scroll wheel, A/D, and Left/Right arrow keys.
    /// - Raycasting to highlight and select companions sitting around the hearth.
    /// - Interactive reticle with floating tooltip.
    /// - On-screen companion talk chips for accessibility.
    /// </summary>
    public class CampfirePanoramicCameraController : MonoBehaviour
    {
        public static CampfirePanoramicCameraController Instance { get; private set; }

        [Header("Camera & Rig")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform cameraPivot;

        [Header("Panning Configuration")]
        [Tooltip("Horizontal look sensitivity")]
        [Range(0.1f, 3.0f)] public float panSensitivity = 0.45f;
        [Range(10f, 120f)] public float minYaw = -75f;
        [Range(10f, 120f)] public float maxYaw = 75f;
        [Range(0.01f, 0.4f)] public float smoothDampTime = 0.08f;

        [Header("State")]
        [SerializeField] private float currentYaw = 0f;
        [SerializeField] private float targetYaw = 0f;
        private float yawVelocity = 0f;
        [SerializeField] private bool isPanningLocked = false;

        [Header("Interactive Reticle UI")]
        [SerializeField] private Canvas interactCanvas;
        [SerializeField] private RectTransform reticleRect;
        [SerializeField] private Image reticleImage;
        [SerializeField] private TextMeshProUGUI promptTooltipTMP;

        [Header("Hovered Target")]
        private string hoveredNpcId = null;
        private string hoveredDisplayName = null;
        private Vector3 hoveredWorldPosition = Vector3.zero;

        private Vector3 initialCamEuler;
        private bool isDraggingMouse = false;
        private Vector3 lastMousePos;

        public bool IsPanningLocked
        {
            get => isPanningLocked;
            set => isPanningLocked = value;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;
            initialCamEuler = transform.eulerAngles;
            currentYaw = 0f;
            targetYaw = 0f;
        }

        private void Start()
        {
            EnsureReticleUI();
        }

        private void Update()
        {
            if (!isPanningLocked)
            {
                HandleInput();
                UpdateCameraRotation();
                CheckHoveredActor();
            }
            else
            {
                if (reticleRect != null) reticleRect.gameObject.SetActive(false);
            }
        }

        private void HandleInput()
        {
            // 1. Mouse Drag (Left or Right click drag across campfire)
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
                {
                    isDraggingMouse = true;
                    lastMousePos = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            {
                isDraggingMouse = false;
            }

            if (isDraggingMouse)
            {
                Vector3 delta = Input.mousePosition - lastMousePos;
                targetYaw += delta.x * panSensitivity;
                lastMousePos = Input.mousePosition;
            }

            // 2. Mouse Scroll Wheel (horizontal sweep)
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetYaw += scroll * 15f * panSensitivity;
            }

            // 3. Keyboard Arrow & A/D Panning
            float keyHorizontal = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyHorizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyHorizontal += 1f;

            if (Mathf.Abs(keyHorizontal) > 0.01f)
            {
                targetYaw += keyHorizontal * 60f * Time.deltaTime;
            }

            // Clamp yaw to campfire viewing cone
            targetYaw = Mathf.Clamp(targetYaw, minYaw, maxYaw);
        }

        private void UpdateCameraRotation()
        {
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothDampTime);
            transform.rotation = Quaternion.Euler(initialCamEuler.x, initialCamEuler.y + currentYaw, initialCamEuler.z);
        }

        private void CheckHoveredActor()
        {
            if (targetCamera == null) return;

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            hoveredNpcId = null;
            hoveredDisplayName = null;

            // Don't raycast if pointer is over UI buttons
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (reticleRect != null) reticleRect.gameObject.SetActive(false);
                return;
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                // Check if hit an NPC root or child
                Transform curr = hit.transform;
                while (curr != null)
                {
                    string n = curr.gameObject.name.ToLower();
                    if (n.Contains("female") || n.Contains("aria"))
                    {
                        hoveredNpcId = "starter_female";
                        hoveredDisplayName = "Aria (Female Starter)";
                        hoveredWorldPosition = curr.position + Vector3.up * 1.5f;
                        break;
                    }
                    else if (n.Contains("male") || n.Contains("leo"))
                    {
                        hoveredNpcId = "starter_male";
                        hoveredDisplayName = "Leo (Male Starter)";
                        hoveredWorldPosition = curr.position + Vector3.up * 1.6f;
                        break;
                    }
                    else if (n.Contains("margo"))
                    {
                        hoveredNpcId = "npc_margo";
                        hoveredDisplayName = "Margo";
                        hoveredWorldPosition = curr.position + Vector3.up * 1.5f;
                        break;
                    }
                    else if (n.Contains("barnaby"))
                    {
                        hoveredNpcId = "npc_barnaby";
                        hoveredDisplayName = "Barnaby";
                        hoveredWorldPosition = curr.position + Vector3.up * 1.6f;
                        break;
                    }
                    else if (n.Contains("zephyr"))
                    {
                        hoveredNpcId = "npc_zephyr";
                        hoveredDisplayName = "Zephyr";
                        hoveredWorldPosition = curr.position + Vector3.up * 1.6f;
                        break;
                    }
                    curr = curr.parent;
                }
            }

            // Update Reticle UI
            if (!string.IsNullOrEmpty(hoveredNpcId))
            {
                if (reticleRect != null)
                {
                    reticleRect.gameObject.SetActive(true);
                    Vector3 screenPos = targetCamera.WorldToScreenPoint(hoveredWorldPosition);
                    reticleRect.position = screenPos;

                    // Subtle breathing pulse
                    float pulse = 1.0f + Mathf.Sin(Time.time * 6f) * 0.08f;
                    reticleRect.localScale = new Vector3(pulse, pulse, 1f);
                }

                if (promptTooltipTMP != null)
                {
                    promptTooltipTMP.text = $"<color=#FFD54F><b>TALK TO {hoveredDisplayName.ToUpper()}</b></color>\n<size=12><color=#FFFFFF>[Click to Speak Directly]</color></size>";
                }

                // If user clicks on character, initiate direct dialogue!
                if (Input.GetMouseButtonDown(0) && !isDraggingMouse)
                {
                    InitiateDirectConversation(hoveredNpcId, hoveredDisplayName);
                }
            }
            else
            {
                if (reticleRect != null) reticleRect.gameObject.SetActive(false);
            }
        }

        public void PanToAngle(float angleDegrees)
        {
            targetYaw = Mathf.Clamp(angleDegrees, minYaw, maxYaw);
        }

        public void InitiateDirectConversation(string npcId, string displayName)
        {
            Debug.Log($"[Campfire] Talking directly with {displayName} ({npcId})...");
            isPanningLocked = true;
            if (reticleRect != null) reticleRect.gameObject.SetActive(false);

            if (CampfireDualDialogueController.Instance != null)
            {
                CampfireDualDialogueController.Instance.StartDialogueWithCharacter(npcId, displayName);
            }
        }

        public void ResumeFreeLook()
        {
            isPanningLocked = false;
        }

        private void EnsureReticleUI()
        {
            if (interactCanvas == null)
            {
                var canvasObj = new GameObject("CampfireInteractReticleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                interactCanvas = canvasObj.GetComponent<Canvas>();
                interactCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                interactCanvas.sortingOrder = 50;

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (reticleRect == null)
            {
                GameObject reticleObj = new GameObject("ReticleContainer", typeof(RectTransform));
                reticleObj.transform.SetParent(interactCanvas.transform, false);
                reticleRect = reticleObj.GetComponent<RectTransform>();
                reticleRect.sizeDelta = new Vector2(100f, 100f);

                // Reticle icon
                GameObject iconObj = new GameObject("ReticleIcon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(reticleRect, false);
                var iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(72f, 72f);
                reticleImage = iconObj.GetComponent<Image>();
                reticleImage.raycastTarget = false;

                // Load reticle sprite
#if UNITY_EDITOR
                var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/CampfireDialogue/Campfire_Reticle_Interact.png");
                if (tex != null)
                {
                    reticleImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
#endif

                // Tooltip text
                GameObject textObj = new GameObject("PromptTooltip_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(reticleRect, false);
                var textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0f);
                textRect.anchorMax = new Vector2(0.5f, 0f);
                textRect.anchoredPosition = new Vector2(0f, -32f);
                textRect.sizeDelta = new Vector2(260f, 60f);

                promptTooltipTMP = textObj.GetComponent<TextMeshProUGUI>();
                promptTooltipTMP.fontSize = 15f;
                promptTooltipTMP.alignment = TextAlignmentOptions.Top;
                promptTooltipTMP.textWrappingMode = TextWrappingModes.NoWrap;
                promptTooltipTMP.color = Color.white;
                promptTooltipTMP.raycastTarget = false;

                reticleRect.gameObject.SetActive(false);
            }
        }
    }
}
