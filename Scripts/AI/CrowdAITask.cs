using ArkhamHunters.Scripts;
using Godot;

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
    public float Probability;

    [Export]
    public float Duration;

    public virtual void StartTask(Character character, StagfootScreen area, double duration, CrowdAIDirector director) { }
}
