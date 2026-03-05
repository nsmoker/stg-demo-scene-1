using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Resources.Abilities;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;
using Character = STGDemoScene1.Scripts.Characters.Character;

namespace STGDemoScene1.Scripts;

public partial class CombatController : Node
{
    private Character _character;
    private bool _isOurTurn;
    private bool _inCombat;
    private bool _inDialogue;
    private bool _pawnMoving;
    private bool _pawnAttacking;
    private bool _pawnTargetingAbility = false;
    private Ability _targetingAbility;
    private Targeting _targetingCursor;

    [Export]
    public Font PathFont;

    public void SetCharacter(Character character)
    {
        _character = character;
        CombatSystem.CombatStartHandlers += OnCombatStarted;
        CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatEnded += OnCombatEnded;
        DialogueSystem.OnDialogueStarted += OnDialogueStarted;
        DialogueSystem.OnDialogueComplete += OnDialogueEnded;
    }

    private bool IsActive => _inCombat && !_inDialogue && !_pawnMoving && !_pawnAttacking;

    private void ActivateCombatControl()
    {
        _inCombat = true;
        _pawnMoving = false;
        _isOurTurn = CombatSystem.GetMovingSide().Contains(_character.CharacterData.ResourcePath);
        _ = _character.UpdateCoverState(_character.GetWorld2D().DirectSpaceState);
        _character.ActionPip1.Visible = true;
        _character.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_character.CharacterData) > 1;
        _character.Draw += OnPawnDraw;
        CombatSystem.TurnHandlers += OnTurnBegin;
        _character.QueueRedraw();
    }

    private void OnCombatStarted(CombatStartEvent e)
    {
        if (e.participants.Contains(_character.CharacterData.ResourcePath))
        {
            ActivateCombatControl();
        }
    }

    private void OnCombatJoined(CharacterData joiner)
    {
        if (joiner.ResourcePath.Equals(_character.CharacterData.ResourcePath))
        {
            ActivateCombatControl();
        }
    }

    private void OnCombatEnded()
    {
        _inCombat = false;
        _pawnMoving = false;
        _character.Draw -= OnPawnDraw;
        CombatSystem.TurnHandlers -= OnTurnBegin;
        _character.QueueRedraw();
    }

    private void OnDialogueStarted(Conversation _1, int _2) => _inDialogue = true;

    private void OnDialogueEnded() => _inDialogue = false;

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive || _pawnTargetingAbility)
        {
            return;
        }

        if (Input.IsActionJustPressed("Combat Interact") && CombatSystem.NavReady() && _isOurTurn)
        {
            if (HoverSystem.AnyHovered())
            {
                var hoveredChar = CharacterSystem.GetInstance(HoverSystem.Hovered);
                _pawnAttacking = true;
                _character.IssueAttack(
                    hoveredChar.CharacterData,
                    _character.GlobalPosition.DirectionTo(hoveredChar.GlobalPosition),
                    () => _character.BasicAttackAbility.Activate(_character, hoveredChar, _character.GetProjectileSpawnPoint(), hoveredChar.GlobalPosition));
            }
            else
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _character.GlobalPosition,
                    _character.GetGlobalMousePosition(),
                    true, 0x1u);
                var len = Character.ComputePathLength(path, _character.GlobalPosition);
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
        if (_isOurTurn)
        {
            _character.ActionPip1.Visible = true;
            _character.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_character.CharacterData) > 1;
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

        if (_isOurTurn && !HoverSystem.AnyHovered())
        {
            _character.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(0.0f, 0.0f, 1.0f), filled: false);
            if (CombatSystem.NavReady())
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _character.GlobalPosition,
                    _character.GetGlobalMousePosition(),
                    true, 0x1u);
                var len = Character.ComputePathLength(path, _character.GlobalPosition);
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
            var chance = CombatSystem.ComputeToHitChance(_character.CharacterData, hovered.CharacterData) * 100.0f;
            _character.DrawString(_character.ToHitFont, new Vector2(12.0f, 0.0f) + _character.GetLocalMousePosition(), $"{chance:0}%", fontSize: 8);
        }
    }

    private void OnTurnBegin(List<string> sideMoving)
    {
        _isOurTurn = sideMoving.Contains(_character.CharacterData.ResourcePath);
        SetPipVisibility();
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
        swCursor.caster = caster;
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
}

