using Godot;

[Tool]
[GlobalClass]
public partial class ContainerData : Resource
{
    [Export]
    public Godot.Collections.Array<Item> StartingItems = [];
}