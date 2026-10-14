using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.Customization;

namespace BattleSummoner.Mounts
{
    /// <summary>
    /// PlayerMountController handles mounting/dismounting familiars and constructs,
    /// checks physical size & weight constraints ("if it can fit them that is"),
    /// and routes riding controls into the active MountEntity for battle & ramming charges.
    /// </summary>
    public class PlayerMountController : MonoBehaviour
    {
        public static PlayerMountController Instance { get; private set; }

        [Header("Interaction Settings")]
        [SerializeField] private float mountInteractionRange = 3.8f;
        [SerializeField] private KeyCode mountInteractKey = KeyCode.F;
        [SerializeField] private KeyCode summonMountKey = KeyCode.M;

        [Header("State")]
        [SerializeField] private MountEntity currentMount;
        public bool IsMounted => currentMount != null;
        public MountEntity CurrentMount => currentMount;

        private CharacterController playerController;
        private PlayerMovementController playerMovement;
        private CustomizablePlayerAvatar playerAvatar;
        private FloatingVirtualStick virtualStick;

        // Summoned Mount Archetypes Deck
        private int selectedMountArchetypeIndex = 0;
        private GameObject activeSummonedMountObject;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            playerController = GetComponent<CharacterController>();
            playerMovement = GetComponent<PlayerMovementController>();
            playerAvatar = GetComponent<CustomizablePlayerAvatar>();
            virtualStick = FindAnyObjectByType<FloatingVirtualStick>();
        }

        private void Update()
        {
            // 1. Summon / Cycle Mount with [M]
            if (Input.GetKeyDown(summonMountKey))
            {
                CycleAndSummonNextMount();
            }

            // 2. Mount / Dismount Interaction with [F]
            if (Input.GetKeyDown(mountInteractKey))
            {
                if (IsMounted)
                {
                    DismountCurrent();
                }
                else
                {
                    TryMountNearest();
                }
            }

            // 3. Process Mounted Controls if riding
            if (IsMounted && currentMount != null)
            {
                Vector2 moveInput = Vector2.zero;

                // Keyboard input
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput.y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput.y -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput.x += 1f;

                // Virtual joystick integration if touched
                if (virtualStick != null && virtualStick.MoveInput.sqrMagnitude > 0.05f)
                {
                    moveInput = virtualStick.MoveInput;
                }

                bool chargeRequested = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || (moveInput.y > 0.85f);
                bool jumpRequested = Input.GetKeyDown(KeyCode.Space);

                currentMount.ProcessRiderInput(moveInput, chargeRequested, jumpRequested, Time.deltaTime);
            }
        }

        #region Mounting Logic & Fit Evaluation
        public void TryMountNearest()
        {
            MountEntity bestCandidate = FindNearestMount(mountInteractionRange);
            if (bestCandidate == null)
            {
                if (DevClassLoadoutManager.Instance != null)
                {
                    DevClassLoadoutManager.Instance.ShowToast("<color=#AAAAAA>[NO MOUNT NEARBY]</color>\nPress <b>[M]</b> to summon a mount or construct!");
                }
                return;
            }

            AttemptMountEntity(bestCandidate);
        }

        public bool AttemptMountEntity(MountEntity targetMount)
        {
            if (targetMount == null) return false;

            // Calculate rider physical dimensions from CustomizablePlayerAvatar
            GetRiderDimensions(out float riderHeight, out float riderWeight);

            // Check if rider can fit on this familiar, summon, or construct
            if (!targetMount.TryMount(transform, riderHeight, riderWeight, out string rejectionReason))
            {
                // Rejection: "If it can fit them that is"
                if (DevClassLoadoutManager.Instance != null)
                {
                    DevClassLoadoutManager.Instance.ShowToast(rejectionReason);
                }

                if (CombatFeedbackManager.Instance != null)
                {
                    CombatFeedbackManager.Instance.SpawnFloatingText(transform.position + Vector3.up * 1.5f, "<color=#FF4444><b>[CANNOT FIT MOUNT]</b></color>", 1.3f);
                }

                if (BattleSummonerAudioMaster.Instance != null)
                {
                    BattleSummonerAudioMaster.Instance.PlayPresetSFX("malformed_wheeze", 0.9f);
                }

                return false;
            }

            // Successfully mounted!
            currentMount = targetMount;

            // Disable player foot locomotion
            if (playerController != null) playerController.enabled = false;
            if (playerMovement != null) playerMovement.enabled = false;

            // Set rider riding pose offset
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            return true;
        }

        public void DismountCurrent()
        {
            if (currentMount == null) return;

            var mount = currentMount;
            currentMount = null;
            mount.Dismount();

            // Re-enable player foot locomotion
            if (playerController != null) playerController.enabled = true;
            if (playerMovement != null) playerMovement.enabled = true;
        }

        public void GetRiderDimensions(out float heightMeters, out float weightKg)
        {
            heightMeters = 1.75f;
            weightKg = 72f;

            if (playerAvatar != null && playerAvatar.CurrentData != null)
            {
                var data = playerAvatar.CurrentData;
                float legFactor = data.legsLength;
                float torsoFactor = data.torsoScale;
                heightMeters = 1.75f * ((legFactor * 0.55f) + (torsoFactor * 0.45f)) * data.heightScale;

                float volumeFactor = data.torsoScale * data.hipsScale;
                weightKg = 72f * volumeFactor;
            }
        }

        public MountEntity FindNearestMount(float maxDist)
        {
            MountEntity[] mounts = FindObjectsByType<MountEntity>(FindObjectsSortMode.None);
            MountEntity best = null;
            float closestDist = maxDist;

            for (int i = 0; i < mounts.Length; i++)
            {
                if (mounts[i].IsOccupied) continue;
                float d = Vector3.Distance(transform.position, mounts[i].transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    best = mounts[i];
                }
            }

            return best;
        }
        #endregion

        #region Summoning & Mount Spawning
        public void CycleAndSummonNextMount()
        {
            if (IsMounted) DismountCurrent();

            // Destroy previously summoned active mount if present
            if (activeSummonedMountObject != null)
            {
                Destroy(activeSummonedMountObject);
            }

            selectedMountArchetypeIndex = (selectedMountArchetypeIndex + 1) % 4;
            Vector3 spawnPos = transform.position + transform.forward * 2.8f + Vector3.up * 0.2f;

            switch (selectedMountArchetypeIndex)
            {
                case 0:
                    // 1. Valkyrie Ramming Chariot (Construct with iron plow & dual side scythes)
                    activeSummonedMountObject = SpawnMountInstance("Valkyrie Ramming Chariot", MountType.RammingWarChariot, MountSizeTier.Large, 500f, spawnPos);
                    break;
                case 1:
                    // 2. Aegis Celestial Lion (Familiar Beast)
                    activeSummonedMountObject = SpawnMountInstance("Aegis Celestial Lion", MountType.FamiliarBeast, MountSizeTier.Medium, 160f, spawnPos);
                    break;
                case 2:
                    // 3. Ironclad Siege Titan (Colossal Construct)
                    activeSummonedMountObject = SpawnMountInstance("Ironclad Siege Titan", MountType.ConstructGolem, MountSizeTier.Colossal, 2500f, spawnPos);
                    break;
                case 3:
                    // 4. Pygmy Sprite Familiar (Tiny Familiar - specifically proves "if it can fit them that is" rejection!)
                    activeSummonedMountObject = SpawnMountInstance("Pygmy Sprite Familiar", MountType.PygmySprite, MountSizeTier.Tiny, 12f, spawnPos);
                    break;
            }

            if (BattleSummonerAudioMaster.Instance != null)
            {
                BattleSummonerAudioMaster.Instance.PlayPresetSFX("spell_cast", 1.0f);
            }

            var newMount = activeSummonedMountObject.GetComponent<MountEntity>();
            if (DevClassLoadoutManager.Instance != null && newMount != null)
            {
                string sizeBadge = newMount.Stats.sizeTier == MountSizeTier.Tiny ? "<color=#FF4444>[TINY - UNMOUNTABLE]</color>" : "<color=#44FF44>[MOUNTABLE]</color>";
                DevClassLoadoutManager.Instance.ShowToast($"<color=#FFEE22>[SUMMONED MOUNT]</color>\n<b>{newMount.Stats.mountName}</b> {sizeBadge}\nWalk close and press <b>[F]</b> to ride!");
            }
        }

        public GameObject SpawnMountInstance(string name, MountType type, MountSizeTier tier, float maxCarry, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.rotation = transform.rotation;

            var mount = go.AddComponent<MountEntity>();
            mount.Stats.mountName = name;
            mount.Stats.mountType = type;
            mount.Stats.sizeTier = tier;
            mount.Stats.maxCarryWeightKg = maxCarry;

            if (tier == MountSizeTier.Tiny)
            {
                mount.Stats.minRiderHeight = 0.2f;
                mount.Stats.maxRiderHeight = 0.5f;
                go.transform.localScale = Vector3.one * 0.4f; // Tiny visual scale
            }
            else if (tier == MountSizeTier.Colossal)
            {
                go.transform.localScale = Vector3.one * 1.5f; // Giant Titan scale
            }

            mount.EnsureSocketsAndProceduralMesh();
            return go;
        }
        #endregion
    }
}
