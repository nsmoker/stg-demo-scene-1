using Godot;
using STGDemoScene1.Scripts.Resources.Abilities;
using STGDemoScene1.Scripts.Resources.Factions;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;
using Character = STGDemoScene1.Scripts.Characters.Character;

namespace STGDemoScene1.Scripts.Controllers;

public partial class HumanCombatController : Node
{
    private Character _pawn;
    private List<Character> _side;
    private bool _pawnMoving;
    private bool _pawnAttacking;
    private bool _pawnTargetingAbility;
    private Ability _targetingAbility;
    private Targeting _targetingCursor;

    [Export]
    public Font PathFont;

    private void SetCharacter(Character character)
    {
        if (_pawn != null)
        {
            _pawn.Draw -= OnPawnDraw;
            _pawn.ActionPip1.Visible = _pawn.ActionPip2.Visible = false;
            _pawn.QueueRedraw();
        }
        _pawn = character;
        _ = _pawn.UpdateCoverState(_pawn.GetWorld2D().DirectSpaceState);
        _pawn.ActionPip1.Visible = true;
        _pawn.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_pawn) > 1;
        _pawn.Draw += OnPawnDraw;
        _pawn.QueueRedraw();
        SceneSystem.GetMasterScene().ActivateAbilityBarForCharacter(_pawn);
    }

    private bool IsOurTurn => CombatSystem.GetMovingSide().Contains(_pawn);
    private bool IsActive => _pawn != null && CombatSystem.IsInCombat(_pawn) && !DialogueSystem.IsInDialogue() && !_pawnMoving && !_pawnAttacking;


    private void ActivateCombatControl() => _pawnMoving = false;

    private void OnCombatStarted(CombatStartEvent e)
    {
        var player = SceneSystem.GetMasterScene().GetPlayer();
        if (e.Participants.Contains(player))
        {
            SetCharacter(player);
            ActivateCombatControl();
        }
    }

    private void OnCombatJoined(Character joiner)
    {
        if (_pawn == null && joiner == SceneSystem.GetMasterScene().GetPlayer())
        {
            SetCharacter(SceneSystem.GetMasterScene().GetPlayer());
            ActivateCombatControl();
        }
        else if (FactionSystem.TryGetFaction(joiner.CharacterData, out Faction faction) && faction == Faction.Player)
        {
            CharacterSystem.GetInstance(joiner.CharacterData).SetPawnState();
        }
    }

    private void OnCombatEnded()
    {
        _pawnMoving = false;
        _pawn.Draw -= OnPawnDraw;
        CombatSystem.TurnHandlers -= OnTurnBegin;
        _pawn.QueueRedraw();
    }

    public override void _Ready()
    {
        CombatSystem.CombatStartHandlers += OnCombatStarted;
        CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatEnded += OnCombatEnded;
        CombatSystem.TurnHandlers += OnTurnBegin;
        HealthSystem.DeathEventHandlers += OnDeathEvent;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || _pawnTargetingAbility)
        {
            return;
        }

        if (IsActive && IsOurTurn && CombatSystem.GetMovesRemaining(_pawn) == 0)
        {
            SetCharacter(_side.First(c => CombatSystem.GetMovesRemaining(c) > 0));
        }

        if (Input.IsActionJustPressed("Combat Interact") && CombatSystem.NavReady() && IsOurTurn)
        {
            if (HoverSystem.AnyHovered())
            {
                var hoveredChar = CharacterSystem.GetInstance(HoverSystem.Hovered);
                var dist = _pawn.GlobalPosition.DistanceTo(hoveredChar.GlobalPosition);
                if (dist > _pawn.CharacterData.AttackRange)
                {
                    return;
                }
                _pawnAttacking = true;
                _pawn.BeginAttackAnim(
                    _pawn.GlobalPosition.DirectionTo(hoveredChar.GlobalPosition),
                    () => _pawn.BasicAttackAbility.Activate(_pawn, hoveredChar, _pawn.GetProjectileSpawnPoint(), hoveredChar.Collider.GlobalPosition, () =>
                                                        _pawnAttacking = false));
            }
            else
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _pawn.GlobalPosition,
                    _pawn.GetGlobalMousePosition(),
                    true);
                var len = Math.ComputePathLength(path, _pawn.GlobalPosition);
                if (len <= _pawn.MovementRange)
                {
                    _pawnMoving = true;
                    _pawn.IssueCombatMove(
                        path.Length > 0 ? path : [_pawn.GetGlobalMousePosition()],
                        () =>
                    {
                        _pawnMoving = false;
                        SetPipVisibility();
                    });
                }
            }
        }
        _pawn.QueueRedraw();
    }

    private void SetPipVisibility()
    {
        if (CombatSystem.GetMovesRemaining(_pawn) > 0)
        {
            _pawn.ActionPip1.Visible = CombatSystem.GetMovesRemaining(_pawn) > 0;
            _pawn.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_pawn) > 1;
        }
        else
        {
            _pawn.ActionPip1.Hide();
            _pawn.ActionPip2.Hide();
        }
    }

    private void OnPawnDraw()
    {
        if (!IsActive || _pawnTargetingAbility)
        {
            return;
        }

        if (IsOurTurn && !HoverSystem.AnyHovered())
        {
            _pawn.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(0.0f, 0.0f, 1.0f), filled: false);
            if (CombatSystem.NavReady())
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _pawn.GlobalPosition,
                    _pawn.GetGlobalMousePosition(),
                    true);
                var len = Math.ComputePathLength(path, _pawn.GlobalPosition);
                var inRange = len <= _pawn.MovementRange;
                var pathTransformed = path.Select(_pawn.ToLocal).ToArray();
                float dist = path.Length > 1
                    ? len / 16.0f
                    : _pawn.GlobalPosition.DistanceTo(_pawn.GetGlobalMousePosition()) / 16.0f;
                var targetPoint = pathTransformed.Length > 1
                    ? pathTransformed[^1]
                    : _pawn.GetLocalMousePosition();
                Color lineColor = inRange ? new Color(1, 1, 1) : new Color(1, 0, 0);
                if (pathTransformed.Length > 1)
                {
                    _pawn.DrawPolyline(pathTransformed, lineColor);
                }
                else
                {
                    _pawn.DrawLine(_pawn.ToLocal(_pawn.GlobalPosition), _pawn.GetLocalMousePosition(), lineColor);
                }

                _pawn.DrawString(PathFont, targetPoint, $"{dist:0.00}m", fontSize: 8);
            }
        }

        if (HoverSystem.AnyHovered())
        {
            var hovered = CharacterSystem.GetInstance(HoverSystem.Hovered);
            var chance = CombatSystem.ComputeToHitChance(_pawn, hovered) * 100.0f;
            _pawn.DrawString(_pawn.ToHitFont, new Vector2(12.0f, 0.0f) + _pawn.GetLocalMousePosition(), $"{chance:0}%", fontSize: 8);
        }
    }

    private void OnTurnBegin(List<Character> sideMoving)
    {
        if (_pawn == null)
        {
            return;
        }
        if (sideMoving.Contains(_pawn))
        {
            _side = sideMoving;
        }
        foreach (var part in sideMoving)
        {
            if (FactionSystem.TryGetFaction(part.CharacterData, out Faction faction) && faction == Faction.Player)
            {
                part.SetPawnState();
            }

        }
        SetPipVisibility();
        SceneSystem.GetMasterScene().SetAbilityBarVisible(IsOurTurn);
        _pawn.QueueRedraw();
    }

    public void OnAbilityTargetingStart(Ability ability, Character caster)
    {
        _targetingAbility = ability;
        _pawnTargetingAbility = true;
        var masterScene = SceneSystem.GetMasterScene();
        var swCursor = ability.TargetingScene != null ? ability.TargetingScene.Instantiate<Targeting>() : new Targeting();
        swCursor.ShouldAnimate = ability.TargetingIsAnimated;
        swCursor.Tex = ability.TargetingSprite;
        swCursor.Caster = caster;
        swCursor.Ability = ability;
        masterScene.AddChild(swCursor);
        _targetingCursor = swCursor;

        masterScene.SetAbilityBarReceiveInput(false);
        _pawn.QueueRedraw();
    }

    public void OnAbilityTargetingEnd(Ability _)
    {
        Input.SetCustomMouseCursor(null);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _targetingAbility = null;
        _pawnTargetingAbility = false;
        SceneSystem.GetMasterScene().SetAbilityBarReceiveInput(true);
        _pawn.QueueRedraw();
    }

    public void OnDeathEvent(DeathEvent e)
    {
        _ = _side.Remove(e.Deceased);
        if (e.Deceased == _pawn)
        {
            if (_side.Count > 0)
            {
                SetCharacter(_side[0]);
            }
            else
            {
                OnCombatEnded();
            }
        }
    }
}

