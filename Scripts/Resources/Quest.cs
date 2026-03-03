using Godot;

namespace STGDemoScene1.Scripts.Resources;

[Tool]
[GlobalClass]
public partial class Quest : Resource
{
    [Export]
    public string Title;

    [Export(PropertyHint.MultilineText)]
    public string Description;

    [Export]
    public Godot.Collections.Array<QuestStage> Stages = [];

    [Export]
    public bool IsCompleted = false;

    [Export]
    public int CurrentStage = 0;

    public QuestStage GetCurrentStage()
    {
        if (CurrentStage < 0 || CurrentStage >= Stages.Count)
        {
            return null;
        }
        return Stages[CurrentStage];
    }

    public void SetStage(int stageNumber)
    {
        for (int i = 0; i < Stages.Count; i++)
        {
            if (Stages[i].StageNumber == stageNumber)
            {
                CurrentStage = i;
            }
        }

        if (GetCurrentStage() != null && GetCurrentStage().CompleteQuest)
        {
            IsCompleted = true;
        }
    }
}
