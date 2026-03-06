using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Triggers;
using System.Collections.Generic;
using System.Linq;

namespace STGDemoScene1.Scripts.AI;

[GlobalClass]
[Tool]
public partial class DontStandInAoe : MovePriority
{
    public override float ScorePosition(Vector2 position, Character me, List<Character> enemies,
        PhysicsDirectSpaceState2D physicsState)
    {
        var coll = me.Collider;
        var collisionParameters = new PhysicsShapeQueryParameters2D()
        {
            Transform = new Transform2D(0.0f, position),
            Shape = coll.Shape,
            CollisionMask = 0x1u
        };
        var overlaps = physicsState.IntersectShape(collisionParameters);
        return overlaps.Any(x => x["collider"].AsGodotObject() is AreaEffect) ? 0.0f : 5.0f;
    }
}
