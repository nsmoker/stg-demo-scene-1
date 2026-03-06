
using Godot;
using STGDemoScene1.Scripts.Characters;

namespace STGDemoScene1.Scripts.Triggers;

public partial class Projectile : Area2D
{
    [Export]
    public float MaxLifetime = 5f;

    private Vector2 _velocity;

    public System.Action OnHit;

    private Character _target;

    public override void _Ready() => BodyEntered += OnBodyEntered;

    public override void _Process(double delta)
    {
        float deltaTime = (float) delta;
        GlobalPosition += _velocity * deltaTime;
        if (GlobalPosition.DistanceTo(_target.GlobalPosition) < 16.0f)
        {
            OnHit?.Invoke();
            QueueFree();
        }
    }

    public void Initialize(Vector2 direction, Character target, float speed)
    {
        direction = direction.Normalized();
        Rotation = direction.Angle();
        _velocity = direction * speed;
        _target = target;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is Character character && character == _target)
        {
            OnHit?.Invoke();
            QueueFree();
        }
    }
}

