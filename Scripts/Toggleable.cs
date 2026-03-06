using Godot;

namespace STGDemoScene1.Scripts;

public partial class Toggleable : StaticBody2D, IInteractable, IToggleableInteractable
{
    private AnimatedSprite2D _sprite;
    private Sprite2D _badgeSprite;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite");
        _sprite.Play();
        _badgeSprite = GetNode<Sprite2D>("BadgeSprite");
    }

    public void SetShowBadge(bool showBadge) => _badgeSprite.Visible = showBadge;

    public InteractionType GetInteractionType() => InteractionType.Toggleable;

    public void Toggle()
    {
        _sprite.Animation = _sprite.Animation == "on" ? "off" : "on";
        _sprite.Play();
    }
}

