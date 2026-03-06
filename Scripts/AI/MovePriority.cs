using Godot;
using STGDemoScene1.Scripts.Characters;

namespace STGDemoScene1.Scripts.AI;

[GlobalClass]
[Tool]
public partial class MovePriority : Resource
{
    public virtual float ScorePosition(Vector2 position, Character me, PhysicsDirectSpaceState2D physicsState) => 0;
}
