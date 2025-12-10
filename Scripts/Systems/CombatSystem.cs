using ArkhamHunters.Scripts;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public struct AbilityUseEvent
{
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

    public static void AttemptAttack(CharacterData attacker, CharacterData attacked)
    {

    }

    public static HashSet<string> GetCombatants()
    {
        return [ .._currentCombatants];
    }

    public static bool IsInCombat(CharacterData c)
    {
        return _currentCombatants.Contains(c.ResourcePath);
    }          
}
