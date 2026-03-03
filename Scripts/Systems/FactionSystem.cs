using Godot.Collections;
using STGDemoScene1.Scripts.Resources;

namespace STGDemoScene1.Scripts.Systems;

public enum Faction
{
    Player = 0,
    Enemies = 1,
    Friendlies = 2,
}

public static class FactionSystem
{
    private static readonly Dictionary<string, Faction> s_factionMap = [];

    private static FactionTable s_relationTable;

    public delegate void FactionChangeEvent(string instance, Faction faction);

    public static event FactionChangeEvent FactionChangeHandlers;

    public delegate void FactionRelationChangedEvent(Faction faction1, Faction faction2);

    public static event FactionRelationChangedEvent FactionRelationChangeHandlers;

    public static void Initialize(FactionTable relations) => s_relationTable = relations;

    public static void SetFaction(string instance, Faction faction)
    {
        s_factionMap[instance] = faction;
        FactionChangeHandlers?.Invoke(instance, faction);
    }

    public static bool TryGetFaction(string instance, out Faction faction) => s_factionMap.TryGetValue(instance, out faction);

    public static bool GetFactionRelation(Faction faction1, Faction faction2)
    {
        if (s_relationTable.Factions.TryGetValue(faction1, out var value) && value.TryGetValue(faction2, out var relation))
        {
            return relation;
        }
        else
        {
            return false;
        }
    }

    public static void SetFactionRelation(Faction faction1, Faction faction2, bool relation)
    {
        if (s_relationTable.Factions.TryGetValue(faction1, out var relations))
        {
            relations[faction2] = relation;
        }
        else
        {
            s_relationTable.Factions[faction1] = [];
            s_relationTable.Factions[faction1][faction2] = relation;
        }

        FactionRelationChangeHandlers?.Invoke(faction1, faction2);
    }
}
