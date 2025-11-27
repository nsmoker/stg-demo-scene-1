using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Abilities;

public struct DamageEvent
{
    public Character inflicter;
    public Character recipient;
    public int damage;
    public Ability ability;
}

public struct DeathEvent
{
    public Character deceased;
    public Character killer;
}

public static class HealthSystem
{
    public delegate void DamageEventHandler(DamageEvent damageEvent);
    public delegate void DeathEventHandler(DeathEvent deathEvent);

    public static DamageEventHandler DamageEventHandlers;
    public static DeathEventHandler DeathEventHandlers;

    public static void PostDamageEvent(Character inflicter, Character recipient, int damage, Ability ability)
    {
        recipient.CurrentHitpoints -= damage;
        var ret = new DamageEvent
        {
            inflicter = inflicter,
            recipient = recipient,
            damage = damage,
            ability = ability
        };
        DamageEventHandlers?.Invoke(ret);

        if (recipient.CurrentHitpoints <= 0)
        {
            PostDeathEvent(recipient, inflicter);
        }
    }

    public static void PostDeathEvent(Character deceased, Character killer)
    {
        deceased.QueueFree();
        var ret = new DeathEvent
        {
            deceased = deceased,
            killer = killer
        };
        DeathEventHandlers?.Invoke(ret);
    }
}
