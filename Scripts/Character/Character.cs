using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArkhamHunters.Scripts;

public struct CoverCheckResult
{
    public int CoverLevelNorth;
    public int CoverLevelSouth;
    public int CoverLevelEast;
    public int CoverLevelWest;
}

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
    protected AnimationPlayer Anim;
    public NavigationObstacle2D NavObstacle;

    [Export]
    private Godot.Collections.Array<Item> InitialInventory = new();

    [Export]
    public CharacterData CharacterData;

    [Export]
    public Font ToHitFont;

    public Sprite2D CoverBadge;

    public int Strength => CharacterData.BaseAttributes.Strength + GetEquipmentSet().ComputeAttributeBonus().StrengthBonus;
    public int Endurance => CharacterData.BaseAttributes.Endurance + GetEquipmentSet().ComputeAttributeBonus().EnduranceBonus;
    public int Dexterity => CharacterData.BaseAttributes.Dexterity + GetEquipmentSet().ComputeAttributeBonus().DexterityBonus;
    public int Intelligence => CharacterData.BaseAttributes.Intelligence + GetEquipmentSet().ComputeAttributeBonus().IntelligenceBonus;
    public int Wisdom => CharacterData.BaseAttributes.Wisdom + GetEquipmentSet().ComputeAttributeBonus().WisdomBonus;
    public int Charisma => CharacterData.BaseAttributes.Charisma + GetEquipmentSet().ComputeAttributeBonus().CharismaBonus;
    public int Willpower => CharacterData.BaseAttributes.Willpower + GetEquipmentSet().ComputeAttributeBonus().WillpowerBonus;

    public Sprite2D ActionPip1;
    public Sprite2D ActionPip2;

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

    protected Sprite2D _mainSprite;

    private CoverCheckResult _coverState = new()
    {
        CoverLevelNorth = 0,
        CoverLevelSouth = 0,
        CoverLevelEast = 0,
        CoverLevelWest = 0,
    };

    protected Area2D _senseArea;

    protected Label _healthLabel;

    private class PatrolState : ICharacterState
    {
        public void Process(double delta, Character character)
        {
            if (character.CharacterData.PatrolLegs.Count > 0)
            {
                var currentPatrolLeg = character.CharacterData.PatrolLegs[character._patrolLegIndex];
                if (currentPatrolLeg.Direction.X < 0)
                {
                    character.Anim.Play("walk_west");
                }

                if (currentPatrolLeg.Direction.X > 0)
                {
                    character.Anim.Play("walk_east");
                }

                if (currentPatrolLeg.Direction.Y > 0)
                {
                    character.Anim.Play("walk_north");
                }

                if (currentPatrolLeg.Direction.Y < 0)
                {
                    character.Anim.Play("walk_south");
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
            _c.UpdateCoverState(_c.GetWorld2D().DirectSpaceState);
        }

        public void PhysicsProcess(double delta, Character character)
        {
            if (_isOurTurn && CombatSystem.NavReady())
            {
                CombatSystem.PassTurn(character.CharacterData);
                //var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), character.GlobalPosition, character.GlobalPosition + new Vector2(32.0f, 0.0f), true, 0x1u);
                //var len = ComputePathLength(path, character.GlobalPosition);
                //if (len <= _c.CharacterData.MovementRange)
                //{
                //    character.State = new CombatNavState(_c, path);
                //    character.MouseEntered -= OnHover;
                //    character.MouseExited -= OnHoverEnd;
                //    character.InputPickable = false;
                //    CombatSystem.TurnHandlers -= OnTurnBegin;
                //    character.Draw -= OnCharacterDraw;
                //}
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

    protected class NavState : ICharacterState
    {
        Vector2 _targetPoint;
        ICharacterState _nextState;
        public NavState(Vector2 point, ICharacterState nextState)
        {
            _targetPoint = point;
            _nextState = nextState;
        }

        public void Process(double delta, Character character)
        {
            
        }

        public void PhysicsProcess(double delta, Character character)
        {
            var targetVector = _targetPoint - character.Position;
            var vel = targetVector.Normalized() * character.CharacterData.Speed;
            character.Velocity = vel;
            character.MoveAndSlide();
            if (character.Position.DistanceTo(_targetPoint) <= 1.0f)
            {
                character.Position = _targetPoint;
                character.State = _nextState;
            }
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
            character.CoverBadge.Hide();
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
                    character.UpdateCoverState(character.GetWorld2D().DirectSpaceState);
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
        Anim = GetNode<AnimationPlayer>("AnimationPlayer");
        CoverBadge = GetNode<Sprite2D>("CoverBadge");
        collider = GetNode<CollisionShape2D>("MainCollider");
        FactionSystem.SetFaction(CharacterData.ResourcePath, CharacterData.InitialFaction);
        InventorySystem.SetInventory(CharacterData.ResourcePath, [.. InitialInventory]);
        CharacterSystem.SetInstance(CharacterData.ResourcePath, this);
        HealthSystem.SetCurrentHitpoints(CharacterData.ResourcePath, CharacterData.CurrentHitpoints);
        CombatSystem.CombatStartHandlers += OnCombatStarted;
		CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatEnded += OnCombatEnded;
        HealthSystem.DeathEventHandlers += OnDeath;
        HealthSystem.DamageEventHandlers += OnDamage;
        State = new PatrolState();
        ActionPip1 = GetNode<Sprite2D>("ActionPip");
        ActionPip2 = GetNode<Sprite2D>("ActionPip2");
        _senseArea = GetNode<Area2D>("SenseArea");
        _mainSprite = GetNode<Sprite2D>("MainSprite");
        _healthLabel = GetNode<Label>("HealthLabel");
        _healthLabel.Text = $"{CharacterData.CurrentHitpoints} / {CharacterData.MaxHitpoints}";

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

    public int ComputeAc(Vector2 fromDirection)
    {
        var ac = GetEquipmentSet().ComputeAc() + DexterityMod;

        if (IsTakingCover())
        {
            // Add cover.
            var toAttacker = (-fromDirection).Normalized();
            List<Vector2> cardinals = [Vector2.Up, Vector2.Down, Vector2.Right, Vector2.Left];
            // Use the cover level of the cardinal direction with the minimum angular distance to the attacker's target vector.
            var toAttackerQuantized = cardinals.MinBy(cardinal => Mathf.Abs(toAttacker.AngleTo(cardinal)));
            if (toAttackerQuantized == Vector2.Up)
            {
                ac += _coverState.CoverLevelNorth * 2;
            }
            if (toAttackerQuantized == Vector2.Down)
            {
                ac += _coverState.CoverLevelSouth * 2;
            }
            if (toAttackerQuantized == Vector2.Right)
            {
                ac += _coverState.CoverLevelEast * 2;
            }
            if (toAttackerQuantized == Vector2.Left)
            {
                ac += _coverState.CoverLevelWest * 2;
            }
        }
        return ac;
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
            _healthLabel.Show();
            State = GetCombatState();
        }
    }

    public virtual void OnCombatJoined(CharacterData c)
    {
        if (c.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            _healthLabel.Show();
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
        CoverBadge.Visible = false;
        if (ActionPip1 != null && ActionPip2 != null)
        {
            ActionPip1.Visible = false;
            ActionPip2.Visible = false;
        }
        _healthLabel.Hide();
        QueueRedraw();
    }

    public virtual void OnDeath(DeathEvent e)
    {
        if (e.deceased.CharacterData.ResourcePath == CharacterData.ResourcePath)
        {
            CleanDelegates();
            QueueFree();
        }
    }

    public bool IsTakingCover()
    {
        return _coverState.CoverLevelWest > 0 || _coverState.CoverLevelEast > 0 || _coverState.CoverLevelSouth > 0 || _coverState.CoverLevelNorth > 0;
    }

    public CoverCheckResult UpdateCoverState(PhysicsDirectSpaceState2D physicsState)
    {
        var ret = new CoverCheckResult();
        var rayNorth = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + new Vector2(0.0f, -30.0f), 1 << (22 - 1));
        var raySouth = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + new Vector2(0.0f, 30.0f), 1 << (22 - 1));
        var rayWest = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + new Vector2(-30.0f, 0.0f), 1 << (22 - 1));
        var rayEast = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + new Vector2(30.0f, 0.0f), 1 << (22 - 1));
        var northResult = physicsState.IntersectRay(rayNorth);
        if (northResult.Count > 0)
        {
            ret.CoverLevelNorth = 1;
        }
        var southResult = physicsState.IntersectRay(raySouth);
        if (southResult.Count > 0)
        {
            ret.CoverLevelSouth = 1;
        }
        var eastResult = physicsState.IntersectRay(rayEast);
        if (eastResult.Count > 0)
        {
            ret.CoverLevelEast = 1;
        }
        var westResult = physicsState.IntersectRay(rayWest);
        if (westResult.Count > 0)
        {
            ret.CoverLevelWest = 1;
        }

        _coverState = ret;
        CoverBadge.Visible = IsTakingCover();
            
        return ret;
    }

    public virtual void OnDamage(DamageEvent e)
	{
        string recipientId = e.recipient.CharacterData.ResourcePath;
		if (recipientId == CharacterData.ResourcePath)
		{
			_healthLabel.Text = $"{HealthSystem.GetCurrentHitpoints(recipientId)} / {CharacterData.MaxHitpoints}";
		}
	}

    public void WalkToPoint(Vector2 point)
    {
        State = new NavState(point, State);
    }

    private void CleanDelegates()
    {
        CombatSystem.CombatStartHandlers -= OnCombatStarted;
        CombatSystem.CharacterJoinedCombatHandlers -= OnCombatJoined;
        CombatSystem.CombatEnded -= OnCombatEnded;
        HealthSystem.DeathEventHandlers -= OnDeath;
        HealthSystem.DamageEventHandlers -= OnDamage;
    }
}