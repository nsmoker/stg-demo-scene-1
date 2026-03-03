using Godot;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;

namespace STGDemoScene1.Scripts;

[GlobalClass]
public partial class QuestMarker : Marker2D
{
    [Export]
    public QuestObjectiveData QuestObjective;

    public override void _Ready() => QuestSystem.SetMarkerPosition(QuestObjective.ResourcePath, GlobalPosition);
}
