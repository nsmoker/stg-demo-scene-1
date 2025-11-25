using Godot;
using System;
using ArkhamHunters.Scripts;

public partial class Toggleable : StaticBody2D, IInteractable, IToggleableInteractable
{
	private AnimatedSprite2D _sprite;
	private Sprite2D _badgeSprite;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetCollisionLayer(1 | (1 << 20));
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite");
		_sprite.Play();
		_badgeSprite = GetNode<Sprite2D>("BadgeSprite");
	}

	public void SetShowBadge(bool showBadge)
	{
		_badgeSprite.Visible = showBadge;
	}

	public InteractionType GetInteractionType()
	{
		return InteractionType.Toggleable;
	}

	public void Toggle()
	{
		if (_sprite.Animation == "on")
		{
			_sprite.Animation = "off";
		}
		else
		{
			_sprite.Animation = "on";
		}
		_sprite.Play();
	}
}
