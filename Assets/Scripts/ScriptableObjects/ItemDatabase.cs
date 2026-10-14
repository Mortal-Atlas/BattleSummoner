using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "BattleSummoner/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<TradeableItem> items = new List<TradeableItem>();

    public List<TradeableItem> Items => items;

    public TradeableItem FindItemById(string itemId)
    {
        return items.Find(i => i.itemId == itemId);
    }
}
