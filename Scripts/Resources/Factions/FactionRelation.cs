using Godot;

namespace STGDemoScene1.Scripts.Resources.Factions;

[Tool]
[GlobalClass]
public partial class FactionRelation : Resource
{
    [Export]
    public Faction FactionA;

    [Export]
    public Faction FactionB;

    [Export]
    public bool IsHostile;
}
