using Godot.Collections;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Resources.Factions;
using System.Linq;

namespace STGDemoScene1.Scripts.Systems;

public static class FactionSystem
{
    private static readonly Dictionary<string, Faction> s_factionMap = [];

    private static readonly FactionTable s_relationTable = new();

    public delegate void FactionChangeEvent(string instance, Faction faction);

    public static event FactionChangeEvent FactionChangeHandlers;

    public delegate void FactionRelationChangedEvent(Faction faction1, Faction faction2);

    public static event FactionRelationChangedEvent FactionRelationChangeHandlers;

    public static void Initialize(FactionTable relations)
    {
        foreach (var relation in relations.FactionRelations)
        {
            SetFactionRelation(relation.FactionA, relation.FactionB, relation.IsHostile);
        }
    }

    public static void SetFaction(string instance, Faction faction)
    {
        s_factionMap[instance] = faction;
        FactionChangeHandlers?.Invoke(instance, faction);
    }

    public static bool TryGetFaction(string instance, out Faction faction) => s_factionMap.TryGetValue(instance, out faction);

    public static bool GetFactionRelation(Faction faction1, Faction faction2) => s_relationTable.FactionRelations.Any(x =>
        x.FactionA == faction1 && x.FactionB == faction2 && x.IsHostile);

    public static void SetFactionRelation(Faction faction1, Faction faction2, bool relation)
    {
        s_relationTable.FactionRelations = [.. s_relationTable.FactionRelations
            .Where(x => (x.FactionA != faction1) || (x.FactionA != faction2))];
        s_relationTable.FactionRelations.Add(new()
        {
            FactionA = faction1,
            FactionB = faction2,
            IsHostile = relation
        });

        FactionRelationChangeHandlers?.Invoke(faction1, faction2);
    }
}
