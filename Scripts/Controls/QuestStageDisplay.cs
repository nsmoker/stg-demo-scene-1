using Godot;
using System;

public partial class QuestStageDisplay : FoldableContainer
{
	private Label _descriptionLabel;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		_descriptionLabel = GetNode<Label>("Description");
    }

	public void SetQuestStage(QuestStage questStage, bool completed)
    {
		Title = questStage.Title + (completed ? " (Completed)" : "");
		_descriptionLabel.Text = questStage.Description;
		Folded = completed;
    }
}
