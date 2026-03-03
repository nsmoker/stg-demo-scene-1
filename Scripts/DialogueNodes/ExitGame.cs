using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[GlobalClass]
[Tool]
public partial class ExitGame : DialogueAction
{
    public override void Execute(Action onComplete)
    {
        (Godot.Engine.GetMainLoop() as SceneTree).Quit();
        onComplete?.Invoke();
    }
}
