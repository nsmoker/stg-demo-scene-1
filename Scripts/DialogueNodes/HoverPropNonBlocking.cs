using Godot;
using STGDemoScene1.Addons.Edi.Scripts;
using STGDemoScene1.Scripts.Resources;
using STGDemoScene1.Scripts.Systems;
using System;

namespace STGDemoScene1.Scripts.DialogueNodes;

[Tool]
[GlobalClass]
public partial class HoverPropNonBlocking : DialogueAction
{
    [Export]
    private PropData _propData;

    [Export]
    private Vector2 _position;

    [Export]
    private float _duration;

    public override void Execute(Action onComplete)
    {
        Prop instance = PropSystem.GetInstance(_propData);
        instance.HoverToPoint(_position, _duration, () => { });
        onComplete?.Invoke();
    }
}
