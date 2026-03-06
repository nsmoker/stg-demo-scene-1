using Godot;

namespace STGDemoScene1.Scripts.Resources;

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
    public bool CompleteQuest;
    [Export]
    public QuestObjectiveData Objective;
}
