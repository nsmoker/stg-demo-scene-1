using ArkhamHunters.Scripts;
using Godot;

[GlobalClass]
[Tool]
public partial class WalkToRandomPointTask : CrowdAITask
{
    [Export]
    public float WalkSpeed;

    public override void StartTask(Character character, StagfootScreen area, double duration, CrowdAIDirector director)
    {
        var targetPoint = character.ToLocal(area.GetRandomTraversablePoint());
        character.WalkToPoint(targetPoint, onComplete: character.SetIdle, speed: WalkSpeed);
    }
}