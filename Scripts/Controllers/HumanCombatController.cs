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
    private Character _character;
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
        if (_character != null)
        {
            _character.Draw -= OnPawnDraw;
            _character.ActionPip1.Visible = _character.ActionPip2.Visible = false;
            _character.QueueRedraw();
        }
        _character = character;
        _ = _character.UpdateCoverState(_character.GetWorld2D().DirectSpaceState);
        _character.ActionPip1.Visible = true;
        _character.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_character) > 1;
        _character.Draw += OnPawnDraw;
        _character.QueueRedraw();
        SceneSystem.GetMasterScene().ActivateAbilityBarForCharacter(_character);
    }

    private bool IsOurTurn => CombatSystem.GetMovingSide().Contains(_character);
    private bool IsActive => _character != null && CombatSystem.IsInCombat(_character) && !DialogueSystem.IsInDialogue() && !_pawnMoving && !_pawnAttacking;


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
        if (_character == null && joiner == SceneSystem.GetMasterScene().GetPlayer())
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
        _character.Draw -= OnPawnDraw;
        CombatSystem.TurnHandlers -= OnTurnBegin;
        _character.QueueRedraw();
    }

    public override void _Ready()
    {
        CombatSystem.CombatStartHandlers += OnCombatStarted;
        CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatEnded += OnCombatEnded;
        CombatSystem.TurnHandlers += OnTurnBegin;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || _pawnTargetingAbility)
        {
            return;
        }

        if (IsActive && IsOurTurn && CombatSystem.GetMovesRemaining(_character) == 0)
        {
            SetCharacter(_side.First(c => CombatSystem.GetMovesRemaining(c) > 0));
        }

        if (Input.IsActionJustPressed("Combat Interact") && CombatSystem.NavReady() && IsOurTurn)
        {
            if (HoverSystem.AnyHovered())
            {
                var hoveredChar = CharacterSystem.GetInstance(HoverSystem.Hovered);
                _pawnAttacking = true;
                _character.BeginAttackAnim(
                    _character.GlobalPosition.DirectionTo(hoveredChar.GlobalPosition),
                    () => _character.BasicAttackAbility.Activate(_character, hoveredChar, _character.GetProjectileSpawnPoint(), hoveredChar.Collider.GlobalPosition, () =>
                                                        _pawnAttacking = false));
            }
            else
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _character.GlobalPosition,
                    _character.GetGlobalMousePosition(),
                    true);
                var len = Math.ComputePathLength(path, _character.GlobalPosition);
                if (len <= _character.MovementRange)
                {
                    _pawnMoving = true;
                    _character.IssueCombatMove(
                        path.Length > 0 ? path : [_character.GetGlobalMousePosition()],
                        () =>
                    {
                        _pawnMoving = false;
                        SetPipVisibility();
                    });
                }
            }
        }
        _character.QueueRedraw();
    }

    private void SetPipVisibility()
    {
        if (CombatSystem.GetMovesRemaining(_character) > 0)
        {
            _character.ActionPip1.Visible = CombatSystem.GetMovesRemaining(_character) > 0;
            _character.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_character) > 1;
        }
        else
        {
            _character.ActionPip1.Hide();
            _character.ActionPip2.Hide();
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
            _character.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(0.0f, 0.0f, 1.0f), filled: false);
            if (CombatSystem.NavReady())
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _character.GlobalPosition,
                    _character.GetGlobalMousePosition(),
                    true);
                var len = Math.ComputePathLength(path, _character.GlobalPosition);
                var inRange = len <= _character.MovementRange;
                var pathTransformed = path.Select(_character.ToLocal).ToArray();
                float dist = path.Length > 1
                    ? len / 16.0f
                    : _character.GlobalPosition.DistanceTo(_character.GetGlobalMousePosition()) / 16.0f;
                var targetPoint = pathTransformed.Length > 1
                    ? pathTransformed[^1]
                    : _character.GetLocalMousePosition();
                Color lineColor = inRange ? new Color(1, 1, 1) : new Color(1, 0, 0);
                if (pathTransformed.Length > 1)
                {
                    _character.DrawPolyline(pathTransformed, lineColor);
                }
                else
                {
                    _character.DrawLine(_character.ToLocal(_character.GlobalPosition), _character.GetLocalMousePosition(), lineColor);
                }

                _character.DrawString(PathFont, targetPoint, $"{dist:0.00}m", fontSize: 8);
            }
        }

        if (HoverSystem.AnyHovered())
        {
            var hovered = CharacterSystem.GetInstance(HoverSystem.Hovered);
            var chance = CombatSystem.ComputeToHitChance(_character, hovered) * 100.0f;
            _character.DrawString(_character.ToHitFont, new Vector2(12.0f, 0.0f) + _character.GetLocalMousePosition(), $"{chance:0}%", fontSize: 8);
        }
    }

    private void OnTurnBegin(List<Character> sideMoving)
    {
        if (_character == null)
        {
            return;
        }
        if (sideMoving.Contains(_character))
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
        _character.QueueRedraw();
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
        _character.QueueRedraw();
    }

    public void OnAbilityTargetingEnd(Ability _)
    {
        Input.SetCustomMouseCursor(null);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _targetingAbility = null;
        _pawnTargetingAbility = false;
        SceneSystem.GetMasterScene().SetAbilityBarReceiveInput(true);
        _character.QueueRedraw();
    }

    public void OnDeathEvent(DeathEvent e)
    {
        _ = _side.Remove(e.Deceased);
        if (e.Deceased == _character)
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

