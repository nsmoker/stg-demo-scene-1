using Godot;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.Resources;

[GlobalClass]
public partial class FactionTable : Resource
{
    [Export]
    public Godot.Collections.Dictionary<Faction, Godot.Collections.Dictionary<Faction, bool>> Factions = [];

    public FactionTable() { }
}
