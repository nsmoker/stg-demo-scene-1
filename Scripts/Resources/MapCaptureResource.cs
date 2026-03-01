using Godot;

[GlobalClass]
[Tool]
public partial class MapCaptureResource : Resource
{
    [Export]
    public Image MapImage;

    [Export]
    public Transform2D LocalTransform;
}
