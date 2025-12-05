using Godot;
using System;

[Tool]
[GlobalClass]
public partial class TriggerQuest : DialogueAction
{
    [Export]
    public Quest QuestToTrigger;

    public override void Execute()
    {
        QuestSystem.AddQuest(QuestToTrigger);
    }
}
