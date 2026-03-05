using Godot;
using STGDemoScene1.Scripts.Resources.Factions;

namespace STGDemoScene1.Scripts.Resources;

[Tool]
[GlobalClass]
public partial class CharacterData : Resource
{
    [Export]
    public string CharacterName = "New Character";

    [Export]
    public AttributeSet BaseAttributes = new();
    [Export]
    public SkillSet BaseSkills = new();

    private static int ComputeAttributeMod(int value) => (int) System.Math.Floor((value - 10.0) / 2.0);

    [Export]
    public int MaxHitpoints = 100;
    [Export]
    public int CurrentHitpoints = 100;

    [Export]
    public Faction InitialFaction;

    [Export]
    public float Speed = 300.0f;

    [Export]
    public Godot.Collections.Array<PatrolLeg> PatrolLegs = [];

    [Export]
    public EquipmentSet StartingEquipment = new();

    [Export]
    public float MovementRange = 20.0f;

    [Export]
    public int CombatMoves = 2;

    [Export]
    public int CombatActions = 1;

    [Export]
    public int AttackRange = 20;

    // Quests the character has or has had.
    [Export]
    public Godot.Collections.Array<Quest> Journal = [];
}
