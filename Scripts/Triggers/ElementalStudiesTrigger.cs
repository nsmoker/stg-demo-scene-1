using Godot;
using System;

public partial class ElementalStudiesTrigger : Area2D
{
	[Export]
	public Quest IntroQuest;

	[Export]
	public CharacterData Player;

	[Export]
	public Conversation IntroConversation;

	[Export]
	public int NudgeEntry;

	[Export]
	public Conversation CoverHelpConversation;

	[Export]
	public Conversation ForceGetBackConversation;

	[Export]
	public Conversation DeathConversation;

	[Export]
	CollisionShape2D _collider;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		HealthSystem.DeathEventHandlers += OnCharacterDeath;
		_collider = GetNode<CollisionShape2D>("CollisionShape2D");
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body is Player && QuestSystem.TryGetQuest(IntroQuest.ResourcePath, out Quest introQuest) && introQuest.CurrentStage < 2)
		{
			introQuest.CurrentStage = 2;
			CombatSystem.AttackHandlers += OnCombatAttack;
			DialogueSystem.StartDialogue(IntroConversation, NudgeEntry);
		}
	}

	public void OnBodyExited(Node2D body)
	{
		if (body is Player && !_collider.Shape.GetRect().HasPoint(body.GlobalPosition))
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

	public void OnCharacterDeath(DeathEvent e)
	{
		DialogueSystem.StartDialogue(DeathConversation, 0);
	}
}
