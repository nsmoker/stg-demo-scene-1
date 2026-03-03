using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class TriggerQuest : DialogueAction
{
    [Export]
    public Quest QuestToTrigger;

    public override void Execute(Action onComplete)
    {
        QuestSystem.AddQuest(QuestToTrigger);
        onComplete?.Invoke();
    }
}
