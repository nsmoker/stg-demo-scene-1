using ArkhamHunters.Scripts;
using Godot;

[GlobalClass]
[Tool]
public partial class IdleTask : CrowdAITask
{
    public override void StartTask(Character character, StagfootScreen area, double duration, CrowdAIDirector director)
    {
        character.SetIdle();
    }
}