using Godot;

namespace STGDemoScene1.Scripts.AI;

public enum CrowdAiTaskType
{
    Idle,
    WalkToRandomPoint,
    FindOpenSeat,
    TalkToPartner,
    FollowCrowdFlow,
}

[GlobalClass]
[Tool]
public partial class CrowdAiTask : Resource
{
    [Export]
    public CrowdAiTaskType Type;

    [Export]
    public double Duration;

    [Export]
    public float Probability;

    [Export]
    public int Tag;
}
