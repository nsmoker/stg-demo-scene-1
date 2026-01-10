using EverydayDialogueEditor;
using Godot;
using System;

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
