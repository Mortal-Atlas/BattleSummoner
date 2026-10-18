using System;
using UnityEngine;
using BattleSummoner.Customization;

namespace BattleSummoner.Multiplayer
{
    /// <summary>
    /// ModularMultiplayerSync coordinates character appearance data synchronization across multiplayer:
    /// - Serializes local custom appearance into network packets / JSON payloads.
    /// - Applies incoming peer appearance data to remote characters.
    /// - Provides utility to spawn remote peer characters using the same rigged modular body part system.
    /// </summary>
    public class ModularMultiplayerSync : MonoBehaviour
    {
        [Header("Character References")]
        public ModularCharacterAppearance modularAppearance;
        public bool isLocalPlayer = false;

        [Header("Network Identity")]
        public string networkId = "Peer_01";
        public string displayName = "AstralWarden";

        public event Action<ModularPlayerAppearanceData> OnNetworkAppearanceReceived;

        private void Awake()
        {
            if (modularAppearance == null)
            {
                modularAppearance = GetComponent<ModularCharacterAppearance>() ?? GetComponentInChildren<ModularCharacterAppearance>();
            }

            if (gameObject.name.Contains("Player_Hero"))
            {
                isLocalPlayer = true;
            }
        }

        private void Start()
        {
            if (isLocalPlayer)
            {
                // Local player: load profile and broadcast
                var profile = ModularPlayerAppearanceData.LoadLocalProfile();
                if (modularAppearance != null)
                {
                    modularAppearance.ApplyAppearance(profile);
                }
            }
        }

        /// <summary>
        /// Generates the payload to send over network (e.g. Mirror, Photon, Netcode for GameObjects, or WebSocket).
        /// </summary>
        public string GetNetworkPayload()
        {
            var data = modularAppearance != null && modularAppearance.CurrentData != null
                ? modularAppearance.CurrentData
                : ModularPlayerAppearanceData.LoadLocalProfile();
            return data.ToJson();
        }

        /// <summary>
        /// Called when network packet with appearance JSON is received from a peer.
        /// </summary>
        public void ReceiveNetworkAppearance(string jsonPayload)
        {
            if (string.IsNullOrEmpty(jsonPayload)) return;
            var data = ModularPlayerAppearanceData.FromJson(jsonPayload);
            ApplyRemoteAppearance(data);
        }

        /// <summary>
        /// Applies remote peer data to this character instance.
        /// </summary>
        public void ApplyRemoteAppearance(ModularPlayerAppearanceData data)
        {
            if (data == null) return;
            displayName = data.playerName;

            if (modularAppearance != null)
            {
                modularAppearance.ApplyAppearance(data);
            }

            OnNetworkAppearanceReceived?.Invoke(data);
            Debug.Log($"[MULTIPLAYER SYNC] Synchronized peer [{networkId} : {displayName}] appearance (Gauntlets={data.equipGauntlets}, Torso={data.equipTorso}, Leggings={data.equipLeggings}, Boots={data.equipBoots}, Weapon={data.weaponId})");
        }

        /// <summary>
        /// Static factory: Spawns a fully rigged modular peer in the current scene and applies the given appearance data.
        /// </summary>
        public static GameObject SpawnRemotePeer(string peerId, ModularPlayerAppearanceData appearanceData, Vector3 position, Quaternion rotation)
        {
            var heroPrefab = Resources.Load<GameObject>("Prefabs/Player/Player_Hero");
#if UNITY_EDITOR
            if (heroPrefab == null)
            {
                heroPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player_Hero.prefab");
            }
#endif
            if (heroPrefab == null)
            {
                Debug.LogError("[MULTIPLAYER] Cannot spawn remote peer: Player_Hero.prefab not found!");
                return null;
            }

            GameObject peer = Instantiate(heroPrefab, position, rotation);
            peer.name = $"Remote_Peer_{peerId}";

            // Disable local player controller components on remote peer
            var movementCtrl = peer.GetComponent<PlayerMovementController>();
            if (movementCtrl != null) movementCtrl.enabled = false;

            var cc = peer.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Ensure ModularMultiplayerSync component
            var sync = peer.GetComponent<ModularMultiplayerSync>();
            if (sync == null) sync = peer.AddComponent<ModularMultiplayerSync>();
            sync.isLocalPlayer = false;
            sync.networkId = peerId;
            sync.displayName = appearanceData.playerName;

            var modApp = peer.GetComponent<ModularCharacterAppearance>() ?? peer.GetComponentInChildren<ModularCharacterAppearance>();
            if (modApp != null)
            {
                sync.modularAppearance = modApp;
            }

            // Apply peer appearance data
            sync.ApplyRemoteAppearance(appearanceData);

            return peer;
        }
    }
}
