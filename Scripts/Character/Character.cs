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

    protected Area2D _senseArea;

    private AbilityMenu _combatInteractionMenu;

    protected BasicAttack _basicAttack;

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

    private class CombatState : ICharacterState
    {
        Character _c;
        public CombatState(Character character)
        {
            _c = character;
            _c.Draw += OnCharacterDraw;
            _c.QueueRedraw();
        }

        public void PhysicsProcess(double delta, Character character)
        {
            
        }

        public void Process(double delta, Character character)
        {
            
        }

        public void OnCharacterDraw()
        {
            _c.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(1.0f, 0.0f, 0.0f), filled: false);
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
        CombatSystem.combatStartHandler += OnCombatStarted;
		CombatSystem.characterJoinedCombatHandler += OnCombatJoined;
        State = new PatrolState();
        _senseArea = GetNode<Area2D>("SenseArea");

        _senseArea.BodyEntered += OnBodyEnteredSenseArea;
        
        _combatInteractionMenu = GetNode<AbilityMenu>("CombatInteractionMenu");
        _combatInteractionMenu.Visible = false;
        _basicAttack = new BasicAttack();
		CharacterData.Abilities.Add(_basicAttack);
        EquipmentSystem.SetEquipment(CharacterData.ResourcePath, CharacterData.StartingEquipment);
        _basicAttack.SetWeapon(CharacterData.StartingEquipment.Weapon);
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

    public virtual void OnBodyEnteredSenseArea(Node2D body)
    {
        if (body is Character character && HostilitySystem.GetHostility(character.CharacterData.ResourcePath, CharacterData.ResourcePath))
		{
			if (CombatSystem.IsInCombat(CharacterData))
			{
				CombatSystem.JoinCombat(character.CharacterData);
			}
			else
			{
				CombatSystem.BeginCombat(CharacterData, [character.CharacterData]);
			}
		}
    }

    public virtual void OnCombatStarted(CombatStartEvent e)
    {
        if (e.participants.Contains(CharacterData.ResourcePath))
        {
            State = new CombatState(this);
        }
    }

    public virtual void OnCombatJoined(CharacterData c)
    {
        if (c.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            State = new CombatState(this);
        } 
    }
}