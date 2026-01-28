using Godot;
using Godot.Collections;
using System;

public static class SceneSystem
{
    private static readonly Dictionary<string, StagfootScreen> _screens = [];
    private static MasterScene _masterScene;

    public static void Register(string resourcePath, StagfootScreen instance) => _screens[resourcePath] = instance;

    public static StagfootScreen GetInstance(string resourcePath) => _screens[resourcePath];

    public static void SetMasterScene(MasterScene masterScene) => _masterScene = masterScene;

    public static MasterScene GetMasterScene() => _masterScene;
}
