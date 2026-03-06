using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Resources.Factions;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts.Controllers;

public partial class ZombieAiCombatController : Node
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

    private static void TryMovePawn(PawnState pState)
    {
        var pawn = pState.Pawn;
        GD.Print($"Trying to move {pawn.CharacterData.CharacterName}.");
        List<Character> enemiesInSense =
        [
            .. pawn.GetSenseArea().GetOverlappingBodies().Where(body => body is Character).Cast<Character>()
                .Where(c => HostilitySystem.GetHostility(pawn.CharacterData, c.CharacterData))
        ];
        var closestEnemy = enemiesInSense.OrderBy(c => c.GlobalPosition.DistanceTo(pawn.GlobalPosition))
            .FirstOrDefault();
        if (closestEnemy != null)
        {
            GD.Print($"Selected target {closestEnemy.CharacterData.CharacterName}.");
            var distance = closestEnemy.GlobalPosition.DistanceTo(pawn.GlobalPosition);
            if (distance > pawn.CharacterData.AttackRange)
            {
                GD.Print($"Target {distance}m away, out of range of {pawn.CharacterData.AttackRange}m. Pursuing.");
                var path = NavigationServer2D.MapGetPath(CombatSystem.NavRegion.GetNavigationMap(),
                    pawn.GlobalPosition, closestEnemy.GlobalPosition, true);
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
            else if (pState.Attacking)
            {
                GD.Print("Already attacking. Taking no action.");
            }
            else if (!pState.Attacking)
            {
                GD.Print("In range. Attacking.");
                pState.Attacking = true;
                // In range, attack.
                pawn.BeginAttackAnim(
                    pawn.GlobalPosition.DirectionTo(closestEnemy.Collider.GlobalPosition),
                    () => pawn.BasicAttackAbility.Activate(pawn, closestEnemy,
                        pawn.GetProjectileSpawnPoint(), closestEnemy.Collider.GlobalPosition,
                        () => pState.Attacking = false));
            }
        }
        else
        {
            GD.Print("No enemies in range. Skipping turn.");
            CombatSystem.PassTurn(pawn);
        }

        pawn.QueueRedraw();
    }
}
