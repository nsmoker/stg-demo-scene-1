using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Items;
using Container = ArkhamHunters.Scripts.Container;
using ArkhamHunters.Scripts.Abilities;
using System.Diagnostics;

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
			player._inventoryDisplay.CurrentEntity = player.GetInstanceId();
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
			InventorySystem.Transfer(_container.GetInstanceId(), _player.GetInstanceId(), item);
		}

		public ContainerSearchState(Character character)
		{
			_player = (Player)character;
			_container = (Container)_player.GetClosestInteractable();
			_player._containerDisplay.ContainerEntity = _container.GetInstanceId();
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
					var containerItems = InventorySystem.RetrieveInventory(_container.GetInstanceId());
					foreach (var item in containerItems)
					{
						InventorySystem.Transfer(_container.GetInstanceId(), _player.GetInstanceId(), item);
					}
				}

				_player._containerDisplay.Visible = false;
				_player._containerDisplay.OnItemSelected -= OnContainerItemSelect;
				_player._containerDisplay.ContainerEntity = 0;
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

	private class PursuitState : ICharacterState
	{
		private Character _target;
		private Ability _ability;

		public PursuitState(NavigationAgent2D agent, Character target, Ability ability)
		{
			_target = target;
			_ability = ability;
			var player = agent.GetParent() as Player;

            agent.VelocityComputed += (safeVelocity) =>
			{
				OnVelocityComputed(player, safeVelocity.LimitLength(player.CharacterData.Speed));
			};

			agent.TargetPosition = target.GetClosestOnCollSurface(player.Position);
			agent.TargetDesiredDistance = ability.Range; 
		}

		public void Process(double delta, Character character)
		{
		}

		public void PhysicsProcess(double delta, Character character)
		{
			var player = (Player)character;
			// Do not query when the map has never synchronized and is empty.
			if (NavigationServer2D.MapGetIterationId(player._navigationAgent.GetNavigationMap()) == 0)
			{
				return;
			}

			if (player._navigationAgent.IsNavigationFinished())
			{
				if (_ability != null)
				{
					player.State = new AttackState(_target, _ability);
				}
				return;
			}

			Vector2 nextPathPosition = player._navigationAgent.GetNextPathPosition();
			Vector2 newVelocity = player.GlobalPosition.DirectionTo(nextPathPosition) * player.CharacterData.Speed;
			if (player._navigationAgent.AvoidanceEnabled)
			{
				player._navigationAgent.Velocity = newVelocity;
			}
			else
			{
				OnVelocityComputed(player, newVelocity);
			}
		}

		private void OnVelocityComputed(Player player, Vector2 safeVelocity)
		{
			player.Velocity = safeVelocity;
			player.MoveAndSlide();
		}
    }
		
	private class AttackState : ICharacterState
	{
		private readonly Character _target;
		private readonly Ability _ability;

		public AttackState(Character target, Ability ability)
		{
			_target = target;
			_ability = ability;
		}

		public void Process(double delta, Character character)
		{

		}

		public void PhysicsProcess(double delta, Character character)
		{
			var player = (Player) character;
			var distance = player.GlobalPosition.DistanceTo(_target.GetClosestOnCollSurface(player.Position));
			if (distance > _ability.Range)
			{
				player.State = new PursuitState(player._navigationAgent, _target, _ability);
			}
			else
			{
				CombatSystem.UseAbility(_ability, player, _target);
				player.State = new CombatState();
			}
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
	private IInteractable _lastBadgedInteractable;
	private Area2D _senseArea;

	private BasicAttack _basicAttack;

	[Export]
	private Godot.Collections.Array<Ability> _abilities = new();

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
		_senseArea = GetNode<Area2D>("SenseArea");

		_basicAttack = new BasicAttack();
		_abilities = new Godot.Collections.Array<Ability>
		{
			_basicAttack
		};

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
			_inventoryDisplay.CurrentEntity = GetInstanceId();
		}
	}

	private void OnHostilityChanged(ulong entity1, ulong entity2, bool hostility)
	{
		if (entity2 == GetInstanceId() && _senseArea.GetOverlappingBodies().Count(n => n.GetInstanceId() == entity1) > 0)
		{
			if (hostility)
			{
				var enemy = (Character) InstanceFromId(entity1);
                var combatMenu = enemy.GetCombatInteractionMenu();
                if (combatMenu != null)
                {
                    combatMenu.Visible = true;
                    combatMenu.SetAbilities(_abilities);
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
		if (body is Character character && HostilitySystem.GetHostility(character.GetInstanceId(), GetInstanceId()))
		{
			var combatMenu = character.GetCombatInteractionMenu();
			if (combatMenu != null)
			{
				combatMenu.Visible = true;
				combatMenu.SetAbilities(_abilities);
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
		if (body is Character character && HostilitySystem.GetHostility(character.GetInstanceId(), GetInstanceId()))
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
