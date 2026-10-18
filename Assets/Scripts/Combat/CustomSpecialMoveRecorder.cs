using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSummoner.Combat
{
    public enum RecordedActionType { MeleeDirectionalSlash, SpellRuneGlyph, ExtraFinisherBurst }

    [Serializable]
    public struct RecordedMoveNode
    {
        public RecordedActionType actionType;
        public Vector2 swipeDirectionOrCastVector;
        public SpellAffinity spellElement;
        public string animationTriggerId;
        public float basePowerMultiplier;
    }

    /// <summary>
    /// Practice Room Custom Special Move Recorder & Earned Team Synergy Combos.
    /// Inside the Practice Room, players record custom Special Moves (Spells and Melee Slashes + Finishers).
    /// Unlocks +1 move slot every 15 levels (starts at 2, up to 8). Recorded moves get a 1.35x power boost.
    /// Earned Team Synergy Gauge (0-100%) prevents spam and enables Shoulder-Throw Launch aerial meteor slams.
    /// </summary>
    public class CustomSpecialMoveRecorder : MonoBehaviour
    {
        public static CustomSpecialMoveRecorder Instance { get; private set; }

        [Header("Progression & Slot Limits")]
        public int playerCharacterLevel = 1;
        public bool isInsidePracticeRoom = false;

        [Header("Saved Custom Special Moves")]
        public List<RecordedMoveNode> customSpellSpecialSequence = new List<RecordedMoveNode>();
        public List<RecordedMoveNode> customMeleeSpecialSequence = new List<RecordedMoveNode>();

        [Header("Earned Team Synergy Meter (Prevents Spam)")]
        [Range(0f, 100f)] public float teamSynergyGauge = 0f;

        public event Action<bool, int> OnSpecialMoveRecorded; // (isMelee, totalRecorded)
        public event Action<float> OnTeamSynergyChanged;
        public event Action OnShoulderThrowLaunchExecuted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Starts at 2 slots at Level 1. Unlocks +1 slot every 15 player levels (up to 8-move giant combos).
        /// </summary>
        public int GetMaxAllowedSpecialMoveSlots() => Mathf.Clamp(2 + (playerCharacterLevel / 15), 2, 8);

        public bool RecordStepInPracticeRoom(bool isMeleeCombo, RecordedMoveNode newStep)
        {
            if (!isInsidePracticeRoom)
            {
                Debug.LogWarning("[Special Move Recorder] Must be in the Practice Room to record or edit Special Moves!");
                return false;
            }

            var targetList = isMeleeCombo ? customMeleeSpecialSequence : customSpellSpecialSequence;
            if (targetList.Count >= GetMaxAllowedSpecialMoveSlots())
            {
                Debug.LogWarning($"[Special Move Recorder] Slot limit reached ({GetMaxAllowedSpecialMoveSlots()} moves). Level up to add more moves!");
                return false;
            }

            // Recorded special moves automatically receive a flashy 1.35x power boost per node
            newStep.basePowerMultiplier = Mathf.Max(1.35f, newStep.basePowerMultiplier * 1.35f);
            targetList.Add(newStep);

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("menu_open", 0.7f);
            }

            OnSpecialMoveRecorded?.Invoke(isMeleeCombo, targetList.Count);
            Debug.Log($"[Special Move Recorder] Recorded {(isMeleeCombo ? "Melee" : "Spell")} node #{targetList.Count}/{GetMaxAllowedSpecialMoveSlots()} (Type: {newStep.actionType}, Power: {newStep.basePowerMultiplier:F2}x)");
            return true;
        }

        public void ClearRecordedSequence(bool isMeleeCombo)
        {
            if (isMeleeCombo) customMeleeSpecialSequence.Clear();
            else customSpellSpecialSequence.Clear();
            Debug.Log($"[Special Move Recorder] Cleared {(isMeleeCombo ? "Melee" : "Spell")} sequence.");
        }

        public void AddTeamSynergyProgress(float amount)
        {
            teamSynergyGauge = Mathf.Clamp(teamSynergyGauge + amount, 0f, 100f);
            OnTeamSynergyChanged?.Invoke(teamSynergyGauge);

            if (teamSynergyGauge >= 100f && BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("crystal_resonate", 1.0f);
            }
        }

        /// <summary>
        /// Shoulder-Throw Launch: Two teammates interlock hands to launch a third ally 40 feet into the air for an aerial meteor slam.
        /// Strict criteria: Requires Team Synergy Gauge at 100%. Prevents spam!
        /// </summary>
        public bool TryExecuteTeammateShoulderThrowLaunch(Transform launchedAlly, float launchHeight = 14f)
        {
            if (teamSynergyGauge < 100f)
            {
                Debug.LogWarning($"[Team Synergy Combo] Gauge at {teamSynergyGauge:F0}%/100%. Parry, heal, or synchronize strikes to charge!");
                return false;
            }

            teamSynergyGauge = 0f;
            OnTeamSynergyChanged?.Invoke(teamSynergyGauge);

            if (launchedAlly != null && launchedAlly.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(Vector3.up * launchHeight, ForceMode.VelocityChange);
            }

            if (SystemBroadcastManager.Instance != null)
            {
                SystemBroadcastManager.Instance.DispatchBroadcast("💥 [TEAM SYNERGY COMBO] 40-Foot Shoulder-Throw Meteor Slam Executed!", true);
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("cinematic_boom", 1.0f);
            }

            OnShoulderThrowLaunchExecuted?.Invoke();
            Debug.Log("[Team Synergy Combo] Shoulder-Throw Launch Executed!");
            return true;
        }
    }
}
