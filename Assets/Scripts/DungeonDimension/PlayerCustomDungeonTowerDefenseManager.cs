using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BattleSummoner.DungeonDimension
{
    public enum CustomDefenseType
    {
        FloorSpikeTrap,
        ElementalTurretTower,
        RollingBoulderChute,
        SwingingPendulumBlade,
        MonsterWaveSpawner,
        DropWallBarricade
    }

    [Serializable]
    public class DefenseBlueprintCard
    {
        public string cardId;
        public string displayName;
        public CustomDefenseType defenseType;
        public int threatManaCost = 25;
        public GameObject defenseOrMonsterPrefab;
        public int monsterCountPerSpawn = 3;
    }

    [Serializable]
    public class PlacedDungeonDefenseNode
    {
        public Vector2Int gridCell;
        public CustomDefenseType defenseType;
        public GameObject spawnedInstance;
    }

    /// <summary>
    /// Section 9: Custom Player-Built Tower-Defense Dungeons ("Dungeon Architect Mode").
    /// Grid-based Architect Build Mode & live Tower-Defense Defender Loop.
    /// </summary>
    public class PlayerCustomDungeonTowerDefenseManager : MonoBehaviour
    {
        public static PlayerCustomDungeonTowerDefenseManager Instance { get; private set; }

        [Header("Dungeon Architect Grid Settings")]
        public int gridWidth = 8;
        public int gridLength = 18;
        public float cellSizeMeters = 6.0f;
        public Transform customDungeonOrigin;
        public Transform dungeonVaultCoreTarget;
        public float vaultCoreHp = 3000f;

        [Header("Tower Defense Runtime State (Active Raid Defense)")]
        public bool isInArchitectBuildMode = false;
        public bool isTowerDefenseMatchActive = false;
        public bool isAutopilotDefending = false;
        public float currentThreatMana = 100f;
        public float maxThreatMana = 200f;
        public float threatManaRegenPerSecond = 8.5f;
        public int architectMasteryLevel = 1;

        [Header("Defender Deck & Placed Defenses")]
        public List<DefenseBlueprintCard> unlockedDefenseDeck = new List<DefenseBlueprintCard>();
        public List<PlacedDungeonDefenseNode> placedTrapsAndTowers = new List<PlacedDungeonDefenseNode>();
        public List<Transform> activeInvaderPlayers = new List<Transform>();

        private float autopilotDecisionTimer = 0f;

        public event Action<int> OnInvaderSquadWipedVictory;
        public event Action OnVaultCoreBreachedDefeat;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (!isTowerDefenseMatchActive) return;

            currentThreatMana = Mathf.Min(maxThreatMana, currentThreatMana + (threatManaRegenPerSecond * Time.deltaTime));

            if (isAutopilotDefending && activeInvaderPlayers.Count > 0)
            {
                TickAutopilotTowerDefenseAI();
            }
        }

        public bool TryPlaceTrapOrTowerOnGrid(Vector2Int cellCoord, DefenseBlueprintCard card)
        {
            if (cellCoord.x < 0 || cellCoord.x >= gridWidth || cellCoord.y < 0 || cellCoord.y >= gridLength)
                return false;

            if (placedTrapsAndTowers.Exists(n => n.gridCell == cellCoord))
                return false;

            Vector3 worldPos = GridToWorldPosition(cellCoord);
            GameObject instance = card.defenseOrMonsterPrefab != null
                ? Instantiate(card.defenseOrMonsterPrefab, worldPos, Quaternion.identity, customDungeonOrigin)
                : null;

            placedTrapsAndTowers.Add(new PlacedDungeonDefenseNode
            {
                gridCell = cellCoord,
                defenseType = card.defenseType,
                spawnedInstance = instance
            });

            Debug.Log($"[Dungeon Architect] Placed '{card.displayName}' at Grid {cellCoord}.");
            return true;
        }

        public bool TryDeployLiveTowerDefenseCard(DefenseBlueprintCard card, Vector3 targetWorldPosition)
        {
            if (!isTowerDefenseMatchActive || card == null) return false;
            if (currentThreatMana < card.threatManaCost)
            {
                Debug.LogWarning($"[Tower Defense] Not enough Threat Mana ({currentThreatMana:F0}/{card.threatManaCost}) to deploy '{card.displayName}'!");
                return false;
            }

            currentThreatMana -= card.threatManaCost;

            if (card.defenseType == CustomDefenseType.MonsterWaveSpawner && card.defenseOrMonsterPrefab != null)
            {
                for (int i = 0; i < card.monsterCountPerSpawn; i++)
                {
                    Vector3 offset = new Vector3(UnityEngine.Random.Range(-2f, 2f), 0f, UnityEngine.Random.Range(-2f, 2f));
                    GameObject mob = Instantiate(card.defenseOrMonsterPrefab, targetWorldPosition + offset, Quaternion.identity);

                    Transform closestInvader = GetClosestInvader(targetWorldPosition);
                    if (closestInvader != null && mob.TryGetComponent<NavMeshAgent>(out var agent))
                    {
                        agent.SetDestination(closestInvader.position);
                    }
                }
                Debug.Log($"[Tower Defense] Spawned {card.monsterCountPerSpawn}x '{card.displayName}' to stop the invaders!");
            }
            else if (card.defenseOrMonsterPrefab != null)
            {
                Instantiate(card.defenseOrMonsterPrefab, targetWorldPosition, Quaternion.identity);
                Debug.Log($"[Tower Defense] Triggered '{card.displayName}' trap at {targetWorldPosition}!");
            }

            return true;
        }

        private void TickAutopilotTowerDefenseAI()
        {
            autopilotDecisionTimer -= Time.deltaTime;
            if (autopilotDecisionTimer > 0f || unlockedDefenseDeck.Count == 0) return;

            autopilotDecisionTimer = 4.0f;
            DefenseBlueprintCard randomCard = unlockedDefenseDeck[UnityEngine.Random.Range(0, unlockedDefenseDeck.Count)];
            if (activeInvaderPlayers.Count > 0)
            {
                Transform leadInvader = activeInvaderPlayers[0];
                if (leadInvader != null && currentThreatMana >= randomCard.threatManaCost)
                {
                    Vector3 ambushPos = leadInvader.position + (leadInvader.forward * 6f);
                    TryDeployLiveTowerDefenseCard(randomCard, ambushPos);
                }
            }
        }

        private Transform GetClosestInvader(Vector3 fromPos)
        {
            Transform best = null;
            float minDist = float.MaxValue;
            foreach (var inv in activeInvaderPlayers)
            {
                if (inv == null) continue;
                float d = Vector3.Distance(fromPos, inv.position);
                if (d < minDist) { minDist = d; best = inv; }
            }
            return best;
        }

        public void RegisterInvaderEliminated(Transform invader)
        {
            activeInvaderPlayers.Remove(invader);
            if (activeInvaderPlayers.Count == 0 && isTowerDefenseMatchActive)
            {
                isTowerDefenseMatchActive = false;
                int rewardCores = 400 * architectMasteryLevel;
                architectMasteryLevel++;
                OnInvaderSquadWipedVictory?.Invoke(rewardCores);
                Debug.Log($"[Tower Defense VICTORY!] Invaders wiped out! Earned {rewardCores} Mana Cores & Architect Lv.{architectMasteryLevel}!");
            }
        }

        public void DamageDungeonVaultCore(float dmg)
        {
            vaultCoreHp -= dmg;
            if (vaultCoreHp <= 0f && isTowerDefenseMatchActive)
            {
                isTowerDefenseMatchActive = false;
                OnVaultCoreBreachedDefeat?.Invoke();
                Debug.LogWarning("[Tower Defense DEFEAT] Invaders made it through and breached the Dungeon Core!");
            }
        }

        public Vector3 GridToWorldPosition(Vector2Int cell)
        {
            Vector3 baseOrigin = customDungeonOrigin != null ? customDungeonOrigin.position : Vector3.zero;
            return baseOrigin + new Vector3(cell.x * cellSizeMeters, 0f, cell.y * cellSizeMeters);
        }
    }
}
