using Godot;
using System;
using System.Linq;


[Tool]
[GlobalClass]
public partial class OpenSceneDoors : DialogueAction
{
    public override void Execute(Action onComplete)
    {
        var masterScene = (Godot.Engine.GetMainLoop() as SceneTree).CurrentScene as MasterScene;
        var currentScene = masterScene.GetCurrentScreen();

        var currentSceneDoors = currentScene.GetDoorProps();
        var castSceneDoors = currentSceneDoors.Cast<Prop>();

        foreach (var door in castSceneDoors)
        {
            door.PlayAnimation("DoorOpening");
        }
        onComplete();
    }
}
