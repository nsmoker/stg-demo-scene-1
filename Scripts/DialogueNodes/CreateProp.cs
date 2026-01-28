using EverydayDialogueEditor;
using Godot;
using System;

[Tool]
[GlobalClass]
public partial class CreateProp : DialogueAction
{
    [Export]
    private PropData propData;

    [Export]
    private Vector2 position;

    public override void Execute(Action onComplete)
    {
        PropSystem.Instantiate(propData, position);
        onComplete?.Invoke();
    }
}
