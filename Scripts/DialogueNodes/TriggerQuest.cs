using Godot;
using System;

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
