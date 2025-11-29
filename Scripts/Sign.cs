using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;

public partial class Sign : StaticBody2D, IDialogueInteractable, IInteractable
{
	[Export] 
	public Conversation Dialogue;

	[Export]
	public int EntryPoint;

	private Sprite2D _badgeSprite;
	
	public override void _Ready()
	{
		SetCollisionLayer(1 | (1 << 20));
		_badgeSprite = GetNode<Sprite2D>("BadgeSprite");
	}

	public Conversation GetDialogue()
	{
		return Dialogue;
	}

	public DialogueGraphNode GetEntryPoint()
	{
		var entryNode = Dialogue.EntryPoints[EntryPoint];
		// This is a hack that only works because we don't have conditions. 
		var conns = Dialogue.GetNodeConnections(Dialogue.GetIndexOfNode(entryNode));
		foreach ( var connsNode in conns)
		{
			if (connsNode.Condition == null || connsNode.Condition.Evaluate())
			{
				return connsNode;
			}
		}
        return conns[0];
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
