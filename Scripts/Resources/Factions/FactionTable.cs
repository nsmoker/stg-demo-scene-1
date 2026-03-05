using Godot;
using STGDemoScene1.Scripts.Resources.Factions;

namespace STGDemoScene1.Scripts.Resources;

[Tool]
[GlobalClass]
public partial class FactionTable : Resource
{
    [Export]
    public Godot.Collections.Array<FactionRelation> FactionRelations = [];
}
