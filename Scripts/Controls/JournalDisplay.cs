using Godot;
using System;
using System.Collections.Generic;

public partial class JournalDisplay : ScrollContainer
{
    private VBoxContainer _contentVBox;
    private readonly List<QuestDisplay> _questEntryDisplays = [];

    [Export]
    public PackedScene QuestEntryScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => _contentVBox = GetNode<VBoxContainer>("PanelContainer/VBoxContainer");

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
