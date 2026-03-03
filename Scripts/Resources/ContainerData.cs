using Godot;
using STGDemoScene1.Scripts.Items;

namespace STGDemoScene1.Scripts.Resources;

[Tool]
[GlobalClass]
public partial class ContainerData : Resource
{
    [Export]
    public Godot.Collections.Array<Item> StartingItems = [];
}
