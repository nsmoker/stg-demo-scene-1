using ArkhamHunters.Scripts;
using Godot;

public partial class DialogueTriggerProp : StaticBody2D, IInteractable, IDialogueInteractable
{
    private Sprite2D _badgeSprite;
    [Export]
    private Conversation _conversation;
    [Export]
    private int _entryPoint;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SetCollisionLayer(1 | (1 << 20));
        _badgeSprite = GetNode<Sprite2D>("BadgeSprite");
    }

    public void SetShowBadge(bool showBadge)
    {
        _badgeSprite.Visible = showBadge;
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Dialogue;
    }

    public Conversation GetDialogue()
    {
        return _conversation;
    }

    public DialogueGraphNode GetEntryPoint()
    {
        return _conversation.EntryPoints[_entryPoint];
    }
}