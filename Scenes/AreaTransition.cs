using Godot;

public partial class AreaTransition : Area2D
{
    [Export]
    PackedScene Destination;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public void OnBodyEntered(Node2D body)
    {
        if (body is Player)
        {
            var masterScene = (MasterScene) GetTree().CurrentScene;
            masterScene.SwitchScene(SceneSystem.GetInstance(Destination.ResourcePath));
        }
    }
}
