using Godot;
using System;

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
    public float Speed;
}
