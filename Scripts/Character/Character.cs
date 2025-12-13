using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArkhamHunters.Scripts;

[GlobalClass]
public partial class Character : CharacterBody2D
{
    public static float ComputePathLength(Vector2[] path, Vector2 origin)
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

    public CollisionShape2D collider;
    protected AnimatedSprite2D SpriteAnim;
    public NavigationObstacle2D NavObstacle;

    [Export]
    private Godot.Collections.Array<Item> InitialInventory = new();

    [Export]
    public CharacterData CharacterData;

    [Export]
    public Font ToHitFont;

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
        bool _isOurTurn;
        bool _hovered = false;
        public CombatState(Character character)
        {
            _c = character;
            _c.Draw += OnCharacterDraw;
            _c.InputPickable = true;
            _c.MouseEntered += OnHover;
            _c.MouseExited += OnHoverEnd;
            CombatSystem.TurnHandlers += OnTurnBegin;
            _c.QueueRedraw();
            _isOurTurn = CombatSystem.GetMovingSide().Contains(character.CharacterData.ResourcePath);
        }

        public void PhysicsProcess(double delta, Character character)
        {
            if (_isOurTurn && CombatSystem.NavReady())
            {
                var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), character.GlobalPosition, character.GlobalPosition + new Vector2(32.0f, 0.0f), true, 0x1u);
                var len = ComputePathLength(path, character.GlobalPosition);
                if (len <= _c.CharacterData.MovementRange)
                {
                    character.State = new CombatNavState(_c, path);
                    character.MouseEntered -= OnHover;
                    character.MouseExited -= OnHoverEnd;
                    character.InputPickable = false;
                    CombatSystem.TurnHandlers -= OnTurnBegin;
                    character.Draw -= OnCharacterDraw;
                }
            }
            character.QueueRedraw();
        }

        public void Process(double delta, Character character)
        {
        }

        public void OnCharacterDraw()
        {
            if (_hovered)
            {
                _c.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(1.0f, 0.0f, 0.0f), filled: false);
            }
        }

        public void OnTurnBegin(List<string> movingSide)
        {
            _isOurTurn = movingSide.Contains(_c.CharacterData.ResourcePath);
        }

        public void OnHover()
        {
            _hovered = true;
            HoverSystem.SetHovered(_c.CharacterData.ResourcePath);
            _c.QueueRedraw();
        }

        public void OnHoverEnd()
        {
            _hovered = false;
            HoverSystem.SetUnhovered(_c.CharacterData.ResourcePath);
            _c.QueueRedraw();
        }
    }

    protected class CombatNavState : ICharacterState
    {
        Character _character;

        private Vector2[] _path;
		private int _currentPoint = 0;

		public CombatNavState(Character character, Vector2[] path)
        {
			_character = character;
			_path = path;
            HoverSystem.SetUnhovered(character.CharacterData.ResourcePath);
        }

        public void PhysicsProcess(double delta, Character character)
        {
			var targetPoint = _path[_currentPoint];
			if (_character.Position.DistanceTo(targetPoint) <= 1.0f)
            {
				_character.Position = targetPoint;
                if (_currentPoint + 1 < _path.Length)
                {
                    _currentPoint += 1;
                }
				else
                {
                    CombatSystem.AttemptMove(character.CharacterData);
                    _character.SetState(_character.GetCombatState());
                }
            }
			else
			{
				var targetVector = targetPoint - _character.Position;
				var vel = targetVector.Normalized() * _character.CharacterData.Speed;
				_character.Velocity = vel;
				_character.MoveAndSlide();
			}
        }

        public void Process(double delta, Character character) { }
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
        CombatSystem.CombatStartHandlers += OnCombatStarted;
		CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatEnded += OnCombatEnded;
        State = new PatrolState();
        _senseArea = GetNode<Area2D>("SenseArea");

        _senseArea.BodyEntered += OnBodyEnteredSenseArea;
        
        EquipmentSystem.SetEquipment(CharacterData.ResourcePath, CharacterData.StartingEquipment);
        NavObstacle = GetNode<NavigationObstacle2D>("NavigationObstacle2D");
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
				CombatSystem.BeginCombat(CharacterData, character.CharacterData);
			}
		}
    }

    public virtual void OnCombatStarted(CombatStartEvent e)
    {
        if (e.participants.Contains(CharacterData.ResourcePath))
        {
            State = GetCombatState();
        }
    }

    public virtual void OnCombatJoined(CharacterData c)
    {
        if (c.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            State = GetCombatState();
        } 
    }

    public virtual ICharacterState GetCombatState()
    {
        return new CombatState(this);
    }

    public virtual void OnCombatEnded()
    {
        if (State is CombatState || State is CombatNavState)
        {
            State = new PatrolState();
        }
    }
}