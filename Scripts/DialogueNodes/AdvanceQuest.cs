using Godot;

[Tool]
[GlobalClass]
public partial class AdvanceQuest : DialogueAction
{
    [Export]
    public Quest QuestToAdvance;

    [Export]
    public int StageToAdvanceTo;
    public override void Execute()
    {
        QuestSystem.SetQuestStage(QuestToAdvance.ResourcePath, StageToAdvanceTo);
    }
}