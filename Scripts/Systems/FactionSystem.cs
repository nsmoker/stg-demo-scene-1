using Godot;
using Godot.Collections;
using System;

public enum Faction
{
    Player = 0,
    Enemies = 1,
    Friendlies = 2,
}

public static class FactionSystem
{
    private static Dictionary<string, Faction> _factionMap = [];

    private static FactionTable _relationTable;

    public delegate void FactionChangeEvent(string instance, Faction faction);

    public static FactionChangeEvent FactionChangeHandlers;

    public delegate void FactionRelationChangedEvent(Faction faction1, Faction faction2);

    public static FactionRelationChangedEvent FactionRelationChangeHandlers;

    public static void Initialize(FactionTable relations)
    {
        _relationTable = relations;
    }

    public static void SetFaction(string instance, Faction faction)
    {
        _factionMap[instance] = faction;
        FactionChangeHandlers?.Invoke(instance, faction);
    }

    public static bool TryGetFaction(string instance, out Faction faction)
    {
        return _factionMap.TryGetValue(instance, out faction);
    }

    public static bool GetFactionRelation(Faction faction1, Faction faction2)
    {
        if (_relationTable.Factions.TryGetValue(faction1, out var value) && value.TryGetValue(faction2, out var relation))
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
        if (_relationTable.Factions.TryGetValue(faction1, out var relations))
        {
            relations[faction2] = relation;
        }
        else
        {
            _relationTable.Factions[faction1] = [];
            _relationTable.Factions[faction1][faction2] = relation;
        }

        FactionRelationChangeHandlers?.Invoke(faction1, faction2);
    }
}
