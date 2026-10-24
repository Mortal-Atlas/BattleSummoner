using System;
using UnityEngine;

namespace BattleSummoner.DesignPhilosophy
{
    public enum DreamerPresenceStatus
    {
        LucidAwake,          // Live player-controlled or active networked squadmate
        SubconsciousAsleep,   // Remnant AI pawn autopilot running locally during disconnect / offline rest
        Disconnected         // Completely despawned / dropped
    }

    /// <summary>
    /// DreamerAwakeStateController manages the presence and AI autonomy of squadmates.
    /// When Wi-Fi/connection drops mid-dungeon or in Airplane Mode, remote squadmates
    /// seamlessly convert into Subconscious Remnant AI Pawns so the solo player can
    /// finish boss fights and expeditions without interruption.
    /// </summary>
    public class DreamerAwakeStateController : MonoBehaviour
    {
        [Header("Dreamer Presence State")]
        [SerializeField] private DreamerPresenceStatus currentStatus = DreamerPresenceStatus.LucidAwake;
        public DreamerPresenceStatus CurrentStatus => currentStatus;

        [Header("Autopilot Components")]
        [SerializeField] private AutoBattlerAIController autoBattlerAI;
        [SerializeField] private CompanionAIController companionAI;

        public event Action<DreamerPresenceStatus> OnDreamerStatusChanged;

        private void Awake()
        {
            if (autoBattlerAI == null) autoBattlerAI = GetComponent<AutoBattlerAIController>();
            if (companionAI == null) companionAI = GetComponent<CompanionAIController>();
        }

        public void SetDreamerStatus(DreamerPresenceStatus newStatus)
        {
            if (currentStatus == newStatus) return;

            currentStatus = newStatus;
            OnDreamerStatusChanged?.Invoke(currentStatus);

            switch (currentStatus)
            {
                case DreamerPresenceStatus.SubconsciousAsleep:
                    ActivateLocalRemnantPawn();
                    break;

                case DreamerPresenceStatus.LucidAwake:
                    DeactivateLocalRemnantPawn();
                    break;

                case DreamerPresenceStatus.Disconnected:
                    gameObject.SetActive(false);
                    break;
            }

            Debug.Log($"[DreamerState] '{gameObject.name}' presence transitioned to {currentStatus}.");
        }

        private void ActivateLocalRemnantPawn()
        {
            // If this is not the main player, enable autonomous AI support
            if (!CompareTag("Player"))
            {
                if (companionAI != null)
                {
                    companionAI.enabled = true;
                }
                else if (autoBattlerAI != null)
                {
                    autoBattlerAI.enabled = true;
                }
                else
                {
                    // Dynamically attach companion AI if none exists
                    companionAI = gameObject.AddComponent<CompanionAIController>();
                }
            }
        }

        private void DeactivateLocalRemnantPawn()
        {
            if (!CompareTag("Player"))
            {
                // In live multiplayer, remote network interpolation handles movement
                if (companionAI != null) companionAI.enabled = false;
                if (autoBattlerAI != null) autoBattlerAI.enabled = false;
            }
        }
    }
}
