using Godot;
using System;

public enum CrowdAITaskType
{
    Idle,
    WalkToRandomPoint,
    TalkToPartner
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
