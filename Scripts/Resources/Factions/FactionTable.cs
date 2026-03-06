using Godot;

namespace STGDemoScene1.Scripts.Resources.Factions;

[Tool]
[GlobalClass]
public partial class FactionTable : Resource
{
    [Export]
    public Godot.Collections.Array<FactionRelation> FactionRelations = [];
}
