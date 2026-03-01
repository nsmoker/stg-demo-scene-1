using Godot;
using Godot.Collections;
using System;

public static class IdSystem
{
    private static readonly Dictionary<string, ulong> _editorToRuntimeId = [];

    public static void Register(string editorId, ulong runtimeId) => _editorToRuntimeId.Add(editorId, runtimeId);

    public static bool TryGetRuntimeId(string editorId, out ulong runtimeId) => _editorToRuntimeId.TryGetValue(editorId, out runtimeId);
}
