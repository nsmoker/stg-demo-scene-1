using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources.Factions;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts.Controllers;

public partial class ZombieAiCombatController : Node2D
{
    private class PawnState
    {
        public Character Pawn;
        public bool Attacking;
    }
    private List<PawnState> _pawns = [];

    public override void _Ready()
    {
        CombatSystem.CombatStartHandlers += OnCombatBegin;
        CombatSystem.CharacterJoinedCombatHandlers += OnCombatJoined;
        CombatSystem.CombatEnded += OnCombatEnded;
        CombatSystem.TurnHandlers += TurnBegin;
        HealthSystem.DeathEventHandlers += OnDeathEvent;
    }

    private void OnCombatBegin(CombatStartEvent e)
    {
        foreach (var character in e.Participants)
        {
            if (FactionSystem.TryGetFaction(character.CharacterData, out Faction faction) && faction != Faction.Player)
            {
                _pawns.Add(new PawnState { Pawn = character });
            }
        }
    }

    private void OnCombatJoined(Character joiner)
    {
        if (FactionSystem.TryGetFaction(joiner.CharacterData, out Faction faction) && faction != Faction.Player)
        {
            var pState = new PawnState { Pawn = joiner };
            _pawns.Add(pState);
            if (CombatSystem.GetMovingSide().Contains(joiner))
            {
                TryMovePawn(pState);
            }
        }
    }

    private void OnCombatEnded() => _pawns.Clear();

    private void OnDeathEvent(DeathEvent e) => _pawns = [.. _pawns.Where(c => c.Pawn != e.Deceased)];

    private void TurnBegin(List<Character> side)
    {
        var state = _pawns.FirstOrDefault(s => side.Any(x => s.Pawn == x));
        if (state != null)
        {
            TryMovePawn(state);
        }
    }

    private static List<Vector2> GeneratePossibleMoves(Character pawn)
    {
        Vector2 topLeft = pawn.GlobalPosition + new Vector2(-1, -1).Normalized() * pawn.CharacterData.MovementRange;
        Vector2 bottomRight = pawn.GlobalPosition + new Vector2(1, 1).Normalized() * pawn.CharacterData.MovementRange;
        List<Vector2> ret = [];
        for (float x = topLeft.X; x <= bottomRight.X; x += 8.0f)
        {
            for (float y = topLeft.Y; y <= bottomRight.Y; y += 8.0f)
            {
                var p = new Vector2(x, y);
                if (NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(), pawn.GlobalPosition, p, false).Length > 0)
                {
                    ret.Add(p);
                }
            }
        }

        return ret;
    }

    private void TryMovePawn(PawnState pState)
    {
        if (pState.Attacking)
        {
            return;
        }
        var pawn = pState.Pawn;
        GD.Print($"Trying to move {pawn.CharacterData.CharacterName}.");
        var closestEnemy = pawn.GetClosestEnemy();
        if (closestEnemy != null &&
            closestEnemy.GlobalPosition.DistanceTo(pawn.GlobalPosition) <= pawn.CharacterData.AttackRange)
        {
            GD.Print($"Selected target {closestEnemy.CharacterData.CharacterName}.");
            GD.Print("In range. Attacking.");
            pState.Attacking = true;
            pawn.BeginAttackAnim(
                pawn.GlobalPosition.DirectionTo(closestEnemy.Collider.GlobalPosition),
                () => pawn.BasicAttackAbility.Activate(pawn, closestEnemy,
                    pawn.GetProjectileSpawnPoint(), closestEnemy.Collider.GlobalPosition,
                    () => pState.Attacking = false));
        }
        else
        {
            var moves = GeneratePossibleMoves(pawn);
            var myPrioritiesInLife = pawn.CharacterData.MovePriorities;
            var enemiesInRange = pawn.GetEnemiesInSense();
            foreach (var priority in myPrioritiesInLife)
            {
                var maxScore = 0.0f;
                foreach (var move in moves)
                {
                    var score = priority.ScorePosition(move, pawn, enemiesInRange, GetWorld2D().GetDirectSpaceState());
                    maxScore = Mathf.Max(maxScore, score);
                }

                moves = [.. moves.Where(x => priority.ScorePosition(x, pawn, enemiesInRange, GetWorld2D().GetDirectSpaceState()) >= maxScore)];
            }
            var maxMove = closestEnemy == null ? moves.OrderBy(GlobalPosition.DistanceTo).Last() : moves.OrderBy(closestEnemy.GlobalPosition.DistanceTo).First();

            var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(),
                pawn.GlobalPosition, maxMove, true);
            foreach (var pathNode in path)
            {
                GD.Print(pathNode.ToString());
            }
            if (path.Length > 0)
            {
                pawn.IssueCombatMove(Math.TrimPath(pawn.GlobalPosition, path, pawn.MovementRange));
            }
            else
            {
                GD.Print("Pathing failed. Skipping turn.");
                CombatSystem.PassTurn(pawn);
            }
        }


        pawn.QueueRedraw();
    }
}
