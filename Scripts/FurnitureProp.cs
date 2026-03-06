using Godot;

namespace STGDemoScene1.Scripts;

public partial class FurnitureProp : Prop, IInteractable
{
    private Sprite2D _badgeSprite;

    public bool Occupied { get; set; }

    public override void _Ready()
    {
        base._Ready();
        _badgeSprite = GetNode<Sprite2D>("BadgeSprite");
    }

    public InteractionType GetInteractionType() => InteractionType.Furniture;

    public void SetShowBadge(bool showBadge) => _badgeSprite.Visible = showBadge;
}
