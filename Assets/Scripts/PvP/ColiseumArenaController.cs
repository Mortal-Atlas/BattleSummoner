using UnityEngine;

public class ColiseumArenaController : MonoBehaviour
{
    public static ColiseumArenaController Instance { get; private set; }

    public enum LeagueType { WizardingCup, WarriorCup, GroupBattle, ProxyGolemCup, ArcheryCup }
    public LeagueType activeLeague = LeagueType.WizardingCup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void InitializeMatchmaking(string playerId)
    {
        Debug.Log($"[Coliseum] Player '{playerId}' registered for {activeLeague} tournament queue.");
    }
}
