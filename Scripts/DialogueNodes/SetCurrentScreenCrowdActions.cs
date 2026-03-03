using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.AI;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[GlobalClass]
[Tool]
public partial class SetCurrentScreenCrowdActions : DialogueAction
{
    [Export]
    public Godot.Collections.Array<CrowdAITask> tasks;

    public override void Execute(Action onComplete)
    {
        var masterScene = (Godot.Engine.GetMainLoop() as SceneTree).CurrentScene as MasterScene;
        var currentScene = masterScene.GetCurrentScreen();
        currentScene.GenericNpcDirector.PossibleTasks = tasks;
        onComplete?.Invoke();
    }
}
