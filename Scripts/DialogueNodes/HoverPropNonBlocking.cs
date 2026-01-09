using System;
using Godot;

[Tool]
[GlobalClass]
public partial class HoverPropNonBlocking : DialogueAction
{
    [Export]
    PropData propData;

    [Export]
    Vector2 position;

    [Export]
    float duration;


    public override void Execute(Action onComplete)
    {
        Prop instance = PropSystem.GetInstance(propData);
        instance.HoverToPoint(position, duration, () => {});
        onComplete?.Invoke();
    }
}