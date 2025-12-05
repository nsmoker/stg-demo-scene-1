using System;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts.Abilities;
using Godot;

namespace ArkhamHunters.Scripts;

[GlobalClass]
public partial class Character : CharacterBody2D
{
    public CollisionShape2D collider;
    protected AnimatedSprite2D SpriteAnim;
    [Export]
    private Godot.Collections.Array<Item> InitialInventory = new();

    [Export]
    public CharacterData CharacterData;

    public int Strength => CharacterData.BaseAttributes.Strength + GetEquipmentSet().ComputeAttributeBonus().StrengthBonus;
    public int Endurance => CharacterData.BaseAttributes.Endurance + GetEquipmentSet().ComputeAttributeBonus().EnduranceBonus;
    public int Dexterity => CharacterData.BaseAttributes.Dexterity + GetEquipmentSet().ComputeAttributeBonus().DexterityBonus;
    public int Intelligence => CharacterData.BaseAttributes.Intelligence + GetEquipmentSet().ComputeAttributeBonus().IntelligenceBonus;
    public int Wisdom => CharacterData.BaseAttributes.Wisdom + GetEquipmentSet().ComputeAttributeBonus().WisdomBonus;
    public int Charisma => CharacterData.BaseAttributes.Charisma + GetEquipmentSet().ComputeAttributeBonus().CharismaBonus;
    public int Willpower => CharacterData.BaseAttributes.Willpower + GetEquipmentSet().ComputeAttributeBonus().WillpowerBonus;

    public AttributeSet FinalAttributes => new AttributeSet()
    {
        Strength = Strength,
        Endurance = Endurance,
        Dexterity = Dexterity,
        Intelligence = Intelligence,
        Wisdom = Wisdom,
        Charisma = Charisma,
        Willpower = Willpower,
    };

    private int ComputeAttributeMod(int value)
    {
        return (int)Math.Floor(((double)value - 10.0) / 2.0);
    }

    public int StrengthMod => ComputeAttributeMod(Strength);
    public int EnduranceMod => ComputeAttributeMod(Endurance);
    public int DexterityMod => ComputeAttributeMod(Dexterity);
    public int IntelligenceMod => ComputeAttributeMod(Intelligence);
    public int WisdomMod => ComputeAttributeMod(Wisdom);
    public int CharismaMod => ComputeAttributeMod(Charisma);
    public int WillpowerMod => ComputeAttributeMod(Willpower);

    private int _patrolLegIndex;
    private double _patrolLegProgress = 0;

    private Area2D _senseArea;

    private AbilityMenu _combatInteractionMenu;

    protected BasicAttack _basicAttack;

    private float _timeSinceLastAttack = 0.0f;

    private class CombatState : ICharacterState
    {
        private HashSet<string> _hostiles = [];
        private Character _self;

        public CombatState(Character character)
        {
            _self = character;
            foreach (var body in character._senseArea.GetOverlappingBodies())
            {
                if (body is Character other && HostilitySystem.GetHostility(character.CharacterData.ResourcePath, other.CharacterData.ResourcePath))
                {
                    _hostiles.Add(other.CharacterData.ResourcePath);
                }
            }
            character._senseArea.BodyEntered += CharacterSensed;
            character._senseArea.BodyExited += CharacterLeftSense;
        }

        public void Process(double delta, Character character)
        {
            if (_hostiles.Count == 0)
            {
                character.SetState(new PatrolState());
            }
            else
            {
                var target = _hostiles
                    .Select(id => CharacterSystem.GetInstance(id))
                    .OrderBy(t => GetTargetPriority(character, t))
                    .FirstOrDefault();
                
                if (target != null)
                {
                    character.State = new AttackState(target, _self.CharacterData.Abilities.OrderByDescending(x => x.TargetDamage).FirstOrDefault());
                }
            }
        }

        public void PhysicsProcess(double delta, Character character)
        {
            
        }

        public static float GetTargetPriority(Character character, Character target)
        {
            var weaponRange = character.GetEquipmentSet().Weapon.WeaponStats.Range;
            var toTarget = target.Position - character.Position;
            var rangePerc = toTarget.Length() / weaponRange;
            var hpPercent = (float)target.CharacterData.CurrentHitpoints / target.CharacterData.MaxHitpoints;

            return hpPercent + rangePerc * 1.5f;
        }

        public void CharacterSensed(Node2D body)
        {
            if (body is Character sensedCharacter)
            {
                var sensedId = sensedCharacter.CharacterData.ResourcePath;
                if (HostilitySystem.GetHostility(_self.CharacterData.ResourcePath, sensedId))
                {
                    _hostiles.Add(sensedId);
                }
            }
        }

        public void CharacterLeftSense(Node2D body)
        {
            if (body is Character sensedCharacter)
            {
                var sensedId = sensedCharacter.CharacterData.ResourcePath;
                _hostiles.Remove(sensedId);
            }
        }
    }

    private class PursuitState : ICharacterState
	{
		private Character _target;
		private Ability _ability;

		public PursuitState(Character character, Character target, Ability ability)
		{
			_target = target;
			_ability = ability;
            var agent = character.GetNode<NavigationAgent2D>("NavigationAgent2D");

            agent.VelocityComputed += (safeVelocity) =>
			{
				OnVelocityComputed(character, safeVelocity.LimitLength(character.CharacterData.Speed));
			};

			agent.TargetPosition = target.GetClosestOnCollSurface(character.Position);
			agent.TargetDesiredDistance = _ability.Range; 
		}

		public void Process(double delta, Character character)
		{
		}

		public void PhysicsProcess(double delta, Character character)
		{
			var navAgent = character.GetNode<NavigationAgent2D>("NavigationAgent2D");
			// Do not query when the map has never synchronized and is empty.
			if (NavigationServer2D.MapGetIterationId(navAgent.GetNavigationMap()) == 0)
			{
				return;
			}

			if (navAgent.IsNavigationFinished())
			{
				if (_ability != null)
				{
					character.State = new AttackState(_target, _ability);
				}
				return;
			}

            navAgent.TargetPosition = _target.GetClosestOnCollSurface(character.Position);

			Vector2 nextPathPosition = navAgent.GetNextPathPosition();
			Vector2 newVelocity = character.GlobalPosition.DirectionTo(nextPathPosition) * character.CharacterData.Speed;
			if (navAgent.AvoidanceEnabled)
			{
				navAgent.Velocity = newVelocity;
			}
			else
			{
				OnVelocityComputed(character, newVelocity);
			}
		}

		private void OnVelocityComputed(Character character, Vector2 safeVelocity)
		{
			character.Velocity = safeVelocity;
			character.MoveAndSlide();
		}
    }

    public class AttackState : ICharacterState
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
			var distance = character.GlobalPosition.DistanceTo(_target.GetClosestOnCollSurface(character.Position));
			if (distance > _ability.Range)
			{
				character.State = new PursuitState(character, _target, _ability);
			}
			else if (character._timeSinceLastAttack >= _ability.Cooldown)
			{
                character._timeSinceLastAttack = 0.0f;
				CombatSystem.UseAbility(_ability, character, _target);
				character.State = new CombatState(character);
			}
		}
	}


    private class PatrolState : ICharacterState
    {
        public void Process(double delta, Character character)
        {
            if (character.CharacterData.PatrolLegs.Count > 0)
            {
                var currentPatrolLeg = character.CharacterData.PatrolLegs[character._patrolLegIndex];
                if (currentPatrolLeg.Direction.X < 0)
                {
                    character.SpriteAnim.Play("walk_west");
                }

                if (currentPatrolLeg.Direction.X > 0)
                {
                    character.SpriteAnim.Play("walk_east");
                }

                if (currentPatrolLeg.Direction.Y > 0)
                {
                    character.SpriteAnim.Play("walk_north");
                }

                if (currentPatrolLeg.Direction.Y < 0)
                {
                    character.SpriteAnim.Play("walk_south");
                }
            }

            foreach (var body in character._senseArea.GetOverlappingBodies())
            {
                if (body is Character other && HostilitySystem.GetHostility(character.CharacterData.ResourcePath, other.CharacterData.ResourcePath))
                {
                    character.State = new CombatState(character);
                    return;
                }
            }
        }

        public void PhysicsProcess(double delta, Character character)
        {
            if (character.CharacterData.PatrolLegs.Count > 0)
            {
                var currentPatrolLeg = character.CharacterData.PatrolLegs[character._patrolLegIndex];
                if (character._patrolLegProgress >= currentPatrolLeg.Distance)
                {
                    character._patrolLegProgress = 0;
                    character._patrolLegIndex = (character._patrolLegIndex + 1) % character.CharacterData.PatrolLegs.Count;
                    currentPatrolLeg = character.CharacterData.PatrolLegs[character._patrolLegIndex];
                }

                character.Velocity = currentPatrolLeg.Direction * character.CharacterData.Speed;
                character._patrolLegProgress += character.Velocity.Length();
                character.MoveAndSlide();
            }
        }
    }

    public AbilityMenu GetCombatInteractionMenu()
    {
        return _combatInteractionMenu;
    }

    public interface ICharacterState
    {
        void Process(double delta, Character character);
        void PhysicsProcess(double delta, Character character);
    }

    protected ICharacterState State;

    public override void _Ready()
    {
        SpriteAnim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        collider = GetNode<CollisionShape2D>("MainCollider");
        FactionSystem.SetFaction(CharacterData.ResourcePath, CharacterData.InitialFaction);
        InventorySystem.SetInventory(CharacterData.ResourcePath, [.. InitialInventory]);
        CharacterSystem.SetInstance(CharacterData.ResourcePath, this);
        State = new PatrolState();
        _senseArea = GetNode<Area2D>("SenseArea");
        _combatInteractionMenu = GetNode<AbilityMenu>("CombatInteractionMenu");
        _combatInteractionMenu.Visible = false;
        _basicAttack = new BasicAttack();
		CharacterData.Abilities.Add(_basicAttack);
        EquipmentSystem.SetEquipment(CharacterData.ResourcePath, CharacterData.StartingEquipment);
        _basicAttack.SetWeapon(CharacterData.StartingEquipment.Weapon);
    }

    public override void _Process(double delta)
    {
        _timeSinceLastAttack += (float)delta;
        State.Process(delta, this);
    }

    public override void _PhysicsProcess(double delta)
    {
        State.PhysicsProcess(delta, this);
    }

    protected EquipmentSet GetEquipmentSet()
    {
        if (EquipmentSystem.RetrieveEquipment(CharacterData.ResourcePath, out EquipmentSet eq))
        {
            return eq;
        }
        else
        {
            return new EquipmentSet();
        }
    }

    public int ComputeAc()
    {
        return GetEquipmentSet().ComputeAc() + DexterityMod;
    }

    public DamageRoll GetDamageRolls()
    {
        var @base = GetEquipmentSet().GetDamageRolls();
        return @base;
    }

    public int ComputeToHitMod()
    {
        var eq = GetEquipmentSet();
        var @base = eq.ComputeToHitMod();
        return @base + (eq.Weapon.WeaponStats.Melee ? StrengthMod : DexterityMod);
    }

    public bool MeetsEquipRequirements(Item item)
    {
        return item.AttributeRequirements.MeetsRequirements(FinalAttributes) &&
               item.SkillRequirements.Proficiencies.All(CharacterData.BaseSkills.Proficiencies.Contains);
    }

    public Vector2 GetClosestOnCollSurface(Vector2 SourcePoint)
    {
        var toTarget = SourcePoint - Position;
        var supportPoint = collider.Shape.GetRect().GetSupport(toTarget);
        return supportPoint + Position;
    }

    public void SetState(ICharacterState characterState)
    {
        State = characterState;
    }

    public Area2D GetSenseArea()
    {
        return _senseArea;
    }
}