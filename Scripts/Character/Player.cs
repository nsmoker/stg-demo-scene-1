using Godot;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;

public partial class Player : Character
{
	private class NavigationState : ICharacterState
	{
		public void Process(double delta, Character character)
		{
			var player = (Player)character;

			// Handle interactable badges
			var closestInteractable = player.GetClosestInteractable();

			if (closestInteractable != player._lastBadgedInteractable)
			{
				player._lastBadgedInteractable?.SetShowBadge(false);
				closestInteractable?.SetShowBadge(true);
				player._lastBadgedInteractable = closestInteractable;
			}

			if (Input.IsActionJustPressed("Interact"))
			{
				if (closestInteractable != null)
				{
					switch (closestInteractable.GetInteractionType())
					{
						case InteractionType.Dialogue:
							{
								var dialogueInteractable = (IDialogueInteractable)closestInteractable;
								DialogueSystem.StartDialogue(dialogueInteractable.GetDialogue(), dialogueInteractable.GetEntryPoint());
								break;
							}
						case InteractionType.Toggleable:
							{
								var toggle = (IToggleableInteractable)closestInteractable;
								toggle.Toggle();
								break;
							}
						case InteractionType.Furniture:
							{
								character.SitOn((Prop) closestInteractable);
								break;
							}
						case InteractionType.Trigger:
							{
								var trigger = (ITriggerInteractable)closestInteractable;
								trigger.Trigger();
								break;
							}
					}
				}
			}

			if (Input.IsActionJustPressed("Journal"))
			{
				if (player._scene.ToggleJournalDisplay())
				{
					player._scene.SetJournalEntries(QuestSystem.GetAllQuests());
				}
			}
		}

		public void PhysicsProcess(double delta, Character character)
		{
			var player = (Player)character;

			// Get the input direction and handle the movement.
			Vector2 direction = Input.GetVector("Move West", "Move East", "Move North", "Move South");
			player.Velocity = direction * player.CharacterData.Speed;
			player.SetWalkAnimState(player.Velocity);

			player.MoveAndSlide();
		}

        public void OnTransition(Character character) { }
    }

    private class PlayerCombatState : ICharacterState
    {
		private Player _player;
		private bool _isOurTurn;
		public PlayerCombatState(Player player)
        {
			_player = player;
            player.Draw += OnPlayerDraw;
			CombatSystem.TurnHandlers += OnTurnBegin;
			_isOurTurn = CombatSystem.GetMovingSide().Contains(_player.CharacterData.ResourcePath);
            player.UpdateCoverState(player.GetWorld2D().DirectSpaceState);
			_player.ActionPip1.Visible = true;
			_player.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_player.CharacterData) > 1;
            player.QueueRedraw();
        }

        public void PhysicsProcess(double delta, Character character)
        {
			if (Input.IsActionJustPressed("Combat Interact") && CombatSystem.NavReady() && _isOurTurn)
			{
				if (HoverSystem.AnyHovered())
				{
					character.SetAttackTarget(ResourceLoader.Load<CharacterData>(HoverSystem.Hovered));
					character.SetAnimState(AnimState.Attack);
                }
				else
				{
					var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), _player.GlobalPosition, _player.GetGlobalMousePosition(), true, 0x1u);
					var len = ComputePathLength(path, character.GlobalPosition);
					if (len <= _player.CharacterData.MovementRange)
					{
						if (path.Length > 0)
						{
							_player.ControllerState = new CombatNavState(_player, path);
						}
						else
						{
							_player.ControllerState = new CombatNavState(_player, [_player.GetGlobalMousePosition()]);
						}
					}
				}
			}
            _player.QueueRedraw();
        }

        public void Process(double delta, Character character) { }

		public void OnPlayerDraw()
        {
			if (_isOurTurn && !HoverSystem.AnyHovered())
			{
				_player.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(0.0f, 0.0f, 1.0f), filled: false);			
				// Draw the path to the player's hovered location.
				if (CombatSystem.NavReady())
				{
					var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), _player.GlobalPosition, _player.GetGlobalMousePosition(), true, 0x1u);
					var len = ComputePathLength(path, _player.GlobalPosition);
					var inRange = len <= _player.CharacterData.MovementRange;
					var pathTransformed = path.Select(_player.ToLocal).ToArray();
					float dist = path.Length > 1 ? len / 16.0f : _player.GlobalPosition.DistanceTo(_player.GetGlobalMousePosition()) / 16.0f;
					var targetPoint = pathTransformed.Length > 1 ? pathTransformed[pathTransformed.Length - 1] : _player.GetLocalMousePosition();
					if (pathTransformed.Length > 1) {
						_player.DrawPolyline(pathTransformed, inRange ? new Color(1.0f, 1.0f, 1.0f) : new Color(1.0f, 0.0f, 0.0f));
					} else
					{
						_player.DrawLine(_player.ToLocal(_player.GlobalPosition), _player.GetLocalMousePosition(), inRange ? new Color(1.0f, 1.0f, 1.0f) : new Color(1.0f, 0.0f, 0.0f));
					}
					_player.DrawString(_player._pathFont, targetPoint, $"{dist:0.00}m", fontSize: 8);
				}
			}

			if (HoverSystem.AnyHovered())
			{
				var hovered = CharacterSystem.GetInstance(HoverSystem.Hovered);
                var chance = CombatSystem.ComputeToHitChance(_player.CharacterData, hovered.CharacterData) * 100.0f;
                _player.DrawString(_player.ToHitFont, new Vector2(12.0f, 0.0f) + _player.GetLocalMousePosition(), $"{chance:0}%", fontSize: 8);
            }
        }

		public void OnTurnBegin(List<string> sideMoving)
        {
            _isOurTurn = sideMoving.Contains(_player.CharacterData.ResourcePath);
			if (_isOurTurn)
			{
				_player.ActionPip1.Visible = true;
				_player.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_player.CharacterData) > 1;
			}
			else
			{
				_player.ActionPip1.Hide();
				_player.ActionPip2.Hide();
			}
			_player.QueueRedraw();
        }

        public void OnTransition(Character character)
        {
            character.Draw -= OnPlayerDraw;
            CombatSystem.TurnHandlers -= OnTurnBegin;
            _player.ActionPip1.Visible = false;
            _player.ActionPip2.Visible = false;
            _player.QueueRedraw();
        }
    }

	private Area2D _interactableRange;
	private IInteractable _lastBadgedInteractable;
	private MasterScene _scene;

	private List<IInteractable> GetInteractablesInRange()
	{
		return _interactableRange.GetOverlappingBodies().ToList().Where(n => n is IInteractable).Select(n => n as IInteractable).ToList();
	}

	private IInteractable GetClosestInteractable()
	{
		var interactables = GetInteractablesInRange();
		IInteractable closestInteractable = null;
		float closestDistance = float.MaxValue;

		foreach (var interactable in interactables)
		{
			var node = (Node2D)interactable;
			var distance = GlobalPosition.DistanceTo(node.GlobalPosition);
			if (distance < closestDistance)
			{
				closestDistance = distance;
				closestInteractable = interactable;
			}
		}

		return closestInteractable;
	}

	[Export]
	private FactionTable _factionTable;
	[Export]
	private Font _pathFont;

    public override void _Ready()
	{
		base._Ready();
		CombatLog.Initialize();
		FactionSystem.Initialize(_factionTable);
		_interactableRange = GetNode<Area2D>("InteractableRange");
		_senseArea = GetNode<Area2D>("SenseArea");
		foreach (Quest q in CharacterData.Journal)
		{
			QuestSystem.AddQuest(q);
		}

		ControllerState = new NavigationState();
		DialogueSystem.OnDialogueComplete += OnConversationEnded;
		DialogueSystem.OnDialogueStarted += OnConversationStarted;
		_scene = (MasterScene) (GetTree().CurrentScene);
	}

	public override void OnCombatStarted(CombatStartEvent e)
    {
        if (e.participants.Contains(CharacterData.ResourcePath))
        {
            ControllerState = new PlayerCombatState(this);
			_healthLabel.Show();
        }
    }

	public override void OnCombatJoined(CharacterData joiner)
    {
        if (joiner.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            ControllerState = new PlayerCombatState(this);
			_healthLabel.Show();
        }
    }

    public override ICharacterState GetCombatState()
    {
        return new PlayerCombatState(this);
    }

    public override void OnCombatEnded()
    {
        if (ControllerState is PlayerCombatState || ControllerState is CombatNavState)
		{
			ControllerState = new NavigationState();
		}
		if (ActionPip1 != null && ActionPip2 != null)
        {
            ActionPip1.Visible = false;
            ActionPip2.Visible = false;
        }
		_healthLabel.Hide();
        QueueRedraw();
    }

	private void OnConversationEnded()
	{
		if (CombatSystem.IsInCombat(CharacterData))
		{
			ControllerState = new PlayerCombatState(this);
		}
		else
		{
			ControllerState = new NavigationState();
		}
	}

	private void OnConversationStarted(Conversation conversation, int entryPoint)
    {
        ControllerState = new DialogueState();
		_currentAnimState = AnimState.Idle;
    }
}
