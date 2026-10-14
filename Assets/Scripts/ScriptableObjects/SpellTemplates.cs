using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellTemplates", menuName = "BattleSummoner/Spell Templates")]
public class SpellTemplates : ScriptableObject
{
    [SerializeField] private List<SpellPayload> templates = new List<SpellPayload>();

    public List<SpellPayload> Templates => templates;
}
