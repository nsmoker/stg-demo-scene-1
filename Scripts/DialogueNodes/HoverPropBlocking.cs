using Godot;
using System;

[Tool]
[GlobalClass]
public partial class HoverPropBlocking : DialogueAction
{
    [Export]
    private PropData propData;

    [Export]
    private Vector2 position;

    [Export]
    private float duration;


    public override void Execute(Action onComplete)
    {
        Prop instance = PropSystem.GetInstance(propData);
        instance.HoverToPoint(position, duration, onComplete);
    }
}
