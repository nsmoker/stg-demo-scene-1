using Godot;
using STGDemoScene1.Scripts.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Character = STGDemoScene1.Scripts.Characters.Character;
using Player = STGDemoScene1.Scripts.Characters.Player;
using Side = System.Collections.Generic.List<STGDemoScene1.Scripts.Characters.Character>;

namespace STGDemoScene1.Scripts.Systems;


public struct AttackEvent
{
    public Character Attacker;
    public Character Target;
    public bool Hit;
    // Only use if hit is true.
    public int TargetDamage;
}

public struct CombatStartEvent
{
    public List<Character> Participants;
    public Character Initiator;
}

internal struct CombatantState
{
    public int MovesRemaining;
    public int ActionsRemaining;
}

public readonly struct CombatTimerHandle
{
    internal readonly ulong _id;
    internal CombatTimerHandle(ulong id) => _id = id;

};

public static class CombatSystem
{
    private static readonly Dictionary<Character, CombatantState> s_currentCombatants = [];

    private static ulong s_timerCount;

    public delegate void AttackEventHandler(AttackEvent e);
    public delegate void CombatStartHandler(CombatStartEvent e);
    public delegate void CharacterJoinedCombatHandler(Character c);
    public delegate void TurnHandler(Side side);

    public static NavigationRegion2D NavRegion { get; set; }

    private static bool s_pathingReady = true;

    public static AttackEventHandler AttackHandlers { get; set; }
    public static CombatStartHandler CombatStartHandlers { get; set; }
    public static CharacterJoinedCombatHandler CharacterJoinedCombatHandlers { get; set; }
    public static TurnHandler TurnHandlers { get; set; }
    public static Action CombatEnded { get; set; }

    private static List<Side> s_sides = [];

    private static List<CombatTimer> s_combatTimers = [];

    private static int s_sideMoving;

    private static Label s_combatStatusLabel;

    private static void OnNavRebakeFinished(Rid rid)
    {
        if (rid == NavRegion.GetNavigationMap())
        {
            s_pathingReady = true;
        }
    }

    private static void OnCharacterDeath(DeathEvent e) => _ = s_sides.Find(x => x.Contains(e.Deceased)).Remove(e.Deceased);

    public static bool NavReady() => s_pathingReady && NavRegion.NavigationPolygon != null;

    private static void EndTurn()
    {
        s_sides = [.. s_sides.Where(x => x.Count > 0)];
        if (s_sides.Count < 2)
        {
            EndCombat();
        }
        else
        {
            var currentSide = s_sides[s_sideMoving];
            currentSide.ForEach(x => x.NavObstacle.AffectNavigationMesh = true);
            s_sideMoving = (s_sideMoving + 1) % s_sides.Count;
            currentSide = s_sides[s_sideMoving];
            s_combatStatusLabel.Text = currentSide.Any(x => x is Player) ? "YOUR TURN" : "ENEMY TURN";
            s_pathingReady = false;
            currentSide.ForEach(x =>
            {
                x.NavObstacle.AffectNavigationMesh = false;
                var state = new CombatantState
                {
                    ActionsRemaining = x.CharacterData.CombatActions,
                    MovesRemaining = x.CharacterData.CombatMoves
                };
                s_currentCombatants[x] = state;
            });
            NavRegion.BakeNavigationPolygon();
            TriggerTimers(currentSide);
            TurnHandlers?.Invoke(currentSide);
        }
    }

    private static void EndCombat()
    {
        SceneSystem.GetMasterScene().SetAbilityBarVisible(false);
        CombatEnded?.Invoke();
        s_combatStatusLabel.Visible = false;
        s_currentCombatants.Clear();
        s_sides.Clear();
        s_sideMoving = 0;
        NavRegion.BakeNavigationPolygon();
    }

    public static void Initialize()
    {
        NavigationServer2D.MapChanged += OnNavRebakeFinished;
        CombatLog.Initialize();
    }

    public static void BeginCombat(Character initiator, Character opponent)
    {
        s_combatStatusLabel = SceneSystem.GetMasterScene().GetCombatStatusLabel();
        if (s_currentCombatants.Count == 0)
        {
            s_currentCombatants.Add(initiator, new CombatantState { MovesRemaining = initiator.CharacterData.CombatMoves, ActionsRemaining = initiator.CharacterData.CombatActions });
            s_currentCombatants.Add(opponent, new CombatantState { MovesRemaining = opponent.CharacterData.CombatMoves, ActionsRemaining = opponent.CharacterData.CombatActions });
            s_sides.Add([initiator]);
            s_sides.Add([opponent]);

            CombatStartEvent e = new()
            {
                Initiator = initiator,
                Participants = [.. s_currentCombatants.Keys]
            };
            initiator.NavObstacle.AffectNavigationMesh = false;
            opponent.NavObstacle.AffectNavigationMesh = true;
            s_pathingReady = false;
            NavRegion.BakeNavigationPolygon();

            s_combatStatusLabel.Text = initiator is Player ? "YOUR TURN" : "ENEMY TURN";
            HealthSystem.DeathEventHandlers += OnCharacterDeath;
            s_combatStatusLabel.Visible = true;
            CombatStartHandlers?.Invoke(e);
            s_sideMoving = 0;
            TurnHandlers?.Invoke(s_sides[0]);
        }
    }

    /// <summary>
    ///  Create a timer based on a number of turns relative to a particular character. Timers will automatically be removed when they expire or when the character they are relative to
    /// exits combat.
    /// </summary>
    /// <param name="duration">The duration in turns of the timer.</param>
    /// <param name="onTimeout">A callback that will be invoked when the timer finishes.</param>
    /// <param name="relativeTo">The character the timer is relative to. The timer ticks when this character's turn starts.</param>
    /// <returns>An opaque handle to the timer which can be passed to `RemoveTimer` for premature removal.</returns>
    public static CombatTimerHandle CreateTimer(int duration, Action onTimeout, Character relativeTo)
    {
        s_timerCount += 1;
        var timer = new CombatTimer()
        {
            TurnsRemaining = duration,
            Timeout = onTimeout,
            RelativeTo = relativeTo,
            Id = s_timerCount
        };
        s_combatTimers.Add(timer);
        return new CombatTimerHandle(s_timerCount);
    }

    /// <summary>
    /// Removes the timer referred to by `handle`. It is an error to pass a handle to this function that was not returned by `CreateTimer` or that has already been removed.
    /// </summary>
    /// <param name="handle">A handle to the timer to clear.</param>
    public static void RemoveTimer(CombatTimerHandle handle) => s_combatTimers = [.. s_combatTimers.Where(x => x.Id != handle._id)];

    public static bool TimerActive(CombatTimerHandle handle) => s_combatTimers.Any(x => x.Id == handle._id);

    private static void TriggerTimers(Side side)
    {
        foreach (var timer in from timer in s_combatTimers let timer1 = timer where side.Any(x => x == timer1.RelativeTo) select timer)
        {
            timer.TurnsRemaining -= 1;
            if (timer.TurnsRemaining <= 0)
            {
                timer.Timeout?.Invoke();
            }
        }

        s_combatTimers = [.. s_combatTimers.Where(x => x.TurnsRemaining > 0)];
    }

    public static void JoinCombat(Character toJoin)
    {
        if (!s_currentCombatants.ContainsKey(toJoin))
        {
            s_currentCombatants.Add(toJoin, new CombatantState { MovesRemaining = toJoin.CharacterData.CombatMoves, ActionsRemaining = toJoin.CharacterData.CombatActions });
            // New participant fights on the first side in which they are hostile to noone.
            var joiningSide = -1;
            for (int i = 0; i < s_sides.Count; ++i)
            {
                var side = s_sides[i];
                if (!side.Any(x => HostilitySystem.GetHostility(x.CharacterData, toJoin.CharacterData) || HostilitySystem.GetHostility(toJoin.CharacterData, x.CharacterData)))
                {
                    side.Add(toJoin);
                    joiningSide = i;
                    break;
                }
            }
            // If no suitable side is found, make a new one.
            if (joiningSide < 0)
            {
                s_sides.Add([toJoin]);
                joiningSide = s_sides.Count - 1;
            }
            // Only redo the nav if the new combatant is not currently moving.
            toJoin.NavObstacle.AffectNavigationMesh = joiningSide != s_sideMoving;
            if (joiningSide != s_sideMoving && NavReady())
            {
                NavRegion.BakeNavigationPolygon();
            }
            CharacterJoinedCombatHandlers?.Invoke(toJoin);
        }
    }

    public static float ComputeToHitChance(Character attacker, Character target)
    {
        var targetVector = target.GlobalPosition - attacker.GlobalPosition;
        return (20.0f - target.ComputeAc(targetVector.Normalized()) + attacker.ComputeToHitMod()) / 20.0f;
    }

    public static void AttemptMove(Character mover)
    {
        var currentSide = s_sides[s_sideMoving];
        var currentState = s_currentCombatants[mover];
        if (currentSide.Contains(mover) && currentState.MovesRemaining > 0)
        {
            int newMovesRem = currentState.MovesRemaining - 1;
            var state = new CombatantState
            {
                MovesRemaining = newMovesRem,
                ActionsRemaining = newMovesRem > 0 ? currentState.ActionsRemaining : 0,
            };

            s_currentCombatants[mover] = state;

            if (TurnShouldEnd())
            {
                EndTurn();
            }
        }
    }


    public static void AttemptAttack(Character attacker, Character target, DamageRoll damageRoll)
    {
        CombatantState attackerState = s_currentCombatants[attacker];
        var currentSide = s_sides[s_sideMoving];
        if (currentSide.Contains(attacker) && attackerState is { ActionsRemaining: > 0, MovesRemaining: > 0 })
        {
            CombatantState newAttackerState = new()
            {
                ActionsRemaining = 0,
                MovesRemaining = 0,
            };
            s_currentCombatants[attacker] = newAttackerState;
            Random rand = new();
            int toHitMod = attacker.ComputeToHitMod();
            int toHitRoll = System.Math.Max(0, System.Math.Min(19, rand.Next(20) + toHitMod));

            Vector2 targetVector = target.GlobalPosition - attacker.GlobalPosition;
            int hitThresh = target.ComputeAc(targetVector);

            bool hit = toHitRoll >= hitThresh;
            var roll = damageRoll.Roll();
            AttackHandlers?.Invoke(new AttackEvent
            {
                Attacker = attacker,
                Target = target,
                Hit = hit,
                TargetDamage = roll,
            });
            if (hit)
            {
                HealthSystem.PostDamageEvent(attacker, target, roll);
            }
        }

        if (TurnShouldEnd())
        {
            EndTurn();
        }
    }

    public static void PassTurn(Character character)
    {
        s_currentCombatants[character] = new CombatantState
        {
            ActionsRemaining = 0,
            MovesRemaining = 0,
        };

        if (TurnShouldEnd())
        {
            EndTurn();
        }
    }

    public static bool TurnShouldEnd() => !s_sides[s_sideMoving].Any(x => s_currentCombatants[x].MovesRemaining > 0 && s_currentCombatants[x].ActionsRemaining > 0);

    public static Side GetMovingSide() => s_sides[s_sideMoving];

    public static bool IsInCombat(Character c) => s_currentCombatants.ContainsKey(c);

    public static int GetActionsRemaining(Character c)
    {
        if (s_currentCombatants.TryGetValue(c, out var value))
        {
            return value.ActionsRemaining;
        }
        return 0;
    }

    public static int GetMovesRemaining(Character c)
    {
        if (s_currentCombatants.TryGetValue(c, out var value))
        {
            return value.MovesRemaining;
        }
        return 0;
    }

    public static List<Character> GetCharactersInRange(Vector2 position, Shape2D shape)
    {
        var spaceState = NavRegion.GetWorld2D().DirectSpaceState;
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
            if (collider is not null and Character character)
            {
                charactersInRange.Add(character);
            }
        }
        return charactersInRange;
    }

    public static List<Side> GetSides() => s_sides;
}

