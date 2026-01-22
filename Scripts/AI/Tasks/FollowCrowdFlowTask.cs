using System.ComponentModel;
using ArkhamHunters.Scripts;
using Godot;

[GlobalClass]
[Tool]
public partial class FollowCrowdFlowTask : CrowdAITask
{
    [Export]
    public int FlowFieldIndex;

    [Export]
    public float WalkSpeed;

    public override void StartTask(Character character, StagfootScreen area, double duration, CrowdAIDirector director)
    {
        var randomField = area.FlowFields[FlowFieldIndex];
        var flow = randomField.SampleFlowField(area.ToLocal(character.GlobalPosition));
        character.WalkToPoint(character.GlobalPosition + flow.Normalized() * WalkSpeed, () =>
        {
            var state = director.GetState(character.GetInstanceId());
            if (state.RemainingDuration >= 0.0f)
            {
                StartTask(character, area, state.RemainingDuration, director);
            }
            if (!area.CheckBounds(character.Position))
            {
                character.GlobalPosition = area.ToGlobal(area.GetEdge() + new Vector2(0.0f, -100.0f + director.DirectorRandom.NextSingle() * 200.0f));
            }
        }, WalkSpeed);
    }
}