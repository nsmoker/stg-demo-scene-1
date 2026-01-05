using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;

public struct AttackEvent
{
    public Character attacker;
    public Character target;
    public bool hit;
    // Only use if hit is true.
    public int targetDamage;
}

public struct CombatStartEvent
{
    public List<string> participants;
    public CharacterData initiator;
}

struct CombatantState
{
    public int MovesRemaining;
    public int ActionsRemaining;
}

public static class CombatSystem
{
    private static readonly Dictionary<string, CombatantState> _currentCombatants = [];
    private static NavigationRegion2D navRegion;

    public delegate void AttackEventHandler(AttackEvent e);
    public delegate void CombatStartHandler(CombatStartEvent e);
    public delegate void CharacterJoinedCombatHandler(CharacterData c);
    public delegate void TurnHandler(List<string> side);

    private static AttackEventHandler attackHandlers;
    private static CombatStartHandler combatStartHandler1;
    private static CharacterJoinedCombatHandler characterJoinedCombatHandler1;

    private static TurnHandler turnHandler;

    public static NavigationRegion2D NavRegion { get => navRegion; set 
    {
        if (navRegion != null)
        {
            NavigationServer2D.MapChanged -= OnNavRebakeFinished;
        }
        navRegion = value; 
        NavigationServer2D.MapChanged += OnNavRebakeFinished;
    }}

    private static bool _pathingReady = true;

    public static AttackEventHandler AttackHandlers { get => attackHandlers; set => attackHandlers = value; }
    public static CombatStartHandler CombatStartHandlers { get => combatStartHandler1; set => combatStartHandler1 = value; }
    public static CharacterJoinedCombatHandler CharacterJoinedCombatHandlers { get => characterJoinedCombatHandler1; set => characterJoinedCombatHandler1 = value; }
    public static TurnHandler TurnHandlers { get => turnHandler; set => turnHandler = value; }
    public static Action CombatEnded { get => combatEnded; set => combatEnded = value; }

    private static System.Action combatEnded;

    private static List<List<string>> _sides = [];

    private static int _sideMoving = 0;

    private static void OnNavRebakeFinished(Rid rid)
    {
        if (rid == navRegion.GetNavigationMap())
        {
            _pathingReady = true;
        }
    }

    private static void OnCharacterDeath(DeathEvent e)
    {
        var deceasedId = e.deceased.CharacterData.ResourcePath;
        _currentCombatants.Remove(deceasedId);
        _sides.Find(x => x.Contains(deceasedId)).Remove(deceasedId);
        _sides = [.. _sides.Where(x => !(x.Count == 0))];
        if (_currentCombatants.Count == 1)
        {
            CombatEnded?.Invoke();
        }
    }

    public static bool NavReady()
    {
        return _pathingReady && navRegion.NavigationPolygon != null;
    }

    private static void EndTurn()
    {
        var currentSide = _sides[_sideMoving];
        currentSide.ForEach(x => CharacterSystem.GetInstance(x).NavObstacle.AffectNavigationMesh = true);
        _sideMoving = (_sideMoving + 1) % _sides.Count;
        currentSide = _sides[_sideMoving];
        _pathingReady = false;
        currentSide.ForEach(x =>
        {
            var instance = CharacterSystem.GetInstance(x);
            instance.NavObstacle.AffectNavigationMesh = false;
            var state = new CombatantState
            {
                ActionsRemaining = instance.CharacterData.CombatActions,
                MovesRemaining = instance.CharacterData.CombatMoves
            };
            _currentCombatants[x] = state;
        });
        NavRegion.BakeNavigationPolygon();
        TurnHandlers?.Invoke(currentSide);
    }

    public static void BeginCombat(CharacterData initiator, CharacterData opponent)
    {
        if (_currentCombatants.Count == 0)
        {
            _currentCombatants.Add(initiator.ResourcePath, new CombatantState { MovesRemaining = initiator.CombatMoves, ActionsRemaining = initiator.CombatActions });
            _currentCombatants.Add(opponent.ResourcePath, new CombatantState { MovesRemaining = opponent.CombatMoves, ActionsRemaining = opponent.CombatActions });
            _sides.Add([initiator.ResourcePath]);
            _sides.Add([opponent.ResourcePath]);

            CombatStartEvent e = new()
            {
                initiator = initiator,
                participants = [.. _currentCombatants.Keys]
            };
            CharacterSystem.GetInstance(initiator.ResourcePath).NavObstacle.AffectNavigationMesh = false;
            CharacterSystem.GetInstance(opponent.ResourcePath).NavObstacle.AffectNavigationMesh = true;
            _pathingReady = false;
            navRegion.BakeNavigationPolygon();


            HealthSystem.DeathEventHandlers += OnCharacterDeath;
            CombatStartHandlers?.Invoke(e);
            _sideMoving = 0;
            TurnHandlers?.Invoke(_sides[0]);
        }
    }

    public static void JoinCombat(CharacterData toJoin)
    {
        if (!_currentCombatants.ContainsKey(toJoin.ResourcePath))
        {
            _currentCombatants.Add(toJoin.ResourcePath, new CombatantState { MovesRemaining = toJoin.CombatMoves, ActionsRemaining = toJoin.CombatActions });
            // New participant fights on the first side in which they are hostile to noone.
            var joiningSide = -1;
            for (int i = 0; i < _sides.Count; ++i)
            {
                var side = _sides[i];
                if (!side.Any(x => HostilitySystem.GetHostility(x, toJoin.ResourcePath) || HostilitySystem.GetHostility(toJoin.ResourcePath, x)))
                {
                    side.Add(toJoin.ResourcePath);
                    joiningSide = i;
                    break;
                }
            }
            // If no suitable side is found, make a new one.
            if (joiningSide < 0)
            {
                _sides.Add([toJoin.ResourcePath]);
                joiningSide = _sides.Count - 1;
            }
            var instance = CharacterSystem.GetInstance(toJoin.ResourcePath);
            // Only redo the nav if the new combatant is not currently moving.
            instance.NavObstacle.AffectNavigationMesh = joiningSide != _sideMoving;
            if (joiningSide != _sideMoving)
            {
                NavRegion.BakeNavigationPolygon();
            }
            CharacterJoinedCombatHandlers?.Invoke(toJoin);
        }
    }

    public static float ComputeToHitChance(CharacterData attacker, CharacterData target)
    {
        var attackerInstance = CharacterSystem.GetInstance(attacker.ResourcePath);
        var attackedInstance = CharacterSystem.GetInstance(target.ResourcePath);
        var targetVector = (attackedInstance.GlobalPosition - attackerInstance.GlobalPosition);
        return (20.0f - attackedInstance.ComputeAc(targetVector.Normalized()) + attackerInstance.ComputeToHitMod()) / 20.0f;
    }

    public static void AttemptMove(CharacterData mover)
    {
        var currentSide = _sides[_sideMoving];
        if (currentSide.Contains(mover.ResourcePath))
        {
            var currentState = _currentCombatants[mover.ResourcePath];
            var state = new CombatantState
            {
                MovesRemaining = currentState.MovesRemaining - 1,
                ActionsRemaining = currentState.ActionsRemaining,
            };

            _currentCombatants[mover.ResourcePath] = state;

            if (TurnShouldEnd())
            {
                EndTurn();
            }
        }
    }


    public static void AttemptAttack(CharacterData attacker, CharacterData attacked)
    {
        var attackerInstance = CharacterSystem.GetInstance(attacker.ResourcePath);
        var attackedInstance = CharacterSystem.GetInstance(attacked.ResourcePath);
        var attackerState = _currentCombatants[attacker.ResourcePath];
        var currentSide = _sides[_sideMoving];
        if (currentSide.Contains(attacker.ResourcePath) && attackerState.ActionsRemaining > 0 && attackerState.MovesRemaining > 0)
        {
            var newAttackerState = new CombatantState
            {
                ActionsRemaining = attackerState.ActionsRemaining - 1,
                MovesRemaining = attackerState.MovesRemaining - 1,
            };
            _currentCombatants[attacker.ResourcePath] = newAttackerState;
            var rand = new Random();
            var toHitMod = attackerInstance.ComputeToHitMod();
            var toHitRoll = Math.Max(0, Math.Min(19, rand.Next(20) + toHitMod));

            var targetVector = attackedInstance.GlobalPosition - attackerInstance.GlobalPosition;
            var hitThresh = attackedInstance.ComputeAc(targetVector);

            var damageRoll = rand.Next(10);
            var hit = toHitRoll >= hitThresh;
            AttackHandlers?.Invoke(new AttackEvent
            {
                attacker = attackerInstance,
                target = attackedInstance,
                hit = hit,
                targetDamage = damageRoll,
            });
            if (hit)
            {
                HealthSystem.PostDamageEvent(attackerInstance, attackedInstance, damageRoll);
            }
        }

        if (TurnShouldEnd())
        {
            EndTurn();
        }
    }

    public static void PassTurn(CharacterData character)
    {
        _currentCombatants[character.ResourcePath] = new CombatantState
        {
            ActionsRemaining = 0,
            MovesRemaining = 0,
        };
        
        if (TurnShouldEnd())
        {
            EndTurn();
        }
    }

    public static bool TurnShouldEnd()
    {
        return !_sides[_sideMoving].Any(x => _currentCombatants[x].MovesRemaining > 0 && _currentCombatants[x].ActionsRemaining > 0);
    }

    public static List<string> GetMovingSide()
    {
        return _sides[_sideMoving];
    }

    public static bool IsInCombat(CharacterData c)
    {
        return _currentCombatants.ContainsKey(c.ResourcePath);
    }

    public static int GetActionsRemianing(CharacterData c)
    {
        return _currentCombatants[c.ResourcePath].ActionsRemaining;
    }

    public static int GetMovesRemaining(CharacterData c)
    {
        return _currentCombatants[c.ResourcePath].MovesRemaining;
    }
}
