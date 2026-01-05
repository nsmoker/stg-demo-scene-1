using Godot;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Items;
using Container = ArkhamHunters.Scripts.Container;

public partial class Player : Character
{
	private class InventoryState : ICharacterState
	{
		public InventoryState(Player player)
		{
			player._inventoryDisplay.CurrentEntity = player.CharacterData.ResourcePath;
			player._inventoryDisplay.Visible = true;
			player._inventoryDisplay.EquipmentId = player.CharacterData.ResourcePath;
		}
		public void Process(double delta, Character character)
		{
			var player = (Player)character;
			if (Input.IsActionJustPressed("Open Inventory"))
			{
				player._inventoryDisplay.Visible = false;
				player.ControllerState = new NavigationState();
                EquipmentSystem.RetrieveEquipment(player.CharacterData.ResourcePath, out EquipmentSet eq);
			}
		}

		public void PhysicsProcess(double delta, Character player) { }
	}

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
						case InteractionType.Container:
							{
								player.ControllerState = new ContainerSearchState(player);
								break;
							}
						case InteractionType.Toggleable:
							{
								var toggle = (IToggleableInteractable)closestInteractable;
								toggle.Toggle();
								break;
							}
					}
				}
			}

            if (Input.IsActionJustPressed("Pause"))
            {
                character.GetTree().Paused = true;
				player.SetState(new PauseState(this));
            }

            if (Input.IsActionJustPressed("Open Inventory"))
			{
				player.SetState(new InventoryState(player));
			}

			if (Input.IsActionJustPressed("Journal"))
			{
				player._journalDisplay.Visible = !player._journalDisplay.Visible;
				if (player._journalDisplay.Visible)
				{
					player._journalDisplay.SetQuestEntries(QuestSystem.GetAllQuests());
				}
			}

			if (Input.IsActionJustPressed("Map"))
            {
                player._mapDisplay.Visible = !player._mapDisplay.Visible;
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
	}

	private class ContainerSearchState : ICharacterState
	{
		private readonly Container _container;
		private Player _player;

		private void OnContainerItemSelect(Item item)
		{
			InventorySystem.Transfer(_container.ContainerData.ResourcePath, _player.CharacterData.ResourcePath, item);
		}

		public ContainerSearchState(Character character)
		{
			_player = (Player)character;
			_container = (Container)_player.GetClosestInteractable();
			_player._containerDisplay.ContainerEntity = _container.ContainerData.ResourcePath;
			_player._containerDisplay.OnItemSelected += OnContainerItemSelect;
		}

		public void Process(double delta, Character character)
		{
			_player = (Player)character;
			var closeRequested = _player._containerDisplay.GetAllPressed() || _player._containerDisplay.ClosePressed();
			if (closeRequested)
			{
				if (_player._containerDisplay.GetAllPressed())
				{
					var containerItems = InventorySystem.RetrieveInventory(_container.ContainerData.ResourcePath);
					foreach (var item in containerItems)
					{
						InventorySystem.Transfer(_container.ContainerData.ResourcePath, _player.CharacterData.ResourcePath, item);
					}
				}

				_player._containerDisplay.Visible = false;
				_player._containerDisplay.OnItemSelected -= OnContainerItemSelect;
				_player._containerDisplay.ContainerEntity = "";
				_player.ControllerState = new NavigationState();
			}
		}

		public void PhysicsProcess(double delta, Character character) { }
	}
		
	private class PauseState: ICharacterState
	{
		private ICharacterState _resumeState;
        public PauseState(ICharacterState resumeState)
        {
			_resumeState = resumeState;
        }

        public void Process(double delta, Character character)
        {
            if (Input.IsActionJustPressed("Pause"))
            {
                character.GetTree().Paused = false;
                character.SetState(_resumeState);
            }
        }

        public void PhysicsProcess(double delta, Character character)
        {
        }
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
			_player._combatStatusLabel.Visible = true;
            player.QueueRedraw();
        }

        public void PhysicsProcess(double delta, Character character)
        {
			if (Input.IsActionJustPressed("Combat Interact") && CombatSystem.NavReady() && _isOurTurn)
			{
				if (HoverSystem.AnyHovered())
				{
					CombatSystem.AttemptAttack(character.CharacterData, CharacterSystem.GetInstance(HoverSystem.Hovered).CharacterData);
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
						CombatSystem.TurnHandlers -= OnTurnBegin;
						_player.Draw -= OnPlayerDraw;
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
			_player._combatStatusLabel.Text = _isOurTurn ? "YOUR TURN" : "ENEMY TURN";
			_player.QueueRedraw();
        }
    }

	private Area2D _interactableRange;
	private ContainerDisplay _containerDisplay;
	private InventoryDisplay _inventoryDisplay;
	private JournalDisplay _journalDisplay;
	private IInteractable _lastBadgedInteractable;

	private PanelContainer _mapDisplay;

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

	private Label _combatStatusLabel;

    public override void _Ready()
	{
		base._Ready();
		CombatLog.Initialize();
		FactionSystem.Initialize(_factionTable);
		_interactableRange = GetNode<Area2D>("InteractableRange");
		_containerDisplay = GetNode<ContainerDisplay>("ContainerDisplay");
		_inventoryDisplay = GetNode<InventoryDisplay>("InventoryDisplay");
		_inventoryDisplay.OnItemSelected += OnInventorySelection;
		_journalDisplay = GetNode<JournalDisplay>("JournalDisplay");
		_mapDisplay = GetNode<PanelContainer>("MapDisplay");
		_senseArea = GetNode<Area2D>("SenseArea");
		_combatStatusLabel = GetNode<Label>("CombatStatusLabel");

		ControllerState = new NavigationState();
		_senseArea.BodyExited += OnBodyExitedSenseArea;
		DialogueSystem.OnDialogueComplete += OnConversationEnded;
		DialogueSystem.OnDialogueStarted += OnConversationStarted;
	}

	private void OnInventorySelection(Item item)
	{
		var itemToEquip = item.Equipped ? Item.NoneItem() : item;
		var eq = GetEquipmentSet();
		if (MeetsEquipRequirements(itemToEquip))
		{
			switch (item.ItemType)
			{
				case ItemType.Weapon:
					{
						eq.Weapon = itemToEquip;
						break;
					}
				case ItemType.Armor:
					{
						eq.Armor = itemToEquip;
						break;
					}
				case ItemType.Wearable:
					{
						eq.Helmet = itemToEquip;
						break;
					}
				default:
					{
						break;
					}
			}

			EquipmentSystem.SetEquipment(CharacterData.ResourcePath, eq);
			_inventoryDisplay.CurrentEntity = CharacterData.ResourcePath;
		}
	}
	
	private void OnBodyExitedSenseArea(Node2D body)
	{
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
		_combatStatusLabel.Visible = false;
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
    }
}
