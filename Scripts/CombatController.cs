using Godot;
using System.Collections.Generic;
using System.Linq;
using ArkhamHunters.Scripts;

public partial class CombatController : Node
{
    private Player _player;
    private bool _isOurTurn;
    private bool _inCombat;
    private bool _inDialogue;
    private bool _playerMoving;
    private bool _playerAttacking;

    public void SetPlayer(Player player)
    {
        _player = player;
        CombatSystem.CombatStartHandlers += OnCombatStarted;
        CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatEnded += OnCombatEnded;
        DialogueSystem.OnDialogueStarted += OnDialogueStarted;
        DialogueSystem.OnDialogueComplete += OnDialogueEnded;
    }

    private bool IsActive => _inCombat && !_inDialogue && !_playerMoving && !_playerAttacking;

    private void ActivateCombatControl()
    {
        _inCombat = true;
        _playerMoving = false;
        _isOurTurn = CombatSystem.GetMovingSide().Contains(_player.CharacterData.ResourcePath);
        _player.UpdateCoverState(_player.GetWorld2D().DirectSpaceState);
        _player.ActionPip1.Visible = true;
        _player.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_player.CharacterData) > 1;
        _player.Draw += OnPlayerDraw;
        CombatSystem.TurnHandlers += OnTurnBegin;
        _player.QueueRedraw();
    }

    private void OnCombatStarted(CombatStartEvent e)
    {
        if (e.participants.Contains(_player.CharacterData.ResourcePath))
            ActivateCombatControl();
    }

    private void OnCombatJoined(CharacterData joiner)
    {
        if (joiner.ResourcePath.Equals(_player.CharacterData.ResourcePath))
            ActivateCombatControl();
    }

    private void OnCombatEnded()
    {
        _inCombat = false;
        _playerMoving = false;
        _player.Draw -= OnPlayerDraw;
        CombatSystem.TurnHandlers -= OnTurnBegin;
        _player.QueueRedraw();
    }

    private void OnDialogueStarted(Conversation conversation, int entryPoint)
    {
        _inDialogue = true;
    }

    private void OnDialogueEnded()
    {
        _inDialogue = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive) return;

        if (Input.IsActionJustPressed("Combat Interact") && CombatSystem.NavReady() && _isOurTurn)
        {
            if (HoverSystem.AnyHovered())
            {
                var hoveredChar = CharacterSystem.GetInstance(HoverSystem.Hovered);
                _playerAttacking = true;
                _player.IssueAttack(
                    hoveredChar.CharacterData,
                    _player.GlobalPosition.DirectionTo(hoveredChar.GlobalPosition),
                    () => _playerAttacking = false);
            }
            else
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _player.GlobalPosition,
                    _player.GetGlobalMousePosition(),
                    true, 0x1u);
                var len = Character.ComputePathLength(path, _player.GlobalPosition);
                if (len <= _player.CharacterData.MovementRange)
                {
                    _playerMoving = true;
                    _player.IssueCombatMove(
                        path.Length > 0 ? path : [_player.GetGlobalMousePosition()],
                        () => _playerMoving = false);
                }
            }
        }
        _player.QueueRedraw();
    }

    private void OnPlayerDraw()
    {
        if (!IsActive) return;

        if (_isOurTurn && !HoverSystem.AnyHovered())
        {
            _player.DrawCircle(new Vector2(0.0f, 2.0f), 8.0f, new Color(0.0f, 0.0f, 1.0f), filled: false);
            if (CombatSystem.NavReady())
            {
                var path = NavigationServer2D.MapGetPath(
                    CombatSystem.NavRegion.GetNavigationMap(),
                    _player.GlobalPosition,
                    _player.GetGlobalMousePosition(),
                    true, 0x1u);
                var len = Character.ComputePathLength(path, _player.GlobalPosition);
                var inRange = len <= _player.CharacterData.MovementRange;
                var pathTransformed = path.Select(_player.ToLocal).ToArray();
                float dist = path.Length > 1
                    ? len / 16.0f
                    : _player.GlobalPosition.DistanceTo(_player.GetGlobalMousePosition()) / 16.0f;
                var targetPoint = pathTransformed.Length > 1
                    ? pathTransformed[pathTransformed.Length - 1]
                    : _player.GetLocalMousePosition();
                Color lineColor = inRange ? new Color(1, 1, 1) : new Color(1, 0, 0);
                if (pathTransformed.Length > 1)
                    _player.DrawPolyline(pathTransformed, lineColor);
                else
                    _player.DrawLine(_player.ToLocal(_player.GlobalPosition), _player.GetLocalMousePosition(), lineColor);
                _player.DrawString(_player.PathFont, targetPoint, $"{dist:0.00}m", fontSize: 8);
            }
        }

        if (HoverSystem.AnyHovered())
        {
            var hovered = CharacterSystem.GetInstance(HoverSystem.Hovered);
            var chance = CombatSystem.ComputeToHitChance(_player.CharacterData, hovered.CharacterData) * 100.0f;
            _player.DrawString(_player.ToHitFont, new Vector2(12.0f, 0.0f) + _player.GetLocalMousePosition(), $"{chance:0}%", fontSize: 8);
        }
    }

    private void OnTurnBegin(List<string> sideMoving)
    {
        _isOurTurn = sideMoving.Contains(_player.CharacterData.ResourcePath);
        if (_isOurTurn)
        {
            _player.ActionPip1.Visible = true;
            _player.ActionPip2.Visible = CombatSystem.GetMovesRemaining(_player.CharacterData) > 1;
        }
        else
        {
            _player.ActionPip1.Hide();
            _player.ActionPip2.Hide();
        }
        _player.QueueRedraw();
    }
}
