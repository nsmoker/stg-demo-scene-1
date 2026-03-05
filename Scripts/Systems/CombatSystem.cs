using Godot;
using STGDemoScene1.Scripts.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using Character = STGDemoScene1.Scripts.Characters.Character;
using Player = STGDemoScene1.Scripts.Characters.Player;

namespace STGDemoScene1.Scripts.Systems;

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

internal struct CombatantState
{
    public int MovesRemaining;
    public int ActionsRemaining;
}

public struct CombatTimerHandle
{
    internal ulong _id;
    internal CombatTimerHandle(ulong id) => _id = id;

};

public static class CombatSystem
{
    private static readonly Dictionary<string, CombatantState> s_currentCombatants = [];

    private static ulong s_timerCount = 0;

    public delegate void AttackEventHandler(AttackEvent e);
    public delegate void CombatStartHandler(CombatStartEvent e);
    public delegate void CharacterJoinedCombatHandler(CharacterData c);
    public delegate void TurnHandler(List<string> side);

    public static NavigationRegion2D NavRegion { get; set; }

    private static bool s_pathingReady = true;

    public static AttackEventHandler AttackHandlers { get; set; }
    public static CombatStartHandler CombatStartHandlers { get; set; }
    public static CharacterJoinedCombatHandler CharacterJoinedCombatHandlers { get; set; }
    public static TurnHandler TurnHandlers { get; set; }
    public static Action CombatEnded { get; set; }

    private static List<List<string>> s_sides = [];

    private static List<CombatTimer> s_combatTimers = [];

    private static int s_sideMoving = 0;

    private static Label s_combatStatusLabel;

    private static void OnNavRebakeFinished(Rid rid)
    {
        if (rid == NavRegion.GetNavigationMap())
        {
            s_pathingReady = true;
        }
    }

    private static void OnCharacterDeath(DeathEvent e)
    {
        var deceasedId = e.deceased.CharacterData.ResourcePath;
        _ = s_currentCombatants.Remove(deceasedId);
        _ = s_sides.Find(x => x.Contains(deceasedId)).Remove(deceasedId);
    }

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
            currentSide.ForEach(x => CharacterSystem.GetInstance(x).NavObstacle.AffectNavigationMesh = true);
            s_sideMoving = (s_sideMoving + 1) % s_sides.Count;
            currentSide = s_sides[s_sideMoving];
            if (currentSide.Any(x => CharacterSystem.GetInstance(x) is Player))
            {
                s_combatStatusLabel.Text = "YOUR TURN";
            }
            else
            {
                s_combatStatusLabel.Text = "ENEMY TURN";
            }
            s_pathingReady = false;
            currentSide.ForEach(x =>
            {
                var instance = CharacterSystem.GetInstance(x);
                instance.NavObstacle.AffectNavigationMesh = false;
                var state = new CombatantState
                {
                    ActionsRemaining = instance.CharacterData.CombatActions,
                    MovesRemaining = instance.CharacterData.CombatMoves
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

    public static void Initialize() => NavigationServer2D.MapChanged += OnNavRebakeFinished;

    public static void BeginCombat(CharacterData initiator, CharacterData opponent)
    {
        var scene = (Engine.GetMainLoop() as SceneTree)
            .CurrentScene as MasterScene;
        s_combatStatusLabel = scene.GetCombatStatusLabel();
        if (s_currentCombatants.Count == 0)
        {
            s_currentCombatants.Add(initiator.ResourcePath, new CombatantState { MovesRemaining = initiator.CombatMoves, ActionsRemaining = initiator.CombatActions });
            s_currentCombatants.Add(opponent.ResourcePath, new CombatantState { MovesRemaining = opponent.CombatMoves, ActionsRemaining = opponent.CombatActions });
            s_sides.Add([initiator.ResourcePath]);
            s_sides.Add([opponent.ResourcePath]);

            CombatStartEvent e = new()
            {
                initiator = initiator,
                participants = [.. s_currentCombatants.Keys]
            };
            var initiatiorInstance = CharacterSystem.GetInstance(initiator.ResourcePath);
            initiatiorInstance.NavObstacle.AffectNavigationMesh = false;
            var opponentInstance = CharacterSystem.GetInstance(opponent.ResourcePath);
            opponentInstance.NavObstacle.AffectNavigationMesh = true;
            s_pathingReady = false;
            NavRegion.BakeNavigationPolygon();

            if (initiatiorInstance is Player)
            {
                s_combatStatusLabel.Text = "YOUR TURN";
            }
            else
            {
                s_combatStatusLabel.Text = "ENEMY TURN";
            }
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
    /// <param name="RelativeTo">The character the timer is relative to. The timer ticks when this character's turn starts.</param>
    /// <returns>An opaque handle to the timer which can be passed to `RemoveTimer` for premature removal.</returns>
    public static CombatTimerHandle CreateTimer(int duration, Action onTimeout, CharacterData RelativeTo)
    {
        s_timerCount += 1;
        var timer = new CombatTimer()
        {
            TurnsRemaining = duration,
            Timeout = onTimeout,
            RelativeToCharacter = RelativeTo,
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

    private static void TriggerTimers(List<string> side)
    {
        for (int i = 0; i < s_combatTimers.Count; ++i)
        {
            CombatTimer timer = s_combatTimers[i];
            if (side.Any(x => x == timer.RelativeToCharacter.ResourcePath))
            {
                timer.TurnsRemaining -= 1;
                if (timer.TurnsRemaining <= 0)
                {
                    timer.Timeout?.Invoke();
                }
            }
        }

        s_combatTimers = [.. s_combatTimers.Where(x => x.TurnsRemaining > 0)];
    }

    public static void JoinCombat(CharacterData toJoin)
    {
        if (!s_currentCombatants.ContainsKey(toJoin.ResourcePath))
        {
            s_currentCombatants.Add(toJoin.ResourcePath, new CombatantState { MovesRemaining = toJoin.CombatMoves, ActionsRemaining = toJoin.CombatActions });
            // New participant fights on the first side in which they are hostile to noone.
            var joiningSide = -1;
            for (int i = 0; i < s_sides.Count; ++i)
            {
                var side = s_sides[i];
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
                s_sides.Add([toJoin.ResourcePath]);
                joiningSide = s_sides.Count - 1;
            }
            var instance = CharacterSystem.GetInstance(toJoin.ResourcePath);
            // Only redo the nav if the new combatant is not currently moving.
            instance.NavObstacle.AffectNavigationMesh = joiningSide != s_sideMoving;
            if (joiningSide != s_sideMoving && NavReady())
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
        var targetVector = attackedInstance.GlobalPosition - attackerInstance.GlobalPosition;
        return (20.0f - attackedInstance.ComputeAc(targetVector.Normalized()) + attackerInstance.ComputeToHitMod()) / 20.0f;
    }

    public static void AttemptMove(CharacterData mover)
    {
        var currentSide = s_sides[s_sideMoving];
        var currentState = s_currentCombatants[mover.ResourcePath];
        if (currentSide.Contains(mover.ResourcePath) && currentState.MovesRemaining > 0)
        {
            int newMovesRem = currentState.MovesRemaining - 1;
            var state = new CombatantState
            {
                MovesRemaining = newMovesRem,
                ActionsRemaining = newMovesRem > 0 ? currentState.ActionsRemaining : 0,
            };

            s_currentCombatants[mover.ResourcePath] = state;

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
        CombatantState attackerState = s_currentCombatants[attacker.ResourcePath];
        var currentSide = s_sides[s_sideMoving];
        if (currentSide.Contains(attacker.ResourcePath) && attackerState.ActionsRemaining > 0 && attackerState.MovesRemaining > 0)
        {
            CombatantState newAttackerState = new()
            {
                ActionsRemaining = 0,
                MovesRemaining = 0,
            };
            s_currentCombatants[attacker.ResourcePath] = newAttackerState;
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
        s_currentCombatants[character.ResourcePath] = new CombatantState
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

    public static List<string> GetMovingSide() => s_sides[s_sideMoving];

    public static bool IsInCombat(CharacterData c) => s_currentCombatants.ContainsKey(c.ResourcePath);

    public static int GetActionsRemaining(CharacterData c)
    {
        if (s_currentCombatants.TryGetValue(c.ResourcePath, out var value))
        {
            return value.ActionsRemaining;
        }
        return 0;
    }

    public static int GetMovesRemaining(CharacterData c)
    {
        if (s_currentCombatants.TryGetValue(c.ResourcePath, out var value))
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

    public static List<List<string>> GetSides() => s_sides;
}

