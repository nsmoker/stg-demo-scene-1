using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

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
