using System;
using EverydayDialogueEditor;
using Godot;

[Tool]
[GlobalClass]
public partial class CreateProp : DialogueAction
{
    [Export]
    PropData propData;

    [Export]
    Vector2 position;

    public override void Execute(Action onComplete)
    {
        PropSystem.Instantiate(propData, position);
        onComplete?.Invoke();
    }
}