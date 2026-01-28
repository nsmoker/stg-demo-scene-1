using ArkhamHunters.Scripts;
using Godot;
using System;

public partial class FurnitureProp : Prop, IInteractable
{
    private Sprite2D _badgeSprite;

    public bool Occupied { get; set; } = false;

    public override void _Ready()
    {
        base._Ready();
        _badgeSprite = GetNode<Sprite2D>("BadgeSprite");
    }

    public InteractionType GetInteractionType() => InteractionType.Furniture;

    public void SetShowBadge(bool showBadge) => _badgeSprite.Visible = showBadge;
}
