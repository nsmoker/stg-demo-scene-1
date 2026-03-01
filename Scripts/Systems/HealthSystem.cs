using ArkhamHunters.Scripts;
using System.Collections.Generic;

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
    private static readonly Dictionary<string, int> _characterHealthMap = [];

    public delegate void DamageEventHandler(DamageEvent damageEvent);
    public delegate void DeathEventHandler(DeathEvent deathEvent);

    public static DamageEventHandler DamageEventHandlers;
    public static DeathEventHandler DeathEventHandlers;

    public static void SetCurrentHitpoints(string characterId, int hitpoints) => _characterHealthMap[characterId] = hitpoints;

    public static int GetCurrentHitpoints(string characterId) => _characterHealthMap.TryGetValue(characterId, out var hp) ? hp : 0;

    public static void PostDamageEvent(Character inflicter, Character recipient, int damage)
    {
        var ret = new DamageEvent
        {
            inflicter = inflicter,
            recipient = recipient,
            damage = damage,
        };
        _characterHealthMap[recipient.CharacterData.ResourcePath] -= damage;
        DamageEventHandlers?.Invoke(ret);

        if (_characterHealthMap[recipient.CharacterData.ResourcePath] <= 0)
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
