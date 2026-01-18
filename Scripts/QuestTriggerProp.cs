using ArkhamHunters.Scripts;
using Godot;

public partial class QuestTriggerProp : Prop, ITriggerInteractable, IInteractable
{
    [Export]
    public Quest QuestToTrigger;

    [Export]
    public int Stage;

    public void Trigger()
    {
        QuestSystem.SetQuestStage(QuestToTrigger.ResourcePath, Stage);
        QueueFree();
    }

    private Sprite2D _badgeSprite;

    public override void _Ready()
    {
        base._Ready();
        _badgeSprite = GetNode<Sprite2D>("BadgeSprite");
    }

    public InteractionType GetInteractionType()
    {
        return InteractionType.Trigger;
    }

    public void SetShowBadge(bool showBadge)
    {
        _badgeSprite.Visible = showBadge;
    }
}