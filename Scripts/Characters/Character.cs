using Godot;
using STGDemoScene1.Scripts.Controls;
using STGDemoScene1.Scripts.Items;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Resources.Abilities;
using STGDemoScene1.Scripts.Resources.Factions;
using STGDemoScene1.Scripts.StatusEffects;
using STGDemoScene1.Scripts.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts.Characters;

public struct CoverCheckResult
{
    public int CoverLevelNorth;
    public int CoverLevelSouth;
    public int CoverLevelEast;
    public int CoverLevelWest;
};

public class StackStatus
{
    public int NumStacks;
    public List<CombatTimerHandle> StackTimers = [];
}

/// A fake, linear "push." These are not true physics forces, but just simple vectors we use to animate characters during combat.
public class Push
{
    public Vector2 Velocity;
    public double Duration;
    public Action OnFinish;
}

[GlobalClass]
public partial class Character : CharacterBody2D
{
    public CollisionShape2D Collider;
    private AnimationPlayer _anim;
    public NavigationObstacle2D NavObstacle;

    [Export]
    private Godot.Collections.Array<Item> _initialInventory = [];

    [Export]
    public CharacterData CharacterData;

    [Export]
    public Font ToHitFont;

    [Export]
    public Ability BasicAttackAbility;

    [Export]
    public Godot.Collections.Array<Ability> Abilities = [];

    private Marker2D _projectileSpawnPoint;

    private Sprite2D _coverBadge;

    private readonly Dictionary<StatusEffect, StackStatus> _statusEffects = [];

    public float Speed { get; set; } = 20.0f;
    public float MovementRange { get; set; } = 20.0f;

    private StatusEffectContainer _statusEffectContainer;

    private int Strength => CharacterData.BaseAttributes.Strength + GetEquipmentSet().ComputeAttributeBonus().StrengthBonus;
    private int Endurance => CharacterData.BaseAttributes.Endurance + GetEquipmentSet().ComputeAttributeBonus().EnduranceBonus;
    private int Dexterity => CharacterData.BaseAttributes.Dexterity + GetEquipmentSet().ComputeAttributeBonus().DexterityBonus;
    private int Intelligence => CharacterData.BaseAttributes.Intelligence + GetEquipmentSet().ComputeAttributeBonus().IntelligenceBonus;
    private int Wisdom => CharacterData.BaseAttributes.Wisdom + GetEquipmentSet().ComputeAttributeBonus().WisdomBonus;
    private int Charisma => CharacterData.BaseAttributes.Charisma + GetEquipmentSet().ComputeAttributeBonus().CharismaBonus;
    private int Willpower => CharacterData.BaseAttributes.Willpower + GetEquipmentSet().ComputeAttributeBonus().WillpowerBonus;

    public Sprite2D ActionPip1;
    public Sprite2D ActionPip2;

    private AttributeSet FinalAttributes => new()
    {
        Strength = Strength,
        Endurance = Endurance,
        Dexterity = Dexterity,
        Intelligence = Intelligence,
        Wisdom = Wisdom,
        Charisma = Charisma,
        Willpower = Willpower,
    };

    private static int ComputeAttributeMod(int value) => (int) System.Math.Floor((value - 10.0) / 2.0);

    private int StrengthMod => ComputeAttributeMod(Strength);
    private int EnduranceMod => ComputeAttributeMod(Endurance);
    private int DexterityMod => ComputeAttributeMod(Dexterity);
    private int IntelligenceMod => ComputeAttributeMod(Intelligence);
    private int WisdomMod => ComputeAttributeMod(Wisdom);
    private int CharismaMod => ComputeAttributeMod(Charisma);
    private int WillpowerMod => ComputeAttributeMod(Willpower);

    private int _patrolLegIndex;
    private double _patrolLegProgress;
    private bool _sitting;
    private bool _collisionOverride = true;
    private FurnitureProp _occupiedProp;
    private Area2D _interactableRange;

    private List<IInteractable> GetInteractablesInRange() => [.. _interactableRange.GetOverlappingBodies().ToList().OfType<IInteractable>()];

    public IInteractable GetClosestInteractable()
    {
        var interactables = GetInteractablesInRange();
        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var interactable in interactables)
        {
            var node = (Node2D) interactable;
            var distance = GlobalPosition.DistanceTo(node.GlobalPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestInteractable = interactable;
            }
        }

        return closestInteractable;
    }

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

    protected AnimState CurrentAnimState = AnimState.Idle;

    protected Sprite2D MainSprite;

    private CoverCheckResult _coverState = new()
    {
        CoverLevelNorth = 0,
        CoverLevelSouth = 0,
        CoverLevelEast = 0,
        CoverLevelWest = 0,
    };

    protected Area2D SenseArea;

    protected Label HealthLabel;

    private Action _onAttackComplete;

    protected List<Push> CurrentPushes = [];

    private class PawnState : ICharacterState
    {
        public void OnTransition(Character character) { }
        public void PhysicsProcess(double delta, Character character) { }
        public void Process(double delta, Character character) { }
    }

    private class CombatState : ICharacterState
    {
        private readonly Character _c;
        private bool _hovered;
        private bool _pawnAttacking;
        public CombatState(Character character)
        {
            _c = character;
            _c.Draw += OnCharacterDraw;
            _c.InputPickable = true;
            _c.MouseEntered += OnHover;
            _c.MouseExited += OnHoverEnd;
            _c.QueueRedraw();
            _ = _c.UpdateCoverState(_c.GetWorld2D().DirectSpaceState);
        }

        public void PhysicsProcess(double delta, Character character)
        {
            if (CombatSystem.NavReady() && CombatSystem.GetMovesRemaining(character) > 0)
            {
                List<Character> enemiesInSense = [.. character.GetSenseArea().GetOverlappingBodies().Where(body => body is Character).Cast<Character>().Where(c => HostilitySystem.GetHostility(character.CharacterData, c.CharacterData))];
                var closestEnemy = enemiesInSense.OrderBy(c => c.GlobalPosition.DistanceTo(character.GlobalPosition)).FirstOrDefault();
                if (closestEnemy != null)
                {
                    var distance = closestEnemy.GlobalPosition.DistanceTo(character.GlobalPosition);
                    if (distance > character.CharacterData.AttackRange)
                    {
                        var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), character.GlobalPosition, closestEnemy.GlobalPosition, true);
                        if (path.Length == 0)
                        {
                            var targetVec = closestEnemy.GlobalPosition - character.GlobalPosition;
                            character.ControllerState = new CombatNavState(character, [character.GlobalPosition + targetVec.Normalized() * character.MovementRange]);
                        }
                        else
                        {
                            character.ControllerState = new CombatNavState(character, Math.TrimPath(character.GlobalPosition, path, character.MovementRange));
                        }
                    }
                    else if (!_pawnAttacking)
                    {
                        _pawnAttacking = true;
                        // In range, attack.
                        character.BeginAttackAnim(
                            character.GlobalPosition.DirectionTo(closestEnemy.Collider.GlobalPosition),
                            () => character.BasicAttackAbility.Activate(character, closestEnemy, character.GetProjectileSpawnPoint(), closestEnemy.Collider.GlobalPosition,
                                    () => _pawnAttacking = false));
                    }
                }
                else
                {
                    CombatSystem.PassTurn(character);
                }
            }
            character.QueueRedraw();
        }

        public void Process(double delta, Character character)
        {
        }

        private void OnCharacterDraw()
        {
            if (_hovered)
            {
                _c.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(1.0f, 0.0f, 0.0f), filled: false);
            }
        }

        private void OnHover()
        {
            _hovered = true;
            HoverSystem.SetHovered(_c.CharacterData.ResourcePath);
            _c.QueueRedraw();
        }

        private void OnHoverEnd()
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
            _c.QueueRedraw();
        }
    }

    protected class NavState(Vector2[] path, ICharacterState nextState, Action onComplete = null, float speed = -1, float tolerance = 1.0f) : ICharacterState
    {
        private int _currentPoint;

        public void Process(double delta, Character character)
        {

        }

        public void PhysicsProcess(double delta, Character character)
        {
            var targetPoint = path[_currentPoint];
            if (character.GlobalPosition.DistanceTo(targetPoint) <= tolerance)
            {
                if (_currentPoint + 1 < path.Length)
                {
                    _currentPoint += 1;
                }
                else
                {
                    character.SetWalkAnimState(Vector2.Zero);
                    character.ControllerState = nextState;
                    onComplete();
                    return;
                }
            }

            var targetVector = targetPoint - character.GlobalPosition;
            var vel = targetVector.Normalized() * (speed > 0.0f ? speed : character.Speed);
            character.Velocity += vel;
            character.SetWalkAnimState(vel);
            _ = character.MoveAndSlide();
        }

        public void OnTransition(Character character) { }
    }

    protected class CombatNavState : ICharacterState
    {
        private readonly Character _character;
        private readonly Action _onComplete;

        private readonly Vector2[] _path;
        private int _currentPoint;

        public CombatNavState(Character character, Vector2[] path, Action onComplete = null)
        {
            _character = character;
            _path = path;
            _onComplete = onComplete;
            HoverSystem.SetUnhovered(character.CharacterData.ResourcePath);
            character._coverBadge.Hide();
        }

        public void OnTransition(Character character) { }

        public void PhysicsProcess(double delta, Character character)
        {
            var targetPoint = _path[_currentPoint];
            if (_character.GlobalPosition.DistanceTo(targetPoint) <= 1.0f)
            {
                _character.GlobalPosition = targetPoint;
                if (_currentPoint + 1 < _path.Length)
                {
                    _currentPoint += 1;
                }
                else
                {
                    CombatSystem.AttemptMove(character);
                    _ = character.UpdateCoverState(character.GetWorld2D().DirectSpaceState);
                    _character.SetAnimState(AnimState.Idle);
                    _character.ControllerState = _character.SetCombatState();
                    _onComplete?.Invoke();
                }
            }
            else
            {
                var targetVector = targetPoint - _character.GlobalPosition;
                var vel = targetVector.Normalized() * _character.Speed;
                _character.Velocity += vel;
                _character.SetWalkAnimState(vel);
                _ = _character.MoveAndSlide();
            }
        }

        public void Process(double delta, Character character) { }
    }

    protected class NavToCharacterState(Character self, Character target, ICharacterState prevState, Action onComplete, float speed, float tolerance) : ICharacterState
    {
        public void OnTransition(Character character) { }

        public void PhysicsProcess(double delta, Character character)
        {
            var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), self.GlobalPosition, target.GlobalPosition, true);
            var substate = new NavState([.. path.TakeLast(path.Length - 1)], prevState, onComplete, speed, tolerance);
            substate.PhysicsProcess(delta, self);
        }

        public void Process(double delta, Character character) { }
    }

    public interface ICharacterState
    {
        void Process(double delta, Character character);
        void PhysicsProcess(double delta, Character character);

        void OnTransition(Character character);
    }

    private ICharacterState _controllerState = new PawnState();

    public ICharacterState ControllerState
    {
        get => _controllerState;
        set
        {
            _controllerState.OnTransition(this);
            _controllerState = value;
        }
    }

    public bool IsNamedCharacter() => CharacterData != null;

    public void AddStatusEffect(StatusEffect effect)
    {
        bool alreadyHas = _statusEffects.ContainsKey(effect);
        if (!alreadyHas)
        {
            _statusEffects[effect] = new StackStatus();
        }
        _ = effect.OnStackAdd(this);
        _statusEffects[effect].NumStacks += 1;
        if (effect.IsPermanent)
        {
            return;
        }

        var timer = CombatSystem.CreateTimer(effect.Duration, () =>
        {
            _ = effect.OnStackRemove(this);
            _statusEffects[effect].NumStacks -= 1;
            _statusEffects[effect].StackTimers =
                [.. _statusEffects[effect].StackTimers.Where(CombatSystem.TimerActive)];
        }, this);
        _statusEffects[effect].StackTimers.Add(timer);
    }

    public void RemoveStatusEffect(StatusEffect effect) => _statusEffects[effect].NumStacks -= 1;

    public override void _Ready()
    {
        _anim = GetNode<AnimationPlayer>("AnimationPlayer");
        _anim.AnimationFinished += OnAnimationFinished;
        _coverBadge = GetNode<Sprite2D>("CoverBadge");
        Collider = GetNode<CollisionShape2D>("MainCollider");
        ActionPip1 = GetNode<Sprite2D>("ActionPip");
        ActionPip2 = GetNode<Sprite2D>("ActionPip2");
        SenseArea = GetNode<Area2D>("SenseArea");
        MainSprite = GetNode<Sprite2D>("MainSprite");
        HealthLabel = GetNode<Label>("HealthLabel");
        _projectileSpawnPoint = GetNode<Marker2D>("ProjectileSpawnPoint");
        _interactableRange = GetNode<Area2D>("InteractableRange");

        // Only hook into gameplay systems if we are a named, non-background character.
        if (IsNamedCharacter())
        {
            HealthLabel.Text = $"{CharacterData.CurrentHitpoints} / {CharacterData.MaxHitpoints}";
            EquipmentSystem.SetEquipment(CharacterData.ResourcePath, CharacterData.StartingEquipment);
            FactionSystem.SetFaction(CharacterData, CharacterData.InitialFaction);
            InventorySystem.SetInventory(CharacterData.ResourcePath, [.. _initialInventory]);
            CharacterSystem.SetInstance(CharacterData, this);
            HealthSystem.SetCurrentHitpoints(CharacterData.ResourcePath, CharacterData.CurrentHitpoints);
            CombatSystem.CombatStartHandlers += OnCombatStarted;
            CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
            CombatSystem.CombatEnded += OnCombatEnded;
            HealthSystem.DeathEventHandlers += OnDeath;
            HealthSystem.DamageEventHandlers += OnDamage;
            HostilitySystem.HostilityChangeHandlers += OnHostilityChanged;
            SenseArea.BodyEntered += OnBodyEnteredSenseArea;
            FactionSystem.FactionChangeHandlers += OnFactionChange;
            FactionSystem.FactionRelationChangeHandlers += OnFactionRelationChange;

            Abilities.Insert(0, BasicAttackAbility);
        }

        NavObstacle = GetNode<NavigationObstacle2D>("NavigationObstacle2D");

        Speed = CharacterData?.Speed ?? Speed;
        MovementRange = CharacterData?.MovementRange ?? MovementRange;
        _statusEffectContainer = GetNode<StatusEffectContainer>("StatusEffectContainer");
    }

    public override void _Process(double delta)
    {
        ControllerState.Process(delta, this);
        UpdateAnimation();
        UpdateUi();
    }
    public void UpdateUi() => _statusEffectContainer.SetStatusEffects(_statusEffects);

    public virtual void UpdateAnimation()
    {
        string desiredAnim;

        switch (CurrentAnimState)
        {
            case AnimState.Idle:
                desiredAnim = "idle";
                break;
            case AnimState.WalkNorth:
                desiredAnim = "walk_north";
                break;
            case AnimState.WalkSouth:
                desiredAnim = "walk_south";
                break;
            case AnimState.WalkEast:
                MainSprite.FlipH = false;
                desiredAnim = "walk_h";
                break;
            case AnimState.WalkWest:
                MainSprite.FlipH = true;
                desiredAnim = "walk_h";
                break;
            case AnimState.Attack:
                desiredAnim = "attack";
                break;
            case AnimState.Sitting:
                desiredAnim = "sit";
                break;
            case AnimState.Talking:
                desiredAnim = "talking";
                break;
            default:
                desiredAnim = "idle";
                break;
        }

        if (desiredAnim != _anim.CurrentAnimation && _anim.HasAnimation(desiredAnim))
        {
            _anim.Play(desiredAnim);
        }

        if (CurrentAnimState != AnimState.Sitting)
        {
            SetCollision(true);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = Vector2.Zero;
        foreach (var push in CurrentPushes)
        {
            Velocity += push.Velocity;
            push.Duration -= delta;
            if (push.Duration <= 0)
            {
                push.OnFinish();
            }
        }
        _ = MoveAndSlide();
        _ = CurrentPushes.RemoveAll(push => push.Duration <= 0);
        Velocity = Vector2.Zero;
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

    public bool MeetsEquipRequirements(Item item) => item.AttributeRequirements.MeetsRequirements(FinalAttributes) &&
               item.SkillRequirements.Proficiencies.All(CharacterData.BaseSkills.Proficiencies.Contains);

    public Vector2 GetClosestOnCollSurface(Vector2 sourcePoint)
    {
        _ = sourcePoint - Position;
        var supportPoint = Collider.Shape.GetRect().GetSupport(sourcePoint);
        return supportPoint;
    }

    public Area2D GetSenseArea() => SenseArea;

    public virtual void OnBodyEnteredSenseArea(Node2D body)
    {
        if (body is Character character &&
            character.IsNamedCharacter() &&
            character.CharacterData.ResourcePath != CharacterData.ResourcePath &&
            HostilitySystem.GetHostility(character.CharacterData, CharacterData))
        {
            if (CombatSystem.IsInCombat(this))
            {
                CombatSystem.JoinCombat(character);
            }
            else
            {
                CombatSystem.BeginCombat(this, character);
            }
        }
    }

    public virtual void OnCombatStarted(CombatStartEvent e)
    {
        if (e.Participants.Contains(this))
        {
            HealthLabel.Show();
            ControllerState = SetCombatState();
        }
    }

    public virtual void OnCombatJoined(Character c)
    {
        if (c == this)
        {
            HealthLabel.Show();
            ControllerState = SetCombatState();
        }
    }

    public virtual ICharacterState SetCombatState()
    {
        if (FactionSystem.TryGetFaction(CharacterData, out Faction fact) && fact == Faction.Player)
        {
            return new PawnState();
        }
        else
        {
            return new CombatState(this);
        }
    }

    public virtual void OnCombatEnded()
    {
        if (ControllerState is CombatState or CombatNavState)
        {
            ControllerState = new PawnState();
        }
        _coverBadge.Visible = false;
        if (ActionPip1 != null && ActionPip2 != null)
        {
            ActionPip1.Visible = false;
            ActionPip2.Visible = false;
        }
        HealthLabel.Hide();
        QueueRedraw();
    }

    public virtual void OnDeath(DeathEvent e)
    {
        if (e.Deceased.CharacterData.ResourcePath.Equals(CharacterData.ResourcePath))
        {
            Despawn();
        }
    }

    public bool IsTakingCover() => _coverState.CoverLevelWest > 0 || _coverState.CoverLevelEast > 0 || _coverState.CoverLevelSouth > 0 || _coverState.CoverLevelNorth > 0;

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
        _coverBadge.Visible = IsTakingCover();

        return ret;
    }

    public virtual void OnDamage(DamageEvent e)
    {
        string recipientId = e.Recipient.CharacterData.ResourcePath;
        if (recipientId == CharacterData.ResourcePath)
        {
            HealthLabel.Text = $"{HealthSystem.GetCurrentHitpoints(recipientId)} / {CharacterData.MaxHitpoints}";
        }
    }

    public virtual void OnHostilityChanged(CharacterData _, CharacterData _1, bool _2) => SenseArea.GetOverlappingBodies().ToList().ForEach(OnBodyEnteredSenseArea);

    public virtual void OnFactionRelationChange(Faction _, Faction _1) => SenseArea.GetOverlappingBodies().ToList().ForEach(OnBodyEnteredSenseArea);
    public virtual void OnFactionChange(string _, Faction _1) => SenseArea.GetOverlappingBodies().ToList().ForEach(OnBodyEnteredSenseArea);

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
        var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), GlobalPosition, point, true);
        ControllerState = path.Length == 0 ? new NavState([GlobalPosition, point], new PawnState(), onComplete, speed > 0 ? speed : Speed) : new NavState(path, new PawnState(), onComplete, speed > 0 ? speed : Speed);
    }

    private void CleanDelegates()
    {
        CombatSystem.CombatStartHandlers -= OnCombatStarted;
        CombatSystem.CharacterJoinedCombatHandlers -= OnCombatJoined;
        CombatSystem.CombatEnded -= OnCombatEnded;
        HealthSystem.DeathEventHandlers -= OnDeath;
        HealthSystem.DamageEventHandlers -= OnDamage;
        HostilitySystem.HostilityChangeHandlers -= OnHostilityChanged;
        _anim.AnimationFinished -= OnAnimationFinished;
        FactionSystem.FactionChangeHandlers -= OnFactionChange;
        FactionSystem.FactionRelationChangeHandlers -= OnFactionRelationChange;
    }

    public void SetFacing(Vector2 dir) => MainSprite.FlipH = dir.X < 0;

    public virtual void OnAnimationFinished(StringName animationName)
    {
        if (animationName.Equals("attack"))
        {
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
        if (state != CurrentAnimState)
        {
            // Reset
            _anim.Play("RESET");
            _anim.Advance(0);
            CurrentAnimState = state;
        }
    }


    public void BeginAttackAnim(Vector2 direction, Action onComplete = null)
    {
        _onAttackComplete = onComplete;
        SetAttackAnimState(direction);
    }

    public void IssueCombatMove(Vector2[] path, Action onComplete = null) => ControllerState = new CombatNavState(this, path, onComplete);

    public void SitOn(Prop prop)
    {
        if (prop is not FurnitureProp { Occupied: false } furniture)
        {
            return;
        }

        SetAnimState(AnimState.Sitting);

        Vector2 seatBottomLeft = furniture.GetSeatRegionCenter();
        furniture.Occupied = true;
        _occupiedProp = furniture;
        GlobalPosition = seatBottomLeft;
        SetCollision(false);
        _sitting = true;
    }

    public void SetSprite(Texture2D sprite) => MainSprite.Texture = sprite;

    public void SetCollisionOverride(bool @override)
    {
        _collisionOverride = @override;
        SetCollision(!Collider.Disabled);
    }

    public void SetCollision(bool enabled) => Collider.Disabled = !enabled || !_collisionOverride;

    public void SetIdle()
    {
        ControllerState = new PawnState();
        SetAnimState(AnimState.Idle);
    }

    public void WalkToCharacter(Character instance, Action onComplete, float speed, float tolerance = 1.0f) =>
        ControllerState = new NavToCharacterState(this, instance, new PawnState(), onComplete, speed, tolerance);

    public void SetTalking()
    {
        ControllerState = new PawnState();
        SetAnimState(AnimState.Talking);
    }

    public bool IsSeated() => _sitting;

    public Vector2 GetProjectileSpawnPoint() => _projectileSpawnPoint.GlobalPosition;

    public void AddPush(Push push) => CurrentPushes.Add(push);

    public void SetPawnState() => ControllerState = new PawnState();
}

