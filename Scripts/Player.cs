using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Items;
using Container = ArkhamHunters.Scripts.Container;

public partial class Player : Character
{
	private class DialogueState : ICharacterState
	{
		public DialogueState(IDialogueInteractable dialogueSource, DialogueController display)
		{
            display.BeginConversation(dialogueSource.GetDialogue(), dialogueSource.GetEntryPoint());
		}

		public void Process(double delta, Character character)
		{
		}

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
								player.State = new DialogueState((IDialogueInteractable)closestInteractable, player._dialogueDisplay);
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

	private DialogueController _dialogueDisplay;
	private Area2D _interactableRange;
	private ContainerDisplay _containerDisplay;
	private InventoryDisplay _inventoryDisplay;
	private JournalDisplay _journalDisplay;
	private IInteractable _lastBadgedInteractable;
	private Area2D _senseArea;

	private Dictionary<Character, Action> _combatInteractions = new();

	private NavigationAgent2D _navigationAgent;

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
	public Quest StartingQuest;

    public override void _Ready()
	{
		base._Ready();
		CombatLog.Initialize();
		FactionSystem.Initialize(_factionTable);
		HostilitySystem.HostilityChangeHandlers += OnHostilityChanged;
		QuestSystem.AddQuest(StartingQuest);
		SpriteAnim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_dialogueDisplay = GetNode<DialogueController>("DialogueDisplay");
		_interactableRange = GetNode<Area2D>("InteractableRange");
		_containerDisplay = GetNode<ContainerDisplay>("ContainerDisplay");
		_inventoryDisplay = GetNode<InventoryDisplay>("InventoryDisplay");
		_inventoryDisplay.OnItemSelected += OnInventorySelection;
		_journalDisplay = GetNode<JournalDisplay>("JournalDisplay");
		_senseArea = GetNode<Area2D>("SenseArea");

		_navigationAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");

		State = new NavigationState();
		_senseArea.BodyEntered += OnBodyEnteredSenseArea;
		_senseArea.BodyExited += OnBodyExitedSenseArea;
		_dialogueDisplay.ConversationEnded += OnConversationEnded;
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
                            SetState(new AttackState(enemy, ability));
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
			var combatMenu = character.GetCombatInteractionMenu();
			if (combatMenu != null)
			{
				combatMenu.Visible = true;
				combatMenu.SetAbilities(CharacterData.Abilities);
				combatMenu.ProcessMode = ProcessModeEnum.Always;
				_combatInteractions[character] = () =>
				{
					var ability = combatMenu.GetCurrentAbility();
					if (ability != null)
					{
						GetTree().Paused = false;
						SetState(new AttackState(character, ability));
					}
				};

				combatMenu._activationButton.Pressed += _combatInteractions[character];
			}
		}
	}
	
	private void OnBodyExitedSenseArea(Node2D body)
	{
		if (body is Character character && HostilitySystem.GetHostility(character.CharacterData.ResourcePath, CharacterData.ResourcePath))
		{
			var combatMenu = character.GetCombatInteractionMenu();
			if (combatMenu != null)
			{
				combatMenu.Visible = false;
				combatMenu.SetAbilities([]);
				combatMenu._activationButton.Pressed -= _combatInteractions[character];
				_combatInteractions.Remove(character);
			}
		}
	}

	private void OnConversationEnded(Conversation conversation)
	{
		State = new NavigationState();
	}
}
