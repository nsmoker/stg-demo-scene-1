using Godot;
using STGDemoScene1.Addons.Edi.Scripts;

namespace STGDemoScene1.Scripts.Characters;

[GlobalClass]
public partial class InteractableCharacter : Character, IDialogueInteractable, IInteractable
{
    [Export]
    public Conversation Dialogue;

    [Export]
    public int EntryPoint;

    private Sprite2D _badgeSprite;

    public override void _EnterTree() => base._EnterTree();

    public override void _Ready()
    {
        base._Ready();
        SetCollisionLayer(1 | (1 << 20));
        _badgeSprite = GetNode<Sprite2D>("BadgeSprite");
    }

    public Conversation GetDialogue() => Dialogue;

    public int GetEntryPoint() => EntryPoint;

    public void SetShowBadge(bool showBadge) => _badgeSprite.Visible = showBadge;

    public InteractionType GetInteractionType() => InteractionType.Dialogue;
}

