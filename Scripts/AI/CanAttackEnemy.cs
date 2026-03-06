using Godot;
using STGDemoScene1.Scripts.Characters;

namespace STGDemoScene1.Scripts.AI;

[GlobalClass]
[Tool]
public partial class CanAttackEnemy : MovePriority
{
    public override float ScorePosition(Vector2 position, Character me, PhysicsDirectSpaceState2D physicsState)
    {
        var closestEnemy = me.GetClosestEnemy();
        if (closestEnemy != null && position.DistanceTo(closestEnemy.GlobalPosition) <= me.CharacterData.AttackRange)
        {
            return 5.0f;
        }

        return 1.0f;
    }
}
