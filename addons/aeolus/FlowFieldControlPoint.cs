using Godot;

namespace STGDemoScene1.Addons.Aeolus;

[Tool]
[GlobalClass]
public partial class FlowFieldControlPoint : Resource
{
    [Export]
    public Vector2 ControlPoint = Vector2.Zero;

    [Export]
    public Vector2 Gradient = Vector2.Zero;
}
