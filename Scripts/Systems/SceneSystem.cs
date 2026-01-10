using Godot;
using Godot.Collections;
using System;

public static class SceneSystem
{
    private static Dictionary<string, StagfootScreen> _screens = [];

    public static void Register(string resourcePath, StagfootScreen instance)
    {
        _screens[resourcePath] = instance;
    }

    public static StagfootScreen GetInstance(string resourcePath)
    {
        return _screens[(resourcePath)];
    }
}
