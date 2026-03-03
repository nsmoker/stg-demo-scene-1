using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class CheckHasQuest : DialogueCondition
{
    [Export]
    public Quest QuestToCheck;
    public override bool Evaluate() => QuestSystem.TryGetQuest(QuestToCheck.ResourcePath, out var q) && !q.IsCompleted;
}
