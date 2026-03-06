using Godot;
using STGDemoScene1.Scripts.Characters;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.AI;

[GlobalClass]
[Tool]
public partial class IsCover : MovePriority
{
    public override float ScorePosition(Vector2 position, Character me, PhysicsDirectSpaceState2D physicsState)
    {
        var coverCheck = CoverSystem.CheckCover(position, physicsState);
        if (coverCheck.CoverLevelEast > 0 || coverCheck.CoverLevelWest > 0 || coverCheck.CoverLevelNorth > 0 ||
            coverCheck.CoverLevelSouth > 0)
        {
            return 5;
        }

        return 1;
    }
}
