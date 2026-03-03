using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public static class HostilitySystem
{
    private static readonly Dictionary<string, HashSet<string>> s_hostilityOverrides = [];

    public delegate void HostilityChangedEvent(string entity1, string entity2, bool newHostility);

    public static event HostilityChangedEvent HostilityChangeHandlers;

    public static void SetHostilityOverride(string entity1, string entity2, bool hostile)
    {
        if (s_hostilityOverrides.TryGetValue(entity1, out var overrides))
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
            s_hostilityOverrides[entity1] = [];
            _ = s_hostilityOverrides[entity1].Add(entity2);
        }
        else
        {
            return;
        }

        HostilityChangeHandlers?.Invoke(entity1, entity2, hostile);
    }

    public static bool GetHostility(string entity1, string entity2)
    {
        if (s_hostilityOverrides.TryGetValue(entity1, out var value))
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
