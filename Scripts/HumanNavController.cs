using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts;

public partial class HumanNavController : Node
{
    public Character Pawn;

    public override void _Process(double delta)
    {
        if (!(CombatSystem.IsInCombat(Pawn) || DialogueSystem.IsInDialogue()))
        {
            var closestInteractable = Pawn.GetClosestInteractable();
            if (Input.IsActionJustPressed("Interact") && closestInteractable != null)
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
                            Pawn.SitOn((Prop) closestInteractable);
                            break;
                        }
                }
            }

            if (Input.IsActionJustPressed("Journal"))
            {
                _ = SceneSystem.GetMasterScene().ToggleJournalDisplay();
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!(CombatSystem.IsInCombat(Pawn) || DialogueSystem.IsInDialogue()))
        {
            // Get the input direction and handle the movement.
            Vector2 direction = Input.GetVector("Move West", "Move East", "Move North", "Move South");
            Pawn.Velocity = direction * Pawn.CharacterData.Speed;
            Pawn.SetWalkAnimState(Pawn.Velocity);

            _ = Pawn.MoveAndSlide();
        }
    }
}
