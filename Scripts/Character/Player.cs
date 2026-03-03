using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts.Characters;

public partial class Player : Character
{
    private class NavigationState : ICharacterState
    {
        public void Process(double delta, Character character)
        {
            var player = (Player) character;

            // Handle interactable badges
            var closestInteractable = player.GetClosestInteractable();

            if (closestInteractable != player._lastBadgedInteractable)
            {
                player._lastBadgedInteractable?.SetShowBadge(false);
                closestInteractable?.SetShowBadge(true);
                player._lastBadgedInteractable = closestInteractable;
            }

            if (Input.IsActionJustPressed("Interact"))
            {
                if (closestInteractable != null)
                {
                    switch (closestInteractable.GetInteractionType())
                    {
                        case InteractionType.Dialogue:
                            {
                                var dialogueInteractable = (IDialogueInteractable) closestInteractable;
                                DialogueSystem.StartDialogue(dialogueInteractable.GetDialogue(), dialogueInteractable.GetEntryPoint());
                                break;
                            }
                        case InteractionType.Toggleable:
                            {
                                var toggle = (IToggleableInteractable) closestInteractable;
                                toggle.Toggle();
                                break;
                            }
                        case InteractionType.Furniture:
                            {
                                character.SitOn((Prop) closestInteractable);
                                break;
                            }
                    }
                }
            }

            if (Input.IsActionJustPressed("Journal"))
            {
                if (player._scene.ToggleJournalDisplay())
                {
                    player._scene.SetJournalEntries(QuestSystem.GetAllQuests());
                }
            }
        }

        public void PhysicsProcess(double delta, Character character)
        {
            var player = (Player) character;

            // Get the input direction and handle the movement.
            Vector2 direction = Input.GetVector("Move West", "Move East", "Move North", "Move South");
            player.Velocity = direction * player.CharacterData.Speed;
            player.SetWalkAnimState(player.Velocity);

            _ = player.MoveAndSlide();
        }

        public void OnTransition(Character character) { }
    }

    // Controlled by CombatController when in combat
    private class PlayerCombatPawnState : ICharacterState
    {
        public void Process(double delta, Character character) { }
        public void PhysicsProcess(double delta, Character character) { }
        public void OnTransition(Character character) { }
    }

    private Area2D _interactableRange;
    private IInteractable _lastBadgedInteractable;
    private MasterScene _scene;

    private List<IInteractable> GetInteractablesInRange() => [.. _interactableRange.GetOverlappingBodies().ToList().Where(n => n is IInteractable).Select(n => n as IInteractable)];

    private IInteractable GetClosestInteractable()
    {
        var interactables = GetInteractablesInRange();
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var interactable in interactables)
        {
            var node = (Node2D) interactable;
            var distance = GlobalPosition.DistanceTo(node.GlobalPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }

    [Export]
    private FactionTable _factionTable;
    [Export]
    public Font PathFont;

    public override void _Ready()
    {
        base._Ready();
        CombatLog.Initialize();
        FactionSystem.Initialize(_factionTable);
        _interactableRange = GetNode<Area2D>("InteractableRange");
        _senseArea = GetNode<Area2D>("SenseArea");
        foreach (Quest q in CharacterData.Journal)
        {
            QuestSystem.AddQuest(q);
        }

        ControllerState = new NavigationState();
        DialogueSystem.OnDialogueComplete += OnConversationEnded;
        DialogueSystem.OnDialogueStarted += OnConversationStarted;
        _scene = (MasterScene) GetTree().CurrentScene;
    }

    public override void OnCombatStarted(CombatStartEvent e)
    {
        if (e.participants.Contains(CharacterData.ResourcePath))
        {
            ControllerState = new PlayerCombatPawnState();
            _healthLabel.Show();
        }
    }

    public override void OnCombatJoined(CharacterData joiner)
    {
        if (joiner.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            ControllerState = new PlayerCombatPawnState();
            _healthLabel.Show();
        }
    }

    public override ICharacterState GetCombatState() => new PlayerCombatPawnState();

    public override void OnCombatEnded()
    {
        if (ControllerState is PlayerCombatPawnState or CombatNavState)
        {
            ControllerState = new NavigationState();
        }
        if (ActionPip1 != null && ActionPip2 != null)
        {
            ActionPip1.Visible = false;
            ActionPip2.Visible = false;
        }
        _healthLabel.Hide();
        QueueRedraw();
    }

    private void OnConversationEnded()
    {
        if (CombatSystem.IsInCombat(CharacterData))
        {
            ControllerState = new PlayerCombatPawnState();
        }
        else
        {
            ControllerState = new NavigationState();
        }
    }

    private void OnConversationStarted(Conversation _1, int _2)
    {
        ControllerState = new DialogueState();
        _currentAnimState = AnimState.Idle;
    }
}

