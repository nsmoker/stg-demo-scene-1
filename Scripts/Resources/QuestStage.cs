using Godot;

[Tool]
[GlobalClass]
public partial class QuestStage : Resource
{
    [Export]
    public int StageNumber;
    [Export]
    public string Title;
    [Export(PropertyHint.MultilineText)]
    public string Description;
    [Export]
    public bool CompleteQuest = false;
    [Export]
    public QuestObjectiveData Objective;
}
