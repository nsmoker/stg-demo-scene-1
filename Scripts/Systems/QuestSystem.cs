using Godot;
using System.Collections.Generic;

public static class QuestSystem
{
    private static readonly Dictionary<string, Quest> _quests = [];
    private static readonly Dictionary<string, Vector2> _markerPositions = [];

    public delegate void QuestUpdatedHandler(Quest quest);
    public static QuestUpdatedHandler OnQuestUpdated;

    public static void AddQuest(Quest quest)
    {
        _quests[quest.ResourcePath] = quest;
        OnQuestUpdated?.Invoke(quest);
    }

    public static void RemoveQuest(Quest quest) => _quests.Remove(quest.ResourcePath);

    public static bool TryGetQuest(string questId, out Quest quest) => _quests.TryGetValue(questId, out quest);

    public static void SetQuestStage(string questId, int stageIndex)
    {
        if (_quests.TryGetValue(questId, out var quest))
        {
            quest.SetStage(stageIndex);
            OnQuestUpdated?.Invoke(quest);
        }
    }

    public static List<Quest> GetAllQuests() => [.. _quests.Values];

    public static void SetMarkerPosition(string markerId, Vector2 pos) => _markerPositions[markerId] = pos;

    public static bool TryGetMarkerPosition(string markerId, out Vector2 pos) => _markerPositions.TryGetValue(markerId, out pos);
}
