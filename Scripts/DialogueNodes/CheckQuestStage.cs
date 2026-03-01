using Godot;

[Tool]
[GlobalClass]
public partial class CheckQuestStage : DialogueCondition
{
    [Export]
    public Quest Quest;

    [Export]
    public int stage;

    [Export]
    private ArkhamHunters.Scripts.Util.ComparisonOperators Operator;

    public override bool Evaluate()
    {
        if (QuestSystem.TryGetQuest(Quest.ResourcePath, out var instance))
        {
            var activeStage = instance.CurrentStage;
            return Operator switch
            {
                ArkhamHunters.Scripts.Util.ComparisonOperators.Less => activeStage < stage,
                ArkhamHunters.Scripts.Util.ComparisonOperators.Greater => activeStage > stage,
                ArkhamHunters.Scripts.Util.ComparisonOperators.GreaterEqual => activeStage >= stage,
                ArkhamHunters.Scripts.Util.ComparisonOperators.LessEqual => activeStage <= stage,
                ArkhamHunters.Scripts.Util.ComparisonOperators.Equal => activeStage == stage,
                _ => false,
            };
        }
        else
        {
            return false;
        }
    }
}
