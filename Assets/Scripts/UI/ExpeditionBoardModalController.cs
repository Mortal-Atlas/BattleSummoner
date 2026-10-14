using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExpeditionBoardModalController : MonoBehaviour
{
    [Header("Bespoke NPC Profiles")]
    public List<BespokeNPCObject> rosterProfiles = new List<BespokeNPCObject>();

    [Header("UI References")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Text statusInfoText;

    [Header("NPC Action Rows")]
    [SerializeField] private Button assignRaidBtn1;
    [SerializeField] private Button dispatchExpBtn1;
    [SerializeField] private Text npcStatusText1;

    [SerializeField] private Button assignRaidBtn2;
    [SerializeField] private Button dispatchExpBtn2;
    [SerializeField] private Text npcStatusText2;

    [SerializeField] private Button assignRaidBtn3;
    [SerializeField] private Button dispatchExpBtn3;
    [SerializeField] private Text npcStatusText3;

    [Header("Dialogue Box Link")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueSpeakerText;
    [SerializeField] private Text dialogueBodyText;
    [SerializeField] private Button closeDialogueBtn;

    private NPCConversationAgent activeAgent;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseModal);
        }

        if (closeDialogueBtn != null)
        {
            closeDialogueBtn.onClick.AddListener(() => {
                if (dialoguePanel != null) dialoguePanel.SetActive(false);
            });
        }

        BindButtonEvents();
    }

    private void OnEnable()
    {
        RefreshUI();
        NPCExpeditionManager.OnExpeditionReturned += HandleExpeditionReturned;
        NPCExpeditionManager.OnRaidCompanionChanged += HandleRaidCompanionChanged;
    }

    private void OnDisable()
    {
        NPCExpeditionManager.OnExpeditionReturned -= HandleExpeditionReturned;
        NPCExpeditionManager.OnRaidCompanionChanged -= HandleRaidCompanionChanged;
    }

    private void Update()
    {
        // Periodic refresh for active expedition timers
        if (NPCExpeditionManager.Instance != null && NPCExpeditionManager.Instance.currentExpeditions.Count > 0)
        {
            RefreshUI();
        }
    }

    private void BindButtonEvents()
    {
        if (assignRaidBtn1 != null) assignRaidBtn1.onClick.AddListener(() => ToggleRaidCompanion(0));
        if (dispatchExpBtn1 != null) dispatchExpBtn1.onClick.AddListener(() => DispatchNPC(0));

        if (assignRaidBtn2 != null) assignRaidBtn2.onClick.AddListener(() => ToggleRaidCompanion(1));
        if (dispatchExpBtn2 != null) dispatchExpBtn2.onClick.AddListener(() => DispatchNPC(1));

        if (assignRaidBtn3 != null) assignRaidBtn3.onClick.AddListener(() => ToggleRaidCompanion(2));
        if (dispatchExpBtn3 != null) dispatchExpBtn3.onClick.AddListener(() => DispatchNPC(2));
    }

    public void CloseModal()
    {
        gameObject.SetActive(false);
    }

    private void ToggleRaidCompanion(int index)
    {
        if (index < 0 || index >= rosterProfiles.Count || NPCExpeditionManager.Instance == null) return;
        BespokeNPCObject npc = rosterProfiles[index];

        if (NPCExpeditionManager.Instance.activeRaidCompanion == npc)
        {
            NPCExpeditionManager.Instance.DismissRaidCompanion();
            SetStatusMessage($"Dismissed {npc.displayName} from active raid duty.");
        }
        else
        {
            if (NPCExpeditionManager.Instance.AssignActiveRaidCompanion(npc))
            {
                SetStatusMessage($"{npc.displayName} deployed as sole real-time raid companion!");
            }
            else
            {
                SetStatusMessage($"Cannot assign {npc.displayName} (already on expedition).");
            }
        }
        RefreshUI();
    }

    private void DispatchNPC(int index)
    {
        if (index < 0 || index >= rosterProfiles.Count || NPCExpeditionManager.Instance == null) return;
        BespokeNPCObject npc = rosterProfiles[index];

        string[] routes = { "Subterranean Azurite Cavern", "Volatile Ruby Chasm", "Void Amethyst Geode Basin" };
        string route = routes[index % routes.Length];

        if (NPCExpeditionManager.Instance.DispatchOnExpedition(npc, route, 15f, (index + 1) * 20))
        {
            SetStatusMessage($"{npc.displayName} dispatched to {route} for 15s foraging.");
        }
        else
        {
            SetStatusMessage($"Failed to dispatch {npc.displayName}. Check capacity (Max 3) or busy status.");
        }
        RefreshUI();
    }

    public void TalkToNPC(int index)
    {
        if (index < 0 || index >= rosterProfiles.Count) return;
        BespokeNPCObject npc = rosterProfiles[index];

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (dialogueSpeakerText != null) dialogueSpeakerText.text = $"{npc.displayName} - {npc.title}";
            if (dialogueBodyText != null) dialogueBodyText.text = "Consulting the Spire's arcane memory...";
        }

        if (activeAgent == null)
        {
            activeAgent = gameObject.AddComponent<NPCConversationAgent>();
        }

        activeAgent.npcProfile = npc;
        activeAgent.InitializeMemory();

        activeAgent.OnDialogueReceived -= HandleDialogueReceived;
        activeAgent.OnDialogueReceived += HandleDialogueReceived;

        activeAgent.SendUserUtterance("Greetings. What insights do you have about the subterranean abyss?");
    }

    private void HandleDialogueReceived(string response)
    {
        if (dialogueBodyText != null)
        {
            dialogueBodyText.text = response;
        }
    }

    public void RefreshUI()
    {
        if (NPCExpeditionManager.Instance == null) return;

        UpdateRowUI(0, npcStatusText1, assignRaidBtn1, dispatchExpBtn1);
        UpdateRowUI(1, npcStatusText2, assignRaidBtn2, dispatchExpBtn2);
        UpdateRowUI(2, npcStatusText3, assignRaidBtn3, dispatchExpBtn3);
    }

    private void UpdateRowUI(int index, Text statusText, Button raidBtn, Button expBtn)
    {
        if (index >= rosterProfiles.Count || rosterProfiles[index] == null) return;
        BespokeNPCObject npc = rosterProfiles[index];

        bool isRaidActive = (NPCExpeditionManager.Instance.activeRaidCompanion == npc);
        bool isExpeditionActive = false;
        float remainingTime = 0f;

        foreach (var exp in NPCExpeditionManager.Instance.currentExpeditions)
        {
            if (exp.dispatchedNPC == npc)
            {
                isExpeditionActive = true;
                remainingTime = Mathf.Max(0f, exp.durationSeconds - exp.elapsedSeconds);
                break;
            }
        }

        if (statusText != null)
        {
            if (isRaidActive)
            {
                statusText.text = $"<b>{npc.displayName}</b> ({npc.quirk})\n<color=#44FF44>ACTIVE RAID PARTNER</color>";
            }
            else if (isExpeditionActive)
            {
                statusText.text = $"<b>{npc.displayName}</b> ({npc.quirk})\n<color=#FFAA00>ON EXPEDITION: {remainingTime:F1}s left</color>";
            }
            else
            {
                statusText.text = $"<b>{npc.displayName}</b> ({npc.quirk})\n<color=#CCCCCC>CAMP IDLE (Affinity: {npc.affinityLevel})</color>";
            }
        }

        if (raidBtn != null)
        {
            Text btnLabel = raidBtn.GetComponentInChildren<Text>();
            if (btnLabel != null)
            {
                btnLabel.text = isRaidActive ? "DISMISS RAID" : "ASSIGN RAID (1 Max)";
            }
            raidBtn.interactable = !isExpeditionActive;
        }

        if (expBtn != null)
        {
            expBtn.interactable = !isRaidActive && !isExpeditionActive && (NPCExpeditionManager.Instance.currentExpeditions.Count < NPCExpeditionManager.MAX_DISPATCHED_EXPEDITIONS);
        }
    }

    private void SetStatusMessage(string msg)
    {
        if (statusInfoText != null)
        {
            statusInfoText.text = msg;
        }
    }

    private void HandleExpeditionReturned(string npcName, int manaCores, MineralRarity gem)
    {
        SetStatusMessage($"[FORAGING YIELD] {npcName} returned! +{manaCores} Mana Cores & 2x {gem}!");
        RefreshUI();
    }

    private void HandleRaidCompanionChanged(BespokeNPCObject companion)
    {
        RefreshUI();
    }
}
