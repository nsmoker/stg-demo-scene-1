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
    public Godot.Collections.Array<CrowdAiTask> Tasks;

    public override void Execute(Action onComplete)
    {
        if (Engine.GetMainLoop() is SceneTree { CurrentScene: MasterScene masterScene })
        {
            var currentScene = masterScene.GetCurrentScreen();
            currentScene.GenericNpcDirector.PossibleTasks = Tasks;
        }

        onComplete?.Invoke();
    }
}
