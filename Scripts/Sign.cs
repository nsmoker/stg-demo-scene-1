using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;

public partial class Sign : StaticBody2D, IDialogueInteractable, IInteractable
{
	[Export] 
	public Godot.Collections.Array<string> Text;

	private Sprite2D _badgeSprite;
	
	public override void _Ready()
	{
		SetCollisionLayer(1 | (1 << 20));
		_badgeSprite = GetNode<Sprite2D>("BadgeSprite");
	}

	public DialogueGraph GetDialogue()
	{
		return new DialogueGraph(Text.ToList(), new List<DialogueChoice>());
	}

	public void SetShowBadge(bool showBadge)
	{
		_badgeSprite.Visible = showBadge;
	}

	public InteractionType GetInteractionType()
	{
		return InteractionType.Dialogue;
	}
}
