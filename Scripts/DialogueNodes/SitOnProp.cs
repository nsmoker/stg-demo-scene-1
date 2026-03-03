using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class SitOnProp : DialogueAction
{
    [Export]
    private CharacterData _sitter;

    [Export]
    private PropData _propData;

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
        });
        onComplete?.Invoke();
    }
}
