using Godot.Collections;

namespace STGDemoScene1.Scripts.Systems;

public static class SceneSystem
{
    private static readonly Dictionary<string, StagfootScreen> s_screens = [];
    private static MasterScene s_masterScene;

    public static void Register(string resourcePath, StagfootScreen instance) => s_screens[resourcePath] = instance;

    public static StagfootScreen GetInstance(string resourcePath) => s_screens[resourcePath];

    public static void SetMasterScene(MasterScene masterScene) => s_masterScene = masterScene;

    public static MasterScene GetMasterScene() => s_masterScene;
}
