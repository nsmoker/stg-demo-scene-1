using Godot;
using STGDemoScene1.Addons.Edi.Scripts;

namespace STGDemoScene1.Scripts;

public partial class DialogueTriggerProp : StaticBody2D, IInteractable, IDialogueInteractable
{
    private Sprite2D _badgeSprite;
    [Export]
    private Conversation _conversation;
    [Export]
    private int _entryPoint;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => _badgeSprite = GetNode<Sprite2D>("BadgeSprite");

    public void SetShowBadge(bool showBadge) => _badgeSprite.Visible = showBadge;

    public InteractionType GetInteractionType() => InteractionType.Dialogue;

    public Conversation GetDialogue() => _conversation;

    public int GetEntryPoint() => _entryPoint;
}
