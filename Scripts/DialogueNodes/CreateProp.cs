using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class CreateProp : DialogueAction
{
    [Export]
    private PropData _propData;

    [Export]
    private Vector2 _position;

    public override void Execute(Action onComplete)
    {
        PropSystem.Instantiate(_propData, _position);
        onComplete?.Invoke();
    }
}
