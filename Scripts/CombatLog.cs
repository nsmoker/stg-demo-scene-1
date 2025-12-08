using Godot;
using System;
using System.Linq;

public static class CombatLog
{
    public static void Initialize()
    {
        HealthSystem.DamageEventHandlers += OnDamageEvent;
        HealthSystem.DeathEventHandlers += OnDeathEvent;
        CombatSystem.abilityEventHandler += OnAbilityUse;
        CombatSystem.characterJoinedCombatHandler += OnCombatJoined;
        CombatSystem.combatStartHandler += OnCombatStarted;
        QuestSystem.OnQuestUpdated += OnQuestUpdate;
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

    public static void OnQuestUpdate(Quest quest)
    {
        GD.Print($"New journal entry: {quest.Title}: {quest.GetCurrentStage().Title}");
    }

    public static void OnCombatStarted(CombatStartEvent e)
    {
        GD.Print($"Combat started by {e.initiator.CharacterName}.");
    }

    public static void OnCombatJoined(CharacterData joiner)
    {
        GD.Print($"{joiner.CharacterName} joined combat.");
    }
}
