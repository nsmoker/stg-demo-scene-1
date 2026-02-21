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
};

[GlobalClass]
public partial class Character : CharacterBody2D
{
    public static Vector2[] TrimPath(Vector2 start, Vector2[] path, float maxLength)
    {
        Vector2 loc = start;
        float remainingLength = maxLength;
        List<Vector2> trimmedPath = [];
        foreach (Vector2 p in path)
        {
            if (remainingLength > 0)
            {
                float length = Mathf.Min((p - start).Length(), remainingLength);
                remainingLength -= length;
                Vector2 targetVector = p - loc;
                Vector2 newLoc = loc + targetVector.Normalized() * length;
                trimmedPath.Add(newLoc);
                loc = newLoc;
            }
        }

        return [.. trimmedPath];
    }

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

    [Export]
    public PackedScene ProjectileScene;

    [Export]
    public Ability BasicAttackAbility;

    [Export]
    public Godot.Collections.Array<Ability> Abilities = [];

    Marker2D _projectileSpawnPoint;

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

    public AttributeSet FinalAttributes => new()
    {
        Strength = Strength,
        Endurance = Endurance,
        Dexterity = Dexterity,
        Intelligence = Intelligence,
        Wisdom = Wisdom,
        Charisma = Charisma,
        Willpower = Willpower,
    };

    private static int ComputeAttributeMod(int value)
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
    private bool _sitting = false;
    private bool _collisionOverride = true;
    private FurnitureProp _occupiedProp;

    public enum AnimState
    {
        Idle,
        WalkNorth,
        WalkSouth,
        WalkEast,
        WalkWest,
        Attack,
        Sitting,
        Talking
    }

    protected AnimState _currentAnimState = AnimState.Idle;

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

    protected CharacterData _attackTarget;
    private Action _onAttackComplete;

    protected class DialogueState : ICharacterState
    {
        public void Process(double delta, Character character) { }

        public void PhysicsProcess(double delta, Character player) { }

        public void OnTransition(Character character) { }
    }

    private class PatrolState : ICharacterState
    {
        public void Process(double delta, Character character) { }

        public void PhysicsProcess(double delta, Character character)
        {
            if (character.IsNamedCharacter() && character.CharacterData.PatrolLegs.Count > 0)
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
                character.SetWalkAnimState(character.Velocity);
            }
        }

        public void OnTransition(Character c) { }
    }

    private class CombatState : ICharacterState
    {
        Character _c;
        bool _isOurTurn;
        bool _attackedThisTurn = false;
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
                List<Character> enemiesInSense = character.GetSenseArea().GetOverlappingBodies().Where(body => body is Character).Cast<Character>().Where(c => HostilitySystem.GetHostility(character.CharacterData.ResourcePath, c.CharacterData.ResourcePath)).ToList();
                var closestEnemy = enemiesInSense.OrderBy(c => c.GlobalPosition.DistanceTo(character.GlobalPosition)).FirstOrDefault();
                if (closestEnemy != null)
                {
                    var distance = closestEnemy.GlobalPosition.DistanceTo(character.GlobalPosition);
                    if (distance > character.CharacterData.AttackRange)
                    {
                        var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), character.GlobalPosition, closestEnemy.GlobalPosition, true, 0x1u); var len = ComputePathLength(path, character.GlobalPosition);
                        if (path.Length == 0)
                        {
                            var targetVec = closestEnemy.GlobalPosition - character.GlobalPosition;
                            character.ControllerState = new CombatNavState(character, [character.GlobalPosition + targetVec.Normalized() * character.CharacterData.MovementRange]);
                        }
                        else
                        {
                            character.ControllerState = new CombatNavState(character, TrimPath(character.GlobalPosition, path, character.CharacterData.MovementRange));
                        }
                    }
                    else if (!_attackedThisTurn)
                    {
                        // In range, attack.
                        character.SetAttackTarget(closestEnemy.CharacterData);
                        character.SetAttackAnimState(character.GlobalPosition.DirectionTo(closestEnemy.GlobalPosition));
                        _attackedThisTurn = true;
                    }
                }
                else
                {
                    CombatSystem.PassTurn(character.CharacterData);
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
            _attackedThisTurn = false;
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

        public void OnTransition(Character character)
        {
            _c.Draw -= OnCharacterDraw;
            _c.InputPickable = false;
            _c.MouseEntered -= OnHover;
            _c.MouseExited -= OnHoverEnd;
            CombatSystem.TurnHandlers -= OnTurnBegin;
            _c.QueueRedraw();
        }
    }

    protected class NavState : ICharacterState
    {
        ICharacterState _nextState;

        readonly Action _onComplete;
        private readonly Vector2[] _path;
        private int _currentPoint = 0;
        private readonly float _speed = -1;
        private readonly float _tolerance;
        public NavState(Vector2[] path, ICharacterState nextState, Action onComplete = null, float speed = -1, float tolerance = 1.0f)
        {
            _path = path;
            _nextState = nextState;
            _onComplete = onComplete;
            _speed = speed;
            _tolerance = tolerance;
        }

        public void Process(double delta, Character character)
        {

        }

        public void PhysicsProcess(double delta, Character character)
        {
            var targetPoint = _path[_currentPoint];
            if (character.GlobalPosition.DistanceTo(targetPoint) <= _tolerance)
            {
                if (_currentPoint + 1 < _path.Length)
                {
                    _currentPoint += 1;
                }
                else
                {
                    character.SetWalkAnimState(Vector2.Zero);
                    character.ControllerState = _nextState;
                    _onComplete();
                    return;
                }
            }

            var targetVector = targetPoint - character.GlobalPosition;
            var vel = targetVector.Normalized() * (_speed > 0.0f ? _speed : character.CharacterData.Speed);
            character.Velocity = vel;
            character.SetWalkAnimState(vel);
            character.MoveAndSlide();
        }

        public void OnTransition(Character character) { }
    }

    protected class CombatNavState : ICharacterState
    {
        readonly Character _character;
        readonly Action _onComplete;

        private Vector2[] _path;
        private int _currentPoint = 0;

        public CombatNavState(Character character, Vector2[] path, Action onComplete = null)
        {
            _character = character;
            _path = path;
            _onComplete = onComplete;
            HoverSystem.SetUnhovered(character.CharacterData.ResourcePath);
            character.CoverBadge.Hide();
        }

        public void OnTransition(Character character) { }

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
                    _character.SetAnimState(AnimState.Idle);
                    _character.ControllerState = _character.GetCombatState();
                    _onComplete?.Invoke();
                }
            }
            else
            {
                var targetVector = targetPoint - _character.Position;
                var vel = targetVector.Normalized() * _character.CharacterData.Speed;
                _character.Velocity = vel;
                _character.SetWalkAnimState(vel);
                _character.MoveAndSlide();
            }
        }

        public void Process(double delta, Character character) { }
    }

    protected class NavToCharacterState : ICharacterState
    {
        private Character _target;
        private Character _self;
        private ICharacterState _prevState;
        private Action _onComplete;
        private float _speed;
        private float _tolerance;

        public NavToCharacterState(Character self, Character target, ICharacterState prevState, Action onComplete, float speed, float tolerance)
        {
            _target = target;
            _self = self;
            _prevState = prevState;
            _onComplete = onComplete;
            _speed = speed;
            _tolerance = tolerance;
        }

        public void OnTransition(Character character) { }

        public void PhysicsProcess(double delta, Character character)
        {
            var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), _self.GlobalPosition, _target.GlobalPosition, true, 0x1u);
            var substate = new NavState([.. path.TakeLast(path.Length - 1)], _prevState, _onComplete, _speed, _tolerance);
            substate.PhysicsProcess(delta, _self);
        }

        public void Process(double delta, Character character) { }
    }

    public interface ICharacterState
    {
        void Process(double delta, Character character);
        void PhysicsProcess(double delta, Character character);

        void OnTransition(Character character);
    }

    private ICharacterState _controllerState = new PatrolState();

    public ICharacterState ControllerState
    {
        get { return _controllerState; }
        set
        {
            _controllerState.OnTransition(this);
            _controllerState = value;
        }
    }

    public bool IsNamedCharacter()
    {
        return CharacterData != null;
    }

    public override void _Ready()
    {
        Anim = GetNode<AnimationPlayer>("AnimationPlayer");
        Anim.AnimationFinished += OnAnimationFinished;
        CoverBadge = GetNode<Sprite2D>("CoverBadge");
        collider = GetNode<CollisionShape2D>("MainCollider");
        ActionPip1 = GetNode<Sprite2D>("ActionPip");
        ActionPip2 = GetNode<Sprite2D>("ActionPip2");
        _senseArea = GetNode<Area2D>("SenseArea");
        _mainSprite = GetNode<Sprite2D>("MainSprite");
        _healthLabel = GetNode<Label>("HealthLabel");
        _projectileSpawnPoint = GetNode<Marker2D>("ProjectileSpawnPoint");

        // Only hook into gameplay systems if we are a named, non-background character.
        if (IsNamedCharacter())
        {
            _healthLabel.Text = $"{CharacterData.CurrentHitpoints} / {CharacterData.MaxHitpoints}";
            EquipmentSystem.SetEquipment(CharacterData.ResourcePath, CharacterData.StartingEquipment);
            FactionSystem.SetFaction(CharacterData.ResourcePath, CharacterData.InitialFaction);
            InventorySystem.SetInventory(CharacterData.ResourcePath, [.. InitialInventory]);
            CharacterSystem.SetInstance(CharacterData.ResourcePath, this);
            HealthSystem.SetCurrentHitpoints(CharacterData.ResourcePath, CharacterData.CurrentHitpoints);
            CombatSystem.CombatStartHandlers += OnCombatStarted;
            CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
            CombatSystem.CombatEnded += OnCombatEnded;
            HealthSystem.DeathEventHandlers += OnDeath;
            HealthSystem.DamageEventHandlers += OnDamage;
            HostilitySystem.HostilityChangeHandlers += OnHostilityChanged;
            _senseArea.BodyEntered += OnBodyEnteredSenseArea;

            Abilities.Insert(0, BasicAttackAbility);
        }

        NavObstacle = GetNode<NavigationObstacle2D>("NavigationObstacle2D");
    }

    public override void _Process(double delta)
    {
        ControllerState.Process(delta, this);
        UpdateAnimation();
    }

    public virtual void UpdateAnimation()
    {
        switch (_currentAnimState)
        {
            case AnimState.Idle:
                Anim.Play("idle");
                break;
            case AnimState.WalkNorth:
                Anim.Play("walk_north");
                break;
            case AnimState.WalkSouth:
                Anim.Play("walk_south");
                break;
            case AnimState.WalkEast:
                _mainSprite.FlipH = false;
                Anim.Play("walk_h");
                break;
            case AnimState.WalkWest:
                _mainSprite.FlipH = true;
                Anim.Play("walk_h");
                break;
            case AnimState.Attack:
                Anim.Play("attack");
                break;
            case AnimState.Sitting:
                Anim.Play("sit");
                break;
            case AnimState.Talking:
                Anim.Play("talking");
                break;
            default:
                Anim.Play("idle");
                break;
        }

        if (_currentAnimState != AnimState.Sitting)
        {
            SetCollision(true);
            _mainSprite.ZIndex = 4;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        ControllerState.PhysicsProcess(delta, this);
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
        var supportPoint = collider.Shape.GetRect().GetSupport(SourcePoint);
        return supportPoint;
    }

    public Area2D GetSenseArea()
    {
        return _senseArea;
    }

    public virtual void OnBodyEnteredSenseArea(Node2D body)
    {
        if (body is Character character && character.IsNamedCharacter() && HostilitySystem.GetHostility(character.CharacterData.ResourcePath, CharacterData.ResourcePath))
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
            ControllerState = GetCombatState();
        }
    }

    public virtual void OnCombatJoined(CharacterData c)
    {
        if (c.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            _healthLabel.Show();
            ControllerState = GetCombatState();
        }
    }

    public virtual ICharacterState GetCombatState()
    {
        return new CombatState(this);
    }

    public virtual void OnCombatEnded()
    {
        if (ControllerState is CombatState || ControllerState is CombatNavState)
        {
            ControllerState = new PatrolState();
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
        if (e.deceased.CharacterData.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            Despawn();
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

    public virtual void OnHostilityChanged(string characterA, string characterB, bool areHostile)
    {
        if (characterA.Equals(CharacterData.ResourcePath) && areHostile && !CombatSystem.IsInCombat(CharacterData))
        {
            _senseArea.GetOverlappingBodies().ToList().ForEach(body =>
            {
                if (body is Character character && HostilitySystem.GetHostility(CharacterData.ResourcePath, character.CharacterData.ResourcePath))
                {
                    if (CombatSystem.IsInCombat(character.CharacterData))
                    {
                        CombatSystem.JoinCombat(CharacterData);
                    }
                    else
                    {
                        CombatSystem.BeginCombat(CharacterData, character.CharacterData);
                    }
                }
            });
        }
    }

    public void SetWalkAnimState(Vector2 direction)
    {
        if (direction.X < 0)
        {
            SetAnimState(AnimState.WalkWest);
        }
        else if (direction.X > 0)
        {
            SetAnimState(AnimState.WalkEast);
        }
        else if (direction.Y < 0)
        {
            SetAnimState(AnimState.WalkNorth);
        }
        else if (direction.Y > 0)
        {
            SetAnimState(AnimState.WalkSouth);
        }
        else
        {
            SetAnimState(_sitting ? AnimState.Sitting : AnimState.Idle);
        }

        if (direction.X != 0 || direction.Y != 0)
        {
            _sitting = false;
            if (_occupiedProp != null)
            {
                _occupiedProp.Occupied = false;
                _occupiedProp = null;
            }
        }
    }

    public void SetAttackAnimState(Vector2 targetVector)
    {
        SetAnimState(AnimState.Attack);
        SetFacing(targetVector);
    }

    public void WalkToPoint(Vector2 point, Action onComplete = null, float speed = -1.0f)
    {
        var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), GlobalPosition, point, true, 0x1u);
        if (path.Length == 0)
        {
            ControllerState = new NavState([GlobalPosition, point], new PatrolState(), onComplete, speed > 0 ? speed : CharacterData.Speed);
        }
        else
        {
            ControllerState = new NavState(path, new PatrolState(), onComplete, speed > 0 ? speed : CharacterData.Speed);
        }
    }

    private void CleanDelegates()
    {
        CombatSystem.CombatStartHandlers -= OnCombatStarted;
        CombatSystem.CharacterJoinedCombatHandlers -= OnCombatJoined;
        CombatSystem.CombatEnded -= OnCombatEnded;
        HealthSystem.DeathEventHandlers -= OnDeath;
        HealthSystem.DamageEventHandlers -= OnDamage;
        HostilitySystem.HostilityChangeHandlers -= OnHostilityChanged;
        Anim.AnimationFinished -= OnAnimationFinished;
    }

    public void SetFacing(Vector2 dir)
    {
        if (dir.X < 0)
        {
            _mainSprite.FlipH = true;
        }
        else
        {
            _mainSprite.FlipH = false;
        }
    }

    public virtual void OnAnimationFinished(StringName animationName)
    {
        if (animationName.Equals("attack"))
        {
            var target = CharacterSystem.GetInstance(_attackTarget.ResourcePath);
            if (target != null)
            {
                BasicAttackAbility.Activate(this, target, _projectileSpawnPoint.GlobalPosition, target.GlobalPosition);
            }
            SetAnimState(AnimState.Idle);
            _onAttackComplete?.Invoke();
            _onAttackComplete = null;
        }
    }

    public void Despawn()
    {
        CleanDelegates();
        QueueFree();
    }

    public void SetAnimState(AnimState state)
    {
        if (state != _currentAnimState)
        {
            // Reset
            Anim.Play("RESET");
            Anim.Advance(0);
            _currentAnimState = state;
        }
    }

    public void SetAttackTarget(CharacterData c)
    {
        _attackTarget = c;
    }

    public void IssueAttack(CharacterData target, Vector2 direction, Action onComplete = null)
    {
        _onAttackComplete = onComplete;
        SetAttackTarget(target);
        SetAttackAnimState(direction);
    }

    public void IssueCombatMove(Vector2[] path, Action onComplete = null)
    {
        ControllerState = new CombatNavState(this, path, onComplete);
    }

    public void SitOn(Prop prop)
    {
        if (prop is FurnitureProp furniture && !furniture.Occupied)
        {
            SetAnimState(AnimState.Sitting);

            Vector2 seatBottomLeft = furniture.GetSeatRegionCenter();
            furniture.Occupied = true;
            _occupiedProp = furniture;
            GlobalPosition = seatBottomLeft;
            SetCollision(false);
            _mainSprite.ZIndex = 2;
            _sitting = true;
        }
    }

    public void SetSprite(Texture2D sprite)
    {
        _mainSprite.Texture = sprite;
    }

    public void SetCollisionOverride(bool @override)
    {
        _collisionOverride = @override;
        SetCollision(!collider.Disabled);
    }

    public void SetCollision(bool enabled)
    {
        collider.Disabled = !enabled || !_collisionOverride;
    }

    public void SetIdle()
    {
        ControllerState = new PatrolState();
        SetAnimState(AnimState.Idle);
    }

    public void WalkToCharacter(Character instance, Action onComplete, float speed, float tolerance = 1.0f)
    {
        ControllerState = new NavToCharacterState(this, instance, new PatrolState(), onComplete, speed, tolerance);
    }

    public void SetTalking()
    {
        ControllerState = new PatrolState();
        SetAnimState(AnimState.Talking);
    }

    public bool IsSeated()
    {
        return _sitting;
    }
}