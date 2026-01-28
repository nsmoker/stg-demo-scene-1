using Godot;
using System;

[GlobalClass]
public partial class QuestMarker : Marker2D
{
    [Export]
    public QuestObjectiveData QuestObjective;

    public override void _Ready() => QuestSystem.SetMarkerPosition(QuestObjective.ResourcePath, GlobalPosition);
}
