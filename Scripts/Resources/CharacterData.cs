using ArkhamHunters.Scripts;
using ArkhamHunters.Scripts.Abilities;
using Godot;

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

    private int ComputeAttributeMod(int value)
    {
        return (int)System.Math.Floor((value - 10.0) / 2.0);
    }

    [Export]
    public int MaxHitpoints = 100;
    [Export]
    public int CurrentHitpoints = 100;

    [Export]
	public Godot.Collections.Array<Ability> Abilities = [];

    [Export]
    public Faction InitialFaction;

    [Export] 
    public float Speed = 300.0f;

    [Export] 
    public Godot.Collections.Array<PatrolLeg> PatrolLegs = [];

    [Export]
    public EquipmentSet StartingEquipment = new();
}