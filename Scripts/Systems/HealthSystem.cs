using STGDemoScene1.Scripts.Characters;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public struct DamageEvent
{
    public Character inflicter;
    public Character recipient;
    public int damage;
}

public struct DeathEvent
{
    public Character deceased;
    public Character killer;
}

public static class HealthSystem
{
    private static readonly Dictionary<string, int> s_characterHealthMap = [];

    public delegate void DamageEventHandler(DamageEvent damageEvent);
    public delegate void DeathEventHandler(DeathEvent deathEvent);

    public static event DamageEventHandler DamageEventHandlers;
    public static event DeathEventHandler DeathEventHandlers;

    public static void SetCurrentHitpoints(string characterId, int hitpoints) => s_characterHealthMap[characterId] = hitpoints;

    public static int GetCurrentHitpoints(string characterId) => s_characterHealthMap.TryGetValue(characterId, out var hp) ? hp : 0;

    public static void PostDamageEvent(Character inflicter, Character recipient, int damage)
    {
        var ret = new DamageEvent
        {
            inflicter = inflicter,
            recipient = recipient,
            damage = damage,
        };
        s_characterHealthMap[recipient.CharacterData.ResourcePath] -= damage;
        DamageEventHandlers?.Invoke(ret);

        if (s_characterHealthMap[recipient.CharacterData.ResourcePath] <= 0)
        {
            PostDeathEvent(recipient, inflicter);
        }
    }

    public static void PostDeathEvent(Character deceased, Character killer)
    {
        var ret = new DeathEvent
        {
            deceased = deceased,
            killer = killer
        };
        HoverSystem.SetUnhovered(deceased.CharacterData.ResourcePath);
        DeathEventHandlers?.Invoke(ret);
    }
}

