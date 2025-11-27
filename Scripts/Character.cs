using System;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts.Abilities;
using Godot;

namespace ArkhamHunters.Scripts;

public abstract partial class Character : CharacterBody2D
{
    public CollisionShape2D collider;
    protected AnimatedSprite2D SpriteAnim;
    protected readonly List<Item> Inventory = new();
    protected EquipmentSet EquipmentSet = new();

    [Export] protected AttributeSet BaseAttributes = new();
    [Export] protected SkillSet BaseSkills = new();

    public int Strength => BaseAttributes.Strength + EquipmentSet.ComputeAttributeBonus().StrengthBonus;
    public int Endurance => BaseAttributes.Endurance + EquipmentSet.ComputeAttributeBonus().EnduranceBonus;
    public int Dexterity => BaseAttributes.Dexterity + EquipmentSet.ComputeAttributeBonus().DexterityBonus;
    public int Intelligence => BaseAttributes.Intelligence + EquipmentSet.ComputeAttributeBonus().IntelligenceBonus;
    public int Wisdom => BaseAttributes.Wisdom + EquipmentSet.ComputeAttributeBonus().WisdomBonus;
    public int Charisma => BaseAttributes.Charisma + EquipmentSet.ComputeAttributeBonus().CharismaBonus;
    public int Willpower => BaseAttributes.Willpower + EquipmentSet.ComputeAttributeBonus().WillpowerBonus;

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
    }

    public override void _Process(double delta)
    {
        State.Process(delta, this);
    }

    public override void _PhysicsProcess(double delta)
    {
        State.PhysicsProcess(delta, this);
    }

    public int ComputeAc()
    {
        return EquipmentSet.ComputeAc() + DexterityMod;
    }

    public DamageRoll GetDamageRolls()
    {
        var @base = EquipmentSet.GetDamageRolls();
        return @base;
    }

    public int ComputeToHitMod()
    {
        var @base = EquipmentSet.ComputeToHitMod();
        return @base + (EquipmentSet.Weapon.WeaponStats.Melee ? StrengthMod : DexterityMod);
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