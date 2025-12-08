using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Items;
using Container = ArkhamHunters.Scripts.Container;
using System.IO;

public partial class Player : Character
{
	private class DialogueState : ICharacterState
	{
		public void Process(double delta, Character character) { }

		public void PhysicsProcess(double delta, Character player) { }
	}

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
				player.State = new NavigationState();
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
				if (player._lastBadgedInteractable != null)
				{
					player._lastBadgedInteractable.SetShowBadge(false);
				}
				if (closestInteractable != null)
				{
					closestInteractable.SetShowBadge(true);
				}
				player._lastBadgedInteractable = closestInteractable;
			}

			if (Input.IsActionPressed("Move West"))
			{
				player.SpriteAnim.Play("walk_west");
			}
			else if (Input.IsActionPressed("Move South"))
			{
				player.SpriteAnim.Play("walk_south");
			}
			else if (Input.IsActionPressed("Move East"))
			{
				player.SpriteAnim.Play("walk_east");
			}
			else if (Input.IsActionPressed("Move North"))
			{
				player.SpriteAnim.Play("walk_north");
			}
			else
			{
				player.SpriteAnim.Pause();
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
								player._dialogueDisplay.BeginConversation(dialogueInteractable.GetDialogue(), dialogueInteractable.GetEntryPoint());
								break;
							}
						case InteractionType.Container:
							{
								player.State = new ContainerSearchState(player);
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
				_player.State = new NavigationState();
			}
		}

		public void PhysicsProcess(double delta, Character character) { }
	}

	private class CombatState : ICharacterState
	{
		private NavigationState _substate;

		public CombatState()
		{
			_substate = new NavigationState();
		}

		public void Process(double delta, Character character)
		{
			_substate.Process(delta, character);
			if (Input.IsActionJustPressed("Pause"))
			{
				character.GetTree().Paused = true;
				character.SetState(new PauseState(this));
			}
		}

		public void PhysicsProcess(double delta, Character character)
		{
			_substate.PhysicsProcess(delta, character);
		}
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
		public PlayerCombatState(Player player)
        {
			_player = player;
            player.Draw += OnPlayerDraw;
			player.QueueRedraw();
        }

		private float ComputePathLength(Vector2[] path, Vector2 origin)
        {
            Vector2 start = origin;
			float len = 0;
			foreach (var vertex in path)
            {
                len += vertex.DistanceTo(start);
				start = vertex;
            }

			return len;
        }

        public void PhysicsProcess(double delta, Character character)
        {
            if (Input.IsActionJustPressed("Combat Interact"))
            {
				var path = NavigationServer2D.MapGetPath(_player._navRegion.GetNavigationMap(), _player.Position, _player.GetGlobalMousePosition(), true, 0x1u);
				var len = ComputePathLength(path, character.GlobalPosition);
				if (len <= _player.CharacterData.MovementRange)
				{
                	_player.State = new PlayerCombatNavState(_player, path);
					_player.Draw -= OnPlayerDraw;
				}
            }
			_player.QueueRedraw();
        }

        public void Process(double delta, Character character) { }

		public void OnPlayerDraw()
        {
			_player.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(0.0f, 0.0f, 1.0f), filled: false);			

			// Draw the path to the player's hovered location.
			var path = NavigationServer2D.MapGetPath(_player._navRegion.GetNavigationMap(), _player.GlobalPosition, _player.GetGlobalMousePosition(), true, 0x1u);
			var len = ComputePathLength(path, _player.GlobalPosition);
			var inRange = len <= _player.CharacterData.MovementRange;
			var pathTransformed = path.Select(_player.ToLocal).ToArray();
			_player.DrawPolyline(pathTransformed, inRange ? new Color(1.0f, 1.0f, 1.0f) : new Color(1.0f, 0.0f, 0.0f));
        }
    }

    private class PlayerCombatNavState : ICharacterState
    {
		private Vector2[] _path;
		private int _currentPoint = 0;
		private Player _player;

		public PlayerCombatNavState(Player player, Vector2[] path)
        {
			_player = player;
			_path = path;
        }

        public void PhysicsProcess(double delta, Character character)
        {
			var targetPoint = _path[_currentPoint];
			if (_player.Position.DistanceTo(targetPoint) <= 1.0f)
            {
				_player.Position = targetPoint;
                if (_currentPoint + 1 < _path.Length)
                {
                    _currentPoint += 1;
                }
				else
                {
                    _player.SetState(new PlayerCombatState(_player));
                }
            }
			else
			{
				var targetVector = targetPoint - _player.Position;
				var vel = targetVector.Normalized() * _player.CharacterData.Speed;
				_player.Velocity = vel;
				_player.MoveAndSlide();
			}
        }

        public void Process(double delta, Character character) { }
    }

    private DialogueController _dialogueDisplay;
	private Area2D _interactableRange;
	private ContainerDisplay _containerDisplay;
	private InventoryDisplay _inventoryDisplay;
	private JournalDisplay _journalDisplay;
	private IInteractable _lastBadgedInteractable;

	private PanelContainer _mapDisplay;

	private Dictionary<Character, Action> _combatInteractions = new();

	public NavigationAgent2D NavigationAgent;

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
	private NavigationRegion2D _navRegion;

    public override void _Ready()
	{
		base._Ready();
		CombatLog.Initialize();
		FactionSystem.Initialize(_factionTable);
		HostilitySystem.HostilityChangeHandlers += OnHostilityChanged;
		SpriteAnim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_dialogueDisplay = GetNode<DialogueController>("DialogueDisplay");
		_interactableRange = GetNode<Area2D>("InteractableRange");
		_containerDisplay = GetNode<ContainerDisplay>("ContainerDisplay");
		_inventoryDisplay = GetNode<InventoryDisplay>("InventoryDisplay");
		_inventoryDisplay.OnItemSelected += OnInventorySelection;
		_journalDisplay = GetNode<JournalDisplay>("JournalDisplay");
		_mapDisplay = GetNode<PanelContainer>("MapDisplay");
		_senseArea = GetNode<Area2D>("SenseArea");

		NavigationAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");

		State = new NavigationState();
		_senseArea.BodyEntered += OnBodyEnteredSenseArea;
		_senseArea.BodyExited += OnBodyExitedSenseArea;
		_dialogueDisplay.ConversationEnded += OnConversationEnded;
		_dialogueDisplay.ConversationBegan += OnConversationStarted;
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
						_basicAttack.SetWeapon(itemToEquip);
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

	private void OnHostilityChanged(string entity1, string entity2, bool hostility)
	{
		var entity2Match = _senseArea.GetOverlappingBodies().FirstOrDefault(n => n is Character c && c.CharacterData.ResourcePath == entity1);
		if (entity2 == CharacterData.ResourcePath && entity2Match != null)
		{
			if (hostility)
			{
				var enemy = (Character) entity2Match;
                var combatMenu = enemy.GetCombatInteractionMenu();
                if (combatMenu != null)
                {
                    combatMenu.Visible = true;
                    combatMenu.SetAbilities(CharacterData.Abilities);
                    combatMenu.ProcessMode = ProcessModeEnum.Always;
                    _combatInteractions[enemy] = () =>
                    {
                        var ability = combatMenu.GetCurrentAbility();
                        if (ability != null)
                        {
                            GetTree().Paused = false;
                        }
                    };

                    combatMenu._activationButton.Pressed += _combatInteractions[enemy];
                }
            }
		}
	}

	private void OnBodyEnteredSenseArea(Node2D body)
	{
		if (body is Character character && HostilitySystem.GetHostility(character.CharacterData.ResourcePath, CharacterData.ResourcePath))
		{
			CombatSystem.BeginCombat(CharacterData, [character.CharacterData]);
		}
	}
	
	private void OnBodyExitedSenseArea(Node2D body)
	{
	}

	private void OnConversationEnded(Conversation conversation)
	{
		State = new NavigationState();
	}

	private void OnConversationStarted(Conversation conversation)
    {
        State = new DialogueState();
    }

	public DialogueController GetDialogueController()
	{
		return _dialogueDisplay;
	}

	public override void OnCombatStarted(CombatStartEvent e)
    {
        if (e.participants.Contains(CharacterData.ResourcePath))
        {
            State = new PlayerCombatState(this);
        }
    }

	public override void OnCombatJoined(CharacterData joiner)
    {
        if (joiner.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            State = new PlayerCombatState(this);
        }
    }
}
