using Godot;
using STGDemoScene1.Scripts.Resources;
using System.Collections.Generic;

namespace STGDemoScene1.Scripts.Systems;

public static class QuestSystem
{
    private static readonly Dictionary<string, Quest> s_quests = [];
    private static readonly Dictionary<string, Vector2> s_markerPositions = [];

    public delegate void QuestUpdatedHandler(Quest quest);
    public static event QuestUpdatedHandler OnQuestUpdated;

    public static void AddQuest(Quest quest)
    {
        s_quests[quest.ResourcePath] = quest;
        OnQuestUpdated?.Invoke(quest);
    }

    public static void RemoveQuest(Quest quest) => s_quests.Remove(quest.ResourcePath);

    public static bool TryGetQuest(string questId, out Quest quest) => s_quests.TryGetValue(questId, out quest);

    public static void SetQuestStage(string questId, int stageIndex)
    {
        if (s_quests.TryGetValue(questId, out var quest))
        {
            quest.SetStage(stageIndex);
            OnQuestUpdated?.Invoke(quest);
        }
    }

    public static List<Quest> GetAllQuests() => [.. s_quests.Values];

    public static void SetMarkerPosition(string markerId, Vector2 pos) => s_markerPositions[markerId] = pos;

    public static bool TryGetMarkerPosition(string markerId, out Vector2 pos) => s_markerPositions.TryGetValue(markerId, out pos);
}
