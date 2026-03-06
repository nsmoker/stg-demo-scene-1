using Godot;
using STGDemoScene1.Scripts.Characters;
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

    private static void OnDamageEvent(DamageEvent e)
    {
        var recipientHp = HealthSystem.GetCurrentHitpoints(e.Recipient.CharacterData.ResourcePath);
        GD.Print($"{e.Inflicter.Name} dealt {e.Damage} damage to {e.Recipient.Name}. {e.Recipient.Name} has {recipientHp} HP remaining out of {e.Recipient.CharacterData.MaxHitpoints}.");
    }

    private static void OnAbilityUse(AttackEvent e) => GD.Print($"{e.Attacker.Name} {(e.Hit ? "hit" : "missed")} against {e.Target.Name}.");

    private static void OnDeathEvent(DeathEvent e) => GD.Print($"{e.Deceased.Name} {(e.Killer.Name != null ? $"was killed by {e.Killer.Name}!" : "died!")}");

    private static void OnQuestUpdate(Quest quest) => GD.Print($"New journal entry: {quest.Title}: {quest.GetCurrentStage().Title}");

    private static void OnCombatStarted(CombatStartEvent e) => GD.Print($"Combat started by {e.Initiator.CharacterData.CharacterName}.");

    private static void OnCombatJoined(Character joiner) => GD.Print($"{joiner.CharacterData.CharacterName} joined combat.");

    private static void OnTurnStarted(List<Character> movingSide)
    {
        var characterNames = movingSide
            .Select(c => c.CharacterData.CharacterName)
            .ToArray();
        GD.Print($"It is now {characterNames[0]}'s turn.");
    }
}
