using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BattleSummoner.Combat;
using BattleSummoner.UI;

namespace BattleSummoner.DungeonDimension
{
    public class DungeonCrawlerFloorMaster : MonoBehaviour
    {
        private static DungeonCrawlerFloorMaster _instance;
        public static DungeonCrawlerFloorMaster Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindAnyObjectByType<DungeonCrawlerFloorMaster>(FindObjectsInactive.Include);
                    if (!_instance)
                    {
                        var go = new GameObject("DungeonCrawlerFloorMaster");
                        _instance = go.AddComponent<DungeonCrawlerFloorMaster>();
                    }
                }
                return _instance;
            }
            set => _instance = value;
        }

        [Header("Floor Progression State")]
        [SerializeField] private int currentFloorNumber = 1;
        [SerializeField] private FloorThemeInfo currentTheme;
        [SerializeField] private int totalShadowsSlainInRun = 0;
        [SerializeField] private int totalChestsOpenedInRun = 0;

        public int CurrentFloorNumber => currentFloorNumber;
        public FloorThemeInfo CurrentTheme => currentTheme;

        private static readonly string[] FloorTitles = {
            "Crypt of the Banished",
            "Halls of Arcane Penitence",
            "Catacombs of the Void Walker",
            "Sunken Reliquary of Shadows",
            "Sanctum of the Iron Sovereign",
            "Labyrinth of Whispering Thorns",
            "Vault of the Eclipse King",
            "Throne of the Abyssal Titan"
        };

        private void Awake()
        {
            if (_instance == null) _instance = this;
        }

        public void StartNewDungeonCrawl(int startFloor = 1)
        {
            currentFloorNumber = startFloor;
            totalShadowsSlainInRun = 0;
            totalChestsOpenedInRun = 0;

            GenerateFloor(currentFloorNumber);
        }

        public void AdvanceToNextFloor()
        {
            currentFloorNumber++;
            StartCoroutine(TransitionToNextFloorRoutine());
        }

        private IEnumerator TransitionToNextFloorRoutine()
        {
            // Announcement
            string toast = $"🌀 <color=#40EBFF><b>[DESCENDING TO FLOOR B{currentFloorNumber}]</b></color>";
            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(toast);
            }

            yield return new WaitForSeconds(0.8f);

            GenerateFloor(currentFloorNumber);
        }

        public void GenerateFloor(int floorNum)
        {
            currentTheme = RollFloorTheme(floorNum);

            Debug.Log($"🏰 [DUNGEON CRAWLER] Generating Floor B{floorNum}: '{currentTheme.floorTitle}' with Affix: {currentTheme.activeAffix}.");

            var gen = ProceduralDungeonGenerator.Instance;
            if (gen != null)
            {
                int floorSeed = Guid.NewGuid().GetHashCode() ^ (floorNum * 7919);
                gen.BuildDungeonFloor(floorSeed, currentTheme);
            }

            // Announce banner
            AnnounceFloorEntry(currentTheme);
        }

        private FloorThemeInfo RollFloorTheme(int floorNum)
        {
            int titleIdx = Mathf.Clamp(floorNum - 1, 0, FloorTitles.Length - 1);
            string title = FloorTitles[titleIdx];

            // Tartarus Floor Affix roll (25% chance of special affix on floor 2+)
            FloorAffix affix = FloorAffix.StandardDeep;
            if (floorNum >= 2)
            {
                float roll = UnityEngine.Random.value;
                if (roll < 0.20f) affix = FloorAffix.CrimsonMist;
                else if (roll < 0.40f) affix = FloorAffix.GildedTreasury;
                else if (roll < 0.55f) affix = FloorAffix.AetherConvergence;
                else if (roll < 0.70f) affix = FloorAffix.ShadowSwarm;
            }

            return new FloorThemeInfo
            {
                floorNumber = floorNum,
                floorTitle = title,
                subtitle = $"Tartarus Abyss • Depth {floorNum * 60}m",
                activeAffix = affix,
                ambientColor = affix == FloorAffix.CrimsonMist ? new Color(0.22f, 0.08f, 0.10f) :
                               affix == FloorAffix.GildedTreasury ? new Color(0.20f, 0.18f, 0.08f) :
                               new Color(0.08f, 0.10f, 0.16f),
                torchColor = affix == FloorAffix.CrimsonMist ? new Color(1.0f, 0.35f, 0.25f) :
                             affix == FloorAffix.AetherConvergence ? new Color(0.3f, 0.9f, 1.0f) :
                             new Color(1.0f, 0.65f, 0.25f),
                enemyHealthScale = 1.0f + (floorNum - 1) * 0.28f,
                enemyDamageScale = 1.0f + (floorNum - 1) * 0.20f
            };
        }

        private void AnnounceFloorEntry(FloorThemeInfo theme)
        {
            string hexAffix = theme.activeAffix == FloorAffix.CrimsonMist ? "FF3355" :
                              theme.activeAffix == FloorAffix.GildedTreasury ? "FFD700" :
                              theme.activeAffix == FloorAffix.AetherConvergence ? "40EBFF" : "FFFFFF";

            string msg = $"⚔ <color=#40EBFF><b>[FLOOR B{theme.floorNumber}]</b></color> <b>{theme.floorTitle.ToUpper()}</b>\n" +
                         $"<size=80%><color=#{hexAffix}>[Affix: {theme.activeAffix}]</color> • {theme.subtitle}</size>";

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast(msg);
            }
        }

        public void EvacuateDungeon()
        {
            Debug.Log($"🏆 [DUNGEON EVACUATION] Escaped Floor B{currentFloorNumber} with full spoils!");

            if (DevClassLoadoutManager.Instance != null)
            {
                DevClassLoadoutManager.Instance.ShowToast($"<color=#40EBFF><b>[EXPEDITION COMPLETE]</b></color>\nReturned safely from Floor B{currentFloorNumber}!");
            }

            // Return to Overworld
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene("OverworldMap", "Overworld_TavernDoor", "Solas Town Overworld");
            }
        }
    }
}
