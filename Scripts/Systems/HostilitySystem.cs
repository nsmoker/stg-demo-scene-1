using Godot;
using System;
using System.Collections.Generic;

public static class HostilitySystem
{
    private static readonly Dictionary<string, HashSet<string>> _hostilityOverrides = [];

    public delegate void HostilityChangedEvent(string entity1, string entity2, bool newHostility);

    public static HostilityChangedEvent HostilityChangeHandlers;

    public static void SetHostilityOverride(string entity1, string entity2, bool hostile)
    {
        if (_hostilityOverrides.TryGetValue(entity1, out var overrides))
        {
            if (hostile)
            {
                _ = overrides.Add(entity2);
            }
            else
            {
                _ = overrides.Remove(entity2);
            }
        }
        else if (hostile)
        {
            _hostilityOverrides[entity1] = [];
            _ = _hostilityOverrides[entity1].Add(entity2);
        }
        else
        {
            return;
        }

        HostilityChangeHandlers?.Invoke(entity1, entity2, hostile);
    }

    public static bool GetHostility(string entity1, string entity2)
    {
        if (_hostilityOverrides.TryGetValue(entity1, out var value))
        {
            return value.Contains(entity2);
        }
        else if (FactionSystem.TryGetFaction(entity1, out var faction1) && FactionSystem.TryGetFaction(entity2, out var faction2))
        {
            return FactionSystem.GetFactionRelation(faction1, faction2);
        }
        else
        {
            return false;
        }
    }
}
