using Godot;
using System;

[Tool]
[GlobalClass]
public partial class CheckHasQuest : DialogueCondition
{
    [Export]
    public Quest QuestToCheck;
    public override bool Evaluate() => QuestSystem.TryGetQuest(QuestToCheck.ResourcePath, out var q) && !q.IsCompleted;
}
