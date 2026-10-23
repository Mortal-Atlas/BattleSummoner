using UnityEngine;

/// <summary>
/// BuildingEntranceTrigger is placed at building doorways, home entrances, and dungeon gates.
/// When the player walks into the trigger, it triggers a screen fade-to-black and loads the
/// interior (or overworld) scene via SceneTransitionManager.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BuildingEntranceTrigger : MonoBehaviour
{
    [Header("Destination Scene")]
    [Tooltip("Target scene name to load (e.g. 'BuildingInterior', 'ShopInterior', 'OverworldMap').")]
    public string targetSceneName = "BuildingInterior";

    [Tooltip("Target marker ID where player spawns in destination scene (e.g. 'Interior_Doorway', 'Town_TavernDoor').")]
    public string targetSpawnMarkerId = "Interior_Doorway";

    [Tooltip("Display name shown on transition banner and toast (e.g. 'The Wandering Wyvern Tavern').")]
    public string locationTitle = "Building Interior";

    [Header("Trigger Behavior")]
    [Tooltip("If true, walking into doorway automatically triggers the fade-to-black scene transition.")]
    public bool triggerOnTouch = true;

    [Tooltip("Keyboard shortcut for manual entrance prompt.")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject entrancePromptUI;
    private bool playerInside = false;

    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = true;

        if (entrancePromptUI != null) entrancePromptUI.SetActive(true);

        if (triggerOnTouch)
        {
            TriggerEntranceTransition();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = false;
        if (entrancePromptUI != null) entrancePromptUI.SetActive(false);
    }

    private void Update()
    {
        if (playerInside && !triggerOnTouch && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.F)))
        {
            TriggerEntranceTransition();
        }
    }

    public void TriggerEntranceTransition()
    {
        if (SceneTransitionManager.Instance.IsTransitioning) return;

        Debug.Log($"🚪 [DOORWAY TRIGGER] Entering '{locationTitle}' -> Loading scene '{targetSceneName}' (Marker: '{targetSpawnMarkerId}')");
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX("door_creak", 0.85f);
        }
        SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnMarkerId, locationTitle);
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
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.4f);
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
