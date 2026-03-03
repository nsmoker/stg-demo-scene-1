using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class AdvanceQuest : DialogueAction
{
    [Export]
    public Quest QuestToAdvance;

    [Export]
    public int StageToAdvanceTo;
    public override void Execute(Action onComplete)
    {
        QuestSystem.SetQuestStage(QuestToAdvance.ResourcePath, StageToAdvanceTo);
        onComplete?.Invoke();
    }
}
