using System;
using UnityEngine;

namespace BattleSummoner.DungeonDimension
{
    public enum DungeonRoomArchetype
    {
        EntrySanctuary,     // Safe starting room with recovery font & teleporter stone
        CombatChamber,       // Roaming shadows patrol; clearing opens gates
        TreasureVault,       // Gilded chests, gemstone clusters, breakable urns
        ArcanaShrine,        // Ancient tarot altar granting major run blessings
        TrapGauntlet,        // Hazardous floor blade/rune tiles testing 3rd-person agility
        GuardianChamber      // Floor Boss arena with Exit Staircase to descend deeper
    }

    [Flags]
    public enum DoorwayDirection
    {
        None = 0,
        North = 1 << 0,  // +Z
        South = 1 << 1,  // -Z
        East  = 1 << 2,  // +X
        West  = 1 << 3   // -X
    }

    public enum FloorAffix
    {
        StandardDeep,      // Normal balanced dungeon exploration
        CrimsonMist,       // Tartarus Blood Mist: Aggressive elite shadows, +100% rare chests
        GildedTreasury,    // Treasure Floor: Golden shadows, double gold payouts
        AetherConvergence, // Magic Surge: -35% Spell Cooldowns, +40% Elemental damage
        ShadowSwarm        // Swarms of fast minor shadows; bountiful EXP farming
    }

    [Serializable]
    public class DungeonCellNode
    {
        public Vector2Int gridPos;
        public Vector3 worldPos;
        public DungeonRoomArchetype archetype;
        public DoorwayDirection openDoorways = DoorwayDirection.None;
        public bool isExplored = false;
        public int distanceFromEntry = 0;
        public GameObject roomInstance;

        public bool HasDoor(DoorwayDirection dir) => dir != DoorwayDirection.None && (openDoorways & dir) == dir;
        public void AddDoor(DoorwayDirection dir) => openDoorways |= dir;
    }

    [Serializable]
    public struct FloorThemeInfo
    {
        public int floorNumber;
        public string floorTitle;
        public string subtitle;
        public FloorAffix activeAffix;
        public Color ambientColor;
        public Color torchColor;
        public float enemyHealthScale;
        public float enemyDamageScale;
    }

    [Serializable]
    public struct GeneratedRoomNode
    {
        public int nodeIndex;
        public DungeonRoomArchetype archetype;
        public Vector3 gridCoordinate;
        public Quaternion orientation;
        public int enemyWaveSeed;
    }
}

