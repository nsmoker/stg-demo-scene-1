using System;
using EverydayDialogueEditor;
using Godot;

[Tool]
[GlobalClass]
public partial class SitOnPropBlocking : DialogueAction
{
    [Export]
    CharacterData _sitter;

    [Export]
    PropData _propData;

    public override void Execute(Action onComplete)
    {
        var walkTo = new WalkCharToPointBlocking();
        Prop propInstance = PropSystem.GetInstance(_propData);

        walkTo.CharacterData = _sitter;
        walkTo.Point = propInstance.GlobalPosition;
        walkTo.Execute(() =>
        {
            var sitterInstance = CharacterSystem.GetInstance(_sitter.ResourcePath);
            sitterInstance.SitOn(propInstance);
            onComplete?.Invoke();
        });
    }
}