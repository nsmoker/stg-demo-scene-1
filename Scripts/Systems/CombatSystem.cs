using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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

public struct CombatTimerHandle
{
    internal int Index;
    internal CombatTimerHandle(int index)
    {
        Index = index;
        IsValid = true;
    }

    public bool IsValid;
};

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

    public static NavigationRegion2D NavRegion
    {
        get => navRegion; set
        {
            navRegion = value;
        }
    }

    private static bool _pathingReady = true;

    public static AttackEventHandler AttackHandlers { get => attackHandlers; set => attackHandlers = value; }
    public static CombatStartHandler CombatStartHandlers { get => combatStartHandler1; set => combatStartHandler1 = value; }
    public static CharacterJoinedCombatHandler CharacterJoinedCombatHandlers { get => characterJoinedCombatHandler1; set => characterJoinedCombatHandler1 = value; }
    public static TurnHandler TurnHandlers { get => turnHandler; set => turnHandler = value; }
    public static Action CombatEnded { get => combatEnded; set => combatEnded = value; }

    private static Action combatEnded;

    private static List<List<string>> _sides = [];

    private static List<CombatTimer> _combatTimers = [];

    private static int _sideMoving = 0;

    private static Label _combatStatusLabel;

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
    }

    public static bool NavReady()
    {
        return _pathingReady && navRegion.NavigationPolygon != null;
    }

    private static void EndTurn()
    {
        _sides = [.. _sides.Where(x => x.Count > 0)];
        if (_sides.Count < 2)
        {
            EndCombat();
        }
        else
        {
            var currentSide = _sides[_sideMoving];
            currentSide.ForEach(x => CharacterSystem.GetInstance(x).NavObstacle.AffectNavigationMesh = true);
            _sideMoving = (_sideMoving + 1) % _sides.Count;
            currentSide = _sides[_sideMoving];
            if (currentSide.Any(x => CharacterSystem.GetInstance(x) is Player))
            {
                _combatStatusLabel.Text = "YOUR TURN";
            }
            else
            {
                _combatStatusLabel.Text = "ENEMY TURN";
            }
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
            TriggerTimers(currentSide);
            TurnHandlers?.Invoke(currentSide);
        }
    }

    private static void EndCombat()
    {
        SceneSystem.GetMasterScene().SetAbilityBarVisible(false);
        combatEnded?.Invoke();
        _combatStatusLabel.Visible = false;
        _currentCombatants.Clear();
        _sides.Clear();
        _sideMoving = 0;
        NavRegion.BakeNavigationPolygon();
    }

    public static void Initialize()
    {
        NavigationServer2D.MapChanged += OnNavRebakeFinished;
    }

    public static void BeginCombat(CharacterData initiator, CharacterData opponent)
    {
        var scene = (Godot.Engine.GetMainLoop() as SceneTree)
            .CurrentScene as MasterScene;
        _combatStatusLabel = scene.GetCombatStatusLabel();
        scene.ActivateAbilityBarForCharacter(scene.GetPlayer());
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
            var initiatiorInstance = CharacterSystem.GetInstance(initiator.ResourcePath);
            initiatiorInstance.NavObstacle.AffectNavigationMesh = false;
            var opponentInstance = CharacterSystem.GetInstance(opponent.ResourcePath);
            opponentInstance.NavObstacle.AffectNavigationMesh = true;
            _pathingReady = false;
            navRegion.BakeNavigationPolygon();

            if (initiatiorInstance is Player)
            {
                _combatStatusLabel.Text = "YOUR TURN";
            }
            else
            {
                _combatStatusLabel.Text = "ENEMY TURN";
            }
            HealthSystem.DeathEventHandlers += OnCharacterDeath;
            _combatStatusLabel.Visible = true;
            CombatStartHandlers?.Invoke(e);
            _sideMoving = 0;
            TurnHandlers?.Invoke(_sides[0]);
        }
    }

    /// <summary>
    ///  Create a timer based on a number of turns relative to a particular character. Timers will automatically be removed when they expire or when the character they are relative to
    /// exits combat.
    /// </summary>
    /// <param name="duration">The duration in turns of the timer.</param>
    /// <param name="onTimeout">A callback that will be invoked when the timer finishes.</param>
    /// <param name="RelativeTo">The character the timer is relative to. The timer ticks when this character's turn starts.</param>
    /// <returns>An opaque handle to the timer which can be passed to `RemoveTimer` for premature removal.</returns>
    public static CombatTimerHandle CreateTimer(int duration, Action onTimeout, CharacterData RelativeTo)
    {
        var timer = new CombatTimer()
        {
            TurnsRemaining = duration,
            Timeout = onTimeout,
            RelativeToCharacter = RelativeTo,
        };
        _combatTimers.Add(timer);
        return new CombatTimerHandle(_combatTimers.Count - 1);
    }

    /// <summary>
    /// Removes the timer referred to by `handle`. It is an error to pass a handle to this function that was not returned by `CreateTimer` or that has already been removed.
    /// </summary>
    /// <param name="handle">A handle to the timer to clear.</param>
    public static void RemoveTimer(CombatTimerHandle handle)
    {
        _combatTimers.RemoveAt(handle.Index);
    }

    private static void TriggerTimers(List<string> side)
    {
        for (int i = 0; i < _combatTimers.Count; ++i)
        {
            CombatTimer timer = _combatTimers[i];
            if (side.Any(x => x == timer.RelativeToCharacter.ResourcePath))
            {
                timer.TurnsRemaining -= 1;
                if (timer.TurnsRemaining <= 0)
                {
                    timer.Timeout?.Invoke();
                }
            }
        }

        _combatTimers = [.. _combatTimers.Where(x => x.TurnsRemaining > 0)];
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


    public static void AttemptAttack(CharacterData attacker, CharacterData attacked, DamageRoll damageRoll)
    {
        Character attackerInstance = CharacterSystem.GetInstance(attacker.ResourcePath);
        Character attackedInstance = CharacterSystem.GetInstance(attacked.ResourcePath);
        CombatantState attackerState = _currentCombatants[attacker.ResourcePath];
        var currentSide = _sides[_sideMoving];
        if (currentSide.Contains(attacker.ResourcePath) && attackerState.ActionsRemaining > 0 && attackerState.MovesRemaining > 0)
        {
            CombatantState newAttackerState = new()
            {
                ActionsRemaining = attackerState.ActionsRemaining - 1,
                MovesRemaining = attackerState.MovesRemaining - 1,
            };
            _currentCombatants[attacker.ResourcePath] = newAttackerState;
            Random rand = new();
            int toHitMod = attackerInstance.ComputeToHitMod();
            int toHitRoll = Math.Max(0, Math.Min(19, rand.Next(20) + toHitMod));

            Vector2 targetVector = attackedInstance.GlobalPosition - attackerInstance.GlobalPosition;
            int hitThresh = attackedInstance.ComputeAc(targetVector);

            bool hit = toHitRoll >= hitThresh;
            var roll = damageRoll.Roll();
            AttackHandlers?.Invoke(new AttackEvent
            {
                attacker = attackerInstance,
                target = attackedInstance,
                hit = hit,
                targetDamage = roll,
            });
            if (hit)
            {
                HealthSystem.PostDamageEvent(attackerInstance, attackedInstance, roll);
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

    public static List<Character> GetCharactersInRange(Vector2 position, Shape2D shape)
    {
        var spaceState = navRegion.GetWorld2D().DirectSpaceState;
        var transform = new Transform2D(0, position);
        var shapeQuery = new PhysicsShapeQueryParameters2D()
        {
            Shape = shape,
            Transform = transform,
            CollideWithAreas = false,
            CollideWithBodies = true,
        };
        var result = spaceState.IntersectShape(shapeQuery);
        List<Character> charactersInRange = [];
        foreach (var r in result)
        {
            var collider = r["collider"].As<Node>();
            if (collider != null && collider is Character character)
            {
                charactersInRange.Add(character);
            }
        }
        return charactersInRange;
    }

    public static List<List<string>> GetSides()
    {
        return _sides;
    }
}
