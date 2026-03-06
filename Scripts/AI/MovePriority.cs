using Godot;
using STGDemoScene1.Scripts.Characters;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.AI;

[GlobalClass]
[Tool]
public partial class MovePriority : Resource
{
    public virtual float ScorePosition(Vector2 position, Character me, List<Character> enemies, PhysicsDirectSpaceState2D physicsState) => 0;
}
