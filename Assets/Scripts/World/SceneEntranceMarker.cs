using UnityEngine;

/// <summary>
/// SceneEntranceMarker defines a designated player spawn / entrance location in a scene.
/// Used by SceneTransitionManager to place the player at specific doorways when entering
/// or exiting buildings, towns, and dungeons.
/// </summary>
public class SceneEntranceMarker : MonoBehaviour
{
    [Tooltip("Unique ID matching the targetSpawnMarkerId on BuildingEntranceTrigger (e.g. 'Tavern_Entrance', 'Overworld_TavernDoor').")]
    public string markerId = "DefaultEntrance";

    [Tooltip("Target player facing direction (Euler Y angle) upon spawning.")]
    public float facingYaw = 0f;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
        Vector3 forward = Quaternion.Euler(0f, facingYaw != 0f ? facingYaw : transform.eulerAngles.y, 0f) * Vector3.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, forward * 1.5f);
    }
}
