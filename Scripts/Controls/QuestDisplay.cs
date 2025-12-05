using Godot;
using System;
using System.Collections.Generic;

public partial class QuestDisplay : FoldableContainer
{
	private List<QuestStageDisplay> _questStageDisplays = [];
	private VBoxContainer _vbox;
	private Label _DescriptionLabel;

	[Export]
	public PackedScene QuestStageDisplayScene;

	public override void _Ready()
	{
		_vbox = GetNode<VBoxContainer>("VBoxContainer");
		_DescriptionLabel = GetNode<Label>("VBoxContainer/DescriptionLabel");
	}

	public void SetQuest(Quest quest)
	{
		Clear();

		Title = quest.Title;
		_DescriptionLabel.Text = quest.Description;
		var questStages = quest.Stages;
		var currentStage = quest.GetCurrentStage();
		foreach (var stage in questStages)
        {
			if (currentStage.StageNumber >= stage.StageNumber)
			{

				var stageDisplay = QuestStageDisplayScene.Instantiate<QuestStageDisplay>();
				_vbox.AddChild(stageDisplay);
				stageDisplay.SetQuestStage(stage, currentStage.StageNumber > stage.StageNumber);
				_questStageDisplays.Add(stageDisplay);
			}
        }
		Visible = !quest.IsCompleted;
	}

	private void Clear()
	{
		foreach (var stageDisplay in _questStageDisplays)
		{
			stageDisplay.QueueFree();
		}
		_questStageDisplays.Clear();
	}
}
