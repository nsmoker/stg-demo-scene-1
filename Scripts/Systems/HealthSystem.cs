using ArkhamHunters.Scripts;

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
    public delegate void DamageEventHandler(DamageEvent damageEvent);
    public delegate void DeathEventHandler(DeathEvent deathEvent);

    public static DamageEventHandler DamageEventHandlers;
    public static DeathEventHandler DeathEventHandlers;

    public static void PostDamageEvent(Character inflicter, Character recipient, int damage)
    {
        recipient.CharacterData.CurrentHitpoints -= damage;
        var ret = new DamageEvent
        {
            inflicter = inflicter,
            recipient = recipient,
            damage = damage,
        };
        DamageEventHandlers?.Invoke(ret);

        if (recipient.CharacterData.CurrentHitpoints <= 0)
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
        HoverSystem.SetUnhovered(deceased.CharacterData.ResourcePath);
        DeathEventHandlers?.Invoke(ret);
    }
}
