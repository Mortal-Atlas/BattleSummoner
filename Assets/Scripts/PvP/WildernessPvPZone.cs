using UnityEngine;

/// <summary>
/// WildernessPvPZone governs unpartitioned, full-loot wilderness combat zones in the Spire.
/// </summary>
public class WildernessPvPZone : MonoBehaviour
{
    [SerializeField] private string zoneName = "Outer Ring Abyss";
    [SerializeField] private bool fullLootDrop = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.LogWarning($"[PvP Warning] Entered {zoneName}. Full player-versus-player damage active. Full loot drop: {fullLootDrop}");
        }
    }
}
