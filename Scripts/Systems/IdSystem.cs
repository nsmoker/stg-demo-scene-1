using Godot.Collections;

namespace STGDemoScene1.Scripts.Systems;

public static class IdSystem
{
    private static readonly Dictionary<string, ulong> s_editorToRuntimeId = [];

    public static void Register(string editorId, ulong runtimeId) => s_editorToRuntimeId.Add(editorId, runtimeId);

    public static bool TryGetRuntimeId(string editorId, out ulong runtimeId) => s_editorToRuntimeId.TryGetValue(editorId, out runtimeId);
}
