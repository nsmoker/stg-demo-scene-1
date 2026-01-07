using Godot;
using System;

public partial class ElementalStudiesTrigger : Area2D
{
	[Export]
	public Quest IntroQuest;

	[Export]
	public CharacterData Player;

	[Export]
	public Conversation CoverHelpConversation;

	[Export]
	public Conversation ForceGetBackConversation;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body is Player && QuestSystem.TryGetQuest(IntroQuest.ResourcePath, out Quest introQuest) && introQuest.CurrentStage < 10)
		{
			CombatSystem.AttackHandlers += OnCombatAttack;
		}
	}

	public void OnBodyExited(Node2D body)
	{
		if (body is Player)
		{
			QuestSystem.TryGetQuest(IntroQuest.ResourcePath, out Quest introQuest);
			if (introQuest.CurrentStage < 10)
			{
				DialogueSystem.StartDialogue(ForceGetBackConversation, 0);
			}
			else
			{
				CombatSystem.AttackHandlers -= OnCombatAttack;
			}
		}
	}

	public void OnCombatAttack(AttackEvent e)
	{
		if (e.target.CharacterData.ResourcePath.Equals(Player.ResourcePath))
		{
			QuestSystem.TryGetQuest(IntroQuest.ResourcePath, out Quest introQuest);
			if (introQuest.CurrentStage < 3)
			{
				introQuest.CurrentStage = 3;
				DialogueSystem.StartDialogue(CoverHelpConversation, 0);
			}
		}
	}
}
