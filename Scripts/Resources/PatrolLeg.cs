using Godot;

namespace ArkhamHunters.Scripts;

[GlobalClass]
public partial class PatrolLeg : Resource
{
    [Export] public Vector2 Direction;
    [Export] public float Distance;
}
