using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class CheckQuestStage : DialogueCondition
{
    [Export]
    public Quest Quest;

    [Export]
    public int Stage;

    [Export]
    private ComparisonOperators _operator;

    public override bool Evaluate()
    {
        if (QuestSystem.TryGetQuest(Quest.ResourcePath, out var instance))
        {
            var activeStage = instance.CurrentStage;
            return _operator switch
            {
                ComparisonOperators.Less => activeStage < Stage,
                ComparisonOperators.Greater => activeStage > Stage,
                ComparisonOperators.GreaterEqual => activeStage >= Stage,
                ComparisonOperators.LessEqual => activeStage <= Stage,
                ComparisonOperators.Equal => activeStage == Stage,
                _ => false,
            };
        }
        else
        {
            return false;
        }
    }
}
