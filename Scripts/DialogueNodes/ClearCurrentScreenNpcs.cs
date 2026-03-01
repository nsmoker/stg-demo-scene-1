using Godot;
using System;

[GlobalClass]
[Tool]
public partial class ClearCurrentScreenNpcs : DialogueAction
{
    public override void Execute(Action onComplete)
    {
        var masterScene = (Godot.Engine.GetMainLoop() as SceneTree).CurrentScene as MasterScene;
        var currentScene = masterScene.GetCurrentScreen();
        currentScene.ClearNpcs();
    }
}
