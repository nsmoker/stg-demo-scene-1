using Godot;

namespace STGDemoScene1.Scripts.Resources;

[GlobalClass]
public partial class PatrolLeg : Resource
{
    [Export] public Vector2 Direction;
    [Export] public float Distance;
}
