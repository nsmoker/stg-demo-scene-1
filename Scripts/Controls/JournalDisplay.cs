using Godot;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Controls;

public partial class JournalDisplay : ScrollContainer
{
    private VBoxContainer _contentVBox;
    private readonly List<QuestDisplay> _questEntryDisplays = [];

    [Export]
    public PackedScene QuestEntryScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _contentVBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer");
        VisibilityChanged += () => SetQuestEntries(QuestSystem.GetAllQuests());
    }

    public void SetQuestEntries(List<Quest> entries)
    {
        Clear();

        foreach (var entry in entries)
        {
            var questEntry = QuestEntryScene.Instantiate<QuestDisplay>();
            _contentVBox.AddChild(questEntry);
            questEntry.SetQuest(entry);
            _questEntryDisplays.Add(questEntry);
        }
    }

    private void Clear()
    {
        foreach (var entry in _questEntryDisplays)
        {
            entry.QueueFree();
        }
        _questEntryDisplays.Clear();
    }
}
