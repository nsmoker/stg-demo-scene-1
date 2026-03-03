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
    public int _stage;

    [Export]
    private ComparisonOperators _operator;

    public override bool Evaluate()
    {
        if (QuestSystem.TryGetQuest(Quest.ResourcePath, out var instance))
        {
            var activeStage = instance.CurrentStage;
            return _operator switch
            {
                ComparisonOperators.Less => activeStage < _stage,
                ComparisonOperators.Greater => activeStage > _stage,
                ComparisonOperators.GreaterEqual => activeStage >= _stage,
                ComparisonOperators.LessEqual => activeStage <= _stage,
                ComparisonOperators.Equal => activeStage == _stage,
                _ => false,
            };
        }
        else
        {
            return false;
        }
    }
}
