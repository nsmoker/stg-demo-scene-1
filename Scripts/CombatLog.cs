using Godot;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts;

public static class CombatLog
{
    public static void Initialize()
    {
        HealthSystem.DamageEventHandlers += OnDamageEvent;
        HealthSystem.DeathEventHandlers += OnDeathEvent;
        CombatSystem.AttackHandlers += OnAbilityUse;
        CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatStartHandlers += OnCombatStarted;
        CombatSystem.TurnHandlers += OnTurnStarted;
        QuestSystem.OnQuestUpdated += OnQuestUpdate;
    }

    public static void OnDamageEvent(DamageEvent e)
    {
        var recipientHp = HealthSystem.GetCurrentHitpoints(e.recipient.CharacterData.ResourcePath);
        GD.Print($"{e.inflicter.Name} dealt {e.damage} damage to {e.recipient.Name}. {e.recipient.Name} has {recipientHp} HP remaining out of {e.recipient.CharacterData.MaxHitpoints}.");
    }

    public static void OnAbilityUse(AttackEvent e) => GD.Print($"{e.attacker.Name} {(e.hit ? "hit" : "missed")} against {e.target.Name}.");

    public static void OnDeathEvent(DeathEvent e) => GD.Print($"{e.deceased.Name} {(e.killer.Name != null ? $"was killed by {e.killer.Name}!" : "died!")}");

    public static void OnQuestUpdate(Quest quest) => GD.Print($"New journal entry: {quest.Title}: {quest.GetCurrentStage().Title}");

    public static void OnCombatStarted(CombatStartEvent e) => GD.Print($"Combat started by {e.initiator.CharacterName}.");

    public static void OnCombatJoined(CharacterData joiner) => GD.Print($"{joiner.CharacterName} joined combat.");

    public static void OnTurnStarted(List<string> movingSide)
    {
        var characterNames = movingSide
            .Select(path => ResourceLoader.Load<CharacterData>(path).CharacterName)
            .ToArray();
        GD.Print($"It is now {characterNames[0]}'s turn.");
    }
}
