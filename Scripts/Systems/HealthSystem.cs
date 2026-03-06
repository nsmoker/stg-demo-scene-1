using STGDemoScene1.Scripts.Characters;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public struct DamageEvent
{
    public Character Inflicter;
    public Character Recipient;
    public int Damage;
}

public struct DeathEvent
{
    public Character Deceased;
    public Character Killer;
}

public static class HealthSystem
{
    private static readonly Dictionary<string, int> s_characterHealthMap = [];

    public delegate void DamageEventHandler(DamageEvent damageEvent);
    public delegate void DeathEventHandler(DeathEvent deathEvent);

    public static event DamageEventHandler DamageEventHandlers;
    public static event DeathEventHandler DeathEventHandlers;

    public static void SetCurrentHitpoints(string characterId, int hitpoints) => s_characterHealthMap[characterId] = hitpoints;

    public static int GetCurrentHitpoints(string characterId) => s_characterHealthMap.GetValueOrDefault(characterId, 0);

    public static void PostDamageEvent(Character inflicter, Character recipient, int damage)
    {
        var ret = new DamageEvent
        {
            Inflicter = inflicter,
            Recipient = recipient,
            Damage = damage,
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
            Deceased = deceased,
            Killer = killer
        };
        HoverSystem.SetUnhovered(deceased.CharacterData.ResourcePath);
        DeathEventHandlers?.Invoke(ret);
    }
}

