using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.AI;

[GlobalClass]
[Tool]
public partial class IsCover : MovePriority
{
    /// <summary>
    /// Scores a position as higher depending on how many enemies we end up in cover from.
    /// </summary>
    public override float ScorePosition(Vector2 position, Character me, List<Character> enemies, PhysicsDirectSpaceState2D physicsState)
    {
        var coverCheck = CoverSystem.CheckCover(position, physicsState);
        float score = 1;
        foreach (var enemy in enemies)
        {
            var dirToEnemy = enemy.GlobalPosition - position;
            var quant = Math.GetCardinalQuantization(dirToEnemy);
            if (quant.IsEqualApprox(Vector2.Up))
            {
                score += coverCheck.CoverLevelNorth * 5.0f;
            }
            if (quant.IsEqualApprox(Vector2.Left))
            {
                score += coverCheck.CoverLevelWest * 5.0f;
            }
            if (quant.IsEqualApprox(Vector2.Right))
            {
                score += coverCheck.CoverLevelEast * 5.0f;
            }
            if (quant.IsEqualApprox(Vector2.Down))
            {
                score += coverCheck.CoverLevelSouth * 5.0f;
            }
        }

        return score;
    }
}
