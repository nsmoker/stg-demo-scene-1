using Godot;

namespace STGDemoScene1.Scripts.AI;

public enum CrowdAITaskType
{
    Idle,
    WalkToRandomPoint,
    FindOpenSeat,
    TalkToPartner,
    FollowCrowdFlow,
}

[GlobalClass]
[Tool]
public partial class CrowdAITask : Resource
{
    [Export]
    public CrowdAITaskType Type;

    [Export]
    public double Duration;

    [Export]
    public float Probability;

    [Export]
    public int Tag;
}
