using ArkhamHunters.Scripts;
using Godot;

public partial class AreaTransition : Area2D
{
    private Marker2D _destination;

    [Export]
    public PackedScene DestinationScene;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _destination = GetNode<Marker2D>("DestinationPoint");
    }

    public void OnBodyEntered(Node2D body)
    {
        if (body is Player)
        {
            var masterScene = (MasterScene) GetTree().CurrentScene;
            masterScene.SwitchScene(SceneSystem.GetInstance(DestinationScene.ResourcePath));
            body.GlobalPosition = _destination.GlobalPosition;
        }
        else if (body is Character)
        {
            body.QueueFree();
        }
    }
}
