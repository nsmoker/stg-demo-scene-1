using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[GlobalClass]
[Tool]
public partial class ClearCurrentScreenNpcs : DialogueAction
{
    public override void Execute(Action onComplete)
    {
        var currentScene = SceneSystem.GetMasterScene().GetCurrentScreen();
        currentScene.ClearNpcs();
    }
}
