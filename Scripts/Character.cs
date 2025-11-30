using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ArkhamHunters.Scripts.Abilities;
using Godot;

namespace ArkhamHunters.Scripts;

[GlobalClass]
public partial class Character : CharacterBody2D
{
    public CollisionShape2D collider;
    protected AnimatedSprite2D SpriteAnim;
    protected readonly List<Item> Inventory = new();

    [Export] protected AttributeSet BaseAttributes = new();
    [Export] protected SkillSet BaseSkills = new();

    public int Strength => BaseAttributes.Strength + GetEquipmentSet().ComputeAttributeBonus().StrengthBonus;
    public int Endurance => BaseAttributes.Endurance + GetEquipmentSet().ComputeAttributeBonus().EnduranceBonus;
    public int Dexterity => BaseAttributes.Dexterity + GetEquipmentSet().ComputeAttributeBonus().DexterityBonus;
    public int Intelligence => BaseAttributes.Intelligence + GetEquipmentSet().ComputeAttributeBonus().IntelligenceBonus;
    public int Wisdom => BaseAttributes.Wisdom + GetEquipmentSet().ComputeAttributeBonus().WisdomBonus;
    public int Charisma => BaseAttributes.Charisma + GetEquipmentSet().ComputeAttributeBonus().CharismaBonus;
    public int Willpower => BaseAttributes.Willpower + GetEquipmentSet().ComputeAttributeBonus().WillpowerBonus;

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
    [Export]
    public int MaxHitpoints = 100;
    [Export]
    public int CurrentHitpoints = 100;

    [Export]
    protected Faction _faction;

    [Export] public float Speed = 300.0f;

    [Export] public Godot.Collections.Array<PatrolLeg> PatrolLegs = [];

    private int _patrolLegIndex;
    private double _patrolLegProgress = 0;

    private Area2D _senseArea;

    private AbilityMenu _combatInteractionMenu;

    private class CombatState : ICharacterState
    {
        public void Process(double delta, Character character)
        {

        }

        public void PhysicsProcess(double delta, Character character)
        {
        }
    }

    private class PatrolState : ICharacterState
    {
        public void Process(double delta, Character character)
        {
            if (character.PatrolLegs.Count > 0)
            {
                var currentPatrolLeg = character.PatrolLegs[character._patrolLegIndex];
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
                if (body is Character other && HostilitySystem.GetHostility(character.GetInstanceId(), other.GetInstanceId()))
                {
                    character.State = new CombatState();
                    return;
                }
            }
        }

        public void PhysicsProcess(double delta, Character character)
        {
            if (character.PatrolLegs.Count > 0)
            {
                var currentPatrolLeg = character.PatrolLegs[character._patrolLegIndex];
                if (character._patrolLegProgress >= currentPatrolLeg.Distance)
                {
                    character._patrolLegProgress = 0;
                    character._patrolLegIndex = (character._patrolLegIndex + 1) % character.PatrolLegs.Count;
                    currentPatrolLeg = character.PatrolLegs[character._patrolLegIndex];
                }

                character.Velocity = currentPatrolLeg.Direction * character.Speed;
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
        FactionSystem.SetFaction(GetInstanceId(), _faction);
        State = new PatrolState();
        _senseArea = GetNode<Area2D>("SenseArea");
        _combatInteractionMenu = GetNode<AbilityMenu>("CombatInteractionMenu");
        _combatInteractionMenu.Visible = false;
    }

    public override void _Process(double delta)
    {
        State.Process(delta, this);
    }

    public override void _PhysicsProcess(double delta)
    {

        State.PhysicsProcess(delta, this);
    }

    protected EquipmentSet GetEquipmentSet()
    {
        EquipmentSet eq;
        if (EquipmentSystem.RetrieveEquipment(GetInstanceId(), out eq))
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
               item.SkillRequirements.Proficiencies.All(x => BaseSkills.Proficiencies.Contains(x));
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
}