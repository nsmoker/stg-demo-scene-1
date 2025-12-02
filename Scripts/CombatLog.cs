using Godot;
using System;

public static class CombatLog
{
    public static void Initialize()
    {
        HealthSystem.DamageEventHandlers += OnDamageEvent;
        HealthSystem.DeathEventHandlers += OnDeathEvent;
        CombatSystem.abilityEventHandler += OnAbilityUse;
    }

    public static void OnDamageEvent(DamageEvent e)
    {
        GD.Print($"{e.inflicter.Name} dealt {e.damage} damage to {e.recipient.Name} with {e.ability.Name}. {e.recipient.Name} has {e.recipient.CharacterData.CurrentHitpoints} HP remaining out of {e.recipient.CharacterData.MaxHitpoints}.");
    }

    public static void OnAbilityUse(AbilityUseEvent e) 
    {
        GD.Print($"{e.attacker.Name} {(e.hit ? "hit" : "missed")} with {e.ability.Name} against {e.target.Name}.");
    }

    public static void OnDeathEvent(DeathEvent e) 
    {
        GD.Print($"{e.deceased.Name} {(e.killer.Name != null ? $"was killed by {e.killer.Name}!" : "died!")}");
    }
}
