using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Abilities;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public struct AbilityUseEvent
{
    public Ability ability;
    public Character attacker;
    public Character target;
    public bool hit;
    // Only use if hit is true.
    public int targetDamage;
    public int areaDamage;
}

public struct CombatStartEvent
{
    public List<string> participants;
    public CharacterData initiator;
}

public static class CombatSystem
{
    private static readonly List<string> _currentCombatants = [];
    private static NavigationRegion2D navRegion;

    public delegate void AbilityEventHandler(AbilityUseEvent e);
    public delegate void CombatStartHandler(CombatStartEvent e);
    public delegate void CharacterJoinedCombatHandler(CharacterData c);
    public delegate void TurnHandler(CharacterData turnTaker);

    private static AbilityEventHandler abilityEventHandler1;
    private static CombatStartHandler combatStartHandler1;
    private static CharacterJoinedCombatHandler characterJoinedCombatHandler1;

    private static Character TakingTurn;
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

    public static AbilityEventHandler AbilityEventHandlers { get => abilityEventHandler1; set => abilityEventHandler1 = value; }
    public static CombatStartHandler CombatStartHandlers { get => combatStartHandler1; set => combatStartHandler1 = value; }
    public static CharacterJoinedCombatHandler CharacterJoinedCombatHandlers { get => characterJoinedCombatHandler1; set => characterJoinedCombatHandler1 = value; }
    public static TurnHandler TurnHandlers { get => turnHandler; set => turnHandler = value; }


    private static void OnNavRebakeFinished(Rid rid)
    {
        if (rid == navRegion.GetNavigationMap())
        {
            _pathingReady = true;
        }
    }

    public static bool NavReady()
    {
        return _pathingReady && navRegion.NavigationPolygon != null;
    }

    private static Character GetNextCharacter()
    {
        int i = _currentCombatants.FindIndex(TakingTurn.CharacterData.ResourcePath.Equals);
        i = (i + 1) % _currentCombatants.Count;
        return CharacterSystem.GetInstance(_currentCombatants[i]);
    }

    public static void EndTurn(CharacterData turnTaker)
    {
        if (turnTaker.ResourcePath.Equals(TakingTurn.CharacterData.ResourcePath))
        {
            TakingTurn.NavObstacle.AffectNavigationMesh = true;
            BeginTurn(GetNextCharacter());
        }
    }

    private static void BeginTurn(Character turnTaker)
    {
        TakingTurn = turnTaker;
        TakingTurn.NavObstacle.AffectNavigationMesh = false;
        _pathingReady = false;
        NavRegion.BakeNavigationPolygon();
        TurnHandlers?.Invoke(TakingTurn.CharacterData);
    }

    public static void BeginCombat(CharacterData initiator, List<CharacterData> opponents)
    {
        if (_currentCombatants.Count == 0)
        {
            _currentCombatants.Add(initiator.ResourcePath);
            _currentCombatants.AddRange(opponents.Select(opp => opp.ResourcePath));

            CombatStartEvent e = new()
            {
                initiator = initiator,
                participants = _currentCombatants
            };

            TakingTurn = CharacterSystem.GetInstance(_currentCombatants.First());
            CombatStartHandlers?.Invoke(e);
            BeginTurn(TakingTurn);
        }
    }

    public static void JoinCombat(CharacterData toJoin)
    {
        if (!_currentCombatants.Contains(toJoin.ResourcePath))
        {
            _currentCombatants.Add(toJoin.ResourcePath);
            var instance = CharacterSystem.GetInstance(toJoin.ResourcePath);
            instance.NavObstacle.AffectNavigationMesh = true;
            NavRegion.BakeNavigationPolygon();
            CharacterJoinedCombatHandlers?.Invoke(toJoin);
        }
    }

    public static HashSet<string> GetCombatants()
    {
        return [ .._currentCombatants];
    }

    public static bool IsInCombat(CharacterData c)
    {
        return _currentCombatants.Contains(c.ResourcePath);
    }

    // Attempt to use an ability. Only call this function from PhysicsProcess.
    public static void UseAbility(Ability ability, Character attacker, Character target)
    {
        var rand = new Random();
        var distance = (attacker.GetClosestOnCollSurface(target.Position) - target.GetClosestOnCollSurface(attacker.Position)).Length();
        AbilityUseEvent ret = new();
        ret.ability = ability;
        ret.target = target;
        ret.attacker = attacker;
        // First, check if the ability hit.
        // If the target's out of range, it definitely didn't hit.
        if (distance > ability.Range)
        {
            ret.hit = false;
        }
        // If it's in range, we might first need to roll to hit.
        else if (ability.RollsToHit)
        {
            var roll = rand.Next(20) + 1 + ability.ToHitMod + attacker.ComputeToHitMod();
            ret.hit = roll > target.ComputeAc();
        }
        // If there's no roll necessary and the ability is a range, it's definitely a hit.
        else
        {
            ret.hit = true;
        }

        // If the ability hit, we need to compute damage and healing.
        if (ret.hit)
        {
            foreach (var roll in ability.TargetDamage.Rolls)
            {
                var rollResult = rand.Next(roll) + 1 + ability.TargetDamage.Mod;
                ret.targetDamage += rollResult;
            }

            foreach (var roll in ability.AreaDamage.Rolls)
            {
                var rollResult = rand.Next(roll) + 1 + ability.AreaDamage.Mod;
                ret.areaDamage += rollResult;
            }

            foreach (var roll in ability.TargetHealing.Rolls)
            {
                var rollResult = rand.Next(roll) + 1 + ability.TargetHealing.Mod;
                ret.targetDamage -= rollResult;
            }

            foreach (var roll in ability.AreaHealing.Rolls)
            { 
                var rollResult = rand.Next(roll) + 1 + ability.AreaHealing.Mod;
                ret.areaDamage -= rollResult;
            }

            // Send our event.
            AbilityEventHandlers?.Invoke(ret);

            // Post target damage event.
            HealthSystem.PostDamageEvent(attacker, target, ret.targetDamage, ability);

            // Check the area for collisions.
            var areaShape = new CircleShape2D();
            areaShape.Radius = ability.AreaRadius;
            var coll = new CollisionShape2D();
            coll.Shape = areaShape;
            coll.Position = target.Position;
            PhysicsDirectSpaceState2D spaceState = target.GetWorld2D().DirectSpaceState;
            var queryParams = new PhysicsShapeQueryParameters2D
            {
                CollideWithAreas = false,
                CollideWithBodies = true,
                Exclude = [],
                Shape = areaShape
            };
            var intersections = spaceState.IntersectShape(queryParams);

            foreach (var intersection in intersections)
            {
                var collided = (CollisionObject2D) intersection["collider"].AsGodotObject();
                if (collided is Character character)
                {
                    HealthSystem.PostDamageEvent(attacker, character, ret.areaDamage, ability);
                }
            }
        }
    }
}
